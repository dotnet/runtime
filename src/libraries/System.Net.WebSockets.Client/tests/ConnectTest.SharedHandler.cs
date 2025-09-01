// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    public partial class ConnectTest_SharedHandler_Loopback
    {
        #region SharedHandler-only HTTP/1.1 loopback tests

        [Fact]
        public async Task ConnectAsync_CancellationRequestedBeforeConnect_ThrowsOperationCanceledException()
        {
            using (var clientSocket = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                Task t = ConnectAsync(clientSocket, new Uri($"ws://{Guid.NewGuid():N}"), cts.Token);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }

        [Fact]
        public async Task ConnectAsync_CancellationRequestedInflightConnect_ThrowsOperationCanceledException()
        {
            using (var clientSocket = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource();
                Task t = ConnectAsync(clientSocket, new Uri($"ws://{Guid.NewGuid():N}"), cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }

        [Fact]
        public async Task ConnectAsync_NonStandardRequestHeaders_HeadersAddedWithoutValidation()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientSocket.Options.SetRequestHeader("Authorization", "AWS4-HMAC-SHA256 Credential=PLACEHOLDER /20190301/us-east-2/neptune-db/aws4_request, SignedHeaders=host;x-amz-date, Signature=b8155de54d9faab00000000000000000000000000a07e0d7dda49902e4d9202");
                    await ConnectAsync(clientSocket, uri, cts.Token);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Assert.NotNull(await LoopbackHelper.WebSocketHandshakeAsync(connection));
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [Fact]
        public async Task ConnectAsync_CancellationRequestedAfterConnect_ThrowsOperationCanceledException()
        {
            var releaseServer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                var clientSocket = new ClientWebSocket();
                try
                {
                    var cts = new CancellationTokenSource();
                    Task t = ConnectAsync(clientSocket, uri, cts.Token);
                    Assert.False(t.IsCompleted);
                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
                }
                finally
                {
                    releaseServer.SetResult();
                    clientSocket.Dispose();
                }
            }, async server =>
            {
                try
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await releaseServer.Task;
                    });
                }
                // Ignore IO exception on server as there are race conditions when client is cancelling.
                catch (IOException) { }
            }, new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [Fact]
        public async Task ConnectAsync_HttpResponseDetailsCollectedOnFailure()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientWebSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientWebSocket.Options.CollectHttpResponseDetails = true;
                    Task t = ConnectAsync(clientWebSocket, uri, cts.Token);
                    await Assert.ThrowsAnyAsync<WebSocketException>(() => t);

                    Assert.Equal(HttpStatusCode.Unauthorized, clientWebSocket.HttpStatusCode);
                    Assert.NotEmpty(clientWebSocket.HttpResponseHeaders);
                }
            }, server => server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [Fact]
        public async Task ConnectAsync_HttpResponseDetailsCollectedOnFailure_CustomHeader()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientWebSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientWebSocket.Options.CollectHttpResponseDetails = true;
                    Task t = ConnectAsync(clientWebSocket, uri, cts.Token);
                    await Assert.ThrowsAnyAsync<WebSocketException>(() => t);

                    Assert.Equal(HttpStatusCode.Unauthorized, clientWebSocket.HttpStatusCode);
                    Assert.NotEmpty(clientWebSocket.HttpResponseHeaders);
                    Assert.Contains("X-CustomHeader1", clientWebSocket.HttpResponseHeaders);
                    Assert.Contains("X-CustomHeader2", clientWebSocket.HttpResponseHeaders);
                    Assert.NotNull(clientWebSocket.HttpResponseHeaders.Values);
                }
            }, server => server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized, "X-CustomHeader1: Value1\r\nX-CustomHeader2: Value2\r\n"), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [Fact]
        public async Task ConnectAsync_HttpResponseDetailsCollectedOnSuccess_Extensions()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientWebSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientWebSocket.Options.CollectHttpResponseDetails = true;
                    await ConnectAsync(clientWebSocket, uri, cts.Token);

                    Assert.Equal(HttpStatusCode.SwitchingProtocols, clientWebSocket.HttpStatusCode);
                    Assert.NotEmpty(clientWebSocket.HttpResponseHeaders);
                    Assert.Contains("Sec-WebSocket-Extensions", clientWebSocket.HttpResponseHeaders);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Dictionary<string, string> headers = await LoopbackHelper.WebSocketHandshakeAsync(connection, "X-CustomHeader1");
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        [Fact]
        public async Task ConnectAsync_AddHostHeader_Success()
        {
            string expectedHost = null;
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                expectedHost = "subdomain." + uri.Host;
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.SetRequestHeader("Host", expectedHost);
                    await ConnectAsync(cws, uri, cts.Token);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Dictionary<string, string> headers = await LoopbackHelper.WebSocketHandshakeAsync(connection);
                Assert.NotNull(headers);
                Assert.True(headers.TryGetValue("Host", out string host));
                Assert.Equal(expectedHost, host);
            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        #endregion

        #region SharedHandler-only unsupported HTTP version tests
        public static IEnumerable<object[]> ConnectAsync_Http2WithNoInvoker_ThrowsArgumentException_MemberData()
        {
            yield return Options(options => options.HttpVersion = Net.HttpVersion.Version20);
            yield return Options(options => options.HttpVersion = Net.HttpVersion.Version30);
            yield return Options(options => options.HttpVersion = new Version(2, 1));
            yield return Options(options => options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher);

            static object[] Options(Action<ClientWebSocketOptions> configureOptions) =>
                new object[] { configureOptions };
        }

        [Theory]
        [MemberData(nameof(ConnectAsync_Http2WithNoInvoker_ThrowsArgumentException_MemberData))]
        [SkipOnPlatform(TestPlatforms.Browser, "HTTP/2 WebSockets aren't supported on Browser")]
        public async Task ConnectAsync_Http2WithNoInvoker_ThrowsArgumentException(Action<ClientWebSocketOptions> configureOptions)
        {
            using var ws = new ClientWebSocket();
            configureOptions(ws.Options);

            Task connectTask = ws.ConnectAsync(new Uri("wss://dummy"), CancellationToken.None);

            Assert.Equal(TaskStatus.Faulted, connectTask.Status);
            await Assert.ThrowsAsync<ArgumentException>("options", () => connectTask);
        }

        #endregion
    }
}
