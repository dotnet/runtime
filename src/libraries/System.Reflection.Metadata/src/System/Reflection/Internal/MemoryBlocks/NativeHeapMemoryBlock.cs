// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Represents memory block allocated on native heap.
    /// </summary>
    /// <remarks>
    /// Owns the native memory resource.
    /// </remarks>
    internal unsafe sealed class NativeHeapMemoryBlock : AbstractMemoryBlock
    {
        private byte* pointer;
        private readonly int size;

        internal NativeHeapMemoryBlock(int size)
        {
            this.pointer = (byte*)Marshal.AllocHGlobal(size);
            this.size = size;
        }

        ~NativeHeapMemoryBlock()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            Marshal.FreeHGlobal((IntPtr)pointer);
            pointer = null;
        }

        public override byte* Pointer
        {
            get { return pointer; }
        }

        public override int Size
        {
            get { return size; }
        }

        public override ImmutableArray<byte> GetContent(int offset)
        {
            var result = CreateImmutableArray(this.pointer + offset, this.size - offset);
            GC.KeepAlive(this);
            return result;
        }
    }
}
