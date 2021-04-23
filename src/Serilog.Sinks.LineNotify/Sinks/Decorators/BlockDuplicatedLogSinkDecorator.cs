using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.LineNotify.Sinks.Decorators
{
    class BlockDuplicatedLogSinkDecorator : ILogEventSink, IDisposable
    {
        private readonly ILogEventSink _logEventSink;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _blockTimeSpan;

        public BlockDuplicatedLogSinkDecorator(ILogEventSink logEventSink, IMemoryCache memoryCache, TimeSpan blockTimeSpan)
        {
            _logEventSink = logEventSink;
            _memoryCache = memoryCache;
            _blockTimeSpan = blockTimeSpan;
        }

        public void Dispose()
        {
            var logEventSink = _logEventSink as IDisposable;
            if (logEventSink != null)
                logEventSink.Dispose();

            _memoryCache?.Dispose();
        }

        public void Emit(LogEvent logEvent)
        {
            var messageTemplateText = logEvent.MessageTemplate.Text;
            var key = Hash(logEvent.Level.ToString() + "-" + messageTemplateText);

            if (!_memoryCache.TryGetValue<DuplicateItem>(key, out DuplicateItem duplicateItem))
            {
                duplicateItem = new DuplicateItem(messageTemplateText);
                _memoryCache.Set(key, duplicateItem, _blockTimeSpan);

                duplicateItem.IncrementDuplicateCount();
                _logEventSink.Emit(logEvent);
            }
        }

        private string Hash(string input)
        {
            using(var algorithm = SHA512.Create())
            {
                var hashedBytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        class DuplicateItem
        {
            public int DuplicateCount { get; private set; }

            public string MessageTemplate { get; }

            public DuplicateItem(string messageTemplate)
            {
                DuplicateCount = 0;
                MessageTemplate = messageTemplate;
            }

            public void IncrementDuplicateCount()
            {
                DuplicateCount++;
            }
        }
    }
}