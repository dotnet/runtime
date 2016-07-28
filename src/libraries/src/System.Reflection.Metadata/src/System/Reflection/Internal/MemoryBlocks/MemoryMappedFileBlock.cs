// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    internal unsafe sealed class MemoryMappedFileBlock : AbstractMemoryBlock
    {
        private readonly int _size;
        private IDisposable _accessor; // MemoryMappedViewAccessor
        private byte* _pointer;
        private SafeBuffer _safeBuffer;

        internal unsafe MemoryMappedFileBlock(IDisposable accessor, SafeBuffer safeBuffer, byte* pointer, int size)
        {
            _accessor = accessor;
            _safeBuffer = safeBuffer;
            _pointer = pointer;
            _size = size;
        }

        ~MemoryMappedFileBlock()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (_safeBuffer != null)
            {
                _safeBuffer.ReleasePointer();
                _safeBuffer = null;
            }

            if (_accessor != null)
            {
                _accessor.Dispose();
                _accessor = null;
            }

            _pointer = null;
        }

        public override byte* Pointer => _pointer;
        public override int Size => _size;
    }
}
