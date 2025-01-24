using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.Client;
using SenseNet.Preview.Aspose.PreviewImageGenerators;
using AsposeTools = SenseNet.Preview.Aspose.PreviewImageGenerators.Tools;
using SenseNet.TaskManagement.Core;
using SenseNet.Tools;
using AsposeWords = Aspose.Words;
using AsposeImaging = Aspose.Imaging;
using AsposeDiagram = Aspose.Diagram;
using AsposeCells = Aspose.Cells;
using AsposePdf = Aspose.Pdf;
using AsposeSlides = Aspose.Slides;
using AsposeEmail = Aspose.Email;
using AsposeTasks = Aspose.Tasks;
using System.Reflection;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Aspose.Words;
using AsposePreviewGenerator.Components;

namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
{
    public class PreviewGenerator
    {
        public static int ContentId { get; set; }
        public static string Version { get; set; }
        public static int StartIndex { get; set; }
        public static int MaxPreviewCount { get; set; }
        public static string SiteUrl { get; set; }
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static string ApiKey { get; set; }
        
        // shortcut
        public static AsposePreviewGeneratorOptions Config => AsposePreviewGeneratorOptions.Instance;

        private static int REQUEST_RETRY_COUNT = 10;
        private static int DELAY_TOO_MANY_REQUESTS = 2000;
        private static string EmptyImage = "empty.png";

        private static SnSubtask _generatingPreviewSubtask;
        private static IPreviewGeneratorManager _previewManager;
        private static IRetrier _retrier;
        private static ISnClientProvider _snClientProvider;

        private static IRepositoryCollection _repositoryCollection;

        public static async Task ExecuteAsync(string[] args, IServiceProvider services, CancellationToken cancel)
        {
            Logger.Instance = services.GetRequiredService<ILogger<PreviewGenerator>>();

            Logger.WriteTrace("Preview generator version: " + typeof(PreviewGenerator).Assembly.GetName().Version);

            if (!ParseParameters(args))
            {
                Logger.WriteError(ContentId, 0,
                    "Aspose preview generator process arguments are not correct.");
                return;
            }

            Logger.WriteTrace("EnableUnixSupport is switched ON because of legacy APIs.");

            var passwordLog = string.IsNullOrEmpty(Password) ? "null" : "[hidden]";
            var apiKeyLog = string.IsNullOrEmpty(ApiKey) ? "null" : $"[{ApiKey[..3]}...]";

            Logger.WriteTrace(SiteUrl, ContentId, 0, $"Parameters: Username: {Username}, " +
                                                     $"Password: {passwordLog} ApiKey: {apiKeyLog} " +
                                                     $"Version: {Version} StartIndex: {StartIndex}");
            try
            {
                if (!await InitializeAsync(services, cancel))
                    return;
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, 0, ex: ex, message: "Error during service initialization.",
                    startIndex: StartIndex, version: Version);
                return;
            }

            try
            {
                //TODO: use the cancellation token to stop image generation manually
                var tokenSource = new CancellationTokenSource();

                await GenerateImagesAsync(tokenSource.Token);
            }
            catch (Exception ex)
            {
                if (AsposeTools.ContentNotFound(ex))
                    return;

                Logger.WriteError(ContentId, 0, ex: ex, startIndex: StartIndex,
                    version: Version);

                await SetPreviewStatusAsync(-3); // PreviewStatus.Error
            }
        }

        public static async Task<bool> InitializeAsync(IServiceProvider services, CancellationToken cancel)
        {
            var config = services.GetRequiredService<IOptions<AsposePreviewGeneratorOptions>>().Value;
            AsposePreviewGeneratorOptions.Initialize(config);

            _previewManager = services.GetRequiredService<IPreviewGeneratorManager>();
            _retrier = services.GetRequiredService<IRetrier>();
            _snClientProvider = services.GetRequiredService<ISnClientProvider>();
            _repositoryCollection = services.GetRequiredService<IRepositoryCollection>();

            ServicePointManager.DefaultConnectionLimit = 10;

            ClientContext.Current.ChunkSizeInBytes = Config.Upload.ChunkSize;

            var server = await _snClientProvider.GetServerContextAsync(SiteUrl, CancellationToken.None)
                .ConfigureAwait(false);

            if (server == null)
                return false;

            server.IsTrusted = Config.Environment.IsDevelopment;

            // set api key if we got one through the command line
            if (!string.IsNullOrEmpty(ApiKey))
            {
                Logger.WriteTrace(SiteUrl, ContentId, 0, "Setting API key");
                server.Authentication.ApiKey = ApiKey;
            }

            ClientContext.Current.AddServer(server);

            var repository = await _repositoryCollection.GetRepositoryAsync(CancellationToken.None);
            var currentUser = await repository.GetCurrentUserAsync(["Id", "Path", "Type", "Name"],
                    Array.Empty<string>(), CancellationToken.None).ConfigureAwait(false);
            
            Logger.WriteTrace(SiteUrl, ContentId, 0, $"Current user: {currentUser?.Path}");

            return true;
        }

        // ================================================================================================== Preview generation

        public static async Task GenerateImagesAsync(CancellationToken cancel)
        {
            int previewsFolderId;
            string contentPath;
            var downloadingSubtask = new SnSubtask("Downloading", "Downloading file and other information");
            downloadingSubtask.Start();

            try
            {
                var fileInfo = await GetFileInfoAsync(cancel).ConfigureAwait(false);
                if (fileInfo == null)
                {
                    Logger.WriteWarning(ContentId, 0, "Content not found.");
                    downloadingSubtask.Finish();
                    return;
                }

                cancel.ThrowIfCancellationRequested();

                previewsFolderId = await GetPreviewsFolderIdAsync();
                if (previewsFolderId < 1)
                {
                    Logger.WriteWarning(ContentId, 0, "Previews folder not found, maybe the content is missing.");
                    downloadingSubtask.Finish();
                    return;
                }

                downloadingSubtask.Progress(10, 100, 2, 110, "File info downloaded.");

                cancel.ThrowIfCancellationRequested();

                contentPath = fileInfo.Path;

                if (Config.ImageGeneration.CheckLicense)
                    CheckLicense(contentPath[(contentPath.LastIndexOf('/') + 1)..]);
                else
                    Logger.WriteTrace(SiteUrl, ContentId, 0, "License check is disabled.");
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, message: "Error during initialization. The process will exit without generating images.", ex: ex, startIndex: StartIndex, version: Version);
                return;
            }

            #region For tests
            //if (contentPath.EndsWith("freeze.txt", StringComparison.OrdinalIgnoreCase))
            //    Freeze();
            //if (contentPath.EndsWith("overflow.txt", StringComparison.OrdinalIgnoreCase))
            //    Overflow();
            #endregion

            await using var docStream = await GetBinaryAsync(cancel);
            if (docStream == null)
            {
                Logger.WriteWarning(ContentId, 0, $"Document not found; maybe the content or its version {Version} is missing.");
                downloadingSubtask.Finish();
                return;
            }

            downloadingSubtask.Progress(100, 100, 10, 110, "File downloaded.");

            cancel.ThrowIfCancellationRequested();

            if (docStream.Length == 0)
            {
                await SetPreviewStatusAsync(0); // PreviewStatus.EmptyDocument
                downloadingSubtask.Finish();
                return;
            }
            downloadingSubtask.Finish();

            _generatingPreviewSubtask = new SnSubtask("Generating images");
            _generatingPreviewSubtask.Start();

            var extension = contentPath.Substring(contentPath.LastIndexOf('.'));

            Logger.WriteTrace(SiteUrl, ContentId, 0, "Generating images.");

            await _previewManager.GeneratePreviewAsync(extension, docStream, new PreviewGenerationContext(
                ContentId, previewsFolderId, StartIndex, MaxPreviewCount, 
                Config.ImageGeneration.PreviewResolution, Version), cancel).ConfigureAwait(false);

            _generatingPreviewSubtask.Finish();
        }

        // ================================================================================================== Communication with the portal

        private static async Task<int> GetPreviewsFolderIdAsync()
        {
            try
            {
                Logger.WriteTrace(SiteUrl, ContentId, 0, "Getting preview folder info.");

                var previewsFolder = await GetResponseJsonAsync(new ODataRequest
                    {
                        ContentId = ContentId,
                        ActionName = "GetPreviewsFolder",
                        Version = Version
                    },
                    HttpMethod.Post,
                    new {empty = StartIndex == 0}).ConfigureAwait(false);

                return previewsFolder.Id;
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, 0,$"GetPreviewsFolderId error: {ex.Message}", ex, version: Version);
            }

            return 0;
        }
        private static async Task<Stream> GetBinaryAsync(CancellationToken cancel)
        {
            Logger.WriteTrace(SiteUrl, ContentId, 0, "Downloading file.");

            var documentStream = new MemoryStream();

            try
            {
                // download the whole file from the server
                var repository = await _repositoryCollection.GetRepositoryAsync(cancel);
                await repository.DownloadAsync(new DownloadRequest {ContentId = ContentId, Version = Version}, 
                    async (stream, properties) => {
                        if (stream == null)
                            throw new ClientException($"Content {ContentId} {Version} not found.", HttpStatusCode.NotFound);

                        await stream.CopyToAsync(documentStream, cancel).ConfigureAwait(false);
                        documentStream.Seek(0, SeekOrigin.Begin);
                    }, cancel).ConfigureAwait(false);
            }
            catch (ClientException ex)
            {
                if (AsposeTools.ContentNotFound(ex))
                    return null;

                Logger.WriteError(ContentId, 0, "Error during remote file access.", ex, StartIndex, Version);

                // We need to throw the error further to let the main catch block
                // log the error and set the preview status to 'Error'.
                throw;
            }

            return documentStream;
        }
        private static async Task<Content> GetFileInfoAsync(CancellationToken cancel)
        {
            Logger.WriteTrace(SiteUrl, ContentId, 0, "Downloading file info.");

            var repository = await _repositoryCollection.GetRepositoryAsync(cancel);
            return await repository.LoadContentAsync(new LoadContentRequest
            {
                ContentId = ContentId,
                Select = new[] { "Name", "DisplayName", "Path", "CreatedById" },
                Version = Version,
                Metadata = MetadataFormat.None
            }, cancel);
        }

        public static Task SetPreviewStatusAsync(int status)
        {
            // PREVIEWSTATUS ENUM in document provider

            // NoProvider = -5,
            // Postponed = -4,
            // Error = -3,
            // NotSupported = -2,
            // InProgress = -1,
            // EmptyDocument = 0,
            // Ready = 1

            Logger.WriteTrace(SiteUrl, ContentId, 0, $"Setting preview status to {status}.");

            return PostAsync("SetPreviewStatus", new {status});
        }
        public static Task SetPageCountAsync(int pageCount)
        {
            Logger.WriteTrace(SiteUrl, ContentId, 0, $"Setting page count to {pageCount}.");

            return PostAsync("SetPageCount", new { pageCount });
        }

        public static async Task SavePreviewAndThumbnailAsync(Stream imageStream, int page, int previewsFolderId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger.WriteTrace(SiteUrl, ContentId, page, "Saving main preview image.");

            // save main preview image
            await SaveImageStreamAsync(imageStream, GetPreviewNameFromPageNumber(page), page, 
                Common.PREVIEW_WIDTH, Common.PREVIEW_HEIGHT, previewsFolderId, cancellationToken);

            var progress = ((page - StartIndex) * 2 - 1) * 100 / MaxPreviewCount / 2;
            _generatingPreviewSubtask.Progress(progress, 100, progress + 10, 110);

            cancellationToken.ThrowIfCancellationRequested();

            Logger.WriteTrace(SiteUrl, ContentId, page, "Saving thumbnail image.");

            // save smaller image for thumbnail
            await SaveImageStreamAsync(imageStream, GetThumbnailNameFromPageNumber(page), page, 
                Common.THUMBNAIL_WIDTH, Common.THUMBNAIL_HEIGHT, previewsFolderId, cancellationToken);

            progress = (page - StartIndex) * 2 * 100 / MaxPreviewCount / 2;
            _generatingPreviewSubtask.Progress(progress, 100, progress + 10, 110);
        }

        private static async Task SaveImageStreamAsync(Stream? imageStream, string name, int page, int width, int height, 
            int previewsFolderId, CancellationToken cancellationToken)
        {
            if (imageStream == null)
                return;

            imageStream.Seek(0, SeekOrigin.Begin);

            // create an in-memory copy of the stream to avoid disposing the original stream
            await using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream, 81920, cancellationToken).ConfigureAwait(false);
            memoryStream.Seek(0, SeekOrigin.Begin);

            using var sourceBitmap = SKBitmap.Decode(memoryStream);
            using var image = ResizeImage(sourceBitmap, width, height);

            if (image == null)
            {
                Logger.WriteTrace(SiteUrl, ContentId, page, "Resized image is null, aborting save operation.");
                return;
            }

            Logger.WriteTrace(SiteUrl, ContentId, page, "Encoding resized image....");
            var encodedImageData = image.Encode();
            await using var convertedMemStream = encodedImageData.AsStream();
            
            await SaveImageStreamAsync(convertedMemStream, name, page, previewsFolderId, cancellationToken);
        }
        private static async Task SaveImageStreamAsync(Stream imageStream, string previewName, int page, int previewsFolderId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            imageStream.Seek(0, SeekOrigin.Begin);

            try
            {
                var imageId = await UploadImageAsync(imageStream, previewsFolderId, previewName, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                Logger.WriteTrace(SiteUrl, ContentId, page, "Setting initial preview properties...");

                // set initial preview image properties (CreatedBy, Index, etc.)
                await PostAsync(imageId, "SetInitialPreviewProperties");
            }
            catch (ClientException ex)
            {
                // a 404 must be handled by the caller
                if (AsposeTools.ContentNotFound(ex))
                    throw;

                // node is out of date is not an error
                if (ex.ErrorData?.ExceptionType?.Contains("NodeIsOutOfDateException") ?? false)
                    return;

                Logger.WriteError(ContentId, page, ex.Message, ex, StartIndex, Version);
                
                // in case of status 500, we still have to terminate the process after logging the error
                if (AsposeTools.IsTerminatorError(ex))
                    throw;
            }
        }
        public static async Task SaveEmptyPreviewAsync(int page, int previewsFolderId, CancellationToken cancellationToken)
        {
            Logger.WriteTrace($"Saving empty image for page {page} of document {ContentId} in repository {SiteUrl}");

            var myAssembly = Assembly.GetExecutingAssembly();
            await using var emptyImageStream =
                myAssembly.GetManifestResourceStream("AsposePreviewGenerator.Components.empty.png");
            
            await SaveImageAsync(emptyImageStream, page, previewsFolderId, cancellationToken);
        }
        public static Task SaveImageAsync(Stream imgStream, int page, int previewsFolderId, CancellationToken cancellationToken)
        {
            return SavePreviewAndThumbnailAsync(imgStream, page, previewsFolderId, cancellationToken);
        }

        private static async Task<int> UploadImageAsync(Stream imageStream, int previewsFolderId, string imageName,
            CancellationToken cancel)
        {
            Logger.WriteTrace(SiteUrl, ContentId, 0, $"Uploading image {imageName}.");

            var repository = await _repositoryCollection.GetRepositoryAsync(CancellationToken.None);
            repository.UploadAsync(new UploadRequest
            {
                ContentType = "PreviewImage", ContentName = imageName, ParentId = previewsFolderId
            }, imageStream, cancel);







            return await _retrier.RetryAsync(async () =>
                {
                    imageStream.Seek(0, SeekOrigin.Begin);
                    var uploadResult = await repository.UploadAsync(new UploadRequest
                            {ContentType = "PreviewImage", ContentName = imageName, ParentId = previewsFolderId},
                        imageStream, cancel);
                    return uploadResult.Id;
                },
                shouldRetryOnError: (ex, i) =>
                {
                    if (AsposeTools.ContentNotFound(ex))
                        return false;

                    // if the server is too busy, wait longer
                    if (ex is ClientException { StatusCode: HttpStatusCode.TooManyRequests })
                    {
                        Task.Delay(DELAY_TOO_MANY_REQUESTS, cancel).GetAwaiter().GetResult();
                    }

                    return true;
                },
                cancel: cancel);

            //return Retrier.RetryAsync(REQUEST_RETRY_COUNT, 50, async () =>
            //{
            //    imageStream.Seek(0, SeekOrigin.Begin);

            //    var image = await Content.UploadAsync(previewsFolderId, imageName,
            //        imageStream, "PreviewImage").ConfigureAwait(false);

            //    return image.Id;
            //}, (result, count, ex) =>
            //{
            //    if (ex == null)
            //        return true;

            //    if (count == 1 || AsposeTools.ContentNotFound(ex))
            //        throw ex;

            //    cancellationToken.ThrowIfCancellationRequested();

            //    // if the server is too busy, wait longer
            //    if (ex is ClientException { StatusCode: HttpStatusCode.TooManyRequests })
            //        Task.Delay(DELAY_TOO_MANY_REQUESTS, cancellationToken);

            //    return false;
            //}, cancellationToken);
        }

        // ================================================================================================== Helper methods

        public static void SetIndexes(int originalStartIndex, int pageCount, out int startIndex, out int lastIndex, int maxPreviewCount)
        {
            startIndex = Math.Min(originalStartIndex, pageCount - 1);
            lastIndex = Math.Min(startIndex + maxPreviewCount - 1, pageCount - 1);
        }

        private static string GetPreviewNameFromPageNumber(int page)
        {
            return string.Format(Common.PREVIEW_IMAGENAME, page);
        }
        private static string GetThumbnailNameFromPageNumber(int page)
        {
            return string.Format(Common.THUMBNAIL_IMAGENAME, page);
        }
        
        private static SKImage? ResizeImage(SKBitmap? sourceBitmap, int maxWidth, int maxHeight)
        {
            if (sourceBitmap == null)
                return null;

            SKImage image;

            maxWidth = Math.Min(maxWidth, sourceBitmap.Width);
            maxHeight = Math.Min(maxHeight, sourceBitmap.Height);

            // do not scale up the image: resize only if it is larger than the target size
            if (sourceBitmap.Width > maxWidth || sourceBitmap.Height > maxHeight)
            {
                ComputeResizedDimensions(sourceBitmap.Width, sourceBitmap.Height, maxWidth, maxHeight, 
                    out var newWidth, out var newHeight);

                Logger.WriteTrace(SiteUrl, ContentId, 0, "Resizing image " +
                                                         $"from {sourceBitmap.Width} x {sourceBitmap.Height} " +
                                                         $"to {newWidth} x {newHeight}.");

                try
                {
                    using var scaledBitmap = sourceBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);

                    image = SKImage.FromBitmap(scaledBitmap);
                }
                catch (OutOfMemoryException ex)
                {
                    Logger.WriteError(ContentId, message: "Out of memory error during image resizing.", 
                        ex: ex, startIndex: StartIndex, version: Version);

                    return null;
                }
            }
            else
            {
                image = SKImage.FromBitmap(sourceBitmap);
            }

            return image;
        }

        private static void ComputeResizedDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight, out int newWidth, out int newHeight)
        {
            // do not scale up the image
            if (originalWidth <= maxWidth && originalHeight <= maxHeight)
            {
                newWidth = originalWidth;
                newHeight = originalHeight;
                return;
            }

            var percentWidth = (float)maxWidth / (float)originalWidth;
            var percentHeight = (float)maxHeight / (float)originalHeight;

            // determine which dimension scale should we use (the smaller)
            var percent = percentHeight < percentWidth ? percentHeight : percentWidth;

            // Compute new width and height, based on the final scale. Do not
            // allow values smaller than 1 because that would be an invalid
            // value for bitmap dimensions.
            newWidth = Math.Max(1, (int)Math.Round(originalWidth * percent));
            newHeight = Math.Max(1, (int)Math.Round(originalHeight * percent));
        }

        private static Task<string> PostAsync(string actionName, object body = null)
        {
            return PostAsync(ContentId, actionName, body);
        }
        private static Task<string> PostAsync(int contentId, string actionName, object body = null)
        {
            var bodyText = body == null
                ? null
                : JsonHelper.Serialize(body);

            return GetResponseStringAsync(new ODataRequest
            {
                ContentId = contentId,
                ActionName = actionName
            }, HttpMethod.Post, bodyText);
        }

        private static async Task<string> GetResponseStringAsync(ODataRequest request, HttpMethod? method = null, string? body = null)
        {
            if(body != null)
                request.PostData = body;

            var repository = await _repositoryCollection.GetRepositoryAsync(CancellationToken.None);
            return await _retrier.RetryAsync<string>(
                action: async () => await repository.GetResponseStringAsync(request, method ?? HttpMethod.Get, CancellationToken.None),
                shouldRetryOnError: (ex, i) =>
                {
                    if (AsposeTools.ContentNotFound(ex))
                        return false;

                    // if the server is too busy, wait longer
                    if (ex is ClientException { StatusCode: HttpStatusCode.TooManyRequests })
                    {
                        Thread.Sleep(DELAY_TOO_MANY_REQUESTS);
                    }

                    return true;
                });
        }

        private static async Task<dynamic> GetResponseJsonAsync(ODataRequest request, HttpMethod? method = null, object? body = null)
        {
            if (body != null)
                request.PostData = body;

            var repository = await _repositoryCollection.GetRepositoryAsync(CancellationToken.None);
            
            return await _retrier.RetryAsync(
                async () => await repository.GetResponseJsonAsync(request, method ?? HttpMethod.Get, CancellationToken.None),
                shouldRetryOnError: (ex, i) =>
                {
                    if (AsposeTools.ContentNotFound(ex))
                        return false;

                    // if the server is too busy, wait longer
                    if (ex is ClientException { StatusCode: HttpStatusCode.TooManyRequests })
                    {
                        Thread.Sleep(DELAY_TOO_MANY_REQUESTS);
                    }

                    return true;
                });
        }
        
        public static bool ParseParameters(string[] args)
        {
            var parser = new PreviewGeneratorArgumentParser();
            if(!parser.TryParse(args, out var parsed))
                return false;

            Username = parsed.Username;
            Password = parsed.Password;
            ApiKey = parsed.ApiKey;
            ContentId = parsed.ContentId;
            Version = parsed.Version;
            StartIndex = parsed.StartIndex;
            MaxPreviewCount = parsed.MaxPreviewCount;
            SiteUrl = parsed.SiteUrl;

            return true;
        }

        private static void CheckLicense(string fileName)
        {
            Logger.WriteTrace(SiteUrl, ContentId, 0, "Checking Aspose license.");

            var extension = fileName[fileName.LastIndexOf('.')..].ToLower();

            try
            {
                string licenseClass = null;

                // imaging is always required
                new AsposeImaging.License().SetLicense(Constants.LicensePath);

                if (Common.WORD_EXTENSIONS.Contains(extension))
                {
                    new AsposeWords.License().SetLicense(Constants.LicensePath);
                    licenseClass = "AsposeWords";
                }
                else if (Common.IMAGE_EXTENSIONS.Contains(extension) || Common.TIFF_EXTENSIONS.Contains(extension))
                {
                    // imaging is already checked above
                    licenseClass = "AsposeImaging";
                }
                else if (Common.DIAGRAM_EXTENSIONS.Contains(extension))
                {
                    new AsposeDiagram.License().SetLicense(Constants.LicensePath);
                    licenseClass = "AsposeDiagram";
                }
                else if (Common.WORKBOOK_EXTENSIONS.Contains(extension))
                {
                    new AsposeCells.License().SetLicense(Constants.LicensePath);
                    licenseClass = "AsposeCells";
                }
                else if (Common.PDF_EXTENSIONS.Contains(extension))
                {
                    new AsposePdf.License().SetLicense(Constants.LicensePath);
                    licenseClass = "AsposePdf";
                }
                else if (Common.PRESENTATION_EXTENSIONS.Contains(extension) ||
                         Common.PRESENTATIONEX_EXTENSIONS.Contains(extension))
                {
                    new AsposeSlides.License().SetLicense(Constants.LicensePath);
                    licenseClass = "AsposeSlides";
                }
                else if (Common.EMAIL_EXTENSIONS.Contains(extension))
                {
                    // we use Aspose.Word for generating preview images from msg files
                    new AsposeEmail.License().SetLicense(Constants.LicensePath);
                    new AsposeWords.License().SetLicense(Constants.LicensePath);
                    licenseClass = "AsposeEmail";
                }
                else if (Common.PROJECT_EXTENSIONS.Contains(extension))
                {
                    // we use Aspose.Pdf for generating preview images from mpp files
                    new AsposeTasks.License().SetLicense(Constants.LicensePath);
                    new AsposePdf.License().SetLicense(Constants.LicensePath);
                    licenseClass = "AsposeTasks";
                }

                Logger.WriteTrace(SiteUrl, ContentId, 0,
                    string.IsNullOrEmpty(licenseClass)
                        ? "WARNING: unknown license class."
                        : $"License check SUCCESSFUL [{licenseClass}].");
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, message: "Error during license check. ", ex: ex, startIndex: StartIndex, version: Version);
            }
        }
    }
}
