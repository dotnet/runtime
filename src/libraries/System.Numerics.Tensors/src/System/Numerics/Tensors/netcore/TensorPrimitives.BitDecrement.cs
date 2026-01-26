// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise bit decrement of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.BitDecrement(<paramref name="x" />[i])</c>.
        /// Each element is decremented to the largest value that compares less than the original.
        /// </para>
        /// </remarks>
        public static void BitDecrement<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            if (typeof(T) == typeof(Half) && TryUnaryInvokeHalfAsInt16<T, BitDecrementOperator<float>>(x, destination))
            {
                return;
            }

            InvokeSpanIntoSpan<T, BitDecrementOperator<T>>(x, destination);
        }

        /// <summary>T.BitDecrement(x)</summary>
        private readonly struct BitDecrementOperator<T> : IUnaryOperator<T, T>
            where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.BitDecrement(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector128<float> xFloat = x.AsSingle();
                    Vector128<uint> bits = xFloat.AsUInt32();

                    // Select based on sign: negative -> increment, positive -> decrement
                    Vector128<uint> isNegative = Vector128.Equals(bits & Vector128.Create(0x8000_0000u), Vector128.Create(0x8000_0000u));
                    Vector128<uint> result = Vector128.ConditionalSelect(
                        isNegative,
                        bits + Vector128<uint>.One,
                        bits - Vector128<uint>.One);

                    // Handle special cases
                    // +0.0 -> -Epsilon
                    Vector128<uint> isPosZero = Vector128.Equals(bits, Vector128.Create(0x0000_0000u)); // PositiveZeroBits
                    result = Vector128.ConditionalSelect(isPosZero, Vector128.Create(BitConverter.SingleToUInt32Bits(-float.Epsilon)), result);

                    // +Infinity -> MaxValue
                    Vector128<uint> isPosInf = Vector128.Equals(bits, Vector128.Create(0x7F80_0000u)); // PositiveInfinityBits
                    result = Vector128.ConditionalSelect(isPosInf, Vector128.Create(BitConverter.SingleToUInt32Bits(float.MaxValue)), result);

                    // NaN -> NaN (return original), -Infinity -> -Infinity (return original)
                    Vector128<uint> isNaN = ~Vector128.Equals(xFloat, xFloat).AsUInt32();
                    Vector128<uint> isNegInf = Vector128.Equals(bits, Vector128.Create(0xFF80_0000u)); // NegativeInfinityBits
                    result = Vector128.ConditionalSelect(isNaN | isNegInf, bits, result);

                    return result.AsSingle().As<float, T>();
                }
                else if (typeof(T) == typeof(double))
                {
                    Vector128<double> xDouble = x.AsDouble();
                    Vector128<ulong> bits = xDouble.AsUInt64();

                    // Select based on sign: negative -> increment, positive -> decrement
                    Vector128<ulong> isNegative = Vector128.Equals(bits & Vector128.Create(0x8000_0000_0000_0000ul), Vector128.Create(0x8000_0000_0000_0000ul));
                    Vector128<ulong> result = Vector128.ConditionalSelect(
                        isNegative,
                        bits + Vector128<ulong>.One,
                        bits - Vector128<ulong>.One);

                    // Handle special cases
                    // +0.0 -> -Epsilon
                    Vector128<ulong> isPosZero = Vector128.Equals(bits, Vector128.Create(0x0000_0000_0000_0000ul)); // PositiveZeroBits
                    result = Vector128.ConditionalSelect(isPosZero, Vector128.Create(BitConverter.DoubleToUInt64Bits(-double.Epsilon)), result);

                    // +Infinity -> MaxValue
                    Vector128<ulong> isPosInf = Vector128.Equals(bits, Vector128.Create(0x7FF0_0000_0000_0000ul)); // PositiveInfinityBits
                    result = Vector128.ConditionalSelect(isPosInf, Vector128.Create(BitConverter.DoubleToUInt64Bits(double.MaxValue)), result);

                    // NaN -> NaN (return original), -Infinity -> -Infinity (return original)
                    Vector128<ulong> isNaN = ~Vector128.Equals(xDouble, xDouble).AsUInt64();
                    Vector128<ulong> isNegInf = Vector128.Equals(bits, Vector128.Create(0xFFF0_0000_0000_0000ul)); // NegativeInfinityBits
                    result = Vector128.ConditionalSelect(isNaN | isNegInf, bits, result);

                    return result.AsDouble().As<double, T>();
                }

                // Fallback for unsupported types - should not be reached since Vectorizable returns false
                throw new NotSupportedException();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector256<float> xFloat = x.AsSingle();
                    Vector256<uint> bits = xFloat.AsUInt32();

                    // Select based on sign: negative -> increment, positive -> decrement
                    Vector256<uint> isNegative = Vector256.Equals(bits & Vector256.Create(0x8000_0000u), Vector256.Create(0x8000_0000u));
                    Vector256<uint> result = Vector256.ConditionalSelect(
                        isNegative,
                        bits + Vector256<uint>.One,
                        bits - Vector256<uint>.One);

                    // Handle special cases
                    // +0.0 -> -Epsilon
                    Vector256<uint> isPosZero = Vector256.Equals(bits, Vector256.Create(0x0000_0000u)); // PositiveZeroBits
                    result = Vector256.ConditionalSelect(isPosZero, Vector256.Create(BitConverter.SingleToUInt32Bits(-float.Epsilon)), result);

                    // +Infinity -> MaxValue
                    Vector256<uint> isPosInf = Vector256.Equals(bits, Vector256.Create(0x7F80_0000u)); // PositiveInfinityBits
                    result = Vector256.ConditionalSelect(isPosInf, Vector256.Create(BitConverter.SingleToUInt32Bits(float.MaxValue)), result);

                    // NaN -> NaN (return original), -Infinity -> -Infinity (return original)
                    Vector256<uint> isNaN = ~Vector256.Equals(xFloat, xFloat).AsUInt32();
                    Vector256<uint> isNegInf = Vector256.Equals(bits, Vector256.Create(0xFF80_0000u)); // NegativeInfinityBits
                    result = Vector256.ConditionalSelect(isNaN | isNegInf, bits, result);

                    return result.AsSingle().As<float, T>();
                }
                else if (typeof(T) == typeof(double))
                {
                    Vector256<double> xDouble = x.AsDouble();
                    Vector256<ulong> bits = xDouble.AsUInt64();

                    // Select based on sign: negative -> increment, positive -> decrement
                    Vector256<ulong> isNegative = Vector256.Equals(bits & Vector256.Create(0x8000_0000_0000_0000ul), Vector256.Create(0x8000_0000_0000_0000ul));
                    Vector256<ulong> result = Vector256.ConditionalSelect(
                        isNegative,
                        bits + Vector256<ulong>.One,
                        bits - Vector256<ulong>.One);

                    // Handle special cases
                    // +0.0 -> -Epsilon
                    Vector256<ulong> isPosZero = Vector256.Equals(bits, Vector256.Create(0x0000_0000_0000_0000ul)); // PositiveZeroBits
                    result = Vector256.ConditionalSelect(isPosZero, Vector256.Create(BitConverter.DoubleToUInt64Bits(-double.Epsilon)), result);

                    // +Infinity -> MaxValue
                    Vector256<ulong> isPosInf = Vector256.Equals(bits, Vector256.Create(0x7FF0_0000_0000_0000ul)); // PositiveInfinityBits
                    result = Vector256.ConditionalSelect(isPosInf, Vector256.Create(BitConverter.DoubleToUInt64Bits(double.MaxValue)), result);

                    // NaN -> NaN (return original), -Infinity -> -Infinity (return original)
                    Vector256<ulong> isNaN = ~Vector256.Equals(xDouble, xDouble).AsUInt64();
                    Vector256<ulong> isNegInf = Vector256.Equals(bits, Vector256.Create(0xFFF0_0000_0000_0000ul)); // NegativeInfinityBits
                    result = Vector256.ConditionalSelect(isNaN | isNegInf, bits, result);

                    return result.AsDouble().As<double, T>();
                }

                // Fallback for unsupported types - should not be reached since Vectorizable returns false
                throw new NotSupportedException();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector512<float> xFloat = x.AsSingle();
                    Vector512<uint> bits = xFloat.AsUInt32();

                    // Select based on sign: negative -> increment, positive -> decrement
                    Vector512<uint> isNegative = Vector512.Equals(bits & Vector512.Create(0x8000_0000u), Vector512.Create(0x8000_0000u));
                    Vector512<uint> result = Vector512.ConditionalSelect(
                        isNegative,
                        bits + Vector512<uint>.One,
                        bits - Vector512<uint>.One);

                    // Handle special cases
                    // +0.0 -> -Epsilon
                    Vector512<uint> isPosZero = Vector512.Equals(bits, Vector512.Create(0x0000_0000u)); // PositiveZeroBits
                    result = Vector512.ConditionalSelect(isPosZero, Vector512.Create(BitConverter.SingleToUInt32Bits(-float.Epsilon)), result);

                    // +Infinity -> MaxValue
                    Vector512<uint> isPosInf = Vector512.Equals(bits, Vector512.Create(0x7F80_0000u)); // PositiveInfinityBits
                    result = Vector512.ConditionalSelect(isPosInf, Vector512.Create(BitConverter.SingleToUInt32Bits(float.MaxValue)), result);

                    // NaN -> NaN (return original), -Infinity -> -Infinity (return original)
                    Vector512<uint> isNaN = ~Vector512.Equals(xFloat, xFloat).AsUInt32();
                    Vector512<uint> isNegInf = Vector512.Equals(bits, Vector512.Create(0xFF80_0000u)); // NegativeInfinityBits
                    result = Vector512.ConditionalSelect(isNaN | isNegInf, bits, result);

                    return result.AsSingle().As<float, T>();
                }
                else if (typeof(T) == typeof(double))
                {
                    Vector512<double> xDouble = x.AsDouble();
                    Vector512<ulong> bits = xDouble.AsUInt64();

                    // Select based on sign: negative -> increment, positive -> decrement
                    Vector512<ulong> isNegative = Vector512.Equals(bits & Vector512.Create(0x8000_0000_0000_0000ul), Vector512.Create(0x8000_0000_0000_0000ul));
                    Vector512<ulong> result = Vector512.ConditionalSelect(
                        isNegative,
                        bits + Vector512<ulong>.One,
                        bits - Vector512<ulong>.One);

                    // Handle special cases
                    // +0.0 -> -Epsilon
                    Vector512<ulong> isPosZero = Vector512.Equals(bits, Vector512.Create(0x0000_0000_0000_0000ul)); // PositiveZeroBits
                    result = Vector512.ConditionalSelect(isPosZero, Vector512.Create(BitConverter.DoubleToUInt64Bits(-double.Epsilon)), result);

                    // +Infinity -> MaxValue
                    Vector512<ulong> isPosInf = Vector512.Equals(bits, Vector512.Create(0x7FF0_0000_0000_0000ul)); // PositiveInfinityBits
                    result = Vector512.ConditionalSelect(isPosInf, Vector512.Create(BitConverter.DoubleToUInt64Bits(double.MaxValue)), result);

                    // NaN -> NaN (return original), -Infinity -> -Infinity (return original)
                    Vector512<ulong> isNaN = ~Vector512.Equals(xDouble, xDouble).AsUInt64();
                    Vector512<ulong> isNegInf = Vector512.Equals(bits, Vector512.Create(0xFFF0_0000_0000_0000ul)); // NegativeInfinityBits
                    result = Vector512.ConditionalSelect(isNaN | isNegInf, bits, result);

                    return result.AsDouble().As<double, T>();
                }

                // Fallback for unsupported types - should not be reached since Vectorizable returns false
                throw new NotSupportedException();
            }
        }
    }
}
