// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    /// <summary>
    /// A handle for the memory.
    /// </summary>
    public unsafe struct MemoryHandle : IDisposable
    {
        private IRetainable _retainable;
        private void* _pointer;
        private GCHandle _handle;

        /// <summary>
        /// Creates a new memory handle for the memory.
        /// </summary>
        /// <param name="retainable">reference to manually managed object</param>
        /// <param name="pinnedPointer">pointer to the buffer, or null if the buffer is not pinned</param>
        /// <param name="handle">handle used to pin array buffers</param>
        public MemoryHandle(IRetainable retainable, void* pinnedPointer = null, GCHandle handle = default(GCHandle))
        {
            _retainable = retainable;
            _pointer = pinnedPointer;
            _handle = handle;
        }

        /// <summary>
        /// Returns the address of the pinned object, or null if the object is not pinned.
        /// </summary>
        public void* PinnedPointer => _pointer;

        /// <summary>
        /// Adds an offset to the pinned pointer.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">
        /// Throw when pinned pointer is null.
        /// </exception>
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

        /// <summary>
        /// Frees the pinned handle and releases IRetainable.
        /// </summary>
        public void Dispose()
        { 
            if (_handle.IsAllocated) 
            {
                _handle.Free();
            }

            if (_retainable != null) 
            {
                _retainable.Release();
                _retainable = null;
            }

            _pointer = null;           
        }
        
    }
}
