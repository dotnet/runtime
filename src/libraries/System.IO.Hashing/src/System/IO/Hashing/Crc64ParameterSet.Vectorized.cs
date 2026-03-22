// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using static System.IO.Hashing.VectorHelper;

namespace System.IO.Hashing
{
    public partial class Crc64ParameterSet
    {
        private partial class ReflectedCrc64
        {
            private Vector128<ulong> _k1k2;
            private Vector128<ulong> _k3k4;
            private ulong _k4;
            private Vector128<ulong> _muPoly;

            partial void InitializeVectorized(ref bool canVectorize)
            {
                if (!BitConverter.IsLittleEndian || !VectorHelper.IsSupported)
                {
                    return;
                }

                ulong reducedPolynomial = Polynomial;

                // The reciprocal polynomial Q is reverse_bits_65(P) where P = x^64 + reducedPolynomial.
                // Q_lower64 = 1 | (ReverseBits(reducedPolynomial) << 1).
                ulong reciprocalLow64 = 1UL | (ReverseBits(reducedPolynomial) << 1);

                // Folding constants: compute x^n mod P, then reflect within 65 bits.
                // If the reflected value overflows 64 bits, reduce mod Q.
                ulong k1 = ReflectFoldingConstant(reducedPolynomial, 4 * 128 + 64, reciprocalLow64);
                ulong k2 = ReflectFoldingConstant(reducedPolynomial, 4 * 128, reciprocalLow64);
                ulong k3 = ReflectFoldingConstant(reducedPolynomial, 128 + 64, reciprocalLow64);
                ulong k4 = ReflectFoldingConstant(reducedPolynomial, 128, reciprocalLow64);

                // Barrett mu: the full 65-bit Barrett quotient, reflected within 65 bits, lower 64 bits.
                ulong barrettLow64 = CrcPolynomialHelper.ComputeBarrettConstantCrc64(reducedPolynomial);
                ulong mu = 1UL | (ReverseBits(barrettLow64) << 1);

                // Reflected polynomial: reciprocal lower 64 with bit 0 cleared (x^64 and x^0 terms are implicit).
                ulong reflectedPoly = ReverseBits(reducedPolynomial) << 1;

                _k1k2 = Vector128.Create(k1, k2);
                _k3k4 = Vector128.Create(k3, k4);
                _k4 = k4;
                _muPoly = Vector128.Create(mu, reflectedPoly);

                canVectorize = true;

                static ulong ReflectFoldingConstant(ulong reducedPolynomial, int power, ulong reciprocalLow64)
                {
                    ulong foldValue = CrcPolynomialHelper.ComputeFoldingConstantCrc64(reducedPolynomial, power);

                    // Reflect within 65 bits: lower 64 bits = ReverseBits(value) << 1, bit 64 = value & 1.
                    ulong reflected = ReverseBits(foldValue) << 1;

                    if ((foldValue & 1) != 0)
                    {
                        // Bit 64 is set, reduce by XOR with the reciprocal polynomial.
                        reflected ^= reciprocalLow64;
                    }

                    return reflected;
                }
            }

            partial void UpdateVectorized(ref ulong crc, ReadOnlySpan<byte> source, ref int bytesConsumed)
            {
                if (!_canVectorize || source.Length < Vector128<byte>.Count)
                {
                    return;
                }

                crc = UpdateVectorizedCore(crc, source, out bytesConsumed);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private ulong UpdateVectorizedCore(ulong crc, ReadOnlySpan<byte> source, out int bytesConsumed)
            {
                ref byte srcRef = ref MemoryMarshal.GetReference(source);
                int length = source.Length;

                Vector128<ulong> x1;

                if (length >= Vector128<byte>.Count * 4)
                {
                    x1 = Vector128.LoadUnsafe(ref srcRef).AsUInt64();
                    Vector128<ulong> x2 = Vector128.LoadUnsafe(ref srcRef, 16).AsUInt64();
                    Vector128<ulong> x3 = Vector128.LoadUnsafe(ref srcRef, 32).AsUInt64();
                    Vector128<ulong> x4 = Vector128.LoadUnsafe(ref srcRef, 48).AsUInt64();

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count * 4);
                    length -= Vector128<byte>.Count * 4;

                    x1 ^= Vector128.CreateScalar(crc);

                    while (length >= Vector128<byte>.Count * 4)
                    {
                        Vector128<ulong> y5 = Vector128.LoadUnsafe(ref srcRef).AsUInt64();
                        Vector128<ulong> y6 = Vector128.LoadUnsafe(ref srcRef, 16).AsUInt64();
                        Vector128<ulong> y7 = Vector128.LoadUnsafe(ref srcRef, 32).AsUInt64();
                        Vector128<ulong> y8 = Vector128.LoadUnsafe(ref srcRef, 48).AsUInt64();

                        x1 = FoldPolynomialPair(y5, x1, _k1k2);
                        x2 = FoldPolynomialPair(y6, x2, _k1k2);
                        x3 = FoldPolynomialPair(y7, x3, _k1k2);
                        x4 = FoldPolynomialPair(y8, x4, _k1k2);

                        srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count * 4);
                        length -= Vector128<byte>.Count * 4;
                    }

                    x1 = FoldPolynomialPair(x2, x1, _k3k4);
                    x1 = FoldPolynomialPair(x3, x1, _k3k4);
                    x1 = FoldPolynomialPair(x4, x1, _k3k4);
                }
                else
                {
                    x1 = Vector128.LoadUnsafe(ref srcRef).AsUInt64();
                    x1 ^= Vector128.CreateScalar(crc);

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                    length -= Vector128<byte>.Count;
                }

                while (length >= Vector128<byte>.Count)
                {
                    x1 = FoldPolynomialPair(Vector128.LoadUnsafe(ref srcRef).AsUInt64(), x1, _k3k4);

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                    length -= Vector128<byte>.Count;
                }

                // Fold 128→64 bits (reflected: fold lower into upper)
                Vector128<ulong> temp = x1;
                x1 = CarrylessMultiplyLower(x1, Vector128.CreateScalar(_k4));
                x1 ^= ShiftRightBytesInVector(temp, 8);

                // Barrett reduction
                temp = x1;
                x1 = CarrylessMultiplyLower(x1, _muPoly);
                Vector128<ulong> shifted = ShiftLowerToUpper(x1);
                x1 = CarrylessMultiplyLeftLowerRightUpper(x1, _muPoly);
                x1 ^= shifted;
                x1 ^= temp;

                bytesConsumed = source.Length - length;
                return x1.GetElement(1);
            }
        }

        private partial class ForwardCrc64
        {
            private Vector128<ulong> _k1k2;
            private Vector128<ulong> _k3k4;
            private ulong _k4;
            private Vector128<ulong> _muPoly;

            partial void InitializeVectorized(ref bool canVectorize)
            {
                if (!BitConverter.IsLittleEndian || !VectorHelper.IsSupported)
                {
                    return;
                }

                ulong reducedPolynomial = Polynomial;

                ulong k1 = CrcPolynomialHelper.ComputeFoldingConstantCrc64(reducedPolynomial, 4 * 128 + 64);
                ulong k2 = CrcPolynomialHelper.ComputeFoldingConstantCrc64(reducedPolynomial, 4 * 128);
                ulong k3 = CrcPolynomialHelper.ComputeFoldingConstantCrc64(reducedPolynomial, 128 + 64);
                ulong k4 = CrcPolynomialHelper.ComputeFoldingConstantCrc64(reducedPolynomial, 128);
                ulong mu = CrcPolynomialHelper.ComputeBarrettConstantCrc64(reducedPolynomial);

                _k1k2 = Vector128.Create(k2, k1);
                _k3k4 = Vector128.Create(k4, k3);
                _k4 = k4;
                _muPoly = Vector128.Create(mu, reducedPolynomial);

                canVectorize = true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<ulong> LoadReversed(ref byte source, nuint elementOffset)
            {
                Vector128<byte> vector = Vector128.LoadUnsafe(ref source, elementOffset);

                if (BitConverter.IsLittleEndian)
                {
                    vector = Vector128.Shuffle(
                        vector,
                        Vector128.Create(
                            (byte)0x0F, 0x0E, 0x0D, 0x0C, 0x0B, 0x0A, 0x09, 0x08,
                            0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00));
                }

                return vector.AsUInt64();
            }

            partial void UpdateVectorized(ref ulong crc, ReadOnlySpan<byte> source, ref int bytesConsumed)
            {
                if (!_canVectorize || source.Length < Vector128<byte>.Count)
                {
                    return;
                }

                crc = UpdateVectorizedCore(crc, source, out bytesConsumed);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private ulong UpdateVectorizedCore(ulong crc, ReadOnlySpan<byte> source, out int bytesConsumed)
            {
                ref byte srcRef = ref MemoryMarshal.GetReference(source);
                int length = source.Length;

                Vector128<ulong> x1;

                if (length >= Vector128<byte>.Count * 4)
                {
                    x1 = LoadReversed(ref srcRef, 0);
                    Vector128<ulong> x2 = LoadReversed(ref srcRef, 16);
                    Vector128<ulong> x3 = LoadReversed(ref srcRef, 32);
                    Vector128<ulong> x4 = LoadReversed(ref srcRef, 48);

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count * 4);
                    length -= Vector128<byte>.Count * 4;

                    x1 ^= ShiftLowerToUpper(Vector128.CreateScalar(crc));

                    while (length >= Vector128<byte>.Count * 4)
                    {
                        Vector128<ulong> y5 = LoadReversed(ref srcRef, 0);
                        Vector128<ulong> y6 = LoadReversed(ref srcRef, 16);
                        Vector128<ulong> y7 = LoadReversed(ref srcRef, 32);
                        Vector128<ulong> y8 = LoadReversed(ref srcRef, 48);

                        x1 = FoldPolynomialPair(y5, x1, _k1k2);
                        x2 = FoldPolynomialPair(y6, x2, _k1k2);
                        x3 = FoldPolynomialPair(y7, x3, _k1k2);
                        x4 = FoldPolynomialPair(y8, x4, _k1k2);

                        srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count * 4);
                        length -= Vector128<byte>.Count * 4;
                    }

                    x1 = FoldPolynomialPair(x2, x1, _k3k4);
                    x1 = FoldPolynomialPair(x3, x1, _k3k4);
                    x1 = FoldPolynomialPair(x4, x1, _k3k4);
                }
                else
                {
                    x1 = LoadReversed(ref srcRef, 0);
                    x1 ^= ShiftLowerToUpper(Vector128.CreateScalar(crc));

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                    length -= Vector128<byte>.Count;
                }

                while (length >= Vector128<byte>.Count)
                {
                    x1 = FoldPolynomialPair(LoadReversed(ref srcRef, 0), x1, _k3k4);

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                    length -= Vector128<byte>.Count;
                }

                // Fold 128→64 bits (forward: fold upper into lower)
                x1 = CarrylessMultiplyLeftUpperRightLower(
                        x1,
                        Vector128.CreateScalar(_k4)) ^ ShiftLowerToUpper(x1);

                // Barrett reduction
                Vector128<ulong> temp = x1;
                x1 = CarrylessMultiplyLeftUpperRightLower(x1, _muPoly) ^
                     (x1 & Vector128.Create(0UL, ~0UL));
                x1 = CarrylessMultiplyUpper(x1, _muPoly);
                x1 ^= temp;

                bytesConsumed = source.Length - length;
                return x1.GetElement(0);
            }
        }
    }
}

#endif
