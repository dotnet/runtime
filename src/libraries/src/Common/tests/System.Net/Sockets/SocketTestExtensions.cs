// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets.Tests
{
    static class SocketTestExtensions
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
            TcpListener listener = TcpListener.Create(0);
            listener.Start();
            port = ((IPEndPoint)listener.LocalEndpoint).Port;
            return listener;
        }

        public static void DoAsyncCall(
            this Socket socket,
            Func<Socket, SocketAsyncEventArgs, bool> call,
            EventHandler<SocketAsyncEventArgs> callback,
            SocketAsyncEventArgs args)
        {
            bool willRaiseEvent = call(socket, args);
            if (!willRaiseEvent)
            {
                callback(null, args);
            }
        }
    }
}
