// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>
    /// Manager of <see cref="System.Memory{T}"/> that provides the implementation.
    /// </summary>
    public abstract class MemoryManager<T> : IMemoryOwner<T>, IPinnable
    {
        /// <summary>
        /// Returns a <see cref="System.Memory{T}"/>.
        /// </summary>
        public virtual Memory<T> Memory => new Memory<T>(this, GetSpan().Length);

        /// <summary>
        /// Returns a span wrapping the underlying memory.
        /// </summary>
        public abstract Span<T> GetSpan();

        /// <summary>
        /// Returns a handle to the memory that has been pinned and hence its address can be taken.
        /// </summary>
        /// <param name="elementIndex">The offset to the element within the memory at which the returned <see cref="MemoryHandle"/> points to. (default = 0)</param>
        public abstract MemoryHandle Pin(int elementIndex = 0);

        /// <summary>
        /// Lets the garbage collector know that the object is free to be moved now.
        /// </summary>
        public abstract void Unpin();

        /// <summary>
        /// Returns a <see cref="System.Memory{T}"/> for the current <see cref="MemoryManager{T}"/>.
        /// </summary>
        /// <param name="length">The element count in the memory, starting at offset 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Memory<T> CreateMemory(int length) => new Memory<T>(this, length);

        /// <summary>
        /// Returns a <see cref="System.Memory{T}"/> for the current <see cref="MemoryManager{T}"/>.
        /// </summary>
        /// <param name="start">The offset to the element which the returned memory starts at.</param>
        /// <param name="length">The element count in the memory, starting at element offset <paramref name="start"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Memory<T> CreateMemory(int start, int length) => new Memory<T>(this, start, length);

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
