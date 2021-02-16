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
        private BrotliDecoder _decoder;
        private int _bufferOffset;
        private int _bufferCount;

        /// <summary>Reads a number of decompressed bytes into the specified byte array.</summary>
        /// <param name="buffer">The array used to store decompressed bytes.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of decompressed bytes to read.</param>
        /// <returns>The number of bytes that were decompressed into the byte array. If the end of the stream has been reached, zero or the number of bytes read is returned.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.IO.Compression.CompressionMode" /> value was <see langword="Compress" /> when the object was created, or there is already an active asynchronous operation on this stream.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset" /> or <paramref name="count" /> is less than zero.</exception>
        /// <exception cref="System.ArgumentException">The <paramref name="buffer" /> length minus the index starting point is less than <paramref name="count" />.</exception>
        /// <exception cref="System.IO.InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="System.ObjectDisposedException">The underlying stream is null or closed.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return Read(new Span<byte>(buffer, offset, count));
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>The unsigned byte cast to an <see cref="int"/>, or -1 if at the end of the stream.</returns>
        /// <exception cref="InvalidOperationException">Cannot perform read operations on a <see cref="BrotliStream" /> constructed with <see cref="CompressionMode.Compress" />.
        /// -or-
        /// <see cref="BaseStream" /> returned more bytes than requested in read.</exception>
        public override int ReadByte()
        {
            byte b = default;
            int numRead = Read(MemoryMarshal.CreateSpan(ref b, 1));
            return numRead != 0 ? b : -1;
        }

        /// <summary>Reads a sequence of bytes from the current Brotli stream to a byte span and advances the position within the Brotli stream by the number of bytes read.</summary>
        /// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the current source.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        /// <remarks>Use the <see cref="System.IO.Compression.BrotliStream.CanRead" /> property to determine whether the current instance supports reading. Use the <see langword="System.IO.Compression.BrotliStream.ReadAsync" /> method to read asynchronously from the current stream.
        /// This method read a maximum of `buffer.Length` bytes from the current stream and store them in <paramref name="buffer" />. The current position within the Brotli stream is advanced by the number of bytes read; however, if an exception occurs, the current position within the Brotli stream remains unchanged. This method will block until at least one byte of data can be read, in the event that no data is available. `Read` returns 0 only when there is no more data in the stream and no more is expected (such as a closed socket or end of file). The method is free to return fewer bytes than requested even if the end of the stream has not been reached.
        /// Use <see cref="System.IO.BinaryReader" /> for reading primitive data types.</remarks>
        public override int Read(Span<byte> buffer)
        {
            if (_mode != CompressionMode.Decompress)
                throw new InvalidOperationException(SR.BrotliStream_Compress_UnsupportedOperation);
            EnsureNotDisposed();
            int totalWritten = 0;

            OperationStatus lastResult = OperationStatus.DestinationTooSmall;
            // We want to continue calling Decompress until we're either out of space for output or until Decompress indicates it is finished.
            while (buffer.Length > 0 && lastResult != OperationStatus.Done)
            {
                if (lastResult == OperationStatus.NeedMoreData)
                {
                    // Ensure any left over data is at the beginning of the array so we can fill the remainder.
                    if (_bufferCount > 0 && _bufferOffset != 0)
                    {
                        _buffer.AsSpan(_bufferOffset, _bufferCount).CopyTo(_buffer);
                    }
                    _bufferOffset = 0;

                    int numRead = 0;
                    while (_bufferCount < _buffer.Length && ((numRead = _stream.Read(_buffer, _bufferCount, _buffer.Length - _bufferCount)) > 0))
                    {
                        _bufferCount += numRead;
                        if (_bufferCount > _buffer.Length)
                        {
                            // The stream is either malicious or poorly implemented and returned a number of
                            // bytes larger than the buffer supplied to it.
                            throw new InvalidDataException(SR.BrotliStream_Decompress_InvalidStream);
                        }
                    }

                    if (_bufferCount <= 0)
                    {
                        break;
                    }
                }

                lastResult = _decoder.Decompress(new ReadOnlySpan<byte>(_buffer, _bufferOffset, _bufferCount), buffer, out int bytesConsumed, out int bytesWritten);
                if (lastResult == OperationStatus.InvalidData)
                {
                    throw new InvalidOperationException(SR.BrotliStream_Decompress_InvalidData);
                }

                if (bytesConsumed > 0)
                {
                    _bufferOffset += bytesConsumed;
                    _bufferCount -= bytesConsumed;
                }

                if (bytesWritten > 0)
                {
                    totalWritten += bytesWritten;
                    buffer = buffer.Slice(bytesWritten);
                }
            }

            return totalWritten;
        }

        /// <summary>Begins an asynchronous read operation. (Consider using the <see cref="System.IO.Stream.ReadAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="buffer">The buffer from which data will be read.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> at which to begin reading data from the stream.</param>
        /// <param name="count">To maximum number of bytes to read.</param>
        /// <param name="asyncCallback">An optional asynchronous callback, to be called when the read operation is complete.</param>
        /// <param name="asyncState">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>An object that represents the asynchronous read operation, which could still be pending.</returns>
        /// <exception cref="System.IO.IOException">The method tried to read asynchronously past the end of the stream, or a disk error occurred.</exception>
        /// <exception cref="System.ArgumentException">One or more of the arguments is invalid.</exception>
        /// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <exception cref="System.NotSupportedException">The current <see cref="System.IO.Compression.BrotliStream" /> implementation does not support the read operation.</exception>
        /// <exception cref="System.InvalidOperationException">This call cannot be completed.</exception>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

        /// <summary>Waits for the pending asynchronous read to complete. (Consider using the <see cref="System.IO.Stream.ReadAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns>The number of bytes read from the stream, between 0 (zero) and the number of bytes you requested. <see cref="System.IO.Compression.BrotliStream" /> returns 0 only at the end of the stream; otherwise, it blocks until at least one byte is available.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="asyncResult" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="asyncResult" /> did not originate from a <see cref="System.IO.Compression.BrotliStream.BeginRead(byte[],int,int,System.AsyncCallback,object)" /> method on the current stream.</exception>
        /// <exception cref="System.InvalidOperationException">The end operation cannot be performed because the stream is closed.</exception>
        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        /// <summary>Asynchronously reads a sequence of bytes from the current Brotli stream, writes them to a byte array starting at a specified index, advances the position within the Brotli stream by the number of bytes read, and monitors cancellation requests.</summary>
        /// <param name="buffer">The buffer to write the data into.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> at which to begin writing data from the Brotli stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the total number of bytes read into the <paramref name="buffer" />. The result value can be less than the number of bytes requested if the number of bytes currently available is less than the requested number, or it can be 0 (zero) if the end of the Brotli stream has been reached.</returns>
        /// <remarks>The `ReadAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.BrotliStream.CanRead" /> property to determine whether the current instance supports reading.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        /// <summary>Asynchronously reads a sequence of bytes from the current Brotli stream, writes them to a byte memory range, advances the position within the Brotli stream by the number of bytes read, and monitors cancellation requests.</summary>
        /// <param name="buffer">The region of memory to write the data into.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the total number of bytes read into the buffer. The result value can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, or it can be 0 (zero) if the end of the Brotli stream has been reached.</returns>
        /// <remarks>The `ReadAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.BrotliStream.CanRead" /> property to determine whether the current instance supports reading.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_mode != CompressionMode.Decompress)
                throw new InvalidOperationException(SR.BrotliStream_Compress_UnsupportedOperation);
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }
            return FinishReadAsyncMemory(buffer, cancellationToken);
        }

        private async ValueTask<int> FinishReadAsyncMemory(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            AsyncOperationStarting();
            try
            {
                int totalWritten = 0;
                OperationStatus lastResult = OperationStatus.DestinationTooSmall;
                // We want to continue calling Decompress until we're either out of space for output or until Decompress indicates it is finished.
                while (buffer.Length > 0 && lastResult != OperationStatus.Done)
                {
                    if (lastResult == OperationStatus.NeedMoreData)
                    {
                        // Ensure any left over data is at the beginning of the array so we can fill the remainder.
                        if (_bufferCount > 0 && _bufferOffset != 0)
                        {
                            _buffer.AsSpan(_bufferOffset, _bufferCount).CopyTo(_buffer);
                        }
                        _bufferOffset = 0;

                        int numRead = 0;
                        while (_bufferCount < _buffer.Length &&
                              ((numRead = await _stream.ReadAsync(new Memory<byte>(_buffer, _bufferCount, _buffer.Length - _bufferCount), cancellationToken).ConfigureAwait(false)) > 0))
                        {
                            _bufferCount += numRead;
                            if (_bufferCount > _buffer.Length)
                            {
                                // The stream is either malicious or poorly implemented and returned a number of
                                // bytes larger than the buffer supplied to it.
                                throw new InvalidDataException(SR.BrotliStream_Decompress_InvalidStream);
                            }
                        }

                        if (_bufferCount <= 0)
                        {
                            break;
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    lastResult = _decoder.Decompress(new ReadOnlySpan<byte>(_buffer, _bufferOffset, _bufferCount), buffer.Span, out int bytesConsumed, out int bytesWritten);
                    if (lastResult == OperationStatus.InvalidData)
                    {
                        throw new InvalidOperationException(SR.BrotliStream_Decompress_InvalidData);
                    }

                    if (bytesConsumed > 0)
                    {
                        _bufferOffset += bytesConsumed;
                        _bufferCount -= bytesConsumed;
                    }

                    if (bytesWritten > 0)
                    {
                        totalWritten += bytesWritten;
                        buffer = buffer.Slice(bytesWritten);
                    }
                }

                return totalWritten;
            }
            finally
            {
                AsyncOperationCompleting();
            }
        }
    }
}
