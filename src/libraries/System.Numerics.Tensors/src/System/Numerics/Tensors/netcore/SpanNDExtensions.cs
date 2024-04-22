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
    public static class SpanNDExtensions
    {
        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        //[Intrinsic] // Unrolled and vectorized for half-constant input
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool SequenceEqual<T>(this SpanND<T> span, SpanND<T> other) where T : IEquatable<T>?
        {
            nint length = span.LinearLength;
            nint otherLength = other.LinearLength;

            return length == otherLength && SpanHelpers.SequenceEqual(ref span.GetPinnableReference(), ref other.GetPinnableReference(), length);
        }

        public static SpanND<T> AsSpanND<T>(this T[]? array, ReadOnlySpan<nint> lengths)
        {
            return new SpanND<T>(array, lengths);
        }

        public static SpanND<T> AsSpanND<T>(this T[]? array, params nint[] lengths)
        {
            return new SpanND<T>(array, lengths);
        }
    }
}
