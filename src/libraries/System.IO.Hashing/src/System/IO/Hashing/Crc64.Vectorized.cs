// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using static System.IO.Hashing.VectorHelper;

namespace System.IO.Hashing
{
    public partial class Crc64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ulong> LoadFromSource(ref byte source, nuint elementOffset)
        {
            Vector128<byte> vector = Vector128.LoadUnsafe(ref source, elementOffset);

            if (BitConverter.IsLittleEndian)
            {
                // Reverse the byte order.

                // SSSE3 is required to get PSHUFB acceleration for Vector128.Shuffle on x86/x64.
                // However, the gains from vectorizing the rest of the operations seem to to be
                // greater than the added cost of emulating the shuffle, so we don't require SSSE3 support.
                vector = Vector128.Shuffle(vector,
                    Vector128.Create((byte)0x0F, 0x0E, 0x0D, 0x0C, 0x0B, 0x0A, 0x09, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03,
                        0x02, 0x01, 0x00));
            }

            return vector.AsUInt64();
        }

        // All of these checks except the length check are elided by JIT, so the JITted implementation
        // will be either a return false or a length check against a constant. This means this method
        // should be inlined into the caller.
        private static bool CanBeVectorized(ReadOnlySpan<byte> source) => VectorHelper.IsSupported && source.Length >= Vector128<byte>.Count;

        // Processes the bytes in source in 128 byte chunks using intrinsics, followed by processing 16
        // byte chunks, and then processing remaining bytes individually. Requires at least 16 bytes of data.
        // Requires little endian byte order and support for PCLMULQDQ intrinsics on Intel architecture
        // or AES and AdvSimd intrinsics on ARM architecture. Based on the algorithm put forth in the Intel paper
        // "Fast CRC Computation for Generic Polynomials Using PCLMULQDQ Instruction" in December, 2009 and the
        // Intel reference implementation.
        // https://github.com/intel/isa-l/blob/33a2d9484595c2d6516c920ce39a694c144ddf69/crc/crc64_ecma_norm_by8.asm
        private static ulong UpdateVectorized(ulong crc, ReadOnlySpan<byte> source)
        {
            Debug.Assert(CanBeVectorized(source), "source cannot be vectorized.");

            // Work with a reference to where we're at in the ReadOnlySpan and a local length
            // to avoid extraneous range checks.
            ref byte srcRef = ref MemoryMarshal.GetReference(source);
            int length = source.Length;

            Vector128<ulong> x7; // Accumulator for the new CRC
            Vector128<ulong> kConstants; // Used to store reused constants

            if (length >= Vector128<byte>.Count * 16) // At least 256 bytes
            {
                // Load the first 128 bytes
                Vector128<ulong> x0 = LoadFromSource(ref srcRef, 0);
                Vector128<ulong> x1 = LoadFromSource(ref srcRef, 16);
                Vector128<ulong> x2 = LoadFromSource(ref srcRef, 32);
                Vector128<ulong> x3 = LoadFromSource(ref srcRef, 48);
                Vector128<ulong> x4 = LoadFromSource(ref srcRef, 64);
                Vector128<ulong> x5 = LoadFromSource(ref srcRef, 80);
                Vector128<ulong> x6 = LoadFromSource(ref srcRef, 96);
                x7 = LoadFromSource(ref srcRef, 112);

                srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count * 8);
                length -= Vector128<byte>.Count * 8;

                // Load and XOR the initial CRC value
                // CRC value does not need to be byte-reflected, but it needs to be moved to the high part of the register.
                // because data will be byte-reflected and will align with initial crc at correct place.
                x0 ^= ShiftLowerToUpper(Vector128.CreateScalar(crc));

                kConstants = Vector128.Create(0x5cf79dea9ac37d6UL, 0x001067e571d7d5c2UL); // k3, k4

                // Parallel fold blocks of 128
                do
                {
                    Vector128<ulong> y1 = LoadFromSource(ref srcRef, 0);
                    Vector128<ulong> y2 = LoadFromSource(ref srcRef, 16);
                    x0 = FoldPolynomialPair(y1, x0, kConstants);
                    x1 = FoldPolynomialPair(y2, x1, kConstants);

                    y1 = LoadFromSource(ref srcRef, 32);
                    y2 = LoadFromSource(ref srcRef, 48);
                    x2 = FoldPolynomialPair(y1, x2, kConstants);
                    x3 = FoldPolynomialPair(y2, x3, kConstants);

                    y1 = LoadFromSource(ref srcRef, 64);
                    y2 = LoadFromSource(ref srcRef, 80);
                    x4 = FoldPolynomialPair(y1, x4, kConstants);
                    x5 = FoldPolynomialPair(y2, x5, kConstants);

                    y1 = LoadFromSource(ref srcRef, 96);
                    y2 = LoadFromSource(ref srcRef, 112);
                    x6 = FoldPolynomialPair(y1, x6, kConstants);
                    x7 = FoldPolynomialPair(y2, x7, kConstants);

                    srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count * 8);
                    length -= Vector128<byte>.Count * 8;
                } while (length >= Vector128<byte>.Count * 8);

                // Fold into 128-bits in x7
                x7 = FoldPolynomialPair(x7, x0, Vector128.Create(0xe464f4df5fb60ac1UL, 0xb649c5b35a759cf2UL)); // k9, k10
                x7 = FoldPolynomialPair(x7, x1, Vector128.Create(0x9af04e1eff82d0ddUL, 0x6e82e609297f8fe8UL)); // k11, k12
                x7 = FoldPolynomialPair(x7, x2, Vector128.Create(0x97c516e98bd2e73UL, 0xb76477b31e22e7bUL)); // k13, k14
                x7 = FoldPolynomialPair(x7, x3, Vector128.Create(0x5f6843ca540df020UL, 0xddf4b6981205b83fUL)); // k15, k16
                x7 = FoldPolynomialPair(x7, x4, Vector128.Create(0x54819d8713758b2cUL, 0x4a6b90073eb0af5aUL)); // k17, k18
                x7 = FoldPolynomialPair(x7, x5, Vector128.Create(0x571bee0a227ef92bUL, 0x44bef2a201b5200cUL)); // k19, k20
                x7 = FoldPolynomialPair(x7, x6, Vector128.Create(0x5f5c3c7eb52fab6UL, 0x4eb938a7d257740eUL)); // k1, k2
            }
            else
            {
                // For shorter sources just load the first vector and XOR with the CRC
                Debug.Assert(length >= 16);

                x7 = LoadFromSource(ref srcRef, 0);

                // Load and XOR the initial CRC value
                // CRC value does not need to be byte-reflected, but it needs to be moved to the high part of the register.
                // because the data will be byte-reflected and will align with initial crc at correct place.
                x7 ^= ShiftLowerToUpper(Vector128.CreateScalar(crc));

                srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                length -= Vector128<byte>.Count;
            }

            // Single fold blocks of 16, if any, into x7
            while (length >= Vector128<byte>.Count)
            {
                x7 = FoldPolynomialPair(LoadFromSource(ref srcRef, 0), x7,
                    Vector128.Create(0x5f5c3c7eb52fab6UL, 0x4eb938a7d257740eUL)); // k1, k2

                srcRef = ref Unsafe.Add(ref srcRef, Vector128<byte>.Count);
                length -= Vector128<byte>.Count;
            }

            // Compute CRC of a 128-bit value and fold to the upper 64-bits
            x7 = CarrylessMultiplyLeftUpperRightLower(x7, Vector128.CreateScalar(0x5f5c3c7eb52fab6UL)) ^ // k5
                 ShiftLowerToUpper(x7);

            // Barrett reduction
            kConstants = Vector128.Create(0x578d29d06cc4f872UL, 0x42f0e1eba9ea3693UL); // k7, k8
            Vector128<ulong> temp = x7;
            x7 = CarrylessMultiplyLeftUpperRightLower(x7, kConstants) ^ (x7 & Vector128.Create(0UL, ~0UL));
            x7 = CarrylessMultiplyUpper(x7, kConstants);
            x7 ^= temp;

            // Process the remaining bytes, if any
            ulong result = x7.GetElement(0);
            return length > 0
                ? UpdateScalar(result, MemoryMarshal.CreateReadOnlySpan(ref srcRef, length))
                : result;
        }
    }
}
