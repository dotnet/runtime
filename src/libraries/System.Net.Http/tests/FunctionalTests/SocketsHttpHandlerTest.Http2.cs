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

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_DataSentBeforeServerPreface_ProtocolError()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.AcceptConnectionAsync();

                // Send a frame despite not having sent the server connection preface.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.Padded, 10, 1);
                await server.WriteFrameAsync(invalidFrame);

                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_NoResponseBody_Success()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
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
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
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
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
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
        [InlineData(SettingId.MaxFrameSize, 16383)]
        [InlineData(SettingId.MaxFrameSize, 162777216)]
        [InlineData(SettingId.InitialWindowSize, 0x80000000)]
        public async Task Http2_ServerSendsInvalidSettingsValue_ProtocolError(SettingId settingId, uint value)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                // Send invalid initial SETTINGS value
                await server.EstablishConnectionAsync(new SettingsEntry { SettingId = settingId, Value = value });

                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_StreamResetByServer_RequestFails()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                await server.ReadRequestHeaderAsync();

                // Send a reset stream frame so that stream 1 moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.Padded, 0x1, 1);
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
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                await server.ReadRequestHeaderAsync();

                // Send a malformed frame (streamId is 0)
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.None, 0, 0);
                await server.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        // This test is based on RFC 7540 section 5.1:
        // "Receiving any frame other than HEADERS or PRIORITY on a stream in this state MUST
        // be treated as a connection error (Section 5.4.1) of type PROTOCOL_ERROR."
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task DataFrame_IdleStream_ConnectionError()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                await server.ReadRequestHeaderAsync();

                // Send a data frame on stream 5, which is in the idle state.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.None, 0, 5);
                await server.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        // The spec does not clearly define how a client should behave when it receives unsolicited
        // headers from the server on an idle stream. We fall back to treating this as a connection
        // level error, as we do for other unexpected frames on idle streams.
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task HeadersFrame_IdleStream_ConnectionError()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                await server.ReadRequestHeaderAsync();

                // Send a headers frame on stream 5, which is in the idle state.
                await server.SendDefaultResponseHeadersAsync(5);

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        // This test is based on RFC 7540 section 6.8:
        // "An endpoint MUST treat a GOAWAY frame with a stream identifier other than 0x0 as a
        // connection error (Section 5.4.1) of type PROTOCOL_ERROR."
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_NonzeroStream_ConnectionError()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                await server.ReadRequestHeaderAsync();

                // Send a GoAway frame on stream 1.
                GoAwayFrame invalidFrame = new GoAwayFrame(0, 0, new byte[0], 1);
                await server.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task DataFrame_TooLong_ConnectionError()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
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
        public async Task CompletedResponse_FrameReceived_ResetsStream()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
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

                // Receive a RST_STREAM frame.
                Frame receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.RstStream, receivedFrame.Type);
                Assert.Equal(streamId, receivedFrame.StreamId);

                // Connection should still be usable.
                sendTask = client.GetAsync(server.Address);
                streamId = await server.ReadRequestHeaderAsync();
                await server.SendDefaultResponseAsync(streamId);
                response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task EmptyResponse_FrameReceived_ResetsStream()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
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

                // Receive a RST_STREAM frame.
                Frame receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.RstStream, receivedFrame.Type);
                Assert.Equal(streamId, receivedFrame.StreamId);

                // Connection should still be usable.
                sendTask = client.GetAsync(server.Address);
                streamId = await server.ReadRequestHeaderAsync();
                await server.SendDefaultResponseAsync(streamId);
                response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResetResponseStream_FrameReceived_ResetsStream()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                await server.EstablishConnectionAsync();
                int streamId = await server.ReadRequestHeaderAsync();

                // Send a reset stream frame so that stream 1 moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.None, 0x1, streamId);
                await server.WriteFrameAsync(resetStream);

                await Assert.ThrowsAsync<HttpRequestException>(async () => await sendTask);

                // Send a frame on the now-closed stream.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.None, 0, streamId);
                await server.WriteFrameAsync(invalidFrame);

                // Receive a RST_STREAM frame.
                Frame receivedFrame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.RstStream, receivedFrame.Type);
                Assert.Equal(streamId, receivedFrame.StreamId);

                // Connection should still be usable.
                sendTask = client.GetAsync(server.Address);
                streamId = await server.ReadRequestHeaderAsync();
                await server.SendDefaultResponseAsync(streamId);
                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_FlowControl_ClientDoesNotExceedWindows()
        {
            const int InitialWindowSize = 65535;
            const int ContentSize = 100_000;

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            var content = new ByteArrayContent(TestHelper.GenerateRandomContent(ContentSize));

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
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
                while (true)
                {
                    frame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                    if (frame.EndStreamFlag)
                    {
                        break;
                    }

                    Assert.Equal(streamId, frame.StreamId);
                    Assert.Equal(FrameType.Data, frame.Type);
                    Assert.Equal(FrameFlags.None, frame.Flags);
                    Assert.True(frame.Length > 0);

                    bytesReceived += frame.Length;
                }

                Assert.Equal(ContentSize, bytesReceived);

                // Verify EndStream frame
                Assert.Equal(streamId, frame.StreamId);
                Assert.Equal(FrameType.Data, frame.Type);
                Assert.Equal(FrameFlags.EndStream, frame.Flags);
                Assert.True(frame.Length == 0);

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

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
            TestHelper.EnsureHttp2Feature(handler);

            var content = new ByteArrayContent(TestHelper.GenerateRandomContent(ContentSize));

            using (var server = Http2LoopbackServer.CreateServer())
            using (var client = new HttpClient(handler))
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
                while (true)
                {
                    frame = await server.ReadFrameAsync(TimeSpan.FromSeconds(30));
                    if (frame.EndStreamFlag)
                    {
                        break;
                    }

                    Assert.Equal(streamId, frame.StreamId);
                    Assert.Equal(FrameType.Data, frame.Type);
                    Assert.Equal(FrameFlags.None, frame.Flags);
                    Assert.True(frame.Length > 0);

                    bytesReceived += frame.Length;
                }

                Assert.Equal(ContentSize, bytesReceived);

                // Verify EndStream frame
                Assert.Equal(streamId, frame.StreamId);
                Assert.Equal(FrameType.Data, frame.Type);
                Assert.Equal(FrameFlags.EndStream, frame.Flags);
                Assert.True(frame.Length == 0);

                await server.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await clientTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MaxConcurrentStreams_LimitEnforced()
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
    }
}
