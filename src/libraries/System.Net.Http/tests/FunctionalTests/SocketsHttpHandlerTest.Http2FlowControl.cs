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

        const int DefaultInitialWindowSize = 65535;

        protected override Version UseVersion => HttpVersion20.Value;

        public SocketsHttpHandler_Http2FlowControl_Test(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Http2_FlowControl_HighBandwidthDelayProduct_ClientStreamReceiveWindowWindowScalesUp()
        {
            int maxCredit = await TestClientWindowScalingAsync(
                TimeSpan.FromMilliseconds(30),
                TimeSpan.Zero,
                2 * 1024 * 1024);

            // Expect the client receive window to grow over 1MB:
            Assert.True(maxCredit > 1024 * 1024);
        }

        [Fact]
        public async Task Http2_FlowControl_LowBandwidthDelayProduct_ClientStreamReceiveWindowStopsScaling()
        {
            int maxCredit = await TestClientWindowScalingAsync(
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(15),
                2 * 1024 * 1024);

            // Expect the client receive window to stay below 1MB:
            Assert.True(maxCredit < 1024 * 1024);
        }

        private async Task<int> TestClientWindowScalingAsync(TimeSpan networkDelay, TimeSpan slowBandwidthSimDelay, int bytesToDownload)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(30);

            using Http2LoopbackServer server = Http2LoopbackServer.CreateServer();
            using HttpClient client = CreateHttpClient();

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
                await connection.SendResponseDataAsync(streamId, responseData, endStream);
                writeSemaphore.Release();

                credit -= bytesToSend;

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

                        credit += windowUpdateFrame.UpdateSize;
                        maxCredit = Math.Max(credit, maxCredit); // Detect if client grows the window
                        _output.WriteLine("MaxCredit: " + maxCredit);
                        creditReceivedSemaphore.Release();
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
