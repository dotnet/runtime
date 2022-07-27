// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Represents a memory block backed by an array of bytes.
    /// </summary>
    internal sealed class ByteArrayMemoryBlock : AbstractMemoryBlock
    {
        private ByteArrayMemoryProvider _provider;
        private readonly int _start;
        private readonly int _size;

        internal ByteArrayMemoryBlock(ByteArrayMemoryProvider provider, int start, int size)
        {
            _provider = provider;
            _size = size;
            _start = start;
        }

        public override void Dispose()
        {
            _provider = null!;
        }

        public override unsafe byte* Pointer => _provider.Pointer + _start;
        public override int Size => _size;

        public override ImmutableArray<byte> GetContentUnchecked(int start, int length)
        {
            return ImmutableArray.Create(_provider.Array, _start + start, length);
        }
    }
}
