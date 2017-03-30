// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;

namespace System.Net.Sockets.Tests
{
    public class SocketTestServerAPM : SocketTestServer
    {
        protected override int Port { get { throw new NotSupportedException(); } }
        public override EndPoint EndPoint { get { throw new NotSupportedException(); } }

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

