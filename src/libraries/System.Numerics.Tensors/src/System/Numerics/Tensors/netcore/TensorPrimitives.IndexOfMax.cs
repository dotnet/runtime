// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Searches for the index of the largest number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the maximum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If any value equal to NaN
        /// is present, the index of the first is returned. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMax<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            IndexOfMinMaxCore<T, IndexOfMaxOperator<T>>(x);

        /// <summary>Returns the index of MathF.Max(x, y)</summary>
        internal readonly struct IndexOfMaxOperator<T> : IIndexOfMinMaxOperator<T> where T : INumber<T>
        {
            public static T Aggregate(Vector128<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
            public static T Aggregate(Vector256<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
            public static T Aggregate(Vector512<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Reduce(T x, T y) => MaxOperator<T>.Invoke(x, y);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Reduce(Vector128<T> x, Vector128<T> y) => MaxOperator<T>.Invoke(x, y);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Reduce(Vector256<T> x, Vector256<T> y) => MaxOperator<T>.Invoke(x, y);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Reduce(Vector512<T> x, Vector512<T> y) => MaxOperator<T>.Invoke(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Compare(T x, T y)
            {
                if (x == y)
                {
                    return T.IsPositive(x) && T.IsNegative(y);
                }
                else
                {
                    return x > y;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Compare(Vector128<T> x, Vector128<T> y)
            {
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector128<T> equalResult = Vector128.IsPositive(x) & Vector128.IsNegative(y);
                    return Vector128.GreaterThan(x, y) | (Vector128.Equals(x, y) & equalResult);
                }
                else
                {
                    return Vector128.GreaterThan(x, y);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Compare(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector256<T> equalResult = Vector256.IsPositive(x) & Vector256.IsNegative(y);
                    return Vector256.GreaterThan(x, y) | (Vector256.Equals(x, y) & equalResult);
                }
                else
                {
                    return Vector256.GreaterThan(x, y);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Compare(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector512<T> equalResult = Vector512.IsPositive(x) & Vector512.IsNegative(y);
                    return Vector512.GreaterThan(x, y) | (Vector512.Equals(x, y) & equalResult);
                }
                else
                {
                    return Vector512.GreaterThan(x, y);
                }
            }
        }

        private static int IndexOfFirstMatch<T>(Vector128<T> mask) =>
            BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());

        private static int IndexOfFirstMatch<T>(Vector256<T> mask) =>
            BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());

        private static int IndexOfFirstMatch<T>(Vector512<T> mask) =>
            BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());

    }
}
