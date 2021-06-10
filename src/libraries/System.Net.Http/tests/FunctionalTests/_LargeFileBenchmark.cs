// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
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
    [CollectionDefinition("NoParallelTests", DisableParallelization = true)]
    public class LargeFileBenchmark_ShouldNotBeParallell { }

    [Collection(nameof(LargeFileBenchmark_ShouldNotBeParallell))]
    public class LargeFileBenchmark : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private LogHttpEventListener _listener;

        public LargeFileBenchmark(ITestOutputHelper output)
        {
            _output = output;
            _listener = new LogHttpEventListener(output);
            _listener.Filter = m => m.Contains("[FlowControl]");
        }

        public void Dispose() => _listener?.Dispose();

        private const double LengthMb = 400;
        private const int TestRunCount = 5;

        //private const string BenchmarkServer = "10.194.114.94";
        //private const string BenchmarkServer = "169.254.132.170"; // duo1
        private const string BenchmarkServer = "192.168.0.152";
        private const string BenchmarkServerGo = "192.168.0.152:5002";
        // private const string BenchmarkServer = "127.0.0.1:5000";

        //private static readonly IPAddress LocalAddress = IPAddress.Parse("169.254.59.132"); // duo2
        private static readonly IPAddress LocalAddress = null;

        //private const string ReportDir = @"c:\_dev\WindowBenchmark";
        private const string ReportDir = @"C:\Users\anfirszo\dev\dotnet\6.0\runtime\artifacts\bin\System.Net.Http.Functional.Tests\net6.0-windows-Release\TestResults";

        [Theory]
        [InlineData(BenchmarkServer)]
        public Task Download11(string hostName) => TestHandler("SocketsHttpHandler HTTP 1.1 - Run1", hostName, false, LengthMb);

        [Theory]
        [InlineData(BenchmarkServer, 1024)]
        [InlineData(BenchmarkServer, 2048)]
        [InlineData(BenchmarkServer, 4096)]
        [InlineData(BenchmarkServer, 8192)]
        [InlineData(BenchmarkServer, 16384)]
        public Task Download20_SpecificWindow_MegaBytes(string hostName, int initialWindowKbytes) => Download20_SpecificWindow(hostName, initialWindowKbytes);

        [Theory]
        [InlineData(BenchmarkServer, 64)]
        [InlineData(BenchmarkServer, 128)]
        [InlineData(BenchmarkServer, 256)]
        [InlineData(BenchmarkServer, 512)]
        [InlineData(BenchmarkServer, 1024)]
        [InlineData(BenchmarkServer, 2048)]
        [InlineData(BenchmarkServer, 4096)]
        public Task Download20_SpecificWindow_KiloBytes(string hostName, int initialWindowKbytes) => Download20_SpecificWindow(hostName, initialWindowKbytes);

        private Task Download20_SpecificWindow(string hostName, int initialWindowKbytes)
        {
            SocketsHttpHandler handler = new SocketsHttpHandler()
            {
                EnableDynamicHttp2StreamWindowSizing = false,
                InitialStreamWindowSize = initialWindowKbytes * 1024
            };
            string details = $"SpecificWindow({initialWindowKbytes})";
            return TestHandler($"SocketsHttpHandler HTTP 2.0 - W: {initialWindowKbytes} KB", hostName, true, LengthMb, handler, details);
        }

        public static TheoryData<string, int, double> Download20_ServerAndRatio = new TheoryData<string, int, double>
        {
            { BenchmarkServer, 8, 0.5 },
            { BenchmarkServer, 8, 0.25 },
            { BenchmarkServer, 8, 0.125 },
            { BenchmarkServer, 4, 0.5 },
            { BenchmarkServer, 4, 0.25 },
            //{ BenchmarkServerGo, 8, 0.5 },
            //{ BenchmarkServerGo, 8, 0.25 },
            //{ BenchmarkServerGo, 8, 0.125 },
            //{ BenchmarkServerGo, 4, 0.5 },
            //{ BenchmarkServerGo, 4, 0.25 },
        };


        [Theory]
        [MemberData(nameof(Download20_ServerAndRatio))]
        private async Task Download20_Dynamic_SingleStream(string hostName, int ratio, double magic)
        {
            _listener.Enabled = true;
            _listener.Filter = m =>  m.Contains("[FlowControl]") && m.Contains("Updated");
            var handler = new SocketsHttpHandler()
            {
                StreamWindowUpdateRatio = ratio,
                StreamWindowMagicMultiplier = magic
            };
            await TestHandler($"SocketsHttpHandler HTTP 2.0 Dynamic single stream | host:{hostName} ratio={ratio} magic={magic}", hostName, true, LengthMb, handler);
        }

        [Theory]
        [MemberData(nameof(Download20_ServerAndRatio))]
        public async Task Download20_StaticRtt(string hostName, int ratio, double magic)
        {
            _listener.Enabled = true;
            _listener.Filter = m =>  m.Contains("[FlowControl]") && m.Contains("Updated");
            var handler = new SocketsHttpHandler
            {
                FakeRtt = await EstimateRttAsync(hostName),
                StreamWindowUpdateRatio = ratio,
                StreamWindowMagicMultiplier = magic
            };

            string details = $"StaticRtt_R({ratio})_M({magic})";
            await TestHandler($"SocketsHttpHandler HTTP 2.0 dynamic Window with Static RTT  | host:{hostName} ratio={ratio} magic={magic}", hostName, true, LengthMb, handler, details);
        }

        [Theory]
        [InlineData(BenchmarkServer)]
        //[InlineData("10.194.114.94:5001")]
        //[InlineData("10.194.114.94:5002")]
        public async Task Download20_Dynamic_MultiStream(string hostName)
        {
            _listener.Enabled = true;
            var handler = new SocketsHttpHandler();
            using var client = new HttpClient(handler, true);
            client.Timeout = TimeSpan.FromMinutes(3);
            const int NStreams = 4;
            string info = $"SocketsHttpHandler HTTP 2.0 Dynamic {NStreams} concurrent streams";


            var message = GenerateRequestMessage(hostName, true, LengthMb);
            _output.WriteLine($"{info} / {LengthMb} MB from {message.RequestUri}");

            Stopwatch sw = Stopwatch.StartNew();
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < NStreams; i++)
            {
                var task = Task.Run(() => client.SendAsync(GenerateRequestMessage(hostName, true, LengthMb)));
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            double elapsedSec = sw.ElapsedMilliseconds * 0.001;
            _output.WriteLine($"{info}: completed in {elapsedSec} sec");
        }

        private async Task TestHandler(string info, string hostName, bool http2, double lengthMb, SocketsHttpHandler handler = null, string details = "")
        {
            handler ??= new SocketsHttpHandler();

            if (LocalAddress != null) handler.ConnectCallback = CustomConnect;

            string reportFileName = CreateOutputFile(details);
            _output.WriteLine("REPORT: " + reportFileName);
            using StreamWriter report = new StreamWriter(reportFileName);

            for (int i = 0; i < TestRunCount; i++)
            {
                _output.WriteLine($"############ run {i} ############");
                await TestHandlerCore(info, hostName, http2, lengthMb, handler, report);
            }
            handler.Dispose();
        }

        private static string CreateOutputFile(string details)
        {
            if (!Directory.Exists(ReportDir)) Directory.CreateDirectory(ReportDir);
            return Path.Combine(ReportDir, $"report_{Environment.TickCount64}_{details}.csv");
        }

        private async Task TestHandlerCore(string info, string hostName, bool http2, double lengthMb, SocketsHttpHandler handler, StreamWriter report)
        {
            _listener.Log2.Clear();
            using var client = new HttpClient(handler, false);
            client.Timeout = TimeSpan.FromMinutes(3);
            var message = GenerateRequestMessage(hostName, http2, lengthMb);
            _output.WriteLine($"{info} / {lengthMb} MB from {message.RequestUri}");
            Stopwatch sw = Stopwatch.StartNew();
            var response = await client.SendAsync(message);

            double elapsedSec = sw.ElapsedMilliseconds * 0.001;
            elapsedSec = Math.Round(elapsedSec, 3);
            _output.WriteLine($"{info}: completed in {elapsedSec} sec");
            double window = GetStreamWindowSizeInMegabytes();
            report.WriteLine($"{elapsedSec}, {window}");
        }

        private double GetStreamWindowSizeInMegabytes()
        {
            const string Prefix = "Updated StreamWindowSize: ";
            string log = _listener.Log2.ToString();

            int idx = log.LastIndexOf(Prefix);
            if (idx < 0) return 0;
            ReadOnlySpan<char> text = log.AsSpan().Slice(idx + Prefix.Length);
            text = text.Slice(0, text.IndexOf(','));

            double size = int.Parse(text);
            double sizeMb = size / 1024 / 1024;
            return Math.Round(sizeMb, 3);
        }

        private static async ValueTask<Stream> CustomConnect(SocketsHttpConnectionContext ctx, CancellationToken cancellationToken)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            socket.Bind(new IPEndPoint(LocalAddress, 0));

            try
            {
                await socket.ConnectAsync(ctx.DnsEndPoint, cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        private async Task<TimeSpan> EstimateRttAsync(string hostName)
        {
            int sep = hostName.IndexOf(':');
            if (sep > 0)
            {
                hostName = hostName.Substring(0, sep);
            }

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
            TimeSpan rtt = TimeSpan.FromMilliseconds(reply1.RoundtripTime + reply2.RoundtripTime) / 2;
            _output.WriteLine($"Estimated RTT: {rtt}");
            if (rtt < TimeSpan.FromMilliseconds(1))
            {
                _output.WriteLine("RTT < 1 ms, changing to 1 ms!");
                rtt = TimeSpan.FromMilliseconds(1);
            }
            return rtt;
        }


        static HttpRequestMessage GenerateRequestMessage(string hostName, bool http2, double lengthMb = 5)
        {
            int port = http2 ? 5001 : 5000;
            int sep = hostName.IndexOf(':');
            if (sep > 0)
            {
                string portStr = hostName.Substring(sep + 1, hostName.Length - sep - 1);
                int.TryParse(portStr, out port);
                hostName = hostName.Substring(0, sep);
            }

            string url = $"http://{hostName}:{port}?lengthMb={lengthMb}";
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

        public StringBuilder Log2 { get; }

        public LogHttpEventListener(ITestOutputHelper log)
        {
            _log = log;
            _messagesChannel = Channel.CreateUnbounded<string>();
            _processMessages = ProcessMessagesAsync();
            _stopProcessing = new CancellationTokenSource();
            Log2 = new StringBuilder(1024 * 1024);
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
                    if (Filter(message))
                    {
                        _log.WriteLine(message);
                        Log2.AppendLine(message);
                    }
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
            var timeout = TimeSpan.FromSeconds(2);

            if (!_processMessages.Wait(timeout))
            {
                _stopProcessing.Cancel();
                _processMessages.Wait(timeout);
            }
        }
    }
}
