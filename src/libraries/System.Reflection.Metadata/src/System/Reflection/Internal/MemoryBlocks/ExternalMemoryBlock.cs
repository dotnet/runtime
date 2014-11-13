// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Class representing raw memory but not owning the memory.
    /// </summary>
    internal unsafe sealed class ExternalMemoryBlock : AbstractMemoryBlock
    {
        // keeps the owner of the memory alive as long as the block is alive:
        private readonly object memoryOwner;

        private byte* buffer;
        private int size;

        public ExternalMemoryBlock(object memoryOwner, byte* buffer, int size)
        {
            this.memoryOwner = memoryOwner;
            this.buffer = buffer;
            this.size = size;
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Assert(disposing);
            this.buffer = null;
            this.size = 0;
        }

        public override byte* Pointer
        {
            get { return this.buffer; }
        }

        public override int Size
        {
            get { return this.size; }
        }

        public override ImmutableArray<byte> GetContent(int offset)
        {
            var result = CreateImmutableArray((this.buffer + offset), this.size - offset);
            GC.KeepAlive(memoryOwner);
            return result;
        }
    }
}