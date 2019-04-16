// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandlerTest_Http2 : HttpClientHandlerTestBase
    {
        protected override bool UseSocketsHttpHandler => true;
        protected override bool UseHttp2LoopbackServer => true;

        public static bool SupportsAlpn => PlatformDetection.SupportsAlpn;

        public HttpClientHandlerTest_Http2(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task Http2_ClientPreface_Sent()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                string connectionPreface = await server.AcceptConnectionAsync();

                Assert.Equal("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n", connectionPreface);
            }
        }

        [Fact]
        public async Task Http2_InitialSettings_SentAndAcked()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
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
                // This doesn't have to be the next frame, as the client is allowed to send before receiving our SETTINGS frame.
                // So, loop until we see it (or the timeout expires)
                while (true)
                {
                    receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                    if (receivedFrame.Type == FrameType.Settings && receivedFrame.AckFlag)
                    {
                        break;
                    }
                }
            }
        }

        [Fact]
        public async Task Http2_DataSentBeforeServerPreface_ProtocolError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.AcceptConnectionAsync();

                // Send a frame despite not having sent the server connection preface.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.Padded, 10, 1);
                await server.WriteFrameAsync(invalidFrame);

                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        [Fact]
        public async Task Http2_NoResponseBody_Success()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();

                int streamId = await server.ReadRequestHeaderAsync();

                await server.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_ZeroLengthResponseBody_Success()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();

                int streamId = await server.ReadRequestHeaderAsync();

                await server.SendDefaultResponseHeadersAsync(streamId);

                // Send zero-length body
                var frame = new DataFrame(new byte[0], FrameFlags.EndStream, 0, streamId);
                await server.WriteFrameAsync(frame);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_ServerSendsValidSettingsValues_Success()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                // Send a bunch of valid SETTINGS values (that won't interfere with processing requests)
                await server.EstablishConnectionAsync(
                    new SettingsEntry { SettingId = SettingId.HeaderTableSize, Value = 0 },
                    new SettingsEntry { SettingId = SettingId.HeaderTableSize, Value = 1 },
                    new SettingsEntry { SettingId = SettingId.HeaderTableSize, Value = 345678 },
                    new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = 0 },
                    new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = 1 },
                    new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = 4567890 },
                    new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 1 },
                    new SettingsEntry { SettingId = SettingId.MaxFrameSize, Value = 16384 },
                    new SettingsEntry { SettingId = SettingId.MaxFrameSize, Value = 16777215 },
                    new SettingsEntry { SettingId = SettingId.MaxHeaderListSize, Value = 0 },
                    new SettingsEntry { SettingId = SettingId.MaxHeaderListSize, Value = 10000000 },
                    new SettingsEntry { SettingId = (SettingId)5678, Value = 1234 });

                int streamId = await server.ReadRequestHeaderAsync();

                await server.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalTheory(nameof(SupportsAlpn))]
        [InlineData(SettingId.MaxFrameSize, 16383, true)]
        [InlineData(SettingId.MaxFrameSize, 162777216, true)]
        [InlineData(SettingId.InitialWindowSize, 0x80000000, false)]
        public async Task Http2_ServerSendsInvalidSettingsValue_ProtocolError(SettingId settingId, uint value, bool skipForWinHttp)
        {
            if (IsWinHttpHandler && skipForWinHttp)
            {
                // WinHTTP does not treat these as errors, it seems to ignore the invalid setting.
                return;
            }

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                // Send invalid initial SETTINGS value
                await server.EstablishConnectionAsync(new SettingsEntry { SettingId = settingId, Value = value });

                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_StreamResetByServerBeforeHeadersSent_RequestFails()
        {
            if (IsWinHttpHandler)
            {
                // WinHTTP does not genenerate an exception here. 
                // It seems to ignore a RST_STREAM sent before headers are sent, and continue to wait for HEADERS.
                return;
            }

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                // Send a reset stream frame so that the stream moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.None, 0x2, streamId);
                await server.WriteFrameAsync(resetStream);

                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_StreamResetByServerAfterHeadersSent_RequestFails()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                // Send response headers
                await server.SendDefaultResponseHeadersAsync(streamId);

                // Send a reset stream frame so that the stream moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.None, 0x2, streamId);
                await server.WriteFrameAsync(resetStream);

                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_StreamResetByServerAfterPartialBodySent_RequestFails()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                // Send response headers and partial response body
                await server.SendDefaultResponseHeadersAsync(streamId);
                DataFrame dataFrame = new DataFrame(new byte[10], FrameFlags.None, 0, streamId);
                await server.WriteFrameAsync(dataFrame);

                // Send a reset stream frame so that the stream moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.None, 0x2, streamId);
                await server.WriteFrameAsync(resetStream);

                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        // This test is based on RFC 7540 section 6.1:
        // "If a DATA frame is received whose stream identifier field is 0x0, the recipient MUST
        // respond with a connection error (Section 5.4.1) of type PROTOCOL_ERROR."
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task DataFrame_NoStream_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                await server.ReadRequestHeaderAsync();

                // Send a malformed frame (streamId is 0)
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.None, 0, 0);
                await server.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        // This test is based on RFC 7540 section 5.1:
        // "Receiving any frame other than HEADERS or PRIORITY on a stream in this state MUST
        // be treated as a connection error (Section 5.4.1) of type PROTOCOL_ERROR."
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task DataFrame_IdleStream_ConnectionError()
        {
            if (IsWinHttpHandler)
            {
                // WinHTTP does not treat this as an error, it seems to ignore the invalid frame.
                return;
            }

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                await server.ReadRequestHeaderAsync();

                // Send a data frame on stream 5, which is in the idle state.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.None, 0, 5);
                await server.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        // The spec does not clearly define how a client should behave when it receives unsolicited
        // headers from the server on an idle stream. We fall back to treating this as a connection
        // level error, as we do for other unexpected frames on idle streams.
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task HeadersFrame_IdleStream_ConnectionError()
        {
            if (IsWinHttpHandler)
            {
                // WinHTTP does not treat this as an error, it seems to ignore the HEADERS frame.
                return;
            }

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                // Send a headers frame on stream 5, which is in the idle state.
                await server.SendDefaultResponseHeadersAsync(5);

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        private static Frame MakeSimpleHeadersFrame(int streamId, bool endHeaders = false, bool endStream = false) =>
            new HeadersFrame(new byte[] { 0x88 },       // :status: 200 
                (endHeaders ? FrameFlags.EndHeaders : FrameFlags.None) | (endStream ? FrameFlags.EndStream : FrameFlags.None),
                0, 0, 0, streamId);

        private static Frame MakeSimpleContinuationFrame(int streamId, bool endHeaders = false) =>
            new ContinuationFrame(new byte[] { 0x88 },       // :status: 200 
                (endHeaders ? FrameFlags.EndHeaders : FrameFlags.None),
                0, 0, 0, streamId);

        private static Frame MakeSimpleDataFrame(int streamId, bool endStream = false) =>
            new DataFrame(new byte[] { 0x00 },
                (endStream ? FrameFlags.EndStream : FrameFlags.None),
                0, streamId);

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_ContinuationBeforeHeaders_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                await server.WriteFrameAsync(MakeSimpleContinuationFrame(streamId));

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_DataBeforeHeaders_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                await server.WriteFrameAsync(MakeSimpleDataFrame(streamId));

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_HeadersAfterHeadersWithoutEndHeaders_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                await server.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));
                await server.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_HeadersAfterHeadersAndContinuationWithoutEndHeaders_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                await server.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));
                await server.WriteFrameAsync(MakeSimpleContinuationFrame(streamId, endHeaders: false));
                await server.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_HeadersAfterHeadersWithEndHeaders_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                await server.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: true));
                await server.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_HeadersAfterHeadersAndContinuationWithEndHeaders_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                await server.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));
                await server.WriteFrameAsync(MakeSimpleContinuationFrame(streamId, endHeaders: true));
                await server.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_DataAfterHeadersWithoutEndHeaders_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                await server.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));
                await server.WriteFrameAsync(MakeSimpleDataFrame(streamId));

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_DataAfterHeadersAndContinuationWithoutEndHeaders_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                await server.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));
                await server.WriteFrameAsync(MakeSimpleContinuationFrame(streamId, endHeaders: false));
                await server.WriteFrameAsync(MakeSimpleDataFrame(streamId));

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        // This test is based on RFC 7540 section 6.8:
        // "An endpoint MUST treat a GOAWAY frame with a stream identifier other than 0x0 as a
        // connection error (Section 5.4.1) of type PROTOCOL_ERROR."
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_NonzeroStream_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                await server.ReadRequestHeaderAsync();

                // Send a GoAway frame on stream 1.
                GoAwayFrame invalidFrame = new GoAwayFrame(0, 0, new byte[0], 1);
                await server.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task DataFrame_TooLong_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                await server.ReadRequestHeaderAsync();

                // Send a malformed frame.
                DataFrame invalidFrame = new DataFrame(new byte[Frame.MaxFrameLength + 1], FrameFlags.None, 0, 0);
                await server.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task CompletedResponse_FrameReceived_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                server.IgnoreWindowUpdates();
                int streamId = await server.ReadRequestHeaderAsync();

                // Send response and end stream.
                await server.SendDefaultResponseHeadersAsync(streamId);
                DataFrame dataFrame = new DataFrame(new byte[10], FrameFlags.EndStream, 0, streamId);
                await server.WriteFrameAsync(dataFrame);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Send a frame on the now-closed stream.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.None, 0, streamId);
                await server.WriteFrameAsync(invalidFrame);

                if (!IsWinHttpHandler)
                {
                    // The client should close the connection as this is a fatal connection level error.
                    Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
                }
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task EmptyResponse_FrameReceived_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                // Send empty response.
                await server.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Send a frame on the now-closed stream.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.None, 0, streamId);
                await server.WriteFrameAsync(invalidFrame);

                if (!IsWinHttpHandler)
                {
                    // The client should close the connection as this is a fatal connection level error.
                    Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
                }
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task CompletedResponse_WindowUpdateFrameReceived_Success()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                // Send empty response.
                await server.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Send a frame on the now-closed stream.
                WindowUpdateFrame invalidFrame = new WindowUpdateFrame(1, streamId);
                await server.WriteFrameAsync(invalidFrame);

                // The client should close the connection.
                await server.WaitForConnectionShutdownAsync();
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResetResponseStream_FrameReceived_ConnectionError()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();
                await server.SendDefaultResponseHeadersAsync(streamId);

                // Send a reset stream frame so that stream 1 moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.None, 0x1, streamId);
                await server.WriteFrameAsync(resetStream);

                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // Send a frame on the now-closed stream.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.None, 0, streamId);
                await server.WriteFrameAsync(invalidFrame);

                if (!IsWinHttpHandler)
                {
                    // The client should close the connection as this is a fatal connection level error.
                    Assert.Null(await server.ReadFrameAsync(TimeSpan.FromSeconds(30)));
                }
            }
        }

        private static async Task<int> EstablishConnectionAndProcessOneRequestAsync(HttpClient client, Http2LoopbackServer server)
        {
            // Establish connection and send initial request/response to ensure connection is available for subsequent use
            Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

            await server.EstablishConnectionAsync();

            int streamId = await server.ReadRequestHeaderAsync();
            await server.SendDefaultResponseAsync(streamId);

            HttpResponseMessage response = await sendTask;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);

            return streamId;
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_NoPendingStreams_ConnectionClosed()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                int streamId = await EstablishConnectionAndProcessOneRequestAsync(client, server);

                // Send GOAWAY.
                GoAwayFrame goAwayFrame = new GoAwayFrame(streamId, 0, new byte[0], 0);
                await server.WriteFrameAsync(goAwayFrame);

                // The client should close the connection.
                await server.WaitForConnectionShutdownAsync();

                // New request should cause a new connection
                await EstablishConnectionAndProcessOneRequestAsync(client, server);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_AllPendingStreamsValid_RequestsSucceedAndConnectionClosed()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                await EstablishConnectionAndProcessOneRequestAsync(client, server);

                // Issue three requests
                Task<HttpResponseMessage> sendTask1 = client.GetAsync(server.Address);
                Task<HttpResponseMessage> sendTask2 = client.GetAsync(server.Address);
                Task<HttpResponseMessage> sendTask3 = client.GetAsync(server.Address);

                // Receive three requests
                int streamId1 = await server.ReadRequestHeaderAsync();
                int streamId2 = await server.ReadRequestHeaderAsync();
                int streamId3 = await server.ReadRequestHeaderAsync();

                Assert.True(streamId1 < streamId2);
                Assert.True(streamId2 < streamId3);

                // Send various partial responses

                // First response: Don't send anything yet

                // Second response: Send headers, no body yet
                await server.SendDefaultResponseHeadersAsync(streamId2);

                // Third response: Send headers, partial body
                await server.SendDefaultResponseHeadersAsync(streamId3);
                await server.SendResponseDataAsync(streamId3, new byte[5], endStream: false);

                // Send a GOAWAY frame that indicates that we will process all three streams
                GoAwayFrame goAwayFrame = new GoAwayFrame(streamId3, 0, new byte[0], 0);
                await server.WriteFrameAsync(goAwayFrame);

                // Finish sending responses
                await server.SendDefaultResponseHeadersAsync(streamId1);
                await server.SendResponseDataAsync(streamId1, new byte[10], endStream: true);
                await server.SendResponseDataAsync(streamId2, new byte[10], endStream: true);
                await server.SendResponseDataAsync(streamId3, new byte[5], endStream: true);

                // We will not send any more frames, so send EOF now, and ensure the client handles this properly.
                server.ShutdownSend();

                // Receive all responses
                HttpResponseMessage response1 = await sendTask1;
                Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                Assert.Equal(10, (await response1.Content.ReadAsByteArrayAsync()).Length);
                HttpResponseMessage response2 = await sendTask2;
                Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
                Assert.Equal(10, (await response2.Content.ReadAsByteArrayAsync()).Length);
                HttpResponseMessage response3 = await sendTask3;
                Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
                Assert.Equal(10, (await response3.Content.ReadAsByteArrayAsync()).Length);

                // Now that all pending responses have been sent, the client should close the connection.
                await server.WaitForConnectionShutdownAsync();

                // New request should cause a new connection
                await EstablishConnectionAndProcessOneRequestAsync(client, server);
            }
        }

        private static async Task<int> ReadToEndOfStream(Http2LoopbackServer server, int streamId)
        {
            int bytesReceived = 0;
            while (true)
            {
                Frame frame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                Assert.Equal(streamId, frame.StreamId);
                Assert.Equal(FrameType.Data, frame.Type);

                bytesReceived += frame.Length;

                if (frame.Flags == FrameFlags.EndStream)
                {
                    break;
                }

                Assert.Equal(FrameFlags.None, frame.Flags);
                Assert.True(frame.Length > 0);
            }

            return bytesReceived;
        }

        [OuterLoop("Uses Task.Delay")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_FlowControl_ClientDoesNotExceedWindows()
        {
            const int InitialWindowSize = 65535;
            const int ContentSize = 100_000;

            var content = new ByteArrayContent(TestHelper.GenerateRandomContent(ContentSize));

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> clientTask = client.PostAsync(server.Address, content);

                await server.EstablishConnectionAsync();

                Frame frame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                int streamId = frame.StreamId;
                Assert.Equal(FrameType.Headers, frame.Type);
                Assert.Equal(FrameFlags.EndHeaders, frame.Flags);

                // Receive up to initial window size
                int bytesReceived = 0;
                while (bytesReceived < InitialWindowSize)
                {
                    frame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                    Assert.Equal(streamId, frame.StreamId);
                    Assert.Equal(FrameType.Data, frame.Type);
                    Assert.Equal(FrameFlags.None, frame.Flags);
                    Assert.True(frame.Length > 0);

                    bytesReceived += frame.Length;
                }

                Assert.Equal(InitialWindowSize, bytesReceived);

                // Issue another read. It shouldn't complete yet. Wait a brief period of time to ensure it doesn't complete.
                Task<Frame> readFrameTask = server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase connection window by one. This should still not complete the read.
                await server.WriteFrameAsync(new WindowUpdateFrame(1, 0));

                await Task.Delay(500);

                Assert.False(readFrameTask.IsCompleted);

                // Increase stream window by two. This should complete the read with a single byte.
                await server.WriteFrameAsync(new WindowUpdateFrame(2, streamId));

                frame = await readFrameTask;
                Assert.Equal(1, frame.Length);
                bytesReceived++;

                // Issue another read and ensure it doesn't complete yet.
                readFrameTask = server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase connection window by two. This should complete the read with a single byte.
                await server.WriteFrameAsync(new WindowUpdateFrame(2, 0));

                frame = await readFrameTask;
                Assert.Equal(1, frame.Length);
                bytesReceived++;

                // Issue another read and ensure it doesn't complete yet.
                readFrameTask = server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase connection window to allow exactly the remaining request size. This should still not complete the read.
                await server.WriteFrameAsync(new WindowUpdateFrame(ContentSize - bytesReceived - 1, 0));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase stream window to allow exactly the remaining request size. This should allow the rest of the request to be sent.
                await server.WriteFrameAsync(new WindowUpdateFrame(ContentSize - bytesReceived, streamId));

                frame = await readFrameTask;
                Assert.Equal(streamId, frame.StreamId);
                Assert.Equal(FrameType.Data, frame.Type);
                Assert.Equal(FrameFlags.None, frame.Flags);
                Assert.True(frame.Length > 0);

                bytesReceived += frame.Length;

                // Read to end of stream
                bytesReceived += await ReadToEndOfStream(server, streamId);

                Assert.Equal(ContentSize, bytesReceived);

                await server.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await clientTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_InitialWindowSize_ClientDoesNotExceedWindows()
        {
            const int DefaultInitialWindowSize = 65535;
            const int ContentSize = 100_000;

            var content = new ByteArrayContent(TestHelper.GenerateRandomContent(ContentSize));

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> clientTask = client.PostAsync(server.Address, content);

                await server.EstablishConnectionAsync();

                // Bump connection window so it won't block the client.
                await server.WriteFrameAsync(new WindowUpdateFrame(ContentSize - DefaultInitialWindowSize, 0));

                Frame frame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                int streamId = frame.StreamId;
                Assert.Equal(FrameType.Headers, frame.Type);
                Assert.Equal(FrameFlags.EndHeaders, frame.Flags);

                // Receive up to initial window size
                int bytesReceived = 0;
                while (bytesReceived < DefaultInitialWindowSize)
                {
                    frame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                    Assert.Equal(streamId, frame.StreamId);
                    Assert.Equal(FrameType.Data, frame.Type);
                    Assert.Equal(FrameFlags.None, frame.Flags);
                    Assert.True(frame.Length > 0);

                    bytesReceived += frame.Length;
                }

                Assert.Equal(DefaultInitialWindowSize, bytesReceived);

                // Issue another read. It shouldn't complete yet. Wait a brief period of time to ensure it doesn't complete.
                Task<Frame> readFrameTask = server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Change SETTINGS_INITIAL_WINDOW_SIZE to 0. This will make the client's credit go negative.
                server.ExpectSettingsAck();
                await server.WriteFrameAsync(new SettingsFrame(new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = 0 }));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase stream window by one. Client credit will still be negative.
                await server.WriteFrameAsync(new WindowUpdateFrame(1, streamId));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Change SETTINGS_INITIAL_WINDOW_SIZE to 1. Client credit will still be negative.
                server.ExpectSettingsAck();
                await server.WriteFrameAsync(new SettingsFrame(new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = 1 }));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase stream window so client credit will be 0.
                await server.WriteFrameAsync(new WindowUpdateFrame(DefaultInitialWindowSize - 2, streamId));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase stream window by one, so client can now send a single byte.
                await server.WriteFrameAsync(new WindowUpdateFrame(1, streamId));

                frame = await readFrameTask;
                Assert.Equal(FrameType.Data, frame.Type);
                Assert.Equal(1, frame.Length);
                bytesReceived++;

                // Issue another read and ensure it doesn't complete yet.
                readFrameTask = server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase SETTINGS_INITIAL_WINDOW_SIZE to 2, so client can now send a single byte.
                server.ExpectSettingsAck();
                await server.WriteFrameAsync(new SettingsFrame(new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = 2 }));

                frame = await readFrameTask;
                Assert.Equal(FrameType.Data, frame.Type);
                Assert.Equal(1, frame.Length);
                bytesReceived++;

                // Issue another read and ensure it doesn't complete yet.
                readFrameTask = server.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase SETTINGS_INITIAL_WINDOW_SIZE to be enough that the client can send the rest of the content.
                server.ExpectSettingsAck();
                await server.WriteFrameAsync(new SettingsFrame(new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = ContentSize - (DefaultInitialWindowSize - 1) }));

                frame = await readFrameTask;
                Assert.Equal(streamId, frame.StreamId);
                Assert.Equal(FrameType.Data, frame.Type);
                Assert.Equal(FrameFlags.None, frame.Flags);
                Assert.True(frame.Length > 0);

                bytesReceived += frame.Length;

                // Read to end of stream
                bytesReceived += await ReadToEndOfStream(server, streamId);

                Assert.Equal(ContentSize, bytesReceived);

                await server.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await clientTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MaxConcurrentStreams_LimitEnforced()
        {
            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                server.IgnoreWindowUpdates();

                // Process first request and send response.
                int streamId = await server.ReadRequestHeaderAsync();
                await server.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Change MaxConcurrentStreams setting and wait for ack.
                // (We don't want to send any new requests until we receive the ack, otherwise we may have a timing issue.)
                SettingsFrame settingsFrame = new SettingsFrame(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 0 });
                await server.WriteFrameAsync(settingsFrame);
                Frame settingsAckFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.Settings, settingsAckFrame.Type);
                Assert.Equal(FrameFlags.Ack, settingsAckFrame.Flags);

                // Issue two more requests. We shouldn't send either of them.
                sendTask = client.GetAsync(server.Address);
                Task<HttpResponseMessage> sendTask2 = client.GetAsync(server.Address);

                // Issue another read. It shouldn't complete yet. Wait a brief period of time to ensure it doesn't complete.
                Task<Frame> readFrameTask = server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Change MaxConcurrentStreams again to allow a single request to come through.
                server.ExpectSettingsAck();
                settingsFrame = new SettingsFrame(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 1 });
                await server.WriteFrameAsync(settingsFrame);

                // First request should be sent
                Frame frame = await readFrameTask;
                Assert.Equal(FrameType.Headers, frame.Type);
                streamId = frame.StreamId;

                // Issue another read. Second request should not be sent yet.
                readFrameTask = server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Send response for first request
                await server.SendDefaultResponseAsync(streamId);
                response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Second request should be sent now
                frame = await readFrameTask;
                Assert.Equal(FrameType.Headers, frame.Type);
                streamId = frame.StreamId;

                // Send response for second request
                await server.SendDefaultResponseAsync(streamId);
                response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_WaitingForStream_Cancellation()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                server.IgnoreWindowUpdates();

                // Process first request and send response.
                int streamId = await server.ReadRequestHeaderAsync();
                await server.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Change MaxConcurrentStreams setting and wait for ack.
                // (We don't want to send any new requests until we receive the ack, otherwise we may have a timing issue.)
                SettingsFrame settingsFrame = new SettingsFrame(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 0 });
                await server.WriteFrameAsync(settingsFrame);
                Frame settingsAckFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.Settings, settingsAckFrame.Type);
                Assert.Equal(FrameFlags.Ack, settingsAckFrame.Flags);

                // Issue a new request, so that we can cancel it while it waits for a stream.
                var cts = new CancellationTokenSource();
                sendTask = client.GetAsync(server.Address, cts.Token);

                // Make sure that the request makes it to the point where it's waiting for a connection.
                // It's possible that we'll still initiate a cancellation before it makes it to the queue,
                // but it should still behave in the same way if so.
                await Task.Delay(500);

                Stopwatch stopwatch = Stopwatch.StartNew();
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sendTask);

                // Ensure that the cancellation occurs promptly
                stopwatch.Stop();
                Assert.True(stopwatch.ElapsedMilliseconds < 30000);

                // As the client has not allocated a stream ID when the corresponding request is cancelled,
                // we do not send a RST stream frame.
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_WaitingOnWindowCredit_Cancellation()
        {
            // The goal of this test is to get the client into the state where it has sent the headers,
            // but is waiting on window credit before it will send the body. We then issue a cancellation
            // to ensure the request is cancelled as expected.
            const int InitialWindowSize = 65535;
            const int ContentSize = InitialWindowSize + 1;

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            var content = new ByteArrayContent(TestHelper.GenerateRandomContent(ContentSize));

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                var cts = new CancellationTokenSource();
                Task<HttpResponseMessage> clientTask = client.PostAsync(server.Address, content, cts.Token);

                await server.EstablishConnectionAsync();

                Frame frame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                int streamId = frame.StreamId;
                Assert.Equal(FrameType.Headers, frame.Type);
                Assert.Equal(FrameFlags.EndHeaders, frame.Flags);

                // Receive up to initial window size
                int bytesReceived = 0;
                while (bytesReceived < InitialWindowSize)
                {
                    frame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                    Assert.Equal(streamId, frame.StreamId);
                    Assert.Equal(FrameType.Data, frame.Type);
                    Assert.Equal(FrameFlags.None, frame.Flags);
                    Assert.True(frame.Length > 0);

                    bytesReceived += frame.Length;
                }

                // The client is waiting for more credit in order to send the last byte of the
                // request body. Test cancellation at this point.
                Stopwatch stopwatch = Stopwatch.StartNew();

                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await clientTask);

                // Ensure that the cancellation occurs promptly
                stopwatch.Stop();
                Assert.True(stopwatch.ElapsedMilliseconds < 30000);

                // The server should receive a RstStream frame.
                frame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.RstStream, frame.Type);
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_PendingSend_Cancellation()
        {
            // The goal of this test is to get the client into the state where it is sending content,
            // but the send pends because the TCP window is full.
            const int InitialWindowSize = 65535;
            const int ContentSize = InitialWindowSize * 2; // Double the default TCP window size.

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            var content = new ByteArrayContent(TestHelper.GenerateRandomContent(ContentSize));

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                var cts = new CancellationTokenSource();

                Task<HttpResponseMessage> clientTask = client.PostAsync(server.Address, content, cts.Token);

                await server.EstablishConnectionAsync();

                Frame frame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                int streamId = frame.StreamId;
                Assert.Equal(FrameType.Headers, frame.Type);
                Assert.Equal(FrameFlags.EndHeaders, frame.Flags);

                // Increase the size of the HTTP/2 Window, so that it is large enough to fill the
                // TCP window when we do not perform any reads on the server side.
                await server.WriteFrameAsync(new WindowUpdateFrame(InitialWindowSize, streamId));

                // Give the client time to read the window update frame, and for the write to pend.
                await Task.Delay(1000);
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await clientTask);
            }
        }
    }
}
