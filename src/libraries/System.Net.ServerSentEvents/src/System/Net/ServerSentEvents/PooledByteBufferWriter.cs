// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Net.ServerSentEvents
{
    internal sealed class PooledByteBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private const int MinimumBufferSize = 256;
        private ArrayBuffer _buffer = new(initialSize: 256, usePool: true);

        public void Advance(int count) => _buffer.Commit(count);

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            _buffer.EnsureAvailableSpace(Math.Max(sizeHint, MinimumBufferSize));
            return _buffer.AvailableMemory;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            _buffer.EnsureAvailableSpace(Math.Max(sizeHint, MinimumBufferSize));
            return _buffer.AvailableSpan;
        }

        public ReadOnlyMemory<byte> WrittenMemory => _buffer.ActiveMemory;
        public int Capacity => _buffer.Capacity;
        public int WrittenCount => _buffer.ActiveLength;
        public void Reset() => _buffer.Discard(_buffer.ActiveLength);
        public void Dispose() => _buffer.Dispose();
    }
}
