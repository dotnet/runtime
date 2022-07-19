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

namespace System.Net.WebSockets.Client.Tests
{
    public class ConnectTest_Http2 : ClientWebSocketTestBase
    {
        public ConnectTest_Http2(ITestOutputHelper output) : base(output) { }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69870", TestPlatforms.Browser)]
        public async Task ConnectAsync_VersionNotSupported_Throws()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var clientSocket = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    clientSocket.Options.HttpVersion = HttpVersion.Version20;
                    clientSocket.Options.HttpVersionPolicy = Http.HttpVersionPolicy.RequestVersionExact;
                    using var handler = new SocketsHttpHandler();
                    handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
                    Task t = clientSocket.ConnectAsync(uri, new HttpMessageInvoker(handler), cts.Token);
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69870", TestPlatforms.Browser)]
        public async Task ConnectAsync_VersionSupported_Success()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = HttpVersion.Version20;
                    cws.Options.HttpVersionPolicy = Http.HttpVersionPolicy.RequestVersionExact;
                    using var handler = new SocketsHttpHandler();
                    handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
                    await cws.ConnectAsync(uri, new HttpMessageInvoker(handler), cts.Token);
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
