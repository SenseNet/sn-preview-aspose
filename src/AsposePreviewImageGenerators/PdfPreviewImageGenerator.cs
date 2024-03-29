﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Pdf;
using Aspose.Pdf.Devices;
using Microsoft.Extensions.Logging;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class PdfPreviewImageGenerator : PreviewImageGenerator
    {
        private readonly ILogger<PdfPreviewImageGenerator> _logger;

        public PdfPreviewImageGenerator(ILogger<PdfPreviewImageGenerator> logger) : base(logger)
        {
            _logger = logger;
        }

        public override string[] KnownExtensions { get; } = { ".pdf" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace($"Loading pdf document from stream (id {context.ContentId}).");

            var document = new Document(docStream);

            if (context.StartIndex == 0)
                await context.SetPageCountAsync(document.Pages.Count, cancellationToken).ConfigureAwait(false);

            var loggedPageError = false;

            context.SetIndexes(document.Pages.Count, out var firstIndex, out var lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var imgStream = new MemoryStream())
                {
                    try
                    {
                        _logger.LogTrace($"Loading page {i} of file {context.ContentId} (pdf)");

                        var pngDevice = new PngDevice(new Resolution(context.PreviewResolution, context.PreviewResolution));

                        _logger.LogTrace($"Processing page {i} of file {context.ContentId} (pdf)");
                        pngDevice.Process(document.Pages[i + 1], imgStream);

                        if (imgStream.Length == 0)
                        {
                            _logger.LogTrace($"Page {i} of file {context.ContentId} is empty.");
                            continue;
                        }

                        await context.SavePreviewAndThumbnailAsync(imgStream, i + 1, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogTrace($"Exception during preview generation: {ex.Message} {Tools.SerializeException(ex)}");

                        if (await Tools.HandlePageErrorAsync(ex, i + 1, context, !loggedPageError,
                            cancellationToken).ConfigureAwait(false))
                            return;

                        loggedPageError = true;
                    }
                }
            }
        }
    }
}
