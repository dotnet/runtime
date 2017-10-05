// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace System
{
    internal static class Marvin
    {
        /// <summary>
        /// Convenience method to compute a Marvin hash and collapse it into a 32-bit hash.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeHash32(ref byte data, int count, ulong seed)
        {
            long hash64 = ComputeHash(ref data, count, seed);
            return ((int)(hash64 >> 32)) ^ (int)hash64;
        }

        /// <summary>
        /// Computes a 64-hash using the Marvin algorithm.
        /// </summary>
        public static long ComputeHash(ref byte data, int count, ulong seed)
        {
            uint ucount = (uint)count;
            uint p0 = (uint)seed;
            uint p1 = (uint)(seed >> 32);

            int byteOffset = 0;  // declared as signed int so we don't have to cast everywhere (it's passed to Unsafe.Add() and used for nothing else.)

            while (ucount >= 8)
            {
                p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset));
                Block(ref p0, ref p1);

                p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset + 4));
                Block(ref p0, ref p1);

                byteOffset += 8;
                ucount -= 8;
            }

            switch (ucount)
            {
                case 4:
                    p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset));
                    Block(ref p0, ref p1);
                    goto case 0;

                case 0:
                    p0 += 0x80u;
                    break;

                case 5:
                    p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset));
                    byteOffset += 4;
                    Block(ref p0, ref p1);
                    goto case 1;

                case 1:
                    p0 += 0x8000u | Unsafe.Add(ref data, byteOffset);
                    break;

                case 6:
                    p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset));
                    byteOffset += 4;
                    Block(ref p0, ref p1);
                    goto case 2;

                case 2:
                    p0 += 0x800000u | Unsafe.As<byte, ushort>(ref Unsafe.Add(ref data, byteOffset));
                    break;

                case 7:
                    p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset));
                    byteOffset += 4;
                    Block(ref p0, ref p1);
                    goto case 3;

                case 3:
                    p0 += 0x80000000u | (((uint)(Unsafe.Add(ref data, byteOffset + 2))) << 16)| (uint)(Unsafe.As<byte, ushort>(ref Unsafe.Add(ref data, byteOffset)));
                    break;

                default:
                    Debug.Fail("Should not get here.");
                    break;
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
