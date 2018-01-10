// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security
{
    //
    // This is a wrapping stream that does data encryption/decryption based on a successfully authenticated SSPI context.
    //
    internal partial class SslStreamInternal
    {
        private const int FrameOverhead = 32;
        private const int ReadBufferSize = 4096 * 4 + FrameOverhead;         // We read in 16K chunks + headers.

        private readonly SslState _sslState;
        private int _nestedWrite;
        private int _nestedRead;

        // Never updated directly, special properties are used.  This is the read buffer.
        private byte[] _internalBuffer;

        private int _internalOffset;
        private int _internalBufferCount;

        private int _decryptedBytesOffset;
        private int _decryptedBytesCount;

        internal SslStreamInternal(SslState sslState)
        {
            _sslState = sslState;

            _decryptedBytesOffset = 0;
            _decryptedBytesCount = 0;
        }

        //We will only free the read buffer if it
        //actually contains no decrypted or encrypted bytes
        private void ReturnReadBufferIfEmpty()
        {
            if (_internalBuffer != null && _decryptedBytesCount == 0 && _internalBufferCount == 0)
            {
                ArrayPool<byte>.Shared.Return(_internalBuffer);
                _internalBuffer = null;
                _internalBufferCount = 0;
                _internalOffset = 0;
                _decryptedBytesCount = 0;
                _decryptedBytesOffset = 0;
            }
        }

        ~SslStreamInternal()
        {
            if (_internalBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_internalBuffer);
                _internalBuffer = null;
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
                if (_decryptedBytesCount > 0)
                {
                    int b = _internalBuffer[_decryptedBytesOffset++];
                    _decryptedBytesCount--;
                    ReturnReadBufferIfEmpty();
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
            ValidateParameters(buffer, offset, count);
            SslReadSync reader = new SslReadSync(_sslState);
            return ReadAsyncInternal(reader, new Memory<byte>(buffer, offset, count)).GetAwaiter().GetResult();
        }

        internal void Write(byte[] buffer, int offset, int count)
        {
            ValidateParameters(buffer, offset, count);

            SslWriteSync writeAdapter = new SslWriteSync(_sslState);
            WriteAsyncInternal(writeAdapter, new ReadOnlyMemory<byte>(buffer, offset, count)).GetAwaiter().GetResult();
        }

        internal IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
        {
            return TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
        }

        internal Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(buffer, offset, count);
            SslReadAsync read = new SslReadAsync(_sslState, cancellationToken);
            return ReadAsyncInternal(read, new Memory<byte>(buffer, offset, count)).AsTask();
        }

        internal ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            SslReadAsync read = new SslReadAsync(_sslState, cancellationToken);
            return ReadAsyncInternal(read, buffer);
        }

        internal int EndRead(IAsyncResult asyncResult) => TaskToApm.End<int>(asyncResult);

        internal IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
        {
            return TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
        }

        internal void EndWrite(IAsyncResult asyncResult) => TaskToApm.End(asyncResult);

        internal Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            SslWriteAsync writeAdapter = new SslWriteAsync(_sslState, cancellationToken);
            return WriteAsyncInternal(writeAdapter, buffer);
        }

        internal Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(buffer, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }

        private void ResetReadBuffer()
        {
            Debug.Assert(_decryptedBytesCount == 0);
            Debug.Assert(_internalBuffer == null || _internalBufferCount > 0);

            if (_internalBuffer == null)
            {
                _internalBuffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
            }
            else if (_internalOffset > 0)
            {
                // We have buffered data at a non-zero offset.
                // To maximize the buffer space available for the next read,
                // copy the existing data down to the beginning of the buffer.
                Buffer.BlockCopy(_internalBuffer, _internalOffset, _internalBuffer, 0, _internalBufferCount);
                _internalOffset = 0;
            }
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

        private async ValueTask<int> ReadAsyncInternal<TReadAdapter>(TReadAdapter adapter, Memory<byte> buffer)
            where TReadAdapter : ISslReadAdapter
        {
            if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, nameof(ReadAsync), "read"));
            }

            while (true)
            {
                int copyBytes;
                if (_decryptedBytesCount != 0)
                {
                    copyBytes = CopyDecryptedData(buffer);

                    _sslState.FinishRead(null);
                    _nestedRead = 0;

                    return copyBytes;
                }

                copyBytes = await adapter.LockAsync(buffer).ConfigureAwait(false);
                try
                {
                    if (copyBytes > 0)
                    {
                        return copyBytes;
                    }

                    ResetReadBuffer();
                    int readBytes = await FillBufferAsync(adapter, SecureChannel.ReadHeaderSize).ConfigureAwait(false);
                    if (readBytes == 0)
                    {
                        return 0;
                    }

                    int payloadBytes = _sslState.GetRemainingFrameSize(_internalBuffer, _internalOffset, readBytes);
                    if (payloadBytes < 0)
                    {
                        throw new IOException(SR.net_frame_read_size);
                    }

                    readBytes = await FillBufferAsync(adapter, SecureChannel.ReadHeaderSize + payloadBytes).ConfigureAwait(false);
                    if (readBytes < 0)
                    {
                        throw new IOException(SR.net_frame_read_size);
                    }

                    // At this point, readBytes contains the size of the header plus body.
                    // Set _decrytpedBytesOffset/Count to the current frame we have (including header)
                    // DecryptData will decrypt in-place and modify these to point to the actual decrypted data, which may be smaller.
                    _decryptedBytesOffset = _internalOffset;
                    _decryptedBytesCount = readBytes;
                    SecurityStatusPal status = _sslState.DecryptData(_internalBuffer, ref _decryptedBytesOffset, ref _decryptedBytesCount);

                    // Treat the bytes we just decrypted as consumed
                    // Note, we won't do another buffer read until the decrypted bytes are processed
                    ConsumeBufferedBytes(readBytes);

                    if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        byte[] extraBuffer = null;
                        if (_decryptedBytesCount != 0)
                        {
                            extraBuffer = new byte[_decryptedBytesCount];
                            Buffer.BlockCopy(_internalBuffer, _decryptedBytesOffset, extraBuffer, 0, _decryptedBytesCount);

                            _decryptedBytesCount = 0;
                        }

                        ProtocolToken message = new ProtocolToken(null, status);
                        if (NetEventSource.IsEnabled)
                            NetEventSource.Info(null, $"***Processing an error Status = {message.Status}");

                        if (message.Renegotiate)
                        {
                            if (!_sslState._sslAuthenticationOptions.AllowRenegotiation)
                            {
                                throw new IOException(SR.net_ssl_io_renego);
                            }

                            _sslState.ReplyOnReAuthentication(extraBuffer);

                            // Loop on read.
                            return -1;
                        }

                        if (message.CloseConnection)
                        {
                            _sslState.FinishRead(null);
                            return 0;
                        }

                        throw new IOException(SR.net_io_decrypt, message.GetException());
                    }
                }
                catch (Exception e)
                {
                    _sslState.FinishRead(null);

                    if (e is IOException)
                    {
                        throw;
                    }

                    throw new IOException(SR.net_io_read, e);
                }
                finally
                {
                    _nestedRead = 0;
                }
            }
        }

        private Task WriteAsyncInternal<TWriteAdapter>(TWriteAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
            where TWriteAdapter : struct, ISslWriteAdapter
        {
            _sslState.CheckThrow(authSuccessCheck: true, shutdownCheck: true);

            if (buffer.Length == 0 && !SslStreamPal.CanEncryptEmptyMessage)
            {
                // If it's an empty message and the PAL doesn't support that, we're done.
                return Task.CompletedTask;
            }

            if (Interlocked.Exchange(ref _nestedWrite, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, nameof(WriteAsync), "write"));
            }

            Task t = buffer.Length < _sslState.MaxDataSize ?
                    WriteSingleChunk(writeAdapter, buffer) :
                    WriteAsyncChunked(writeAdapter, buffer);

            if (t.IsCompletedSuccessfully)
            {
                _nestedWrite = 0;
                return t;
            }
            return ExitWriteAsync(t);

            async Task ExitWriteAsync(Task task)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _sslState.FinishWrite();

                    if (e is IOException)
                    {
                        throw;
                    }

                    throw new IOException(SR.net_io_write, e);
                }
                finally
                {
                    _nestedWrite = 0;
                }
            }
        }

        private Task WriteSingleChunk<TWriteAdapter>(TWriteAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
            where TWriteAdapter : struct, ISslWriteAdapter
        {
            // Request a write IO slot.
            Task ioSlot = writeAdapter.LockAsync();
            if (!ioSlot.IsCompletedSuccessfully)
            {
                // Operation is async and has been queued, return.
                return WaitForWriteIOSlot(writeAdapter, ioSlot, buffer);
            }

            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length + FrameOverhead);
            byte[] outBuffer = rentedBuffer;

            SecurityStatusPal status = _sslState.EncryptData(buffer, ref outBuffer, out int encryptedBytes);

            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                // Re-handshake status is not supported.
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                ProtocolToken message = new ProtocolToken(null, status);
                return Task.FromException(new IOException(SR.net_io_encrypt, message.GetException()));
            }

            Task t = writeAdapter.WriteAsync(outBuffer, 0, encryptedBytes);
            if (t.IsCompletedSuccessfully)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                _sslState.FinishWrite();
                return t;
            }
            else
            {
                return CompleteAsync(t, rentedBuffer);
            }

            async Task WaitForWriteIOSlot(TWriteAdapter wAdapter, Task lockTask, ReadOnlyMemory<byte> buff)
            {
                await lockTask.ConfigureAwait(false);
                await WriteSingleChunk(wAdapter, buff).ConfigureAwait(false);
            }

            async Task CompleteAsync(Task writeTask, byte[] bufferToReturn)
            {
                try
                {
                    await writeTask.ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bufferToReturn);
                    _sslState.FinishWrite();
                }
            }
        }

        private async Task WriteAsyncChunked<TWriteAdapter>(TWriteAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
            where TWriteAdapter : struct, ISslWriteAdapter
        {
            do
            {
                int chunkBytes = Math.Min(buffer.Length, _sslState.MaxDataSize);
                await WriteSingleChunk(writeAdapter, buffer.Slice(0, chunkBytes)).ConfigureAwait(false);
                buffer = buffer.Slice(chunkBytes);

            } while (buffer.Length != 0);
        }

        private ValueTask<int> FillBufferAsync<TReadAdapter>(TReadAdapter adapter, int minSize)
            where TReadAdapter : ISslReadAdapter
        {
            if (_internalBufferCount >= minSize)
            {
                return new ValueTask<int>(minSize);
            }

            int initialCount = _internalBufferCount;
            do
            {
                ValueTask<int> t = adapter.ReadAsync(_internalBuffer, _internalBufferCount, _internalBuffer.Length - _internalBufferCount);
                if (!t.IsCompletedSuccessfully)
                {
                    return new ValueTask<int>(InternalFillBufferAsync(adapter, t.AsTask(), minSize, initialCount));
                }
                int bytes = t.Result;
                if (bytes == 0)
                {
                    if (_internalBufferCount != initialCount)
                    {
                        // We read some bytes, but not as many as we expected, so throw.
                        throw new IOException(SR.net_io_eof);
                    }

                    return new ValueTask<int>(0);
                }

                _internalBufferCount += bytes;
            } while (_internalBufferCount < minSize);

            return new ValueTask<int>(minSize);

            async Task<int> InternalFillBufferAsync(TReadAdapter adap, Task<int> task, int min, int initial)
            {
                while (true)
                {
                    int b = await task.ConfigureAwait(false);
                    if (b == 0)
                    {
                        if (_internalBufferCount != initial)
                        {
                            throw new IOException(SR.net_io_eof);
                        }

                        return 0;
                    }

                    _internalBufferCount += b;
                    if (_internalBufferCount >= min)
                    {
                        return min;
                    }

                    task = adap.ReadAsync(_internalBuffer, _internalBufferCount, _internalBuffer.Length - _internalBufferCount).AsTask();
                }
            }
        }

        private void ConsumeBufferedBytes(int byteCount)
        {
            Debug.Assert(byteCount >= 0);
            Debug.Assert(byteCount <= _internalBufferCount);

            _internalOffset += byteCount;
            _internalBufferCount -= byteCount;

            ReturnReadBufferIfEmpty();
        }

        private int CopyDecryptedData(Memory<byte> buffer)
        {
            Debug.Assert(_decryptedBytesCount > 0);

            int copyBytes = Math.Min(_decryptedBytesCount, buffer.Length);
            if (copyBytes != 0)
            {
                new Span<byte>(_internalBuffer, _decryptedBytesOffset, copyBytes).CopyTo(buffer.Span);

                _decryptedBytesOffset += copyBytes;
                _decryptedBytesCount -= copyBytes;
            }
            ReturnReadBufferIfEmpty();
            return copyBytes;
        }
    }
}
