// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Runtime.InteropServices.JavaScript;

using JavaScript = System.Runtime.InteropServices.JavaScript;

namespace System.Net.WebSockets
{
    // **Note** on `Task.ConfigureAwait(continueOnCapturedContext: true)` for the WebAssembly Browser.
    // The current implementation of WebAssembly for the Browser does not have a SynchronizationContext nor a Scheduler
    // thus forcing the callbacks to run on the main browser thread.  When threading is eventually implemented using
    // emscripten's threading model of remote worker threads, via SharedArrayBuffer, any API calls will have to be
    // remoted back to the main thread.  Most APIs only work on the main browser thread.
    // During discussions the concensus has been that it will not matter right now which value is used for ConfigureAwait
    // we should put this in place now.

    /// <summary>
    /// Provides a client for connecting to WebSocket services.
    /// </summary>
    internal sealed class BrowserWebSocket : WebSocket
    {
        private readonly Channel<ReceivePayload> _receiveMessageQueue = Channel.CreateUnbounded<ReceivePayload>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = true,
        });

        private TaskCompletionSource<bool>? _tcsClose;
        private WebSocketCloseStatus? _innerWebSocketCloseStatus;
        private string? _innerWebSocketCloseStatusDescription;

        private JSObject? _innerWebSocket;

        private Action<JSObject>? _onOpen;
        private Action<JSObject>? _onError;
        private Action<JSObject>? _onClose;
        private Action<JSObject>? _onMessage;

        private MemoryStream? _writeBuffer;
        private ReceivePayload? _bufferedPayload;
        private readonly CancellationTokenSource _cts;

        // Stages of this class.
        private int _state;
        private const int _created = 0;
        private const int _connecting = 1;
        private const int _connected = 2;
        private const int _disposed = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="System.Net.WebSockets.BrowserWebSocket"/> class.
        /// </summary>
        public BrowserWebSocket()
        {
            _cts = new CancellationTokenSource();
        }

        #region Properties

        /// <summary>
        /// Gets the WebSocket state of the <see cref="System.Net.WebSockets.BrowserWebSocket"/> instance.
        /// </summary>
        /// <value>The state.</value>
        public override WebSocketState State
        {
            get
            {
                if (_innerWebSocket != null && !_innerWebSocket.IsDisposed)
                {
                    return ReadyStateToDotNetState((int)_innerWebSocket.GetObjectProperty("readyState"));
                }
                return _state switch
                {
                    _created => WebSocketState.None,
                    _connecting => WebSocketState.Connecting,
                    _disposed => WebSocketState.Closed, // We only get here if disposed before connecting
                    _ => WebSocketState.Closed
                };
            }
        }

        private static WebSocketState ReadyStateToDotNetState(int readyState) =>
            // https://developer.mozilla.org/en-US/docs/Web/API/WebSocket/readyState
            readyState switch
            {
                0 => WebSocketState.Connecting, // 0 (CONNECTING)
                1 => WebSocketState.Open, // 1 (OPEN)
                2 => WebSocketState.CloseSent, // 2 (CLOSING)
                3 => WebSocketState.Closed, // 3 (CLOSED)
                _ => WebSocketState.None
            };

        public override WebSocketCloseStatus? CloseStatus => _innerWebSocket == null ? null : _innerWebSocketCloseStatus;

        public override string? CloseStatusDescription => _innerWebSocket == null ? null : _innerWebSocketCloseStatusDescription;

        public override string? SubProtocol => _innerWebSocket != null && !_innerWebSocket.IsDisposed ? _innerWebSocket!.GetObjectProperty("protocol")?.ToString() : null;

        #endregion Properties

        internal async Task ConnectAsyncJavaScript(Uri uri, CancellationToken cancellationToken, List<string>? requestedSubProtocols)
        {
            var tcsConnect = new TaskCompletionSource<bool>();

            // For Abort/Dispose.  Calling Abort on the request at any point will close the connection.
            _cts.Token.Register(AbortRequest);

            // Wrap the cancellationToken in a using so that it can be disposed of whether
            // we successfully connected or failed trying.
            // Otherwise any timeout/cancellation would apply to the full session.
            // In the failure case we need to release the references and dispose of the objects.
            using (cancellationToken.Register(() => tcsConnect.TrySetCanceled()))
            {
                try
                {
                    if (requestedSubProtocols?.Count > 0)
                    {
                        using (JavaScript.Array subProtocols = new JavaScript.Array())
                        {
                            foreach (var item in requestedSubProtocols)
                            {
                                subProtocols.Push(item);
                            }
                            _innerWebSocket = new HostObject("WebSocket", uri.ToString(), subProtocols);
                        }
                    }
                    else
                    {
                        _innerWebSocket = new HostObject("WebSocket", uri.ToString());
                    }
                    _innerWebSocket.SetObjectProperty("binaryType", "arraybuffer");

                    // Setup the onError callback
                    _onError = new Action<JSObject>((errorEvt) =>
                    {
                        errorEvt.Dispose();
                    });

                    // Attach the onError callback
                    _innerWebSocket.SetObjectProperty("onerror", _onError);

                    // Setup the onClose callback
                    _onClose = new Action<JSObject>((closeEvt) =>
                    {
                        _innerWebSocketCloseStatus = (WebSocketCloseStatus)closeEvt.GetObjectProperty("code");
                        _innerWebSocketCloseStatusDescription = closeEvt.GetObjectProperty("reason")?.ToString();
                        _receiveMessageQueue.Writer.TryWrite(new ReceivePayload(Array.Empty<byte>(), WebSocketMessageType.Close));

                        if (!tcsConnect.Task.IsCanceled && !tcsConnect.Task.IsCompleted && !tcsConnect.Task.IsFaulted)
                        {
                            tcsConnect.SetException(new WebSocketException(WebSocketError.NativeError));
                        }
                        else
                        {
                            _tcsClose?.SetResult(true);
                        }

                        closeEvt.Dispose();
                    });

                    // Attach the onClose callback
                    _innerWebSocket.SetObjectProperty("onclose", _onClose);

                    // Setup the onOpen callback
                    _onOpen = new Action<JSObject> ((evt) =>
                    {
                        using (evt)
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                // Change internal _state to '_connected' to enable the other methods
                                if (Interlocked.CompareExchange(ref _state, _connected, _connecting) != _connecting)
                                {
                                    // Aborted/Disposed during connect.
                                    throw new ObjectDisposedException(GetType().FullName);
                                }
                                tcsConnect.SetResult(true);
                            }
                        }
                    });

                    // Attach the onOpen callback
                    _innerWebSocket.SetObjectProperty("onopen", _onOpen);

                    // Setup the onMessage callback
                    _onMessage = new Action<JSObject>((messageEvent) =>
                    {
                        // get the events "data"
                        using (messageEvent)
                        {
                            ThrowIfNotConnected();
                            // If the messageEvent's data property is marshalled as a JSObject then we are dealing with
                            // binary data
                            var eventData = messageEvent.GetObjectProperty("data");
                            switch (eventData)
                            {
                                case ArrayBuffer buffer:
                                    using (buffer)
                                    {
                                        var mess = new ReceivePayload(buffer, WebSocketMessageType.Binary);
                                        _receiveMessageQueue.Writer.TryWrite(mess);
                                        break;
                                    }
                                case JSObject blobData:
                                    using (blobData)
                                    {
                                        Action<JSObject>? loadend = null;
                                        // Create a new "FileReader" object
                                        using (var reader = new HostObject("FileReader"))
                                        {
                                            loadend = (loadEvent) =>
                                            {
                                                using (loadEvent)
                                                using (var target = (JSObject)loadEvent.GetObjectProperty("target"))
                                                {
                                                    // https://developer.mozilla.org/en-US/docs/Web/API/FileReader/readyState
                                                    if ((int)target.GetObjectProperty("readyState") == 2) // DONE - The operation is complete.
                                                    {
                                                        using (var binResult = (ArrayBuffer)target.GetObjectProperty("result"))
                                                        {
                                                            var mess = new ReceivePayload(binResult, WebSocketMessageType.Binary);
                                                            _receiveMessageQueue.Writer.TryWrite(mess);
                                                            if (loadend != null)
                                                                JavaScript.Runtime.FreeObject(loadend);
                                                        }
                                                    }
                                                }
                                            };

                                            reader.Invoke("addEventListener", "loadend", loadend);
                                            reader.Invoke("readAsArrayBuffer", blobData);
                                        }
                                        break;
                                    }
                                case string message:
                                    {
                                        var mess = new ReceivePayload(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text);
                                        _receiveMessageQueue.Writer.TryWrite(mess);
                                        break;
                                    }
                                default:
                                    throw new NotImplementedException($"WebSocket bynary type '{_innerWebSocket.GetObjectProperty("binaryType").ToString()}' not supported.");
                            }
                        }
                    });

                    // Attach the onMessage callaback
                    _innerWebSocket.SetObjectProperty("onmessage", _onMessage);
                    await tcsConnect.Task.ConfigureAwait(continueOnCapturedContext: true);
                }
                catch (Exception wse)
                {
                    ConnectExceptionCleanup();
                    WebSocketException wex = new WebSocketException(SR.net_webstatus_ConnectFailure, wse);
                    throw wex;
                }
            }
        }

        private void ConnectExceptionCleanup()
        {
            Dispose();
        }

        public override void Dispose()
        {
            int priorState = Interlocked.Exchange(ref _state, _disposed);
            if (priorState == _disposed)
            {
                // No cleanup required.
                return;
            }

            // registered by the CancellationTokenSource cts in the connect method
            _cts.Cancel(false);
            _cts.Dispose();

            _writeBuffer?.Dispose();

            // We need to clear the events on websocket as well or stray events
            // are possible leading to crashes.
            if (_onClose != null)
            {
                _innerWebSocket?.SetObjectProperty("onclose", "");
                _onClose = null;
            }
            if (_onError != null)
            {
                _innerWebSocket?.SetObjectProperty("onerror", "");
                _onError = null;
            }
            if (_onOpen != null)
            {
                _innerWebSocket?.SetObjectProperty("onopen", "");
                _onOpen = null;
            }
            if (_onMessage != null)
            {
                _innerWebSocket?.SetObjectProperty("onmessage", "");
                _onMessage = null;
            }
            _innerWebSocket?.Dispose();
        }

        // This method is registered by the CancellationTokenSource cts in the connect method
        // and called by Dispose or Abort so that any open websocket connection can be closed.
        private async void AbortRequest()
        {
            if (State == WebSocketState.Open)
            {
                await CloseAsyncCore(WebSocketCloseStatus.NormalClosure, "Connection was aborted", CancellationToken.None).ConfigureAwait(continueOnCapturedContext: true);
            }
        }

        /// <summary>
        /// Send data on <see cref="System.Net.WebSockets.ClientWebSocket"/> as an asynchronous operation.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="buffer">Buffer.</param>
        /// <param name="messageType">Message type.</param>
        /// <param name="endOfMessage">If set to <c>true</c> end of message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();

            if (messageType != WebSocketMessageType.Binary &&
                    messageType != WebSocketMessageType.Text)
            {
                throw new ArgumentException(SR.Format(SR.net_WebSockets_Argument_InvalidMessageType,
                    messageType,
                    nameof(SendAsync),
                    WebSocketMessageType.Binary,
                    WebSocketMessageType.Text,
                    nameof(CloseOutputAsync)),
                    nameof(messageType));
            }

            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

            if (!endOfMessage)
            {
                _writeBuffer = _writeBuffer ?? new MemoryStream();
                _writeBuffer.Write(buffer.Array!, buffer.Offset, buffer.Count);
                return;
            }
            else
            {
                _writeBuffer = _writeBuffer ?? new MemoryStream();
                _writeBuffer.Write(buffer.Array!, buffer.Offset, buffer.Count);
                if (!_writeBuffer.TryGetBuffer(out buffer))
                    throw new WebSocketException(WebSocketError.NativeError);
            }

            var tcsSend = new TaskCompletionSource<bool>();
            // Wrap the cancellationToken in a using so that it can be disposed of whether
            // we successfully send or not.
            // Otherwise any timeout/cancellation would apply to the full session.
            var writtenBuffer = _writeBuffer;
            _writeBuffer = null;

            using (cancellationToken.Register(() => tcsSend.TrySetCanceled()))
            {
                try
                {
                    switch (messageType)
                    {
                        case WebSocketMessageType.Binary:
                            using (var uint8Buffer = Uint8Array.From(buffer))
                            {
                                _innerWebSocket!.Invoke("send", uint8Buffer);
                                tcsSend.SetResult(true);
                            }
                            break;
                        default:
                            string strBuffer = buffer.Array == null ? string.Empty : Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
                            _innerWebSocket!.Invoke("send", strBuffer);
                            tcsSend.SetResult(true);
                            break;
                    }
                }
                catch (Exception excb)
                {
                    tcsSend.TrySetException(new WebSocketException(WebSocketError.NativeError, excb));
                }
                finally
                {
                    writtenBuffer?.Dispose();
                }
                await tcsSend.Task.ConfigureAwait(continueOnCapturedContext: true);
            }
        }

        /// <summary>
        /// Receives data on <see cref="System.Net.WebSockets.ClientWebSocket"/> as an asynchronous operation.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="buffer">Buffer.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

            ThrowIfDisposed();
            ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseSent);
            _bufferedPayload ??= await _receiveMessageQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: true);

            try
            {
                var endOfMessage = _bufferedPayload.BufferPayload(buffer, out WebSocketReceiveResult receiveResult);
                if (endOfMessage)
                    _bufferedPayload = null;
                return receiveResult;
            }
            catch (Exception exc)
            {
                throw new WebSocketException(WebSocketError.NativeError, exc);
            }
        }

        /// <summary>
        /// Aborts the connection and cancels any pending IO operations.
        /// </summary>
        public override void Abort()
        {
            if (_state == _disposed)
            {
                return;
            }
            _state = (int)WebSocketState.Aborted;
            Dispose();
        }

        public override async Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _writeBuffer = null;
            _receiveMessageQueue.Writer.Complete();
            ThrowIfNotConnected();

            await CloseAsyncCore(closeStatus, statusDescription, cancellationToken).ConfigureAwait(continueOnCapturedContext: true);
        }

        private async Task CloseAsyncCore(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            ThrowOnInvalidState(State,
                WebSocketState.Open, WebSocketState.CloseReceived, WebSocketState.CloseSent);

            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);

            _tcsClose = new TaskCompletionSource<bool>();
            // Wrap the cancellationToken in a using so that it can be disposed of whether
            // we successfully connected or failed trying.
            // Otherwise any timeout/cancellation would apply to the full session.
            // In the failure case we need to release the references and dispose of the objects.
            using (cancellationToken.Register(() => _tcsClose.TrySetCanceled()))
            {
                _innerWebSocketCloseStatus = closeStatus;
                _innerWebSocketCloseStatusDescription = statusDescription;
                _innerWebSocket!.Invoke("close", (int)closeStatus, statusDescription);
                await _tcsClose.Task.ConfigureAwait(continueOnCapturedContext: true);
            }
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => throw new PlatformNotSupportedException();

        private void ThrowIfNotConnected()
        {
            if (_state == _disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else if (State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_state == _disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
