// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Buffers
{
    /// <summary>
    /// A handle for the memory.
    /// </summary>
    public unsafe struct MemoryHandle : IDisposable
    {
        private void* _pointer;
        private GCHandle _handle;
        private IPinnable? _pinnable;

        /// <summary>
        /// Creates a new memory handle for the memory.
        /// </summary>
        /// <param name="pointer">pointer to memory</param>
        /// <param name="pinnable">reference to manually managed object, or default if there is no memory manager</param>
        /// <param name="handle">handle used to pin array buffers</param>
        /// <safety>Only stores the supplied pointer, GC handle, and pinnable reference into the struct's fields; it performs no dereference of the pointer.</safety>
        [CLSCompliant(false)]
        public MemoryHandle(void* pointer, GCHandle handle = default, IPinnable? pinnable = default)
        {
            _pointer = pointer;
            _handle = handle;
            _pinnable = pinnable;
        }

        /// <summary>
        /// Returns the pointer to memory, where the memory is assumed to be pinned and hence the address won't change.
        /// </summary>
        /// <safety>Returns the stored pointer value to already-pinned memory; obtaining the address performs no dereference.</safety>
        [CLSCompliant(false)]
        public void* Pointer => _pointer;

        /// <summary>
        /// Frees the pinned handle and releases IPinnable.
        /// </summary>
        public void Dispose()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }

            if (_pinnable != null)
            {
                _pinnable.Unpin();
                _pinnable = null;
            }

            _pointer = null;
        }
    }
}
