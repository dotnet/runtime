// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    // ConnectOverlappedAsyncResult - used to take care of storage for async Socket BeginConnect call.
    internal sealed partial class ConnectOverlappedAsyncResult : BaseOverlappedAsyncResult
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
                        errorCode = SocketPal.GetLastSocketError();
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
