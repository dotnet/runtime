// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public override byte* Pointer => _pointer;
        public override int Size => _size;
    }
}
