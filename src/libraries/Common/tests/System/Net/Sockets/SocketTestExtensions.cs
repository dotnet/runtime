// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Net.Sockets.Tests
{
    internal static class SocketTestExtensions
    {
        // Binds to an IP address and OS-assigned port. Returns the chosen port.
        public static int BindToAnonymousPort(this Socket socket, IPAddress address)
        {
            socket.Bind(new IPEndPoint(address, 0));
            return ((IPEndPoint)socket.LocalEndPoint).Port;
        }

        // Binds to an OS-assigned port.
        public static TcpListener CreateAndStartTcpListenerOnAnonymousPort(out int port)
        {
            TcpListener listener = new TcpListener(IPAddress.IPv6Any, 0);
            listener.Server.DualMode = true;

            listener.Start();
            port = ((IPEndPoint)listener.LocalEndpoint).Port;
            return listener;
        }

        // On non-Windows platforms, once non-blocking is turned on (either explicitly
        // or by performing an async operation), we always stay in non-blocking mode.
        // Therefore, sync operation have to be simulated via async and explicit blocking.
        // Force us into this mode for testing purposes.
        public static void ForceNonBlocking(this Socket socket, bool force)
        {
            if (force)
            {
                socket.Blocking = false;
                socket.Blocking = true;
            }
        }

        public static (Socket, Socket) CreateConnectedSocketPair()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(listener.LocalEndPoint);
            Socket server = listener.Accept();

            return (client, server);
        }

        // Tries to connect within the provided timeout interval
        // Useful to speed up "can not connect" assertions on Windows
        public static bool TryConnect(this Socket socket, EndPoint remoteEndpoint, int millisecondsTimeout)
        {
            var mre = new ManualResetEventSlim(false);
            using var sea = new SocketAsyncEventArgs()
            {
                RemoteEndPoint = remoteEndpoint,
                UserToken = mre
            };

            sea.Completed += (s, e) => ((ManualResetEventSlim)e.UserToken).Set();

            bool pending = socket.ConnectAsync(sea);
            if (!pending || mre.Wait(millisecondsTimeout))
            {
                mre.Dispose();
                return sea.SocketError == SocketError.Success;
            }

            Socket.CancelConnectAsync(sea); // this will close the socket!

            // In case of time-out, ManualResetEventSlim is left undisposed to avoid race conditions,
            // letting SafeHandle's finalizer to do the cleanup.
            return false;
        }
    }
}
