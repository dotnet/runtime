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

        public override void WriteByte(byte value)
        {
            WriteCore(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
        }

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
                int bytesConsumed = 0;
                int bytesWritten = 0;
                lastResult = _encoder.Compress(buffer, output, out bytesConsumed, out bytesWritten, isFinalBlock);
                if (lastResult == OperationStatus.InvalidData)
                    throw new InvalidOperationException(SR.BrotliStream_Compress_InvalidData);
                if (bytesWritten > 0)
                    _stream.Write(output.Slice(0, bytesWritten));
                if (bytesConsumed > 0)
                    buffer = buffer.Slice(bytesConsumed);
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(buffer, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_mode != CompressionMode.Compress)
                throw new InvalidOperationException(SR.BrotliStream_Decompress_UnsupportedOperation);
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            return new ValueTask(cancellationToken.IsCancellationRequested ?
                Task.FromCanceled<int>(cancellationToken) :
                WriteAsyncMemoryCore(buffer, cancellationToken));
        }

        private async Task WriteAsyncMemoryCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, bool isFinalBlock = false)
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
