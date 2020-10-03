// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Mock
{
    internal sealed class MockStream : QuicStreamProvider
    {
        private bool _disposed;
        private readonly long _streamId;
        private bool _isOutbound;
        private bool _isBidirectional;

        private MockConnection? _connection;

        private StreamState? _streamState;

        // Constructor for outbound streams
        internal MockStream(MockConnection connection, long streamId, bool bidirectional)
        {
            _connection = connection;
            _streamId = streamId;
            _isOutbound = true;
            _isBidirectional = bidirectional;
        }

        // Constructor for inbound streams
        internal MockStream(StreamState streamState, long streamId, bool bidirectional)
        {
            _streamState = streamState;
            _streamId = streamId;
            _isOutbound = false;
            _isBidirectional = bidirectional;
        }

        private ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            Debug.Assert(_connection != null, "Stream not connected but no connection???");

            _streamState = _connection.CreateOutboundStream();

            // Don't need to hold on to the connection any longer.
            _connection = null;
        }

        internal override long StreamId
        {
            get
            {
                CheckDisposed();
                return _streamId;
            }
        }

        internal override bool CanRead => !_isOutbound || _isBidirectional;

        internal override int Read(Span<byte> buffer)
        {
            CheckDisposed();

            if (!_canRead)
            {
                throw new NotSupportedException();
            }

            return _socket!.Receive(buffer);
        }

        internal override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (!_canRead)
            {
                throw new NotSupportedException();
            }

            if (_socket == null)
            {
                await ConnectAsync(cancellationToken).ConfigureAwait(false);
            }

            return await _socket!.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
        }

        internal override bool CanWrite => _isOutbound || _isBidirectional;

        internal override void Write(ReadOnlySpan<byte> buffer)
        {
            CheckDisposed();

            if (!_canWrite)
            {
                throw new NotSupportedException();
            }

            _socket!.Send(buffer);
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffer, endStream: false, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (!_canWrite)
            {
                throw new NotSupportedException();
            }

            if (_socket == null)
            {
                await ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
            await _socket!.SendAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);

            if (endStream)
            {
                _socket!.Shutdown(SocketShutdown.Send);
            }
        }

        internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffers, endStream: false, cancellationToken);
        }
        internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (!_canWrite)
            {
                throw new NotSupportedException();
            }

            if (_socket == null)
            {
                await ConnectAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                await _socket!.SendAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            }

            if (endStream)
            {
                _socket!.Shutdown(SocketShutdown.Send);
            }
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffers, endStream: false, cancellationToken);
        }
        internal override async ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (!_canWrite)
            {
                throw new NotSupportedException();
            }

            if (_socket == null)
            {
                await ConnectAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (ReadOnlyMemory<byte> buffer in buffers.ToArray())
            {
                await _socket!.SendAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            }

            if (endStream)
            {
                _socket!.Shutdown(SocketShutdown.Send);
            }
        }

        internal override void Flush()
        {
            CheckDisposed();
        }

        internal override Task FlushAsync(CancellationToken cancellationToken)
        {
            CheckDisposed();

            return Task.CompletedTask;
        }

        internal override void AbortRead(long errorCode)
        {
            throw new NotImplementedException();
        }

        internal override void AbortWrite(long errorCode)
        {
            throw new NotImplementedException();
        }


        internal override ValueTask ShutdownWriteCompleted(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            return default;
        }

        internal override void Shutdown()
        {
            CheckDisposed();

            _socket!.Shutdown(SocketShutdown.Send);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(QuicStream));
            }
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _socket?.Dispose();
                _socket = null;
            }
        }

        public override ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;

                _socket?.Dispose();
                _socket = null;
            }

            return default;
        }

        internal sealed class StreamState
        {
            public StreamBuffer _outboundStreamBuffer;
            public StreamBuffer? _inboundStreamBuffer;

            public StreamState(bool bidirectional)
            {
                _outboundStreamBuffer = new StreamBuffer();
                _inboundStreamBuffer = (bidirectional ? new StreamBuffer() : null);
            }
        }
    }
}
