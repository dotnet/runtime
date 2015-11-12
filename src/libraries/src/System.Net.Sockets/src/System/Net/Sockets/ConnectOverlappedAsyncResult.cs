// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace System.Net.Sockets
{
    // ConnectOverlappedAsyncResult - used to take care of storage for async Socket BeginConnect call.
    internal partial class ConnectOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private EndPoint _endPoint;

        internal ConnectOverlappedAsyncResult(Socket socket, EndPoint endPoint, Object asyncState, AsyncCallback asyncCallback) :
            base(socket, asyncState, asyncCallback)
        {
            _endPoint = endPoint;
        }

        internal EndPoint RemoteEndPoint
        {
            get { return _endPoint; }
        }
    }
}
