// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed class WinHttpWebSocketState : IDisposable
    {
        // TODO (Issue 2506): The current locking mechanism doesn't allow any two WinHttp functions executing at
        // the same time for the same handle. Enahnce locking to prevent only WinHttpCloseHandle being called
        // during other API execution. E.g. using a Reader/Writer model or, even better, Interlocked functions.

        // The _lock object must be during the execution of any WinHttp function to ensure no race conditions with 
        // calling WinHttpCloseHandle.
        private readonly object _lock = new object();

        private Interop.WinHttp.SafeWinHttpHandle _sessionHandle;
        private Interop.WinHttp.SafeWinHttpHandle _connectionHandle;
        private Interop.WinHttp.SafeWinHttpHandleWithCallback _requestHandle;
        private Interop.WinHttp.SafeWinHttpHandleWithCallback _webSocketHandle;
        private int _handlesOpenWithCallback = 0;

        // A GCHandle for this operation object.
        // This is owned by the callback and will be unpinned by the callback when it determines that
        // no further calls will happen on the callback, i.e. all WinHTTP handles have fully closed via
        // a WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING notfication being received by the callback.
        private GCHandle _operationHandle = new GCHandle();

        private volatile WebSocketState _state = WebSocketState.None;
        private volatile bool _pendingReadOperation = false;
        private volatile bool _pendingWriteOperation = false;

        private volatile bool _disposed = false; // To detect redundant calls

        public WinHttpWebSocketState()
        {
        }

        public void Pin()
        {
            Debug.Assert(!_operationHandle.IsAllocated);
            _operationHandle = GCHandle.Alloc(this);
        }

        public void Unpin()
        {
            if (_operationHandle.IsAllocated)
            {
                Debug.Assert(_handlesOpenWithCallback == 0);

                _operationHandle.Free();
                _operationHandle = default(GCHandle);
            }
        }

        public void IncrementHandlesOpenWithCallback()
        {
            Interlocked.Increment(ref _handlesOpenWithCallback);
        }

        public int DecrementHandlesOpenWithCallback()
        {
            int count = Interlocked.Decrement(ref _handlesOpenWithCallback);
            Debug.Assert(count >= 0);
            
            return count;
        }

        public WebSocketState State
        {
            get
            {
                return _state;
            }
        }

        public object Lock
        {
            get
            {
                return _lock;
            }
        }

        public Interop.WinHttp.SafeWinHttpHandle SessionHandle
        {
            get
            {
                return _sessionHandle;
            }
            set
            {
                _sessionHandle = value;
            }
        }

        public Interop.WinHttp.SafeWinHttpHandle ConnectionHandle
        {
            get
            {
                return _connectionHandle;
            }
            set
            {
                _connectionHandle = value;
            }
        }

        public Interop.WinHttp.SafeWinHttpHandleWithCallback RequestHandle
        {
            get
            {
                return _requestHandle;
            }
            set
            {
                _requestHandle = value;
            }
        }

        public Interop.WinHttp.SafeWinHttpHandleWithCallback WebSocketHandle
        {
            get
            {
                return _webSocketHandle;
            }
            set
            {
                _webSocketHandle = value;
            }
        }

        // Important: do not hold _lock while signaling completion of any of below TaskCompletionSources.
        public TaskCompletionSource<bool> TcsUpgrade { get; set; }
        public TaskCompletionSource<bool> TcsSend { get; set; }
        public TaskCompletionSource<bool> TcsReceive { get; set; }
        public TaskCompletionSource<bool> TcsClose { get; set; }
        public TaskCompletionSource<bool> TcsCloseOutput { get; set; }

        public bool PendingReadOperation
        {
            get
            {
                return _pendingReadOperation;
            }

            set
            {
                _pendingReadOperation = value;
            }
        }

        public bool PendingWriteOperation
        {
            get
            {
                return _pendingWriteOperation;
            }

            set
            {
                _pendingWriteOperation = value;
            }
        }

        public IntPtr ToIntPtr()
        {
            return GCHandle.ToIntPtr(_operationHandle);
        }

        public static WinHttpWebSocketState FromIntPtr(IntPtr gcHandle)
        {
            var stateHandle = GCHandle.FromIntPtr(gcHandle);
            return (WinHttpWebSocketState)stateHandle.Target;
        }

        public Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE BufferType { get; set; }

        public uint BytesTransferred { get; set; }

        public void InterlockedCheckValidStates(WebSocketState[] validStates)
        {
            lock (_lock)
            {
                CheckValidState(validStates);
            }
        }

        public void InterlockedCheckAndUpdateState(
            WebSocketState newState,
            params WebSocketState[] validStates)
        {
            lock (_lock)
            {
                CheckValidState(validStates);
                UpdateState(newState);
            }
        }

        // Must be called with Lock taken.
        public void CheckValidState(WebSocketState[] validStates)
        {
            string validStatesText = string.Empty;

            if (validStates != null && validStates.Length > 0)
            {
                foreach (WebSocketState currentState in validStates)
                {
                    if (_state == currentState)
                    {
                        // Ordering is important to maintain .Net 4.5 WebSocket implementation exception behavior.
                        if (_disposed)
                        {
                            throw new ObjectDisposedException(GetType().FullName);
                        }

                        return;
                    }
                }

                validStatesText = string.Join(", ", validStates);
            }

            throw new WebSocketException(SR.Format(SR.net_WebSockets_InvalidState, _state, validStatesText));
        }

        public void UpdateState(WebSocketState value)
        {
            if ((_state != WebSocketState.Closed) && (_state != WebSocketState.Aborted))
            {
                _state = value;
            }
        }

        #region IDisposable Support
        private void Dispose(bool disposing)
        {
            if (_webSocketHandle != null)
            {
                _webSocketHandle.Dispose();
                // Will be set to null in the callback.
            }

            if (_requestHandle != null)
            {
                _requestHandle.Dispose();
                // Will be set to null in the callback.
            }

            Interop.WinHttp.SafeWinHttpHandle.DisposeAndClearHandle(ref _connectionHandle);
            Interop.WinHttp.SafeWinHttpHandle.DisposeAndClearHandle(ref _sessionHandle);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // No need to suppress finalization since the finalizer is not overridden.
                Dispose(true);
                _disposed = true;
            }
        }
        #endregion
    }
}
