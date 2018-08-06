// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public sealed class SocketsHttpHandler_HttpClientHandler_Http2_Test : HttpClientTestBase
    {
        protected override bool UseSocketsHttpHandler => true;
        public static bool SupportsAlpn => PlatformDetection.SupportsAlpn;

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_ClientPreface_Sent()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                string connectionPreface = await server.AcceptConnectionAsync();

                Assert.Equal("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n", connectionPreface);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_InitialSettings_SentAndAcked()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.AcceptConnectionAsync();

                // Receive the initial client settings frame.
                Frame receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.Settings, receivedFrame.Type);

                // Send the initial server settings frame.
                Frame emptySettings = new Frame(0, FrameType.Settings, FrameFlags.None, 0);
                await server.WriteFrameAsync(emptySettings).ConfigureAwait(false);

                // Receive the server settings frame ACK.
                receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.Settings, receivedFrame.Type);
                Assert.True(receivedFrame.AckFlag);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        [ActiveIssue(31315)]
        public async Task Http2_DataSentBeforeServerPreface_ProtocolError()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.AcceptConnectionAsync();

                // Send a frame despite not having sent the server connection preface.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.Padded, 10, 1);
                await server.WriteFrameAsync(invalidFrame);

                // This currently throws an Http2ProtocolException, but that type is not public.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        [ActiveIssue(31315)]
        public async Task Http2_StreamResetByServer_RequestFails()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.AcceptConnectionAsync();
                await server.SendConnectionPrefaceAsync();

                // Receive the request header frame.
                Frame receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                // Send a reset stream frame so that stream 1 moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.Padded, 0x1, 1);
                await server.WriteFrameAsync(resetStream);

                // This currently throws an IOException.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        [ActiveIssue(31394)]
        public async Task DataFrame_NoStream_ConnectionError()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.AcceptConnectionAsync();
                await server.SendConnectionPrefaceAsync();

                // Receive the request header frame.
                Frame receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                // Send a malformed frame.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.None, 0, 0);
                await server.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The server should receive a GOAWAY frame.
                receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.GoAway, receivedFrame.Type);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        [ActiveIssue(31520)]
        public async Task DataFrame_TooLong_ConnectionError()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.AcceptConnectionAsync();
                await server.SendConnectionPrefaceAsync();

                // Receive the request header frame.
                Frame receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                // Send a malformed frame.
                DataFrame invalidFrame = new DataFrame(new byte[Frame.MaxFrameLength + 1], FrameFlags.None, 0, 0);
                await server.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The server should receive a GOAWAY frame.
                receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.GoAway, receivedFrame.Type);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        [ActiveIssue(31315)]
        public async Task DataFrame_PaddingOnly_ResetsStream()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.AcceptConnectionAsync();
                await server.SendConnectionPrefaceAsync();

                // Receive the request header frame.
                Frame receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                // Send a malformed frame.
                DataFrame invalidFrame = new DataFrame(new byte[0], FrameFlags.Padded, 10, 1);
                await server.WriteFrameAsync(invalidFrame);

                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // Receive a RST_STREAM frame.
                receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.RstStream, receivedFrame.Type);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        [ActiveIssue(31514)]
        public async Task ClosedStream_FrameReceived_ResetsStream()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.AcceptConnectionAsync();
                await server.SendConnectionPrefaceAsync();

                // Receive the request header frame.
                Frame receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                // Send a reset stream frame so that stream 1 moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.Padded, 0x1, 1);
                await server.WriteFrameAsync(resetStream);

                // Send a frame on the now-closed stream.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.Padded, 10, 1);
                await server.WriteFrameAsync(invalidFrame);

                // Receive a RST_STREAM frame.
                receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.RstStream, receivedFrame.Type);

                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }
    }
}
