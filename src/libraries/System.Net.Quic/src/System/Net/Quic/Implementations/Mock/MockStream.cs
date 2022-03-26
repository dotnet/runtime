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
        private readonly MockConnection _connection;

        private readonly StreamState _streamState;
        private bool _writesCanceled;

        internal MockStream(MockConnection connection, StreamState streamState, bool isInitiator)
        {
            _connection = connection;
            _streamState = streamState;
            _isInitiator = isInitiator;
        }

        private static ValueTask ConnectAsync(CancellationToken cancellationToken = default)
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

        internal override bool CanTimeout => false;

        internal override int ReadTimeout
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        internal override int WriteTimeout
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        internal override bool CanRead => !_disposed && ReadStreamBuffer is not null;

        internal override bool ReadsCompleted => ReadStreamBuffer?.IsComplete ?? false;

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
                if (_connection.ConnectionError is long connectonError)
                {
                    throw new QuicConnectionAbortedException(connectonError);
                }

                long errorCode = _isInitiator ? _streamState._inboundReadErrorCode : _streamState._outboundReadErrorCode;
                if (errorCode != 0)
                {
                    throw (errorCode == -1) ? new QuicOperationAbortedException() : new QuicStreamAbortedException(errorCode);
                }
            }

            return bytesRead;
        }

        private StreamBuffer? WriteStreamBuffer => _isInitiator ? _streamState._outboundStreamBuffer : _streamState._inboundStreamBuffer;

        internal override bool CanWrite => !_disposed && WriteStreamBuffer is not null;

        internal override void Write(ReadOnlySpan<byte> buffer)
        {
            CheckDisposed();
            if (Volatile.Read(ref _writesCanceled))
            {
                throw new OperationCanceledException();
            }

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
            if (Volatile.Read(ref _writesCanceled))
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new OperationCanceledException();
            }

            StreamBuffer? streamBuffer = WriteStreamBuffer;
            if (streamBuffer is null)
            {
                throw new NotSupportedException();
            }

            if (_connection.ConnectionError is long connectonError)
            {
                throw new QuicConnectionAbortedException(connectonError);
            }

            long errorCode = _isInitiator ? _streamState._inboundWriteErrorCode : _streamState._outboundWriteErrorCode;
            if (errorCode != 0)
            {
                throw new QuicStreamAbortedException(errorCode);
            }

            using var registration = cancellationToken.UnsafeRegister(static s =>
            {
                var stream = (MockStream)s!;
                Volatile.Write(ref stream._writesCanceled, true);
            }, this);

            await streamBuffer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (endStream)
            {
                streamBuffer.EndWrite();
                WritesCompletedTcs.TrySetResult();
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
            if (_isInitiator)
            {
                _streamState._outboundWriteErrorCode = errorCode;
                _streamState._inboundWritesCompletedTcs.TrySetException(new QuicStreamAbortedException(errorCode));
            }
            else
            {
                _streamState._inboundWriteErrorCode = errorCode;
                _streamState._outboundWritesCompletedTcs.TrySetException(new QuicOperationAbortedException());
            }

            ReadStreamBuffer?.AbortRead();
        }

        internal override void AbortWrite(long errorCode)
        {
            if (_isInitiator)
            {
                _streamState._outboundReadErrorCode = errorCode;
                _streamState._outboundWritesCompletedTcs.TrySetException(new QuicStreamAbortedException(errorCode));
            }
            else
            {
                _streamState._inboundReadErrorCode = errorCode;
                _streamState._inboundWritesCompletedTcs.TrySetException(new QuicOperationAbortedException());
            }

            WriteStreamBuffer?.EndWrite();
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

            if (_streamState._inboundStreamBuffer is null) // unidirectional stream
            {
                _connection.LocalStreamLimit!.Unidirectional.Decrement();
            }
            else
            {
                _connection.LocalStreamLimit!.Bidirectional.Decrement();
            }

            WritesCompletedTcs.TrySetResult();
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

        internal override ValueTask WaitForWriteCompletionAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            return new ValueTask(WritesCompletedTcs.Task);
        }

        private TaskCompletionSource WritesCompletedTcs => _isInitiator
            ? _streamState._outboundWritesCompletedTcs
            : _streamState._inboundWritesCompletedTcs;

        internal sealed class StreamState
        {
            public readonly long _streamId;
            public StreamBuffer _outboundStreamBuffer;
            public StreamBuffer? _inboundStreamBuffer;
            public long _outboundReadErrorCode;
            public long _inboundReadErrorCode;
            public long _outboundWriteErrorCode;
            public long _inboundWriteErrorCode;
            public TaskCompletionSource _outboundWritesCompletedTcs;
            public TaskCompletionSource _inboundWritesCompletedTcs;

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
                _outboundWritesCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _inboundWritesCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }
}
