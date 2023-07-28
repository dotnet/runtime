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

        public static (Socket client, Socket server) CreateConnectedSocketPair(bool ipv6 = false, bool dualModeClient = false)
        {
            IPAddress serverAddress = ipv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;

            // PortBlocker creates a temporary socket of the opposite AddressFamily in the background, so parallel tests won't attempt
            // to create their listener sockets on the same port, regardless of address family.
            // This should prevent 'listener' from accepting DualMode connections of unrelated tests.
            using PortBlocker portBlocker = new PortBlocker(() =>
            {
                Socket l = new Socket(serverAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                l.BindToAnonymousPort(serverAddress);
                return l;
            });
            Socket listener = portBlocker.MainSocket; // PortBlocker shall dispose this
            listener.Listen(1);

            IPEndPoint connectTo = (IPEndPoint)listener.LocalEndPoint;
            if (dualModeClient) connectTo = new IPEndPoint(connectTo.Address.MapToIPv6(), connectTo.Port);

            Socket client = new Socket(connectTo.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (dualModeClient) client.DualMode = true;
            client.Connect(connectTo);
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

    /// <summary>
    /// A utility to create and bind a socket while blocking it's port for both IPv4 and IPv6
    /// by also creating and binding a "shadow" socket of the opposite address family.
    /// </summary>
    internal class PortBlocker : IDisposable
    {
        private const int MaxAttempts = 16;
        private Socket _shadowSocket;
        public Socket MainSocket { get; }

        public PortBlocker(Func<Socket> socketFactory)
        {
            bool success = false;
            for (int i = 0; i < MaxAttempts; i++)
            {
                MainSocket = socketFactory();
                if (MainSocket.LocalEndPoint is not IPEndPoint)
                {
                    MainSocket.Dispose();
                    throw new Exception($"{nameof(socketFactory)} is expected create and bind the socket.");
                }

                IPAddress shadowAddress = MainSocket.AddressFamily == AddressFamily.InterNetwork ?
                        IPAddress.IPv6Loopback :
                        IPAddress.Loopback;
                int port = ((IPEndPoint)MainSocket.LocalEndPoint).Port;
                IPEndPoint shadowEndPoint = new IPEndPoint(shadowAddress, port);

                try
                {
                    _shadowSocket = new Socket(shadowAddress.AddressFamily, MainSocket.SocketType, MainSocket.ProtocolType);
                    success = TryBindWithoutReuseAddress(_shadowSocket, shadowEndPoint, out _);

                    if (success) break;
                }
                catch (SocketException)
                {
                    MainSocket.Dispose();
                    _shadowSocket?.Dispose();
                }
            }

            if (!success)
            {
                throw new Exception($"Failed to create the 'shadow' (port blocker) socket in {MaxAttempts} attempts.");
            }
        }

        public void Dispose()
        {
            MainSocket.Dispose();
            _shadowSocket.Dispose();
        }

        // Socket.Bind() auto-enables SO_REUSEADDR on Unix to allow Bind() during TIME_WAIT to emulate Windows behavior, see SystemNative_Bind() in 'pal_networking.c'.
        // To prevent other sockets from succesfully binding to the same port port, we need to avoid this logic when binding the shadow socket.
        // This method is doing a custom P/Invoke to bind() on Unix to achieve that.
        private static unsafe bool TryBindWithoutReuseAddress(Socket socket, IPEndPoint endPoint, out int port)
        {
            if (PlatformDetection.IsWindows)
            {
                try
                {
                    socket.Bind(endPoint);
                }
                catch (SocketException)
                {
                    port = default;
                    return false;
                }

                port = ((IPEndPoint)socket.LocalEndPoint).Port;
                return true;
            }

            SocketAddress addr = endPoint.Serialize();
            byte[] data = new byte[addr.Size];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = addr[i];
            }

            fixed (byte* dataPtr = data)
            {
                int result = bind(socket.SafeHandle, (nint)dataPtr, (uint)data.Length);
                if (result != 0)
                {
                    port = default;
                    return false;
                }
                uint sockLen = (uint)data.Length;
                result = getsockname(socket.SafeHandle, (nint)dataPtr, (IntPtr)(&sockLen));
                if (result != 0)
                {
                    port = default;
                    return false;
                }

                addr = new SocketAddress(endPoint.AddressFamily, (int)sockLen);
            }

            for (int i = 0; i < data.Length; i++)
            {
                addr[i] = data[i];
            }

            port = ((IPEndPoint)endPoint.Create(addr)).Port;
            return true;

            [Runtime.InteropServices.DllImport("libc", SetLastError = true)]
            static extern int bind(SafeSocketHandle socket, IntPtr socketAddress, uint addrLen);

            [Runtime.InteropServices.DllImport("libc", SetLastError = true)]
            static extern int getsockname(SafeSocketHandle socket, IntPtr socketAddress, IntPtr addrLenPtr);
        }
    }
}
