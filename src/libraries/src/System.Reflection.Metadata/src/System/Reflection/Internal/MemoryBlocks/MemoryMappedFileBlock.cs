// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        internal unsafe MemoryMappedFileBlock(IDisposable accessor, int size)
        {
            _accessor = accessor;
            _pointer = MemoryMapLightUp.AcquirePointer(accessor, out _safeBuffer);
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
            var result = CreateImmutableArray(this.Pointer + offset, this.Size - offset);
            GC.KeepAlive(this);
            return result;
        }
    }
}
