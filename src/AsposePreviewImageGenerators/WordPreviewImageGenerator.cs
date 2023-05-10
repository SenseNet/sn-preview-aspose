using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Words;
using Aspose.Words.Saving;
using Microsoft.Extensions.Logging;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class WordPreviewImageGenerator : PreviewImageGenerator
    {
        private readonly ILogger<WordPreviewImageGenerator> _logger;

        public WordPreviewImageGenerator(ILogger<WordPreviewImageGenerator> logger) : base(logger)
        {
            _logger = logger;
        }

        public override string[] KnownExtensions { get; } = { ".doc", ".docx", ".odt", ".rtf", ".txt", ".xml", ".csv" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace($"Loading word document from stream (id {context.ContentId}).");

            var document = new Document(docStream);
            var pc = document.PageCount;
            
            // save the document only if this is the first round
            if (context.StartIndex == 0 || pc < 1)
                await context.SetPageCountAsync(pc, cancellationToken).ConfigureAwait(false);

            if (pc <= 0)
            {
                _logger.LogTrace($"Loaded document page count is {pc}, finishing operation (id {context.ContentId}).");
                return;
            }

            var loggedPageError = false;

            context.SetIndexes(pc, out var firstIndex, out var lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogTrace($"Loading page {i} of file {context.ContentId} (word document)");

                    using var imgStream = new MemoryStream();
                    var options = new ImageSaveOptions(SaveFormat.Png)
                    {
                        PageSet = new PageSet(i),
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
