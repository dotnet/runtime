// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [CollectionDefinition(nameof(NonParallelTestCollection), DisableParallelization = true)]
    public class NonParallelTestCollection
    {
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
            options.StartInfo.EnvironmentVariables["DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_FLOWCONTROL_STREAMWINDOWSCALETHRESHOLDMULTIPLIER"] = "10000"; // Extreme value

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
}
