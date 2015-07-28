// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

namespace System.Net.WebSockets
{
    public sealed class ClientWebSocket : WebSocket
    {
        private enum InternalState
        {
            Created = 0,
            Connecting = 1,
            Connected = 2,
            Disposed = 3
        }

        private const string WebSocketAvailableApiCheck = "WinHttpWebSocketCompleteUpgrade";

        private readonly ClientWebSocketOptions _options;
        private WinHttpWebSocket _innerWebSocket;
        private readonly CancellationTokenSource _cts;

        // NOTE: this is really an InternalState value, but Interlocked doesn't support
        //       operations on values of enum types.
        private int _state;

        public ClientWebSocket()
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.WebSockets, this, ".ctor", null);
            }

            CheckPlatformSupport();

            _state = (int)InternalState.Created;
            _options = new ClientWebSocketOptions();
            _cts = new CancellationTokenSource();

            if (Logging.On)
            {
                Logging.Exit(Logging.WebSockets, this, ".ctor", null);
            }
        }

        #region Properties

        public ClientWebSocketOptions Options
        {
            get
            {
                return _options;
            }
        }

        public override WebSocketCloseStatus? CloseStatus
        {
            get
            {
                if (_innerWebSocket != null)
                {
                    return _innerWebSocket.CloseStatus;
                }
                return null;
            }
        }

        public override string CloseStatusDescription
        {
            get
            {
                if (_innerWebSocket != null)
                {
                    return _innerWebSocket.CloseStatusDescription;
                }
                return null;
            }
        }

        public override string SubProtocol
        {
            get
            {
                if (_innerWebSocket != null)
                {
                    return _innerWebSocket.SubProtocol;
                }
                return null;
            }
        }

        public override WebSocketState State
        {
            get
            {
                // state == Connected or Disposed
                if (_innerWebSocket != null)
                {
                    return _innerWebSocket.State;
                }
                switch ((InternalState)_state)
                {
                    case InternalState.Created:
                        return WebSocketState.None;
                    case InternalState.Connecting:
                        return WebSocketState.Connecting;
                    default: // We only get here if disposed before connecting
                        Debug.Assert((InternalState)_state == InternalState.Disposed);
                        return WebSocketState.Closed;
                }
            }
        }

        #endregion Properties

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException(SR.net_uri_NotAbsolute, "uri");
            }
            if (uri.Scheme != UriScheme.Ws && uri.Scheme != UriScheme.Wss)
            {
                throw new ArgumentException(SR.net_WebSockets_Scheme, "uri");
            }

            // Check that we have not started already
            var priorState = (InternalState)Interlocked.CompareExchange(ref _state, (int)InternalState.Connecting, (int)InternalState.Created);
            if (priorState == InternalState.Disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else if (priorState != InternalState.Created)
            {
                throw new InvalidOperationException(SR.net_WebSockets_AlreadyStarted);
            }
            _options.SetToReadOnly();

            return ConnectAsyncCore(uri, cancellationToken);
        }

        private Task ConnectAsyncCore(Uri uri, CancellationToken cancellationToken)
        {
            _innerWebSocket = new WinHttpWebSocket();

            try
            {
                // Change internal state to 'connected' to enable the other methods
                if ((InternalState)Interlocked.CompareExchange(ref _state, (int)InternalState.Connected, (int)InternalState.Connecting) != InternalState.Connecting)
                {
                    // Aborted/Disposed during connect.
                    throw new ObjectDisposedException(GetType().FullName);
                }

                return _innerWebSocket.ConnectAsync(uri, cancellationToken, _options);
            }
            catch (Win32Exception ex)
            {
                WebSocketException wex = new WebSocketException(SR.net_webstatus_ConnectFailure, ex);
                if (Logging.On)
                {
                    Logging.Exception(Logging.WebSockets, this, "ConnectAsync", wex);
                }
                throw wex;
            }
            catch (Exception ex)
            {
                if (Logging.On)
                {
                    Logging.Exception(Logging.WebSockets, this, "ConnectAsync", ex);
                }
                throw;
            }
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();

            if (!((messageType == WebSocketMessageType.Text) || (messageType == WebSocketMessageType.Binary)))
            {
                string errorMessage = SR.Format(
                        SR.net_WebSockets_Argument_InvalidMessageType,
                        "Close",
                        "SendAsync",
                        "Binary",
                        "Text",
                        "CloseOutputAsync");

                throw new ArgumentException(errorMessage, "messageType");
            }

            WebSocketValidate.ValidateArraySegment<byte>(buffer, "buffer");
            return _innerWebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();
            WebSocketValidate.ValidateArraySegment<byte>(buffer, "buffer");
            return _innerWebSocket.ReceiveAsync(buffer, cancellationToken);
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
            return _innerWebSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
            return _innerWebSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override void Abort()
        {
            if ((InternalState)_state == InternalState.Disposed)
            {
                return;
            }
            if (_innerWebSocket != null)
            {
                _innerWebSocket.Abort();
            }
            Dispose();
        }

        public override void Dispose()
        {
            var priorState = (InternalState)Interlocked.Exchange(ref _state, (int)InternalState.Disposed);
            if (priorState == InternalState.Disposed)
            {
                // No cleanup required.
                return;
            }
            _cts.Cancel(false);
            _cts.Dispose();
            if (_innerWebSocket != null)
            {
                _innerWebSocket.Dispose();
            }
        }

        private void ThrowIfNotConnected()
        {
            if ((InternalState)_state == InternalState.Disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else if ((InternalState)_state != InternalState.Connected)
            {
                throw new InvalidOperationException(SR.net_WebSockets_NotConnected);
            }
        }

        private void CheckPlatformSupport()
        {
            bool isPlatformSupported = false;

            using (SafeLibraryHandle libHandle = Interop.mincore.LoadLibraryExW(Interop.Libraries.WinHttp, IntPtr.Zero, 0))
            {
                isPlatformSupported = Interop.mincore.GetProcAddress(libHandle, WebSocketAvailableApiCheck) != IntPtr.Zero;
            }

            if (!isPlatformSupported)
            {
                WebSocketValidate.ThrowPlatformNotSupportedException();
            }
        }
    }
}
