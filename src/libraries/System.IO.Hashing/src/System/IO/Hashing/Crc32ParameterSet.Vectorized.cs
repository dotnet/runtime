// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using static System.IO.Hashing.VectorHelper;

namespace System.IO.Hashing
{
    public partial class Crc32ParameterSet
    {
        private partial class ForwardCrc32
        {
            private Vector128<ulong> _k1k2;
            private Vector128<ulong> _k3k4;
            private Vector128<ulong> _foldConstants;
            private ulong _k6;
            private ulong _mu;
            private bool _canVectorize;

            partial void InitializeVectorized()
            {
                if (!BitConverter.IsLittleEndian || !VectorHelper.IsSupported)
                {
                    return;
                }

                ulong fullPoly = 1UL << 32 | Polynomial;

                ulong k1 = CrcPolynomialHelper.ComputeFoldingConstantCrc32(fullPoly, 4 * 128 + 64);
                ulong k2 = CrcPolynomialHelper.ComputeFoldingConstantCrc32(fullPoly, 4 * 128);
                ulong k3 = CrcPolynomialHelper.ComputeFoldingConstantCrc32(fullPoly, 128 + 64);
                ulong k4 = CrcPolynomialHelper.ComputeFoldingConstantCrc32(fullPoly, 128);
                ulong k5 = CrcPolynomialHelper.ComputeFoldingConstantCrc32(fullPoly, 96);
                ulong k6 = CrcPolynomialHelper.ComputeFoldingConstantCrc32(fullPoly, 64);

                _k1k2 = Vector128.Create(k2, k1);
                _k3k4 = Vector128.Create(k4, k3);
                _k6 = k6;

                _foldConstants = Vector128.Create(
                    CrcPolynomialHelper.ComputeFoldingConstantCrc32(fullPoly, 32),
                    k5);

                _mu = CrcPolynomialHelper.ComputeBarrettConstantCrc32(fullPoly);

                _canVectorize = true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<ulong> LoadReversed(ref byte source, nuint elementOffset)
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

            partial void UpdateVectorized(ref uint crc, ReadOnlySpan<byte> source, ref int bytesConsumed)
            {
                if (!_canVectorize || source.Length < Vector128<byte>.Count)
                {
                    return;
                }

                crc = UpdateVectorizedCore(crc, source, out bytesConsumed);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private uint UpdateVectorizedCore(uint crc, ReadOnlySpan<byte> source, out int bytesConsumed)
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

                    x1 ^= ShiftLowerToUpper(Vector128.CreateScalar((ulong)crc << 32));

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
                    x1 ^= ShiftLowerToUpper(Vector128.CreateScalar((ulong)crc << 32));

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                    length -= Vector128<byte>.Count;
                }

                while (length >= Vector128<byte>.Count)
                {
                    x1 = FoldPolynomialPair(LoadReversed(ref srcRef, 0), x1, _k3k4);

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                    length -= Vector128<byte>.Count;
                }

                x1 = FoldPolynomialPair(Vector128<ulong>.Zero, x1, _foldConstants);

                Vector128<ulong> lowerMask = Vector128.Create(~0UL, 0UL);
                x1 = CarrylessMultiplyLeftUpperRightLower(x1, Vector128.CreateScalar(_k6)) ^ (x1 & lowerMask);

                Vector128<ulong> bitmask = Vector128.Create(~0, 0, ~0, 0).AsUInt64();
                Vector128<ulong> temp = x1;
                x1 = ShiftRightBytesInVector(x1, 4) & bitmask;
                x1 = CarrylessMultiplyLower(x1, Vector128.CreateScalar(_mu));
                x1 = ShiftRightBytesInVector(x1, 4) & bitmask;

                ulong polyFull = 1UL << 32 | Polynomial;
                x1 = CarrylessMultiplyLower(x1, Vector128.CreateScalar(polyFull));
                x1 ^= temp;

                bytesConsumed = source.Length - length;

                return x1.AsUInt32().GetElement(0);
            }
        }
    }
}

#endif
