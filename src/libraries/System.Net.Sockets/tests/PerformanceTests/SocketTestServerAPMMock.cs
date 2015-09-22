using System;
using System.Net;

namespace System.Net.Sockets.Tests
{
    public class SocketTestServerAPM : SocketTestServer
    {
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

