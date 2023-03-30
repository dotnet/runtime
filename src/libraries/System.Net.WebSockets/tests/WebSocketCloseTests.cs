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
        [MemberData(nameof(CloseStatuses))]
        public void WebSocketReceiveResult_WebSocketCloseStatus_Roundtrip(WebSocketCloseStatus closeStatus)
        {
            string closeStatusDescription = "closeStatus " + closeStatus.ToString();
            WebSocketReceiveResult wsrr = new WebSocketReceiveResult(42, WebSocketMessageType.Close, endOfMessage: true, closeStatus, closeStatusDescription);
            Assert.Equal(42, wsrr.Count);
            Assert.Equal(closeStatus, wsrr.CloseStatus);
            Assert.Equal(closeStatusDescription, wsrr.CloseStatusDescription);
        }

        [Theory]
        [MemberData(nameof(CloseStatuses))]
        public async Task ReceiveAsync_ValidCloseStatus_Success(WebSocketCloseStatus closeStatus)
        {
            byte[] receiveBuffer = new byte[1024];
            WebSocketTestStream stream = new();
            Encoding encoding = Encoding.UTF8;

            using (WebSocket server = WebSocket.CreateFromStream(stream, isServer: true, subProtocol: null, TimeSpan.FromSeconds(3)))
            using (WebSocket client = WebSocket.CreateFromStream(stream.Remote, isServer: false, subProtocol: null, TimeSpan.FromSeconds(3)))
            {
                Assert.NotNull(server);
                Assert.NotNull(client);

                // send something
                string hello = "Testing " + closeStatus.ToString();
                byte[] sendBytes = encoding.GetBytes(hello);
                await server.SendAsync(sendBytes.AsMemory(), WebSocketMessageType.Text, WebSocketMessageFlags.None, CancellationToken);

                // and then server-side close with the test status
                string closeStatusDescription = "CloseStatus " + closeStatus.ToString();
                await server.CloseOutputAsync(closeStatus, closeStatusDescription, CancellationToken);

                // get the hello from the client (after the close message was sent)
                WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);
                string response = encoding.GetString(receiveBuffer.AsSpan(0, result.Count));
                Assert.Equal(hello, response);

                // now look for the expected close status
                WebSocketReceiveResult closing = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken);
                Assert.Equal(WebSocketMessageType.Close, closing.MessageType);
                Assert.Equal(closeStatus, closing.CloseStatus);
                Assert.Equal(closeStatusDescription, closing.CloseStatusDescription);
            }
        }
    }
}
