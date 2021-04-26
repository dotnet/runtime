// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private readonly bool _isInitiator;

        private readonly StreamState _streamState;

        internal MockStream(StreamState streamState, bool isInitiator)
        {
            _streamState = streamState;
            _isInitiator = isInitiator;
        }

        private ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        internal override long StreamId
        {
            get
            {
                CheckDisposed();
                return _streamState._streamId;
            }
        }

        private StreamBuffer? ReadStreamBuffer => _isInitiator ? _streamState._inboundStreamBuffer : _streamState._outboundStreamBuffer;

        internal override bool CanRead => !_disposed && ReadStreamBuffer is not null;

        internal override int Read(Span<byte> buffer)
        {
            CheckDisposed();

            StreamBuffer? streamBuffer = ReadStreamBuffer;
            if (streamBuffer is null)
            {
                throw new NotSupportedException();
            }

            return streamBuffer.Read(buffer);
        }

        internal override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            StreamBuffer? streamBuffer = ReadStreamBuffer;
            if (streamBuffer is null)
            {
                throw new NotSupportedException();
            }

            int bytesRead = await streamBuffer.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                long errorCode = _isInitiator ? _streamState._inboundErrorCode : _streamState._outboundErrorCode;
                if (errorCode != 0)
                {
                    throw new QuicStreamAbortedException(errorCode);
                }
            }

            return bytesRead;
        }

        private StreamBuffer? WriteStreamBuffer => _isInitiator ? _streamState._outboundStreamBuffer : _streamState._inboundStreamBuffer;

        internal override bool CanWrite => !_disposed && WriteStreamBuffer is not null;

        internal override void Write(ReadOnlySpan<byte> buffer)
        {
            CheckDisposed();

            StreamBuffer? streamBuffer = WriteStreamBuffer;
            if (streamBuffer is null)
            {
                throw new NotSupportedException();
            }

            streamBuffer.Write(buffer);
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffer, endStream: false, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            StreamBuffer? streamBuffer = WriteStreamBuffer;
            if (streamBuffer is null)
            {
                throw new NotSupportedException();
            }

            await streamBuffer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (endStream)
            {
                streamBuffer.EndWrite();
            }
        }

        internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        internal override async ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < buffers.Length; i++)
            {
                await WriteAsync(buffers.Span[i], cancellationToken).ConfigureAwait(false);
            }
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
            if (_isInitiator)
            {
                _streamState._outboundErrorCode = errorCode;
            }
            else
            {
                _streamState._inboundErrorCode = errorCode;
            }

            WriteStreamBuffer?.EndWrite();
        }


        internal override ValueTask ShutdownWriteCompleted(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            return default;
        }


        internal override ValueTask ShutdownCompleted(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            return default;
        }

        internal override void Shutdown()
        {
            CheckDisposed();

            // This seems to mean shutdown send, in particular, not both.
            WriteStreamBuffer?.EndWrite();
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
                Shutdown();

                _disposed = true;
            }
        }

        public override ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                Shutdown();

                _disposed = true;
            }

            return default;
        }

        internal sealed class StreamState
        {
            public readonly long _streamId;
            public StreamBuffer _outboundStreamBuffer;
            public StreamBuffer? _inboundStreamBuffer;
            public long _outboundErrorCode;
            public long _inboundErrorCode;

            private const int InitialBufferSize =
#if DEBUG
                10;
#else
                4096;
#endif
            private const int MaxBufferSize =
#if DEBUG
                4096;
#else
                32 * 1024;
#endif
            public StreamState(long streamId, bool bidirectional)
            {
                _streamId = streamId;
                _outboundStreamBuffer = new StreamBuffer(initialBufferSize: InitialBufferSize, maxBufferSize: MaxBufferSize);
                _inboundStreamBuffer = (bidirectional ? new StreamBuffer() : null);
            }
        }
    }
}
