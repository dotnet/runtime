// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [ConditionalClass(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
    public sealed class SocketsHttpHandler_Http2ExtendedConnect_Test : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_Http2ExtendedConnect_Test(ITestOutputHelper output) : base(output) { }

        protected override Version UseVersion => HttpVersion.Version20;

        public static IEnumerable<object[]> UseSsl_MemberData()
        {
            yield return new object[] { false };

            if (PlatformDetection.SupportsAlpn)
            {
                yield return new object[] { true };
            }
        }

        [Theory]
        [MemberData(nameof(UseSsl_MemberData))]
        public async Task Connect_ReadWriteResponseStream(bool useSsl)
        {
            const int MessageCount = 3;
            byte[] clientMessage = new byte[] { 1, 2, 3 };
            byte[] serverMessage = new byte[] { 4, 5, 6, 7 };

            TaskCompletionSource clientCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

            await Http2LoopbackServerFactory.Singleton.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();

                HttpRequestMessage request = CreateRequest(HttpMethod.Connect, uri, UseVersion, exactVersion: true);
                request.Headers.Protocol = "foo";

                bool readFromContentStream = false;

                // We won't send the content bytes, but we will send content headers.
                // Since we're dropping the content, we'll also drop the Content-Length header.
                request.Content = new StreamContent(new DelegateStream(
                    readAsyncFunc: (_, _, _, _) =>
                    {
                        readFromContentStream = true;
                        throw new UnreachableException();
                    }));

                request.Headers.Add("User-Agent", "foo");
                request.Content.Headers.Add("Content-Language", "bar");
                request.Content.Headers.ContentLength = 42;

                using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                using Stream responseStream = await response.Content.ReadAsStreamAsync();

                for (int i = 0; i < MessageCount; i++)
                {
                    await responseStream.WriteAsync(clientMessage);
                    await responseStream.FlushAsync();

                    byte[] readBuffer = new byte[serverMessage.Length];
                    await responseStream.ReadExactlyAsync(readBuffer);
                    Assert.Equal(serverMessage, readBuffer);
                }

                // Receive server's EOS
                Assert.Equal(0, await responseStream.ReadAsync(new byte[1]));

                Assert.False(readFromContentStream);

                clientCompleted.SetResult();
            },
            async server =>
            {
                await using Http2LoopbackConnection connection = await ((Http2LoopbackServer)server).EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });

                (int streamId, HttpRequestData request) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);

                Assert.Equal("foo", request.GetSingleHeaderValue("User-Agent"));
                Assert.Equal("bar", request.GetSingleHeaderValue("Content-Language"));
                Assert.Equal(0, request.GetHeaderValueCount("Content-Length"));

                await connection.SendResponseHeadersAsync(streamId, endStream: false).ConfigureAwait(false);

                for (int i = 0; i < MessageCount; i++)
                {
                    DataFrame dataFrame = await connection.ReadDataFrameAsync();
                    Assert.Equal(clientMessage, dataFrame.Data.ToArray());

                    await connection.SendResponseDataAsync(streamId, serverMessage, endStream: i == MessageCount - 1);
                }

                await clientCompleted.Task.WaitAsync(TestHelper.PassingTestTimeout);
            }, options: new GenericLoopbackOptions { UseSsl = useSsl });
        }

        [Theory]
        [MemberData(nameof(UseSsl_MemberData))]
        public async Task Connect_ServerDoesNotSupportExtendedConnect_ClientIncludesExceptionData(bool useSsl)
        {
            TaskCompletionSource clientCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();

                HttpRequestMessage request = CreateRequest(HttpMethod.Connect, uri, UseVersion, exactVersion: true);
                request.Headers.Protocol = "foo";

                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
                Assert.Equal(HttpRequestError.ExtendedConnectNotSupported, ex.HttpRequestError);

                clientCompleted.SetResult();
            },
            async server =>
            {
                try
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await clientCompleted.Task.WaitAsync(TestHelper.PassingTestTimeout);
                    });
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Ignoring exception {ex}");
                }
            }, options: new GenericLoopbackOptions { UseSsl = useSsl });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Connect_Http11Endpoint_Throws(bool useSsl)
        {
            using var server = new LoopbackServer(new LoopbackServer.Options
            {
                UseSsl = useSsl
            });

            await server.ListenAsync();

            TaskCompletionSource clientCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

            Task serverTask = Task.Run(async () =>
            {
                try
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        if (!useSsl)
                        {
                            byte[] http2GoAwayHttp11RequiredBytes = new byte[17] { 0, 0, 8, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 13 };

                            await connection.SendResponseAsync(http2GoAwayHttp11RequiredBytes);

                            await clientCompleted.Task.WaitAsync(TestHelper.PassingTestTimeout);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Ignoring exception {ex}");
                }
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();

                HttpRequestMessage request = CreateRequest(HttpMethod.Connect, server.Address, UseVersion, exactVersion: true);
                request.Headers.Protocol = "foo";

                Exception ex = await Assert.ThrowsAnyAsync<Exception>(() => client.SendAsync(request));
                clientCompleted.SetResult();
                if (useSsl)
                {
                    Assert.Equal(false, ex.Data["HTTP2_ENABLED"]);
                }
            });

            await new[] { serverTask, clientTask }.WhenAllOrAnyFailed().WaitAsync(TestHelper.PassingTestTimeout);
        }

        [Theory]
        [MemberData(nameof(UseSsl_MemberData))]
        public async Task Connect_ServerSideEOS_ReceivedByClient(bool useSsl)
        {
            var timeoutTcs = new CancellationTokenSource(TestHelper.PassingTestTimeout);
            var serverReceivedEOS = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await Http2LoopbackServerFactory.Singleton.CreateClientAndServerAsync(
                clientFunc: async uri =>
                {
                    var client = CreateHttpClient();
                    var request = CreateRequest(HttpMethod.Connect, uri, UseVersion, exactVersion: true);
                    request.Headers.Protocol = "foo";

                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutTcs.Token);
                    var responseStream = await response.Content.ReadAsStreamAsync(timeoutTcs.Token);

                    // receive server's EOS
                    Assert.Equal(0, await responseStream.ReadAsync(new byte[1], timeoutTcs.Token));

                    // send client's EOS
                    responseStream.Dispose();

                    // wait for "ack" from server
                    await serverReceivedEOS.Task.WaitAsync(timeoutTcs.Token);

                    // can dispose handler now
                    client.Dispose();
                },
                serverFunc: async server =>
                {
                    await using var connection = await ((Http2LoopbackServer)server).EstablishConnectionAsync(
                        new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);

                    // send server's EOS
                    await connection.SendResponseDataAsync(streamId, Array.Empty<byte>(), endStream: true);

                    // receive client's EOS "in response" to server's EOS
                    var eosFrame = Assert.IsType<DataFrame>(await connection.ReadFrameAsync(timeoutTcs.Token));
                    Assert.Equal(streamId, eosFrame.StreamId);
                    Assert.Equal(0, eosFrame.Data.Length);
                    Assert.True(eosFrame.EndStreamFlag);

                    serverReceivedEOS.SetResult();

                    // on handler dispose, client should shutdown the connection without sending additional frames
                    await connection.WaitForClientDisconnectAsync().WaitAsync(timeoutTcs.Token);
                },
                options: new GenericLoopbackOptions { UseSsl = useSsl });
        }

        [Theory]
        [MemberData(nameof(UseSsl_MemberData))]
        public async Task Connect_ClientSideEOS_ReceivedByServer(bool useSsl)
        {
            var timeoutTcs = new CancellationTokenSource(TestHelper.PassingTestTimeout);
            var serverReceivedRst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await Http2LoopbackServerFactory.Singleton.CreateClientAndServerAsync(
                clientFunc: async uri =>
                {
                    var client = CreateHttpClient();
                    var request = CreateRequest(HttpMethod.Connect, uri, UseVersion, exactVersion: true);
                    request.Headers.Protocol = "foo";

                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutTcs.Token);
                    var responseStream = await response.Content.ReadAsStreamAsync(timeoutTcs.Token);

                    // send client's EOS
                    // this will also send RST_STREAM as we didn't receive server's EOS before
                    responseStream.Dispose();

                    // wait for "ack" from server
                    await serverReceivedRst.Task.WaitAsync(timeoutTcs.Token);

                    // can dispose handler now
                    client.Dispose();
                },
                serverFunc: async server =>
                {
                    await using var connection = await ((Http2LoopbackServer)server).EstablishConnectionAsync(
                        new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });

                    (int streamId, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                    await connection.SendResponseHeadersAsync(streamId, endStream: false);

                    // receive client's EOS
                    var eosFrame = Assert.IsType<DataFrame>(await connection.ReadFrameAsync(timeoutTcs.Token));
                    Assert.Equal(streamId, eosFrame.StreamId);
                    Assert.Equal(0, eosFrame.Data.Length);
                    Assert.True(eosFrame.EndStreamFlag);

                    // receive client's RST_STREAM as we didn't send server's EOS before
                    var rstFrame = Assert.IsType<RstStreamFrame>(await connection.ReadFrameAsync(timeoutTcs.Token));
                    Assert.Equal(streamId, rstFrame.StreamId);

                    serverReceivedRst.SetResult();

                    // on handler dispose, client should shutdown the connection without sending additional frames
                    await connection.WaitForClientDisconnectAsync().WaitAsync(timeoutTcs.Token);
                },
                options: new GenericLoopbackOptions { UseSsl = useSsl });
        }
    }
}
