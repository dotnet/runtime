// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets.Tests
{
    // Each individual test must configure this class by defining s_implementationType within 
    // SocketTestServer.DefaultFactoryConfiguration.cs
    public abstract partial class SocketTestServer : IDisposable
    {
        private const int DefaultNumConnections = 5;
        private const int DefaultReceiveBufferSize = 1024;

        protected abstract int Port { get; }

        public static SocketTestServer SocketTestServerFactory(EndPoint endpoint)
        {
            return SocketTestServerFactory(DefaultNumConnections, DefaultReceiveBufferSize, endpoint);
        }

        public static SocketTestServer SocketTestServerFactory(IPAddress address, out int port)
        {
            return SocketTestServerFactory(DefaultNumConnections, DefaultReceiveBufferSize, address, out port);
        }

        public static SocketTestServer SocketTestServerFactory(
            int numConnections,
            int receiveBufferSize,
            EndPoint localEndPoint)
        {
            return SocketTestServerFactory(
                s_implementationType,
                numConnections,
                receiveBufferSize,
                localEndPoint);
        }

        public static SocketTestServer SocketTestServerFactory(
            int numConnections,
            int receiveBufferSize,
            IPAddress address,
            out int port)
        {
            return SocketTestServerFactory(
                s_implementationType,
                numConnections,
                receiveBufferSize,
                address,
                out port);
        }

        public static SocketTestServer SocketTestServerFactory(
            SocketImplementationType type,
            int numConnections,
            int receiveBufferSize,
            EndPoint localEndPoint)
        {
            switch (type)
            {
                case SocketImplementationType.APM:
                    return new SocketTestServerAPM(numConnections, receiveBufferSize, localEndPoint);
                case SocketImplementationType.Async:
                    return new SocketTestServerAsync(numConnections, receiveBufferSize, localEndPoint);
                default:
                    throw new ArgumentOutOfRangeException("type");
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
