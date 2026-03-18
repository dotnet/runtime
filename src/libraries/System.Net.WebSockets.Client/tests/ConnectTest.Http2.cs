// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    public abstract partial class ConnectTest_Http2Loopback
    {
        #region HTTP/2-only loopback tests

        [Fact]
        public async Task ConnectAsync_VersionNotSupported_NoSsl_Throws()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = Net.HttpVersion.Version20;
                    cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    Task t = cws.ConnectAsync(uri, GetInvoker(), cts.Token);

                    var ex = await Assert.ThrowsAnyAsync<WebSocketException>(() => t);
                    HttpRequestException inner = Assert.IsType<HttpRequestException>(ex.InnerException);
                    Assert.Equal(HttpRequestError.ExtendedConnectNotSupported, inner.HttpRequestError);
                    Assert.True(ex.InnerException.Data.Contains("SETTINGS_ENABLE_CONNECT_PROTOCOL"));
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 0 });
            }, new Http2Options() { WebSocketEndpoint = true, UseSsl = false });
        }

        [Fact]
        public async Task ConnectAsync_VersionNotSupported_WithSsl_Throws()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = Net.HttpVersion.Version20;
                    cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    Task t = cws.ConnectAsync(uri, GetInvoker(), cts.Token);

                    var ex = await Assert.ThrowsAnyAsync<WebSocketException>(() => t);
                    HttpRequestException inner = Assert.IsType<HttpRequestException>(ex.InnerException);
                    Assert.Equal(HttpRequestError.ExtendedConnectNotSupported, inner.HttpRequestError);
                    Assert.True(ex.InnerException.Data.Contains("SETTINGS_ENABLE_CONNECT_PROTOCOL"));
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 0 });
            }, new Http2Options() { WebSocketEndpoint = true });
        }

        [Fact]
        public async Task ConnectAsync_VersionSupported_NoSsl_Success()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = Net.HttpVersion.Version20;
                    cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    await cws.ConnectAsync(uri, GetInvoker(), cts.Token);
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });
                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                await connection.SendResponseHeadersAsync(streamId, endStream: false, HttpStatusCode.OK);
            }, new Http2Options() { WebSocketEndpoint = true, UseSsl = false });
        }

        [Fact]
        public async Task ConnectAsync_VersionSupported_WithSsl_Success()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = Net.HttpVersion.Version20;
                    cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    await cws.ConnectAsync(uri, GetInvoker(), cts.Token);
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });
                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                await connection.SendResponseHeadersAsync(streamId, endStream: false, HttpStatusCode.OK);
            }, new Http2Options() { WebSocketEndpoint = true });
        }

        [Fact]
        public async Task ConnectAsync_SameHttp2ConnectionUsedForMultipleWebSocketConnection()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using var cws1 = new ClientWebSocket();
                cws1.Options.HttpVersion = Net.HttpVersion.Version20;
                cws1.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                using var cws2 = new ClientWebSocket();
                cws2.Options.HttpVersion = Net.HttpVersion.Version20;
                cws2.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                using var cts = new CancellationTokenSource(TimeOutMilliseconds);
                HttpMessageInvoker? invoker = GetInvoker();

                await cws1.ConnectAsync(uri, invoker, cts.Token);
                await cws2.ConnectAsync(uri, invoker, cts.Token);
            },
            async server =>
            {
                await using Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });

                (int streamId1, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                await connection.SendResponseHeadersAsync(streamId1, endStream: false, HttpStatusCode.OK);

                (int streamId2, _) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                await connection.SendResponseHeadersAsync(streamId2, endStream: false, HttpStatusCode.OK);
            }, new Http2Options() { WebSocketEndpoint = true, UseSsl = false });
        }

        #endregion
    }
}
