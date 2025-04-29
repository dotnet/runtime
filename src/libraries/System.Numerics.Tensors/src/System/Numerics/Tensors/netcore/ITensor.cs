// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace System.Numerics.Tensors
{
    /// <summary>
    /// Represents a tensor.
    /// </summary>
    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public interface ITensor : IReadOnlyTensor
    {
        /// <summary>
        /// Gets the value at the specified indexes.
        /// </summary>
        /// <param name="indexes">The indexes to be used.</param>
        new object this[params scoped ReadOnlySpan<nint> indexes] { get; set; }

        /// <summary>
        /// Gets the value at the specified indexes.
        /// </summary>
        /// <param name="indexes">The indexes to be used.</param>
        new object this[params scoped ReadOnlySpan<NIndex> indexes] { get; set; }

        /// <summary>
        /// Gets a value that indicates whether the collection is read-only.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Clears the tensor.
        /// </summary>
        void Clear();

        /// <summary>
        /// Fills the contents of this tensor with the given value.
        /// </summary>
        void Fill(object value);
    }

    /// <summary>
    /// Represents a tensor.
    /// </summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="T">The element type.</typeparam>
    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public interface ITensor<TSelf, T> : ITensor, IReadOnlyTensor<TSelf, T>
        where TSelf : ITensor<TSelf, T>
    {
        // TODO: Determine if we can implement `IEqualityOperators<TSelf, T, bool>`.
        // It looks like C#/.NET currently hits limitations here as it believes TSelf and T could be the same type
        // Ideally we could annotate it such that they cannot be the same type and no conflicts would exist

        /// <summary>
        /// Creates a new tensor with the specified lengths.
        /// </summary>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="pinned"><see langword="true" /> to pin the underlying buffer. The default is <see langword="false" />.</param>
        /// <remarks>
        /// If <paramref name="pinned"/> is true the underlying buffer is created permanently pinned, otherwise the underlying buffer is not pinned.
        /// The underlying buffer is initialized to default values.
        /// </remarks>
        static abstract TSelf Create(scoped ReadOnlySpan<nint> lengths, bool pinned = false);

        /// <summary>
        /// Creates a new tensor with the specified lengths and strides.
        /// </summary>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides of each dimension.</param>
        /// <param name="pinned"><see langword="true" /> to pin the underlying buffer. The default is <see langword="false" />.</param>
        /// <remarks>
        /// If <paramref name="pinned"/> is true the underlying buffer is created permanently pinned, otherwise the underlying buffer is not pinned.
        /// The underlying buffer is initialized to default values.
        /// </remarks>
        static abstract TSelf Create(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false);

        /// <summary>
        /// Creates a new tensor with the specified lengths and strides.
        /// </summary>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="pinned"><see langword="true" /> to pin the underlying buffer. The default is <see langword="false" />.</param>
        /// <remarks>
        /// If <paramref name="pinned"/> is true the underlying buffer is created permanently pinned, otherwise the underlying buffer is not pinned.
        /// The underlying buffer is not initialized.
        /// </remarks>
        static abstract TSelf CreateUninitialized(scoped ReadOnlySpan<nint> lengths, bool pinned = false);

        /// <summary>
        /// Creates a new tensor with the specified lengths and strides. If <paramref name="pinned"/> is true the underlying buffer is
        /// created permanently pinned, otherwise the underlying buffer is not pinned. The underlying buffer is not initialized.
        /// </summary>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides of each dimension.</param>
        /// <param name="pinned"><see langword="true" /> to pin the underlying buffer. The default is <see langword="false" />.</param>
        /// <remarks>
        /// If <paramref name="pinned"/> is true the underlying buffer is created permanently pinned, otherwise the underlying buffer is not pinned.
        /// The underlying buffer is not initialized.
        /// </remarks>
        static abstract TSelf CreateUninitialized(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false);

        /// <summary>
        /// Gets the value at the specified indexes.
        /// </summary>
        /// <param name="indexes">The indexes to use.</param>
        new T this[params scoped ReadOnlySpan<nint> indexes] { get; set; }

        /// <summary>
        /// Gets the value at the specified indexes.
        /// </summary>
        /// <param name="indexes">The indexes to use.</param>
        new T this[params scoped ReadOnlySpan<NIndex> indexes] { get; set; }

        /// <summary>
        /// Gets the values at the specified ranges.
        /// </summary>
        /// <param name="ranges">The ranges to be used.</param>
        new TSelf this[params scoped ReadOnlySpan<NRange> ranges] { get; set; }

        /// <summary>
        /// Creates a tensor span for the entire underlying buffer.
        /// </summary>
        /// <returns>The converted <see cref="TensorSpan{T}"/>.</returns>
        TensorSpan<T> AsTensorSpan();

        /// <summary>
        /// Creates a tensor span for the specified start indexes.
        /// </summary>
        /// <param name="start">The start locations to be used.</param>
        /// <returns>The converted <see cref="TensorSpan{T}"/>.</returns>
        TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<nint> start);

        /// <summary>
        /// Creates a tensor span for the specified start indexes.
        /// </summary>
        /// <param name="startIndex">The start indexes to be used.</param>
        /// <returns>The converted <see cref="TensorSpan{T}"/>.</returns>
        TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<NIndex> startIndex);

        /// <summary>
        /// Creates a tensor span for the specified ranges.
        /// </summary>
        /// <param name="range">The ranges to be used.</param>
        /// <returns>The converted <see cref="TensorSpan{T}"/>.</returns>
        TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<NRange> range);

        /// <summary>
        /// Fills the contents of this tensor with the given value.
        /// </summary>
        void Fill(T value);

        /// <summary>
        /// Returns a reference to the 0th element of the tensor. If the tensor is empty, returns <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// This method can be used for pinning and is required to support the use of the tensor within a fixed statement.
        /// </remarks>
        new ref T GetPinnableReference();
    }
}
