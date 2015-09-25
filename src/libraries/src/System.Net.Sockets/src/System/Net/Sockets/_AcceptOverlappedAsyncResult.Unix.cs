// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace System.Net.Sockets
{
    // AcceptOverlappedAsyncResult - used to take care of storage for async Socket BeginAccept call.
    internal partial class AcceptOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private Socket _acceptedSocket;

        internal Socket AcceptSocket
        {
            set
            {
                // *nix does not support the reuse of an existing socket as the accepted
                // socket.
                Debug.Assert(value == null);
            }
        }

        public void CompletionCallback(int acceptedFileDescriptor, byte[] socketAddress, int socketAddressLen, SocketError errorCode)
        {
            // TODO: receive bytes on accepted socket if requested

            _buffer = null;
            _localBytesTransferred = 0;

			if (errorCode == SocketError.Success)
			{
				Internals.SocketAddress remoteSocketAddress = IPEndPointExtensions.Serialize(_listenSocket._rightEndPoint);
				System.Buffer.BlockCopy(socketAddress, 0, remoteSocketAddress.Buffer, 0, socketAddressLen);

				_acceptedSocket = _listenSocket.CreateAcceptSocket(
					SafeCloseSocket.CreateSocket(acceptedFileDescriptor),
					_listenSocket._rightEndPoint.Create(remoteSocketAddress));
			}

            base.CompletionCallback(0, errorCode);
        }

        internal override object PostCompletion(int numBytes)
        {
            return (SocketError)ErrorCode == SocketError.Success ? _acceptedSocket : null;
        }
    }
}
