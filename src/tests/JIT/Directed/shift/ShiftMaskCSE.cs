// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

// The shift count is masked to the operand bit width by IL semantics, so `x >> n`
// is imported as `x >> (n & 31)` (or `& 63` for 64-bit). When that masked count is
// shared between two shifts it gets CSE'd, and the JIT must still produce the same
// result as an unshared masked shift for every count, including counts past the
// operand bit width where the masking is observable.
namespace ShiftMaskCSE
{
    public static class Test
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ShiftAndCSE_U32(uint foo, int shift)
        {
            uint res = foo >> shift;
            res <<= shift;
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ShiftAndCSE_U32_Ref(uint foo, int shift)
        {
            int s = shift & 31;
            uint res = foo >> s;
            res <<= s;
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ShiftAndCSE_I32(int foo, int shift)
        {
            int res = foo >> shift;
            res <<= shift;
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ShiftAndCSE_I32_Ref(int foo, int shift)
        {
            int s = shift & 31;
            int res = foo >> s;
            res <<= s;
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ShiftAndCSE_U64(ulong foo, int shift)
        {
            ulong res = foo >> shift;
            res <<= shift;
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ShiftAndCSE_U64_Ref(ulong foo, int shift)
        {
            int s = shift & 63;
            ulong res = foo >> s;
            res <<= s;
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ShiftAndCSE_I64(long foo, int shift)
        {
            long res = foo >> shift;
            res <<= shift;
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ShiftAndCSE_I64_Ref(long foo, int shift)
        {
            int s = shift & 63;
            long res = foo >> s;
            res <<= s;
            return res;
        }

        [Fact]
        public static void TestEntryPoint()
        {
            // Counts span past the bit width (and negative), exercising the masking.
            foreach (int shift in new int[] { 0, 1, 5, 31, 32, 33, 63, 64, 65, 100, -1, -31, -32 })
            {
                foreach (uint foo in new uint[] { 0u, 1u, 0xF0F0F0F0u, 0xFFFFFFFFu, 0x12345678u })
                {
                    Assert.Equal(ShiftAndCSE_U32_Ref(foo, shift), ShiftAndCSE_U32(foo, shift));
                    Assert.Equal(ShiftAndCSE_I32_Ref((int)foo, shift), ShiftAndCSE_I32((int)foo, shift));
                }

                foreach (ulong foo in new ulong[] { 0ul, 1ul, 0xF0F0F0F0F0F0F0F0ul, 0xFFFFFFFFFFFFFFFFul, 0x123456789ABCDEF0ul })
                {
                    Assert.Equal(ShiftAndCSE_U64_Ref(foo, shift), ShiftAndCSE_U64(foo, shift));
                    Assert.Equal(ShiftAndCSE_I64_Ref((long)foo, shift), ShiftAndCSE_I64((long)foo, shift));
                }
            }
        }
    }
}
