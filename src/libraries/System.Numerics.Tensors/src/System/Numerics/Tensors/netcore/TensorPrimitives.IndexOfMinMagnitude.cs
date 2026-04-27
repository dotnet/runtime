// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Searches for the index of the number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the element in <paramref name="x"/> with the smallest magnitude (absolute value), or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the minimum magnitude matches the IEEE 754:2019 `minimumMagnitude` function. If any value equal to NaN
        /// is present, the index of the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        /// the negative value is considered to have the smaller magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMinMagnitude<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            IndexOfMinMaxCore<T, IndexOfMinMagnitudeOperator<T>>(x);

        internal readonly struct IndexOfMinMagnitudeOperator<T> : IIndexOfMinMaxOperator<T> where T : INumber<T>
        {
            public static T Aggregate(Vector128<T> x) => HorizontalAggregate<T, MinMagnitudeOperator<T>>(x);
            public static T Aggregate(Vector256<T> x) => HorizontalAggregate<T, MinMagnitudeOperator<T>>(x);
            public static T Aggregate(Vector512<T> x) => HorizontalAggregate<T, MinMagnitudeOperator<T>>(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsQuickReturn(T value) => T.IsNaN(value);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> IsQuickReturn(Vector128<T> value) => IsNaN(value);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> IsQuickReturn(Vector256<T> value) => IsNaN(value);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> IsQuickReturn(Vector512<T> value) => IsNaN(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Compare(T x, T y)
            {
                T xMag = T.Abs(x), yMag = T.Abs(y);
                if (xMag == yMag)
                {
                    return T.IsNegative(x) && T.IsPositive(y);
                }
                else
                {
                    return xMag < yMag;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Compare(Vector128<T> x, Vector128<T> y)
            {
                Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector128<T> equalResult = IsNegative(x) & IsPositive(y);
                    return Vector128.LessThan(xMag, yMag) | (Vector128.Equals(xMag, yMag) & equalResult);
                }
                else
                {
                    return Vector128.LessThan(xMag, yMag);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Compare(Vector256<T> x, Vector256<T> y)
            {
                Vector256<T> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector256<T> equalResult = IsNegative(x) & IsPositive(y);
                    return Vector256.LessThan(xMag, yMag) | (Vector256.Equals(xMag, yMag) & equalResult);
                }
                else
                {
                    return Vector256.LessThan(xMag, yMag);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Compare(Vector512<T> x, Vector512<T> y)
            {
                Vector512<T> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector512<T> equalResult = IsNegative(x) & IsPositive(y);
                    return Vector512.LessThan(xMag, yMag) | (Vector512.Equals(xMag, yMag) & equalResult);
                }
                else
                {
                    return Vector512.LessThan(xMag, yMag);
                }
            }
        }
    }
}
