// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    internal unsafe sealed partial class ReceiveMessageOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private Internals.SocketAddress _socketAddressOriginal;
        private Internals.SocketAddress _socketAddress;

        private SocketFlags _socketFlags;
        private IPPacketInformation _ipPacketInformation;

        internal ReceiveMessageOverlappedAsyncResult(Socket socket, Object asyncState, AsyncCallback asyncCallback) :
            base(socket, asyncState, asyncCallback)
        { }

        internal Internals.SocketAddress SocketAddress
        {
            get
            {
                return _socketAddress;
            }
            set
            {
                _socketAddress = value;
            }
        }

        internal Internals.SocketAddress SocketAddressOriginal
        {
            get
            {
                return _socketAddressOriginal;
            }
            set
            {
                _socketAddressOriginal = value;
            }
        }

        internal SocketFlags SocketFlags
        {
            get
            {
                return _socketFlags;
            }
        }

        internal IPPacketInformation IPPacketInformation
        {
            get
            {
                return _ipPacketInformation;
            }
        }
    }
}
