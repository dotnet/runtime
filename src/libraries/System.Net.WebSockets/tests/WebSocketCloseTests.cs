// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public class WebSocketCloseTests
    {
        private readonly CancellationTokenSource? _cancellation;

        public WebSocketCloseTests()
        {
            if (!Debugger.IsAttached)
            {
                _cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            }
        }

        public CancellationToken CancellationToken => _cancellation?.Token ?? default;

        public static object[][] CloseStatuses = {
            new object[] { WebSocketCloseStatus.EndpointUnavailable },
            new object[] { WebSocketCloseStatus.InternalServerError },
            new object[] { WebSocketCloseStatus.InvalidMessageType},
            new object[] { WebSocketCloseStatus.InvalidPayloadData },
            new object[] { WebSocketCloseStatus.MandatoryExtension },
            new object[] { WebSocketCloseStatus.MessageTooBig },
            new object[] { WebSocketCloseStatus.NormalClosure },
            new object[] { WebSocketCloseStatus.PolicyViolation },
            new object[] { WebSocketCloseStatus.ProtocolError },
            new object[] { (WebSocketCloseStatus)1012 },  // ServiceRestart indicates that the server / service is restarting.
            new object[] { (WebSocketCloseStatus)1013 },  // TryAgainLater indicates that a temporary server condition forced blocking the client's request.
            new object[] { (WebSocketCloseStatus)1014 },  // BadGateway indicates that the server acting as gateway received an invalid response
        };

        [Theory]
        [MemberData(nameof(ClosesData))]
        public void WebSocketReceiveResult_WebSocketCloseStatus_Roundtrip(WebSocketCloseStatus closeStatus)
        {
            string closeStatusDescription = "closeStatus " + closeStatus.ToString();
            WebSocketReceiveResult wsrr = new WebSocketReceiveResult(42, WebSocketMessageType.Close, true, closeStatus, closeStatusDescription);
            Assert.Equal(42, wsrr.Count);
            Assert.Equal(closeStatus, wsrr.CloseStatus);
            Assert.Equal(closeStatusDescription, wsrr.CloseStatusDescription);
        }

        [Theory]
        [MemberData(nameof(ClosesData))]
        public async Task ReceiveAsync_ValidCloseStatus_Success(WebSocketCloseStatus closeStatus)
        {
            WebSocketTestStream stream = new();
            using (WebSocket server = WebSocket.CreateFromStream(stream, true, null, TimeSpan.FromSeconds(3)))
            using (WebSocket client = WebSocket.CreateFromStream(stream.Remote, false, null, TimeSpan.FromSeconds(3)))
            {
                Assert.NotNull(server);
                Assert.NotNull(client);

                string closeStatusDescription = "closeStatus " + closeStatus.ToString();
                await SendTextAsync(closeStatusDescription, server);
                var response = await ReceiveAsync(client);
                Assert.Equal(closeStatusDescription, response);

                string closed = "Closed";
                await SendTextAsync(closed, server);
                await server.CloseOutputAsync(closeStatus, closeStatusDescription, CancellationToken);

                var received = await ReceiveAsync(client);
                Assert.Equal(closed, received);
            }
        }

        private async Task<string> ReceiveAsync(WebSocket client)
        {
            var buffer = new byte[1024];
            ValueWebSocketReceiveResult result = await client.ReceiveAsync(buffer.AsMemory(), CancellationToken);

            Assert.True(result.EndOfMessage);
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);
            return Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count));
        }

        private ValueTask SendTextAsync(string text, WebSocket websocket)
        {
            WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage | WebSocketMessageFlags.DisableCompression;
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return websocket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, flags, CancellationToken);
        }
    }
}
