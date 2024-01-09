﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

// TODO:
// - Vectorize the trig-related functions for Ts other than floats
// - Vectorize integer operations when sizeof(T) == 1 or 2 (currently only vectorized in most operations for sizeof(T) == 4 or 8).
// - Implement generic version of IndexOfMinMaxCore and corresponding IndexOf methods.

namespace System.Numerics.Tensors
{
    public static unsafe partial class TensorPrimitives
    {
        /// <summary>Defines the threshold, in bytes, at which non-temporal stores will be used.</summary>
        /// <remarks>
        ///     A non-temporal store is one that allows the CPU to bypass the cache when writing to memory.
        ///
        ///     This can be beneficial when working with large amounts of memory where the writes would otherwise
        ///     cause large amounts of repeated updates and evictions. The hardware optimization manuals recommend
        ///     the threshold to be roughly half the size of the last level of on-die cache -- that is, if you have approximately
        ///     4MB of L3 cache per core, you'd want this to be approx. 1-2MB, depending on if hyperthreading was enabled.
        ///
        ///     However, actually computing the amount of L3 cache per core can be tricky or error prone. Native memcpy
        ///     algorithms use a constant threshold that is typically around 256KB and we match that here for simplicity. This
        ///     threshold accounts for most processors in the last 10-15 years that had approx. 1MB L3 per core and support
        ///     hyperthreading, giving a per core last level cache of approx. 512KB.
        /// </remarks>
        private const nuint NonTemporalByteThreshold = 256 * 1024;

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
        }

        /// <summary>Computes the cosine similarity between the two specified non-empty, equal-length tensors of single-precision floating-point numbers.</summary>
        /// <remarks>Assumes arguments have already been validated to be non-empty and equal length.</remarks>
        private static T CosineSimilarityCore<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y) where T : IRootFunctions<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            // Compute the same as:
            // TensorPrimitives.Dot(x, y) / (Math.Sqrt(TensorPrimitives.SumOfSquares(x)) * Math.Sqrt(TensorPrimitives.SumOfSquares(y)))
            // but only looping over each span once.

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref T yRef = ref MemoryMarshal.GetReference(y);

                Vector512<T> dotProductVector = Vector512<T>.Zero;
                Vector512<T> xSumOfSquaresVector = Vector512<T>.Zero;
                Vector512<T> ySumOfSquaresVector = Vector512<T>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector512<T>.Count;
                int i = 0;
                do
                {
                    Vector512<T> xVec = Vector512.LoadUnsafe(ref xRef, (uint)i);
                    Vector512<T> yVec = Vector512.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);

                    i += Vector512<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector512<T> xVec = Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<T>.Count));
                    Vector512<T> yVec = Vector512.LoadUnsafe(ref yRef, (uint)(x.Length - Vector512<T>.Count));

                    Vector512<T> remainderMask = CreateRemainderMaskVector512<T>(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector512.Sum(dotProductVector) /
                    (T.Sqrt(Vector512.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector512.Sum(ySumOfSquaresVector)));
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref T yRef = ref MemoryMarshal.GetReference(y);

                Vector256<T> dotProductVector = Vector256<T>.Zero;
                Vector256<T> xSumOfSquaresVector = Vector256<T>.Zero;
                Vector256<T> ySumOfSquaresVector = Vector256<T>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector256<T>.Count;
                int i = 0;
                do
                {
                    Vector256<T> xVec = Vector256.LoadUnsafe(ref xRef, (uint)i);
                    Vector256<T> yVec = Vector256.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);

                    i += Vector256<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector256<T> xVec = Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<T>.Count));
                    Vector256<T> yVec = Vector256.LoadUnsafe(ref yRef, (uint)(x.Length - Vector256<T>.Count));

                    Vector256<T> remainderMask = CreateRemainderMaskVector256<T>(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector256.Sum(dotProductVector) /
                    (T.Sqrt(Vector256.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector256.Sum(ySumOfSquaresVector)));
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref T yRef = ref MemoryMarshal.GetReference(y);

                Vector128<T> dotProductVector = Vector128<T>.Zero;
                Vector128<T> xSumOfSquaresVector = Vector128<T>.Zero;
                Vector128<T> ySumOfSquaresVector = Vector128<T>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector128<T>.Count;
                int i = 0;
                do
                {
                    Vector128<T> xVec = Vector128.LoadUnsafe(ref xRef, (uint)i);
                    Vector128<T> yVec = Vector128.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);

                    i += Vector128<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector128<T> xVec = Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<T>.Count));
                    Vector128<T> yVec = Vector128.LoadUnsafe(ref yRef, (uint)(x.Length - Vector128<T>.Count));

                    Vector128<T> remainderMask = CreateRemainderMaskVector128<T>(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = FusedMultiplyAdd(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = FusedMultiplyAdd(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = FusedMultiplyAdd(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector128.Sum(dotProductVector) /
                    (T.Sqrt(Vector128.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector128.Sum(ySumOfSquaresVector)));
            }

            // Vectorization isn't supported or there are too few elements to vectorize.
            // Use a scalar implementation.
            T dotProduct = T.Zero, xSumOfSquares = T.Zero, ySumOfSquares = T.Zero;
            for (int i = 0; i < x.Length; i++)
            {
                dotProduct = FusedMultiplyAdd(x[i], y[i], dotProduct);
                xSumOfSquares = FusedMultiplyAdd(x[i], x[i], xSumOfSquares);
                ySumOfSquares = FusedMultiplyAdd(y[i], y[i], ySumOfSquares);
            }

            // Sum(X * Y) / (|X| * |Y|)
            return
                dotProduct /
                (T.Sqrt(xSumOfSquares) * T.Sqrt(ySumOfSquares));
        }

        /// <summary>Performs an aggregation over all elements in <paramref name="x"/> to produce a single-precision floating-point value.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TTransformOperator">Specifies the transform operation that should be applied to each element loaded from <paramref name="x"/>.</typeparam>
        /// <typeparam name="TAggregationOperator">
        /// Specifies the aggregation binary operation that should be applied to multiple values to aggregate them into a single value.
        /// The aggregation is applied after the transform is applied to each element.
        /// </typeparam>
        private static T Aggregate<T, TTransformOperator, TAggregationOperator>(
            ReadOnlySpan<T> x)
            where TTransformOperator : struct, IUnaryOperator<T>
            where TAggregationOperator : struct, IAggregationOperator<T>
        {
            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TTransformOperator.Vectorizable && Unsafe.SizeOf<T>() >= 4)
            {
                T result;

                if (remainder >= (uint)Vector512<T>.Count)
                {
                    result = Vectorized512(ref xRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    result = VectorizedSmall(ref xRef, remainder);
                }

                return result;
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TTransformOperator.Vectorizable && Unsafe.SizeOf<T>() >= 4)
            {
                T result;

                if (remainder >= (uint)Vector256<T>.Count)
                {
                    result = Vectorized256(ref xRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    result = VectorizedSmall(ref xRef, remainder);
                }

                return result;
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TTransformOperator.Vectorizable && Unsafe.SizeOf<T>() >= 4)
            {
                T result;

                if (remainder >= (uint)Vector128<T>.Count)
                {
                    result = Vectorized128(ref xRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    result = VectorizedSmall(ref xRef, remainder);
                }

                return result;
            }

            // This is the software fallback when no acceleration is available.
            // It requires no branches to hit.

            return SoftwareFallback(ref xRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T SoftwareFallback(ref T xRef, nuint length)
            {
                T result = TAggregationOperator.IdentityValue;

                for (nuint i = 0; i < length; i++)
                {
                    result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, i)));
                }

                return result;
            }

            static T Vectorized128(ref T xRef, nuint remainder)
            {
                Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                Vector128<T> end = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    {
                        T* xPtr = px;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector128<T>) - ((nuint)xPtr % (uint)sizeof(Vector128<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector128<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<T> vector1;
                        Vector128<T> vector2;
                        Vector128<T> vector3;
                        Vector128<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector128<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)));
                            vector2 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)));
                            vector3 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)));
                            vector4 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)));
                            vector2 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)));
                            vector3 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)));
                            vector4 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector128<T>.Count * 8);

                            remainder -= (uint)(Vector128<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector128.ConditionalSelect(CreateAlignmentMaskVector128<T>((int)misalignment), beg, Vector128.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector128<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                    {
                        Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 1;
                    }

                    case 1:
                    {
                        Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 1)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)trailing), end, Vector128.Create(TAggregationOperator.IdentityValue));
                        vresult = TAggregationOperator.Invoke(vresult, end);
                        break;
                    }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            static T Vectorized256(ref T xRef, nuint remainder)
            {
                Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                Vector256<T> end = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    {
                        T* xPtr = px;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)xPtr % (uint)sizeof(Vector256<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector256<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<T> vector1;
                        Vector256<T> vector2;
                        Vector256<T> vector3;
                        Vector256<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector256<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)));
                            vector2 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)));
                            vector3 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)));
                            vector4 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)));
                            vector2 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)));
                            vector3 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)));
                            vector4 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector256<T>.Count * 8);

                            remainder -= (uint)(Vector256<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector256.ConditionalSelect(CreateAlignmentMaskVector256<T>((int)misalignment), beg, Vector256.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector256<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                    {
                        Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 1;
                    }

                    case 1:
                    {
                        Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 1)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)trailing), end, Vector256.Create(TAggregationOperator.IdentityValue));
                        vresult = TAggregationOperator.Invoke(vresult, end);
                        break;
                    }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            static T Vectorized512(ref T xRef, nuint remainder)
            {
                Vector512<T> vresult = Vector512.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> beg = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef));
                Vector512<T> end = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    {
                        T* xPtr = px;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector512<T>) - ((nuint)xPtr % (uint)sizeof(Vector512<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector512<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<T> vector1;
                        Vector512<T> vector2;
                        Vector512<T> vector3;
                        Vector512<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector512<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)));
                            vector2 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)));
                            vector3 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)));
                            vector4 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)));
                            vector2 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)));
                            vector3 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)));
                            vector4 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector512<T>.Count * 8);

                            remainder -= (uint)(Vector512<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector512.ConditionalSelect(CreateAlignmentMaskVector512<T>((int)misalignment), beg, Vector512.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector512<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                    {
                        Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 1;
                    }

                    case 1:
                    {
                        Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 1)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end = Vector512.ConditionalSelect(CreateRemainderMaskVector512<T>((int)trailing), end, Vector512.Create(TAggregationOperator.IdentityValue));
                        vresult = TAggregationOperator.Invoke(vresult, end);
                        break;
                    }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall(ref T xRef, nuint remainder)
            {
                if (sizeof(T) == 4)
                {
                    return VectorizedSmall4(ref xRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    return VectorizedSmall8(ref xRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall4(ref T xRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 4);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);
                        Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                        Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                        Vector256<T> end = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                        end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                        vresult = TAggregationOperator.Invoke(vresult, beg);
                        vresult = TAggregationOperator.Invoke(vresult, end);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 8:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);
                        Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                        Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                        vresult = TAggregationOperator.Invoke(vresult, beg);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);
                        Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                        Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                        Vector128<T> end = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                        end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                        vresult = TAggregationOperator.Invoke(vresult, beg);
                        vresult = TAggregationOperator.Invoke(vresult, end);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);
                        Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                        Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                        vresult = TAggregationOperator.Invoke(vresult, beg);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 3:
                    {
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 2)));
                        goto case 2;
                    }

                    case 2:
                    {
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 1)));
                        goto case 1;
                    }

                    case 1:
                    {
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(xRef));
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall8(ref T xRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);
                        Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                        Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                        Vector256<T> end = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                        end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                        vresult = TAggregationOperator.Invoke(vresult, beg);
                        vresult = TAggregationOperator.Invoke(vresult, end);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);
                        Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                        Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                        vresult = TAggregationOperator.Invoke(vresult, beg);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 3:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);
                        Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                        Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                        Vector128<T> end = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                        end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                        vresult = TAggregationOperator.Invoke(vresult, beg);
                        vresult = TAggregationOperator.Invoke(vresult, end);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 2:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);
                        Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                        Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                        vresult = TAggregationOperator.Invoke(vresult, beg);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 1:
                    {
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(xRef));
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }

                return result;
            }
        }

        /// <summary>Performs an aggregation over all pair-wise elements in <paramref name="x"/> and <paramref name="y"/> to produce a single-precision floating-point value.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TBinaryOperator">Specifies the binary operation that should be applied to the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>.</typeparam>
        /// <typeparam name="TAggregationOperator">
        /// Specifies the aggregation binary operation that should be applied to multiple values to aggregate them into a single value.
        /// The aggregation is applied to the results of the binary operations on the pair-wise values.
        /// </typeparam>
        private static T Aggregate<T, TBinaryOperator, TAggregationOperator>(
            ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where TBinaryOperator : struct, IBinaryOperator<T>
            where TAggregationOperator : struct, IAggregationOperator<T>
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T yRef = ref MemoryMarshal.GetReference(y);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                T result;

                if (remainder >= (uint)Vector512<T>.Count)
                {
                    result = Vectorized512(ref xRef, ref yRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    result = VectorizedSmall(ref xRef, ref yRef, remainder);
                }

                return result;
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                T result;

                if (remainder >= (uint)Vector256<T>.Count)
                {
                    result = Vectorized256(ref xRef, ref yRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    result = VectorizedSmall(ref xRef, ref yRef, remainder);
                }

                return result;
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                T result;

                if (remainder >= (uint)Vector128<T>.Count)
                {
                    result = Vectorized128(ref xRef, ref yRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    result = VectorizedSmall(ref xRef, ref yRef, remainder);
                }

                return result;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            return SoftwareFallback(ref xRef, ref yRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T SoftwareFallback(ref T xRef, ref T yRef, nuint length)
            {
                T result = TAggregationOperator.IdentityValue;

                for (nuint i = 0; i < length; i++)
                {
                    result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                                        Unsafe.Add(ref yRef, i)));
                }

                return result;
            }

            static T Vectorized128(ref T xRef, ref T yRef, nuint remainder)
            {
                Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                          Vector128.LoadUnsafe(ref yRef));
                Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector128<T>) - ((nuint)xPtr % (uint)sizeof(Vector128<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector128<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<T> vector1;
                        Vector128<T> vector2;
                        Vector128<T> vector3;
                        Vector128<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector128<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)));
                            vector2 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)));
                            vector3 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)));
                            vector4 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)));
                            vector2 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)));
                            vector3 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)));
                            vector4 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector128<T>.Count * 8);
                            yPtr += (uint)(Vector128<T>.Count * 8);

                            remainder -= (uint)(Vector128<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector128.ConditionalSelect(CreateAlignmentMaskVector128<T>((int)misalignment), beg, Vector128.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector128<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 7)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 6)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 5)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 4)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 3)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 2)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 1;
                    }

                    case 1:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 1)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 1)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)trailing), end, Vector128.Create(TAggregationOperator.IdentityValue));
                        vresult = TAggregationOperator.Invoke(vresult, end);
                        break;
                    }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            static T Vectorized256(ref T xRef, ref T yRef, nuint remainder)
            {
                Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                          Vector256.LoadUnsafe(ref yRef));
                Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)xPtr % (uint)sizeof(Vector256<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector256<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<T> vector1;
                        Vector256<T> vector2;
                        Vector256<T> vector3;
                        Vector256<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector256<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)));
                            vector2 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)));
                            vector3 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)));
                            vector4 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)));
                            vector2 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)));
                            vector3 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)));
                            vector4 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector256<T>.Count * 8);
                            yPtr += (uint)(Vector256<T>.Count * 8);

                            remainder -= (uint)(Vector256<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector256.ConditionalSelect(CreateAlignmentMaskVector256<T>((int)misalignment), beg, Vector256.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector256<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 7)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 6)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 5)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 4)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 3)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 2)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 1;
                    }

                    case 1:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 1)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 1)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)trailing), end, Vector256.Create(TAggregationOperator.IdentityValue));
                        vresult = TAggregationOperator.Invoke(vresult, end);
                        break;
                    }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            static T Vectorized512(ref T xRef, ref T yRef, nuint remainder)
            {
                Vector512<T> vresult = Vector512.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> beg = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef),
                                                          Vector512.LoadUnsafe(ref yRef));
                Vector512<T> end = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count),
                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)Vector512<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector512<T>) - ((nuint)xPtr % (uint)sizeof(Vector512<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector512<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<T> vector1;
                        Vector512<T> vector2;
                        Vector512<T> vector3;
                        Vector512<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector512<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)));
                            vector2 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)));
                            vector3 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)));
                            vector4 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)));
                            vector2 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)));
                            vector3 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)));
                            vector4 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector512<T>.Count * 8);
                            yPtr += (uint)(Vector512<T>.Count * 8);

                            remainder -= (uint)(Vector512<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector512.ConditionalSelect(CreateAlignmentMaskVector512<T>((int)misalignment), beg, Vector512.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector512<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 7)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 6)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 5)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 4)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 3)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 2)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 1;
                    }

                    case 1:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 1)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 1)));
                        vresult = TAggregationOperator.Invoke(vresult, vector);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end = Vector512.ConditionalSelect(CreateRemainderMaskVector512<T>((int)trailing), end, Vector512.Create(TAggregationOperator.IdentityValue));
                        vresult = TAggregationOperator.Invoke(vresult, end);
                        break;
                    }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall(ref T xRef, ref T yRef, nuint remainder)
            {
                if (sizeof(T) == 4)
                {
                    return VectorizedSmall4(ref xRef, ref yRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    return VectorizedSmall8(ref xRef, ref yRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall4(ref T xRef, ref T yRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 4);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);
                        Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                        Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                  Vector256.LoadUnsafe(ref yRef));
                        Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                  Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                        end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                        vresult = TAggregationOperator.Invoke(vresult, beg);
                        vresult = TAggregationOperator.Invoke(vresult, end);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 8:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);
                        Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                        Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                  Vector256.LoadUnsafe(ref yRef));
                        vresult = TAggregationOperator.Invoke(vresult, beg);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);
                        Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                        Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                  Vector128.LoadUnsafe(ref yRef));
                        Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                  Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                        end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                        vresult = TAggregationOperator.Invoke(vresult, beg);
                        vresult = TAggregationOperator.Invoke(vresult, end);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);
                        Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                        Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                  Vector128.LoadUnsafe(ref yRef));
                        vresult = TAggregationOperator.Invoke(vresult, beg);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 3:
                    {
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                                            Unsafe.Add(ref yRef, 2)));
                        goto case 2;
                    }

                    case 2:
                    {
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                                            Unsafe.Add(ref yRef, 1)));
                        goto case 1;
                    }

                    case 1:
                    {
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(xRef, yRef));
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall8(ref T xRef, ref T yRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);
                        Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                        Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                  Vector256.LoadUnsafe(ref yRef));
                        Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                  Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                        end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                        vresult = TAggregationOperator.Invoke(vresult, beg);
                        vresult = TAggregationOperator.Invoke(vresult, end);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);
                        Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                        Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                  Vector256.LoadUnsafe(ref yRef));
                        vresult = TAggregationOperator.Invoke(vresult, beg);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 3:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);
                        Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                        Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                  Vector128.LoadUnsafe(ref yRef));
                        Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                  Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                        end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                        vresult = TAggregationOperator.Invoke(vresult, beg);
                        vresult = TAggregationOperator.Invoke(vresult, end);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 2:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);
                        Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                        Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                  Vector128.LoadUnsafe(ref yRef));
                        vresult = TAggregationOperator.Invoke(vresult, beg);

                        result = TAggregationOperator.Invoke(vresult);
                        break;
                    }

                    case 1:
                    {
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(xRef, yRef));
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }

                return result;
            }
        }

        /// <remarks>
        /// This is the same as <see cref="Aggregate{T, TTransformOperator, TAggregationOperator}(ReadOnlySpan{T})"/>
        /// with an identity transform, except it early exits on NaN.
        /// </remarks>
        private static T MinMaxCore<T, TMinMaxOperator>(ReadOnlySpan<T> x)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            // This matches the IEEE 754:2019 `maximum`/`minimum` functions.
            // It propagates NaN inputs back to the caller and
            // otherwise returns the greater of the inputs.
            // It treats +0 as greater than -0 as per the specification.

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector512<T> result = Vector512.LoadUnsafe(ref xRef, 0);
                Vector512<T> current;

                Vector512<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    // Check for NaNs
                    nanMask = ~Vector512.Equals(result, result);
                    if (nanMask != Vector512<T>.Zero)
                    {
                        return result.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                int oneVectorFromEnd = x.Length - Vector512<T>.Count;
                int i = Vector512<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector512.LoadUnsafe(ref xRef, (uint)i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector512.Equals(current, current);
                        if (nanMask != Vector512<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector512<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<T>.Count));

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector512.Equals(current, current);
                        if (nanMask != Vector512<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector256<T> result = Vector256.LoadUnsafe(ref xRef, 0);
                Vector256<T> current;

                Vector256<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    // Check for NaNs
                    nanMask = ~Vector256.Equals(result, result);
                    if (nanMask != Vector256<T>.Zero)
                    {
                        return result.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                int oneVectorFromEnd = x.Length - Vector256<T>.Count;
                int i = Vector256<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector256.LoadUnsafe(ref xRef, (uint)i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector256.Equals(current, current);
                        if (nanMask != Vector256<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector256<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<T>.Count));


                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector256.Equals(current, current);
                        if (nanMask != Vector256<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector128<T> result = Vector128.LoadUnsafe(ref xRef, 0);
                Vector128<T> current;

                Vector128<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    // Check for NaNs
                    nanMask = ~Vector128.Equals(result, result);
                    if (nanMask != Vector128<T>.Zero)
                    {
                        return result.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                int oneVectorFromEnd = x.Length - Vector128<T>.Count;
                int i = Vector128<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector128.LoadUnsafe(ref xRef, (uint)i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector128.Equals(current, current);
                        if (nanMask != Vector128<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector128<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<T>.Count));

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector128.Equals(current, current);
                        if (nanMask != Vector128<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }

            // Scalar path used when either vectorization is not supported or the input is too small to vectorize.
            T curResult = x[0];
            if (T.IsNaN(curResult))
            {
                return curResult;
            }

            for (int i = 1; i < x.Length; i++)
            {
                T current = x[i];
                if (T.IsNaN(current))
                {
                    return current;
                }

                curResult = TMinMaxOperator.Invoke(curResult, current);
            }

            return curResult;
        }

        private static int IndexOfMinMaxCore<TIndexOfMinMax>(ReadOnlySpan<float> x)
            where TIndexOfMinMax : struct, IIndexOfOperator
        {
            if (x.IsEmpty)
            {
                return -1;
            }

            // This matches the IEEE 754:2019 `maximum`/`minimum` functions.
            // It propagates NaN inputs back to the caller and
            // otherwise returns the index of the greater of the inputs.
            // It treats +0 as greater than -0 as per the specification.

            if (Vector512.IsHardwareAccelerated && x.Length >= Vector512<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                Vector512<int> resultIndex = Vector512.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
                Vector512<int> curIndex = resultIndex;
                Vector512<int> increment = Vector512.Create(Vector512<float>.Count);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector512<float> result = Vector512.LoadUnsafe(ref xRef);
                Vector512<float> current;

                Vector512<float> nanMask = ~Vector512.Equals(result, result);
                if (nanMask != Vector512<float>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }

                int oneVectorFromEnd = x.Length - Vector512<float>.Count;
                int i = Vector512<float>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector512.LoadUnsafe(ref xRef, (uint)i);
                    curIndex += increment;

                    nanMask = ~Vector512.Equals(current, current);
                    if (nanMask != Vector512<float>.Zero)
                    {
                        return i + IndexOfFirstMatch(nanMask);
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, curIndex);

                    i += Vector512<float>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<float>.Count));
                    curIndex += Vector512.Create(x.Length - i);

                    nanMask = ~Vector512.Equals(current, current);
                    if (nanMask != Vector512<float>.Zero)
                    {
                        return curIndex[IndexOfFirstMatch(nanMask)];
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, curIndex);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TIndexOfMinMax.Invoke(result, resultIndex);
            }

            if (Vector256.IsHardwareAccelerated && x.Length >= Vector256<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                Vector256<int> resultIndex = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7);
                Vector256<int> curIndex = resultIndex;
                Vector256<int> increment = Vector256.Create(Vector256<float>.Count);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector256<float> result = Vector256.LoadUnsafe(ref xRef);
                Vector256<float> current;

                Vector256<float> nanMask = ~Vector256.Equals(result, result);
                if (nanMask != Vector256<float>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }

                int oneVectorFromEnd = x.Length - Vector256<float>.Count;
                int i = Vector256<float>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector256.LoadUnsafe(ref xRef, (uint)i);
                    curIndex += increment;

                    nanMask = ~Vector256.Equals(current, current);
                    if (nanMask != Vector256<float>.Zero)
                    {
                        return i + IndexOfFirstMatch(nanMask);
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, curIndex);

                    i += Vector256<float>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<float>.Count));
                    curIndex += Vector256.Create(x.Length - i);

                    nanMask = ~Vector256.Equals(current, current);
                    if (nanMask != Vector256<float>.Zero)
                    {
                        return curIndex[IndexOfFirstMatch(nanMask)];
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, curIndex);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TIndexOfMinMax.Invoke(result, resultIndex);
            }

            if (Vector128.IsHardwareAccelerated && x.Length >= Vector128<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                Vector128<int> resultIndex = Vector128.Create(0, 1, 2, 3);
                Vector128<int> curIndex = resultIndex;
                Vector128<int> increment = Vector128.Create(Vector128<float>.Count);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector128<float> result = Vector128.LoadUnsafe(ref xRef);
                Vector128<float> current;

                Vector128<float> nanMask = ~Vector128.Equals(result, result);
                if (nanMask != Vector128<float>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }

                int oneVectorFromEnd = x.Length - Vector128<float>.Count;
                int i = Vector128<float>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector128.LoadUnsafe(ref xRef, (uint)i);
                    curIndex += increment;

                    nanMask = ~Vector128.Equals(current, current);
                    if (nanMask != Vector128<float>.Zero)
                    {
                        return i + IndexOfFirstMatch(nanMask);
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, curIndex);

                    i += Vector128<float>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    curIndex += Vector128.Create(x.Length - i);

                    current = Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<float>.Count));

                    nanMask = ~Vector128.Equals(current, current);
                    if (nanMask != Vector128<float>.Zero)
                    {
                        return curIndex[IndexOfFirstMatch(nanMask)];
                    }

                    TIndexOfMinMax.Invoke(ref result, current, ref resultIndex, curIndex);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TIndexOfMinMax.Invoke(result, resultIndex);
            }

            // Scalar path used when either vectorization is not supported or the input is too small to vectorize.
            float curResult = x[0];
            int curIn = 0;
            if (float.IsNaN(curResult))
            {
                return curIn;
            }

            for (int i = 1; i < x.Length; i++)
            {
                float current = x[i];
                if (float.IsNaN(current))
                {
                    return i;
                }

                curIn = TIndexOfMinMax.Invoke(ref curResult, current, curIn, i);
            }

            return curIn;
        }

        private static int IndexOfFirstMatch<T>(Vector128<T> mask)
        {
            return BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());
        }

        private static int IndexOfFirstMatch<T>(Vector256<T> mask)
        {
            return BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());
        }

        private static int IndexOfFirstMatch<T>(Vector512<T> mask)
        {
            return BitOperations.TrailingZeroCount(mask.ExtractMostSignificantBits());
        }

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination"/>.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static void InvokeSpanIntoSpan<T, TUnaryOperator>(
            ReadOnlySpan<T> x, Span<T> destination)
            where TUnaryOperator : struct, IUnaryOperator<T>
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TUnaryOperator.Vectorizable && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref dRef, remainder);
                }

                return;
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TUnaryOperator.Vectorizable && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref dRef, remainder);
                }

                return;
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TUnaryOperator.Vectorizable && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref T xRef, ref T dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, i));
                }
            }

            static void Vectorized128(ref T xRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                Vector128<T> end = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector128<T>) - ((nuint)dPtr % (uint)sizeof(Vector128<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector128<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<T> vector1;
                        Vector128<T> vector2;
                        Vector128<T> vector3;
                        Vector128<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector128<T>.Count - 1)) & (nuint)(-Vector128<T>.Count);

                switch (remainder / (uint)Vector128<T>.Count)
                {
                    case 8:
                    {
                        Vector128<T> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector128<T> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector128<T> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector128<T> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector128<T> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector128<T> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector128<T> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector128<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized256(ref T xRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                Vector256<T> end = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)dPtr % (uint)sizeof(Vector256<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector256<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<T> vector1;
                        Vector256<T> vector2;
                        Vector256<T> vector3;
                        Vector256<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector256<T>.Count - 1)) & (nuint)(-Vector256<T>.Count);

                switch (remainder / (uint)Vector256<T>.Count)
                {
                    case 8:
                    {
                        Vector256<T> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector256<T> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector256<T> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector256<T> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector256<T> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector256<T> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector256<T> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector256<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized512(ref T xRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> beg = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef));
                Vector512<T> end = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count));

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector512<T>) - ((nuint)dPtr % (uint)sizeof(Vector512<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector512<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<T> vector1;
                        Vector512<T> vector2;
                        Vector512<T> vector3;
                        Vector512<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector512<T>.Count - 1)) & (nuint)(-Vector512<T>.Count);

                switch (remainder / (uint)Vector512<T>.Count)
                {
                    case 8:
                    {
                        Vector512<T> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector512<T> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector512<T> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector512<T> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector512<T> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector512<T> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector512<T> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector512<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref T xRef, ref T dRef, nuint remainder)
            {
                if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 4);

                switch (remainder)
                {
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                        Vector256<T> end = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 8:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                        Vector128<T> end = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 2));
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 1));
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = TUnaryOperator.Invoke(xRef);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                        Vector256<T> end = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                        Vector128<T> end = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 2:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 1:
                    {
                        dRef = TUnaryOperator.Invoke(xRef);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TBinaryOperator{T}">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>.
        /// </typeparam>
        private static void InvokeSpanSpanIntoSpan<T, TBinaryOperator>(
            ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where TBinaryOperator : struct, IBinaryOperator<T>
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

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T yRef = ref MemoryMarshal.GetReference(y);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, ref yRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, ref dRef, remainder);
                }

                return;
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, ref yRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, ref dRef, remainder);
                }

                return;
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, ref yRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, ref yRef, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref T xRef, ref T yRef, ref T dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                     Unsafe.Add(ref yRef, i));
                }
            }

            static void Vectorized128(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                          Vector128.LoadUnsafe(ref yRef));
                Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector128<T>) - ((nuint)dPtr % (uint)sizeof(Vector128<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector128<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<T> vector1;
                        Vector128<T> vector2;
                        Vector128<T> vector3;
                        Vector128<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector128<T>.Count - 1)) & (nuint)(-Vector128<T>.Count);

                switch (remainder / (uint)Vector128<T>.Count)
                {
                    case 8:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                     Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector128<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized256(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                          Vector256.LoadUnsafe(ref yRef));
                Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)dPtr % (uint)sizeof(Vector256<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector256<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<T> vector1;
                        Vector256<T> vector2;
                        Vector256<T> vector3;
                        Vector256<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector256<T>.Count - 1)) & (nuint)(-Vector256<T>.Count);

                switch (remainder / (uint)Vector256<T>.Count)
                {
                    case 8:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                     Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector256<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized512(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> beg = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef),
                                                          Vector512.LoadUnsafe(ref yRef));
                Vector512<T> end = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count),
                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)Vector512<T>.Count));

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector512<T>) - ((nuint)dPtr % (uint)sizeof(Vector512<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector512<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<T> vector1;
                        Vector512<T> vector2;
                        Vector512<T> vector3;
                        Vector512<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector512<T>.Count - 1)) & (nuint)(-Vector512<T>.Count);

                switch (remainder / (uint)Vector512<T>.Count)
                {
                    case 8:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                     Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector512<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
            {
                if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, ref yRef, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, ref yRef, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 4);

                switch (remainder)
                {
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                  Vector256.LoadUnsafe(ref yRef));
                        Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                  Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 8:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                  Vector256.LoadUnsafe(ref yRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                  Vector128.LoadUnsafe(ref yRef));
                        Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                  Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                  Vector128.LoadUnsafe(ref yRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                         Unsafe.Add(ref yRef, 2));
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                         Unsafe.Add(ref yRef, 1));
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = TBinaryOperator.Invoke(xRef, yRef);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                  Vector256.LoadUnsafe(ref yRef));
                        Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                  Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                  Vector256.LoadUnsafe(ref yRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                  Vector128.LoadUnsafe(ref yRef));
                        Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                  Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 2:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                  Vector128.LoadUnsafe(ref yRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 1:
                    {
                        dRef = TBinaryOperator.Invoke(xRef, yRef);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on each element loaded from <paramref name="x"/> with <paramref name="y"/>.
        /// </typeparam>
        private static void InvokeSpanScalarIntoSpan<T, TBinaryOperator>(
            ReadOnlySpan<T> x, T y, Span<T> destination)
            where TBinaryOperator : struct, IBinaryOperator<T> =>
            InvokeSpanScalarIntoSpan<T, IdentityOperator<T>, TBinaryOperator>(x, y, destination);

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TTransformOperator">
        /// Specifies the operation to perform on each element loaded from <paramref name="x"/>.
        /// It is not used with <paramref name="y"/>.
        /// </typeparam>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on the transformed value from <paramref name="x"/> with <paramref name="y"/>.
        /// </typeparam>
        private static void InvokeSpanScalarIntoSpan<T, TTransformOperator, TBinaryOperator>(
            ReadOnlySpan<T> x, T y, Span<T> destination)
            where TTransformOperator : struct, IUnaryOperator<T>
            where TBinaryOperator : struct, IBinaryOperator<T>
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TTransformOperator.Vectorizable && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, y, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, y, ref dRef, remainder);
                }

                return;
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TTransformOperator.Vectorizable && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, y, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, y, ref dRef, remainder);
                }

                return;
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TTransformOperator.Vectorizable && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, y, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, y, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, y, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref T xRef, T y, ref T dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, i)),
                                                                     y);
                }
            }

            static void Vectorized128(ref T xRef, T y, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> yVec = Vector128.Create(y);

                Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                          yVec);
                Vector128<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count)),
                                                          yVec);

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector128<T>) - ((nuint)dPtr % (uint)sizeof(Vector128<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector128<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<T> vector1;
                        Vector128<T> vector2;
                        Vector128<T> vector3;
                        Vector128<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3))),
                                                                 yVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7))),
                                                                 yVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3))),
                                                                 yVec);

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7))),
                                                                 yVec);

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector128<T>.Count - 1)) & (nuint)(-Vector128<T>.Count);

                switch (remainder / (uint)Vector128<T>.Count)
                {
                    case 8:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector128<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized256(ref T xRef, T y, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> yVec = Vector256.Create(y);

                Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                          yVec);
                Vector256<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count)),
                                                          yVec);

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)dPtr % (uint)sizeof(Vector256<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector256<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<T> vector1;
                        Vector256<T> vector2;
                        Vector256<T> vector3;
                        Vector256<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3))),
                                                                 yVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7))),
                                                                 yVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3))),
                                                                 yVec);

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7))),
                                                                 yVec);

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector256<T>.Count - 1)) & (nuint)(-Vector256<T>.Count);

                switch (remainder / (uint)Vector256<T>.Count)
                {
                    case 8:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector256<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized512(ref T xRef, T y, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> yVec = Vector512.Create(y);

                Vector512<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef)),
                                                          yVec);
                Vector512<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count)),
                                                          yVec);

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector512<T>) - ((nuint)dPtr % (uint)sizeof(Vector512<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector512<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<T> vector1;
                        Vector512<T> vector2;
                        Vector512<T> vector3;
                        Vector512<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3))),
                                                                 yVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7))),
                                                                 yVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3))),
                                                                 yVec);

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7))),
                                                                 yVec);

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector512<T>.Count - 1)) & (nuint)(-Vector512<T>.Count);

                switch (remainder / (uint)Vector512<T>.Count)
                {
                    case 8:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2))),
                                                                     yVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector512<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref T xRef, T y, ref T dRef, nuint remainder)
            {
                if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, y, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, y, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, T y, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 4);

                switch (remainder)
                {
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> yVec = Vector256.Create(y);

                        Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                  yVec);
                        Vector256<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count)),
                                                                  yVec);

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 8:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                  Vector256.Create(y));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> yVec = Vector128.Create(y);

                        Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                            yVec);
                        Vector128<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count)),
                                                                                            yVec);

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                            Vector128.Create(y));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 2)),
                                                                         y);
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 1)),
                                                                         y);
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = TBinaryOperator.Invoke(TTransformOperator.Invoke(xRef), y);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, T y, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> yVec = Vector256.Create(y);

                        Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                  yVec);
                        Vector256<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count)),
                                                                  yVec);

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                  Vector256.Create(y));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> yVec = Vector128.Create(y);

                        Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                            yVec);
                        Vector128<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count)),
                                                                                            yVec);

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 2:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                            Vector128.Create(y));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 1:
                    {
                        dRef = TBinaryOperator.Invoke(TTransformOperator.Invoke(xRef), y);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/>, <paramref name="y"/>,
        /// and <paramref name="z"/>.
        /// </typeparam>
        private static void InvokeSpanSpanSpanIntoSpan<T, TTernaryOperator>(
            ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> z, Span<T> destination)
            where TTernaryOperator : struct, ITernaryOperator<T>
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

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T yRef = ref MemoryMarshal.GetReference(y);
            ref T zRef = ref MemoryMarshal.GetReference(z);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }

                return;
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }

                return;
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, ref yRef, ref zRef, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                      Unsafe.Add(ref yRef, i),
                                                                      Unsafe.Add(ref zRef, i));
                }
            }

            static void Vectorized128(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                           Vector128.LoadUnsafe(ref yRef),
                                                           Vector128.LoadUnsafe(ref zRef));
                Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                           Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                           Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* zPtr = pz;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector128<T>) - ((nuint)dPtr % (uint)sizeof(Vector128<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            zPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector128<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<T> vector1;
                        Vector128<T> vector2;
                        Vector128<T> vector3;
                        Vector128<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                zPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                zPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        zRef = ref *zPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector128<T>.Count - 1)) & (nuint)(-Vector128<T>.Count);

                switch (remainder / (uint)Vector128<T>.Count)
                {
                    case 8:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                      Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                      Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                      Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                      Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                      Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                      Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                      Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector128<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized256(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                           Vector256.LoadUnsafe(ref yRef),
                                                           Vector256.LoadUnsafe(ref zRef));
                Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                           Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                           Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* zPtr = pz;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)dPtr % (uint)sizeof(Vector256<T>))) / (nuint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            zPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector256<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<T> vector1;
                        Vector256<T> vector2;
                        Vector256<T> vector3;
                        Vector256<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                zPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                zPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        zRef = ref *zPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector256<T>.Count - 1)) & (nuint)(-Vector256<T>.Count);

                switch (remainder / (uint)Vector256<T>.Count)
                {
                    case 8:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                      Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                      Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                      Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                      Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                      Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                      Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                      Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector256<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized512(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> beg = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef),
                                                           Vector512.LoadUnsafe(ref yRef),
                                                           Vector512.LoadUnsafe(ref zRef));
                Vector512<T> end = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count),
                                                           Vector512.LoadUnsafe(ref yRef, remainder - (uint)Vector512<T>.Count),
                                                           Vector512.LoadUnsafe(ref zRef, remainder - (uint)Vector512<T>.Count));

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* zPtr = pz;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector512<T>) - ((nuint)dPtr % (uint)sizeof(Vector512<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            zPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector512<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<T> vector1;
                        Vector512<T> vector2;
                        Vector512<T> vector3;
                        Vector512<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                zPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                zPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        zRef = ref *zPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector512<T>.Count - 1)) & (nuint)(-Vector512<T>.Count);

                switch (remainder / (uint)Vector512<T>.Count)
                {
                    case 8:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                      Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                      Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                      Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                      Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                      Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                      Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                      Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector512<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
            {
                if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 4);

                switch (remainder)
                {
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                   Vector256.LoadUnsafe(ref yRef),
                                                                   Vector256.LoadUnsafe(ref zRef));
                        Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                   Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                   Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 8:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.LoadUnsafe(ref zRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.LoadUnsafe(ref zRef));
                        Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.LoadUnsafe(ref zRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                          Unsafe.Add(ref yRef, 2),
                                                                          Unsafe.Add(ref zRef, 2));
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                          Unsafe.Add(ref yRef, 1),
                                                                          Unsafe.Add(ref zRef, 1));
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = TTernaryOperator.Invoke(xRef, yRef, zRef);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                   Vector256.LoadUnsafe(ref yRef),
                                                                   Vector256.LoadUnsafe(ref zRef));
                        Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                   Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                   Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.LoadUnsafe(ref zRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.LoadUnsafe(ref zRef));
                        Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 2:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.LoadUnsafe(ref zRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 1:
                    {
                        dRef = TTernaryOperator.Invoke(xRef, yRef, zRef);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>
        /// with <paramref name="z"/>.
        /// </typeparam>
        private static void InvokeSpanSpanScalarIntoSpan<T, TTernaryOperator>(
            ReadOnlySpan<T> x, ReadOnlySpan<T> y, T z, Span<T> destination)
            where TTernaryOperator : struct, ITernaryOperator<T>
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

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T yRef = ref MemoryMarshal.GetReference(y);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, ref yRef, z, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, z, ref dRef, remainder);
                }

                return;
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, ref yRef, z, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, z, ref dRef, remainder);
                }

                return;
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, ref yRef, z, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, z, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, ref yRef, z, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref T xRef, ref T yRef, T z, ref T dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                      Unsafe.Add(ref yRef, i),
                                                                      z);
                }
            }

            static void Vectorized128(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> zVec = Vector128.Create(z);

                Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                               Vector128.LoadUnsafe(ref yRef),
                                                               zVec);
                Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                               Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                               zVec);

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector128<T>) - ((nuint)dPtr % (uint)sizeof(Vector128<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector128<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<T> vector1;
                        Vector128<T> vector2;
                        Vector128<T> vector3;
                        Vector128<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector128<T>.Count - 1)) & (nuint)(-Vector128<T>.Count);

                switch (remainder / (uint)Vector128<T>.Count)
                {
                    case 8:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector128<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized256(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> zVec = Vector256.Create(z);

                Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                               Vector256.LoadUnsafe(ref yRef),
                                                               zVec);
                Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                               Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                               zVec);

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)dPtr % (uint)sizeof(Vector256<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector256<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<T> vector1;
                        Vector256<T> vector2;
                        Vector256<T> vector3;
                        Vector256<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector256<T>.Count - 1)) & (nuint)(-Vector256<T>.Count);

                switch (remainder / (uint)Vector256<T>.Count)
                {
                    case 8:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                          zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector256<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized512(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> zVec = Vector512.Create(z);

                Vector512<T> beg = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef),
                                                           Vector512.LoadUnsafe(ref yRef),
                                                           zVec);
                Vector512<T> end = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count),
                                                           Vector512.LoadUnsafe(ref yRef, remainder - (uint)Vector512<T>.Count),
                                                           zVec);

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector512<T>) - ((nuint)dPtr % (uint)sizeof(Vector512<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector512<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<T> vector1;
                        Vector512<T> vector2;
                        Vector512<T> vector3;
                        Vector512<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector512<T>.Count - 1)) & (nuint)(-Vector512<T>.Count);

                switch (remainder / (uint)Vector512<T>.Count)
                {
                    case 8:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                      zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                      zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                      zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                      zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                      zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                      zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                      Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                      zVec);
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector512<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
            {
                if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, ref yRef, z, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, ref yRef, z, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 4);

                switch (remainder)
                {
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> zVec = Vector256.Create(z);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       zVec);
                        Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                       zVec);

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 8:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.Create(z));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> zVec = Vector128.Create(z);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       zVec);
                        Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                       zVec);

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.Create(z));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                          Unsafe.Add(ref yRef, 2),
                                                                          z);
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                          Unsafe.Add(ref yRef, 1),
                                                                          z);
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = TTernaryOperator.Invoke(xRef, yRef, z);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> zVec = Vector256.Create(z);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       zVec);
                        Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                       zVec);

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.Create(z));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> zVec = Vector128.Create(z);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       zVec);
                        Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                       zVec);

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 2:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.Create(z));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 1:
                    {
                        dRef = TTernaryOperator.Invoke(xRef, yRef, z);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise element loaded from <paramref name="x"/>, with <paramref name="y"/>,
        /// and the element loaded from <paramref name="z"/>.
        /// </typeparam>
        private static void InvokeSpanScalarSpanIntoSpan<T, TTernaryOperator>(
            ReadOnlySpan<T> x, T y, ReadOnlySpan<T> z, Span<T> destination)
            where TTernaryOperator : struct, ITernaryOperator<T>
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

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T zRef = ref MemoryMarshal.GetReference(z);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, y, ref zRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, y, ref zRef, ref dRef, remainder);
                }

                return;
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, y, ref zRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, y, ref zRef, ref dRef, remainder);
                }

                return;
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && Unsafe.SizeOf<T>() >= 4)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, y, ref zRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, y, ref zRef, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, y, ref zRef, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref T xRef, T y, ref T zRef, ref T dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                      y,
                                                                      Unsafe.Add(ref zRef, i));
                }
            }

            static void Vectorized128(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> yVec = Vector128.Create(y);

                Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                               yVec,
                                                               Vector128.LoadUnsafe(ref zRef));
                Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                               yVec,
                                                               Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* zPtr = pz;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector128<T>) - ((nuint)dPtr % (uint)sizeof(Vector128<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            zPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector128<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<T> vector1;
                        Vector128<T> vector2;
                        Vector128<T> vector3;
                        Vector128<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                zPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                zPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        zRef = ref *zPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector128<T>.Count - 1)) & (nuint)(-Vector128<T>.Count);

                switch (remainder / (uint)Vector128<T>.Count)
                {
                    case 8:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector128<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized256(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> yVec = Vector256.Create(y);

                Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                               yVec,
                                                               Vector256.LoadUnsafe(ref zRef));
                Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                               yVec,
                                                               Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* zPtr = pz;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)dPtr % (uint)sizeof(Vector256<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            zPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector256<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<T> vector1;
                        Vector256<T> vector2;
                        Vector256<T> vector3;
                        Vector256<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                zPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                zPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        zRef = ref *zPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector256<T>.Count - 1)) & (nuint)(-Vector256<T>.Count);

                switch (remainder / (uint)Vector256<T>.Count)
                {
                    case 8:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector256<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            static void Vectorized512(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> yVec = Vector512.Create(y);

                Vector512<T> beg = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef),
                                                               yVec,
                                                               Vector512.LoadUnsafe(ref zRef));
                Vector512<T> end = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count),
                                                               yVec,
                                                               Vector512.LoadUnsafe(ref zRef, remainder - (uint)Vector512<T>.Count));

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* zPtr = pz;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector512<T>) - ((nuint)dPtr % (uint)sizeof(Vector512<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            zPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector512<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<T> vector1;
                        Vector512<T> vector2;
                        Vector512<T> vector3;
                        Vector512<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                zPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                zPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        zRef = ref *zPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector512<T>.Count - 1)) & (nuint)(-Vector512<T>.Count);

                switch (remainder / (uint)Vector512<T>.Count)
                {
                    case 8:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 8)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 7)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 6)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 5)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 4)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 3)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 2)));
                        vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 2));
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end.StoreUnsafe(ref dRef, endIndex - (uint)Vector512<T>.Count);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        beg.StoreUnsafe(ref dRefBeg);
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
            {
                if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, y, ref zRef, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, y, ref zRef, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 4);

                switch (remainder)
                {
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> yVec = Vector256.Create(y);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef));
                        Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 8:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.Create(y),
                                                                       Vector256.LoadUnsafe(ref zRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> yVec = Vector128.Create(y);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef));
                        Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.Create(y),
                                                                       Vector128.LoadUnsafe(ref zRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 2));
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 1));
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = TTernaryOperator.Invoke(xRef, y, zRef);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> yVec = Vector256.Create(y);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef));
                        Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                        break;
                    }

                    case 4:
                    {
                        Debug.Assert(Vector256.IsHardwareAccelerated);

                        Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.Create(y),
                                                                       Vector256.LoadUnsafe(ref zRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 3:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> yVec = Vector128.Create(y);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef));
                        Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                        beg.StoreUnsafe(ref dRef);
                        end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                        break;
                    }

                    case 2:
                    {
                        Debug.Assert(Vector128.IsHardwareAccelerated);

                        Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.Create(y),
                                                                       Vector128.LoadUnsafe(ref zRef));
                        beg.StoreUnsafe(ref dRef);

                        break;
                    }

                    case 1:
                    {
                        dRef = TTernaryOperator.Invoke(xRef, y, zRef);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>Performs (x * y) + z. It will be rounded as one ternary operation if such an operation is accelerated on the current hardware.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T FusedMultiplyAdd<T>(T x, T y, T addend) where T : INumberBase<T>
        {
            if (typeof(T) == typeof(Half))
            {
                Half result = Half.FusedMultiplyAdd(Unsafe.As<T, Half>(ref x), Unsafe.As<T, Half>(ref y), Unsafe.As<T, Half>(ref addend));
                return Unsafe.As<Half, T>(ref result);
            }

            if (typeof(T) == typeof(float))
            {
                float result = float.FusedMultiplyAdd(Unsafe.As<T, float>(ref x), Unsafe.As<T, float>(ref y), Unsafe.As<T, float>(ref addend));
                return Unsafe.As<float, T>(ref result);
            }

            if (typeof(T) == typeof(double))
            {
                double result = double.FusedMultiplyAdd(Unsafe.As<T, double>(ref x), Unsafe.As<T, double>(ref y), Unsafe.As<T, double>(ref addend));
                return Unsafe.As<double, T>(ref result);
            }

            return (x * y) + addend;
        }

        /// <summary>Performs (x * y) + z. It will be rounded as one ternary operation if such an operation is accelerated on the current hardware.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<T> FusedMultiplyAdd<T>(Vector128<T> x, Vector128<T> y, Vector128<T> addend) where T : INumberBase<T>
        {
            if (Fma.IsSupported)
            {
                if (typeof(T) == typeof(float)) return Fma.MultiplyAdd(x.AsSingle(), y.AsSingle(), addend.AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Fma.MultiplyAdd(x.AsDouble(), y.AsDouble(), addend.AsDouble()).As<double, T>();
            }

            if (AdvSimd.IsSupported)
            {
                if (typeof(T) == typeof(float)) return AdvSimd.FusedMultiplyAdd(addend.AsSingle(), x.AsSingle(), y.AsSingle()).As<float, T>();
            }

            return (x * y) + addend;
        }

        /// <summary>Performs (x * y) + z. It will be rounded as one ternary operation if such an operation is accelerated on the current hardware.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<T> FusedMultiplyAdd<T>(Vector256<T> x, Vector256<T> y, Vector256<T> addend) where T : INumberBase<T>
        {
            if (Fma.IsSupported)
            {
                if (typeof(T) == typeof(float)) return Fma.MultiplyAdd(x.AsSingle(), y.AsSingle(), addend.AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Fma.MultiplyAdd(x.AsDouble(), y.AsDouble(), addend.AsDouble()).As<double, T>();
            }

            return (x * y) + addend;
        }

        /// <summary>Performs (x * y) + z. It will be rounded as one ternary operation if such an operation is accelerated on the current hardware.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<T> FusedMultiplyAdd<T>(Vector512<T> x, Vector512<T> y, Vector512<T> addend) where T : INumberBase<T>
        {
            if (Avx512F.IsSupported)
            {
                if (typeof(T) == typeof(float)) return Avx512F.FusedMultiplyAdd(x.AsSingle(), y.AsSingle(), addend.AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Avx512F.FusedMultiplyAdd(x.AsDouble(), y.AsDouble(), addend.AsDouble()).As<double, T>();
            }

            return (x * y) + addend;
        }

        /// <summary>Aggregates all of the elements in the <paramref name="x"/> into a single value.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TAggregate">Specifies the operation to be performed on each pair of values.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T HorizontalAggregate<T, TAggregate>(Vector128<T> x) where TAggregate : struct, IBinaryOperator<T>
        {
            // We need to do log2(count) operations to compute the total sum

            if (Unsafe.SizeOf<T>() == 1)
            {
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsByte(), Vector128.Create((byte)8, 9, 10, 11, 12, 13, 14, 15, 0, 1, 2, 3, 4, 5, 6, 7)).As<byte, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsByte(), Vector128.Create((byte)4, 5, 6, 7, 0, 1, 2, 3, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsByte(), Vector128.Create((byte)2, 3, 0, 1, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsByte(), Vector128.Create((byte)1, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>());
            }
            else if (Unsafe.SizeOf<T>() == 2)
            {
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt16(), Vector128.Create(4, 5, 6, 7, 0, 1, 2, 3)).As<short, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt16(), Vector128.Create(2, 3, 0, 1, 4, 5, 6, 7)).As<short, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt16(), Vector128.Create(1, 0, 2, 3, 4, 5, 6, 7)).As<short, T>());
            }
            else if (Unsafe.SizeOf<T>() == 4)
            {
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt32(), Vector128.Create(2, 3, 0, 1)).As<int, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt32(), Vector128.Create(1, 0, 3, 2)).As<int, T>());
            }
            else if (Unsafe.SizeOf<T>() == 8)
            {
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt64(), Vector128.Create(1, 0)).As<long, T>());
            }
            else
            {
                Debug.Fail("Should not be reachable");
                throw new NotSupportedException();
            }

            return x.ToScalar();
        }

        /// <summary>Aggregates all of the elements in the <paramref name="x"/> into a single value.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TAggregate">Specifies the operation to be performed on each pair of values.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T HorizontalAggregate<T, TAggregate>(Vector256<T> x) where TAggregate : struct, IBinaryOperator<T> =>
            HorizontalAggregate<T, TAggregate>(TAggregate.Invoke(x.GetLower(), x.GetUpper()));

        /// <summary>Aggregates all of the elements in the <paramref name="x"/> into a single value.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TAggregate">Specifies the operation to be performed on each pair of values.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T HorizontalAggregate<T, TAggregate>(Vector512<T> x) where TAggregate : struct, IBinaryOperator<T> =>
            HorizontalAggregate<T, TAggregate>(TAggregate.Invoke(x.GetLower(), x.GetUpper()));

        /// <summary>Gets whether the specified <see cref="float"/> is negative.</summary>
        private static bool IsNegative<T>(T f) where T : INumberBase<T> => T.IsNegative(f);

        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<T> IsNegative<T>(Vector128<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return Vector128.LessThan(vector.AsInt32(), Vector128<int>.Zero).As<int, T>();
            }

            if (typeof(T) == typeof(double))
            {
                return Vector128.LessThan(vector.AsInt64(), Vector128<long>.Zero).As<long, T>();
            }

            return Vector128.LessThan(vector, Vector128<T>.Zero);
        }

        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<T> IsNegative<T>(Vector256<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return Vector256.LessThan(vector.AsInt32(), Vector256<int>.Zero).As<int, T>();
            }

            if (typeof(T) == typeof(double))
            {
                return Vector256.LessThan(vector.AsInt64(), Vector256<long>.Zero).As<long, T>();
            }

            return Vector256.LessThan(vector, Vector256<T>.Zero);
        }

        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<T> IsNegative<T>(Vector512<T> vector)
        {
            if (typeof(T) == typeof(float))
            {
                return Vector512.LessThan(vector.AsInt32(), Vector512<int>.Zero).As<int, T>();
            }

            if (typeof(T) == typeof(double))
            {
                return Vector512.LessThan(vector.AsInt64(), Vector512<long>.Zero).As<long, T>();
            }

            return Vector512.LessThan(vector, Vector512<T>.Zero);
        }

        /// <summary>Gets whether the specified <see cref="float"/> is positive.</summary>
        private static bool IsPositive(float f) => float.IsPositive(f);

        /// <summary>Gets whether each specified <see cref="float"/> is positive.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> IsPositive(Vector128<float> vector) =>
            Vector128.GreaterThan(vector.AsInt32(), Vector128<int>.AllBitsSet).AsSingle();

        /// <summary>Gets whether each specified <see cref="float"/> is positive.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> IsPositive(Vector256<float> vector) =>
            Vector256.GreaterThan(vector.AsInt32(), Vector256<int>.AllBitsSet).AsSingle();

        /// <summary>Gets whether each specified <see cref="float"/> is positive.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<float> IsPositive(Vector512<float> vector) =>
            Vector512.GreaterThan(vector.AsInt32(), Vector512<int>.AllBitsSet).AsSingle();

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<T> CreateAlignmentMaskVector128<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(AlignmentByteMask_64x65)),
                    (uint)(count * 64));
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(AlignmentUInt16Mask_32x33)),
                    (uint)(count * 32));
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(AlignmentUInt32Mask_16x17)),
                    (uint)(count * 16));
            }

            if (Unsafe.SizeOf<T>() == 8)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(AlignmentUInt64Mask_8x9)),
                    (uint)(count * 8));
            }

            Debug.Fail("Shouldn't get here");
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<T> CreateAlignmentMaskVector256<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(AlignmentByteMask_64x65)),
                    (uint)(count * 64));
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(AlignmentUInt16Mask_32x33)),
                    (uint)(count * 32));
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(AlignmentUInt32Mask_16x17)),
                    (uint)(count * 16));
            }

            if (Unsafe.SizeOf<T>() == 8)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(AlignmentUInt64Mask_8x9)),
                    (uint)(count * 8));
            }

            Debug.Fail("Shouldn't get here");
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<T> CreateAlignmentMaskVector512<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(AlignmentByteMask_64x65)),
                    (uint)(count * 64));
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(AlignmentUInt16Mask_32x33)),
                    (uint)(count * 32));
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(AlignmentUInt32Mask_16x17)),
                    (uint)(count * 16));
            }

            if (Unsafe.SizeOf<T>() == 8)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(AlignmentUInt64Mask_8x9)),
                    (uint)(count * 8));
            }

            Debug.Fail("Shouldn't get here - CreateAlignmentMaskVector512");
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<T> CreateRemainderMaskVector128<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(RemainderByteMask_64x65)),
                    (uint)(count * 64) + 48); // last 16 bytes in the row
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(RemainderUInt16Mask_32x33)),
                    (uint)(count * 32) + 24); // last 8 shorts in the row
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x17)),
                    (uint)(count * 16) + 12); // last 4 ints in the row
            }

            if (Unsafe.SizeOf<T>() == 8)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(RemainderUInt64Mask_8x9)),
                    (uint)(count * 8) + 6); // last 2 longs in the row
            }

            Debug.Fail("Shouldn't get here - CreateRemainderMaskVector128");
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<T> CreateRemainderMaskVector256<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(RemainderByteMask_64x65)),
                    (uint)(count * 64) + 32); // last 32 bytes in the row
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(RemainderUInt16Mask_32x33)),
                    (uint)(count * 32) + 16); // last 16 shorts in the row
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x17)),
                    (uint)(count * 16) + 8); // last 8 ints in the row
            }

            if (Unsafe.SizeOf<T>() == 8)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(RemainderUInt64Mask_8x9)),
                    (uint)(count * 8) + 4); // last 4 longs in the row
            }

            Debug.Fail("Shouldn't get here - CreateRemainderMaskVector256");
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<T> CreateRemainderMaskVector512<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(RemainderByteMask_64x65)),
                    (uint)(count * 64));
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(RemainderUInt16Mask_32x33)),
                    (uint)(count * 32));
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x17)),
                    (uint)(count * 16));
            }

            if (Unsafe.SizeOf<T>() == 8)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(RemainderUInt64Mask_8x9)),
                    (uint)(count * 8));
            }

            Debug.Fail("Shouldn't get here - CreateRemainderMaskVector512");
            throw new NotSupportedException();
        }

        /// <summary>x + y</summary>
        internal readonly struct AddOperator<T> : IAggregationOperator<T> where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            public static T Invoke(T x, T y) => x + y;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => x + y;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => x + y;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => x + y;

            public static T Invoke(Vector128<T> x) => Vector128.Sum(x);
            public static T Invoke(Vector256<T> x) => Vector256.Sum(x);
            public static T Invoke(Vector512<T> x) => Vector512.Sum(x);

            public static T IdentityValue => T.AdditiveIdentity;
        }

        /// <summary>x - y</summary>
        internal readonly struct SubtractOperator<T> : IBinaryOperator<T> where T : ISubtractionOperators<T, T, T>
        {
            public static T Invoke(T x, T y) => x - y;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => x - y;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => x - y;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => x - y;
        }

        /// <summary>(x - y) * (x - y)</summary>
        internal readonly struct SubtractSquaredOperator<T> : IBinaryOperator<T> where T : ISubtractionOperators<T, T, T>, IMultiplyOperators<T, T, T>
        {
            public static T Invoke(T x, T y)
            {
                T tmp = x - y;
                return tmp * tmp;
            }

            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                Vector128<T> tmp = x - y;
                return tmp * tmp;
            }

            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                Vector256<T> tmp = x - y;
                return tmp * tmp;
            }

            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                Vector512<T> tmp = x - y;
                return tmp * tmp;
            }
        }

        /// <summary>x * y</summary>
        internal readonly struct MultiplyOperator<T> : IAggregationOperator<T> where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            public static T Invoke(T x, T y) => x * y;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => x * y;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => x * y;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => x * y;

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MultiplyOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MultiplyOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MultiplyOperator<T>>(x);

            public static T IdentityValue => T.MultiplicativeIdentity;
        }

        /// <summary>x / y</summary>
        internal readonly struct DivideOperator<T> : IBinaryOperator<T> where T : IDivisionOperators<T, T, T>
        {
            public static T Invoke(T x, T y) => x / y;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => x / y;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => x / y;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => x / y;
        }

        /// <summary>T.Max(x, y) (but NaNs may not be propagated)</summary>
        internal readonly struct MaxOperator<T> : IAggregationOperator<T> where T : INumber<T>
        {
            public static T Invoke(T x, T y)
            {
                if (typeof(T) == typeof(Half) || typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return x == y ?
                        (IsNegative(x) ? y : x) :
                        (y > x ? y : x);
                }

                return T.Max(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(byte)) return AdvSimd.Max(x.AsByte(), y.AsByte()).As<byte, T>();
                    if (typeof(T) == typeof(sbyte)) return AdvSimd.Max(x.AsSByte(), y.AsSByte()).As<sbyte, T>();
                    if (typeof(T) == typeof(short)) return AdvSimd.Max(x.AsInt16(), y.AsInt16()).As<short, T>();
                    if (typeof(T) == typeof(ushort)) return AdvSimd.Max(x.AsUInt16(), y.AsUInt16()).As<ushort, T>();
                    if (typeof(T) == typeof(int)) return AdvSimd.Max(x.AsInt32(), y.AsInt32()).As<int, T>();
                    if (typeof(T) == typeof(uint)) return AdvSimd.Max(x.AsUInt32(), y.AsUInt32()).As<uint, T>();
                    if (typeof(T) == typeof(float)) return AdvSimd.Max(x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (typeof(T) == typeof(float))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, y),
                            Vector128.ConditionalSelect(IsNegative(x.AsSingle()).As<float, T>(), y, x),
                            Vector128.Max(x, y));
                }

                if (typeof(T) == typeof(double))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, y),
                            Vector128.ConditionalSelect(IsNegative(x.AsDouble()).As<double, T>(), y, x),
                            Vector128.Max(x, y));
                }

                return Vector128.Max(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, y),
                            Vector256.ConditionalSelect(IsNegative(x.AsSingle()).As<float, T>(), y, x),
                            Vector256.Max(x, y));
                }

                if (typeof(T) == typeof(double))
                {
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, y),
                            Vector256.ConditionalSelect(IsNegative(x.AsDouble()).As<double, T>(), y, x),
                            Vector256.Max(x, y));
                }

                return Vector256.Max(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, y),
                            Vector512.ConditionalSelect(IsNegative(x.AsSingle()).As<float, T>(), y, x),
                            Vector512.Max(x, y));
                }

                if (typeof(T) == typeof(double))
                {
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, y),
                            Vector512.ConditionalSelect(IsNegative(x.AsDouble()).As<double, T>(), y, x),
                            Vector512.Max(x, y));
                }

                return Vector512.Max(x, y);
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MaxOperator<T>>(x);
        }

        private interface IIndexOfOperator
        {
            static abstract int Invoke(ref float result, float current, int resultIndex, int curIndex);
            static abstract int Invoke(Vector128<float> result, Vector128<int> resultIndex);
            static abstract void Invoke(ref Vector128<float> result, Vector128<float> current, ref Vector128<int> resultIndex, Vector128<int> curIndex);
            static abstract int Invoke(Vector256<float> result, Vector256<int> resultIndex);
            static abstract void Invoke(ref Vector256<float> result, Vector256<float> current, ref Vector256<int> resultIndex, Vector256<int> curIndex);
            static abstract int Invoke(Vector512<float> result, Vector512<int> resultIndex);
            static abstract void Invoke(ref Vector512<float> result, Vector512<float> current, ref Vector512<int> resultIndex, Vector512<int> curIndex);
        }

        /// <summary>Returns the index of MathF.Max(x, y)</summary>
        internal readonly struct IndexOfMaxOperator : IIndexOfOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector128<float> result, Vector128<int> maxIndex)
            {
                Vector128<float> tmpResult;
                Vector128<int> tmpIndex;

                tmpResult = Vector128.Shuffle(result, Vector128.Create(2, 3, 0, 1));
                tmpIndex = Vector128.Shuffle(maxIndex, Vector128.Create(2, 3, 0, 1));
                Invoke(ref result, tmpResult, ref maxIndex, tmpIndex);

                tmpResult = Vector128.Shuffle(result, Vector128.Create(1, 0, 3, 2));
                tmpIndex = Vector128.Shuffle(maxIndex, Vector128.Create(1, 0, 3, 2));
                Invoke(ref result, tmpResult, ref maxIndex, tmpIndex);

                return maxIndex.ToScalar();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector128<float> max, Vector128<float> current, ref Vector128<int> maxIndex, Vector128<int> curIndex)
            {
                Vector128<float> greaterThanMask = Vector128.GreaterThan(max, current);

                Vector128<float> equalMask = Vector128.Equals(max, current);
                if (equalMask.AsInt32() != Vector128<int>.Zero)
                {
                    Vector128<float> negativeMask = IsNegative(current);
                    Vector128<int> lessThanMask = Vector128.LessThan(maxIndex, curIndex);

                    greaterThanMask |= (negativeMask & equalMask) | (~IsNegative(max) & equalMask & lessThanMask.AsSingle());
                }

                max = ElementWiseSelect(greaterThanMask, max, current);

                maxIndex = ElementWiseSelect(greaterThanMask.AsInt32(), maxIndex, curIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector256<float> result, Vector256<int> maxIndex)
            {
                // Max the upper/lower halves of the Vector256
                Vector128<float> resultLower = result.GetLower();
                Vector128<int> indexLower = maxIndex.GetLower();

                Invoke(ref resultLower, result.GetUpper(), ref indexLower, maxIndex.GetUpper());
                return Invoke(resultLower, indexLower);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector256<float> max, Vector256<float> current, ref Vector256<int> maxIndex, Vector256<int> curIndex)
            {
                Vector256<float> greaterThanMask = Vector256.GreaterThan(max, current);

                Vector256<float> equalMask = Vector256.Equals(max, current);
                if (equalMask.AsInt32() != Vector256<int>.Zero)
                {
                    Vector256<float> negativeMask = IsNegative(current);
                    Vector256<int> lessThanMask = Vector256.LessThan(maxIndex, curIndex);

                    greaterThanMask |= (negativeMask & equalMask) | (~IsNegative(max) & equalMask & lessThanMask.AsSingle());
                }

                max = ElementWiseSelect(greaterThanMask, max, current);

                maxIndex = ElementWiseSelect(greaterThanMask.AsInt32(), maxIndex, curIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector512<float> result, Vector512<int> resultIndex)
            {
                // Min the upper/lower halves of the Vector512
                Vector256<float> resultLower = result.GetLower();
                Vector256<int> indexLower = resultIndex.GetLower();

                Invoke(ref resultLower, result.GetUpper(), ref indexLower, resultIndex.GetUpper());
                return Invoke(resultLower, indexLower);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector512<float> max, Vector512<float> current, ref Vector512<int> maxIndex, Vector512<int> curIndex)
            {
                Vector512<float> greaterThanMask = Vector512.GreaterThan(max, current);

                Vector512<float> equalMask = Vector512.Equals(max, current);
                if (equalMask.AsInt32() != Vector512<int>.Zero)
                {
                    Vector512<float> negativeMask = IsNegative(current);
                    Vector512<int> lessThanMask = Vector512.LessThan(maxIndex, curIndex);

                    greaterThanMask |= (negativeMask & equalMask) | (~IsNegative(max) & equalMask & lessThanMask.AsSingle());
                }

                max = ElementWiseSelect(greaterThanMask, max, current);

                maxIndex = ElementWiseSelect(greaterThanMask.AsInt32(), maxIndex, curIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(ref float result, float current, int resultIndex, int curIndex)
            {
                if (result == current)
                {
                    if (IsNegative(result) && !IsNegative(current))
                    {
                        result = current;
                        return curIndex;
                    }
                }
                else if (current > result)
                {
                    result = current;
                    return curIndex;
                }

                return resultIndex;
            }
        }

        internal readonly struct IndexOfMaxMagnitudeOperator : IIndexOfOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector128<float> result, Vector128<int> maxIndex)
            {
                Vector128<float> tmpResult = Vector128.Shuffle(result, Vector128.Create(2, 3, 0, 1));
                Vector128<int> tmpIndex = Vector128.Shuffle(maxIndex, Vector128.Create(2, 3, 0, 1));

                Invoke(ref result, tmpResult, ref maxIndex, tmpIndex);

                tmpResult = Vector128.Shuffle(result, Vector128.Create(1, 0, 3, 2));
                tmpIndex = Vector128.Shuffle(maxIndex, Vector128.Create(1, 0, 3, 2));

                Invoke(ref result, tmpResult, ref maxIndex, tmpIndex);
                return maxIndex.ToScalar();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector128<float> max, Vector128<float> current, ref Vector128<int> maxIndex, Vector128<int> curIndex)
            {
                Vector128<float> maxMag = Vector128.Abs(max), currentMag = Vector128.Abs(current);

                Vector128<float> greaterThanMask = Vector128.GreaterThan(maxMag, currentMag);

                Vector128<float> equalMask = Vector128.Equals(max, current);
                if (equalMask.AsInt32() != Vector128<int>.Zero)
                {
                    Vector128<float> negativeMask = IsNegative(current);
                    Vector128<int> lessThanMask = Vector128.LessThan(maxIndex, curIndex);

                    greaterThanMask |= (negativeMask & equalMask) | (~IsNegative(max) & equalMask & lessThanMask.AsSingle());
                }

                max = ElementWiseSelect(greaterThanMask, max, current);

                maxIndex = ElementWiseSelect(greaterThanMask.AsInt32(), maxIndex, curIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector256<float> result, Vector256<int> maxIndex)
            {
                // Max the upper/lower halves of the Vector256
                Vector128<float> resultLower = result.GetLower();
                Vector128<int> indexLower = maxIndex.GetLower();

                Invoke(ref resultLower, result.GetUpper(), ref indexLower, maxIndex.GetUpper());
                return Invoke(resultLower, indexLower);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector256<float> max, Vector256<float> current, ref Vector256<int> maxIndex, Vector256<int> curIndex)
            {
                Vector256<float> maxMag = Vector256.Abs(max), currentMag = Vector256.Abs(current);

                Vector256<float> greaterThanMask = Vector256.GreaterThan(maxMag, currentMag);

                Vector256<float> equalMask = Vector256.Equals(max, current);
                if (equalMask.AsInt32() != Vector256<int>.Zero)
                {
                    Vector256<float> negativeMask = IsNegative(current);
                    Vector256<int> lessThanMask = Vector256.LessThan(maxIndex, curIndex);

                    greaterThanMask |= (negativeMask & equalMask) | (~IsNegative(max) & equalMask & lessThanMask.AsSingle());
                }

                max = ElementWiseSelect(greaterThanMask, max, current);

                maxIndex = ElementWiseSelect(greaterThanMask.AsInt32(), maxIndex, curIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector512<float> result, Vector512<int> resultIndex)
            {
                // Min the upper/lower halves of the Vector512
                Vector256<float> resultLower = result.GetLower();
                Vector256<int> indexLower = resultIndex.GetLower();

                Invoke(ref resultLower, result.GetUpper(), ref indexLower, resultIndex.GetUpper());
                return Invoke(resultLower, indexLower);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector512<float> max, Vector512<float> current, ref Vector512<int> maxIndex, Vector512<int> curIndex)
            {
                Vector512<float> maxMag = Vector512.Abs(max), currentMag = Vector512.Abs(current);
                Vector512<float> greaterThanMask = Vector512.GreaterThan(maxMag, currentMag);

                Vector512<float> equalMask = Vector512.Equals(max, current);
                if (equalMask.AsInt32() != Vector512<int>.Zero)
                {
                    Vector512<float> negativeMask = IsNegative(current);
                    Vector512<int> lessThanMask = Vector512.LessThan(maxIndex, curIndex);

                    greaterThanMask |= (negativeMask & equalMask) | (~IsNegative(max) & equalMask & lessThanMask.AsSingle());
                }

                max = ElementWiseSelect(greaterThanMask, max, current);

                maxIndex = ElementWiseSelect(greaterThanMask.AsInt32(), maxIndex, curIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(ref float result, float current, int resultIndex, int curIndex)
            {
                float curMaxAbs = MathF.Abs(result);
                float currentAbs = MathF.Abs(current);

                if (curMaxAbs == currentAbs)
                {
                    if (IsNegative(result) && !IsNegative(current))
                    {
                        result = current;
                        return curIndex;
                    }
                }
                else if (currentAbs > curMaxAbs)
                {
                    result = current;
                    return curIndex;
                }

                return resultIndex;
            }
        }

        /// <summary>Returns the index of MathF.Min(x, y)</summary>
        internal readonly struct IndexOfMinOperator : IIndexOfOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector128<float> result, Vector128<int> resultIndex)
            {
                Vector128<float> tmpResult = Vector128.Shuffle(result, Vector128.Create(2, 3, 0, 1));
                Vector128<int> tmpIndex = Vector128.Shuffle(resultIndex, Vector128.Create(2, 3, 0, 1));

                Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                tmpResult = Vector128.Shuffle(result, Vector128.Create(1, 0, 3, 2));
                tmpIndex = Vector128.Shuffle(resultIndex, Vector128.Create(1, 0, 3, 2));

                Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);
                return resultIndex.ToScalar();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector128<float> result, Vector128<float> current, ref Vector128<int> resultIndex, Vector128<int> curIndex)
            {
                Vector128<float> lessThanMask = Vector128.LessThan(result, current);

                Vector128<float> equalMask = Vector128.Equals(result, current);
                if (equalMask.AsInt32() != Vector128<int>.Zero)
                {
                    Vector128<float> negativeMask = IsNegative(current);
                    Vector128<int> lessThanIndexMask = Vector128.LessThan(resultIndex, curIndex);

                    lessThanMask |= (~negativeMask & equalMask) | (IsNegative(result) & equalMask & lessThanIndexMask.AsSingle());
                }

                result = ElementWiseSelect(lessThanMask, result, current);

                resultIndex = ElementWiseSelect(lessThanMask.AsInt32(), resultIndex, curIndex);
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector256<float> result, Vector256<int> resultIndex)
            {
                // Min the upper/lower halves of the Vector256
                Vector128<float> resultLower = result.GetLower();
                Vector128<int> indexLower = resultIndex.GetLower();

                Invoke(ref resultLower, result.GetUpper(), ref indexLower, resultIndex.GetUpper());
                return Invoke(resultLower, indexLower);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector256<float> result, Vector256<float> current, ref Vector256<int> resultIndex, Vector256<int> curIndex)
            {
                Vector256<float> lessThanMask = Vector256.LessThan(result, current);

                Vector256<float> equalMask = Vector256.Equals(result, current);
                if (equalMask.AsInt32() != Vector256<int>.Zero)
                {
                    Vector256<float> negativeMask = IsNegative(current);
                    Vector256<int> lessThanIndexMask = Vector256.LessThan(resultIndex, curIndex);

                    lessThanMask |= (~negativeMask & equalMask) | (IsNegative(result) & equalMask & lessThanIndexMask.AsSingle());
                }

                result = ElementWiseSelect(lessThanMask, result, current);

                resultIndex = ElementWiseSelect(lessThanMask.AsInt32(), resultIndex, curIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector512<float> result, Vector512<int> resultIndex)
            {
                // Min the upper/lower halves of the Vector512
                Vector256<float> resultLower = result.GetLower();
                Vector256<int> indexLower = resultIndex.GetLower();

                Invoke(ref resultLower, result.GetUpper(), ref indexLower, resultIndex.GetUpper());
                return Invoke(resultLower, indexLower);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector512<float> result, Vector512<float> current, ref Vector512<int> resultIndex, Vector512<int> curIndex)
            {
                Vector512<float> lessThanMask = Vector512.LessThan(result, current);

                Vector512<float> equalMask = Vector512.Equals(result, current);
                if (equalMask.AsInt32() != Vector512<int>.Zero)
                {
                    Vector512<float> negativeMask = IsNegative(current);
                    Vector512<int> lessThanIndexMask = Vector512.LessThan(resultIndex, curIndex);

                    lessThanMask |= (~negativeMask & equalMask) | (IsNegative(result) & equalMask & lessThanIndexMask.AsSingle());
                }

                result = ElementWiseSelect(lessThanMask, result, current);

                resultIndex = ElementWiseSelect(lessThanMask.AsInt32(), resultIndex, curIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(ref float result, float current, int resultIndex, int curIndex)
            {
                if (result == current)
                {
                    if (IsPositive(result) && !IsPositive(current))
                    {
                        result = current;
                        return curIndex;
                    }
                }
                else if (current < result)
                {
                    result = current;
                    return curIndex;
                }

                return resultIndex;
            }
        }

        internal readonly struct IndexOfMinMagnitudeOperator : IIndexOfOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector128<float> result, Vector128<int> resultIndex)
            {
                Vector128<float> tmpResult = Vector128.Shuffle(result, Vector128.Create(2, 3, 0, 1));
                Vector128<int> tmpIndex = Vector128.Shuffle(resultIndex, Vector128.Create(2, 3, 0, 1));

                Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                tmpResult = Vector128.Shuffle(result, Vector128.Create(1, 0, 3, 2));
                tmpIndex = Vector128.Shuffle(resultIndex, Vector128.Create(1, 0, 3, 2));

                Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);
                return resultIndex.ToScalar();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector128<float> result, Vector128<float> current, ref Vector128<int> resultIndex, Vector128<int> curIndex)
            {
                Vector128<float> minMag = Vector128.Abs(result), currentMag = Vector128.Abs(current);

                Vector128<float> lessThanMask = Vector128.LessThan(minMag, currentMag);

                Vector128<float> equalMask = Vector128.Equals(result, current);
                if (equalMask.AsInt32() != Vector128<int>.Zero)
                {
                    Vector128<float> negativeMask = IsNegative(current);
                    Vector128<int> lessThanIndexMask = Vector128.LessThan(resultIndex, curIndex);

                    lessThanMask |= (~negativeMask & equalMask) | (IsNegative(result) & equalMask & lessThanIndexMask.AsSingle());
                }

                result = ElementWiseSelect(lessThanMask, result, current);

                resultIndex = ElementWiseSelect(lessThanMask.AsInt32(), resultIndex, curIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector256<float> result, Vector256<int> resultIndex)
            {
                // Min the upper/lower halves of the Vector256
                Vector128<float> resultLower = result.GetLower();
                Vector128<int> indexLower = resultIndex.GetLower();

                Invoke(ref resultLower, result.GetUpper(), ref indexLower, resultIndex.GetUpper());
                return Invoke(resultLower, indexLower);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector256<float> result, Vector256<float> current, ref Vector256<int> resultIndex, Vector256<int> curIndex)
            {
                Vector256<float> minMag = Vector256.Abs(result), currentMag = Vector256.Abs(current);

                Vector256<float> lessThanMask = Vector256.LessThan(minMag, currentMag);

                Vector256<float> equalMask = Vector256.Equals(result, current);
                if (equalMask.AsInt32() != Vector256<int>.Zero)
                {
                    Vector256<float> negativeMask = IsNegative(current);
                    Vector256<int> lessThanIndexMask = Vector256.LessThan(resultIndex, curIndex);

                    lessThanMask |= (~negativeMask & equalMask) | (IsNegative(result) & equalMask & lessThanIndexMask.AsSingle());
                }

                result = ElementWiseSelect(lessThanMask, result, current);

                resultIndex = ElementWiseSelect(lessThanMask.AsInt32(), resultIndex, curIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(Vector512<float> result, Vector512<int> resultIndex)
            {
                // Min the upper/lower halves of the Vector512
                Vector256<float> resultLower = result.GetLower();
                Vector256<int> indexLower = resultIndex.GetLower();

                Invoke(ref resultLower, result.GetUpper(), ref indexLower, resultIndex.GetUpper());
                return Invoke(resultLower, indexLower);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Invoke(ref Vector512<float> result, Vector512<float> current, ref Vector512<int> resultIndex, Vector512<int> curIndex)
            {
                Vector512<float> minMag = Vector512.Abs(result), currentMag = Vector512.Abs(current);

                Vector512<float> lessThanMask = Vector512.LessThan(minMag, currentMag);

                Vector512<float> equalMask = Vector512.Equals(result, current);
                if (equalMask.AsInt32() != Vector512<int>.Zero)
                {
                    Vector512<float> negativeMask = IsNegative(current);
                    Vector512<int> lessThanIndexMask = Vector512.LessThan(resultIndex, curIndex);

                    lessThanMask |= (~negativeMask & equalMask) | (IsNegative(result) & equalMask & lessThanIndexMask.AsSingle());
                }

                result = ElementWiseSelect(lessThanMask, result, current);

                resultIndex = ElementWiseSelect(lessThanMask.AsInt32(), resultIndex, curIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Invoke(ref float result, float current, int resultIndex, int curIndex)
            {
                float curMinAbs = MathF.Abs(result);
                float currentAbs = MathF.Abs(current);
                if (curMinAbs == currentAbs)
                {
                    if (IsPositive(result) && !IsPositive(current))
                    {
                        result = current;
                        return curIndex;
                    }
                }
                else if (currentAbs < curMinAbs)
                {
                    result = current;
                    return curIndex;
                }

                return resultIndex;
            }
        }

        /// <summary>Max(x, y)</summary>
        internal readonly struct MaxPropagateNaNOperator<T> : IBinaryOperator<T>
             where T : INumber<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.Max(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(byte)) return AdvSimd.Max(x.AsByte(), y.AsByte()).As<byte, T>();
                    if (typeof(T) == typeof(sbyte)) return AdvSimd.Max(x.AsSByte(), y.AsSByte()).As<sbyte, T>();
                    if (typeof(T) == typeof(ushort)) return AdvSimd.Max(x.AsUInt16(), y.AsUInt16()).As<ushort, T>();
                    if (typeof(T) == typeof(short)) return AdvSimd.Max(x.AsInt16(), y.AsInt16()).As<short, T>();
                    if (typeof(T) == typeof(uint)) return AdvSimd.Max(x.AsUInt32(), y.AsUInt32()).As<uint, T>();
                    if (typeof(T) == typeof(int)) return AdvSimd.Max(x.AsInt32(), y.AsInt32()).As<int, T>();
                    if (typeof(T) == typeof(float)) return AdvSimd.Max(x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, x),
                            Vector128.ConditionalSelect(Vector128.Equals(y, y),
                                Vector128.ConditionalSelect(Vector128.Equals(x, y),
                                    Vector128.ConditionalSelect(IsNegative(x), y, x),
                                    Vector128.Max(x, y)),
                                y),
                            x);
                }

                return Vector128.Max(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, x),
                            Vector256.ConditionalSelect(Vector256.Equals(y, y),
                                Vector256.ConditionalSelect(Vector256.Equals(x, y),
                                    Vector256.ConditionalSelect(IsNegative(x), y, x),
                                    Vector256.Max(x, y)),
                                y),
                            x);
                }

                return Vector256.Max(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, x),
                            Vector512.ConditionalSelect(Vector512.Equals(y, y),
                                Vector512.ConditionalSelect(Vector512.Equals(x, y),
                                    Vector512.ConditionalSelect(IsNegative(x), y, x),
                                    Vector512.Max(x, y)),
                                y),
                            x);
                }

                return Vector512.Max(x, y);
            }
        }

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs (but NaNs may not be propagated)</summary>
        internal readonly struct MaxMagnitudeOperator<T> : IAggregationOperator<T>
            where T : INumberBase<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.MaxMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);

                Vector128<T> result =
                    Vector128.ConditionalSelect(Vector128.Equals(xMag, yMag),
                        Vector128.ConditionalSelect(IsNegative(x), y, x),
                        Vector128.ConditionalSelect(Vector128.GreaterThan(xMag, yMag), x, y));

                // Handle minimum signed value that should have the largest magnitude
                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector128<T> negativeMagnitudeX = Vector128.LessThan(xMag, Vector128<T>.Zero);
                    Vector128<T> negativeMagnitudeY = Vector128.LessThan(yMag, Vector128<T>.Zero);
                    result = Vector128.ConditionalSelect(negativeMagnitudeX,
                        x,
                        Vector128.ConditionalSelect(negativeMagnitudeY,
                            y,
                            result));
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                Vector256<T> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);

                Vector256<T> result =
                    Vector256.ConditionalSelect(Vector256.Equals(xMag, yMag),
                        Vector256.ConditionalSelect(IsNegative(x), y, x),
                        Vector256.ConditionalSelect(Vector256.GreaterThan(xMag, yMag), x, y));

                // Handle minimum signed value that should have the largest magnitude
                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector256<T> negativeMagnitudeX = Vector256.LessThan(xMag, Vector256<T>.Zero);
                    Vector256<T> negativeMagnitudeY = Vector256.LessThan(yMag, Vector256<T>.Zero);
                    result = Vector256.ConditionalSelect(negativeMagnitudeX,
                        x,
                        Vector256.ConditionalSelect(negativeMagnitudeY,
                            y,
                            result));
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                Vector512<T> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);

                Vector512<T> result =
                    Vector512.ConditionalSelect(Vector512.Equals(xMag, yMag),
                        Vector512.ConditionalSelect(IsNegative(x), y, x),
                        Vector512.ConditionalSelect(Vector512.GreaterThan(xMag, yMag), x, y));

                // Handle minimum signed value that should have the largest magnitude
                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector512<T> negativeMagnitudeX = Vector512.LessThan(xMag, Vector512<T>.Zero);
                    Vector512<T> negativeMagnitudeY = Vector512.LessThan(yMag, Vector512<T>.Zero);
                    result = Vector512.ConditionalSelect(negativeMagnitudeX,
                        x,
                        Vector512.ConditionalSelect(negativeMagnitudeY,
                            y,
                            result));
                }

                return result;
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MaxMagnitudeOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MaxMagnitudeOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MaxMagnitudeOperator<T>>(x);
        }

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs</summary>
        internal readonly struct MaxMagnitudePropagateNaNOperator<T> : IBinaryOperator<T>
            where T : INumberBase<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.MaxMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, x),
                            Vector128.ConditionalSelect(Vector128.Equals(y, y),
                                Vector128.ConditionalSelect(Vector128.Equals(yMag, xMag),
                                    Vector128.ConditionalSelect(IsNegative(x), y, x),
                                    Vector128.ConditionalSelect(Vector128.GreaterThan(yMag, xMag), y, x)),
                                y),
                            x);
                }

                return MaxMagnitudeOperator<T>.Invoke(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector256<T> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, x),
                            Vector256.ConditionalSelect(Vector256.Equals(y, y),
                                Vector256.ConditionalSelect(Vector256.Equals(xMag, yMag),
                                    Vector256.ConditionalSelect(IsNegative(x), y, x),
                                    Vector256.ConditionalSelect(Vector256.GreaterThan(xMag, yMag), x, y)),
                                y),
                            x);
                }

                return MaxMagnitudeOperator<T>.Invoke(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector512<T> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, x),
                        Vector512.ConditionalSelect(Vector512.Equals(y, y),
                            Vector512.ConditionalSelect(Vector512.Equals(xMag, yMag),
                                Vector512.ConditionalSelect(IsNegative(x), y, x),
                                Vector512.ConditionalSelect(Vector512.GreaterThan(xMag, yMag), x, y)),
                            y),
                        x);
                }

                return MaxMagnitudeOperator<T>.Invoke(x, y);
            }
        }

        /// <summary>T.Min(x, y) (but NaNs may not be propagated)</summary>
        internal readonly struct MinOperator<T> : IAggregationOperator<T>
            where T : INumber<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y)
            {
                if (typeof(T) == typeof(Half) || typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return x == y ?
                        (IsNegative(y) ? y : x) :
                        (y < x ? y : x);
                }

                return T.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(byte)) return AdvSimd.Min(x.AsByte(), y.AsByte()).As<byte, T>();
                    if (typeof(T) == typeof(sbyte)) return AdvSimd.Min(x.AsSByte(), y.AsSByte()).As<sbyte, T>();
                    if (typeof(T) == typeof(short)) return AdvSimd.Min(x.AsInt16(), y.AsInt16()).As<short, T>();
                    if (typeof(T) == typeof(ushort)) return AdvSimd.Min(x.AsUInt16(), y.AsUInt16()).As<ushort, T>();
                    if (typeof(T) == typeof(int)) return AdvSimd.Min(x.AsInt32(), y.AsInt32()).As<int, T>();
                    if (typeof(T) == typeof(uint)) return AdvSimd.Min(x.AsUInt32(), y.AsUInt32()).As<uint, T>();
                    if (typeof(T) == typeof(float)) return AdvSimd.Min(x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, y),
                            Vector128.ConditionalSelect(IsNegative(y), y, x),
                            Vector128.Min(x, y));
                }

                return Vector128.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return Vector256.ConditionalSelect(Vector256.Equals(x, y),
                        Vector256.ConditionalSelect(IsNegative(y), y, x),
                        Vector256.Min(x, y));
                }

                return Vector256.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return Vector512.ConditionalSelect(Vector512.Equals(x, y),
                        Vector512.ConditionalSelect(IsNegative(y), y, x),
                        Vector512.Min(x, y));
                }

                return Vector512.Min(x, y);
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MinOperator<T>>(x);
        }

        /// <summary>T.Min(x, y)</summary>
        internal readonly struct MinPropagateNaNOperator<T> : IBinaryOperator<T>
            where T : INumber<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.Min(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(byte)) return AdvSimd.Min(x.AsByte(), y.AsByte()).As<byte, T>();
                    if (typeof(T) == typeof(sbyte)) return AdvSimd.Min(x.AsSByte(), y.AsSByte()).As<sbyte, T>();
                    if (typeof(T) == typeof(short)) return AdvSimd.Min(x.AsInt16(), y.AsInt16()).As<short, T>();
                    if (typeof(T) == typeof(ushort)) return AdvSimd.Min(x.AsUInt16(), y.AsUInt16()).As<ushort, T>();
                    if (typeof(T) == typeof(int)) return AdvSimd.Min(x.AsInt32(), y.AsInt32()).As<int, T>();
                    if (typeof(T) == typeof(uint)) return AdvSimd.Min(x.AsUInt32(), y.AsUInt32()).As<uint, T>();
                    if (typeof(T) == typeof(float)) return AdvSimd.Min(x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, x),
                            Vector128.ConditionalSelect(Vector128.Equals(y, y),
                                Vector128.ConditionalSelect(Vector128.Equals(x, y),
                                    Vector128.ConditionalSelect(IsNegative(x), x, y),
                                    Vector128.Min(x, y)),
                                y),
                            x);
                }

                return Vector128.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, x),
                            Vector256.ConditionalSelect(Vector256.Equals(y, y),
                                Vector256.ConditionalSelect(Vector256.Equals(x, y),
                                    Vector256.ConditionalSelect(IsNegative(x), x, y),
                                    Vector256.Min(x, y)),
                                y),
                            x);
                }

                return Vector256.Min(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, x),
                            Vector512.ConditionalSelect(Vector512.Equals(y, y),
                                Vector512.ConditionalSelect(Vector512.Equals(x, y),
                                    Vector512.ConditionalSelect(IsNegative(x), x, y),
                                    Vector512.Min(x, y)),
                                y),
                            x);
                }

                return Vector512.Min(x, y);
            }
        }

        /// <summary>Operator to get x or y based on which has the smaller MathF.Abs (but NaNs may not be propagated)</summary>
        internal readonly struct MinMagnitudeOperator<T> : IAggregationOperator<T>
            where T : INumberBase<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.MinMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);

                Vector128<T> result =
                    Vector128.ConditionalSelect(Vector128.Equals(yMag, xMag),
                        Vector128.ConditionalSelect(IsNegative(y), y, x),
                        Vector128.ConditionalSelect(Vector128.LessThan(yMag, xMag), y, x));

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector128<T> negativeMagnitudeX = Vector128.LessThan(xMag, Vector128<T>.Zero);
                    Vector128<T> negativeMagnitudeY = Vector128.LessThan(yMag, Vector128<T>.Zero);
                    result = Vector128.ConditionalSelect(negativeMagnitudeX,
                        y,
                        Vector128.ConditionalSelect(negativeMagnitudeY,
                            x,
                            result));
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                Vector256<T> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);

                Vector256<T> result =
                    Vector256.ConditionalSelect(Vector256.Equals(yMag, xMag),
                        Vector256.ConditionalSelect(IsNegative(y), y, x),
                        Vector256.ConditionalSelect(Vector256.LessThan(yMag, xMag), y, x));

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector256<T> negativeMagnitudeX = Vector256.LessThan(xMag, Vector256<T>.Zero);
                    Vector256<T> negativeMagnitudeY = Vector256.LessThan(yMag, Vector256<T>.Zero);
                    result = Vector256.ConditionalSelect(negativeMagnitudeX,
                        y,
                        Vector256.ConditionalSelect(negativeMagnitudeY,
                            x,
                            result));
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                Vector512<T> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);

                Vector512<T> result =
                    Vector512.ConditionalSelect(Vector512.Equals(yMag, xMag),
                        Vector512.ConditionalSelect(IsNegative(y), y, x),
                        Vector512.ConditionalSelect(Vector512.LessThan(yMag, xMag), y, x));

                if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(nint))
                {
                    Vector512<T> negativeMagnitudeX = Vector512.LessThan(xMag, Vector512<T>.Zero);
                    Vector512<T> negativeMagnitudeY = Vector512.LessThan(yMag, Vector512<T>.Zero);
                    result = Vector512.ConditionalSelect(negativeMagnitudeX,
                        y,
                        Vector512.ConditionalSelect(negativeMagnitudeY,
                            x,
                            result));
                }

                return result;
            }

            public static T Invoke(Vector128<T> x) => HorizontalAggregate<T, MinMagnitudeOperator<T>>(x);
            public static T Invoke(Vector256<T> x) => HorizontalAggregate<T, MinMagnitudeOperator<T>>(x);
            public static T Invoke(Vector512<T> x) => HorizontalAggregate<T, MinMagnitudeOperator<T>>(x);
        }

        /// <summary>Operator to get x or y based on which has the smaller MathF.Abs</summary>
        internal readonly struct MinMagnitudePropagateNaNOperator<T> : IBinaryOperator<T>
            where T : INumberBase<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.MinMagnitude(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector128<T> xMag = Vector128.Abs(x), yMag = Vector128.Abs(y);
                    return
                        Vector128.ConditionalSelect(Vector128.Equals(x, x),
                            Vector128.ConditionalSelect(Vector128.Equals(y, y),
                                Vector128.ConditionalSelect(Vector128.Equals(yMag, xMag),
                                    Vector128.ConditionalSelect(IsNegative(x), x, y),
                                    Vector128.ConditionalSelect(Vector128.LessThan(xMag, yMag), x, y)),
                                y),
                            x);
                }

                return MinMagnitudeOperator<T>.Invoke(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector256<T> xMag = Vector256.Abs(x), yMag = Vector256.Abs(y);
                    return
                        Vector256.ConditionalSelect(Vector256.Equals(x, x),
                            Vector256.ConditionalSelect(Vector256.Equals(y, y),
                                Vector256.ConditionalSelect(Vector256.Equals(yMag, xMag),
                                    Vector256.ConditionalSelect(IsNegative(x), x, y),
                                    Vector256.ConditionalSelect(Vector256.LessThan(xMag, yMag), x, y)),
                                y),
                            x);
                }

                return MinMagnitudeOperator<T>.Invoke(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                // Handle NaNs
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector512<T> xMag = Vector512.Abs(x), yMag = Vector512.Abs(y);
                    return
                        Vector512.ConditionalSelect(Vector512.Equals(x, x),
                            Vector512.ConditionalSelect(Vector512.Equals(y, y),
                                Vector512.ConditionalSelect(Vector512.Equals(yMag, xMag),
                                    Vector512.ConditionalSelect(IsNegative(x), x, y),
                                    Vector512.ConditionalSelect(Vector512.LessThan(xMag, yMag), x, y)),
                                y),
                            x);
                }

                return MinMagnitudeOperator<T>.Invoke(x, y);
            }
        }

        /// <summary>-x</summary>
        internal readonly struct NegateOperator<T> : IUnaryOperator<T> where T : IUnaryNegationOperators<T, T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => -x;
            public static Vector128<T> Invoke(Vector128<T> x) => -x;
            public static Vector256<T> Invoke(Vector256<T> x) => -x;
            public static Vector512<T> Invoke(Vector512<T> x) => -x;
        }

        /// <summary>(x + y) * z</summary>
        internal readonly struct AddMultiplyOperator<T> : ITernaryOperator<T> where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T>
        {
            public static T Invoke(T x, T y, T z) => (x + y) * z;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> z) => (x + y) * z;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> z) => (x + y) * z;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> z) => (x + y) * z;
        }

        /// <summary>(x * y) + z</summary>
        internal readonly struct MultiplyAddOperator<T> : ITernaryOperator<T> where T : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T>
        {
            public static T Invoke(T x, T y, T z) => (x * y) + z;
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> z) => (x * y) + z;
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> z) => (x * y) + z;
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> z) => (x * y) + z;
        }

        /// <summary>x</summary>
        internal readonly struct IdentityOperator<T> : IUnaryOperator<T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => x;
            public static Vector128<T> Invoke(Vector128<T> x) => x;
            public static Vector256<T> Invoke(Vector256<T> x) => x;
            public static Vector512<T> Invoke(Vector512<T> x) => x;
        }

        /// <summary>x * x</summary>
        internal readonly struct SquaredOperator<T> : IUnaryOperator<T> where T : IMultiplyOperators<T, T, T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => x * x;
            public static Vector128<T> Invoke(Vector128<T> x) => x * x;
            public static Vector256<T> Invoke(Vector256<T> x) => x * x;
            public static Vector512<T> Invoke(Vector512<T> x) => x * x;
        }

        /// <summary>T.Abs(x)</summary>
        internal readonly struct AbsoluteOperator<T> : IUnaryOperator<T> where T : INumberBase<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x) => T.Abs(x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(sbyte) ||
                    typeof(T) == typeof(short) ||
                    typeof(T) == typeof(int) ||
                    typeof(T) == typeof(long) ||
                    typeof(T) == typeof(nint))
                {
                    // Handle signed integers specially, in order to throw if any attempt is made to
                    // take the absolute value of the minimum value of the type, which doesn't have
                    // a positive absolute value representation.
                    Vector128<T> negated = -x;
                    if (Vector128.Equals(x, negated) != Vector128<T>.Zero)
                    {
                        ThrowNegateTwosCompOverflow();
                    }

                    return Vector128.ConditionalSelect(Vector128.LessThan(x, Vector128<T>.Zero), negated, x);
                }

                return Vector128.Abs(x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(sbyte) ||
                    typeof(T) == typeof(short) ||
                    typeof(T) == typeof(int) ||
                    typeof(T) == typeof(long) ||
                    typeof(T) == typeof(nint))
                {
                    // Handle signed integers specially, in order to throw if any attempt is made to
                    // take the absolute value of the minimum value of the type, which doesn't have
                    // a positive absolute value representation.
                    Vector256<T> negated = -x;
                    if (Vector256.Equals(x, negated) != Vector256<T>.Zero)
                    {
                        ThrowNegateTwosCompOverflow();
                    }

                    return Vector256.ConditionalSelect(Vector256.LessThan(x, Vector256<T>.Zero), negated, x);
                }

                return Vector256.Abs(x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(sbyte) ||
                    typeof(T) == typeof(short) ||
                    typeof(T) == typeof(int) ||
                    typeof(T) == typeof(long) ||
                    typeof(T) == typeof(nint))
                {
                    // Handle signed integers specially, in order to throw if any attempt is made to
                    // take the absolute value of the minimum value of the type, which doesn't have
                    // a positive absolute value representation.
                    Vector512<T> negated = -x;
                    if (Vector512.Equals(x, negated) != Vector512<T>.Zero)
                    {
                        ThrowNegateTwosCompOverflow();
                    }

                    return Vector512.ConditionalSelect(Vector512.LessThan(x, Vector512<T>.Zero), negated, x);
                }

                return Vector512.Abs(x);
            }
        }

        /// <summary>T.Exp(x)</summary>
        internal readonly struct ExpOperator<T> : IUnaryOperator<T>
            where T : IExponentialFunctions<T>
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

            public static bool Vectorizable => typeof(T) == typeof(float);

            public static T Invoke(T x) => T.Exp(x);

            public static Vector128<T> Invoke(Vector128<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector128<float> x = t.AsSingle();

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

                return ret.As<float, T>();
            }

            public static Vector256<T> Invoke(Vector256<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector256<float> x = t.AsSingle();

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

                return ret.As<float, T>();
            }

            public static Vector512<T> Invoke(Vector512<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector512<float> x = t.AsSingle();

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

                return ret.As<float, T>();
            }
        }

        /// <summary>T.Cosh(x)</summary>
        internal readonly struct CoshOperator<T> : IUnaryOperator<T>
            where T : IHyperbolicFunctions<T>
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

            public static bool Vectorizable => typeof(T) == typeof(float);

            public static T Invoke(T x) => T.Cosh(x);

            public static Vector128<T> Invoke(Vector128<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector128<float> x = t.AsSingle();

                Vector128<float> y = Vector128.Abs(x);
                Vector128<float> z = ExpOperator<float>.Invoke(y - Vector128.Create(LOGV));
                return (Vector128.Create(HALFV) * (z + (Vector128.Create(INVV2) / z))).As<float, T>();
            }

            public static Vector256<T> Invoke(Vector256<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector256<float> x = t.AsSingle();

                Vector256<float> y = Vector256.Abs(x);
                Vector256<float> z = ExpOperator<float>.Invoke(y - Vector256.Create(LOGV));
                return (Vector256.Create(HALFV) * (z + (Vector256.Create(INVV2) / z))).As<float, T>();
            }

            public static Vector512<T> Invoke(Vector512<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector512<float> x = t.AsSingle();

                Vector512<float> y = Vector512.Abs(x);
                Vector512<float> z = ExpOperator<float>.Invoke(y - Vector512.Create(LOGV));
                return (Vector512.Create(HALFV) * (z + (Vector512.Create(INVV2) / z))).As<float, T>();
            }
        }

        /// <summary>T.Sinh(x)</summary>
        internal readonly struct SinhOperator<T> : IUnaryOperator<T>
            where T : IHyperbolicFunctions<T>
        {
            // Same as cosh, but with `z -` rather than `z +`, and with the sign
            // flipped on the result based on the sign of the input.

            private const uint SIGN_MASK = 0x7FFFFFFF;
            private const float LOGV = 0.693161f;
            private const float HALFV = 1.0000138f;
            private const float INVV2 = 0.24999309f;

            public static bool Vectorizable => typeof(T) == typeof(float);

            public static T Invoke(T x) => T.Sinh(x);

            public static Vector128<T> Invoke(Vector128<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector128<float> x = t.AsSingle();

                Vector128<float> y = Vector128.Abs(x);
                Vector128<float> z = ExpOperator<float>.Invoke(y - Vector128.Create(LOGV));
                Vector128<float> result = Vector128.Create(HALFV) * (z - (Vector128.Create(INVV2) / z));
                Vector128<uint> sign = x.AsUInt32() & Vector128.Create(~SIGN_MASK);
                return (sign ^ result.AsUInt32()).AsSingle().As<float, T>();
            }

            public static Vector256<T> Invoke(Vector256<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector256<float> x = t.AsSingle();

                Vector256<float> y = Vector256.Abs(x);
                Vector256<float> z = ExpOperator<float>.Invoke(y - Vector256.Create(LOGV));
                Vector256<float> result = Vector256.Create(HALFV) * (z - (Vector256.Create(INVV2) / z));
                Vector256<uint> sign = x.AsUInt32() & Vector256.Create(~SIGN_MASK);
                return (sign ^ result.AsUInt32()).AsSingle().As<float, T>();
            }

            public static Vector512<T> Invoke(Vector512<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector512<float> x = t.AsSingle();

                Vector512<float> y = Vector512.Abs(x);
                Vector512<float> z = ExpOperator<float>.Invoke(y - Vector512.Create(LOGV));
                Vector512<float> result = Vector512.Create(HALFV) * (z - (Vector512.Create(INVV2) / z));
                Vector512<uint> sign = x.AsUInt32() & Vector512.Create(~SIGN_MASK);
                return (sign ^ result.AsUInt32()).AsSingle().As<float, T>();
            }
        }

        /// <summary>T.Tanh(x)</summary>
        internal readonly struct TanhOperator<T> : IUnaryOperator<T>
            where T : IHyperbolicFunctions<T>
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

            public static bool Vectorizable => typeof(T) == typeof(float);

            public static T Invoke(T x) => T.Tanh(x);

            public static Vector128<T> Invoke(Vector128<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector128<float> x = t.AsSingle();

                Vector128<float> y = Vector128.Abs(x);
                Vector128<float> z = ExpOperator<float>.Invoke(Vector128.Create(-2f) * y) - Vector128.Create(1f);
                Vector128<uint> sign = x.AsUInt32() & Vector128.Create(~SIGN_MASK);
                return (sign ^ (-z / (z + Vector128.Create(2f))).AsUInt32()).AsSingle().As<float, T>();
            }

            public static Vector256<T> Invoke(Vector256<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector256<float> x = t.AsSingle();

                Vector256<float> y = Vector256.Abs(x);
                Vector256<float> z = ExpOperator<float>.Invoke(Vector256.Create(-2f) * y) - Vector256.Create(1f);
                Vector256<uint> sign = x.AsUInt32() & Vector256.Create(~SIGN_MASK);
                return (sign ^ (-z / (z + Vector256.Create(2f))).AsUInt32()).AsSingle().As<float, T>();
            }

            public static Vector512<T> Invoke(Vector512<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector512<float> x = t.AsSingle();

                Vector512<float> y = Vector512.Abs(x);
                Vector512<float> z = ExpOperator<float>.Invoke(Vector512.Create(-2f) * y) - Vector512.Create(1f);
                Vector512<uint> sign = x.AsUInt32() & Vector512.Create(~SIGN_MASK);
                return (sign ^ (-z / (z + Vector512.Create(2f))).AsUInt32()).AsSingle().As<float, T>();
            }
        }

        /// <summary>T.Log(x)</summary>
        internal readonly struct LogOperator<T> : IUnaryOperator<T>
            where T : ILogarithmicFunctions<T>
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

            public static bool Vectorizable => typeof(T) == typeof(float);

            public static T Invoke(T x) => T.Log(x);

            public static Vector128<T> Invoke(Vector128<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector128<float> x = t.AsSingle();

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
                ).As<float, T>();
            }

            public static Vector256<T> Invoke(Vector256<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector256<float> x = t.AsSingle();

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
                ).As<float, T>();
            }

            public static Vector512<T> Invoke(Vector512<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector512<float> x = t.AsSingle();

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
                ).As<float, T>();
            }
        }

        /// <summary>T.Log2(x)</summary>
        internal readonly struct Log2Operator<T> : IUnaryOperator<T>
            where T : ILogarithmicFunctions<T>
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

            public static bool Vectorizable => typeof(T) == typeof(float);

            public static T Invoke(T x) => T.Log2(x);

            public static Vector128<T> Invoke(Vector128<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector128<float> x = t.AsSingle();

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
                ).As<float, T>();
            }

            public static Vector256<T> Invoke(Vector256<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector256<float> x = t.AsSingle();

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
                ).As<float, T>();
            }

            public static Vector512<T> Invoke(Vector512<T> t)
            {
                Debug.Assert(typeof(T) == typeof(float));
                Vector512<float> x = t.AsSingle();

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
                ).As<float, T>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<T> ElementWiseSelect<T>(Vector128<T> mask, Vector128<T> left, Vector128<T> right)
        {
            if (Sse41.IsSupported)
            {
                if (typeof(T) == typeof(byte)) return Sse41.BlendVariable(left.AsByte(), right.AsByte(), (~mask).AsByte()).As<byte, T>();
                if (typeof(T) == typeof(sbyte)) return Sse41.BlendVariable(left.AsSByte(), right.AsSByte(), (~mask).AsSByte()).As<sbyte, T>();
                if (typeof(T) == typeof(ushort)) return Sse41.BlendVariable(left.AsUInt16(), right.AsUInt16(), (~mask).AsUInt16()).As<ushort, T>();
                if (typeof(T) == typeof(short)) return Sse41.BlendVariable(left.AsInt16(), right.AsInt16(), (~mask).AsInt16()).As<short, T>();
                if (typeof(T) == typeof(uint)) return Sse41.BlendVariable(left.AsUInt32(), right.AsUInt32(), (~mask).AsUInt32()).As<uint, T>();
                if (typeof(T) == typeof(int)) return Sse41.BlendVariable(left.AsInt32(), right.AsInt32(), (~mask).AsInt32()).As<int, T>();
                if (typeof(T) == typeof(ulong)) return Sse41.BlendVariable(left.AsUInt64(), right.AsUInt64(), (~mask).AsUInt64()).As<ulong, T>();
                if (typeof(T) == typeof(long)) return Sse41.BlendVariable(left.AsInt64(), right.AsInt64(), (~mask).AsInt64()).As<long, T>();
                if (typeof(T) == typeof(float)) return Sse41.BlendVariable(left.AsSingle(), right.AsSingle(), (~mask).AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Sse41.BlendVariable(left.AsDouble(), right.AsDouble(), (~mask).AsDouble()).As<double, T>();
            }

            return Vector128.ConditionalSelect(mask, left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<T> ElementWiseSelect<T>(Vector256<T> mask, Vector256<T> left, Vector256<T> right)
        {
            if (Avx2.IsSupported)
            {
                if (typeof(T) == typeof(byte)) return Avx2.BlendVariable(left.AsByte(), right.AsByte(), (~mask).AsByte()).As<byte, T>();
                if (typeof(T) == typeof(sbyte)) return Avx2.BlendVariable(left.AsSByte(), right.AsSByte(), (~mask).AsSByte()).As<sbyte, T>();
                if (typeof(T) == typeof(ushort)) return Avx2.BlendVariable(left.AsUInt16(), right.AsUInt16(), (~mask).AsUInt16()).As<ushort, T>();
                if (typeof(T) == typeof(short)) return Avx2.BlendVariable(left.AsInt16(), right.AsInt16(), (~mask).AsInt16()).As<short, T>();
                if (typeof(T) == typeof(uint)) return Avx2.BlendVariable(left.AsUInt32(), right.AsUInt32(), (~mask).AsUInt32()).As<uint, T>();
                if (typeof(T) == typeof(int)) return Avx2.BlendVariable(left.AsInt32(), right.AsInt32(), (~mask).AsInt32()).As<int, T>();
                if (typeof(T) == typeof(ulong)) return Avx2.BlendVariable(left.AsUInt64(), right.AsUInt64(), (~mask).AsUInt64()).As<ulong, T>();
                if (typeof(T) == typeof(long)) return Avx2.BlendVariable(left.AsInt64(), right.AsInt64(), (~mask).AsInt64()).As<long, T>();
                if (typeof(T) == typeof(float)) return Avx2.BlendVariable(left.AsSingle(), right.AsSingle(), (~mask).AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Avx2.BlendVariable(left.AsDouble(), right.AsDouble(), (~mask).AsDouble()).As<double, T>();
            }

            return Vector256.ConditionalSelect(mask, left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<T> ElementWiseSelect<T>(Vector512<T> mask, Vector512<T> left, Vector512<T> right)
        {
            if (Avx512F.IsSupported)
            {
                if (typeof(T) == typeof(uint)) return Avx512F.BlendVariable(left.AsUInt32(), right.AsUInt32(), (~mask).AsUInt32()).As<uint, T>();
                if (typeof(T) == typeof(int)) return Avx512F.BlendVariable(left.AsInt32(), right.AsInt32(), (~mask).AsInt32()).As<int, T>();
                if (typeof(T) == typeof(ulong)) return Avx512F.BlendVariable(left.AsUInt64(), right.AsUInt64(), (~mask).AsUInt64()).As<ulong, T>();
                if (typeof(T) == typeof(long)) return Avx512F.BlendVariable(left.AsInt64(), right.AsInt64(), (~mask).AsInt64()).As<long, T>();
                if (typeof(T) == typeof(float)) return Avx512F.BlendVariable(left.AsSingle(), right.AsSingle(), (~mask).AsSingle()).As<float, T>();
                if (typeof(T) == typeof(double)) return Avx512F.BlendVariable(left.AsDouble(), right.AsDouble(), (~mask).AsDouble()).As<double, T>();
            }

            return Vector512.ConditionalSelect(mask, left, right);
        }

        /// <summary>1f / (1f + MathF.Exp(-x))</summary>
        internal readonly struct SigmoidOperator<T> : IUnaryOperator<T> where T : IExponentialFunctions<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float);
            public static T Invoke(T x) => T.One / (T.One + T.Exp(-x));
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128.Create(T.One) / (Vector128.Create(T.One) + ExpOperator<T>.Invoke(-x));
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256.Create(T.One) / (Vector256.Create(T.One) + ExpOperator<T>.Invoke(-x));
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512.Create(T.One) / (Vector512.Create(T.One) + ExpOperator<T>.Invoke(-x));
        }

        /// <summary>Operator that takes one input value and returns a single value.</summary>
        private interface IUnaryOperator<T>
        {
            static abstract bool Vectorizable { get; }
            static abstract T Invoke(T x);
            static abstract Vector128<T> Invoke(Vector128<T> x);
            static abstract Vector256<T> Invoke(Vector256<T> x);
            static abstract Vector512<T> Invoke(Vector512<T> x);
        }

        /// <summary>Operator that takes two input values and returns a single value.</summary>
        private interface IBinaryOperator<T>
        {
            static abstract T Invoke(T x, T y);
            static abstract Vector128<T> Invoke(Vector128<T> x, Vector128<T> y);
            static abstract Vector256<T> Invoke(Vector256<T> x, Vector256<T> y);
            static abstract Vector512<T> Invoke(Vector512<T> x, Vector512<T> y);
        }

        /// <summary><see cref="IBinaryOperator{T}"/> that specializes horizontal aggregation of all elements in a vector.</summary>
        private interface IAggregationOperator<T> : IBinaryOperator<T>
        {
            static abstract T Invoke(Vector128<T> x);
            static abstract T Invoke(Vector256<T> x);
            static abstract T Invoke(Vector512<T> x);

            static virtual T IdentityValue => throw new NotSupportedException();
        }

        /// <summary>Operator that takes three input values and returns a single value.</summary>
        private interface ITernaryOperator<T>
        {
            static abstract T Invoke(T x, T y, T z);
            static abstract Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> z);
            static abstract Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> z);
            static abstract Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> z);
        }
    }
}
