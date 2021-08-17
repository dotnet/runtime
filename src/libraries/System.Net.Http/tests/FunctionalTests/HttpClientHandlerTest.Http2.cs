// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandlerTest_Http2 : HttpClientHandlerTestBase
    {
        protected override Version UseVersion => HttpVersion.Version20;

        public static bool SupportsAlpn => PlatformDetection.SupportsAlpn;

        public HttpClientHandlerTest_Http2(ITestOutputHelper output) : base(output)
        {
        }

        private async Task AssertProtocolErrorAsync<T>(Task task, ProtocolErrors errorCode)
            where T : Exception
        {
            Exception e = await Assert.ThrowsAsync<T>(() => task);
            string text = e.ToString();
            Assert.Contains(((int)errorCode).ToString("x"), text);
            Assert.Contains(
                Enum.IsDefined(typeof(ProtocolErrors), errorCode) ? errorCode.ToString() : "(unknown error)",
                text);
        }

        private Task AssertProtocolErrorAsync(Task task, ProtocolErrors errorCode)
        {
            return AssertProtocolErrorAsync<HttpRequestException>(task, errorCode);
        }

        private Task AssertProtocolErrorForIOExceptionAsync(Task task, ProtocolErrors errorCode)
        {
            return AssertProtocolErrorAsync<IOException>(task, errorCode);
        }

        private async Task<(bool, T)> IgnoreSpecificException<ExpectedException, T>(Task<T> task, string expectedExceptionContent = null) where ExpectedException : Exception
        {
            try
            {
                return (true, await task);
            }
            catch (ExpectedException e)
            {
                if (expectedExceptionContent != null)
                {
                    if (!e.ToString().Contains(expectedExceptionContent))
                    {
                        throw;
                    }
                }

                return (false, default(T));
            }
        }

        [Fact]
        public async Task Http2_ClientPreface_Sent()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                string connectionPreface = (await server.AcceptConnectionAsync()).PrefixString;

                Assert.Equal(Http2LoopbackConnection.Http2Prefix, connectionPreface);
            }
        }

        [Fact]
        public async Task Http2_InitialSettings_SentAndAcked()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.AcceptConnectionAsync();

                // Receive the initial client settings frame.
                Frame receivedFrame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.Settings, receivedFrame.Type);

                // Send the initial server settings frame.
                Frame emptySettings = new Frame(0, FrameType.Settings, FrameFlags.None, 0);
                await connection.WriteFrameAsync(emptySettings).ConfigureAwait(false);

                // Receive the server settings frame ACK.
                // This doesn't have to be the next frame, as the client is allowed to send before receiving our SETTINGS frame.
                // So, loop until we see it (or the timeout expires)
                while (true)
                {
                    receivedFrame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
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
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.AcceptConnectionAsync();

                // Send a frame despite not having sent the server connection preface.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.Padded, 10, 1);
                await connection.WriteFrameAsync(invalidFrame);

                await Assert.ThrowsAsync<HttpRequestException>(() => sendTask);
            }
        }

        [Fact]
        public async Task Http2_NoResponseBody_Success()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                await connection.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_ZeroLengthResponseBody_Success()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();

                await connection.SendDefaultResponseHeadersAsync(streamId);

                // Send zero-length body
                var frame = new DataFrame(new byte[0], FrameFlags.EndStream, 0, streamId);
                await connection.WriteFrameAsync(frame);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Theory]
        [InlineData("Client content", null)]
        [InlineData("Client content", "Server content")]
        [InlineData(null, null)]
        [InlineData(null, "Server content")]
        public async Task Http2ClearText_SendAsync_Success(string clientContent, string serverContent)
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri)
                {
                    Version = UseVersion,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    Content = clientContent is null ? (HttpContent)null : new StringContent(clientContent),
                };

                using HttpResponseMessage response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                Assert.Equal(serverContent ?? String.Empty, responseContent);
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                Assert.IsNotType<SslStream>(connection.Stream);

                HttpRequestData requestData = await connection.ReadRequestDataAsync();                
                string requestContent = requestData.Body is null ? (string)null : Encoding.ASCII.GetString(requestData.Body);
                Assert.Equal(clientContent, requestContent);
                await connection.SendResponseAsync(HttpStatusCode.OK, content: serverContent);
            }, new Http2Options() { UseSsl = false });
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_ServerSendsValidSettingsValues_Success()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                // Send a bunch of valid SETTINGS values (that won't interfere with processing requests)
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(
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

                int streamId = await connection.ReadRequestHeaderAsync();

                await connection.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalTheory(nameof(SupportsAlpn))]
        [InlineData(SettingId.MaxFrameSize, 16383)]
        [InlineData(SettingId.MaxFrameSize, 162777216)]
        [InlineData(SettingId.InitialWindowSize, 0x80000000)]
        public async Task Http2_ServerSendsInvalidSettingsValue_Error(SettingId settingId, uint value)
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                // Send invalid initial SETTINGS value
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = settingId, Value = value });

                await Assert.ThrowsAsync<HttpRequestException>(() => sendTask);

                connection.Dispose();
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_StreamResetByServerBeforeHeadersSent_RequestFails()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                // Send a reset stream frame so that the stream moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.INTERNAL_ERROR, streamId);
                await connection.WriteFrameAsync(resetStream);

                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.INTERNAL_ERROR);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_StreamResetByServerAfterHeadersSent_RequestFails()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                // Send response headers
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // Send a reset stream frame so that the stream moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.INTERNAL_ERROR, streamId);
                await connection.WriteFrameAsync(resetStream);

                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.INTERNAL_ERROR);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_StreamResetByServerAfterPartialBodySent_RequestFails()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                // Send response headers and partial response body
                await connection.SendDefaultResponseHeadersAsync(streamId);
                DataFrame dataFrame = new DataFrame(new byte[10], FrameFlags.None, 0, streamId);
                await connection.WriteFrameAsync(dataFrame);

                // Send a reset stream frame so that the stream moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.INTERNAL_ERROR, streamId);
                await connection.WriteFrameAsync(resetStream);

                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.INTERNAL_ERROR);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GetAsync_StreamRefused_RequestIsRetried()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                (_, Http2LoopbackConnection connection) = await EstablishConnectionAndProcessOneRequestAsync(client, server);

                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);
                (int streamId1, HttpRequestData requestData1) = await connection.ReadAndParseRequestHeaderAsync(readBody: true);

                // Send SETTINGS frame to change stream limit to 0, allowing us to send a REFUSED_STREAM error due to the new limit
                await connection.ExpectSettingsAckAsync();
                Frame frame = new SettingsFrame(new SettingsEntry() { SettingId = SettingId.MaxConcurrentStreams, Value = 0 });
                await connection.WriteFrameAsync(frame);

                // Send a RST_STREAM with error = REFUSED_STREAM, indicating the request can be retried.
                frame = new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.REFUSED_STREAM, streamId1);
                await connection.WriteFrameAsync(frame);

                await connection.PingPong();

                // Send SETTINGS frame to change stream limit to 1, allowing the request to now be processed
                await connection.ExpectSettingsAckAsync();
                frame = new SettingsFrame(new SettingsEntry() { SettingId = SettingId.MaxConcurrentStreams, Value = 1 });
                await connection.WriteFrameAsync(frame);

                // Process retried request
                (int streamId2, HttpRequestData requestData2) = await connection.ReadAndParseRequestHeaderAsync(readBody: true);
                await connection.SendDefaultResponseAsync(streamId2);
                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task PostAsync_StreamRefused_RequestIsRetried()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                const string Content = "Hello world";

                (_, Http2LoopbackConnection connection) = await EstablishConnectionAndProcessOneRequestAsync(client, server);

                Task<HttpResponseMessage> sendTask = client.PostAsync(server.Address, new StringContent(Content));
                (int streamId1, HttpRequestData requestData1) = await connection.ReadAndParseRequestHeaderAsync(readBody: true);
                Assert.Equal(Content, Encoding.UTF8.GetString(requestData1.Body));

                // Send SETTINGS frame to change stream limit to 0, allowing us to send a REFUSED_STREAM error due to the new limit
                await connection.ExpectSettingsAckAsync();
                Frame frame = new SettingsFrame(new SettingsEntry() { SettingId = SettingId.MaxConcurrentStreams, Value = 0 });
                await connection.WriteFrameAsync(frame);

                // Send a RST_STREAM with error = REFUSED_STREAM, indicating the request can be retried.
                frame = new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.REFUSED_STREAM, streamId1);
                await connection.WriteFrameAsync(frame);

                await connection.PingPong();

                // Send SETTINGS frame to change stream limit to 1, allowing the request to now be processed
                await connection.ExpectSettingsAckAsync();
                frame = new SettingsFrame(new SettingsEntry() { SettingId = SettingId.MaxConcurrentStreams, Value = 1 });
                await connection.WriteFrameAsync(frame);

                // Process retried request
                (int streamId2, HttpRequestData requestData2) = await connection.ReadAndParseRequestHeaderAsync(readBody: true);
                Assert.Equal(Content, Encoding.UTF8.GetString(requestData2.Body));
                await connection.SendDefaultResponseAsync(streamId2);
                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop("Uses delays")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GetAsync_SettingsFrameNotSentOnNewConnection_ClientApplies100StreamLimit()
        {
            const int DefaultMaxConcurrentStreams = 100;
            TimeSpan timeout = TimeSpan.FromSeconds(3);
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<Http2LoopbackConnection> connectionTask = AcceptConnectionAndReadSettings(server, timeout);

                List<Task<HttpResponseMessage>> sendTasks = new List<Task<HttpResponseMessage>>();
                for (int i = 0; i < DefaultMaxConcurrentStreams; i++)
                {
                    sendTasks.Add(client.GetAsync(server.Address));
                }

                Http2LoopbackConnection connection = await connectionTask.WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

                // Client sets the default MaxConcurrentStreams to 100, so accept 100 requests.
                List<int> acceptedRequests = await AcceptRequests(connection, DefaultMaxConcurrentStreams).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

                Assert.Equal(DefaultMaxConcurrentStreams, acceptedRequests.Count);

                // Extra request is queued on the client.
                Task<HttpResponseMessage> extraSendTask = client.GetAsync(server.Address);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.ReadRequestHeaderAsync()).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

                // Send SETTINGS frame to increase the default stream limit by 1, unblocking the extra request.
                await connection.SendSettingsAsync(timeout, new[] { new SettingsEntry() { SettingId = SettingId.MaxConcurrentStreams, Value = DefaultMaxConcurrentStreams + 1 } }).ConfigureAwait(false);

                (int extraStreamId, _) = await connection.ReadAndParseRequestHeaderAsync().WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

                // Respond to all the requests.
                acceptedRequests.Add(extraStreamId);
                foreach (int streamId in acceptedRequests)
                {
                    await connection.SendDefaultResponseAsync(streamId).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
                }

                sendTasks.Add(extraSendTask);
                await Task.WhenAll(sendTasks).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

                foreach (Task<HttpResponseMessage> sendTask in sendTasks)
                {
                    using HttpResponseMessage response = sendTask.Result;
                    Assert.True(response.IsSuccessStatusCode);
                }
            }
        }

        [OuterLoop("Uses delays")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GetAsync_ServerDelaysSendingSettingsThenSetsLowerMaxConcurrentStreamsLimitThenIncreaseIt_ClientAppliesEachLimitChangeProperly()
        {
            const int DefaultMaxConcurrentStreams = 100;
            const int ExtraStreams = 20;
            TimeSpan timeout = TimeSpan.FromSeconds(3);
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<Http2LoopbackConnection> connectionTask = AcceptConnectionAndReadSettings(server, timeout);

                List<Task<HttpResponseMessage>> sendTasks = new List<Task<HttpResponseMessage>>();
                for (int i = 0; i < DefaultMaxConcurrentStreams + ExtraStreams; i++)
                {
                    sendTasks.Add(client.GetAsync(server.Address));
                }

                Http2LoopbackConnection connection = await connectionTask.WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

                // Client sets the default MaxConcurrentStreams to 100, so accept 100 requests.
                List<int> acceptedRequests = await AcceptRequests(connection, DefaultMaxConcurrentStreams + ExtraStreams).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
                Assert.Equal(DefaultMaxConcurrentStreams, acceptedRequests.Count);

                // Send SETTINGS frame with MaxConcurrentStreams = 102
                await connection.SendSettingsAsync(timeout, new[] { new SettingsEntry() { SettingId = SettingId.MaxConcurrentStreams, Value = DefaultMaxConcurrentStreams + 2 } }).ConfigureAwait(false);

                // Increased MaxConcurrentStreams ublocks only 2 requests.
                List<int> acceptedExtraRequests = await AcceptRequests(connection, ExtraStreams).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
                Assert.Equal(2, acceptedExtraRequests.Count);

                acceptedRequests.AddRange(acceptedExtraRequests);

                // Send SETTINGS frame with MaxConcurrentStreams = DefaultMaxConcurrentStreams + ExtraStreams
                await connection.ExpectSettingsAckAsync().ConfigureAwait(false);
                Frame frame = new SettingsFrame(new SettingsEntry() { SettingId = SettingId.MaxConcurrentStreams, Value = DefaultMaxConcurrentStreams + ExtraStreams });
                await connection.WriteFrameAsync(frame).ConfigureAwait(false);

                // Increased MaxConcurrentStreams ublocks all remaining requests.
                acceptedExtraRequests = await AcceptRequests(connection, ExtraStreams - 2).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
                Assert.Equal(ExtraStreams - 2, acceptedExtraRequests.Count);

                acceptedRequests.AddRange(acceptedExtraRequests);

                // Respond to all the requests.
                foreach (int streamId in acceptedRequests)
                {
                    await connection.SendDefaultResponseAsync(streamId).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
                }

                await Task.WhenAll(sendTasks).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

                foreach (Task<HttpResponseMessage> sendTask in sendTasks)
                {
                    using HttpResponseMessage response = sendTask.Result;
                    Assert.True(response.IsSuccessStatusCode);
                }
            }
        }

        [OuterLoop("Uses delays")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GetAsync_ServerSendSettingsWithoutMaxConcurrentStreams_ClientAppliesInfiniteLimit()
        {
            const int DefaultMaxConcurrentStreams = 100;
            TimeSpan timeout = TimeSpan.FromSeconds(3);
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<Http2LoopbackConnection> connectionTask = AcceptConnectionAndReadSettings(server, timeout);

                List<Task<HttpResponseMessage>> sendTasks = new List<Task<HttpResponseMessage>>();
                for (int i = 0; i < DefaultMaxConcurrentStreams; i++)
                {
                    sendTasks.Add(client.GetAsync(server.Address));
                }

                Http2LoopbackConnection connection = await connectionTask.WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

                // Client sets the default MaxConcurrentStreams to 100, so accept 100 requests.
                List<int> acceptedRequests = await AcceptRequests(connection, DefaultMaxConcurrentStreams).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
                Assert.Equal(DefaultMaxConcurrentStreams, acceptedRequests.Count);

                // Extra request is queued on the client.
                Task<HttpResponseMessage> extraSendTask = client.GetAsync(server.Address);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.ReadRequestHeaderAsync()).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

                // Send SETTINGS frame without MaxConcurrentSettings value.
                await connection.SendSettingsAsync(timeout, new SettingsEntry[0]).ConfigureAwait(false);

                // Send 100 more requests
                sendTasks.Add(extraSendTask);
                for (int i = 0; i < DefaultMaxConcurrentStreams; i++)
                {
                    sendTasks.Add(client.GetAsync(server.Address));
                }

                // Client sets the MaxConcurrentStreams to 'infinite', so accept 100 + 1 extra requests.
                List<int> acceptedExtraRequests = await AcceptRequests(connection, DefaultMaxConcurrentStreams + 1).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
                Assert.Equal(DefaultMaxConcurrentStreams + 1, acceptedExtraRequests.Count);

                // Respond to all the requests.
                acceptedRequests.AddRange(acceptedExtraRequests);
                foreach (int streamId in acceptedRequests)
                {
                    await connection.SendDefaultResponseAsync(streamId).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);
                }

                sendTasks.Add(extraSendTask);
                await Task.WhenAll(sendTasks).WaitAsync(TestHelper.PassingTestTimeout).ConfigureAwait(false);

                foreach (Task<HttpResponseMessage> sendTask in sendTasks)
                {
                    using HttpResponseMessage response = sendTask.Result;
                    Assert.True(response.IsSuccessStatusCode);
                }
            }
        }

        // This test is based on RFC 7540 section 6.1:
        // "If a DATA frame is received whose stream identifier field is 0x0, the recipient MUST
        // respond with a connection error (Section 5.4.1) of type PROTOCOL_ERROR."
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task DataFrame_NoStream_ConnectionError()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                await connection.ReadRequestHeaderAsync();

                // Send a malformed frame (streamId is 0)
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.None, 0, 0);
                await connection.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.PROTOCOL_ERROR);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await connection.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        // This test is based on RFC 7540 section 5.1:
        // "Receiving any frame other than HEADERS or PRIORITY on a stream in this state MUST
        // be treated as a connection error (Section 5.4.1) of type PROTOCOL_ERROR."
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task DataFrame_IdleStream_ConnectionError()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                await connection.ReadRequestHeaderAsync();

                // Send a data frame on stream 5, which is in the idle state.
                DataFrame invalidFrame = new DataFrame(new byte[10], FrameFlags.None, 0, 5);
                await connection.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.PROTOCOL_ERROR);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await connection.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        // The spec does not clearly define how a client should behave when it receives unsolicited
        // headers from the server on an idle stream. We fall back to treating this as a connection
        // level error, as we do for other unexpected frames on idle streams.
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task HeadersFrame_IdleStream_ConnectionError()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                // Send a headers frame on stream 5, which is in the idle state.
                await connection.SendDefaultResponseHeadersAsync(5);

                // As this is a connection level error, the client should see the request fail.
                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.PROTOCOL_ERROR);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await connection.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        private async Task<Http2LoopbackConnection> AcceptConnectionAndReadSettings(Http2LoopbackServer server, TimeSpan timeout)
        {
            Http2LoopbackConnection connection = await server.AcceptConnectionAsync(timeout).ConfigureAwait(false);
            await connection.ReadSettingsAsync().ConfigureAwait(false);
            return connection;
        }

        private async Task<List<int>> AcceptRequests(Http2LoopbackConnection connection, int maxRequests = int.MaxValue)
        {
            List<int> streamIds = new List<int>();
            for (int i = 0; i < maxRequests; i++)
            {
                try
                {
                    streamIds.Add((await connection.ReadAndParseRequestHeaderAsync().ConfigureAwait(false)).streamId);
                }
                catch (OperationCanceledException)
                {
                    return streamIds;
                }
            }

            return streamIds;
        }

        private static Frame MakeSimpleHeadersFrame(int streamId, bool endHeaders = false, bool endStream = false) =>
            new HeadersFrame(new byte[] { 0x88 },       // :status: 200
                (endHeaders ? FrameFlags.EndHeaders : FrameFlags.None) | (endStream ? FrameFlags.EndStream : FrameFlags.None),
                0, 0, 0, streamId);

        private static Frame MakeSimpleContinuationFrame(int streamId, bool endHeaders = false)
        {
            Memory<byte> headerBlock = new byte[Frame.MaxFrameLength];
            int bytesGenerated = HPackEncoder.EncodeHeader("foo", "bar", HPackFlags.None, headerBlock.Span);

            return new ContinuationFrame(headerBlock.Slice(0, bytesGenerated),
                (endHeaders ? FrameFlags.EndHeaders : FrameFlags.None),
                0, 0, 0, streamId);
        }

        private static Frame MakeSimpleDataFrame(int streamId, bool endStream = false) =>
            new DataFrame(new byte[] { 0x00 },
                (endStream ? FrameFlags.EndStream : FrameFlags.None),
                0, streamId);

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_ContinuationBeforeHeaders_ConnectionError()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                await connection.WriteFrameAsync(MakeSimpleContinuationFrame(streamId));

                // As this is a connection level error, the client should see the request fail.
                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.PROTOCOL_ERROR);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await connection.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_DataBeforeHeaders_ConnectionError()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                await connection.WriteFrameAsync(MakeSimpleDataFrame(streamId));

                // As this is a connection level error, the client should see the request fail.
                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.PROTOCOL_ERROR);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await connection.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_HeadersAfterHeadersWithoutEndHeaders_ConnectionError()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                await connection.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));
                await connection.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));

                // As this is a connection level error, the client should see the request fail.
                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.PROTOCOL_ERROR);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await connection.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_HeadersAfterHeadersAndContinuationWithoutEndHeaders_ConnectionError()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                await connection.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));
                await connection.WriteFrameAsync(MakeSimpleContinuationFrame(streamId, endHeaders: false));
                await connection.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));

                // As this is a connection level error, the client should see the request fail.
                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.PROTOCOL_ERROR);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await connection.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_DataAfterHeadersWithoutEndHeaders_ConnectionError()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                await connection.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));
                await connection.WriteFrameAsync(MakeSimpleDataFrame(streamId));

                // As this is a connection level error, the client should see the request fail.
                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.PROTOCOL_ERROR);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await connection.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task ResponseStreamFrames_DataAfterHeadersAndContinuationWithoutEndHeaders_ConnectionError()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                await connection.WriteFrameAsync(MakeSimpleHeadersFrame(streamId, endHeaders: false));
                await connection.WriteFrameAsync(MakeSimpleContinuationFrame(streamId, endHeaders: false));
                await connection.WriteFrameAsync(MakeSimpleDataFrame(streamId));

                // As this is a connection level error, the client should see the request fail.
                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.PROTOCOL_ERROR);

                // The client should close the connection as this is a fatal connection level error.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        // This test is based on RFC 7540 section 6.8:
        // "An endpoint MUST treat a GOAWAY frame with a stream identifier other than 0x0 as a
        // connection error (Section 5.4.1) of type PROTOCOL_ERROR."
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_NonzeroStream_ConnectionError()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                await connection.ReadRequestHeaderAsync();

                // Send a GoAway frame on stream 1.
                GoAwayFrame invalidFrame = new GoAwayFrame(0, (int)ProtocolErrors.ENHANCE_YOUR_CALM, new byte[0], 1);
                await connection.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.PROTOCOL_ERROR);

                // The client should close the connection as this is a fatal connection level error.
                Assert.Null(await connection.ReadFrameAsync(TimeSpan.FromSeconds(30)));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_NewRequest_NewConnection()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                server.AllowMultipleConnections = true;

                Task<HttpResponseMessage> sendTask1 = client.GetAsync(server.Address);
                Http2LoopbackConnection connection1 = await server.EstablishConnectionAsync();
                int streamId1 = await connection1.ReadRequestHeaderAsync();

                await connection1.SendGoAway(streamId1);

                await connection1.SendDefaultResponseAsync(streamId1);
                HttpResponseMessage response1 = await sendTask1;
                Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

                // New connection should be established after GOAWAY
                Task<HttpResponseMessage> sendTask2 = client.GetAsync(server.Address);
                Http2LoopbackConnection connection2 = await server.EstablishConnectionAsync();
                int streamId2 = await connection2.ReadRequestHeaderAsync();
                await connection2.SendDefaultResponseAsync(streamId2);
                HttpResponseMessage response2 = await sendTask2;
                Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

                await connection1.WaitForConnectionShutdownAsync();
                await connection2.WaitForConnectionShutdownAsync();
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_ServerDisconnect_AbortStreams()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                await connection.SendGoAway(streamId);

                // wait until GOAWAY received
                await connection.PingPong();

                // Shutdown the connection unexpectedly
                await connection.WaitForConnectionShutdownAsync();

                // Expect client to detect that server has disconnected and throw an exception
                await Assert.ThrowsAnyAsync<HttpRequestException>(() =>
                    new Task[]
                    {
                        sendTask
                    }.WhenAllOrAnyFailed(TestHelper.PassingTestTimeoutMilliseconds));
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_RequestWithBody_ServerDisconnect_AbortStreamsAndThrowIOException()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                request.Version = new Version(2, 0);
                var content = new string('*', 300);
                var stream = new CustomContent.SlowTestStream(Encoding.UTF8.GetBytes(content), null, count: 60);
                request.Content = new CustomContent(stream);

                Task<HttpResponseMessage> sendTask = client.SendAsync(request);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                await connection.SendDefaultResponseHeadersAsync(streamId);

                await connection.SendGoAway(0, errorCode: ProtocolErrors.PROTOCOL_ERROR);

                // Expect client to detect that server has disconnected and throw an exception
                var exception = await Assert.ThrowsAnyAsync<HttpRequestException>(() =>
                    new Task[]
                    {
                        sendTask
                    }.WhenAllOrAnyFailed(TestHelper.PassingTestTimeoutMilliseconds));

                Assert.IsType<IOException>(exception.InnerException);
                Assert.NotNull(exception.InnerException.InnerException);
                Assert.Contains("PROTOCOL_ERROR", exception.InnerException.InnerException.Message);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_UnprocessedStreamFirstRequestFinishedFirst_RequestRestarted()
        {
            // This test case is similar to GoAwayFrame_UnprocessedStreamFirstRequestWaitsUntilSecondFinishes_RequestRestarted
            // but is easier: we close first connection before we expect retry to happen

            using (await Watchdog.CreateAsync())
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                server.AllowMultipleConnections = true;

                Task<HttpResponseMessage> sendTask1 = client.GetAsync(server.Address);
                Http2LoopbackConnection connection1 = await server.EstablishConnectionAsync();
                int streamId1 = await connection1.ReadRequestHeaderAsync();

                Task<HttpResponseMessage> sendTask2 = client.GetAsync(server.Address);
                int streamId2 = await connection1.ReadRequestHeaderAsync();

                // Client should try to restart on new connection when GOAWAY is sent
                Task<Http2LoopbackConnection> waitForSecondConnection = server.EstablishConnectionAsync();

                await connection1.SendGoAway(streamId1);

                // Wait until GOAWAY received
                await connection1.PingPong();

                // Complete first request
                await connection1.SendDefaultResponseAsync(streamId1);
                HttpResponseMessage response1 = await sendTask1;
                Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                await connection1.WaitForConnectionShutdownAsync();

                // Client should retry connection
                Http2LoopbackConnection connection2 = await waitForSecondConnection;

                // Complete second request
                int streamId3 = await connection2.ReadRequestHeaderAsync();
                await connection2.SendDefaultResponseAsync(streamId3);
                HttpResponseMessage response2 = await sendTask2;
                Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
                await connection2.WaitForConnectionShutdownAsync();
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_UnprocessedStreamFirstRequestWaitsUntilSecondFinishes_RequestRestarted()
        {
            using (await Watchdog.CreateAsync())
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                server.AllowMultipleConnections = true;

                Task<HttpResponseMessage> sendTask1 = client.GetAsync(server.Address);
                Http2LoopbackConnection connection1 = await server.EstablishConnectionAsync();
                int streamId1 = await connection1.ReadRequestHeaderAsync();

                Task<HttpResponseMessage> sendTask2 = client.GetAsync(server.Address);
                int streamId2 = await connection1.ReadRequestHeaderAsync();

                // Client should try to restart on new connection when GOAWAY is sent
                Task<Http2LoopbackConnection> waitForSecondConnection = server.EstablishConnectionAsync();

                await connection1.SendGoAway(streamId1);

                // Wait until GOAWAY received
                await connection1.PingPong();

                // Client should retry connection
                Http2LoopbackConnection connection2 = await waitForSecondConnection;

                // Complete second request
                int streamId3 = await connection2.ReadRequestHeaderAsync();
                await connection2.SendDefaultResponseAsync(streamId3);
                HttpResponseMessage response2 = await sendTask2;
                Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
                await connection2.WaitForConnectionShutdownAsync();

                // Complete first request
                await connection1.SendDefaultResponseAsync(streamId1);
                HttpResponseMessage response1 = await sendTask1;
                Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                await connection1.WaitForConnectionShutdownAsync();
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_RequestInFlight_Finished()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        HttpResponseMessage response = await client.GetAsync(uri);
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        byte[] responseBody = await response.Content.ReadAsByteArrayAsync();

                        Assert.Equal(
                            new byte[20]
                            {
                                5, 4, 3, 2, 1,
                                11, 10, 9, 8, 7, 6,
                                15, 14, 13, 12,
                                20, 19, 18, 17, 16,
                            },
                            responseBody);
                    }
                },
                async server =>
                {
                    Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                    int streamId = await connection.ReadRequestHeaderAsync();

                    await connection.SendDefaultResponseHeadersAsync(streamId);

                    await connection.SendResponseBodyAsync(streamId, new byte[5] { 5, 4, 3, 2, 1 }, isFinal: false);
                    await connection.SendResponseBodyAsync(streamId, new byte[6] { 11, 10, 9, 8, 7, 6 }, isFinal: false);

                    // Signal that our request is the last thing which will be processed
                    await connection.SendGoAway(streamId);

                    // Make sure client received GOAWAY
                    await connection.PingPong();

                    await connection.SendResponseBodyAsync(streamId, new byte[4] { 15, 14, 13, 12 }, isFinal: false);
                    await connection.SendResponseBodyAsync(streamId, new byte[5] { 20, 19, 18, 17, 16 }, isFinal: true);

                    await connection.WaitForConnectionShutdownAsync();
                });
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task DataFrame_TooLong_ConnectionError()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                await connection.ReadRequestHeaderAsync();

                // Send a malformed frame.
                DataFrame invalidFrame = new DataFrame(new byte[Frame.MaxFrameLength + 1], FrameFlags.None, 0, 0);
                await connection.WriteFrameAsync(invalidFrame);

                // As this is a connection level error, the client should see the request fail.
                await AssertProtocolErrorAsync(sendTask, ProtocolErrors.FRAME_SIZE_ERROR);
            }
        }

        [ConditionalTheory(nameof(SupportsAlpn))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CompletedResponse_FrameReceived_Ignored(bool sendDataFrame)
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                connection.IgnoreWindowUpdates();
                int streamId = await connection.ReadRequestHeaderAsync();

                // Send response and end stream.
                await connection.SendDefaultResponseHeadersAsync(streamId);
                DataFrame dataFrame = new DataFrame(new byte[10], FrameFlags.EndStream, 0, streamId);
                await connection.WriteFrameAsync(dataFrame);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Send a frame on the now-closed stream.
                Frame invalidFrame = ConstructInvalidFrameForClosedStream(streamId, sendDataFrame);
                await connection.WriteFrameAsync(invalidFrame);

                // Pingpong to ensure the frame is processed and ignored
                await connection.PingPong();

                await ValidateConnection(client, server.Address, connection);
            }
        }

        [ConditionalTheory(nameof(SupportsAlpn))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EmptyResponse_FrameReceived_Ignored(bool sendDataFrame)
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                // Send empty response.
                await connection.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Send a frame on the now-closed stream.
                Frame invalidFrame = ConstructInvalidFrameForClosedStream(streamId, sendDataFrame);
                await connection.WriteFrameAsync(invalidFrame);

                // Pingpong to ensure the frame is processed and ignored
                await connection.PingPong();

                await ValidateConnection(client, server.Address, connection);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task CompletedResponse_WindowUpdateFrameReceived_Success()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();

                // Send empty response.
                await connection.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Send a frame on the now-closed stream.
                WindowUpdateFrame invalidFrame = new WindowUpdateFrame(1, streamId);
                await connection.WriteFrameAsync(invalidFrame);

                // The client should close the connection.
                await connection.WaitForConnectionShutdownAsync();
            }
        }

        private static Frame ConstructInvalidFrameForClosedStream(int streamId, bool dataFrame)
        {
            if (dataFrame)
            {
                return new DataFrame(new byte[10], FrameFlags.None, 0, streamId);
            }
            else
            {
                byte[] headers = new byte[] { 0x88 };   // Encoding for ":status: 200"
                return new HeadersFrame(headers, FrameFlags.EndHeaders, 0, 0, 0, streamId);
            }
        }

        // Validate that connection is still usable, by sending a request and receiving a response
        private static async Task<int> ValidateConnection(HttpClient client, Uri serverAddress, Http2LoopbackConnection connection)
        {
            Task<HttpResponseMessage> sendTask = client.GetAsync(serverAddress);

            int streamId = await connection.ReadRequestHeaderAsync();
            await connection.SendDefaultResponseAsync(streamId);

            HttpResponseMessage response = await sendTask;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return streamId;
        }

        public static IEnumerable<object[]> ValidAndInvalidProtocolErrors() =>
            Enum.GetValues(typeof(ProtocolErrors))
            .Cast<ProtocolErrors>()
            .Where(e => e != ProtocolErrors.REFUSED_STREAM)     // REFUSED_STREAM allows retry instead of abort, so skip it
            .Concat(new[] { (ProtocolErrors)12345 })
            .Select(p => new object[] { p });

        public static IEnumerable<object[]> ValidAndInvalidProtocolErrorsAndBool()
        {
            foreach (object[] args in ValidAndInvalidProtocolErrors())
            {
                yield return args.Append(true).ToArray();
                yield return args.Append(false).ToArray();
            }
        }

        [ConditionalTheory(nameof(SupportsAlpn))]
        [MemberData(nameof(ValidAndInvalidProtocolErrorsAndBool))]
        public async Task ResetResponseStream_FrameReceived_Ignored(ProtocolErrors error, bool dataFrame)
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.SendDefaultResponseHeadersAsync(streamId);

                // Send a reset stream frame so that stream 1 moves to a terminal state.
                RstStreamFrame resetStream = new RstStreamFrame(FrameFlags.None, (int)error, streamId);
                await connection.WriteFrameAsync(resetStream);

                await AssertProtocolErrorAsync(sendTask, error);

                // Send a frame on the now-closed stream.
                Frame invalidFrame = ConstructInvalidFrameForClosedStream(streamId, dataFrame);
                await connection.WriteFrameAsync(invalidFrame);

                // Pingpong to ensure the frame is processed and ignored
                await connection.PingPong();

                await ValidateConnection(client, server.Address, connection);
            }
        }

        private static async Task<(int, Http2LoopbackConnection)> EstablishConnectionAndProcessOneRequestAsync(HttpClient client, Http2LoopbackServer server)
        {
            int streamId = -1;

            // Establish connection and send initial request/response to ensure connection is available for subsequent use
            Http2LoopbackConnection connection = null;

            await new[]
            {
                Task.Run(async () =>
                {
                    using (HttpResponseMessage response = await client.GetAsync(server.Address))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
                    }
                }),
                Task.Run(async () =>
                {
                    connection = await server.EstablishConnectionAsync();
                    streamId = await connection.ReadRequestHeaderAsync();
                    await connection.SendDefaultResponseAsync(streamId);
                })
            }.WhenAllOrAnyFailed(TestHelper.PassingTestTimeoutMilliseconds);

            return (streamId, connection);
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_NoPendingStreams_ConnectionClosed()
        {
            using (await Watchdog.CreateAsync())
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                (int streamId, Http2LoopbackConnection connection) = await EstablishConnectionAndProcessOneRequestAsync(client, server);

                // Send GOAWAY.
                GoAwayFrame goAwayFrame = new GoAwayFrame(streamId, 0, new byte[0], 0);
                await connection.WriteFrameAsync(goAwayFrame);

                // The client should close the connection.
                await connection.WaitForConnectionShutdownAsync();

                // New request should cause a new connection
                await EstablishConnectionAndProcessOneRequestAsync(client, server);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_AllPendingStreamsValid_RequestsSucceedAndConnectionClosed()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                (_, Http2LoopbackConnection connection) = await EstablishConnectionAndProcessOneRequestAsync(client, server);

                // Issue three requests
                Task<HttpResponseMessage> sendTask1 = client.GetAsync(server.Address);
                Task<HttpResponseMessage> sendTask2 = client.GetAsync(server.Address);
                Task<HttpResponseMessage> sendTask3 = client.GetAsync(server.Address);

                // Receive three requests
                int streamId1 = await connection.ReadRequestHeaderAsync();
                int streamId2 = await connection.ReadRequestHeaderAsync();
                int streamId3 = await connection.ReadRequestHeaderAsync();

                Assert.True(streamId1 < streamId2);
                Assert.True(streamId2 < streamId3);

                // Send various partial responses

                // First response: Don't send anything yet

                // Second response: Send headers, no body yet
                await connection.SendDefaultResponseHeadersAsync(streamId2);

                // Third response: Send headers, partial body
                await connection.SendDefaultResponseHeadersAsync(streamId3);
                await connection.SendResponseDataAsync(streamId3, new byte[5], endStream: false);

                // Send a GOAWAY frame that indicates that we will process all three streams
                GoAwayFrame goAwayFrame = new GoAwayFrame(streamId3, 0, new byte[0], 0);
                await connection.WriteFrameAsync(goAwayFrame);

                // Finish sending responses
                await connection.SendDefaultResponseHeadersAsync(streamId1);
                await connection.SendResponseDataAsync(streamId1, new byte[10], endStream: true);
                await connection.SendResponseDataAsync(streamId2, new byte[10], endStream: true);
                await connection.SendResponseDataAsync(streamId3, new byte[5], endStream: true);

                // We will not send any more frames, so send EOF now, and ensure the client handles this properly.
                connection.ShutdownSend();

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
                await connection.WaitForConnectionShutdownAsync();

                // New request should cause a new connection
                await EstablishConnectionAndProcessOneRequestAsync(client, server);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task GoAwayFrame_AbortAllPendingStreams_StreamFailWithExpectedException()
        {
            using (await Watchdog.CreateAsync())
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                client.BaseAddress = server.Address;
                server.AllowMultipleConnections = true;

                (_, Http2LoopbackConnection connection) = await EstablishConnectionAndProcessOneRequestAsync(client, server);

                // Issue three requests, we want to make sure the specific task is related with specific stream
                Task<HttpResponseMessage> sendTask1 = client.GetAsync("request1");
                int streamId1 = await connection.ReadRequestHeaderAsync();

                Task<HttpResponseMessage> sendTask2 = client.GetAsync("request2");
                int streamId2 = await connection.ReadRequestHeaderAsync();

                Task<HttpResponseMessage> sendTask3 = client.GetAsync("request3");
                int streamId3 = await connection.ReadRequestHeaderAsync();

                Assert.InRange(streamId1, 1, streamId2 - 1);
                Assert.InRange(streamId2, streamId1 + 1, streamId3 - 1);
                Assert.InRange(streamId3, streamId2 + 1, Int32.MaxValue);

                // Send various partial responses

                // First response: Don't send anything yet, request should be retried on the new connection

                // Second response: Send headers, no body yet - request should fail
                await connection.SendDefaultResponseHeadersAsync(streamId2);

                // Third response: Send headers, partial body - request should fail
                await connection.SendDefaultResponseHeadersAsync(streamId3);
                await connection.SendResponseDataAsync(streamId3, new byte[5], endStream: false);

                // Ensure all sent frames are received by client
                await connection.PingPong();

                // Send a GOAWAY frame that indicates that we have not processed any of the requests
                await connection.SendGoAway(0, ProtocolErrors.ENHANCE_YOUR_CALM);

                Http2LoopbackConnection newConnection = await server.EstablishConnectionAsync();

                HeadersFrame retriedFrame = await newConnection.ReadRequestHeaderFrameAsync();
                int retriedStreamId = retriedFrame.StreamId;
                Assert.InRange(retriedStreamId, 1, Int32.MaxValue);
                string headerData = Encoding.UTF8.GetString(retriedFrame.Data.Span);

                await newConnection.SendDefaultResponseHeadersAsync(retriedStreamId);
                await newConnection.SendResponseDataAsync(retriedStreamId, new byte[3], endStream: true);

                Assert.Contains("request1", headerData);

                HttpResponseMessage response1 = await sendTask1;
                Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
                await newConnection.ShutdownIgnoringErrorsAsync(retriedStreamId, ProtocolErrors.ENHANCE_YOUR_CALM);

                await AssertProtocolErrorAsync(sendTask2, ProtocolErrors.ENHANCE_YOUR_CALM);
                await AssertProtocolErrorAsync(sendTask3, ProtocolErrors.ENHANCE_YOUR_CALM);

                await connection.WaitForConnectionShutdownAsync();
            }
        }

        private static async Task ReadExactDataSizeFromStream(Http2LoopbackConnection connection, int size, int streamId)
        {
            int bytesReceived = 0;
            while (bytesReceived < size)
            {
                Frame frame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(streamId, frame.StreamId);
                Assert.Equal(FrameType.Data, frame.Type);
                Assert.Equal(FrameFlags.None, frame.Flags);
                Assert.True(frame.Length > 0);

                bytesReceived += frame.Length;
            }

            Assert.Equal(size, bytesReceived);
        }

        private static async Task<int> ReadToEndOfStream(Http2LoopbackConnection connection, int streamId)
        {
            int bytesReceived = 0;
            while (true)
            {
                Frame frame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));

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
            const int ContentSize = 100_000;

            var content = new ByteAtATimeContent(ContentSize);

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> clientTask = client.PostAsync(server.Address, content);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                Frame frame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                int streamId = frame.StreamId;
                Assert.Equal(FrameType.Headers, frame.Type);
                Assert.Equal(FrameFlags.EndHeaders, frame.Flags);

                // Receive up to initial window size
                int bytesReceived = DefaultInitialWindowSize;
                await ReadExactDataSizeFromStream(connection, bytesReceived, streamId);

                // Issue another read. It shouldn't complete yet. Wait a brief period of time to ensure it doesn't complete.
                Task<Frame> readFrameTask = connection.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase connection window by one. This should still not complete the read.
                await connection.WriteFrameAsync(new WindowUpdateFrame(1, 0));

                await Task.Delay(500);

                Assert.False(readFrameTask.IsCompleted);

                // Increase stream window by two. This should complete the read with a single byte.
                await connection.WriteFrameAsync(new WindowUpdateFrame(2, streamId));

                frame = await readFrameTask;
                Assert.Equal(1, frame.Length);
                bytesReceived++;

                // Issue another read and ensure it doesn't complete yet.
                readFrameTask = connection.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase connection window by two. This should complete the read with a single byte.
                await connection.WriteFrameAsync(new WindowUpdateFrame(2, 0));

                frame = await readFrameTask;
                Assert.Equal(1, frame.Length);
                bytesReceived++;

                // Issue another read and ensure it doesn't complete yet.
                readFrameTask = connection.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase connection window to allow exactly the remaining request size. This should still not complete the read.
                await connection.WriteFrameAsync(new WindowUpdateFrame(ContentSize - bytesReceived - 1, 0));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase stream window to allow exactly the remaining request size. This should allow the rest of the request to be sent.
                await connection.WriteFrameAsync(new WindowUpdateFrame(ContentSize - bytesReceived, streamId));

                frame = await readFrameTask;
                Assert.Equal(streamId, frame.StreamId);
                Assert.Equal(FrameType.Data, frame.Type);
                Assert.Equal(FrameFlags.None, frame.Flags);
                Assert.True(frame.Length > 0);

                bytesReceived += frame.Length;

                // Read to end of stream
                bytesReceived += await ReadToEndOfStream(connection, streamId);

                Assert.Equal(ContentSize, bytesReceived);

                await connection.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await clientTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_InitialWindowSize_ClientDoesNotExceedWindows()
        {
            const int ContentSize = DefaultInitialWindowSize + 1000;

            var content = new ByteAtATimeContent(ContentSize);

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> clientTask = client.PostAsync(server.Address, content);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                // Bump connection window so it won't block the client.
                await connection.WriteFrameAsync(new WindowUpdateFrame(ContentSize - DefaultInitialWindowSize, 0));

                Frame frame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                int streamId = frame.StreamId;
                Assert.Equal(FrameType.Headers, frame.Type);
                Assert.Equal(FrameFlags.EndHeaders, frame.Flags);

                int bytesReceived = DefaultInitialWindowSize;
                await ReadExactDataSizeFromStream(connection, bytesReceived, streamId);

                // Issue another read. It shouldn't complete yet. Wait a brief period of time to ensure it doesn't complete.
                Task<Frame> readFrameTask = connection.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Change SETTINGS_INITIAL_WINDOW_SIZE to 0. This will make the client's credit go negative.
                await connection.ExpectSettingsAckAsync();
                await connection.WriteFrameAsync(new SettingsFrame(new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = 0 }));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase stream window by one. Client credit will still be negative.
                await connection.WriteFrameAsync(new WindowUpdateFrame(1, streamId));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Change SETTINGS_INITIAL_WINDOW_SIZE to 1. Client credit will still be negative.
                await connection.ExpectSettingsAckAsync();
                await connection.WriteFrameAsync(new SettingsFrame(new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = 1 }));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase stream window so client credit will be 0.
                await connection.WriteFrameAsync(new WindowUpdateFrame(DefaultInitialWindowSize - 2, streamId));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase stream window by one, so client can now send a single byte.
                await connection.WriteFrameAsync(new WindowUpdateFrame(1, streamId));

                frame = await readFrameTask;
                Assert.Equal(FrameType.Data, frame.Type);
                Assert.Equal(1, frame.Length);
                bytesReceived++;

                // Issue another read and ensure it doesn't complete yet.
                readFrameTask = connection.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase SETTINGS_INITIAL_WINDOW_SIZE to 2, so client can now send a single byte.
                await connection.ExpectSettingsAckAsync();
                await connection.WriteFrameAsync(new SettingsFrame(new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = 2 }));

                frame = await readFrameTask;
                Assert.Equal(FrameType.Data, frame.Type);
                Assert.Equal(1, frame.Length);
                bytesReceived++;

                // Issue another read and ensure it doesn't complete yet.
                readFrameTask = connection.ReadFrameAsync(TimeSpan.FromSeconds(30));

                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Increase SETTINGS_INITIAL_WINDOW_SIZE to be enough that the client can send the rest of the content.
                await connection.ExpectSettingsAckAsync();
                await connection.WriteFrameAsync(new SettingsFrame(new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = ContentSize - (DefaultInitialWindowSize - 1) }));

                frame = await readFrameTask;
                Assert.Equal(streamId, frame.StreamId);
                Assert.Equal(FrameType.Data, frame.Type);
                Assert.Equal(FrameFlags.None, frame.Flags);
                Assert.True(frame.Length > 0);

                bytesReceived += frame.Length;

                // Read to end of stream
                bytesReceived += await ReadToEndOfStream(connection, streamId);

                Assert.Equal(ContentSize, bytesReceived);

                await connection.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await clientTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        // Flush behavior is heuristic-based and may change in the future.
        // Try various content sizes here to ensure we are not simply getting lucky with the flush heuristic.
        [ConditionalTheory(nameof(SupportsAlpn))]
        [InlineData(DefaultInitialWindowSize + 1)]
        [InlineData(DefaultInitialWindowSize + 4 * 1024)]
        [InlineData(DefaultInitialWindowSize + 8 * 1024)]
        [InlineData(DefaultInitialWindowSize + 16 * 1024)]
        [InlineData(DefaultInitialWindowSize + 32 * 1024)]
        [InlineData(DefaultInitialWindowSize + 64 * 1024)]
        [InlineData(DefaultInitialWindowSize + 96 * 1024)]
        public async Task Http2_SendOverStreamWindowSizeWithoutExplicitFlush_ClientSendsUpToFullWindowSize(int contentSize)
        {
            var content = new ByteArrayContent(new byte[contentSize]);

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> clientTask = client.PostAsync(server.Address, content);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                // Bump connection window so it won't block the client.
                await connection.WriteFrameAsync(new WindowUpdateFrame(contentSize - DefaultInitialWindowSize, 0));

                Frame frame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                int streamId = frame.StreamId;
                Assert.Equal(FrameType.Headers, frame.Type);
                Assert.Equal(FrameFlags.EndHeaders, frame.Flags);

                // Bump stream window so the client should send the entire body minus one byte.
                if ((contentSize - 1) > DefaultInitialWindowSize)
                {
                    await connection.WriteFrameAsync(new WindowUpdateFrame((contentSize - 1) - DefaultInitialWindowSize, streamId));
                }

                int bytesReceived = contentSize - 1;
                await ReadExactDataSizeFromStream(connection, bytesReceived, streamId);

                // Bump stream window so the client should send the final byte.
                await connection.WriteFrameAsync(new WindowUpdateFrame(1, streamId));

                // Read to end of stream
                bytesReceived += await ReadToEndOfStream(connection, streamId);

                Assert.Equal(contentSize, bytesReceived);

                await connection.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await clientTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        // Flush behavior is heuristic-based and may change in the future.
        // Try various content sizes here to ensure we are not simply getting lucky with the flush heuristic.
        [ConditionalTheory(nameof(SupportsAlpn))]
        [InlineData(DefaultInitialWindowSize + 1)]
        [InlineData(DefaultInitialWindowSize + 4 * 1024)]
        [InlineData(DefaultInitialWindowSize + 8 * 1024)]
        [InlineData(DefaultInitialWindowSize + 16 * 1024)]
        [InlineData(DefaultInitialWindowSize + 32 * 1024)]
        [InlineData(DefaultInitialWindowSize + 64 * 1024)]
        [InlineData(DefaultInitialWindowSize + 96 * 1024)]
        public async Task Http2_SendOverConnectionWindowSizeWithoutExplicitFlush_ClientSendsUpToFullWindowSize(int contentSize)
        {
            var content = new ByteArrayContent(new byte[contentSize]);

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> clientTask = client.PostAsync(server.Address, content);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                // Bump connection window so the client should send the entire body minus one byte.
                if ((contentSize - 1) > DefaultInitialWindowSize)
                {
                    await connection.WriteFrameAsync(new WindowUpdateFrame((contentSize - 1) - DefaultInitialWindowSize, 0));
                }

                Frame frame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                int streamId = frame.StreamId;
                Assert.Equal(FrameType.Headers, frame.Type);
                Assert.Equal(FrameFlags.EndHeaders, frame.Flags);

                // Bump stream window so it won't block the client.
                await connection.WriteFrameAsync(new WindowUpdateFrame(contentSize - DefaultInitialWindowSize, streamId));

                // Receive up to window size (i.e. contentSize - 1)
                int bytesReceived = contentSize - 1;
                await ReadExactDataSizeFromStream(connection, bytesReceived, streamId);

                Assert.Equal((contentSize - 1), bytesReceived);

                // Bump connection window so the client should send the final byte.
                await connection.WriteFrameAsync(new WindowUpdateFrame(1, 0));

                // Read to end of stream
                bytesReceived += await ReadToEndOfStream(connection, streamId);

                Assert.Equal(contentSize, bytesReceived);

                await connection.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await clientTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_MaxConcurrentStreams_LimitEnforced()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                connection.IgnoreWindowUpdates();

                // Process first request and send response.
                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Change MaxConcurrentStreams setting and wait for ack.
                // (We don't want to send any new requests until we receive the ack, otherwise we may have a timing issue.)
                SettingsFrame settingsFrame = new SettingsFrame(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 0 });
                await connection.WriteFrameAsync(settingsFrame);
                Frame settingsAckFrame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.Settings, settingsAckFrame.Type);
                Assert.Equal(FrameFlags.Ack, settingsAckFrame.Flags);

                // Issue two more requests. We shouldn't send either of them.
                sendTask = client.GetAsync(server.Address);
                Task<HttpResponseMessage> sendTask2 = client.GetAsync(server.Address);

                // Issue another read. It shouldn't complete yet. Wait a brief period of time to ensure it doesn't complete.
                Task<Frame> readFrameTask = connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Change MaxConcurrentStreams again to allow a single request to come through.
                await connection.ExpectSettingsAckAsync();
                settingsFrame = new SettingsFrame(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 1 });
                await connection.WriteFrameAsync(settingsFrame);

                // First request should be sent
                Frame frame = await readFrameTask;
                Assert.Equal(FrameType.Headers, frame.Type);
                streamId = frame.StreamId;

                // Issue another read. Second request should not be sent yet.
                readFrameTask = connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                await Task.Delay(500);
                Assert.False(readFrameTask.IsCompleted);

                // Send response for first request
                await connection.SendDefaultResponseAsync(streamId);
                response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Second request should be sent now
                frame = await readFrameTask;
                Assert.Equal(FrameType.Headers, frame.Type);
                streamId = frame.StreamId;

                // Send response for second request
                await connection.SendDefaultResponseAsync(streamId);
                response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_WaitingForStream_Cancellation()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                connection.IgnoreWindowUpdates();

                // Process first request and send response.
                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.SendDefaultResponseAsync(streamId);

                HttpResponseMessage response = await sendTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Change MaxConcurrentStreams setting and wait for ack.
                // (We don't want to send any new requests until we receive the ack, otherwise we may have a timing issue.)
                SettingsFrame settingsFrame = new SettingsFrame(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 0 });
                await connection.WriteFrameAsync(settingsFrame);
                Frame settingsAckFrame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
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
            const int ContentSize = DefaultInitialWindowSize + 1;

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            var content = new ByteAtATimeContent(ContentSize);

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient(handler))
            {
                var cts = new CancellationTokenSource();
                Task<HttpResponseMessage> clientTask = client.PostAsync(server.Address, content, cts.Token);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                Frame frame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                int streamId = frame.StreamId;
                Assert.Equal(FrameType.Headers, frame.Type);
                Assert.Equal(FrameFlags.EndHeaders, frame.Flags);

                // Receive up to initial window size
                int bytesReceived = DefaultInitialWindowSize;
                await ReadExactDataSizeFromStream(connection, bytesReceived, streamId);

                // The client is waiting for more credit in order to send the last byte of the
                // request body. Test cancellation at this point.
                Stopwatch stopwatch = Stopwatch.StartNew();

                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await clientTask);

                // Ensure that the cancellation occurs promptly
                stopwatch.Stop();
                Assert.True(stopwatch.ElapsedMilliseconds < 30000);

                // The server should receive a RstStream frame.
                frame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                Assert.Equal(FrameType.RstStream, frame.Type);
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_PendingSend_Cancellation()
        {
            // The goal of this test is to get the client into the state where it is sending content,
            // but the send pends because the TCP window is full.
            const int ContentSize = DefaultInitialWindowSize * 2; // Double the default TCP window size.

            var content = new ByteArrayContent(TestHelper.GenerateRandomContent(ContentSize));

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                var cts = new CancellationTokenSource();

                Task<HttpResponseMessage> clientTask = client.PostAsync(server.Address, content, cts.Token);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                Frame frame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
                int streamId = frame.StreamId;
                Assert.Equal(FrameType.Headers, frame.Type);
                Assert.Equal(FrameFlags.EndHeaders, frame.Flags);

                // Increase the size of the HTTP/2 Window, so that it is large enough to fill the
                // TCP window when we do not perform any reads on the server side.
                await connection.WriteFrameAsync(new WindowUpdateFrame(DefaultInitialWindowSize, streamId));

                // Give the client time to read the window update frame, and for the write to pend.
                await Task.Delay(1000);
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await clientTask);
            }
        }

        [ConditionalTheory(nameof(SupportsAlpn))]
        [OuterLoop("Uses Task.Delay")]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Http2_PendingSend_SendsReset(bool waitForData)
        {
            var cts = new CancellationTokenSource();
            var rstReceived = new TaskCompletionSource<bool>();

            string content = new string('*', 300);
            var stream = new CustomContent.SlowTestStream(Encoding.UTF8.GetBytes(content), null, count: 60);
            await Http2LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Version = new Version(2,0);
                    request.Content = new CustomContent(stream);

                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await client.SendAsync(request, cts.Token));

                    // Wait until the RST_STREAM for the previous request is received before the next request starts.
                    await rstReceived.Task.WaitAsync(TimeSpan.FromSeconds(60));

                    // Send another request to verify that connection is still functional.
                    request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = new Version(2,0);

                    await client.SendAsync(request);
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody : false);
                int frameCount = 0;
                Frame frame;
                do
                {
                    if (frameCount == (waitForData ? 1 : 0)) {
                        // Cancel client after receiving Headers or part of request body.
                        cts.Cancel();
                    }
                    frame = await connection.ReadFrameAsync(TestHelper.PassingTestTimeout);
                    Assert.NotNull(frame); // We should get Rst before closing connection.
                    Assert.Equal(0, (int)(frame.Flags & FrameFlags.EndStream));
                    frameCount++;
                 } while (frame.Type != FrameType.RstStream);

                Assert.Equal(1, frame.StreamId);

                rstReceived.SetResult(true);

                frame = null;
                (streamId, requestData) = await connection.ReadAndParseRequestHeaderAsync();
                await connection.SendResponseHeadersAsync(streamId, endStream: false, HttpStatusCode.OK);
                await connection.SendResponseBodyAsync(streamId, Encoding.ASCII.GetBytes($"Http2_PendingSend_SendsReset(waitForData: {waitForData})"), isFinal: false);
                // Wait for any lingering frames or extra reset frames.
                try
                {
                    frame = await connection.ReadFrameAsync(TimeSpan.FromMilliseconds(1000));
                }
                catch (System.OperationCanceledException) { };
                Assert.Null(frame);    // Make sure we do not get any frames after getting Rst.
                await connection.SendResponseBodyAsync(streamId, Encoding.ASCII.GetBytes("final"), isFinal: true);
                await connection.WaitForConnectionShutdownAsync();
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Http2_PendingReceive_SendsReset(bool doRead)
        {
            var cts = new CancellationTokenSource();
            var doCancel = new TaskCompletionSource();
            HttpResponseMessage response = null;

            using (HttpClient client = CreateHttpClient())
            {
                await Http2LoopbackServer.CreateClientAndServerAsync(async url =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Version = new Version(2,0);

                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    {
                        byte[] buffer = new byte[100];

                        if (doRead)
                        {
                            // Start reading response as variation.
                            _ = await stream.ReadAsync(buffer, cts.Token);
                        }

                        doCancel.SetResult();
                        _output.WriteLine($"{DateTime.Now} cancellation requested.");

                        // Keep reading response.
                        if (doRead)
                        {
                            try
                            {
                                while (true)
                                {
                                    _ = await stream.ReadAsync(buffer, cts.Token);
                                }
                            }
                            catch (OperationCanceledException) { };
                        }
                    }
                },
                async server =>
                {
                    Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                    (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody : false);
                    _output.WriteLine($"{DateTime.Now} Connection established");

                    await connection.SendResponseHeadersAsync(streamId, endStream: false, HttpStatusCode.OK);
                    // Start streaming response
                    DataFrame dataFrame = new DataFrame(new byte[100], FrameFlags.None, 0, streamId);
                    await connection.WriteFrameAsync(dataFrame);
                    // Wait for client to start processing response and cancel it.
                    await doCancel.Task;
                    cts.Cancel();

                    _output.WriteLine($"{DateTime.Now} HttpRequest was canceled");
                    Frame frame;
                    do
                    {
                        frame = await connection.ReadFrameAsync(TestHelper.PassingTestTimeout);
                        Assert.NotNull(frame); // We should get Rst before closing connection.
                        Assert.Equal(0, (int)(frame.Flags & FrameFlags.EndStream));
                    } while (frame.Type != FrameType.RstStream);
                });
            }
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2Connection_Should_Wrap_HttpContent_InvalidOperationException()
        {
            // test for https://github.com/dotnet/runtime/issues/30187
            var throwingContent = new ThrowingContent(() => new InvalidOperationException());

            var tcs = new TaskCompletionSource();
            await Http2LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Version = new Version(2, 0);
                    request.Content = throwingContent;

                    HttpRequestException exn = await Assert.ThrowsAnyAsync<HttpRequestException>(async () => await client.SendAsync(request));
                    Assert.IsType<InvalidOperationException>(exn.InnerException);
                    await tcs.Task; // prevent disposal of client until server has completed operations
                }
            },
            async server =>
            {
                await server.EstablishConnectionAsync();
                tcs.SetResult();
            });
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2Connection_Should_Not_Wrap_HttpContent_CustomException()
        {
            // Assert existing HttpConnection behaviour in which custom HttpContent exception types are surfaced as-is
            // c.f. https://github.com/dotnet/runtime/issues/30187#issuecomment-510569836

            var throwingContent = new ThrowingContent(() => new CustomException());

            var tcs = new TaskCompletionSource();
            await Http2LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Version = new Version(2, 0);
                    request.Content = throwingContent;

                    await Assert.ThrowsAnyAsync<CustomException>(async () => await client.SendAsync(request));
                    await tcs.Task; // prevent disposal of client until server has completed operations
                }
            },
            async server =>
            {
                await server.EstablishConnectionAsync();
                tcs.SetResult();
            });
        }

        private class CustomException : Exception { }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PostAsyncExpect100Continue_SendRequest_Ok(bool send100Continue)
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Version = new Version(2,0);
                    request.Content = new StringContent(new string('*', 3000));
                    request.Headers.ExpectContinue = true;
                    request.Headers.Add("x-test", $"PostAsyncExpect100Continue_SendRequest_Ok({send100Continue}");

                    HttpResponseMessage response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody : false);
                Assert.Equal("100-continue", requestData.GetSingleHeaderValue("Expect"));

                if (send100Continue)
                {
                    await connection.SendResponseHeadersAsync(streamId, endStream: false, HttpStatusCode.Continue);
                }
                await connection.ReadBodyAsync();
                await connection.SendResponseHeadersAsync(streamId, endStream: false, HttpStatusCode.OK);
                await connection.SendResponseBodyAsync(streamId, Encoding.ASCII.GetBytes("OK"));
                await connection.ShutdownIgnoringErrorsAsync(streamId);
            });
        }

        [Fact]
        public async Task PostAsyncExpect100Continue_NonSuccessResponse_RequestBodyNotSent()
        {
            string responseContent = "no no!";

            await Http2LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (var handler = new SocketsHttpHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
                    // Increase default Expect: 100-continue timeout to ensure that we don't accidentally fire the timer and send the request body.
                    handler.Expect100ContinueTimeout = TimeSpan.FromSeconds(300);

                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Version = new Version(2,0);
                    request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    request.Content = new StringContent(new string('*', 3000));
                    request.Headers.ExpectContinue = true;
                    request.Headers.Add("x-test", "PostAsyncExpect100Continue_NonSuccessResponse_RequestBodyNotSent");

                    HttpResponseMessage response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                    Assert.Equal(responseContent, await response.Content.ReadAsStringAsync());
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody : false);
                Assert.Equal("100-continue", requestData.GetSingleHeaderValue("Expect"));

                // Reject content with 403.
                await connection.SendResponseHeadersAsync(streamId, endStream: false, HttpStatusCode.Forbidden);
                await connection.SendResponseBodyAsync(streamId, Encoding.ASCII.GetBytes(responseContent));

                // Client should send empty request body
                byte[] requestBody = await connection.ReadBodyAsync(expectEndOfStream:true);
                Assert.Null(requestBody);

                await connection.ShutdownIgnoringErrorsAsync(streamId);
            });
        }

        class DuplexContent : HttpContent
        {
            private TaskCompletionSource<Stream> _waitForStream;
            private TaskCompletionSource _waitForCompletion;

            public DuplexContent()
            {
                _waitForStream = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                _waitForCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _waitForStream.SetResult(stream);
                await _waitForCompletion.Task;
            }

            public Task<Stream> WaitForStreamAsync()
            {
                return _waitForStream.Task;
            }

            public void Complete()
            {
                _waitForCompletion.SetResult();
            }

            public void Fail(Exception e)
            {
                _waitForCompletion.SetException(e);
            }
        }

        private async Task SendAndReceiveResponseDataAsync(Memory<byte> data, Stream responseStream, Http2LoopbackConnection connection, int streamId)
        {
            byte[] readBuffer = new byte[data.Length];

            await connection.SendResponseDataAsync(streamId, data, endStream: false);
            int bytesRead = await responseStream.ReadAsync(readBuffer);
            Assert.True(data.Span.SequenceEqual(readBuffer));
        }

        private async Task SendAndReceiveResponseEOFAsync(Stream responseStream, Http2LoopbackConnection connection, int streamId)
        {
            byte[] readBuffer = new byte[1];

            await connection.SendResponseDataAsync(streamId, new byte[] { }, endStream: true);
            int bytesRead = await responseStream.ReadAsync(readBuffer);
            Assert.Equal(0, bytesRead);

            // Another read should still give EOF
            bytesRead = await responseStream.ReadAsync(readBuffer);
            Assert.Equal(0, bytesRead);

            // Dispose the response stream, to ensure that this doesn't affect the request stream
            responseStream.Dispose();

            // Attempting to read now should throw ObjectDisposedException
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => { await responseStream.ReadAsync(readBuffer); });
        }

        private async Task SendAndReceiveRequestDataAsync(Memory<byte> data, Stream requestStream, Http2LoopbackConnection connection, int streamId)
        {
            await requestStream.WriteAsync(data);
            await requestStream.FlushAsync();
            DataFrame dataFrame = (DataFrame)await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
            Assert.True(data.Span.SequenceEqual(dataFrame.Data.Span));
        }

        private async Task SendAndReceiveRequestEOFAsync(DuplexContent duplexContent, Stream requestStream, Http2LoopbackConnection connection, int streamId)
        {
            duplexContent.Complete();
            DataFrame dataFrame = (DataFrame)await connection.ReadFrameAsync(TimeSpan.FromSeconds(30));
            Assert.True(dataFrame.EndStreamFlag);
            Assert.Equal(0, dataFrame.Data.Length);

            // Attempting to write now should throw ObjectDisposedException
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => { await requestStream.WriteAsync(new byte[1]); });
        }

        [Fact]
        public async Task PostAsyncDuplex_ClientSendsEndStream_Success()
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes("Hello world");

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;
                using (HttpClient client = CreateHttpClient())
                {
                    var duplexContent = new DuplexContent();

                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = duplexContent;
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    connection = await server.EstablishConnectionAsync();

                    // Client should have sent the request headers, and the request stream should now be available
                    Stream requestStream = await duplexContent.WaitForStreamAsync();

                    // Flush the content stream. Otherwise, the request headers are not guaranteed to be sent.
                    await requestStream.FlushAsync();

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                    // Send data to the server, even before we've received response headers.
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send response headers
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);
                    HttpResponseMessage response = await responseTask;
                    Stream responseStream = await response.Content.ReadAsStreamAsync();

                    // Send some data back and forth
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);

                    // Complete sending the request body
                    await SendAndReceiveRequestEOFAsync(duplexContent, requestStream, connection, streamId);

                    // Send more data to the client
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);

                    // Complete sending the response body
                    await SendAndReceiveResponseEOFAsync(responseStream, connection, streamId);
                }

                // On handler dispose, client should shutdown the connection without sending additional frames.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        [Fact]
        public async Task PostAsyncDuplex_ServerSendsEndStream_Success()
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes("Hello world");

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;
                using (HttpClient client = CreateHttpClient())
                {
                    var duplexContent = new DuplexContent();

                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = duplexContent;
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    connection = await server.EstablishConnectionAsync();

                    // Client should have sent the request headers, and the request stream should now be available
                    Stream requestStream = await duplexContent.WaitForStreamAsync();

                    // Flush the content stream. Otherwise, the request headers are not guaranteed to be sent.
                    await requestStream.FlushAsync();

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                    // Send data to the server, even before we've received response headers.
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send response headers
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);
                    HttpResponseMessage response = await responseTask;
                    Stream responseStream = await response.Content.ReadAsStreamAsync();

                    // Send some data back and forth
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);

                    // Complete sending the response body
                    await SendAndReceiveResponseEOFAsync(responseStream, connection, streamId);

                    // Send more data to the server
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Complete sending the request body
                    await SendAndReceiveRequestEOFAsync(duplexContent, requestStream, connection, streamId);
                }

                // On handler dispose, client should shutdown the connection without sending additional frames.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        [Fact]
        public async Task PostAsyncDuplex_RequestContentException_ResetsStream()
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes("Hello world");

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;
                using (HttpClient client = CreateHttpClient())
                {
                    var duplexContent = new DuplexContent();

                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = duplexContent;
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    connection = await server.EstablishConnectionAsync();

                    // Client should have sent the request headers, and the request stream should now be available
                    Stream requestStream = await duplexContent.WaitForStreamAsync();

                    // Flush the content stream. Otherwise, the request headers are not guaranteed to be sent.
                    await requestStream.FlushAsync();

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                    // Send data to the server, even before we've received response headers.
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send response headers
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);
                    HttpResponseMessage response = await responseTask;
                    Stream responseStream = await response.Content.ReadAsStreamAsync();

                    // Send some data back and forth
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Throw an exception from the request content.
                    Exception e = new ArithmeticException();
                    duplexContent.Fail(e);

                    // Client should set RST_STREAM.
                    await connection.ReadRstStreamAsync(streamId);

                    // Trying to read on the response stream should fail now, and client should ignore any data received
                    await Assert.ThrowsAsync<IOException>(async () => await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId));
                }

                // On handler dispose, client should shutdown the connection without sending additional frames.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        [Fact]
        public async Task PostAsyncDuplex_RequestContentExceptionAfterResponseEndReceivedButBeforeConsumed_ResetsStreamAndThrowsOnResponseStreamRead()
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes("Hello world");

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;
                using (HttpClient client = CreateHttpClient())
                {
                    var duplexContent = new DuplexContent();

                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = duplexContent;
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    connection = await server.EstablishConnectionAsync();

                    // Client should have sent the request headers, and the request stream should now be available
                    Stream requestStream = await duplexContent.WaitForStreamAsync();

                    // Flush the content stream. Otherwise, the request headers are not guaranteed to be sent.
                    await requestStream.FlushAsync();

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                    // Send data to the server, even before we've received response headers.
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send response headers
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);
                    HttpResponseMessage response = await responseTask;
                    Stream responseStream = await response.Content.ReadAsStreamAsync();

                    // Send some data back and forth
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send data on response stream and complete it, but don't read it on the client yet
                    await connection.SendResponseDataAsync(streamId, contentBytes, endStream: true);

                    // Pingpong to ensure it's processed by client
                    await connection.PingPong();

                    // Throw an exception from the request content.
                    Exception e = new ArithmeticException();
                    duplexContent.Fail(e);

                    // Client should set RST_STREAM.
                    await connection.ReadRstStreamAsync(streamId);

                    // Trying to read on the response stream should fail now, and client should ignore any data received
                    await Assert.ThrowsAsync<IOException>(async () => await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId));
                }

                // On handler dispose, client should shutdown the connection without sending additional frames.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [Fact]
        public async Task PostAsyncDuplex_CancelledBeforeResponseHeadersReceived_ResetsStream()
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes("Hello world");

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;
                using (HttpClient client = CreateHttpClient())
                {
                    var duplexContent = new DuplexContent();

                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = duplexContent;
                    var cancellationTokenSource = new CancellationTokenSource();
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);

                    connection = await server.EstablishConnectionAsync();

                    // Client should have sent the request headers, and the request stream should now be available
                    Stream requestStream = await duplexContent.WaitForStreamAsync();

                    // Flush the content stream. Otherwise, the request headers are not guaranteed to be sent.
                    await requestStream.FlushAsync();

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                    // Send data to the server, even before we've received response headers.
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Cancel the request
                    cancellationTokenSource.Cancel();

                    // Cancellation may not propagate immediately. So wait a brief time to try to ensure it propagates.
                    await Task.Delay(3000);

                    // Attempting to write on the request body should now fail with OperationCanceledException.
                    Exception e = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId); });

                    // Propagate the exception to the request stream serialization task.
                    // This allows the request processing to complete.
                    duplexContent.Fail(e);

                    // Client should set RST_STREAM.
                    await connection.ReadRstStreamAsync(streamId);

                    // Request should fail.
                    await Assert.ThrowsAsync<TaskCanceledException>(() => responseTask);
                }

                // On handler dispose, client should shutdown the connection without sending additional frames.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        [Fact]
        public async Task PostAsyncDuplex_ServerResetsStream_Throws()
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes("Hello world");

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;
                using (HttpClient client = CreateHttpClient())
                {
                    var duplexContent = new DuplexContent();

                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = duplexContent;
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    connection = await server.EstablishConnectionAsync();

                    // Client should have sent the request headers, and the request stream should now be available
                    Stream requestStream = await duplexContent.WaitForStreamAsync();

                    // Flush the content stream. Otherwise, the request headers are not guaranteed to be sent.
                    await requestStream.FlushAsync();

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                    // Send data to the server, even before we've received response headers.
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send response headers
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);
                    HttpResponseMessage response = await responseTask;
                    Stream responseStream = await response.Content.ReadAsStreamAsync();

                    // Send some data back and forth
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send RST_STREAM to client.
                    await connection.WriteFrameAsync(new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.ENHANCE_YOUR_CALM, streamId));

                    // Trying to read on the response stream should fail now, and client should ignore any data received
                    await AssertProtocolErrorForIOExceptionAsync(SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId), ProtocolErrors.ENHANCE_YOUR_CALM);

                    // Attempting to write on the request body should now fail with IOException.
                    Exception e = await Assert.ThrowsAnyAsync<IOException>(async () => { await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId); });

                    // Propagate the exception to the request stream serialization task.
                    // This allows the request processing to complete.
                    duplexContent.Fail(e);
                }

                // On handler dispose, client should shutdown the connection without sending additional frames.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [Fact]
        public async Task PostAsyncDuplex_DisposeResponseBodyBeforeEnd_ResetsStreamAndThrowsOnRequestStreamWriteAndResponseStreamRead()
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes("Hello world");

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;
                using (HttpClient client = CreateHttpClient())
                {
                    var duplexContent = new DuplexContent();

                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = duplexContent;
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    connection = await server.EstablishConnectionAsync();

                    // Client should have sent the request headers, and the request stream should now be available
                    Stream requestStream = await duplexContent.WaitForStreamAsync();

                    // Flush the content stream. Otherwise, the request headers are not guaranteed to be sent.
                    await requestStream.FlushAsync();

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                    // Send data to the server, even before we've received response headers.
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send response headers
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);
                    HttpResponseMessage response = await responseTask;
                    Stream responseStream = await response.Content.ReadAsStreamAsync();

                    // Send some data back and forth
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Dispose the response stream.
                    responseStream.Dispose();

                    // Trying to read on the response stream should fail now, and client should ignore any data received
                    await Assert.ThrowsAsync<ObjectDisposedException>(async () => await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId));

                    // Cancellation on the request body may not propagate immediately. So wait a brief time to try to ensure it propagates.
                    await Task.Delay(500);

                    // Attempting to write on the request body should now fail with OperationCanceledException.
                    Exception e = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId); });

                    // Propagate the exception to the request stream serialization task.
                    // This allows the request processing to complete.
                    duplexContent.Fail(e);

                    // Client should set RST_STREAM.
                    await connection.ReadRstStreamAsync(streamId);
                }

                // On handler dispose, client should shutdown the connection without sending additional frames.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [Fact]
        public async Task PostAsyncDuplex_DisposeResponseBodyAfterEndReceivedButBeforeConsumed_ResetsStreamAndThrowsOnRequestStreamWriteAndResponseStreamRead()
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes("Hello world");

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;
                using (HttpClient client = CreateHttpClient())
                {
                    var duplexContent = new DuplexContent();

                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = duplexContent;
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    connection = await server.EstablishConnectionAsync();

                    // Client should have sent the request headers, and the request stream should now be available
                    Stream requestStream = await duplexContent.WaitForStreamAsync();

                    // Flush the content stream. Otherwise, the request headers are not guaranteed to be sent.
                    await requestStream.FlushAsync();

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                    // Send data to the server, even before we've received response headers.
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send response headers
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);
                    HttpResponseMessage response = await responseTask;
                    Stream responseStream = await response.Content.ReadAsStreamAsync();

                    // Send some data back and forth
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send data on response stream and completed it, but don't read it on the client yet
                    await connection.SendResponseDataAsync(streamId, contentBytes, endStream: true);

                    // Pingpong to ensure it's processed by client
                    await connection.PingPong();

                    // Dispose the response stream.
                    responseStream.Dispose();

                    // Trying to read on the response stream should fail now, and client should ignore any data received
                    await Assert.ThrowsAsync<ObjectDisposedException>(async () => await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId));

                    // Cancellation on the request body may not propagate immediately. So wait a brief time to try to ensure it propagates.
                    await Task.Delay(500);

                    // Attempting to write on the request body should now fail with OperationCanceledException.
                    Exception e = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId); });

                    // Propagate the exception to the request stream serialization task.
                    // This allows the request processing to complete.
                    duplexContent.Fail(e);

                    // Client should set RST_STREAM.
                    await connection.ReadRstStreamAsync(streamId);
                }

                // On handler dispose, client should shutdown the connection without sending additional frames.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        [OuterLoop("Uses Task.Delay")]
        [Fact]
        public async Task PostAsyncDuplex_FinishRequestBodyAndDisposeResponseBodyAfterEndReceivedButBeforeConsumed_DoesNotResetStream()
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes("Hello world");

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;
                using (HttpClient client = CreateHttpClient())
                {
                    var duplexContent = new DuplexContent();

                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = duplexContent;
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    connection = await server.EstablishConnectionAsync();

                    // Client should have sent the request headers, and the request stream should now be available
                    Stream requestStream = await duplexContent.WaitForStreamAsync();

                    // Flush the content stream. Otherwise, the request headers are not guaranteed to be sent.
                    await requestStream.FlushAsync();

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                    // Send data to the server, even before we've received response headers.
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send response headers
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);
                    HttpResponseMessage response = await responseTask;
                    Stream responseStream = await response.Content.ReadAsStreamAsync();

                    // Send some data back and forth
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Complete request stream.
                    await SendAndReceiveRequestEOFAsync(duplexContent, requestStream, connection, streamId);

                    // Send data on response stream and completed it, but don't read it on the client yet
                    await connection.SendResponseDataAsync(streamId, contentBytes, endStream: true);

                    // Pingpong to ensure it's processed by client
                    await connection.PingPong();


                    // Dispose the response stream.
                    responseStream.Dispose();

                    // Trying to read on the response stream should fail now, and client should ignore any data received
                    await Assert.ThrowsAsync<ObjectDisposedException>(async () => await SendAndReceiveResponseDataAsync(contentBytes, responseStream, connection, streamId));

                    // Client should NOT send RST_STREAM. The stream was completed succesfully, but the remaining response data was discarded.
                }

                // On handler dispose, client should shutdown the connection without sending additional frames.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        [Fact]
        public async Task PostAsyncDuplex_ServerCompletesResponseBodyThenResetsStreamWithNoError_SuccessAndRequestBodyCancelled()
        {
            // Per section 8.1 of the RFC:
            // Receiving RST_STREAM with NO_ERROR after receiving EndStream on the response body is a special case.
            // We should stop sending the request body, but treat the request as successful and
            // return the completed response body to the user.

            byte[] contentBytes = Encoding.UTF8.GetBytes("Hello world");

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;
                using (HttpClient client = CreateHttpClient())
                {
                    var duplexContent = new DuplexContent();

                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = duplexContent;
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    connection = await server.EstablishConnectionAsync();

                    // Client should have sent the request headers, and the request stream should now be available
                    Stream requestStream = await duplexContent.WaitForStreamAsync();

                    // Flush the content stream. Otherwise, the request headers are not guaranteed to be sent.
                    await requestStream.FlushAsync();

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                    // Send data to the server, even before we've received response headers.
                    await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId);

                    // Send response headers
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);
                    HttpResponseMessage response = await responseTask;
                    Stream responseStream = await response.Content.ReadAsStreamAsync();

                    // Send response body and complete response
                    await connection.SendResponseDataAsync(streamId, contentBytes, endStream: true);

                    // Send RST_STREAM to client with error = NO_ERROR.
                    await connection.WriteFrameAsync(new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.NO_ERROR, streamId));

                    // Ensure client has processed the RST_STREAM.
                    await connection.PingPong();

                    // Attempting to write on the request body should now fail with OperationCanceledException.
                    Exception e = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await SendAndReceiveRequestDataAsync(contentBytes, requestStream, connection, streamId); });

                    // Propagate the exception to the request stream serialization task.
                    // This allows the request processing to complete.
                    duplexContent.Fail(e);

                    // We should receive the response body and EOF.
                    byte[] readBuffer = new byte[contentBytes.Length];
                    int bytesRead = await responseStream.ReadAsync(readBuffer);
                    Assert.True(contentBytes.SequenceEqual(readBuffer));
                    bytesRead = await responseStream.ReadAsync(readBuffer);
                    Assert.Equal(0, bytesRead);
                }

                // On handler dispose, client should shutdown the connection without sending additional frames.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        [Fact]
        public async Task PostAsyncNonDuplex_ServerCompletesResponseBodyThenResetsStreamWithNoError_SuccessAndRequestBodyCancelled()
        {
            // Per section 8.1 of the RFC:
            // Receiving RST_STREAM with NO_ERROR after receiving EndStream on the response body is a special case.
            // We should stop sending the request body, but treat the request as successful and
            // return the completed response body to the user.

            byte[] contentBytes = Encoding.UTF8.GetBytes("Hello world");

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;
                using (HttpClient client = CreateHttpClient())
                {
                    // We want non-duplex content, so use ByteArrayContent,
                    // but make it large enough to ensure that the content can't be fully sent because of flow control limitations.
                    // This allows us to validate that the content is actually canceled, not just fully sent and completed.
                    const int ContentSize = 100_000;
                    var requestContent = new ByteArrayContent(TestHelper.GenerateRandomContent(ContentSize));

                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = requestContent;
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    connection = await server.EstablishConnectionAsync();

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                    // Send full response
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);
                    await connection.SendResponseDataAsync(streamId, contentBytes, endStream: true);

                    // Send RST_STREAM to client with error = NO_ERROR.
                    await connection.WriteFrameAsync(new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.NO_ERROR, streamId));

                    // Response should now complete successfully
                    HttpResponseMessage response = await responseTask;
                    Assert.Equal("Hello world", await response.Content.ReadAsStringAsync());
                }

                // On handler dispose, client should shutdown the connection. Ignore any request stream frames already sent.
                await connection.WaitForClientDisconnectAsync(ignoreUnexpectedFrames: true);
            }
        }

        [Theory]
        [InlineData(true, HttpStatusCode.Forbidden)]
        [InlineData(false, HttpStatusCode.Forbidden)]
        [InlineData(true, HttpStatusCode.OK)]
        [InlineData(false, HttpStatusCode.OK)]
        [OuterLoop("Uses Task.Delay")]
        public async Task SendAsync_ConcurentSendReceive_Ok(bool shouldWaitForRequestBody, HttpStatusCode responseCode)
        {
            string requestContent = new string('*', 300);
            const string responseContent = "SendAsync_ConcurentSendReceive_Ok";

            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            {
                Http2LoopbackConnection connection;

                using (HttpClient client = CreateHttpClient())
                {
                    var duplexContent = new DuplexContent();
                    var request = new HttpRequestMessage(HttpMethod.Post, server.Address);
                    request.Version = new Version(2, 0);
                    request.Content = duplexContent;

                    Task<HttpResponseMessage> responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    connection = await server.EstablishConnectionAsync();

                     // Client should have sent the request headers, and the request stream should now be available
                    Stream requestStream = await duplexContent.WaitForStreamAsync();
                    // Flush the content stream. Otherwise, the request headers are not guaranteed to be sent.
                    await requestStream.FlushAsync();

                    (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody : false);

                    // Client finished sending request headers and we received them.
                    // Send request body.
                    await requestStream.WriteAsync(Encoding.UTF8.GetBytes(requestContent));
                    duplexContent.Complete();

                    if (shouldWaitForRequestBody)
                    {
                        // Read body first before sending back any response in non-duplex variant.
                        await connection.ReadBodyAsync();
                    }

                    // Send response headers
                    await connection.SendResponseHeadersAsync(streamId, endStream: false, responseCode);

                    HttpResponseMessage response = await responseTask;
                    Assert.Equal(responseCode, response.StatusCode);


                    await connection.SendResponseDataAsync(streamId, Encoding.ASCII.GetBytes(responseContent), endStream: false);
                    if (!shouldWaitForRequestBody)
                    {
                        // In duplex case response is sent before reading request body.
                        await connection.ReadBodyAsync();
                    }

                    // Send trailing headers for good measure and close stream.
                    var headers = new HttpHeaderData[] { new HttpHeaderData("x-last", "done") };
                    await connection.SendResponseHeadersAsync(streamId, endStream: true, isTrailingHeader : true, headers: headers);

                    // Finish reading response body and verify it for all cases.
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Assert.Equal(responseContent, responseBody);
                }

                // On handler dispose, client should shutdown the connection without sending additional frames.
                await connection.WaitForClientDisconnectAsync();
            }
        }

        [Fact]
        [OuterLoop("Uses Task.Delay")]
        public async Task SendAsync_ConcurentSendReceive_Fail()
        {
            TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();
            string requestContent = new string('*', 300);
            const string responseContent = "SendAsync_ConcurentSendReceive_Fail";
            var stream = new CustomContent.SlowTestStream(Encoding.UTF8.GetBytes(requestContent), tsc, trigger : 1, count : 50);
            bool stopSending = false;

            await Http2LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Version = new Version(2,0);
                    request.Content = new CustomContent(stream);

                    // This should fail either while getting response headers or while reading response body.
                    HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    // Wait for request body start streaming.
                    await tsc.Task.ConfigureAwait(false);
                    // and inject distinct exception on request stream.
                    stream.SetException(new ArithmeticException("Injected test exception"));

                    HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsStringAsync());
                    Assert.IsType<IOException>(e.InnerException);

                    stopSending = true;
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();

                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody : false);
                await connection.SendResponseHeadersAsync(streamId, endStream: false, HttpStatusCode.OK);

                // Wait for client so start sending body.
                await tsc.Task.ConfigureAwait(false);

                int maxCount = 120;
                while (!stopSending && maxCount != 0)
                {
                   try
                    {
                        await connection.SendResponseDataAsync(streamId, Encoding.ASCII.GetBytes(responseContent), endStream: false);
                    }
                    catch (IOException)
                    {
                        // When client sets stopSending, client will be disposed and sending may fail.
                        Assert.True(stopSending);
                        break;
                    }
                    await Task.Delay(500);
                    maxCount --;
                }
                // We should not reach retry limit without failing.
                Assert.NotEqual(0, maxCount);

                try
                {
                    await connection.SendGoAway(streamId);
                    await connection.WaitForConnectionShutdownAsync();
                }
                catch { };
            });
        }

        [Fact]
        [OuterLoop("Waits for seconds for events that shouldn't happen")]
        public async Task SendAsync_StreamContentRequestBody_WaitsForRequestBodyToComplete()
        {
            var waitToSendRequestBody = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var sendAsyncCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // Create a content stream that will wait for a signal before it dribbles out some request content.
            int sent = 0;
            var stream = new DelegateStream(
                canReadFunc: () => true,
                readAsyncFunc: async (buffer, offset, count, token) =>
                {
                    await waitToSendRequestBody.Task;
                    return sent++ < 10 ? 1 : 0;
                });

            await Http2LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    // Connect to the server and SendAsync.
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Version = new Version(2, 0);
                    request.Content = new StreamContent(stream);
                    Task<HttpResponseMessage> sendAsyncTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    // Wait a bit, and confirm SendAsync hasn't completed yet (because the request content can't have finished yet).
                    await Task.Delay(1000);
                    Assert.False(sendAsyncTask.IsCompleted);

                    // Now let the request content go.  The SendAsync task should complete quickly.
                    waitToSendRequestBody.SetResult();
                    using (HttpResponseMessage r = await sendAsyncTask)
                    {
                        // Wake up the server.
                        sendAsyncCompleted.SetResult();
                    }
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync().ConfigureAwait(false);
                await connection.SendResponseHeadersAsync(streamId);
                await sendAsyncCompleted.Task;
                await connection.WaitForConnectionShutdownAsync();
            });
        }

        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2_ProtocolMismatch_Throws()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

            using (HttpClient client = CreateHttpClient())
            {
                // Create HTTP/1.1 loopback server and advertise HTTP2 via ALPN.
                await LoopbackServer.CreateServerAsync(async (server, uri) =>
                {
                    // Convert http to https as we are going to negotiate TLS.
                    Task<string> requestTask = client.GetStringAsync(uri.ToString().Replace("http://", "https://"));

                    await server.AcceptConnectionAsync(async connection =>
                    {
                        // negotiate TLS with ALPN H/2
                        var sslStream = new SslStream(connection.Stream, false, delegate { return true; });
                        SslServerAuthenticationOptions options = new SslServerAuthenticationOptions();
                        options.ServerCertificate = Net.Test.Common.Configuration.Certificates.GetServerCertificate();
                        options.ApplicationProtocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http2 };
                        options.ApplicationProtocols.Add(SslApplicationProtocol.Http2);

                        // Negotiate TLS.
                        await sslStream.AuthenticateAsServerAsync(options, CancellationToken.None).ConfigureAwait(false);

                        // Send back HTTP/1.1 response
                        await sslStream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 400 Unrecognized request\r\n\r\n"), CancellationToken.None);
                    });

                    Exception e = await Assert.ThrowsAsync<HttpRequestException>(() => requestTask);
                });
            }
        }

        // rfc7540 8.1.2.3.
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2GetAsync_MultipleStatusHeaders_Throws()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                IList<HttpHeaderData> headers = new HttpHeaderData[] { new HttpHeaderData(":status", "300"), new HttpHeaderData("x-test", "Http2GetAsync_MultipleStatusHeaders_Throws") };
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.SendResponseHeadersAsync(streamId, endStream : true, headers: headers);
                await Assert.ThrowsAsync<HttpRequestException>(() => sendTask);
            }
        }

        // rfc7540 8.1.2.3.
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2GetAsync_StatusHeaderNotFirst_Throws()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                IList<HttpHeaderData> headers = new HttpHeaderData[] { new HttpHeaderData("x-test", "Http2GetAsync_StatusHeaderNotFirst_Throws"), new HttpHeaderData(":status", "200") };
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.SendResponseHeadersAsync(streamId, endStream : true, isTrailingHeader : true, headers: headers);

                await Assert.ThrowsAsync<HttpRequestException>(() => sendTask);
            }
        }

        // rfc7540 8.1.2.3.
        [ConditionalFact(nameof(SupportsAlpn))]
        public async Task Http2GetAsync_TrailigPseudo_Throw()
        {
            using (Http2LoopbackServer server = Http2LoopbackServer.CreateServer())
            using (HttpClient client = CreateHttpClient())
            {
                IList<HttpHeaderData> headers = new HttpHeaderData[] { new HttpHeaderData(":path", "http"), new HttpHeaderData("x-test", "Http2GetAsync_TrailigPseudo_Throw") };
                Task<HttpResponseMessage> sendTask = client.GetAsync(server.Address);

                Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.SendDefaultResponseHeadersAsync(streamId);
                await connection.SendResponseDataAsync(streamId, Encoding.ASCII.GetBytes("hello"), endStream: false);
                await connection.SendResponseHeadersAsync(streamId, endStream : true, isTrailingHeader : true, headers: headers);

                await Assert.ThrowsAsync<HttpRequestException>(() => sendTask);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task Http2SendAsync_LargeHeaders_CorrectlyWritten(int continuationCount)
        {
            // Intentionally larger than 2x16K in total because that's the limit that will trigger a CONTINUATION frame in HTTP2.
            string largeHeaderValue = new string('a', 1024);
            int count = continuationCount * 16 + 1;

            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();

                var message = new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion };
                for (int i = 0; i < count; i++)
                {
                    message.Headers.TryAddWithoutValidation("large-header" + i, largeHeaderValue);
                }
                var response = await client.SendAsync(TestAsync, message).ConfigureAwait(false);
            },
            async server =>
            {
                // Primes the connection, i.e. forces it to exchange SETTINGS whose ACK sometimes flushed the headers as well.
                // See: https://github.com/dotnet/runtime/issues/860 for repro description.
                (Http2LoopbackConnection connection, SettingsFrame clientSettings) = await server.EstablishConnectionGetSettingsAsync();

                // Read individual frames and assert number of CONTINUATIONs.
                int receivedContinuations = 0;
                int streamId = 0;
                await foreach (Frame frame in connection.ReadRequestHeadersFrames())
                {
                    if (frame.Type == FrameType.Headers)
                    {
                        streamId = ((HeadersFrame)frame).StreamId;
                    }
                    if (frame.Type == FrameType.Continuation)
                    {
                        ++receivedContinuations;
                    }
                }
                Assert.Equal(continuationCount, receivedContinuations);
                await connection.SendResponseAsync(HttpStatusCode.OK, requestId: streamId);
            });
        }

        [Fact]
        public async Task InboundWindowSize_Exceeded_Throw()
        {
            var semaphore = new SemaphoreSlim(0);

            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    // An exception will be thrown by either GetAsync or ReadAsStringAsync once
                    // the inbound window size has been exceeded. Which one depends on how quickly
                    // ProcessIncomingFramesAsync() can read data off the socket.
                    Exception requestException = await Assert.ThrowsAsync<HttpRequestException>(async () =>
                    {
                        using HttpClient client = CreateHttpClient();
                        using HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                        // Keep client open until server is done.
                        await semaphore.WaitAsync(10000);

                        await response.Content.ReadAsStringAsync();
                    });

                    // A Http2ConnectionException will be present somewhere in the inner exceptions.
                    // Its location depends on which method threw the exception.
                    while (requestException?.GetType().FullName.Equals("System.Net.Http.Http2ConnectionException") == false)
                    {
                        requestException = requestException.InnerException;
                    }

                    Assert.NotNull(requestException);
                    Assert.Contains("FLOW_CONTROL_ERROR", requestException.Message);
                },
                async server =>
                {
                    try
                    {
                        (Http2LoopbackConnection connection, SettingsFrame clientSettings) = await server.EstablishConnectionGetSettingsAsync();

                        SettingsEntry clientWindowSizeSetting = clientSettings.Entries.SingleOrDefault(x => x.SettingId == SettingId.InitialWindowSize);
                        int clientWindowSize = clientWindowSizeSetting.SettingId == SettingId.InitialWindowSize ? (int)clientWindowSizeSetting.Value : 65535;

                        // Exceed the window size by 1 byte.
                        ++clientWindowSize;

                        int streamId = await connection.ReadRequestHeaderAsync();

                        // Write the response.
                        await connection.SendDefaultResponseHeadersAsync(streamId);

                        byte[] buffer = new byte[4096];
                        int totalSent = 0;

                        while (totalSent < clientWindowSize)
                        {
                            int sendSize = Math.Min(buffer.Length, clientWindowSize - totalSent);
                            ReadOnlyMemory<byte> sendBuf = buffer.AsMemory(0, sendSize);

                            await connection.SendResponseDataAsync(streamId, sendBuf, endStream: false);
                            totalSent += sendSize;
                        }

                        // Try to read a frame. Should get null if connection reset or RST_STREAM if stream reset.
                        // If client is misbehaving, we'll get an OperationCanceledException due to timeout.
                        try
                        {
                            Frame clientFrame = await connection.ReadFrameAsync(TimeSpan.FromSeconds(5));
                            Assert.True(clientFrame == null || (clientFrame.Type == FrameType.RstStream && clientFrame.StreamId == streamId),
                                "Unexpected frame received from HttpClient; Expected either RST_STREAM or connection reset.");
                        }
                        catch (OperationCanceledException ex)
                        {
                            Assert.True(ex == null, "Stream unexpectedly left open by HttpClient; Expected either RST_STREAM or connection reset.");
                        }
                    }
                    finally
                    {
                        // Shut down client.
                        semaphore.Release();
                    }
                });
        }

        [Fact]
        [OuterLoop("Uses Task.Delay")]
        public async Task SocketSendQueueFull_RequestCanceled_ThrowsOperationCanceled()
        {
            TaskCompletionSource clientComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                    using HttpClient client = CreateHttpClient();

                    // Post a large request, large enough to fill the socket send buffer
                    Task postTask = client.PostAsync(uri, new StringContent(new string('a', 16 * 1024 * 1024)), cancellationTokenSource.Token);

                    // Wait a while to hopefully ensure that the send buffer has been completely filled
                    await Task.Delay(3000);

                    Assert.False(postTask.IsCompleted);

                    // Ensure that the request can be cancelled
                    cancellationTokenSource.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => postTask);

                    // Allow server to exit
                    clientComplete.SetResult();
                },
                async server =>
                {
                    // Establish the connection and ensure client is not blocked on windows
                    Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.InitialWindowSize, Value = 128 * 1024 * 1024 });
                    await connection.WriteFrameAsync(new WindowUpdateFrame(128 * 1024 * 1024, 0));

                    // Now, don't process any frames. Let the client's send buffer fill up.
                    // When the client is done, it will signal us.
                    await clientComplete.Task;
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MaxResponseHeadersLength_Exact_Success(bool huffmanEncode)
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpClientHandler handler = CreateHttpClientHandler();
                    handler.MaxResponseHeadersLength = 1;

                    using HttpClient client = CreateHttpClient(handler);
                    using HttpResponseMessage response = await client.GetAsync(uri);
                },
                async server =>
                {
                    Http2LoopbackConnection con = await server.EstablishConnectionAsync();
                    int streamId = await con.ReadRequestHeaderAsync();

                    await con.SendResponseHeadersAsync(streamId, isTrailingHeader: true, headers: new[]
                    {
                        // 1000 + other strings = 1024
                        new HttpHeaderData(":status", "200", huffmanEncoded: huffmanEncode),
                        new HttpHeaderData("padding-header", new string(' ', 1000), huffmanEncoded: huffmanEncode)
                    });

                    await con.ShutdownIgnoringErrorsAsync(streamId);
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MaxResponseHeadersLength_Exceeded_Throws(bool huffmanEncode)
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpClientHandler handler = CreateHttpClientHandler();
                    handler.MaxResponseHeadersLength = 1;

                    using HttpClient client = CreateHttpClient(handler);
                    Exception e = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri));
                    Assert.Contains((handler.MaxResponseHeadersLength * 1024).ToString(), e.ToString());
                },
                async server =>
                {
                    Http2LoopbackConnection con = await server.EstablishConnectionAsync();
                    int streamId = await con.ReadRequestHeaderAsync();

                    await con.SendResponseHeadersAsync(streamId, isTrailingHeader: true, headers: new[]
                    {
                        // 1001 + other strings = 1025
                        new HttpHeaderData(":status", "200", huffmanEncoded: huffmanEncode),
                        new HttpHeaderData("padding-header", new string(' ', 1001), huffmanEncoded: huffmanEncode)
                    });

                    await con.ShutdownIgnoringErrorsAsync(streamId);
                });
        }

        [Fact]
        public async Task MaxResponseHeadersLength_Malicious_Throws()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpClientHandler handler = CreateHttpClientHandler();
                    handler.MaxResponseHeadersLength = 1;

                    using HttpClient client = CreateHttpClient(handler);
                    Exception e = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri));
                    Assert.Contains((handler.MaxResponseHeadersLength * 1024).ToString(), e.ToString());
                },
                async server =>
                {
                    Http2LoopbackConnection con = await server.EstablishConnectionAsync();
                    int streamId = await con.ReadRequestHeaderAsync();

                    // A small malicious/corrupt payload that expands into two 1GB strings. We don't want HPackDecoder to allocate buffers when they exceed MaxResponseHeadersLength.
                    byte[] headerData = new byte[] { 0x88, 0x00, 0x7F, 0x81, 0xFF, 0xFF, 0xFF, 0x03, 0x70, 0x6C, 0x61, 0x69, 0x6E, 0x2D, 0x74, 0x65, 0x78, 0x74, 0x7F, 0x81, 0xFF, 0xFF, 0xFF, 0x03, 0x61, 0x61, 0x61, 0x61, 0x61, 0x61, 0x61, 0x61, 0x61, 0x61 };
                    HeadersFrame frame = new HeadersFrame(headerData, FrameFlags.EndHeaders | FrameFlags.EndStream, 0, 0, 0, streamId);

                    await con.WriteFrameAsync(frame);
                    await con.ShutdownIgnoringErrorsAsync(streamId);
                });
        }

        [Fact]
        public async Task DynamicTableSizeUpdate_Exceeds_Settings_Throws()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient client = CreateHttpClient();
                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri));
                },
                async server =>
                {
                    (Http2LoopbackConnection con, SettingsFrame settings) = await server.EstablishConnectionGetSettingsAsync();
                    int streamId = await con.ReadRequestHeaderAsync();

                    int headerTableSize = settings.GetHeaderTableSize();

                    byte[] headerData = new byte[16];
                    int headersLen = HPackEncoder.EncodeDynamicTableSizeUpdate(headerTableSize + 1, headerData);
                    HeadersFrame frame = new HeadersFrame(headerData.AsMemory(0, headersLen), FrameFlags.EndHeaders | FrameFlags.EndStream, 0, 0, 0, streamId);

                    await con.WriteFrameAsync(frame);
                    await con.ShutdownIgnoringErrorsAsync(streamId);
                });
        }

        [Fact]
        public async Task DynamicTable_Reuse_Concat()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient client = CreateHttpClient();
                    using HttpResponseMessage response = await client.GetAsync(uri);

                    Assert.True(response.Headers.TryGetValues("my-header", out IEnumerable<string> values));
                    Assert.True(values.SequenceEqual(new[] { "foo", "bar" }));
                },
                async server =>
                {
                    byte[] frameData = new byte[16 * 1024];

                    Http2LoopbackConnection con = await server.EstablishConnectionAsync();
                    int streamId = await con.ReadRequestHeaderAsync();

                    int pos = 0;
                    pos += HPackEncoder.EncodeHeader(":status", "200", HPackFlags.None, frameData.AsSpan(pos));
                    pos += HPackEncoder.EncodeHeader("my-header", "foo", HPackFlags.NewIndexed, frameData.AsSpan(pos));
                    pos += HPackEncoder.EncodeHeader(HPackEncoder.SmallestDynamicIndex, "bar", HPackFlags.None, frameData.AsSpan(pos));
                    await con.WriteFrameAsync(new HeadersFrame(frameData.AsMemory(0, pos), FrameFlags.EndHeaders | FrameFlags.EndStream, 0, 0, 0, streamId));

                    // Close out connection.
                    await con.ShutdownIgnoringErrorsAsync(streamId);
                });
        }

        [Fact]
        public async Task DynamicTable_Resize_Success()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient client = CreateHttpClient();

                    // First request should initialize our dynamic table.
                    using (HttpResponseMessage response = await client.GetAsync(uri))
                    {
                        IEnumerable<string> values;

                        Assert.True(response.Headers.TryGetValues("header-that-gos", out values));
                        Assert.Equal("bar", Assert.Single(values));

                        Assert.True(response.Headers.TryGetValues("header-that-stays", out values));
                        Assert.Equal("foo", Assert.Single(values));
                    }

                    // Second request should reset the dynamic table.
                    using (HttpResponseMessage response = await client.GetAsync(uri))
                    {
                        Assert.True(response.Headers.TryGetValues("new-header", out IEnumerable<string> values));
                        Assert.Equal("baz", Assert.Single(values));
                    }

                    // Third request should use the reset values.
                    using (HttpResponseMessage response = await client.GetAsync(uri))
                    {
                        IEnumerable<string> values;

                        Assert.True(response.Headers.TryGetValues("header-that-stays", out values));
                        Assert.Equal("foo", Assert.Single(values));

                        Assert.True(response.Headers.TryGetValues("new-header", out values));
                        Assert.Equal("baz", Assert.Single(values));
                    }
                },
                async server =>
                {
                    Http2LoopbackConnection con = await server.EstablishConnectionAsync();
                    byte[] frameData = new byte[16 * 1024];

                    // First stream, create dynamic indexes.
                    // header-that-goes: 63, header-that-stays: 62
                    int streamId = await con.ReadRequestHeaderAsync();
                    int pos = 0;
                    pos += HPackEncoder.EncodeHeader(":status", "200", HPackFlags.None, frameData.AsSpan(pos));
                    pos += HPackEncoder.EncodeHeader("header-that-gos", "bar", HPackFlags.NewIndexed, frameData.AsSpan(pos));
                    pos += HPackEncoder.EncodeHeader("header-that-stays", "foo", HPackFlags.NewIndexed, frameData.AsSpan(pos));
                    await con.WriteFrameAsync(new HeadersFrame(frameData.AsMemory(0, pos), FrameFlags.EndHeaders | FrameFlags.EndStream, 0, 0, 0, streamId));

                    // Second stream, resize the table so that the header-that-gos is removed from table, and add a new header.
                    // 1) resize: header-that-stays: 62
                    // 2) add new header: header-that-stays:63, new-header:62
                    streamId = await con.ReadRequestHeaderAsync();
                    pos = 0;
                    pos += HPackEncoder.EncodeDynamicTableSizeUpdate("header-that-staysfoo".Length + HttpHeaderData.RfcOverhead, frameData.AsSpan(pos));
                    pos += HPackEncoder.EncodeDynamicTableSizeUpdate(4096, frameData.AsSpan(pos));
                    pos += HPackEncoder.EncodeHeader(":status", "200", HPackFlags.None, frameData.AsSpan(pos));
                    pos += HPackEncoder.EncodeHeader("new-header", "baz", HPackFlags.NewIndexed, frameData.AsSpan(pos));
                    await con.WriteFrameAsync(new HeadersFrame(frameData.AsMemory(0, pos), FrameFlags.EndHeaders | FrameFlags.EndStream, 0, 0, 0, streamId));

                    // Third stream, use the dynamic table established in previous stream.
                    streamId = await con.ReadRequestHeaderAsync();
                    pos = 0;
                    pos += HPackEncoder.EncodeHeader(":status", "200", HPackFlags.None, frameData.AsSpan(pos));
                    pos += HPackEncoder.EncodeHeader(HPackEncoder.SmallestDynamicIndex + 1, frameData.AsSpan(pos)); // header-that-stays:foo
                    pos += HPackEncoder.EncodeHeader(HPackEncoder.SmallestDynamicIndex, frameData.AsSpan(pos)); // new-header:baz
                    await con.WriteFrameAsync(new HeadersFrame(frameData.AsMemory(0, pos), FrameFlags.EndHeaders | FrameFlags.EndStream, 0, 0, 0, streamId));

                    // Close out connection.
                    await con.ShutdownIgnoringErrorsAsync(streamId);
                });
        }

        [Theory]
        [MemberData(nameof(DynamicTable_Data))]
        public async Task DynamicTable_Receipt_Success(IEnumerable<HttpHeaderData> headers)
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient client = CreateHttpClient();

                    // First request should initialize our dynamic table.
                    using (HttpResponseMessage response = await client.GetAsync(uri))
                    {
                        AssertExpectedHeaders(response);
                    }

                    // Second request should use the dynamic table.
                    using (HttpResponseMessage response = await client.GetAsync(uri))
                    {
                        AssertExpectedHeaders(response);
                    }
                },
                async server =>
                {
                    (Http2LoopbackConnection con, SettingsFrame settings) = await server.EstablishConnectionGetSettingsAsync();

                    Debug.Assert(settings.GetHeaderTableSize() >= 4096, "Data for this theory requires a header table size of at least 4096.");

                    // First stream, create dynamic indexes.
                    int streamId = await con.ReadRequestHeaderAsync();

                    byte[] frameData = new byte[16 * 1024];
                    int pos = 0;

                    foreach (HttpHeaderData header in headers)
                    {
                        pos += HPackEncoder.EncodeHeader(header.Name, header.Value, HPackFlags.NewIndexed, frameData.AsSpan(pos));
                    }

                    await con.WriteFrameAsync(new HeadersFrame(frameData.AsMemory(0, pos), FrameFlags.EndHeaders | FrameFlags.EndStream, 0, 0, 0, streamId));

                    // Second stream, send headers using indexes only.
                    streamId = await con.ReadRequestHeaderAsync();
                    pos = 0;
                    for (int i = HPackEncoder.LargestStaticIndex + headers.Count(); i > HPackEncoder.LargestStaticIndex; --i)
                    {
                        pos += HPackEncoder.EncodeHeader(i, frameData.AsSpan(pos));
                    }

                    await con.WriteFrameAsync(new HeadersFrame(frameData.AsMemory(0, pos), FrameFlags.EndHeaders | FrameFlags.EndStream, 0, 0, 0, streamId));

                    // Close out connection.
                    await con.ShutdownIgnoringErrorsAsync(streamId);
                });

            void AssertExpectedHeaders(HttpResponseMessage response)
            {
                var expected = headers.Select(x => (name: x.Name.ToLowerInvariant(), value: x.Value.ToLowerInvariant()));
                var actual = response.Headers.SelectMany(x => x.Value, (kvp, v) => (name: kvp.Key.ToLowerInvariant(), value: v.ToLowerInvariant()));
                Assert.Empty(actual.Except(expected));
            }
        }

        public static IEnumerable<object[]> DynamicTable_Data()
        {
            yield return new object[] { GenerateHeadersWithStatus200(512) };
            yield return new object[] { GenerateHeadersWithStatus200(4096) };
        }

        /// <summary>
        /// Generates headers to an exact dynamic table size.
        /// </summary>
        private static IEnumerable<HttpHeaderData> GenerateHeadersWithStatus200(int targetSize)
        {
            Debug.Assert(targetSize > 64, $"{nameof(targetSize)} must be more than 64 to account for a status 200 and padding headers.");

            const string NamePrefix = "hn-";
            const string ValuePrefix = "hv-";

            // Add status 200 and remove its length and overhead.
            targetSize -= ":status200".Length + HttpHeaderData.RfcOverhead;
            yield return new HttpHeaderData(":status", "200");

            for (int i = 0; targetSize > 0; ++i)
            {
                string s = i.ToString(CultureInfo.InvariantCulture);

                string name = NamePrefix + s;
                string value;

                int left = targetSize - name.Length - HttpHeaderData.RfcOverhead;
                if (left < 64)
                {
                    // At this point, we pad out to reach the remaining size.
                    value = ValuePrefix + s.PadLeft(left - ValuePrefix.Length, '0');
                }
                else
                {
                    value = ValuePrefix + s;
                }

                yield return new HttpHeaderData(name, value);

                targetSize -= name.Length + value.Length + HttpHeaderData.RfcOverhead;
            }

            Debug.Assert(targetSize == 0); // Ensure we perfectly filled the target size.
        }
    }
}
