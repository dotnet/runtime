// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        /// Requests the <see cref="Memory{Byte}"/> of at least <paramref name="minimumLength"/> in size.
        /// If <paramref name="minimumLength"/> is equal to <code>0</code>, currently available memory would get returned.
        /// </summary>
        Memory<T> GetMemory(int minimumLength = 0);

        /// <summary>
        /// Requests the <see cref="Span{Byte}"/> of at least <paramref name="minimumLength"/> in size.
        /// If <paramref name="minimumLength"/> is equal to <code>0</code>, currently available memory would get returned.
        /// </summary>
        Span<T> GetSpan(int minimumLength = 0);

        /// <summary>
        /// Returns the maximum buffer size supported by this <see cref="IBufferWriter{T}"/>.
        /// </summary>
        int MaxBufferSize { get; }
    }
}
