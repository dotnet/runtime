// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers
{
    /// <summary>
    /// Represents a region of native memory. The <see cref="Memory"/> property can be used
    /// to get a <see cref="Memory{Byte}"/> backed by this memory region.
    /// </summary>
    public abstract class BoundedMemory<T> : IDisposable where T : unmanaged
    {
        /// <summary>
        /// Returns a value stating whether this native memory block is readonly.
        /// </summary>
        public abstract bool IsReadonly { get; }

        /// <summary>Gets the length of the <see cref="BoundedMemory{T}"/> instance.</summary>
        public abstract int Length { get; }

        /// <summary>
        /// Gets the <see cref="Memory{Byte}"/> which represents this native memory.
        /// This <see cref="BoundedMemory{T}"/> instance must be kept alive while working with the <see cref="Memory{Byte}"/>.
        /// </summary>
        public abstract Memory<T> Memory { get; }

        /// <summary>
        /// Gets the <see cref="Span{Byte}"/> which represents this native memory.
        /// This <see cref="BoundedMemory{T}"/> instance must be kept alive while working with the <see cref="Span{Byte}"/>.
        /// </summary>
        public abstract Span<T> Span { get; }

        /// <summary>
        /// Disposes this <see cref="BoundedMemory{T}"/> instance.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Sets this native memory block to be readonly. Writes to this block will cause an AV.
        /// This method has no effect if the memory block is zero length or if the underlying
        /// OS does not support marking the memory block as readonly.
        /// </summary>
        public abstract void MakeReadonly();

        /// <summary>
        /// Sets this native memory block to be read+write.
        /// This method has no effect if the memory block is zero length or if the underlying
        /// OS does not support marking the memory block as read+write.
        /// </summary>
        public abstract void MakeWriteable();

        /// <summary>
        /// Gets the <see cref="Span{Byte}"/> which represents this native memory.
        /// This <see cref="BoundedMemory{T}"/> instance must be kept alive while working with the <see cref="Span{Byte}"/>.
        /// </summary>
        public static implicit operator Span<T>(BoundedMemory<T> boundedMemory) => boundedMemory.Span;

        /// <summary>
        /// Gets the <see cref="ReadOnlySpan{Byte}"/> which represents this native memory.
        /// This <see cref="BoundedMemory{T}"/> instance must be kept alive while working with the <see cref="ReadOnlySpan{Byte}"/>.
        /// </summary>
        public static implicit operator ReadOnlySpan<T>(BoundedMemory<T> boundedMemory) => boundedMemory.Span;

        /// <summary>
        /// Gets a reference to the element at the specified index.
        /// This <see cref="BoundedMemory{T}"/> instance must be kept alive while working with the reference.
        /// </summary>
        public ref T this[int index] => ref Span[index];
    }
}
