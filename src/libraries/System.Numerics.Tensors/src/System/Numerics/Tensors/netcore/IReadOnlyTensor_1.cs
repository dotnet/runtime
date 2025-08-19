// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Numerics.Tensors
{
    /// <summary>Represents a read-only tensor.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="T">The element type.</typeparam>
    public interface IReadOnlyTensor<TSelf, T> : IReadOnlyTensor
        where TSelf : IReadOnlyTensor<TSelf, T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        /// <summary>Gets an empty tensor.</summary>
        static abstract TSelf Empty { get; }

        /// <summary>Gets a reference to the specified element of the tensor.</summary>
        /// <param name="indexes">The index of the element for which to get a reference.</param>
        /// <returns>A reference to the element that exists at <paramref name="indexes" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   One of the following conditions is met:
        ///   * <paramref name="indexes" /> does not contain <see cref="IReadOnlyTensor.Rank" /> elements.
        ///   * <paramref name="indexes" /> contains an element that is negative or greater than or equal to the corresponding dimension length.
        /// </exception>
        new ref readonly T this[params scoped ReadOnlySpan<nint> indexes] { get; }

        /// <inheritdoc cref="this[ReadOnlySpan{nint}]" />
        new ref readonly T this[params scoped ReadOnlySpan<NIndex> indexes] { get; }

        /// <inheritdoc cref="Slice(ReadOnlySpan{NRange})" />
        TSelf this[params scoped ReadOnlySpan<NRange> ranges] { get; }

        /// <summary>Creates a new readonly tensor span over the tensor.</summary>
        /// <returns>The readonly tensor span representation of the tensor.</returns>
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan();

        /// <summary>Creates a new readonly tensor span over a portion of the tensor starting at a specified position to the end of the tensor.</summary>
        /// <param name="startIndexes">The initial indexes from which the tensor will be converted.</param>
        /// <returns>The readonly tensor span representation of the tensor.</returns>
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<nint> startIndexes);

        /// <inheritdoc cref="AsReadOnlyTensorSpan(ReadOnlySpan{nint})" />
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NIndex> startIndexes);

        /// <summary>Creates a new readonly tensor span over a portion of the tensor defined by the specified range.</summary>
        /// <param name="ranges">The ranges of the tensor to convert.</param>
        /// <returns>The readonly tensor span representation of the tensor.</returns>
        ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NRange> ranges);

        /// <summary>Copies the contents of the tensor into a destination tensor span.</summary>
        /// <param name="destination">The destination tensor span.</param>
        /// <exception cref="ArgumentException"><paramref name="destination" /> is shorter than the source tensor.</exception>
        /// <remarks>This method copies all of the source tensor to <paramref name="destination" /> even if they overlap.</remarks>
        void CopyTo(scoped in TensorSpan<T> destination);

        /// <summary>Flattens the contents of the tensor into a destination span.</summary>
        /// <param name="destination">The destination span.</param>
        /// <exception cref="ArgumentException"><paramref name="destination" /> is shorter than the source tensor.</exception>
        /// <remarks>This method copies all of the source tensor to <paramref name="destination" /> even if they overlap.</remarks>
        void FlattenTo(scoped Span<T> destination);

        /// <summary>Returns a span that can be used to access the flattened elements for a given dimension.</summary>
        /// <param name="dimension">The dimension for which the span should be created.</param>
        /// <returns>A span that can be used to access the flattened elements for a given dimension.</returns>
        ReadOnlyTensorDimensionSpan<T> GetDimensionSpan(int dimension);

        /// <summary>Returns a reference to an object of type <typeparamref name="T" /> that can be used for pinning.</summary>
        /// <returns>A reference to the element of the tensor at index 0, or <c>null</c> if the tensor is empty.</returns>
        /// <remarks>This method is intended to support .NET compilers and is not intended to be called by user code.</remarks>
        ref readonly T GetPinnableReference();

        /// <summary>Return a span that starts at the specified index and contains the specified number of items.</summary>
        /// <param name="startIndexes">The index at which the span should start.</param>
        /// <param name="length">The length for the span to return.</param>
        /// <returns>A span that consists of <paramref name="length" /> elements from the current tensor starting at <paramref name="startIndexes" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para><paramref name="startIndexes" /> does not contain <see cref="IReadOnlyTensor.Rank" /> elements.</para>
        ///   -or-
        ///   <para><paramref name="length" /> is negative, greater than <see cref="IReadOnlyTensor.FlattenedLength" />, or would cause the span to contain elements that should be skipped due to <see cref="IReadOnlyTensor.Strides" />.</para>
        /// </exception>
        /// <exception cref="IndexOutOfRangeException"><paramref name="startIndexes" /> is not a valid index into the tensor.</exception>
        ReadOnlySpan<T> GetSpan(scoped ReadOnlySpan<nint> startIndexes, int length);

        /// <inheritdoc cref="GetSpan(ReadOnlySpan{nint}, int)" />
        ReadOnlySpan<T> GetSpan(scoped ReadOnlySpan<NIndex> startIndexes, int length);

        /// <summary>Forms a slice out of the current tensor that begins at a specified index.</summary>
        /// <param name="startIndexes">The indexes at which to begin the slice.</param>
        /// <returns>A tensor that consists of all elements of the current tensor from <paramref name="startIndexes" /> to the end of the tensor.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndexes" /> is greater than the number of items in the tensor.</exception>
        TSelf Slice(params scoped ReadOnlySpan<nint> startIndexes);

        /// <inheritdoc cref="Slice(ReadOnlySpan{nint})" />
        TSelf Slice(params scoped ReadOnlySpan<NIndex> startIndexes);

        /// <summary>Gets a slice out of the current tensor that contains a specified range.</summary>
        /// <param name="ranges">The range of which to slice.</param>
        /// <returns>A tensor that consists of all elements of the current tensor in <paramref name="ranges" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ranges" /> is larger than the tensor.</exception>
        TSelf Slice(params scoped ReadOnlySpan<NRange> ranges);

        /// <summary>Creates a dense tensor from the elements of the current tensor.</summary>
        /// <returns>The current tensor if it is already dense; otherwise, a new tensor that contains the elements of this tensor.</returns>
        /// <remarks>
        ///   <para>A dense tensor is one where the elements are ordered sequentially in memory and where no gaps exist between the elements.</para>
        ///   <para>For a 2x2 Tensor, this would mean it has <c>FlattendLength: 4; Lengths: [2, 2]; Strides: [4, 1]</c>. The elements would be sequentially accessed via indexes: <c>[0, 0]; [0, 1]; [1, 0]; [1, 1]</c>.</para>
        /// </remarks>
        TSelf ToDenseTensor();

        /// <summary>Attempts to copy the contents of this tensor into a destination tensor span and returns a value to indicate whether or not the operation succeeded.</summary>
        /// <param name="destination">The target of the copy operation.</param>
        /// <returns><see langword="true"/> if the copy operation succeeded; otherwise, <c>false</c>.</returns>
        /// <remarks>
        ///   <para>If the source and <paramref name="destination" /> overlap, the entirety of the source is handled as if it was copied to a temporary location before it is copied to <paramref name="destination" />.</para>
        ///   <para>If the <paramref name="destination" /> length is shorter than the source, no items are copied and the method returns <c>false</c>.</para>
        /// </remarks>
        bool TryCopyTo(scoped in TensorSpan<T> destination);

        /// <summary>Attempts to flatten the contents of this tensor into a destination span and returns a value to indicate whether or not the operation succeeded.</summary>
        /// <param name="destination">The target of the copy operation.</param>
        /// <returns><see langword="true"/> if the copy operation succeeded; otherwise, <c>false</c>.</returns>
        /// <remarks>
        ///   <para>If the source and <paramref name="destination" /> overlap, the entirety of the source is handled as if it was flattened to a temporary location before it is copied to <paramref name="destination" />.</para>
        ///   <para>If the <paramref name="destination" /> length is shorter than the source, no items are copied and the method returns <c>false</c>.</para>
        /// </remarks>
        bool TryFlattenTo(scoped Span<T> destination);

        /// <summary>Tries to return a span that starts at the specified index and contains the specified number of items.</summary>
        /// <param name="startIndexes">The index at which the span should start.</param>
        /// <param name="length">The desired length of the span to retrieve.</param>
        /// <param name="span">On successful return, a span that consists of <paramref name="length" /> elements from the current tensor starting at <paramref name="startIndexes" />.</param>
        /// <returns><c>true</c> if a span was successfully retrieved; otherwise, <c>false</c> which indicates <paramref name="length" /> was invalid.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndexes" /> does not contain <see cref="IReadOnlyTensor.Rank" /> elements.</exception>
        /// <exception cref="IndexOutOfRangeException"><paramref name="startIndexes" /> is not a valid index into the tensor.</exception>
        bool TryGetSpan(scoped ReadOnlySpan<nint> startIndexes, int length, out ReadOnlySpan<T> span);

        /// <inheritdoc cref="TryGetSpan(ReadOnlySpan{nint}, int, out ReadOnlySpan{T})" />
        bool TryGetSpan(scoped ReadOnlySpan<NIndex> startIndexes, int length, out ReadOnlySpan<T> span);
    }
}
