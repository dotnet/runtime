// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public class IntAnd
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void SideEffect()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool Test_UInt32_UInt32_And(uint x, uint y)
        {
            // X64-NOT: movzx

            // We expect 'and reg8, reg8'.
            // X64: and {{[a-z]+[l|b]}}, {{[a-z]+[l|b]}}

            if ((byte)(x & y) == 0)
            {
                SideEffect();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool Test_UInt32_UInt32_CastByte_And(uint x, uint y)
        {
            // X64-NOT: movzx

            // We expect 'and reg8, reg8'.
            // X64: and {{[a-z]+[l|b]}}, {{[a-z]+[l|b]}}

            if ((byte)((byte)x & y) == 0)
            {
                SideEffect();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool Test_UInt32_UInt32_CastByte_CastByte_And(uint x, uint y)
        {
            // X64-NOT: movzx

            // We expect 'and reg8, reg8'.
            // X64: and {{[a-z]+[l|b]}}, {{[a-z]+[l|b]}}

            if ((byte)((byte)x & (byte)y) == 0)
            {
                SideEffect();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint Test_And_UInt32_MaxValue(uint i)
        {
            // X64: mov
            
            // X64-NOT: and
            return i & UInt32.MaxValue;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            // No CastByte
            if (!Test_UInt32_UInt32_And(0b1000_0000_0000_0000_0000_0000_0000_0000, 0b0000_0000_0000_0000_0000_0000_0000_0001))
                return 0;

            if (!Test_UInt32_UInt32_And(0b0000_0000_0000_0000_0000_0000_0000_0001, 0b1000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (!Test_UInt32_UInt32_And(0b1000_0000_0000_0000_0000_0000_0000_0000, 0b1000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (!Test_UInt32_UInt32_And(0b1000_0000_0000_0000_0000_0000_0000_0000, 0b0000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (!Test_UInt32_UInt32_And(0b0000_0000_0000_0000_0000_0000_0000_0000, 0b1000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (Test_UInt32_UInt32_And(0b0000_0000_0000_0000_0000_0000_0000_0001, 0b0000_0000_0000_0000_0000_0000_0000_0001))
                return 0;

            if (!Test_UInt32_UInt32_And(0b0000_0000_0000_0000_0000_0000_0000_0010, 0b0000_0000_0000_0000_0000_0000_0000_0001))
                return 0;

            if (Test_UInt32_UInt32_And(0b0000_0000_0000_0000_0000_0000_0000_0010, 0b0000_0000_0000_0000_0000_0000_0000_0010))
                return 0;

            if (Test_UInt32_UInt32_And(0b1000_0000_0000_0000_0000_0000_0000_0010, 0b1000_0000_0000_0000_0000_0000_0000_0010))
                return 0;

            if (Test_UInt32_UInt32_And(0b0010_0000_0000_0000_0000_0000_0000_0010, 0b1000_0000_0000_0000_0000_0000_0000_0010))
                return 0;

            if (Test_UInt32_UInt32_And(0b1000_0000_0000_0000_0000_0000_0000_0010, 0b0010_0000_0000_0000_0000_0000_0000_0010))
                return 0;

            // CastByte
            if (!Test_UInt32_UInt32_CastByte_And(0b1000_0000_0000_0000_0000_0000_0000_0000, 0b0000_0000_0000_0000_0000_0000_0000_0001))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_And(0b0000_0000_0000_0000_0000_0000_0000_0001, 0b1000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_And(0b1000_0000_0000_0000_0000_0000_0000_0000, 0b1000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_And(0b1000_0000_0000_0000_0000_0000_0000_0000, 0b0000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_And(0b0000_0000_0000_0000_0000_0000_0000_0000, 0b1000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (Test_UInt32_UInt32_CastByte_And(0b0000_0000_0000_0000_0000_0000_0000_0001, 0b0000_0000_0000_0000_0000_0000_0000_0001))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_And(0b0000_0000_0000_0000_0000_0000_0000_0010, 0b0000_0000_0000_0000_0000_0000_0000_0001))
                return 0;

            if (Test_UInt32_UInt32_CastByte_And(0b0000_0000_0000_0000_0000_0000_0000_0010, 0b0000_0000_0000_0000_0000_0000_0000_0010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_And(0b1000_0000_0000_0000_0000_0000_0000_0010, 0b1000_0000_0000_0000_0000_0000_0000_0010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_And(0b0010_0000_0000_0000_0000_0000_0000_0010, 0b1000_0000_0000_0000_0000_0000_0000_0010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_And(0b1000_0000_0000_0000_0000_0000_0000_0010, 0b0010_0000_0000_0000_0000_0000_0000_0010))
                return 0;

            // CastByte_CastByte
            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b1000_0000_0000_0000_0000_0000_0000_0000, 0b0000_0000_0000_0000_0000_0000_0000_0001))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b0000_0000_0000_0000_0000_0000_0000_0001, 0b1000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b1000_0000_0000_0000_0000_0000_0000_0000, 0b1000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b1000_0000_0000_0000_0000_0000_0000_0000, 0b0000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b0000_0000_0000_0000_0000_0000_0000_0000, 0b1000_0000_0000_0000_0000_0000_0000_0000))
                return 0;

            if (Test_UInt32_UInt32_CastByte_CastByte_And(0b0000_0000_0000_0000_0000_0000_0000_0001, 0b0000_0000_0000_0000_0000_0000_0000_0001))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b0000_0000_0000_0000_0000_0000_0000_0010, 0b0000_0000_0000_0000_0000_0000_0000_0001))
                return 0;

            if (Test_UInt32_UInt32_CastByte_CastByte_And(0b0000_0000_0000_0000_0000_0000_0000_0010, 0b0000_0000_0000_0000_0000_0000_0000_0010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_CastByte_And(0b1000_0000_0000_0000_0000_0000_0000_0010, 0b1000_0000_0000_0000_0000_0000_0000_0010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_CastByte_And(0b0010_0000_0000_0000_0000_0000_0000_0010, 0b1000_0000_0000_0000_0000_0000_0000_0010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_CastByte_And(0b1000_0000_0000_0000_0000_0000_0000_0010, 0b0010_0000_0000_0000_0000_0000_0000_0010))
                return 0;
                
            if (Test_And_UInt32_MaxValue(1234) != 1234)
                return 0;

            return 100;
        }
    }
}
