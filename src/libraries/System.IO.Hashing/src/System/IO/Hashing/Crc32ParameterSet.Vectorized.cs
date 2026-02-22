// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using static System.IO.Hashing.VectorHelper;

namespace System.IO.Hashing
{
    public partial class Crc32ParameterSet
    {
        private partial class ReflectedTableBasedCrc32
        {
            // Precomputed constants for PCLMULQDQ-based folding.
            private bool _canVectorize;
            private ulong _k1, _k2;    // 4-way fold constants
            private ulong _k3, _k4;    // 1-way fold constants
            private ulong _k5;          // 128-to-64 fold constant
            private ulong _pStar, _mu;  // Barrett reduction constants

            partial void InitializeVectorized()
            {
                if (!BitConverter.IsLittleEndian || !VectorHelper.IsSupported)
                    return;

                ulong polynomial = Polynomial;
                CrcPolynomialHelper.UInt640 fullPoly = new((1UL << 32) | polynomial);
                int polyDeg = 32;

                // Reflected folding constants: reverse_bits(x^power mod fullPoly, polyDeg+1)
                _k1 = CrcPolynomialHelper.ReverseBits(
                    CrcPolynomialHelper.ComputeFoldingConstant(fullPoly, 4 * 128 + polyDeg), polyDeg + 1);
                _k2 = CrcPolynomialHelper.ReverseBits(
                    CrcPolynomialHelper.ComputeFoldingConstant(fullPoly, 4 * 128 - polyDeg), polyDeg + 1);
                _k3 = CrcPolynomialHelper.ReverseBits(
                    CrcPolynomialHelper.ComputeFoldingConstant(fullPoly, 128 + polyDeg), polyDeg + 1);
                _k4 = CrcPolynomialHelper.ReverseBits(
                    CrcPolynomialHelper.ComputeFoldingConstant(fullPoly, 128 - polyDeg), polyDeg + 1);
                _k5 = CrcPolynomialHelper.ReverseBits(
                    CrcPolynomialHelper.ComputeFoldingConstant(fullPoly, 2 * polyDeg), polyDeg + 1);

                // Barrett reduction constants
                _pStar = CrcPolynomialHelper.ReverseBits((1UL << polyDeg) | polynomial, polyDeg + 1);
                _mu = CrcPolynomialHelper.ReverseBits(
                    CrcPolynomialHelper.ComputeBarrettConstant(fullPoly, 2 * polyDeg), polyDeg + 1);

                _canVectorize = true;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private uint UpdateVectorized(uint crc, ReadOnlySpan<byte> source)
            {
                Debug.Assert(_canVectorize);
                Debug.Assert(source.Length >= Vector128<byte>.Count);

                ref byte srcRef = ref MemoryMarshal.GetReference(source);
                int length = source.Length;

                Vector128<ulong> kConstants;
                Vector128<ulong> x1;
                Vector128<ulong> x2;

                if (length >= Vector128<byte>.Count * 8)
                {
                    x1 = Vector128.LoadUnsafe(ref srcRef).AsUInt64();
                    x2 = Vector128.LoadUnsafe(ref srcRef, 16).AsUInt64();
                    Vector128<ulong> x3 = Vector128.LoadUnsafe(ref srcRef, 32).AsUInt64();
                    Vector128<ulong> x4 = Vector128.LoadUnsafe(ref srcRef, 48).AsUInt64();

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count * 4);
                    length -= Vector128<byte>.Count * 4;

                    x1 ^= Vector128.CreateScalar(crc).AsUInt64();

                    kConstants = Vector128.Create(_k1, _k2);

                    do
                    {
                        Vector128<ulong> y5 = Vector128.LoadUnsafe(ref srcRef).AsUInt64();
                        Vector128<ulong> y6 = Vector128.LoadUnsafe(ref srcRef, 16).AsUInt64();
                        Vector128<ulong> y7 = Vector128.LoadUnsafe(ref srcRef, 32).AsUInt64();
                        Vector128<ulong> y8 = Vector128.LoadUnsafe(ref srcRef, 48).AsUInt64();

                        x1 = FoldPolynomialPair(y5, x1, kConstants);
                        x2 = FoldPolynomialPair(y6, x2, kConstants);
                        x3 = FoldPolynomialPair(y7, x3, kConstants);
                        x4 = FoldPolynomialPair(y8, x4, kConstants);

                        srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count * 4);
                        length -= Vector128<byte>.Count * 4;
                    } while (length >= Vector128<byte>.Count * 4);

                    kConstants = Vector128.Create(_k3, _k4);
                    x1 = FoldPolynomialPair(x2, x1, kConstants);
                    x1 = FoldPolynomialPair(x3, x1, kConstants);
                    x1 = FoldPolynomialPair(x4, x1, kConstants);
                }
                else
                {
                    Debug.Assert(length >= 16);

                    x1 = Vector128.LoadUnsafe(ref srcRef).AsUInt64();
                    x1 ^= Vector128.CreateScalar(crc).AsUInt64();

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                    length -= Vector128<byte>.Count;
                }

                kConstants = Vector128.Create(_k3, _k4);

                while (length >= Vector128<byte>.Count)
                {
                    x1 = FoldPolynomialPair(Vector128.LoadUnsafe(ref srcRef).AsUInt64(), x1, kConstants);

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                    length -= Vector128<byte>.Count;
                }

                // Fold 128 bits to 64 bits.
                Vector128<ulong> bitmask = Vector128.Create(~0, 0, ~0, 0).AsUInt64();
                x1 = ShiftRightBytesInVector(x1, 8) ^
                     CarrylessMultiplyLower(x1, Vector128.CreateScalar(_k4));
                x1 = CarrylessMultiplyLower(x1 & bitmask, Vector128.CreateScalar(_k5)) ^
                     ShiftRightBytesInVector(x1, 4);

                // Reduce to 32 bits via Barrett reduction.
                kConstants = Vector128.Create(_pStar, _mu);
                x2 = CarrylessMultiplyLeftLowerRightUpper(x1 & bitmask, kConstants) & bitmask;
                x2 = CarrylessMultiplyLower(x2, kConstants);
                x1 ^= x2;

                uint result = x1.AsUInt32().GetElement(1);
                return length > 0
                    ? UpdateScalar(result, MemoryMarshal.CreateReadOnlySpan(ref srcRef, length))
                    : result;
            }
        }

        private partial class ForwardTableBasedCrc32
        {
            // Precomputed constants for PCLMULQDQ-based folding.
            private bool _canVectorize;
            private ulong _k1, _k2;    // 4-way fold constants
            private ulong _k3, _k4;    // 1-way fold constants
            private ulong _k5;          // 128-to-64 fold constant
            private ulong _poly, _mu;   // Barrett reduction constants

            partial void InitializeVectorized()
            {
                if (!VectorHelper.IsSupported)
                    return;

                ulong polynomial = Polynomial;
                CrcPolynomialHelper.UInt640 fullPoly = new((1UL << 32) | polynomial);

                // Forward folding constants: x^power mod fullPoly
                _k1 = CrcPolynomialHelper.ComputeFoldingConstant(fullPoly, 4 * 128);
                _k2 = CrcPolynomialHelper.ComputeFoldingConstant(fullPoly, 4 * 128 + 64);
                _k3 = CrcPolynomialHelper.ComputeFoldingConstant(fullPoly, 128);
                _k4 = CrcPolynomialHelper.ComputeFoldingConstant(fullPoly, 128 + 64);
                _k5 = CrcPolynomialHelper.ComputeFoldingConstant(fullPoly, 128);

                // Barrett reduction constants
                _poly = polynomial;
                _mu = CrcPolynomialHelper.ComputeBarrettConstant(fullPoly, 2 * 32) & 0xFFFFFFFF;

                _canVectorize = true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<ulong> LoadFromSourceByteSwapped(ref byte source, nuint elementOffset)
            {
                Vector128<byte> vector = Vector128.LoadUnsafe(ref source, elementOffset);

                if (BitConverter.IsLittleEndian)
                {
                    vector = Vector128.Shuffle(vector,
                        Vector128.Create((byte)0x0F, 0x0E, 0x0D, 0x0C, 0x0B, 0x0A, 0x09, 0x08,
                                         0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00));
                }

                return vector.AsUInt64();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private uint UpdateVectorized(uint crc, ReadOnlySpan<byte> source)
            {
                Debug.Assert(_canVectorize);
                Debug.Assert(source.Length >= Vector128<byte>.Count);

                ref byte srcRef = ref MemoryMarshal.GetReference(source);
                int length = source.Length;

                Vector128<ulong> x7;
                Vector128<ulong> kConstants;

                if (length >= Vector128<byte>.Count * 8)
                {
                    Vector128<ulong> x0 = LoadFromSourceByteSwapped(ref srcRef, 0);
                    Vector128<ulong> x1 = LoadFromSourceByteSwapped(ref srcRef, 16);
                    Vector128<ulong> x2 = LoadFromSourceByteSwapped(ref srcRef, 32);
                    x7 = LoadFromSourceByteSwapped(ref srcRef, 48);

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count * 4);
                    length -= Vector128<byte>.Count * 4;

                    x0 ^= ShiftLowerToUpper(Vector128.CreateScalar((ulong)crc));

                    kConstants = Vector128.Create(_k1, _k2);

                    do
                    {
                        Vector128<ulong> y1 = LoadFromSourceByteSwapped(ref srcRef, 0);
                        Vector128<ulong> y2 = LoadFromSourceByteSwapped(ref srcRef, 16);
                        Vector128<ulong> y3 = LoadFromSourceByteSwapped(ref srcRef, 32);
                        Vector128<ulong> y4 = LoadFromSourceByteSwapped(ref srcRef, 48);

                        x0 = FoldPolynomialPair(y1, x0, kConstants);
                        x1 = FoldPolynomialPair(y2, x1, kConstants);
                        x2 = FoldPolynomialPair(y3, x2, kConstants);
                        x7 = FoldPolynomialPair(y4, x7, kConstants);

                        srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count * 4);
                        length -= Vector128<byte>.Count * 4;
                    } while (length >= Vector128<byte>.Count * 4);

                    kConstants = Vector128.Create(_k3, _k4);
                    x7 = FoldPolynomialPair(x7, x0, kConstants);
                    x7 = FoldPolynomialPair(x7, x1, kConstants);
                    x7 = FoldPolynomialPair(x7, x2, kConstants);
                }
                else
                {
                    Debug.Assert(length >= 16);

                    x7 = LoadFromSourceByteSwapped(ref srcRef, 0);
                    x7 ^= ShiftLowerToUpper(Vector128.CreateScalar((ulong)crc));

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                    length -= Vector128<byte>.Count;
                }

                kConstants = Vector128.Create(_k3, _k4);

                while (length >= Vector128<byte>.Count)
                {
                    x7 = FoldPolynomialPair(LoadFromSourceByteSwapped(ref srcRef, 0), x7, kConstants);

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                    length -= Vector128<byte>.Count;
                }

                // Compute CRC of a 128-bit value and fold to the upper 64-bits.
                x7 = CarrylessMultiplyLeftUpperRightLower(x7, Vector128.CreateScalar(_k5)) ^
                     ShiftLowerToUpper(x7);

                // Barrett reduction.
                kConstants = Vector128.Create(_mu, _poly);
                Vector128<ulong> temp = x7;
                x7 = CarrylessMultiplyLeftUpperRightLower(x7, kConstants) ^ (x7 & Vector128.Create(0UL, ~0UL));
                x7 = CarrylessMultiplyUpper(x7, kConstants);
                x7 ^= temp;

                uint result = (uint)x7.GetElement(0);
                return length > 0
                    ? UpdateScalar(result, MemoryMarshal.CreateReadOnlySpan(ref srcRef, length))
                    : result;
            }
        }
    }
}

#endif
