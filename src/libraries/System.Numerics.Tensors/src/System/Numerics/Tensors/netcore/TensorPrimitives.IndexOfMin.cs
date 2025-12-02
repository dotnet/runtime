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
        internal readonly struct IndexOfMinOperator<T> : IIndexOfOperator<T> where T : INumber<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector128<T> result, Vector128<T> current, ref Vector128<T> resultIndex, Vector128<T> currentIndex)
            {
                Vector128<T> useResult = Vector128.LessThan(result, current);
                Vector128<T> equalMask = Vector128.Equals(result, current);

                if (equalMask != Vector128<T>.Zero)
                {
                    Vector128<T> lessThanIndexMask = IndexLessThan(resultIndex, currentIndex);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // bool useResult = equal && ((IsNegative(result) == IsNegative(current)) ? (resultIndex < currentIndex) : IsNegative(result));
                        Vector128<T> resultNegative = IsNegative(result);
                        Vector128<T> sameSign = Vector128.Equals(resultNegative.AsInt32(), IsNegative(current).AsInt32()).As<int, T>();
                        useResult |= equalMask & ElementWiseSelect(sameSign, lessThanIndexMask, resultNegative);
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
                Vector256<T> useResult = Vector256.LessThan(result, current);
                Vector256<T> equalMask = Vector256.Equals(result, current);

                if (equalMask != Vector256<T>.Zero)
                {
                    Vector256<T> lessThanIndexMask = IndexLessThan(resultIndex, currentIndex);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // bool useResult = equal && ((IsNegative(result) == IsNegative(current)) ? (resultIndex < currentIndex) : IsNegative(result));
                        Vector256<T> resultNegative = IsNegative(result);
                        Vector256<T> sameSign = Vector256.Equals(resultNegative.AsInt32(), IsNegative(current).AsInt32()).As<int, T>();
                        useResult |= equalMask & ElementWiseSelect(sameSign, lessThanIndexMask, resultNegative);
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
                Vector512<T> useResult = Vector512.LessThan(result, current);
                Vector512<T> equalMask = Vector512.Equals(result, current);

                if (equalMask != Vector512<T>.Zero)
                {
                    Vector512<T> lessThanIndexMask = IndexLessThan(resultIndex, currentIndex);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // bool useResult = equal && ((IsNegative(result) == IsNegative(current)) ? (resultIndex < currentIndex) : IsNegative(result));
                        Vector512<T> resultNegative = IsNegative(result);
                        Vector512<T> sameSign = Vector512.Equals(resultNegative.AsInt32(), IsNegative(current).AsInt32()).As<int, T>();
                        useResult |= equalMask & ElementWiseSelect(sameSign, lessThanIndexMask, resultNegative);
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
                if (result == current)
                {
                    bool currentNegative = IsNegative(current);
                    if ((IsNegative(result) == currentNegative) ? (currentIndex < resultIndex) : currentNegative)
                    {
                        result = current;
                        return currentIndex;
                    }
                }
                else if (current < result)
                {
                    result = current;
                    return currentIndex;
                }

                return resultIndex;
            }
        }
    }
}
