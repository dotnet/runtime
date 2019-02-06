// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public sealed partial class BrotliStream : Stream
    {
        private BrotliDecoder _decoder;
        private int _bufferOffset;
        private int _bufferCount;

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateParameters(buffer, offset, count);
            return Read(new Span<byte>(buffer, offset, count));
        }

        public override int ReadByte()
        {
            byte b = default;
            int numRead = Read(MemoryMarshal.CreateSpan(ref b, 1));
            return numRead != 0 ? b : -1;
        }

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

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_mode != CompressionMode.Decompress)
                throw new InvalidOperationException(SR.BrotliStream_Compress_UnsupportedOperation);
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
            }
             return FinishReadAsyncMemory(buffer, cancellationToken);
        }

        private async ValueTask<int> FinishReadAsyncMemory(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            AsyncOperationStarting();
            try
            {
                int totalWritten = 0;
                Memory<byte> source = Memory<byte>.Empty;
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
                        while (_bufferCount < _buffer.Length && ((numRead = await _stream.ReadAsync(new Memory<byte>(_buffer, _bufferCount, _buffer.Length - _bufferCount)).ConfigureAwait(false)) > 0))
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
