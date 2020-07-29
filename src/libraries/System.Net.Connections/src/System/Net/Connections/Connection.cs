// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    /// <summary>
    /// A connection.
    /// </summary>
    public abstract class Connection : ConnectionBase
    {
        private Stream? _stream;
        private IDuplexPipe? _pipe;
        private bool _initializing;

        /// <summary>
        /// The connection's <see cref="Stream"/>.
        /// </summary>
        public Stream Stream =>
            _stream != null ? _stream :
            _pipe != null ? throw new InvalidOperationException(SR.net_connections_stream_use_after_pipe) :
            (_stream = CreateStream() ?? throw new InvalidOperationException(SR.net_connections_createstream_null));

        /// <summary>
        /// The connection's <see cref="IDuplexPipe"/>.
        /// </summary>
        public IDuplexPipe Pipe =>
            _pipe != null ? _pipe :
            _stream != null ? throw new InvalidOperationException(SR.net_connections_pipe_use_after_stream) :
            (_pipe = CreatePipe() ?? throw new InvalidOperationException(SR.net_connections_createpipe_null));

        /// <summary>
        /// Initializes the <see cref="Stream"/> for the <see cref="Connection"/>.
        /// </summary>
        /// <returns>A <see cref="Stream"/>.</returns>
        /// <remarks>
        /// At least one of <see cref="CreateStream"/> and <see cref="CreatePipe"/> must be overridden.
        /// If only <see cref="CreateStream"/> is overridden, a user accessing <see cref="Pipe"/> will get a <see cref="IDuplexPipe"/> wrapping the <see cref="Stream"/>.
        /// </remarks>
        protected virtual Stream CreateStream()
        {
            if (_initializing) throw new InvalidOperationException(SR.net_connections_no_create_overrides);

            try
            {
                _initializing = true;

                IDuplexPipe pipe = CreatePipe();
                if (pipe == null) throw new InvalidOperationException(SR.net_connections_createpipe_null);

                return new DuplexPipeStream(pipe);
            }
            finally
            {
                _initializing = false;
            }
        }

        /// <summary>
        /// Initializes the <see cref="Pipe"/> for the <see cref="Connection"/>.
        /// </summary>
        /// <returns>An <see cref="IDuplexPipe"/>.</returns>
        /// <remarks>
        /// At least one of <see cref="CreateStream"/> and <see cref="CreatePipe"/> must be overridden.
        /// If only <see cref="CreatePipe"/> is overridden, a user accessing <see cref="Stream"/> will get a <see cref="Stream"/> wrapping the <see cref="Pipe"/>.
        /// </remarks>
        protected virtual IDuplexPipe CreatePipe()
        {
            if (_initializing) throw new InvalidOperationException(SR.net_connections_no_create_overrides);

            try
            {
                _initializing = true;

                Stream stream = CreateStream();
                if (stream == null) throw new InvalidOperationException(SR.net_connections_createstream_null);

                return new DuplexStreamPipe(stream);
            }
            finally
            {
                _initializing = false;
            }
        }

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

        /// <summary>
        /// Creates a connection for a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The connection's <see cref="Connection.Stream"/>.</param>
        /// <param name="leaveOpen">If false, the <paramref name="stream"/> will be disposed of once the connection has been closed.</param>
        /// <param name="properties">The connection's <see cref="ConnectionBase.ConnectionProperties"/>.</param>
        /// <param name="localEndPoint">The connection's <see cref="ConnectionBase.LocalEndPoint"/>.</param>
        /// <param name="remoteEndPoint">The connection's <see cref="ConnectionBase.RemoteEndPoint"/>.</param>
        /// <returns>A new <see cref="Connection"/>.</returns>
        public static Connection FromStream(Stream stream, bool leaveOpen = false, IConnectionProperties? properties = null, EndPoint? localEndPoint = null, EndPoint? remoteEndPoint = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            return new ConnectionFromStream(stream, leaveOpen, properties, localEndPoint, remoteEndPoint);
        }

        /// <summary>
        /// Creates a connection for an <see cref="IDuplexPipe"/>.
        /// </summary>
        /// <param name="pipe">The connection's <see cref="Connection.Pipe"/>.</param>
        /// <param name="leaveOpen">If false and the <paramref name="pipe"/> implements <see cref="IAsyncDisposable"/> or <see cref="IDisposable"/>, it will be disposed of once the connection has been closed.</param>
        /// <param name="properties">The connection's <see cref="ConnectionBase.ConnectionProperties"/>.</param>
        /// <param name="localEndPoint">The connection's <see cref="ConnectionBase.LocalEndPoint"/>.</param>
        /// <param name="remoteEndPoint">The connection's <see cref="ConnectionBase.RemoteEndPoint"/>.</param>
        /// <returns>A new <see cref="Connection"/>.</returns>
        public static Connection FromPipe(IDuplexPipe pipe, bool leaveOpen = false, IConnectionProperties? properties = null, EndPoint? localEndPoint = null, EndPoint? remoteEndPoint = null)
        {
            if (pipe == null) throw new ArgumentNullException(nameof(pipe));
            return new ConnectionFromPipe(pipe, leaveOpen, properties, localEndPoint, remoteEndPoint);
        }

        private sealed class ConnectionFromStream : Connection, IConnectionProperties
        {
            private Stream? _originalStream;
            private IConnectionProperties? _properties;
            private readonly bool _leaveOpen;

            public override IConnectionProperties ConnectionProperties => _properties ?? this;

            public override EndPoint? LocalEndPoint { get; }

            public override EndPoint? RemoteEndPoint { get; }

            public ConnectionFromStream(Stream stream, bool leaveOpen, IConnectionProperties? properties, EndPoint? localEndPoint, EndPoint? remoteEndPoint)
            {
                _originalStream = stream;
                _leaveOpen = leaveOpen;
                _properties = properties;
                LocalEndPoint = localEndPoint;
                RemoteEndPoint = remoteEndPoint;
            }

            protected override Stream CreateStream() => _originalStream ?? throw new ObjectDisposedException(nameof(Connection));

            protected override async ValueTask CloseAsyncCore(ConnectionCloseMethod method, CancellationToken cancellationToken)
            {
                if (_originalStream == null)
                {
                    return;
                }

                if (method == ConnectionCloseMethod.GracefulShutdown)
                {
                    await _originalStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                if (!_leaveOpen)
                {
                    await _originalStream.DisposeAsync().ConfigureAwait(false);
                }

                _originalStream = null;
            }

            bool IConnectionProperties.TryGet(Type propertyKey, [NotNullWhen(true)] out object? property)
            {
                property = null;
                return false;
            }
        }

        private sealed class ConnectionFromPipe : Connection, IConnectionProperties
        {
            private IDuplexPipe? _originalPipe;
            private IConnectionProperties? _properties;
            private readonly bool _leaveOpen;

            public override IConnectionProperties ConnectionProperties => _properties ?? this;

            public override EndPoint? LocalEndPoint { get; }

            public override EndPoint? RemoteEndPoint { get; }

            public ConnectionFromPipe(IDuplexPipe pipe, bool leaveOpen, IConnectionProperties? properties, EndPoint? localEndPoint, EndPoint? remoteEndPoint)
            {
                _originalPipe = pipe;
                _leaveOpen = leaveOpen;
                _properties = properties;
                LocalEndPoint = localEndPoint;
                RemoteEndPoint = remoteEndPoint;
            }

            protected override IDuplexPipe CreatePipe() => _originalPipe ?? throw new ObjectDisposedException(nameof(Connection));

            protected override async ValueTask CloseAsyncCore(ConnectionCloseMethod method, CancellationToken cancellationToken)
            {
                if (_originalPipe == null)
                {
                    return;
                }

                Exception? inputException, outputException;

                if (method == ConnectionCloseMethod.GracefulShutdown)
                {
                    // Flush happens implicitly from CompleteAsync(null), so only flush here if we need cancellation.
                    if (cancellationToken.CanBeCanceled)
                    {
                        FlushResult r = await _originalPipe.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
                        if (r.IsCanceled) cancellationToken.ThrowIfCancellationRequested();
                    }

                    inputException = null;
                    outputException = null;
                }
                else
                {
                    inputException = ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(Connection)));
                    outputException = ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(Connection)));
                }

                await _originalPipe.Input.CompleteAsync(inputException).ConfigureAwait(false);
                await _originalPipe.Output.CompleteAsync(outputException).ConfigureAwait(false);

                if (!_leaveOpen)
                {
                    switch (_originalPipe)
                    {
                        case IAsyncDisposable d:
                            await d.DisposeAsync().ConfigureAwait(false);
                            break;
                        case IDisposable d:
                            d.Dispose();
                            break;
                    }
                }

                _originalPipe = null;
            }

            bool IConnectionProperties.TryGet(Type propertyKey, [NotNullWhen(true)] out object? property)
            {
                property = null;
                return false;
            }
        }
    }
}
