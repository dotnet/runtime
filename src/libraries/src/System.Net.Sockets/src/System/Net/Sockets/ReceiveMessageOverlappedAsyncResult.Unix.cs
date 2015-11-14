// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Net.Sockets
{
    unsafe internal partial class ReceiveMessageOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private int _socketAddressSize;

        internal int GetSocketAddressSize()
        {
            return _socketAddressSize;
        }

        public void CompletionCallback(int numBytes, byte[] socketAddress, int socketAddressSize, SocketFlags receivedFlags, IPPacketInformation ipPacketInformation, SocketError errorCode)
        {
            Debug.Assert(_socketAddress != null);
            Debug.Assert(socketAddress == null || _socketAddress.Buffer == socketAddress);

            _socketAddressSize = socketAddressSize;
            _socketFlags = receivedFlags;
            _ipPacketInformation = ipPacketInformation;

            base.CompletionCallback(numBytes, errorCode);
        }
    }
}
