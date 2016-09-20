// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets.Tests
{
    // Each individual test must configure this class by defining s_implementationType within 
    // SocketTestServer.DefaultFactoryConfiguration.cs
    public abstract partial class SocketTestServer : IDisposable
    {
        private const int DefaultNumConnections = 5;
        private const int DefaultReceiveBufferSize = 1024;

        protected abstract int Port { get; }
        public abstract EndPoint EndPoint { get; }

        public static SocketTestServer SocketTestServerFactory(SocketImplementationType type, EndPoint endpoint, ProtocolType protocolType = ProtocolType.Tcp)
        {
            return SocketTestServerFactory(type, DefaultNumConnections, DefaultReceiveBufferSize, endpoint, protocolType);
        }

        public static SocketTestServer SocketTestServerFactory(SocketImplementationType type, IPAddress address, out int port)
        {
            return SocketTestServerFactory(type, DefaultNumConnections, DefaultReceiveBufferSize, address, out port);
        }

        public static SocketTestServer SocketTestServerFactory(
            SocketImplementationType type,
            int numConnections,
            int receiveBufferSize,
            EndPoint localEndPoint,
            ProtocolType protocolType = ProtocolType.Tcp)
        {
            switch (type)
            {
                case SocketImplementationType.APM:
                    return new SocketTestServerAPM(numConnections, receiveBufferSize, localEndPoint);
                case SocketImplementationType.Async:
                    return new SocketTestServerAsync(numConnections, receiveBufferSize, localEndPoint, protocolType);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public static SocketTestServer SocketTestServerFactory(
            SocketImplementationType type,
            int numConnections,
            int receiveBufferSize,
            IPAddress address,
            out int port)
        {
            SocketTestServer server = SocketTestServerFactory(type, numConnections, receiveBufferSize, new IPEndPoint(address, 0));
            port = server.Port;
            return server;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}
