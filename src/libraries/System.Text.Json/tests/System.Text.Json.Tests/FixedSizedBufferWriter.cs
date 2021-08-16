// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Tests
{
    internal class FixedSizedBufferWriter : IBufferWriter<byte>
    {
        private readonly byte[] _buffer;
        private int _count;

        public FixedSizedBufferWriter(int capacity)
        {
            _buffer = new byte[capacity];
        }

        public void Clear()
        {
            _count = 0;
        }

        public Span<byte> Free => _buffer.AsSpan(_count);

        public byte[] Formatted => _buffer.AsSpan(0, _count).ToArray();

        public int FormattedCount => _count;

        public Memory<byte> GetMemory(int minimumLength = 0) => _buffer.AsMemory(_count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int minimumLength = 0) => _buffer.AsSpan(_count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int bytes)
        {
            if (_count > _buffer.Length - bytes)
            {
                throw new InvalidOperationException("Cannot advance past the end of the buffer.");
            }
            _count += bytes;
        }
    }
}
