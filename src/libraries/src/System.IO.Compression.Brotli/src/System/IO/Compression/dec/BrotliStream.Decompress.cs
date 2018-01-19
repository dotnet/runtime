// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public sealed partial class BrotliStream : Stream
    {
        private BrotliDecoder _decoder;

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateParameters(buffer, offset, count);
            return Read(new Span<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> destination)
        {
            if (_mode != CompressionMode.Decompress)
                throw new InvalidOperationException(SR.BrotliStream_Compress_UnsupportedOperation);
            EnsureNotDisposed();
            int totalWritten = 0;
            Span<byte> source = Span<byte>.Empty;
            OperationStatus lastResult = OperationStatus.DestinationTooSmall;
            // We want to continue calling Decompress until we're either out of space for output or until Decompress indicates it is finished.
            while (destination.Length > 0 && lastResult != OperationStatus.Done)
            {
                int bytesConsumed = 0;
                int bytesWritten = 0;

                if (lastResult == OperationStatus.NeedMoreData)
                {
                    int readBytes = 0;
                    int iter = 0;
                    while (readBytes < _buffer.Length && ((iter = _stream.Read(_buffer, readBytes, _buffer.Length - readBytes)) > 0))
                    {
                        readBytes += iter;
                        if (readBytes > _buffer.Length)
                        {
                            // The stream is either malicious or poorly implemented and returned a number of
                            // bytes larger than the buffer supplied to it.
                            throw new InvalidDataException(SR.BrotliStream_Decompress_InvalidStream);
                        }
                    }
                    if (readBytes <= 0)
                    {
                        break;
                    }
                    source = new Span<byte>(_buffer, 0, readBytes);
                }

                lastResult = _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);
                if (lastResult == OperationStatus.InvalidData)
                    throw new InvalidOperationException(SR.BrotliStream_Decompress_InvalidData);
                if (bytesConsumed > 0)
                    source = source.Slice(bytesConsumed);
                if (bytesWritten > 0)
                {
                    totalWritten += bytesWritten;
                    destination = destination.Slice(bytesWritten);
                }
            }

            return totalWritten;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override Task<int> ReadAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(array, offset, count);
            return ReadAsync(new Memory<byte>(array, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_mode != CompressionMode.Decompress)
                throw new InvalidOperationException(SR.BrotliStream_Compress_UnsupportedOperation);
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
            }
             return FinishReadAsyncMemory(destination, cancellationToken);
        }

        private async ValueTask<int> FinishReadAsyncMemory(Memory<byte> destination, CancellationToken cancellationToken)
        {
            AsyncOperationStarting();
            try
            {
                int totalWritten = 0;
                Memory<byte> source = Memory<byte>.Empty;
                OperationStatus lastResult = OperationStatus.DestinationTooSmall;
                // We want to continue calling Decompress until we're either out of space for output or until Decompress indicates it is finished.
                while (destination.Length > 0 && lastResult != OperationStatus.Done)
                {

                    int bytesConsumed = 0;
                    int bytesWritten = 0;

                    if (lastResult == OperationStatus.NeedMoreData)
                    {
                        int readBytes = 0;
                        int iter = 0;
                        while (readBytes < _buffer.Length && ((iter = await _stream.ReadAsync(_buffer, readBytes, _buffer.Length - readBytes, cancellationToken).ConfigureAwait(false)) > 0))
                        {
                            readBytes += iter;
                            if (readBytes > _buffer.Length)
                            {
                                // The stream is either malicious or poorly implemented and returned a number of
                                // bytes larger than the buffer supplied to it.
                                throw new InvalidDataException(SR.BrotliStream_Decompress_InvalidStream);
                            }
                        }
                        if (readBytes <= 0)
                        {
                            break;
                        }
                        source = new Memory<byte>(_buffer, 0, readBytes);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    lastResult = _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);
                    if (lastResult == OperationStatus.InvalidData)
                        throw new InvalidOperationException(SR.BrotliStream_Decompress_InvalidData);
                    if (bytesConsumed > 0)
                        source = source.Slice(bytesConsumed);
                    if (bytesWritten > 0)
                    {
                        totalWritten += bytesWritten;
                        destination = destination.Slice(bytesWritten);
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
