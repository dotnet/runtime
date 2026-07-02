// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace LdpStpPairing
{
    public unsafe class Program
    {
        // On Arm64, "[base, index]" and "[base, #offset]" are valid addressing modes but
        // "[base, index, #offset]" is not. When the same "base + index" is accessed at
        // adjacent offsets, the JIT should materialize "base + index" once (a single "add")
        // and fold the accesses into "ldp"/"stp" pairs, rather than recomputing the address.

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CopyTwoVectors(byte* dst, byte* src, nuint i)
        {
            //ARM64-FULL-LINE: ldp {{q[0-9]+}}, {{q[0-9]+}}, [{{x[0-9]+}}]
            //ARM64-FULL-LINE: stp {{q[0-9]+}}, {{q[0-9]+}}, [{{x[0-9]+}}]
            Vector128<byte> v1 = Vector128.Load(src + i);
            Vector128<byte> v2 = Vector128.Load(src + i + 16);
            v1.Store(dst + i);
            v2.Store(dst + i + 16);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long SumTwoLongs(long* p, nuint i)
        {
            //ARM64-FULL-LINE: ldp {{x[0-9]+}}, {{x[0-9]+}}, [{{x[0-9]+}}]
            return *(p + i) + *(p + i + 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int LoadFourInts(int* p, nuint i)
        {
            //ARM64-FULL-LINE: ldp {{w[0-9]+}}, {{w[0-9]+}}, [{{x[0-9]+}}]
            //ARM64-FULL-LINE: ldp {{w[0-9]+}}, {{w[0-9]+}}, [{{x[0-9]+}}, #0x08]
            return *(p + i) + *(p + i + 1) + *(p + i + 2) + *(p + i + 3);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void StoreTwoFarVectors(byte* dst, byte* src, nuint i)
        {
            // Two non-adjacent, non-zero offsets cannot form a pair, but "src + i" / "dst + i"
            // should still be materialized once and shared (rather than recomputed per access).
            // Both loads using the same base register is consistent with the shared "add"
            // (this does not by itself prove the address is computed only once).
            //ARM64-FULL-LINE: ldr {{q[0-9]+}}, [[[SRCBASE:x[0-9]+]], #0x10]
            //ARM64-FULL-LINE: ldr {{q[0-9]+}}, [[[SRCBASE]], #0x30]
            Vector128<byte> v1 = Vector128.Load(src + i + 16);
            Vector128<byte> v2 = Vector128.Load(src + i + 48);
            v1.Store(dst + i + 16);
            v2.Store(dst + i + 48);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int result = 100;

            byte* src = stackalloc byte[64];
            byte* dst = stackalloc byte[64];
            for (int k = 0; k < 64; k++)
            {
                src[k] = (byte)(k + 1);
                dst[k] = 0;
            }

            CopyTwoVectors(dst, src, 4);
            for (int k = 0; k < 32; k++)
            {
                if (dst[4 + k] != src[4 + k])
                {
                    Console.WriteLine($"CopyTwoVectors failed at {k}");
                    result = -1;
                }
            }

            long* longs = stackalloc long[8];
            for (int k = 0; k < 8; k++)
            {
                longs[k] = (k + 1) * 1000;
            }

            if (SumTwoLongs(longs, 2) != (longs[2] + longs[3]))
            {
                Console.WriteLine("SumTwoLongs failed");
                result = -1;
            }

            int* ints = stackalloc int[8];
            for (int k = 0; k < 8; k++)
            {
                ints[k] = (k + 1) * 7;
            }

            if (LoadFourInts(ints, 1) != (ints[1] + ints[2] + ints[3] + ints[4]))
            {
                Console.WriteLine("LoadFourInts failed");
                result = -1;
            }

            StoreTwoFarVectors(dst, src, 0);
            for (int k = 0; k < 16; k++)
            {
                if (dst[16 + k] != src[16 + k] || dst[48 + k] != src[48 + k])
                {
                    Console.WriteLine($"StoreTwoFarVectors failed at {k}");
                    result = -1;
                }
            }

            if (result == 100)
            {
                Console.WriteLine("PASSED");
            }

            return result;
        }
    }
}
