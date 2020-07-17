using System;
using SenseNet.Preview;
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
        public static IRepositoryBuilder UseAsposeDocumentPreviewProvider(this IRepositoryBuilder repositoryBuilder,
            Action<AsposeOptions> configure = null)
        {
            var options = new AsposeOptions();
            configure?.Invoke(options);

            var provider = new AsposePreviewProvider
            {
                SkipLicenseCheck = options.SkipLicenseCheck
            };

            repositoryBuilder.UseDocumentPreviewProvider(provider);
            
            return repositoryBuilder;
        }
    }
}
