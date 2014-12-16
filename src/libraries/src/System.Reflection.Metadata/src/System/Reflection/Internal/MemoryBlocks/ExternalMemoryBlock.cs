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
        private readonly object _memoryOwner;

        private byte* _buffer;
        private int _size;

        public ExternalMemoryBlock(object memoryOwner, byte* buffer, int size)
        {
            _memoryOwner = memoryOwner;
            _buffer = buffer;
            _size = size;
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Assert(disposing);
            _buffer = null;
            _size = 0;
        }

        public override byte* Pointer
        {
            get { return _buffer; }
        }

        public override int Size
        {
            get { return _size; }
        }

        public override ImmutableArray<byte> GetContent(int offset)
        {
            var result = CreateImmutableArray((_buffer + offset), _size - offset);
            GC.KeepAlive(_memoryOwner);
            return result;
        }
    }
}