// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace System.Numerics.Tensors
{
    /// <summary>Represents a tensor.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="T">The element type.</typeparam>
    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public interface ITensor<TSelf, T> : ITensor, IReadOnlyTensor<TSelf, T>
        where TSelf : ITensor<TSelf, T>
    {
        // TODO: Determine if we can implement `IEqualityOperators<TSelf, T, bool>`.
        // It looks like C#/.NET currently hits limitations here as it believes TSelf and T could be the same type
        // Ideally we could annotate it such that they cannot be the same type and no conflicts would exist

        /// <summary>Creates a new tensor with the specified lengths.</summary>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="pinned"><see langword="true" /> to pin the underlying buffer. The default is <see langword="false" />.</param>
        /// <remarks>
        ///   <para>If <paramref name="pinned"/> is true the underlying buffer is created permanently pinned, otherwise the underlying buffer is not pinned.</para>
        ///   <para>The underlying buffer is initialized to default values.</para>
        /// </remarks>
        static abstract TSelf Create(scoped ReadOnlySpan<nint> lengths, bool pinned = false);

        /// <summary>Creates a new tensor with the specified lengths and strides.</summary>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides of each dimension.</param>
        /// <param name="pinned"><see langword="true" /> to pin the underlying buffer. The default is <see langword="false" />.</param>
        /// <remarks>
        ///   <para>If <paramref name="pinned"/> is true the underlying buffer is created permanently pinned, otherwise the underlying buffer is not pinned.</para>
        ///   <para>The underlying buffer is initialized to default values.</para>
        /// </remarks>
        static abstract TSelf Create(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false);

        /// <summary>Creates a new tensor with the specified lengths and strides.</summary>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="pinned"><see langword="true" /> to pin the underlying buffer. The default is <see langword="false" />.</param>
        /// <remarks>
        ///   <para>If <paramref name="pinned"/> is true the underlying buffer is created permanently pinned, otherwise the underlying buffer is not pinned.</para>
        ///   <para>The underlying buffer is not initialized.</para>
        /// </remarks>
        static abstract TSelf CreateUninitialized(scoped ReadOnlySpan<nint> lengths, bool pinned = false);

        /// <summary>Creates a new tensor with the specified lengths and strides. If <paramref name="pinned"/> is true the underlying buffer is created permanently pinned, otherwise the underlying buffer is not pinned. The underlying buffer is not initialized.</summary>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides of each dimension.</param>
        /// <param name="pinned"><see langword="true" /> to pin the underlying buffer. The default is <see langword="false" />.</param>
        /// <remarks>
        ///   If <paramref name="pinned"/> is true the underlying buffer is created permanently pinned, otherwise the underlying buffer is not pinned.
        /// The underlying buffer is not initialized.
        /// </remarks>
        static abstract TSelf CreateUninitialized(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.this[ReadOnlySpan{nint}]" />
        new ref T this[params scoped ReadOnlySpan<nint> indexes] { get; }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.this[ReadOnlySpan{NIndex}]" />
        new ref T this[params scoped ReadOnlySpan<NIndex> indexes] { get; }

        /// <summary>Gets or sets a slice out of the current tensor that contains a specified range.</summary>
        /// <param name="ranges">The range of which to slice.</param>
        /// <returns>A tensor that consists of all elements of the current tensor in <paramref name="ranges" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ranges" /> is larger than the tensor.</exception>
        new TSelf this[params scoped ReadOnlySpan<NRange> ranges] { get; set; }

        /// <summary>Creates a new tensor span over the tensor.</summary>
        /// <returns>The tensor span representation of the tensor.</returns>
        TensorSpan<T> AsTensorSpan();

        /// <summary>Creates a new tensor span over a portion of the tensor starting at a specified position to the end of the tensor.</summary>
        /// <param name="startIndexes">The initial indexes from which the tensor will be converted.</param>
        /// <returns>The tensor span representation of the tensor.</returns>
        TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<nint> startIndexes);

        /// <inheritdoc cref="AsTensorSpan(ReadOnlySpan{nint})" />
        TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<NIndex> startIndexes);

        /// <summary>Creates a new tensor span over a portion of the tensor defined by the specified range.</summary>
        /// <param name="ranges">The ranges of the tensor to convert.</param>
        /// <returns>The tensor span representation of the tensor.</returns>
        TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<NRange> ranges);

        /// <inheritdoc cref="ITensor.Fill(object)" />
        void Fill(T value);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.GetDimensionSpan(int)" />
        new TensorDimensionSpan<T> GetDimensionSpan(int dimension);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.GetPinnableReference" />
        new ref T GetPinnableReference();
    }
}
