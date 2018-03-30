// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>
    /// Manager of Memory<typeparamref name="T"/> that provides the implementation.
    /// </summary>
    public abstract class MemoryManager<T> : IMemoryOwner<T>, IPinnable
    {
        /// <summary>
        /// The number of items in the Memory<typeparamref name="T"/>.
        /// </summary>
        public virtual int Length => GetSpan().Length;

        /// <summary>
        /// Returns a Memory<typeparamref name="T"/>.
        /// </summary>
        public virtual Memory<T> Memory => new Memory<T>(this, 0, Length);

        /// <summary>
        /// Returns a span wrapping the underlying memory.
        /// </summary>
        public abstract Span<T> GetSpan();

        /// <summary>
        /// Returns a handle to the memory that has been pinned and hence its address can be taken.
        /// <param name="elementIndex">The offset to the element within the memory at which the returned <see cref="MemoryHandle"/> points to. (default = 0)</param>
        /// </summary>
        public abstract MemoryHandle Pin(int elementIndex = 0);

        /// <summary>
        /// Lets the garbage collector know that the object is free to be moved now.
        /// </summary>
        public abstract void Unpin();

        /// <summary>
        /// Returns an array segment.
        /// <remarks>Returns the default array segment if not overriden.</remarks>
        /// </summary>
        protected internal virtual bool TryGetArray(out ArraySegment<T> segment)
        {
            segment = default;
            return false;
        }

        /// <summary>
        /// Implements IDisposable.
        /// </summary>
        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Clean up of any leftover managed and unmanaged resources.
        /// </summary>
        protected abstract void Dispose(bool disposing);

    }
}
