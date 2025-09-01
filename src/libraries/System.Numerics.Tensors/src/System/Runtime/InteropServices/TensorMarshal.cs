// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics.Tensors;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>Provides methods to interoperate with <see cref="Tensor{T}" />, <see cref="TensorSpan{T}" />, and <see cref="ReadOnlyTensorSpan{T}" />.</summary>
    public static class TensorMarshal
    {
        /// <summary>Creates a new tensor span over a portion of a regular managed object.</summary>
        /// <typeparam name="T">The type of the data items.</typeparam>
        /// <param name="data">A reference to data.</param>
        /// <param name="dataLength">The number of <typeparamref name="T" /> elements that <paramref name="data" /> contains.</param>
        /// <param name="lengths">The lengths of the dimensions. If an empty span is provided, the created tensor will have a single dimension that is the same length as <paramref name="dataLength" />.</param>
        /// <param name="strides">The strides of each dimension. If an empty span is provided, then strides will be automatically calculated from <paramref name="lengths" />.</param>
        /// <param name="pinned"><c>true</c> if <paramref name="data" /> is permanently pinned; otherwise, <c>false</c>.</param>
        /// <returns>The created tensor span.</returns>
        /// <remarks>This method should be used with caution. It is dangerous because the inputs may not be fully checked. Even though <paramref name="data" /> is marked as <c>scoped</c>, it will be stored into the returned tensor span, and the lifetime of the returned tensor span will not be validated for safety, even by span-aware languages.</remarks>
        public static TensorSpan<T> CreateTensorSpan<T>(scoped ref T data, nint dataLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned)
        {
            return new TensorSpan<T>(ref Unsafe.AsRef(ref data), dataLength, lengths, strides, pinned);
        }

        /// <summary>Creates a new readonly tensor span over a portion of a regular managed object.</summary>
        /// <typeparam name="T">The type of the data items.</typeparam>
        /// <param name="data">A readonly reference to data.</param>
        /// <param name="dataLength">The number of <typeparamref name="T" /> elements that <paramref name="data" /> contains.</param>
        /// <param name="lengths">The lengths of the dimensions. If an empty span is provided, the created tensor will have a single dimension that is the same length as <paramref name="dataLength" />.</param>
        /// <param name="strides">The strides of each dimension. If an empty span is provided, then strides will be automatically calculated from <paramref name="lengths" />.</param>
        /// <param name="pinned"><c>true</c> if <paramref name="data" /> is permanently pinned; otherwise, <c>false</c>.</param>
        /// <returns>The created readonly tensor span.</returns>
        /// <remarks>This method should be used with caution. It is dangerous because the inputs may not be fully checked. Even though <paramref name="data" /> is marked as <c>scoped</c>, it will be stored into the returned tensor span, and the lifetime of the returned tensor span will not be validated for safety, even by span-aware languages.</remarks>
        public static ReadOnlyTensorSpan<T> CreateReadOnlyTensorSpan<T>(scoped ref readonly T data, nint dataLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned)
        {
            return new ReadOnlyTensorSpan<T>(in Unsafe.AsRef(in data), dataLength, lengths, strides, pinned);
        }

        /// <summary>Returns a reference to the element of the tensor span at index 0.</summary>
        /// <typeparam name="T">The type of items in the tensor span.</typeparam>
        /// <param name="tensorSpan">The tensor span from which the reference is retrieved.</param>
        /// <returns>A reference to the element at index 0.</returns>
        /// <remarks>If the tensor span is empty, this method returns a reference to the location where the element at index 0 would have been stored. Such a reference may or may not be <c>null</c>. The returned reference can be used for pinning, but it must never be dereferenced.</remarks>
        public static ref T GetReference<T>(in TensorSpan<T> tensorSpan)
        {
            return ref tensorSpan._reference;
        }

        /// <summary>Returns a reference to the element of the readonly tensor span at index 0.</summary>
        /// <typeparam name="T">The type of items in the readonly tensor span.</typeparam>
        /// <param name="tensorSpan">The readonly tensor span from which the reference is retrieved.</param>
        /// <returns>A readonly reference to the element at index 0.</returns>
        /// <remarks>If the readonly tensor span is empty, this method returns a reference to the location where the element at index 0 would have been stored. Such a reference may or may not be <c>null</c>. The returned reference can be used for pinning, but it must never be dereferenced.</remarks>
        public static ref readonly T GetReference<T>(in ReadOnlyTensorSpan<T> tensorSpan)
        {
            return ref tensorSpan._reference;
        }
    }
}
