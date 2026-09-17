// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics.Tensors
{
    public static unsafe partial class TensorPrimitives
    {
        /// <summary>Unary operator that produces a Boolean result for each element.</summary>
        /// <remarks>
        /// For vector-based methods, the Boolean result is either all-bits-set or zero.
        /// An operator may also have a threshold form (<see cref="HasThresholdForm"/>): its result is then a single unsigned comparison of a
        /// key derived from the element's bits against a constant, which lets <see cref="AggregateAnyAll{T, TOperator, TAnyAll}"/> fold a
        /// block of vectors with one unsigned minimum or maximum per vector and compare once per block.
        /// </remarks>
        private interface IBooleanUnaryOperator<T>
        {
            static abstract bool Vectorizable { get; }
            static abstract bool Invoke(T x);
            static abstract Vector128<T> Invoke(Vector128<T> x);
            static abstract Vector256<T> Invoke(Vector256<T> x);
            static abstract Vector512<T> Invoke(Vector512<T> x);

            /// <summary>
            /// Whether, reading the bits of the key and <see cref="ThresholdBits"/> as unsigned integers of the element size, <c>Invoke(x)</c>
            /// is <c>Key(x) &lt; ThresholdBits</c> when <see cref="TrueBelowThreshold"/> and <c>Key(x) &gt; ThresholdBits</c> otherwise.
            /// </summary>
            static virtual bool HasThresholdForm => false;

            /// <summary>Whether the operator is true for keys below the threshold rather than for keys above it.</summary>
            static virtual bool TrueBelowThreshold => throw new NotSupportedException();

            /// <summary>The threshold of the threshold form, as the bits of an unsigned integer of the element size.</summary>
            static virtual ulong ThresholdBits => throw new NotSupportedException();

            /// <summary>The key of the threshold form: the element's bits, transformed so that the operator is a single comparison of them.</summary>
            static virtual Vector128<T> Key(Vector128<T> x) => throw new NotSupportedException();
            /// <inheritdoc cref="Key(Vector128{T})"/>
            static virtual Vector256<T> Key(Vector256<T> x) => throw new NotSupportedException();
            /// <inheritdoc cref="Key(Vector128{T})"/>
            static virtual Vector512<T> Key(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>The bits of the positive infinity of <typeparamref name="T"/>, <see cref="float"/> or <see cref="double"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong PositiveInfinityBits<T>()
        {
            Debug.Assert(typeof(T) == typeof(float) || typeof(T) == typeof(double));
            return typeof(T) == typeof(float) ?
                BitConverter.SingleToUInt32Bits(float.PositiveInfinity) :
                BitConverter.DoubleToUInt64Bits(double.PositiveInfinity);
        }

        /// <summary>The bits of the smallest positive normal value of <typeparamref name="T"/>, <see cref="float"/> or <see cref="double"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong SmallestNormalBits<T>()
        {
            Debug.Assert(typeof(T) == typeof(float) || typeof(T) == typeof(double));
            return typeof(T) == typeof(float) ? 0x0080_0000u : 0x0010_0000_0000_0000ul;
        }

        /// <summary>The sign bit of <typeparamref name="T"/>, a primitive signed integer, <see cref="float"/> or <see cref="double"/>, as an unsigned integer of the element size.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong SignBit<T>() => 1ul << ((sizeof(T) * 8) - 1);

        /// <summary>Subtracts <paramref name="bits"/> from every element's bits, read as unsigned integers of the element size (<see cref="float"/> or <see cref="double"/>).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<T> SubtractBits<T>(Vector128<T> x, ulong bits)
        {
            Debug.Assert(typeof(T) == typeof(float) || typeof(T) == typeof(double));
            return typeof(T) == typeof(float) ?
                (x.AsUInt32() - Vector128.Create((uint)bits)).As<uint, T>() :
                (x.AsUInt64() - Vector128.Create(bits)).As<ulong, T>();
        }

        /// <summary>Subtracts <paramref name="bits"/> from every element's bits, read as unsigned integers of the element size (<see cref="float"/> or <see cref="double"/>).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<T> SubtractBits<T>(Vector256<T> x, ulong bits)
        {
            Debug.Assert(typeof(T) == typeof(float) || typeof(T) == typeof(double));
            return typeof(T) == typeof(float) ?
                (x.AsUInt32() - Vector256.Create((uint)bits)).As<uint, T>() :
                (x.AsUInt64() - Vector256.Create(bits)).As<ulong, T>();
        }

        /// <summary>Subtracts <paramref name="bits"/> from every element's bits, read as unsigned integers of the element size (<see cref="float"/> or <see cref="double"/>).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<T> SubtractBits<T>(Vector512<T> x, ulong bits)
        {
            Debug.Assert(typeof(T) == typeof(float) || typeof(T) == typeof(double));
            return typeof(T) == typeof(float) ?
                (x.AsUInt32() - Vector512.Create((uint)bits)).As<uint, T>() :
                (x.AsUInt64() - Vector512.Create(bits)).As<ulong, T>();
        }

        /// <summary>
        /// Combines the results of <see cref="IBooleanUnaryOperator{T}"/> into an Any/All decision. The vectorized loops of
        /// <see cref="AggregateAnyAll{T, TOperator, TAnyAll}"/> fold the per-vector results of a block of vectors into an accumulator
        /// and decide once per block whether the block contains an element which settles the result, in which case they exit with
        /// <c>!DefaultResult</c>. At 128 and 256 bits the accumulator is a result itself, the fold of the block's results with
        /// <c>Accumulate</c> starting from the result that equals <c>DefaultResult</c>, so <c>ShouldEarlyExit</c> decides. At 512 bits
        /// the operator's comparison leaves its result in a mask register, which a select consumes as it is (a masked blend) whereas a
        /// bitwise operation would first have to expand the mask into a vector, so the accumulator starts from all bits set and
        /// <c>ClearSettled</c> clears the lanes whose result settles the aggregation, which a zero lane then signals.
        /// </summary>
        private interface IAnyAllAggregator<T>
        {
            static abstract bool DefaultResult { get; }
            static abstract bool ShouldEarlyExit(bool result);
            static abstract bool ShouldEarlyExit(Vector128<T> result);
            static abstract bool ShouldEarlyExit(Vector256<T> result);
            static abstract bool ShouldEarlyExit(Vector512<T> result);

            /// <summary>Folds an operator result into the aggregation of the results folded so far, a result itself: the OR of Any results, the AND of All results.</summary>
            static abstract Vector128<T> Accumulate(Vector128<T> accumulator, Vector128<T> result);
            /// <inheritdoc cref="Accumulate(Vector128{T}, Vector128{T})"/>
            static abstract Vector256<T> Accumulate(Vector256<T> accumulator, Vector256<T> result);

            /// <summary>Clears the lanes of <paramref name="accumulator"/> whose lane of <paramref name="result"/> settles the aggregation, that is, for which <see cref="ShouldEarlyExit(Vector512{T})"/> would hold.</summary>
            static abstract Vector512<T> ClearSettled(Vector512<T> accumulator, Vector512<T> result);
        }

        private readonly struct AnyAggregator<T> : IAnyAllAggregator<T>
        {
            public static bool DefaultResult => false;

            public static bool ShouldEarlyExit(bool result) => result;

            public static bool ShouldEarlyExit(Vector128<T> result) => Vector128.AnyWhereAllBitsSet(result);
            public static bool ShouldEarlyExit(Vector256<T> result) => Vector256.AnyWhereAllBitsSet(result);
            public static bool ShouldEarlyExit(Vector512<T> result) => Vector512.AnyWhereAllBitsSet(result);

            public static Vector128<T> Accumulate(Vector128<T> accumulator, Vector128<T> result) => accumulator | result;
            public static Vector256<T> Accumulate(Vector256<T> accumulator, Vector256<T> result) => accumulator | result;

            // A lane where the operator was true is cleared: the select's constant is the zero vector, which costs no instruction,
            // and the JIT folds the selection of the other lanes into a zero-masking move under the inverted comparison.
            public static Vector512<T> ClearSettled(Vector512<T> accumulator, Vector512<T> result) => Vector512.ConditionalSelect(result, Vector512<T>.Zero, accumulator);
        }

        private readonly struct AllAggregator<T> : IAnyAllAggregator<T>
        {
            public static bool DefaultResult => true;

            public static bool ShouldEarlyExit(bool result) => !result;

            public static bool ShouldEarlyExit(Vector128<T> result) => AnyLaneZero(result);
            public static bool ShouldEarlyExit(Vector256<T> result) => AnyLaneZero(result);
            public static bool ShouldEarlyExit(Vector512<T> result) => AnyLaneZero(result);

            public static Vector128<T> Accumulate(Vector128<T> accumulator, Vector128<T> result) => accumulator & result;
            public static Vector256<T> Accumulate(Vector256<T> accumulator, Vector256<T> result) => accumulator & result;

            // A lane where the operator was false (its result is zero) is cleared (see AnyAggregator).
            public static Vector512<T> ClearSettled(Vector512<T> accumulator, Vector512<T> result) => Vector512.ConditionalSelect(result, accumulator, Vector512<T>.Zero);
        }

        /// <summary>Whether any lane of <paramref name="vector"/> is zero. For the floating-point types the lanes are compared as integers: a lane of an operator result or of an accumulator is either all bits set (a NaN) or zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AnyLaneZero<T>(Vector128<T> vector) =>
            typeof(T) == typeof(float) ? Vector128.EqualsAny(vector.AsUInt32(), Vector128<uint>.Zero) :
            typeof(T) == typeof(double) ? Vector128.EqualsAny(vector.AsUInt64(), Vector128<ulong>.Zero) :
            Vector128.EqualsAny(vector, Vector128<T>.Zero);

        /// <summary>Whether any lane of <paramref name="vector"/> is zero. For the floating-point types the lanes are compared as integers: a lane of an operator result or of an accumulator is either all bits set (a NaN) or zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AnyLaneZero<T>(Vector256<T> vector) =>
            typeof(T) == typeof(float) ? Vector256.EqualsAny(vector.AsUInt32(), Vector256<uint>.Zero) :
            typeof(T) == typeof(double) ? Vector256.EqualsAny(vector.AsUInt64(), Vector256<ulong>.Zero) :
            Vector256.EqualsAny(vector, Vector256<T>.Zero);

        /// <summary>Whether any lane of <paramref name="vector"/> is zero. For the floating-point types the lanes are compared as integers: a lane of an operator result or of an accumulator is either all bits set (a NaN) or zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AnyLaneZero<T>(Vector512<T> vector) =>
            typeof(T) == typeof(float) ? Vector512.EqualsAny(vector.AsUInt32(), Vector512<uint>.Zero) :
            typeof(T) == typeof(double) ? Vector512.EqualsAny(vector.AsUInt64(), Vector512<ulong>.Zero) :
            Vector512.EqualsAny(vector, Vector512<T>.Zero);

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

            if (TOperator.HasThresholdForm)
            {
                // The keys are folded as unsigned integers of the element size. Their 64-bit minimum and maximum are single instructions
                // only with AVX-512; elsewhere the operator's own comparison is cheaper.
                if (sizeof(T) == 1) return AggregateAnyAllThreshold<T, byte, TOperator, TAnyAll>(x);
                if (sizeof(T) == 2) return AggregateAnyAllThreshold<T, ushort, TOperator, TAnyAll>(x);
                if (sizeof(T) == 4) return AggregateAnyAllThreshold<T, uint, TOperator, TAnyAll>(x);
                if (sizeof(T) == 8 && Avx512F.VL.IsSupported) return AggregateAnyAllThreshold<T, ulong, TOperator, TAnyAll>(x);
            }

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

            return AggregateAnyAllScalar<T, TOperator, TAnyAll>(x);
        }

        /// <summary>The scalar path of <see cref="AggregateAnyAll{T, TOperator, TAnyAll}"/>, used when vectorization is not supported or the input is too small to vectorize.</summary>
        private static bool AggregateAnyAllScalar<T, TOperator, TAnyAll>(ReadOnlySpan<T> x)
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T>
        {
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
        /// The accumulators start from all bits set and have the lanes of the settling results cleared (see <see cref="IAnyAllAggregator{T}"/>).
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
                    Vector512<T> accumulator0 = Vector512<T>.AllBitsSet;
                    Vector512<T> accumulator1 = Vector512<T>.AllBitsSet;
                    nuint blockEnd = i + blockLength;
                    do
                    {
                        accumulator0 = TAnyAll.ClearSettled(accumulator0, TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, i)));
                        accumulator1 = TAnyAll.ClearSettled(accumulator1, TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, i + (uint)Vector512<T>.Count)));
                        i += (uint)(2 * Vector512<T>.Count);
                    }
                    while (i < blockEnd);

                    if (AnyLaneZero(accumulator0 & accumulator1))
                    {
                        return !TAnyAll.DefaultResult;
                    }
                }
                while (i <= oneBlockFromEnd);
            }

            // The remaining whole vectors, fewer than a block.
            if (i <= oneVectorFromEnd)
            {
                Vector512<T> accumulator = Vector512<T>.AllBitsSet;
                do
                {
                    accumulator = TAnyAll.ClearSettled(accumulator, TOperator.Invoke(Vector512.LoadUnsafe(ref xRef, i)));
                    i += (uint)Vector512<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                if (AnyLaneZero(accumulator))
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
        /// The accumulators are results themselves: the fold of the block's results, starting from the result that equals the default
        /// (see <see cref="IAnyAllAggregator{T}"/>).
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

            Vector256<T> defaultResult = TAnyAll.DefaultResult ? Vector256<T>.AllBitsSet : Vector256<T>.Zero;

            // Whole blocks: two accumulators, one decision per block.
            nuint blockLength = (uint)(AnyAllBlockVectors * Vector256<T>.Count);
            if (length >= blockLength)
            {
                nuint oneBlockFromEnd = length - blockLength;
                do
                {
                    Vector256<T> accumulator0 = defaultResult;
                    Vector256<T> accumulator1 = defaultResult;
                    nuint blockEnd = i + blockLength;
                    do
                    {
                        accumulator0 = TAnyAll.Accumulate(accumulator0, TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, i)));
                        accumulator1 = TAnyAll.Accumulate(accumulator1, TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, i + (uint)Vector256<T>.Count)));
                        i += (uint)(2 * Vector256<T>.Count);
                    }
                    while (i < blockEnd);

                    if (TAnyAll.ShouldEarlyExit(TAnyAll.Accumulate(accumulator0, accumulator1)))
                    {
                        return !TAnyAll.DefaultResult;
                    }
                }
                while (i <= oneBlockFromEnd);
            }

            // The remaining whole vectors, fewer than a block.
            if (i <= oneVectorFromEnd)
            {
                Vector256<T> accumulator = defaultResult;
                do
                {
                    accumulator = TAnyAll.Accumulate(accumulator, TOperator.Invoke(Vector256.LoadUnsafe(ref xRef, i)));
                    i += (uint)Vector256<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                if (TAnyAll.ShouldEarlyExit(accumulator))
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
        /// The accumulators are results themselves: the fold of the block's results, starting from the result that equals the default
        /// (see <see cref="IAnyAllAggregator{T}"/>).
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

            Vector128<T> defaultResult = TAnyAll.DefaultResult ? Vector128<T>.AllBitsSet : Vector128<T>.Zero;

            // Whole blocks: two accumulators, one decision per block.
            nuint blockLength = (uint)(AnyAllBlockVectors * Vector128<T>.Count);
            if (length >= blockLength)
            {
                nuint oneBlockFromEnd = length - blockLength;
                do
                {
                    Vector128<T> accumulator0 = defaultResult;
                    Vector128<T> accumulator1 = defaultResult;
                    nuint blockEnd = i + blockLength;
                    do
                    {
                        accumulator0 = TAnyAll.Accumulate(accumulator0, TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, i)));
                        accumulator1 = TAnyAll.Accumulate(accumulator1, TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, i + (uint)Vector128<T>.Count)));
                        i += (uint)(2 * Vector128<T>.Count);
                    }
                    while (i < blockEnd);

                    if (TAnyAll.ShouldEarlyExit(TAnyAll.Accumulate(accumulator0, accumulator1)))
                    {
                        return !TAnyAll.DefaultResult;
                    }
                }
                while (i <= oneBlockFromEnd);
            }

            // The remaining whole vectors, fewer than a block.
            if (i <= oneVectorFromEnd)
            {
                Vector128<T> accumulator = defaultResult;
                do
                {
                    accumulator = TAnyAll.Accumulate(accumulator, TOperator.Invoke(Vector128.LoadUnsafe(ref xRef, i)));
                    i += (uint)Vector128<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                if (TAnyAll.ShouldEarlyExit(accumulator))
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

        /// <summary>
        /// <see cref="AggregateAnyAll{T, TOperator, TAnyAll}"/> for an operator with a threshold form, whose keys are folded as
        /// <typeparamref name="TKey"/>, the unsigned integer of the element size.
        /// </summary>
        private static bool AggregateAnyAllThreshold<T, TKey, TOperator, TAnyAll>(ReadOnlySpan<T> x)
            where TKey : unmanaged, IBinaryInteger<TKey>
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T>
        {
            Debug.Assert(TOperator.HasThresholdForm);
            Debug.Assert(sizeof(TKey) == sizeof(T));
            Debug.Assert(!x.IsEmpty);

            // The fold is chosen by type rather than by a branch on the direction so that the loops contain a single use of each key,
            // which the JIT then folds into the fold instruction's memory operand.
            return ThresholdFoldsMax<T, TOperator, TAnyAll>() ?
                AggregateAnyAllThreshold<T, TKey, MaxOperator<TKey>, TOperator, TAnyAll>(x) :
                AggregateAnyAllThreshold<T, TKey, MinOperator<TKey>, TOperator, TAnyAll>(x);
        }

        /// <summary>
        /// Whether the threshold form of <typeparamref name="TOperator"/> aggregated by <typeparamref name="TAnyAll"/> folds the keys of a block
        /// with their maximum rather than their minimum. Any looks for an element for which the operator holds and All for one for which it
        /// does not: with the operator true below the threshold, those are a key below it (the minimum decides) and a key at or above it (the
        /// maximum decides); with the operator true above the threshold, it is the other way round.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ThresholdFoldsMax<T, TOperator, TAnyAll>()
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T> =>
            TOperator.TrueBelowThreshold == TAnyAll.DefaultResult;

        /// <summary>
        /// <see cref="AggregateAnyAllThreshold{T, TKey, TOperator, TAnyAll}"/> with the fold of the keys, <typeparamref name="TFold"/>, chosen:
        /// <see cref="MaxOperator{T}"/> or <see cref="MinOperator{T}"/> over <typeparamref name="TKey"/> (see <see cref="ThresholdFoldsMax{T, TOperator, TAnyAll}"/>).
        /// </summary>
        private static bool AggregateAnyAllThreshold<T, TKey, TFold, TOperator, TAnyAll>(ReadOnlySpan<T> x)
            where TKey : unmanaged, IBinaryInteger<TKey>
            where TFold : struct, IBinaryOperator<TKey>
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T>
        {
            Debug.Assert(typeof(TFold) == (ThresholdFoldsMax<T, TOperator, TAnyAll>() ? typeof(MaxOperator<TKey>) : typeof(MinOperator<TKey>)));

            if (Vector512.IsHardwareAccelerated && TOperator.Vectorizable && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
            {
                return AggregateAnyAllThreshold512<T, TKey, TFold, TOperator, TAnyAll>(x);
            }

            if (Vector256.IsHardwareAccelerated && TOperator.Vectorizable && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                return AggregateAnyAllThreshold256<T, TKey, TFold, TOperator, TAnyAll>(x);
            }

            if (Vector128.IsHardwareAccelerated && TOperator.Vectorizable && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                return AggregateAnyAllThreshold128<T, TKey, TFold, TOperator, TAnyAll>(x);
            }

            return AggregateAnyAllScalar<T, TOperator, TAnyAll>(x);
        }

        /// <summary>The 512-bit path of <see cref="AggregateAnyAllThreshold{T, TKey, TFold, TOperator, TAnyAll}"/>: the whole vectors in blocks, then one final vector that overlaps the last whole one.</summary>
        /// <remarks>
        /// The shape of <see cref="AggregateAnyAllVectorized512{T, TOperator, TAnyAll}"/>, except that a block is folded with the unsigned
        /// minimum or maximum of the elements' keys, one instruction per vector with no comparison, and the fold is compared against the
        /// threshold once per block.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per aggregation; its own inlining budget keeps the operator inlined
        private static bool AggregateAnyAllThreshold512<T, TKey, TFold, TOperator, TAnyAll>(ReadOnlySpan<T> x)
            where TKey : unmanaged, IBinaryInteger<TKey>
            where TFold : struct, IBinaryOperator<TKey>
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T>
        {
            Debug.Assert(Vector512.IsHardwareAccelerated && TOperator.Vectorizable && Vector512<T>.IsSupported);
            Debug.Assert(x.Length >= Vector512<T>.Count);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            nuint length = (uint)x.Length;
            nuint oneVectorFromEnd = length - (uint)Vector512<T>.Count;
            nuint i = 0;

            Vector512<TKey> identity = ThresholdFoldsMax<T, TOperator, TAnyAll>() ? Vector512<TKey>.Zero : Vector512<TKey>.AllBitsSet;
            Vector512<TKey> threshold = Vector512.Create(TKey.CreateTruncating(TOperator.ThresholdBits));

            // Whole blocks: two accumulators, one decision per block.
            nuint blockLength = (uint)(AnyAllBlockVectors * Vector512<T>.Count);
            if (length >= blockLength)
            {
                nuint oneBlockFromEnd = length - blockLength;
                do
                {
                    Vector512<TKey> accumulator0 = identity;
                    Vector512<TKey> accumulator1 = identity;
                    nuint blockEnd = i + blockLength;
                    do
                    {
                        accumulator0 = TFold.Invoke(accumulator0, TOperator.Key(Vector512.LoadUnsafe(ref xRef, i)).As<T, TKey>());
                        accumulator1 = TFold.Invoke(accumulator1, TOperator.Key(Vector512.LoadUnsafe(ref xRef, i + (uint)Vector512<T>.Count)).As<T, TKey>());
                        i += (uint)(2 * Vector512<T>.Count);
                    }
                    while (i < blockEnd);

                    if (Settles(TFold.Invoke(accumulator0, accumulator1), threshold))
                    {
                        return !TAnyAll.DefaultResult;
                    }
                }
                while (i <= oneBlockFromEnd);
            }

            // The remaining whole vectors, fewer than a block.
            if (i <= oneVectorFromEnd)
            {
                Vector512<TKey> accumulator = identity;
                do
                {
                    accumulator = TFold.Invoke(accumulator, TOperator.Key(Vector512.LoadUnsafe(ref xRef, i)).As<T, TKey>());
                    i += (uint)Vector512<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                if (Settles(accumulator, threshold))
                {
                    return !TAnyAll.DefaultResult;
                }
            }

            // Handle any remaining elements with a final vector.
            if (i != length &&
                Settles(TOperator.Key(Vector512.LoadUnsafe(ref xRef, oneVectorFromEnd)).As<T, TKey>(), threshold))
            {
                return !TAnyAll.DefaultResult;
            }

            return TAnyAll.DefaultResult;

            // Whether the keys folded into the accumulator include one that settles the result: for Any a key on the operator's side of
            // the threshold, for All a key on the other side (or on the threshold).
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool Settles(Vector512<TKey> accumulator, Vector512<TKey> threshold) =>
                TAnyAll.DefaultResult ?
                    (TOperator.TrueBelowThreshold ? Vector512.GreaterThanOrEqualAny(accumulator, threshold) : Vector512.LessThanOrEqualAny(accumulator, threshold)) :
                    (TOperator.TrueBelowThreshold ? Vector512.LessThanAny(accumulator, threshold) : Vector512.GreaterThanAny(accumulator, threshold));
        }

        /// <summary>The 256-bit path of <see cref="AggregateAnyAllThreshold{T, TKey, TFold, TOperator, TAnyAll}"/>: the whole vectors in blocks, then one final vector that overlaps the last whole one.</summary>
        /// <remarks>
        /// The shape of <see cref="AggregateAnyAllVectorized256{T, TOperator, TAnyAll}"/>, except that a block is folded with the unsigned
        /// minimum or maximum of the elements' keys, one instruction per vector with no comparison, and the fold is compared against the
        /// threshold once per block.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per aggregation; its own inlining budget keeps the operator inlined
        private static bool AggregateAnyAllThreshold256<T, TKey, TFold, TOperator, TAnyAll>(ReadOnlySpan<T> x)
            where TKey : unmanaged, IBinaryInteger<TKey>
            where TFold : struct, IBinaryOperator<TKey>
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T>
        {
            Debug.Assert(Vector256.IsHardwareAccelerated && TOperator.Vectorizable && Vector256<T>.IsSupported);
            Debug.Assert(x.Length >= Vector256<T>.Count);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            nuint length = (uint)x.Length;
            nuint oneVectorFromEnd = length - (uint)Vector256<T>.Count;
            nuint i = 0;

            Vector256<TKey> identity = ThresholdFoldsMax<T, TOperator, TAnyAll>() ? Vector256<TKey>.Zero : Vector256<TKey>.AllBitsSet;
            Vector256<TKey> threshold = Vector256.Create(TKey.CreateTruncating(TOperator.ThresholdBits));

            // Whole blocks: two accumulators, one decision per block.
            nuint blockLength = (uint)(AnyAllBlockVectors * Vector256<T>.Count);
            if (length >= blockLength)
            {
                nuint oneBlockFromEnd = length - blockLength;
                do
                {
                    Vector256<TKey> accumulator0 = identity;
                    Vector256<TKey> accumulator1 = identity;
                    nuint blockEnd = i + blockLength;
                    do
                    {
                        accumulator0 = TFold.Invoke(accumulator0, TOperator.Key(Vector256.LoadUnsafe(ref xRef, i)).As<T, TKey>());
                        accumulator1 = TFold.Invoke(accumulator1, TOperator.Key(Vector256.LoadUnsafe(ref xRef, i + (uint)Vector256<T>.Count)).As<T, TKey>());
                        i += (uint)(2 * Vector256<T>.Count);
                    }
                    while (i < blockEnd);

                    if (Settles(TFold.Invoke(accumulator0, accumulator1), threshold))
                    {
                        return !TAnyAll.DefaultResult;
                    }
                }
                while (i <= oneBlockFromEnd);
            }

            // The remaining whole vectors, fewer than a block.
            if (i <= oneVectorFromEnd)
            {
                Vector256<TKey> accumulator = identity;
                do
                {
                    accumulator = TFold.Invoke(accumulator, TOperator.Key(Vector256.LoadUnsafe(ref xRef, i)).As<T, TKey>());
                    i += (uint)Vector256<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                if (Settles(accumulator, threshold))
                {
                    return !TAnyAll.DefaultResult;
                }
            }

            // Handle any remaining elements with a final vector.
            if (i != length &&
                Settles(TOperator.Key(Vector256.LoadUnsafe(ref xRef, oneVectorFromEnd)).As<T, TKey>(), threshold))
            {
                return !TAnyAll.DefaultResult;
            }

            return TAnyAll.DefaultResult;

            // Whether the keys folded into the accumulator include one that settles the result: for Any a key on the operator's side of
            // the threshold, for All a key on the other side (or on the threshold).
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool Settles(Vector256<TKey> accumulator, Vector256<TKey> threshold) =>
                TAnyAll.DefaultResult ?
                    (TOperator.TrueBelowThreshold ? Vector256.GreaterThanOrEqualAny(accumulator, threshold) : Vector256.LessThanOrEqualAny(accumulator, threshold)) :
                    (TOperator.TrueBelowThreshold ? Vector256.LessThanAny(accumulator, threshold) : Vector256.GreaterThanAny(accumulator, threshold));
        }

        /// <summary>The 128-bit path of <see cref="AggregateAnyAllThreshold{T, TKey, TFold, TOperator, TAnyAll}"/>: the whole vectors in blocks, then one final vector that overlaps the last whole one.</summary>
        /// <remarks>
        /// The shape of <see cref="AggregateAnyAllVectorized128{T, TOperator, TAnyAll}"/>, except that a block is folded with the unsigned
        /// minimum or maximum of the elements' keys, one instruction per vector with no comparison, and the fold is compared against the
        /// threshold once per block.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per aggregation; its own inlining budget keeps the operator inlined
        private static bool AggregateAnyAllThreshold128<T, TKey, TFold, TOperator, TAnyAll>(ReadOnlySpan<T> x)
            where TKey : unmanaged, IBinaryInteger<TKey>
            where TFold : struct, IBinaryOperator<TKey>
            where TOperator : struct, IBooleanUnaryOperator<T>
            where TAnyAll : struct, IAnyAllAggregator<T>
        {
            Debug.Assert(Vector128.IsHardwareAccelerated && TOperator.Vectorizable && Vector128<T>.IsSupported);
            Debug.Assert(x.Length >= Vector128<T>.Count);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            nuint length = (uint)x.Length;
            nuint oneVectorFromEnd = length - (uint)Vector128<T>.Count;
            nuint i = 0;

            Vector128<TKey> identity = ThresholdFoldsMax<T, TOperator, TAnyAll>() ? Vector128<TKey>.Zero : Vector128<TKey>.AllBitsSet;
            Vector128<TKey> threshold = Vector128.Create(TKey.CreateTruncating(TOperator.ThresholdBits));

            // Whole blocks: two accumulators, one decision per block.
            nuint blockLength = (uint)(AnyAllBlockVectors * Vector128<T>.Count);
            if (length >= blockLength)
            {
                nuint oneBlockFromEnd = length - blockLength;
                do
                {
                    Vector128<TKey> accumulator0 = identity;
                    Vector128<TKey> accumulator1 = identity;
                    nuint blockEnd = i + blockLength;
                    do
                    {
                        accumulator0 = TFold.Invoke(accumulator0, TOperator.Key(Vector128.LoadUnsafe(ref xRef, i)).As<T, TKey>());
                        accumulator1 = TFold.Invoke(accumulator1, TOperator.Key(Vector128.LoadUnsafe(ref xRef, i + (uint)Vector128<T>.Count)).As<T, TKey>());
                        i += (uint)(2 * Vector128<T>.Count);
                    }
                    while (i < blockEnd);

                    if (Settles(TFold.Invoke(accumulator0, accumulator1), threshold))
                    {
                        return !TAnyAll.DefaultResult;
                    }
                }
                while (i <= oneBlockFromEnd);
            }

            // The remaining whole vectors, fewer than a block.
            if (i <= oneVectorFromEnd)
            {
                Vector128<TKey> accumulator = identity;
                do
                {
                    accumulator = TFold.Invoke(accumulator, TOperator.Key(Vector128.LoadUnsafe(ref xRef, i)).As<T, TKey>());
                    i += (uint)Vector128<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                if (Settles(accumulator, threshold))
                {
                    return !TAnyAll.DefaultResult;
                }
            }

            // Handle any remaining elements with a final vector.
            if (i != length &&
                Settles(TOperator.Key(Vector128.LoadUnsafe(ref xRef, oneVectorFromEnd)).As<T, TKey>(), threshold))
            {
                return !TAnyAll.DefaultResult;
            }

            return TAnyAll.DefaultResult;

            // Whether the keys folded into the accumulator include one that settles the result: for Any a key on the operator's side of
            // the threshold, for All a key on the other side (or on the threshold).
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool Settles(Vector128<TKey> accumulator, Vector128<TKey> threshold) =>
                TAnyAll.DefaultResult ?
                    (TOperator.TrueBelowThreshold ? Vector128.GreaterThanOrEqualAny(accumulator, threshold) : Vector128.LessThanOrEqualAny(accumulator, threshold)) :
                    (TOperator.TrueBelowThreshold ? Vector128.LessThanAny(accumulator, threshold) : Vector128.GreaterThanAny(accumulator, threshold));
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
