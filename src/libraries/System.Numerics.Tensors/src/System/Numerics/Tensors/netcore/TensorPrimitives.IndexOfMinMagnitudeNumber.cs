// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using static System.Numerics.Tensors.TensorOperation;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Searches for the index of the non-NaN number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the element in <paramref name="x"/> with the smallest magnitude (absolute value), or -1 if <paramref name="x"/> is empty or only contain NaN-values.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the minimum magnitude matches the IEEE 754:2019 `minimumMagnitudeNumber` function. NaN-values are ignored.
        /// If two values have the same magnitude and one is positive and the other is negative, the negative value is considered to have the smaller magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMinMagnitudeNumber<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            IndexOfMinMaxCore<T, IndexOfMinMagnitudeNumberOperator<T>>(x);

        internal readonly struct IndexOfMinMagnitudeNumberOperator<T> : IIndexOfMinMaxOperator<T> where T : INumber<T>
        {
            public static bool ShouldEarlyExitOnNan => false;
            public static T Aggregate(Vector128<T> x) => HorizontalAggregate<T, MinMagnitudeNumberOperator<T>>(x);
            public static T Aggregate(Vector256<T> x) => HorizontalAggregate<T, MinMagnitudeNumberOperator<T>>(x);
            public static T Aggregate(Vector512<T> x) => HorizontalAggregate<T, MinMagnitudeNumberOperator<T>>(x);

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

                // Don't use T.Abs since it can throw OverflowException.
                T result = T.MinMagnitude(x, y);
                if (result == x)
                {
                    if (result == y)
                    {
                        // x and y are equal in magnitude
                        return T.IsNegative(x) && T.IsPositive(y);
                    }
                    else
                    {
                        // x == result && y != result means x has lesser magnitude than y.
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Compare(Vector128<T> x, Vector128<T> y)
            {
                Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);
                if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
                {
                    Vector128<T> equalResult = Vector128.IsNegative(x) & Vector128.IsPositive(y);
                    Vector128<T> notNanResult = Vector128.LessThan(xMag, yMag) | (Vector128.Equals(xMag, yMag) & equalResult);
                    return notNanResult | Vector128.IsNaN(y); // notNanResult will be false if x is NaN
                }
                else if (typeof(T) == typeof(sbyte)
                    || typeof(T) == typeof(short)
                    || typeof(T) == typeof(int)
                    || typeof(T) == typeof(long)
                    || typeof(T) == typeof(nint))
                {
                    Vector128<T> equalResult = Vector128.IsNegative(x) & Vector128.IsPositive(y);
                    Vector128<T> nonOverflowResult = Vector128.LessThan(xMag, yMag) | (Vector128.Equals(xMag, yMag) & equalResult);
                    return Vector128.AndNot(nonOverflowResult | Vector128.IsNegative(yMag), Vector128.IsNegative(xMag));
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
                    Vector256<T> equalResult = Vector256.IsNegative(x) & Vector256.IsPositive(y);
                    Vector256<T> notNanResult = Vector256.LessThan(xMag, yMag) | (Vector256.Equals(xMag, yMag) & equalResult);
                    return notNanResult | Vector256.IsNaN(y); // notNanResult will be false if x is NaN
                }
                else if (typeof(T) == typeof(sbyte)
                    || typeof(T) == typeof(short)
                    || typeof(T) == typeof(int)
                    || typeof(T) == typeof(long)
                    || typeof(T) == typeof(nint))
                {
                    Vector256<T> equalResult = Vector256.IsNegative(x) & Vector256.IsPositive(y);
                    Vector256<T> nonOverflowResult = Vector256.LessThan(xMag, yMag) | (Vector256.Equals(xMag, yMag) & equalResult);
                    return Vector256.AndNot(nonOverflowResult | Vector256.IsNegative(yMag), Vector256.IsNegative(xMag));
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
                    Vector512<T> equalResult = Vector512.IsNegative(x) & Vector512.IsPositive(y);
                    Vector512<T> notNanResult = Vector512.LessThan(xMag, yMag) | (Vector512.Equals(xMag, yMag) & equalResult);
                    return notNanResult | Vector512.IsNaN(y); // notNanResult will be false if x is NaN
                }
                else if (typeof(T) == typeof(sbyte)
                    || typeof(T) == typeof(short)
                    || typeof(T) == typeof(int)
                    || typeof(T) == typeof(long)
                    || typeof(T) == typeof(nint))
                {
                    Vector512<T> equalResult = Vector512.IsNegative(x) & Vector512.IsPositive(y);
                    Vector512<T> nonOverflowResult = Vector512.LessThan(xMag, yMag) | (Vector512.Equals(xMag, yMag) & equalResult);
                    return Vector512.AndNot(nonOverflowResult | Vector512.IsNegative(yMag), Vector512.IsNegative(xMag));
                }
                else
                {
                    return Vector512.LessThan(xMag, yMag);
                }
            }
        }
    }
}
