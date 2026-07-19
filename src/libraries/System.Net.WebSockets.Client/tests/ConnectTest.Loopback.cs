// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets are not supported on browser")]
    public abstract class ConnectTest_LoopbackBase(ITestOutputHelper output) : ConnectTestBase(output)
    {
        #region Common (Echo Server) tests

        [Theory, MemberData(nameof(UseSsl))]
        public Task EchoBinaryMessage_Success(bool useSsl) => RunEchoAsync(
            RunClient_EchoBinaryMessage_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task EchoTextMessage_Success(bool useSsl) => RunEchoAsync(
            RunClient_EchoTextMessage_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task ConnectAsync_AddCustomHeaders_Success(bool useSsl) => RunEchoHeadersAsync(
            RunClient_ConnectAsync_AddCustomHeaders_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task ConnectAsync_CookieHeaders_Success(bool useSsl) => RunEchoHeadersAsync(
            RunClient_ConnectAsync_CookieHeaders_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException(bool useSsl) => RunEchoAsync(
            RunClient_ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketException, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol(bool useSsl) => RunEchoAsync(
            RunClient_ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol, useSsl);

        [ConditionalTheory] // Uses SkipTestException
        [MemberData(nameof(UseSsl))]
        public Task ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState(bool useSsl) => RunEchoAsync(
            RunClient_ConnectAndCloseAsync_UseProxyServer_ExpectedClosedState, useSsl);

        #endregion
    }

    public abstract class ConnectTest_Loopback(ITestOutputHelper output) : ConnectTest_LoopbackBase(output)
    {
        #region HTTP/1.1-only loopback tests

        [ConditionalTheory] // Uses SkipTestException
        [MemberData(nameof(UseSsl))]
        public async Task ConnectAsync_Http11WithRequestVersionOrHigher_Loopback_DowngradeSuccess(bool useSsl)
        {
            if (UseSharedHandler)
            {
                throw new SkipTestException("HTTP/2 is not supported with SharedHandler");
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    using (var cws = new ClientWebSocket())
                    using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                    {
                        cws.Options.HttpVersion = Net.HttpVersion.Version11;
                        cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                        Task connectTask = cws.ConnectAsync(url, GetInvoker(), cts.Token);

                        await server.AcceptConnectionAsync(async connection =>
                        {
                            await LoopbackHelper.WebSocketHandshakeAsync(connection);
                        });

                        await connectTask;
                        Assert.Equal(WebSocketState.Open, cws.State);
                    }
                }, new LoopbackServer.Options { UseSsl = useSsl, WebSocketEndpoint = true });
        }

        #endregion
    }

    public abstract partial class ConnectTest_Http2Loopback(ITestOutputHelper output) : ConnectTest_LoopbackBase(output)
    {
        internal override Version HttpVersion => Net.HttpVersion.Version20;

        // #region HTTP/2-only loopback tests -> extracted to ConnectTest.Http2.cs
    }

    #region Runnable test classes: HTTP/1.1 Loopback

    public sealed partial class ConnectTest_SharedHandler_Loopback(ITestOutputHelper output) : ConnectTest_Loopback(output)
    {
        // #region SharedHandler-only HTTP/1.1 loopback tests -> extracted to ConnectTest.SharedHandler.cs
    }

    public sealed partial class ConnectTest_Invoker_Loopback(ITestOutputHelper output) : ConnectTest_Loopback(output)
    {
        protected override bool UseCustomInvoker => true;

        // #region Invoker-only HTTP/1.1 loopback tests -> extracted to ConnectTest.Invoker.cs
    }

    public sealed class ConnectTest_HttpClient_Loopback(ITestOutputHelper output) : ConnectTest_Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }

    #endregion

    #region Runnable test classes: HTTP/2 Loopback

    public sealed class ConnectTest_Invoker_Http2Loopback(ITestOutputHelper output) : ConnectTest_Http2Loopback(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class ConnectTest_HttpClient_Http2Loopback(ITestOutputHelper output) : ConnectTest_Http2Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }

    #endregion
}
