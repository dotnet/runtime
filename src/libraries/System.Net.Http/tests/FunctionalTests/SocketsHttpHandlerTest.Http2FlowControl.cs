// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Quic;
using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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

    // This test class contains tests which are strongly timing-dependant.
    // There are two mitigations avoid flaky behavior on CI:
    // - The tests are executed in a non-parallel manner
    // - The timing-dependent behavior is pushed to the extremes, making it very unlikely to fail.
    [Collection(nameof(NonParallelTestCollection))]
    [ConditionalClass(typeof(SocketsHttpHandler_Http2FlowControl_Test), nameof(IsSupported))]
    public sealed class SocketsHttpHandler_Http2FlowControl_Test : HttpClientHandlerTestBase
    {
        public static readonly bool IsSupported = PlatformDetection.SupportsAlpn && PlatformDetection.IsNotBrowser;

        protected override Version UseVersion => HttpVersion20.Value;

        public SocketsHttpHandler_Http2FlowControl_Test(ITestOutputHelper output) : base(output)
        {
        }

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
        public void DisableDynamicWindowScaling_HighBandwidthDelayProduct_WindowRemainsConstant()
        {
            static async Task RunTest()
            {
                AppContext.SetSwitch("System.Net.SocketsHttpHandler.Http2FlowControl.DisableDynamic2WindowSizing", true);

                int maxCredit = await TestClientWindowScalingAsync(
                    TimeSpan.FromMilliseconds(30),
                    TimeSpan.Zero,
                    2 * 1024 * 1024,
                    null);

                Assert.Equal(DefaultInitialWindowSize, maxCredit);
            }

            RemoteExecutor.Invoke(RunTest).Dispose();
        }

        [Fact]
        public void MaxStreamWindowSize_HighBandwidthDelayProduct_WindowStopsAtMaxValue()
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

        [Fact]
        public async Task LowBandwidthDelayProduct_ClientStreamReceiveWindowStopsScaling()
        {
            int maxCredit = await TestClientWindowScalingAsync(
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(15),
                2 * 1024 * 1024,
                _output);

            // Expect the client receive window to stay below 1MB:
            Assert.True(maxCredit < 1024 * 1024);
        }

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
            ITestOutputHelper output)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(30);

            HttpClientHandler handler = CreateHttpClientHandler(HttpVersion20.Value);

            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using HttpClient client = new HttpClient(handler, true);
            client.DefaultRequestVersion = HttpVersion20.Value;

            Task<HttpResponseMessage> clientTask = client.GetAsync(server.Address);
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
            _ = Task.Run(ProcessIncomingFramesAsync);
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
            int dataReceived = (await response.Content.ReadAsByteArrayAsync()).Length;
            Assert.Equal(bytesToDownload, dataReceived);

            return maxCredit;

            async Task ProcessIncomingFramesAsync()
            {
                while (remainingBytes > 0)
                {
                    Frame frame = await connection.ReadFrameAsync(timeout);

                    if (frame is PingFrame pingFrame)
                    {
                        // Simulate network delay for RTT PING
                        Wait(networkDelay);

                        await writeSemaphore.WaitAsync();
                        await connection.SendPingAckAsync(pingFrame.Data);
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
                        throw new Exception("Unexpected frame: " + frame);
                    }
                }
            }

            static void Wait(TimeSpan dt) { if (dt != TimeSpan.Zero) Thread.Sleep(dt); }
        }
    }
}
