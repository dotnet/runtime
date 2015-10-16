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
    internal partial struct WebSocketHandle
    {
        #region Properties
        public bool IsValid
        {
            get
            {
                return _webSocket != null;
            }
        }

        public WebSocketCloseStatus? CloseStatus
        {
            get
            {
                return _webSocket.CloseStatus;
            }
        }

        public string CloseStatusDescription
        {
            get
            {
                return _webSocket.CloseStatusDescription;
            }
        }

        public WebSocketState State
        {
            get
            {
                return _webSocket.State;
            }
        }

        public string SubProtocol
        {
            get
            {
                return _webSocket.SubProtocol;
            }
        }
        #endregion

        public Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            return _webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            return _webSocket.ReceiveAsync(buffer, cancellationToken);
        }

        public Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            return _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            return _webSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
        }

        public void Dispose()
        {
            _webSocket.Dispose();
        }

        public void Abort()
        {
            _webSocket.Abort();
        }
    }
}
