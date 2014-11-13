// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Represents raw memory owned by an external object. 
    /// </summary>
    internal unsafe sealed class ExternalMemoryBlockProvider : MemoryBlockProvider
    {
        private byte* memory;
        private int size;

        public unsafe ExternalMemoryBlockProvider(byte* memory, int size)
        {
            this.memory = memory;
            this.size = size;
        }

        public override int Size
        {
            get
            {
                return size;
            }
        }

        protected override AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
        {
            return new ExternalMemoryBlock(this, memory + start, size);
        }

        public override Stream GetStream(out StreamConstraints constraints)
        {
            constraints = new StreamConstraints(null, 0, size);
            return new ReadOnlyUnmanagedMemoryStream(memory, size);
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Assert(disposing);

            // we don't own the memory, just null out the pointer.
            memory = null;
            size = 0;
        }

        public byte* Pointer
        {
            get
            {
                return memory;
            }
        }
    }
}
