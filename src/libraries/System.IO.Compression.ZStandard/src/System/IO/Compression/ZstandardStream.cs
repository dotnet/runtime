// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties used to compress and decompress streams by using the ZStandard data format specification.</summary>
    public sealed partial class ZStandardStream : Stream
    {
        private const int DefaultInternalBufferSize = 65536; // 64KB default buffer
        private Stream _stream;
        private byte[] _buffer;
        private readonly bool _leaveOpen;
        private readonly CompressionMode _mode;
        private volatile bool _activeRwOperation;

        /// <summary>Initializes a new instance of the <see cref="ZStandardStream" /> class by using the specified stream and compression mode.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress data to the stream or decompress data from the stream.</param>
        public ZStandardStream(Stream stream, CompressionMode mode) : this(stream, mode, leaveOpen: false) { }

        /// <summary>Initializes a new instance of the <see cref="ZStandardStream" /> class by using the specified stream and compression mode, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress data to the stream or decompress data from the stream.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after the <see cref="ZStandardStream" /> object is disposed; otherwise, <see langword="false" />.</param>
        public ZStandardStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            ArgumentNullException.ThrowIfNull(stream);

            _mode = mode;
            _leaveOpen = leaveOpen;

            switch (mode)
            {
                case CompressionMode.Compress:
                    if (!stream.CanWrite)
                    {
                        throw new ArgumentException(SR.Stream_FalseCanWrite, nameof(stream));
                    }
                    break;

                case CompressionMode.Decompress:
                    if (!stream.CanRead)
                    {
                        throw new ArgumentException(SR.Stream_FalseCanRead, nameof(stream));
                    }
                    break;

                default:
                    throw new ArgumentException(SR.ArgumentOutOfRange_Enum, nameof(mode));
            }

            _stream = stream;
            _buffer = ArrayPool<byte>.Shared.Rent(DefaultInternalBufferSize);
        }

        /// <summary>Gets a reference to the underlying stream.</summary>
        /// <value>A stream object that represents the underlying stream.</value>
        /// <exception cref="System.ObjectDisposedException">The underlying stream is closed.</exception>
        public Stream BaseStream
        {
            get
            {
                EnsureNotDisposed();
                return _stream;
            }
        }

        /// <summary>Gets a value indicating whether the stream supports reading while decompressing a file.</summary>
        /// <value><see langword="true" /> if the <see cref="CompressionMode" /> value is <c>Decompress,</c> and the underlying stream supports reading and is not closed; otherwise, <see langword="false" />.</value>
        public override bool CanRead => _mode == CompressionMode.Decompress && _stream?.CanRead == true;

        /// <summary>Gets a value indicating whether the stream supports writing.</summary>
        /// <value><see langword="true" /> if the <see cref="CompressionMode" /> value is <c>Compress,</c> and the underlying stream supports writing and is not closed; otherwise, <see langword="false" />.</value>
        public override bool CanWrite => _mode == CompressionMode.Compress && _stream?.CanWrite == true;

        /// <summary>Gets a value indicating whether the stream supports seeking.</summary>
        /// <value><see langword="false" /> in all cases.</value>
        public override bool CanSeek => false;

        /// <summary>This property is not supported and always throws a <see cref="NotSupportedException" />.</summary>
        /// <value>A long value.</value>
        /// <exception cref="NotSupportedException">This property is not supported on this stream.</exception>
        public override long Length => throw new NotSupportedException();

        /// <summary>This property is not supported and always throws a <see cref="NotSupportedException" />.</summary>
        /// <value>A long value.</value>
        /// <exception cref="NotSupportedException">This property is not supported on this stream.</exception>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>This operation is not supported and always throws a <see cref="NotSupportedException" />.</summary>
        /// <param name="offset">The byte offset relative to the <paramref name="origin" /> parameter.</param>
        /// <param name="origin">One of the <see cref="SeekOrigin" /> values that indicates the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        /// <exception cref="NotSupportedException">This property is not supported on this stream.</exception>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <summary>This operation is not supported and always throws a <see cref="NotSupportedException" />.</summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="NotSupportedException">This property is not supported on this stream.</exception>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>Releases the unmanaged resources used by the <see cref="ZStandardStream" /> and optionally releases the managed resources.</summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _stream != null)
                {
                    if (_mode == CompressionMode.Compress)
                    {
                        WriteCore(ReadOnlySpan<byte>.Empty, isFinalBlock: true, checkActiveRWOps: false);
                    }

                    if (!_leaveOpen)
                    {
                        _stream.Dispose();
                    }
                }
            }
            finally
            {
                ReleaseStateForDispose();
                base.Dispose(disposing);
            }
        }

        /// <summary>Asynchronously releases the unmanaged resources used by the <see cref="ZStandardStream" />.</summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public override async ValueTask DisposeAsync()
        {
            try
            {
                if (_stream != null)
                {
                    if (_mode == CompressionMode.Compress)
                    {
                        await WriteCoreAsync(ReadOnlyMemory<byte>.Empty, CancellationToken.None, isFinalBlock: true).ConfigureAwait(false);
                    }

                    if (!_leaveOpen)
                    {
                        await _stream.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                ReleaseStateForDispose();
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }

        private void ReleaseStateForDispose()
        {
            _stream = null!;
            _encoder.Dispose();
            _decoder.Dispose();

            byte[] buffer = _buffer;
            if (buffer != null)
            {
                _buffer = null!;
                if (!_activeRwOperation)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_stream == null, this);
        }

        private static void ThrowConcurrentRWOperation()
        {
            throw new InvalidOperationException(SR.ZStandardStream_ConcurrentRWOperation);
        }

        private void BeginRWOperation()
        {
            if (Interlocked.Exchange(ref _activeRwOperation, true))
            {
                ThrowConcurrentRWOperation();
            }
        }

        private void EndRWOperation()
        {
            Interlocked.Exchange(ref _activeRwOperation, false);
        }

        private void EnsureNoActiveRWOperation()
        {
            if (_activeRwOperation)
            {
                ThrowConcurrentRWOperation();
            }
        }
    }
}
