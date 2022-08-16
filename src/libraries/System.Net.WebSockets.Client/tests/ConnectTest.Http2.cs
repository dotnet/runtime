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
    public sealed class InvokerConnectTest_Http2 : ConnectTest_Http2
    {
        public InvokerConnectTest_Http2(ITestOutputHelper output) : base(output) { }

        protected override HttpMessageInvoker? GetInvoker() => new HttpClient(new HttpClientHandler());
    }

    public sealed class HttpClientConnectTest_Http2 : ConnectTest_Http2
    {
        public HttpClientConnectTest_Http2(ITestOutputHelper output) : base(output) { }

        protected override HttpMessageInvoker? GetInvoker() => new HttpClient(new HttpClientHandler());
    }

    public class ConnectTest_Http2 : ClientWebSocketTestBase
    {
        public ConnectTest_Http2(ITestOutputHelper output) : base(output) { }


        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets is not supported on this platform")]
        public async Task ConnectAsync_VersionNotSupported_NoSsl_Throws(bool useHandler)
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = HttpVersion.Version20;
                    cws.Options.HttpVersionPolicy = Http.HttpVersionPolicy.RequestVersionExact;
                    Task t;
                    if (useHandler)
                    {
                        var handler = new SocketsHttpHandler();
                        t = cws.ConnectAsync(uri, new HttpMessageInvoker(handler), cts.Token);
                    }
                    else
                    {
                        t = cws.ConnectAsync(uri, cts.Token);
                    }
                    var ex = await Assert.ThrowsAnyAsync<WebSocketException>(() => t);
                    Assert.IsType<HttpRequestException>(ex.InnerException);
                    Assert.True(ex.InnerException.Data.Contains("SETTINGS_ENABLE_CONNECT_PROTOCOL"));
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 0 });
            }, new Http2Options() { WebSocketEndpoint = true, UseSsl = false }
            );
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
                    cws.Options.HttpVersionPolicy = Http.HttpVersionPolicy.RequestVersionExact;
                    Task t;
                    var handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                    t = cws.ConnectAsync(uri, new HttpMessageInvoker(handler), cts.Token);

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

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory]
        [MemberData(nameof(SecureEchoServersAndBoolean))]
        [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets is not supported on this platform")]
        public async Task ConnectAsync_Http11Server_DowngradeFail(Uri server, bool useHandler)
        {
            using (var cws = new ClientWebSocket())
            using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
            {
                cws.Options.HttpVersion = HttpVersion.Version20;
                cws.Options.HttpVersionPolicy = Http.HttpVersionPolicy.RequestVersionExact;
                Task t;
                if (useHandler)
                {
                    var handler = new SocketsHttpHandler();
                    t = cws.ConnectAsync(server, new HttpMessageInvoker(handler), cts.Token);
                }
                else
                {
                    t = cws.ConnectAsync(server, cts.Token);
                }
                var ex = await Assert.ThrowsAnyAsync<WebSocketException>(() => t);
                Assert.IsType<HttpRequestException>(ex.InnerException);
                Assert.True(ex.InnerException.Data.Contains("HTTP2_ENABLED"));
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory]
        [MemberData(nameof(EchoServersAndBoolean))]
        [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets is not supported on this platform")]
        public async Task ConnectAsync_Http11Server_DowngradeSuccess(Uri server, bool useHandler)
        {
            using (var cws = new ClientWebSocket())
            using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
            {
                cws.Options.HttpVersion = HttpVersion.Version20;
                cws.Options.HttpVersionPolicy = Http.HttpVersionPolicy.RequestVersionOrLower;
                if (useHandler)
                {
                    var handler = new SocketsHttpHandler();
                    await cws.ConnectAsync(server, new HttpMessageInvoker(handler), cts.Token);
                }
                else
                {
                    await cws.ConnectAsync(server, cts.Token);
                }
                Assert.Equal(WebSocketState.Open, cws.State);
            }
        }


        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets is not supported on this platform")]
        public async Task ConnectAsync_VersionSupported_NoSsl_Success(bool useHandler)
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = HttpVersion.Version20;
                    cws.Options.HttpVersionPolicy = Http.HttpVersionPolicy.RequestVersionExact;
                    if (useHandler)
                    {
                        var handler = new SocketsHttpHandler();
                        await cws.ConnectAsync(uri, new HttpMessageInvoker(handler), cts.Token);
                    }
                    else
                    {
                        await cws.ConnectAsync(uri, cts.Token);
                    }
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });
                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                await connection.SendResponseHeadersAsync(streamId, endStream: false, HttpStatusCode.OK);
            }, new Http2Options() { WebSocketEndpoint = true, UseSsl = false }
            );
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
                    cws.Options.HttpVersionPolicy = Http.HttpVersionPolicy.RequestVersionExact;

                    var handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                    await cws.ConnectAsync(uri, new HttpMessageInvoker(handler), cts.Token);
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });
                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                await connection.SendResponseHeadersAsync(streamId, endStream: false, HttpStatusCode.OK);
            }, new Http2Options() { WebSocketEndpoint = true }
            );
        }
    }
}
