// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    // This is done to couple disposal of the SocketConnection and the NetworkStream.
    internal sealed class SocketConnectionNetworkStream : NetworkStream
    {
        private readonly SocketConnection _connection;

        public SocketConnectionNetworkStream(Socket socket, SocketConnection connection) : base(socket, ownsSocket: true)
        {
            _connection = connection;
        }

        public void DisposeWithoutClosingConnection()
        {
            base.Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // This will call base.Dispose().
                _connection.Dispose();
            }
            else
            {
                base.Dispose(disposing);
            }
        }

        public override ValueTask DisposeAsync()
        {
            // This will call base.Dispose().
            Dispose(true);
            return default;
        }
    }
}
