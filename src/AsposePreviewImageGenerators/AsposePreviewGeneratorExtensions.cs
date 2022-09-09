using Microsoft.Extensions.DependencyInjection;
using SenseNet.Preview.Aspose.PreviewImageGenerators;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class AsposePreviewGeneratorExtensions
    {
        /// <summary>
        /// Adds all sensenet Aspose preview generator implementations to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetAsposePreviewGenerators(this IServiceCollection services)
        {
            return services.AddSenseNetPreviewGenerator<DiagramPreviewImageGenerator>()
                .AddSenseNetPreviewGenerator<EmailPreviewImageGenerator>()
                .AddSenseNetPreviewGenerator<ImagePreviewImageGenerator>()
                .AddSenseNetPreviewGenerator<PdfPreviewImageGenerator>()
                .AddSenseNetPreviewGenerator<PresentationPreviewImageGenerator>()
                .AddSenseNetPreviewGenerator<ProjectPreviewImageGenerator>()
                .AddSenseNetPreviewGenerator<TiffPreviewImageGenerator>()
                .AddSenseNetPreviewGenerator<WordPreviewImageGenerator>()
                .AddSenseNetPreviewGenerator<WorkBookPreviewImageGenerator>();
        }
    }
}
