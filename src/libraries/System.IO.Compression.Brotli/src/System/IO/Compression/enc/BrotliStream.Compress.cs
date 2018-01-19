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
        private BrotliEncoder _encoder;

        public BrotliStream(Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, leaveOpen: false) { }
        public BrotliStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen) : this(stream, CompressionMode.Compress, leaveOpen)
        {
            _encoder.SetQuality(BrotliUtils.GetQualityFromCompressionLevel(compressionLevel));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateParameters(buffer, offset, count);
            WriteCore(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override void Write(ReadOnlySpan<byte> source)
        {
            WriteCore(source);
        }

        internal void WriteCore(ReadOnlySpan<byte> source, bool isFinalBlock = false)
        {
            if (_mode != CompressionMode.Compress)
                throw new InvalidOperationException(SR.BrotliStream_Decompress_UnsupportedOperation);
            EnsureNotDisposed();

            OperationStatus lastResult = OperationStatus.DestinationTooSmall;
            Span<byte> output = new Span<byte>(_buffer);
            while (lastResult == OperationStatus.DestinationTooSmall)
            {
                int bytesConsumed = 0;
                int bytesWritten = 0;
                lastResult = _encoder.Compress(source, output, out bytesConsumed, out bytesWritten, isFinalBlock);
                if (lastResult == OperationStatus.InvalidData)
                    throw new InvalidOperationException(SR.BrotliStream_Compress_InvalidData);
                if (bytesWritten > 0)
                    _stream.Write(output.Slice(0, bytesWritten));
                if (bytesConsumed > 0)
                    source = source.Slice(bytesConsumed);
            }
        }

        public override IAsyncResult BeginWrite(byte[] array, int offset, int count, AsyncCallback asyncCallback, object asyncState) =>
            TaskToApm.Begin(WriteAsync(array, offset, count, CancellationToken.None), asyncCallback, asyncState);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        public override Task WriteAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(array, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(array, offset, count), cancellationToken);
        }

        public override Task WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_mode != CompressionMode.Compress)
                throw new InvalidOperationException(SR.BrotliStream_Decompress_UnsupportedOperation);
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled<int>(cancellationToken) :
                WriteAsyncMemoryCore(source, cancellationToken);
        }

        private async Task WriteAsyncMemoryCore(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
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
                    lastResult = _encoder.Compress(source, output, out bytesConsumed, out bytesWritten, isFinalBlock: false);
                    if (lastResult == OperationStatus.InvalidData)
                        throw new InvalidOperationException(SR.BrotliStream_Compress_InvalidData);
                    if (bytesConsumed > 0)
                        source = source.Slice(bytesConsumed);
                    if (bytesWritten > 0)
                        await _stream.WriteAsync(_buffer, 0, bytesWritten, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                AsyncOperationCompleting();
            }
        }

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
                    int bytesWritten = 0;
                    lastResult = _encoder.Flush(output, out bytesWritten);
                    if (lastResult == OperationStatus.InvalidData)
                        throw new InvalidDataException(SR.BrotliStream_Compress_InvalidData);
                    if (bytesWritten > 0)
                    {
                        _stream.Write(output.Slice(0, bytesWritten));
                    }
                }
            }
        }

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
            }
            finally
            {
                AsyncOperationCompleting();
            }
        }
    }
}
