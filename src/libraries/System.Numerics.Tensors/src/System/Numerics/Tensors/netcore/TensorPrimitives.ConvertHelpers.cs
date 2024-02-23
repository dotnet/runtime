// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Performs conversions that are the same regardless of checked, truncating, or saturation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // at most one of the branches will be kept
        private static bool TryConvertUniversal<TFrom, TTo>(ReadOnlySpan<TFrom> source, Span<TTo> destination)
            where TFrom : INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            if (typeof(TFrom) == typeof(TTo))
            {
                if (source.Length > destination.Length)
                {
                    ThrowHelper.ThrowArgument_DestinationTooShort();
                }

                ValidateInputOutputSpanNonOverlapping(source, Rename<TTo, TFrom>(destination));

                source.CopyTo(Rename<TTo, TFrom>(destination));
                return true;
            }

            if (IsInt32Like<TFrom>() && typeof(TTo) == typeof(float))
            {
                InvokeSpanIntoSpan<int, float, ConvertInt32ToSingle>(Rename<TFrom, int>(source), Rename<TTo, float>(destination));
                return true;
            }

            if (IsUInt32Like<TFrom>() && typeof(TTo) == typeof(float))
            {
                InvokeSpanIntoSpan<uint, float, ConvertUInt32ToSingle>(Rename<TFrom, uint>(source), Rename<TTo, float>(destination));
                return true;
            }

            if (IsInt64Like<TFrom>() && typeof(TTo) == typeof(double))
            {
                InvokeSpanIntoSpan<long, double, ConvertInt64ToDouble>(Rename<TFrom, long>(source), Rename<TTo, double>(destination));
                return true;
            }

            if (IsUInt64Like<TFrom>() && typeof(TTo) == typeof(double))
            {
                InvokeSpanIntoSpan<ulong, double, ConvertUInt64ToDouble>(Rename<TFrom, ulong>(source), Rename<TTo, double>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(float) && typeof(TTo) == typeof(Half))
            {
                InvokeSpanIntoSpan_2to1<float, ushort, NarrowSingleToHalfAsUInt16Operator>(Rename<TFrom, float>(source), Rename<TTo, ushort>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(Half) && typeof(TTo) == typeof(float))
            {
                InvokeSpanIntoSpan_1to2<short, float, WidenHalfAsInt16ToSingleOperator>(Rename<TFrom, short>(source), Rename<TTo, float>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(float) && typeof(TTo) == typeof(double))
            {
                InvokeSpanIntoSpan_1to2<float, double, WidenSingleToDoubleOperator>(Rename<TFrom, float>(source), Rename<TTo, double>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(double) && typeof(TTo) == typeof(float))
            {
                InvokeSpanIntoSpan_2to1<double, float, NarrowDoubleToSingleOperator>(Rename<TFrom, double>(source), Rename<TTo, float>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(byte) && typeof(TTo) == typeof(ushort))
            {
                InvokeSpanIntoSpan_1to2<byte, ushort, WidenByteToUInt16Operator>(Rename<TFrom, byte>(source), Rename<TTo, ushort>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(sbyte) && typeof(TTo) == typeof(short))
            {
                InvokeSpanIntoSpan_1to2<sbyte, short, WidenSByteToInt16Operator>(Rename<TFrom, sbyte>(source), Rename<TTo, short>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(ushort) && IsUInt32Like<TTo>())
            {
                InvokeSpanIntoSpan_1to2<ushort, uint, WidenUInt16ToUInt32Operator>(Rename<TFrom, ushort>(source), Rename<TTo, uint>(destination));
                return true;
            }

            if (typeof(TFrom) == typeof(short) && IsInt32Like<TTo>())
            {
                InvokeSpanIntoSpan_1to2<short, int, WidenInt16ToInt32Operator>(Rename<TFrom, short>(source), Rename<TTo, int>(destination));
                return true;
            }

            if (IsUInt32Like<TTo>() && IsUInt64Like<TTo>())
            {
                InvokeSpanIntoSpan_1to2<uint, ulong, WidenUInt32ToUInt64Operator>(Rename<TFrom, uint>(source), Rename<TTo, ulong>(destination));
                return true;
            }

            if (IsInt32Like<TFrom>() && IsInt64Like<TTo>())
            {
                InvokeSpanIntoSpan_1to2<int, long, WidenInt32ToInt64Operator>(Rename<TFrom, int>(source), Rename<TTo, long>(destination));
                return true;
            }

            return false;
        }

        /// <summary>(int)float</summary>
        private readonly struct ConvertInt32ToSingle : IUnaryOperator<int, float>
        {
            public static bool Vectorizable => true;

            public static float Invoke(int x) => x;
            public static Vector128<float> Invoke(Vector128<int> x) => Vector128.ConvertToSingle(x);
            public static Vector256<float> Invoke(Vector256<int> x) => Vector256.ConvertToSingle(x);
            public static Vector512<float> Invoke(Vector512<int> x) => Vector512.ConvertToSingle(x);
        }

        /// <summary>(uint)float</summary>
        private readonly struct ConvertUInt32ToSingle : IUnaryOperator<uint, float>
        {
            public static bool Vectorizable => true;

            public static float Invoke(uint x) => x;
            public static Vector128<float> Invoke(Vector128<uint> x) => Vector128.ConvertToSingle(x);
            public static Vector256<float> Invoke(Vector256<uint> x) => Vector256.ConvertToSingle(x);
            public static Vector512<float> Invoke(Vector512<uint> x) => Vector512.ConvertToSingle(x);
        }

        /// <summary>(double)ulong</summary>
        private readonly struct ConvertUInt64ToDouble : IUnaryOperator<ulong, double>
        {
            public static bool Vectorizable => true;

            public static double Invoke(ulong x) => x;
            public static Vector128<double> Invoke(Vector128<ulong> x) => Vector128.ConvertToDouble(x);
            public static Vector256<double> Invoke(Vector256<ulong> x) => Vector256.ConvertToDouble(x);
            public static Vector512<double> Invoke(Vector512<ulong> x) => Vector512.ConvertToDouble(x);
        }

        /// <summary>(double)long</summary>
        private readonly struct ConvertInt64ToDouble : IUnaryOperator<long, double>
        {
            public static bool Vectorizable => true;

            public static double Invoke(long x) => x;
            public static Vector128<double> Invoke(Vector128<long> x) => Vector128.ConvertToDouble(x);
            public static Vector256<double> Invoke(Vector256<long> x) => Vector256.ConvertToDouble(x);
            public static Vector512<double> Invoke(Vector512<long> x) => Vector512.ConvertToDouble(x);
        }

        /// <summary>(double)float</summary>
        private readonly struct WidenSingleToDoubleOperator : IUnaryOneToTwoOperator<float, double>
        {
            public static bool Vectorizable => true;

            public static double Invoke(float x) => x;
            public static (Vector128<double> Lower, Vector128<double> Upper) Invoke(Vector128<float> x) => Vector128.Widen(x);
            public static (Vector256<double> Lower, Vector256<double> Upper) Invoke(Vector256<float> x) => Vector256.Widen(x);
            public static (Vector512<double> Lower, Vector512<double> Upper) Invoke(Vector512<float> x) => Vector512.Widen(x);
        }

        /// <summary>(float)double</summary>
        private readonly struct NarrowDoubleToSingleOperator : IUnaryTwoToOneOperator<double, float>
        {
            public static bool Vectorizable => true;

            public static float Invoke(double x) => (float)x;
            public static Vector128<float> Invoke(Vector128<double> lower, Vector128<double> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<float> Invoke(Vector256<double> lower, Vector256<double> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<float> Invoke(Vector512<double> lower, Vector512<double> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(ushort)byte</summary>
        private readonly struct WidenByteToUInt16Operator : IUnaryOneToTwoOperator<byte, ushort>
        {
            public static bool Vectorizable => true;

            public static ushort Invoke(byte x) => x;
            public static (Vector128<ushort> Lower, Vector128<ushort> Upper) Invoke(Vector128<byte> x) => Vector128.Widen(x);
            public static (Vector256<ushort> Lower, Vector256<ushort> Upper) Invoke(Vector256<byte> x) => Vector256.Widen(x);
            public static (Vector512<ushort> Lower, Vector512<ushort> Upper) Invoke(Vector512<byte> x) => Vector512.Widen(x);
        }

        /// <summary>(short)sbyte</summary>
        private readonly struct WidenSByteToInt16Operator : IUnaryOneToTwoOperator<sbyte, short>
        {
            public static bool Vectorizable => true;

            public static short Invoke(sbyte x) => x;
            public static (Vector128<short> Lower, Vector128<short> Upper) Invoke(Vector128<sbyte> x) => Vector128.Widen(x);
            public static (Vector256<short> Lower, Vector256<short> Upper) Invoke(Vector256<sbyte> x) => Vector256.Widen(x);
            public static (Vector512<short> Lower, Vector512<short> Upper) Invoke(Vector512<sbyte> x) => Vector512.Widen(x);
        }

        /// <summary>(uint)ushort</summary>
        private readonly struct WidenUInt16ToUInt32Operator : IUnaryOneToTwoOperator<ushort, uint>
        {
            public static bool Vectorizable => true;

            public static uint Invoke(ushort x) => x;
            public static (Vector128<uint> Lower, Vector128<uint> Upper) Invoke(Vector128<ushort> x) => Vector128.Widen(x);
            public static (Vector256<uint> Lower, Vector256<uint> Upper) Invoke(Vector256<ushort> x) => Vector256.Widen(x);
            public static (Vector512<uint> Lower, Vector512<uint> Upper) Invoke(Vector512<ushort> x) => Vector512.Widen(x);
        }

        /// <summary>(int)short</summary>
        private readonly struct WidenInt16ToInt32Operator : IUnaryOneToTwoOperator<short, int>
        {
            public static bool Vectorizable => true;

            public static int Invoke(short x) => x;
            public static (Vector128<int> Lower, Vector128<int> Upper) Invoke(Vector128<short> x) => Vector128.Widen(x);
            public static (Vector256<int> Lower, Vector256<int> Upper) Invoke(Vector256<short> x) => Vector256.Widen(x);
            public static (Vector512<int> Lower, Vector512<int> Upper) Invoke(Vector512<short> x) => Vector512.Widen(x);
        }

        /// <summary>(ulong)uint</summary>
        private readonly struct WidenUInt32ToUInt64Operator : IUnaryOneToTwoOperator<uint, ulong>
        {
            public static bool Vectorizable => true;

            public static ulong Invoke(uint x) => x;
            public static (Vector128<ulong> Lower, Vector128<ulong> Upper) Invoke(Vector128<uint> x) => Vector128.Widen(x);
            public static (Vector256<ulong> Lower, Vector256<ulong> Upper) Invoke(Vector256<uint> x) => Vector256.Widen(x);
            public static (Vector512<ulong> Lower, Vector512<ulong> Upper) Invoke(Vector512<uint> x) => Vector512.Widen(x);
        }

        /// <summary>(long)int</summary>
        private readonly struct WidenInt32ToInt64Operator : IUnaryOneToTwoOperator<int, long>
        {
            public static bool Vectorizable => true;

            public static long Invoke(int x) => x;
            public static (Vector128<long> Lower, Vector128<long> Upper) Invoke(Vector128<int> x) => Vector128.Widen(x);
            public static (Vector256<long> Lower, Vector256<long> Upper) Invoke(Vector256<int> x) => Vector256.Widen(x);
            public static (Vector512<long> Lower, Vector512<long> Upper) Invoke(Vector512<int> x) => Vector512.Widen(x);
        }

        private readonly struct WidenHalfAsInt16ToSingleOperator : IUnaryOneToTwoOperator<short, float>
        {
            // This implements a vectorized version of the `explicit operator float(Half value) operator`.
            // See detailed description of the algorithm used here:
            //     https://github.com/dotnet/runtime/blob/3bf40a378f00cb5bf18ff62796bc7097719b974c/src/libraries/System.Private.CoreLib/src/System/Half.cs#L1010-L1040
            // The cast operator converts a Half represented as uint to a float. This does the same, with an input VectorXx<uint> and an output VectorXx<float>.
            // The VectorXx<uint> is created by reading a vector of Halfs as a VectorXx<short> then widened to two VectorXx<int>s and cast to VectorXx<uint>s.
            // We loop handling one input vector at a time, producing two output float vectors.

            private const uint ExponentLowerBound = 0x3880_0000u; // The smallest positive normal number in Half, converted to Single
            private const uint ExponentOffset = 0x3800_0000u; // BitConverter.SingleToUInt32Bits(1.0f) - ((uint)BitConverter.HalfToUInt16Bits((Half)1.0f) << 13)
            private const uint SingleSignMask = 0x8000_0000; // float.SignMask; // Mask for sign bit in Single
            private const uint HalfExponentMask = 0x7C00; // Mask for exponent bits in Half
            private const uint HalfToSingleBitsMask = 0x0FFF_E000; // Mask for bits in Single converted from Half

            public static bool Vectorizable => true;

            public static float Invoke(short x) => (float)Unsafe.BitCast<short, Half>(x);

            public static (Vector128<float> Lower, Vector128<float> Upper) Invoke(Vector128<short> x)
            {
                (Vector128<int> lowerInt32, Vector128<int> upperInt32) = Vector128.Widen(x);
                return
                    (HalfAsWidenedUInt32ToSingle(lowerInt32.AsUInt32()),
                     HalfAsWidenedUInt32ToSingle(upperInt32.AsUInt32()));

                static Vector128<float> HalfAsWidenedUInt32ToSingle(Vector128<uint> value)
                {
                    // Extract sign bit of value
                    Vector128<uint> sign = value & Vector128.Create(SingleSignMask);

                    // Copy sign bit to upper bits
                    Vector128<uint> bitValueInProcess = value;

                    // Extract exponent bits of value (BiasedExponent is not for here as it performs unnecessary shift)
                    Vector128<uint> offsetExponent = bitValueInProcess & Vector128.Create(HalfExponentMask);

                    // ~0u when value is subnormal, 0 otherwise
                    Vector128<uint> subnormalMask = Vector128.Equals(offsetExponent, Vector128<uint>.Zero);

                    // ~0u when value is either Infinity or NaN, 0 otherwise
                    Vector128<uint> infinityOrNaNMask = Vector128.Equals(offsetExponent, Vector128.Create(HalfExponentMask));

                    // 0x3880_0000u if value is subnormal, 0 otherwise
                    Vector128<uint> maskedExponentLowerBound = subnormalMask & Vector128.Create(ExponentLowerBound);

                    // 0x3880_0000u if value is subnormal, 0x3800_0000u otherwise
                    Vector128<uint> offsetMaskedExponentLowerBound = Vector128.Create(ExponentOffset) | maskedExponentLowerBound;

                    // Match the position of the boundary of exponent bits and fraction bits with IEEE 754 Binary32(Single)
                    bitValueInProcess = Vector128.ShiftLeft(bitValueInProcess, 13);

                    // Double the offsetMaskedExponentLowerBound if value is either Infinity or NaN
                    offsetMaskedExponentLowerBound = Vector128.ConditionalSelect(Vector128.Equals(infinityOrNaNMask, Vector128<uint>.Zero),
                        offsetMaskedExponentLowerBound,
                        Vector128.ShiftLeft(offsetMaskedExponentLowerBound, 1));

                    // Extract exponent bits and fraction bits of value
                    bitValueInProcess &= Vector128.Create(HalfToSingleBitsMask);

                    // Adjust exponent to match the range of exponent
                    bitValueInProcess += offsetMaskedExponentLowerBound;

                    // If value is subnormal, remove unnecessary 1 on top of fraction bits.
                    Vector128<uint> absoluteValue = (bitValueInProcess.AsSingle() - maskedExponentLowerBound.AsSingle()).AsUInt32();

                    // Merge sign bit with rest
                    return (absoluteValue | sign).AsSingle();
                }
            }

            public static (Vector256<float> Lower, Vector256<float> Upper) Invoke(Vector256<short> x)
            {
                (Vector256<int> lowerInt32, Vector256<int> upperInt32) = Vector256.Widen(x);
                return
                    (HalfAsWidenedUInt32ToSingle(lowerInt32.AsUInt32()),
                     HalfAsWidenedUInt32ToSingle(upperInt32.AsUInt32()));

                static Vector256<float> HalfAsWidenedUInt32ToSingle(Vector256<uint> value)
                {
                    // Extract sign bit of value
                    Vector256<uint> sign = value & Vector256.Create(SingleSignMask);

                    // Copy sign bit to upper bits
                    Vector256<uint> bitValueInProcess = value;

                    // Extract exponent bits of value (BiasedExponent is not for here as it performs unnecessary shift)
                    Vector256<uint> offsetExponent = bitValueInProcess & Vector256.Create(HalfExponentMask);

                    // ~0u when value is subnormal, 0 otherwise
                    Vector256<uint> subnormalMask = Vector256.Equals(offsetExponent, Vector256<uint>.Zero);

                    // ~0u when value is either Infinity or NaN, 0 otherwise
                    Vector256<uint> infinityOrNaNMask = Vector256.Equals(offsetExponent, Vector256.Create(HalfExponentMask));

                    // 0x3880_0000u if value is subnormal, 0 otherwise
                    Vector256<uint> maskedExponentLowerBound = subnormalMask & Vector256.Create(ExponentLowerBound);

                    // 0x3880_0000u if value is subnormal, 0x3800_0000u otherwise
                    Vector256<uint> offsetMaskedExponentLowerBound = Vector256.Create(ExponentOffset) | maskedExponentLowerBound;

                    // Match the position of the boundary of exponent bits and fraction bits with IEEE 754 Binary32(Single)
                    bitValueInProcess = Vector256.ShiftLeft(bitValueInProcess, 13);

                    // Double the offsetMaskedExponentLowerBound if value is either Infinity or NaN
                    offsetMaskedExponentLowerBound = Vector256.ConditionalSelect(Vector256.Equals(infinityOrNaNMask, Vector256<uint>.Zero),
                        offsetMaskedExponentLowerBound,
                        Vector256.ShiftLeft(offsetMaskedExponentLowerBound, 1));

                    // Extract exponent bits and fraction bits of value
                    bitValueInProcess &= Vector256.Create(HalfToSingleBitsMask);

                    // Adjust exponent to match the range of exponent
                    bitValueInProcess += offsetMaskedExponentLowerBound;

                    // If value is subnormal, remove unnecessary 1 on top of fraction bits.
                    Vector256<uint> absoluteValue = (bitValueInProcess.AsSingle() - maskedExponentLowerBound.AsSingle()).AsUInt32();

                    // Merge sign bit with rest
                    return (absoluteValue | sign).AsSingle();
                }
            }

            public static (Vector512<float> Lower, Vector512<float> Upper) Invoke(Vector512<short> x)
            {
                (Vector512<int> lowerInt32, Vector512<int> upperInt32) = Vector512.Widen(x);
                return
                    (HalfAsWidenedUInt32ToSingle(lowerInt32.AsUInt32()),
                     HalfAsWidenedUInt32ToSingle(upperInt32.AsUInt32()));

                static Vector512<float> HalfAsWidenedUInt32ToSingle(Vector512<uint> value)
                {
                    // Extract sign bit of value
                    Vector512<uint> sign = value & Vector512.Create(SingleSignMask);

                    // Copy sign bit to upper bits
                    Vector512<uint> bitValueInProcess = value;

                    // Extract exponent bits of value (BiasedExponent is not for here as it performs unnecessary shift)
                    Vector512<uint> offsetExponent = bitValueInProcess & Vector512.Create(HalfExponentMask);

                    // ~0u when value is subnormal, 0 otherwise
                    Vector512<uint> subnormalMask = Vector512.Equals(offsetExponent, Vector512<uint>.Zero);

                    // ~0u when value is either Infinity or NaN, 0 otherwise
                    Vector512<uint> infinityOrNaNMask = Vector512.Equals(offsetExponent, Vector512.Create(HalfExponentMask));

                    // 0x3880_0000u if value is subnormal, 0 otherwise
                    Vector512<uint> maskedExponentLowerBound = subnormalMask & Vector512.Create(ExponentLowerBound);

                    // 0x3880_0000u if value is subnormal, 0x3800_0000u otherwise
                    Vector512<uint> offsetMaskedExponentLowerBound = Vector512.Create(ExponentOffset) | maskedExponentLowerBound;

                    // Match the position of the boundary of exponent bits and fraction bits with IEEE 754 Binary32(Single)
                    bitValueInProcess = Vector512.ShiftLeft(bitValueInProcess, 13);

                    // Double the offsetMaskedExponentLowerBound if value is either Infinity or NaN
                    offsetMaskedExponentLowerBound = Vector512.ConditionalSelect(Vector512.Equals(infinityOrNaNMask, Vector512<uint>.Zero),
                        offsetMaskedExponentLowerBound,
                        Vector512.ShiftLeft(offsetMaskedExponentLowerBound, 1));

                    // Extract exponent bits and fraction bits of value
                    bitValueInProcess &= Vector512.Create(HalfToSingleBitsMask);

                    // Adjust exponent to match the range of exponent
                    bitValueInProcess += offsetMaskedExponentLowerBound;

                    // If value is subnormal, remove unnecessary 1 on top of fraction bits.
                    Vector512<uint> absoluteValue = (bitValueInProcess.AsSingle() - maskedExponentLowerBound.AsSingle()).AsUInt32();

                    // Merge sign bit with rest
                    return (absoluteValue | sign).AsSingle();
                }
            }
        }

        private readonly struct NarrowSingleToHalfAsUInt16Operator : IUnaryTwoToOneOperator<float, ushort>
        {
            // This implements a vectorized version of the `explicit operator Half(float value) operator`.
            // See detailed description of the algorithm used here:
            //     https://github.com/dotnet/runtime/blob/ca8d6f0420096831766ec11c7d400e4f7ccc7a34/src/libraries/System.Private.CoreLib/src/System/Half.cs#L606-L714
            // The cast operator converts a float to a Half represented as a UInt32, then narrows to a UInt16, and reinterpret casts to Half.
            // This does the same, with an input VectorXx<float> and an output VectorXx<uint>.
            // Loop handling two input vectors at a time; each input float is double the size of each output Half,
            // so we need two vectors of floats to produce one vector of Halfs. Half isn't supported in VectorXx<T>,
            // so we convert the VectorXx<float> to a VectorXx<uint>, and the caller then uses this twice, narrows the combination
            // into a VectorXx<ushort>, and then saves that out to the destination `ref Half` reinterpreted as `ref ushort`.

            private const uint MinExp = 0x3880_0000u; // Minimum exponent for rounding
            private const uint Exponent126 = 0x3f00_0000u; // Exponent displacement #1
            private const uint SingleBiasedExponentMask = 0x7F80_0000; // float.BiasedExponentMask; // Exponent mask
            private const uint Exponent13 = 0x0680_0000u; // Exponent displacement #2
            private const float MaxHalfValueBelowInfinity = 65520.0f; // Maximum value that is not Infinity in Half
            private const uint ExponentMask = 0x7C00; // Mask for exponent bits in Half
            private const uint SingleSignMask = 0x8000_0000u; // float.SignMask; // Mask for sign bit in float

            public static bool Vectorizable => true;

            public static ushort Invoke(float x) => Unsafe.BitCast<Half, ushort>((Half)x);

            public static Vector128<ushort> Invoke(Vector128<float> lower, Vector128<float> upper)
            {
                return Vector128.Narrow(
                    SingleToHalfAsWidenedUInt32(lower),
                    SingleToHalfAsWidenedUInt32(upper));

                static Vector128<uint> SingleToHalfAsWidenedUInt32(Vector128<float> value)
                {
                    Vector128<uint> bitValue = value.AsUInt32();

                    // Extract sign bit
                    Vector128<uint> sign = Vector128.ShiftRightLogical(bitValue & Vector128.Create(SingleSignMask), 16);

                    // Detecting NaN (0u if value is NaN; otherwise, ~0u)
                    Vector128<uint> realMask = Vector128.Equals(value, value).AsUInt32();

                    // Clear sign bit
                    value = Vector128.Abs(value);

                    // Rectify values that are Infinity in Half.
                    value = Vector128.Min(Vector128.Create(MaxHalfValueBelowInfinity), value);

                    // Rectify lower exponent
                    Vector128<uint> exponentOffset0 = Vector128.Max(value, Vector128.Create(MinExp).AsSingle()).AsUInt32();

                    // Extract exponent
                    exponentOffset0 &= Vector128.Create(SingleBiasedExponentMask);

                    // Add exponent by 13
                    exponentOffset0 += Vector128.Create(Exponent13);

                    // Round Single into Half's precision (NaN also gets modified here, just setting the MSB of fraction)
                    value += exponentOffset0.AsSingle();
                    bitValue = value.AsUInt32();

                    // Only exponent bits will be modified if NaN
                    Vector128<uint> maskedHalfExponentForNaN = ~realMask & Vector128.Create(ExponentMask);

                    // Subtract exponent by 126
                    bitValue -= Vector128.Create(Exponent126);

                    // Shift bitValue right by 13 bits to match the boundary of exponent part and fraction part.
                    Vector128<uint> newExponent = Vector128.ShiftRightLogical(bitValue, 13);

                    // Clear the fraction parts if the value was NaN.
                    bitValue &= realMask;

                    // Merge the exponent part with fraction part, and add the exponent part and fraction part's overflow.
                    bitValue += newExponent;

                    // Clear exponents if value is NaN
                    bitValue &= ~maskedHalfExponentForNaN;

                    // Merge sign bit with possible NaN exponent
                    Vector128<uint> signAndMaskedExponent = maskedHalfExponentForNaN | sign;

                    // Merge sign bit and possible NaN exponent
                    bitValue |= signAndMaskedExponent;

                    // The final result
                    return bitValue;
                }
            }

            public static Vector256<ushort> Invoke(Vector256<float> lower, Vector256<float> upper)
            {
                return Vector256.Narrow(
                    SingleToHalfAsWidenedUInt32(lower),
                    SingleToHalfAsWidenedUInt32(upper));

                static Vector256<uint> SingleToHalfAsWidenedUInt32(Vector256<float> value)
                {
                    Vector256<uint> bitValue = value.AsUInt32();

                    // Extract sign bit
                    Vector256<uint> sign = Vector256.ShiftRightLogical(bitValue & Vector256.Create(SingleSignMask), 16);

                    // Detecting NaN (0u if value is NaN; otherwise, ~0u)
                    Vector256<uint> realMask = Vector256.Equals(value, value).AsUInt32();

                    // Clear sign bit
                    value = Vector256.Abs(value);

                    // Rectify values that are Infinity in Half.
                    value = Vector256.Min(Vector256.Create(MaxHalfValueBelowInfinity), value);

                    // Rectify lower exponent
                    Vector256<uint> exponentOffset0 = Vector256.Max(value, Vector256.Create(MinExp).AsSingle()).AsUInt32();

                    // Extract exponent
                    exponentOffset0 &= Vector256.Create(SingleBiasedExponentMask);

                    // Add exponent by 13
                    exponentOffset0 += Vector256.Create(Exponent13);

                    // Round Single into Half's precision (NaN also gets modified here, just setting the MSB of fraction)
                    value += exponentOffset0.AsSingle();
                    bitValue = value.AsUInt32();

                    // Only exponent bits will be modified if NaN
                    Vector256<uint> maskedHalfExponentForNaN = ~realMask & Vector256.Create(ExponentMask);

                    // Subtract exponent by 126
                    bitValue -= Vector256.Create(Exponent126);

                    // Shift bitValue right by 13 bits to match the boundary of exponent part and fraction part.
                    Vector256<uint> newExponent = Vector256.ShiftRightLogical(bitValue, 13);

                    // Clear the fraction parts if the value was NaN.
                    bitValue &= realMask;

                    // Merge the exponent part with fraction part, and add the exponent part and fraction part's overflow.
                    bitValue += newExponent;

                    // Clear exponents if value is NaN
                    bitValue &= ~maskedHalfExponentForNaN;

                    // Merge sign bit with possible NaN exponent
                    Vector256<uint> signAndMaskedExponent = maskedHalfExponentForNaN | sign;

                    // Merge sign bit and possible NaN exponent
                    bitValue |= signAndMaskedExponent;

                    // The final result
                    return bitValue;
                }
            }

            public static Vector512<ushort> Invoke(Vector512<float> lower, Vector512<float> upper)
            {
                return Vector512.Narrow(
                    SingleToHalfAsWidenedUInt32(lower),
                    SingleToHalfAsWidenedUInt32(upper));

                static Vector512<uint> SingleToHalfAsWidenedUInt32(Vector512<float> value)
                {
                    Vector512<uint> bitValue = value.AsUInt32();

                    // Extract sign bit
                    Vector512<uint> sign = Vector512.ShiftRightLogical(bitValue & Vector512.Create(SingleSignMask), 16);

                    // Detecting NaN (0u if value is NaN; otherwise, ~0u)
                    Vector512<uint> realMask = Vector512.Equals(value, value).AsUInt32();

                    // Clear sign bit
                    value = Vector512.Abs(value);

                    // Rectify values that are Infinity in Half.
                    value = Vector512.Min(Vector512.Create(MaxHalfValueBelowInfinity), value);

                    // Rectify lower exponent
                    Vector512<uint> exponentOffset0 = Vector512.Max(value, Vector512.Create(MinExp).AsSingle()).AsUInt32();

                    // Extract exponent
                    exponentOffset0 &= Vector512.Create(SingleBiasedExponentMask);

                    // Add exponent by 13
                    exponentOffset0 += Vector512.Create(Exponent13);

                    // Round Single into Half's precision (NaN also gets modified here, just setting the MSB of fraction)
                    value += exponentOffset0.AsSingle();
                    bitValue = value.AsUInt32();

                    // Only exponent bits will be modified if NaN
                    Vector512<uint> maskedHalfExponentForNaN = ~realMask & Vector512.Create(ExponentMask);

                    // Subtract exponent by 126
                    bitValue -= Vector512.Create(Exponent126);

                    // Shift bitValue right by 13 bits to match the boundary of exponent part and fraction part.
                    Vector512<uint> newExponent = Vector512.ShiftRightLogical(bitValue, 13);

                    // Clear the fraction parts if the value was NaN.
                    bitValue &= realMask;

                    // Merge the exponent part with fraction part, and add the exponent part and fraction part's overflow.
                    bitValue += newExponent;

                    // Clear exponents if value is NaN
                    bitValue &= ~maskedHalfExponentForNaN;

                    // Merge sign bit with possible NaN exponent
                    Vector512<uint> signAndMaskedExponent = maskedHalfExponentForNaN | sign;

                    // Merge sign bit and possible NaN exponent
                    bitValue |= signAndMaskedExponent;

                    // The final result
                    return bitValue;
                }
            }
        }

        /// <summary>Creates a span of <typeparamref name="TTo"/> from a <typeparamref name="TTo"/> when they're the same type.</summary>
        private static unsafe ReadOnlySpan<TTo> Rename<TFrom, TTo>(ReadOnlySpan<TFrom> span)
        {
            Debug.Assert(sizeof(TFrom) == sizeof(TTo));
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(span)), span.Length);
        }

        /// <summary>Gets whether <typeparamref name="T"/> is <see cref="uint"/> or <see cref="nuint"/> if in a 32-bit process.</summary>
        private static bool IsUInt32Like<T>() => typeof(T) == typeof(uint) || (IntPtr.Size == 4 && typeof(T) == typeof(nuint));

        /// <summary>Gets whether <typeparamref name="T"/> is <see cref="int"/> or <see cref="nint"/> if in a 32-bit process.</summary>
        private static bool IsInt32Like<T>() => typeof(T) == typeof(int) || (IntPtr.Size == 4 && typeof(T) == typeof(nint));

        /// <summary>Gets whether <typeparamref name="T"/> is <see cref="ulong"/> or <see cref="nuint"/> if in a 64-bit process.</summary>
        private static bool IsUInt64Like<T>() => typeof(T) == typeof(ulong) || (IntPtr.Size == 8 && typeof(T) == typeof(nuint));

        /// <summary>Gets whether <typeparamref name="T"/> is <see cref="long"/> or <see cref="nint"/> if in a 64-bit process.</summary>
        private static bool IsInt64Like<T>() => typeof(T) == typeof(long) || (IntPtr.Size == 8 && typeof(T) == typeof(nint));
    }
}
