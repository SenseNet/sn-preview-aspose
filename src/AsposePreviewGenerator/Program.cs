using System;
using System.Threading.Tasks;
using AsposePreviewGenerator.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SenseNet.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Configuration;
using SenseNet.TaskManagement.Core;

namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // This is a workaround for the Aspose libraries using the
            // System.Drawing.Common library, which is not supported on Linux.
            //TODO: remove this line when the issue is fixed in Aspose
            AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);

            var host = CreateHostBuilder(args).Build();

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
                    .AddSingleton<ISnClientProvider, DefaultSnClientProvider>()
                    .AddSenseNetClientTokenStore()
                    .AddSenseNetPreview()
                    .AddSenseNetAsposePreviewGenerators()
                    .AddSenseNetRetrier());
    }
}
