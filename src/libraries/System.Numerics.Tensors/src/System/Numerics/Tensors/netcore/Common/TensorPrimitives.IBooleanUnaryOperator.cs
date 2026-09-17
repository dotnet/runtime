// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

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

        /// <summary>
        /// Combines the results of <see cref="IBooleanUnaryOperator{T}"/> into an Any/All decision. The vectorized loops of
        /// <see cref="AggregateAnyAll{T, TOperator, TAnyAll}"/> fold the per-vector results of a block of vectors into an accumulator
        /// with <c>Accumulate</c>, starting from zero, and decide once per block: a lane of the accumulator with all bits set means
        /// that the block contains an element which settles the result, and the loop exits with <c>!DefaultResult</c>.
        /// </summary>
        private interface IAnyAllAggregator<T>
        {
            static abstract bool DefaultResult { get; }
            static abstract bool ShouldEarlyExit(bool result);
            static abstract bool ShouldEarlyExit(Vector128<T> result);
            static abstract bool ShouldEarlyExit(Vector256<T> result);
            static abstract bool ShouldEarlyExit(Vector512<T> result);

            /// <summary>Folds an operator result into a block accumulator: a lane of the accumulator ends up with all bits set if <see cref="ShouldEarlyExit(Vector128{T})"/> holds for any result folded into it.</summary>
            static abstract Vector128<T> Accumulate(Vector128<T> accumulator, Vector128<T> result);
            /// <inheritdoc cref="Accumulate(Vector128{T}, Vector128{T})"/>
            static abstract Vector256<T> Accumulate(Vector256<T> accumulator, Vector256<T> result);
            /// <inheritdoc cref="Accumulate(Vector128{T}, Vector128{T})"/>
            static abstract Vector512<T> Accumulate(Vector512<T> accumulator, Vector512<T> result);
        }

        private readonly struct AnyAggregator<T> : IAnyAllAggregator<T>
        {
            public static bool DefaultResult => false;

            public static bool ShouldEarlyExit(bool result) => result;

            public static bool ShouldEarlyExit(Vector128<T> result) => Vector128.AnyWhereAllBitsSet(result);
            public static bool ShouldEarlyExit(Vector256<T> result) => Vector256.AnyWhereAllBitsSet(result);
            public static bool ShouldEarlyExit(Vector512<T> result) => Vector512.AnyWhereAllBitsSet(result);

            // A lane where the operator was true stays set.
            public static Vector128<T> Accumulate(Vector128<T> accumulator, Vector128<T> result) => accumulator | result;
            public static Vector256<T> Accumulate(Vector256<T> accumulator, Vector256<T> result) => accumulator | result;
            public static Vector512<T> Accumulate(Vector512<T> accumulator, Vector512<T> result) => accumulator | result;
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

            // A lane where the operator was false (its result is zero) becomes all bits set and stays set. Accumulating the
            // complement rather than AND-ing the results keeps the block test the same as for Any and lets the JIT fold the
            // complement into an operator that ends in a negation (such as IsFinite).
            public static Vector128<T> Accumulate(Vector128<T> accumulator, Vector128<T> result) => accumulator | ~result;
            public static Vector256<T> Accumulate(Vector256<T> accumulator, Vector256<T> result) => accumulator | ~result;
            public static Vector512<T> Accumulate(Vector512<T> accumulator, Vector512<T> result) => accumulator | ~result;
        }

        private static bool All<T, TOperator>(ReadOnlySpan<T> x)
            where TOperator : struct, IBooleanUnaryOperator<T> =>
            AggregateAnyAll<T, TOperator, AllAggregator<T>>(x);

        private static bool Any<T, TOperator>(ReadOnlySpan<T> x)
            where TOperator : struct, IBooleanUnaryOperator<T> =>
            AggregateAnyAll<T, TOperator, AnyAggregator<T>>(x);

        /// <summary>Vectors per block in the vectorized paths of <see cref="AggregateAnyAll{T, TOperator, TAnyAll}"/>. Must be even.</summary>
        private const int AnyAllBlockVectors = 32;

        private static bool AggregateAnyAll<T, TOperator, TAnyAll>(ReadOnlySpan<T> x)
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T>
        {
            Debug.Assert(!x.IsEmpty);

            if (Vector512.IsHardwareAccelerated && TOperator.Vectorizable && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
            {
                return AggregateAnyAllVectorized512<T, TOperator, TAnyAll>(x);
            }

            if (Vector256.IsHardwareAccelerated && TOperator.Vectorizable && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                return AggregateAnyAllVectorized256<T, TOperator, TAnyAll>(x);
            }

            if (Vector128.IsHardwareAccelerated && TOperator.Vectorizable && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                return AggregateAnyAllVectorized128<T, TOperator, TAnyAll>(x);
            }

            ref T xRef = ref MemoryMarshal.GetReference(x);
            for (int i = 0; i < x.Length; i++)
            {
                if (TAnyAll.ShouldEarlyExit(TOperator.Invoke(Unsafe.Add(ref xRef, i))))
                {
                    return !TAnyAll.DefaultResult;
                }
            }

            return TAnyAll.DefaultResult;
        }

        /// <summary>The 512-bit path of <see cref="AggregateAnyAll{T, TOperator, TAnyAll}"/>: the whole vectors in blocks, then one final vector that overlaps the last whole one.</summary>
        /// <remarks>
        /// Every block of up to <see cref="AnyAllBlockVectors"/> vectors is folded into two independent accumulators with no branch on the
        /// data, and the exit decision is made once per block, so a hit is detected after at most one block of extra reads.
        /// Blocks are visited in order and the whole input lies within the span, so the result is the same as with a test per vector.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per aggregation; its own inlining budget keeps the operator and the aggregator inlined
        private static bool AggregateAnyAllVectorized512<T, TOperator, TAnyAll>(ReadOnlySpan<T> x)
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T>
        {
            Debug.Assert(Vector512.IsHardwareAccelerated && TOperator.Vectorizable && Vector512<T>.IsSupported);
            Debug.Assert(x.Length >= Vector512<T>.Count);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            nuint length = (uint)x.Length;
            nuint oneVectorFromEnd = length - (uint)Vector512<T>.Count;
            nuint i = 0;

            // Whole blocks: two accumulators, one decision per block.
            nuint blockLength = (uint)(AnyAllBlockVectors * Vector512<T>.Count);
            if (length >= blockLength)
            {
                nuint oneBlockFromEnd = length - blockLength;
                do
                {
                    Vector512<T> accumulator0 = Vector512<T>.Zero;
                    Vector512<T> accumulator1 = Vector512<T>.Zero;
                    nuint blockEnd = i + blockLength;
                    do
                    {
                        accumulator0 = TAnyAll.Accumulate(accumulator0, TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, i)));
                        accumulator1 = TAnyAll.Accumulate(accumulator1, TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, i + (uint)Vector512<T>.Count)));
                        i += (uint)(2 * Vector512<T>.Count);
                    }
                    while (i < blockEnd);

                    if (Vector512.AnyWhereAllBitsSet(accumulator0 | accumulator1))
                    {
                        return !TAnyAll.DefaultResult;
                    }
                }
                while (i <= oneBlockFromEnd);
            }

            // The remaining whole vectors, fewer than a block.
            if (i <= oneVectorFromEnd)
            {
                Vector512<T> accumulator = Vector512<T>.Zero;
                do
                {
                    accumulator = TAnyAll.Accumulate(accumulator, TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, i)));
                    i += (uint)Vector512<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                if (Vector512.AnyWhereAllBitsSet(accumulator))
                {
                    return !TAnyAll.DefaultResult;
                }
            }

            // Handle any remaining elements with a final vector.
            if (i != length &&
                TAnyAll.ShouldEarlyExit(TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, oneVectorFromEnd))))
            {
                return !TAnyAll.DefaultResult;
            }

            return TAnyAll.DefaultResult;
        }

        /// <summary>The 256-bit path of <see cref="AggregateAnyAll{T, TOperator, TAnyAll}"/>: the whole vectors in blocks, then one final vector that overlaps the last whole one.</summary>
        /// <remarks>
        /// Every block of up to <see cref="AnyAllBlockVectors"/> vectors is folded into two independent accumulators with no branch on the
        /// data, and the exit decision is made once per block, so a hit is detected after at most one block of extra reads.
        /// Blocks are visited in order and the whole input lies within the span, so the result is the same as with a test per vector.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per aggregation; its own inlining budget keeps the operator and the aggregator inlined
        private static bool AggregateAnyAllVectorized256<T, TOperator, TAnyAll>(ReadOnlySpan<T> x)
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T>
        {
            Debug.Assert(Vector256.IsHardwareAccelerated && TOperator.Vectorizable && Vector256<T>.IsSupported);
            Debug.Assert(x.Length >= Vector256<T>.Count);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            nuint length = (uint)x.Length;
            nuint oneVectorFromEnd = length - (uint)Vector256<T>.Count;
            nuint i = 0;

            // Whole blocks: two accumulators, one decision per block.
            nuint blockLength = (uint)(AnyAllBlockVectors * Vector256<T>.Count);
            if (length >= blockLength)
            {
                nuint oneBlockFromEnd = length - blockLength;
                do
                {
                    Vector256<T> accumulator0 = Vector256<T>.Zero;
                    Vector256<T> accumulator1 = Vector256<T>.Zero;
                    nuint blockEnd = i + blockLength;
                    do
                    {
                        accumulator0 = TAnyAll.Accumulate(accumulator0, TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, i)));
                        accumulator1 = TAnyAll.Accumulate(accumulator1, TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, i + (uint)Vector256<T>.Count)));
                        i += (uint)(2 * Vector256<T>.Count);
                    }
                    while (i < blockEnd);

                    if (Vector256.AnyWhereAllBitsSet(accumulator0 | accumulator1))
                    {
                        return !TAnyAll.DefaultResult;
                    }
                }
                while (i <= oneBlockFromEnd);
            }

            // The remaining whole vectors, fewer than a block.
            if (i <= oneVectorFromEnd)
            {
                Vector256<T> accumulator = Vector256<T>.Zero;
                do
                {
                    accumulator = TAnyAll.Accumulate(accumulator, TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, i)));
                    i += (uint)Vector256<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                if (Vector256.AnyWhereAllBitsSet(accumulator))
                {
                    return !TAnyAll.DefaultResult;
                }
            }

            // Handle any remaining elements with a final vector.
            if (i != length &&
                TAnyAll.ShouldEarlyExit(TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, oneVectorFromEnd))))
            {
                return !TAnyAll.DefaultResult;
            }

            return TAnyAll.DefaultResult;
        }

        /// <summary>The 128-bit path of <see cref="AggregateAnyAll{T, TOperator, TAnyAll}"/>: the whole vectors in blocks, then one final vector that overlaps the last whole one.</summary>
        /// <remarks>
        /// Every block of up to <see cref="AnyAllBlockVectors"/> vectors is folded into two independent accumulators with no branch on the
        /// data, and the exit decision is made once per block, so a hit is detected after at most one block of extra reads.
        /// Blocks are visited in order and the whole input lies within the span, so the result is the same as with a test per vector.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per aggregation; its own inlining budget keeps the operator and the aggregator inlined
        private static bool AggregateAnyAllVectorized128<T, TOperator, TAnyAll>(ReadOnlySpan<T> x)
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T>
        {
            Debug.Assert(Vector128.IsHardwareAccelerated && TOperator.Vectorizable && Vector128<T>.IsSupported);
            Debug.Assert(x.Length >= Vector128<T>.Count);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            nuint length = (uint)x.Length;
            nuint oneVectorFromEnd = length - (uint)Vector128<T>.Count;
            nuint i = 0;

            // Whole blocks: two accumulators, one decision per block.
            nuint blockLength = (uint)(AnyAllBlockVectors * Vector128<T>.Count);
            if (length >= blockLength)
            {
                nuint oneBlockFromEnd = length - blockLength;
                do
                {
                    Vector128<T> accumulator0 = Vector128<T>.Zero;
                    Vector128<T> accumulator1 = Vector128<T>.Zero;
                    nuint blockEnd = i + blockLength;
                    do
                    {
                        accumulator0 = TAnyAll.Accumulate(accumulator0, TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, i)));
                        accumulator1 = TAnyAll.Accumulate(accumulator1, TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, i + (uint)Vector128<T>.Count)));
                        i += (uint)(2 * Vector128<T>.Count);
                    }
                    while (i < blockEnd);

                    if (Vector128.AnyWhereAllBitsSet(accumulator0 | accumulator1))
                    {
                        return !TAnyAll.DefaultResult;
                    }
                }
                while (i <= oneBlockFromEnd);
            }

            // The remaining whole vectors, fewer than a block.
            if (i <= oneVectorFromEnd)
            {
                Vector128<T> accumulator = Vector128<T>.Zero;
                do
                {
                    accumulator = TAnyAll.Accumulate(accumulator, TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, i)));
                    i += (uint)Vector128<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                if (Vector128.AnyWhereAllBitsSet(accumulator))
                {
                    return !TAnyAll.DefaultResult;
                }
            }

            // Handle any remaining elements with a final vector.
            if (i != length &&
                TAnyAll.ShouldEarlyExit(TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, oneVectorFromEnd))))
            {
                return !TAnyAll.DefaultResult;
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
