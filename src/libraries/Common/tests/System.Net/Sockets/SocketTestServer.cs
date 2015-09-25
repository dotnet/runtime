// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets.Tests
{
    // Each individual test must configure this class by defining s_implementationType within 
    // SocketTestServer.DefaultFactoryConfiguration.cs
    public abstract partial class SocketTestServer : IDisposable
    {
        public static SocketTestServer SocketTestServerFactory(EndPoint endpoint)
        {
            return SocketTestServerFactory(5, 1024, endpoint);
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}
