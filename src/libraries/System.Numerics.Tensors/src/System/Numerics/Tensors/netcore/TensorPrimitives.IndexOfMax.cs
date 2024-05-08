// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

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
        internal readonly struct IndexOfMaxOperator<T> : IIndexOfOperator<T> where T : INumber<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector128<T> result, Vector128<T> current, ref Vector128<T> resultIndex, Vector128<T> currentIndex)
            {
                Vector128<T> useResult = Vector128.GreaterThan(result, current);
                Vector128<T> equalMask = Vector128.Equals(result, current);

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
                Vector256<T> useResult = Vector256.GreaterThan(result, current);
                Vector256<T> equalMask = Vector256.Equals(result, current);

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
                Vector512<T> useResult = Vector512.GreaterThan(result, current);
                Vector512<T> equalMask = Vector512.Equals(result, current);

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
                if (result == current)
                {
                    bool resultNegative = IsNegative(result);
                    if ((resultNegative == IsNegative(current)) ? (currentIndex < resultIndex) : resultNegative)
                    {
                        result = current;
                        return currentIndex;
                    }
                }
                else if (current > result)
                {
                    result = current;
                    return currentIndex;
                }

                return resultIndex;
            }
        }

        private static unsafe int IndexOfMinMaxCore<T, TIndexOfMinMax>(ReadOnlySpan<T> x)
    where T : INumber<T>
    where TIndexOfMinMax : struct, IIndexOfOperator<T>
        {
            if (x.IsEmpty)
            {
                return -1;
            }

            // This matches the IEEE 754:2019 `maximum`/`minimum` functions.
            // It propagates NaN inputs back to the caller and
            // otherwise returns the index of the greater of the inputs.
            // It treats +0 as greater than -0 as per the specification.

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
            {
                Debug.Assert(sizeof(T) is 1 or 2 or 4 or 8);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static Vector512<T> CreateVector512T(int i) =>
                    sizeof(T) == sizeof(long) ? Vector512.Create((long)i).As<long, T>() :
                    sizeof(T) == sizeof(int) ? Vector512.Create(i).As<int, T>() :
                    sizeof(T) == sizeof(short) ? Vector512.Create((short)i).As<short, T>() :
                    Vector512.Create((byte)i).As<byte, T>();

                ref T xRef = ref MemoryMarshal.GetReference(x);
                Vector512<T> resultIndex =
#if NET9_0_OR_GREATER
                    sizeof(T) == sizeof(long) ? Vector512<long>.Indices.As<long, T>() :
                    sizeof(T) == sizeof(int) ? Vector512<int>.Indices.As<int, T>() :
                    sizeof(T) == sizeof(short) ? Vector512<short>.Indices.As<short, T>() :
                    Vector512<byte>.Indices.As<byte, T>();
#else
                    sizeof(T) == sizeof(long) ? Vector512.Create(0L, 1, 2, 3, 4, 5, 6, 7).As<long, T>() :
                    sizeof(T) == sizeof(int) ? Vector512.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).As<int, T>() :
                    sizeof(T) == sizeof(short) ? Vector512.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31).As<short, T>() :
                    Vector512.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63).As<byte, T>();
#endif
                Vector512<T> currentIndex = resultIndex;
                Vector512<T> increment = CreateVector512T(Vector512<T>.Count);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector512<T> result = Vector512.LoadUnsafe(ref xRef);
                Vector512<T> current;

                Vector512<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    nanMask = ~Vector512.Equals(result, result);
                    if (nanMask != Vector512<T>.Zero)
                    {
                        return IndexOfFirstMatch(nanMask);
                    }
                }

                int oneVectorFromEnd = x.Length - Vector512<T>.Count;
                int i = Vector512<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector512.LoadUnsafe(ref xRef, (uint)i);
                    currentIndex += increment;

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        nanMask = ~Vector512.Equals(current, current);
                        if (nanMask != Vector512<T>.Zero)
                        {
                            return i + IndexOfFirstMatch(nanMask);
                        }
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, currentIndex);

                    i += Vector512<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<T>.Count));
                    currentIndex += CreateVector512T(x.Length - i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        nanMask = ~Vector512.Equals(current, current);
                        if (nanMask != Vector512<T>.Zero)
                        {
                            int indexInVectorOfFirstMatch = IndexOfFirstMatch(nanMask);
                            return typeof(T) == typeof(double) ?
                                (int)(long)(object)currentIndex.As<T, long>()[indexInVectorOfFirstMatch] :
                                (int)(object)currentIndex.As<T, int>()[indexInVectorOfFirstMatch];
                        }
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, currentIndex);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return IndexOfFinalAggregate<T, TIndexOfMinMax>(result, resultIndex);
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                Debug.Assert(sizeof(T) is 1 or 2 or 4 or 8);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static Vector256<T> CreateVector256T(int i) =>
                    sizeof(T) == sizeof(long) ? Vector256.Create((long)i).As<long, T>() :
                    sizeof(T) == sizeof(int) ? Vector256.Create(i).As<int, T>() :
                    sizeof(T) == sizeof(short) ? Vector256.Create((short)i).As<short, T>() :
                    Vector256.Create((byte)i).As<byte, T>();

                ref T xRef = ref MemoryMarshal.GetReference(x);
                Vector256<T> resultIndex =
#if NET9_0_OR_GREATER
                    sizeof(T) == sizeof(long) ? Vector256<long>.Indices.As<long, T>() :
                    sizeof(T) == sizeof(int) ? Vector256<int>.Indices.As<int, T>() :
                    sizeof(T) == sizeof(short) ? Vector256<short>.Indices.As<short, T>() :
                    Vector256<byte>.Indices.As<byte, T>();
#else
                    sizeof(T) == sizeof(long) ? Vector256.Create(0L, 1, 2, 3).As<long, T>() :
                    sizeof(T) == sizeof(int) ? Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7).As<int, T>() :
                    sizeof(T) == sizeof(short) ? Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).As<short, T>() :
                    Vector256.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31).As<byte, T>();
#endif
                Vector256<T> currentIndex = resultIndex;
                Vector256<T> increment = CreateVector256T(Vector256<T>.Count);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector256<T> result = Vector256.LoadUnsafe(ref xRef);
                Vector256<T> current;

                Vector256<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    nanMask = ~Vector256.Equals(result, result);
                    if (nanMask != Vector256<T>.Zero)
                    {
                        return IndexOfFirstMatch(nanMask);
                    }
                }

                int oneVectorFromEnd = x.Length - Vector256<T>.Count;
                int i = Vector256<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector256.LoadUnsafe(ref xRef, (uint)i);
                    currentIndex += increment;

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        nanMask = ~Vector256.Equals(current, current);
                        if (nanMask != Vector256<T>.Zero)
                        {
                            return i + IndexOfFirstMatch(nanMask);
                        }
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, currentIndex);

                    i += Vector256<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<T>.Count));
                    currentIndex += CreateVector256T(x.Length - i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        nanMask = ~Vector256.Equals(current, current);
                        if (nanMask != Vector256<T>.Zero)
                        {
                            int indexInVectorOfFirstMatch = IndexOfFirstMatch(nanMask);
                            return typeof(T) == typeof(double) ?
                                (int)(long)(object)currentIndex.As<T, long>()[indexInVectorOfFirstMatch] :
                                (int)(object)currentIndex.As<T, int>()[indexInVectorOfFirstMatch];
                        }
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, currentIndex);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return IndexOfFinalAggregate<T, TIndexOfMinMax>(result, resultIndex);
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                Debug.Assert(sizeof(T) is 1 or 2 or 4 or 8);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static Vector128<T> CreateVector128T(int i) =>
                    sizeof(T) == sizeof(long) ? Vector128.Create((long)i).As<long, T>() :
                    sizeof(T) == sizeof(int) ? Vector128.Create(i).As<int, T>() :
                    sizeof(T) == sizeof(short) ? Vector128.Create((short)i).As<short, T>() :
                    Vector128.Create((byte)i).As<byte, T>();

                ref T xRef = ref MemoryMarshal.GetReference(x);
                Vector128<T> resultIndex =
#if NET9_0_OR_GREATER
                    sizeof(T) == sizeof(long) ? Vector128<long>.Indices.As<long, T>() :
                    sizeof(T) == sizeof(int) ? Vector128<int>.Indices.As<int, T>() :
                    sizeof(T) == sizeof(short) ? Vector128<short>.Indices.As<short, T>() :
                    Vector128<byte>.Indices.As<byte, T>();
#else
                    sizeof(T) == sizeof(long) ? Vector128.Create(0L, 1).As<long, T>() :
                    sizeof(T) == sizeof(int) ? Vector128.Create(0, 1, 2, 3).As<int, T>() :
                    sizeof(T) == sizeof(short) ? Vector128.Create(0, 1, 2, 3, 4, 5, 6, 7).As<short, T>() :
                    Vector128.Create((byte)0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15).As<byte, T>();
#endif
                Vector128<T> currentIndex = resultIndex;
                Vector128<T> increment = CreateVector128T(Vector128<T>.Count);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector128<T> result = Vector128.LoadUnsafe(ref xRef);
                Vector128<T> current;

                Vector128<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    nanMask = ~Vector128.Equals(result, result);
                    if (nanMask != Vector128<T>.Zero)
                    {
                        return IndexOfFirstMatch(nanMask);
                    }
                }

                int oneVectorFromEnd = x.Length - Vector128<T>.Count;
                int i = Vector128<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector128.LoadUnsafe(ref xRef, (uint)i);
                    currentIndex += increment;

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        nanMask = ~Vector128.Equals(current, current);
                        if (nanMask != Vector128<T>.Zero)
                        {
                            return i + IndexOfFirstMatch(nanMask);
                        }
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, currentIndex);

                    i += Vector128<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<T>.Count));
                    currentIndex += CreateVector128T(x.Length - i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        nanMask = ~Vector128.Equals(current, current);
                        if (nanMask != Vector128<T>.Zero)
                        {
                            int indexInVectorOfFirstMatch = IndexOfFirstMatch(nanMask);
                            return typeof(T) == typeof(double) ?
                                (int)(long)(object)currentIndex.As<T, long>()[indexInVectorOfFirstMatch] :
                                (int)(object)currentIndex.As<T, int>()[indexInVectorOfFirstMatch];
                        }
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, currentIndex);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return IndexOfFinalAggregate<T, TIndexOfMinMax>(result, resultIndex);
            }

            // Scalar path used when either vectorization is not supported or the input is too small to vectorize.
            T curResult = x[0];
            int curIn = 0;
            if (T.IsNaN(curResult))
            {
                return curIn;
            }

            for (int i = 1; i < x.Length; i++)
            {
                T current = x[i];
                if (T.IsNaN(current))
                {
                    return i;
                }

                curIn = TIndexOfMinMax.Invoke(ref curResult, current, curIn, i);
            }

            return curIn;
        }

        private static int IndexOfFirstMatch<T>(Vector128<T> mask) =>
            BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());

        private static int IndexOfFirstMatch<T>(Vector256<T> mask) =>
            BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());

        private static int IndexOfFirstMatch<T>(Vector512<T> mask) =>
            BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector256<T> IndexLessThan<T>(Vector256<T> indices1, Vector256<T> indices2) =>
            sizeof(T) == sizeof(long) ? Vector256.LessThan(indices1.AsInt64(), indices2.AsInt64()).As<long, T>() :
            sizeof(T) == sizeof(int) ? Vector256.LessThan(indices1.AsInt32(), indices2.AsInt32()).As<int, T>() :
            sizeof(T) == sizeof(short) ? Vector256.LessThan(indices1.AsInt16(), indices2.AsInt16()).As<short, T>() :
            Vector256.LessThan(indices1.AsByte(), indices2.AsByte()).As<byte, T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector512<T> IndexLessThan<T>(Vector512<T> indices1, Vector512<T> indices2) =>
            sizeof(T) == sizeof(long) ? Vector512.LessThan(indices1.AsInt64(), indices2.AsInt64()).As<long, T>() :
            sizeof(T) == sizeof(int) ? Vector512.LessThan(indices1.AsInt32(), indices2.AsInt32()).As<int, T>() :
            sizeof(T) == sizeof(short) ? Vector512.LessThan(indices1.AsInt16(), indices2.AsInt16()).As<short, T>() :
            Vector512.LessThan(indices1.AsByte(), indices2.AsByte()).As<byte, T>();

        /// <summary>Gets whether the specified <see cref="float"/> is negative.</summary>
        private static bool IsNegative<T>(T f) where T : INumberBase<T> => T.IsNegative(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector128<T> ElementWiseSelect<T>(Vector128<T> mask, Vector128<T> left, Vector128<T> right)
        {
            if (Sse41.IsSupported)
            {
                if (typeof(T) == typeof(float)) return Sse41.BlendVariable(left.AsSingle(), right.AsSingle(), (~mask).AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Sse41.BlendVariable(left.AsDouble(), right.AsDouble(), (~mask).AsDouble()).As<double, T>();

                if (sizeof(T) == 1) return Sse41.BlendVariable(left.AsByte(), right.AsByte(), (~mask).AsByte()).As<byte, T>();
                if (sizeof(T) == 2) return Sse41.BlendVariable(left.AsUInt16(), right.AsUInt16(), (~mask).AsUInt16()).As<ushort, T>();
                if (sizeof(T) == 4) return Sse41.BlendVariable(left.AsUInt32(), right.AsUInt32(), (~mask).AsUInt32()).As<uint, T>();
                if (sizeof(T) == 8) return Sse41.BlendVariable(left.AsUInt64(), right.AsUInt64(), (~mask).AsUInt64()).As<ulong, T>();
            }

            return Vector128.ConditionalSelect(mask, left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector256<T> ElementWiseSelect<T>(Vector256<T> mask, Vector256<T> left, Vector256<T> right)
        {
            if (Avx2.IsSupported)
            {
                if (typeof(T) == typeof(float)) return Avx2.BlendVariable(left.AsSingle(), right.AsSingle(), (~mask).AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Avx2.BlendVariable(left.AsDouble(), right.AsDouble(), (~mask).AsDouble()).As<double, T>();

                if (sizeof(T) == 1) return Avx2.BlendVariable(left.AsByte(), right.AsByte(), (~mask).AsByte()).As<byte, T>();
                if (sizeof(T) == 2) return Avx2.BlendVariable(left.AsUInt16(), right.AsUInt16(), (~mask).AsUInt16()).As<ushort, T>();
                if (sizeof(T) == 4) return Avx2.BlendVariable(left.AsUInt32(), right.AsUInt32(), (~mask).AsUInt32()).As<uint, T>();
                if (sizeof(T) == 8) return Avx2.BlendVariable(left.AsUInt64(), right.AsUInt64(), (~mask).AsUInt64()).As<ulong, T>();
            }

            return Vector256.ConditionalSelect(mask, left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector512<T> ElementWiseSelect<T>(Vector512<T> mask, Vector512<T> left, Vector512<T> right)
        {
            if (Avx512F.IsSupported)
            {
                if (typeof(T) == typeof(float)) return Avx512F.BlendVariable(left.AsSingle(), right.AsSingle(), (~mask).AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Avx512F.BlendVariable(left.AsDouble(), right.AsDouble(), (~mask).AsDouble()).As<double, T>();

                if (sizeof(T) == 4) return Avx512F.BlendVariable(left.AsUInt32(), right.AsUInt32(), (~mask).AsUInt32()).As<uint, T>();
                if (sizeof(T) == 8) return Avx512F.BlendVariable(left.AsUInt64(), right.AsUInt64(), (~mask).AsUInt64()).As<ulong, T>();
            }

            return Vector512.ConditionalSelect(mask, left, right);
        }
    }
}
