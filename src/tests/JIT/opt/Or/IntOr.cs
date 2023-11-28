// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public class IntOr
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void SideEffect()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Test_UInt32_UInt32_CastByte_Or(uint x, uint y)
        {
            // X64-NOT: movzx

            // We expect 'or reg8, reg8'.
            // X64: or {{[a-z]+[l|b]}}, {{[a-z]+[l|b]}}

            if ((byte)((byte)x | y) == 0)
            {
                SideEffect();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Test_UInt32_ByRef_CastByte_CastByte_Or(uint x, ref uint y)
        {
            // X64-NOT: movzx

            // We expect 'or reg8, mem8'.
            // X64: or {{[a-z]+[l|b]}}, byte ptr

            if ((byte)((byte)x | (byte)y) == 0)
            {
                SideEffect();
                return true;
            }
            return false;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            uint leftMostBit  = 0b10000000000000000000000000000000;
            uint rightMostBit = 0b00000000000000000000000000000001;
            uint noBits       = 0b00000000000000000000000000000000;

            if (!Test_UInt32_UInt32_CastByte_Or(leftMostBit, leftMostBit))
                return 0;

            if (Test_UInt32_UInt32_CastByte_Or(leftMostBit, rightMostBit))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_Or(leftMostBit, noBits))
                return 0;

            if (Test_UInt32_UInt32_CastByte_Or(rightMostBit, leftMostBit))
                return 0;

            if (Test_UInt32_UInt32_CastByte_Or(rightMostBit, rightMostBit))
                return 0;

            if (Test_UInt32_UInt32_CastByte_Or(rightMostBit, noBits))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_Or(noBits, leftMostBit))
                return 0;

            if (Test_UInt32_UInt32_CastByte_Or(noBits, rightMostBit))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_Or(noBits, noBits))
                return 0;

            // ByRef
            if (!Test_UInt32_ByRef_CastByte_CastByte_Or(leftMostBit, ref leftMostBit))
                return 0;

            if (Test_UInt32_ByRef_CastByte_CastByte_Or(leftMostBit, ref rightMostBit))
                return 0;

            if (!Test_UInt32_ByRef_CastByte_CastByte_Or(leftMostBit, ref noBits))
                return 0;

            if (Test_UInt32_ByRef_CastByte_CastByte_Or(rightMostBit, ref leftMostBit))
                return 0;

            if (Test_UInt32_ByRef_CastByte_CastByte_Or(rightMostBit, ref rightMostBit))
                return 0;

            if (Test_UInt32_ByRef_CastByte_CastByte_Or(rightMostBit, ref noBits))
                return 0;

            if (!Test_UInt32_ByRef_CastByte_CastByte_Or(noBits, ref leftMostBit))
                return 0;

            if (Test_UInt32_ByRef_CastByte_CastByte_Or(noBits, ref rightMostBit))
                return 0;

            if (!Test_UInt32_ByRef_CastByte_CastByte_Or(noBits, ref noBits))
                return 0;

            return 100;
        }
    }
}
