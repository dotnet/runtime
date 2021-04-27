// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed class WebSocketHandle
    {
        private readonly CancellationTokenSource _abortSource = new CancellationTokenSource();
        private WebSocketState _state = WebSocketState.Connecting;

        public WebSocket? WebSocket { get; private set; }
        public WebSocketState State => WebSocket?.State ?? _state;

        public static ClientWebSocketOptions CreateDefaultOptions() => new ClientWebSocketOptions();

        public void Dispose()
        {
            _state = WebSocketState.Closed;
            WebSocket?.Dispose();
        }

        public void Abort()
        {
            _abortSource.Cancel();
            WebSocket?.Abort();
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken, ClientWebSocketOptions options)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();  // avoid allocating a WebSocket object if cancellation was requested before connect
                CancellationTokenSource? linkedCancellation;
                CancellationTokenSource externalAndAbortCancellation;
                if (cancellationToken.CanBeCanceled) // avoid allocating linked source if external token is not cancelable
                {
                    linkedCancellation =
                        externalAndAbortCancellation =
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _abortSource.Token);
                }
                else
                {
                    linkedCancellation = null;
                    externalAndAbortCancellation = _abortSource;
                }

                using (linkedCancellation)
                {
                    WebSocket = new BrowserWebSocket();
                    await ((BrowserWebSocket)WebSocket).ConnectAsyncJavaScript(uri, externalAndAbortCancellation.Token, options.RequestedSubProtocols).ConfigureAwait(continueOnCapturedContext: true);
                    externalAndAbortCancellation.Token.ThrowIfCancellationRequested();
                }
            }
            catch (Exception exc)
            {
                if (_state < WebSocketState.Closed)
                {
                    _state = WebSocketState.Closed;
                }

                Abort();

                switch (exc) {
                    case WebSocketException:
                    case OperationCanceledException _ when cancellationToken.IsCancellationRequested:
                        throw;
                    default:
                        throw new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure, exc);
                }
            }
        }
    }
}
