// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting each <see cref="float" />
        /// value to its nearest representable half-precision floating-point value.
        /// </summary>
        /// <param name="source">The source span from which to copy values.</param>
        /// <param name="destination">The destination span into which the converted values should be written.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (Half)<paramref name="source" />[i]</c>.
        /// </para>
        /// <para>
        /// <paramref name="source"/> and <paramref name="destination"/> must not overlap. If they do, behavior is undefined.
        /// </para>
        /// </remarks>
        public static void ConvertToHalf(ReadOnlySpan<float> source, Span<Half> destination) =>
            ConvertTruncating(source, destination);

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting each half-precision
        /// floating-point value to its nearest representable <see cref="float"/> value.
        /// </summary>
        /// <param name="source">The source span from which to copy values.</param>
        /// <param name="destination">The destination span into which the converted values should be written.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = (float)<paramref name="source" />[i]</c>.
        /// </para>
        /// <para>
        /// <paramref name="source"/> and <paramref name="destination"/> must not overlap. If they do, behavior is undefined.
        /// </para>
        /// </remarks>
        public static void ConvertToSingle(ReadOnlySpan<Half> source, Span<float> destination) =>
            ConvertTruncating(source, destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryUnaryInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, Span<T> destination)
            where TOp : struct, IUnaryOperator<float, float>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable && IsVectorizable(Rename<T, Half>(x)))
            {
                InvokeSpanIntoSpan<short, HalfAsInt16UnaryOperator<TOp>>(
                    Rename<T, short>(x),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryUnaryBitwiseInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, Span<T> destination)
            where TOp : struct, IUnaryOperator<short, short>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable) // not checking IsVectorizable(x) because there's no runtime overhead to the Half<=>short conversion
            {
                InvokeSpanIntoSpan<short, TOp>(
                    Rename<T, short>(x),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryBinaryInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where TOp : struct, IBinaryOperator<float>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable && IsVectorizable(Rename<T, Half>(x)))
            {
                InvokeSpanSpanIntoSpan<short, HalfAsInt16BinaryOperator<TOp>>(
                    Rename<T, short>(x),
                    Rename<T, short>(y),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryBinaryInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where TOp : struct, IBinaryOperator<float>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable && IsVectorizable(Rename<T, Half>(x)))
            {
                InvokeSpanScalarIntoSpan<short, HalfAsInt16BinaryOperator<TOp>>(
                    Rename<T, short>(x),
                    BitConverter.HalfToInt16Bits((Half)(object)y!),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryBinaryInvokeHalfAsInt16<T, TOp>(T x, ReadOnlySpan<T> y, Span<T> destination)
            where TOp : struct, IBinaryOperator<float>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable && IsVectorizable(Rename<T, Half>(y)))
            {
                InvokeScalarSpanIntoSpan<short, HalfAsInt16BinaryOperator<TOp>>(
                    BitConverter.HalfToInt16Bits((Half)(object)x!),
                    Rename<T, short>(y),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryBinaryBitwiseInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where TOp : struct, IBinaryOperator<short>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable) // not checking IsVectorizable(x) because there's no runtime overhead to the Half<=>short conversion
            {
                InvokeSpanSpanIntoSpan<short, TOp>(
                    Rename<T, short>(x),
                    Rename<T, short>(y),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryBinaryBitwiseInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where TOp : struct, IBinaryOperator<short>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable) // not checking IsVectorizable(x) because there's no runtime overhead to the Half<=>short conversion
            {
                InvokeSpanScalarIntoSpan<short, TOp>(
                    Rename<T, short>(x),
                    BitConverter.HalfToInt16Bits((Half)(object)y!),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryAggregateInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where TOp : struct, IAggregationOperator<float>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable && IsVectorizable(Rename<T, Half>(x)))
            {
                InvokeSpanSpanIntoSpan<short, HalfAsInt16AggregationOperator<TOp>>(
                    Rename<T, short>(x),
                    Rename<T, short>(y),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryAggregateInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where TOp : struct, IAggregationOperator<float>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable && IsVectorizable(Rename<T, Half>(x)))
            {
                InvokeSpanScalarIntoSpan<short, HalfAsInt16AggregationOperator<TOp>>(
                    Rename<T, short>(x),
                    BitConverter.HalfToInt16Bits((Half)(object)y!),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryMinMaxHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, out T result)
            where TOp : struct, IAggregationOperator<float>
        {
            if (typeof(T) == typeof(Half) && IsVectorizable(Rename<T, Half>(x)))
            {
                result = (T)(object)BitConverter.Int16BitsToHalf(
                    MinMaxCore<short, HalfAsInt16AggregationOperator<TOp>>(
                        Rename<T, short>(x)));
                return true;
            }

            result = default!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryTernaryInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> z, Span<T> destination)
            where TOp : struct, ITernaryOperator<float>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable && IsVectorizable(Rename<T, Half>(x)))
            {
                InvokeSpanSpanSpanIntoSpan<short, HalfAsInt16TernaryOperator<TOp>>(
                    Rename<T, short>(x),
                    Rename<T, short>(y),
                    Rename<T, short>(z),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryTernaryInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, T y, ReadOnlySpan<T> z, Span<T> destination)
            where TOp : struct, ITernaryOperator<float>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable && IsVectorizable(Rename<T, Half>(x)))
            {
                InvokeSpanScalarSpanIntoSpan<short, HalfAsInt16TernaryOperator<TOp>>(
                    Rename<T, short>(x),
                    BitConverter.HalfToInt16Bits((Half)(object)y!),
                    Rename<T, short>(z),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryTernaryInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, T z, Span<T> destination)
            where TOp : struct, ITernaryOperator<float>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable && IsVectorizable(Rename<T, Half>(x)))
            {
                InvokeSpanSpanScalarIntoSpan<short, HalfAsInt16TernaryOperator<TOp>>(
                    Rename<T, short>(x),
                    Rename<T, short>(y),
                    BitConverter.HalfToInt16Bits((Half)(object)z!),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryTernaryInvokeHalfAsInt16<T, TOp>(ReadOnlySpan<T> x, T y, T z, Span<T> destination)
            where TOp : struct, ITernaryOperator<float>
        {
            Debug.Assert(typeof(T) == typeof(Half));

            if (TOp.Vectorizable && IsVectorizable(Rename<T, Half>(x)))
            {
                InvokeSpanScalarScalarIntoSpan<short, HalfAsInt16TernaryOperator<TOp>>(
                    Rename<T, short>(x),
                    BitConverter.HalfToInt16Bits((Half)(object)y!),
                    BitConverter.HalfToInt16Bits((Half)(object)z!),
                    Rename<T, short>(destination));
                return true;
            }

            return false;
        }

        /// <summary>
        /// To vectorize Half, we need to reinterpret to short, widen to float, operate on floats, then narrow back to short, and reinterpret back to Half.
        /// Those widening and narrowing operations add overhead. If we're able to actually vectorize, it's worthwhile, but if it's not, we want to take
        /// the normal scalar path rather than going through the short-based path that won't actually be fruitful.
        /// </summary>
        private static bool IsVectorizable(ReadOnlySpan<Half> source) =>
            Vector128.IsHardwareAccelerated &&
            source.Length >= Vector128<short>.Count;

        /// <summary><see cref="IUnaryOperator{T, T}"/> wrapper for working with <see cref="Half"/> reinterpreted as <see cref="short"/> in order to enable vectorization.</summary>
        private readonly struct HalfAsInt16UnaryOperator<TUnary> : IUnaryOperator<short, short>
            where TUnary : struct, IUnaryOperator<float, float>
        {
            public static bool Vectorizable => TUnary.Vectorizable;

            public static short Invoke(short x) =>
                BitConverter.HalfToInt16Bits(
                    (Half)TUnary.Invoke((float)BitConverter.Int16BitsToHalf(x)));

            public static Vector128<short> Invoke(Vector128<short> x)
            {
                (Vector128<float> xVecLower, Vector128<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TUnary.Invoke(xVecLower),
                    TUnary.Invoke(xVecUpper)).AsInt16();
            }

            public static Vector256<short> Invoke(Vector256<short> x)
            {
                (Vector256<float> xVecLower, Vector256<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TUnary.Invoke(xVecLower),
                    TUnary.Invoke(xVecUpper)).AsInt16();
            }

            public static Vector512<short> Invoke(Vector512<short> x)
            {
                (Vector512<float> xVecLower, Vector512<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TUnary.Invoke(xVecLower),
                    TUnary.Invoke(xVecUpper)).AsInt16();
            }
        }

        /// <summary><see cref="IBinaryOperator{T}"/> wrapper for working with <see cref="Half"/> reinterpreted as <see cref="short"/> in order to enable vectorization.</summary>
        private readonly struct HalfAsInt16BinaryOperator<TBinary> : IBinaryOperator<short>
            where TBinary : struct, IBinaryOperator<float>
        {
            public static bool Vectorizable => TBinary.Vectorizable;

            public static short Invoke(short x, short y) =>
                BitConverter.HalfToInt16Bits((Half)TBinary.Invoke(
                    (float)BitConverter.Int16BitsToHalf(x),
                    (float)BitConverter.Int16BitsToHalf(y)));

            public static Vector128<short> Invoke(Vector128<short> x, Vector128<short> y)
            {
                (Vector128<float> xVecLower, Vector128<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                (Vector128<float> yVecLower, Vector128<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(y);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TBinary.Invoke(xVecLower, yVecLower),
                    TBinary.Invoke(xVecUpper, yVecUpper)).AsInt16();
            }

            public static Vector256<short> Invoke(Vector256<short> x, Vector256<short> y)
            {
                (Vector256<float> xVecLower, Vector256<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                (Vector256<float> yVecLower, Vector256<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(y);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TBinary.Invoke(xVecLower, yVecLower),
                    TBinary.Invoke(xVecUpper, yVecUpper)).AsInt16();
            }

            public static Vector512<short> Invoke(Vector512<short> x, Vector512<short> y)
            {
                (Vector512<float> xVecLower, Vector512<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                (Vector512<float> yVecLower, Vector512<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(y);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TBinary.Invoke(xVecLower, yVecLower),
                    TBinary.Invoke(xVecUpper, yVecUpper)).AsInt16();
            }
        }

        /// <summary><see cref="IAggregationOperator{T}"/> wrapper for working with <see cref="Half"/> reinterpreted as <see cref="short"/> in order to enable vectorization.</summary>
        private readonly struct HalfAsInt16AggregationOperator<TAggregate> : IAggregationOperator<short>
            where TAggregate : struct, IAggregationOperator<float>
        {
            public static bool Vectorizable => TAggregate.Vectorizable;

            public static short Invoke(short x, short y) =>
                BitConverter.HalfToInt16Bits((Half)TAggregate.Invoke(
                    (float)BitConverter.Int16BitsToHalf(x),
                    (float)BitConverter.Int16BitsToHalf(y)));

            public static Vector128<short> Invoke(Vector128<short> x, Vector128<short> y)
            {
                (Vector128<float> xVecLower, Vector128<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                (Vector128<float> yVecLower, Vector128<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(y);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TAggregate.Invoke(xVecLower, yVecLower),
                    TAggregate.Invoke(xVecUpper, yVecUpper)).AsInt16();
            }

            public static Vector256<short> Invoke(Vector256<short> x, Vector256<short> y)
            {
                (Vector256<float> xVecLower, Vector256<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                (Vector256<float> yVecLower, Vector256<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(y);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TAggregate.Invoke(xVecLower, yVecLower),
                    TAggregate.Invoke(xVecUpper, yVecUpper)).AsInt16();
            }

            public static Vector512<short> Invoke(Vector512<short> x, Vector512<short> y)
            {
                (Vector512<float> xVecLower, Vector512<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                (Vector512<float> yVecLower, Vector512<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(y);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TAggregate.Invoke(xVecLower, yVecLower),
                    TAggregate.Invoke(xVecUpper, yVecUpper)).AsInt16();
            }

            public static short Invoke(Vector128<short> x)
            {
                (Vector128<float> xVecLower, Vector128<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                return BitConverter.HalfToInt16Bits((Half)TAggregate.Invoke(
                    TAggregate.Invoke(xVecLower),
                    TAggregate.Invoke(xVecUpper)));
            }

            public static short Invoke(Vector256<short> x)
            {
                (Vector256<float> xVecLower, Vector256<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                return BitConverter.HalfToInt16Bits((Half)TAggregate.Invoke(
                    TAggregate.Invoke(xVecLower),
                    TAggregate.Invoke(xVecUpper)));
            }

            public static short Invoke(Vector512<short> x)
            {
                (Vector512<float> xVecLower, Vector512<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                return BitConverter.HalfToInt16Bits((Half)TAggregate.Invoke(
                    TAggregate.Invoke(xVecLower),
                    TAggregate.Invoke(xVecUpper)));
            }

            public static short IdentityValue => BitConverter.HalfToInt16Bits((Half)TAggregate.IdentityValue);
        }

        /// <summary><see cref="ITernaryOperator{T}"/> wrapper for working with <see cref="Half"/> reinterpreted as <see cref="short"/> in order to enable vectorization.</summary>
        private readonly struct HalfAsInt16TernaryOperator<TTernary> : ITernaryOperator<short>
            where TTernary : struct, ITernaryOperator<float>
        {
            public static bool Vectorizable => TTernary.Vectorizable;

            public static short Invoke(short x, short y, short z) =>
                BitConverter.HalfToInt16Bits((Half)TTernary.Invoke(
                    (float)BitConverter.Int16BitsToHalf(x),
                    (float)BitConverter.Int16BitsToHalf(y),
                    (float)BitConverter.Int16BitsToHalf(z)));

            public static Vector128<short> Invoke(Vector128<short> x, Vector128<short> y, Vector128<short> z)
            {
                (Vector128<float> xVecLower, Vector128<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                (Vector128<float> yVecLower, Vector128<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(y);
                (Vector128<float> zVecLower, Vector128<float> zVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(z);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TTernary.Invoke(xVecLower, yVecLower, zVecLower),
                    TTernary.Invoke(xVecUpper, yVecUpper, zVecUpper)).AsInt16();
            }

            public static Vector256<short> Invoke(Vector256<short> x, Vector256<short> y, Vector256<short> z)
            {
                (Vector256<float> xVecLower, Vector256<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                (Vector256<float> yVecLower, Vector256<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(y);
                (Vector256<float> zVecLower, Vector256<float> zVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(z);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TTernary.Invoke(xVecLower, yVecLower, zVecLower),
                    TTernary.Invoke(xVecUpper, yVecUpper, zVecUpper)).AsInt16();
            }

            public static Vector512<short> Invoke(Vector512<short> x, Vector512<short> y, Vector512<short> z)
            {
                (Vector512<float> xVecLower, Vector512<float> xVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(x);
                (Vector512<float> yVecLower, Vector512<float> yVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(y);
                (Vector512<float> zVecLower, Vector512<float> zVecUpper) = WidenHalfAsInt16ToSingleOperator.Invoke(z);
                return NarrowSingleToHalfAsUInt16Operator.Invoke(
                    TTernary.Invoke(xVecLower, yVecLower, zVecLower),
                    TTernary.Invoke(xVecUpper, yVecUpper, zVecUpper)).AsInt16();
            }
        }
    }
}
