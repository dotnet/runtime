// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        protected override void Dispose(bool disposing)
        {
            Debug.Assert(disposing);
            _provider = null;
        }

        public unsafe override byte* Pointer
        {
            get
            {
                return _provider.Pointer + _start;
            }
        }

        public override int Size
        {
            get
            {
                return _size;
            }
        }

        public override ImmutableArray<byte> GetContent(int offset)
        {
            return ImmutableArray.Create(_provider.array, _start + offset, _size - offset);
        }
    }
}