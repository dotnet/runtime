// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    }
}
