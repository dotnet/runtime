// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public sealed class InvokerConnectTest_Http2 : ConnectTest_Http2
    {
        public InvokerConnectTest_Http2(ITestOutputHelper output) : base(output) { }

        protected override bool UseCustomInvoker => true;
    }

    public sealed class HttpClientConnectTest_Http2 : ConnectTest_Http2
    {
        public HttpClientConnectTest_Http2(ITestOutputHelper output) : base(output) { }

        protected override bool UseHttpClient => true;
    }

    public sealed class HttpClientConnectTest_Http2_NoInvoker : ClientWebSocketTestBase
    {
        public HttpClientConnectTest_Http2_NoInvoker(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> ConnectAsync_Http2WithNoInvoker_ThrowsArgumentException_MemberData()
        {
            yield return Options(options => options.HttpVersion = HttpVersion.Version20);
            yield return Options(options => options.HttpVersion = HttpVersion.Version30);
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
    }

    public abstract class ConnectTest_Http2 : ClientWebSocketTestBase
    {
        public ConnectTest_Http2(ITestOutputHelper output) : base(output) { }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets is not supported on this platform")]
        public async Task ConnectAsync_VersionNotSupported_NoSsl_Throws()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = HttpVersion.Version20;
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
        [SkipOnPlatform(TestPlatforms.Browser, "Self-signed certificates are not supported on browser")]
        public async Task ConnectAsync_VersionNotSupported_WithSsl_Throws()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = HttpVersion.Version20;
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

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets is not supported on this platform")]
        public async Task ConnectAsync_Http11Server_DowngradeFail()
        {
            using (var cws = new ClientWebSocket())
            using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
            {
                cws.Options.HttpVersion = HttpVersion.Version20;
                cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                Task t = cws.ConnectAsync(Test.Common.Configuration.WebSockets.SecureRemoteEchoServer, GetInvoker(), cts.Token);

                var ex = await Assert.ThrowsAnyAsync<WebSocketException>(() => t);
                Assert.True(ex.InnerException.Data.Contains("HTTP2_ENABLED"));
                HttpRequestException inner = Assert.IsType<HttpRequestException>(ex.InnerException);
                HttpRequestError expectedError = PlatformDetection.SupportsAlpn ?
                    HttpRequestError.SecureConnectionError :
                    HttpRequestError.VersionNegotiationError;
                Assert.Equal(expectedError, inner.HttpRequestError);
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory]
        [MemberData(nameof(EchoServers))]
        [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets is not supported on this platform")]
        public async Task ConnectAsync_Http11Server_DowngradeSuccess(Uri server)
        {
            using (var cws = new ClientWebSocket())
            using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
            {
                cws.Options.HttpVersion = HttpVersion.Version20;
                cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
                await cws.ConnectAsync(server, GetInvoker(), cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets is not supported on this platform")]
        public async Task ConnectAsync_VersionSupported_NoSsl_Success()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = HttpVersion.Version20;
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
        [SkipOnPlatform(TestPlatforms.Browser, "Self-signed certificates are not supported on browser")]
        public async Task ConnectAsync_VersionSupported_WithSsl_Success()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = HttpVersion.Version20;
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
        [SkipOnPlatform(TestPlatforms.Browser, "HTTP/2 WebSockets aren't supported on Browser")]
        public async Task ConnectAsync_SameHttp2ConnectionUsedForMultipleWebSocketConnection()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using var cws1 = new ClientWebSocket();
                cws1.Options.HttpVersion = HttpVersion.Version20;
                cws1.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                using var cws2 = new ClientWebSocket();
                cws2.Options.HttpVersion = HttpVersion.Version20;
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
    }
}
