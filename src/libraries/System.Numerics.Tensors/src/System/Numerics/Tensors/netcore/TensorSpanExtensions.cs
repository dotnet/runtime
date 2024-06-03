// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Numerics.Tensors
{
    public static class TensorSpan
    {
        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool SequenceEqual<T>(this TensorSpan<T> span, TensorSpan<T> other) where T : IEquatable<T>? => span.FlattenedLength == other.FlattenedLength && span.Lengths.SequenceEqual(other.Lengths) && TensorSpanHelpers.SequenceEqual(ref span.GetPinnableReference(), ref other.GetPinnableReference(), (nuint)span.FlattenedLength);

        // Doing a copy here for shape because otherwise I get a CS8347 about potentially exposing it beyond its lifetime. In this case that would never happen
        // because we were always doing a copy of the shape in the constructor anyways, but I couldn't figure out another way around it.
        /// <summary>
        /// Extension method to more easily create a TensorSpan from an array.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array</typeparam>
        /// <param name="array">The <see cref="System.Array"/> with the data</param>
        /// <param name="shape">The shape for the <see cref="TensorSpan{T}"/></param>
        /// <returns></returns>
        public static TensorSpan<T> AsTensorSpan<T>(this T[]? array, params scoped ReadOnlySpan<nint> shape) => new(array, 0, shape, default);
    }
}
