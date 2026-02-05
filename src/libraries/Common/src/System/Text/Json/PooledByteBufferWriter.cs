// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
    internal sealed class PooledByteBufferWriter : PipeWriter, IDisposable
    {
        private const int MinimumBufferSize = 256;

        private ArrayBuffer _buffer;
        private readonly Stream? _stream;

        public PooledByteBufferWriter(int initialCapacity)
        {
            _buffer = new ArrayBuffer(initialCapacity, usePool: true);
        }

        public PooledByteBufferWriter(int initialCapacity, Stream stream) : this(initialCapacity)
        {
            _stream = stream;
        }

        public ReadOnlySpan<byte> WrittenSpan => _buffer.ActiveSpan;

        public ReadOnlyMemory<byte> WrittenMemory => _buffer.ActiveMemory;

        public int Capacity => _buffer.Capacity;

        public void Clear() => _buffer.Discard(_buffer.ActiveLength);

        public void ClearAndReturnBuffers() => _buffer.ClearAndReturnBuffer();

        public void Dispose() => _buffer.Dispose();

        public void InitializeEmptyInstance(int initialCapacity)
        {
            Debug.Assert(initialCapacity > 0);
            Debug.Assert(_buffer.ActiveLength == 0);

            _buffer.EnsureAvailableSpace(initialCapacity);
        }

        public static PooledByteBufferWriter CreateEmptyInstanceForCaching() => new PooledByteBufferWriter(initialCapacity: 0);

        public override void Advance(int count) => _buffer.Commit(count);

        public override Memory<byte> GetMemory(int sizeHint = MinimumBufferSize)
        {
            Debug.Assert(sizeHint > 0);

            _buffer.EnsureAvailableSpace(sizeHint);
            return _buffer.AvailableMemory;
        }

        public override Span<byte> GetSpan(int sizeHint = MinimumBufferSize)
        {
            Debug.Assert(sizeHint > 0);

            _buffer.EnsureAvailableSpace(sizeHint);
            return _buffer.AvailableSpan;
        }

#if NET
        internal void WriteToStream(Stream destination) => destination.Write(_buffer.ActiveSpan);
#else
        internal void WriteToStream(Stream destination) => destination.Write(_buffer.ActiveMemory);
#endif

        public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            Debug.Assert(_stream is not null);
            await _stream.WriteAsync(WrittenMemory, cancellationToken).ConfigureAwait(false);
            Clear();

            return new FlushResult(isCanceled: false, isCompleted: false);
        }

        public override bool CanGetUnflushedBytes => true;
        public override long UnflushedBytes => _buffer.ActiveLength;

        // This type is used internally in JsonSerializer to help buffer and flush bytes to the underlying Stream.
        // It's only pretending to be a PipeWriter and doesn't need Complete or CancelPendingFlush for the internal usage.
        public override void CancelPendingFlush() => throw new NotImplementedException();
        public override void Complete(Exception? exception = null) => throw new NotImplementedException();
    }
}
