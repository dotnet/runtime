// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace System.Net.Sockets
{
    // ConnectOverlappedAsyncResult - used to take care of storage for async Socket BeginConnect call.
    internal sealed partial class ConnectOverlappedAsyncResult : BaseOverlappedAsyncResult
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
