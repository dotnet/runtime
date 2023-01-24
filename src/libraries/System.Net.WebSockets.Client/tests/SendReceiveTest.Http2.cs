// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public sealed class HttpClientSendReceiveTest_Http2 : SendReceiveTest_Http2
    {
        public HttpClientSendReceiveTest_Http2(ITestOutputHelper output) : base(output) { }

        protected override bool UseHttpClient => true;
    }

    public sealed class InvokerSendReceiveTest_Http2 : SendReceiveTest_Http2
    {
        public InvokerSendReceiveTest_Http2(ITestOutputHelper output) : base(output) { }

        protected override bool UseCustomInvoker => true;
    }

    public abstract class SendReceiveTest_Http2 : ClientWebSocketTestBase
    {
        public SendReceiveTest_Http2(ITestOutputHelper output) : base(output) { }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets is not supported on this platform")]
        public async Task ReceiveNoThrowAfterSend_NoSsl()
        {
            var serverMessage = new byte[] { 4, 5, 6 };
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = HttpVersion.Version20;
                    cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    await cws.ConnectAsync(uri, GetInvoker(), cts.Token);

                    await cws.SendAsync(new byte[] { 2, 3, 4 }, WebSocketMessageType.Binary, true, cts.Token);

                    var readBuffer = new byte[serverMessage.Length];
                    await cws.ReceiveAsync(readBuffer, cts.Token);
                    Assert.Equal(serverMessage, readBuffer);
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });
                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                // send status 200 OK to establish websocket
                await connection.SendResponseHeadersAsync(streamId, endStream: false).ConfigureAwait(false);

                // send reply
                byte binaryMessageType = 2;
                var prefix = new byte[] { binaryMessageType, (byte)serverMessage.Length };
                byte[] constructMessage = prefix.Concat(serverMessage).ToArray();
                await connection.SendResponseDataAsync(streamId, constructMessage, endStream: false);

            }, new Http2Options() { WebSocketEndpoint = true, UseSsl = false });
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Self-signed certificates are not supported on browser")]
        public async Task ReceiveNoThrowAfterSend_WithSsl()
        {
            var serverMessage = new byte[] { 4, 5, 6 };
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = HttpVersion.Version20;
                    cws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    await cws.ConnectAsync(uri, GetInvoker(), cts.Token);

                    await cws.SendAsync(new byte[] { 2, 3, 4 }, WebSocketMessageType.Binary, true, cts.Token);

                    var readBuffer = new byte[serverMessage.Length];
                    await cws.ReceiveAsync(readBuffer, cts.Token);
                    Assert.Equal(serverMessage, readBuffer);
                }
            },
            async server =>
            {
                Http2LoopbackConnection connection = await server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.EnableConnect, Value = 1 });
                (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync(readBody: false);
                // send status 200 OK to establish websocket
                await connection.SendResponseHeadersAsync(streamId, endStream: false).ConfigureAwait(false);

                // send reply
                byte binaryMessageType = 2;
                var prefix = new byte[] { binaryMessageType, (byte)serverMessage.Length };
                byte[] constructMessage = prefix.Concat(serverMessage).ToArray();
                await connection.SendResponseDataAsync(streamId, constructMessage, endStream: false);

            }, new Http2Options() { WebSocketEndpoint = true });
        }
    }
}
