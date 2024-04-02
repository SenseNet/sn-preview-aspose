using System;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Preview.Aspose;
using SenseNet.Tools;
using SenseNet.Tools.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    [OptionsClass(sectionName: "sensenet:AsposePreviewProvider")]
    public class AsposeOptions
    {
        /// <summary>
        /// If set to false, the system will try to load and use Aspose dlls as a trial version.
        /// </summary>
        public bool SkipLicenseCheck { get; set; }
    }

    public static class AsposeExtensions
    {
        /// <summary>
        /// Adds the Aspose document provider to the service collection.
        /// </summary>
        public static IServiceCollection AddAsposeDocumentPreviewProvider(this IServiceCollection services, 
            Action<AsposeOptions> configureAspose = null)
        {
            services.Configure<AsposeOptions>(options =>
            {
                configureAspose?.Invoke(options);
            });

            return services
                .AddSenseNetPreview()
                .AddSenseNetDocumentPreviewProvider<AsposePreviewProvider>()
                .AddSenseNetAsposePreviewGenerators();
        }
    }
}
