// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;

namespace System.Net.Security
{
    //
    // This is a wrapping stream that does data encryption/decryption based on a successfully authenticated SSPI context.
    //
    internal class SslStreamInternal
    {
        private static readonly AsyncCallback s_writeCallback = new AsyncCallback(WriteCallback);
        private static readonly AsyncProtocolCallback s_resumeAsyncWriteCallback = new AsyncProtocolCallback(ResumeAsyncWriteCallback);
        private static readonly AsyncProtocolCallback s_resumeAsyncReadCallback = new AsyncProtocolCallback(ResumeAsyncReadCallback);
        private static readonly AsyncProtocolCallback s_readHeaderCallback = new AsyncProtocolCallback(ReadHeaderCallback);
        private static readonly AsyncProtocolCallback s_readFrameCallback = new AsyncProtocolCallback(ReadFrameCallback);

        private const int PinnableReadBufferSize = 4096 * 4 + 32;         // We read in 16K chunks + headers.
        private static PinnableBufferCache s_PinnableReadBufferCache = new PinnableBufferCache("System.Net.SslStream", PinnableReadBufferSize);
        private const int PinnableWriteBufferSize = 4096 + 1024;        // We write in 4K chunks + encryption overhead.
        private static PinnableBufferCache s_PinnableWriteBufferCache = new PinnableBufferCache("System.Net.SslStream", PinnableWriteBufferSize);

        private SslState _sslState;
        private int _nestedWrite;
        private int _nestedRead;

        // Never updated directly, special properties are used.  This is the read buffer.
        private byte[] _internalBuffer;
        private bool _internalBufferFromPinnableCache;

        private byte[] _pinnableOutputBuffer;                        // Used for writes when we can do it. 
        private byte[] _pinnableOutputBufferInUse;                   // Remembers what UNENCRYPTED buffer is using _PinnableOutputBuffer.

        private int _internalOffset;
        private int _internalBufferCount;

        private FixedSizeReader _reader;

        internal SslStreamInternal(SslState sslState)
        {
            if (PinnableBufferCacheEventSource.Log.IsEnabled())
            {
                PinnableBufferCacheEventSource.Log.DebugMessage1("CTOR: In System.Net._SslStream.SslStream", this.GetHashCode());
            }

            _sslState = sslState;
            _reader = new FixedSizeReader(_sslState.InnerStream);
        }

        // If we have a read buffer from the pinnable cache, return it.
        private void FreeReadBuffer()
        {
            if (_internalBufferFromPinnableCache)
            {
                s_PinnableReadBufferCache.FreeBuffer(_internalBuffer);
                _internalBufferFromPinnableCache = false;
            }

            _internalBuffer = null;
        }

        ~SslStreamInternal()
        {
            if (_internalBufferFromPinnableCache)
            {
                if (PinnableBufferCacheEventSource.Log.IsEnabled())
                {
                    PinnableBufferCacheEventSource.Log.DebugMessage2("DTOR: In System.Net._SslStream.~SslStream Freeing Read Buffer", this.GetHashCode(), PinnableBufferCacheEventSource.AddressOfByteArray(_internalBuffer));
                }

                FreeReadBuffer();
            }
            if (_pinnableOutputBuffer != null)
            {
                if (PinnableBufferCacheEventSource.Log.IsEnabled())
                {
                    PinnableBufferCacheEventSource.Log.DebugMessage2("DTOR: In System.Net._SslStream.~SslStream Freeing Write Buffer", this.GetHashCode(), PinnableBufferCacheEventSource.AddressOfByteArray(_pinnableOutputBuffer));
                }

                s_PinnableWriteBufferCache.FreeBuffer(_pinnableOutputBuffer);
            }
        }

        internal int ReadByte()
        {
            if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "ReadByte", "read"));
            }

            // If there's any data in the buffer, take one byte, and we're done.
            try
            {
                if (InternalBufferCount > 0)
                {
                    int b = InternalBuffer[InternalOffset];
                    SkipBytes(1);
                    return b;
                }
            }
            finally
            {
                // Regardless of whether we were able to read a byte from the buffer,
                // reset the read tracking.  If we weren't able to read a byte, the
                // subsequent call to Read will set the flag again.
                _nestedRead = 0;
            }

            // Otherwise, fall back to reading a byte via Read, the same way Stream.ReadByte does.
            // This allocation is unfortunate but should be relatively rare, as it'll only occur once
            // per buffer fill internally by Read.
            byte[] oneByte = new byte[1];
            int bytesRead = Read(oneByte, 0, 1);
            Debug.Assert(bytesRead == 0 || bytesRead == 1);
            return bytesRead == 1 ? oneByte[0] : -1;
        }

        internal int Read(byte[] buffer, int offset, int count)
        {
            return ProcessRead(buffer, offset, count, null);
        }

        internal void Write(byte[] buffer, int offset, int count)
        {
            ProcessWrite(buffer, offset, count, null);
        }

        internal IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
        {
            BufferAsyncResult bufferResult = new BufferAsyncResult(this, buffer, offset, count, asyncState, asyncCallback);
            AsyncProtocolRequest asyncRequest = new AsyncProtocolRequest(bufferResult);
            ProcessRead(buffer, offset, count, asyncRequest);
            return bufferResult;
        }

        internal int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            BufferAsyncResult bufferResult = asyncResult as BufferAsyncResult;
            if (bufferResult == null)
            {
                throw new ArgumentException(SR.Format(SR.net_io_async_result, asyncResult.GetType().FullName), nameof(asyncResult));
            }

            if (Interlocked.Exchange(ref _nestedRead, 0) == 0)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndRead"));
            }

            // No "artificial" timeouts implemented so far, InnerStream controls timeout.
            bufferResult.InternalWaitForCompletion();

            if (bufferResult.Result is Exception)
            {
                if (bufferResult.Result is IOException)
                {
                    throw (Exception)bufferResult.Result;
                }

                throw new IOException(SR.net_io_read, (Exception)bufferResult.Result);
            }

            return (int)bufferResult.Result;
        }

        internal IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
        {
            LazyAsyncResult lazyResult = new LazyAsyncResult(this, asyncState, asyncCallback);
            AsyncProtocolRequest asyncRequest = new AsyncProtocolRequest(lazyResult);
            ProcessWrite(buffer, offset, count, asyncRequest);
            return lazyResult;
        }

        internal void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            LazyAsyncResult lazyResult = asyncResult as LazyAsyncResult;
            if (lazyResult == null)
            {
                throw new ArgumentException(SR.Format(SR.net_io_async_result, asyncResult.GetType().FullName), nameof(asyncResult));
            }

            if (Interlocked.Exchange(ref _nestedWrite, 0) == 0)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndWrite"));
            }

            // No "artificial" timeouts implemented so far, InnerStream controls timeout.
            lazyResult.InternalWaitForCompletion();

            if (lazyResult.Result is Exception)
            {
                if (lazyResult.Result is IOException)
                {
                    throw (Exception)lazyResult.Result;
                }

                throw new IOException(SR.net_io_write, (Exception)lazyResult.Result);
            }
        }

        internal bool DataAvailable
        {
            get { return InternalBufferCount != 0; }
        }

        private byte[] InternalBuffer
        {
            get
            {
                return _internalBuffer;
            }
        }

        private int InternalOffset
        {
            get
            {
                return _internalOffset;
            }
        }

        private int InternalBufferCount
        {
            get
            {
                return _internalBufferCount;
            }
        }

        private void SkipBytes(int decrCount)
        {
            _internalOffset += decrCount;
            _internalBufferCount -= decrCount;
        }

        //
        // This will set the internal offset to "curOffset" and ensure internal buffer.
        // If not enough, reallocate and copy up to "curOffset".
        //
        private void EnsureInternalBufferSize(int curOffset, int addSize)
        {
            if (_internalBuffer == null || _internalBuffer.Length < addSize + curOffset)
            {
                bool wasPinnable = _internalBufferFromPinnableCache;
                byte[] saved = _internalBuffer;

                int newSize = addSize + curOffset;
                if (newSize <= PinnableReadBufferSize)
                {
                    if (PinnableBufferCacheEventSource.Log.IsEnabled())
                    {
                        PinnableBufferCacheEventSource.Log.DebugMessage2("In System.Net._SslStream.EnsureInternalBufferSize IS pinnable", this.GetHashCode(), newSize);
                    }

                    _internalBufferFromPinnableCache = true;
                    _internalBuffer = s_PinnableReadBufferCache.AllocateBuffer();
                }
                else
                {
                    if (PinnableBufferCacheEventSource.Log.IsEnabled())
                    {
                        PinnableBufferCacheEventSource.Log.DebugMessage2("In System.Net._SslStream.EnsureInternalBufferSize NOT pinnable", this.GetHashCode(), newSize);
                    }

                    _internalBufferFromPinnableCache = false;
                    _internalBuffer = new byte[newSize];
                }

                if (saved != null && curOffset != 0)
                {
                    Buffer.BlockCopy(saved, 0, _internalBuffer, 0, curOffset);
                }

                if (wasPinnable)
                {
                    s_PinnableReadBufferCache.FreeBuffer(saved);
                }
            }
            _internalOffset = curOffset;
            _internalBufferCount = curOffset + addSize;
        }

        //
        // Validates user parameters for all Read/Write methods.
        //
        private void ValidateParameters(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.net_offset_plus_count);
            }
        }

        //
        // Sync write method.
        //
        private void ProcessWrite(byte[] buffer, int offset, int count, AsyncProtocolRequest asyncRequest)
        {
            _sslState.CheckThrow(authSuccessCheck:true, shutdownCheck:true);
            ValidateParameters(buffer, offset, count);

            if (Interlocked.Exchange(ref _nestedWrite, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "Write", "write"));
            }

            bool failed = false;

            try
            {
                StartWriting(buffer, offset, count, asyncRequest);
            }
            catch (Exception e)
            {
                _sslState.FinishWrite();

                failed = true;
                if (e is IOException)
                {
                    throw;
                }

                throw new IOException(SR.net_io_write, e);
            }
            finally
            {
                if (asyncRequest == null || failed)
                {
                    _nestedWrite = 0;
                }
            }
        }

        private void StartWriting(byte[] buffer, int offset, int count, AsyncProtocolRequest asyncRequest)
        {
            if (asyncRequest != null)
            {
                asyncRequest.SetNextRequest(buffer, offset, count, s_resumeAsyncWriteCallback);
            }

            // We loop to this method from the callback.
            // If the last chunk was just completed from async callback (count < 0), we complete user request.
            if (count >= 0 )
            {
                byte[] outBuffer = null;
                if (_pinnableOutputBufferInUse == null)
                {
                    if (_pinnableOutputBuffer == null)
                    {
                        _pinnableOutputBuffer = s_PinnableWriteBufferCache.AllocateBuffer();
                    }

                    _pinnableOutputBufferInUse = buffer;
                    outBuffer = _pinnableOutputBuffer;
                    if (PinnableBufferCacheEventSource.Log.IsEnabled())
                    {
                        PinnableBufferCacheEventSource.Log.DebugMessage3("In System.Net._SslStream.StartWriting Trying Pinnable", this.GetHashCode(), count, PinnableBufferCacheEventSource.AddressOfByteArray(outBuffer));
                    }
                }
                else
                {
                    if (PinnableBufferCacheEventSource.Log.IsEnabled())
                    {
                        PinnableBufferCacheEventSource.Log.DebugMessage2("In System.Net._SslStream.StartWriting BufferInUse", this.GetHashCode(), count);
                    }
                }

                do
                {
                    if (count == 0 && !SslStreamPal.CanEncryptEmptyMessage)
                    {
                        // If it's an empty message and the PAL doesn't support that,
                        // we're done.
                        return;
                    }

                    // Request a write IO slot.
                    if (_sslState.CheckEnqueueWrite(asyncRequest))
                    {
                        // Operation is async and has been queued, return.
                        return;
                    }

                    int chunkBytes = Math.Min(count, _sslState.MaxDataSize);
                    int encryptedBytes;
                    SecurityStatusPal status = _sslState.EncryptData(buffer, offset, chunkBytes, ref outBuffer, out encryptedBytes);
                    if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        // Re-handshake status is not supported.
                        ProtocolToken message = new ProtocolToken(null, status);
                        throw new IOException(SR.net_io_encrypt, message.GetException());
                    }

                    if (PinnableBufferCacheEventSource.Log.IsEnabled())
                    {
                        PinnableBufferCacheEventSource.Log.DebugMessage3("In System.Net._SslStream.StartWriting Got Encrypted Buffer",
                            this.GetHashCode(), encryptedBytes, PinnableBufferCacheEventSource.AddressOfByteArray(outBuffer));
                    }

                    if (asyncRequest != null)
                    {
                        // Prepare for the next request.
                        asyncRequest.SetNextRequest(buffer, offset + chunkBytes, count - chunkBytes, s_resumeAsyncWriteCallback);
                        IAsyncResult ar = _sslState.InnerStream.BeginWrite(outBuffer, 0, encryptedBytes, s_writeCallback, asyncRequest);
                        if (!ar.CompletedSynchronously)
                        {
                            return;
                        }

                        _sslState.InnerStream.EndWrite(ar);

                    }
                    else
                    {
                        _sslState.InnerStream.Write(outBuffer, 0, encryptedBytes);
                    }

                    offset += chunkBytes;
                    count -= chunkBytes;

                    // Release write IO slot.
                    _sslState.FinishWrite();

                } while (count != 0);
            }

            if (asyncRequest != null) 
            {
                asyncRequest.CompleteUser();
            }

            if (buffer == _pinnableOutputBufferInUse)
            {
                _pinnableOutputBufferInUse = null;
                if (PinnableBufferCacheEventSource.Log.IsEnabled())
                {
                    PinnableBufferCacheEventSource.Log.DebugMessage1("In System.Net._SslStream.StartWriting Freeing buffer.", this.GetHashCode());
                }
            }
        }

        //
        // Combined sync/async read method. For sync request asyncRequest==null.
        //
        private int ProcessRead(byte[] buffer, int offset, int count, AsyncProtocolRequest asyncRequest)
        {
            ValidateParameters(buffer, offset, count);

            if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, (asyncRequest!=null? "BeginRead":"Read"), "read"));
            }

            bool failed = false;

            try
            {
                int copyBytes;
                if (InternalBufferCount != 0)
                {
                    copyBytes = InternalBufferCount > count ? count : InternalBufferCount;
                    if (copyBytes != 0)
                    {
                        Buffer.BlockCopy(InternalBuffer, InternalOffset, buffer, offset, copyBytes);
                        SkipBytes(copyBytes);
                    }
                    
                    if (asyncRequest != null) {
                        asyncRequest.CompleteUser((object) copyBytes);
                    }
                    
                    return copyBytes;
                }

                return StartReading(buffer, offset, count, asyncRequest);
            }
            catch (Exception e)
            {
                _sslState.FinishRead(null);
                failed = true;

                if (e is IOException)
                {
                    throw;
                }

                throw new IOException(SR.net_io_read, e);
            }
            finally
            {
                if (asyncRequest == null || failed)
                {
                    _nestedRead = 0;
                }
            }
        }

        //
        // To avoid recursion when decrypted 0 bytes this method will loop until a decrypted result at least 1 byte.
        //
        private int StartReading(byte[] buffer, int offset, int count, AsyncProtocolRequest asyncRequest)
        {
            int result = 0;

            if (InternalBufferCount != 0)
            {
                NetEventSource.Fail(this, $"Previous frame was not consumed. InternalBufferCount: {InternalBufferCount}");
            }

            do
            {
                if (asyncRequest != null)
                {
                    asyncRequest.SetNextRequest(buffer, offset, count, s_resumeAsyncReadCallback);
                }

                int copyBytes = _sslState.CheckEnqueueRead(buffer, offset, count, asyncRequest);
                if (copyBytes == 0)
                {
                    // Queued but not completed!
                    return 0;
                }

                if (copyBytes != -1)
                {
                    if (asyncRequest != null)
                    {
                        asyncRequest.CompleteUser((object)copyBytes);
                    }

                    return copyBytes;
                }
            }

            // When we read -1 bytes means we have decrypted 0 bytes or rehandshaking, need looping.
            while ((result = StartFrameHeader(buffer, offset, count, asyncRequest)) == -1);

            return result;
        }

        private int StartFrameHeader(byte[] buffer, int offset, int count, AsyncProtocolRequest asyncRequest)
        {
            int readBytes = 0;

            //
            // Always pass InternalBuffer for SSPI "in place" decryption.
            // A user buffer can be shared by many threads in that case decryption/integrity check may fail cause of data corruption.
            //

            // Reset internal buffer for a new frame.
            EnsureInternalBufferSize(0, SecureChannel.ReadHeaderSize);

            if (asyncRequest != null)
            {
                asyncRequest.SetNextRequest(InternalBuffer, 0, SecureChannel.ReadHeaderSize, s_readHeaderCallback);
                _reader.AsyncReadPacket(asyncRequest);

                if (!asyncRequest.MustCompleteSynchronously)
                {
                    return 0;
                }

                readBytes = asyncRequest.Result;
            }
            else
            {
                readBytes = _reader.ReadPacket(InternalBuffer, 0, SecureChannel.ReadHeaderSize);
            }

            return StartFrameBody(readBytes, buffer, offset, count, asyncRequest);
        }

        private int StartFrameBody(int readBytes, byte[] buffer, int offset, int count, AsyncProtocolRequest asyncRequest)
        {
            if (readBytes == 0)
            {
                //EOF : Reset the buffer as we did not read anything into it.
                SkipBytes(InternalBufferCount);
                if (asyncRequest != null)
                {
                    asyncRequest.CompleteUser((object)0);
                }
                
                return 0;
            }

            // Now readBytes is a payload size.
            readBytes = _sslState.GetRemainingFrameSize(InternalBuffer, readBytes);

            if (readBytes < 0)
            {
                throw new IOException(SR.net_frame_read_size);
            }

            EnsureInternalBufferSize(SecureChannel.ReadHeaderSize, readBytes);

            if (asyncRequest != null)
            {
                asyncRequest.SetNextRequest(InternalBuffer, SecureChannel.ReadHeaderSize, readBytes, s_readFrameCallback);

                _reader.AsyncReadPacket(asyncRequest);

                if (!asyncRequest.MustCompleteSynchronously)
                {
                    return 0;
                }
                
                readBytes = asyncRequest.Result;
            }
            else
            {
                readBytes = _reader.ReadPacket(InternalBuffer, SecureChannel.ReadHeaderSize, readBytes);
            }
            
            return ProcessFrameBody(readBytes, buffer, offset, count, asyncRequest);
        }

        //
        // readBytes == SSL Data Payload size on input or 0 on EOF.
        //
        private int ProcessFrameBody(int readBytes, byte[] buffer, int offset, int count, AsyncProtocolRequest asyncRequest)
        {
            if (readBytes == 0)
            {
                // EOF
                throw new IOException(SR.net_io_eof);
            }

            // Set readBytes to total number of received bytes.
            readBytes += SecureChannel.ReadHeaderSize;

            // Decrypt into internal buffer, change "readBytes" to count now _Decrypted Bytes_.
            int data_offset = 0;

            SecurityStatusPal status = _sslState.DecryptData(InternalBuffer, ref data_offset, ref readBytes);

            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                byte[] extraBuffer = null;
                if (readBytes != 0)
                {
                    extraBuffer = new byte[readBytes];
                    Buffer.BlockCopy(InternalBuffer, data_offset, extraBuffer, 0, readBytes);
                }

                // Reset internal buffer count.
                SkipBytes(InternalBufferCount);
                return ProcessReadErrorCode(status, buffer, offset, count, asyncRequest, extraBuffer);
            }


            if (readBytes == 0 && count != 0)
            {
                // Read again since remote side has sent encrypted 0 bytes.
                SkipBytes(InternalBufferCount);
                return -1;
            }

            // Decrypted data start from "data_offset" offset, the total count can be shrunk after decryption.
            EnsureInternalBufferSize(0, data_offset + readBytes);
            SkipBytes(data_offset);

            if (readBytes > count)
            {
                readBytes = count;
            }

            Buffer.BlockCopy(InternalBuffer, InternalOffset, buffer, offset, readBytes);

            // This will adjust both the remaining internal buffer count and the offset.
            SkipBytes(readBytes);

            _sslState.FinishRead(null);
            if (asyncRequest != null)
            {
                asyncRequest.CompleteUser((object)readBytes);
            }

            return readBytes;
        }

        private int ProcessReadErrorCode(SecurityStatusPal status, byte[] buffer, int offset, int count, AsyncProtocolRequest asyncRequest, byte[] extraBuffer)
        {
            ProtocolToken message = new ProtocolToken(null, status);
            if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"***Processing an error Status = {message.Status}");

            if (message.Renegotiate)
            {
                _sslState.ReplyOnReAuthentication(extraBuffer);

                // Loop on read.
                return -1;
            }

            if (message.CloseConnection)
            {
                _sslState.FinishRead(null);
                if (asyncRequest != null)
                {
                    asyncRequest.CompleteUser((object)0);
                }

                return 0;
            }

            throw new IOException(SR.net_io_decrypt, message.GetException());
        }

        private static void WriteCallback(IAsyncResult transportResult)
        {
            if (transportResult.CompletedSynchronously)
            {
                return;
            }

            if (!(transportResult.AsyncState is AsyncProtocolRequest))
            {
                NetEventSource.Fail(transportResult, "State type is wrong, expected AsyncProtocolRequest.");
            }

            AsyncProtocolRequest asyncRequest = (AsyncProtocolRequest)transportResult.AsyncState;

            var sslStream = (SslStreamInternal)asyncRequest.AsyncObject;

            try
            {
                sslStream._sslState.InnerStream.EndWrite(transportResult);
                sslStream._sslState.FinishWrite();

                if (asyncRequest.Count == 0)
                {
                    // This was the last chunk.
                    asyncRequest.Count = -1;
                }

                sslStream.StartWriting(asyncRequest.Buffer, asyncRequest.Offset, asyncRequest.Count, asyncRequest);
            }
            catch (Exception e)
            {
                if (asyncRequest.IsUserCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }

                sslStream._sslState.FinishWrite();
                asyncRequest.CompleteWithError(e);
            }
        }

        //
        // This is used in a rare situation when async Read is resumed from completed handshake.
        //
        private static void ResumeAsyncReadCallback(AsyncProtocolRequest request)
        {
            try
            {
                ((SslStreamInternal)request.AsyncObject).StartReading(request.Buffer, request.Offset, request.Count, request);
            }
            catch (Exception e)
            {
                if (request.IsUserCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }

                ((SslStreamInternal)request.AsyncObject)._sslState.FinishRead(null);
                request.CompleteWithError(e);
            }
        }

        //
        // This is used in a rare situation when async Write is resumed from completed handshake.
        //
        private static void ResumeAsyncWriteCallback(AsyncProtocolRequest asyncRequest)
        {
            try
            {
                ((SslStreamInternal)asyncRequest.AsyncObject).StartWriting(asyncRequest.Buffer, asyncRequest.Offset, asyncRequest.Count, asyncRequest);
            }
            catch (Exception e)
            {
                if (asyncRequest.IsUserCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }

                ((SslStreamInternal)asyncRequest.AsyncObject)._sslState.FinishWrite();
                asyncRequest.CompleteWithError(e);
            }
        }

        private static void ReadHeaderCallback(AsyncProtocolRequest asyncRequest)
        {
            try
            {
                SslStreamInternal sslStream = (SslStreamInternal)asyncRequest.AsyncObject;
                BufferAsyncResult bufferResult = (BufferAsyncResult)asyncRequest.UserAsyncResult;
                if (-1 == sslStream.StartFrameBody(asyncRequest.Result, bufferResult.Buffer, bufferResult.Offset, bufferResult.Count, asyncRequest))
                {
                    // in case we decrypted 0 bytes start another reading.
                    sslStream.StartReading(bufferResult.Buffer, bufferResult.Offset, bufferResult.Count, asyncRequest);
                }
            }
            catch (Exception e)
            {
                if (asyncRequest.IsUserCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }

                asyncRequest.CompleteWithError(e);
            }
        }

        private static void ReadFrameCallback(AsyncProtocolRequest asyncRequest)
        {
            try
            {
                SslStreamInternal sslStream = (SslStreamInternal)asyncRequest.AsyncObject;
                BufferAsyncResult bufferResult = (BufferAsyncResult)asyncRequest.UserAsyncResult;
                if (-1 == sslStream.ProcessFrameBody(asyncRequest.Result, bufferResult.Buffer, bufferResult.Offset, bufferResult.Count, asyncRequest))
                {
                    // in case we decrypted 0 bytes start another reading.
                    sslStream.StartReading(bufferResult.Buffer, bufferResult.Offset, bufferResult.Count, asyncRequest);
                }
            }
            catch (Exception e)
            {
                if (asyncRequest.IsUserCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }

                asyncRequest.CompleteWithError(e);
            }
        }
    }
}
