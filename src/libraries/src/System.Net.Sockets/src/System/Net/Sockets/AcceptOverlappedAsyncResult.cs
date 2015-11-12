// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace System.Net.Sockets
{
    // AcceptOverlappedAsyncResult - used to take care of storage for async Socket BeginAccept call.
    internal partial class AcceptOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private int _localBytesTransferred;
        private Socket _listenSocket;
        private byte[] _buffer;

        internal AcceptOverlappedAsyncResult(Socket listenSocket, Object asyncState, AsyncCallback asyncCallback) :
            base(listenSocket, asyncState, asyncCallback)
        {
            _listenSocket = listenSocket;
        }

        internal byte[] Buffer
        {
            get
            {
                return _buffer;
            }
        }

        internal int BytesTransferred
        {
            get
            {
                return _localBytesTransferred;
            }
        }
    }
}
