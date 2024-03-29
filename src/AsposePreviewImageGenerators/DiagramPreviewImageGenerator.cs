﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Diagram;
using Aspose.Diagram.Saving;
using Microsoft.Extensions.Logging;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class DiagramPreviewImageGenerator : PreviewImageGenerator
    {
        private readonly ILogger<DiagramPreviewImageGenerator> _logger;

        public DiagramPreviewImageGenerator(ILogger<DiagramPreviewImageGenerator> logger) : base(logger)
        {
            _logger = logger;
        }

        public override string[] KnownExtensions { get; } = { ".vdw", ".vdx", ".vsd", ".vss", ".vst", ".vsx", ".vtx" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context, 
            CancellationToken cancellationToken)
        {
            var document = new Diagram(docStream);

            if (context.StartIndex == 0)
                await context.SetPageCountAsync(document.Pages.Count, cancellationToken).ConfigureAwait(false);

            var loggedPageError = false;

            context.SetIndexes(document.Pages.Count, out var firstIndex, out var lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogTrace($"Loading page {i} of file {context.ContentId} (diagram)");

                    using (var imgStream = new MemoryStream())
                    {
                        var options = new ImageSaveOptions(SaveFileFormat.PNG)
                        {
                            PageIndex = i,
                            Resolution = context.PreviewResolution
                        };

                        document.Save(imgStream, options);
                        if (imgStream.Length == 0)
                        {
                            _logger.LogTrace($"Page {i} of file {context.ContentId} is empty.");
                            continue;
                        }

                        await context.SavePreviewAndThumbnailAsync(imgStream, i + 1, cancellationToken)
                            .ConfigureAwait(false);
                    }
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
