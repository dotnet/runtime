// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using System.Collections.Generic;

namespace System.Net.Sockets
{
    // OverlappedAsyncResult
    //
    // This class is used to take care of storage for async Socket operation
    // from the BeginSend, BeginSendTo, BeginReceive, BeginReceiveFrom calls.
    internal sealed partial class OverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private Internals.SocketAddress _socketAddress;
        private Internals.SocketAddress _socketAddressOriginal; // Needed for partial BeginReceiveFrom/EndReceiveFrom completion.

        internal OverlappedAsyncResult(Socket socket, Object asyncState, AsyncCallback asyncCallback) :
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
    }
}
