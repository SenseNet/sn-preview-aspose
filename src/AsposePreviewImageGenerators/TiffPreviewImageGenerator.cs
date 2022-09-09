using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Imaging;
using Aspose.Imaging.FileFormats.Tiff;
using Aspose.Imaging.ImageOptions;
using Microsoft.Extensions.Logging;
using SeekOrigin = System.IO.SeekOrigin;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class TiffPreviewImageGenerator : PreviewImageGenerator
    {
        private readonly ILogger<TiffPreviewImageGenerator> _logger;

        public TiffPreviewImageGenerator(ILogger<TiffPreviewImageGenerator> logger) : base(logger)
        {
            _logger = logger;
        }

        public override string[] KnownExtensions { get; } = { ".tif", ".tiff" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            docStream.Seek(0, SeekOrigin.Begin);

            _logger.LogTrace($"Loading tiff image from stream (id {context.ContentId}).");

            var document = (TiffImage)Image.Load(docStream);

            if (context.StartIndex == 0)
                await context.SetPageCountAsync(document.Frames.Length, cancellationToken).ConfigureAwait(false);

            var loggedPageError = false;

            context.SetIndexes(document.Frames.Length, out var firstIndex, out var lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogTrace($"Loading page {i} of tiff image {context.ContentId}.");

                    document.ActiveFrame = document.Frames[i];
                    using (var imgStream = new MemoryStream())
                    {
                        var options = new PngOptions();
                        document.Save(imgStream, options);

                        await context.SavePreviewAndThumbnailAsync(imgStream, i + 1, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    if (await Tools.HandlePageErrorAsync(ex, i + 1, context, !loggedPageError,
                        cancellationToken).ConfigureAwait(false))
                        return;

                    loggedPageError = true;
                }
            }
        }
    }
}
