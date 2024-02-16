// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        // Defines vectorizable operators for types implementing INumberBase<T> or related interfaces.

        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<T> IsNegative<T>(Vector128<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return Vector128.LessThan(vector.AsInt32(), Vector128<int>.Zero).As<int, T>();
            }

            if (typeof(T) == typeof(double))
            {
                return Vector128.LessThan(vector.AsInt64(), Vector128<long>.Zero).As<long, T>();
            }

            return Vector128.LessThan(vector, Vector128<T>.Zero);
        }

        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<T> IsNegative<T>(Vector256<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return Vector256.LessThan(vector.AsInt32(), Vector256<int>.Zero).As<int, T>();
            }

            if (typeof(T) == typeof(double))
            {
                return Vector256.LessThan(vector.AsInt64(), Vector256<long>.Zero).As<long, T>();
            }

            return Vector256.LessThan(vector, Vector256<T>.Zero);
        }

        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<T> IsNegative<T>(Vector512<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return Vector512.LessThan(vector.AsInt32(), Vector512<int>.Zero).As<int, T>();
            }

            if (typeof(T) == typeof(double))
            {
                return Vector512.LessThan(vector.AsInt64(), Vector512<long>.Zero).As<long, T>();
            }

            return Vector512.LessThan(vector, Vector512<T>.Zero);
        }

        /// <summary>Creates a span of <typeparamref name="TTo"/> from a <typeparamref name="TTo"/> when they're the same type.</summary>
        private static unsafe ReadOnlySpan<TTo> Rename<TFrom, TTo>(ReadOnlySpan<TFrom> span)
        {
            Debug.Assert(sizeof(TFrom) == sizeof(TTo));
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(span)), span.Length);
        }

        /// <summary>Gets whether <typeparamref name="T"/> is <see cref="uint"/> or <see cref="nuint"/> if in a 32-bit process.</summary>
        private static bool IsUInt32Like<T>() => typeof(T) == typeof(uint) || (IntPtr.Size == 4 && typeof(T) == typeof(nuint));

        /// <summary>Gets whether <typeparamref name="T"/> is <see cref="int"/> or <see cref="nint"/> if in a 32-bit process.</summary>
        private static bool IsInt32Like<T>() => typeof(T) == typeof(int) || (IntPtr.Size == 4 && typeof(T) == typeof(nint));

        /// <summary>Gets whether <typeparamref name="T"/> is <see cref="ulong"/> or <see cref="nuint"/> if in a 64-bit process.</summary>
        private static bool IsUInt64Like<T>() => typeof(T) == typeof(ulong) || (IntPtr.Size == 8 && typeof(T) == typeof(nuint));

        /// <summary>Gets whether <typeparamref name="T"/> is <see cref="long"/> or <see cref="nint"/> if in a 64-bit process.</summary>
        private static bool IsInt64Like<T>() => typeof(T) == typeof(long) || (IntPtr.Size == 8 && typeof(T) == typeof(nint));

        /// <remarks>
        /// This is the same as <see cref="Aggregate{T, TTransformOperator, TAggregationOperator}(ReadOnlySpan{T})"/>
        /// with an identity transform, except it early exits on NaN.
        /// </remarks>
        private static T MinMaxCore<T, TMinMaxOperator>(ReadOnlySpan<T> x)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            // This matches the IEEE 754:2019 `maximum`/`minimum` functions.
            // It propagates NaN inputs back to the caller and
            // otherwise returns the greater of the inputs.
            // It treats +0 as greater than -0 as per the specification.

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector512<T> result = Vector512.LoadUnsafe(ref xRef, 0);
                Vector512<T> current;

                Vector512<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    // Check for NaNs
                    nanMask = ~Vector512.Equals(result, result);
                    if (nanMask != Vector512<T>.Zero)
                    {
                        return result.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                int oneVectorFromEnd = x.Length - Vector512<T>.Count;
                int i = Vector512<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector512.LoadUnsafe(ref xRef, (uint)i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector512.Equals(current, current);
                        if (nanMask != Vector512<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector512<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<T>.Count));

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector512.Equals(current, current);
                        if (nanMask != Vector512<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector256<T> result = Vector256.LoadUnsafe(ref xRef, 0);
                Vector256<T> current;

                Vector256<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    // Check for NaNs
                    nanMask = ~Vector256.Equals(result, result);
                    if (nanMask != Vector256<T>.Zero)
                    {
                        return result.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                int oneVectorFromEnd = x.Length - Vector256<T>.Count;
                int i = Vector256<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector256.LoadUnsafe(ref xRef, (uint)i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector256.Equals(current, current);
                        if (nanMask != Vector256<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector256<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<T>.Count));


                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector256.Equals(current, current);
                        if (nanMask != Vector256<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector128<T> result = Vector128.LoadUnsafe(ref xRef, 0);
                Vector128<T> current;

                Vector128<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    // Check for NaNs
                    nanMask = ~Vector128.Equals(result, result);
                    if (nanMask != Vector128<T>.Zero)
                    {
                        return result.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                int oneVectorFromEnd = x.Length - Vector128<T>.Count;
                int i = Vector128<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector128.LoadUnsafe(ref xRef, (uint)i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector128.Equals(current, current);
                        if (nanMask != Vector128<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector128<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<T>.Count));

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector128.Equals(current, current);
                        if (nanMask != Vector128<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }

            // Scalar path used when either vectorization is not supported or the input is too small to vectorize.
            T curResult = x[0];
            if (T.IsNaN(curResult))
            {
                return curResult;
            }

            for (int i = 1; i < x.Length; i++)
            {
                T current = x[i];
                if (T.IsNaN(current))
                {
                    return current;
                }

                curResult = TMinMaxOperator.Invoke(curResult, current);
            }

            return curResult;
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

        /// <summary>T.Abs(x)</summary>
        internal readonly struct AbsoluteOperator<T> : IUnaryOperator<T, T> where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x) => T.Abs(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(sbyte) ||
                    typeof(T) == typeof(short) ||
                    typeof(T) == typeof(int) ||
                    typeof(T) == typeof(long) ||
                    typeof(T) == typeof(nint))
                {
                    // Handle signed integers specially, in order to throw if any attempt is made to
                    // take the absolute value of the minimum value of the type, which doesn't have
                    // a positive absolute value representation.
                    Vector128<T> abs = Vector128.ConditionalSelect(Vector128.LessThan(x, Vector128<T>.Zero), -x, x);
                    if (Vector128.LessThan(abs, Vector128<T>.Zero) != Vector128<T>.Zero)
                    {
                        ThrowNegateTwosCompOverflow();
                    }
                }

                return Vector128.Abs(x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(sbyte) ||
                    typeof(T) == typeof(short) ||
                    typeof(T) == typeof(int) ||
                    typeof(T) == typeof(long) ||
                    typeof(T) == typeof(nint))
                {
                    // Handle signed integers specially, in order to throw if any attempt is made to
                    // take the absolute value of the minimum value of the type, which doesn't have
                    // a positive absolute value representation.
                    Vector256<T> abs = Vector256.ConditionalSelect(Vector256.LessThan(x, Vector256<T>.Zero), -x, x);
                    if (Vector256.LessThan(abs, Vector256<T>.Zero) != Vector256<T>.Zero)
                    {
                        ThrowNegateTwosCompOverflow();
                    }
                }

                return Vector256.Abs(x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(sbyte) ||
                    typeof(T) == typeof(short) ||
                    typeof(T) == typeof(int) ||
                    typeof(T) == typeof(long) ||
                    typeof(T) == typeof(nint))
                {
                    // Handle signed integers specially, in order to throw if any attempt is made to
                    // take the absolute value of the minimum value of the type, which doesn't have
                    // a positive absolute value representation.
                    Vector512<T> abs = Vector512.ConditionalSelect(Vector512.LessThan(x, Vector512<T>.Zero), -x, x);
                    if (Vector512.LessThan(abs, Vector512<T>.Zero) != Vector512<T>.Zero)
                    {
                        ThrowNegateTwosCompOverflow();
                    }
                }

                return Vector512.Abs(x);
            }
        }

        /// <summary>T.Max(x, y) (but NaNs may not be propagated)</summary>
        internal readonly struct MaxOperator<T> : IAggregationOperator<T> where T : INumber<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y)
            {
                if (typeof(T) == typeof(Half) || typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return x == y ?
                        (IsNegative(x) ? y : x) :
                        (y > x ? y : x);
                }

                return T.Max(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(byte)) return AdvSimd.Max(x.AsByte(), y.AsByte()).As<byte, T>();
                    if (typeof(T) == typeof(sbyte)) return AdvSimd.Max(x.AsSByte(), y.AsSByte()).As<sbyte, T>();
                    if (typeof(T) == typeof(short)) return AdvSimd.Max(x.AsInt16(), y.AsInt16()).As<short, T>();
                    if (typeof(T) == typeof(ushort)) return AdvSimd.Max(x.AsUInt16(), y.AsUInt16()).As<ushort, T>();
                    if (typeof(T) == typeof(int)) return AdvSimd.Max(x.AsInt32(), y.AsInt32()).As<int, T>();
                    if (typeof(T) == typeof(uint)) return AdvSimd.Max(x.AsUInt32(), y.AsUInt32()).As<uint, T>();
                    if (typeof(T) == typeof(float)) return AdvSimd.Max(x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.Max(x.AsDouble(), y.AsDouble()).As<double, T>();
                }

                if (typeof(T) == typeof(float))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, y),
                            Vector128.ConditionalSelect(IsNegative(x.AsSingle()).As<float, T>(), y, x),
                            Vector128.Max(x, y));
                }

                if (typeof(T) == typeof(double))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, y),
                            Vector128.ConditionalSelect(IsNegative(x.AsDouble()).As<double, T>(), y, x),
                            Vector128.Max(x, y));
                }

                return Vector128.Max(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, y),
                            Vector256.ConditionalSelect(IsNegative(x.AsSingle()).As<float, T>(), y, x),
                            Vector256.Max(x, y));
                }

                if (typeof(T) == typeof(double))
                {
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, y),
                            Vector256.ConditionalSelect(IsNegative(x.AsDouble()).As<double, T>(), y, x),
                            Vector256.Max(x, y));
                }

                return Vector256.Max(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, y),
                            Vector512.ConditionalSelect(IsNegative(x.AsSingle()).As<float, T>(), y, x),
                            Vector512.Max(x, y));
                }

                if (typeof(T) == typeof(double))
                {
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, y),
                            Vector512.ConditionalSelect(IsNegative(x.AsDouble()).As<double, T>(), y, x),
                            Vector512.Max(x, y));
                }

                return Vector512.Max(x, y);
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
        }

        /// <summary>Max(x, y)</summary>
        internal readonly struct MaxPropagateNaNOperator<T> : IBinaryOperator<T>
             where T : INumber<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.Max(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(byte)) return AdvSimd.Max(x.AsByte(), y.AsByte()).As<byte, T>();
                    if (typeof(T) == typeof(sbyte)) return AdvSimd.Max(x.AsSByte(), y.AsSByte()).As<sbyte, T>();
                    if (typeof(T) == typeof(ushort)) return AdvSimd.Max(x.AsUInt16(), y.AsUInt16()).As<ushort, T>();
                    if (typeof(T) == typeof(short)) return AdvSimd.Max(x.AsInt16(), y.AsInt16()).As<short, T>();
                    if (typeof(T) == typeof(uint)) return AdvSimd.Max(x.AsUInt32(), y.AsUInt32()).As<uint, T>();
                    if (typeof(T) == typeof(int)) return AdvSimd.Max(x.AsInt32(), y.AsInt32()).As<int, T>();
                    if (typeof(T) == typeof(float)) return AdvSimd.Max(x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.Max(x.AsDouble(), y.AsDouble()).As<double, T>();
                }

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, x),
                            Vector128.ConditionalSelect(Vector128.Equals(y, y),
                                Vector128.ConditionalSelect(Vector128.Equals(x, y),
                                    Vector128.ConditionalSelect(IsNegative(x), y, x),
                                    Vector128.Max(x, y)),
                                y),
                            x);
                }

                return Vector128.Max(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, x),
                            Vector256.ConditionalSelect(Vector256.Equals(y, y),
                                Vector256.ConditionalSelect(Vector256.Equals(x, y),
                                    Vector256.ConditionalSelect(IsNegative(x), y, x),
                                    Vector256.Max(x, y)),
                                y),
                            x);
                }

                return Vector256.Max(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, x),
                            Vector512.ConditionalSelect(Vector512.Equals(y, y),
                                Vector512.ConditionalSelect(Vector512.Equals(x, y),
                                    Vector512.ConditionalSelect(IsNegative(x), y, x),
                                    Vector512.Max(x, y)),
                                y),
                            x);
                }

                return Vector512.Max(x, y);
            }
        }

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs (but NaNs may not be propagated)</summary>
        internal readonly struct MaxMagnitudeOperator<T> : IAggregationOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.MaxMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);

                Vector128<T> result =
                    Vector128.ConditionalSelect(Vector128.Equals(xMag, yMag),
                        Vector128.ConditionalSelect(IsNegative(x), y, x),
                        Vector128.ConditionalSelect(Vector128.GreaterThan(xMag, yMag), x, y));

                // Handle minimum signed value that should have the largest magnitude
                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector128<T> negativeMagnitudeX = Vector128.LessThan(xMag, Vector128<T>.Zero);
                    Vector128<T> negativeMagnitudeY = Vector128.LessThan(yMag, Vector128<T>.Zero);
                    result = Vector128.ConditionalSelect(negativeMagnitudeX,
                        x,
                        Vector128.ConditionalSelect(negativeMagnitudeY,
                            y,
                            result));
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                Vector256<T> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);

                Vector256<T> result =
                    Vector256.ConditionalSelect(Vector256.Equals(xMag, yMag),
                        Vector256.ConditionalSelect(IsNegative(x), y, x),
                        Vector256.ConditionalSelect(Vector256.GreaterThan(xMag, yMag), x, y));

                // Handle minimum signed value that should have the largest magnitude
                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector256<T> negativeMagnitudeX = Vector256.LessThan(xMag, Vector256<T>.Zero);
                    Vector256<T> negativeMagnitudeY = Vector256.LessThan(yMag, Vector256<T>.Zero);
                    result = Vector256.ConditionalSelect(negativeMagnitudeX,
                        x,
                        Vector256.ConditionalSelect(negativeMagnitudeY,
                            y,
                            result));
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                Vector512<T> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);

                Vector512<T> result =
                    Vector512.ConditionalSelect(Vector512.Equals(xMag, yMag),
                        Vector512.ConditionalSelect(IsNegative(x), y, x),
                        Vector512.ConditionalSelect(Vector512.GreaterThan(xMag, yMag), x, y));

                // Handle minimum signed value that should have the largest magnitude
                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector512<T> negativeMagnitudeX = Vector512.LessThan(xMag, Vector512<T>.Zero);
                    Vector512<T> negativeMagnitudeY = Vector512.LessThan(yMag, Vector512<T>.Zero);
                    result = Vector512.ConditionalSelect(negativeMagnitudeX,
                        x,
                        Vector512.ConditionalSelect(negativeMagnitudeY,
                            y,
                            result));
                }

                return result;
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MaxMagnitudeOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MaxMagnitudeOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MaxMagnitudeOperator<T>>(x);
        }

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs</summary>
        internal readonly struct MaxMagnitudePropagateNaNOperator<T> : IBinaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.MaxMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, x),
                            Vector128.ConditionalSelect(Vector128.Equals(y, y),
                                Vector128.ConditionalSelect(Vector128.Equals(yMag, xMag),
                                    Vector128.ConditionalSelect(IsNegative(x), y, x),
                                    Vector128.ConditionalSelect(Vector128.GreaterThan(yMag, xMag), y, x)),
                                y),
                            x);
                }

                return MaxMagnitudeOperator<T>.Invoke(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector256<T> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, x),
                            Vector256.ConditionalSelect(Vector256.Equals(y, y),
                                Vector256.ConditionalSelect(Vector256.Equals(xMag, yMag),
                                    Vector256.ConditionalSelect(IsNegative(x), y, x),
                                    Vector256.ConditionalSelect(Vector256.GreaterThan(xMag, yMag), x, y)),
                                y),
                            x);
                }

                return MaxMagnitudeOperator<T>.Invoke(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector512<T> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, x),
                        Vector512.ConditionalSelect(Vector512.Equals(y, y),
                            Vector512.ConditionalSelect(Vector512.Equals(xMag, yMag),
                                Vector512.ConditionalSelect(IsNegative(x), y, x),
                                Vector512.ConditionalSelect(Vector512.GreaterThan(xMag, yMag), x, y)),
                            y),
                        x);
                }

                return MaxMagnitudeOperator<T>.Invoke(x, y);
            }
        }

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

        /// <summary>T.Min(x, y) (but NaNs may not be propagated)</summary>
        internal readonly struct MinOperator<T> : IAggregationOperator<T>
            where T : INumber<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y)
            {
                if (typeof(T) == typeof(Half) || typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return x == y ?
                        (IsNegative(y) ? y : x) :
                        (y < x ? y : x);
                }

                return T.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(byte)) return AdvSimd.Min(x.AsByte(), y.AsByte()).As<byte, T>();
                    if (typeof(T) == typeof(sbyte)) return AdvSimd.Min(x.AsSByte(), y.AsSByte()).As<sbyte, T>();
                    if (typeof(T) == typeof(short)) return AdvSimd.Min(x.AsInt16(), y.AsInt16()).As<short, T>();
                    if (typeof(T) == typeof(ushort)) return AdvSimd.Min(x.AsUInt16(), y.AsUInt16()).As<ushort, T>();
                    if (typeof(T) == typeof(int)) return AdvSimd.Min(x.AsInt32(), y.AsInt32()).As<int, T>();
                    if (typeof(T) == typeof(uint)) return AdvSimd.Min(x.AsUInt32(), y.AsUInt32()).As<uint, T>();
                    if (typeof(T) == typeof(float)) return AdvSimd.Min(x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.Min(x.AsDouble(), y.AsDouble()).As<double, T>();
                }

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, y),
                            Vector128.ConditionalSelect(IsNegative(y), y, x),
                            Vector128.Min(x, y));
                }

                return Vector128.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return Vector256.ConditionalSelect(Vector256.Equals(x, y),
                        Vector256.ConditionalSelect(IsNegative(y), y, x),
                        Vector256.Min(x, y));
                }

                return Vector256.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return Vector512.ConditionalSelect(Vector512.Equals(x, y),
                        Vector512.ConditionalSelect(IsNegative(y), y, x),
                        Vector512.Min(x, y));
                }

                return Vector512.Min(x, y);
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
        }

        /// <summary>T.Min(x, y)</summary>
        internal readonly struct MinPropagateNaNOperator<T> : IBinaryOperator<T>
            where T : INumber<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.Min(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(byte)) return AdvSimd.Min(x.AsByte(), y.AsByte()).As<byte, T>();
                    if (typeof(T) == typeof(sbyte)) return AdvSimd.Min(x.AsSByte(), y.AsSByte()).As<sbyte, T>();
                    if (typeof(T) == typeof(short)) return AdvSimd.Min(x.AsInt16(), y.AsInt16()).As<short, T>();
                    if (typeof(T) == typeof(ushort)) return AdvSimd.Min(x.AsUInt16(), y.AsUInt16()).As<ushort, T>();
                    if (typeof(T) == typeof(int)) return AdvSimd.Min(x.AsInt32(), y.AsInt32()).As<int, T>();
                    if (typeof(T) == typeof(uint)) return AdvSimd.Min(x.AsUInt32(), y.AsUInt32()).As<uint, T>();
                    if (typeof(T) == typeof(float)) return AdvSimd.Min(x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.Min(x.AsDouble(), y.AsDouble()).As<double, T>();
                }

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, x),
                            Vector128.ConditionalSelect(Vector128.Equals(y, y),
                                Vector128.ConditionalSelect(Vector128.Equals(x, y),
                                    Vector128.ConditionalSelect(IsNegative(x), x, y),
                                    Vector128.Min(x, y)),
                                y),
                            x);
                }

                return Vector128.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, x),
                            Vector256.ConditionalSelect(Vector256.Equals(y, y),
                                Vector256.ConditionalSelect(Vector256.Equals(x, y),
                                    Vector256.ConditionalSelect(IsNegative(x), x, y),
                                    Vector256.Min(x, y)),
                                y),
                            x);
                }

                return Vector256.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, x),
                            Vector512.ConditionalSelect(Vector512.Equals(y, y),
                                Vector512.ConditionalSelect(Vector512.Equals(x, y),
                                    Vector512.ConditionalSelect(IsNegative(x), x, y),
                                    Vector512.Min(x, y)),
                                y),
                            x);
                }

                return Vector512.Min(x, y);
            }
        }

        /// <summary>Operator to get x or y based on which has the smaller MathF.Abs (but NaNs may not be propagated)</summary>
        internal readonly struct MinMagnitudeOperator<T> : IAggregationOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.MinMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);

                Vector128<T> result =
                    Vector128.ConditionalSelect(Vector128.Equals(yMag, xMag),
                        Vector128.ConditionalSelect(IsNegative(y), y, x),
                        Vector128.ConditionalSelect(Vector128.LessThan(yMag, xMag), y, x));

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector128<T> negativeMagnitudeX = Vector128.LessThan(xMag, Vector128<T>.Zero);
                    Vector128<T> negativeMagnitudeY = Vector128.LessThan(yMag, Vector128<T>.Zero);
                    result = Vector128.ConditionalSelect(negativeMagnitudeX,
                        y,
                        Vector128.ConditionalSelect(negativeMagnitudeY,
                            x,
                            result));
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                Vector256<T> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);

                Vector256<T> result =
                    Vector256.ConditionalSelect(Vector256.Equals(yMag, xMag),
                        Vector256.ConditionalSelect(IsNegative(y), y, x),
                        Vector256.ConditionalSelect(Vector256.LessThan(yMag, xMag), y, x));

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector256<T> negativeMagnitudeX = Vector256.LessThan(xMag, Vector256<T>.Zero);
                    Vector256<T> negativeMagnitudeY = Vector256.LessThan(yMag, Vector256<T>.Zero);
                    result = Vector256.ConditionalSelect(negativeMagnitudeX,
                        y,
                        Vector256.ConditionalSelect(negativeMagnitudeY,
                            x,
                            result));
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                Vector512<T> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);

                Vector512<T> result =
                    Vector512.ConditionalSelect(Vector512.Equals(yMag, xMag),
                        Vector512.ConditionalSelect(IsNegative(y), y, x),
                        Vector512.ConditionalSelect(Vector512.LessThan(yMag, xMag), y, x));

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector512<T> negativeMagnitudeX = Vector512.LessThan(xMag, Vector512<T>.Zero);
                    Vector512<T> negativeMagnitudeY = Vector512.LessThan(yMag, Vector512<T>.Zero);
                    result = Vector512.ConditionalSelect(negativeMagnitudeX,
                        y,
                        Vector512.ConditionalSelect(negativeMagnitudeY,
                            x,
                            result));
                }

                return result;
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MinMagnitudeOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MinMagnitudeOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MinMagnitudeOperator<T>>(x);
        }

        /// <summary>Operator to get x or y based on which has the smaller MathF.Abs</summary>
        internal readonly struct MinMagnitudePropagateNaNOperator<T> : IBinaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.MinMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, x),
                            Vector128.ConditionalSelect(Vector128.Equals(y, y),
                                Vector128.ConditionalSelect(Vector128.Equals(yMag, xMag),
                                    Vector128.ConditionalSelect(IsNegative(x), x, y),
                                    Vector128.ConditionalSelect(Vector128.LessThan(xMag, yMag), x, y)),
                                y),
                            x);
                }

                return MinMagnitudeOperator<T>.Invoke(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector256<T> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, x),
                            Vector256.ConditionalSelect(Vector256.Equals(y, y),
                                Vector256.ConditionalSelect(Vector256.Equals(yMag, xMag),
                                    Vector256.ConditionalSelect(IsNegative(x), x, y),
                                    Vector256.ConditionalSelect(Vector256.LessThan(xMag, yMag), x, y)),
                                y),
                            x);
                }

                return MinMagnitudeOperator<T>.Invoke(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector512<T> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, x),
                            Vector512.ConditionalSelect(Vector512.Equals(y, y),
                                Vector512.ConditionalSelect(Vector512.Equals(yMag, xMag),
                                    Vector512.ConditionalSelect(IsNegative(x), x, y),
                                    Vector512.ConditionalSelect(Vector512.LessThan(xMag, yMag), x, y)),
                                y),
                            x);
                }

                return MinMagnitudeOperator<T>.Invoke(x, y);
            }
        }

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

        internal readonly struct IndexOfMinMagnitudeOperator<T> : IIndexOfOperator<T> where T : INumber<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector128<T> result, Vector128<T> current, ref Vector128<T> resultIndex, Vector128<T> currentIndex)
            {
                Vector128<T> resultMag = Vector128.Abs(result), currentMag = Vector128.Abs(current);
                Vector128<T> useResult = Vector128.LessThan(resultMag, currentMag);
                Vector128<T> equalMask = Vector128.Equals(resultMag, currentMag);

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
                Vector256<T> resultMag = Vector256.Abs(result), currentMag = Vector256.Abs(current);
                Vector256<T> useResult = Vector256.LessThan(resultMag, currentMag);
                Vector256<T> equalMask = Vector256.Equals(resultMag, currentMag);

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
                Vector512<T> resultMag = Vector512.Abs(result), currentMag = Vector512.Abs(current);
                Vector512<T> useResult = Vector512.LessThan(resultMag, currentMag);
                Vector512<T> equalMask = Vector512.Equals(resultMag, currentMag);

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
                T resultMag = T.Abs(result);
                T currentMag = T.Abs(current);

                if (resultMag == currentMag)
                {
                    bool currentNegative = IsNegative(current);
                    if ((IsNegative(result) == currentNegative) ? (currentIndex < resultIndex) : currentNegative)
                    {
                        result = current;
                        return currentIndex;
                    }
                }
                else if (currentMag < resultMag)
                {
                    result = current;
                    return currentIndex;
                }

                return resultIndex;
            }
        }

        /// <summary>x / y</summary>
        internal readonly struct DivideOperator<T> : IBinaryOperator<T> where T : IDivisionOperators<T, T, T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x, T y) => x / y;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => x / y;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => x / y;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => x / y;
        }

        /// <summary>-x</summary>
        internal readonly struct NegateOperator<T> : IUnaryOperator<T, T> where T : IUnaryNegationOperators<T, T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => -x;
            public static Vector128<T> Invoke(Vector128<T> x) => -x;
            public static Vector256<T> Invoke(Vector256<T> x) => -x;
            public static Vector512<T> Invoke(Vector512<T> x) => -x;
        }

        /// <summary>(x + y) * z</summary>
        internal readonly struct AddMultiplyOperator<T> : ITernaryOperator<T> where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T>
        {
            public static T Invoke(T x, T y, T z) => (x + y) * z;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> z) => (x + y) * z;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> z) => (x + y) * z;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> z) => (x + y) * z;
        }

        /// <summary>(x * y) + z</summary>
        internal readonly struct MultiplyAddOperator<T> : ITernaryOperator<T> where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T>
        {
            public static T Invoke(T x, T y, T z) => (x * y) + z;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> z) => (x * y) + z;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> z) => (x * y) + z;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> z) => (x * y) + z;
        }

        /// <summary>(x * y) + z</summary>
        internal readonly struct MultiplyAddEstimateOperator<T> : ITernaryOperator<T> where T : INumberBase<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y, T z)
            {
                // TODO https://github.com/dotnet/runtime/issues/98053: Use T.MultiplyAddEstimate when it's available.

                if (Fma.IsSupported || AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(Half))
                    {
                        Half result = Half.FusedMultiplyAdd(Unsafe.As<T, Half>(ref x), Unsafe.As<T, Half>(ref y), Unsafe.As<T, Half>(ref z));
                        return Unsafe.As<Half, T>(ref result);
                    }

                    if (typeof(T) == typeof(float))
                    {
                        float result = float.FusedMultiplyAdd(Unsafe.As<T, float>(ref x), Unsafe.As<T, float>(ref y), Unsafe.As<T, float>(ref z));
                        return Unsafe.As<float, T>(ref result);
                    }

                    if (typeof(T) == typeof(double))
                    {
                        double result = double.FusedMultiplyAdd(Unsafe.As<T, double>(ref x), Unsafe.As<T, double>(ref y), Unsafe.As<T, double>(ref z));
                        return Unsafe.As<double, T>(ref result);
                    }
                }

                return (x * y) + z;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> z)
            {
                if (Fma.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Fma.MultiplyAdd(x.AsSingle(), y.AsSingle(), z.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Fma.MultiplyAdd(x.AsDouble(), y.AsDouble(), z.AsDouble()).As<double, T>();
                }

                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return AdvSimd.FusedMultiplyAdd(z.AsSingle(), x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.FusedMultiplyAdd(z.AsDouble(), x.AsDouble(), y.AsDouble()).As<double, T>();
                }

                return (x * y) + z;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> z)
            {
                if (Fma.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Fma.MultiplyAdd(x.AsSingle(), y.AsSingle(), z.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Fma.MultiplyAdd(x.AsDouble(), y.AsDouble(), z.AsDouble()).As<double, T>();
                }

                return (x * y) + z;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> z)
            {
                if (Avx512F.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx512F.FusedMultiplyAdd(x.AsSingle(), y.AsSingle(), z.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Avx512F.FusedMultiplyAdd(x.AsDouble(), y.AsDouble(), z.AsDouble()).As<double, T>();
                }

                return (x * y) + z;
            }
        }

        /// <summary>x</summary>
        internal readonly struct IdentityOperator<T> : IUnaryOperator<T, T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => x;
            public static Vector128<T> Invoke(Vector128<T> x) => x;
            public static Vector256<T> Invoke(Vector256<T> x) => x;
            public static Vector512<T> Invoke(Vector512<T> x) => x;
        }

        /// <summary>x * x</summary>
        internal readonly struct SquaredOperator<T> : IUnaryOperator<T, T> where T : IMultiplyOperators<T, T, T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => x * x;
            public static Vector128<T> Invoke(Vector128<T> x) => x * x;
            public static Vector256<T> Invoke(Vector256<T> x) => x * x;
            public static Vector512<T> Invoke(Vector512<T> x) => x * x;
        }

        /// <summary>x + y</summary>
        internal readonly struct AddOperator<T> : IAggregationOperator<T> where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y) => x + y;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => x + y;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => x + y;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => x + y;

            public static T Invoke(Vector128<T> x) => Vector128.Sum(x);
            public static T Invoke(Vector256<T> x) => Vector256.Sum(x);
            public static T Invoke(Vector512<T> x) => Vector512.Sum(x);

            public static T IdentityValue => T.AdditiveIdentity;
        }

        /// <summary>x - y</summary>
        internal readonly struct SubtractOperator<T> : IBinaryOperator<T> where T : ISubtractionOperators<T, T, T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x, T y) => x - y;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => x - y;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => x - y;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => x - y;
        }

        /// <summary>(x - y) * (x - y)</summary>
        internal readonly struct SubtractSquaredOperator<T> : IBinaryOperator<T> where T : ISubtractionOperators<T, T, T>, IMultiplyOperators<T, T, T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y)
            {
                T tmp = x - y;
                return tmp * tmp;
            }

            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                Vector128<T> tmp = x - y;
                return tmp * tmp;
            }

            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                Vector256<T> tmp = x - y;
                return tmp * tmp;
            }

            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                Vector512<T> tmp = x - y;
                return tmp * tmp;
            }
        }

        /// <summary>x * y</summary>
        internal readonly struct MultiplyOperator<T> : IAggregationOperator<T> where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y) => x * y;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => x * y;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => x * y;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => x * y;

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MultiplyOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MultiplyOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MultiplyOperator<T>>(x);

            public static T IdentityValue => T.MultiplicativeIdentity;
        }

        private readonly struct CopySignOperator<T> : IBinaryOperator<T> where T : INumber<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y) => T.CopySign(x, y);

            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector128.ConditionalSelect(Vector128.Create(-0.0f).As<float, T>(), y, x);
                }

                if (typeof(T) == typeof(double))
                {
                    return Vector128.ConditionalSelect(Vector128.Create(-0.0d).As<double, T>(), y, x);
                }

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector128<T> absValue = Vector128.Abs(x);
                    Vector128<T> sign = Vector128.GreaterThanOrEqual(y, Vector128<T>.Zero);
                    Vector128<T> error = sign & Vector128.LessThan(absValue, Vector128<T>.Zero);
                    if (error != Vector128<T>.Zero)
                    {
                        Math.Abs(int.MinValue); // throw OverflowException
                    }

                    return Vector128.ConditionalSelect(sign, absValue, -absValue);
                }

                return x;
            }

            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector256.ConditionalSelect(Vector256.Create(-0.0f).As<float, T>(), y, x);
                }

                if (typeof(T) == typeof(double))
                {
                    return Vector256.ConditionalSelect(Vector256.Create(-0.0d).As<double, T>(), y, x);
                }

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector256<T> absValue = Vector256.Abs(x);
                    Vector256<T> sign = Vector256.GreaterThanOrEqual(y, Vector256<T>.Zero);
                    Vector256<T> error = sign & Vector256.LessThan(absValue, Vector256<T>.Zero);
                    if (error != Vector256<T>.Zero)
                    {
                        Math.Abs(int.MinValue); // throw OverflowException
                    }

                    return Vector256.ConditionalSelect(sign, absValue, -absValue);
                }

                return x;
            }

            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector512.ConditionalSelect(Vector512.Create(-0.0f).As<float, T>(), y, x);
                }

                if (typeof(T) == typeof(double))
                {
                    return Vector512.ConditionalSelect(Vector512.Create(-0.0d).As<double, T>(), y, x);
                }

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector512<T> absValue = Vector512.Abs(x);
                    Vector512<T> sign = Vector512.GreaterThanOrEqual(y, Vector512<T>.Zero);
                    Vector512<T> error = sign & Vector512.LessThan(absValue, Vector512<T>.Zero);
                    if (error != Vector512<T>.Zero)
                    {
                        Math.Abs(int.MinValue); // throw OverflowException
                    }

                    return Vector512.ConditionalSelect(sign, absValue, -absValue);
                }

                return x;
            }
        }

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
