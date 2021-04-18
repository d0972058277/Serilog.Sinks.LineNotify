using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.LineNotify
{
    class LineNotifySink : IBatchedLogEventSink, IDisposable
    {
        private const int _defaultWriteBufferCapacity = 256;
        private readonly HttpClient _httpClient;
        private readonly string _lineNotifyApiUrl;
        private readonly ITextFormatter _textFormatter;
        private readonly IEnumerable<string> _lineNotifyTokens;

        public LineNotifySink(HttpClient httpClient, string lineNotifyApiUrl, ITextFormatter textFormatter, IEnumerable<string> lineNotifyTokens)
        {
            _httpClient = httpClient;
            _lineNotifyApiUrl = lineNotifyApiUrl;
            _textFormatter = textFormatter;
            _lineNotifyTokens = lineNotifyTokens;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            foreach (var logEvent in batch)
            {
                var message = FormatMessage(logEvent);

                foreach (var lineNotifyToken in _lineNotifyTokens)
                {
                    await NotifyAsync(lineNotifyToken, message);
                }
            }
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        private string FormatMessage(LogEvent logEvent)
        {
            var buffer = new StringWriter(new StringBuilder(_defaultWriteBufferCapacity));

            _textFormatter.Format(logEvent, buffer);

            var formatMessage = buffer.ToString();

            return formatMessage;
        }

        private async Task NotifyAsync(string token, string message)
        {
            using(var requestMessage = new HttpRequestMessage(HttpMethod.Post, _lineNotifyApiUrl))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                requestMessage.Content = new FormUrlEncodedContent(new []
                {
                    new KeyValuePair<string, string>("message", message)
                });

                using(var response = await _httpClient.SendAsync(requestMessage))
                {
                    if (response.StatusCode == HttpStatusCode.OK) { }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new LoggingFailedException($"此Token已失效 : {token}");
                    }
                    else
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        throw new LoggingFailedException($"發送Line Notify時發生錯誤，HttpStatusCode : {response.StatusCode}，body: {body}");
                    }
                }
            }
        }
    }
}