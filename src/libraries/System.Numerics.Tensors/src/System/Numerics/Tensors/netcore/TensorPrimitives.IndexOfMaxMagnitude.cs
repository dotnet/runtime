﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Searches for the index of the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The index of the element in <paramref name="x"/> with the largest magnitude (absolute value), or -1 if <paramref name="x"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// The determination of the maximum magnitude matches the IEEE 754:2019 `maximumMagnitude` function. If any value equal to NaN
        /// is present, the index of the first is returned. If two values have the same magnitude and one is positive and the other is negative,
        /// the positive value is considered to have the larger magnitude.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static int IndexOfMaxMagnitude<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            IndexOfMinMaxCore<T, IndexOfMaxMagnitudeOperator<T>>(x);

        internal readonly struct IndexOfMaxMagnitudeOperator<T> : IIndexOfOperator<T> where T : INumber<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector128<T> result, Vector128<T> current, ref Vector128<T> resultIndex, Vector128<T> currentIndex)
            {
                Vector128<T> resultMag = Vector128.Abs(result), currentMag = Vector128.Abs(current);
                Vector128<T> useResult = Vector128.GreaterThan(resultMag, currentMag);
                Vector128<T> equalMask = Vector128.Equals(resultMag, currentMag);

                if (equalMask != Vector128<T>.Zero)
                {
                    Vector128<T> lessThanIndexMask = IndexLessThan(resultIndex, currentIndex);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // bool useResult = equal && ((IsNegative(result) == IsNegative(current)) ? (resultIndex < currentIndex) : IsNegative(current));
                        Vector128<T> currentNegative = IsNegative(current);
                        Vector128<T> sameSign = Vector128.Equals(IsNegative(result).AsInt32(), currentNegative.AsInt32()).As<int, T>();
                        useResult |= equalMask & ElementWiseSelect(sameSign, lessThanIndexMask, currentNegative);
                    }
                    else
                    {
                        useResult |= equalMask & lessThanIndexMask;
                    }
                }

                result = ElementWiseSelect(useResult, result, current);
                resultIndex = ElementWiseSelect(useResult, resultIndex, currentIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector256<T> result, Vector256<T> current, ref Vector256<T> resultIndex, Vector256<T> currentIndex)
            {
                Vector256<T> resultMag = Vector256.Abs(result), currentMag = Vector256.Abs(current);
                Vector256<T> useResult = Vector256.GreaterThan(resultMag, currentMag);
                Vector256<T> equalMask = Vector256.Equals(resultMag, currentMag);

                if (equalMask != Vector256<T>.Zero)
                {
                    Vector256<T> lessThanIndexMask = IndexLessThan(resultIndex, currentIndex);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // bool useResult = equal && ((IsNegative(result) == IsNegative(current)) ? (resultIndex < currentIndex) : IsNegative(current));
                        Vector256<T> currentNegative = IsNegative(current);
                        Vector256<T> sameSign = Vector256.Equals(IsNegative(result).AsInt32(), currentNegative.AsInt32()).As<int, T>();
                        useResult |= equalMask & ElementWiseSelect(sameSign, lessThanIndexMask, currentNegative);
                    }
                    else
                    {
                        useResult |= equalMask & lessThanIndexMask;
                    }
                }

                result = ElementWiseSelect(useResult, result, current);
                resultIndex = ElementWiseSelect(useResult, resultIndex, currentIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector512<T> result, Vector512<T> current, ref Vector512<T> resultIndex, Vector512<T> currentIndex)
            {
                Vector512<T> resultMag = Vector512.Abs(result), currentMag = Vector512.Abs(current);
                Vector512<T> useResult = Vector512.GreaterThan(resultMag, currentMag);
                Vector512<T> equalMask = Vector512.Equals(resultMag, currentMag);

                if (equalMask != Vector512<T>.Zero)
                {
                    Vector512<T> lessThanIndexMask = IndexLessThan(resultIndex, currentIndex);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // bool useResult = equal && ((IsNegative(result) == IsNegative(current)) ? (resultIndex < currentIndex) : IsNegative(current));
                        Vector512<T> currentNegative = IsNegative(current);
                        Vector512<T> sameSign = Vector512.Equals(IsNegative(result).AsInt32(), currentNegative.AsInt32()).As<int, T>();
                        useResult |= equalMask & ElementWiseSelect(sameSign, lessThanIndexMask, currentNegative);
                    }
                    else
                    {
                        useResult |= equalMask & lessThanIndexMask;
                    }
                }

                result = ElementWiseSelect(useResult, result, current);
                resultIndex = ElementWiseSelect(useResult, resultIndex, currentIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(ref T result, T current, int resultIndex, int currentIndex)
            {
                T resultMag = T.Abs(result);
                T currentMag = T.Abs(current);

                if (resultMag == currentMag)
                {
                    bool resultNegative = IsNegative(result);
                    if ((resultNegative == IsNegative(current)) ? (currentIndex < resultIndex) : resultNegative)
                    {
                        result = current;
                        return currentIndex;
                    }
                }
                else if (currentMag > resultMag)
                {
                    result = current;
                    return currentIndex;
                }

                return resultIndex;
            }
        }
    }
}
