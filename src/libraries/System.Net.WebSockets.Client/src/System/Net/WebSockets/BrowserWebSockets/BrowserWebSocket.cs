// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    /// <summary>
    /// Provides a client for connecting to WebSocket services.
    /// </summary>
    internal sealed class BrowserWebSocket : WebSocket
    {
        private readonly object _lockObject = new object();

        private WebSocketCloseStatus? _closeStatus;
        private string? _closeStatusDescription;
        private JSObject? _innerWebSocket;
        private WebSocketState _state;
        private bool _closeSent;
        private bool _closeReceived;
        private bool _disposed;
        private bool _aborted;
        private bool _cancelled;
        private int[] responseStatus = new int[3];
        private MemoryHandle? responseStatusHandle;

        #region Properties

        public override WebSocketState State
        {
            get
            {
                lock (_lockObject)
                {
                    if (_innerWebSocket == null || _disposed || _state == WebSocketState.Aborted || _state == WebSocketState.Closed)
                    {
                        return _state;
                    }
                    var st = GetReadyState(_innerWebSocket!);
                    if (st == WebSocketState.Closed || st == WebSocketState.CloseSent)
                    {
                        if (_closeReceived && _closeSent)
                        {
                            st = WebSocketState.Closed;
                        }
                        else if (_closeReceived && !_closeSent)
                        {
                            st = WebSocketState.CloseReceived;
                        }
                        else if (!_closeReceived && _closeSent)
                        {
                            st = WebSocketState.CloseSent;
                        }
                    }
                    return FastState = st;
                } // lock
            }
        }

        private WebSocketState FastState
        {
            get
            {
                lock (_lockObject)
                {
                    return _state;
                } // lock
            }
            set
            {
                lock (_lockObject)
                {
                    _state = value;
                } // lock
            }
        }

        public override WebSocketCloseStatus? CloseStatus
        {
            get
            {
                lock (_lockObject)
                {
                    if (_closeStatus != null)
                    {
                        return _closeStatus;
                    }
                    if (_disposed || _aborted || _cancelled)
                    {
                        return null;
                    }
                    return GetCloseStatus();
                }
            }
        }

        public override string? CloseStatusDescription
        {
            get
            {
                lock (_lockObject)
                {
                    if (_closeStatusDescription != null)
                    {
                        return _closeStatusDescription;
                    }
                    if (_disposed || _aborted || _cancelled)
                    {
                        return null;
                    }
                    return GetCloseStatusDescription();
                }
            }
        }

        public override string? SubProtocol
        {
            get
            {
                ThrowIfDisposed();
                lock (_lockObject)
                {
                    if (_innerWebSocket == null) throw new InvalidOperationException(SR.net_WebSockets_NotConnected);
                    return BrowserInterop.GetProtocol(_innerWebSocket);
                } // lock
            }
        }

        #endregion Properties

        internal Task ConnectAsync(Uri uri, List<string>? requestedSubProtocols, CancellationToken cancellationToken)
        {
            lock (_lockObject)
            {
                if (FastState != WebSocketState.None)
                {
                    throw new InvalidOperationException(SR.net_WebSockets_AlreadyStarted);
                }
                ThrowIfDisposed();
                FastState = WebSocketState.Connecting;
            } // lock
            CreateCore(uri, requestedSubProtocols);
            return ConnectAsyncCore(cancellationToken);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // fast check of previous _state instead of GetReadyState(), the readyState would be validated on JS side
            var fastState = FastState;
            if (fastState != WebSocketState.Open && fastState != WebSocketState.CloseReceived)
            {
                throw new InvalidOperationException(SR.net_WebSockets_NotConnected);
            }

            if (messageType != WebSocketMessageType.Binary && messageType != WebSocketMessageType.Text)
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

            ThrowIfDisposed();
            return SendAsyncCore(buffer, messageType, endOfMessage, cancellationToken, fastState);
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromException<WebSocketReceiveResult>(new OperationCanceledException(cancellationToken));
            }

            // fast check of previous _state instead of GetReadyState(), the readyState would be validated on JS side
            var fastState = FastState;
            if (fastState != WebSocketState.Open && fastState != WebSocketState.CloseSent)
            {
                throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, fastState, "Open, CloseSent"));
            }

            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

            ThrowIfDisposed();
            return ReceiveAsyncCore(buffer, cancellationToken, fastState);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
            var fastState = FastState;
            if (fastState == WebSocketState.None || fastState == WebSocketState.Closed)
            {
                throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, fastState, "Connecting, Open, CloseSent, Aborted"));
            }

            lock (_lockObject)
            {
                var state = State;
                if (state == WebSocketState.None || state == WebSocketState.Closed)
                {
                    throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, state, "Connecting, Open, CloseSent, Aborted"));
                }
                if (_closeSent)
                {
                    return Task.CompletedTask;
                }
            } // lock
            return CloseAsyncCore(closeStatus, statusDescription, false, cancellationToken, fastState);
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);

            var fastState = FastState;
            if (fastState == WebSocketState.None || fastState == WebSocketState.Closed)
            {
                throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, fastState, "Connecting, Open, CloseSent, Aborted"));
            }
            WebSocketState state;
            lock (_lockObject)
            {
                state = State;
                if (state == WebSocketState.Closed)
                {
                    return Task.CompletedTask;
                }
                if (state == WebSocketState.None || state == WebSocketState.Closed)
                {
                    throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, state, "Connecting, Open, CloseSent, Aborted"));
                }

            } // lock
            return CloseAsyncCore(closeStatus, statusDescription, state != WebSocketState.Aborted, cancellationToken, fastState);
        }

        public override void Abort()
        {
            WebSocketState state;
            lock (_lockObject)
            {
                if (_disposed)
                {
                    return;
                }
                state = State;
            } // lock
            AbortCore(this, state);
        }

        public override void Dispose()
        {
            WebSocketState state;
            lock (_lockObject)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                state = State;
            } // lock

            if (state < WebSocketState.Closed && state != WebSocketState.None)
            {
                AbortCore(this, state);
            }
            if (state != WebSocketState.Aborted)
            {
                FastState = WebSocketState.Closed;
            }
            _innerWebSocket?.Dispose();
            responseStatusHandle?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            lock (_lockObject)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
            } // lock
        }

        private static void AbortCore(BrowserWebSocket self, WebSocketState currentState)
        {
            lock (self._lockObject)
            {
                self.FastState = WebSocketState.Aborted;
                self._aborted = true;
                if (self._disposed || currentState == WebSocketState.Closed || currentState == WebSocketState.Aborted)
                {
                    return;
                }
            }
            BrowserInterop.WebSocketAbort(self._innerWebSocket!);
        }

        private void CreateCore(Uri uri, List<string>? requestedSubProtocols)
        {
            try
            {
                string[]? subProtocols = requestedSubProtocols?.ToArray();

                Memory<int> responseMemory = new Memory<int>(responseStatus);

                responseStatusHandle = responseMemory.Pin();
                _innerWebSocket = BrowserInterop.UnsafeCreate(uri.ToString(), subProtocols, responseStatusHandle.Value);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private async Task ConnectAsyncCore(CancellationToken cancellationToken)
        {
            lock (_lockObject)
            {
                if (_aborted)
                {
                    FastState = WebSocketState.Closed;
                    throw new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure);
                }
                ThrowIfDisposed();
            } // lock

            try
            {
                var openTask = BrowserInterop.WebSocketOpen(_innerWebSocket!);

                await CancellationHelper(openTask!, cancellationToken, WebSocketState.Connecting).ConfigureAwait(false);

                lock (_lockObject)
                {
                    WebSocketState state = State;
                    if (state == WebSocketState.Connecting)
                    {
                        FastState = WebSocketState.Open;
                    }
                } // lock
            }
            catch (OperationCanceledException ex)
            {
                lock (_lockObject)
                {
                    FastState = WebSocketState.Closed;
                    if (_aborted)
                    {
                        throw new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure, ex);
                    }
                } // lock

                throw;
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private async Task SendAsyncCore(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken, WebSocketState previousState)
        {
            try
            {
                Task? sendTask;
                if (buffer.Count == 0)
                {
                    sendTask = BrowserInterop.WebSocketSend(_innerWebSocket!, IntPtr.Zero, 0, (int)messageType, endOfMessage);
                    if (sendTask != null) // this is optimization for single-threaded build, see resolvedPromise() in web-socket.ts
                    {
                        await CancellationHelper(sendTask, cancellationToken, previousState).ConfigureAwait(false);
                    }
                }
                else
                {
                    Memory<byte> bufferMemory = buffer.AsMemory();
                    MemoryHandle pinBuffer = bufferMemory.Pin();
                    try
                    {
                        sendTask = BrowserInterop.UnsafeSend(_innerWebSocket!, pinBuffer, bufferMemory.Length, messageType, endOfMessage);
                        if (sendTask != null)  // this is optimization for single-threaded build, see resolvedPromise() in web-socket.ts
                        {
                            await CancellationHelper(sendTask, cancellationToken, previousState, pinBuffer).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        // must be after await!
                        pinBuffer.Dispose();
                    }
                }
            }
            catch (JSException ex)
            {
                if (ex.Message.StartsWith("InvalidState:"))
                {
                    throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, FastState, "Open"), ex);
                }
                throw new WebSocketException(WebSocketError.NativeError, ex);
            }
        }

        private async Task<WebSocketReceiveResult> ReceiveAsyncCore(ArraySegment<byte> buffer, CancellationToken cancellationToken, WebSocketState previousState)
        {
            Memory<byte> bufferMemory = buffer.AsMemory();
            MemoryHandle pinBuffer = bufferMemory.Pin();
            try
            {
                var receiveTask = BrowserInterop.ReceiveUnsafe(_innerWebSocket!, pinBuffer, bufferMemory.Length);

                if (receiveTask != null)  // this is optimization for single-threaded build, see resolvedPromise() in web-socket.ts
                {
                    await CancellationHelper(receiveTask, cancellationToken, previousState, pinBuffer).ConfigureAwait(false);
                }

                return ConvertResponse();
            }
            catch (JSException ex)
            {
                if (ex.Message.StartsWith("InvalidState:"))
                {
                    throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, previousState, "Open, CloseSent"), ex);
                }
                throw new WebSocketException(WebSocketError.NativeError, ex);
            }
            finally
            {
                // must be after await!
                pinBuffer.Dispose();
            }
        }

        private WebSocketReceiveResult ConvertResponse()
        {
            const int countIndex = 0;
            const int typeIndex = 1;
            const int endIndex = 2;

            int count;
            WebSocketMessageType messageType;
            bool isEnd = responseStatus[endIndex] != 0;
            lock (_lockObject)
            {
                messageType = (WebSocketMessageType)responseStatus[typeIndex];
                count = responseStatus[countIndex];
                if (messageType == WebSocketMessageType.Close)
                {
                    _closeReceived = true;
                    FastState = _closeSent ? WebSocketState.Closed : WebSocketState.CloseReceived;
                }
            } // lock

            if (messageType == WebSocketMessageType.Close)
            {
                ForceReadCloseStatus();
                switch (_closeStatus ?? WebSocketCloseStatus.NormalClosure)
                {
                    case WebSocketCloseStatus.NormalClosure:
                    case WebSocketCloseStatus.Empty:
                        return new WebSocketReceiveResult(count, messageType, isEnd, _closeStatus, _closeStatusDescription);
                    case WebSocketCloseStatus.InvalidMessageType:
                    case WebSocketCloseStatus.InvalidPayloadData:
                        throw new WebSocketException(WebSocketError.InvalidMessageType, _closeStatusDescription);
                    case WebSocketCloseStatus.EndpointUnavailable:
                        throw new WebSocketException(WebSocketError.NotAWebSocket, _closeStatusDescription);
                    case WebSocketCloseStatus.ProtocolError:
                        throw new WebSocketException(WebSocketError.UnsupportedProtocol, _closeStatusDescription);
                    case WebSocketCloseStatus.InternalServerError:
                        throw new WebSocketException(WebSocketError.Faulted, _closeStatusDescription);
                    default:
                        throw new WebSocketException(WebSocketError.NativeError, (int)_closeStatus!.Value, _closeStatusDescription);
                }
            }
            return new WebSocketReceiveResult(count, messageType, isEnd);
        }

        private async Task CloseAsyncCore(WebSocketCloseStatus closeStatus, string? statusDescription, bool waitForCloseReceived, CancellationToken cancellationToken, WebSocketState previousState)
        {
            lock (_lockObject)
            {
                if (!_closeReceived)
                {
                    _closeStatus = closeStatus;
                    _closeStatusDescription = statusDescription;
                }
                _closeSent = true;
            }
            var closeTask = BrowserInterop.WebSocketClose(_innerWebSocket!, (int)closeStatus, statusDescription, waitForCloseReceived);
            if (closeTask != null) // this is optimization for single-threaded build, see resolvedPromise() in web-socket.ts
            {
                await CancellationHelper(closeTask, cancellationToken, previousState).ConfigureAwait(false);
            }
            if (waitForCloseReceived)
            {
                lock (_lockObject)
                {
                    _closeReceived = true;
                    ForceReadCloseStatus();
                    _ = State;
                }
            }
        }

        private async Task CancellationHelper(Task promise, CancellationToken cancellationToken, WebSocketState previousState, IDisposable? disposable = null)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                disposable?.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }
            if (promise.IsCompletedSuccessfully)
            {
                disposable?.Dispose();
                return;
            }
            try
            {
                using (var receiveRegistration = cancellationToken.Register(static s =>
                {
                    CancelablePromise.CancelPromise((Task)s!);
                }, promise))
                {
                    await promise.ConfigureAwait(false);
                    return;
                }
            }
            catch (JSException ex)
            {
                lock (_lockObject)
                {
                    var state = State;
                    if (state == WebSocketState.Aborted)
                    {
                        ForceReadCloseStatus();
                        throw new OperationCanceledException(nameof(WebSocketState.Aborted), ex);
                    }

                    if (state != WebSocketState.Closed && cancellationToken.IsCancellationRequested)
                    {
                        FastState = WebSocketState.Aborted;
                        _cancelled = true;
                        throw new OperationCanceledException(cancellationToken);
                    }
                    if (state != WebSocketState.Closed && ex.Message == "Error: OperationCanceledException")
                    {
                        FastState = WebSocketState.Aborted;
                        _cancelled = true;
                        throw new OperationCanceledException("The operation was cancelled.", ex, cancellationToken);
                    }
                    if (previousState == WebSocketState.Connecting)
                    {
                        ForceReadCloseStatus();
                        throw new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure, ex);
                    }
                    throw new WebSocketException(WebSocketError.NativeError, ex);
                }
            }
            finally
            {
                disposable?.Dispose();
            }
        }

        private void ForceReadCloseStatus()
        {
            lock (_lockObject)
            {
                if (!_disposed && _closeStatus == null)
                {
                    GetCloseStatus();
                    GetCloseStatusDescription();
                }
            }
        }

        private WebSocketCloseStatus? GetCloseStatus()
        {
            ThrowIfDisposed();
            var closeStatus = BrowserInterop.GetCloseStatus(_innerWebSocket);
            lock (_lockObject)
            {
                if (closeStatus != null && _closeStatus == null)
                {
                    _closeStatus = closeStatus;
                }
            }
            return _closeStatus;
        }

        private string? GetCloseStatusDescription()
        {
            ThrowIfDisposed();
            var closeStatusDescription = BrowserInterop.GetCloseStatusDescription(_innerWebSocket);
            lock (_lockObject)
            {
                if (closeStatusDescription != null && _closeStatusDescription == null)
                {
                    _closeStatusDescription = closeStatusDescription;
                }
            }
            return _closeStatusDescription;
        }

        private static WebSocketState GetReadyState(JSObject innerWebSocket)
        {
            var readyState = BrowserInterop.GetReadyState(innerWebSocket);
            // https://developer.mozilla.org/en-US/docs/Web/API/WebSocket/readyState
            return readyState switch
            {
                0 => WebSocketState.Connecting, // 0 (CONNECTING)
                1 => WebSocketState.Open, // 1 (OPEN)
                2 => WebSocketState.CloseSent, // 2 (CLOSING)
                3 => WebSocketState.Closed, // 3 (CLOSED)
                _ => WebSocketState.None
            };
        }

    }
}
