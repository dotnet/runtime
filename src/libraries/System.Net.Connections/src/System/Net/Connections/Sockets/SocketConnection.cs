// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    internal sealed class SocketConnection : Connection, IConnectionProperties
    {
        private readonly Socket _socket;
        private readonly SocketsConnectionFactory _factory;
        private readonly IConnectionProperties? _options;
        private Stream? _stream;
        private IDuplexPipe? _pipe;

        public override EndPoint? RemoteEndPoint => _socket.RemoteEndPoint;
        public override EndPoint? LocalEndPoint => _socket.LocalEndPoint;
        public override IConnectionProperties ConnectionProperties => this;

        public SocketConnection(Socket socket, SocketsConnectionFactory factory, IConnectionProperties? options)
        {
            _socket = socket;
            _factory = factory;
            _options = options;
        }

        protected override ValueTask CloseAsyncCore(ConnectionCloseMethod method, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (method != ConnectionCloseMethod.GracefulShutdown)
                {
                    // Dispose must be called first in order to cause a connection reset,
                    // as NetworkStream.Dispose() will call Shutdown(Both).
                    _socket.Dispose();
                }

                _stream?.Dispose();
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

        bool IConnectionProperties.TryGet(Type propertyKey, [NotNullWhen(true)] out object? property)
        {
            if (propertyKey == typeof(Socket))
            {
                property = _socket;
                return true;
            }

            property = null;
            return false;
        }

        protected override Stream CreateStream() => _stream ??= _factory.CreateStreamForConnection(_socket, _options);

        protected override IDuplexPipe CreatePipe() => _pipe ??= _factory.CreatePipeForConnection(_socket, _options);
    }
}
