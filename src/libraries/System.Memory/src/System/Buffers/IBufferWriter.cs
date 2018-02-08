// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Buffers
{
    /// <summary>
    /// Represents a <see cref="Byte"/> sink
    /// </summary>
    public interface IBufferWriter
    {
        /// <summary>
        /// Notifies <see cref="IBufferWriter"/> that <paramref name="bytes"/> amount of data was written to <see cref="Span{Byte}"/>/<see cref="Memory{Byte}"/>
        /// </summary>
        void Advance(int bytes);

        /// <summary>
        /// Requests the <see cref="Memory{Byte}"/> of at least <paramref name="minimumLength"/> in size.
        /// If <paramref name="minimumLength"/> is equal to <code>0</code>, currently available memory would get returned.
        /// </summary>
        Memory<byte> GetMemory(int minimumLength = 0);

        /// <summary>
        /// Requests the <see cref="Span{Byte}"/> of at least <paramref name="minimumLength"/> in size.
        /// If <paramref name="minimumLength"/> is equal to <code>0</code>, currently available memory would get returned.
        /// </summary>
        Span<byte> GetSpan(int minimumLength = 0);
    }
}
