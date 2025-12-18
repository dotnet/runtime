// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    public abstract partial class SendReceiveTest_Http2Loopback
    {
        #region HTTP/2-only loopback tests

        [Fact]
        public async Task ReceiveNoThrowAfterSend_NoSsl()
        {
            var serverMessage = new byte[] { 4, 5, 6 };
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = Net.HttpVersion.Version20;
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
        public async Task ReceiveNoThrowAfterSend_WithSsl()
        {
            var serverMessage = new byte[] { 4, 5, 6 };
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var cws = new ClientWebSocket())
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    cws.Options.HttpVersion = Net.HttpVersion.Version20;
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

        #endregion
    }
}
