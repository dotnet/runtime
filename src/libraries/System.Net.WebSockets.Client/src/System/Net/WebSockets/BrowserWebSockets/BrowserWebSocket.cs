// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using System.Buffers;

namespace System.Net.WebSockets
{
    /// <summary>
    /// Provides a client for connecting to WebSocket services.
    /// </summary>
    internal sealed class BrowserWebSocket : WebSocket
    {
#if FEATURE_WASM_THREADS
        private readonly object _thisLock = new object();
#endif

        private WebSocketCloseStatus? _closeStatus;
        private string? _closeStatusDescription;
        private JSObject? _innerWebSocket;
        private WebSocketState _state;
        private bool _disposed;
        private bool _aborted;
        private bool _closeReceived;
        private bool _closeSent;
        private int[] responseStatus = new int[3];
        private MemoryHandle? responseStatusHandle;

        #region Properties

        public override WebSocketState State
        {
            get
            {
#if FEATURE_WASM_THREADS
                lock (_thisLock)
                {
#endif
                    if (_innerWebSocket == null || _disposed || _state == WebSocketState.Aborted || _state == WebSocketState.Closed)
                    {
                        return _state;
                    }
#if FEATURE_WASM_THREADS
                } //lock
#endif

#if FEATURE_WASM_THREADS
                return _innerWebSocket!.SynchronizationContext.Send(GetReadyState, this);
#else
                return GetReadyState(this);
#endif
            }
        }

        private WebSocketState FastState
        {
            get
            {
#if FEATURE_WASM_THREADS
                lock (_thisLock)
                {
#endif
                    return _state;
#if FEATURE_WASM_THREADS
                } //lock
#endif
            }
            set
            {
#if FEATURE_WASM_THREADS
                lock (_thisLock)
                {
#endif
                    _state = value;
#if FEATURE_WASM_THREADS
                } //lock
#endif
            }
        }

        public override WebSocketCloseStatus? CloseStatus => _closeStatus;
        public override string? CloseStatusDescription => _closeStatusDescription;
        public override string? SubProtocol
        {
            get
            {
#if FEATURE_WASM_THREADS
                lock (_thisLock)
                {
#endif
                    ThrowIfDisposed();
                    if (_innerWebSocket == null) throw new InvalidOperationException(SR.net_WebSockets_NotConnected);
#if FEATURE_WASM_THREADS
                } //lock
#endif

#if FEATURE_WASM_THREADS
                return _innerWebSocket.SynchronizationContext.Send(BrowserInterop.GetProtocol, _innerWebSocket);
#else
                return BrowserInterop.GetProtocol(_innerWebSocket);
#endif

            }
        }

        #endregion Properties

        internal Task ConnectAsync(Uri uri, List<string>? requestedSubProtocols, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (FastState != WebSocketState.None)
            {
                throw new InvalidOperationException(SR.net_WebSockets_AlreadyStarted);
            }
#if FEATURE_WASM_THREADS
            JSHost.CurrentOrMainJSSynchronizationContext!.Send(_ =>
            {
                lock (_thisLock)
                {
                    ThrowIfDisposed();
                    FastState = WebSocketState.Connecting;
                    CreateCore(uri, requestedSubProtocols);
                }
            }, null);

            return JSHost.CurrentOrMainJSSynchronizationContext.Send(() =>
            {
                return ConnectAsyncCore(cancellationToken);
            });
#else
            FastState = WebSocketState.Connecting;
            CreateCore(uri, requestedSubProtocols);
            return ConnectAsyncCore(cancellationToken);
#endif
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ThrowIfDisposed();

            // fast check of previous _state instead of GetReadyState(), the readyState would be validated on JS side
            if (FastState != WebSocketState.Open && FastState != WebSocketState.CloseReceived)
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

#if FEATURE_WASM_THREADS
            return _innerWebSocket!.SynchronizationContext.Send(() =>
            {
                Task promise;
                lock (_thisLock)
                {
                    ThrowIfDisposed();
                    promise = SendAsyncCore(buffer, messageType, endOfMessage, cancellationToken);
                } //lock will unlock synchronously before promise is resolved!

                return promise;
            });
#else
            return SendAsyncCore(buffer, messageType, endOfMessage, cancellationToken);
#endif
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromException<WebSocketReceiveResult>(new OperationCanceledException(cancellationToken));
            }
            ThrowIfDisposed();
            // fast check of previous _state instead of GetReadyState(), the readyState would be validated on JS side
            var fastState = FastState;
            if (fastState != WebSocketState.Open && fastState != WebSocketState.CloseSent)
            {
                throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, fastState, "Open, CloseSent"));
            }

            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

#if FEATURE_WASM_THREADS
            return _innerWebSocket!.SynchronizationContext.Send(() =>
            {
                Task<WebSocketReceiveResult> promise;
                lock (_thisLock)
                {
                    ThrowIfDisposed();
                    promise = ReceiveAsyncCore(buffer, cancellationToken);
                } //lock will unlock synchronously before task is resolved!
                return promise;
            });
#else
            return ReceiveAsyncCore(buffer, cancellationToken);
#endif
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
            var fastState = FastState;
            if (fastState == WebSocketState.None || fastState == WebSocketState.Closed)
            {
                throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, fastState, "Connecting, Open, CloseSent, Aborted"));
            }

#if FEATURE_WASM_THREADS
            return _innerWebSocket!.SynchronizationContext.Send(() =>
            {
                Task promise;
                lock (_thisLock)
                {
                    ThrowIfDisposed();
#endif
                    var state = State;
                    if (state == WebSocketState.None || state == WebSocketState.Closed)
                    {
                        throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, state, "Connecting, Open, CloseSent, Aborted"));
                    }
                    if (state == WebSocketState.CloseSent)
                    {
                        return Task.CompletedTask;
                    }
#if FEATURE_WASM_THREADS
                    promise = CloseAsyncCore(closeStatus, statusDescription, false, cancellationToken);
                } //lock will unlock synchronously before task is resolved!
                return promise;
            });
#else
            return CloseAsyncCore(closeStatus, statusDescription, false, cancellationToken);
#endif
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);

            var fastState = FastState;
            if (fastState == WebSocketState.None || fastState == WebSocketState.Closed)
            {
                throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, fastState, "Connecting, Open, CloseSent, Aborted"));
            }

#if FEATURE_WASM_THREADS
            return _innerWebSocket!.SynchronizationContext.Send(() =>
            {
                Task promise;
                lock (_thisLock)
                {
                    ThrowIfDisposed();
#endif
                    var state = State;
                    if (state == WebSocketState.None || state == WebSocketState.Closed)
                    {
                        throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, state, "Connecting, Open, CloseSent, Aborted"));
                    }

#if FEATURE_WASM_THREADS
                    promise = CloseAsyncCore(closeStatus, statusDescription, state != WebSocketState.Aborted, cancellationToken);
                } //lock will unlock synchronously before task is resolved!
                return promise;
            });
#else
            return CloseAsyncCore(closeStatus, statusDescription, state != WebSocketState.Aborted, cancellationToken);
#endif
        }

        public override void Abort()
        {
#if FEATURE_WASM_THREADS
            if (_disposed)
            {
                return;
            }
            _innerWebSocket?.SynchronizationContext.Send(static (BrowserWebSocket self) =>
            {
                lock (self._thisLock)
                {
                    AbortCore(self, self.State);
                }
            }, this);
#else
            AbortCore(this, this.State);
#endif
        }

        public override void Dispose()
        {
#if FEATURE_WASM_THREADS
            if (_disposed)
            {
                return;
            }
            _innerWebSocket?.SynchronizationContext.Send(static (BrowserWebSocket self) =>
            {
                lock (self._thisLock)
                {
                    DisposeCore(self);
                }
            }, this);
#else
            DisposeCore(this);
#endif
        }

        private void ThrowIfDisposed()
        {
#if FEATURE_WASM_THREADS
            lock (_thisLock)
            {
#endif
                ObjectDisposedException.ThrowIf(_disposed, this);
#if FEATURE_WASM_THREADS
            } //lock
#endif
        }

        #region methods always called on one thread only, exclusively

        private static void DisposeCore(BrowserWebSocket self)
        {
            if (!self._disposed)
            {
                var state = self.State;
                self._disposed = true;
                if (state < WebSocketState.Aborted && state != WebSocketState.None)
                {
                    AbortCore(self, state);
                }
                if (self.FastState != WebSocketState.Aborted)
                {
                    self.FastState = WebSocketState.Closed;
                }
                self._innerWebSocket?.Dispose();
                self._innerWebSocket = null;
                self.responseStatusHandle?.Dispose();
            }
        }

        private static void AbortCore(BrowserWebSocket self, WebSocketState currentState)
        {
            if (!self._disposed && currentState != WebSocketState.Closed)
            {
                self.FastState = WebSocketState.Aborted;
                self._aborted = true;
                if (self._innerWebSocket != null)
                {
                    BrowserInterop.WebSocketAbort(self._innerWebSocket!);
                }
            }
        }

        private void CreateCore(Uri uri, List<string>? requestedSubProtocols)
        {
            try
            {
                string[]? subProtocols = requestedSubProtocols?.ToArray();
                var onClose = (int code, string reason) =>
                {
#if FEATURE_WASM_THREADS
                    lock (_thisLock)
                    {
#endif
                        _closeStatus = (WebSocketCloseStatus)code;
                        _closeStatusDescription = reason;
                        _closeReceived = true;
                        WebSocketState state = State;
                        if (state == WebSocketState.Connecting || state == WebSocketState.Open || state == WebSocketState.CloseSent)
                        {
                            FastState = WebSocketState.Closed;
                        }
#if FEATURE_WASM_THREADS
                    } //lock
#endif
                };

                Memory<int> responseMemory = new Memory<int>(responseStatus);

                responseStatusHandle = responseMemory.Pin();
                _innerWebSocket = BrowserInterop.UnsafeCreate(uri.ToString(), subProtocols, responseStatusHandle.Value, onClose);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private async Task ConnectAsyncCore(CancellationToken cancellationToken)
        {
#if FEATURE_WASM_THREADS
            lock (_thisLock)
            {
#endif
                if (_aborted)
                {
                    FastState = WebSocketState.Closed;
                    throw new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure);
                }
                ThrowIfDisposed();
#if FEATURE_WASM_THREADS
            } //lock
#endif

            try
            {
                var openTask = BrowserInterop.WebSocketOpen(_innerWebSocket!);

                await CancelationHelper(openTask!, cancellationToken, FastState).ConfigureAwait(true);
#if FEATURE_WASM_THREADS
                lock (_thisLock)
                {
#endif
                    if (State == WebSocketState.Connecting)
                    {
                        FastState = WebSocketState.Open;
                    }
#if FEATURE_WASM_THREADS
                } //lock
#endif
            }
            catch (OperationCanceledException ex)
            {
#if FEATURE_WASM_THREADS
                lock (_thisLock)
                {
#endif
                    FastState = WebSocketState.Closed;
                    if (_aborted)
                    {
                        throw new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure, ex);
                    }
#if FEATURE_WASM_THREADS
                } //lock
#endif
                throw;
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private async Task SendAsyncCore(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            try
            {
                var sendTask = BrowserInterop.UnsafeSendSync(_innerWebSocket!, buffer, messageType, endOfMessage);
                if (sendTask == null)
                {
                    // return synchronously
                    return;
                }

                await CancelationHelper(sendTask, cancellationToken, FastState).ConfigureAwait(true);
            }
            catch (JSException ex)
            {
                if (ex.Message.StartsWith("InvalidState:"))
                {
                    throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, State, "Open"), ex);
                }
                throw new WebSocketException(WebSocketError.NativeError, ex);
            }
        }

        private async Task<WebSocketReceiveResult> ReceiveAsyncCore(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                Memory<byte> bufferMemory = buffer.AsMemory();
                using (MemoryHandle pinBuffer = bufferMemory.Pin())
                {
                    var receiveTask = BrowserInterop.ReceiveUnsafeSync(_innerWebSocket!, pinBuffer, bufferMemory.Length);
                    if (receiveTask == null)
                    {
                        // return synchronously
#if FEATURE_WASM_THREADS
                        lock (_thisLock)
                        {
#endif
                            return ConvertResponse(this);
#if FEATURE_WASM_THREADS
                        } //lock
#endif
                    }
                    await CancelationHelper(receiveTask, cancellationToken, FastState).ConfigureAwait(true);

#if FEATURE_WASM_THREADS
                    lock (_thisLock)
                    {
#endif
                        return ConvertResponse(this);
#if FEATURE_WASM_THREADS
                    } //lock
#endif
                }
            }
            catch (JSException ex)
            {
                if (ex.Message.StartsWith("InvalidState:"))
                {
                    throw new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState, State, "Open, CloseSent"), ex);
                }
                throw new WebSocketException(WebSocketError.NativeError, ex);
            }
        }

        private static WebSocketReceiveResult ConvertResponse(BrowserWebSocket self)
        {
            const int countIndex = 0;
            const int typeIndex = 1;
            const int endIndex = 2;

            WebSocketMessageType messageType = (WebSocketMessageType)self.responseStatus[typeIndex];
            if (messageType == WebSocketMessageType.Close)
            {
                self._closeReceived = true;
                self.FastState = self._closeSent ? WebSocketState.Closed : WebSocketState.CloseReceived;
                return new WebSocketReceiveResult(self.responseStatus[countIndex], messageType, self.responseStatus[endIndex] != 0, self.CloseStatus, self.CloseStatusDescription);
            }
            return new WebSocketReceiveResult(self.responseStatus[countIndex], messageType, self.responseStatus[endIndex] != 0);
        }

        private async Task CloseAsyncCore(WebSocketCloseStatus closeStatus, string? statusDescription, bool waitForCloseReceived, CancellationToken cancellationToken)
        {
            if (!_closeReceived)
            {
                _closeStatus = closeStatus;
                _closeStatusDescription = statusDescription;
            }
            _closeSent = true;

            var closeTask = BrowserInterop.WebSocketClose(_innerWebSocket!, (int)closeStatus, statusDescription, waitForCloseReceived) ?? Task.CompletedTask;
            await CancelationHelper(closeTask, cancellationToken, FastState).ConfigureAwait(true);

#if FEATURE_WASM_THREADS
            lock (_thisLock)
            {
#endif
                if (waitForCloseReceived)
                {
                    _closeReceived = true;
                }
                var state = State;
                if (state == WebSocketState.Open || state == WebSocketState.Connecting || state == WebSocketState.CloseSent)
                {
                    FastState = waitForCloseReceived ? WebSocketState.Closed : WebSocketState.CloseSent;
                }
#if FEATURE_WASM_THREADS
            } //lock
#endif
        }

        private async Task CancelationHelper(Task jsTask, CancellationToken cancellationToken, WebSocketState previousState)
        {
            if (jsTask.IsCompletedSuccessfully)
            {
                return;
            }
            try
            {
                using (var receiveRegistration = cancellationToken.Register(() =>
                {
                    CancelablePromise.CancelPromise(jsTask);
                }))
                {
                    await jsTask.ConfigureAwait(true);
                    return;
                }
            }
            catch (JSException ex)
            {
                if (State == WebSocketState.Aborted)
                {
                    throw new OperationCanceledException(nameof(WebSocketState.Aborted), ex);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    FastState = WebSocketState.Aborted;
                    throw new OperationCanceledException(cancellationToken);
                }
                if (ex.Message == "Error: OperationCanceledException")
                {
                    FastState = WebSocketState.Aborted;
                    throw new OperationCanceledException("The operation was cancelled.", ex, cancellationToken);
                }
                if (previousState == WebSocketState.Connecting)
                {
                    throw new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure, ex);
                }
                throw new WebSocketException(WebSocketError.NativeError, ex);
            }
        }

        private static WebSocketState GetReadyState(BrowserWebSocket self)
        {
#if FEATURE_WASM_THREADS
            lock (self._thisLock)
            {
#endif
                var readyState = BrowserInterop.GetReadyState(self._innerWebSocket);
                // https://developer.mozilla.org/en-US/docs/Web/API/WebSocket/readyState
                var st = readyState switch
                {
                    0 => WebSocketState.Connecting, // 0 (CONNECTING)
                    1 => WebSocketState.Open, // 1 (OPEN)
                    2 => WebSocketState.CloseSent, // 2 (CLOSING)
                    3 => WebSocketState.Closed, // 3 (CLOSED)
                    _ => WebSocketState.None
                };
                if (st == WebSocketState.Closed || st == WebSocketState.CloseSent)
                {
                    if (self._closeReceived && self._closeSent)
                    {
                        st = WebSocketState.Closed;
                    }
                    else if (self._closeReceived && !self._closeSent)
                    {
                        st = WebSocketState.CloseReceived;
                    }
                    else if (!self._closeReceived && self._closeSent)
                    {
                        st = WebSocketState.CloseSent;
                    }
                }
                self.FastState = st;
                return st;
#if FEATURE_WASM_THREADS
            } //lock
#endif
        }

        #endregion
    }
}
