using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public EmailPreviewImageGenerator(IEnumerable<IPreviewImageGenerator> generators,
            ILogger<EmailPreviewImageGenerator> logger) : base(logger)
        {
            _wordPreviewImageGenerator = generators.FirstOrDefault(pg => pg is WordPreviewImageGenerator) 
                as WordPreviewImageGenerator;
            _logger = logger;
        }

        public override string[] KnownExtensions { get; } = { ".msg" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
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
