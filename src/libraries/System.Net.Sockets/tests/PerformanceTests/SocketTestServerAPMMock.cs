// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;

namespace System.Net.Sockets.Tests
{
    public class SocketTestServerAPM : SocketTestServer
    {
        protected override int Port { get { throw new NotSupportedException(); } }

        public SocketTestServerAPM(int numConnections, int receiveBufferSize, EndPoint localEndPoint)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotSupportedException();
        }
    }
}

