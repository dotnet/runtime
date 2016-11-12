// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    // DisconnectOverlappedAsyncResult - used to take care of storage for async Socket BeginAccept call.
    internal sealed class DisconnectOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        internal DisconnectOverlappedAsyncResult(Socket socket, Object asyncState, AsyncCallback asyncCallback) :
            base(socket, asyncState, asyncCallback)
        {
        }

        // This method will be called by us when the IO completes synchronously and
        // by the ThreadPool when the IO completes asynchronously.
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
