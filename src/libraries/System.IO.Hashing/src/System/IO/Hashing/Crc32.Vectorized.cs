// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using static System.IO.Hashing.VectorHelper;

namespace System.IO.Hashing
{
    public partial class Crc32
    {
        // We check for little endian byte order here in case we're ever on ARM in big endian mode.
        // All of these checks except the length check are elided by JIT, so the JITted implementation
        // will be either a return false or a length check against a constant. This means this method
        // should be inlined into the caller.
        private static bool CanBeVectorized(ReadOnlySpan<byte> source) =>
            BitConverter.IsLittleEndian
            && VectorHelper.IsSupported
            // Vectorization can process spans as short as a single vector (16 bytes), but if ARM intrinsics are supported they
            // seem to be more performant for spans less than 8 vectors (128 bytes).
            && source.Length >= Vector128<byte>.Count * (System.Runtime.Intrinsics.Arm.Crc32.IsSupported ? 8 : 1);

        // Processes the bytes in source in 64 byte chunks using carryless/polynomial multiplication intrinsics,
        // followed by processing 16 byte chunks, and then processing remaining bytes individually. Requires
        // little endian byte order and support for PCLMULQDQ intrinsics on Intel architecture or AES and
        // AdvSimd intrinsics on ARM architecture. Based on the algorithm put forth in the Intel paper "Fast CRC
        // Computation for Generic Polynomials Using PCLMULQDQ Instruction" in December, 2009.
        // https://github.com/intel/isa-l/blob/33a2d9484595c2d6516c920ce39a694c144ddf69/crc/crc32_ieee_by4.asm
        // https://github.com/SixLabors/ImageSharp/blob/f4f689ce67ecbcc35cebddba5aacb603e6d1068a/src/ImageSharp/Formats/Png/Zlib/Crc32.cs#L80
        private static uint UpdateVectorized(uint crc, ReadOnlySpan<byte> source)
        {
            Debug.Assert(CanBeVectorized(source), "source cannot be vectorized.");

            // Work with a reference to where we're at in the ReadOnlySpan and a local length
            // to avoid extraneous range checks.
            ref byte srcRef = ref MemoryMarshal.GetReference(source);
            int length = source.Length;

            Vector128<ulong> kConstants;
            Vector128<ulong> x1; // Accumulator for the new CRC
            Vector128<ulong> x2;

            if (length >= Vector128<byte>.Count * 8)
            {
                x1 = Vector128.LoadUnsafe(ref srcRef).AsUInt64();
                x2 = Vector128.LoadUnsafe(ref srcRef, 16).AsUInt64();
                Vector128<ulong> x3 = Vector128.LoadUnsafe(ref srcRef, 32).AsUInt64();
                Vector128<ulong> x4 = Vector128.LoadUnsafe(ref srcRef, 48).AsUInt64();

                srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count * 4);
                length -= Vector128<byte>.Count * 4;

                // Load and XOR the initial CRC value
                x1 ^= Vector128.CreateScalar(crc).AsUInt64();

                kConstants = Vector128.Create(0x0154442bd4UL, 0x01c6e41596UL); // k1, k2

                // Parallel fold blocks of 64, if any.
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

                // Fold into 128-bits.
                kConstants = Vector128.Create(0x01751997d0UL, 0x00ccaa009eUL); // k3, k4
                x1 = FoldPolynomialPair(x2, x1, kConstants);
                x1 = FoldPolynomialPair(x3, x1, kConstants);
                x1 = FoldPolynomialPair(x4, x1, kConstants);
            }
            else
            {
                // For shorter sources just load the first vector and XOR with the CRC
                Debug.Assert(length >= 16);

                x1 = Vector128.LoadUnsafe(ref srcRef).AsUInt64();
                x1 ^= Vector128.CreateScalar(crc).AsUInt64();

                srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                length -= Vector128<byte>.Count;
            }

            // Single fold blocks of 16, if any.
            while (length >= Vector128<byte>.Count)
            {
                x1 = FoldPolynomialPair(Vector128.LoadUnsafe(ref srcRef).AsUInt64(), x1,
                    Vector128.Create(0x01751997d0UL, 0x00ccaa009eUL));

                srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                length -= Vector128<byte>.Count;
            }

            // Fold 128 bits to 64 bits.
            Vector128<ulong> bitmask = Vector128.Create(~0, 0, ~0, 0).AsUInt64();
            x1 = ShiftRightBytesInVector(x1, 8) ^
                 CarrylessMultiplyLower(x1, Vector128.CreateScalar(0x00ccaa009eUL));
            x1 = CarrylessMultiplyLower(x1 & bitmask, Vector128.CreateScalar(0x0163cd6124UL)) ^ // k5, k0
                 ShiftRightBytesInVector(x1, 4);

            // Reduce to 32 bits.
            kConstants = Vector128.Create(0x01db710641UL, 0x01f7011641UL); // polynomial
            x2 = CarrylessMultiplyLeftLowerRightUpper(x1 & bitmask, kConstants) & bitmask;
            x2 = CarrylessMultiplyLower(x2, kConstants);
            x1 ^= x2;

            // Process the remaining bytes, if any
            uint result = x1.AsUInt32().GetElement(1);
            return length > 0
                ? UpdateScalar(result, MemoryMarshal.CreateReadOnlySpan(ref srcRef, length))
                : result;
        }
    }
}
