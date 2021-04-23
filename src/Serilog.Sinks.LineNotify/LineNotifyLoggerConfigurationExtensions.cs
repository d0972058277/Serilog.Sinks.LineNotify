using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Caching.Memory;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.LineNotify;
using Serilog.Sinks.LineNotify.Sinks.Decorators;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog
{
    // https://notify-api.line.me/api/notify
    public static class LineNotifyLoggerConfigurationExtensions
    {
        private static ILogEventSink CreateLineNotify(string outputTemplate, IEnumerable<string> lineNotifyTokens, HttpClient httpClient = null, IFormatProvider formatProvider = null)
        {
            if (httpClient is null)
                httpClient = new HttpClient();
            var lineNotifyApiUrl = "https://notify-api.line.me/api/notify";
            var textFormatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            var sink = new LineNotifySink(httpClient, lineNotifyApiUrl, textFormatter, lineNotifyTokens);
            var sinkOptions = new PeriodicBatchingSinkOptions { BatchSizeLimit = 1, Period = TimeSpan.FromSeconds(1), QueueLimit = 1000 };
            var logEventSink = new PeriodicBatchingSink(sink, sinkOptions);
            return logEventSink;
        }

        private static ILogEventSink ApplyBlockDuplicatedLogDecorator(ILogEventSink logEventSink, int minutesForBlockDuplicatedLog, MemoryCache memoryCache = null)
        {
            if (memoryCache is null)
                memoryCache = new MemoryCache(new MemoryCacheOptions());
            var timespan = TimeSpan.FromMinutes(minutesForBlockDuplicatedLog);
            var decorator = new BlockDuplicatedLogSinkDecorator(logEventSink, memoryCache, timespan);
            return decorator;
        }

        public static LoggerConfiguration LineNotify(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string outputTemplate,
            IEnumerable<string> lineNotifyTokens,
            HttpClient httpClient = null,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose)
        {
            var logEventSink = CreateLineNotify(outputTemplate, lineNotifyTokens, httpClient : httpClient, formatProvider : formatProvider);
            return loggerSinkConfiguration.Sink(logEventSink, restrictedToMinimumLevel);
        }

        public static LoggerConfiguration LineNotify(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string outputTemplate,
            string lineNotifyToken,
            HttpClient httpClient = null,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose) => loggerSinkConfiguration.LineNotify(outputTemplate, new [] { lineNotifyToken }, httpClient : httpClient, formatProvider : formatProvider, restrictedToMinimumLevel);

        public static LoggerConfiguration LineNotify(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string outputTemplate,
            IEnumerable<string> lineNotifyTokens,
            int minutesForBlockDuplicatedLog,
            HttpClient httpClient = null,
            IFormatProvider formatProvider = null,
            MemoryCache memoryCache = null,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose)
        {
            var logEventSink = CreateLineNotify(outputTemplate, lineNotifyTokens, httpClient : httpClient, formatProvider : formatProvider);
            logEventSink = ApplyBlockDuplicatedLogDecorator(logEventSink, minutesForBlockDuplicatedLog, memoryCache : memoryCache);
            return loggerSinkConfiguration.Sink(logEventSink, restrictedToMinimumLevel);
        }
    }
}