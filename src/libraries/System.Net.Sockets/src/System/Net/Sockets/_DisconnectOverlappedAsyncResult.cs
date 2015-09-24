// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace System.Net.Sockets
{
    // DisconnectOverlappedAsyncResult - used to take care of storage for async Socket BeginDisconnect call.
    internal class DisconnectOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        internal DisconnectOverlappedAsyncResult(Socket socket, Object asyncState, AsyncCallback asyncCallback) :
            base(socket, asyncState, asyncCallback)
        {
        }

        // This method will be called by us when the IO completes synchronously and
        // by the ThreadPool when the IO completes asynchronously. (only called on WinNT)
        internal override object PostCompletion(int numBytes)
        {
            if (ErrorCode == (int)SocketError.Success)
            {
                Socket socket = (Socket)AsyncObject;
                socket.SetToDisconnected();
                socket._remoteEndPoint = null;
            }

            return base.PostCompletion(numBytes);
        }
    }
}
