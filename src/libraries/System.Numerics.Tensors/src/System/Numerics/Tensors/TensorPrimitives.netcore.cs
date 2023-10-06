﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;

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
        public static void ConvertToHalf(ReadOnlySpan<float> source, Span<Half> destination)
        {
            if (source.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref float sourceRef = ref MemoryMarshal.GetReference(source);
            ref ushort destinationRef = ref Unsafe.As<Half, ushort>(ref MemoryMarshal.GetReference(destination));
            int i = 0, twoVectorsFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                twoVectorsFromEnd = source.Length - (Vector512<float>.Count * 2);
                if (i <= twoVectorsFromEnd)
                {
                    // Loop handling two input vectors / one output vector at a time.
                    do
                    {
                        Vector512<uint> lower = SingleToHalfAsWidenedUInt32_Vector512(Vector512.LoadUnsafe(ref sourceRef, (uint)i));
                        Vector512<uint> upper = SingleToHalfAsWidenedUInt32_Vector512(Vector512.LoadUnsafe(ref sourceRef, (uint)(i + Vector512<float>.Count)));
                        Vector512.Narrow(lower, upper).StoreUnsafe(ref destinationRef, (uint)i);

                        i += Vector512<float>.Count * 2;
                    }
                    while (i <= twoVectorsFromEnd);

                    // Handle any remaining elements with final vectors.
                    if (i != source.Length)
                    {
                        i = source.Length - (Vector512<float>.Count * 2);

                        Vector512<uint> lower = SingleToHalfAsWidenedUInt32_Vector512(Vector512.LoadUnsafe(ref sourceRef, (uint)i));
                        Vector512<uint> upper = SingleToHalfAsWidenedUInt32_Vector512(Vector512.LoadUnsafe(ref sourceRef, (uint)(i + Vector512<float>.Count)));
                        Vector512.Narrow(lower, upper).StoreUnsafe(ref destinationRef, (uint)i);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                twoVectorsFromEnd = source.Length - (Vector256<float>.Count * 2);
                if (i <= twoVectorsFromEnd)
                {
                    // Loop handling two input vectors / one output vector at a time.
                    do
                    {
                        Vector256<uint> lower = SingleToHalfAsWidenedUInt32_Vector256(Vector256.LoadUnsafe(ref sourceRef, (uint)i));
                        Vector256<uint> upper = SingleToHalfAsWidenedUInt32_Vector256(Vector256.LoadUnsafe(ref sourceRef, (uint)(i + Vector256<float>.Count)));
                        Vector256<ushort> halfs = Vector256.Narrow(lower, upper);
                        halfs.StoreUnsafe(ref destinationRef, (uint)i);

                        i += Vector256<float>.Count * 2;
                    }
                    while (i <= twoVectorsFromEnd);

                    // Handle any remaining elements with final vectors.
                    if (i != source.Length)
                    {
                        i = source.Length - (Vector256<float>.Count * 2);

                        Vector256<uint> lower = SingleToHalfAsWidenedUInt32_Vector256(Vector256.LoadUnsafe(ref sourceRef, (uint)i));
                        Vector256<uint> upper = SingleToHalfAsWidenedUInt32_Vector256(Vector256.LoadUnsafe(ref sourceRef, (uint)(i + Vector256<float>.Count)));
                        Vector256.Narrow(lower, upper).StoreUnsafe(ref destinationRef, (uint)i);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                twoVectorsFromEnd = source.Length - (Vector128<float>.Count * 2);
                if (i <= twoVectorsFromEnd)
                {
                    // Loop handling two input vectors / one output vector at a time.
                    do
                    {
                        Vector128<uint> lower = SingleToHalfAsWidenedUInt32_Vector128(Vector128.LoadUnsafe(ref sourceRef, (uint)i));
                        Vector128<uint> upper = SingleToHalfAsWidenedUInt32_Vector128(Vector128.LoadUnsafe(ref sourceRef, (uint)(i + Vector128<float>.Count)));
                        Vector128.Narrow(lower, upper).StoreUnsafe(ref destinationRef, (uint)i);

                        i += Vector128<float>.Count * 2;
                    }
                    while (i <= twoVectorsFromEnd);

                    // Handle any remaining elements with final vectors.
                    if (i != source.Length)
                    {
                        i = source.Length - (Vector128<float>.Count * 2);

                        Vector128<uint> lower = SingleToHalfAsWidenedUInt32_Vector128(Vector128.LoadUnsafe(ref sourceRef, (uint)i));
                        Vector128<uint> upper = SingleToHalfAsWidenedUInt32_Vector128(Vector128.LoadUnsafe(ref sourceRef, (uint)(i + Vector128<float>.Count)));
                        Vector128.Narrow(lower, upper).StoreUnsafe(ref destinationRef, (uint)i);
                    }

                    return;
                }
            }

            while (i < source.Length)
            {
                Unsafe.Add(ref destinationRef, i) = BitConverter.HalfToUInt16Bits((Half)Unsafe.Add(ref sourceRef, i));
                i++;
            }

            // This implements a vectorized version of the `explicit operator Half(float value) operator`.
            // See detailed description of the algorithm used here:
            //     https://github.com/dotnet/runtime/blob/ca8d6f0420096831766ec11c7d400e4f7ccc7a34/src/libraries/System.Private.CoreLib/src/System/Half.cs#L606-L714
            // The cast operator converts a float to a Half represented as a UInt32, then narrows to a UInt16, and reinterpret casts to Half.
            // This does the same, with an input VectorXx<float> and an output VectorXx<uint>.
            // Loop handling two input vectors at a time; each input float is double the size of each output Half,
            // so we need two vectors of floats to produce one vector of Halfs. Half isn't supported in VectorXx<T>,
            // so we convert the VectorXx<float> to a VectorXx<uint>, and the caller then uses this twice, narrows the combination
            // into a VectorXx<ushort>, and then saves that out to the destination `ref Half` reinterpreted as `ref ushort`.

#pragma warning disable IDE0059 // https://github.com/dotnet/roslyn/issues/44948
            const uint MinExp = 0x3880_0000u; // Minimum exponent for rounding
            const uint Exponent126 = 0x3f00_0000u; // Exponent displacement #1
            const uint SingleBiasedExponentMask = 0x7F80_0000; // float.BiasedExponentMask; // Exponent mask
            const uint Exponent13 = 0x0680_0000u; // Exponent displacement #2
            const float MaxHalfValueBelowInfinity = 65520.0f; // Maximum value that is not Infinity in Half
            const uint ExponentMask = 0x7C00; // Mask for exponent bits in Half
            const uint SingleSignMask = 0x8000_0000u; // float.SignMask; // Mask for sign bit in float
#pragma warning restore IDE0059

            static Vector128<uint> SingleToHalfAsWidenedUInt32_Vector128(Vector128<float> value)
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

            static Vector256<uint> SingleToHalfAsWidenedUInt32_Vector256(Vector256<float> value)
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

#if NET8_0_OR_GREATER
            static Vector512<uint> SingleToHalfAsWidenedUInt32_Vector512(Vector512<float> value)
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
#endif
        }

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
        public static void ConvertToSingle(ReadOnlySpan<Half> source, Span<float> destination)
        {
            if (source.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref short sourceRef = ref Unsafe.As<Half, short>(ref MemoryMarshal.GetReference(source));
            ref float destinationRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = source.Length - Vector512<short>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two output vectors at a time.
                    do
                    {
                        (Vector512<int> lower, Vector512<int> upper) = Vector512.Widen(Vector512.LoadUnsafe(ref sourceRef, (uint)i));
                        HalfAsWidenedUInt32ToSingle_Vector512(lower.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)i);
                        HalfAsWidenedUInt32ToSingle_Vector512(upper.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)(i + Vector512<float>.Count));

                        i += Vector512<short>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != source.Length)
                    {
                        i = source.Length - Vector512<short>.Count;

                        (Vector512<int> lower, Vector512<int> upper) = Vector512.Widen(Vector512.LoadUnsafe(ref sourceRef, (uint)i));
                        HalfAsWidenedUInt32ToSingle_Vector512(lower.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)i);
                        HalfAsWidenedUInt32ToSingle_Vector512(upper.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)(i + Vector512<float>.Count));
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = source.Length - Vector256<short>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two output vectors at a time.
                    do
                    {
                        (Vector256<int> lower, Vector256<int> upper) = Vector256.Widen(Vector256.LoadUnsafe(ref sourceRef, (uint)i));
                        HalfAsWidenedUInt32ToSingle_Vector256(lower.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)i);
                        HalfAsWidenedUInt32ToSingle_Vector256(upper.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)(i + Vector256<float>.Count));

                        i += Vector256<short>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != source.Length)
                    {
                        i = source.Length - Vector256<short>.Count;

                        (Vector256<int> lower, Vector256<int> upper) = Vector256.Widen(Vector256.LoadUnsafe(ref sourceRef, (uint)i));
                        HalfAsWidenedUInt32ToSingle_Vector256(lower.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)i);
                        HalfAsWidenedUInt32ToSingle_Vector256(upper.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)(i + Vector256<float>.Count));
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = source.Length - Vector128<short>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two output vectors at a time.
                    do
                    {
                        (Vector128<int> lower, Vector128<int> upper) = Vector128.Widen(Vector128.LoadUnsafe(ref sourceRef, (uint)i));
                        HalfAsWidenedUInt32ToSingle_Vector128(lower.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)i);
                        HalfAsWidenedUInt32ToSingle_Vector128(upper.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)(i + Vector128<float>.Count));

                        i += Vector128<short>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != source.Length)
                    {
                        i = source.Length - Vector128<short>.Count;

                        (Vector128<int> lower, Vector128<int> upper) = Vector128.Widen(Vector128.LoadUnsafe(ref sourceRef, (uint)i));
                        HalfAsWidenedUInt32ToSingle_Vector128(lower.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)i);
                        HalfAsWidenedUInt32ToSingle_Vector128(upper.AsUInt32()).StoreUnsafe(ref destinationRef, (uint)(i + Vector128<float>.Count));
                    }

                    return;
                }
            }

            while (i < source.Length)
            {
                Unsafe.Add(ref destinationRef, i) = (float)Unsafe.As<short, Half>(ref Unsafe.Add(ref sourceRef, i));
                i++;
            }

            // This implements a vectorized version of the `explicit operator float(Half value) operator`.
            // See detailed description of the algorithm used here:
            //     https://github.com/dotnet/runtime/blob/3bf40a378f00cb5bf18ff62796bc7097719b974c/src/libraries/System.Private.CoreLib/src/System/Half.cs#L1010-L1040
            // The cast operator converts a Half represented as uint to a float. This does the same, with an input VectorXx<uint> and an output VectorXx<float>.
            // The VectorXx<uint> is created by reading a vector of Halfs as a VectorXx<short> then widened to two VectorXx<int>s and cast to VectorXx<uint>s.
            // We loop handling one input vector at a time, producing two output float vectors.

#pragma warning disable IDE0059 // https://github.com/dotnet/roslyn/issues/44948
            const uint ExponentLowerBound = 0x3880_0000u; // The smallest positive normal number in Half, converted to Single
            const uint ExponentOffset = 0x3800_0000u; // BitConverter.SingleToUInt32Bits(1.0f) - ((uint)BitConverter.HalfToUInt16Bits((Half)1.0f) << 13)
            const uint SingleSignMask = 0x8000_0000; // float.SignMask; // Mask for sign bit in Single
            const uint HalfExponentMask = 0x7C00; // Mask for exponent bits in Half
            const uint HalfToSingleBitsMask = 0x0FFF_E000; // Mask for bits in Single converted from Half
#pragma warning restore IDE0059

            static Vector128<float> HalfAsWidenedUInt32ToSingle_Vector128(Vector128<uint> value)
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

            static Vector256<float> HalfAsWidenedUInt32ToSingle_Vector256(Vector256<uint> value)
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

#if NET8_0_OR_GREATER
            static Vector512<float> HalfAsWidenedUInt32ToSingle_Vector512(Vector512<uint> value)
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
#endif
        }

        /// <summary>Computes the cosine similarity between the two specified non-empty, equal-length tensors of single-precision floating-point numbers.</summary>
        /// <remarks>Assumes arguments have already been validated to be non-empty and equal length.</remarks>
        private static float CosineSimilarityCore(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            // Compute the same as:
            // TensorPrimitives.Dot(x, y) / (Math.Sqrt(TensorPrimitives.SumOfSquares(x)) * Math.Sqrt(TensorPrimitives.SumOfSquares(y)))
            // but only looping over each span once.

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && x.Length >= Vector512<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                Vector512<float> dotProductVector = Vector512<float>.Zero;
                Vector512<float> xSumOfSquaresVector = Vector512<float>.Zero;
                Vector512<float> ySumOfSquaresVector = Vector512<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector512<float>.Count;
                int i = 0;
                do
                {
                    Vector512<float> xVec = Vector512.LoadUnsafe(ref xRef, (uint)i);
                    Vector512<float> yVec = Vector512.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);

                    i += Vector512<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector512<float> xVec = Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<float>.Count));
                    Vector512<float> yVec = Vector512.LoadUnsafe(ref yRef, (uint)(x.Length - Vector512<float>.Count));

                    Vector512<float> remainderMask = CreateRemainderMaskSingleVector512(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector512.Sum(dotProductVector) /
                    (MathF.Sqrt(Vector512.Sum(xSumOfSquaresVector)) * MathF.Sqrt(Vector512.Sum(ySumOfSquaresVector)));
            }
#endif

            if (Vector256.IsHardwareAccelerated && x.Length >= Vector256<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                Vector256<float> dotProductVector = Vector256<float>.Zero;
                Vector256<float> xSumOfSquaresVector = Vector256<float>.Zero;
                Vector256<float> ySumOfSquaresVector = Vector256<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector256<float>.Count;
                int i = 0;
                do
                {
                    Vector256<float> xVec = Vector256.LoadUnsafe(ref xRef, (uint)i);
                    Vector256<float> yVec = Vector256.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);

                    i += Vector256<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector256<float> xVec = Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<float>.Count));
                    Vector256<float> yVec = Vector256.LoadUnsafe(ref yRef, (uint)(x.Length - Vector256<float>.Count));

                    Vector256<float> remainderMask = CreateRemainderMaskSingleVector256(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector256.Sum(dotProductVector) /
                    (MathF.Sqrt(Vector256.Sum(xSumOfSquaresVector)) * MathF.Sqrt(Vector256.Sum(ySumOfSquaresVector)));
            }

            if (Vector128.IsHardwareAccelerated && x.Length >= Vector128<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                Vector128<float> dotProductVector = Vector128<float>.Zero;
                Vector128<float> xSumOfSquaresVector = Vector128<float>.Zero;
                Vector128<float> ySumOfSquaresVector = Vector128<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector128<float>.Count;
                int i = 0;
                do
                {
                    Vector128<float> xVec = Vector128.LoadUnsafe(ref xRef, (uint)i);
                    Vector128<float> yVec = Vector128.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);

                    i += Vector128<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector128<float> xVec = Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<float>.Count));
                    Vector128<float> yVec = Vector128.LoadUnsafe(ref yRef, (uint)(x.Length - Vector128<float>.Count));

                    Vector128<float> remainderMask = CreateRemainderMaskSingleVector128(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector128.Sum(dotProductVector) /
                    (MathF.Sqrt(Vector128.Sum(xSumOfSquaresVector)) * MathF.Sqrt(Vector128.Sum(ySumOfSquaresVector)));
            }

            // Vectorization isn't supported or there are too few elements to vectorize.
            // Use a scalar implementation.
            float dotProduct = 0f, xSumOfSquares = 0f, ySumOfSquares = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                dotProduct = MathF.FusedMultiplyAdd(x[i], y[i], dotProduct);
                xSumOfSquares = MathF.FusedMultiplyAdd(x[i], x[i], xSumOfSquares);
                ySumOfSquares = MathF.FusedMultiplyAdd(y[i], y[i], ySumOfSquares);
            }

            // Sum(X * Y) / (|X| * |Y|)
            return
                dotProduct /
                (MathF.Sqrt(xSumOfSquares) * MathF.Sqrt(ySumOfSquares));
        }

        /// <summary>Performs an aggregation over all elements in <paramref name="x"/> to produce a single-precision floating-point value.</summary>
        /// <typeparam name="TTransformOperator">Specifies the transform operation that should be applied to each element loaded from <paramref name="x"/>.</typeparam>
        /// <typeparam name="TAggregationOperator">
        /// Specifies the aggregation binary operation that should be applied to multiple values to aggregate them into a single value.
        /// The aggregation is applied after the transform is applied to each element.
        /// </typeparam>
        private static float Aggregate<TTransformOperator, TAggregationOperator>(
            ReadOnlySpan<float> x)
            where TTransformOperator : struct, IUnaryOperator
            where TAggregationOperator : struct, IAggregationOperator
        {
            if (x.Length == 0)
            {
                return 0;
            }

            ref float xRef = ref MemoryMarshal.GetReference(x);

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && x.Length >= Vector512<float>.Count)
            {
                // Load the first vector as the initial set of results
                Vector512<float> result = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, 0));
                int oneVectorFromEnd = x.Length - Vector512<float>.Count;
                int i = Vector512<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i)));
                    i += Vector512<float>.Count;
                }

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregationOperator.Invoke(result,
                        Vector512.ConditionalSelect(
                            Vector512.Equals(CreateRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                            Vector512.Create(TAggregationOperator.IdentityValue),
                            TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregationOperator.Invoke(result);
            }
#endif

            if (Vector256.IsHardwareAccelerated && x.Length >= Vector256<float>.Count)
            {
                // Load the first vector as the initial set of results
                Vector256<float> result = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, 0));
                int oneVectorFromEnd = x.Length - Vector256<float>.Count;
                int i = Vector256<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i)));
                    i += Vector256<float>.Count;
                }

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregationOperator.Invoke(result,
                        Vector256.ConditionalSelect(
                            Vector256.Equals(CreateRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                            Vector256.Create(TAggregationOperator.IdentityValue),
                            TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregationOperator.Invoke(result);
            }

            if (Vector128.IsHardwareAccelerated && x.Length >= Vector128<float>.Count)
            {
                // Load the first vector as the initial set of results
                Vector128<float> result = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, 0));
                int oneVectorFromEnd = x.Length - Vector128<float>.Count;
                int i = Vector128<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i)));
                    i += Vector128<float>.Count;
                }

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregationOperator.Invoke(result,
                        Vector128.ConditionalSelect(
                            Vector128.Equals(CreateRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                            Vector128.Create(TAggregationOperator.IdentityValue),
                            TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregationOperator.Invoke(result);
            }

            // Vectorization isn't supported or there are too few elements to vectorize.
            // Use a scalar implementation.
            {
                float result = TTransformOperator.Invoke(x[0]);
                for (int i = 1; i < x.Length; i++)
                {
                    result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(x[i]));
                }

                return result;
            }
        }

        /// <summary>Performs an aggregation over all pair-wise elements in <paramref name="x"/> and <paramref name="y"/> to produce a single-precision floating-point value.</summary>
        /// <typeparam name="TBinaryOperator">Specifies the binary operation that should be applied to the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>.</typeparam>
        /// <typeparam name="TAggregationOperator">
        /// Specifies the aggregation binary operation that should be applied to multiple values to aggregate them into a single value.
        /// The aggregation is applied to the results of the binary operations on the pair-wise values.
        /// </typeparam>
        private static float Aggregate<TBinaryOperator, TAggregationOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y)
            where TBinaryOperator : struct, IBinaryOperator
            where TAggregationOperator : struct, IAggregationOperator
        {
            Debug.Assert(x.Length == y.Length);

            if (x.IsEmpty)
            {
                return 0;
            }

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && x.Length >= Vector512<float>.Count)
            {
                // Load the first vector as the initial set of results
                Vector512<float> result = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, 0), Vector512.LoadUnsafe(ref yRef, 0));
                int oneVectorFromEnd = x.Length - Vector512<float>.Count;
                int i = Vector512<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i), Vector512.LoadUnsafe(ref yRef, (uint)i)));
                    i += Vector512<float>.Count;
                }

                // Process the last vector in the spans, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregationOperator.Invoke(result,
                        Vector512.ConditionalSelect(
                            Vector512.Equals(CreateRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                            Vector512.Create(TAggregationOperator.IdentityValue),
                            TBinaryOperator.Invoke(
                                Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<float>.Count)),
                                Vector512.LoadUnsafe(ref yRef, (uint)(x.Length - Vector512<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregationOperator.Invoke(result);
            }
#endif

            if (Vector256.IsHardwareAccelerated && x.Length >= Vector256<float>.Count)
            {
                // Load the first vector as the initial set of results
                Vector256<float> result = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, 0), Vector256.LoadUnsafe(ref yRef, 0));
                int oneVectorFromEnd = x.Length - Vector256<float>.Count;
                int i = Vector256<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i), Vector256.LoadUnsafe(ref yRef, (uint)i)));
                    i += Vector256<float>.Count;
                }

                // Process the last vector in the spans, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregationOperator.Invoke(result,
                        Vector256.ConditionalSelect(
                            Vector256.Equals(CreateRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                            Vector256.Create(TAggregationOperator.IdentityValue),
                            TBinaryOperator.Invoke(
                                Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<float>.Count)),
                                Vector256.LoadUnsafe(ref yRef, (uint)(x.Length - Vector256<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregationOperator.Invoke(result);
            }

            if (Vector128.IsHardwareAccelerated && x.Length >= Vector128<float>.Count)
            {
                // Load the first vector as the initial set of results
                Vector128<float> result = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, 0), Vector128.LoadUnsafe(ref yRef, 0));
                int oneVectorFromEnd = x.Length - Vector128<float>.Count;
                int i = Vector128<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i), Vector128.LoadUnsafe(ref yRef, (uint)i)));
                    i += Vector128<float>.Count;
                }

                // Process the last vector in the spans, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregationOperator.Invoke(result,
                        Vector128.ConditionalSelect(
                            Vector128.Equals(CreateRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                            Vector128.Create(TAggregationOperator.IdentityValue),
                            TBinaryOperator.Invoke(
                                Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<float>.Count)),
                                Vector128.LoadUnsafe(ref yRef, (uint)(x.Length - Vector128<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregationOperator.Invoke(result);
            }

            // Vectorization isn't supported or there are too few elements to vectorize.
            // Use a scalar implementation.
            {
                float result = TBinaryOperator.Invoke(xRef, yRef);
                for (int i = 1; i < x.Length; i++)
                {
                    result = TAggregationOperator.Invoke(result,
                        TBinaryOperator.Invoke(
                            Unsafe.Add(ref xRef, i),
                            Unsafe.Add(ref yRef, i)));
                }

                return result;
            }
        }

        /// <remarks>
        /// This is the same as <see cref="Aggregate{TTransformOperator, TAggregationOperator}(ReadOnlySpan{float})"/>
        /// with an identity transform, except it early exits on NaN.
        /// </remarks>
        private static float MinMaxCore<TMinMaxOperator>(ReadOnlySpan<float> x)
            where TMinMaxOperator : struct, IAggregationOperator
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            // This matches the IEEE 754:2019 `maximum`/`minimum` functions.
            // It propagates NaN inputs back to the caller and
            // otherwise returns the greater of the inputs.
            // It treats +0 as greater than -0 as per the specification.

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated && x.Length >= Vector512<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector512<float> result = Vector512.LoadUnsafe(ref xRef, 0), current;
                if (!Vector512.EqualsAll(result, result))
                {
                    return GetFirstNaN(result);
                }

                int oneVectorFromEnd = x.Length - Vector512<float>.Count;
                int i = Vector512<float>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector512.LoadUnsafe(ref xRef, (uint)i);
                    if (!Vector512.EqualsAll(current, current))
                    {
                        return GetFirstNaN(current);
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector512<float>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<float>.Count));
                    if (!Vector512.EqualsAll(current, current))
                    {
                        return GetFirstNaN(current);
                    }

                    result = Vector512.ConditionalSelect(
                        Vector512.Equals(CreateRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                        result,
                        TMinMaxOperator.Invoke(result, current));
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }
#endif

            if (Vector256.IsHardwareAccelerated && x.Length >= Vector256<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector256<float> result = Vector256.LoadUnsafe(ref xRef, 0), current;
                if (!Vector256.EqualsAll(result, result))
                {
                    return GetFirstNaN(result);
                }

                int oneVectorFromEnd = x.Length - Vector256<float>.Count;
                int i = Vector256<float>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector256.LoadUnsafe(ref xRef, (uint)i);
                    if (!Vector256.EqualsAll(current, current))
                    {
                        return GetFirstNaN(current);
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector256<float>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<float>.Count));
                    if (!Vector256.EqualsAll(current, current))
                    {
                        return GetFirstNaN(current);
                    }

                    result = Vector256.ConditionalSelect(
                        Vector256.Equals(CreateRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                        result,
                        TMinMaxOperator.Invoke(result, current));
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }

            if (Vector128.IsHardwareAccelerated && x.Length >= Vector128<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector128<float> result = Vector128.LoadUnsafe(ref xRef, 0), current;
                if (!Vector128.EqualsAll(result, result))
                {
                    return GetFirstNaN(result);
                }

                int oneVectorFromEnd = x.Length - Vector128<float>.Count;
                int i = Vector128<float>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector128.LoadUnsafe(ref xRef, (uint)i);
                    if (!Vector128.EqualsAll(current, current))
                    {
                        return GetFirstNaN(current);
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector128<float>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<float>.Count));
                    if (!Vector128.EqualsAll(current, current))
                    {
                        return GetFirstNaN(current);
                    }

                    result = Vector128.ConditionalSelect(
                        Vector128.Equals(CreateRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                        result,
                        TMinMaxOperator.Invoke(result, current));
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }

            // Scalar path used when either vectorization is not supported or the input is too small to vectorize.
            {
                float result = x[0];
                if (float.IsNaN(result))
                {
                    return result;
                }

                for (int i = 1; i < x.Length; i++)
                {
                    float current = x[i];
                    if (float.IsNaN(current))
                    {
                        return current;
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                }

                return result;
            }
        }

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination"/>.</summary>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static unsafe void InvokeSpanIntoSpan<TUnaryOperator>(
            ReadOnlySpan<float> x, Span<float> destination)
            where TUnaryOperator : struct, IUnaryOperator
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        Vector512.ConditionalSelect(
                            Vector512.Equals(CreateRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                            Vector512.LoadUnsafe(ref dRef, lastVectorIndex),
                            TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        Vector256.ConditionalSelect(
                            Vector256.Equals(CreateRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                            Vector256.LoadUnsafe(ref dRef, lastVectorIndex),
                            TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        Vector128.ConditionalSelect(
                            Vector128.Equals(CreateRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                            Vector128.LoadUnsafe(ref dRef, lastVectorIndex),
                            TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, i));

                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>.
        /// </typeparam>
        private static unsafe void InvokeSpanSpanIntoSpan<TBinaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination)
            where TBinaryOperator : struct, IBinaryOperator
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(y, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i),
                                               Vector512.LoadUnsafe(ref yRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        Vector512.ConditionalSelect(
                            Vector512.Equals(CreateRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                            Vector512.LoadUnsafe(ref dRef, lastVectorIndex),
                            TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex),
                                                   Vector512.LoadUnsafe(ref yRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i),
                                               Vector256.LoadUnsafe(ref yRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        Vector256.ConditionalSelect(
                            Vector256.Equals(CreateRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                            Vector256.LoadUnsafe(ref dRef, lastVectorIndex),
                            TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex),
                                                   Vector256.LoadUnsafe(ref yRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i),
                                               Vector128.LoadUnsafe(ref yRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        Vector128.ConditionalSelect(
                            Vector128.Equals(CreateRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                            Vector128.LoadUnsafe(ref dRef, lastVectorIndex),
                            TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex),
                                                   Vector128.LoadUnsafe(ref yRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                 Unsafe.Add(ref yRef, i));

                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on each element loaded from <paramref name="x"/> with <paramref name="y"/>.
        /// </typeparam>
        private static unsafe void InvokeSpanScalarIntoSpan<TBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination)
            where TBinaryOperator : struct, IBinaryOperator =>
            InvokeSpanScalarIntoSpan<IdentityOperator, TBinaryOperator>(x, y, destination);

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTransformOperator">
        /// Specifies the operation to perform on each element loaded from <paramref name="x"/>.
        /// It is not used with <paramref name="y"/>.
        /// </typeparam>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on the transformed value from <paramref name="x"/> with <paramref name="y"/>.
        /// </typeparam>
        private static unsafe void InvokeSpanScalarIntoSpan<TTransformOperator, TBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination)
            where TTransformOperator : struct, IUnaryOperator
            where TBinaryOperator : struct, IBinaryOperator
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector512<float> yVec = Vector512.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i)),
                                               yVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        Vector512.ConditionalSelect(
                            Vector512.Equals(CreateRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                            Vector512.LoadUnsafe(ref dRef, lastVectorIndex),
                            TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex)),
                                                   yVec)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector256<float> yVec = Vector256.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i)),
                                               yVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        Vector256.ConditionalSelect(
                            Vector256.Equals(CreateRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                            Vector256.LoadUnsafe(ref dRef, lastVectorIndex),
                            TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex)),
                                                   yVec)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector128<float> yVec = Vector128.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i)),
                                               yVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        Vector128.ConditionalSelect(
                            Vector128.Equals(CreateRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                            Vector128.LoadUnsafe(ref dRef, lastVectorIndex),
                            TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex)),
                                                   yVec)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, i)),
                                                                 y);

                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/>, <paramref name="y"/>,
        /// and <paramref name="z"/>.
        /// </typeparam>
        private static unsafe void InvokeSpanSpanSpanIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> z, Span<float> destination)
            where TTernaryOperator : struct, ITernaryOperator
        {
            if (x.Length != y.Length || x.Length != z.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(y, destination);
            ValidateInputOutputSpanNonOverlapping(z, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float zRef = ref MemoryMarshal.GetReference(z);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i),
                                                Vector512.LoadUnsafe(ref yRef, (uint)i),
                                                Vector512.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        Vector512.ConditionalSelect(
                            Vector512.Equals(CreateRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                            Vector512.LoadUnsafe(ref dRef, lastVectorIndex),
                            TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex),
                                                    Vector512.LoadUnsafe(ref yRef, lastVectorIndex),
                                                    Vector512.LoadUnsafe(ref zRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i),
                                                Vector256.LoadUnsafe(ref yRef, (uint)i),
                                                Vector256.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        Vector256.ConditionalSelect(
                            Vector256.Equals(CreateRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                            Vector256.LoadUnsafe(ref dRef, lastVectorIndex),
                            TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex),
                                                    Vector256.LoadUnsafe(ref yRef, lastVectorIndex),
                                                    Vector256.LoadUnsafe(ref zRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i),
                                                Vector128.LoadUnsafe(ref yRef, (uint)i),
                                                Vector128.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        Vector128.ConditionalSelect(
                            Vector128.Equals(CreateRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                            Vector128.LoadUnsafe(ref dRef, lastVectorIndex),
                            TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex),
                                                    Vector128.LoadUnsafe(ref yRef, lastVectorIndex),
                                                    Vector128.LoadUnsafe(ref zRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                  Unsafe.Add(ref yRef, i),
                                                                  Unsafe.Add(ref zRef, i));

                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>
        /// with <paramref name="z"/>.
        /// </typeparam>
        private static unsafe void InvokeSpanSpanScalarIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, float z, Span<float> destination)
            where TTernaryOperator : struct, ITernaryOperator
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(y, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector512<float> zVec = Vector512.Create(z);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i),
                                                Vector512.LoadUnsafe(ref yRef, (uint)i),
                                                zVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        Vector512.ConditionalSelect(
                            Vector512.Equals(CreateRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                            Vector512.LoadUnsafe(ref dRef, lastVectorIndex),
                            TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex),
                                                    Vector512.LoadUnsafe(ref yRef, lastVectorIndex),
                                                    zVec)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector256<float> zVec = Vector256.Create(z);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i),
                                                Vector256.LoadUnsafe(ref yRef, (uint)i),
                                                zVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        Vector256.ConditionalSelect(
                            Vector256.Equals(CreateRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                            Vector256.LoadUnsafe(ref dRef, lastVectorIndex),
                            TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex),
                                                    Vector256.LoadUnsafe(ref yRef, lastVectorIndex),
                                                    zVec)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector128<float> zVec = Vector128.Create(z);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i),
                                                Vector128.LoadUnsafe(ref yRef, (uint)i),
                                                zVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        Vector128.ConditionalSelect(
                            Vector128.Equals(CreateRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                            Vector128.LoadUnsafe(ref dRef, lastVectorIndex),
                            TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex),
                                                    Vector128.LoadUnsafe(ref yRef, lastVectorIndex),
                                                    zVec)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                  Unsafe.Add(ref yRef, i),
                                                                  z);

                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise element loaded from <paramref name="x"/>, with <paramref name="y"/>,
        /// and the element loaded from <paramref name="z"/>.
        /// </typeparam>
        private static unsafe void InvokeSpanScalarSpanIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, float y, ReadOnlySpan<float> z, Span<float> destination)
            where TTernaryOperator : struct, ITernaryOperator
        {
            if (x.Length != z.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(z, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float zRef = ref MemoryMarshal.GetReference(z);
            ref float dRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector512<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector512<float> yVec = Vector512.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i),
                                                yVec,
                                                Vector512.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        Vector512.ConditionalSelect(
                            Vector512.Equals(CreateRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                            Vector512.LoadUnsafe(ref dRef, lastVectorIndex),
                            TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex),
                                                    yVec,
                                                    Vector512.LoadUnsafe(ref zRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }
#endif

            if (Vector256.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector256<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector256<float> yVec = Vector256.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i),
                                                yVec,
                                                Vector256.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        Vector256.ConditionalSelect(
                            Vector256.Equals(CreateRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                            Vector256.LoadUnsafe(ref dRef, lastVectorIndex),
                            TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex),
                                                    yVec,
                                                    Vector256.LoadUnsafe(ref zRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                oneVectorFromEnd = x.Length - Vector128<float>.Count;
                if (i <= oneVectorFromEnd)
                {
                    Vector128<float> yVec = Vector128.Create(y);

                    // Loop handling one vector at a time.
                    do
                    {
                        TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i),
                                                yVec,
                                                Vector128.LoadUnsafe(ref zRef, (uint)i)).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        Vector128.ConditionalSelect(
                            Vector128.Equals(CreateRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                            Vector128.LoadUnsafe(ref dRef, lastVectorIndex),
                            TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex),
                                                    yVec,
                                                    Vector128.LoadUnsafe(ref zRef, lastVectorIndex))).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                  y,
                                                                  Unsafe.Add(ref zRef, i));

                i++;
            }
        }

        /// <summary>Performs (x * y) + z. It will be rounded as one ternary operation if such an operation is accelerated on the current hardware.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> FusedMultiplyAdd(Vector128<float> x, Vector128<float> y, Vector128<float> addend)
        {
            if (Fma.IsSupported)
            {
                return Fma.MultiplyAdd(x, y, addend);
            }

            if (AdvSimd.IsSupported)
            {
                return AdvSimd.FusedMultiplyAdd(addend, x, y);
            }

            return (x * y) + addend;
        }

        /// <summary>Performs (x * y) + z. It will be rounded as one ternary operation if such an operation is accelerated on the current hardware.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> FusedMultiplyAdd(Vector256<float> x, Vector256<float> y, Vector256<float> addend)
        {
            if (Fma.IsSupported)
            {
                return Fma.MultiplyAdd(x, y, addend);
            }

            return (x * y) + addend;
        }

#if NET8_0_OR_GREATER
        /// <summary>Performs (x * y) + z. It will be rounded as one ternary operation if such an operation is accelerated on the current hardware.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<float> FusedMultiplyAdd(Vector512<float> x, Vector512<float> y, Vector512<float> addend)
        {
            if (Avx512F.IsSupported)
            {
                return Avx512F.FusedMultiplyAdd(x, y, addend);
            }

            return (x * y) + addend;
        }
#endif

        /// <summary>Aggregates all of the elements in the <paramref name="x"/> into a single value.</summary>
        /// <typeparam name="TAggregate">Specifies the operation to be performed on each pair of values.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float HorizontalAggregate<TAggregate>(Vector128<float> x) where TAggregate : struct, IBinaryOperator =>
            TAggregate.Invoke(
                TAggregate.Invoke(x[0], x[1]),
                TAggregate.Invoke(x[2], x[3]));

        /// <summary>Aggregates all of the elements in the <paramref name="x"/> into a single value.</summary>
        /// <typeparam name="TAggregate">Specifies the operation to be performed on each pair of values.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float HorizontalAggregate<TAggregate>(Vector256<float> x) where TAggregate : struct, IBinaryOperator =>
            HorizontalAggregate<TAggregate>(TAggregate.Invoke(x.GetLower(), x.GetUpper()));

#if NET8_0_OR_GREATER
        /// <summary>Aggregates all of the elements in the <paramref name="x"/> into a single value.</summary>
        /// <typeparam name="TAggregate">Specifies the operation to be performed on each pair of values.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float HorizontalAggregate<TAggregate>(Vector512<float> x) where TAggregate : struct, IBinaryOperator =>
            HorizontalAggregate<TAggregate>(TAggregate.Invoke(x.GetLower(), x.GetUpper()));
#endif

        /// <summary>Gets whether the specified <see cref="float"/> is negative.</summary>
        private static bool IsNegative(float f) => float.IsNegative(f);

        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> IsNegative(Vector128<float> vector) =>
            Vector128.LessThan(vector.AsInt32(), Vector128<int>.Zero).AsSingle();

        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> IsNegative(Vector256<float> vector) =>
            Vector256.LessThan(vector.AsInt32(), Vector256<int>.Zero).AsSingle();

#if NET8_0_OR_GREATER
        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<float> IsNegative(Vector512<float> vector) =>
            Vector512.LessThan(vector.AsInt32(), Vector512<int>.Zero).AsSingle();
#endif

        /// <summary>Finds and returns the first NaN value in <paramref name="vector"/>.</summary>
        /// <remarks>The vector must have already been validated to contain a NaN.</remarks>
        private static float GetFirstNaN(Vector128<float> vector)
        {
            Debug.Assert(!Vector128.EqualsAll(vector, vector), "Expected vector to contain a NaN");
            return vector.GetElement(BitOperations.TrailingZeroCount((~Vector128.Equals(vector, vector)).ExtractMostSignificantBits()));
        }

        /// <summary>Finds and returns the first NaN value in <paramref name="vector"/>.</summary>
        /// <remarks>The vector must have already been validated to contain a NaN.</remarks>
        private static float GetFirstNaN(Vector256<float> vector)
        {
            Debug.Assert(!Vector256.EqualsAll(vector, vector), "Expected vector to contain a NaN");
            return vector.GetElement(BitOperations.TrailingZeroCount((~Vector256.Equals(vector, vector)).ExtractMostSignificantBits()));
        }

#if NET8_0_OR_GREATER
        /// <summary>Finds and returns the first NaN value in <paramref name="vector"/>.</summary>
        /// <remarks>The vector must have already been validated to contain a NaN.</remarks>
        private static float GetFirstNaN(Vector512<float> vector)
        {
            Debug.Assert(!Vector512.EqualsAll(vector, vector), "Expected vector to contain a NaN");
            return vector.GetElement(BitOperations.TrailingZeroCount((~Vector512.Equals(vector, vector)).ExtractMostSignificantBits()));
        }
#endif

        /// <summary>Gets the base 2 logarithm of <paramref name="x"/>.</summary>
        private static float Log2(float x) => MathF.Log2(x);

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector128<float> CreateRemainderMaskSingleVector128(int count) =>
            Vector128.LoadUnsafe(
                ref Unsafe.As<uint, float>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x16)),
                (uint)((count * 16) + 12)); // last four floats in the row

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector256<float> CreateRemainderMaskSingleVector256(int count) =>
            Vector256.LoadUnsafe(
                ref Unsafe.As<uint, float>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x16)),
                (uint)((count * 16) + 8)); // last eight floats in the row

#if NET8_0_OR_GREATER
        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector512<float> CreateRemainderMaskSingleVector512(int count) =>
            Vector512.LoadUnsafe(
                ref Unsafe.As<uint, float>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x16)),
                (uint)(count * 16)); // all sixteen floats in the row
#endif

        /// <summary>x + y</summary>
        private readonly struct AddOperator : IAggregationOperator
        {
            public static float Invoke(float x, float y) => x + y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x + y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x + y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x + y;
#endif

            public static float Invoke(Vector128<float> x) => Vector128.Sum(x);
            public static float Invoke(Vector256<float> x) => Vector256.Sum(x);
#if NET8_0_OR_GREATER
            public static float Invoke(Vector512<float> x) => Vector512.Sum(x);
#endif

            public static float IdentityValue => 0;
        }

        /// <summary>x - y</summary>
        private readonly struct SubtractOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y) => x - y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x - y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x - y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x - y;
#endif
        }

        /// <summary>(x - y) * (x - y)</summary>
        private readonly struct SubtractSquaredOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y)
            {
                float tmp = x - y;
                return tmp * tmp;
            }

            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y)
            {
                Vector128<float> tmp = x - y;
                return tmp * tmp;
            }

            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y)
            {
                Vector256<float> tmp = x - y;
                return tmp * tmp;
            }

#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y)
            {
                Vector512<float> tmp = x - y;
                return tmp * tmp;
            }
#endif
        }

        /// <summary>x * y</summary>
        private readonly struct MultiplyOperator : IAggregationOperator
        {
            public static float Invoke(float x, float y) => x * y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x * y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x * y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x * y;
#endif

            public static float Invoke(Vector128<float> x) => HorizontalAggregate<MultiplyOperator>(x);
            public static float Invoke(Vector256<float> x) => HorizontalAggregate<MultiplyOperator>(x);
#if NET8_0_OR_GREATER
            public static float Invoke(Vector512<float> x) => HorizontalAggregate<MultiplyOperator>(x);
#endif

            public static float IdentityValue => 1;
        }

        /// <summary>x / y</summary>
        private readonly struct DivideOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y) => x / y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x / y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x / y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x / y;
#endif
        }

        /// <summary>MathF.Max(x, y) (but NaNs may not be propagated)</summary>
        private readonly struct MaxOperator : IAggregationOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Invoke(float x, float y) =>
                x == y ?
                    (IsNegative(x) ? y : x) :
                    (y > x ? y : x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y)
            {
                if (AdvSimd.IsSupported)
                {
                    return AdvSimd.Max(x, y);
                }

                return
                    Vector128.ConditionalSelect(Vector128.Equals(x, y),
                        Vector128.ConditionalSelect(IsNegative(x), y, x),
                        Vector128.Max(x, y));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) =>
                Vector256.ConditionalSelect(Vector256.Equals(x, y),
                    Vector256.ConditionalSelect(IsNegative(x), y, x),
                    Vector256.Max(x, y));

#if NET8_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) =>
                Vector512.ConditionalSelect(Vector512.Equals(x, y),
                    Vector512.ConditionalSelect(IsNegative(x), y, x),
                    Vector512.Max(x, y));
#endif

            public static float Invoke(Vector128<float> x) => HorizontalAggregate<MaxOperator>(x);
            public static float Invoke(Vector256<float> x) => HorizontalAggregate<MaxOperator>(x);
#if NET8_0_OR_GREATER
            public static float Invoke(Vector512<float> x) => HorizontalAggregate<MaxOperator>(x);
#endif
        }

        /// <summary>MathF.Max(x, y)</summary>
        private readonly struct MaxPropagateNaNOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Invoke(float x, float y) => MathF.Max(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y)
            {
                if (AdvSimd.IsSupported)
                {
                    return AdvSimd.Max(x, y);
                }

                return
                    Vector128.ConditionalSelect(Vector128.Equals(x, x),
                        Vector128.ConditionalSelect(Vector128.Equals(y, y),
                            Vector128.ConditionalSelect(Vector128.Equals(x, y),
                                Vector128.ConditionalSelect(IsNegative(x), y, x),
                                Vector128.Max(x, y)),
                            y),
                        x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) =>
                Vector256.ConditionalSelect(Vector256.Equals(x, x),
                    Vector256.ConditionalSelect(Vector256.Equals(y, y),
                        Vector256.ConditionalSelect(Vector256.Equals(x, y),
                            Vector256.ConditionalSelect(IsNegative(x), y, x),
                            Vector256.Max(x, y)),
                        y),
                    x);

#if NET8_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) =>
                Vector512.ConditionalSelect(Vector512.Equals(x, x),
                    Vector512.ConditionalSelect(Vector512.Equals(y, y),
                        Vector512.ConditionalSelect(Vector512.Equals(x, y),
                            Vector512.ConditionalSelect(IsNegative(x), y, x),
                            Vector512.Max(x, y)),
                        y),
                    x);
#endif
        }

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs (but NaNs may not be propagated)</summary>
        private readonly struct MaxMagnitudeOperator : IAggregationOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Invoke(float x, float y)
            {
                float xMag = MathF.Abs(x), yMag = MathF.Abs(y);
                return
                    xMag == yMag ?
                        (IsNegative(x) ? y : x) :
                        (xMag > yMag ? x : y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y)
            {
                Vector128<float> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);
                return
                    Vector128.ConditionalSelect(Vector128.Equals(xMag, yMag),
                        Vector128.ConditionalSelect(IsNegative(x), y, x),
                        Vector128.ConditionalSelect(Vector128.GreaterThan(xMag, yMag), x, y));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y)
            {
                Vector256<float> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);
                return
                    Vector256.ConditionalSelect(Vector256.Equals(xMag, yMag),
                        Vector256.ConditionalSelect(IsNegative(x), y, x),
                        Vector256.ConditionalSelect(Vector256.GreaterThan(xMag, yMag), x, y));
            }

#if NET8_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y)
            {
                Vector512<float> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);
                return
                    Vector512.ConditionalSelect(Vector512.Equals(xMag, yMag),
                        Vector512.ConditionalSelect(IsNegative(x), y, x),
                        Vector512.ConditionalSelect(Vector512.GreaterThan(xMag, yMag), x, y));
            }
#endif

            public static float Invoke(Vector128<float> x) => HorizontalAggregate<MaxMagnitudeOperator>(x);
            public static float Invoke(Vector256<float> x) => HorizontalAggregate<MaxMagnitudeOperator>(x);
#if NET8_0_OR_GREATER
            public static float Invoke(Vector512<float> x) => HorizontalAggregate<MaxMagnitudeOperator>(x);
#endif
        }

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs</summary>
        private readonly struct MaxMagnitudePropagateNaNOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Invoke(float x, float y) => MathF.MaxMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y)
            {
                Vector128<float> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);
                return
                    Vector128.ConditionalSelect(Vector128.Equals(x, x),
                        Vector128.ConditionalSelect(Vector128.Equals(y, y),
                            Vector128.ConditionalSelect(Vector128.Equals(yMag, xMag),
                                Vector128.ConditionalSelect(IsNegative(x), y, x),
                                Vector128.ConditionalSelect(Vector128.GreaterThan(yMag, xMag), y, x)),
                            y),
                        x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y)
            {
                Vector256<float> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);
                return
                    Vector256.ConditionalSelect(Vector256.Equals(x, x),
                        Vector256.ConditionalSelect(Vector256.Equals(y, y),
                            Vector256.ConditionalSelect(Vector256.Equals(xMag, yMag),
                                Vector256.ConditionalSelect(IsNegative(x), y, x),
                                Vector256.ConditionalSelect(Vector256.GreaterThan(xMag, yMag), x, y)),
                            y),
                        x);
            }

#if NET8_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y)
            {
                Vector512<float> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);
                return
                    Vector512.ConditionalSelect(Vector512.Equals(x, x),
                        Vector512.ConditionalSelect(Vector512.Equals(y, y),
                            Vector512.ConditionalSelect(Vector512.Equals(xMag, yMag),
                                Vector512.ConditionalSelect(IsNegative(x), y, x),
                                Vector512.ConditionalSelect(Vector512.GreaterThan(xMag, yMag), x, y)),
                            y),
                        x);
            }
#endif
        }

        /// <summary>MathF.Min(x, y) (but NaNs may not be propagated)</summary>
        private readonly struct MinOperator : IAggregationOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Invoke(float x, float y) =>
                x == y ?
                    (IsNegative(y) ? y : x) :
                    (y < x ? y : x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y)
            {
                if (AdvSimd.IsSupported)
                {
                    return AdvSimd.Min(x, y);
                }

                return
                    Vector128.ConditionalSelect(Vector128.Equals(x, y),
                        Vector128.ConditionalSelect(IsNegative(y), y, x),
                        Vector128.Min(x, y));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) =>
                Vector256.ConditionalSelect(Vector256.Equals(x, y),
                    Vector256.ConditionalSelect(IsNegative(y), y, x),
                    Vector256.Min(x, y));

#if NET8_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) =>
                Vector512.ConditionalSelect(Vector512.Equals(x, y),
                    Vector512.ConditionalSelect(IsNegative(y), y, x),
                    Vector512.Min(x, y));
#endif

            public static float Invoke(Vector128<float> x) => HorizontalAggregate<MinOperator>(x);
            public static float Invoke(Vector256<float> x) => HorizontalAggregate<MinOperator>(x);
#if NET8_0_OR_GREATER
            public static float Invoke(Vector512<float> x) => HorizontalAggregate<MinOperator>(x);
#endif
        }

        /// <summary>MathF.Min(x, y)</summary>
        private readonly struct MinPropagateNaNOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Invoke(float x, float y) => MathF.Min(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y)
            {
                if (AdvSimd.IsSupported)
                {
                    return AdvSimd.Min(x, y);
                }

                return
                    Vector128.ConditionalSelect(Vector128.Equals(x, x),
                        Vector128.ConditionalSelect(Vector128.Equals(y, y),
                            Vector128.ConditionalSelect(Vector128.Equals(x, y),
                                Vector128.ConditionalSelect(IsNegative(x), x, y),
                                Vector128.Min(x, y)),
                            y),
                        x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) =>
                Vector256.ConditionalSelect(Vector256.Equals(x, x),
                    Vector256.ConditionalSelect(Vector256.Equals(y, y),
                        Vector256.ConditionalSelect(Vector256.Equals(x, y),
                            Vector256.ConditionalSelect(IsNegative(x), x, y),
                            Vector256.Min(x, y)),
                        y),
                    x);

#if NET8_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) =>
                Vector512.ConditionalSelect(Vector512.Equals(x, x),
                    Vector512.ConditionalSelect(Vector512.Equals(y, y),
                        Vector512.ConditionalSelect(Vector512.Equals(x, y),
                            Vector512.ConditionalSelect(IsNegative(x), x, y),
                            Vector512.Min(x, y)),
                        y),
                    x);
#endif
        }

        /// <summary>Operator to get x or y based on which has the smaller MathF.Abs (but NaNs may not be propagated)</summary>
        private readonly struct MinMagnitudeOperator : IAggregationOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Invoke(float x, float y)
            {
                float xMag = MathF.Abs(x), yMag = MathF.Abs(y);
                return xMag == yMag ?
                    (IsNegative(y) ? y : x) :
                    (yMag < xMag ? y : x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y)
            {
                Vector128<float> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);
                return
                    Vector128.ConditionalSelect(Vector128.Equals(yMag, xMag),
                        Vector128.ConditionalSelect(IsNegative(y), y, x),
                        Vector128.ConditionalSelect(Vector128.LessThan(yMag, xMag), y, x));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y)
            {
                Vector256<float> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);
                return
                    Vector256.ConditionalSelect(Vector256.Equals(yMag, xMag),
                        Vector256.ConditionalSelect(IsNegative(y), y, x),
                        Vector256.ConditionalSelect(Vector256.LessThan(yMag, xMag), y, x));
            }

#if NET8_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y)
            {
                Vector512<float> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);
                return
                    Vector512.ConditionalSelect(Vector512.Equals(yMag, xMag),
                        Vector512.ConditionalSelect(IsNegative(y), y, x),
                        Vector512.ConditionalSelect(Vector512.LessThan(yMag, xMag), y, x));
            }
#endif

            public static float Invoke(Vector128<float> x) => HorizontalAggregate<MinMagnitudeOperator>(x);
            public static float Invoke(Vector256<float> x) => HorizontalAggregate<MinMagnitudeOperator>(x);
#if NET8_0_OR_GREATER
            public static float Invoke(Vector512<float> x) => HorizontalAggregate<MinMagnitudeOperator>(x);
#endif
        }

        /// <summary>Operator to get x or y based on which has the smaller MathF.Abs</summary>
        private readonly struct MinMagnitudePropagateNaNOperator : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Invoke(float x, float y) => MathF.MinMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y)
            {
                Vector128<float> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);
                return
                    Vector128.ConditionalSelect(Vector128.Equals(x, x),
                        Vector128.ConditionalSelect(Vector128.Equals(y, y),
                            Vector128.ConditionalSelect(Vector128.Equals(yMag, xMag),
                                Vector128.ConditionalSelect(IsNegative(x), x, y),
                                Vector128.ConditionalSelect(Vector128.LessThan(xMag, yMag), x, y)),
                            y),
                        x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y)
            {
                Vector256<float> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);
                return
                    Vector256.ConditionalSelect(Vector256.Equals(x, x),
                        Vector256.ConditionalSelect(Vector256.Equals(y, y),
                            Vector256.ConditionalSelect(Vector256.Equals(yMag, xMag),
                                Vector256.ConditionalSelect(IsNegative(x), x, y),
                                Vector256.ConditionalSelect(Vector256.LessThan(xMag, yMag), x, y)),
                            y),
                        x);
            }

#if NET8_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y)
            {
                Vector512<float> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);
                return
                    Vector512.ConditionalSelect(Vector512.Equals(x, x),
                        Vector512.ConditionalSelect(Vector512.Equals(y, y),
                            Vector512.ConditionalSelect(Vector512.Equals(yMag, xMag),
                                Vector512.ConditionalSelect(IsNegative(x), x, y),
                                Vector512.ConditionalSelect(Vector512.LessThan(xMag, yMag), x, y)),
                            y),
                        x);
            }
#endif
        }

        /// <summary>-x</summary>
        private readonly struct NegateOperator : IUnaryOperator
        {
            public static float Invoke(float x) => -x;
            public static Vector128<float> Invoke(Vector128<float> x) => -x;
            public static Vector256<float> Invoke(Vector256<float> x) => -x;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => -x;
#endif
        }

        /// <summary>(x + y) * z</summary>
        private readonly struct AddMultiplyOperator : ITernaryOperator
        {
            public static float Invoke(float x, float y, float z) => (x + y) * z;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y, Vector128<float> z) => (x + y) * z;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y, Vector256<float> z) => (x + y) * z;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y, Vector512<float> z) => (x + y) * z;
#endif
        }

        /// <summary>(x * y) + z</summary>
        private readonly struct MultiplyAddOperator : ITernaryOperator
        {
            public static float Invoke(float x, float y, float z) => (x * y) + z;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y, Vector128<float> z) => (x * y) + z;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y, Vector256<float> z) => (x * y) + z;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y, Vector512<float> z) => (x * y) + z;
#endif
        }

        /// <summary>x</summary>
        private readonly struct IdentityOperator : IUnaryOperator
        {
            public static float Invoke(float x) => x;
            public static Vector128<float> Invoke(Vector128<float> x) => x;
            public static Vector256<float> Invoke(Vector256<float> x) => x;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => x;
#endif
        }

        /// <summary>x * x</summary>
        private readonly struct SquaredOperator : IUnaryOperator
        {
            public static float Invoke(float x) => x * x;
            public static Vector128<float> Invoke(Vector128<float> x) => x * x;
            public static Vector256<float> Invoke(Vector256<float> x) => x * x;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => x * x;
#endif
        }

        /// <summary>MathF.Abs(x)</summary>
        private readonly struct AbsoluteOperator : IUnaryOperator
        {
            public static float Invoke(float x) => MathF.Abs(x);
            public static Vector128<float> Invoke(Vector128<float> x) => Vector128.Abs(x);
            public static Vector256<float> Invoke(Vector256<float> x) => Vector256.Abs(x);
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => Vector512.Abs(x);
#endif
        }

        /// <summary>MathF.Exp(x)</summary>
        private readonly struct ExpOperator : IUnaryOperator
        {
            // This code is based on `vrs4_expf` from amd/aocl-libm-ose
            // Copyright (C) 2019-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes:
            // 1. Argument Reduction:
            //      e^x = 2^(x/ln2)                          --- (1)
            //
            //      Let x/ln(2) = z                          --- (2)
            //
            //      Let z = n + r , where n is an integer    --- (3)
            //                      |r| <= 1/2
            //
            //     From (1), (2) and (3),
            //      e^x = 2^z
            //          = 2^(N+r)
            //          = (2^N)*(2^r)                        --- (4)
            //
            // 2. Polynomial Evaluation
            //   From (4),
            //     r   = z - N
            //     2^r = C1 + C2*r + C3*r^2 + C4*r^3 + C5 *r^4 + C6*r^5
            //
            // 4. Reconstruction
            //      Thus,
            //        e^x = (2^N) * (2^r)

            private const uint V_ARG_MAX = 0x42AE0000;
            private const uint V_MASK = 0x7FFFFFFF;

            private const float V_EXPF_MIN = -103.97208f;
            private const float V_EXPF_MAX = 88.72284f;

            private const double V_EXPF_HUGE = 6755399441055744;
            private const double V_TBL_LN2 = 1.4426950408889634;

            private const double C1 = 1.0000000754895704;
            private const double C2 = 0.6931472254087585;
            private const double C3 = 0.2402210737432219;
            private const double C4 = 0.05550297297702539;
            private const double C5 = 0.009676036358193323;
            private const double C6 = 0.001341000536524434;

            public static float Invoke(float x) => MathF.Exp(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                // Convert x to double precision
                (Vector128<double> xl, Vector128<double> xu) = Vector128.Widen(x);

                // x * (64.0 / ln(2))
                Vector128<double> v_tbl_ln2 = Vector128.Create(V_TBL_LN2);

                Vector128<double> zl = xl * v_tbl_ln2;
                Vector128<double> zu = xu * v_tbl_ln2;

                Vector128<double> v_expf_huge = Vector128.Create(V_EXPF_HUGE);

                Vector128<double> dnl = zl + v_expf_huge;
                Vector128<double> dnu = zu + v_expf_huge;

                // n = int (z)
                Vector128<ulong> nl = dnl.AsUInt64();
                Vector128<ulong> nu = dnu.AsUInt64();

                // dn = double(n)
                dnl -= v_expf_huge;
                dnu -= v_expf_huge;

                // r = z - dn
                Vector128<double> c1 = Vector128.Create(C1);
                Vector128<double> c2 = Vector128.Create(C2);
                Vector128<double> c3 = Vector128.Create(C3);
                Vector128<double> c4 = Vector128.Create(C4);
                Vector128<double> c5 = Vector128.Create(C5);
                Vector128<double> c6 = Vector128.Create(C6);

                Vector128<double> rl = zl - dnl;

                Vector128<double> rl2 = rl * rl;
                Vector128<double> rl4 = rl2 * rl2;

                Vector128<double> polyl = (c4 * rl + c3) * rl2
                                       + ((c6 * rl + c5) * rl4
                                        + (c2 * rl + c1));


                Vector128<double> ru = zu - dnu;

                Vector128<double> ru2 = ru * ru;
                Vector128<double> ru4 = ru2 * ru2;

                Vector128<double> polyu = (c4 * ru + c3) * ru2
                                       + ((c6 * ru + c5) * ru4
                                        + (c2 * ru + c1));

                // result = (float)[poly + (n << 52)]
                Vector128<float> ret = Vector128.Narrow(
                    (polyl.AsUInt64() + Vector128.ShiftLeft(nl, 52)).AsDouble(),
                    (polyu.AsUInt64() + Vector128.ShiftLeft(nu, 52)).AsDouble()
                );

                // Check if -103 < |x| < 88
                if (Vector128.GreaterThanAny(x.AsUInt32() & Vector128.Create(V_MASK), Vector128.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                    Vector128<float> infinityMask = Vector128.GreaterThan(x, Vector128.Create(V_EXPF_MAX));

                    ret = Vector128.ConditionalSelect(
                        infinityMask,
                        Vector128.Create(float.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector128.AndNot(ret, Vector128.LessThan(x, Vector128.Create(V_EXPF_MIN)));
                }

                return ret;
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                // Convert x to double precision
                (Vector256<double> xl, Vector256<double> xu) = Vector256.Widen(x);

                // x * (64.0 / ln(2))
                Vector256<double> v_tbl_ln2 = Vector256.Create(V_TBL_LN2);

                Vector256<double> zl = xl * v_tbl_ln2;
                Vector256<double> zu = xu * v_tbl_ln2;

                Vector256<double> v_expf_huge = Vector256.Create(V_EXPF_HUGE);

                Vector256<double> dnl = zl + v_expf_huge;
                Vector256<double> dnu = zu + v_expf_huge;

                // n = int (z)
                Vector256<ulong> nl = dnl.AsUInt64();
                Vector256<ulong> nu = dnu.AsUInt64();

                // dn = double(n)
                dnl -= v_expf_huge;
                dnu -= v_expf_huge;

                // r = z - dn
                Vector256<double> c1 = Vector256.Create(C1);
                Vector256<double> c2 = Vector256.Create(C2);
                Vector256<double> c3 = Vector256.Create(C3);
                Vector256<double> c4 = Vector256.Create(C4);
                Vector256<double> c5 = Vector256.Create(C5);
                Vector256<double> c6 = Vector256.Create(C6);

                Vector256<double> rl = zl - dnl;

                Vector256<double> rl2 = rl * rl;
                Vector256<double> rl4 = rl2 * rl2;

                Vector256<double> polyl = (c4 * rl + c3) * rl2
                                       + ((c6 * rl + c5) * rl4
                                        + (c2 * rl + c1));


                Vector256<double> ru = zu - dnu;

                Vector256<double> ru2 = ru * ru;
                Vector256<double> ru4 = ru2 * ru2;

                Vector256<double> polyu = (c4 * ru + c3) * ru2
                                       + ((c6 * ru + c5) * ru4
                                        + (c2 * ru + c1));

                // result = (float)[poly + (n << 52)]
                Vector256<float> ret = Vector256.Narrow(
                    (polyl.AsUInt64() + Vector256.ShiftLeft(nl, 52)).AsDouble(),
                    (polyu.AsUInt64() + Vector256.ShiftLeft(nu, 52)).AsDouble()
                );

                // Check if -103 < |x| < 88
                if (Vector256.GreaterThanAny(x.AsUInt32() & Vector256.Create(V_MASK), Vector256.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                    Vector256<float> infinityMask = Vector256.GreaterThan(x, Vector256.Create(V_EXPF_MAX));

                    ret = Vector256.ConditionalSelect(
                        infinityMask,
                        Vector256.Create(float.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector256.AndNot(ret, Vector256.LessThan(x, Vector256.Create(V_EXPF_MIN)));
                }

                return ret;
            }

#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x)
            {
                // Convert x to double precision
                (Vector512<double> xl, Vector512<double> xu) = Vector512.Widen(x);

                // x * (64.0 / ln(2))
                Vector512<double> v_tbl_ln2 = Vector512.Create(V_TBL_LN2);

                Vector512<double> zl = xl * v_tbl_ln2;
                Vector512<double> zu = xu * v_tbl_ln2;

                Vector512<double> v_expf_huge = Vector512.Create(V_EXPF_HUGE);

                Vector512<double> dnl = zl + v_expf_huge;
                Vector512<double> dnu = zu + v_expf_huge;

                // n = int (z)
                Vector512<ulong> nl = dnl.AsUInt64();
                Vector512<ulong> nu = dnu.AsUInt64();

                // dn = double(n)
                dnl -= v_expf_huge;
                dnu -= v_expf_huge;

                // r = z - dn
                Vector512<double> c1 = Vector512.Create(C1);
                Vector512<double> c2 = Vector512.Create(C2);
                Vector512<double> c3 = Vector512.Create(C3);
                Vector512<double> c4 = Vector512.Create(C4);
                Vector512<double> c5 = Vector512.Create(C5);
                Vector512<double> c6 = Vector512.Create(C6);

                Vector512<double> rl = zl - dnl;

                Vector512<double> rl2 = rl * rl;
                Vector512<double> rl4 = rl2 * rl2;

                Vector512<double> polyl = (c4 * rl + c3) * rl2
                                       + ((c6 * rl + c5) * rl4
                                        + (c2 * rl + c1));


                Vector512<double> ru = zu - dnu;

                Vector512<double> ru2 = ru * ru;
                Vector512<double> ru4 = ru2 * ru2;

                Vector512<double> polyu = (c4 * ru + c3) * ru2
                                       + ((c6 * ru + c5) * ru4
                                        + (c2 * ru + c1));

                // result = (float)[poly + (n << 52)]
                Vector512<float> ret = Vector512.Narrow(
                    (polyl.AsUInt64() + Vector512.ShiftLeft(nl, 52)).AsDouble(),
                    (polyu.AsUInt64() + Vector512.ShiftLeft(nu, 52)).AsDouble()
                );

                // Check if -103 < |x| < 88
                if (Vector512.GreaterThanAny(x.AsUInt32() & Vector512.Create(V_MASK), Vector512.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                    Vector512<float> infinityMask = Vector512.GreaterThan(x, Vector512.Create(V_EXPF_MAX));

                    ret = Vector512.ConditionalSelect(
                        infinityMask,
                        Vector512.Create(float.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector512.AndNot(ret, Vector512.LessThan(x, Vector512.Create(V_EXPF_MIN)));
                }

                return ret;
            }
#endif
        }

        /// <summary>MathF.Cosh(x)</summary>
        private readonly struct CoshOperator : IUnaryOperator
        {
            // This code is based on `vrs4_coshf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Spec:
            //   coshf(|x| > 89.415985107421875) = Infinity
            //   coshf(Infinity)  = infinity
            //   coshf(-Infinity) = infinity
            //
            // cosh(x) = (exp(x) + exp(-x))/2
            // cosh(-x) = +cosh(x)
            //
            // checks for special cases
            // if ( asint(x) > infinity) return x with overflow exception and
            // return x.
            // if x is NaN then raise invalid FP operation exception and return x.
            //
            // coshf = v/2 * exp(x - log(v)) where v = 0x1.0000e8p-1

            private const float LOGV = 0.693161f;
            private const float HALFV = 1.0000138f;
            private const float INVV2 = 0.24999309f;

            public static float Invoke(float x) => MathF.Cosh(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> y = Vector128.Abs(x);
                Vector128<float> z = ExpOperator.Invoke(y - Vector128.Create(LOGV));
                return Vector128.Create(HALFV) * (z + (Vector128.Create(INVV2) / z));
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> y = Vector256.Abs(x);
                Vector256<float> z = ExpOperator.Invoke(y - Vector256.Create(LOGV));
                return Vector256.Create(HALFV) * (z + (Vector256.Create(INVV2) / z));
            }

#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> y = Vector512.Abs(x);
                Vector512<float> z = ExpOperator.Invoke(y - Vector512.Create(LOGV));
                return Vector512.Create(HALFV) * (z + (Vector512.Create(INVV2) / z));
            }
#endif
        }

        /// <summary>MathF.Sinh(x)</summary>
        private readonly struct SinhOperator : IUnaryOperator
        {
            // Same as cosh, but with `z -` rather than `z +`, and with the sign
            // flipped on the result based on the sign of the input.

            private const uint SIGN_MASK = 0x7FFFFFFF;
            private const float LOGV = 0.693161f;
            private const float HALFV = 1.0000138f;
            private const float INVV2 = 0.24999309f;

            public static float Invoke(float x) => MathF.Sinh(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> y = Vector128.Abs(x);
                Vector128<float> z = ExpOperator.Invoke(y - Vector128.Create(LOGV));
                Vector128<float> result = Vector128.Create(HALFV) * (z - (Vector128.Create(INVV2) / z));
                Vector128<uint> sign = x.AsUInt32() & Vector128.Create(~SIGN_MASK);
                return (sign ^ result.AsUInt32()).AsSingle();
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> y = Vector256.Abs(x);
                Vector256<float> z = ExpOperator.Invoke(y - Vector256.Create(LOGV));
                Vector256<float> result = Vector256.Create(HALFV) * (z - (Vector256.Create(INVV2) / z));
                Vector256<uint> sign = x.AsUInt32() & Vector256.Create(~SIGN_MASK);
                return (sign ^ result.AsUInt32()).AsSingle();
            }

#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> y = Vector512.Abs(x);
                Vector512<float> z = ExpOperator.Invoke(y - Vector512.Create(LOGV));
                Vector512<float> result = Vector512.Create(HALFV) * (z - (Vector512.Create(INVV2) / z));
                Vector512<uint> sign = x.AsUInt32() & Vector512.Create(~SIGN_MASK);
                return (sign ^ result.AsUInt32()).AsSingle();
            }
#endif
        }

        /// <summary>MathF.Tanh(x)</summary>
        private readonly struct TanhOperator : IUnaryOperator
        {
            // This code is based on `vrs4_tanhf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // To compute vrs4_tanhf(v_f32x4_t x)
            // Let y = |x|
            // If 0 <= y < 0x1.154246p3
            //    Let z = e^(-2.0 * y) - 1      -(1)
            //
            //    Using (1), tanhf(y) can be calculated as,
            //    tanhf(y) = -z / (z + 2.0)
            //
            // For other cases, call scalar tanhf()
            //
            // If x < 0, then we use the identity
            //    tanhf(-x) = -tanhf(x)

            private const uint SIGN_MASK = 0x7FFFFFFF;

            public static float Invoke(float x) => MathF.Tanh(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> y = Vector128.Abs(x);
                Vector128<float> z = ExpOperator.Invoke(Vector128.Create(-2f) * y) - Vector128.Create(1f);
                Vector128<uint> sign = x.AsUInt32() & Vector128.Create(~SIGN_MASK);
                return (sign ^ (-z / (z + Vector128.Create(2f))).AsUInt32()).AsSingle();
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> y = Vector256.Abs(x);
                Vector256<float> z = ExpOperator.Invoke(Vector256.Create(-2f) * y) - Vector256.Create(1f);
                Vector256<uint> sign = x.AsUInt32() & Vector256.Create(~SIGN_MASK);
                return (sign ^ (-z / (z + Vector256.Create(2f))).AsUInt32()).AsSingle();
            }

#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> y = Vector512.Abs(x);
                Vector512<float> z = ExpOperator.Invoke(Vector512.Create(-2f) * y) - Vector512.Create(1f);
                Vector512<uint> sign = x.AsUInt32() & Vector512.Create(~SIGN_MASK);
                return (sign ^ (-z / (z + Vector512.Create(2f))).AsUInt32()).AsSingle();
            }
#endif
        }

        /// <summary>MathF.Log(x)</summary>
        private readonly struct LogOperator : IUnaryOperator
        {
            // This code is based on `vrs4_logf` from amd/aocl-libm-ose
            // Copyright (C) 2018-2019 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Spec:
            //   logf(x)
            //          = logf(x)           if x ∈ F and x > 0
            //          = x                 if x = qNaN
            //          = 0                 if x = 1
            //          = -inf              if x = (-0, 0}
            //          = NaN               otherwise
            //
            // Assumptions/Expectations
            //      - ULP is derived to be << 4 (always)
            // - Some FPU Exceptions may not be available
            //      - Performance is at least 3x
            //
            // Implementation Notes:
            //  1. Range Reduction:
            //      x = 2^n*(1+f)                                          .... (1)
            //         where n is exponent and is an integer
            //             (1+f) is mantissa ∈ [1,2). i.e., 1 ≤ 1+f < 2    .... (2)
            //
            //    From (1), taking log on both sides
            //      log(x) = log(2^n * (1+f))
            //             = log(2^n) + log(1+f)
            //             = n*log(2) + log(1+f)                           .... (3)
            //
            //      let z = 1 + f
            //             log(z) = log(k) + log(z) - log(k)
            //             log(z) = log(kz) - log(k)
            //
            //    From (2), range of z is [1, 2)
            //       by simply dividing range by 'k', z is in [1/k, 2/k)  .... (4)
            //       Best choice of k is the one which gives equal and opposite values
            //       at extrema        +-      -+
            //              1          | 2      |
            //             --- - 1 = - |--- - 1 |
            //              k          | k      |                         .... (5)
            //                         +-      -+
            //
            //       Solving for k, k = 3/2,
            //    From (4), using 'k' value, range is therefore [-0.3333, 0.3333]
            //
            //  2. Polynomial Approximation:
            //     More information refer to tools/sollya/vrs4_logf.sollya
            //
            //     7th Deg -   Error abs: 0x1.04c4ac98p-22   rel: 0x1.2216e6f8p-19
            //     6th Deg -   Error abs: 0x1.179e97d8p-19   rel: 0x1.db676c1p-17

            private const uint V_MIN = 0x00800000;
            private const uint V_MAX = 0x7F800000;
            private const uint V_MASK = 0x007FFFFF;
            private const uint V_OFF = 0x3F2AAAAB;

            private const float V_LN2 = 0.6931472f;

            private const float C0 = 0.0f;
            private const float C1 = 1.0f;
            private const float C2 = -0.5000001f;
            private const float C3 = 0.33332965f;
            private const float C4 = -0.24999046f;
            private const float C5 = 0.20018855f;
            private const float C6 = -0.16700386f;
            private const float C7 = 0.13902695f;
            private const float C8 = -0.1197452f;
            private const float C9 = 0.14401625f;
            private const float C10 = -0.13657966f;

            public static float Invoke(float x) => MathF.Log(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector128<uint> specialMask = Vector128.GreaterThanOrEqual(x.AsUInt32() - Vector128.Create(V_MIN), Vector128.Create(V_MAX - V_MIN));

                if (specialMask != Vector128<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector128<float> zeroMask = Vector128.Equals(x, Vector128<float>.Zero);

                    specialResult = Vector128.ConditionalSelect(
                        zeroMask,
                        Vector128.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector128<float> lessThanZeroMask = Vector128.LessThan(x, Vector128<float>.Zero);

                    specialResult = Vector128.ConditionalSelect(
                        lessThanZeroMask,
                        Vector128.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector128<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector128.Equals(x, x)
                                          | Vector128.Equals(x, Vector128.Create(float.PositiveInfinity));

                    // subnormal
                    Vector128<float> subnormalMask = Vector128.AndNot(specialMask.AsSingle(), temp);

                    x = Vector128.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector128.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector128<uint> vx = x.AsUInt32() - Vector128.Create(V_OFF);
                Vector128<float> n = Vector128.ConvertToSingle(Vector128.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector128.Create(V_MASK)) + Vector128.Create(V_OFF);

                Vector128<float> r = vx.AsSingle() - Vector128.Create(1.0f);

                Vector128<float> r2 = r * r;
                Vector128<float> r4 = r2 * r2;
                Vector128<float> r8 = r4 * r4;

                Vector128<float> q = (Vector128.Create(C10) * r2 + (Vector128.Create(C9) * r + Vector128.Create(C8)))
                                                          * r8 + (((Vector128.Create(C7) * r + Vector128.Create(C6))
                                                            * r2 + (Vector128.Create(C5) * r + Vector128.Create(C4)))
                                                           * r4 + ((Vector128.Create(C3) * r + Vector128.Create(C2))
                                                            * r2 + (Vector128.Create(C1) * r + Vector128.Create(C0))));

                return Vector128.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n * Vector128.Create(V_LN2) + q
                );
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector256<uint> specialMask = Vector256.GreaterThanOrEqual(x.AsUInt32() - Vector256.Create(V_MIN), Vector256.Create(V_MAX - V_MIN));

                if (specialMask != Vector256<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector256<float> zeroMask = Vector256.Equals(x, Vector256<float>.Zero);

                    specialResult = Vector256.ConditionalSelect(
                        zeroMask,
                        Vector256.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector256<float> lessThanZeroMask = Vector256.LessThan(x, Vector256<float>.Zero);

                    specialResult = Vector256.ConditionalSelect(
                        lessThanZeroMask,
                        Vector256.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector256<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector256.Equals(x, x)
                                          | Vector256.Equals(x, Vector256.Create(float.PositiveInfinity));

                    // subnormal
                    Vector256<float> subnormalMask = Vector256.AndNot(specialMask.AsSingle(), temp);

                    x = Vector256.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector256.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector256<uint> vx = x.AsUInt32() - Vector256.Create(V_OFF);
                Vector256<float> n = Vector256.ConvertToSingle(Vector256.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector256.Create(V_MASK)) + Vector256.Create(V_OFF);

                Vector256<float> r = vx.AsSingle() - Vector256.Create(1.0f);

                Vector256<float> r2 = r * r;
                Vector256<float> r4 = r2 * r2;
                Vector256<float> r8 = r4 * r4;

                Vector256<float> q = (Vector256.Create(C10) * r2 + (Vector256.Create(C9) * r + Vector256.Create(C8)))
                                                          * r8 + (((Vector256.Create(C7) * r + Vector256.Create(C6))
                                                            * r2 + (Vector256.Create(C5) * r + Vector256.Create(C4)))
                                                           * r4 + ((Vector256.Create(C3) * r + Vector256.Create(C2))
                                                            * r2 + (Vector256.Create(C1) * r + Vector256.Create(C0))));

                return Vector256.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n * Vector256.Create(V_LN2) + q
                );
            }

#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector512<uint> specialMask = Vector512.GreaterThanOrEqual(x.AsUInt32() - Vector512.Create(V_MIN), Vector512.Create(V_MAX - V_MIN));

                if (specialMask != Vector512<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector512<float> zeroMask = Vector512.Equals(x, Vector512<float>.Zero);

                    specialResult = Vector512.ConditionalSelect(
                        zeroMask,
                        Vector512.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector512<float> lessThanZeroMask = Vector512.LessThan(x, Vector512<float>.Zero);

                    specialResult = Vector512.ConditionalSelect(
                        lessThanZeroMask,
                        Vector512.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector512<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector512.Equals(x, x)
                                          | Vector512.Equals(x, Vector512.Create(float.PositiveInfinity));

                    // subnormal
                    Vector512<float> subnormalMask = Vector512.AndNot(specialMask.AsSingle(), temp);

                    x = Vector512.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector512.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector512<uint> vx = x.AsUInt32() - Vector512.Create(V_OFF);
                Vector512<float> n = Vector512.ConvertToSingle(Vector512.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector512.Create(V_MASK)) + Vector512.Create(V_OFF);

                Vector512<float> r = vx.AsSingle() - Vector512.Create(1.0f);

                Vector512<float> r2 = r * r;
                Vector512<float> r4 = r2 * r2;
                Vector512<float> r8 = r4 * r4;

                Vector512<float> q = (Vector512.Create(C10) * r2 + (Vector512.Create(C9) * r + Vector512.Create(C8)))
                                                          * r8 + (((Vector512.Create(C7) * r + Vector512.Create(C6))
                                                            * r2 + (Vector512.Create(C5) * r + Vector512.Create(C4)))
                                                           * r4 + ((Vector512.Create(C3) * r + Vector512.Create(C2))
                                                            * r2 + (Vector512.Create(C1) * r + Vector512.Create(C0))));

                return Vector512.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n * Vector512.Create(V_LN2) + q
                );
            }
#endif
        }

        /// <summary>MathF.Log2(x)</summary>
        private readonly struct Log2Operator : IUnaryOperator
        {
            // This code is based on `vrs4_log2f` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Spec:
            //   log2f(x)
            //          = log2f(x)          if x ∈ F and x > 0
            //          = x                 if x = qNaN
            //          = 0                 if x = 1
            //          = -inf              if x = (-0, 0}
            //          = NaN               otherwise
            //
            // Assumptions/Expectations
            //      - Maximum ULP is observed to be at 4
            //      - Some FPU Exceptions may not be available
            //      - Performance is at least 3x
            //
            // Implementation Notes:
            //  1. Range Reduction:
            //      x = 2^n*(1+f)                                          .... (1)
            //         where n is exponent and is an integer
            //             (1+f) is mantissa ∈ [1,2). i.e., 1 ≤ 1+f < 2    .... (2)
            //
            //    From (1), taking log on both sides
            //      log2(x) = log2(2^n * (1+f))
            //             = n + log2(1+f)                           .... (3)
            //
            //      let z = 1 + f
            //             log2(z) = log2(k) + log2(z) - log2(k)
            //             log2(z) = log2(kz) - log2(k)
            //
            //    From (2), range of z is [1, 2)
            //       by simply dividing range by 'k', z is in [1/k, 2/k)  .... (4)
            //       Best choice of k is the one which gives equal and opposite values
            //       at extrema        +-      -+
            //              1          | 2      |
            //             --- - 1 = - |--- - 1 |
            //              k          | k      |                         .... (5)
            //                         +-      -+
            //
            //       Solving for k, k = 3/2,
            //    From (4), using 'k' value, range is therefore [-0.3333, 0.3333]
            //
            //  2. Polynomial Approximation:
            //     More information refer to tools/sollya/vrs4_logf.sollya
            //
            //     7th Deg -   Error abs: 0x1.04c4ac98p-22   rel: 0x1.2216e6f8p-19

            private const uint V_MIN = 0x00800000;
            private const uint V_MAX = 0x7F800000;
            private const uint V_MASK = 0x007FFFFF;
            private const uint V_OFF = 0x3F2AAAAB;

            private const float C0 = 0.0f;
            private const float C1 = 1.4426951f;
            private const float C2 = -0.72134554f;
            private const float C3 = 0.48089063f;
            private const float C4 = -0.36084408f;
            private const float C5 = 0.2888971f;
            private const float C6 = -0.23594281f;
            private const float C7 = 0.19948183f;
            private const float C8 = -0.22616665f;
            private const float C9 = 0.21228963f;

            public static float Invoke(float x) => MathF.Log2(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector128<uint> specialMask = Vector128.GreaterThanOrEqual(x.AsUInt32() - Vector128.Create(V_MIN), Vector128.Create(V_MAX - V_MIN));

                if (specialMask != Vector128<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector128<float> zeroMask = Vector128.Equals(x, Vector128<float>.Zero);

                    specialResult = Vector128.ConditionalSelect(
                        zeroMask,
                        Vector128.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector128<float> lessThanZeroMask = Vector128.LessThan(x, Vector128<float>.Zero);

                    specialResult = Vector128.ConditionalSelect(
                        lessThanZeroMask,
                        Vector128.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector128<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector128.Equals(x, x)
                                          | Vector128.Equals(x, Vector128.Create(float.PositiveInfinity));

                    // subnormal
                    Vector128<float> subnormalMask = Vector128.AndNot(specialMask.AsSingle(), temp);

                    x = Vector128.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector128.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector128<uint> vx = x.AsUInt32() - Vector128.Create(V_OFF);
                Vector128<float> n = Vector128.ConvertToSingle(Vector128.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector128.Create(V_MASK)) + Vector128.Create(V_OFF);

                Vector128<float> r = vx.AsSingle() - Vector128.Create(1.0f);

                Vector128<float> r2 = r * r;
                Vector128<float> r4 = r2 * r2;
                Vector128<float> r8 = r4 * r4;

                Vector128<float> poly = (Vector128.Create(C9) * r + Vector128.Create(C8)) * r8
                                    + (((Vector128.Create(C7) * r + Vector128.Create(C6)) * r2
                                      + (Vector128.Create(C5) * r + Vector128.Create(C4))) * r4
                                     + ((Vector128.Create(C3) * r + Vector128.Create(C2)) * r2
                                      + (Vector128.Create(C1) * r + Vector128.Create(C0))));

                return Vector128.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n + poly
                );
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector256<uint> specialMask = Vector256.GreaterThanOrEqual(x.AsUInt32() - Vector256.Create(V_MIN), Vector256.Create(V_MAX - V_MIN));

                if (specialMask != Vector256<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector256<float> zeroMask = Vector256.Equals(x, Vector256<float>.Zero);

                    specialResult = Vector256.ConditionalSelect(
                        zeroMask,
                        Vector256.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector256<float> lessThanZeroMask = Vector256.LessThan(x, Vector256<float>.Zero);

                    specialResult = Vector256.ConditionalSelect(
                        lessThanZeroMask,
                        Vector256.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector256<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector256.Equals(x, x)
                                          | Vector256.Equals(x, Vector256.Create(float.PositiveInfinity));

                    // subnormal
                    Vector256<float> subnormalMask = Vector256.AndNot(specialMask.AsSingle(), temp);

                    x = Vector256.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector256.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector256<uint> vx = x.AsUInt32() - Vector256.Create(V_OFF);
                Vector256<float> n = Vector256.ConvertToSingle(Vector256.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector256.Create(V_MASK)) + Vector256.Create(V_OFF);

                Vector256<float> r = vx.AsSingle() - Vector256.Create(1.0f);

                Vector256<float> r2 = r * r;
                Vector256<float> r4 = r2 * r2;
                Vector256<float> r8 = r4 * r4;

                Vector256<float> poly = (Vector256.Create(C9) * r + Vector256.Create(C8)) * r8
                                    + (((Vector256.Create(C7) * r + Vector256.Create(C6)) * r2
                                      + (Vector256.Create(C5) * r + Vector256.Create(C4))) * r4
                                     + ((Vector256.Create(C3) * r + Vector256.Create(C2)) * r2
                                      + (Vector256.Create(C1) * r + Vector256.Create(C0))));

                return Vector256.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n + poly
                );
            }

#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector512<uint> specialMask = Vector512.GreaterThanOrEqual(x.AsUInt32() - Vector512.Create(V_MIN), Vector512.Create(V_MAX - V_MIN));

                if (specialMask != Vector512<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector512<float> zeroMask = Vector512.Equals(x, Vector512<float>.Zero);

                    specialResult = Vector512.ConditionalSelect(
                        zeroMask,
                        Vector512.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector512<float> lessThanZeroMask = Vector512.LessThan(x, Vector512<float>.Zero);

                    specialResult = Vector512.ConditionalSelect(
                        lessThanZeroMask,
                        Vector512.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector512<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector512.Equals(x, x)
                                          | Vector512.Equals(x, Vector512.Create(float.PositiveInfinity));

                    // subnormal
                    Vector512<float> subnormalMask = Vector512.AndNot(specialMask.AsSingle(), temp);

                    x = Vector512.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector512.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector512<uint> vx = x.AsUInt32() - Vector512.Create(V_OFF);
                Vector512<float> n = Vector512.ConvertToSingle(Vector512.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector512.Create(V_MASK)) + Vector512.Create(V_OFF);

                Vector512<float> r = vx.AsSingle() - Vector512.Create(1.0f);

                Vector512<float> r2 = r * r;
                Vector512<float> r4 = r2 * r2;
                Vector512<float> r8 = r4 * r4;

                Vector512<float> poly = (Vector512.Create(C9) * r + Vector512.Create(C8)) * r8
                                    + (((Vector512.Create(C7) * r + Vector512.Create(C6)) * r2
                                      + (Vector512.Create(C5) * r + Vector512.Create(C4))) * r4
                                     + ((Vector512.Create(C3) * r + Vector512.Create(C2)) * r2
                                      + (Vector512.Create(C1) * r + Vector512.Create(C0))));

                return Vector512.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n + poly
                );
            }
#endif
        }

        /// <summary>1f / (1f + MathF.Exp(-x))</summary>
        private readonly struct SigmoidOperator : IUnaryOperator
        {
            public static float Invoke(float x) => 1.0f / (1.0f + MathF.Exp(-x));
            public static Vector128<float> Invoke(Vector128<float> x) => Vector128.Create(1f) / (Vector128.Create(1f) + ExpOperator.Invoke(-x));
            public static Vector256<float> Invoke(Vector256<float> x) => Vector256.Create(1f) / (Vector256.Create(1f) + ExpOperator.Invoke(-x));
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => Vector512.Create(1f) / (Vector512.Create(1f) + ExpOperator.Invoke(-x));
#endif
        }

        /// <summary>Operator that takes one input value and returns a single value.</summary>
        private interface IUnaryOperator
        {
            static abstract float Invoke(float x);
            static abstract Vector128<float> Invoke(Vector128<float> x);
            static abstract Vector256<float> Invoke(Vector256<float> x);
#if NET8_0_OR_GREATER
            static abstract Vector512<float> Invoke(Vector512<float> x);
#endif
        }

        /// <summary>Operator that takes two input values and returns a single value.</summary>
        private interface IBinaryOperator
        {
            static abstract float Invoke(float x, float y);
            static abstract Vector128<float> Invoke(Vector128<float> x, Vector128<float> y);
            static abstract Vector256<float> Invoke(Vector256<float> x, Vector256<float> y);
#if NET8_0_OR_GREATER
            static abstract Vector512<float> Invoke(Vector512<float> x, Vector512<float> y);
#endif
        }

        /// <summary><see cref="IBinaryOperator"/> that specializes horizontal aggregation of all elements in a vector.</summary>
        private interface IAggregationOperator : IBinaryOperator
        {
            static abstract float Invoke(Vector128<float> x);
            static abstract float Invoke(Vector256<float> x);
#if NET8_0_OR_GREATER
            static abstract float Invoke(Vector512<float> x);
#endif

            static virtual float IdentityValue => throw new NotSupportedException();
        }

        /// <summary>Operator that takes three input values and returns a single value.</summary>
        private interface ITernaryOperator
        {
            static abstract float Invoke(float x, float y, float z);
            static abstract Vector128<float> Invoke(Vector128<float> x, Vector128<float> y, Vector128<float> z);
            static abstract Vector256<float> Invoke(Vector256<float> x, Vector256<float> y, Vector256<float> z);
#if NET8_0_OR_GREATER
            static abstract Vector512<float> Invoke(Vector512<float> x, Vector512<float> y, Vector512<float> z);
#endif
        }
    }
}
