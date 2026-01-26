// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise bit increment of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.BitIncrement(<paramref name="x" />[i])</c>.
        /// Each element is incremented to the smallest value that compares greater than the original.
        /// </para>
        /// </remarks>
        public static void BitIncrement<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanIntoSpan<T, BitIncrementOperator<T>>(x, destination);

        /// <summary>T.BitIncrement(x)</summary>
        private readonly struct BitIncrementOperator<T> : IUnaryOperator<T, T>
            where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.BitIncrement(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector128<float> xFloat = x.AsSingle();
                    Vector128<uint> bits = xFloat.AsUInt32();

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector128<uint> isNegative = Vector128.Equals(bits & Vector128.Create(0x8000_0000u), Vector128.Create(0x8000_0000u));
                    Vector128<uint> result = Vector128.ConditionalSelect(
                        isNegative,
                        bits - Vector128<uint>.One,
                        bits + Vector128<uint>.One);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    Vector128<uint> isNegZero = Vector128.Equals(bits, Vector128.Create(0x8000_0000u)); // NegativeZeroBits
                    result = Vector128.ConditionalSelect(isNegZero, Vector128.Create(0x0000_0001u), result); // EpsilonBits

                    // -Infinity -> MinValue
                    Vector128<uint> isNegInf = Vector128.Equals(bits, Vector128.Create(0xFF80_0000u)); // NegativeInfinityBits
                    result = Vector128.ConditionalSelect(isNegInf, Vector128.Create(BitConverter.SingleToUInt32Bits(float.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector128<uint> isNaN = ~Vector128.Equals(xFloat, xFloat).AsUInt32();
                    Vector128<uint> isPosInf = Vector128.Equals(bits, Vector128.Create(0x7F80_0000u)); // PositiveInfinityBits
                    result = Vector128.ConditionalSelect(isNaN | isPosInf, bits, result);

                    return result.AsSingle().As<float, T>();
                }
                else if (typeof(T) == typeof(double))
                {
                    Vector128<double> xDouble = x.AsDouble();
                    Vector128<ulong> bits = xDouble.AsUInt64();

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector128<ulong> isNegative = Vector128.Equals(bits & Vector128.Create(0x8000_0000_0000_0000ul), Vector128.Create(0x8000_0000_0000_0000ul));
                    Vector128<ulong> result = Vector128.ConditionalSelect(
                        isNegative,
                        bits - Vector128<ulong>.One,
                        bits + Vector128<ulong>.One);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    Vector128<ulong> isNegZero = Vector128.Equals(bits, Vector128.Create(0x8000_0000_0000_0000ul)); // NegativeZeroBits
                    result = Vector128.ConditionalSelect(isNegZero, Vector128.Create(0x0000_0000_0000_0001ul), result); // EpsilonBits

                    // -Infinity -> MinValue
                    Vector128<ulong> isNegInf = Vector128.Equals(bits, Vector128.Create(0xFFF0_0000_0000_0000ul)); // NegativeInfinityBits
                    result = Vector128.ConditionalSelect(isNegInf, Vector128.Create(BitConverter.DoubleToUInt64Bits(double.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector128<ulong> isNaN = ~Vector128.Equals(xDouble, xDouble).AsUInt64();
                    Vector128<ulong> isPosInf = Vector128.Equals(bits, Vector128.Create(0x7FF0_0000_0000_0000ul)); // PositiveInfinityBits
                    result = Vector128.ConditionalSelect(isNaN | isPosInf, bits, result);

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

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector256<uint> isNegative = Vector256.Equals(bits & Vector256.Create(0x8000_0000u), Vector256.Create(0x8000_0000u));
                    Vector256<uint> result = Vector256.ConditionalSelect(
                        isNegative,
                        bits - Vector256<uint>.One,
                        bits + Vector256<uint>.One);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    Vector256<uint> isNegZero = Vector256.Equals(bits, Vector256.Create(0x8000_0000u)); // NegativeZeroBits
                    result = Vector256.ConditionalSelect(isNegZero, Vector256.Create(0x0000_0001u), result); // EpsilonBits

                    // -Infinity -> MinValue
                    Vector256<uint> isNegInf = Vector256.Equals(bits, Vector256.Create(0xFF80_0000u)); // NegativeInfinityBits
                    result = Vector256.ConditionalSelect(isNegInf, Vector256.Create(BitConverter.SingleToUInt32Bits(float.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector256<uint> isNaN = ~Vector256.Equals(xFloat, xFloat).AsUInt32();
                    Vector256<uint> isPosInf = Vector256.Equals(bits, Vector256.Create(0x7F80_0000u)); // PositiveInfinityBits
                    result = Vector256.ConditionalSelect(isNaN | isPosInf, bits, result);

                    return result.AsSingle().As<float, T>();
                }
                else if (typeof(T) == typeof(double))
                {
                    Vector256<double> xDouble = x.AsDouble();
                    Vector256<ulong> bits = xDouble.AsUInt64();

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector256<ulong> isNegative = Vector256.Equals(bits & Vector256.Create(0x8000_0000_0000_0000ul), Vector256.Create(0x8000_0000_0000_0000ul));
                    Vector256<ulong> result = Vector256.ConditionalSelect(
                        isNegative,
                        bits - Vector256<ulong>.One,
                        bits + Vector256<ulong>.One);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    Vector256<ulong> isNegZero = Vector256.Equals(bits, Vector256.Create(0x8000_0000_0000_0000ul)); // NegativeZeroBits
                    result = Vector256.ConditionalSelect(isNegZero, Vector256.Create(0x0000_0000_0000_0001ul), result); // EpsilonBits

                    // -Infinity -> MinValue
                    Vector256<ulong> isNegInf = Vector256.Equals(bits, Vector256.Create(0xFFF0_0000_0000_0000ul)); // NegativeInfinityBits
                    result = Vector256.ConditionalSelect(isNegInf, Vector256.Create(BitConverter.DoubleToUInt64Bits(double.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector256<ulong> isNaN = ~Vector256.Equals(xDouble, xDouble).AsUInt64();
                    Vector256<ulong> isPosInf = Vector256.Equals(bits, Vector256.Create(0x7FF0_0000_0000_0000ul)); // PositiveInfinityBits
                    result = Vector256.ConditionalSelect(isNaN | isPosInf, bits, result);

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

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector512<uint> isNegative = Vector512.Equals(bits & Vector512.Create(0x8000_0000u), Vector512.Create(0x8000_0000u));
                    Vector512<uint> result = Vector512.ConditionalSelect(
                        isNegative,
                        bits - Vector512<uint>.One,
                        bits + Vector512<uint>.One);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    Vector512<uint> isNegZero = Vector512.Equals(bits, Vector512.Create(0x8000_0000u)); // NegativeZeroBits
                    result = Vector512.ConditionalSelect(isNegZero, Vector512.Create(0x0000_0001u), result); // EpsilonBits

                    // -Infinity -> MinValue
                    Vector512<uint> isNegInf = Vector512.Equals(bits, Vector512.Create(0xFF80_0000u)); // NegativeInfinityBits
                    result = Vector512.ConditionalSelect(isNegInf, Vector512.Create(BitConverter.SingleToUInt32Bits(float.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector512<uint> isNaN = ~Vector512.Equals(xFloat, xFloat).AsUInt32();
                    Vector512<uint> isPosInf = Vector512.Equals(bits, Vector512.Create(0x7F80_0000u)); // PositiveInfinityBits
                    result = Vector512.ConditionalSelect(isNaN | isPosInf, bits, result);

                    return result.AsSingle().As<float, T>();
                }
                else if (typeof(T) == typeof(double))
                {
                    Vector512<double> xDouble = x.AsDouble();
                    Vector512<ulong> bits = xDouble.AsUInt64();

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector512<ulong> isNegative = Vector512.Equals(bits & Vector512.Create(0x8000_0000_0000_0000ul), Vector512.Create(0x8000_0000_0000_0000ul));
                    Vector512<ulong> result = Vector512.ConditionalSelect(
                        isNegative,
                        bits - Vector512<ulong>.One,
                        bits + Vector512<ulong>.One);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    Vector512<ulong> isNegZero = Vector512.Equals(bits, Vector512.Create(0x8000_0000_0000_0000ul)); // NegativeZeroBits
                    result = Vector512.ConditionalSelect(isNegZero, Vector512.Create(0x0000_0000_0000_0001ul), result); // EpsilonBits

                    // -Infinity -> MinValue
                    Vector512<ulong> isNegInf = Vector512.Equals(bits, Vector512.Create(0xFFF0_0000_0000_0000ul)); // NegativeInfinityBits
                    result = Vector512.ConditionalSelect(isNegInf, Vector512.Create(BitConverter.DoubleToUInt64Bits(double.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector512<ulong> isNaN = ~Vector512.Equals(xDouble, xDouble).AsUInt64();
                    Vector512<ulong> isPosInf = Vector512.Equals(bits, Vector512.Create(0x7FF0_0000_0000_0000ul)); // PositiveInfinityBits
                    result = Vector512.ConditionalSelect(isNaN | isPosInf, bits, result);

                    return result.AsDouble().As<double, T>();
                }

                // Fallback for unsupported types - should not be reached since Vectorizable returns false
                throw new NotSupportedException();
            }
        }
    }
}
