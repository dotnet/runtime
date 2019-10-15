// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
