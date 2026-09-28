// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    internal sealed class PooledDeflateStream : Stream
    {
        private const int BufferSize = 8192;
        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly CompressionLevel _compressionLevel;
        private DeflateEncoder? _encoder;
        private byte[]? _buffer;
        private volatile bool _activeAsyncOperation;
        private bool _disposed;
        private bool _encoderFaulted;

        internal PooledDeflateStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (!stream.CanWrite)
            {
                throw new ArgumentException(SR.NotSupported_UnwritableStream, nameof(stream));
            }

            _stream = stream;
            _leaveOpen = leaveOpen;
            _compressionLevel = compressionLevel;
            _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            _encoder = DeflateEncoderPool.Rent(compressionLevel);
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => !_disposed && _stream.CanWrite;

        public override long Length => throw new NotSupportedException(SR.SeekingNotSupported);

        public override long Position
        {
            get => throw new NotSupportedException(SR.SeekingNotSupported);
            set => throw new NotSupportedException(SR.SeekingNotSupported);
        }

        public override void Flush()
        {
            EnsureNotDisposed();
            EnsureNoActiveAsyncOperation();
            FlushEncoder();
            _stream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            EnsureNotDisposed();
            EnsureNoActiveAsyncOperation();
            return FlushAsyncCore(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException(SR.ReadingNotSupported);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(SR.SeekingNotSupported);

        public override void SetLength(long value) => throw new NotSupportedException(SR.SetLengthRequiresSeekingAndWriting);

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            EnsureNotDisposed();
            EnsureNoActiveAsyncOperation();

            if (buffer.IsEmpty)
            {
                return;
            }

            WriteEncoder(buffer);
        }

        public override void WriteByte(byte value) => Write(new ReadOnlySpan<byte>(in value));

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            return cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled(cancellationToken) :
                buffer.IsEmpty ? default :
                WriteEncoderAsync(buffer, cancellationToken);
        }

        private void WriteEncoder(ReadOnlySpan<byte> source)
        {
            DeflateEncoder encoder = _encoder!;
            byte[] buffer = _buffer!;

            try
            {
                while (true)
                {
                    OperationStatus status = encoder.Compress(source, buffer, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);
                    if (bytesWritten > 0)
                    {
                        _stream.Write(buffer, 0, bytesWritten);
                    }

                    source = source[bytesConsumed..];
                    if (source.IsEmpty && status != OperationStatus.DestinationTooSmall)
                    {
                        return;
                    }
                }
            }
            catch
            {
                _encoderFaulted = true;
                throw;
            }
        }

        private async ValueTask WriteEncoderAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            AsyncOperationStarting();
            try
            {
                DeflateEncoder encoder = _encoder!;
                byte[] buffer = _buffer!;

                while (true)
                {
                    OperationStatus status = encoder.Compress(source.Span, buffer, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);
                    if (bytesWritten > 0)
                    {
                        await _stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesWritten), cancellationToken).ConfigureAwait(false);
                    }

                    source = source[bytesConsumed..];
                    if (source.IsEmpty && status != OperationStatus.DestinationTooSmall)
                    {
                        return;
                    }
                }
            }
            catch
            {
                _encoderFaulted = true;
                throw;
            }
            finally
            {
                AsyncOperationCompleting();
            }
        }

        private void FlushEncoder()
        {
            DeflateEncoder encoder = _encoder!;
            byte[] buffer = _buffer!;

            try
            {
                while (true)
                {
                    OperationStatus status = encoder.Flush(buffer, out int bytesWritten);
                    if (bytesWritten > 0)
                    {
                        _stream.Write(buffer, 0, bytesWritten);
                    }

                    if (status != OperationStatus.DestinationTooSmall)
                    {
                        return;
                    }
                }
            }
            catch
            {
                _encoderFaulted = true;
                throw;
            }
        }

        private async Task FlushAsyncCore(CancellationToken cancellationToken)
        {
            AsyncOperationStarting();
            try
            {
                DeflateEncoder encoder = _encoder!;
                byte[] buffer = _buffer!;

                while (true)
                {
                    OperationStatus status = encoder.Flush(buffer, out int bytesWritten);
                    if (bytesWritten > 0)
                    {
                        await _stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesWritten), cancellationToken).ConfigureAwait(false);
                    }

                    if (status != OperationStatus.DestinationTooSmall)
                    {
                        break;
                    }
                }

                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                _encoderFaulted = true;
                throw;
            }
            finally
            {
                AsyncOperationCompleting();
            }
        }

        private void FinishEncoder()
        {
            DeflateEncoder encoder = _encoder!;
            byte[] buffer = _buffer!;

            try
            {
                while (true)
                {
                    OperationStatus status = encoder.Compress(ReadOnlySpan<byte>.Empty, buffer, out int _, out int bytesWritten, isFinalBlock: true);
                    if (bytesWritten > 0)
                    {
                        _stream.Write(buffer, 0, bytesWritten);
                    }

                    if (status != OperationStatus.DestinationTooSmall)
                    {
                        return;
                    }
                }
            }
            catch
            {
                _encoderFaulted = true;
                throw;
            }
        }

        private async ValueTask FinishEncoderAsync()
        {
            DeflateEncoder encoder = _encoder!;
            byte[] buffer = _buffer!;

            try
            {
                while (true)
                {
                    OperationStatus status = encoder.Compress(ReadOnlySpan<byte>.Empty, buffer, out int _, out int bytesWritten, isFinalBlock: true);
                    if (bytesWritten > 0)
                    {
                        await _stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesWritten)).ConfigureAwait(false);
                    }

                    if (status != OperationStatus.DestinationTooSmall)
                    {
                        return;
                    }
                }
            }
            catch
            {
                _encoderFaulted = true;
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                EnsureNoActiveAsyncOperation();
                try
                {
                    FinishEncoder();
                }
                finally
                {
                    try
                    {
                        if (!_leaveOpen)
                        {
                            _stream.Dispose();
                        }
                    }
                    finally
                    {
                        ReturnResources();
                    }
                }
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                EnsureNoActiveAsyncOperation();
                AsyncOperationStarting();
                try
                {
                    await FinishEncoderAsync().ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        if (!_leaveOpen)
                        {
                            await _stream.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        ReturnResources();
                        AsyncOperationCompleting();
                    }
                }
            }

            await base.DisposeAsync().ConfigureAwait(false);
        }

        private void ReturnResources()
        {
            DeflateEncoder? encoder = _encoder;
            _encoder = null;
            if (encoder is not null)
            {
                if (_encoderFaulted)
                {
                    encoder.Dispose();
                }
                else
                {
                    DeflateEncoderPool.Return(_compressionLevel, encoder);
                }
            }

            byte[]? buffer = _buffer;
            _buffer = null;
            if (buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            _disposed = true;
        }

        private void EnsureNotDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

        private void EnsureNoActiveAsyncOperation()
        {
            if (_activeAsyncOperation)
            {
                throw new InvalidOperationException(SR.InvalidBeginCall);
            }
        }

        private void AsyncOperationStarting()
        {
            if (Interlocked.Exchange(ref _activeAsyncOperation, true))
            {
                throw new InvalidOperationException(SR.InvalidBeginCall);
            }
        }

        private void AsyncOperationCompleting()
        {
            Debug.Assert(_activeAsyncOperation);
            _activeAsyncOperation = false;
        }

        private static class DeflateEncoderPool
        {
            private const int MaxPoolSize = 1;
            private static readonly EncoderPool s_optimal = new(ZLibNative.DefaultQuality);
            private static readonly EncoderPool s_fastest = new((int)ZLibNative.CompressionLevel.BestSpeed);
            private static readonly EncoderPool s_smallestSize = new((int)ZLibNative.CompressionLevel.BestCompression);

            internal static DeflateEncoder Rent(CompressionLevel compressionLevel) => GetPool(compressionLevel).Rent();

            internal static void Return(CompressionLevel compressionLevel, DeflateEncoder encoder) => GetPool(compressionLevel).Return(encoder);

            private static EncoderPool GetPool(CompressionLevel compressionLevel) => compressionLevel switch
            {
                CompressionLevel.Optimal => s_optimal,
                CompressionLevel.Fastest => s_fastest,
                CompressionLevel.SmallestSize => s_smallestSize,
                _ => throw new ArgumentOutOfRangeException(nameof(compressionLevel)),
            };

            private sealed class EncoderPool
            {
                private readonly Stack<DeflateEncoder> _encoders = new();
                private readonly int _quality;

                internal EncoderPool(int quality) => _quality = quality;

                internal DeflateEncoder Rent()
                {
                    lock (_encoders)
                    {
                        if (_encoders.TryPop(out DeflateEncoder? encoder))
                        {
                            return encoder;
                        }
                    }

                    return new DeflateEncoder(_quality, ZLibNative.DefaultWindowLog, CompressionFormat.Deflate);
                }

                internal void Return(DeflateEncoder encoder)
                {
                    encoder.Reset();

                    lock (_encoders)
                    {
                        if (_encoders.Count < MaxPoolSize)
                        {
                            _encoders.Push(encoder);
                            return;
                        }
                    }

                    encoder.Dispose();
                }
            }
        }
    }
}
