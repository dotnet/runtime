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
        private readonly IDataChannelProvider _factory;
        private Stream? _stream;
        private IDuplexPipe? _pipe;
        private readonly IConnectionProperties? _options;

        public override EndPoint? RemoteEndPoint => _socket.RemoteEndPoint;
        public override EndPoint? LocalEndPoint => _socket.LocalEndPoint;
        public override IConnectionProperties ConnectionProperties => this;

        public SocketConnection(Socket socket, IDataChannelProvider factory, IConnectionProperties? options)
        {
            _socket = socket;
            _factory = factory;
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
                _stream = _factory.CreateStreamForConnection(_socket, this);
            }
            return _stream;
        }

        protected override IDuplexPipe CreatePipe()
        {
            if (_pipe == null)
            {
                _pipe = _factory.CreatePipeForConnection(_socket, this);
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

        internal interface IDataChannelProvider
        {
            Stream CreateStreamForConnection(Socket socket, IConnectionProperties options);
            IDuplexPipe CreatePipeForConnection(Socket socket, IConnectionProperties options);
        }

        internal sealed class DuplexStreamPipe : IDuplexPipe
        {
            private static readonly StreamPipeReaderOptions s_readerOpts = new StreamPipeReaderOptions(leaveOpen: true);
            private static readonly StreamPipeWriterOptions s_writerOpts = new StreamPipeWriterOptions(leaveOpen: true);

            public DuplexStreamPipe(Stream stream)
            {
                Input = PipeReader.Create(stream, s_readerOpts);
                Output = PipeWriter.Create(stream, s_writerOpts);
            }

            public PipeReader Input { get; }

            public PipeWriter Output { get; }
        }
    }
}
