// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace System.Net.Sockets
{
    // ConnectOverlappedAsyncResult - used to take care of storage for async Socket BeginConnect call.
    internal partial class ConnectOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        // This method is called by base.CompletionPortCallback base.OverlappedCallback as part of IO completion
        internal override object PostCompletion(int numBytes)
        {
            SocketError errorCode = (SocketError)ErrorCode;
            Socket socket = (Socket)AsyncObject;

            if (errorCode == SocketError.Success)
            {
                // Set the socket context.
                try
                {
                    errorCode = Interop.Winsock.setsockopt(
                        socket.SafeHandle,
                        SocketOptionLevel.Socket,
                        SocketOptionName.UpdateConnectContext,
                        null,
                        0);
                    if (errorCode == SocketError.SocketError)
                    {
                        errorCode = (SocketError)Marshal.GetLastWin32Error();
                    }
                }
                catch (ObjectDisposedException)
                {
                    errorCode = SocketError.OperationAborted;
                }

                ErrorCode = (int)errorCode;
            }

            if (errorCode == SocketError.Success)
            {
                socket.SetToConnected();
                return socket;
            }

            return null;
        }
    }
}
