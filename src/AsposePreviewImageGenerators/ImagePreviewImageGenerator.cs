using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using SeekOrigin = System.IO.SeekOrigin;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class ImagePreviewImageGenerator : PreviewImageGenerator
    {
        private readonly ILogger<ImagePreviewImageGenerator> _logger;

        public ImagePreviewImageGenerator(ILogger<ImagePreviewImageGenerator> logger) : base(logger)
        {
            _logger = logger;
        }

        public override string[] KnownExtensions { get; } = { ".gif", ".jpg", ".jpeg", ".bmp", ".png", ".svg", ".exif", ".icon" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            docStream.Seek(0, SeekOrigin.Begin);

            _logger.LogTrace($"Loading image from stream (id {context.ContentId}).");

            using var sourceBitmap = SKBitmap.Decode(docStream);

            if (context.StartIndex == 0)
                await context.SetPageCountAsync(1, cancellationToken).ConfigureAwait(false);

            using var imgStream = new MemoryStream();
            var result = sourceBitmap.Encode(imgStream, SKEncodedImageFormat.Png, 100);

            _logger.LogTrace(result
                ? $"Image conversion succeeded (id {context.ContentId})."
                : $"Image conversion FAILED (id {context.ContentId}).");
            
            try
            {
                await context.SavePreviewAndThumbnailAsync(imgStream, 1, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Tools.IsTerminatorError(ex as WebException))
                {
                    context.LogWarning(1, SR.Exceptions.NotFound);
                    return;
                }

                // the preview generator tool will handle the error
                throw;
            }
        }
    }
}
