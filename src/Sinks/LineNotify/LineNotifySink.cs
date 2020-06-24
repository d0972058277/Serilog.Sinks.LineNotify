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
        private const int DefaultWriteBufferCapacity = 256;

        private readonly HttpClient _httpClient;

        private readonly string _lineNotifyApiUrl;
        private readonly ITextFormatter _textFormatter;
        private readonly IEnumerable<string> _lineNotifyTokens;

        public LineNotifySink(ITextFormatter textFormatter, IEnumerable<string> lineNotifyTokens, string lineNotifyApiUrl = "https://notify-api.line.me/api/notify")
        {
            if (!lineNotifyTokens.Any())
            {
                throw new ArgumentException("長度不能為0。", nameof(lineNotifyTokens));
            }

            _httpClient = new HttpClient();
            _lineNotifyApiUrl = lineNotifyApiUrl;
            _textFormatter = textFormatter;
            _lineNotifyTokens = lineNotifyTokens;
        }

        public LineNotifySink(ITextFormatter textFormatter, string lineNotifyToken, string lineNotifyApiUrl = "https://notify-api.line.me/api/notify")
        {
            if (!string.IsNullOrWhiteSpace(lineNotifyToken))
            {
                throw new ArgumentException("line notify token 不能為空。", nameof(lineNotifyToken));
            }

            _httpClient = new HttpClient();
            _lineNotifyApiUrl = lineNotifyApiUrl;
            _textFormatter = textFormatter;
            _lineNotifyTokens = new [] { lineNotifyToken };
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
            var buffer = new StringWriter(new StringBuilder(DefaultWriteBufferCapacity));

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

                var response = await _httpClient.SendAsync(requestMessage);

                if (response.StatusCode == HttpStatusCode.OK) { }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new LoggingFailedException($"此Token已失效 : {token}");
                }
                else
                {
                    var body = response.Content.ReadAsStringAsync();

                    throw new LoggingFailedException($"發送Line Notify時發生錯誤，HttpStatusCode : {response.StatusCode}，body: {body}");
                }
            }
        }
    }
}