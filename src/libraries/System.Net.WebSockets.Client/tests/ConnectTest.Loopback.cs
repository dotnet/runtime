// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets are not supported on browser")]
    public abstract class ConnectTest_Loopback : ConnectTestBase
    {
        public ConnectTest_Loopback(ITestOutputHelper output) : base(output) { }

        // --- Loopback Echo Server "overrides" ---

        //[ActiveIssue("https://github.com/dotnet/runtime/issues/1895")]
        //[Theory, MemberData(nameof(UnavailableWebSocketServers))]
        //public Task ConnectAsync_NotWebSocketServer_ThrowsWebSocketExceptionWithMessage(Uri server, string exceptionMessage, WebSocketError errorCode)
        //    => RunClient_ConnectAsync_NotWebSocketServer_ThrowsWebSocketExceptionWithMessage(server, exceptionMessage, errorCode);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task EchoBinaryMessage_Success(bool useSsl) => RunEchoAsync(
            RunClient_EchoBinaryMessage_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task EchoTextMessage_Success(bool useSsl) => RunEchoAsync(
            RunClient_EchoTextMessage_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ConnectAsync_AddCustomHeaders_Success(bool useSsl) => RunEchoHeadersAsync(
            RunClient_ConnectAsync_AddCustomHeaders_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ConnectAsync_CookieHeaders_Success(bool useSsl) => RunEchoHeadersAsync(
            RunClient_ConnectAsync_CookieHeaders_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException(bool useSsl) => RunEchoAsync(
            RunClient_ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException, useSsl);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol(bool useSsl) => RunEchoAsync(
            RunClient_ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol, useSsl);

        // TODO: this test is HTTP/1.1 only
        //[Theory, MemberData(nameof(UseSsl_MemberData))]
        //public Task ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState(bool useSsl) => RunEchoAsync(
        //    RunClient_ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState, useSsl);
    }

    // --- HTTP/1.1 WebSocket loopback tests ---

    // TODO
    public abstract class ConnectTest_Loopback_Http11Only : ConnectTest_Loopback
    {
        public ConnectTest_Loopback_Http11Only(ITestOutputHelper output) : base(output) { }

        // TODO: this test is HTTP/1.1 only
        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState(bool useSsl) => RunEchoAsync(
            RunClient_ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState, useSsl);
    }

    public sealed class ConnectTest_Invoker_Loopback : ConnectTest_Loopback_Http11Only //!
    {
        public ConnectTest_Invoker_Loopback(ITestOutputHelper output) : base(output) { }
        protected override bool UseCustomInvoker => true;
    }

    public sealed class ConnectTest_HttpClient_Loopback : ConnectTest_Loopback_Http11Only //!
    {
        public ConnectTest_HttpClient_Loopback(ITestOutputHelper output) : base(output) { }
        protected override bool UseHttpClient => true;
    }

    // TODO
    public sealed class ConnectTest_SharedHandler_Loopback : ConnectTestBase //!
    {
        public ConnectTest_SharedHandler_Loopback(ITestOutputHelper output) : base(output) { }

        // TODO
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

        //[ActiveIssue("https://github.com/dotnet/runtime/issues/1895")]
        //[Theory, MemberData(nameof(UnavailableWebSocketServers))]
        //public Task ConnectAsync_NotWebSocketServer_ThrowsWebSocketExceptionWithMessage(Uri server, string exceptionMessage, WebSocketError errorCode)
        //    => RunClient_ConnectAsync_NotWebSocketServer_ThrowsWebSocketExceptionWithMessage(server, exceptionMessage, errorCode);

        [Fact]
        public Task EchoBinaryMessage_Success() => RunEchoAsync(
            RunClient_EchoBinaryMessage_Success, useSsl: false);

        [Fact]
        public Task EchoTextMessage_Success() => RunEchoAsync(
            RunClient_EchoTextMessage_Success, useSsl: false);

        [Fact]
        public Task ConnectAsync_AddCustomHeaders_Success() => RunEchoHeadersAsync(
            RunClient_ConnectAsync_AddCustomHeaders_Success, useSsl: false);

        [Fact]
        public Task ConnectAsync_CookieHeaders_Success() => RunEchoHeadersAsync(
            RunClient_ConnectAsync_CookieHeaders_Success, useSsl: false);

        /*[Fact]
        public Task ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException() => RunEchoAsync(
            RunClient_ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException, useSsl: false);*/

        [Fact]
        public Task ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol() => RunEchoAsync(
            RunClient_ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol, useSsl: false);

        [Fact]
        public Task ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState() => RunEchoAsync(
            RunClient_ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState, useSsl: false);
    }

    // --- HTTP/2 WebSocket loopback tests ---

    public sealed class ConnectTest_Invoker_Http2 : ConnectTest_Loopback
    {
        public ConnectTest_Invoker_Http2(ITestOutputHelper output) : base(output) { }
        protected override bool UseCustomInvoker => true;
        internal override Version HttpVersion => Net.HttpVersion.Version20;
    }

    /*public sealed class ConnectTest_HttpClient_Http2 : ConnectTest_Loopback
    {
        public ConnectTest_HttpClient_Http2(ITestOutputHelper output) : base(output) { }
        protected override bool UseHttpClient => true;
        internal override Version HttpVersion => Net.HttpVersion.Version20;
    }*/
}
