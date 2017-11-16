// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal readonly partial struct WebSocketHandle
    {
        #region Properties
        public static bool IsValid(WebSocketHandle handle)
        {
            return handle._webSocket != null;
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
            if (messageType != WebSocketMessageType.Text && messageType != WebSocketMessageType.Binary)
            {
                string errorMessage = SR.Format(
                        SR.net_WebSockets_Argument_InvalidMessageType,
                        nameof(WebSocketMessageType.Close),
                        nameof(SendAsync),
                        nameof(WebSocketMessageType.Binary),
                        nameof(WebSocketMessageType.Text),
                        nameof(CloseOutputAsync));
                throw new ArgumentException(errorMessage, nameof(messageType));
            }

            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));

            return _webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public Task SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            if (messageType != WebSocketMessageType.Text && messageType != WebSocketMessageType.Binary)
            {
                string errorMessage = SR.Format(
                        SR.net_WebSockets_Argument_InvalidMessageType,
                        nameof(WebSocketMessageType.Close),
                        nameof(SendAsync),
                        nameof(WebSocketMessageType.Binary),
                        nameof(WebSocketMessageType.Text),
                        nameof(CloseOutputAsync));
                throw new ArgumentException(errorMessage, nameof(messageType));
            }

            return _webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));
            return _webSocket.ReceiveAsync(buffer, cancellationToken);
        }

        public ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
            _webSocket.ReceiveAsync(buffer, cancellationToken);

        public Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
            return _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
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
