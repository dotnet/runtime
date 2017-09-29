// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    /// <summary>
    /// ClientWebSocket tests that do require a remote server.
    /// </summary>
    public class ClientWebSocketTestBase
    {
        public static readonly object[][] EchoServers = System.Net.Test.Common.Configuration.WebSockets.EchoServers;
        public static readonly object[][] EchoHeadersServers = System.Net.Test.Common.Configuration.WebSockets.EchoHeadersServers;

        public const int TimeOutMilliseconds = 20000;
        public const int CloseDescriptionMaxLength = 123;
        public readonly ITestOutputHelper _output;

        public ClientWebSocketTestBase(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> UnavailableWebSocketServers
        {
            get
            {
                Uri server;

                // Unknown server.
                {
                    server = new Uri(string.Format("ws://{0}", Guid.NewGuid().ToString()));
                    yield return new object[] { server };
                }

                // Known server but not a real websocket endpoint.
                {
                    server = System.Net.Test.Common.Configuration.Http.RemoteEchoServer;
                    var ub = new UriBuilder("ws", server.Host, server.Port, server.PathAndQuery);

                    yield return new object[] { ub.Uri };
                }
            }
        }

        public async Task TestCancellation(Func<ClientWebSocket, Task> action, Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                try
                {
                    await action(cws);
                    // Operation finished before CTS expired.
                }
                catch (OperationCanceledException)
                {
                    // Expected exception
                    Assert.Equal(WebSocketState.Aborted, cws.State);
                }
                catch (ObjectDisposedException)
                {
                    // Expected exception
                    Assert.Equal(WebSocketState.Aborted, cws.State);
                }
                catch (WebSocketException exception)
                {
                    Assert.Equal(ResourceHelper.GetExceptionMessage(
                        "net_WebSockets_InvalidState_ClosedOrAborted",
                        "System.Net.WebSockets.InternalClientWebSocket",
                        "Aborted"),
                        exception.Message);

                    Assert.Equal(WebSocketError.InvalidState, exception.WebSocketErrorCode);
                    Assert.Equal(WebSocketState.Aborted, cws.State);
                }
            }
        }

        protected static async Task<WebSocketReceiveResult> ReceiveEntireMessageAsync(WebSocket ws, ArraySegment<byte> segment, CancellationToken cancellationToken)
        {
            int bytesReceived = 0;
            while (true)
            {
                WebSocketReceiveResult r = await ws.ReceiveAsync(segment, cancellationToken);
                if (r.EndOfMessage)
                {
                    return new WebSocketReceiveResult(bytesReceived + r.Count, r.MessageType, true, r.CloseStatus, r.CloseStatusDescription);
                }
                else
                {
                    bytesReceived += r.Count;
                    segment = new ArraySegment<byte>(segment.Array, segment.Offset + r.Count, segment.Count - r.Count);
                }
            }
        }

        public static bool WebSocketsSupported { get { return WebSocketHelper.WebSocketsSupported; } }
    }
}
