// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Numerics.Tensors
{
    /// <summary>Represents a tensor.</summary>
    public interface ITensor : IReadOnlyTensor
    {
        /// <summary>Gets or sets the specified element of the tensor.</summary>
        /// <param name="indexes">The index of the element for which to get.</param>
        /// <returns>The element that exists at <paramref name="indexes" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   One of the following conditions is met:
        ///   * <paramref name="indexes" /> does not contain <see cref="IReadOnlyTensor.Rank" /> elements.
        ///   * <paramref name="indexes" /> contains an element that is negative or greater than or equal to the corresponding dimension length.
        /// </exception>
        new object? this[params scoped ReadOnlySpan<nint> indexes] { get; set; }

        /// <inheritdoc cref="this[ReadOnlySpan{nint}]" />
        new object? this[params scoped ReadOnlySpan<NIndex> indexes] { get; set; }

        /// <summary>Gets a value that indicates whether the tensor is read-only.</summary>
        bool IsReadOnly { get; }

        /// <summary>Clears the contents of the tensor span.</summary>
        /// <remarks>This method sets the items in the tensor span to their default values. It does not remove items from the tensor span.</remarks>
        void Clear();

        /// <summary>Fills the elements of this tensor with a specified value.</summary>
        /// <param name="value">The value to assign to each element of the tensor.</param>
        void Fill(object value);
    }
}
