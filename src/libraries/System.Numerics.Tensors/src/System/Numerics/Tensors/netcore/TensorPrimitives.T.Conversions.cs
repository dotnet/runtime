// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        // Defines vectorizable operators for applying conversions between numeric types.

        /// <summary>T.CreateChecked(x)</summary>
        internal readonly struct ConvertCheckedFallbackOperator<TFrom, TTo> : IUnaryOperator<TFrom, TTo> where TFrom : INumberBase<TFrom> where TTo : INumberBase<TTo>
        {
            public static bool Vectorizable => false;

            public static TTo Invoke(TFrom x) => TTo.CreateChecked(x);
            public static Vector128<TTo> Invoke(Vector128<TFrom> x) => throw new NotSupportedException();
            public static Vector256<TTo> Invoke(Vector256<TFrom> x) => throw new NotSupportedException();
            public static Vector512<TTo> Invoke(Vector512<TFrom> x) => throw new NotSupportedException();
        }

        /// <summary>T.CreateSaturating(x)</summary>
        internal readonly struct ConvertSaturatingFallbackOperator<TFrom, TTo> : IUnaryOperator<TFrom, TTo> where TFrom : INumberBase<TFrom> where TTo : INumberBase<TTo>
        {
            public static bool Vectorizable => false;

            public static TTo Invoke(TFrom x) => TTo.CreateSaturating(x);
            public static Vector128<TTo> Invoke(Vector128<TFrom> x) => throw new NotSupportedException();
            public static Vector256<TTo> Invoke(Vector256<TFrom> x) => throw new NotSupportedException();
            public static Vector512<TTo> Invoke(Vector512<TFrom> x) => throw new NotSupportedException();
        }

        /// <summary>T.CreateTruncating(x)</summary>
        internal readonly struct ConvertTruncatingFallbackOperator<TFrom, TTo> : IUnaryOperator<TFrom, TTo> where TFrom : INumberBase<TFrom> where TTo : INumberBase<TTo>
        {
            public static bool Vectorizable => false;

            public static TTo Invoke(TFrom x) => TTo.CreateTruncating(x);
            public static Vector128<TTo> Invoke(Vector128<TFrom> x) => throw new NotSupportedException();
            public static Vector256<TTo> Invoke(Vector256<TFrom> x) => throw new NotSupportedException();
            public static Vector512<TTo> Invoke(Vector512<TFrom> x) => throw new NotSupportedException();
        }

        /// <summary>(uint)float</summary>
        internal readonly struct ConvertUInt32ToSingle : IUnaryOperator<uint, float>
        {
            public static bool Vectorizable => true;

            public static float Invoke(uint x) => x;
            public static Vector128<float> Invoke(Vector128<uint> x) => Vector128.ConvertToSingle(x);
            public static Vector256<float> Invoke(Vector256<uint> x) => Vector256.ConvertToSingle(x);
            public static Vector512<float> Invoke(Vector512<uint> x) => Vector512.ConvertToSingle(x);
        }

        /// <summary>(int)float</summary>
        internal readonly struct ConvertInt32ToSingle : IUnaryOperator<int, float>
        {
            public static bool Vectorizable => true;

            public static float Invoke(int x) => x;
            public static Vector128<float> Invoke(Vector128<int> x) => Vector128.ConvertToSingle(x);
            public static Vector256<float> Invoke(Vector256<int> x) => Vector256.ConvertToSingle(x);
            public static Vector512<float> Invoke(Vector512<int> x) => Vector512.ConvertToSingle(x);
        }

        /// <summary>(float)uint</summary>
        internal readonly struct ConvertSingleToUInt32 : IUnaryOperator<float, uint>
        {
            public static bool Vectorizable => false; // TODO https://github.com/dotnet/runtime/pull/97529: make this true once vectorized behavior matches scalar

            public static uint Invoke(float x) => uint.CreateTruncating(x);
            public static Vector128<uint> Invoke(Vector128<float> x) => Vector128.ConvertToUInt32(x);
            public static Vector256<uint> Invoke(Vector256<float> x) => Vector256.ConvertToUInt32(x);
            public static Vector512<uint> Invoke(Vector512<float> x) => Vector512.ConvertToUInt32(x);
        }

        /// <summary>(float)int</summary>
        internal readonly struct ConvertSingleToInt32 : IUnaryOperator<float, int>
        {
            public static bool Vectorizable => false; // TODO https://github.com/dotnet/runtime/pull/97529: make this true once vectorized behavior matches scalar

            public static int Invoke(float x) => int.CreateTruncating(x);
            public static Vector128<int> Invoke(Vector128<float> x) => Vector128.ConvertToInt32(x);
            public static Vector256<int> Invoke(Vector256<float> x) => Vector256.ConvertToInt32(x);
            public static Vector512<int> Invoke(Vector512<float> x) => Vector512.ConvertToInt32(x);
        }

        /// <summary>(double)ulong</summary>
        internal readonly struct ConvertUInt64ToDouble : IUnaryOperator<ulong, double>
        {
            public static bool Vectorizable => true;

            public static double Invoke(ulong x) => x;
            public static Vector128<double> Invoke(Vector128<ulong> x) => Vector128.ConvertToDouble(x);
            public static Vector256<double> Invoke(Vector256<ulong> x) => Vector256.ConvertToDouble(x);
            public static Vector512<double> Invoke(Vector512<ulong> x) => Vector512.ConvertToDouble(x);
        }

        /// <summary>(double)long</summary>
        internal readonly struct ConvertInt64ToDouble : IUnaryOperator<long, double>
        {
            public static bool Vectorizable => true;

            public static double Invoke(long x) => x;
            public static Vector128<double> Invoke(Vector128<long> x) => Vector128.ConvertToDouble(x);
            public static Vector256<double> Invoke(Vector256<long> x) => Vector256.ConvertToDouble(x);
            public static Vector512<double> Invoke(Vector512<long> x) => Vector512.ConvertToDouble(x);
        }

        /// <summary>(ulong)double</summary>
        internal readonly struct ConvertDoubleToUInt64 : IUnaryOperator<double, ulong>
        {
            public static bool Vectorizable => false; // TODO https://github.com/dotnet/runtime/pull/97529: make this true once vectorized behavior matches scalar

            public static ulong Invoke(double x) => ulong.CreateTruncating(x);
            public static Vector128<ulong> Invoke(Vector128<double> x) => Vector128.ConvertToUInt64(x);
            public static Vector256<ulong> Invoke(Vector256<double> x) => Vector256.ConvertToUInt64(x);
            public static Vector512<ulong> Invoke(Vector512<double> x) => Vector512.ConvertToUInt64(x);
        }

        /// <summary>(long)double</summary>
        internal readonly struct ConvertDoubleToInt64 : IUnaryOperator<double, long>
        {
            public static bool Vectorizable => false; // TODO https://github.com/dotnet/runtime/pull/97529: make this true once vectorized behavior matches scalar

            public static long Invoke(double x) => long.CreateTruncating(x);
            public static Vector128<long> Invoke(Vector128<double> x) => Vector128.ConvertToInt64(x);
            public static Vector256<long> Invoke(Vector256<double> x) => Vector256.ConvertToInt64(x);
            public static Vector512<long> Invoke(Vector512<double> x) => Vector512.ConvertToInt64(x);
        }

        /// <summary>(double)float</summary>
        internal readonly struct WidenSingleToDoubleOperator : IUnaryOneToTwoOperator<float, double>
        {
            public static bool Vectorizable => true;

            public static double Invoke(float x) => x;
            public static (Vector128<double> Lower, Vector128<double> Upper) Invoke(Vector128<float> x) => Vector128.Widen(x);
            public static (Vector256<double> Lower, Vector256<double> Upper) Invoke(Vector256<float> x) => Vector256.Widen(x);
            public static (Vector512<double> Lower, Vector512<double> Upper) Invoke(Vector512<float> x) => Vector512.Widen(x);
        }

        /// <summary>(float)double</summary>
        internal readonly struct NarrowDoubleToSingleOperator : IUnaryTwoToOneOperator<double, float>
        {
            public static bool Vectorizable => true;

            public static float Invoke(double x) => (float)x;
            public static Vector128<float> Invoke(Vector128<double> lower, Vector128<double> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<float> Invoke(Vector256<double> lower, Vector256<double> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<float> Invoke(Vector512<double> lower, Vector512<double> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(ushort)byte</summary>
        internal readonly struct WidenByteToUInt16Operator : IUnaryOneToTwoOperator<byte, ushort>
        {
            public static bool Vectorizable => true;

            public static ushort Invoke(byte x) => x;
            public static (Vector128<ushort> Lower, Vector128<ushort> Upper) Invoke(Vector128<byte> x) => Vector128.Widen(x);
            public static (Vector256<ushort> Lower, Vector256<ushort> Upper) Invoke(Vector256<byte> x) => Vector256.Widen(x);
            public static (Vector512<ushort> Lower, Vector512<ushort> Upper) Invoke(Vector512<byte> x) => Vector512.Widen(x);
        }

        /// <summary>(byte)ushort</summary>
        internal readonly struct NarrowUInt16ToByteOperator : IUnaryTwoToOneOperator<ushort, byte>
        {
            public static bool Vectorizable => true;

            public static byte Invoke(ushort x) => (byte)x;
            public static Vector128<byte> Invoke(Vector128<ushort> lower, Vector128<ushort> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<byte> Invoke(Vector256<ushort> lower, Vector256<ushort> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<byte> Invoke(Vector512<ushort> lower, Vector512<ushort> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(short)sbyte</summary>
        internal readonly struct WidenSByteToInt16Operator : IUnaryOneToTwoOperator<sbyte, short>
        {
            public static bool Vectorizable => true;

            public static short Invoke(sbyte x) => x;
            public static (Vector128<short> Lower, Vector128<short> Upper) Invoke(Vector128<sbyte> x) => Vector128.Widen(x);
            public static (Vector256<short> Lower, Vector256<short> Upper) Invoke(Vector256<sbyte> x) => Vector256.Widen(x);
            public static (Vector512<short> Lower, Vector512<short> Upper) Invoke(Vector512<sbyte> x) => Vector512.Widen(x);
        }

        /// <summary>(sbyte)short</summary>
        internal readonly struct NarrowInt16ToSByteOperator : IUnaryTwoToOneOperator<short, sbyte>
        {
            public static bool Vectorizable => true;

            public static sbyte Invoke(short x) => (sbyte)x;
            public static Vector128<sbyte> Invoke(Vector128<short> lower, Vector128<short> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<sbyte> Invoke(Vector256<short> lower, Vector256<short> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<sbyte> Invoke(Vector512<short> lower, Vector512<short> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(uint)ushort</summary>
        internal readonly struct WidenUInt16ToUInt32Operator : IUnaryOneToTwoOperator<ushort, uint>
        {
            public static bool Vectorizable => true;

            public static uint Invoke(ushort x) => x;
            public static (Vector128<uint> Lower, Vector128<uint> Upper) Invoke(Vector128<ushort> x) => Vector128.Widen(x);
            public static (Vector256<uint> Lower, Vector256<uint> Upper) Invoke(Vector256<ushort> x) => Vector256.Widen(x);
            public static (Vector512<uint> Lower, Vector512<uint> Upper) Invoke(Vector512<ushort> x) => Vector512.Widen(x);
        }

        /// <summary>(ushort)uint</summary>
        internal readonly struct NarrowUInt32ToUInt16Operator : IUnaryTwoToOneOperator<uint, ushort>
        {
            public static bool Vectorizable => true;

            public static ushort Invoke(uint x) => (ushort)x;
            public static Vector128<ushort> Invoke(Vector128<uint> lower, Vector128<uint> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<ushort> Invoke(Vector256<uint> lower, Vector256<uint> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<ushort> Invoke(Vector512<uint> lower, Vector512<uint> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(int)short</summary>
        internal readonly struct WidenInt16ToInt32Operator : IUnaryOneToTwoOperator<short, int>
        {
            public static bool Vectorizable => true;

            public static int Invoke(short x) => x;
            public static (Vector128<int> Lower, Vector128<int> Upper) Invoke(Vector128<short> x) => Vector128.Widen(x);
            public static (Vector256<int> Lower, Vector256<int> Upper) Invoke(Vector256<short> x) => Vector256.Widen(x);
            public static (Vector512<int> Lower, Vector512<int> Upper) Invoke(Vector512<short> x) => Vector512.Widen(x);
        }

        /// <summary>(short)int</summary>
        internal readonly struct NarrowInt32ToInt16Operator : IUnaryTwoToOneOperator<int, short>
        {
            public static bool Vectorizable => true;

            public static short Invoke(int x) => (short)x;
            public static Vector128<short> Invoke(Vector128<int> lower, Vector128<int> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<short> Invoke(Vector256<int> lower, Vector256<int> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<short> Invoke(Vector512<int> lower, Vector512<int> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(ulong)uint</summary>
        internal readonly struct WidenUInt32ToUInt64Operator : IUnaryOneToTwoOperator<uint, ulong>
        {
            public static bool Vectorizable => true;

            public static ulong Invoke(uint x) => x;
            public static (Vector128<ulong> Lower, Vector128<ulong> Upper) Invoke(Vector128<uint> x) => Vector128.Widen(x);
            public static (Vector256<ulong> Lower, Vector256<ulong> Upper) Invoke(Vector256<uint> x) => Vector256.Widen(x);
            public static (Vector512<ulong> Lower, Vector512<ulong> Upper) Invoke(Vector512<uint> x) => Vector512.Widen(x);
        }

        /// <summary>(uint)ulong</summary>
        internal readonly struct NarrowUInt64ToUInt32Operator : IUnaryTwoToOneOperator<ulong, uint>
        {
            public static bool Vectorizable => true;

            public static uint Invoke(ulong x) => (uint)x;
            public static Vector128<uint> Invoke(Vector128<ulong> lower, Vector128<ulong> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<uint> Invoke(Vector256<ulong> lower, Vector256<ulong> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<uint> Invoke(Vector512<ulong> lower, Vector512<ulong> upper) => Vector512.Narrow(lower, upper);
        }

        /// <summary>(long)int</summary>
        internal readonly struct WidenInt32ToInt64Operator : IUnaryOneToTwoOperator<int, long>
        {
            public static bool Vectorizable => true;

            public static long Invoke(int x) => x;
            public static (Vector128<long> Lower, Vector128<long> Upper) Invoke(Vector128<int> x) => Vector128.Widen(x);
            public static (Vector256<long> Lower, Vector256<long> Upper) Invoke(Vector256<int> x) => Vector256.Widen(x);
            public static (Vector512<long> Lower, Vector512<long> Upper) Invoke(Vector512<int> x) => Vector512.Widen(x);
        }

        /// <summary>(int)long</summary>
        internal readonly struct NarrowInt64ToInt32Operator : IUnaryTwoToOneOperator<long, int>
        {
            public static bool Vectorizable => true;

            public static int Invoke(long x) => (int)x;
            public static Vector128<int> Invoke(Vector128<long> lower, Vector128<long> upper) => Vector128.Narrow(lower, upper);
            public static Vector256<int> Invoke(Vector256<long> lower, Vector256<long> upper) => Vector256.Narrow(lower, upper);
            public static Vector512<int> Invoke(Vector512<long> lower, Vector512<long> upper) => Vector512.Narrow(lower, upper);
        }

        internal readonly struct WidenHalfAsInt16ToSingleOperator : IUnaryOneToTwoOperator<short, float>
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

        internal readonly struct NarrowSingleToHalfAsUInt16Operator : IUnaryTwoToOneOperator<float, ushort>
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
    }
}
