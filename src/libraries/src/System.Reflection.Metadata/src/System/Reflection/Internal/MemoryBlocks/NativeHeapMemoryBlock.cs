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
        private byte* _pointer;
        private readonly int _size;

        internal NativeHeapMemoryBlock(int size)
        {
            _pointer = (byte*)Marshal.AllocHGlobal(size);
            _size = size;
        }

        ~NativeHeapMemoryBlock()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            Marshal.FreeHGlobal((IntPtr)_pointer);
            _pointer = null;
        }

        public override byte* Pointer
        {
            get { return _pointer; }
        }

        public override int Size
        {
            get { return _size; }
        }

        public override ImmutableArray<byte> GetContent(int offset)
        {
            var result = CreateImmutableArray(_pointer + offset, _size - offset);
            GC.KeepAlive(this);
            return result;
        }
    }
}
