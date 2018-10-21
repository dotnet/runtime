// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace System.Net.Sockets
{
    // AcceptOverlappedAsyncResult - used to take care of storage for async Socket BeginAccept call.
    internal sealed partial class AcceptOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private Socket _acceptedSocket;

        internal Socket AcceptSocket
        {
            set
            {
                // *nix does not support the reuse of an existing socket as the accepted
                // socket.
                Debug.Assert(value == null, $"Unexpected value: {value}");
            }
        }

        public void CompletionCallback(IntPtr acceptedFileDescriptor, byte[] socketAddress, int socketAddressLen, SocketError errorCode)
        {
            _buffer = null;
            _numBytes = 0;

			if (errorCode == SocketError.Success)
			{
				Internals.SocketAddress remoteSocketAddress = IPEndPointExtensions.Serialize(_listenSocket._rightEndPoint);
				System.Buffer.BlockCopy(socketAddress, 0, remoteSocketAddress.Buffer, 0, socketAddressLen);

				_acceptedSocket = _listenSocket.CreateAcceptSocket(
					SafeSocketHandle.CreateSocket(acceptedFileDescriptor),
					_listenSocket._rightEndPoint.Create(remoteSocketAddress));
			}

            base.CompletionCallback(0, errorCode);
        }

        internal override object PostCompletion(int numBytes)
        {
            _numBytes = numBytes;
            return (SocketError)ErrorCode == SocketError.Success ? _acceptedSocket : null;
        }
    }
}
