// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    /// <summary>
    /// TODO
    /// </summary>
    public class SocketsConnectionFactory : ConnectionFactory, SocketConnection.ISocketStreamProvider
    {
        private readonly AddressFamily _addressFamily;
        private readonly SocketType _socketType;
        private readonly ProtocolType _protocolType;

        // See Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        public SocketsConnectionFactory(
            AddressFamily addressFamily,
            SocketType socketType,
            ProtocolType protocolType)
        {
            _addressFamily = addressFamily;
            _socketType = socketType;
            _protocolType = protocolType;
        }

        // dual-mode IPv6 socket. See Socket(SocketType socketType, ProtocolType protocolType)
        public SocketsConnectionFactory(SocketType socketType, ProtocolType protocolType)
            : this(AddressFamily.InterNetworkV6, socketType, protocolType)
        {
        }

        // This must be thread-safe!
        public override async ValueTask<Connection> ConnectAsync(
            EndPoint? endPoint,
            IConnectionProperties? options = null,
            CancellationToken cancellationToken = default)
        {
            Socket socket = CreateSocket(_addressFamily, _socketType, _protocolType, endPoint, options);

            try
            {
                using var args = new TaskSocketAsyncEventArgs();
                args.RemoteEndPoint = endPoint;

                if (socket.ConnectAsync(args))
                {
                    using (cancellationToken.UnsafeRegister(o => Socket.CancelConnectAsync((SocketAsyncEventArgs)o!), args))
                    {
                        await args.Task.ConfigureAwait(false);
                    }
                }

                if (args.SocketError != SocketError.Success)
                {
                    if (args.SocketError == SocketError.OperationAborted && cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }

                    SocketException ex = new SocketException((int)args.SocketError);
                    throw NetworkErrorHelper.MapSocketException(ex);
                }

                return new SocketConnection(socket, this, options);
            }
            catch (SocketException socketException)
            {
                socket.Dispose();
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        // These exist to provide an easy way to shim the default behavior.
        // Note: Connect must call this to create its socket.
        protected virtual Socket CreateSocket(
            AddressFamily addressFamily,
            SocketType socketType,
            ProtocolType protocolType,
            EndPoint? endPoint,
            IConnectionProperties? options)
        {
            return new Socket(addressFamily, socketType, protocolType)
            {
                NoDelay = true
            };
        }

        protected virtual Stream CreateStream(Socket socket, IConnectionProperties? options) => new NetworkStream(socket, ownsSocket: true);

        protected virtual IDuplexPipe CreatePipe(Socket socket, IConnectionProperties? options) => new DuplexStreamPipe(CreateStream(socket, options));

        Stream SocketConnection.ISocketStreamProvider.CreateStream(Socket socket, IConnectionProperties options) => CreateStream(socket, options);

        IDuplexPipe SocketConnection.ISocketStreamProvider.CreatePipe(Socket socket, IConnectionProperties options) => CreatePipe(socket, options);

        private sealed class DuplexStreamPipe : IDuplexPipe
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
