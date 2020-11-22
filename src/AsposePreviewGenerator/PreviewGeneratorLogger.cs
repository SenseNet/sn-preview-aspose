using Microsoft.Extensions.Logging;
using EventId = Microsoft.Extensions.Logging.EventId;
using System;

namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
{
    internal class PreviewGeneratorLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);

            switch (logLevel)
            {
                case LogLevel.Information:
                    Logger.WriteInfo(0, 0, message);
                    break;
                case LogLevel.Warning:
                    Logger.WriteWarning(0, 0, message);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Logger.WriteWarning(0, 0, message);
                    break;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
    internal class PreviewGeneratorLoggerProvider : ILoggerProvider
    {
        public void Dispose()
        {
            // do nothing
        }

        private readonly Lazy<PreviewGeneratorLogger> _logger =
            new Lazy<PreviewGeneratorLogger>(() => new PreviewGeneratorLogger());

        public ILogger CreateLogger(string categoryName)
        {
            return _logger.Value;
        }
    }
}
