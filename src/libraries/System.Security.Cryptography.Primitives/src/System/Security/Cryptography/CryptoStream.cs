// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
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
        private SemaphoreSlim _lazyAsyncActiveSemaphore;
        private readonly bool _leaveOpen;

        // Constructors

        public CryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode)
            : this(stream, transform, mode, false)
        {
        }

        public CryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode, bool leaveOpen)
        {

            _stream = stream;
            _transform = transform;
            _leaveOpen = leaveOpen;
            switch (mode)
            {
                case CryptoStreamMode.Read:
                    if (!(_stream.CanRead)) throw new ArgumentException(SR.Format(SR.Argument_StreamNotReadable, nameof(stream)));
                    _canRead = true;
                    break;
                case CryptoStreamMode.Write:
                    if (!(_stream.CanWrite)) throw new ArgumentException(SR.Format(SR.Argument_StreamNotWritable, nameof(stream)));
                    _canWrite = true;
                    break;
                default:
                    throw new ArgumentException(SR.Argument_InvalidValue);
            }
            InitializeBuffer();
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
            FlushFinalBlockAsync(useAsync: false).GetAwaiter().GetResult();

        private async Task FlushFinalBlockAsync(bool useAsync)
        {
            if (_finalBlockTransformed)
                throw new NotSupportedException(SR.Cryptography_CryptoStream_FlushFinalBlockTwice);
            _finalBlockTransformed = true;

            // Transform and write out the final bytes.
            if (_canWrite)
            {
                Debug.Assert(_outputBufferIndex == 0, "The output index can only ever be non-zero when in read mode.");

                byte[] finalBytes = _transform.TransformFinalBlock(_inputBuffer, 0, _inputBufferIndex);
                if (useAsync)
                {
                    await _stream.WriteAsync(new ReadOnlyMemory<byte>(finalBytes)).ConfigureAwait(false);
                }
                else
                {
                    _stream.Write(finalBytes, 0, finalBytes.Length);
                }
            }

            // If the inner stream is a CryptoStream, then we want to call FlushFinalBlock on it too, otherwise just Flush.
            CryptoStream innerCryptoStream = _stream as CryptoStream;
            if (innerCryptoStream != null)
            {
                if (!innerCryptoStream.HasFlushedFinalBlock)
                {
                    await innerCryptoStream.FlushFinalBlockAsync(useAsync).ConfigureAwait(false);
                }
            }
            else
            {
                if (useAsync)
                {
                    await _stream.FlushAsync().ConfigureAwait(false);
                }
                else
                {
                    _stream.Flush();
                }
            }

            // zeroize plain text material before returning
            if (_inputBuffer != null)
                Array.Clear(_inputBuffer, 0, _inputBuffer.Length);
            if (_outputBuffer != null)
                Array.Clear(_outputBuffer, 0, _outputBuffer.Length);
        }

        public override void Flush()
        {
            return;
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
                Task.CompletedTask;
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
            return ReadAsyncInternal(buffer, offset, count, cancellationToken);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        private async Task<int> ReadAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // To avoid a race with a stream's position pointer & generating race 
            // conditions with internal buffer indexes in our own streams that 
            // don't natively support async IO operations when there are multiple 
            // async requests outstanding, we will block the application's main
            // thread if it does a second IO request until the first one completes.

            SemaphoreSlim semaphore = AsyncActiveSemaphore;
            await semaphore.WaitAsync().ForceAsync();
            try
            {
                return await ReadAsyncCore(buffer, offset, count, cancellationToken, useAsync: true); // ConfigureAwait not needed as ForceAsync was used
            }
            finally
            {
                semaphore.Release();
            }
        }

        public override int ReadByte()
        {
            // If we have enough bytes in the buffer such that reading 1 will still leave bytes
            // in the buffer, then take the faster path of simply returning the first byte.
            // (This unfortunately still involves shifting down the bytes in the buffer, as it
            // does in Read.  If/when that's fixed for Read, it should be fixed here, too.)
            if (_outputBufferIndex > 1)
            {
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
                _inputBuffer[_inputBufferIndex++] = value;
                return;
            }

            // Otherwise, the logic is complicated, so we simply fall back to the base 
            // implementation that'll use Write.
            base.WriteByte(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckReadArguments(buffer, offset, count);
            return ReadAsyncCore(buffer, offset, count, default(CancellationToken), useAsync: false).GetAwaiter().GetResult();
        }

        private void CheckReadArguments(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException(SR.NotSupported_UnreadableStream);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
        }

        private async Task<int> ReadAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken, bool useAsync)
        {
            // read <= count bytes from the input stream, transforming as we go.
            // Basic idea: first we deliver any bytes we already have in the
            // _OutputBuffer, because we know they're good.  Then, if asked to deliver 
            // more bytes, we read & transform a block at a time until either there are
            // no bytes ready or we've delivered enough.
            int bytesToDeliver = count;
            int currentOutputIndex = offset;
            if (_outputBufferIndex != 0)
            {
                // we have some already-transformed bytes in the output buffer
                if (_outputBufferIndex <= count)
                {
                    Buffer.BlockCopy(_outputBuffer, 0, buffer, offset, _outputBufferIndex);
                    bytesToDeliver -= _outputBufferIndex;
                    currentOutputIndex += _outputBufferIndex;
                    int toClear = _outputBuffer.Length - _outputBufferIndex;
                    CryptographicOperations.ZeroMemory(new Span<byte>(_outputBuffer, _outputBufferIndex, toClear));
                    _outputBufferIndex = 0;
                }
                else
                {
                    Buffer.BlockCopy(_outputBuffer, 0, buffer, offset, count);
                    Buffer.BlockCopy(_outputBuffer, count, _outputBuffer, 0, _outputBufferIndex - count);
                    _outputBufferIndex -= count;

                    int toClear = _outputBuffer.Length - _outputBufferIndex;
                    CryptographicOperations.ZeroMemory(new Span<byte>(_outputBuffer, _outputBufferIndex, toClear));

                    return (count);
                }
            }
            // _finalBlockTransformed == true implies we're at the end of the input stream
            // if we got through the previous if block then _OutputBufferIndex = 0, meaning
            // we have no more transformed bytes to give
            // so return count-bytesToDeliver, the amount we were able to hand back
            // eventually, we'll just always return 0 here because there's no more to read
            if (_finalBlockTransformed)
            {
                return (count - bytesToDeliver);
            }
            // ok, now loop until we've delivered enough or there's nothing available
            int amountRead = 0;
            int numOutputBytes;

            // OK, see first if it's a multi-block transform and we can speed up things
            int blocksToProcess = bytesToDeliver / _outputBlockSize;

            if (blocksToProcess > 1 && _transform.CanTransformMultipleBlocks)
            {
                int numWholeBlocksInBytes = blocksToProcess * _inputBlockSize;
                byte[] tempInputBuffer = ArrayPool<byte>.Shared.Rent(numWholeBlocksInBytes);
                byte[] tempOutputBuffer = null;

                try
                {
                    amountRead = useAsync ?
                        await _stream.ReadAsync(new Memory<byte>(tempInputBuffer, _inputBufferIndex, numWholeBlocksInBytes - _inputBufferIndex), cancellationToken) : // ConfigureAwait not needed, as useAsync is only true if we're already on a TP thread
                        _stream.Read(tempInputBuffer, _inputBufferIndex, numWholeBlocksInBytes - _inputBufferIndex);

                    int totalInput = _inputBufferIndex + amountRead;

                    // If there's still less than a block, copy the new data into the hold buffer and move to the slow read.
                    if (totalInput < _inputBlockSize)
                    {
                        Buffer.BlockCopy(tempInputBuffer, _inputBufferIndex, _inputBuffer, _inputBufferIndex, amountRead);
                        _inputBufferIndex = totalInput;
                    }
                    else
                    {
                        // Copy any held data into tempInputBuffer now that we know we're proceeding
                        Buffer.BlockCopy(_inputBuffer, 0, tempInputBuffer, 0, _inputBufferIndex);
                        CryptographicOperations.ZeroMemory(new Span<byte>(_inputBuffer, 0, _inputBufferIndex));
                        amountRead += _inputBufferIndex;
                        _inputBufferIndex = 0;
                        
                        // Make amountRead an integral multiple of _InputBlockSize
                        int numWholeReadBlocks = amountRead / _inputBlockSize;
                        int numWholeReadBlocksInBytes = numWholeReadBlocks * _inputBlockSize;
                        int numIgnoredBytes = amountRead - numWholeReadBlocksInBytes;

                        if (numIgnoredBytes != 0)
                        {
                            _inputBufferIndex = numIgnoredBytes;
                            Buffer.BlockCopy(tempInputBuffer, numWholeReadBlocksInBytes, _inputBuffer, 0, numIgnoredBytes);
                        }

                        tempOutputBuffer = ArrayPool<byte>.Shared.Rent(numWholeReadBlocks * _outputBlockSize);
                        numOutputBytes = _transform.TransformBlock(tempInputBuffer, 0, numWholeReadBlocksInBytes, tempOutputBuffer, 0);
                        Buffer.BlockCopy(tempOutputBuffer, 0, buffer, currentOutputIndex, numOutputBytes);

                        // Clear what was written while we know how much that was
                        CryptographicOperations.ZeroMemory(new Span<byte>(tempOutputBuffer, 0, numOutputBytes));
                        ArrayPool<byte>.Shared.Return(tempOutputBuffer);
                        tempOutputBuffer = null;

                        bytesToDeliver -= numOutputBytes;
                        currentOutputIndex += numOutputBytes;
                    }
                }
                finally
                {
                    // If we rented and then an exception happened we don't know how much was written to,
                    // clear the whole thing and return it.
                    if (tempOutputBuffer != null)
                    {
                        CryptographicOperations.ZeroMemory(tempOutputBuffer);
                        ArrayPool<byte>.Shared.Return(tempOutputBuffer);
                        tempOutputBuffer = null;
                    }

                    CryptographicOperations.ZeroMemory(new Span<byte>(tempInputBuffer, 0, numWholeBlocksInBytes));
                    ArrayPool<byte>.Shared.Return(tempInputBuffer);
                    tempInputBuffer = null;
                }
            }

            // try to fill _InputBuffer so we have something to transform
            while (bytesToDeliver > 0)
            {
                while (_inputBufferIndex < _inputBlockSize)
                {
                    amountRead = useAsync ?
                        await _stream.ReadAsync(new Memory<byte>(_inputBuffer, _inputBufferIndex, _inputBlockSize - _inputBufferIndex), cancellationToken) : // ConfigureAwait not needed, as useAsync is only true if we're already on a TP thread
                        _stream.Read(_inputBuffer, _inputBufferIndex, _inputBlockSize - _inputBufferIndex);

                    // first, check to see if we're at the end of the input stream
                    if (amountRead == 0) goto ProcessFinalBlock;
                    _inputBufferIndex += amountRead;
                }

                numOutputBytes = _transform.TransformBlock(_inputBuffer, 0, _inputBlockSize, _outputBuffer, 0);
                _inputBufferIndex = 0;

                if (bytesToDeliver >= numOutputBytes)
                {
                    Buffer.BlockCopy(_outputBuffer, 0, buffer, currentOutputIndex, numOutputBytes);
                    CryptographicOperations.ZeroMemory(new Span<byte>(_outputBuffer, 0, numOutputBytes));
                    currentOutputIndex += numOutputBytes;
                    bytesToDeliver -= numOutputBytes;
                }
                else
                {
                    Buffer.BlockCopy(_outputBuffer, 0, buffer, currentOutputIndex, bytesToDeliver);
                    _outputBufferIndex = numOutputBytes - bytesToDeliver;
                    Buffer.BlockCopy(_outputBuffer, bytesToDeliver, _outputBuffer, 0, _outputBufferIndex);
                    int toClear = _outputBuffer.Length - _outputBufferIndex;
                    CryptographicOperations.ZeroMemory(new Span<byte>(_outputBuffer, _outputBufferIndex, toClear));
                    return count;
                }
            }
            return count;

        ProcessFinalBlock:
            // if so, then call TransformFinalBlock to get whatever is left
            byte[] finalBytes = _transform.TransformFinalBlock(_inputBuffer, 0, _inputBufferIndex);
            // now, since _OutputBufferIndex must be 0 if we're in the while loop at this point,
            // reset it to be what we just got back
            _outputBuffer = finalBytes;
            _outputBufferIndex = finalBytes.Length;
            // set the fact that we've transformed the final block
            _finalBlockTransformed = true;
            // now, return either everything we just got or just what's asked for, whichever is smaller
            if (bytesToDeliver < _outputBufferIndex)
            {
                Buffer.BlockCopy(_outputBuffer, 0, buffer, currentOutputIndex, bytesToDeliver);
                _outputBufferIndex -= bytesToDeliver;
                Buffer.BlockCopy(_outputBuffer, bytesToDeliver, _outputBuffer, 0, _outputBufferIndex);
                int toClear = _outputBuffer.Length - _outputBufferIndex;
                CryptographicOperations.ZeroMemory(new Span<byte>(_outputBuffer, _outputBufferIndex, toClear));
                return (count);
            }
            else
            {
                Buffer.BlockCopy(_outputBuffer, 0, buffer, currentOutputIndex, _outputBufferIndex);
                bytesToDeliver -= _outputBufferIndex;
                _outputBufferIndex = 0;
                CryptographicOperations.ZeroMemory(_outputBuffer);
                return (count - bytesToDeliver);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckWriteArguments(buffer, offset, count);
            return WriteAsyncInternal(buffer, offset, count, cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        private async Task WriteAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // To avoid a race with a stream's position pointer & generating race 
            // conditions with internal buffer indexes in our own streams that 
            // don't natively support async IO operations when there are multiple 
            // async requests outstanding, we will block the application's main
            // thread if it does a second IO request until the first one completes.

            SemaphoreSlim semaphore = AsyncActiveSemaphore;
            await semaphore.WaitAsync().ForceAsync();
            try
            {
                await WriteAsyncCore(buffer, offset, count, cancellationToken, useAsync: true); // ConfigureAwait not needed due to earlier ForceAsync
            }
            finally
            {
                semaphore.Release();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckWriteArguments(buffer, offset, count);
            WriteAsyncCore(buffer, offset, count, default(CancellationToken), useAsync: false).GetAwaiter().GetResult();
        }

        private void CheckWriteArguments(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.NotSupported_UnwritableStream);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
        }

        private async Task WriteAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken, bool useAsync)
        {
            // write <= count bytes to the output stream, transforming as we go.
            // Basic idea: using bytes in the _InputBuffer first, make whole blocks,
            // transform them, and write them out.  Cache any remaining bytes in the _InputBuffer.
            int bytesToWrite = count;
            int currentInputIndex = offset;
            // if we have some bytes in the _InputBuffer, we have to deal with those first,
            // so let's try to make an entire block out of it
            if (_inputBufferIndex > 0)
            {
                if (count >= _inputBlockSize - _inputBufferIndex)
                {
                    // we have enough to transform at least a block, so fill the input block
                    Buffer.BlockCopy(buffer, offset, _inputBuffer, _inputBufferIndex, _inputBlockSize - _inputBufferIndex);
                    currentInputIndex += (_inputBlockSize - _inputBufferIndex);
                    bytesToWrite -= (_inputBlockSize - _inputBufferIndex);
                    _inputBufferIndex = _inputBlockSize;
                    // Transform the block and write it out
                }
                else
                {
                    // not enough to transform a block, so just copy the bytes into the _InputBuffer
                    // and return
                    Buffer.BlockCopy(buffer, offset, _inputBuffer, _inputBufferIndex, count);
                    _inputBufferIndex += count;
                    return;
                }
            }

            Debug.Assert(_outputBufferIndex == 0, "The output index can only ever be non-zero when in read mode.");

            // At this point, either the _InputBuffer is full, empty, or we've already returned.
            // If full, let's process it -- we now know the _OutputBuffer is empty
            int numOutputBytes;
            if (_inputBufferIndex == _inputBlockSize)
            {
                numOutputBytes = _transform.TransformBlock(_inputBuffer, 0, _inputBlockSize, _outputBuffer, 0);
                // write out the bytes we just got
                if (useAsync)
                    await _stream.WriteAsync(new ReadOnlyMemory<byte>(_outputBuffer, 0, numOutputBytes), cancellationToken); // ConfigureAwait not needed, as useAsync is only true if we're already on a TP thread
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
                        byte[] tempOutputBuffer = ArrayPool<byte>.Shared.Rent(numWholeBlocks * _outputBlockSize);
                        numOutputBytes = 0;

                        try
                        {
                            numOutputBytes =
                                _transform.TransformBlock(buffer, currentInputIndex, numWholeBlocksInBytes, tempOutputBuffer, 0);

                            if (useAsync)
                            {
                                await _stream.WriteAsync(new ReadOnlyMemory<byte>(tempOutputBuffer, 0, numOutputBytes), cancellationToken); // ConfigureAwait not needed, as useAsync is only true if we're already on a TP thread
                            }
                            else
                            {
                                _stream.Write(tempOutputBuffer, 0, numOutputBytes);
                            }

                            currentInputIndex += numWholeBlocksInBytes;
                            bytesToWrite -= numWholeBlocksInBytes;
                        }
                        finally
                        {
                            CryptographicOperations.ZeroMemory(new Span<byte>(tempOutputBuffer, 0, numOutputBytes));
                            ArrayPool<byte>.Shared.Return(tempOutputBuffer);
                            tempOutputBuffer = null;
                        }
                    }
                    else
                    {
                        // do it the slow way
                        numOutputBytes = _transform.TransformBlock(buffer, currentInputIndex, _inputBlockSize, _outputBuffer, 0);

                        if (useAsync)
                            await _stream.WriteAsync(new ReadOnlyMemory<byte>(_outputBuffer, 0, numOutputBytes), cancellationToken); // ConfigureAwait not needed, as useAsync is only true if we're already on a TP thread
                        else
                            _stream.Write(_outputBuffer, 0, numOutputBytes);

                        currentInputIndex += _inputBlockSize;
                        bytesToWrite -= _inputBlockSize;
                    }
                }
                else
                {
                    // In this case, we don't have an entire block's worth left, so store it up in the 
                    // input buffer, which by now must be empty.
                    Buffer.BlockCopy(buffer, currentInputIndex, _inputBuffer, 0, bytesToWrite);
                    _inputBufferIndex += bytesToWrite;
                    return;
                }
            }
            return;
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
                        Array.Clear(_inputBuffer, 0, _inputBuffer.Length);
                    if (_outputBuffer != null)
                        Array.Clear(_outputBuffer, 0, _outputBuffer.Length);

                    _inputBuffer = null;
                    _outputBuffer = null;
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
                    await FlushFinalBlockAsync(useAsync: true).ConfigureAwait(false);
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
                    Array.Clear(_inputBuffer, 0, _inputBuffer.Length);
                }

                if (_outputBuffer != null)
                {
                    Array.Clear(_outputBuffer, 0, _outputBuffer.Length);
                }

                _inputBuffer = null;
                _outputBuffer = null;
                _canRead = false;
                _canWrite = false;
            }
        }

        // Private methods 

        private void InitializeBuffer()
        {
            if (_transform != null)
            {
                _inputBlockSize = _transform.InputBlockSize;
                _inputBuffer = new byte[_inputBlockSize];
                _outputBlockSize = _transform.OutputBlockSize;
                _outputBuffer = new byte[_outputBlockSize];
            }
        }

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
