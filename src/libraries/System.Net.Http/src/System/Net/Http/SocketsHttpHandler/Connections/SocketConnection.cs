// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    internal sealed class SocketConnection : Connection, IConnectionProperties
    {
        private readonly NetworkStream _stream;

        public override EndPoint? RemoteEndPoint => _stream.Socket.RemoteEndPoint;
        public override EndPoint? LocalEndPoint => _stream.Socket.LocalEndPoint;
        public override IConnectionProperties ConnectionProperties => this;

        public SocketConnection(Socket socket)
        {
            _stream = new NetworkStream(socket, ownsSocket: true);
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

                _stream.Dispose();
            }
            catch (SocketException socketException)
            {
                return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(NetworkErrorHelper.MapSocketException(socketException)));
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
    }
}
