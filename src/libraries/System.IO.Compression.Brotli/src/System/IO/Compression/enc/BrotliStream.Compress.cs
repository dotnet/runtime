// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties used to compress and decompress streams by using the Brotli data format specification.</summary>
    public sealed partial class BrotliStream : Stream
    {
        private BrotliEncoder _encoder;

        /// <summary>Initializes a new instance of the <see cref="System.IO.Compression.BrotliStream" /> class by using the specified stream and compression level.</summary>
        /// <param name="stream">The stream to compress.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency when compressing the stream.</param>
        public BrotliStream(Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, leaveOpen: false) { }
        /// <summary>Initializes a new instance of the <see cref="System.IO.Compression.BrotliStream" /> class by using the specified stream and compression level, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream to compress.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency when compressing the stream.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after disposing the <see cref="System.IO.Compression.BrotliStream" /> object; otherwise, <see langword="false" />.</param>
        public BrotliStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen) : this(stream, CompressionMode.Compress, leaveOpen)
        {
            _encoder.SetQuality(BrotliUtils.GetQualityFromCompressionLevel(compressionLevel));
        }

        /// <summary>Writes compressed bytes to the underlying stream from the specified byte array.</summary>
        /// <param name="buffer">The buffer containing the data to compress.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> from which the bytes will be read.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <exception cref="System.ObjectDisposedException">The write operation cannot be performed because the stream is closed.</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            WriteCore(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        /// <exception cref="InvalidOperationException">Cannot perform write operations on a <see cref="BrotliStream" /> constructed with <see cref="CompressionMode.Decompress" />.
        /// -or-
        /// The encoder ran into invalid data.</exception>
        public override void WriteByte(byte value)
        {
            WriteCore(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
        }

        /// <summary>Writes a sequence of bytes to the current Brotli stream from a read-only byte span and advances the current position within this Brotli stream by the number of bytes written.</summary>
        /// <param name="buffer">A region of memory. This method copies the contents of this region to the current Brotli stream.</param>
        /// <remarks>Use the <see cref="System.IO.Compression.BrotliStream.CanWrite" /> property to determine whether the current instance supports writing. Use the <see langword="System.IO.Compression.BrotliStream.WriteAsync" /> method to write asynchronously to the current stream.
        /// If the write operation is successful, the position within the Brotli stream advances by the number of bytes written. If an exception occurs, the position within the Brotli stream remains unchanged.</remarks>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            WriteCore(buffer);
        }

        internal void WriteCore(ReadOnlySpan<byte> buffer, bool isFinalBlock = false)
        {
            if (_mode != CompressionMode.Compress)
                throw new InvalidOperationException(SR.BrotliStream_Decompress_UnsupportedOperation);
            EnsureNotDisposed();

            OperationStatus lastResult = OperationStatus.DestinationTooSmall;
            Span<byte> output = new Span<byte>(_buffer);
            while (lastResult == OperationStatus.DestinationTooSmall)
            {
                int bytesConsumed;
                int bytesWritten;
                lastResult = _encoder.Compress(buffer, output, out bytesConsumed, out bytesWritten, isFinalBlock);
                if (lastResult == OperationStatus.InvalidData)
                    throw new InvalidOperationException(SR.BrotliStream_Compress_InvalidData);
                if (bytesWritten > 0)
                    _stream.Write(output.Slice(0, bytesWritten));
                if (bytesConsumed > 0)
                    buffer = buffer.Slice(bytesConsumed);
            }
        }

        /// <summary>Begins an asynchronous write operation. (Consider using the <see cref="System.IO.Stream.WriteAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="buffer">The buffer from which data will be written.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> at which to begin writing data from the stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="asyncCallback">An optional asynchronous callback, to be called when the write operation is complete.</param>
        /// <param name="asyncState">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <returns>An object that represents the asynchronous write operation, which could still be pending.</returns>
        /// <exception cref="System.IO.IOException">The method tried to write asynchronously past the end of the stream, or a disk error occurred.</exception>
        /// <exception cref="System.ArgumentException">One or more of the arguments is invalid.</exception>
        /// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <exception cref="System.NotSupportedException">The current <see cref="System.IO.Compression.BrotliStream" /> implementation does not support the write operation.</exception>
        /// <exception cref="System.InvalidOperationException">The write operation cannot be performed because the stream is closed.</exception>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

        /// <summary>Handles the end of an asynchronous write operation. (Consider using the <see cref="System.IO.Stream.WriteAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="asyncResult">The object that represents the asynchronous call.</param>
        /// <exception cref="System.InvalidOperationException">The underlying stream is closed or <see langword="null" />.</exception>
        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        /// <summary>Asynchronously writes compressed bytes to the underlying Brotli stream from the specified byte array.</summary>
        /// <param name="buffer">The buffer that contains the data to compress.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> from which to begin copying bytes to the Brotli stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <remarks>The `WriteAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.BrotliStream.CanWrite" /> property to determine whether the current instance supports writing.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        /// <summary>Asynchronously writes compressed bytes to the underlying Brotli stream from the specified byte memory range.</summary>
        /// <param name="buffer">The memory region to write data from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <remarks>The `WriteAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.BrotliStream.CanWrite" /> property to determine whether the current instance supports writing.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_mode != CompressionMode.Compress)
                throw new InvalidOperationException(SR.BrotliStream_Decompress_UnsupportedOperation);
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            return cancellationToken.IsCancellationRequested ?
                ValueTask.FromCanceled(cancellationToken) :
                WriteAsyncMemoryCore(buffer, cancellationToken);
        }

        private async ValueTask WriteAsyncMemoryCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, bool isFinalBlock = false)
        {
            AsyncOperationStarting();
            try
            {
                OperationStatus lastResult = OperationStatus.DestinationTooSmall;
                while (lastResult == OperationStatus.DestinationTooSmall)
                {
                    Memory<byte> output = new Memory<byte>(_buffer);
                    int bytesConsumed = 0;
                    int bytesWritten = 0;
                    lastResult = _encoder.Compress(buffer, output, out bytesConsumed, out bytesWritten, isFinalBlock);
                    if (lastResult == OperationStatus.InvalidData)
                        throw new InvalidOperationException(SR.BrotliStream_Compress_InvalidData);
                    if (bytesConsumed > 0)
                        buffer = buffer.Slice(bytesConsumed);
                    if (bytesWritten > 0)
                        await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, bytesWritten), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                AsyncOperationCompleting();
            }
        }

        /// <summary>If the stream is not disposed, and the compression mode is set to compress, writes all the remaining encoder's data into this stream.</summary>
        /// <exception cref="InvalidDataException">The encoder ran into invalid data.</exception>
        /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
        public override void Flush()
        {
            EnsureNotDisposed();
            if (_mode == CompressionMode.Compress)
            {
                if (_encoder._state == null || _encoder._state.IsClosed)
                    return;

                OperationStatus lastResult = OperationStatus.DestinationTooSmall;
                Span<byte> output = new Span<byte>(_buffer);
                while (lastResult == OperationStatus.DestinationTooSmall)
                {
                    int bytesWritten;
                    lastResult = _encoder.Flush(output, out bytesWritten);
                    if (lastResult == OperationStatus.InvalidData)
                        throw new InvalidDataException(SR.BrotliStream_Compress_InvalidData);
                    if (bytesWritten > 0)
                    {
                        _stream.Write(output.Slice(0, bytesWritten));
                    }
                }

                _stream.Flush();
            }
        }

        /// <summary>Asynchronously clears all buffers for this Brotli stream, causes any buffered data to be written to the underlying device, and monitors cancellation requests.</summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        /// <remarks>If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            return _mode != CompressionMode.Compress ? Task.CompletedTask : FlushAsyncCore(cancellationToken);
        }

        private async Task FlushAsyncCore(CancellationToken cancellationToken)
        {
            AsyncOperationStarting();
            try
            {
                if (_encoder._state == null || _encoder._state.IsClosed)
                    return;

                OperationStatus lastResult = OperationStatus.DestinationTooSmall;
                while (lastResult == OperationStatus.DestinationTooSmall)
                {
                    Memory<byte> output = new Memory<byte>(_buffer);
                    int bytesWritten = 0;
                    lastResult = _encoder.Flush(output, out bytesWritten);
                    if (lastResult == OperationStatus.InvalidData)
                        throw new InvalidDataException(SR.BrotliStream_Compress_InvalidData);
                    if (bytesWritten > 0)
                        await _stream.WriteAsync(output.Slice(0, bytesWritten), cancellationToken).ConfigureAwait(false);
                }

                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                AsyncOperationCompleting();
            }
        }
    }
}
