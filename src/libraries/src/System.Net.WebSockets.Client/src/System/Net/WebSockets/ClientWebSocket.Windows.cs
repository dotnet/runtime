// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

namespace System.Net.WebSockets
{
    public sealed partial class ClientWebSocket : WebSocket
    {
        partial void CheckPlatformSupport()
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
        
        private Task ConnectAsyncCore(Uri uri, CancellationToken cancellationToken)
        {
            var winHttpWebSocket = new WinHttpWebSocket();
            _innerWebSocket = winHttpWebSocket;

            try
            {
                // Change internal state to 'connected' to enable the other methods
                if ((InternalState)Interlocked.CompareExchange(ref _state, (int)InternalState.Connected, (int)InternalState.Connecting) != InternalState.Connecting)
                {
                    // Aborted/Disposed during connect.
                    throw new ObjectDisposedException(GetType().FullName);
                }

                return winHttpWebSocket.ConnectAsync(uri, cancellationToken, _options);
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
    }        
}
