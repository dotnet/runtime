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
        private const string ReportDir = @"C:\Users\anfirszo\dev\dotnet\6.0\runtime\artifacts\bin\System.Net.Http.Functional.Tests\net6.0-windows-Release\TestResults";

        [Theory]
        [InlineData(BenchmarkServer)]
        public Task Download11(string hostName) => TestHandler("SocketsHttpHandler HTTP 1.1 - Run1", hostName, false, LengthMb, details: "http1.1");

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

        public static TheoryData<string, int, int> Download20_Data = new TheoryData<string, int, int>
        {
            { BenchmarkServer, 8, 1 },
            { BenchmarkServer, 8, 2 },
            { BenchmarkServer, 8, 4 },
            { BenchmarkServer, 8, 8 },
            { BenchmarkServer, 4, 1 },
            { BenchmarkServer, 4, 2 },
            { BenchmarkServer, 4, 4 },
            //{ BenchmarkServerGo, 8, 0.5 },
            //{ BenchmarkServerGo, 8, 0.25 },
            //{ BenchmarkServerGo, 8, 0.125 },
            //{ BenchmarkServerGo, 4, 0.5 },
            //{ BenchmarkServerGo, 4, 0.25 },
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

        [Theory]
        [MemberData(nameof(Download20_Data8))]
        public Task Download20_StaticRtt_8(string hostName, int ratio, int correction) => Download20_StaticRtt(hostName, ratio, correction);

        [Theory]
        [MemberData(nameof(Download20_Data4))]
        public Task Download20_StaticRtt_4(string hostName, int ratio, int correction) => Download20_StaticRtt(hostName, ratio, correction);

        private async Task Download20_StaticRtt(string hostName, int ratio, int correction)
        {
            _listener.Enabled = true;
            _listener.Filter = m => m.Contains("[FlowControl]") && m.Contains("Updated");
            var handler = new SocketsHttpHandler
            {
                FakeRtt = await EstimateRttAsync(hostName),
                StreamWindowUpdateRatio = ratio,
                StreamWindowMagicMultiplier = 1.0 / correction
            };

            string details = $"StaticRtt_R({ratio})_C({correction})";
            await TestHandler($"SocketsHttpHandler HTTP 2.0 dynamic Window with Static RTT  | host:{hostName} ratio={ratio} magic={handler.StreamWindowMagicMultiplier}",
                hostName, true, LengthMb, handler, details);
        }

        [Theory]
        [MemberData(nameof(Download20_Data8))]
        public Task Download20_Dynamic_SingleStream_8(string hostName, int ratio, int correction) => Download20_Dynamic_SingleStream(hostName, ratio, correction);

        [Theory]
        [MemberData(nameof(Download20_Data4))]
        public Task Download20_Dynamic_SingleStream_4(string hostName, int ratio, int correction) => Download20_Dynamic_SingleStream(hostName, ratio, correction);

        private async Task Download20_Dynamic_SingleStream(string hostName, int ratio, int correction)
        {
            _listener.Enabled = true;
            _listener.Filter = m =>  m.Contains("[FlowControl]") && m.Contains("Updated");
            var handler = new SocketsHttpHandler()
            {
                StreamWindowUpdateRatio = ratio,
                StreamWindowMagicMultiplier = 1.0/correction
            };
            string details = $"Dynamic_R({ratio})_C({correction})";
            await TestHandler($"SocketsHttpHandler HTTP 2.0 Dynamic single stream | host:{hostName} ratio={ratio} magic={handler.StreamWindowMagicMultiplier}",
                hostName, true, LengthMb, handler, details);
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

            _output.WriteLine($"############ Warmup Run ############");
            await TestHandlerCore(info, hostName, http2, lengthMb, handler, null);

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
            using var client = new HttpClient(CopyHandler(handler), true);
            client.Timeout = TimeSpan.FromMinutes(3);
            var message = GenerateRequestMessage(hostName, http2, lengthMb);
            _output.WriteLine($"{info} / {lengthMb} MB from {message.RequestUri}");
            Stopwatch sw = Stopwatch.StartNew();
            var response = await client.SendAsync(message);

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

        private double? GetStreamWindowSizeInMegabytes()
        {
            const string Prefix = "Updated StreamWindowSize: ";
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
                FakeRtt = h.FakeRtt,
                EnableDynamicHttp2StreamWindowSizing = h.EnableDynamicHttp2StreamWindowSizing,
                InitialStreamWindowSize = h.InitialStreamWindowSize,
                StreamWindowUpdateRatio = h.StreamWindowUpdateRatio,
                StreamWindowMagicMultiplier = h.StreamWindowMagicMultiplier,
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

        [Fact]
        public async Task TestEstimateRtt()
        {
            TimeSpan rtt = await EstimateRttAsync(BenchmarkServer);
            _output.WriteLine($"RTT: {rtt.TotalMilliseconds} ms");
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

            IPAddress destAddr = (await Dns.GetHostAddressesAsync(hostName))[0];

            // warmup:
            PingEx.Send(LocalAddress, destAddr);

            IPAddress local = LocalAddress != null ? LocalAddress : IPAddress.Loopback;

            var reply1 = PingEx.Send(LocalAddress, destAddr).RoundTripTime;
            var reply2 = PingEx.Send(LocalAddress, destAddr).RoundTripTime;

            TimeSpan rtt = reply1 > reply2 ? reply1 : reply2;

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

    // https://stackoverflow.com/a/66380228/797482
    internal static class PingEx
    {
        /// <summary>
        /// Pass in the IP you want to ping as a string along with the name of the NIC on your machine that
        /// you want to send the ping from.
        /// </summary>
        /// <param name="ipToPing">The destination IP as a string ex. '10.10.10.1'</param>
        /// <param name="nicName">The name of the NIC ex. 'LECO Hardware'.  Non-case sensitive.</param>
        /// <returns></returns>
        public static bool PingIpFromNic(string ipToPing, string nicName)
        {
            var sourceIpStr = GetIpOfNicFromName(nicName);

            if (sourceIpStr == "")
            {
                return false;
            }

            var p = Send(
                srcAddress: IPAddress.Parse(sourceIpStr),
                destAddress: IPAddress.Parse(ipToPing));

            return p.Status == IPStatus.Success;
        }

        /// <summary>
        /// Pass in the name of a NIC on your machine and this method will return the IPV4 address of it.
        /// </summary>
        /// <param name="nicName">The name of the NIC you want the IP of ex. 'TE Hardware'</param>
        /// <returns></returns>
        public static string GetIpOfNicFromName(string nicName)
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var adapter in adapters)
            {
                // Ignoring case in NIC name
                if (!string.Equals(adapter.Name, nicName, StringComparison.CurrentCultureIgnoreCase)) continue;

                foreach (var uni in adapter.GetIPProperties().UnicastAddresses)
                {
                    // Return the first one found
                    return uni.Address.ToString();
                }
            }

            return "";
        }

        public static PingReplyEx Send(IPAddress srcAddress, IPAddress destAddress,
            int timeout = 5000,
            byte[] buffer = null, PingOptions po = null)
        {
            if (destAddress == null || destAddress.AddressFamily != AddressFamily.InterNetwork ||
                destAddress.Equals(IPAddress.Any))
                throw new ArgumentException();

            //Defining pinvoke args
            var source = srcAddress == null ? 0 : BitConverter.ToUInt32(srcAddress.GetAddressBytes(), 0);

            var destination = BitConverter.ToUInt32(destAddress.GetAddressBytes(), 0);

            var sendBuffer = buffer ?? new byte[] { };

            var options = new Interop.Option
            {
                Ttl = (po == null ? (byte)255 : (byte)po.Ttl),
                Flags = (po == null ? (byte)0 : po.DontFragment ? (byte)0x02 : (byte)0) //0x02
            };

            var fullReplyBufferSize =
                Interop.ReplyMarshalLength +
                sendBuffer.Length; //Size of Reply struct and the transmitted buffer length.

            var allocSpace =
                Marshal.AllocHGlobal(
                    fullReplyBufferSize); // unmanaged allocation of reply size. TODO Maybe should be allocated on stack
            try
            {
                var start = DateTime.Now;
                var nativeCode = Interop.IcmpSendEcho2Ex(
                    Interop.IcmpHandle,             //_In_      HANDLE IcmpHandle,
                    Event: default(IntPtr),         //_In_opt_  HANDLE Event,
                    apcRoutine: default(IntPtr),    //_In_opt_  PIO_APC_ROUTINE ApcRoutine,
                    apcContext: default(IntPtr),    //_In_opt_  PVOID ApcContext
                    source,                         //_In_      IPAddr SourceAddress,
                    destination,                    //_In_      IPAddr DestinationAddress,
                    sendBuffer,                     //_In_      LPVOID RequestData,
                    (short)sendBuffer.Length,       //_In_      WORD RequestSize,
                    ref options,                    //_In_opt_  PIP_OPTION_INFORMATION RequestOptions,
                    replyBuffer: allocSpace,        //_Out_     LPVOID ReplyBuffer,
                    fullReplyBufferSize,            //_In_      DWORD ReplySize,
                    timeout                         //_In_      DWORD Timeout
                );

                var duration = DateTime.Now - start;

                var reply = (Interop.Reply)Marshal.PtrToStructure(allocSpace,
                    typeof(Interop.Reply)); // Parse the beginning of reply memory to reply struct

                byte[] replyBuffer = null;
                if (sendBuffer.Length != 0)
                {
                    replyBuffer = new byte[sendBuffer.Length];
                    Marshal.Copy(allocSpace + Interop.ReplyMarshalLength, replyBuffer, 0,
                        sendBuffer.Length); //copy the rest of the reply memory to managed byte[]
                }

                if (nativeCode == 0) //Means that native method is faulted.
                    return new PingReplyEx(nativeCode, reply.Status,
                        new IPAddress(reply.Address), duration);
                else
                    return new PingReplyEx(nativeCode, reply.Status,
                        new IPAddress(reply.Address), reply.RoundTripTime,
                        replyBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(allocSpace); //free allocated space
            }
        }


        /// <summary>Interoperability Helper
        ///     <see cref="http://msdn.microsoft.com/en-us/library/windows/desktop/bb309069(v=vs.85).aspx" />
        /// </summary>
        public static class Interop
        {
            private static IntPtr? _icmpHandle;
            private static int? _replyStructLength;

            /// <summary>Returns the application legal icmp handle. Should be close by IcmpCloseHandle
            ///     <see cref="http://msdn.microsoft.com/en-us/library/windows/desktop/aa366045(v=vs.85).aspx" />
            /// </summary>
            public static IntPtr IcmpHandle
            {
                get
                {
                    if (_icmpHandle == null)
                    {
                        _icmpHandle = IcmpCreateFile();
                        //TODO Close Icmp Handle appropriate
                    }

                    return _icmpHandle.GetValueOrDefault();
                }
            }

            /// <summary>Returns the the marshaled size of the reply struct.</summary>
            public static int ReplyMarshalLength
            {
                get
                {
                    if (_replyStructLength == null)
                    {
                        _replyStructLength = Marshal.SizeOf(typeof(Reply));
                    }
                    return _replyStructLength.GetValueOrDefault();
                }
            }


            [DllImport("Iphlpapi.dll", SetLastError = true)]
            private static extern IntPtr IcmpCreateFile();
            [DllImport("Iphlpapi.dll", SetLastError = true)]
            private static extern bool IcmpCloseHandle(IntPtr handle);
            [DllImport("Iphlpapi.dll", SetLastError = true)]
            public static extern uint IcmpSendEcho2Ex(IntPtr icmpHandle, IntPtr Event, IntPtr apcRoutine, IntPtr apcContext, uint sourceAddress, UInt32 destinationAddress, byte[] requestData, short requestSize, ref Option requestOptions, IntPtr replyBuffer, int replySize, int timeout);
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct Option
            {
                public byte Ttl;
                public readonly byte Tos;
                public byte Flags;
                public readonly byte OptionsSize;
                public readonly IntPtr OptionsData;
            }
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct Reply
            {
                public readonly UInt32 Address;
                public readonly int Status;
                public readonly int RoundTripTime;
                public readonly short DataSize;
                public readonly short Reserved;
                public readonly IntPtr DataPtr;
                public readonly Option Options;
            }
        }

        public class PingReplyEx
        {
            private Win32Exception _exception;

            internal PingReplyEx(uint nativeCode, int replyStatus, IPAddress ipAddress, TimeSpan duration)
            {
                NativeCode = nativeCode;
                IpAddress = ipAddress;
                if (Enum.IsDefined(typeof(IPStatus), replyStatus))
                    Status = (IPStatus)replyStatus;
            }
            internal PingReplyEx(uint nativeCode, int replyStatus, IPAddress ipAddress, int roundTripTime, byte[] buffer)
            {
                NativeCode = nativeCode;
                IpAddress = ipAddress;
                RoundTripTime = TimeSpan.FromMilliseconds(roundTripTime);
                Buffer = buffer;
                if (Enum.IsDefined(typeof(IPStatus), replyStatus))
                    Status = (IPStatus)replyStatus;
            }

            /// <summary>Native result from <code>IcmpSendEcho2Ex</code>.</summary>
            public uint NativeCode { get; }

            public IPStatus Status { get; } = IPStatus.Unknown;

            /// <summary>The source address of the reply.</summary>
            public IPAddress IpAddress { get; }

            public byte[] Buffer { get; }

            public TimeSpan RoundTripTime { get; } = TimeSpan.Zero;

            public Win32Exception Exception
            {
                get
                {
                    if (Status != IPStatus.Success)
                        return _exception ?? (_exception = new Win32Exception((int)NativeCode, Status.ToString()));
                    else
                        return null;
                }
            }

            public override string ToString()
            {
                if (Status == IPStatus.Success)
                    return Status + " from " + IpAddress + " in " + RoundTripTime + " ms with " + Buffer.Length + " bytes";
                else if (Status != IPStatus.Unknown)
                    return Status + " from " + IpAddress;
                else
                    return Exception.Message + " from " + IpAddress;
            }
        }
    }
}
