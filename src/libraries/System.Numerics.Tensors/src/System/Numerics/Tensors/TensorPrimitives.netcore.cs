// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

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

                    Vector512<float> remainderMask = LoadRemainderMaskSingleVector512(x.Length - i);
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

                    Vector256<float> remainderMask = LoadRemainderMaskSingleVector256(x.Length - i);
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

                    Vector128<float> remainderMask = LoadRemainderMaskSingleVector128(x.Length - i);
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

        private static float Aggregate<TLoad, TAggregate>(
            ReadOnlySpan<float> x)
            where TLoad : struct, IUnaryOperator
            where TAggregate : struct, IAggregationOperator
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
                Vector512<float> result = TLoad.Invoke(Vector512.LoadUnsafe(ref xRef, 0));
                int oneVectorFromEnd = x.Length - Vector512<float>.Count;
                int i = Vector512<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregate.Invoke(result, TLoad.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i)));
                    i += Vector512<float>.Count;
                }

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregate.Invoke(result,
                        Vector512.ConditionalSelect(
                            Vector512.Equals(LoadRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                            Vector512.Create(TAggregate.IdentityValue),
                            TLoad.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregate.Invoke(result);
            }
#endif

            if (Vector256.IsHardwareAccelerated && x.Length >= Vector256<float>.Count)
            {
                // Load the first vector as the initial set of results
                Vector256<float> result = TLoad.Invoke(Vector256.LoadUnsafe(ref xRef, 0));
                int oneVectorFromEnd = x.Length - Vector256<float>.Count;
                int i = Vector256<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregate.Invoke(result, TLoad.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i)));
                    i += Vector256<float>.Count;
                }

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregate.Invoke(result,
                        Vector256.ConditionalSelect(
                            Vector256.Equals(LoadRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                            Vector256.Create(TAggregate.IdentityValue),
                            TLoad.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregate.Invoke(result);
            }

            if (Vector128.IsHardwareAccelerated && x.Length >= Vector128<float>.Count)
            {
                // Load the first vector as the initial set of results
                Vector128<float> result = TLoad.Invoke(Vector128.LoadUnsafe(ref xRef, 0));
                int oneVectorFromEnd = x.Length - Vector128<float>.Count;
                int i = Vector128<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregate.Invoke(result, TLoad.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i)));
                    i += Vector128<float>.Count;
                }

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregate.Invoke(result,
                        Vector128.ConditionalSelect(
                            Vector128.Equals(LoadRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                            Vector128.Create(TAggregate.IdentityValue),
                            TLoad.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregate.Invoke(result);
            }

            // Vectorization isn't supported or there are too few elements to vectorize.
            // Use a scalar implementation.
            {
                float result = TLoad.Invoke(x[0]);
                for (int i = 1; i < x.Length; i++)
                {
                    result = TAggregate.Invoke(result, TLoad.Invoke(x[i]));
                }

                return result;
            }
        }

        private static float Aggregate<TBinary, TAggregate>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y)
            where TBinary : struct, IBinaryOperator
            where TAggregate : struct, IAggregationOperator
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
                Vector512<float> result = TBinary.Invoke(Vector512.LoadUnsafe(ref xRef, 0), Vector512.LoadUnsafe(ref yRef, 0));
                int oneVectorFromEnd = x.Length - Vector512<float>.Count;
                int i = Vector512<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregate.Invoke(result, TBinary.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i), Vector512.LoadUnsafe(ref yRef, (uint)i)));
                    i += Vector512<float>.Count;
                }

                // Process the last vector in the spans, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregate.Invoke(result,
                        Vector512.ConditionalSelect(
                            Vector512.Equals(LoadRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                            Vector512.Create(TAggregate.IdentityValue),
                            TBinary.Invoke(
                                Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<float>.Count)),
                                Vector512.LoadUnsafe(ref yRef, (uint)(x.Length - Vector512<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregate.Invoke(result);
            }
#endif

            if (Vector256.IsHardwareAccelerated && x.Length >= Vector256<float>.Count)
            {
                // Load the first vector as the initial set of results
                Vector256<float> result = TBinary.Invoke(Vector256.LoadUnsafe(ref xRef, 0), Vector256.LoadUnsafe(ref yRef, 0));
                int oneVectorFromEnd = x.Length - Vector256<float>.Count;
                int i = Vector256<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregate.Invoke(result, TBinary.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i), Vector256.LoadUnsafe(ref yRef, (uint)i)));
                    i += Vector256<float>.Count;
                }

                // Process the last vector in the spans, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregate.Invoke(result,
                        Vector256.ConditionalSelect(
                            Vector256.Equals(LoadRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                            Vector256.Create(TAggregate.IdentityValue),
                            TBinary.Invoke(
                                Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<float>.Count)),
                                Vector256.LoadUnsafe(ref yRef, (uint)(x.Length - Vector256<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregate.Invoke(result);
            }

            if (Vector128.IsHardwareAccelerated && x.Length >= Vector128<float>.Count)
            {
                // Load the first vector as the initial set of results
                Vector128<float> result = TBinary.Invoke(Vector128.LoadUnsafe(ref xRef, 0), Vector128.LoadUnsafe(ref yRef, 0));
                int oneVectorFromEnd = x.Length - Vector128<float>.Count;
                int i = Vector128<float>.Count;

                // Aggregate additional vectors into the result as long as there's at
                // least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    result = TAggregate.Invoke(result, TBinary.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i), Vector128.LoadUnsafe(ref yRef, (uint)i)));
                    i += Vector128<float>.Count;
                }

                // Process the last vector in the spans, masking off elements already processed.
                if (i != x.Length)
                {
                    result = TAggregate.Invoke(result,
                        Vector128.ConditionalSelect(
                            Vector128.Equals(LoadRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                            Vector128.Create(TAggregate.IdentityValue),
                            TBinary.Invoke(
                                Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<float>.Count)),
                                Vector128.LoadUnsafe(ref yRef, (uint)(x.Length - Vector128<float>.Count)))));
                }

                // Aggregate the lanes in the vector back into the scalar result
                return TAggregate.Invoke(result);
            }

            // Vectorization isn't supported or there are too few elements to vectorize.
            // Use a scalar implementation.
            {
                float result = TBinary.Invoke(xRef, yRef);
                for (int i = 1; i < x.Length; i++)
                {
                    result = TAggregate.Invoke(result,
                        TBinary.Invoke(
                            Unsafe.Add(ref xRef, i),
                            Unsafe.Add(ref yRef, i)));
                }

                return result;
            }
        }

        /// <remarks>
        /// This is the same as <see cref="Aggregate{TLoad, TAggregate}(ReadOnlySpan{float})"/>,
        /// except it early exits on NaN.
        /// </remarks>
        private static float MinMaxCore<TMinMax>(ReadOnlySpan<float> x) where TMinMax : struct, IAggregationOperator
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

                    result = TMinMax.Invoke(result, current);
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
                        Vector512.Equals(LoadRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                        result,
                        TMinMax.Invoke(result, current));
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMax.Invoke(result);
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

                    result = TMinMax.Invoke(result, current);
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
                        Vector256.Equals(LoadRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                        result,
                        TMinMax.Invoke(result, current));
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMax.Invoke(result);
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

                    result = TMinMax.Invoke(result, current);
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
                        Vector128.Equals(LoadRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                        result,
                        TMinMax.Invoke(result, current));
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMax.Invoke(result);
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

                    result = TMinMax.Invoke(result, current);
                }

                return result;
            }
        }

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
                            Vector512.Equals(LoadRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
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
                            Vector256.Equals(LoadRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
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
                            Vector128.Equals(LoadRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
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
                            Vector512.Equals(LoadRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
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
                            Vector256.Equals(LoadRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
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
                            Vector128.Equals(LoadRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
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

        private static unsafe void InvokeSpanScalarIntoSpan<TBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination)
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
                        TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, (uint)i),
                                               yVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector512<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector512<float>.Count);
                        Vector512.ConditionalSelect(
                            Vector512.Equals(LoadRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
                            Vector512.LoadUnsafe(ref dRef, lastVectorIndex),
                            TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, lastVectorIndex),
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
                        TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, (uint)i),
                                               yVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector256<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector256<float>.Count);
                        Vector256.ConditionalSelect(
                            Vector256.Equals(LoadRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
                            Vector256.LoadUnsafe(ref dRef, lastVectorIndex),
                            TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, lastVectorIndex),
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
                        TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, (uint)i),
                                               yVec).StoreUnsafe(ref dRef, (uint)i);

                        i += Vector128<float>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final vector.
                    if (i != x.Length)
                    {
                        uint lastVectorIndex = (uint)(x.Length - Vector128<float>.Count);
                        Vector128.ConditionalSelect(
                            Vector128.Equals(LoadRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
                            Vector128.LoadUnsafe(ref dRef, lastVectorIndex),
                            TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, lastVectorIndex),
                                                   yVec)).StoreUnsafe(ref dRef, lastVectorIndex);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref dRef, i) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                 y);

                i++;
            }
        }

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
                            Vector512.Equals(LoadRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
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
                            Vector256.Equals(LoadRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
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
                            Vector128.Equals(LoadRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
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
                            Vector512.Equals(LoadRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
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
                            Vector256.Equals(LoadRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
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
                            Vector128.Equals(LoadRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
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
                            Vector512.Equals(LoadRemainderMaskSingleVector512(x.Length - i), Vector512<float>.Zero),
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
                            Vector256.Equals(LoadRemainderMaskSingleVector256(x.Length - i), Vector256<float>.Zero),
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
                            Vector128.Equals(LoadRemainderMaskSingleVector128(x.Length - i), Vector128<float>.Zero),
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float HorizontalAggregate<TAggregate>(Vector128<float> x) where TAggregate : struct, IBinaryOperator =>
            TAggregate.Invoke(
                TAggregate.Invoke(x[0], x[1]),
                TAggregate.Invoke(x[2], x[3]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float HorizontalAggregate<TAggregate>(Vector256<float> x) where TAggregate : struct, IBinaryOperator =>
            HorizontalAggregate<TAggregate>(TAggregate.Invoke(x.GetLower(), x.GetUpper()));

#if NET8_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float HorizontalAggregate<TAggregate>(Vector512<float> x) where TAggregate : struct, IBinaryOperator =>
            HorizontalAggregate<TAggregate>(TAggregate.Invoke(x.GetLower(), x.GetUpper()));
#endif

        private static bool IsNegative(float f) => float.IsNegative(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> IsNegative(Vector128<float> vector) =>
            Vector128.LessThan(vector.AsInt32(), Vector128<int>.Zero).AsSingle();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> IsNegative(Vector256<float> vector) =>
            Vector256.LessThan(vector.AsInt32(), Vector256<int>.Zero).AsSingle();

#if NET8_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<float> IsNegative(Vector512<float> vector) =>
            Vector512.LessThan(vector.AsInt32(), Vector512<int>.Zero).AsSingle();
#endif

        private static float GetFirstNaN(Vector128<float> vector) =>
            vector[BitOperations.TrailingZeroCount((~Vector128.Equals(vector, vector)).ExtractMostSignificantBits())];

        private static float GetFirstNaN(Vector256<float> vector) =>
            vector[BitOperations.TrailingZeroCount((~Vector256.Equals(vector, vector)).ExtractMostSignificantBits())];

#if NET8_0_OR_GREATER
        private static float GetFirstNaN(Vector512<float> vector) =>
            vector[BitOperations.TrailingZeroCount((~Vector512.Equals(vector, vector)).ExtractMostSignificantBits())];
#endif

        private static float Log2(float x) => MathF.Log2(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector128<float> LoadRemainderMaskSingleVector128(int validItems) =>
            Vector128.LoadUnsafe(
                ref Unsafe.As<uint, float>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x16)),
                (uint)((validItems * 16) + 12)); // last four floats in the row

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector256<float> LoadRemainderMaskSingleVector256(int validItems) =>
            Vector256.LoadUnsafe(
                ref Unsafe.As<uint, float>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x16)),
                (uint)((validItems * 16) + 8)); // last eight floats in the row

#if NET8_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector512<float> LoadRemainderMaskSingleVector512(int validItems) =>
            Vector512.LoadUnsafe(
                ref Unsafe.As<uint, float>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x16)),
                (uint)(validItems * 16)); // all sixteen floats in the row
#endif

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

        private readonly struct SubtractOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y) => x - y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x - y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x - y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x - y;
#endif
        }

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

        private readonly struct DivideOperator : IBinaryOperator
        {
            public static float Invoke(float x, float y) => x / y;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y) => x / y;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y) => x / y;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y) => x / y;
#endif
        }

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

        private readonly struct NegateOperator : IUnaryOperator
        {
            public static float Invoke(float x) => -x;
            public static Vector128<float> Invoke(Vector128<float> x) => -x;
            public static Vector256<float> Invoke(Vector256<float> x) => -x;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => -x;
#endif
        }

        private readonly struct AddMultiplyOperator : ITernaryOperator
        {
            public static float Invoke(float x, float y, float z) => (x + y) * z;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y, Vector128<float> z) => (x + y) * z;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y, Vector256<float> z) => (x + y) * z;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y, Vector512<float> z) => (x + y) * z;
#endif
        }

        private readonly struct MultiplyAddOperator : ITernaryOperator
        {
            public static float Invoke(float x, float y, float z) => (x * y) + z;
            public static Vector128<float> Invoke(Vector128<float> x, Vector128<float> y, Vector128<float> z) => (x * y) + z;
            public static Vector256<float> Invoke(Vector256<float> x, Vector256<float> y, Vector256<float> z) => (x * y) + z;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x, Vector512<float> y, Vector512<float> z) => (x * y) + z;
#endif
        }

        private readonly struct IdentityOperator : IUnaryOperator
        {
            public static float Invoke(float x) => x;
            public static Vector128<float> Invoke(Vector128<float> x) => x;
            public static Vector256<float> Invoke(Vector256<float> x) => x;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => x;
#endif
        }

        private readonly struct SquaredOperator : IUnaryOperator
        {
            public static float Invoke(float x) => x * x;
            public static Vector128<float> Invoke(Vector128<float> x) => x * x;
            public static Vector256<float> Invoke(Vector256<float> x) => x * x;
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => x * x;
#endif
        }

        private readonly struct AbsoluteOperator : IUnaryOperator
        {
            public static float Invoke(float x) => MathF.Abs(x);
            public static Vector128<float> Invoke(Vector128<float> x) => Vector128.Abs(x);
            public static Vector256<float> Invoke(Vector256<float> x) => Vector256.Abs(x);
#if NET8_0_OR_GREATER
            public static Vector512<float> Invoke(Vector512<float> x) => Vector512.Abs(x);
#endif
        }

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

        private interface IUnaryOperator
        {
            static abstract float Invoke(float x);
            static abstract Vector128<float> Invoke(Vector128<float> x);
            static abstract Vector256<float> Invoke(Vector256<float> x);
#if NET8_0_OR_GREATER
            static abstract Vector512<float> Invoke(Vector512<float> x);
#endif
        }

        private interface IBinaryOperator
        {
            static abstract float Invoke(float x, float y);
            static abstract Vector128<float> Invoke(Vector128<float> x, Vector128<float> y);
            static abstract Vector256<float> Invoke(Vector256<float> x, Vector256<float> y);
#if NET8_0_OR_GREATER
            static abstract Vector512<float> Invoke(Vector512<float> x, Vector512<float> y);
#endif
        }

        private interface IAggregationOperator : IBinaryOperator
        {
            static abstract float Invoke(Vector128<float> x);
            static abstract float Invoke(Vector256<float> x);
#if NET8_0_OR_GREATER
            static abstract float Invoke(Vector512<float> x);
#endif

            static virtual float IdentityValue => throw new NotSupportedException();
        }

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
