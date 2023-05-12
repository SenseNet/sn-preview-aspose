using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Cells;
using Aspose.Cells.Drawing;
using Aspose.Cells.Rendering;
using Microsoft.Extensions.Logging;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class WorkBookPreviewImageGenerator : PreviewImageGenerator
    {
        private readonly ILogger<WorkBookPreviewImageGenerator> _logger;

        public WorkBookPreviewImageGenerator(ILogger<WorkBookPreviewImageGenerator> logger) : base(logger)
        {
            _logger = logger;
        }

        public override string[] KnownExtensions { get; } = { ".ods", ".xls", ".xlsm", ".xlsx", ".xltm", ".xltx" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace($"Loading excel document from stream (id {context.ContentId}).");

            var document = new Workbook(docStream);
            var printOptions = new ImageOrPrintOptions
            {
                ImageType = GetImageType(),
                OnePagePerSheet = false,
                HorizontalResolution = context.PreviewResolution,
                VerticalResolution = context.PreviewResolution
            };
            
            // every worksheet may contain multiple pages (as set by Excel 
            // automatically, or by the user using the print layout)
            var estimatedPageCount = document.Worksheets.Select(w => new SheetRender(w, printOptions).PageCount).Sum();

            _logger.LogTrace($"Excel document estimated page count is {estimatedPageCount} (id {context.ContentId}).");

            if (context.StartIndex == 0)
                await context.SetPageCountAsync(estimatedPageCount, cancellationToken).ConfigureAwait(false);

            context.SetIndexes(estimatedPageCount, out var firstIndex, out var lastIndex);

            var workbookPageIndex = 0;
            var worksheetIndex = 0;
            var loggedPageError = false;

            // iterate through worksheets
            while (worksheetIndex < document.Worksheets.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogTrace($"Loading worksheet index {worksheetIndex} of file {context.ContentId} (excel document)");

                    var worksheet = document.Worksheets[worksheetIndex];
                    var sheetRender = new SheetRender(worksheet, printOptions);

                    // if we need to start preview generation on a subsequent worksheet, skip the previous ones
                    if (workbookPageIndex + sheetRender.PageCount < context.StartIndex)
                    {
                        workbookPageIndex += sheetRender.PageCount;
                        worksheetIndex++;
                        continue;
                    }

                    // iterate through pages inside a worksheet
                    for (var worksheetPageIndex = 0; worksheetPageIndex < sheetRender.PageCount; worksheetPageIndex++)
                    {
                        // if the desired page interval contains this page, generate the image
                        if (workbookPageIndex >= firstIndex && workbookPageIndex <= lastIndex)
                        {
                            using (var imgStream = new MemoryStream())
                            {
                                _logger.LogTrace($"Converting worksheet page {worksheetPageIndex} of file {context.ContentId} (excel document)");

                                sheetRender.ToImage(worksheetPageIndex, imgStream);

                                // handle empty sheets
                                if (imgStream.Length == 0)
                                    await context.SaveEmptyPreviewAsync(workbookPageIndex + 1, cancellationToken)
                                        .ConfigureAwait(false);
                                else
                                    await context.SavePreviewAndThumbnailAsync(imgStream, workbookPageIndex + 1, 
                                        cancellationToken).ConfigureAwait(false);
                            }
                        }

                        workbookPageIndex++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogTrace($"Exception during preview generation: {ex.Message} {Tools.SerializeException(ex)}");

                    if (await Tools.HandlePageErrorAsync(ex, workbookPageIndex + 1, context, !loggedPageError,
                        cancellationToken).ConfigureAwait(false))
                        return;

                    loggedPageError = true;
                    workbookPageIndex++;
                }

                worksheetIndex++;
            }

            // set the real count if some of the sheets turned out to be empty
            if (workbookPageIndex < estimatedPageCount)
                await context.SetPageCountAsync(workbookPageIndex, cancellationToken).ConfigureAwait(false);
        }

        private static ImageType GetImageType()
        {
            if (Common.PREVIEWIMAGEFORMAT.Equals("png", StringComparison.InvariantCultureIgnoreCase))
                return ImageType.Png;
            if (Common.PREVIEWIMAGEFORMAT.Equals("jpeg", StringComparison.InvariantCultureIgnoreCase))
                return ImageType.Jpeg;
            if (Common.PREVIEWIMAGEFORMAT.Equals("bmp", StringComparison.InvariantCultureIgnoreCase))
                return ImageType.Bmp;
            if (Common.PREVIEWIMAGEFORMAT.Equals("gif", StringComparison.InvariantCultureIgnoreCase))
                return ImageType.Gif;

            return ImageType.Png;
        }
    }
}
