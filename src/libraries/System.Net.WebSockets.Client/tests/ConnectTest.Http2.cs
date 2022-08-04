// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

using static System.Net.Http.Functional.Tests.TestHelper;

namespace System.Net.WebSockets.Client.Tests
{
    public sealed class InvokerConnectTestt_Http2 : ConnectTest_Http2
    {
        public InvokerConnectTestt_Http2(ITestOutputHelper output) : base(output) { }

        protected override Task ConnectAsync(ClientWebSocket cws, Uri uri, CancellationToken cancellationToken) =>
            cws.ConnectAsync(uri, new HttpMessageInvoker(CreateSocketsHttpHandler(allowAllCertificates: true)), cancellationToken);
    }

    public sealed class HttpClientConnectTestt_Http2 : ConnectTest_Http2
    {
        public HttpClientConnectTestt_Http2(ITestOutputHelper output) : base(output) { }

        protected override Task ConnectAsync(ClientWebSocket cws, Uri uri, CancellationToken cancellationToken) =>
            cws.ConnectAsync(uri, new HttpClient(CreateSocketsHttpHandler(allowAllCertificates: true)), cancellationToken);
    }

    public abstract class ConnectTest_Http2 : ClientWebSocketTestBase
    {
        public ConnectTest_Http2(ITestOutputHelper output) : base(output) { }

        protected abstract Task ConnectAsync(ClientWebSocket cws, Uri uri, CancellationToken cancellationToken);

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Self-signed certificates are not supported on browser")]
        public async Task ConnectAsync_VersionNotSupported_Throws()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientSocket.Options.HttpVersion = HttpVersion.Version20;
                    clientSocket.Options.HttpVersionPolicy = Http.HttpVersionPolicy.RequestVersionExact;
                    using var handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                    Task t = ConnectAsync(clientSocket, uri, cts.Token);
                    var ex = await Assert.ThrowsAnyAsync<WebSocketException>(() => t);
                    Assert.IsType<HttpRequestException>(ex.InnerException);
                    Assert.True(ex.InnerException.Data.Contains("SETTINGS_ENABLE_CONNECT_PROTOCOL"));
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 0 });
            }, new Http2Options() { WebSocketEndpoint = true }
            );
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Self-signed certificates are not supported on browser")]
        public async Task ConnectAsync_VersionSupported_Success()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = HttpVersion.Version20;
                    cws.Options.HttpVersionPolicy = Http.HttpVersionPolicy.RequestVersionExact;
                    await ConnectAsync(cws, uri, cts.Token);
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });
                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody : false);
                await connection.SendResponseHeadersAsync(streamId, endStream: false, HttpStatusCode.OK);
            }, new Http2Options() { WebSocketEndpoint = true }
            );
        }
    }
}
