// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>An abstract base class that is used to replace the implementation of <see cref="System.Memory{T}" />.</summary>
    /// <typeparam name="T">The type of items in the memory buffer managed by this memory manager.</typeparam>
    /// <remarks>The <see cref="MemoryManager{T}" /> class is used to extend the knowledge of types that <see cref="System.Memory{T}" /> is able to represent. For example, you can derive from <see cref="MemoryManager{T}" /> to allow <see cref="System.Memory{T}" /> to be backed by a <see cref="System.Runtime.InteropServices.SafeHandle" />.
    /// <format type="text/markdown"><![CDATA[
    /// > [!NOTE]
    /// > The `MemoryManager<T>` class is intended for advanced scenarios. Most developers do not need to use it.
    /// ]]></format></remarks>
    public abstract class MemoryManager<T> : IMemoryOwner<T>, IPinnable
    {
        /// <summary>Gets the memory block handled by this <see cref="System.Buffers.MemoryManager{T}" />.</summary>
        /// <value>The memory block handled by this <see cref="System.Buffers.MemoryManager{T}" />.</value>
        public virtual Memory<T> Memory => new Memory<T>(this, GetSpan().Length);

        /// <summary>Returns a memory span that wraps the underlying memory buffer.</summary>
        /// <returns>A memory span that wraps the underlying memory buffer.</returns>
        public abstract Span<T> GetSpan();

        /// <summary>Returns a handle to the memory that has been pinned and whose address can be taken.</summary>
        /// <param name="elementIndex">The offset to the element in the memory buffer at which the returned <see cref="System.Buffers.MemoryHandle" /> points.</param>
        /// <returns>A handle to the memory that has been pinned.</returns>
        public abstract MemoryHandle Pin(int elementIndex = 0);

        /// <summary>Unpins pinned memory so that the garbage collector is free to move it.</summary>
        public abstract void Unpin();

        /// <summary>Returns a memory buffer consisting of a specified number of elements from the memory managed by the current memory manager.</summary>
        /// <param name="length">The number of elements in the memory buffer, starting at offset 0.</param>
        /// <returns>A memory buffer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Memory<T> CreateMemory(int length) => new Memory<T>(this, length);

        /// <summary>Returns a memory buffer consisting of a specified number of elements starting at a specified offset from the memory managed by the current memory manager.</summary>
        /// <param name="start">The offset to the element at which the returned memory buffer starts.</param>
        /// <param name="length">The number of elements to include in the returned memory buffer.</param>
        /// <returns>A memory buffer that consists of <paramref name="length" /> elements starting at offset <paramref name="start" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Memory<T> CreateMemory(int start, int length) => new Memory<T>(this, start, length);

        /// <summary>Returns an array segment.</summary>
        /// <param name="segment">The array segment to write to.</param>
        /// <returns><see langword="true" /> if the method succeeded in retrieving the array segment; otherwise, <see langword="false" />.</returns>
        /// <remarks>If this method is not overridden, it returns the default array segment.</remarks>
        protected internal virtual bool TryGetArray(out ArraySegment<T> segment)
        {
            segment = default;
            return false;
        }

        /// <summary>Releases all resources used by the memory manager.</summary>
        /// <remarks>This method provides the memory manager's <see cref="System.IDisposable.Dispose" /> implementation.</remarks>
        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Releases all resources used by the current memory manager.</summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        protected abstract void Dispose(bool disposing);
    }
}
