// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum CryptoStreamMode {
        Read = 0,
        Write = 1,
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    public class CryptoStream : Stream, IDisposable {

        // Member veriables
        private Stream _stream;
        private ICryptoTransform _Transform;
        private byte[] _InputBuffer;  // read from _stream before _Transform
        private int _InputBufferIndex = 0;
        private int _InputBlockSize;
        private byte[] _OutputBuffer; // buffered output of _Transform
        private int _OutputBufferIndex = 0;
        private int _OutputBlockSize;
        private CryptoStreamMode _transformMode;
        private bool _canRead = false;
        private bool _canWrite = false;
        private bool _finalBlockTransformed = false;

        // Constructors

        public CryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode) {
            _stream = stream;
            _transformMode = mode;
            _Transform = transform;
            switch (_transformMode) {
            case CryptoStreamMode.Read:
                if (!(_stream.CanRead)) throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotReadable"),"stream");
                _canRead = true;
                break;
            case CryptoStreamMode.Write:
                if (!(_stream.CanWrite)) throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotWritable"),"stream");
                _canWrite = true;
                break;
            default:
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            }
            InitializeBuffer();
        }

        public override bool CanRead {
            [Pure]
            get { return _canRead; }
        }

        // For now, assume we can never seek into the middle of a cryptostream
        // and get the state right.  This is too strict.
        public override bool CanSeek {
            [Pure]
            get { return false; }
        }

        public override bool CanWrite {
            [Pure]
            get { return _canWrite; }
        }

        public override long Length {
            get { throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream")); }
        }

        public override long Position {
            get { throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream")); }
            set { throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream")); }
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
        public void FlushFinalBlock() {
            if (_finalBlockTransformed) 
                throw new NotSupportedException(Environment.GetResourceString("Cryptography_CryptoStream_FlushFinalBlockTwice"));
            // We have to process the last block here.  First, we have the final block in _InputBuffer, so transform it

            byte[] finalBytes = _Transform.TransformFinalBlock(_InputBuffer, 0, _InputBufferIndex);

            _finalBlockTransformed = true;
            // Now, write out anything sitting in the _OutputBuffer...
            if (_canWrite && _OutputBufferIndex > 0) {
                _stream.Write(_OutputBuffer, 0, _OutputBufferIndex);
                _OutputBufferIndex = 0;
            }
            // Write out finalBytes
            if (_canWrite)
                _stream.Write(finalBytes, 0, finalBytes.Length);

            // If the inner stream is a CryptoStream, then we want to call FlushFinalBlock on it too, otherwise just Flush.
            CryptoStream innerCryptoStream = _stream as CryptoStream;
            if (innerCryptoStream != null) {
                if (!innerCryptoStream.HasFlushedFinalBlock) {
                    innerCryptoStream.FlushFinalBlock();
                }
            } else {
                _stream.Flush();
            }
            // zeroize plain text material before returning
            if (_InputBuffer != null)
                Array.Clear(_InputBuffer, 0, _InputBuffer.Length);
            if (_OutputBuffer != null)
                Array.Clear(_OutputBuffer, 0, _OutputBuffer.Length);
            return;
        }

        public override void Flush() {
            return;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Flush() which a subclass might have overriden.  To be safe 
            // we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Flush) when we are not sure.
            if (this.GetType() != typeof(CryptoStream))
                return base.FlushAsync(cancellationToken);

            return cancellationToken.IsCancellationRequested ?
                Task.FromCancellation(cancellationToken) :
                Task.CompletedTask;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
        }

        public override void SetLength(long value) {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count) {
          // argument checking
            if (!CanRead) 
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
            if (offset < 0) 
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            // read <= count bytes from the input stream, transforming as we go.
            // Basic idea: first we deliver any bytes we already have in the
            // _OutputBuffer, because we know they're good.  Then, if asked to deliver 
            // more bytes, we read & transform a block at a time until either there are
            // no bytes ready or we've delivered enough.
            int bytesToDeliver = count;
            int currentOutputIndex = offset;
            if (_OutputBufferIndex != 0) {
                // we have some already-transformed bytes in the output buffer
                if (_OutputBufferIndex <= count) {
                    Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, offset, _OutputBufferIndex);
                    bytesToDeliver -= _OutputBufferIndex;
                    currentOutputIndex += _OutputBufferIndex;
                    _OutputBufferIndex = 0;
                } else {
                    Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, offset, count);
                    Buffer.InternalBlockCopy(_OutputBuffer, count, _OutputBuffer, 0, _OutputBufferIndex - count);
                    _OutputBufferIndex -= count;
                    return(count);
                }
            }
            // _finalBlockTransformed == true implies we're at the end of the input stream
            // if we got through the previous if block then _OutputBufferIndex = 0, meaning
            // we have no more transformed bytes to give
            // so return count-bytesToDeliver, the amount we were able to hand back
            // eventually, we'll just always return 0 here because there's no more to read
            if (_finalBlockTransformed) {
                return(count - bytesToDeliver);
            }
            // ok, now loop until we've delivered enough or there's nothing available
            int amountRead = 0;
            int numOutputBytes;

            // OK, see first if it's a multi-block transform and we can speed up things
            if (bytesToDeliver > _OutputBlockSize)
            {
                if (_Transform.CanTransformMultipleBlocks) {
                    int BlocksToProcess = bytesToDeliver / _OutputBlockSize;
                    int numWholeBlocksInBytes = BlocksToProcess * _InputBlockSize;
                    byte[] tempInputBuffer = new byte[numWholeBlocksInBytes];
                    // get first the block already read
                    Buffer.InternalBlockCopy(_InputBuffer, 0, tempInputBuffer, 0, _InputBufferIndex);
                    amountRead = _InputBufferIndex;
                    amountRead += _stream.Read(tempInputBuffer, _InputBufferIndex, numWholeBlocksInBytes - _InputBufferIndex);
                    _InputBufferIndex = 0;
                    if (amountRead <= _InputBlockSize) {
                        _InputBuffer = tempInputBuffer;
                        _InputBufferIndex = amountRead;
                        goto slow;
                    }
                    // Make amountRead an integral multiple of _InputBlockSize
                    int numWholeReadBlocksInBytes = (amountRead / _InputBlockSize) * _InputBlockSize;
                    int numIgnoredBytes = amountRead - numWholeReadBlocksInBytes;
                    if (numIgnoredBytes != 0) {
                        _InputBufferIndex = numIgnoredBytes;
                        Buffer.InternalBlockCopy(tempInputBuffer, numWholeReadBlocksInBytes, _InputBuffer, 0, numIgnoredBytes);
                    }
                    byte[] tempOutputBuffer = new byte[(numWholeReadBlocksInBytes / _InputBlockSize) * _OutputBlockSize];
                    numOutputBytes = _Transform.TransformBlock(tempInputBuffer, 0, numWholeReadBlocksInBytes, tempOutputBuffer, 0);
                    Buffer.InternalBlockCopy(tempOutputBuffer, 0, buffer, currentOutputIndex, numOutputBytes);
                    // Now, tempInputBuffer and tempOutputBuffer are no more needed, so zeroize them to protect plain text
                    Array.Clear(tempInputBuffer, 0, tempInputBuffer.Length);
                    Array.Clear(tempOutputBuffer, 0, tempOutputBuffer.Length);
                    bytesToDeliver -= numOutputBytes;
                    currentOutputIndex += numOutputBytes;
                }
            }

slow:
            // try to fill _InputBuffer so we have something to transform
            while (bytesToDeliver > 0) {
                while (_InputBufferIndex < _InputBlockSize) {
                    amountRead = _stream.Read(_InputBuffer, _InputBufferIndex, _InputBlockSize - _InputBufferIndex);
                    // first, check to see if we're at the end of the input stream
                    if (amountRead == 0) goto ProcessFinalBlock;
                    _InputBufferIndex += amountRead;
                }
                numOutputBytes = _Transform.TransformBlock(_InputBuffer, 0, _InputBlockSize, _OutputBuffer, 0);
                _InputBufferIndex = 0;
                if (bytesToDeliver >= numOutputBytes) {
                    Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, numOutputBytes);
                    currentOutputIndex += numOutputBytes;
                    bytesToDeliver -= numOutputBytes;
                } else {
                    Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, bytesToDeliver);
                    _OutputBufferIndex = numOutputBytes - bytesToDeliver;
                    Buffer.InternalBlockCopy(_OutputBuffer, bytesToDeliver, _OutputBuffer, 0, _OutputBufferIndex);
                    return count;
                }
            }
            return count;

        ProcessFinalBlock:
            // if so, then call TransformFinalBlock to get whatever is left
            byte[] finalBytes = _Transform.TransformFinalBlock(_InputBuffer, 0, _InputBufferIndex);
            // now, since _OutputBufferIndex must be 0 if we're in the while loop at this point,
            // reset it to be what we just got back
            _OutputBuffer = finalBytes;
            _OutputBufferIndex = finalBytes.Length;
            // set the fact that we've transformed the final block
            _finalBlockTransformed = true;
            // now, return either everything we just got or just what's asked for, whichever is smaller
            if (bytesToDeliver < _OutputBufferIndex) {
                Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, bytesToDeliver);
                _OutputBufferIndex -= bytesToDeliver;
                Buffer.InternalBlockCopy(_OutputBuffer, bytesToDeliver, _OutputBuffer, 0, _OutputBufferIndex);
                return(count);
            } else {
                Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, _OutputBufferIndex);
                bytesToDeliver -= _OutputBufferIndex;
                _OutputBufferIndex = 0;
                return(count - bytesToDeliver);
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // argument checking
            if (!CanRead)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() or BeginRead() which a subclass might have overriden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read/BeginRead) when we are not sure.
            if (this.GetType() != typeof(CryptoStream))
                return base.ReadAsync(buffer, offset, count, cancellationToken);

            // Fast path check for cancellation already requested
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation<int>(cancellationToken);

            return ReadAsyncInternal(buffer, offset, count, cancellationToken);
        }

        // simple awaitable that allows for hopping to the thread pool
        private struct HopToThreadPoolAwaitable : INotifyCompletion
        {
            public HopToThreadPoolAwaitable GetAwaiter() { return this; }
            public bool IsCompleted { get { return false; } }
            public void OnCompleted(Action continuation) { Task.Run(continuation); }
            public void GetResult() {}
        }

        private async Task<int> ReadAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Same conditions validated with exceptions in ReadAsync
            Contract.Requires(CanRead);
            Contract.Requires(offset >= 0);
            Contract.Requires(count >= 0);
            Contract.Requires(buffer.Length - offset >= count);

            await default(HopToThreadPoolAwaitable); // computationally-intensive operation follows, so force execution to run asynchronously
            var sem = base.EnsureAsyncActiveSemaphoreInitialized();
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                // The following logic is identical to that in Read, except calling async 
                // methods instead of synchronous on the underlying stream.

                // read <= count bytes from the input stream, transforming as we go.
                // Basic idea: first we deliver any bytes we already have in the
                // _OutputBuffer, because we know they're good.  Then, if asked to deliver 
                // more bytes, we read & transform a block at a time until either there are
                // no bytes ready or we've delivered enough.
                int bytesToDeliver = count;
                int currentOutputIndex = offset;
                if (_OutputBufferIndex != 0)
                {
                    // we have some already-transformed bytes in the output buffer
                    if (_OutputBufferIndex <= count)
                    {
                        Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, offset, _OutputBufferIndex);
                        bytesToDeliver -= _OutputBufferIndex;
                        currentOutputIndex += _OutputBufferIndex;
                        _OutputBufferIndex = 0;
                    }
                    else
                    {
                        Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, offset, count);
                        Buffer.InternalBlockCopy(_OutputBuffer, count, _OutputBuffer, 0, _OutputBufferIndex - count);
                        _OutputBufferIndex -= count;
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
                if (bytesToDeliver > _OutputBlockSize)
                {
                    if (_Transform.CanTransformMultipleBlocks)
                    {
                        int BlocksToProcess = bytesToDeliver / _OutputBlockSize;
                        int numWholeBlocksInBytes = BlocksToProcess * _InputBlockSize;
                        byte[] tempInputBuffer = new byte[numWholeBlocksInBytes];
                        // get first the block already read
                        Buffer.InternalBlockCopy(_InputBuffer, 0, tempInputBuffer, 0, _InputBufferIndex);
                        amountRead = _InputBufferIndex;
                        amountRead += await _stream.ReadAsync(tempInputBuffer, _InputBufferIndex, numWholeBlocksInBytes - _InputBufferIndex, cancellationToken).ConfigureAwait(false);
                        _InputBufferIndex = 0;
                        if (amountRead <= _InputBlockSize)
                        {
                            _InputBuffer = tempInputBuffer;
                            _InputBufferIndex = amountRead;
                            goto slow;
                        }
                        // Make amountRead an integral multiple of _InputBlockSize
                        int numWholeReadBlocksInBytes = (amountRead / _InputBlockSize) * _InputBlockSize;
                        int numIgnoredBytes = amountRead - numWholeReadBlocksInBytes;
                        if (numIgnoredBytes != 0)
                        {
                            _InputBufferIndex = numIgnoredBytes;
                            Buffer.InternalBlockCopy(tempInputBuffer, numWholeReadBlocksInBytes, _InputBuffer, 0, numIgnoredBytes);
                        }
                        byte[] tempOutputBuffer = new byte[(numWholeReadBlocksInBytes / _InputBlockSize) * _OutputBlockSize];
                        numOutputBytes = _Transform.TransformBlock(tempInputBuffer, 0, numWholeReadBlocksInBytes, tempOutputBuffer, 0);
                        Buffer.InternalBlockCopy(tempOutputBuffer, 0, buffer, currentOutputIndex, numOutputBytes);
                        // Now, tempInputBuffer and tempOutputBuffer are no more needed, so zeroize them to protect plain text
                        Array.Clear(tempInputBuffer, 0, tempInputBuffer.Length);
                        Array.Clear(tempOutputBuffer, 0, tempOutputBuffer.Length);
                        bytesToDeliver -= numOutputBytes;
                        currentOutputIndex += numOutputBytes;
                    }
                }

            slow:
                // try to fill _InputBuffer so we have something to transform
                while (bytesToDeliver > 0)
                {
                    while (_InputBufferIndex < _InputBlockSize)
                    {
                        amountRead = await _stream.ReadAsync(_InputBuffer, _InputBufferIndex, _InputBlockSize - _InputBufferIndex, cancellationToken).ConfigureAwait(false);
                        // first, check to see if we're at the end of the input stream
                        if (amountRead == 0) goto ProcessFinalBlock;
                        _InputBufferIndex += amountRead;
                    }
                    numOutputBytes = _Transform.TransformBlock(_InputBuffer, 0, _InputBlockSize, _OutputBuffer, 0);
                    _InputBufferIndex = 0;
                    if (bytesToDeliver >= numOutputBytes)
                    {
                        Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, numOutputBytes);
                        currentOutputIndex += numOutputBytes;
                        bytesToDeliver -= numOutputBytes;
                    }
                    else
                    {
                        Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, bytesToDeliver);
                        _OutputBufferIndex = numOutputBytes - bytesToDeliver;
                        Buffer.InternalBlockCopy(_OutputBuffer, bytesToDeliver, _OutputBuffer, 0, _OutputBufferIndex);
                        return count;
                    }
                }
                return count;

            ProcessFinalBlock:
                // if so, then call TransformFinalBlock to get whatever is left
                byte[] finalBytes = _Transform.TransformFinalBlock(_InputBuffer, 0, _InputBufferIndex);
                // now, since _OutputBufferIndex must be 0 if we're in the while loop at this point,
                // reset it to be what we just got back
                _OutputBuffer = finalBytes;
                _OutputBufferIndex = finalBytes.Length;
                // set the fact that we've transformed the final block
                _finalBlockTransformed = true;
                // now, return either everything we just got or just what's asked for, whichever is smaller
                if (bytesToDeliver < _OutputBufferIndex)
                {
                    Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, bytesToDeliver);
                    _OutputBufferIndex -= bytesToDeliver;
                    Buffer.InternalBlockCopy(_OutputBuffer, bytesToDeliver, _OutputBuffer, 0, _OutputBufferIndex);
                    return (count);
                }
                else
                {
                    Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, _OutputBufferIndex);
                    bytesToDeliver -= _OutputBufferIndex;
                    _OutputBufferIndex = 0;
                    return (count - bytesToDeliver);
                }
            }
            finally { sem.Release(); }
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if (!CanWrite) 
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
            if (offset < 0) 
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            // write <= count bytes to the output stream, transforming as we go.
            // Basic idea: using bytes in the _InputBuffer first, make whole blocks,
            // transform them, and write them out.  Cache any remaining bytes in the _InputBuffer.
            int bytesToWrite = count;
            int currentInputIndex = offset;
            // if we have some bytes in the _InputBuffer, we have to deal with those first,
            // so let's try to make an entire block out of it
            if (_InputBufferIndex > 0) {
                if (count >= _InputBlockSize - _InputBufferIndex) {
                    // we have enough to transform at least a block, so fill the input block
                    Buffer.InternalBlockCopy(buffer, offset, _InputBuffer, _InputBufferIndex, _InputBlockSize - _InputBufferIndex);
                    currentInputIndex += (_InputBlockSize - _InputBufferIndex);
                    bytesToWrite -= (_InputBlockSize - _InputBufferIndex);
                    _InputBufferIndex = _InputBlockSize;
                    // Transform the block and write it out
                } else {
                    // not enough to transform a block, so just copy the bytes into the _InputBuffer
                    // and return
                    Buffer.InternalBlockCopy(buffer, offset, _InputBuffer, _InputBufferIndex, count);
                    _InputBufferIndex += count;
                    return;
                }
            }
            // If the OutputBuffer has anything in it, write it out
            if (_OutputBufferIndex > 0) {
                _stream.Write(_OutputBuffer, 0, _OutputBufferIndex);
                _OutputBufferIndex = 0;
            }
            // At this point, either the _InputBuffer is full, empty, or we've already returned.
            // If full, let's process it -- we now know the _OutputBuffer is empty
            int numOutputBytes;
            if (_InputBufferIndex == _InputBlockSize) {
                numOutputBytes = _Transform.TransformBlock(_InputBuffer, 0, _InputBlockSize, _OutputBuffer, 0);
                // write out the bytes we just got 
                _stream.Write(_OutputBuffer, 0, numOutputBytes);
                // reset the _InputBuffer
                _InputBufferIndex = 0;
            }
            while (bytesToWrite > 0) {
                if (bytesToWrite >= _InputBlockSize) {
                    // We have at least an entire block's worth to transform
                    // If the transform will handle multiple blocks at once, do that
                    if (_Transform.CanTransformMultipleBlocks) {
                        int numWholeBlocks = bytesToWrite / _InputBlockSize;
                        int numWholeBlocksInBytes = numWholeBlocks * _InputBlockSize;
                        byte[] _tempOutputBuffer = new byte[numWholeBlocks * _OutputBlockSize];
                        numOutputBytes = _Transform.TransformBlock(buffer, currentInputIndex, numWholeBlocksInBytes, _tempOutputBuffer, 0);
                        _stream.Write(_tempOutputBuffer, 0, numOutputBytes);
                        currentInputIndex += numWholeBlocksInBytes;
                        bytesToWrite -= numWholeBlocksInBytes;
                    } else {
                        // do it the slow way
                        numOutputBytes = _Transform.TransformBlock(buffer, currentInputIndex, _InputBlockSize, _OutputBuffer, 0);
                        _stream.Write(_OutputBuffer, 0, numOutputBytes);
                        currentInputIndex += _InputBlockSize;
                        bytesToWrite -= _InputBlockSize;
                    }
                } else {
                    // In this case, we don't have an entire block's worth left, so store it up in the 
                    // input buffer, which by now must be empty.
                    Buffer.InternalBlockCopy(buffer, currentInputIndex, _InputBuffer, 0, bytesToWrite);
                    _InputBufferIndex += bytesToWrite;
                    return;
                }
            }
            return;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanWrite)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() or BeginWrite() which a subclass might have overriden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write/BeginWrite) when we are not sure.
            if (this.GetType() != typeof(CryptoStream))
                return base.WriteAsync(buffer, offset, count, cancellationToken);

            // Fast path check for cancellation already requested
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation(cancellationToken);

            return WriteAsyncInternal(buffer, offset, count, cancellationToken);
        }

        private async Task WriteAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Same conditions validated with exceptions in ReadAsync
            Contract.Requires(CanWrite);
            Contract.Requires(offset >= 0);
            Contract.Requires(count >= 0);
            Contract.Requires(buffer.Length - offset >= count);

            await default(HopToThreadPoolAwaitable); // computationally-intensive operation follows, so force execution to run asynchronously
            var sem = base.EnsureAsyncActiveSemaphoreInitialized();
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                // The following logic is identical to that in Write, except calling async 
                // methods instead of synchronous on the underlying stream.

                // write <= count bytes to the output stream, transforming as we go.
                // Basic idea: using bytes in the _InputBuffer first, make whole blocks,
                // transform them, and write them out.  Cache any remaining bytes in the _InputBuffer.
                int bytesToWrite = count;
                int currentInputIndex = offset;
                // if we have some bytes in the _InputBuffer, we have to deal with those first,
                // so let's try to make an entire block out of it
                if (_InputBufferIndex > 0)
                {
                    if (count >= _InputBlockSize - _InputBufferIndex)
                    {
                        // we have enough to transform at least a block, so fill the input block
                        Buffer.InternalBlockCopy(buffer, offset, _InputBuffer, _InputBufferIndex, _InputBlockSize - _InputBufferIndex);
                        currentInputIndex += (_InputBlockSize - _InputBufferIndex);
                        bytesToWrite -= (_InputBlockSize - _InputBufferIndex);
                        _InputBufferIndex = _InputBlockSize;
                        // Transform the block and write it out
                    }
                    else
                    {
                        // not enough to transform a block, so just copy the bytes into the _InputBuffer
                        // and return
                        Buffer.InternalBlockCopy(buffer, offset, _InputBuffer, _InputBufferIndex, count);
                        _InputBufferIndex += count;
                        return;
                    }
                }
                // If the OutputBuffer has anything in it, write it out
                if (_OutputBufferIndex > 0)
                {
                    await _stream.WriteAsync(_OutputBuffer, 0, _OutputBufferIndex, cancellationToken).ConfigureAwait(false);
                    _OutputBufferIndex = 0;
                }
                // At this point, either the _InputBuffer is full, empty, or we've already returned.
                // If full, let's process it -- we now know the _OutputBuffer is empty
                int numOutputBytes;
                if (_InputBufferIndex == _InputBlockSize)
                {
                    numOutputBytes = _Transform.TransformBlock(_InputBuffer, 0, _InputBlockSize, _OutputBuffer, 0);
                    // write out the bytes we just got 
                    await _stream.WriteAsync(_OutputBuffer, 0, numOutputBytes, cancellationToken).ConfigureAwait(false);
                    // reset the _InputBuffer
                    _InputBufferIndex = 0;
                }
                while (bytesToWrite > 0)
                {
                    if (bytesToWrite >= _InputBlockSize)
                    {
                        // We have at least an entire block's worth to transform
                        // If the transform will handle multiple blocks at once, do that
                        if (_Transform.CanTransformMultipleBlocks)
                        {
                            int numWholeBlocks = bytesToWrite / _InputBlockSize;
                            int numWholeBlocksInBytes = numWholeBlocks * _InputBlockSize;
                            byte[] _tempOutputBuffer = new byte[numWholeBlocks * _OutputBlockSize];
                            numOutputBytes = _Transform.TransformBlock(buffer, currentInputIndex, numWholeBlocksInBytes, _tempOutputBuffer, 0);
                            await _stream.WriteAsync(_tempOutputBuffer, 0, numOutputBytes, cancellationToken).ConfigureAwait(false);
                            currentInputIndex += numWholeBlocksInBytes;
                            bytesToWrite -= numWholeBlocksInBytes;
                        }
                        else
                        {
                            // do it the slow way
                            numOutputBytes = _Transform.TransformBlock(buffer, currentInputIndex, _InputBlockSize, _OutputBuffer, 0);
                            await _stream.WriteAsync(_OutputBuffer, 0, numOutputBytes, cancellationToken).ConfigureAwait(false);
                            currentInputIndex += _InputBlockSize;
                            bytesToWrite -= _InputBlockSize;
                        }
                    }
                    else
                    {
                        // In this case, we don't have an entire block's worth left, so store it up in the 
                        // input buffer, which by now must be empty.
                        Buffer.InternalBlockCopy(buffer, currentInputIndex, _InputBuffer, 0, bytesToWrite);
                        _InputBufferIndex += bytesToWrite;
                        return;
                    }
                }
                return;
            }
            finally { sem.Release(); }
        }

        public void Clear() {
            Close();
        }

        protected override void Dispose(bool disposing) {
            try {
                if (disposing) {
                    if (!_finalBlockTransformed) {
                        FlushFinalBlock();
                    }
                    _stream.Close();
                }                
            }
            finally {
                try {
                    // Ensure we don't try to transform the final block again if we get disposed twice
                    // since it's null after this
                    _finalBlockTransformed = true;
                     // we need to clear all the internal buffers
                     if (_InputBuffer != null)
                         Array.Clear(_InputBuffer, 0, _InputBuffer.Length);
                     if (_OutputBuffer != null)
                         Array.Clear(_OutputBuffer, 0, _OutputBuffer.Length);

                     _InputBuffer = null;
                     _OutputBuffer = null;
                     _canRead = false;
                     _canWrite = false;
                }
                finally {
                     base.Dispose(disposing);
                }
            }
        }

        // Private methods 

        private void InitializeBuffer() {
            if (_Transform != null) {
                _InputBlockSize = _Transform.InputBlockSize;
                _InputBuffer = new byte[_InputBlockSize];
                _OutputBlockSize = _Transform.OutputBlockSize;
                _OutputBuffer = new byte[_OutputBlockSize];
            }
        }
    }
}
