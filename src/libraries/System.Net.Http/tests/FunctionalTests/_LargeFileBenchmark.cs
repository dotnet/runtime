// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
#pragma warning disable xUnit1004 // Test methods should not be skipped
        //public const string SkipSwitch = null;
        public const string SkipSwitch = "Local benchmark";

        private readonly ITestOutputHelper _output;
        private LogHttpEventListener _listener;

        public LargeFileBenchmark(ITestOutputHelper output)
        {
            _output = output;
            _listener = new LogHttpEventListener(output);
            _listener.Filter = m => m.Contains("[FlowControl]");
        }

        public void Dispose() => _listener?.Dispose();

        private const double LengthMb = 100;
        private const int TestRunCount = 10;

        //private const string BenchmarkServer = "10.194.114.94";
        //private const string BenchmarkServer = "169.254.132.170"; // duo1
        private const string BenchmarkServer = "192.168.0.152";
        //private const string BenchmarkServer = "127.0.0.1";
        private const string BenchmarkServerGo = "192.168.0.152:5002";
        // private const string BenchmarkServer = "127.0.0.1:5000";

        //private static readonly IPAddress LocalAddress = IPAddress.Parse("169.254.59.132"); // duo2
        private static readonly IPAddress LocalAddress = null;

        //private const string ReportDir = @"C:\_dev\r6r\artifacts\bin\System.Net.Http.Functional.Tests\net6.0-windows-Release\TestResults";
        //private const string ReportDir = @"C:\Users\anfirszo\dev\dotnet\6.0\runtime\artifacts\bin\System.Net.Http.Functional.Tests\net6.0-windows-Release\TestResults";
        private const string ReportDir = @"C:\_dev\r6r\artifacts\bin\System.Net.Http.Functional.Tests\net6.0-windows-Release\TestResults";

        [Theory(Skip = SkipSwitch)]
        [InlineData(BenchmarkServer)]
        public Task Download11(string hostName) => TestHandler("SocketsHttpHandler HTTP 1.1 - Run1", hostName, false, LengthMb, details: "http1.1");

        [Theory(Skip = SkipSwitch)]
        [InlineData(BenchmarkServer, 1024)]
        [InlineData(BenchmarkServer, 2048)]
        [InlineData(BenchmarkServer, 4096)]
        [InlineData(BenchmarkServer, 8192)]
        [InlineData(BenchmarkServer, 16384)]
        public Task Download20_SpecificWindow_MegaBytes(string hostName, int initialWindowKbytes) => Download20_SpecificWindow(hostName, initialWindowKbytes);

        [Theory(Skip = SkipSwitch)]
        [InlineData(BenchmarkServer, 64)]
        [InlineData(BenchmarkServer, 128)]
        [InlineData(BenchmarkServer, 256)]
        [InlineData(BenchmarkServer, 512)]
        public Task Download20_SpecificWindow_KiloBytes(string hostName, int initialWindowKbytes) => Download20_SpecificWindow(hostName, initialWindowKbytes);

        private Task Download20_SpecificWindow(string hostName, int initialWindowKbytes)
        {
            SocketsHttpHandler handler = new SocketsHttpHandler()
            {
                EnableDynamicHttp2StreamWindowSizing = false,
                InitialHttp2StreamWindowSize = initialWindowKbytes * 1024
            };
            string details = $"SpecificWindow({initialWindowKbytes})";
            return TestHandler($"SocketsHttpHandler HTTP 2.0 - W: {initialWindowKbytes} KB", hostName, true, LengthMb, handler, details);
        }

        public static TheoryData<string, int, int> Download20_Data = new TheoryData<string, int, int>
        {
            { BenchmarkServer, 8, 1 },
            { BenchmarkServer, 8, 2 },
            { BenchmarkServer, 8, 4 },
            { BenchmarkServer, 8, 8 },
            { BenchmarkServer, 4, 1 },
            { BenchmarkServer, 4, 2 },
            { BenchmarkServer, 4, 4 },
        };

        public static TheoryData<string, int, int> Download20_Data8 = new TheoryData<string, int, int>
        {
            { BenchmarkServer, 8, 1 },
            { BenchmarkServer, 8, 2 },
            { BenchmarkServer, 8, 4 },
            { BenchmarkServer, 8, 8 },
            { BenchmarkServer, 8, 16 },
        };


        public static TheoryData<string, int, int> Download20_Data4 = new TheoryData<string, int, int>
        {
            { BenchmarkServer, 4, 1 },
            { BenchmarkServer, 4, 2 },
            { BenchmarkServer, 4, 4 },
            { BenchmarkServer, 4, 8 },
        };

        [Theory(Skip = SkipSwitch)]
        [MemberData(nameof(Download20_Data8))]
        public Task Download20_Dynamic_SingleStream_8(string hostName, int ratio, int correction) => Download20_Dynamic_SingleStream(hostName, ratio, correction);

        [Theory(Skip = SkipSwitch)]
        [MemberData(nameof(Download20_Data4))]
        public Task Download20_Dynamic_SingleStream_4(string hostName, int ratio, int correction) => Download20_Dynamic_SingleStream(hostName, ratio, correction);

        [Fact(Skip = SkipSwitch)]
        public Task Download20_Dynamic_Test()
        {
            _listener.Enabled = true;
            return Download20_Dynamic_SingleStream(BenchmarkServer, 8, 8, true);
        }

        private async Task Download20_Dynamic_SingleStream(string hostName, int ratio, int correction, bool keepFilter = false)
        {
            _listener.Enabled = true;
            if (!keepFilter)
            {
                _listener.Filter = m => m.Contains("[FlowControl]") && m.Contains("Updated");
            }

            var handler = new SocketsHttpHandler()
            {
                StreamWindowUpdateRatio = ratio,
                StreamWindowThresholdMultiplier = correction
            };
            string details = $"Dynamic_R({ratio})_C({correction})";
            await TestHandler($"SocketsHttpHandler HTTP 2.0 Dynamic single stream | host:{hostName} ratio={ratio} correction={handler.StreamWindowThresholdMultiplier}",
                hostName, true, LengthMb, handler, details);
        }

        [Theory(Skip = SkipSwitch)]
        [InlineData(BenchmarkServer, 1)]
        [InlineData(BenchmarkServer, 10)]
        [InlineData(BenchmarkServer, 50)]
        public async Task Download20_Dynamic_MultiStream(string hostName, int streamCount)
        {
            _listener.Enabled = true;
            _listener.Filter = m => m.Contains("[FlowControl]") && m.Contains("Updated");
            string info = $"SocketsHttpHandler HTTP 2.0 Dynamic {streamCount} concurrent streams R=8 D=8";

            var handler = new SocketsHttpHandler()
            {
                StreamWindowUpdateRatio = 8,
                StreamWindowThresholdMultiplier = 8
            };

            string details = $"SC({streamCount})";

            await TestHandler(info, hostName, true, LengthMb, handler, details, streamCount);
        }

        private async Task TestHandler(string info, string hostName, bool http2, double lengthMb, SocketsHttpHandler handler = null, string details = "", int streamCount = -1)
        {
            handler ??= new SocketsHttpHandler();

            if (LocalAddress != null) handler.ConnectCallback = CustomConnect;

            string reportFileName = CreateOutputFile(details);
            _output.WriteLine("REPORT: " + reportFileName);
            using StreamWriter report = new StreamWriter(reportFileName);

            _output.WriteLine($"############ Warmup Run ############");
            await TestHandlerCore(info, hostName, http2, lengthMb, handler, null);

            for (int i = 0; i < TestRunCount; i++)
            {
                _output.WriteLine($"############ run {i} ############");
                if (streamCount > 0)
                {
                    await TestHandlerCoreMultiStream(info, hostName, http2, lengthMb, handler, report, streamCount);
                }
                else
                {
                    await TestHandlerCore(info, hostName, http2, lengthMb, handler, report);
                }
                await report.FlushAsync();                
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
            using var client = new HttpClient(CopyHandler(handler), true);
            client.Timeout = TimeSpan.FromMinutes(3);
            using var message = GenerateRequestMessage(hostName, http2, lengthMb);
            _output.WriteLine($"{info} / {lengthMb} MB from {message.RequestUri}");
            Stopwatch sw = Stopwatch.StartNew();
            using var response = await client.SendAsync(message);

            double elapsedSec = sw.ElapsedMilliseconds * 0.001;
            elapsedSec = Math.Round(elapsedSec, 3);
            _output.WriteLine($"{info}: completed in {elapsedSec} sec");

            if (report != null)
            {
                report.Write(elapsedSec);
                double? window = GetStreamWindowSizeInMegabytes();
                if (window.HasValue) report.Write($", {window}");
                double? rtt = GetRtt();
                if (rtt.HasValue) report.Write($", {rtt}");
                report.WriteLine();
            }
        }

        private async Task TestHandlerCoreMultiStream(string info, string hostName, bool http2, double lengthMb, SocketsHttpHandler handler, StreamWriter report, int streamCount)
        {
            _listener.Log2.Clear();
            using var client = new HttpClient(CopyHandler(handler), true);
            client.Timeout = TimeSpan.FromMinutes(3);

            async Task<double> SendRequestAsync(int i)
            {
                using var message = GenerateRequestMessage(hostName, http2, lengthMb);
                _output.WriteLine($"[STREAM {i}] {info} / {lengthMb} MB from {message.RequestUri}");
                Stopwatch sw = Stopwatch.StartNew();

                using var response = await client.SendAsync(message).ConfigureAwait(false);
                double elapsedSec = sw.ElapsedMilliseconds * 0.001;
                elapsedSec = Math.Round(elapsedSec, 3);
                _output.WriteLine($"[STREAM {i}] {info}: completed in {elapsedSec} sec");
                return elapsedSec;
            }

            List<Task<double>> allTasks = new List<Task<double>>();

            for (int i =  0; i < streamCount; i++)
            {
                Task<double> task = SendRequestAsync(i);
                allTasks.Add(task);
            }

            await Task.WhenAll(allTasks);

            if (report != null)
            {
                double averageTime = allTasks.Select(t => t.Result).Average();
                averageTime = Math.Round(averageTime, 3);
                report.Write($"{averageTime}");
                double? window = GetStreamWindowSizeInMegabytes();
                if (window.HasValue) report.Write($", {window}");
                double? rtt = GetRtt();
                if (rtt.HasValue) report.Write($", {rtt}");
                report.WriteLine();
            }
        }

        private double? GetStreamWindowSizeInMegabytes()
        {
            const string Prefix = "Updated Stream Window. StreamWindowSize: ";
            string log = _listener.Log2.ToString();

            int idx = log.LastIndexOf(Prefix);
            if (idx < 0) return null;
            ReadOnlySpan<char> text = log.AsSpan().Slice(idx + Prefix.Length);
            text = text.Slice(0, text.IndexOf(','));

            double size = int.Parse(text);
            double sizeMb = size / 1024 / 1024;
            return Math.Round(sizeMb, 3);
        }

        private double? GetRtt()
        {
            const string Prefix = "Updated MinRtt: ";
            string log = _listener.Log2.ToString();

            int idx = log.LastIndexOf(Prefix);
            if (idx < 0) return null;

            ReadOnlySpan<char> text = log.AsSpan().Slice(idx + Prefix.Length);
            text = text.Slice(0, text.IndexOf(' '));

            double rtt = double.Parse(text);
            return Math.Round(rtt, 3);
        }

        private static SocketsHttpHandler CopyHandler(SocketsHttpHandler h)
        {
            return new SocketsHttpHandler()
            {
                EnableDynamicHttp2StreamWindowSizing = h.EnableDynamicHttp2StreamWindowSizing,
                InitialHttp2StreamWindowSize = h.InitialHttp2StreamWindowSize,
                StreamWindowUpdateRatio = h.StreamWindowUpdateRatio,
                StreamWindowThresholdMultiplier = h.StreamWindowThresholdMultiplier,
                ConnectCallback = h.ConnectCallback
            };
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
