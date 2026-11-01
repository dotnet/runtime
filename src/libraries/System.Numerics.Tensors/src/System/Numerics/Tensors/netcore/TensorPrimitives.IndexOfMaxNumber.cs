// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Searches for the index of the largest non-NaN number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the maximum element in <paramref name="x"/>, or -1 if <paramref name="x"/> is empty or only contain NaN-values.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximumNumber` function. NaN-values are ignored.
        /// Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMaxNumber<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            IndexOfMinMaxCore<T, IndexOfMaxNumberOperator<T>>(x);

        internal readonly struct IndexOfMaxNumberOperator<T> : IIndexOfMinMaxOperator<T> where T : INumber<T>
        {
            public static bool ShouldEarlyExitOnNan => false;
            public static T Aggregate(Vector128<T> x) => HorizontalAggregate<T, MaxNumberOperator<T>>(x);
            public static T Aggregate(Vector256<T> x) => HorizontalAggregate<T, MaxNumberOperator<T>>(x);
            public static T Aggregate(Vector512<T> x) => HorizontalAggregate<T, MaxNumberOperator<T>>(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Compare(T x, T y)
            {
                if (T.IsNaN(x))
                {
                    return false;
                }
                else if (T.IsNaN(y))
                {
                    return true;
                }

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
                    Vector128<T> notNanResult = Vector128.GreaterThan(x, y) | (Vector128.Equals(x, y) & equalResult);
                    return notNanResult | Vector128.IsNaN(y); // notNanResult will be false if x is NaN
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
                    Vector256<T> notNanResult = Vector256.GreaterThan(x, y) | (Vector256.Equals(x, y) & equalResult);
                    return notNanResult | Vector256.IsNaN(y); // notNanResult will be false if x is NaN
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
                    Vector512<T> notNanResult = Vector512.GreaterThan(x, y) | (Vector512.Equals(x, y) & equalResult);
                    return notNanResult | Vector512.IsNaN(y); // notNanResult will be false if x is NaN
                }
                else
                {
                    return Vector512.GreaterThan(x, y);
                }
            }
        }
    }
}
