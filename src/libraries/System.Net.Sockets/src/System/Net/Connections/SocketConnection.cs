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
        private readonly SocketsConnectionFactory _streamProvider;
        private Stream? _stream;
        private IDuplexPipe? _pipe;
        private readonly IConnectionProperties? _options;

        public override EndPoint? RemoteEndPoint => _socket.RemoteEndPoint;
        public override EndPoint? LocalEndPoint => _socket.LocalEndPoint;
        public override IConnectionProperties ConnectionProperties => this;

        public SocketConnection(Socket socket, SocketsConnectionFactory streamProvider, IConnectionProperties? options)
        {
            _socket = socket;
            _streamProvider = streamProvider;
            _options = options;
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
                    _socket.Dispose();
                }

                if (_stream != null)
                {
                    return _stream.DisposeAsync();
                }
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

        protected override Stream CreateStream()
        {
            if (_stream == null)
            {
                _stream = _streamProvider.CreateStreamForConnection(_socket, this);
            }
            return _stream;
        }

        protected override IDuplexPipe CreatePipe()
        {
            if (_pipe == null)
            {
                _pipe = _streamProvider.CreatePipeForConnection(_socket, this);
            }

            return _pipe;
        }

        bool IConnectionProperties.TryGet(Type propertyKey, [NotNullWhen(true)] out object? property)
        {
            if (propertyKey == typeof(Socket))
            {
                property = _socket;
                return true;
            }

            if (_options != null)
            {
                return _options.TryGet(propertyKey, out property);
            }

            property = null;
            return false;
        }
    }
}
