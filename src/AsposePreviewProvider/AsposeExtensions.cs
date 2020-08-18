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
        public static IServiceCollection AddAsposeDocumentPreviewProvider(this IServiceCollection services, IConfiguration configuration = null)
        {
            if (configuration != null)
                services.Configure<AsposeOptions>(configuration.GetSection("sensenet:AsposePreviewProvider"));

            //UNDONE: do not call AddSingleton directly. Call the AddSenseNetDocumentPreviewProvider method
            // when it is available. Currently it is in the Services.Core project which is not referenced here.
            return services.AddSingleton<IPreviewProvider, AsposePreviewProvider>();
        }
    }
}
