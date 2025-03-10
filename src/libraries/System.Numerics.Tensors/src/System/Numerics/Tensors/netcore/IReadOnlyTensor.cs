// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Numerics.Tensors
{

    /// <summary>
    /// Represents a read-only tensor.
    /// </summary>
    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public interface IReadOnlyTensor
    {
        /// <summary>
        /// Gets a value that indicates whether the collection is currently empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets a value that indicates whether the underlying buffer is pinned.
        /// </summary>
        bool IsPinned { get; }

        /// <summary>
        /// Gets the number of elements in the tensor.
        /// </summary>
        nint FlattenedLength { get; }

        /// <summary>
        /// Gets the number of dimensions in the tensor.
        /// </summary>
        int Rank { get; }

        /// <summary>
        /// Gets the length of each dimension in the tensor.
        /// </summary>
        [UnscopedRef]
        ReadOnlySpan<nint> Lengths { get; }

        /// <summary>
        /// Gets the stride of each dimension in the tensor.
        /// </summary>
        [UnscopedRef]
        ReadOnlySpan<nint> Strides { get; }

        /// <summary>
        /// Gets the value at the specified indexes.
        /// </summary>
        /// <param name="indexes">The indexes to be used.</param>
        object this[params scoped ReadOnlySpan<nint> indexes] { get; }

        /// <summary>
        /// Gets the value at the specified indexes.
        /// </summary>
        /// <param name="indexes">The indexes to be used.</param>
        object this[params scoped ReadOnlySpan<NIndex> indexes] { get; }

        /// <summary>
        /// Pins and gets a <see cref="MemoryHandle"/> to the backing memory.
        /// </summary>
        /// <returns><see cref="MemoryHandle"/></returns>
        MemoryHandle GetPinnedHandle();
    }

    /// <summary>
    /// Represents a read-only tensor.
    /// </summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="T">The element type.</typeparam>
    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public interface IReadOnlyTensor<TSelf, T> : IReadOnlyTensor, IEnumerable<T>
        where TSelf : IReadOnlyTensor<TSelf, T>
    {
        /// <summary>
        /// Gets an empty tensor.
        /// </summary>
        static abstract TSelf? Empty { get; }

        /// <summary>
        /// Gets the value at the specified indexes.
        /// </summary>
        /// <param name="indexes">The indexes to be used.</param>
        new T this[params scoped ReadOnlySpan<nint> indexes] { get; }

        /// <summary>
        /// Gets the value at the specified indexes.
        /// </summary>
        /// <param name="indexes">The indexes to be used.</param>
        new T this[params scoped ReadOnlySpan<NIndex> indexes] { get; }

        /// <summary>
        /// Gets the values at the specified ranges.
        /// </summary>
        /// <param name="ranges">The ranges to be used.</param>
        TSelf this[params scoped ReadOnlySpan<NRange> ranges] { get; }

        /// <summary>
        /// Creates a read-only tensor span for the entire underlying buffer.
        /// </summary>
        /// <returns>The converted <see cref="ReadOnlyTensorSpan{T}"/>.</returns>
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan();

        /// <summary>
        /// Creates a read-only tensor span for the specified start indexes.
        /// </summary>
        /// <param name="start">The start locations to be used.</param>
        /// <returns>The converted <see cref="ReadOnlyTensorSpan{T}"/>.</returns>
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<nint> start);

        /// <summary>
        /// Creates a read-only tensor span for the specified start indexes.
        /// </summary>
        /// <param name="startIndex">The started indexes to be used.</param>
        /// <returns>The converted <see cref="ReadOnlyTensorSpan{T}"/>.</returns>
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NIndex> startIndex);

        /// <summary>
        /// Creates a read-only tensor span for the specified ranges.
        /// </summary>
        /// <param name="range">The ranges to be used.</param>
        /// <returns>The converted <see cref="ReadOnlyTensorSpan{T}"/>.</returns>
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NRange> range);

        /// <summary>
        /// Copies the tensor to the specified destination. The destination tensor must be equal to or larger than the source tensor.
        /// </summary>
        /// <param name="destination">The destination span where the data should be copied to.</param>
        void CopyTo(scoped TensorSpan<T> destination);

        /// <summary>
        /// Flattens the tensor to the specified destination. The destination span must be equal to or larger than the number of elements in the source tensor.
        /// </summary>
        /// <param name="destination">The destination span where the data should be flattened to.</param>
        void FlattenTo(scoped Span<T> destination);

        /// <summary>
        /// Returns a reference to the 0th element of the tensor. If the tensor is empty, returns <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// This method can be used for pinning and is required to support the use of the tensor within a fixed statement.
        /// </remarks>
        ref readonly T GetPinnableReference();

        /// <summary>
        /// Slices the tensor using the specified start indexes.
        /// </summary>
        /// <param name="start">The start locations to be used.</param>
        /// <returns>The sliced tensor.</returns>
        TSelf Slice(params scoped ReadOnlySpan<nint> start);

        /// <summary>
        /// Slices the tensor using the specified start indexes.
        /// </summary>
        /// <param name="startIndex">The start indexes to be used.</param>
        /// <returns>The sliced tensor.</returns>
        TSelf Slice(params scoped ReadOnlySpan<NIndex> startIndex);

        /// <summary>
        /// Slices the tensor using the specified ranges.
        /// </summary>
        /// <param name="range">The ranges to be used.</param>
        /// <returns>The sliced tensor.</returns>
        TSelf Slice(params scoped ReadOnlySpan<NRange> range);

        /// <summary>
        /// Tries to copy the tensor to the specified destination. The destination tensor must be equal to or larger than the source tensor.
        /// </summary>
        /// <param name="destination">The destination span where the data should be copied to.</param>
        /// <returns><see langword="true" /> if the copy succeeded, <see langword="false" /> otherwise.</returns>
        bool TryCopyTo(scoped TensorSpan<T> destination);

        /// <summary>
        /// Tries to flatten the tensor to the specified destination. The destination span must be equal to or larger than the number of elements in the source tensor.
        /// </summary>
        /// <param name="destination">The destination span where the data should be flattened to.</param>
        /// <returns><see langword="true" /> if the flatten succeeded, <see langword="false" /> otherwise.</returns>
        bool TryFlattenTo(scoped Span<T> destination);
    }
}
