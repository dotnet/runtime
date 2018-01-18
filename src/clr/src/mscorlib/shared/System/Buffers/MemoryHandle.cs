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
        /// <param name="pointer">pointer to memory, or null if a pointer was not provided when the handle was created</param>
        /// <param name="handle">handle used to pin array buffers</param>
        [CLSCompliant(false)]
        public MemoryHandle(IRetainable retainable, void* pointer = null, GCHandle handle = default(GCHandle))
        {
            _retainable = retainable;
            _pointer = pointer;
            _handle = handle;
        }

        /// <summary>
        /// Returns the pointer to memory, or null if a pointer was not provided when the handle was created.
        /// </summary>
        [CLSCompliant(false)]
        public void* Pointer => _pointer;

        /// <summary>
        /// Returns false if the pointer to memory is null.
        /// </summary>
        public bool HasPointer => _pointer != null;

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