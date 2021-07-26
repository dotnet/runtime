// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

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
        public event Action<Socket> Accepted;

        public static SocketTestServer SocketTestServerFactory(SocketImplementationType type, EndPoint endpoint, ProtocolType protocolType = ProtocolType.Tcp)
        {
            return SocketTestServerFactory(type, DefaultNumConnections, DefaultReceiveBufferSize, endpoint, protocolType);
        }

        public static SocketTestServer SocketTestServerFactory(SocketImplementationType type, IPAddress address, out int port)
        {
            return SocketTestServerFactory(type, DefaultNumConnections, DefaultReceiveBufferSize, address, out port);
        }

        public static SocketTestServer SocketTestServerFactory(SocketImplementationType type, IPAddress address)
            => SocketTestServerFactory(type, address, out _);

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

        protected void NotifyAccepted(Socket socket) => Accepted?.Invoke(socket);

        public static Socket CreateListenerSocketWithDualSafeGuard(
            AddressFamily addressFamily,
            ProtocolType protocolType,
            EndPoint listenEp,
            bool dualMode,
            out Socket guard)
        {
            bool mustCreateGuard = !PlatformDetection.IsWindows && // Do not guard on Windows, where port selection is deterministic
                protocolType == ProtocolType.Tcp &&
                listenEp is IPEndPoint ipEp &&
                ipEp.Port == 0 && // Only guard when we are binding to an anonymous port
                (ipEp.Address == IPAddress.Loopback || ipEp.Address == IPAddress.IPv6Loopback);

            Socket listener;
            if (!mustCreateGuard)
            {
                listener = CreateSocket();
                listener.Bind(listenEp);
                guard = null;
                return listener;
            }

            for (int attempt = 0; attempt < 10; attempt++)
            {
                listener = CreateSocket();
                (guard, IPAddress guardAddress) = CreateGuard();

                listener.Bind(listenEp);
                int port = ((IPEndPoint)listener.LocalEndPoint).Port;

                try
                {
                    guard.Bind(new IPEndPoint(guardAddress, port));
                    return listener;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    listener.Dispose();
                    guard.Dispose();
                }
            }

            throw new Exception("CreateListenerSocketWithDualSafeGuard exceeded the maximum number of attempts");

            Socket CreateSocket() => dualMode ?
                new Socket(SocketType.Stream, protocolType) :
                new Socket(addressFamily, SocketType.Stream, protocolType);

            (Socket, IPAddress) CreateGuard() => addressFamily == AddressFamily.InterNetwork ?
                (new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, protocolType), IPAddress.IPv6Loopback) :
                (new Socket(AddressFamily.InterNetwork, SocketType.Stream, protocolType), IPAddress.Loopback);

        }
    }
}
