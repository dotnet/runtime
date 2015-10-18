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
            try
            {
                await _webSocket.ConnectAsync(uri, cancellationToken, options).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // TODO: ** TFS BUILD IS BROKEN ** 
                // This doesn't compile right now due to missing types 'WebErrorStatus' and 'RTWebSocketError'
                // Commenting out for now to allow the build to resume.
                //WebErrorStatus status = RTWebSocketError.GetStatus(ex.HResult);
                //var inner = new Exception(status.ToString(), ex);
                var inner = ex;
                WebSocketException wex = new WebSocketException(SR.net_webstatus_ConnectFailure, inner);
                if (Logging.On)
                {
                    Logging.Exception(Logging.WebSockets, this, "ConnectAsync", wex);
                }

                throw wex;
            }
        }
    }
}
