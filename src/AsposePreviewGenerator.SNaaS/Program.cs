using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SenseNet.Extensions.DependencyInjection;
using Serilog;
using SNaaS.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SenseNet.TaskManagement.Core;
using Microsoft.Extensions.Logging;

namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogTrace("Starting AsposePreviewGenerator.SNaaS");

            await PreviewGenerator.ExecuteAsync(args, host.Services).ConfigureAwait(false);
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hb, services) => services
                    .AddLogging(logging =>
                    {
                        logging.AddSerilog(new LoggerConfiguration()
                            .ReadFrom.Configuration(hb.Configuration)
                            .CreateLogger());
                    })
                    .Configure<AsposePreviewGeneratorOptions>(options =>
                    {
                        hb.Configuration.GetSection("sensenet:AsposePreviewGenerator").Bind(options);

                        var environmentName = hb.Configuration["NETCORE_ENVIRONMENT"] ?? "Production";
                        options.Environment.IsDevelopment = string.Equals(environmentName, "Development",
                                                       StringComparison.InvariantCultureIgnoreCase);
                    })
                    .AddSingleton<ISnClientProvider, SNaaSClientProvider>()
                    .ConfigureSnaasOptions(hb.Configuration)
                    .AddSnaasSecretStore()
                    .AddSenseNetPreview()
                    .AddSenseNetAsposePreviewGenerators()
                    .AddSenseNetRetrier());
    }
}
