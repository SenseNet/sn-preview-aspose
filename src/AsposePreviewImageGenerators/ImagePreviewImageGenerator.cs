using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Imaging;
using Aspose.Imaging.ImageOptions;
using Microsoft.Extensions.Logging;
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

            var document = Image.Load(docStream);

            if (context.StartIndex == 0)
                await context.SetPageCountAsync(1, cancellationToken).ConfigureAwait(false);

            using (var imgStream = new MemoryStream())
            {
                var options = new PngOptions();
                document.Save(imgStream, options);

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
}
