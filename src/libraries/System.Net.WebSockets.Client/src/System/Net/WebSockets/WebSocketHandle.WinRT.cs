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

using Windows.Web;
using RTWebSocketError = Windows.Networking.Sockets.WebSocketError;

namespace System.Net.WebSockets
{
    internal readonly partial struct WebSocketHandle
    {
        private readonly WinRTWebSocket _webSocket;

        public static WebSocketHandle Create()
        {
            return new WebSocketHandle(new WinRTWebSocket());
        }

        public static void CheckPlatformSupport()
        {
        }

        private WebSocketHandle(WinRTWebSocket webSocket)
        {
            _webSocket = webSocket;
        }
        
        public async Task ConnectAsyncCore(Uri uri, CancellationToken cancellationToken, ClientWebSocketOptions options)
        {
            if (options.RemoteCertificateValidationCallback != null)
            {
                throw new PlatformNotSupportedException(SR.net_WebSockets_RemoteValidationCallbackNotSupported);
            }

            try
            {
                await _webSocket.ConnectAsync(uri, cancellationToken, options).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                WebErrorStatus status = RTWebSocketError.GetStatus(ex.HResult);
                var inner = new Exception(status.ToString(), ex);
                WebSocketException wex = new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure, inner);
                if (NetEventSource.IsEnabled) NetEventSource.Error(_webSocket, wex);
                throw wex;
            }
        }
    }
}
