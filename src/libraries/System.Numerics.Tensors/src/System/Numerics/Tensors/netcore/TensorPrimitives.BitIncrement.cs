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

                    // Create masks for special cases
                    Vector128<float> isNaNMask = Vector128.Create(BitConverter.UInt32BitsToSingle(0xFFFFFFFF));
                    Vector128<uint> isNaN = Vector128.Equals(xFloat, xFloat).AsUInt32() ^ isNaNMask.AsUInt32();
                    Vector128<uint> isNegInf = Vector128.Equals(bits, Vector128.Create(float.NegativeInfinityBits));
                    Vector128<uint> isNegZero = Vector128.Equals(bits, Vector128.Create(float.NegativeZeroBits));
                    Vector128<uint> isPosInf = Vector128.Equals(bits, Vector128.Create(float.PositiveInfinityBits));

                    // Determine if negative (sign bit set)
                    Vector128<uint> isNegative = Vector128.LessThan(xFloat, Vector128<float>.Zero).AsUInt32();

                    // Compute bit incremented/decremented results
                    Vector128<uint> decremented = bits - Vector128<uint>.One;
                    Vector128<uint> incremented = bits + Vector128<uint>.One;

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector128<uint> result = Vector128.ConditionalSelect(isNegative, decremented, incremented);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    result = Vector128.ConditionalSelect(isNegZero, Vector128.Create(float.EpsilonBits), result);

                    // -Infinity -> MinValue
                    result = Vector128.ConditionalSelect(isNegInf, Vector128.Create(BitConverter.SingleToUInt32Bits(float.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector128<uint> preserveOriginal = isNaN | isPosInf;
                    result = Vector128.ConditionalSelect(preserveOriginal, bits, result);

                    return result.AsSingle().As<float, T>();
                }
                else if (typeof(T) == typeof(double))
                {
                    Vector128<double> xDouble = x.AsDouble();
                    Vector128<ulong> bits = xDouble.AsUInt64();

                    // Create masks for special cases
                    Vector128<double> isNaNMask = Vector128.Create(BitConverter.UInt64BitsToDouble(0xFFFFFFFFFFFFFFFF));
                    Vector128<ulong> isNaN = Vector128.Equals(xDouble, xDouble).AsUInt64() ^ isNaNMask.AsUInt64();
                    Vector128<ulong> isNegInf = Vector128.Equals(bits, Vector128.Create(double.NegativeInfinityBits));
                    Vector128<ulong> isNegZero = Vector128.Equals(bits, Vector128.Create(double.NegativeZeroBits));
                    Vector128<ulong> isPosInf = Vector128.Equals(bits, Vector128.Create(double.PositiveInfinityBits));

                    // Determine if negative (sign bit set)
                    Vector128<ulong> isNegative = Vector128.LessThan(xDouble, Vector128<double>.Zero).AsUInt64();

                    // Compute bit incremented/decremented results
                    Vector128<ulong> decremented = bits - Vector128<ulong>.One;
                    Vector128<ulong> incremented = bits + Vector128<ulong>.One;

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector128<ulong> result = Vector128.ConditionalSelect(isNegative, decremented, incremented);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    result = Vector128.ConditionalSelect(isNegZero, Vector128.Create(double.EpsilonBits), result);

                    // -Infinity -> MinValue
                    result = Vector128.ConditionalSelect(isNegInf, Vector128.Create(BitConverter.DoubleToUInt64Bits(double.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector128<ulong> preserveOriginal = isNaN | isPosInf;
                    result = Vector128.ConditionalSelect(preserveOriginal, bits, result);

                    return result.AsDouble().As<double, T>();
                }

                return ApplyScalar<BitIncrementOperator<T>>(x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector256<float> xFloat = x.AsSingle();
                    Vector256<uint> bits = xFloat.AsUInt32();

                    // Create masks for special cases
                    Vector256<float> isNaNMask = Vector256.Create(BitConverter.UInt32BitsToSingle(0xFFFFFFFF));
                    Vector256<uint> isNaN = Vector256.Equals(xFloat, xFloat).AsUInt32() ^ isNaNMask.AsUInt32();
                    Vector256<uint> isNegInf = Vector256.Equals(bits, Vector256.Create(float.NegativeInfinityBits));
                    Vector256<uint> isNegZero = Vector256.Equals(bits, Vector256.Create(float.NegativeZeroBits));
                    Vector256<uint> isPosInf = Vector256.Equals(bits, Vector256.Create(float.PositiveInfinityBits));

                    // Determine if negative (sign bit set)
                    Vector256<uint> isNegative = Vector256.LessThan(xFloat, Vector256<float>.Zero).AsUInt32();

                    // Compute bit incremented/decremented results
                    Vector256<uint> decremented = bits - Vector256<uint>.One;
                    Vector256<uint> incremented = bits + Vector256<uint>.One;

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector256<uint> result = Vector256.ConditionalSelect(isNegative, decremented, incremented);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    result = Vector256.ConditionalSelect(isNegZero, Vector256.Create(float.EpsilonBits), result);

                    // -Infinity -> MinValue
                    result = Vector256.ConditionalSelect(isNegInf, Vector256.Create(BitConverter.SingleToUInt32Bits(float.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector256<uint> preserveOriginal = isNaN | isPosInf;
                    result = Vector256.ConditionalSelect(preserveOriginal, bits, result);

                    return result.AsSingle().As<float, T>();
                }
                else if (typeof(T) == typeof(double))
                {
                    Vector256<double> xDouble = x.AsDouble();
                    Vector256<ulong> bits = xDouble.AsUInt64();

                    // Create masks for special cases
                    Vector256<double> isNaNMask = Vector256.Create(BitConverter.UInt64BitsToDouble(0xFFFFFFFFFFFFFFFF));
                    Vector256<ulong> isNaN = Vector256.Equals(xDouble, xDouble).AsUInt64() ^ isNaNMask.AsUInt64();
                    Vector256<ulong> isNegInf = Vector256.Equals(bits, Vector256.Create(double.NegativeInfinityBits));
                    Vector256<ulong> isNegZero = Vector256.Equals(bits, Vector256.Create(double.NegativeZeroBits));
                    Vector256<ulong> isPosInf = Vector256.Equals(bits, Vector256.Create(double.PositiveInfinityBits));

                    // Determine if negative (sign bit set)
                    Vector256<ulong> isNegative = Vector256.LessThan(xDouble, Vector256<double>.Zero).AsUInt64();

                    // Compute bit incremented/decremented results
                    Vector256<ulong> decremented = bits - Vector256<ulong>.One;
                    Vector256<ulong> incremented = bits + Vector256<ulong>.One;

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector256<ulong> result = Vector256.ConditionalSelect(isNegative, decremented, incremented);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    result = Vector256.ConditionalSelect(isNegZero, Vector256.Create(double.EpsilonBits), result);

                    // -Infinity -> MinValue
                    result = Vector256.ConditionalSelect(isNegInf, Vector256.Create(BitConverter.DoubleToUInt64Bits(double.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector256<ulong> preserveOriginal = isNaN | isPosInf;
                    result = Vector256.ConditionalSelect(preserveOriginal, bits, result);

                    return result.AsDouble().As<double, T>();
                }

                return ApplyScalar<BitIncrementOperator<T>>(x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector512<float> xFloat = x.AsSingle();
                    Vector512<uint> bits = xFloat.AsUInt32();

                    // Create masks for special cases
                    Vector512<float> isNaNMask = Vector512.Create(BitConverter.UInt32BitsToSingle(0xFFFFFFFF));
                    Vector512<uint> isNaN = Vector512.Equals(xFloat, xFloat).AsUInt32() ^ isNaNMask.AsUInt32();
                    Vector512<uint> isNegInf = Vector512.Equals(bits, Vector512.Create(float.NegativeInfinityBits));
                    Vector512<uint> isNegZero = Vector512.Equals(bits, Vector512.Create(float.NegativeZeroBits));
                    Vector512<uint> isPosInf = Vector512.Equals(bits, Vector512.Create(float.PositiveInfinityBits));

                    // Determine if negative (sign bit set)
                    Vector512<uint> isNegative = Vector512.LessThan(xFloat, Vector512<float>.Zero).AsUInt32();

                    // Compute bit incremented/decremented results
                    Vector512<uint> decremented = bits - Vector512<uint>.One;
                    Vector512<uint> incremented = bits + Vector512<uint>.One;

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector512<uint> result = Vector512.ConditionalSelect(isNegative, decremented, incremented);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    result = Vector512.ConditionalSelect(isNegZero, Vector512.Create(float.EpsilonBits), result);

                    // -Infinity -> MinValue
                    result = Vector512.ConditionalSelect(isNegInf, Vector512.Create(BitConverter.SingleToUInt32Bits(float.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector512<uint> preserveOriginal = isNaN | isPosInf;
                    result = Vector512.ConditionalSelect(preserveOriginal, bits, result);

                    return result.AsSingle().As<float, T>();
                }
                else if (typeof(T) == typeof(double))
                {
                    Vector512<double> xDouble = x.AsDouble();
                    Vector512<ulong> bits = xDouble.AsUInt64();

                    // Create masks for special cases
                    Vector512<double> isNaNMask = Vector512.Create(BitConverter.UInt64BitsToDouble(0xFFFFFFFFFFFFFFFF));
                    Vector512<ulong> isNaN = Vector512.Equals(xDouble, xDouble).AsUInt64() ^ isNaNMask.AsUInt64();
                    Vector512<ulong> isNegInf = Vector512.Equals(bits, Vector512.Create(double.NegativeInfinityBits));
                    Vector512<ulong> isNegZero = Vector512.Equals(bits, Vector512.Create(double.NegativeZeroBits));
                    Vector512<ulong> isPosInf = Vector512.Equals(bits, Vector512.Create(double.PositiveInfinityBits));

                    // Determine if negative (sign bit set)
                    Vector512<ulong> isNegative = Vector512.LessThan(xDouble, Vector512<double>.Zero).AsUInt64();

                    // Compute bit incremented/decremented results
                    Vector512<ulong> decremented = bits - Vector512<ulong>.One;
                    Vector512<ulong> incremented = bits + Vector512<ulong>.One;

                    // Select based on sign: negative -> decrement, positive -> increment
                    Vector512<ulong> result = Vector512.ConditionalSelect(isNegative, decremented, incremented);

                    // Handle special cases
                    // -0.0 -> Epsilon
                    result = Vector512.ConditionalSelect(isNegZero, Vector512.Create(double.EpsilonBits), result);

                    // -Infinity -> MinValue
                    result = Vector512.ConditionalSelect(isNegInf, Vector512.Create(BitConverter.DoubleToUInt64Bits(double.MinValue)), result);

                    // NaN -> NaN (return original), +Infinity -> +Infinity (return original)
                    Vector512<ulong> preserveOriginal = isNaN | isPosInf;
                    result = Vector512.ConditionalSelect(preserveOriginal, bits, result);

                    return result.AsDouble().As<double, T>();
                }

                return ApplyScalar<BitIncrementOperator<T>>(x);
            }
        }
    }
}
