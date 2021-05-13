// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class LargeFileBenchmark : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private LogHttpEventListener _listener;

        public LargeFileBenchmark(ITestOutputHelper output)
        {
            _output = output;
            _listener = new LogHttpEventListener(output);
        }

        public void Dispose() => _listener?.Dispose();

        private const double LengthMb = 5;

        [Theory]
        [InlineData("10.194.114.94")]
        public Task Download11(string hostName) => TestHandler("SocketsHttpHandler HTTP 1.1", hostName, false, LengthMb);

        [Theory]
        [InlineData("10.194.114.94")]
        public Task Download20(string hostName) => TestHandler("SocketsHttpHandler HTTP 2.0", hostName, true, LengthMb);

        [Theory]
        [InlineData("10.194.114.94", 256)]
        [InlineData("10.194.114.94", 2 * 1024)]
        public Task Download20_LargeWindow(string hostName, int initialWindowKbytes)
        {
            SocketsHttpHandler handler = new SocketsHttpHandler()
            {
                InitialStreamWindowSize = initialWindowKbytes * 1024
            };
            return TestHandler("SocketsHttpHandler HTTP 2.0", hostName, true, LengthMb, handler);
        }


        [Theory]
        [InlineData("10.194.114.94")]
        public async Task Download20_Dynamic(string hostName)
        {
            _listener.Enabled = true;
            _listener.Filter = m => m.Contains("No adjustment") || m.Contains("Updated StreamWindowSize") || m.Contains("SendWindowUpdateAsync");

            SocketsHttpHandler handler = new SocketsHttpHandler()
            {
                FakeRtt = await EstimateRttAsync(hostName)
            };
            await TestHandler("SocketsHttpHandler HTTP 2.0", hostName, true, LengthMb, handler);
        }

        private async Task TestHandler(string info, string hostName, bool http2, double lengthMb, HttpMessageHandler handler = null)
        {
            handler ??= new SocketsHttpHandler();
            using var client = new HttpClient(handler, true);
            var message = GenerateRequestMessage(hostName, http2, lengthMb);
            _output.WriteLine($"{info} / {lengthMb} MB from {hostName}");
            Stopwatch sw = Stopwatch.StartNew();
            var response = await client.SendAsync(message);
            long elapsedMs = sw.ElapsedMilliseconds;

            _output.WriteLine($"{info}: {response.StatusCode} in {elapsedMs} ms");
        }

        private async Task<TimeSpan> EstimateRttAsync(string hostName)
        {
            IPAddress addr;
            if (!IPAddress.TryParse(hostName, out addr))
            {
                addr = (await Dns.GetHostAddressesAsync(hostName)).FirstOrDefault(e => e.AddressFamily == AddressFamily.InterNetwork);
            }

            Ping ping = new Ping();

            // warmup:
            await ping.SendPingAsync(addr);

            PingReply reply1 = await ping.SendPingAsync(addr);
            PingReply reply2 = await ping.SendPingAsync(addr);
            TimeSpan rtt = new TimeSpan(reply1.RoundtripTime + reply2.RoundtripTime) / 2;
            _output.WriteLine($"Estimated RTT: {rtt.TotalMilliseconds} ms");
            return rtt;
        }


        static HttpRequestMessage GenerateRequestMessage(string hostName, bool http2, double lengthMb = 5)
        {
            string url = $"http://{hostName}:{(http2 ? "5001" : "5000")}?lengthMb={lengthMb}";
            var msg = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Version = new Version(1, 1)
            };

            if (http2)
            {
                msg.Version = new Version(2, 0);
                msg.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            }

            return msg;
        }
    }

    public sealed class LogHttpEventListener : EventListener
    {
        private Channel<string> _messagesChannel = Channel.CreateUnbounded<string>();
        private Task _processMessages;
        private CancellationTokenSource _stopProcessing;
        private ITestOutputHelper _log;

        public LogHttpEventListener(ITestOutputHelper log)
        {
            _log = log;
            _messagesChannel = Channel.CreateUnbounded<string>();
            _processMessages = ProcessMessagesAsync();
            _stopProcessing = new CancellationTokenSource();
        }

        public bool Enabled { get; set; }
        public Predicate<string> Filter { get; set; } = _ => true;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Private.InternalDiagnostics.System.Net.Http")
            {
                EnableEvents(eventSource, EventLevel.LogAlways);
            }
        }

        private async Task ProcessMessagesAsync()
        {
            await Task.Yield();

            try
            {
                await foreach (string message in _messagesChannel.Reader.ReadAllAsync(_stopProcessing.Token))
                {
                    if (Filter(message)) _log.WriteLine(message);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        protected override async void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!Enabled) return;

            var sb = new StringBuilder().Append($"{eventData.TimeStamp:HH:mm:ss.fffffff}[{eventData.EventName}] ");
            for (int i = 0; i < eventData.Payload?.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(eventData.PayloadNames?[i]).Append(": ").Append(eventData.Payload[i]);
            }
            await _messagesChannel.Writer.WriteAsync(sb.ToString());
        }

        public override void Dispose()
        {
            base.Dispose();

            if (!_processMessages.Wait(TimeSpan.FromSeconds(10)))
            {
                _stopProcessing.Cancel();
                _processMessages.Wait();
            }
        }
    }
}
