// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    internal sealed class SocketConnection : Connection, IConnectionProperties
    {
        private readonly SocketConnectionNetworkStream _stream;

        public override EndPoint? RemoteEndPoint => _stream.Socket.RemoteEndPoint;
        public override EndPoint? LocalEndPoint => _stream.Socket.LocalEndPoint;
        public override IConnectionProperties ConnectionProperties => this;

        public SocketConnection(Socket socket)
        {
            _stream = new SocketConnectionNetworkStream(socket, this);
        }

        protected override ValueTask CloseAsyncCore(ConnectionCloseMethod method, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            try
            {
                if (method != ConnectionCloseMethod.GracefulShutdown)
                {
                    // Dispose must be called first in order to cause a connection reset,
                    // as NetworkStream.Dispose() will call Shutdown(Both).
                    _stream.Socket.Dispose();
                }

                _stream.DisposeWithoutClosingConnection();
            }
            catch (Exception ex)
            {
                return ValueTask.FromException(ex);
            }

            return default;
        }

        protected override Stream CreateStream() => _stream;

        bool IConnectionProperties.TryGet(Type propertyKey, [NotNullWhen(true)] out object? property)
        {
            if (propertyKey == typeof(Socket))
            {
                property = _stream.Socket;
                return true;
            }

            property = null;
            return false;
        }

        // This is done to couple disposal of the SocketConnection and the NetworkStream.
        private sealed class SocketConnectionNetworkStream : NetworkStream
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
}
