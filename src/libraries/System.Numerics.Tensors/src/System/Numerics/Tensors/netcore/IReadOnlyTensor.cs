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
        ///   One of the following conditions is met:
        ///   * <paramref name="indexes" /> does not contain <see cref="Rank" /> elements.
        ///   * <paramref name="indexes" /> contains an element that is negative or greater than or equal to the corresponding dimension length.
        /// </exception>
        object? this[params scoped ReadOnlySpan<nint> indexes] { get; }

        /// <inheritdoc cref="this[ReadOnlySpan{nint}]" />
        object? this[params scoped ReadOnlySpan<NIndex> indexes] { get; }

        /// <summary>Gets the total number of items in the tensor.</summary>
        nint FlattenedLength { get; }

        /// <summary>Gets a value that indicates whether the current tensor has any dimension span where <see cref="IsDense" /> is <see langword="true"/>.</summary>
        /// <value><see langword="true"/> if this tensor has any dense dimensions; otherwise, <see langword="false"/>.</value>
        /// <remarks>
        ///   <para>This does not include the last dimension, <c>GetDimensionSpan(Rank - 1)</c>, as it always iterates one element at a time and would mean this property always returns <see langword="true"/>.</para>
        ///   <para>An example of a tensor that's not dense but has a dense dimension is a 2x2 Tensor where <c>FlattenedLength: 4; Lengths: [2, 2]; Strides: [4, 1]</c>. In such a scenario, the overall tensor is not dense because the backing storage has a length of at least 6. It has two used elements, two unused elements, followed by the last two used elements. However, the two slices representing <c>[0..1, ..]</c> and <c>[1..2, ..]</c> are dense; thus <c>GetDimension(0).GetSlice(n)</c> will iterate dense tensors: <c>FlattenedLength: 2, Length: [2], Strides: [1]</c>.</para>
        /// </remarks>
        bool HasAnyDenseDimensions { get; }

        /// <summary>Gets a value that indicates whether the current tensor is dense.</summary>
        /// <value><see langword="true"/> if this tensor is dense; otherwise, <see langword="false"/>.</value>
        /// <remarks>
        ///   <para>A dense tensor is one where the elements are ordered sequentially in memory and where no gaps exist between the elements.</para>
        ///   <para>For a 2x2 Tensor, this would mean it has <c>FlattenedLength: 4; Lengths: [2, 2]; Strides: [2, 1]</c>. The elements would be sequentially accessed via indexes: <c>[0, 0]; [0, 1]; [1, 0]; [1, 1]</c>.</para>
        /// </remarks>
        bool IsDense { get; }

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
