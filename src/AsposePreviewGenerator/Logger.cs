using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SenseNet.Client;
using SenseNet.Diagnostics;
using SenseNet.TaskManagement.Core;

namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
{
    internal class Logger
    {
        internal static ILogger Instance { get; set; }
        private static readonly string LOG_PREFIX = "#AsposePreviewGenerator> ";

        internal static void WriteTrace(string message)
        {
            WriteTrace(null, 0, 0, message);
        }
        internal static void WriteTrace(string repository, int contentId, int page, string message)
        {
            Instance?.LogTrace($"{LOG_PREFIX} {message} Repo: {repository} Content id: {contentId}, page number: {page}");
        }

        internal static void WriteInfo(int contentId, int page, string message)
        {
            var msg = $"{LOG_PREFIX} {message} Content id: {contentId}, page number: {page}";
            Trace.WriteLine(msg);
            Instance?.LogInformation(msg);
        }

        internal static void WriteWarning(int contentId, int page, string message)
        {
            WriteInfo(contentId, page, message);

            // this will be recognized and logged by the agent (because of the WARNING prefix)
            Console.WriteLine("WARNING: Content id: {0}, page: {1}. {2}", contentId, page, message);
        }

        internal static void WriteError(int contentId, int page = 0, string message = null, Exception ex = null, int startIndex = 0, string version = "")
        {
            var msg = message == null
                ? "Error during preview generation."
                : message.Replace(Environment.NewLine, " * ");

            Trace.WriteLine($"{LOG_PREFIX} ERROR {msg} Content id: {contentId}, " +
                            $"version: {version}, page number: {page}, start index: {startIndex}, " +
                            $"Exception: {FormatException(ex).Replace(Environment.NewLine, " * ")}");

            if (ex != null)
                Console.WriteLine("ERROR:" + SnTaskError.Create(ex, new
                {
                    ContentId = contentId, 
                    Page = page, 
                    StartIndex = startIndex, 
                    Version = version,
                    Message = msg
                }));

            Instance?.LogError(ex, $"Error during preview generation. {msg} {FormatException(ex)}");
        }

        private static string FormatException(Exception ex)
        {
            return ex switch
            {
                null => string.Empty,
                ClientException cex => $"{cex.Message} StatusCode: {cex.StatusCode} " +
                                       $"ErrorData: {cex.ErrorData?.Message} # " +
                                       $"{cex.ErrorData?.ErrorCode} # " +
                                       $"{cex.ErrorData?.ExceptionType} {cex.ErrorData?.InnerError}",
                _ => ex.ToString(),
            };
        }
    }
}
