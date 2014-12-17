// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;

namespace System.Reflection.PortableExecutable
{
    public struct PEMemoryBlock
    {
        private readonly AbstractMemoryBlock block;
        private readonly int offset;

        internal PEMemoryBlock(AbstractMemoryBlock block, int offset = 0)
        {
            Debug.Assert(block != null);
            Debug.Assert(offset >= 0 && offset < block.Size);

            this.block = block;
            this.offset = offset;
        }

        public unsafe byte* Pointer
        {
            get
            {
                return (block != null) ? block.Pointer + offset : null;
            }
        }

        public int Length
        {
            get
            {
                return (block != null) ? block.Size - offset : 0;
            }
        }

        // TODO: GetBytes (mutable)

        public ImmutableArray<byte> GetContent()
        {
            return (block != null) ? block.GetContent(offset) : ImmutableArray<byte>.Empty;
        }
    }
}
