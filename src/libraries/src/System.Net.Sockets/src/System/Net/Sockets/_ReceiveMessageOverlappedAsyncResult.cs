// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using System.Collections.Generic;

namespace System.Net.Sockets
{
    unsafe internal partial class ReceiveMessageOverlappedAsyncResult : BaseOverlappedAsyncResult
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
