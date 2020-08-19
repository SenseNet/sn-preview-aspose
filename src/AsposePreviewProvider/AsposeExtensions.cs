using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository.Storage;
using SenseNet.Preview.Aspose;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public class AsposeOptions
    {
        public bool SkipLicenseCheck { get; set; }
    }

    public static class AsposeExtensions
    {
        [Obsolete("Please use the AddAsposeDocumentPreviewProvider method instead.", true)]
        public static IRepositoryBuilder UseAsposeDocumentPreviewProvider(this IRepositoryBuilder repositoryBuilder,
            Action<AsposeOptions> configure = null)
        {
            return repositoryBuilder;
        }

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

            return services.AddSenseNetDocumentPreviewProvider<AsposePreviewProvider>();
        }
    }
}
