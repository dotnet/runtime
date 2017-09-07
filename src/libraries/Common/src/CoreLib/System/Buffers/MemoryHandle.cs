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
        public MemoryHandle(IRetainable owner, void* pinnedPointer = null, GCHandle handle = default(GCHandle))
        {
            _owner = owner;
            _pointer = pinnedPointer;
            _handle = handle;
        }

        [CLSCompliant(false)]
        public void* PinnedPointer => _pointer;

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