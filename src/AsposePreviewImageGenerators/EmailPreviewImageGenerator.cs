using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Email;
using Microsoft.Extensions.Logging;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class EmailPreviewImageGenerator : PreviewImageGenerator
    {
        private readonly ILogger<EmailPreviewImageGenerator> _logger;
        private readonly WordPreviewImageGenerator _wordPreviewImageGenerator;

        public EmailPreviewImageGenerator(ILogger<EmailPreviewImageGenerator> logger,
            ILogger<WordPreviewImageGenerator> wordLogger) : base(logger)
        {
            // We have to manually create this internal instance because currently we are
            // in the process of instantiating generators and if we tried to load it from
            // DI it would lead to a circular reference.
            _wordPreviewImageGenerator = new WordPreviewImageGenerator(wordLogger);
            _logger = logger;
        }

        public override string[] KnownExtensions { get; } = { ".msg" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace($"Loading email from stream (id {context.ContentId}).");

            var email = MailMessage.Load(docStream);

            using (var emailStream = new MemoryStream())
            {
                email.Save(emailStream, SaveOptions.DefaultMhtml);
                emailStream.Position = 0;

                await _wordPreviewImageGenerator.GeneratePreviewAsync(emailStream, context, 
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
