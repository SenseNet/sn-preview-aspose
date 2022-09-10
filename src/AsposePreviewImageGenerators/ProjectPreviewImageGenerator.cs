using System.IO;
using System.Threading;
using Aspose.Tasks;
using Aspose.Tasks.Saving;
using Microsoft.Extensions.Logging;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class ProjectPreviewImageGenerator : PreviewImageGenerator
    {
        private readonly ILogger<ProjectPreviewImageGenerator> _logger;
        private readonly PdfPreviewImageGenerator _pdfPreviewImageGenerator;

        public ProjectPreviewImageGenerator(ILogger<ProjectPreviewImageGenerator> logger,
            ILogger<PdfPreviewImageGenerator> pdfLogger) : base(logger)
        {
            // We have to manually create this internal instance because currently we are
            // in the process of instantiating generators and if we tried to load it from
            // DI it would lead to a circular reference.
            _pdfPreviewImageGenerator = new PdfPreviewImageGenerator(pdfLogger);
            _logger = logger;
        }

        public override string[] KnownExtensions { get; } = { ".mpp" };

        public override async System.Threading.Tasks.Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace($"Loading project document from stream (id {context.ContentId}).");

            var document = new Project(docStream);
            
            // This is the simplest way to create a reasonably readable 
            // preview from a project file: convert it to a PDF first.
            using (var pdfStream = new MemoryStream())
            {
                _logger.LogTrace($"Saving project document as pdf (id {context.ContentId}).");

                // save project file in memory as a pdf document
                document.Save(pdfStream, SaveFileFormat.PDF);

                // generate previews from the pdf document
                await _pdfPreviewImageGenerator.GeneratePreviewAsync(pdfStream, context, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
