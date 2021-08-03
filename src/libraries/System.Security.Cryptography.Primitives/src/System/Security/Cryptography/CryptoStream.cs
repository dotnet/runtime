// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
    public class CryptoStream : Stream, IDisposable
    {
        // Member variables
        private readonly Stream _stream;
        private readonly ICryptoTransform _transform;
        private byte[] _inputBuffer;  // read from _stream before _Transform
        private int _inputBufferIndex;
        private int _inputBlockSize;
        private byte[] _outputBuffer; // buffered output of _Transform
        private int _outputBufferIndex;
        private int _outputBlockSize;
        private bool _canRead;
        private bool _canWrite;
        private bool _finalBlockTransformed;
        private SemaphoreSlim? _lazyAsyncActiveSemaphore;
        private readonly bool _leaveOpen;

        // Constructors

        public CryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode)
            : this(stream, transform, mode, false)
        {
        }

        public CryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode, bool leaveOpen)
        {
            if (transform is null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            _stream = stream;
            _transform = transform;
            _leaveOpen = leaveOpen;

            switch (mode)
            {
                case CryptoStreamMode.Read:
                    if (!_stream.CanRead)
                    {
                        throw new ArgumentException(SR.Format(SR.Argument_StreamNotReadable, nameof(stream)));
                    }
                    _canRead = true;
                    break;

                case CryptoStreamMode.Write:
                    if (!_stream.CanWrite)
                    {
                        throw new ArgumentException(SR.Format(SR.Argument_StreamNotWritable, nameof(stream)));
                    }
                    _canWrite = true;
                    break;

                default:
                    throw new ArgumentException(SR.Argument_InvalidValue, nameof(mode));
            }

            _inputBlockSize = _transform.InputBlockSize;
            _inputBuffer = new byte[_inputBlockSize];
            _outputBlockSize = _transform.OutputBlockSize;
            _outputBuffer = new byte[_outputBlockSize];
        }

        public override bool CanRead
        {
            get { return _canRead; }
        }

        // For now, assume we can never seek into the middle of a cryptostream
        // and get the state right.  This is too strict.
        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return _canWrite; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(SR.NotSupported_UnseekableStream); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(SR.NotSupported_UnseekableStream); }
            set { throw new NotSupportedException(SR.NotSupported_UnseekableStream); }
        }

        public bool HasFlushedFinalBlock
        {
            get { return _finalBlockTransformed; }
        }

        // The flush final block functionality used to be part of close, but that meant you couldn't do something like this:
        // MemoryStream ms = new MemoryStream();
        // CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
        // cs.Write(foo, 0, foo.Length);
        // cs.Close();
        // and get the encrypted data out of ms, because the cs.Close also closed ms and the data went away.
        // so now do this:
        // cs.Write(foo, 0, foo.Length);
        // cs.FlushFinalBlock() // which can only be called once
        // byte[] ciphertext = ms.ToArray();
        // cs.Close();
        public void FlushFinalBlock() =>
            FlushFinalBlockAsync(useAsync: false, default).AsTask().GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronously updates the underlying data source or repository with the
        /// current state of the buffer, then clears the buffer.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        public ValueTask FlushFinalBlockAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            return FlushFinalBlockAsync(useAsync: true, cancellationToken);
        }

        private async ValueTask FlushFinalBlockAsync(bool useAsync, CancellationToken cancellationToken)
        {
            if (_finalBlockTransformed)
                throw new NotSupportedException(SR.Cryptography_CryptoStream_FlushFinalBlockTwice);
            _finalBlockTransformed = true;

            // Transform and write out the final bytes.
            if (_canWrite)
            {
                Debug.Assert(_outputBufferIndex == 0, "The output index can only ever be non-zero when in read mode.");

                byte[] finalBytes = _transform.TransformFinalBlock(_inputBuffer!, 0, _inputBufferIndex);
                if (useAsync)
                {
                    await _stream.WriteAsync(new ReadOnlyMemory<byte>(finalBytes), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _stream.Write(finalBytes, 0, finalBytes.Length);
                }
            }

            // If the inner stream is a CryptoStream, then we want to call FlushFinalBlock on it too, otherwise just Flush.
            if (_stream is CryptoStream innerCryptoStream)
            {
                if (!innerCryptoStream.HasFlushedFinalBlock)
                {
                    await innerCryptoStream.FlushFinalBlockAsync(useAsync, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                if (useAsync)
                {
                    await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _stream.Flush();
                }
            }

            // zeroize plain text material before returning
            if (_inputBuffer != null)
                Array.Clear(_inputBuffer);
            if (_outputBuffer != null)
                Array.Clear(_outputBuffer);
        }

        public override void Flush()
        {
            if (_canWrite)
            {
                _stream.Flush();
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Flush() which a subclass might have overridden.  To be safe
            // we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Flush) when we are not sure.
            if (GetType() != typeof(CryptoStream))
                return base.FlushAsync(cancellationToken);

            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled(cancellationToken) :
                !_canWrite ? Task.CompletedTask :
                _stream.FlushAsync(cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.NotSupported_UnseekableStream);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.NotSupported_UnseekableStream);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckReadArguments(buffer, offset, count);
            return ReadAsyncInternal(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!CanRead)
                return ValueTask.FromException<int>(new NotSupportedException(SR.NotSupported_UnreadableStream));

            return ReadAsyncInternal(buffer, cancellationToken);
        }

        private async ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            // To avoid a race with a stream's position pointer & generating race
            // conditions with internal buffer indexes in our own streams that
            // don't natively support async IO operations when there are multiple
            // async requests outstanding, we will block the application's main
            // thread if it does a second IO request until the first one completes.

            await AsyncActiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await ReadAsyncCore(buffer, cancellationToken, useAsync: true).ConfigureAwait(false);
            }
            finally
            {
                _lazyAsyncActiveSemaphore.Release();
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override int ReadByte()
        {
            // If we have enough bytes in the buffer such that reading 1 will still leave bytes
            // in the buffer, then take the faster path of simply returning the first byte.
            // (This unfortunately still involves shifting down the bytes in the buffer, as it
            // does in Read.  If/when that's fixed for Read, it should be fixed here, too.)
            if (_outputBufferIndex > 1)
            {
                Debug.Assert(_outputBuffer != null);
                byte b = _outputBuffer[0];
                Buffer.BlockCopy(_outputBuffer, 1, _outputBuffer, 0, _outputBufferIndex - 1);
                _outputBufferIndex -= 1;
                return b;
            }

            // Otherwise, fall back to the more robust but expensive path of using the base
            // Stream.ReadByte to call Read.
            return base.ReadByte();
        }

        public override void WriteByte(byte value)
        {
            // If there's room in the input buffer such that even with this byte we wouldn't
            // complete a block, simply add the byte to the input buffer.
            if (_inputBufferIndex + 1 < _inputBlockSize)
            {
                _inputBuffer![_inputBufferIndex++] = value;
                return;
            }

            // Otherwise, the logic is complicated, so we simply fall back to the base
            // implementation that'll use Write.
            base.WriteByte(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckReadArguments(buffer, offset, count);
            ValueTask<int> completedValueTask = ReadAsyncCore(buffer.AsMemory(offset, count), default(CancellationToken), useAsync: false);
            Debug.Assert(completedValueTask.IsCompleted);

            return completedValueTask.GetAwaiter().GetResult();
        }

        private void CheckReadArguments(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanRead)
                throw new NotSupportedException(SR.NotSupported_UnreadableStream);
        }

        private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken, bool useAsync)
        {
            while (true)
            {
                // If there are currently any bytes stored in the output buffer, hand back as many as we can.
                if (_outputBufferIndex != 0)
                {
                    int bytesToCopy = Math.Min(_outputBufferIndex, buffer.Length);
                    if (bytesToCopy != 0)
                    {
                        // Copy as many bytes as we can, then shift down the remaining bytes.
                        new ReadOnlySpan<byte>(_outputBuffer, 0, bytesToCopy).CopyTo(buffer.Span);
                        _outputBufferIndex -= bytesToCopy;
                        _outputBuffer.AsSpan(bytesToCopy).CopyTo(_outputBuffer);
                        CryptographicOperations.ZeroMemory(_outputBuffer.AsSpan(_outputBufferIndex, bytesToCopy));
                    }
                    return bytesToCopy;
                }

                // If we've already hit the end of the stream, there's nothing more to do.
                Debug.Assert(_outputBufferIndex == 0);
                if (_finalBlockTransformed)
                {
                    Debug.Assert(_inputBufferIndex == 0);
                    return 0;
                }

                int bytesRead = 0;
                bool eof = false;

                // If the transform supports transforming multiple blocks, try to read as large a chunk as would yield
                // data to fill the output buffer and do the appropriate transform directly into the output buffer.
                int blocksToProcess = buffer.Length / _outputBlockSize;
                if (blocksToProcess > 1 && _transform.CanTransformMultipleBlocks)
                {
                    // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
                    int numWholeBlocksInBytes = checked(blocksToProcess * _inputBlockSize);
                    byte[] tempInputBuffer = ArrayPool<byte>.Shared.Rent(numWholeBlocksInBytes);
                    try
                    {
                        // Read into our temporary input buffer, leaving enough room at the beginning for any existing data
                        // we have in _inputBuffer.
                        bytesRead = useAsync ?
                            await _stream.ReadAsync(new Memory<byte>(tempInputBuffer, _inputBufferIndex, numWholeBlocksInBytes - _inputBufferIndex), cancellationToken).ConfigureAwait(false) :
                            _stream.Read(tempInputBuffer, _inputBufferIndex, numWholeBlocksInBytes - _inputBufferIndex);
                        eof = bytesRead == 0;

                        // If we got enough data to form at least one block, transform as much as we can.
                        int totalInput = _inputBufferIndex + bytesRead;
                        if (totalInput >= _inputBlockSize)
                        {
                            // Copy any held data into tempInputBuffer now that we know we're proceeding to handle
                            // decrypting all the received data.
                            Buffer.BlockCopy(_inputBuffer, 0, tempInputBuffer, 0, _inputBufferIndex);
                            CryptographicOperations.ZeroMemory(new Span<byte>(_inputBuffer, 0, _inputBufferIndex));
                            bytesRead += _inputBufferIndex;

                            // Determine how many entire blocks worth of data we read.
                            int numWholeReadBlocks = bytesRead / _inputBlockSize;
                            int numWholeReadBlocksInBytes = numWholeReadBlocks * _inputBlockSize;

                            // If there's anything left over, copy that back into _inputBuffer for a later read.
                            _inputBufferIndex = bytesRead - numWholeReadBlocksInBytes;
                            if (_inputBufferIndex != 0)
                            {
                                Buffer.BlockCopy(tempInputBuffer, numWholeReadBlocksInBytes, _inputBuffer, 0, _inputBufferIndex);
                            }

                            // Transform the read data into the caller's buffer.
                            int numOutputBytes;
                            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> bufferArray))
                            {
                                // Because TransformBlock is based on arrays, we can only write directly into the output
                                // buffer if it's backed by an array; otherwise, we need to rent from the pool.
                                numOutputBytes = _transform.TransformBlock(tempInputBuffer, 0, numWholeReadBlocksInBytes, bufferArray.Array!, bufferArray.Offset);
                            }
                            else
                            {
                                // Otherwise, we need to rent a temporary from the pool.
                                byte[] tempOutputBuffer = ArrayPool<byte>.Shared.Rent(numWholeReadBlocks * _outputBlockSize);
                                numOutputBytes = numWholeReadBlocks * _outputBlockSize;
                                try
                                {
                                    numOutputBytes = _transform.TransformBlock(tempInputBuffer, 0, numWholeReadBlocksInBytes, tempOutputBuffer, 0);
                                    tempOutputBuffer.AsSpan(0, numOutputBytes).CopyTo(buffer.Span);
                                }
                                finally
                                {
                                    CryptographicOperations.ZeroMemory(new Span<byte>(tempOutputBuffer, 0, numOutputBytes));
                                    ArrayPool<byte>.Shared.Return(tempOutputBuffer);
                                }
                            }

                            // Return anything we've got at this point.
                            if (numOutputBytes != 0)
                            {
                                return numOutputBytes;
                            }
                        }
                        else
                        {
                            // We have less than a block's worth of data.  Copy the new data back into the _inputBuffer
                            // and fall back to using the single block code path.
                            Buffer.BlockCopy(tempInputBuffer, _inputBufferIndex, _inputBuffer, _inputBufferIndex, bytesRead);
                            _inputBufferIndex = totalInput;
                        }
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(new Span<byte>(tempInputBuffer, 0, numWholeBlocksInBytes));
                        ArrayPool<byte>.Shared.Return(tempInputBuffer);
                    }
                }

                // Read enough to fill one input block, as anything less won't be able to be transformed to produce output.
                if (!eof)
                {
                    while (_inputBufferIndex < _inputBlockSize)
                    {
                        bytesRead = useAsync ?
                            await _stream.ReadAsync(new Memory<byte>(_inputBuffer, _inputBufferIndex, _inputBlockSize - _inputBufferIndex), cancellationToken).ConfigureAwait(false) :
                            _stream.Read(_inputBuffer, _inputBufferIndex, _inputBlockSize - _inputBufferIndex);
                        if (bytesRead <= 0)
                        {
                            break;
                        }

                        _inputBufferIndex += bytesRead;
                    }
                }

                // Transform the received data.
                if (bytesRead <= 0)
                {
                    _outputBuffer = _transform.TransformFinalBlock(_inputBuffer, 0, _inputBufferIndex);
                    _outputBufferIndex = _outputBuffer.Length;
                    _finalBlockTransformed = true;
                }
                else
                {
                    _outputBufferIndex = _transform.TransformBlock(_inputBuffer, 0, _inputBufferIndex, _outputBuffer, 0);
                }

                // All input data has been processed.
                _inputBufferIndex = 0;
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckWriteArguments(buffer, offset, count);
            return WriteAsyncInternal(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!CanWrite)
                return ValueTask.FromException(new NotSupportedException(SR.NotSupported_UnwritableStream));

            return WriteAsyncInternal(buffer, cancellationToken);
        }

        private async ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            // To avoid a race with a stream's position pointer & generating race
            // conditions with internal buffer indexes in our own streams that
            // don't natively support async IO operations when there are multiple
            // async requests outstanding, we will block the application's main
            // thread if it does a second IO request until the first one completes.

            await AsyncActiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await WriteAsyncCore(buffer, cancellationToken, useAsync: true).ConfigureAwait(false);
            }
            finally
            {
                _lazyAsyncActiveSemaphore.Release();
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckWriteArguments(buffer, offset, count);
            WriteAsyncCore(buffer.AsMemory(offset, count), default, useAsync: false).AsTask().GetAwaiter().GetResult();
        }

        private void CheckWriteArguments(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanWrite)
                throw new NotSupportedException(SR.NotSupported_UnwritableStream);
        }

        private async ValueTask WriteAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, bool useAsync)
        {
            // write <= count bytes to the output stream, transforming as we go.
            // Basic idea: using bytes in the _InputBuffer first, make whole blocks,
            // transform them, and write them out.  Cache any remaining bytes in the _InputBuffer.
            int bytesToWrite = buffer.Length;
            int currentInputIndex = 0;
            // if we have some bytes in the _InputBuffer, we have to deal with those first,
            // so let's try to make an entire block out of it
            if (_inputBufferIndex > 0)
            {
                Debug.Assert(_inputBuffer != null);
                if (buffer.Length >= _inputBlockSize - _inputBufferIndex)
                {
                    // we have enough to transform at least a block, so fill the input block
                    buffer.Slice(0, _inputBlockSize - _inputBufferIndex).CopyTo(_inputBuffer.AsMemory(_inputBufferIndex));
                    currentInputIndex += (_inputBlockSize - _inputBufferIndex);
                    bytesToWrite -= (_inputBlockSize - _inputBufferIndex);
                    _inputBufferIndex = _inputBlockSize;
                    // Transform the block and write it out
                }
                else
                {
                    // not enough to transform a block, so just copy the bytes into the _InputBuffer
                    // and return
                    buffer.CopyTo(_inputBuffer.AsMemory(_inputBufferIndex));
                    _inputBufferIndex += buffer.Length;
                    return;
                }
            }

            Debug.Assert(_outputBufferIndex == 0, "The output index can only ever be non-zero when in read mode.");
            // At this point, either the _InputBuffer is full, empty, or we've already returned.
            // If full, let's process it -- we now know the _OutputBuffer is empty
            int numOutputBytes;
            if (_inputBufferIndex == _inputBlockSize)
            {
                Debug.Assert(_inputBuffer != null && _outputBuffer != null);
                numOutputBytes = _transform.TransformBlock(_inputBuffer, 0, _inputBlockSize, _outputBuffer, 0);
                // write out the bytes we just got
                if (useAsync)
                    await _stream.WriteAsync(new ReadOnlyMemory<byte>(_outputBuffer, 0, numOutputBytes), cancellationToken).ConfigureAwait(false);
                else
                    _stream.Write(_outputBuffer, 0, numOutputBytes);

                // reset the _InputBuffer
                _inputBufferIndex = 0;
            }
            while (bytesToWrite > 0)
            {
                if (bytesToWrite >= _inputBlockSize)
                {
                    // We have at least an entire block's worth to transform
                    int numWholeBlocks = bytesToWrite / _inputBlockSize;

                    // If the transform will handle multiple blocks at once, do that
                    if (_transform.CanTransformMultipleBlocks && numWholeBlocks > 1)
                    {
                        int numWholeBlocksInBytes = numWholeBlocks * _inputBlockSize;

                        // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
                        byte[]? tempOutputBuffer = ArrayPool<byte>.Shared.Rent(checked(numWholeBlocks * _outputBlockSize));
                        numOutputBytes = 0;

                        try
                        {
                            numOutputBytes = TransformBlock(_transform, buffer.Slice(currentInputIndex, numWholeBlocksInBytes), tempOutputBuffer, 0);

                            if (useAsync)
                            {
                                await _stream.WriteAsync(new ReadOnlyMemory<byte>(tempOutputBuffer, 0, numOutputBytes), cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                _stream.Write(tempOutputBuffer, 0, numOutputBytes);
                            }

                            currentInputIndex += numWholeBlocksInBytes;
                            bytesToWrite -= numWholeBlocksInBytes;
                            CryptographicOperations.ZeroMemory(new Span<byte>(tempOutputBuffer, 0, numOutputBytes));
                            ArrayPool<byte>.Shared.Return(tempOutputBuffer);
                            tempOutputBuffer = null;
                        }
                        catch
                        {
                            CryptographicOperations.ZeroMemory(new Span<byte>(tempOutputBuffer, 0, numOutputBytes));
                            tempOutputBuffer = null;
                            throw;
                        }
                    }
                    else
                    {
                        Debug.Assert(_outputBuffer != null);
                        // do it the slow way
                        numOutputBytes = TransformBlock(_transform, buffer.Slice(currentInputIndex, _inputBlockSize), _outputBuffer, 0);

                        if (useAsync)
                            await _stream.WriteAsync(new ReadOnlyMemory<byte>(_outputBuffer, 0, numOutputBytes), cancellationToken).ConfigureAwait(false);
                        else
                            _stream.Write(_outputBuffer, 0, numOutputBytes);

                        currentInputIndex += _inputBlockSize;
                        bytesToWrite -= _inputBlockSize;
                    }
                }
                else
                {
                    Debug.Assert(_inputBuffer != null);
                    // In this case, we don't have an entire block's worth left, so store it up in the
                    // input buffer, which by now must be empty.
                    buffer.Slice(currentInputIndex, bytesToWrite).CopyTo(_inputBuffer);
                    _inputBufferIndex += bytesToWrite;
                    return;
                }
            }
            return;

            unsafe static int TransformBlock(ICryptoTransform transform, ReadOnlyMemory<byte> inputBuffer, byte[] outputBuffer, int outputOffset)
            {
                if (MemoryMarshal.TryGetArray(inputBuffer, out ArraySegment<byte> segment))
                {
                    // Skip the copy if readonlymemory is actually an array.
                    Debug.Assert(segment.Array is not null);
                    return transform.TransformBlock(segment.Array, segment.Offset, inputBuffer.Length, outputBuffer, outputOffset);
                }
                else
                {
                    // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
                    byte[]? rentedBuffer = ArrayPool<byte>.Shared.Rent(inputBuffer.Length);
                    int result = default;

                    // Pin the rented buffer for security.
                    fixed (byte* _ = &rentedBuffer[0])
                    {
                        try
                        {
                            inputBuffer.CopyTo(rentedBuffer);
                            result = transform.TransformBlock(rentedBuffer, 0, inputBuffer.Length, outputBuffer, outputOffset);
                        }
                        finally
                        {
                            CryptographicOperations.ZeroMemory(rentedBuffer.AsSpan(0, inputBuffer.Length));
                        }
                    }

                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                    rentedBuffer = null;
                    return result;
                }
            }
        }

        /// <inheritdoc/>
        public unsafe override void CopyTo(Stream destination, int bufferSize)
        {
            CheckCopyToArguments(destination, bufferSize);

            // Use ArrayPool<byte>.Shared instead of CryptoPool because the array is passed out.
            byte[]? rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            // Pin the array for security.
            fixed (byte* _ = &rentedBuffer[0])
            {
                try
                {
                    int bytesRead;
                    do
                    {
                        bytesRead = Read(rentedBuffer, 0, bufferSize);
                        destination.Write(rentedBuffer, 0, bytesRead);
                    } while (bytesRead > 0);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(rentedBuffer.AsSpan(0, bufferSize));
                }
            }
            ArrayPool<byte>.Shared.Return(rentedBuffer);
            rentedBuffer = null;
        }

        /// <inheritdoc/>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            CheckCopyToArguments(destination, bufferSize);
            return CopyToAsyncInternal(destination, bufferSize, cancellationToken);
        }

        private async Task CopyToAsyncInternal(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // Use ArrayPool<byte>.Shared instead of CryptoPool because the array is passed out.
            byte[]? rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            // Pin the array for security.
            GCHandle pinHandle = GCHandle.Alloc(rentedBuffer, GCHandleType.Pinned);
            try
            {
                int bytesRead;
                do
                {
                    bytesRead = await ReadAsync(rentedBuffer.AsMemory(0, bufferSize), cancellationToken).ConfigureAwait(false);
                    await destination.WriteAsync(rentedBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                } while (bytesRead > 0);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rentedBuffer.AsSpan(0, bufferSize));
                pinHandle.Free();
            }
            ArrayPool<byte>.Shared.Return(rentedBuffer);
            rentedBuffer = null;
        }

        private void CheckCopyToArguments(Stream destination, int bufferSize)
        {
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            EnsureNotDisposed(destination, nameof(destination));

            if (!destination.CanWrite)
                throw new NotSupportedException(SR.NotSupported_UnwritableStream);
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);
            if (!CanRead)
                throw new NotSupportedException(SR.NotSupported_UnreadableStream);
        }

        private static void EnsureNotDisposed(Stream stream, string objectName)
        {
            if (!stream.CanRead && !stream.CanWrite)
                throw new ObjectDisposedException(objectName);
        }

        public void Clear()
        {
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (!_finalBlockTransformed)
                    {
                        FlushFinalBlock();
                    }
                    if (!_leaveOpen)
                    {
                        _stream.Dispose();
                    }
                }
            }
            finally
            {
                try
                {
                    // Ensure we don't try to transform the final block again if we get disposed twice
                    // since it's null after this
                    _finalBlockTransformed = true;
                    // we need to clear all the internal buffers
                    if (_inputBuffer != null)
                        Array.Clear(_inputBuffer);
                    if (_outputBuffer != null)
                        Array.Clear(_outputBuffer);

                    _inputBuffer = null!;
                    _outputBuffer = null!;
                    _canRead = false;
                    _canWrite = false;
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        public override ValueTask DisposeAsync()
        {
            return GetType() != typeof(CryptoStream) ?
                base.DisposeAsync() :
                DisposeAsyncCore();
        }

        private async ValueTask DisposeAsyncCore()
        {
            // Same logic as in Dispose, but with async counterparts
            try
            {
                if (!_finalBlockTransformed)
                {
                    await FlushFinalBlockAsync(useAsync: true, default).ConfigureAwait(false);
                }

                if (!_leaveOpen)
                {
                    await _stream.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                // Ensure we don't try to transform the final block again if we get disposed twice
                // since it's null after this
                _finalBlockTransformed = true;

                // we need to clear all the internal buffers
                if (_inputBuffer != null)
                {
                    Array.Clear(_inputBuffer);
                }

                if (_outputBuffer != null)
                {
                    Array.Clear(_outputBuffer);
                }

                _inputBuffer = null!;
                _outputBuffer = null!;
                _canRead = false;
                _canWrite = false;
            }
        }

        [MemberNotNull(nameof(_lazyAsyncActiveSemaphore))]
        private SemaphoreSlim AsyncActiveSemaphore
        {
            get
            {
                // Lazily-initialize _lazyAsyncActiveSemaphore.  As we're never accessing the SemaphoreSlim's
                // WaitHandle, we don't need to worry about Disposing it.
                return LazyInitializer.EnsureInitialized(ref _lazyAsyncActiveSemaphore, () => new SemaphoreSlim(1, 1));
            }
        }
    }
}
