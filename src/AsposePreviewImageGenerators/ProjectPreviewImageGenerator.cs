using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public ProjectPreviewImageGenerator(IEnumerable<IPreviewImageGenerator> generators, 
            ILogger<ProjectPreviewImageGenerator> logger) : base(logger)
        {
            _pdfPreviewImageGenerator = generators.FirstOrDefault(pg => pg is PdfPreviewImageGenerator)
                as PdfPreviewImageGenerator;
            _logger = logger;
        }

        public override string[] KnownExtensions { get; } = { ".mpp" };

        public override async System.Threading.Tasks.Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            var document = new Project(docStream);
            
            // This is the simplest way to create a reasonably readable 
            // preview from a project file: convert it to a PDF first.
            using (var pdfStream = new MemoryStream())
            {
                // save project file in memory as a pdf document
                document.Save(pdfStream, SaveFileFormat.PDF);

                // generate previews from the pdf document
                await _pdfPreviewImageGenerator.GeneratePreviewAsync(pdfStream, context, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
