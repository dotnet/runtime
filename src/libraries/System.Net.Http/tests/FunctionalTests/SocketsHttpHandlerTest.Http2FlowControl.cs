// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit;
using Xunit.Abstractions;
using static System.Net.Test.Common.LoopbackServer;

namespace System.Net.Http.Functional.Tests
{
    [CollectionDefinition(nameof(NonParallelTestCollection), DisableParallelization = true)]
    public class NonParallelTestCollection
    {
    }

    [Collection(nameof(NonParallelTestCollection))]
    [ConditionalClass(typeof(SocketsHttpHandler_Http2FlowControl_Test), nameof(IsSupported))]
    public sealed class SocketsHttpHandler_Http2KeepAlivePing_Test : HttpClientHandlerTestBase
    {
        public static readonly bool IsSupported = PlatformDetection.SupportsAlpn && PlatformDetection.IsNotBrowser;

        protected override Version UseVersion => HttpVersion20.Value;

        private LogHttpEventListener _listener;

        private int _pingCounter;
        private Http2LoopbackConnection _connection;
        private SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1);
        private Channel<Frame> _framesChannel = Channel.CreateUnbounded<Frame>();
        private CancellationTokenSource _incomingFramesCts = new CancellationTokenSource();
        private Task _incomingFramesTask;
        private TaskCompletionSource _serverFinished = new TaskCompletionSource();
        private int _sendPingResponse = 1;

        private static Http2Options NoAutoPingResponseHttp2Options => new Http2Options() { EnableTransparentPingResponse = false };

        public SocketsHttpHandler_Http2KeepAlivePing_Test(ITestOutputHelper output) : base(output)
        {
            _listener = new LogHttpEventListener(output);
        }

        private async Task ProcessIncomingFramesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Frame frame = await _connection.ReadFrameAsync(cancellationToken);

                    if (frame is PingFrame pingFrame)
                    {
                        if (pingFrame.AckFlag)
                        {
                            _output?.WriteLine($"Received unexpected PING ACK ({pingFrame.Data})");
                            await _framesChannel.Writer.WriteAsync(frame, cancellationToken);
                        }
                        else
                        {
                            _output?.WriteLine($"Received PING ({pingFrame.Data})");
                            Interlocked.Increment(ref _pingCounter);

                            if (_sendPingResponse > 0)
                            {
                                await GuardConnctionWriteAsync(() => _connection.SendPingAckAsync(pingFrame.Data, cancellationToken), cancellationToken);
                            }
                        }
                    }
                    else if (frame is WindowUpdateFrame windowUpdateFrame)
                    {
                        _output?.WriteLine($"Received WINDOW_UPDATE");
                    }
                    else if (frame is not null)
                    {
                        //_output?.WriteLine($"Received {frame}");
                        await _framesChannel.Writer.WriteAsync(frame, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            _output?.WriteLine("ProcessIncomingFramesAsync finished");
            _connection.Dispose();
        }

        private void DisablePingResponse() => Interlocked.Exchange(ref _sendPingResponse, 0);

        private async Task EstablishConnectionAsync(Http2LoopbackServer server)
        {
            _connection = await server.EstablishConnectionAsync();
            _incomingFramesTask = ProcessIncomingFramesAsync(_incomingFramesCts.Token);
        }

        private async Task TerminateLoopbackConnectionAsync()
        {
            _serverFinished.SetResult();
            _incomingFramesCts.Cancel();
            await _incomingFramesTask;
        }

        private async Task GuardConnctionWriteAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            await _writeSemaphore.WaitAsync(cancellationToken);
            await action();
            _writeSemaphore.Release();
        }

        private async Task<HeadersFrame> ReadRequestHeaderFrameAsync(bool expectEndOfStream = true, CancellationToken cancellationToken = default)
        {
            // Receive HEADERS frame for request.
            Frame frame = await _framesChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (frame == null)
            {
                throw new IOException("Failed to read Headers frame.");
            }

            Assert.Equal(FrameType.Headers, frame.Type);
            Assert.Equal(FrameFlags.EndHeaders, frame.Flags & FrameFlags.EndHeaders);
            if (expectEndOfStream)
            {
                Assert.Equal(FrameFlags.EndStream, frame.Flags & FrameFlags.EndStream);
            }
            return (HeadersFrame)frame;
        }

        private async Task<int> ReadRequestHeaderAsync(bool expectEndOfStream = true, CancellationToken cancellationToken = default)
        {
            HeadersFrame frame = await ReadRequestHeaderFrameAsync(expectEndOfStream, cancellationToken);
            return frame.StreamId;
        }

        [Theory]
        [InlineData(HttpKeepAlivePingPolicy.Always)]
        [InlineData(HttpKeepAlivePingPolicy.WithActiveRequests)]
        public async Task KeepAlivePingDelay_Infinite_NoKeepAlivePingIsSent(HttpKeepAlivePingPolicy policy)
        {
            //_listener.Enabled = true;

            TimeSpan testTimeout = TimeSpan.FromSeconds(30);

            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = new SocketsHttpHandler()
                {
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(1),
                    KeepAlivePingPolicy = policy,
                    KeepAlivePingDelay = Timeout.InfiniteTimeSpan
                };
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;

                // Warmup request to create connection:
                await client.GetStringAsync(uri).WaitAsync(testTimeout);

                // Actual request:
                await client.GetStringAsync(uri).WaitAsync(testTimeout);

                // Let connection live until server finishes:
                await _serverFinished.Task.WaitAsync(testTimeout);
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                // Warmup the connection.
                int streamId1 = await ReadRequestHeaderAsync();
                await GuardConnctionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                Interlocked.Exchange(ref _pingCounter, 0); // reset the PING counter
                // Request under the test scope.
                int streamId2 = await ReadRequestHeaderAsync();

                // Simulate inactive period:
                await Task.Delay(5_000);

                // We may have received one RTT PING in response to HEADERS, but should receive no KeepAlive PING
                Assert.True(_pingCounter <= 1);
                Interlocked.Exchange(ref _pingCounter, 0); // reset the counter

                // Finish the response:
                await GuardConnctionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId2));

                // Simulate inactive period:
                await Task.Delay(5_000);

                // We may have received one RTT PING in response to HEADERS, but should receive no KeepAlive PING
                Assert.True(_pingCounter <= 1);

                await TerminateLoopbackConnectionAsync();
            }).WaitAsync(testTimeout);
        }

        [Theory]
        [InlineData(HttpKeepAlivePingPolicy.Always)]
        [InlineData(HttpKeepAlivePingPolicy.WithActiveRequests)]
        public async Task KeepAliveConfigured_KeepAlivePingsAreSentAccordingToPolicy(HttpKeepAlivePingPolicy policy)
        {
            _listener.Enabled = true;

            TimeSpan testTimeout = TimeSpan.FromSeconds(60);

            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = new SocketsHttpHandler()
                {
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                    KeepAlivePingPolicy = policy,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(1)
                };
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;

                // Warmup request to create connection:
                HttpResponseMessage response0 = await client.GetAsync(uri).WaitAsync(testTimeout);
                Assert.Equal(HttpStatusCode.OK, response0.StatusCode);

                // Actual request:
                HttpResponseMessage response1 = await client.GetAsync(uri).WaitAsync(testTimeout);
                Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

                // Let connection live until server finishes:
                await _serverFinished.Task.WaitAsync(testTimeout);
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                // Warmup the connection.
                int streamId1 = await ReadRequestHeaderAsync();
                await GuardConnctionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                // Request under the test scope.
                int streamId2 = await ReadRequestHeaderAsync();
                Interlocked.Exchange(ref _pingCounter, 0); // reset the PING counter

                // Simulate inactive period:
                await Task.Delay(10_000);

                // We may receive one RTT PING in response to HEADERS.
                // Upon that, we expect to receive at least 2 keep alive PINGs:
                Assert.True(_pingCounter > 2);

                // Finish the response:
                await GuardConnctionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId2));
                Interlocked.Exchange(ref _pingCounter, 0); // reset the PING counter

                if (policy == HttpKeepAlivePingPolicy.Always)
                {
                    // Simulate inactive period:
                    await Task.Delay(10_000);

                    // We may receive one RTT PING in response to HEADERS.
                    // Upon that, we expect to receive at least 2 keep alive PINGs:
                    Assert.True(_pingCounter > 2);
                }
                else
                {
                    // We should receive no more KeepAlive PINGs
                    Assert.True(_pingCounter <= 1);
                }

                await TerminateLoopbackConnectionAsync();

                List<Frame> unexpectedFrames = new List<Frame>();
                while (_framesChannel.Reader.Count > 0)
                {
                    Frame unexpectedFrame = await _framesChannel.Reader.ReadAsync();
                    unexpectedFrames.Add(unexpectedFrame);
                }

                Assert.False(unexpectedFrames.Any(), "Received unexpected frames: \n" + string.Join('\n', unexpectedFrames.Select(f => f.ToString()).ToArray()));
            }, NoAutoPingResponseHttp2Options).WaitAsync(testTimeout);
        }

        [Theory]
        [InlineData(HttpKeepAlivePingPolicy.Always)]
        [InlineData(HttpKeepAlivePingPolicy.WithActiveRequests)]
        public async Task KeepAliveConfigured_NoPingResponseDuringActiveStream_RequestShouldFail(HttpKeepAlivePingPolicy policy)
        {
            TimeSpan testTimeout = TimeSpan.FromSeconds(60);

            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = new SocketsHttpHandler()
                {
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(2),
                    KeepAlivePingPolicy = policy,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(1)
                };
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;

                // Warmup request to create connection:
                HttpResponseMessage response0 = await client.GetAsync(uri).WaitAsync(testTimeout);
                Assert.Equal(HttpStatusCode.OK, response0.StatusCode);

                // Actual request:
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri)).WaitAsync(testTimeout);

                // Let connection live until server finishes:
                await _serverFinished.Task.WaitAsync(testTimeout);
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                // Warmup the connection.
                int streamId1 = await ReadRequestHeaderAsync();
                await GuardConnctionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                // Request under the test scope.
                int streamId2 = await ReadRequestHeaderAsync();

                DisablePingResponse();

                // Simulate inactive period:
                await Task.Delay(10_000);

                // Finish the response:
                await GuardConnctionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId2));

                await TerminateLoopbackConnectionAsync();
            }, NoAutoPingResponseHttp2Options).WaitAsync(testTimeout);
        }

        [Fact]
        public async Task HttpKeepAlivePingPolicy_Always_NoPingResponseBetweenStreams_SecondRequestShouldFail()
        {
            TimeSpan testTimeout = TimeSpan.FromSeconds(60);

            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = new SocketsHttpHandler()
                {
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(2),
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(1)
                };
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;

                // Warmup request to create connection:
                HttpResponseMessage response0 = await client.GetAsync(uri).WaitAsync(testTimeout);
                Assert.Equal(HttpStatusCode.OK, response0.StatusCode);

                // Second request should fail:
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri)).WaitAsync(testTimeout);

                // Let connection live until server finishes:
                await _serverFinished.Task.WaitAsync(testTimeout);
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                // Warmup the connection.
                int streamId1 = await ReadRequestHeaderAsync();
                await GuardConnctionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                DisablePingResponse();

                // Simulate inactive period:
                await Task.Delay(10_000);

                // Request under the test scope.
                int streamId2 = await ReadRequestHeaderAsync();

                // Finish the response:
                await GuardConnctionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId2));

                await TerminateLoopbackConnectionAsync();
            }, NoAutoPingResponseHttp2Options).WaitAsync(testTimeout);
        }
    }

    // This test class contains tests which are strongly timing-dependent.
    // There are two mitigations avoid flaky behavior on CI:
    // - Parallel test execution is disabled
    // - Using extreme parameters, and checks which are very unlikely to fail, if the implementation is correct
    [Collection(nameof(NonParallelTestCollection))]
    [ConditionalClass(typeof(SocketsHttpHandler_Http2FlowControl_Test), nameof(IsSupported))]
    public sealed class SocketsHttpHandler_Http2FlowControl_Test : HttpClientHandlerTestBase
    {
        public static readonly bool IsSupported = PlatformDetection.SupportsAlpn && PlatformDetection.IsNotBrowser;

        protected override Version UseVersion => HttpVersion20.Value;

        public SocketsHttpHandler_Http2FlowControl_Test(ITestOutputHelper output) : base(output)
        {
        }

        private static Http2Options NoAutoPingResponseHttp2Options => new Http2Options() { EnableTransparentPingResponse = false };

        [Fact]
        public async Task InitialHttp2StreamWindowSize_SentInSettingsFrame()
        {
            const int WindowSize = 123456;
            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using var handler = CreateHttpClientHandler();
            GetUnderlyingSocketsHttpHandler(handler).InitialHttp2StreamWindowSize = WindowSize;
            using HttpClient client = CreateHttpClient(handler);

            Task<HttpResponseMessage> clientTask = client.GetAsync(server.Address);
            Http2LoopbackConnection connection = await server.AcceptConnectionAsync().ConfigureAwait(false);
            SettingsFrame clientSettingsFrame = await connection.ReadSettingsAsync().ConfigureAwait(false);
            SettingsEntry entry = clientSettingsFrame.Entries.First(e => e.SettingId == SettingId.InitialWindowSize);

            Assert.Equal(WindowSize, (int)entry.Value);
        }

        [Fact]
        public Task InvalidRttPingResponse_RequestShouldFail()
        {
            return Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using var handler = CreateHttpClientHandler();
                using HttpClient client = CreateHttpClient(handler);
                HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri));
                _output.WriteLine(exception.Message + exception.StatusCode);
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync();
                await connection.SendDefaultResponseHeadersAsync(streamId);
                PingFrame pingFrame = await connection.ReadPingAsync(); // expect an RTT PING
                await connection.SendPingAckAsync(-6666); // send an invalid PING response
                await connection.SendResponseDataAsync(streamId, new byte[] { 1, 2, 3 }, true); // otherwise fine response
            },
            NoAutoPingResponseHttp2Options);
        }


        [OuterLoop("Runs long")]
        [Fact]
        public async Task HighBandwidthDelayProduct_ClientStreamReceiveWindowWindowScalesUp()
        {
            int maxCredit = await TestClientWindowScalingAsync(
                TimeSpan.FromMilliseconds(30),
                TimeSpan.Zero,
                2 * 1024 * 1024,
                _output);

            // Expect the client receive window to grow over 1MB:
            Assert.True(maxCredit > 1024 * 1024);
        }

        [OuterLoop("Runs long")]
        [Fact]
        public void DisableDynamicWindowScaling_HighBandwidthDelayProduct_WindowRemainsConstant()
        {
            static async Task RunTest()
            {
                AppContext.SetSwitch("System.Net.SocketsHttpHandler.Http2FlowControl.DisableDynamicWindowSizing", true);

                int maxCredit = await TestClientWindowScalingAsync(
                    TimeSpan.FromMilliseconds(30),
                    TimeSpan.Zero,
                    2 * 1024 * 1024,
                    null);

                Assert.Equal(DefaultInitialWindowSize, maxCredit);
            }

            RemoteExecutor.Invoke(RunTest).Dispose();
        }

        [OuterLoop("Runs long")]
        [Fact]
        public void MaxStreamWindowSize_WhenSet_WindowDoesNotScaleAboveMaximum()
        {
            const int MaxWindow = 654321;

            static async Task RunTest()
            {
                int maxCredit = await TestClientWindowScalingAsync(
                    TimeSpan.FromMilliseconds(30),
                    TimeSpan.Zero,
                    2 * 1024 * 1024,
                    null);

                Assert.True(maxCredit <= MaxWindow);
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_FLOWCONTROL_MAXSTREAMWINDOWSIZE"] = MaxWindow.ToString();

            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        [OuterLoop("Runs long")]
        [Fact]
        public void StreamWindowScaleThresholdMultiplier_HighValue_WindowScalesSlower()
        {
            static async Task RunTest()
            {
                int maxCredit = await TestClientWindowScalingAsync(
                    TimeSpan.FromMilliseconds(30),
                    TimeSpan.Zero,
                    2 * 1024 * 1024,
                    null);

                Assert.True(maxCredit <= 128 * 1024);
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_FLOWCONTROL_STREAMWINDOWSCALETHRESHOLDMULTIPLIER"] = "1000"; // Extreme value

            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        [OuterLoop("Runs long")]
        [Fact]
        public void StreamWindowScaleThresholdMultiplier_LowValue_WindowScalesFaster()
        {
            static async Task RunTest()
            {
                int maxCredit = await TestClientWindowScalingAsync(
                    TimeSpan.Zero,
                    TimeSpan.FromMilliseconds(15), // Low bandwidth * delay product
                    2 * 1024 * 1024,
                    null);

                Assert.True(maxCredit >= 256 * 1024);
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_FLOWCONTROL_STREAMWINDOWSCALETHRESHOLDMULTIPLIER"] = "0.00001"; // Extreme value

            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        private static async Task<int> TestClientWindowScalingAsync(
            TimeSpan networkDelay,
            TimeSpan slowBandwidthSimDelay,
            int bytesToDownload,
            ITestOutputHelper output = null,
            int maxWindowForPingStopValidation = int.MaxValue, // set to actual maximum to test if we stop sending PING when window reached maximum
            Action<SocketsHttpHandler> configureHandler = null)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(30);
            CancellationTokenSource timeoutCts = new CancellationTokenSource(timeout);

            HttpClientHandler handler = CreateHttpClientHandler(HttpVersion20.Value);
            configureHandler?.Invoke(GetUnderlyingSocketsHttpHandler(handler));

            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer(NoAutoPingResponseHttp2Options);
            using HttpClient client = new HttpClient(handler, true);
            client.DefaultRequestVersion = HttpVersion20.Value;

            Task<HttpResponseMessage> clientTask = client.GetAsync(server.Address, timeoutCts.Token);
            Http2LoopbackConnection connection = await server.AcceptConnectionAsync().ConfigureAwait(false);
            SettingsFrame clientSettingsFrame = await connection.ReadSettingsAsync().ConfigureAwait(false);

            // send server SETTINGS:
            await connection.WriteFrameAsync(new SettingsFrame()).ConfigureAwait(false);

            // Initial client SETTINGS also works as a PING. Do not send ACK immediately to avoid low RTT estimation
            await Task.Delay(networkDelay);
            await connection.WriteFrameAsync(new SettingsFrame(FrameFlags.Ack, new SettingsEntry[0]));

            // Expect SETTINGS ACK from client:
            await connection.ExpectSettingsAckAsync();

            int maxCredit = (int)clientSettingsFrame.Entries.SingleOrDefault(e => e.SettingId == SettingId.InitialWindowSize).Value;
            if (maxCredit == default) maxCredit = DefaultInitialWindowSize;
            int credit = maxCredit;

            int streamId = await connection.ReadRequestHeaderAsync();
            // Write the response.
            await connection.SendDefaultResponseHeadersAsync(streamId);

            using SemaphoreSlim creditReceivedSemaphore = new SemaphoreSlim(0);
            using SemaphoreSlim writeSemaphore = new SemaphoreSlim(1);
            int remainingBytes = bytesToDownload;

            bool pingReceivedAfterReachingMaxWindow = false;
            bool unexpectedFrameReceived = false;
            CancellationTokenSource stopFrameProcessingCts = new CancellationTokenSource();
            
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stopFrameProcessingCts.Token, timeoutCts.Token);
            Task processFramesTask = ProcessIncomingFramesAsync(linkedCts.Token);
            byte[] buffer = new byte[16384];

            while (remainingBytes > 0)
            {
                Wait(slowBandwidthSimDelay);
                while (credit == 0) await creditReceivedSemaphore.WaitAsync(timeout);
                int bytesToSend = Math.Min(Math.Min(buffer.Length, credit), remainingBytes);

                Memory<byte> responseData = buffer.AsMemory(0, bytesToSend);

                int nextRemainingBytes = remainingBytes - bytesToSend;
                bool endStream = nextRemainingBytes == 0;
                await writeSemaphore.WaitAsync();
                Interlocked.Add(ref credit, -bytesToSend);
                await connection.SendResponseDataAsync(streamId, responseData, endStream);
                writeSemaphore.Release();
                output?.WriteLine($"Sent {bytesToSend}, credit reduced to: {credit}");

                remainingBytes = nextRemainingBytes;
            }

            using HttpResponseMessage response = await clientTask;

            stopFrameProcessingCts.Cancel();
            await processFramesTask;

            int dataReceived = (await response.Content.ReadAsByteArrayAsync()).Length;
            Assert.Equal(bytesToDownload, dataReceived);
            Assert.False(pingReceivedAfterReachingMaxWindow, "Server received a PING after reaching max window");
            Assert.False(unexpectedFrameReceived, "Server received an unexpected frame, see test output for more details.");

            return maxCredit;

            async Task ProcessIncomingFramesAsync(CancellationToken cancellationToken)
            {
                // If credit > 90% of the maximum window, we are safe to assume we reached the max window.
                // We should not receive any more RTT PING's after this point
                int maxWindowCreditThreshold = (int) (0.9 * maxWindowForPingStopValidation);
                output?.WriteLine($"maxWindowCreditThreshold: {maxWindowCreditThreshold} maxWindowForPingStopValidation: {maxWindowForPingStopValidation}");

                try
                {
                    while (remainingBytes > 0 && !cancellationToken.IsCancellationRequested)
                    {
                        Frame frame = await connection.ReadFrameAsync(cancellationToken);

                        if (frame is PingFrame pingFrame)
                        {
                            // Simulate network delay for RTT PING
                            Wait(networkDelay);

                            output?.WriteLine($"Received PING ({pingFrame.Data})");

                            if (maxCredit > maxWindowCreditThreshold)
                            {
                                output?.WriteLine("PING was unexpected");
                                Volatile.Write(ref pingReceivedAfterReachingMaxWindow, true);
                            }

                            await writeSemaphore.WaitAsync(cancellationToken);
                            await connection.SendPingAckAsync(pingFrame.Data, cancellationToken);
                            writeSemaphore.Release();
                        }
                        else if (frame is WindowUpdateFrame windowUpdateFrame)
                        {
                            // Ignore connection window:
                            if (windowUpdateFrame.StreamId != streamId) continue;

                            int currentCredit = Interlocked.Add(ref credit, windowUpdateFrame.UpdateSize);
                            maxCredit = Math.Max(currentCredit, maxCredit); // Detect if client grows the window
                            creditReceivedSemaphore.Release();

                            output?.WriteLine($"UpdateSize:{windowUpdateFrame.UpdateSize} currentCredit:{currentCredit} MaxCredit: {maxCredit}");
                        }
                        else if (frame is not null)
                        {
                            Volatile.Write(ref unexpectedFrameReceived, true);
                            output?.WriteLine("Received unexpected frame: " + frame);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                

                output?.WriteLine("ProcessIncomingFramesAsync finished");
            }

            static void Wait(TimeSpan dt) { if (dt != TimeSpan.Zero) Thread.Sleep(dt); }
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

        public ValueTask WriteAsync(string message) => _messagesChannel.Writer.WriteAsync(message);

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
