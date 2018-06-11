// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers
{
    /// <summary>
    /// Represents a <typeparam name="T"/> sink
    /// </summary>
    public interface IBufferWriter<T>
    {
        /// <summary>
        /// Notifies <see cref="IBufferWriter{T}"/> that <paramref name="count"/> amount of data was written to <see cref="Span{T}"/>/<see cref="Memory{T}"/>
        /// </summary>
        void Advance(int count);

        /// <summary>
        /// Requests the <see cref="Memory{T}"/> that is at least <paramref name="sizeHint"/> in size if possible, otherwise returns maximum available memory.
        /// If <paramref name="sizeHint"/> is equal to <code>0</code>, currently available memory would get returned.
        /// </summary>
        Memory<T> GetMemory(int sizeHint = 0);

        /// <summary>
        /// Requests the <see cref="Span{T}"/> that is at least <paramref name="sizeHint"/> in size if possible, otherwise returns maximum available memory.
        /// If <paramref name="sizeHint"/> is equal to <code>0</code>, currently available memory would get returned.
        /// </summary>
        Span<T> GetSpan(int sizeHint = 0);
    }
}
