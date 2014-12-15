// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;

namespace System.Reflection.PortableExecutable
{
    public struct PEMemoryBlock
    {
        private readonly AbstractMemoryBlock _block;
        private readonly int _offset;

        internal PEMemoryBlock(AbstractMemoryBlock block, int offset = 0)
        {
            Debug.Assert(block != null);
            Debug.Assert(offset >= 0 && offset < block.Size);

            _block = block;
            _offset = offset;
        }

        public unsafe byte* Pointer
        {
            get
            {
                return (_block != null) ? _block.Pointer + _offset : null;
            }
        }

        public int Length
        {
            get
            {
                return (_block != null) ? _block.Size - _offset : 0;
            }
        }

        // TODO: GetBytes (mutable)

        public ImmutableArray<byte> GetContent()
        {
            return (_block != null) ? _block.GetContent(_offset) : ImmutableArray<byte>.Empty;
        }
    }
}
