// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

using Windows.Web;
using RTWebSocketError = Windows.Networking.Sockets.WebSocketError;

namespace System.Net.WebSockets
{
    public sealed partial class ClientWebSocket : WebSocket
    {
        private async Task ConnectAsyncCore(Uri uri, CancellationToken cancellationToken)
        {
            var rtWebSocket = new WinRTWebSocket();
            _innerWebSocket = rtWebSocket;

            try
            {
                // Change internal state to 'connected' to enable the other methods
                if ((InternalState)Interlocked.CompareExchange(ref _state, (int)InternalState.Connected, (int)InternalState.Connecting) != InternalState.Connecting)
                {
                    // Aborted/Disposed during connect.
                    throw new ObjectDisposedException(GetType().FullName);
                }

                await rtWebSocket.ConnectAsync(uri, cancellationToken, Options);
            }
            catch (Exception ex)
            {
                WebErrorStatus status = RTWebSocketError.GetStatus(ex.HResult);
                var inner = new Exception(status.ToString(), ex);
                WebSocketException wex = new WebSocketException(SR.net_webstatus_ConnectFailure, inner);
                if (Logging.On)
                {
                    Logging.Exception(Logging.WebSockets, this, "ConnectAsync", wex);
                }

                throw wex;
            }
        }
    }
}