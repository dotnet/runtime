// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Sockets
{
    // OverlappedAsyncResult
    //
    // This class is used to take care of storage for async Socket operation
    // from the BeginSend, BeginSendTo, BeginReceive, BeginReceiveFrom calls.
    internal partial class OverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private int _socketAddressSize;

        internal int GetSocketAddressSize()
        {
            return _socketAddressSize;
        }

        public void CompletionCallback(int numBytes, byte[] socketAddress, int socketAddressSize, SocketFlags receivedFlags, SocketError errorCode)
        {
            if (_socketAddress != null)
            {
                Debug.Assert(socketAddress == null || _socketAddress.Buffer == socketAddress, $"Unexpected socket address: {socketAddress}");
                _socketAddressSize = socketAddressSize;
            }

            base.CompletionCallback(numBytes, errorCode);
        }
    }
}
