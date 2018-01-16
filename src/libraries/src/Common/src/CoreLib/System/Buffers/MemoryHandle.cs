// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    public unsafe struct MemoryHandle : IDisposable
    {
        private IRetainable _owner;
        private void* _pointer;
        private GCHandle _handle;

        [CLSCompliant(false)]
        public MemoryHandle(IRetainable owner, void* pointer = null, GCHandle handle = default(GCHandle))
        {
            _owner = owner;
            _pointer = pointer;
            _handle = handle;
        }

        internal void AddOffset(int offset)
        {
            if (_pointer == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.pointer);
            }
            else
            {
                _pointer = (void*)((byte*)_pointer + offset);
            }
        }

        [CLSCompliant(false)]
        public void* Pointer => _pointer;

        public bool HasPointer => _pointer != null;

        public void Dispose()
        { 
            if (_handle.IsAllocated) 
            {
                _handle.Free();
            }

            if (_owner != null) 
            {
                _owner.Release();
                _owner = null;
            }

            _pointer = null;           
        }
        
    }
}