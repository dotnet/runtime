// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Searches for the index of the smallest number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the minimum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the minimum element matches the IEEE 754:2019 `minimum` function. If any value equal to NaN
        /// is present, the index of the first is returned. Negative 0 is considered smaller than positive 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMin<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            IndexOfMinMaxCore<T, IndexOfMinOperator<T>>(x);

        /// <summary>Returns the index of MathF.Min(x, y)</summary>
        internal readonly struct IndexOfMinOperator<T> : IIndexOfMinMaxOperator<T> where T : INumber<T>
        {
            public static T Aggregate(Vector128<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
            public static T Aggregate(Vector256<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
            public static T Aggregate(Vector512<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Compare(T x, T y)
            {
                if (x == y)
                {
                    return T.IsNegative(x) && T.IsPositive(y);
                }
                else
                {
                    return x < y;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Compare(Vector128<T> x, Vector128<T> y)
            {
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector128<T> equalResult = IsNegative(x) & IsPositive(y);
                    return Vector128.LessThan(x, y) | (Vector128.Equals(x, y) & equalResult);
                }
                else
                {
                    return Vector128.LessThan(x, y);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Compare(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector256<T> equalResult = IsNegative(x) & IsPositive(y);
                    return Vector256.LessThan(x, y) | (Vector256.Equals(x, y) & equalResult);
                }
                else
                {
                    return Vector256.LessThan(x, y);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Compare(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector512<T> equalResult = IsNegative(x) & IsPositive(y);
                    return Vector512.LessThan(x, y) | (Vector512.Equals(x, y) & equalResult);
                }
                else
                {
                    return Vector512.LessThan(x, y);
                }
            }
        }
    }
}
