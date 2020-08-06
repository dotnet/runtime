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
        private readonly ISocketStreamProvider _streamProvider;
        private readonly Lazy<Stream> _stream;
        private readonly Lazy<IDuplexPipe> _pipe;
        private readonly IConnectionProperties? _options;


        public override EndPoint? RemoteEndPoint => _socket.RemoteEndPoint;
        public override EndPoint? LocalEndPoint => _socket.LocalEndPoint;
        public override IConnectionProperties ConnectionProperties => this;

        public SocketConnection(Socket socket, ISocketStreamProvider streamProvider, IConnectionProperties? options)
        {
            _socket = socket;
            _streamProvider = streamProvider;
            _options = options;
            _stream = new Lazy<Stream>(() => _streamProvider.CreateStream(socket, this), true);
            _pipe = new Lazy<IDuplexPipe>(() => _streamProvider.CreatePipe(socket, this), true);
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

                if (_stream.IsValueCreated) _stream.Value.Dispose();
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

        protected override Stream CreateStream() => _stream.Value;
        protected override IDuplexPipe CreatePipe() => _pipe.Value;

        bool IConnectionProperties.TryGet(Type propertyKey, [NotNullWhen(true)] out object? property)
        {
            if (propertyKey == typeof(Socket))
            {
                property = _stream;
                return true;
            }

            if (_options != null)
            {
                return _options.TryGet(propertyKey, out property);
            }

            property = null;
            return false;
        }

        internal interface ISocketStreamProvider
        {
            Stream CreateStream(Socket socket, IConnectionProperties connectionProperties);
            IDuplexPipe CreatePipe(Socket socket, IConnectionProperties connectionProperties);
        }
    }
}
