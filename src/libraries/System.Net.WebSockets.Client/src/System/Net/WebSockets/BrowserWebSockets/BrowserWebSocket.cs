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
        private bool _shouldAbort;
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
                    var st = GetReadyStateLocked(_innerWebSocket!);
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
                    return GetCloseStatusLocked();
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
                    return GetCloseStatusDescriptionLocked();
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
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();

                if (FastState != WebSocketState.None)
                {
                    throw new InvalidOperationException(SR.net_WebSockets_AlreadyStarted);
                }
                FastState = WebSocketState.Connecting;
            } // lock
            CreateCore(uri, requestedSubProtocols);
            return ConnectAsyncCore(cancellationToken);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            // this validation should be synchronous
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
            return SendAsyncCore(buffer, messageType, endOfMessage, cancellationToken);
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            // this validation should be synchronous
            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

            ThrowIfDisposed();
            return ReceiveAsyncCore(buffer, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            // this validation should be synchronous
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);

            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            return CloseAsyncCore(closeStatus, statusDescription, false, cancellationToken);
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            // this validation should be synchronous
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);

            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            return CloseAsyncCore(closeStatus, statusDescription, true, cancellationToken);
        }

        public override void Abort()
        {
            lock (_lockObject)
            {
                if (_disposed || _aborted)
                {
                    return;
                }
                var fastState = FastState;
                if (fastState == WebSocketState.Closed || fastState == WebSocketState.Aborted)
                {
                    return;
                }

                FastState = WebSocketState.Aborted;
                _aborted = true;

                // We can call this cross-thread from inside the lock, because there are no callbacks which would lock the same lock
                // This will reject/resolve some promises
                BrowserInterop.WebSocketAbort(_innerWebSocket!);
            }
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

                if (state < WebSocketState.Closed && state != WebSocketState.None)
                {
                    _shouldAbort = true;
                    FastState = WebSocketState.Aborted;
                }
                else if (state != WebSocketState.Aborted)
                {
                    FastState = WebSocketState.Closed;
                }

            } // lock

            static void Cleanup(object? _state)
            {
                var self = (BrowserWebSocket)_state!;
                var state = self.State;
                lock (self._lockObject)
                {
                    if (self._shouldAbort && !self._aborted)
                    {
                        self._aborted = true;
                        self._shouldAbort = false;

                        // We can call this inside the lock, because there are no callbacks which would lock the same lock
                        // This will reject/resolve some promises
                        BrowserInterop.WebSocketAbort(self._innerWebSocket!);
                    }
                }
                self._innerWebSocket?.Dispose();
                self.responseStatusHandle?.Dispose();
            }

#if FEATURE_WASM_THREADS
            // if this is finalizer thread, we need to postpone the abort -> dispose
            _innerWebSocket?.SynchronizationContext.Post(Cleanup, this);
#else
            Cleanup(this);
#endif
        }

        private void ThrowIfDisposed()
        {
            lock (_lockObject)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
            } // lock
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
            Task openTask;

            lock (_lockObject)
            {
                if (_aborted)
                {
                    FastState = WebSocketState.Closed;
                    throw new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure);
                }
                ThrowIfDisposed();

                openTask = BrowserInterop.WebSocketOpen(_innerWebSocket!);
            } // lock

            try
            {
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
                lock (_lockObject)
                {
                    FastState = WebSocketState.Closed;
                }
                Dispose();
                throw;
            }
        }

        private async Task SendAsyncCore(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            WebSocketState previousState = WebSocketState.None;
            Task? sendTask;
            MemoryHandle? pinBuffer = null;

            try
            {
                lock (_lockObject)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ThrowIfDisposed();

                    previousState = FastState;
                    if (previousState != WebSocketState.Open && previousState != WebSocketState.CloseReceived)
                    {
                        throw new InvalidOperationException(SR.net_WebSockets_NotConnected);
                    }

                    if (buffer.Count == 0)
                    {
                        sendTask = BrowserInterop.WebSocketSend(_innerWebSocket!, IntPtr.Zero, 0, (int)messageType, endOfMessage);
                    }
                    else
                    {
                        Memory<byte> bufferMemory = buffer.AsMemory();
                        pinBuffer = bufferMemory.Pin();
                        sendTask = BrowserInterop.UnsafeSend(_innerWebSocket!, pinBuffer.Value, bufferMemory.Length, messageType, endOfMessage);
                    }
                }

                if (sendTask != null)  // this is optimization for single-threaded build, see resolvedPromise() in web-socket.ts. Null means synchronously resolved.
                {
                    await CancellationHelper(sendTask, cancellationToken, previousState, pinBuffer).ConfigureAwait(false);
                }
            }
            catch (JSException ex)
            {
                if (ex.Message.StartsWith("InvalidState:"))
                {
                    throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, previousState, "Open"), ex);
                }
                throw new WebSocketException(WebSocketError.NativeError, ex);
            }
            finally
            {
                // must be after await!
                pinBuffer?.Dispose();
            }
        }

        private async Task<WebSocketReceiveResult> ReceiveAsyncCore(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            WebSocketState previousState = WebSocketState.None;
            Task? receiveTask;
            MemoryHandle? pinBuffer = null;
            try
            {
                lock (_lockObject)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ThrowIfDisposed();

                    previousState = FastState;
                    if (previousState != WebSocketState.Open && previousState != WebSocketState.CloseSent)
                    {
                        throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, previousState, "Open, CloseSent"));
                    }

                    Memory<byte> bufferMemory = buffer.AsMemory();
                    pinBuffer = bufferMemory.Pin();
                    receiveTask = BrowserInterop.ReceiveUnsafe(_innerWebSocket!, pinBuffer.Value, bufferMemory.Length);
                }

                if (receiveTask != null)  // this is optimization for single-threaded build, see resolvedPromise() in web-socket.ts. Null means synchronously resolved.
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
                pinBuffer?.Dispose();
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
                    ForceReadCloseStatusLocked();
                }
            } // lock

            if (messageType == WebSocketMessageType.Close)
            {
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

        private async Task CloseAsyncCore(WebSocketCloseStatus closeStatus, string? statusDescription, bool fullClose, CancellationToken cancellationToken)
        {
            Task? closeTask;
            WebSocketState previousState;
            lock (_lockObject)
            {
                cancellationToken.ThrowIfCancellationRequested();

                previousState = FastState;
                if (_aborted)
                {
                    return;
                }
                if (!_closeReceived)
                {
                    _closeStatus = closeStatus;
                    _closeStatusDescription = statusDescription;
                }
                if (previousState == WebSocketState.None || previousState == WebSocketState.Closed)
                {
                    throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, previousState, "Connecting, Open, CloseSent, Aborted"));
                }

                _closeSent = true;

                closeTask = BrowserInterop.WebSocketClose(_innerWebSocket!, (int)closeStatus, statusDescription, fullClose);
            }

            if (closeTask != null) // this is optimization for single-threaded build, see resolvedPromise() in web-socket.ts. Null means synchronously resolved.
            {
                await CancellationHelper(closeTask, cancellationToken, previousState).ConfigureAwait(false);
            }

            if (fullClose)
            {
                lock (_lockObject)
                {
                    _closeReceived = true;
                    ForceReadCloseStatusLocked();
                    _ = State;
                }
            }
        }

        private async Task CancellationHelper(Task promise, CancellationToken cancellationToken, WebSocketState previousState, IDisposable? disposable = null)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (promise.IsCompletedSuccessfully)
                {
                    disposable?.Dispose();
                    return;
                }
                if (promise.IsCompleted)
                {
                    // don't have to register for cancelation
                    await promise.ConfigureAwait(false);
                    return;
                }

                using (var receiveRegistration = cancellationToken.Register(static s =>
                {
                    CancelablePromise.CancelPromise((Task)s!);
                }, promise))
                {
                    await promise.ConfigureAwait(false);
                    return;
                }
            }
            catch (Exception ex)
            {
                lock (_lockObject)
                {
                    var state = State;
                    if (state == WebSocketState.Aborted)
                    {
                        ForceReadCloseStatusLocked();
                        throw new OperationCanceledException(nameof(WebSocketState.Aborted), ex);
                    }
                    if (ex is OperationCanceledException)
                    {
                        if(state != WebSocketState.Closed)
                        {
                            FastState = WebSocketState.Aborted;
                        }
                        _cancelled = true;
                        throw;
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
                        ForceReadCloseStatusLocked();
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

        // needs to be called with locked _lockObject
        private void ForceReadCloseStatusLocked()
        {
            if (!_disposed && _closeStatus == null)
            {
                GetCloseStatusLocked();
                GetCloseStatusDescriptionLocked();
            }
        }

        // needs to be called with locked _lockObject
        private WebSocketCloseStatus? GetCloseStatusLocked()
        {
            ThrowIfDisposed();
            var closeStatus = BrowserInterop.GetCloseStatus(_innerWebSocket);
            if (closeStatus != null && _closeStatus == null)
            {
                _closeStatus = closeStatus;
            }
            return _closeStatus;
        }

        // needs to be called with locked _lockObject
        private string? GetCloseStatusDescriptionLocked()
        {
            ThrowIfDisposed();
            var closeStatusDescription = BrowserInterop.GetCloseStatusDescription(_innerWebSocket);
            if (closeStatusDescription != null && _closeStatusDescription == null)
            {
                _closeStatusDescription = closeStatusDescription;
            }
            return _closeStatusDescription;
        }

        // needs to be called with locked _lockObject
        private static WebSocketState GetReadyStateLocked(JSObject innerWebSocket)
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
