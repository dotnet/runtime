// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Buffers
{
    /// <summary>Provides a memory handle for a block of memory.</summary>
    /// <remarks>A <see cref="MemoryHandle" /> instance represents a handle to a pinned block of memory. It is returned by the following methods:
    /// - <see cref="System.Buffers.IPinnable.Pin" />.
    /// - <see cref="System.Memory{T}.Pin" />
    /// - <see cref="System.ReadOnlyMemory{T}.Pin" />.
    /// - <see cref="System.Buffers.MemoryManager{T}.Pin" /></remarks>
    public unsafe struct MemoryHandle : IDisposable
    {
        private void* _pointer;
        private GCHandle _handle;
        private IPinnable? _pinnable;

        /// <summary>Creates a new memory handle for the block of memory.</summary>
        /// <param name="pointer">A pointer to memory.</param>
        /// <param name="handle">A handle used to pin array buffers.</param>
        /// <param name="pinnable">A reference to a manually managed object, or <see langword="default" /> if there is no memory manager.</param>
        [CLSCompliant(false)]
        public MemoryHandle(void* pointer, GCHandle handle = default, IPinnable? pinnable = default)
        {
            _pointer = pointer;
            _handle = handle;
            _pinnable = pinnable;
        }

        /// <summary>Returns a pointer to the memory block.</summary>
        /// <value>A pointer to the memory block.</value>
        /// <remarks>The memory is assumed to be pinned so that its address won't change.</remarks>
        [CLSCompliant(false)]
        public void* Pointer => _pointer;

        /// <summary>Frees the pinned handle and releases the <see cref="System.Buffers.IPinnable" /> instance.</summary>
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
