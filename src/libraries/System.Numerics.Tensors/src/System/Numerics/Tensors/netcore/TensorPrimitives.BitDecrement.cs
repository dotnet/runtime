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
            if (typeof(T) == typeof(Half) && TryUnaryBitwiseInvokeHalfAsInt16<T, HalfBitDecrementOperator>(x, destination))
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

                    // General case: negative -> increment, positive -> decrement
                    Vector128<uint> result = Vector128.ConditionalSelect(
                        Vector128.IsNegative(xFloat).AsUInt32(),
                        bits + Vector128<uint>.One,
                        bits - Vector128<uint>.One);

                    // Handle special cases with a single conditional select
                    Vector128<uint> isPositiveZero = Vector128.IsZero(xFloat).AsUInt32();
                    Vector128<uint> specialValue = Vector128.Create(BitConverter.SingleToUInt32Bits(-float.Epsilon)) & isPositiveZero;

                    Vector128<uint> isNaNOrNegInf = (Vector128.IsNaN(xFloat) | Vector128.IsNegativeInfinity(xFloat)).AsUInt32();
                    specialValue |= bits & isNaNOrNegInf;

                    Vector128<uint> specialMask = isPositiveZero | isNaNOrNegInf;
                    return Vector128.ConditionalSelect(specialMask, specialValue, result).AsSingle().As<float, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector128<double> xDouble = x.AsDouble();
                    Vector128<ulong> bits = xDouble.AsUInt64();

                    // General case: negative -> increment, positive -> decrement
                    Vector128<ulong> result = Vector128.ConditionalSelect(
                        Vector128.IsNegative(xDouble).AsUInt64(),
                        bits + Vector128<ulong>.One,
                        bits - Vector128<ulong>.One);

                    // Handle special cases with a single conditional select
                    Vector128<ulong> isPositiveZero = Vector128.IsZero(xDouble).AsUInt64();
                    Vector128<ulong> specialValue = Vector128.Create(BitConverter.DoubleToUInt64Bits(-double.Epsilon)) & isPositiveZero;

                    Vector128<ulong> isNaNOrNegInf = (Vector128.IsNaN(xDouble) | Vector128.IsNegativeInfinity(xDouble)).AsUInt64();
                    specialValue |= bits & isNaNOrNegInf;

                    Vector128<ulong> specialMask = isPositiveZero | isNaNOrNegInf;
                    return Vector128.ConditionalSelect(specialMask, specialValue, result).AsDouble().As<double, T>();
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

                    // General case: negative -> increment, positive -> decrement
                    Vector256<uint> result = Vector256.ConditionalSelect(
                        Vector256.IsNegative(xFloat).AsUInt32(),
                        bits + Vector256<uint>.One,
                        bits - Vector256<uint>.One);

                    // Handle special cases with a single conditional select
                    Vector256<uint> isPositiveZero = Vector256.IsZero(xFloat).AsUInt32();
                    Vector256<uint> specialValue = Vector256.Create(BitConverter.SingleToUInt32Bits(-float.Epsilon)) & isPositiveZero;

                    Vector256<uint> isNaNOrNegInf = (Vector256.IsNaN(xFloat) | Vector256.IsNegativeInfinity(xFloat)).AsUInt32();
                    specialValue |= bits & isNaNOrNegInf;

                    Vector256<uint> specialMask = isPositiveZero | isNaNOrNegInf;
                    return Vector256.ConditionalSelect(specialMask, specialValue, result).AsSingle().As<float, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector256<double> xDouble = x.AsDouble();
                    Vector256<ulong> bits = xDouble.AsUInt64();

                    // General case: negative -> increment, positive -> decrement
                    Vector256<ulong> result = Vector256.ConditionalSelect(
                        Vector256.IsNegative(xDouble).AsUInt64(),
                        bits + Vector256<ulong>.One,
                        bits - Vector256<ulong>.One);

                    // Handle special cases with a single conditional select
                    Vector256<ulong> isPositiveZero = Vector256.IsZero(xDouble).AsUInt64();
                    Vector256<ulong> specialValue = Vector256.Create(BitConverter.DoubleToUInt64Bits(-double.Epsilon)) & isPositiveZero;

                    Vector256<ulong> isNaNOrNegInf = (Vector256.IsNaN(xDouble) | Vector256.IsNegativeInfinity(xDouble)).AsUInt64();
                    specialValue |= bits & isNaNOrNegInf;

                    Vector256<ulong> specialMask = isPositiveZero | isNaNOrNegInf;
                    return Vector256.ConditionalSelect(specialMask, specialValue, result).AsDouble().As<double, T>();
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

                    // General case: negative -> increment, positive -> decrement
                    Vector512<uint> result = Vector512.ConditionalSelect(
                        Vector512.IsNegative(xFloat).AsUInt32(),
                        bits + Vector512<uint>.One,
                        bits - Vector512<uint>.One);

                    // Handle special cases with a single conditional select
                    Vector512<uint> isPositiveZero = Vector512.IsZero(xFloat).AsUInt32();
                    Vector512<uint> specialValue = Vector512.Create(BitConverter.SingleToUInt32Bits(-float.Epsilon)) & isPositiveZero;

                    Vector512<uint> isNaNOrNegInf = (Vector512.IsNaN(xFloat) | Vector512.IsNegativeInfinity(xFloat)).AsUInt32();
                    specialValue |= bits & isNaNOrNegInf;

                    Vector512<uint> specialMask = isPositiveZero | isNaNOrNegInf;
                    return Vector512.ConditionalSelect(specialMask, specialValue, result).AsSingle().As<float, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector512<double> xDouble = x.AsDouble();
                    Vector512<ulong> bits = xDouble.AsUInt64();

                    // General case: negative -> increment, positive -> decrement
                    Vector512<ulong> result = Vector512.ConditionalSelect(
                        Vector512.IsNegative(xDouble).AsUInt64(),
                        bits + Vector512<ulong>.One,
                        bits - Vector512<ulong>.One);

                    // Handle special cases with a single conditional select
                    Vector512<ulong> isPositiveZero = Vector512.IsZero(xDouble).AsUInt64();
                    Vector512<ulong> specialValue = Vector512.Create(BitConverter.DoubleToUInt64Bits(-double.Epsilon)) & isPositiveZero;

                    Vector512<ulong> isNaNOrNegInf = (Vector512.IsNaN(xDouble) | Vector512.IsNegativeInfinity(xDouble)).AsUInt64();
                    specialValue |= bits & isNaNOrNegInf;

                    Vector512<ulong> specialMask = isPositiveZero | isNaNOrNegInf;
                    return Vector512.ConditionalSelect(specialMask, specialValue, result).AsDouble().As<double, T>();
                }

                // Fallback for unsupported types - should not be reached since Vectorizable returns false
                throw new NotSupportedException();
            }
        }

        /// <summary>Half.BitDecrement(x) operator for direct ushort manipulation.</summary>
        private readonly struct HalfBitDecrementOperator : IUnaryOperator<short, short>
        {
            // Half constants
            private const ushort PositiveInfinityBits = 0x7C00;
            private const ushort NegativeInfinityBits = 0xFC00;
            private const ushort NegativeEpsilonBits = 0x8001;

            public static bool Vectorizable => true;

            public static short Invoke(short x) => BitConverter.HalfToInt16Bits(Half.BitDecrement(BitConverter.Int16BitsToHalf(x)));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<short> Invoke(Vector128<short> x)
            {
                Vector128<ushort> bits = x.AsUInt16();

                // General case: negative -> increment, positive -> decrement
                Vector128<ushort> isNegative = Vector128.IsNegative(x).AsUInt16();
                Vector128<ushort> result = Vector128.ConditionalSelect(
                    isNegative,
                    bits + Vector128<ushort>.One,
                    bits - Vector128<ushort>.One);

                // Handle special cases with a single conditional select
                Vector128<ushort> isPositiveZero = Vector128.IsZero(bits);
                Vector128<ushort> specialValue = Vector128.Create(NegativeEpsilonBits) & isPositiveZero;

                // NaN: (bits & 0x7FFF) > 0x7C00 (both positive and negative NaN)
                // -Infinity: bits == 0xFC00
                Vector128<ushort> absValue = bits & Vector128.Create((ushort)0x7FFF);
                Vector128<ushort> isNaN = Vector128.GreaterThan(absValue, Vector128.Create(PositiveInfinityBits));
                Vector128<ushort> isNegInf = Vector128.Equals(bits, Vector128.Create(NegativeInfinityBits));
                Vector128<ushort> isNaNOrNegInf = isNaN | isNegInf;
                specialValue |= bits & isNaNOrNegInf;

                Vector128<ushort> specialMask = isPositiveZero | isNaNOrNegInf;
                return Vector128.ConditionalSelect(specialMask, specialValue, result).AsInt16();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<short> Invoke(Vector256<short> x)
            {
                Vector256<ushort> bits = x.AsUInt16();

                // General case: negative -> increment, positive -> decrement
                Vector256<ushort> isNegative = Vector256.IsNegative(x).AsUInt16();
                Vector256<ushort> result = Vector256.ConditionalSelect(
                    isNegative,
                    bits + Vector256<ushort>.One,
                    bits - Vector256<ushort>.One);

                // Handle special cases with a single conditional select
                Vector256<ushort> isPositiveZero = Vector256.IsZero(bits);
                Vector256<ushort> specialValue = Vector256.Create(NegativeEpsilonBits) & isPositiveZero;

                // NaN: (bits & 0x7FFF) > 0x7C00 (both positive and negative NaN)
                // -Infinity: bits == 0xFC00
                Vector256<ushort> absValue = bits & Vector256.Create((ushort)0x7FFF);
                Vector256<ushort> isNaN = Vector256.GreaterThan(absValue, Vector256.Create(PositiveInfinityBits));
                Vector256<ushort> isNegInf = Vector256.Equals(bits, Vector256.Create(NegativeInfinityBits));
                Vector256<ushort> isNaNOrNegInf = isNaN | isNegInf;
                specialValue |= bits & isNaNOrNegInf;

                Vector256<ushort> specialMask = isPositiveZero | isNaNOrNegInf;
                return Vector256.ConditionalSelect(specialMask, specialValue, result).AsInt16();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<short> Invoke(Vector512<short> x)
            {
                Vector512<ushort> bits = x.AsUInt16();

                // General case: negative -> increment, positive -> decrement
                Vector512<ushort> isNegative = Vector512.IsNegative(x).AsUInt16();
                Vector512<ushort> result = Vector512.ConditionalSelect(
                    isNegative,
                    bits + Vector512<ushort>.One,
                    bits - Vector512<ushort>.One);

                // Handle special cases with a single conditional select
                Vector512<ushort> isPositiveZero = Vector512.IsZero(bits);
                Vector512<ushort> specialValue = Vector512.Create(NegativeEpsilonBits) & isPositiveZero;

                // NaN: (bits & 0x7FFF) > 0x7C00 (both positive and negative NaN)
                // -Infinity: bits == 0xFC00
                Vector512<ushort> absValue = bits & Vector512.Create((ushort)0x7FFF);
                Vector512<ushort> isNaN = Vector512.GreaterThan(absValue, Vector512.Create(PositiveInfinityBits));
                Vector512<ushort> isNegInf = Vector512.Equals(bits, Vector512.Create(NegativeInfinityBits));
                Vector512<ushort> isNaNOrNegInf = isNaN | isNegInf;
                specialValue |= bits & isNaNOrNegInf;

                Vector512<ushort> specialMask = isPositiveZero | isNaNOrNegInf;
                return Vector512.ConditionalSelect(specialMask, specialValue, result).AsInt16();
            }
        }
    }
}
