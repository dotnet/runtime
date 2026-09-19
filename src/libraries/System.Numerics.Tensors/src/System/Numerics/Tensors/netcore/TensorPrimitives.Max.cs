// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The maximum element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If any value equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        /// is present, the first is returned. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Max<T>(ReadOnlySpan<T> x)
            where T : INumber<T>
        {
            if (typeof(T) == typeof(Half) && TryMinMaxHalfAsInt16<T, MaxOperator<float>>(x, out T result))
            {
                return result;
            }

            return MinMaxCore<T, MaxOperator<T>>(x);
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Max(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If either value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// that value is stored as the result. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Max<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : INumber<T>
        {
            if (typeof(T) == typeof(Half) && TryAggregateInvokeHalfAsInt16<T, MaxOperator<float>>(x, y, destination))
            {
                return;
            }

            InvokeSpanSpanIntoSpan<T, MaxOperator<T>>(x, y, destination);
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Max(<paramref name="x" />[i], <paramref name="y" />)</c>.
        /// </para>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If either value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// that value is stored as the result. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Max<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : INumber<T>
        {
            if (typeof(T) == typeof(Half) && TryAggregateInvokeHalfAsInt16<T, MaxOperator<float>>(x, y, destination))
            {
                return;
            }

            InvokeSpanScalarIntoSpan<T, MaxOperator<T>>(x, y, destination);
        }

        /// <summary>Max(x, y)</summary>
        internal readonly struct MaxOperator<T> : IAggregationOperator<T>
             where T : INumber<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.Max(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => Vector128.Max(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => Vector256.Max(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => Vector512.Max(x, y);

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
        }

        /// <summary>Vectors per block in the vectorized paths of <see cref="MinMaxCore{T, TMinMaxOperator}"/>.</summary>
        private const int MinMaxBlockVectors = 32;

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
                return MinMaxVectorized512<T, TMinMaxOperator>(x);
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                return MinMaxVectorized256<T, TMinMaxOperator>(x);
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                return MinMaxVectorized128<T, TMinMaxOperator>(x);
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

        /// <summary>
        /// Whether <typeparamref name="TMinMaxOperator"/> is <see cref="MinOperator{T}"/> or <see cref="MaxOperator{T}"/> over <see cref="float"/>
        /// or <see cref="double"/>: the reductions whose blocks are reduced with the native vector minimum/maximum
        /// (<see cref="Vector512.MinNative{T}(Vector512{T}, Vector512{T})"/>, one instruction) plus masks that restore the IEEE 754:2019 rules once per block,
        /// rather than with the operator itself (<see cref="Vector512.Min{T}(Vector512{T}, Vector512{T})"/>, several dependent instructions).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNativeMinMax<T, TMinMaxOperator>() =>
            (typeof(T) == typeof(float) && (typeof(TMinMaxOperator) == typeof(MinOperator<float>) || typeof(TMinMaxOperator) == typeof(MaxOperator<float>))) ||
            (typeof(T) == typeof(double) && (typeof(TMinMaxOperator) == typeof(MinOperator<double>) || typeof(TMinMaxOperator) == typeof(MaxOperator<double>)));

        /// <summary>Whether <typeparamref name="TMinMaxOperator"/> is a minimum rather than a maximum. Only meaningful when <see cref="IsNativeMinMax{T, TMinMaxOperator}"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNativeMin<TMinMaxOperator>() =>
            typeof(TMinMaxOperator) == typeof(MinOperator<float>) || typeof(TMinMaxOperator) == typeof(MinOperator<double>);

        /// <summary>
        /// Restores the IEEE 754:2019 `minimum`/`maximum` result of a block reduced with the native vector minimum/maximum, which does not
        /// order the signed zeros. The native operation never returns a value below (above) every input, so if the block minimum (maximum)
        /// compares equal to zero and the block holds no NaN, no element is negative (positive), and an element whose sign bit is set (clear)
        /// can only be -0 (+0).
        /// </summary>
        /// <param name="blockResult">The result of the native reduction of a block that contains no NaN.</param>
        /// <param name="anyOppositeSignBit">For a minimum, whether any element of the block has its sign bit set; for a maximum, whether any has it clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T FixUpNativeSignedZero<T, TMinMaxOperator>(T blockResult, bool anyOppositeSignBit)
            where T : INumberBase<T>
        {
            Debug.Assert(IsNativeMinMax<T, TMinMaxOperator>());
            Debug.Assert(!T.IsNaN(blockResult));

            if (anyOppositeSignBit && blockResult == T.Zero)
            {
                return IsNativeMin<TMinMaxOperator>() ? -T.Zero : T.Zero;
            }

            return blockResult;
        }

        /// <summary>Min(x, y) with no NaN or signed-zero guarantees: the hardware minimum where there is one. Used for the horizontal reductions of the native blocks.</summary>
        private readonly struct MinNativeOperator<T> : IBinaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y) => Vector128.MinNative(Vector128.CreateScalar(x), Vector128.CreateScalar(y)).ToScalar();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => Vector128.MinNative(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => Vector256.MinNative(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => Vector512.MinNative(x, y);
        }

        /// <summary>Max(x, y) with no NaN or signed-zero guarantees: the hardware maximum where there is one. Used for the horizontal reductions of the native blocks.</summary>
        private readonly struct MaxNativeOperator<T> : IBinaryOperator<T>
            where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y) => Vector128.MaxNative(Vector128.CreateScalar(x), Vector128.CreateScalar(y)).ToScalar();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => Vector128.MaxNative(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => Vector256.MaxNative(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => Vector512.MaxNative(x, y);
        }

        /// <summary>The 512-bit path of <see cref="MinMaxCore{T, TMinMaxOperator}"/>: a block reduction of the whole vectors followed by one final vector that overlaps the last whole one.</summary>
        /// <remarks>
        /// Every block of up to <see cref="MinMaxBlockVectors"/> vectors is reduced with two independent accumulators and a single NaN decision
        /// (the OR of the elements' NaN masks), so the hot loop contains no branch on the data. Blocks are visited in order and the running
        /// result is only combined after a block has been checked, so the first NaN of the input is the one returned.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per reduction; its own inlining budget keeps the block reduction and the horizontal aggregates inlined
        private static T MinMaxVectorized512<T, TMinMaxOperator>(ReadOnlySpan<T> x)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            Debug.Assert(Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported);
            Debug.Assert(x.Length >= Vector512<T>.Count);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            int length = x.Length;
            int blockSize = MinMaxBlockVectors * Vector512<T>.Count;
            int wholeVectorsLength = length - (length % Vector512<T>.Count);

            // Reduce the whole vectors block by block. Starting from the first element is harmless: it is part of the first block,
            // and combining an element with a result that already accounts for it does not change that result.
            T result = xRef;
            for (int i = 0; i < wholeVectorsLength; i += blockSize)
            {
                int blockLength = Math.Min(blockSize, wholeVectorsLength - i);
                T blockResult;
                bool anyNaN;

                if (IsNativeMinMax<T, TMinMaxOperator>())
                {
                    blockResult = BlockReduceNative512<T, TMinMaxOperator>(ref Unsafe.Add(ref xRef, i), blockLength, out anyNaN, out bool anyOppositeSignBit);
                    if (anyNaN)
                    {
                        return FirstNaN512(ref Unsafe.Add(ref xRef, i), blockLength);
                    }

                    blockResult = FixUpNativeSignedZero<T, TMinMaxOperator>(blockResult, anyOppositeSignBit);
                }
                else
                {
                    blockResult = BlockReduce512<T, TMinMaxOperator>(ref Unsafe.Add(ref xRef, i), blockLength, out anyNaN);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        if (anyNaN)
                        {
                            return FirstNaN512(ref Unsafe.Add(ref xRef, i), blockLength);
                        }
                    }
                }

                result = TMinMaxOperator.Invoke(result, blockResult);
            }

            // If any elements remain, handle them in one final vector. Its NaN check does not change which NaN is returned:
            // a NaN among the elements it revisits would already have been returned by their block.
            if (wholeVectorsLength != length)
            {
                Vector512<T> last = Vector512.LoadUnsafe(ref xRef, (uint)(length - Vector512<T>.Count));

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector512<T> nanMask = Vector512.IsNaN(last);
                    if (nanMask != Vector512<T>.Zero)
                    {
                        return last.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                result = TMinMaxOperator.Invoke(result, TMinMaxOperator.Invoke(last));
            }

            return result;
        }

        /// <summary>Reduces the <paramref name="length"/> elements at <paramref name="xRef"/>, a whole number of 512-bit vectors, with <typeparamref name="TMinMaxOperator"/>.</summary>
        /// <param name="xRef">The first element of the block.</param>
        /// <param name="length">The number of elements in the block, a positive multiple of <see cref="Vector512{T}.Count"/>.</param>
        /// <param name="anyNaN">
        /// For <see cref="float"/> and <see cref="double"/>, whether any element is NaN, in which case the returned value is meaningless.
        /// It is gathered from the elements rather than from the result because the Number operators discard NaN.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T BlockReduce512<T, TMinMaxOperator>(ref T xRef, int length, out bool anyNaN)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            Debug.Assert(length >= Vector512<T>.Count && length % Vector512<T>.Count == 0);

            Vector512<T> acc1 = Vector512.LoadUnsafe(ref xRef);
            Vector512<T> nan = Vector512.IsNaN(acc1);
            int i = Vector512<T>.Count;

            if (length >= 2 * Vector512<T>.Count)
            {
                Vector512<T> acc2 = Vector512.LoadUnsafe(ref xRef, (uint)i);
                nan |= Vector512.IsNaN(acc2);
                i += Vector512<T>.Count;

                int twoVectorsFromEnd = length - (2 * Vector512<T>.Count);
                while (i <= twoVectorsFromEnd)
                {
                    Vector512<T> current1 = Vector512.LoadUnsafe(ref xRef, (uint)i);
                    Vector512<T> current2 = Vector512.LoadUnsafe(ref xRef, (uint)(i + Vector512<T>.Count));
                    acc1 = TMinMaxOperator.Invoke(acc1, current1);
                    acc2 = TMinMaxOperator.Invoke(acc2, current2);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        nan |= Vector512.IsNaN(current1) | Vector512.IsNaN(current2);
                    }

                    i += 2 * Vector512<T>.Count;
                }

                acc1 = TMinMaxOperator.Invoke(acc1, acc2);
            }

            if (i != length)
            {
                Vector512<T> current = Vector512.LoadUnsafe(ref xRef, (uint)i);
                acc1 = TMinMaxOperator.Invoke(acc1, current);
                nan |= Vector512.IsNaN(current);
            }

            anyNaN = (typeof(T) == typeof(float) || typeof(T) == typeof(double)) && nan != Vector512<T>.Zero;
            return TMinMaxOperator.Invoke(acc1);
        }

        /// <summary>
        /// Reduces the <paramref name="length"/> elements at <paramref name="xRef"/>, a whole number of 512-bit vectors, with the native vector
        /// minimum/maximum, gathering alongside the two facts that let the caller restore the IEEE 754:2019 result (see <see cref="FixUpNativeSignedZero{T, TMinMaxOperator}"/>).
        /// </summary>
        /// <param name="xRef">The first element of the block.</param>
        /// <param name="length">The number of elements in the block, a positive multiple of <see cref="Vector512{T}.Count"/>.</param>
        /// <param name="anyNaN">Whether any element is NaN, in which case the returned value is meaningless.</param>
        /// <param name="anyOppositeSignBit">For a minimum, whether any element has its sign bit set; for a maximum, whether any has it clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T BlockReduceNative512<T, TMinMaxOperator>(ref T xRef, int length, out bool anyNaN, out bool anyOppositeSignBit)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            Debug.Assert(IsNativeMinMax<T, TMinMaxOperator>());
            Debug.Assert(length >= Vector512<T>.Count && length % Vector512<T>.Count == 0);

            // For a minimum, OR the elements together so that any set sign bit survives; for a maximum, AND them so that any clear one does.
            Vector512<T> acc1 = Vector512.LoadUnsafe(ref xRef);
            Vector512<T> nan = Vector512.IsNaN(acc1);
            Vector512<T> signBits = acc1;
            int i = Vector512<T>.Count;

            if (length >= 2 * Vector512<T>.Count)
            {
                Vector512<T> acc2 = Vector512.LoadUnsafe(ref xRef, (uint)i);
                nan |= Vector512.IsNaN(acc2);
                signBits = IsNativeMin<TMinMaxOperator>() ? signBits | acc2 : signBits & acc2;
                i += Vector512<T>.Count;

                int twoVectorsFromEnd = length - (2 * Vector512<T>.Count);
                while (i <= twoVectorsFromEnd)
                {
                    Vector512<T> current1 = Vector512.LoadUnsafe(ref xRef, (uint)i);
                    Vector512<T> current2 = Vector512.LoadUnsafe(ref xRef, (uint)(i + Vector512<T>.Count));
                    if (IsNativeMin<TMinMaxOperator>())
                    {
                        acc1 = Vector512.MinNative(acc1, current1);
                        acc2 = Vector512.MinNative(acc2, current2);
                        signBits |= current1 | current2;
                    }
                    else
                    {
                        acc1 = Vector512.MaxNative(acc1, current1);
                        acc2 = Vector512.MaxNative(acc2, current2);
                        signBits &= current1 & current2;
                    }

                    nan |= Vector512.IsNaN(current1) | Vector512.IsNaN(current2);
                    i += 2 * Vector512<T>.Count;
                }

                acc1 = IsNativeMin<TMinMaxOperator>() ? Vector512.MinNative(acc1, acc2) : Vector512.MaxNative(acc1, acc2);
            }

            if (i != length)
            {
                Vector512<T> current = Vector512.LoadUnsafe(ref xRef, (uint)i);
                if (IsNativeMin<TMinMaxOperator>())
                {
                    acc1 = Vector512.MinNative(acc1, current);
                    signBits |= current;
                }
                else
                {
                    acc1 = Vector512.MaxNative(acc1, current);
                    signBits &= current;
                }

                nan |= Vector512.IsNaN(current);
            }

            anyNaN = nan != Vector512<T>.Zero;
            anyOppositeSignBit = IsNativeMin<TMinMaxOperator>() ?
                signBits.ExtractMostSignificantBits() != 0 :
                (~signBits).ExtractMostSignificantBits() != 0;

            return IsNativeMin<TMinMaxOperator>() ?
                HorizontalAggregate<T, MinNativeOperator<T>>(acc1) :
                HorizontalAggregate<T, MaxNativeOperator<T>>(acc1);
        }

        /// <summary>Returns the first NaN among the <paramref name="length"/> elements at <paramref name="xRef"/>, a whole number of 512-bit vectors that contains one.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // cold: called at most once per reduction; keeps the caller within the inlining budget
        private static T FirstNaN512<T>(ref T xRef, int length)
            where T : INumberBase<T>
        {
            Debug.Assert(typeof(T) == typeof(float) || typeof(T) == typeof(double));
            Debug.Assert(length >= Vector512<T>.Count && length % Vector512<T>.Count == 0);

            for (int i = 0; i < length; i += Vector512<T>.Count)
            {
                Vector512<T> current = Vector512.LoadUnsafe(ref xRef, (uint)i);
                Vector512<T> nanMask = Vector512.IsNaN(current);
                if (nanMask != Vector512<T>.Zero)
                {
                    return current.GetElement(IndexOfFirstMatch(nanMask));
                }
            }

            Debug.Fail("The block must contain a NaN.");
            return default!;
        }

        /// <summary>The 256-bit path of <see cref="MinMaxCore{T, TMinMaxOperator}"/>: a block reduction of the whole vectors followed by one final vector that overlaps the last whole one.</summary>
        /// <remarks>
        /// Every block of up to <see cref="MinMaxBlockVectors"/> vectors is reduced with two independent accumulators and a single NaN decision
        /// (the OR of the elements' NaN masks), so the hot loop contains no branch on the data. Blocks are visited in order and the running
        /// result is only combined after a block has been checked, so the first NaN of the input is the one returned.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per reduction; its own inlining budget keeps the block reduction and the horizontal aggregates inlined
        private static T MinMaxVectorized256<T, TMinMaxOperator>(ReadOnlySpan<T> x)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            Debug.Assert(Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported);
            Debug.Assert(x.Length >= Vector256<T>.Count);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            int length = x.Length;
            int blockSize = MinMaxBlockVectors * Vector256<T>.Count;
            int wholeVectorsLength = length - (length % Vector256<T>.Count);

            // Reduce the whole vectors block by block. Starting from the first element is harmless: it is part of the first block,
            // and combining an element with a result that already accounts for it does not change that result.
            T result = xRef;
            for (int i = 0; i < wholeVectorsLength; i += blockSize)
            {
                int blockLength = Math.Min(blockSize, wholeVectorsLength - i);
                T blockResult;
                bool anyNaN;

                if (IsNativeMinMax<T, TMinMaxOperator>())
                {
                    blockResult = BlockReduceNative256<T, TMinMaxOperator>(ref Unsafe.Add(ref xRef, i), blockLength, out anyNaN, out bool anyOppositeSignBit);
                    if (anyNaN)
                    {
                        return FirstNaN256(ref Unsafe.Add(ref xRef, i), blockLength);
                    }

                    blockResult = FixUpNativeSignedZero<T, TMinMaxOperator>(blockResult, anyOppositeSignBit);
                }
                else
                {
                    blockResult = BlockReduce256<T, TMinMaxOperator>(ref Unsafe.Add(ref xRef, i), blockLength, out anyNaN);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        if (anyNaN)
                        {
                            return FirstNaN256(ref Unsafe.Add(ref xRef, i), blockLength);
                        }
                    }
                }

                result = TMinMaxOperator.Invoke(result, blockResult);
            }

            // If any elements remain, handle them in one final vector. Its NaN check does not change which NaN is returned:
            // a NaN among the elements it revisits would already have been returned by their block.
            if (wholeVectorsLength != length)
            {
                Vector256<T> last = Vector256.LoadUnsafe(ref xRef, (uint)(length - Vector256<T>.Count));

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector256<T> nanMask = Vector256.IsNaN(last);
                    if (nanMask != Vector256<T>.Zero)
                    {
                        return last.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                result = TMinMaxOperator.Invoke(result, TMinMaxOperator.Invoke(last));
            }

            return result;
        }

        /// <summary>Reduces the <paramref name="length"/> elements at <paramref name="xRef"/>, a whole number of 256-bit vectors, with <typeparamref name="TMinMaxOperator"/>.</summary>
        /// <param name="xRef">The first element of the block.</param>
        /// <param name="length">The number of elements in the block, a positive multiple of <see cref="Vector256{T}.Count"/>.</param>
        /// <param name="anyNaN">
        /// For <see cref="float"/> and <see cref="double"/>, whether any element is NaN, in which case the returned value is meaningless.
        /// It is gathered from the elements rather than from the result because the Number operators discard NaN.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T BlockReduce256<T, TMinMaxOperator>(ref T xRef, int length, out bool anyNaN)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            Debug.Assert(length >= Vector256<T>.Count && length % Vector256<T>.Count == 0);

            Vector256<T> acc1 = Vector256.LoadUnsafe(ref xRef);
            Vector256<T> nan = Vector256.IsNaN(acc1);
            int i = Vector256<T>.Count;

            if (length >= 2 * Vector256<T>.Count)
            {
                Vector256<T> acc2 = Vector256.LoadUnsafe(ref xRef, (uint)i);
                nan |= Vector256.IsNaN(acc2);
                i += Vector256<T>.Count;

                int twoVectorsFromEnd = length - (2 * Vector256<T>.Count);
                while (i <= twoVectorsFromEnd)
                {
                    Vector256<T> current1 = Vector256.LoadUnsafe(ref xRef, (uint)i);
                    Vector256<T> current2 = Vector256.LoadUnsafe(ref xRef, (uint)(i + Vector256<T>.Count));
                    acc1 = TMinMaxOperator.Invoke(acc1, current1);
                    acc2 = TMinMaxOperator.Invoke(acc2, current2);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        nan |= Vector256.IsNaN(current1) | Vector256.IsNaN(current2);
                    }

                    i += 2 * Vector256<T>.Count;
                }

                acc1 = TMinMaxOperator.Invoke(acc1, acc2);
            }

            if (i != length)
            {
                Vector256<T> current = Vector256.LoadUnsafe(ref xRef, (uint)i);
                acc1 = TMinMaxOperator.Invoke(acc1, current);
                nan |= Vector256.IsNaN(current);
            }

            anyNaN = (typeof(T) == typeof(float) || typeof(T) == typeof(double)) && nan != Vector256<T>.Zero;
            return TMinMaxOperator.Invoke(acc1);
        }

        /// <summary>
        /// Reduces the <paramref name="length"/> elements at <paramref name="xRef"/>, a whole number of 256-bit vectors, with the native vector
        /// minimum/maximum, gathering alongside the two facts that let the caller restore the IEEE 754:2019 result (see <see cref="FixUpNativeSignedZero{T, TMinMaxOperator}"/>).
        /// </summary>
        /// <param name="xRef">The first element of the block.</param>
        /// <param name="length">The number of elements in the block, a positive multiple of <see cref="Vector256{T}.Count"/>.</param>
        /// <param name="anyNaN">Whether any element is NaN, in which case the returned value is meaningless.</param>
        /// <param name="anyOppositeSignBit">For a minimum, whether any element has its sign bit set; for a maximum, whether any has it clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T BlockReduceNative256<T, TMinMaxOperator>(ref T xRef, int length, out bool anyNaN, out bool anyOppositeSignBit)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            Debug.Assert(IsNativeMinMax<T, TMinMaxOperator>());
            Debug.Assert(length >= Vector256<T>.Count && length % Vector256<T>.Count == 0);

            // For a minimum, OR the elements together so that any set sign bit survives; for a maximum, AND them so that any clear one does.
            Vector256<T> acc1 = Vector256.LoadUnsafe(ref xRef);
            Vector256<T> nan = Vector256.IsNaN(acc1);
            Vector256<T> signBits = acc1;
            int i = Vector256<T>.Count;

            if (length >= 2 * Vector256<T>.Count)
            {
                Vector256<T> acc2 = Vector256.LoadUnsafe(ref xRef, (uint)i);
                nan |= Vector256.IsNaN(acc2);
                signBits = IsNativeMin<TMinMaxOperator>() ? signBits | acc2 : signBits & acc2;
                i += Vector256<T>.Count;

                int twoVectorsFromEnd = length - (2 * Vector256<T>.Count);
                while (i <= twoVectorsFromEnd)
                {
                    Vector256<T> current1 = Vector256.LoadUnsafe(ref xRef, (uint)i);
                    Vector256<T> current2 = Vector256.LoadUnsafe(ref xRef, (uint)(i + Vector256<T>.Count));
                    if (IsNativeMin<TMinMaxOperator>())
                    {
                        acc1 = Vector256.MinNative(acc1, current1);
                        acc2 = Vector256.MinNative(acc2, current2);
                        signBits |= current1 | current2;
                    }
                    else
                    {
                        acc1 = Vector256.MaxNative(acc1, current1);
                        acc2 = Vector256.MaxNative(acc2, current2);
                        signBits &= current1 & current2;
                    }

                    nan |= Vector256.IsNaN(current1) | Vector256.IsNaN(current2);
                    i += 2 * Vector256<T>.Count;
                }

                acc1 = IsNativeMin<TMinMaxOperator>() ? Vector256.MinNative(acc1, acc2) : Vector256.MaxNative(acc1, acc2);
            }

            if (i != length)
            {
                Vector256<T> current = Vector256.LoadUnsafe(ref xRef, (uint)i);
                if (IsNativeMin<TMinMaxOperator>())
                {
                    acc1 = Vector256.MinNative(acc1, current);
                    signBits |= current;
                }
                else
                {
                    acc1 = Vector256.MaxNative(acc1, current);
                    signBits &= current;
                }

                nan |= Vector256.IsNaN(current);
            }

            anyNaN = nan != Vector256<T>.Zero;
            anyOppositeSignBit = IsNativeMin<TMinMaxOperator>() ?
                signBits.ExtractMostSignificantBits() != 0 :
                (~signBits).ExtractMostSignificantBits() != 0;

            return IsNativeMin<TMinMaxOperator>() ?
                HorizontalAggregate<T, MinNativeOperator<T>>(acc1) :
                HorizontalAggregate<T, MaxNativeOperator<T>>(acc1);
        }

        /// <summary>Returns the first NaN among the <paramref name="length"/> elements at <paramref name="xRef"/>, a whole number of 256-bit vectors that contains one.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // cold: called at most once per reduction; keeps the caller within the inlining budget
        private static T FirstNaN256<T>(ref T xRef, int length)
            where T : INumberBase<T>
        {
            Debug.Assert(typeof(T) == typeof(float) || typeof(T) == typeof(double));
            Debug.Assert(length >= Vector256<T>.Count && length % Vector256<T>.Count == 0);

            for (int i = 0; i < length; i += Vector256<T>.Count)
            {
                Vector256<T> current = Vector256.LoadUnsafe(ref xRef, (uint)i);
                Vector256<T> nanMask = Vector256.IsNaN(current);
                if (nanMask != Vector256<T>.Zero)
                {
                    return current.GetElement(IndexOfFirstMatch(nanMask));
                }
            }

            Debug.Fail("The block must contain a NaN.");
            return default!;
        }

        /// <summary>The 128-bit path of <see cref="MinMaxCore{T, TMinMaxOperator}"/>: a block reduction of the whole vectors followed by one final vector that overlaps the last whole one.</summary>
        /// <remarks>
        /// Every block of up to <see cref="MinMaxBlockVectors"/> vectors is reduced with two independent accumulators and a single NaN decision
        /// (the OR of the elements' NaN masks), so the hot loop contains no branch on the data. Blocks are visited in order and the running
        /// result is only combined after a block has been checked, so the first NaN of the input is the one returned.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per reduction; its own inlining budget keeps the block reduction and the horizontal aggregates inlined
        private static T MinMaxVectorized128<T, TMinMaxOperator>(ReadOnlySpan<T> x)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            Debug.Assert(Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported);
            Debug.Assert(x.Length >= Vector128<T>.Count);

            ref T xRef = ref MemoryMarshal.GetReference(x);
            int length = x.Length;
            int blockSize = MinMaxBlockVectors * Vector128<T>.Count;
            int wholeVectorsLength = length - (length % Vector128<T>.Count);

            // Reduce the whole vectors block by block. Starting from the first element is harmless: it is part of the first block,
            // and combining an element with a result that already accounts for it does not change that result.
            T result = xRef;
            for (int i = 0; i < wholeVectorsLength; i += blockSize)
            {
                int blockLength = Math.Min(blockSize, wholeVectorsLength - i);
                T blockResult;
                bool anyNaN;

                if (IsNativeMinMax<T, TMinMaxOperator>())
                {
                    blockResult = BlockReduceNative128<T, TMinMaxOperator>(ref Unsafe.Add(ref xRef, i), blockLength, out anyNaN, out bool anyOppositeSignBit);
                    if (anyNaN)
                    {
                        return FirstNaN128(ref Unsafe.Add(ref xRef, i), blockLength);
                    }

                    blockResult = FixUpNativeSignedZero<T, TMinMaxOperator>(blockResult, anyOppositeSignBit);
                }
                else
                {
                    blockResult = BlockReduce128<T, TMinMaxOperator>(ref Unsafe.Add(ref xRef, i), blockLength, out anyNaN);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        if (anyNaN)
                        {
                            return FirstNaN128(ref Unsafe.Add(ref xRef, i), blockLength);
                        }
                    }
                }

                result = TMinMaxOperator.Invoke(result, blockResult);
            }

            // If any elements remain, handle them in one final vector. Its NaN check does not change which NaN is returned:
            // a NaN among the elements it revisits would already have been returned by their block.
            if (wholeVectorsLength != length)
            {
                Vector128<T> last = Vector128.LoadUnsafe(ref xRef, (uint)(length - Vector128<T>.Count));

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector128<T> nanMask = Vector128.IsNaN(last);
                    if (nanMask != Vector128<T>.Zero)
                    {
                        return last.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                result = TMinMaxOperator.Invoke(result, TMinMaxOperator.Invoke(last));
            }

            return result;
        }

        /// <summary>Reduces the <paramref name="length"/> elements at <paramref name="xRef"/>, a whole number of 128-bit vectors, with <typeparamref name="TMinMaxOperator"/>.</summary>
        /// <param name="xRef">The first element of the block.</param>
        /// <param name="length">The number of elements in the block, a positive multiple of <see cref="Vector128{T}.Count"/>.</param>
        /// <param name="anyNaN">
        /// For <see cref="float"/> and <see cref="double"/>, whether any element is NaN, in which case the returned value is meaningless.
        /// It is gathered from the elements rather than from the result because the Number operators discard NaN.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T BlockReduce128<T, TMinMaxOperator>(ref T xRef, int length, out bool anyNaN)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            Debug.Assert(length >= Vector128<T>.Count && length % Vector128<T>.Count == 0);

            Vector128<T> acc1 = Vector128.LoadUnsafe(ref xRef);
            Vector128<T> nan = Vector128.IsNaN(acc1);
            int i = Vector128<T>.Count;

            if (length >= 2 * Vector128<T>.Count)
            {
                Vector128<T> acc2 = Vector128.LoadUnsafe(ref xRef, (uint)i);
                nan |= Vector128.IsNaN(acc2);
                i += Vector128<T>.Count;

                int twoVectorsFromEnd = length - (2 * Vector128<T>.Count);
                while (i <= twoVectorsFromEnd)
                {
                    Vector128<T> current1 = Vector128.LoadUnsafe(ref xRef, (uint)i);
                    Vector128<T> current2 = Vector128.LoadUnsafe(ref xRef, (uint)(i + Vector128<T>.Count));
                    acc1 = TMinMaxOperator.Invoke(acc1, current1);
                    acc2 = TMinMaxOperator.Invoke(acc2, current2);
                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        nan |= Vector128.IsNaN(current1) | Vector128.IsNaN(current2);
                    }

                    i += 2 * Vector128<T>.Count;
                }

                acc1 = TMinMaxOperator.Invoke(acc1, acc2);
            }

            if (i != length)
            {
                Vector128<T> current = Vector128.LoadUnsafe(ref xRef, (uint)i);
                acc1 = TMinMaxOperator.Invoke(acc1, current);
                nan |= Vector128.IsNaN(current);
            }

            anyNaN = (typeof(T) == typeof(float) || typeof(T) == typeof(double)) && nan != Vector128<T>.Zero;
            return TMinMaxOperator.Invoke(acc1);
        }

        /// <summary>
        /// Reduces the <paramref name="length"/> elements at <paramref name="xRef"/>, a whole number of 128-bit vectors, with the native vector
        /// minimum/maximum, gathering alongside the two facts that let the caller restore the IEEE 754:2019 result (see <see cref="FixUpNativeSignedZero{T, TMinMaxOperator}"/>).
        /// </summary>
        /// <param name="xRef">The first element of the block.</param>
        /// <param name="length">The number of elements in the block, a positive multiple of <see cref="Vector128{T}.Count"/>.</param>
        /// <param name="anyNaN">Whether any element is NaN, in which case the returned value is meaningless.</param>
        /// <param name="anyOppositeSignBit">For a minimum, whether any element has its sign bit set; for a maximum, whether any has it clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T BlockReduceNative128<T, TMinMaxOperator>(ref T xRef, int length, out bool anyNaN, out bool anyOppositeSignBit)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            Debug.Assert(IsNativeMinMax<T, TMinMaxOperator>());
            Debug.Assert(length >= Vector128<T>.Count && length % Vector128<T>.Count == 0);

            // For a minimum, OR the elements together so that any set sign bit survives; for a maximum, AND them so that any clear one does.
            Vector128<T> acc1 = Vector128.LoadUnsafe(ref xRef);
            Vector128<T> nan = Vector128.IsNaN(acc1);
            Vector128<T> signBits = acc1;
            int i = Vector128<T>.Count;

            if (length >= 2 * Vector128<T>.Count)
            {
                Vector128<T> acc2 = Vector128.LoadUnsafe(ref xRef, (uint)i);
                nan |= Vector128.IsNaN(acc2);
                signBits = IsNativeMin<TMinMaxOperator>() ? signBits | acc2 : signBits & acc2;
                i += Vector128<T>.Count;

                int twoVectorsFromEnd = length - (2 * Vector128<T>.Count);
                while (i <= twoVectorsFromEnd)
                {
                    Vector128<T> current1 = Vector128.LoadUnsafe(ref xRef, (uint)i);
                    Vector128<T> current2 = Vector128.LoadUnsafe(ref xRef, (uint)(i + Vector128<T>.Count));
                    if (IsNativeMin<TMinMaxOperator>())
                    {
                        acc1 = Vector128.MinNative(acc1, current1);
                        acc2 = Vector128.MinNative(acc2, current2);
                        signBits |= current1 | current2;
                    }
                    else
                    {
                        acc1 = Vector128.MaxNative(acc1, current1);
                        acc2 = Vector128.MaxNative(acc2, current2);
                        signBits &= current1 & current2;
                    }

                    nan |= Vector128.IsNaN(current1) | Vector128.IsNaN(current2);
                    i += 2 * Vector128<T>.Count;
                }

                acc1 = IsNativeMin<TMinMaxOperator>() ? Vector128.MinNative(acc1, acc2) : Vector128.MaxNative(acc1, acc2);
            }

            if (i != length)
            {
                Vector128<T> current = Vector128.LoadUnsafe(ref xRef, (uint)i);
                if (IsNativeMin<TMinMaxOperator>())
                {
                    acc1 = Vector128.MinNative(acc1, current);
                    signBits |= current;
                }
                else
                {
                    acc1 = Vector128.MaxNative(acc1, current);
                    signBits &= current;
                }

                nan |= Vector128.IsNaN(current);
            }

            anyNaN = nan != Vector128<T>.Zero;
            anyOppositeSignBit = IsNativeMin<TMinMaxOperator>() ?
                signBits.ExtractMostSignificantBits() != 0 :
                (~signBits).ExtractMostSignificantBits() != 0;

            return IsNativeMin<TMinMaxOperator>() ?
                HorizontalAggregate<T, MinNativeOperator<T>>(acc1) :
                HorizontalAggregate<T, MaxNativeOperator<T>>(acc1);
        }

        /// <summary>Returns the first NaN among the <paramref name="length"/> elements at <paramref name="xRef"/>, a whole number of 128-bit vectors that contains one.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // cold: called at most once per reduction; keeps the caller within the inlining budget
        private static T FirstNaN128<T>(ref T xRef, int length)
            where T : INumberBase<T>
        {
            Debug.Assert(typeof(T) == typeof(float) || typeof(T) == typeof(double));
            Debug.Assert(length >= Vector128<T>.Count && length % Vector128<T>.Count == 0);

            for (int i = 0; i < length; i += Vector128<T>.Count)
            {
                Vector128<T> current = Vector128.LoadUnsafe(ref xRef, (uint)i);
                Vector128<T> nanMask = Vector128.IsNaN(current);
                if (nanMask != Vector128<T>.Zero)
                {
                    return current.GetElement(IndexOfFirstMatch(nanMask));
                }
            }

            Debug.Fail("The block must contain a NaN.");
            return default!;
        }
    }
}
