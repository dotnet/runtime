// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using static System.Numerics.Tensors.TensorPrimitives;

namespace System.Numerics.Tensors
{
    public static unsafe partial class TensorPrimitives
    {
        /// <summary>Unary operator that produces a Boolean result for each element.</summary>
        /// <remarks>For vector-based methods, the Boolean result is either all-bits-set or zero.</remarks>
        private interface IBooleanUnaryOperator<T>
        {
            static abstract bool Vectorizable { get; }
            static abstract bool Invoke(T x);
            static abstract Vector128<T> Invoke(Vector128<T> x);
            static abstract Vector256<T> Invoke(Vector256<T> x);
            static abstract Vector512<T> Invoke(Vector512<T> x);
        }

        private interface IAnyAllAggregator<T>
        {
            static abstract bool DefaultResult { get; }
            static abstract bool ShouldEarlyExit(bool result);
            static abstract bool ShouldEarlyExit(Vector128<T> result);
            static abstract bool ShouldEarlyExit(Vector256<T> result);
            static abstract bool ShouldEarlyExit(Vector512<T> result);
        }

        private readonly struct AnyAggregator<T> : IAnyAllAggregator<T>
        {
            public static bool DefaultResult => false;

            public static bool ShouldEarlyExit(bool result) => result;

#if NET10_0_OR_GREATER
            public static bool ShouldEarlyExit(Vector128<T> result) => Vector128.AnyWhereAllBitsSet(result);
            public static bool ShouldEarlyExit(Vector256<T> result) => Vector256.AnyWhereAllBitsSet(result);
            public static bool ShouldEarlyExit(Vector512<T> result) => Vector512.AnyWhereAllBitsSet(result);
#else
            public static bool ShouldEarlyExit(Vector128<T> result) =>
                typeof(T) == typeof(float) ? Vector128.EqualsAny(result.AsUInt32(), Vector128<uint>.AllBitsSet) :
                typeof(T) == typeof(double) ? Vector128.EqualsAny(result.AsUInt64(), Vector128<ulong>.AllBitsSet) :
                Vector128.EqualsAny(result, Vector128<T>.AllBitsSet);

            public static bool ShouldEarlyExit(Vector256<T> result) =>
                typeof(T) == typeof(float) ? Vector256.EqualsAny(result.AsUInt32(), Vector256<uint>.AllBitsSet) :
                typeof(T) == typeof(double) ? Vector256.EqualsAny(result.AsUInt64(), Vector256<ulong>.AllBitsSet) :
                Vector256.EqualsAny(result, Vector256<T>.AllBitsSet);

            public static bool ShouldEarlyExit(Vector512<T> result) =>
                typeof(T) == typeof(float) ? Vector512.EqualsAny(result.AsUInt32(), Vector512<uint>.AllBitsSet) :
                typeof(T) == typeof(double) ? Vector512.EqualsAny(result.AsUInt64(), Vector512<ulong>.AllBitsSet) :
                Vector512.EqualsAny(result, Vector512<T>.AllBitsSet);
#endif
        }

        private readonly struct AllAggregator<T> : IAnyAllAggregator<T>
        {
            public static bool DefaultResult => true;

            public static bool ShouldEarlyExit(bool result) => !result;

            public static bool ShouldEarlyExit(Vector128<T> result) =>
                typeof(T) == typeof(float) ? Vector128.EqualsAny(result.AsUInt32(), Vector128<uint>.Zero) :
                typeof(T) == typeof(double) ? Vector128.EqualsAny(result.AsUInt64(), Vector128<ulong>.Zero) :
                Vector128.EqualsAny(result, Vector128<T>.Zero);

            public static bool ShouldEarlyExit(Vector256<T> result) =>
                typeof(T) == typeof(float) ? Vector256.EqualsAny(result.AsUInt32(), Vector256<uint>.Zero) :
                typeof(T) == typeof(double) ? Vector256.EqualsAny(result.AsUInt64(), Vector256<ulong>.Zero) :
                Vector256.EqualsAny(result, Vector256<T>.Zero);

            public static bool ShouldEarlyExit(Vector512<T> result) =>
                typeof(T) == typeof(float) ? Vector512.EqualsAny(result.AsUInt32(), Vector512<uint>.Zero) :
                typeof(T) == typeof(double) ? Vector512.EqualsAny(result.AsUInt64(), Vector512<ulong>.Zero) :
                Vector512.EqualsAny(result, Vector512<T>.Zero);
        }

        private static bool All<T, TOperator>(ReadOnlySpan<T> x)
            where TOperator : struct, IBooleanUnaryOperator<T> =>
            AggregateAnyAll<T, TOperator, AllAggregator<T>>(x);

        private static bool Any<T, TOperator>(ReadOnlySpan<T> x)
            where TOperator : struct, IBooleanUnaryOperator<T> =>
            AggregateAnyAll<T, TOperator, AnyAggregator<T>>(x);

        private static bool AggregateAnyAll<T, TOperator, TAnyAll>(ReadOnlySpan<T> x)
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T>
        {
            Debug.Assert(!x.IsEmpty);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            int i = 0, oneVectorFromEnd;

            if (Vector512.IsHardwareAccelerated && TOperator.Vectorizable && Vector512<T>.IsSupported)
            {
                oneVectorFromEnd = x.Length - Vector512<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        if (TAnyAll.ShouldEarlyExit(TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i))))
                        {
                            return !TAnyAll.DefaultResult;
                        }

                        i += Vector512<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length &&
                        TAnyAll.ShouldEarlyExit(TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<T>.Count)))))
                    {
                        return !TAnyAll.DefaultResult;
                    }

                    return TAnyAll.DefaultResult;
                }
            }

            if (Vector256.IsHardwareAccelerated && TOperator.Vectorizable && Vector256<T>.IsSupported)
            {
                oneVectorFromEnd = x.Length - Vector256<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        if (TAnyAll.ShouldEarlyExit(TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i))))
                        {
                            return !TAnyAll.DefaultResult;
                        }

                        i += Vector256<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length &&
                        TAnyAll.ShouldEarlyExit(TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<T>.Count)))))
                    {
                        return !TAnyAll.DefaultResult;
                    }

                    return TAnyAll.DefaultResult;
                }
            }

            if (Vector128.IsHardwareAccelerated && TOperator.Vectorizable && Vector128<T>.IsSupported)
            {
                oneVectorFromEnd = x.Length - Vector128<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        if (TAnyAll.ShouldEarlyExit(TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i))))
                        {
                            return !TAnyAll.DefaultResult;
                        }

                        i += Vector128<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length &&
                        TAnyAll.ShouldEarlyExit(TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<T>.Count)))))
                    {
                        return !TAnyAll.DefaultResult;
                    }

                    return TAnyAll.DefaultResult;
                }
            }

            while (i < x.Length)
            {
                if (TAnyAll.ShouldEarlyExit(TOperator.Invoke(Unsafe.Add(ref xRef, i))))
                {
                    return !TAnyAll.DefaultResult;
                }

                i++;
            }

            return TAnyAll.DefaultResult;
        }

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination"/>.</summary>
        /// <typeparam name="T">The element input type.</typeparam>
        /// <typeparam name="TOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static void InvokeSpanIntoSpan<T, TOperator>(
            ReadOnlySpan<T> x, Span<bool> destination)
            where TOperator : struct, IBooleanUnaryOperator<T>
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            if (sizeof(T) == 1)
            {
                Vectorized_Size1(x, destination);
            }
            else if (sizeof(T) == 2)
            {
                Vectorized_Size2(x, destination);
            }
            else if (sizeof(T) == 4)
            {
                Vectorized_Size4(x, destination);
            }
            else
            {
                Vectorized_Size8OrOther(x, destination);
            }

            static void Vectorized_Size1(ReadOnlySpan<T> x, Span<bool> destination)
            {
                Debug.Assert(sizeof(T) == sizeof(bool));

                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref bool destinationRef = ref MemoryMarshal.GetReference(destination);
                int i = 0;

                if (Vector512.IsHardwareAccelerated && TOperator.Vectorizable && Vector512<T>.IsSupported)
                {
                    int vectorFromEnd = x.Length - Vector512<T>.Count;
                    if (i <= vectorFromEnd)
                    {
                        // Loop handling one input vector / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector512<T>.Count;
                        }
                        while (i <= vectorFromEnd);

                        // Handle any remaining elements with a final vector.
                        if (i != x.Length)
                        {
                            i = x.Length - Vector512<T>.Count;
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector512<byte> v = TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i)).AsByte();

                            (v & Vector512<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                if (Vector256.IsHardwareAccelerated && TOperator.Vectorizable && Vector256<T>.IsSupported)
                {
                    int vectorFromEnd = x.Length - Vector256<T>.Count;
                    if (i <= vectorFromEnd)
                    {
                        // Loop handling one input vector / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector256<T>.Count;
                        }
                        while (i <= vectorFromEnd);

                        // Handle any remaining elements with a final vector.
                        if (i != x.Length)
                        {
                            i = x.Length - Vector256<T>.Count;
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector256<byte> v = TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i)).AsByte();

                            (v & Vector256<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                if (Vector128.IsHardwareAccelerated && TOperator.Vectorizable && Vector128<T>.IsSupported)
                {
                    int vectorFromEnd = x.Length - Vector128<T>.Count;
                    if (i <= vectorFromEnd)
                    {
                        // Loop handling one input vector / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector128<T>.Count;
                        }
                        while (i <= vectorFromEnd);

                        // Handle any remaining elements with a final vector.
                        if (i != x.Length)
                        {
                            i = x.Length - Vector128<T>.Count;
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector128<byte> v = TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i)).AsByte();

                            (v & Vector128<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                while (i < x.Length)
                {
                    Unsafe.Add(ref destinationRef, i) = TOperator.Invoke(Unsafe.Add(ref xRef, i));
                    i++;
                }
            }

            static void Vectorized_Size2(ReadOnlySpan<T> x, Span<bool> destination)
            {
                Debug.Assert(sizeof(T) == 2 * sizeof(bool));

                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref bool destinationRef = ref MemoryMarshal.GetReference(destination);
                int i = 0;

                if (Vector512.IsHardwareAccelerated && TOperator.Vectorizable && Vector512<T>.IsSupported)
                {
                    int vectorsFromEnd = x.Length - (Vector512<T>.Count * sizeof(T));
                    if (i <= vectorsFromEnd)
                    {
                        // Loop handling two input vectors / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector512<T>.Count * sizeof(T);
                        }
                        while (i <= vectorsFromEnd);

                        // Handle any remaining elements with final vectors.
                        if (i != x.Length)
                        {
                            i = x.Length - (Vector512<T>.Count * sizeof(T));
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector512<byte> v =
                                Vector512.Narrow(
                                    TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i)).AsUInt16(),
                                    TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(i + Vector512<T>.Count))).AsUInt16());

                            (v & Vector512<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                if (Vector256.IsHardwareAccelerated && TOperator.Vectorizable && Vector256<T>.IsSupported)
                {
                    int vectorsFromEnd = x.Length - (Vector256<T>.Count * sizeof(T));
                    if (i <= vectorsFromEnd)
                    {
                        // Loop handling two input vectors / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector256<T>.Count * sizeof(T);
                        }
                        while (i <= vectorsFromEnd);

                        // Handle any remaining elements with final vectors.
                        if (i != x.Length)
                        {
                            i = x.Length - (Vector256<T>.Count * sizeof(T));
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector256<byte> v =
                                Vector256.Narrow(
                                    TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i)).AsUInt16(),
                                    TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(i + Vector256<T>.Count))).AsUInt16());

                            (v & Vector256<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                if (Vector128.IsHardwareAccelerated && TOperator.Vectorizable && Vector128<T>.IsSupported)
                {
                    int vectorsFromEnd = x.Length - (Vector128<T>.Count * sizeof(T));
                    if (i <= vectorsFromEnd)
                    {
                        // Loop handling two input vectors / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector128<T>.Count * sizeof(T);
                        }
                        while (i <= vectorsFromEnd);

                        // Handle any remaining elements with final vectors.
                        if (i != x.Length)
                        {
                            i = x.Length - (Vector128<T>.Count * sizeof(T));
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector128<byte> v =
                                Vector128.Narrow(
                                    TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i)).AsUInt16(),
                                    TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(i + Vector128<T>.Count))).AsUInt16());

                            (v & Vector128<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                while (i < x.Length)
                {
                    Unsafe.Add(ref destinationRef, i) = TOperator.Invoke(Unsafe.Add(ref xRef, i));
                    i++;
                }
            }

            static void Vectorized_Size4(ReadOnlySpan<T> x, Span<bool> destination)
            {
                Debug.Assert(sizeof(T) == 4 * sizeof(bool));

                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref bool destinationRef = ref MemoryMarshal.GetReference(destination);
                int i = 0;

                if (Vector512.IsHardwareAccelerated && TOperator.Vectorizable && Vector512<T>.IsSupported)
                {
                    int vectorsFromEnd = x.Length - (Vector512<T>.Count * sizeof(T));
                    if (i <= vectorsFromEnd)
                    {
                        // Loop handling two input vectors / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector512<T>.Count * sizeof(T);
                        }
                        while (i <= vectorsFromEnd);

                        // Handle any remaining elements with final vectors.
                        if (i != x.Length)
                        {
                            i = x.Length - (Vector512<T>.Count * sizeof(T));
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector512<byte> v =
                                Vector512.Narrow(
                                    Vector512.Narrow(
                                        TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i)).AsUInt32(),
                                        TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(i + Vector512<T>.Count))).AsUInt32()),
                                    Vector512.Narrow(
                                        TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(i + (2 * Vector512<T>.Count)))).AsUInt32(),
                                        TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(i + (3 * Vector512<T>.Count)))).AsUInt32()));

                            (v & Vector512<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                if (Vector256.IsHardwareAccelerated && TOperator.Vectorizable && Vector256<T>.IsSupported)
                {
                    int vectorsFromEnd = x.Length - (Vector256<T>.Count * sizeof(T));
                    if (i <= vectorsFromEnd)
                    {
                        // Loop handling two input vectors / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector256<T>.Count * sizeof(T);
                        }
                        while (i <= vectorsFromEnd);

                        // Handle any remaining elements with final vectors.
                        if (i != x.Length)
                        {
                            i = x.Length - (Vector256<T>.Count * sizeof(T));
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector256<byte> v =
                                Vector256.Narrow(
                                    Vector256.Narrow(
                                        TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i)).AsUInt32(),
                                        TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(i + Vector256<T>.Count))).AsUInt32()),
                                    Vector256.Narrow(
                                        TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(i + (2 * Vector256<T>.Count)))).AsUInt32(),
                                        TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(i + (3 * Vector256<T>.Count)))).AsUInt32()));

                            (v & Vector256<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                if (Vector128.IsHardwareAccelerated && TOperator.Vectorizable && Vector128<T>.IsSupported)
                {
                    int vectorsFromEnd = x.Length - (Vector128<T>.Count * sizeof(T));
                    if (i <= vectorsFromEnd)
                    {
                        // Loop handling two input vectors / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector128<T>.Count * sizeof(T);
                        }
                        while (i <= vectorsFromEnd);

                        // Handle any remaining elements with final vectors.
                        if (i != x.Length)
                        {
                            i = x.Length - (Vector128<T>.Count * sizeof(T));
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector128<byte> v =
                                Vector128.Narrow(
                                    Vector128.Narrow(
                                        TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i)).AsUInt32(),
                                        TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(i + Vector128<T>.Count))).AsUInt32()),
                                    Vector128.Narrow(
                                        TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(i + (2 * Vector128<T>.Count)))).AsUInt32(),
                                        TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(i + (3 * Vector128<T>.Count)))).AsUInt32()));

                            (v & Vector128<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                while (i < x.Length)
                {
                    Unsafe.Add(ref destinationRef, i) = TOperator.Invoke(Unsafe.Add(ref xRef, i));
                    i++;
                }
            }

            static void Vectorized_Size8OrOther(ReadOnlySpan<T> x, Span<bool> destination)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref bool destinationRef = ref MemoryMarshal.GetReference(destination);
                int i = 0;

                if (Vector512.IsHardwareAccelerated && TOperator.Vectorizable && Vector512<T>.IsSupported)
                {
                    Debug.Assert(sizeof(T) == 8 * sizeof(bool));

                    int vectorsFromEnd = x.Length - (Vector512<T>.Count * sizeof(T));
                    if (i <= vectorsFromEnd)
                    {
                        // Loop handling two input vectors / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector512<T>.Count * sizeof(T);
                        }
                        while (i <= vectorsFromEnd);

                        // Handle any remaining elements with final vectors.
                        if (i != x.Length)
                        {
                            i = x.Length - (Vector512<T>.Count * sizeof(T));
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector512<byte> v =
                                Vector512.Narrow(
                                    Vector512.Narrow(
                                        Vector512.Narrow(
                                            TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i)).AsUInt64(),
                                            TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(i + Vector512<T>.Count))).AsUInt64()),
                                        Vector512.Narrow(
                                            TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(i + (2 * Vector512<T>.Count)))).AsUInt64(),
                                            TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(i + (3 * Vector512<T>.Count)))).AsUInt64())),
                                    Vector512.Narrow(
                                        Vector512.Narrow(
                                            TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(i + (4 * Vector512<T>.Count)))).AsUInt64(),
                                            TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(i + (5 * Vector512<T>.Count)))).AsUInt64()),
                                        Vector512.Narrow(
                                            TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(i + (6 * Vector512<T>.Count)))).AsUInt64(),
                                            TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(i + (7 * Vector512<T>.Count)))).AsUInt64())));

                            (v & Vector512<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                if (Vector256.IsHardwareAccelerated && TOperator.Vectorizable && Vector256<T>.IsSupported)
                {
                    Debug.Assert(sizeof(T) == 8 * sizeof(bool));

                    int vectorsFromEnd = x.Length - (Vector256<T>.Count * sizeof(T));
                    if (i <= vectorsFromEnd)
                    {
                        // Loop handling two input vectors / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector256<T>.Count * sizeof(T);
                        }
                        while (i <= vectorsFromEnd);

                        // Handle any remaining elements with final vectors.
                        if (i != x.Length)
                        {
                            i = x.Length - (Vector256<T>.Count * sizeof(T));
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector256<byte> v =
                                Vector256.Narrow(
                                    Vector256.Narrow(
                                        Vector256.Narrow(
                                            TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i)).AsUInt64(),
                                            TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(i + Vector256<T>.Count))).AsUInt64()),
                                        Vector256.Narrow(
                                            TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(i + (2 * Vector256<T>.Count)))).AsUInt64(),
                                            TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(i + (3 * Vector256<T>.Count)))).AsUInt64())),
                                    Vector256.Narrow(
                                        Vector256.Narrow(
                                            TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(i + (4 * Vector256<T>.Count)))).AsUInt64(),
                                            TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(i + (5 * Vector256<T>.Count)))).AsUInt64()),
                                        Vector256.Narrow(
                                            TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(i + (6 * Vector256<T>.Count)))).AsUInt64(),
                                            TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(i + (7 * Vector256<T>.Count)))).AsUInt64())));

                            (v & Vector256<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                if (Vector128.IsHardwareAccelerated && TOperator.Vectorizable && Vector128<T>.IsSupported)
                {
                    Debug.Assert(sizeof(T) == 8 * sizeof(bool));

                    int vectorsFromEnd = x.Length - (Vector128<T>.Count * sizeof(T));
                    if (i <= vectorsFromEnd)
                    {
                        // Loop handling two input vectors / one output vector at a time.
                        do
                        {
                            Process(ref xRef, ref destinationRef, i);
                            i += Vector128<T>.Count * sizeof(T);
                        }
                        while (i <= vectorsFromEnd);

                        // Handle any remaining elements with final vectors.
                        if (i != x.Length)
                        {
                            i = x.Length - (Vector128<T>.Count * sizeof(T));
                            Process(ref xRef, ref destinationRef, i);
                        }

                        return;

                        static void Process(ref T xRef, ref bool destinationRef, int i)
                        {
                            Vector128<byte> v =
                                Vector128.Narrow(
                                    Vector128.Narrow(
                                        Vector128.Narrow(
                                            TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i)).AsUInt64(),
                                            TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(i + Vector128<T>.Count))).AsUInt64()),
                                        Vector128.Narrow(
                                            TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(i + (2 * Vector128<T>.Count)))).AsUInt64(),
                                            TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(i + (3 * Vector128<T>.Count)))).AsUInt64())),
                                    Vector128.Narrow(
                                        Vector128.Narrow(
                                            TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(i + (4 * Vector128<T>.Count)))).AsUInt64(),
                                            TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(i + (5 * Vector128<T>.Count)))).AsUInt64()),
                                        Vector128.Narrow(
                                            TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(i + (6 * Vector128<T>.Count)))).AsUInt64(),
                                            TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(i + (7 * Vector128<T>.Count)))).AsUInt64())));

                            (v & Vector128<byte>.One).StoreUnsafe(ref Unsafe.As<bool, byte>(ref destinationRef), (uint)i);
                        }
                    }
                }

                while (i < x.Length)
                {
                    Unsafe.Add(ref destinationRef, i) = TOperator.Invoke(Unsafe.Add(ref xRef, i));
                    i++;
                }
            }
        }
    }
}
