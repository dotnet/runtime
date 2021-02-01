// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System
{
    internal static class Marvin
    {
        /// <summary>
        /// Convenience method to compute a Marvin hash and collapse it into a 32-bit hash.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeHash32(ReadOnlySpan<byte> data, ulong seed)
        {
            long hash64 = ComputeHash(data, seed);
            return ((int)(hash64 >> 32)) ^ (int)hash64;
        }

        /// <summary>
        /// Computes a 64-hash using the Marvin algorithm.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ComputeHash(ReadOnlySpan<byte> data, ulong seed)
            => ComputeHash(ref MemoryMarshal.GetReference(data), (uint)data.Length, p0: (uint)seed, p1: (uint)(seed >> 32));

        private static unsafe long ComputeHash(ref byte rBuffer, nuint cbBuffer, uint p0, uint p1)
        {
            nuint currentOffset = 0;

            fixed (byte* pbBuffer = &rBuffer)
            {
                // Consume as many 4-byte chunks as possible.

                if (cbBuffer >= 4)
                {
                    nuint stopOffset = cbBuffer & ~(nuint)3;
                    do
                    {
                        p0 += Unsafe.ReadUnaligned<uint>(pbBuffer + currentOffset);
                        currentOffset += 4;
                        Block(ref p0, ref p1);
                    } while (currentOffset < stopOffset);
                }

                // Fewer than 4 bytes remain; drain remaining bytes.

                Debug.Assert(cbBuffer - currentOffset < 4, "Should have 0 - 3 bytes remaining.");
                switch ((int)cbBuffer & 3)
                {
                    case 0:
                        p0 += 0x80u;
                        break;

                    case 1:
                        p0 += 0x8000u | pbBuffer[currentOffset];
                        break;

                    case 2:
                        p0 += 0x800000u | Unsafe.ReadUnaligned<ushort>(pbBuffer + currentOffset);
                        break;

                    case 3:
                        p0 += 0x80000000u | Unsafe.ReadUnaligned<ushort>(pbBuffer + currentOffset) | ((uint)pbBuffer[currentOffset + 2] << 16);
                        break;

                    default:
                        Debug.Fail("Should not get here.");
                        break;
                }
            }

            Block(ref p0, ref p1);
            Block(ref p0, ref p1);

            return (((long)p1) << 32) | p0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Block(ref uint rp0, ref uint rp1)
        {
            uint p0 = rp0;
            uint p1 = rp1;

            p1 ^= p0;
            p0 = _rotl(p0, 20);

            p0 += p1;
            p1 = _rotl(p1, 9);

            p1 ^= p0;
            p0 = _rotl(p0, 27);

            p0 += p1;
            p1 = _rotl(p1, 19);

            rp0 = p0;
            rp1 = p1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint _rotl(uint value, int shift)
        {
            // This is expected to be optimized into a single rol (or ror with negated shift value) instruction
            return (value << shift) | (value >> (32 - shift));
        }

        public static ulong DefaultSeed { get; } = GenerateSeed();

        private static ulong GenerateSeed()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[sizeof(ulong)];
                rng.GetBytes(bytes);
                return BitConverter.ToUInt64(bytes, 0);
            }
        }
    }
}
