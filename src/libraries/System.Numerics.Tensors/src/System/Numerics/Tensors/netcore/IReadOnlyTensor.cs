// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace System.Numerics.Tensors
{
    /// <summary>Represents a read-only tensor.</summary>
    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public interface IReadOnlyTensor
    {
        /// <summary>Gets the specified element of the tensor.</summary>
        /// <param name="indexes">The index of the element for which to get.</param>
        /// <returns>The element that exists at <paramref name="indexes" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   Thrown when one of the following conditions is met:
        ///   * <paramref name="indexes" /> does not contain <see cref="Rank" /> elements
        ///   * <paramref name="indexes" /> contains an element that is negative or greater than or equal to the corresponding dimension length
        /// </exception>
        object? this[params scoped ReadOnlySpan<nint> indexes] { get; }

        /// <inheritdoc cref="this[ReadOnlySpan{nint}]" />
        object? this[params scoped ReadOnlySpan<NIndex> indexes] { get; }

        /// <summary>Gets the total number of items in the tensor.</summary>
        nint FlattenedLength { get; }

        /// <summary>Gets a value indicating whether this tensor is empty.</summary>
        /// <value><see langword="true"/> if this tensor is empty; otherwise, <see langword="false"/>.</value>
        bool IsEmpty { get; }

        /// <summary>Gets a value that indicates whether the underlying buffer is pinned.</summary>
        bool IsPinned { get; }

        /// <summary>Gets the length of each dimension in the tensor.</summary>
        [UnscopedRef]
        ReadOnlySpan<nint> Lengths { get; }

        /// <summary>Gets the rank, or number of dimensions, in the tensor.</summary>
        int Rank { get; }

        /// <summary>Gets the stride of each dimension in the tensor.</summary>
        [UnscopedRef]
        ReadOnlySpan<nint> Strides { get; }

        /// <summary>Pins and gets a <see cref="MemoryHandle"/> to the backing memory.</summary>
        /// <returns><see cref="MemoryHandle"/></returns>
        MemoryHandle GetPinnedHandle();
    }
}
