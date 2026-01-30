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

                    // General case: negative -> decrement, positive -> increment
                    Vector128<uint> result = Vector128.ConditionalSelect(
                        Vector128.IsNegative(xFloat).AsUInt32(),
                        bits - Vector128<uint>.One,
                        bits + Vector128<uint>.One);

                    // Handle special cases with a single conditional select
                    Vector128<uint> isNegativeZero = Vector128.Equals(bits, Vector128.Create(0x8000_0000u));
                    Vector128<uint> specialValue = Vector128.Create(0x0000_0001u) & isNegativeZero;

                    Vector128<uint> isNaNOrPosInf = (Vector128.IsNaN(xFloat) | Vector128.IsPositiveInfinity(xFloat)).AsUInt32();
                    specialValue |= bits & isNaNOrPosInf;

                    Vector128<uint> specialMask = isNegativeZero | isNaNOrPosInf;
                    return Vector128.ConditionalSelect(specialMask, specialValue, result).AsSingle().As<float, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector128<double> xDouble = x.AsDouble();
                    Vector128<ulong> bits = xDouble.AsUInt64();

                    // General case: negative -> decrement, positive -> increment
                    Vector128<ulong> result = Vector128.ConditionalSelect(
                        Vector128.IsNegative(xDouble).AsUInt64(),
                        bits - Vector128<ulong>.One,
                        bits + Vector128<ulong>.One);

                    // Handle special cases with a single conditional select
                    Vector128<ulong> isNegativeZero = Vector128.Equals(bits, Vector128.Create(0x8000_0000_0000_0000ul));
                    Vector128<ulong> specialValue = Vector128.Create(0x0000_0000_0000_0001ul) & isNegativeZero;

                    Vector128<ulong> isNaNOrPosInf = (Vector128.IsNaN(xDouble) | Vector128.IsPositiveInfinity(xDouble)).AsUInt64();
                    specialValue |= bits & isNaNOrPosInf;

                    Vector128<ulong> specialMask = isNegativeZero | isNaNOrPosInf;
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

                    // General case: negative -> decrement, positive -> increment
                    Vector256<uint> result = Vector256.ConditionalSelect(
                        Vector256.IsNegative(xFloat).AsUInt32(),
                        bits - Vector256<uint>.One,
                        bits + Vector256<uint>.One);

                    // Handle special cases with a single conditional select
                    Vector256<uint> isNegativeZero = Vector256.Equals(bits, Vector256.Create(0x8000_0000u));
                    Vector256<uint> specialValue = Vector256.Create(0x0000_0001u) & isNegativeZero;

                    Vector256<uint> isNaNOrPosInf = (Vector256.IsNaN(xFloat) | Vector256.IsPositiveInfinity(xFloat)).AsUInt32();
                    specialValue |= bits & isNaNOrPosInf;

                    Vector256<uint> specialMask = isNegativeZero | isNaNOrPosInf;
                    return Vector256.ConditionalSelect(specialMask, specialValue, result).AsSingle().As<float, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector256<double> xDouble = x.AsDouble();
                    Vector256<ulong> bits = xDouble.AsUInt64();

                    // General case: negative -> decrement, positive -> increment
                    Vector256<ulong> result = Vector256.ConditionalSelect(
                        Vector256.IsNegative(xDouble).AsUInt64(),
                        bits - Vector256<ulong>.One,
                        bits + Vector256<ulong>.One);

                    // Handle special cases with a single conditional select
                    Vector256<ulong> isNegativeZero = Vector256.Equals(bits, Vector256.Create(0x8000_0000_0000_0000ul));
                    Vector256<ulong> specialValue = Vector256.Create(0x0000_0000_0000_0001ul) & isNegativeZero;

                    Vector256<ulong> isNaNOrPosInf = (Vector256.IsNaN(xDouble) | Vector256.IsPositiveInfinity(xDouble)).AsUInt64();
                    specialValue |= bits & isNaNOrPosInf;

                    Vector256<ulong> specialMask = isNegativeZero | isNaNOrPosInf;
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

                    // General case: negative -> decrement, positive -> increment
                    Vector512<uint> result = Vector512.ConditionalSelect(
                        Vector512.IsNegative(xFloat).AsUInt32(),
                        bits - Vector512<uint>.One,
                        bits + Vector512<uint>.One);

                    // Handle special cases with a single conditional select
                    Vector512<uint> isNegativeZero = Vector512.Equals(bits, Vector512.Create(0x8000_0000u));
                    Vector512<uint> specialValue = Vector512.Create(0x0000_0001u) & isNegativeZero;

                    Vector512<uint> isNaNOrPosInf = (Vector512.IsNaN(xFloat) | Vector512.IsPositiveInfinity(xFloat)).AsUInt32();
                    specialValue |= bits & isNaNOrPosInf;

                    Vector512<uint> specialMask = isNegativeZero | isNaNOrPosInf;
                    return Vector512.ConditionalSelect(specialMask, specialValue, result).AsSingle().As<float, T>();
                }

                if (typeof(T) == typeof(double))
                {
                    Vector512<double> xDouble = x.AsDouble();
                    Vector512<ulong> bits = xDouble.AsUInt64();

                    // General case: negative -> decrement, positive -> increment
                    Vector512<ulong> result = Vector512.ConditionalSelect(
                        Vector512.IsNegative(xDouble).AsUInt64(),
                        bits - Vector512<ulong>.One,
                        bits + Vector512<ulong>.One);

                    // Handle special cases with a single conditional select
                    Vector512<ulong> isNegativeZero = Vector512.Equals(bits, Vector512.Create(0x8000_0000_0000_0000ul));
                    Vector512<ulong> specialValue = Vector512.Create(0x0000_0000_0000_0001ul) & isNegativeZero;

                    Vector512<ulong> isNaNOrPosInf = (Vector512.IsNaN(xDouble) | Vector512.IsPositiveInfinity(xDouble)).AsUInt64();
                    specialValue |= bits & isNaNOrPosInf;

                    Vector512<ulong> specialMask = isNegativeZero | isNaNOrPosInf;
                    return Vector512.ConditionalSelect(specialMask, specialValue, result).AsDouble().As<double, T>();
                }

                // Fallback for unsupported types - should not be reached since Vectorizable returns false
                throw new NotSupportedException();
            }
        }
    }
}
