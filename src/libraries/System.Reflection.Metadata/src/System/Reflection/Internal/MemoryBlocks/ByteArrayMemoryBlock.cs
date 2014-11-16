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
        private ByteArrayMemoryProvider provider;
        private readonly int start;
        private readonly int size;

        internal ByteArrayMemoryBlock(ByteArrayMemoryProvider provider, int start, int size)
        {
            this.provider = provider;
            this.size = size;
            this.start = start;
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Assert(disposing);
            provider = null;
        }

        public unsafe override byte* Pointer
        {
            get
            {
                return provider.Pointer + start;
            }
        }

        public override int Size
        {
            get
            {
                return size;
            }
        }

        public override ImmutableArray<byte> GetContent(int offset)
        {
            return ImmutableArray.Create(provider.array, start + offset, size - offset);
        }
    }
}