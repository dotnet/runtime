// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Internal
{
    internal unsafe sealed class MemoryMappedFileBlock : AbstractMemoryBlock
    {
        private readonly int size;
        private IDisposable accessor; // MemoryMappedViewAccessor
        private byte* pointer;
        private SafeBuffer safeBuffer;

        internal unsafe MemoryMappedFileBlock(IDisposable accessor, int size)
        {
            this.accessor = accessor;
            this.pointer = MemoryMapLightUp.AcquirePointer(accessor, out safeBuffer);
            this.size = size;
        }

        ~MemoryMappedFileBlock()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (safeBuffer != null)
            {
                safeBuffer.ReleasePointer();
                safeBuffer = null;
            }

            if (accessor != null)
            {
                accessor.Dispose();
                accessor = null;
            }

            pointer = null;
        }

        public override byte* Pointer
        {
            get { return this.pointer; }
        }

        public override int Size
        {
            get { return size; }
        }

        public override ImmutableArray<byte> GetContent(int offset)
        {
            var result = CreateImmutableArray(this.Pointer + offset, this.Size - offset);
            GC.KeepAlive(this);
            return result;
        }
    }
}
