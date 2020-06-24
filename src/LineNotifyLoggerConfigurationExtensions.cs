using System;
using System.Collections.Generic;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.LineNotify;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog
{
    public static class LineNotifyLoggerConfigurationExtensions
    {
        public static LoggerConfiguration LineNotify(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string outputTemplate,
            IEnumerable<string> lineNotifyTokens,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose)
        {
            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            var sink = new LineNotifySink(formatter, lineNotifyTokens);

            var batchingSink = new PeriodicBatchingSink(sink, new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = 1,
                    Period = TimeSpan.FromSeconds(1),
                    QueueLimit = 1000
            });

            return loggerSinkConfiguration.Sink(batchingSink, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration LineNotify(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string outputTemplate,
            string lineNotifyToken,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose)
        {
            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            var sink = new LineNotifySink(formatter, lineNotifyToken);

            var batchingSink = new PeriodicBatchingSink(sink, new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = 1,
                    Period = TimeSpan.FromSeconds(1),
                    QueueLimit = 1000
            });

            return loggerSinkConfiguration.Sink(batchingSink, restrictedToMinimumLevel);
        }
    }
}