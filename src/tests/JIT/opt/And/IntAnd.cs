// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace CodeGenTests
{
    class IntAnd
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void SideEffect()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool Test_UInt32_UInt32_And(uint x, uint y)
        {
            // X64-NOT: movzx

            // We expect 'and r8, r8'.
            // X64: and [[REG0:[a-z]+[l|b]]], [[REG1:[a-z]+[l|b]]]

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

            // We expect 'and r8, r8'.
            // X64: and [[REG0:[a-z]+[l|b]]], [[REG1:[a-z]+[l|b]]]

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

            // We expect 'and r8, r8'.
            // X64: and [[REG0:[a-z]+[l|b]]], [[REG1:[a-z]+[l|b]]]

            if ((byte)((byte)x & (byte)y) == 0)
            {
                SideEffect();
                return true;
            }
            return false;
        }

        static int Main()
        {
            // No CastByte
            if (!Test_UInt32_UInt32_And(0b10000000000000000000000000000000, 0b00000000000000000000000000000001))
                return 0;

            if (!Test_UInt32_UInt32_And(0b00000000000000000000000000000001, 0b10000000000000000000000000000000))
                return 0;

            if (!Test_UInt32_UInt32_And(0b10000000000000000000000000000000, 0b10000000000000000000000000000000))
                return 0;

            if (!Test_UInt32_UInt32_And(0b10000000000000000000000000000000, 0b00000000000000000000000000000000))
                return 0;

            if (!Test_UInt32_UInt32_And(0b00000000000000000000000000000000, 0b10000000000000000000000000000000))
                return 0;

            if (Test_UInt32_UInt32_And(0b00000000000000000000000000000001, 0b00000000000000000000000000000001))
                return 0;

            if (!Test_UInt32_UInt32_And(0b00000000000000000000000000000010, 0b00000000000000000000000000000001))
                return 0;

            if (Test_UInt32_UInt32_And(0b00000000000000000000000000000010, 0b00000000000000000000000000000010))
                return 0;

            if (Test_UInt32_UInt32_And(0b10000000000000000000000000000010, 0b10000000000000000000000000000010))
                return 0;

            if (Test_UInt32_UInt32_And(0b00100000000000000000000000000010, 0b10000000000000000000000000000010))
                return 0;

            if (Test_UInt32_UInt32_And(0b10000000000000000000000000000010, 0b00100000000000000000000000000010))
                return 0;

            // CastByte
            if (!Test_UInt32_UInt32_CastByte_And(0b10000000000000000000000000000000, 0b00000000000000000000000000000001))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_And(0b00000000000000000000000000000001, 0b10000000000000000000000000000000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_And(0b10000000000000000000000000000000, 0b10000000000000000000000000000000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_And(0b10000000000000000000000000000000, 0b00000000000000000000000000000000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_And(0b00000000000000000000000000000000, 0b10000000000000000000000000000000))
                return 0;

            if (Test_UInt32_UInt32_CastByte_And(0b00000000000000000000000000000001, 0b00000000000000000000000000000001))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_And(0b00000000000000000000000000000010, 0b00000000000000000000000000000001))
                return 0;

            if (Test_UInt32_UInt32_CastByte_And(0b00000000000000000000000000000010, 0b00000000000000000000000000000010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_And(0b10000000000000000000000000000010, 0b10000000000000000000000000000010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_And(0b00100000000000000000000000000010, 0b10000000000000000000000000000010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_And(0b10000000000000000000000000000010, 0b00100000000000000000000000000010))
                return 0;

            // CastByte_CastByte
            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b10000000000000000000000000000000, 0b00000000000000000000000000000001))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b00000000000000000000000000000001, 0b10000000000000000000000000000000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b10000000000000000000000000000000, 0b10000000000000000000000000000000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b10000000000000000000000000000000, 0b00000000000000000000000000000000))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b00000000000000000000000000000000, 0b10000000000000000000000000000000))
                return 0;

            if (Test_UInt32_UInt32_CastByte_CastByte_And(0b00000000000000000000000000000001, 0b00000000000000000000000000000001))
                return 0;

            if (!Test_UInt32_UInt32_CastByte_CastByte_And(0b00000000000000000000000000000010, 0b00000000000000000000000000000001))
                return 0;

            if (Test_UInt32_UInt32_CastByte_CastByte_And(0b00000000000000000000000000000010, 0b00000000000000000000000000000010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_CastByte_And(0b10000000000000000000000000000010, 0b10000000000000000000000000000010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_CastByte_And(0b00100000000000000000000000000010, 0b10000000000000000000000000000010))
                return 0;

            if (Test_UInt32_UInt32_CastByte_CastByte_And(0b10000000000000000000000000000010, 0b00100000000000000000000000000010))
                return 0;

            Console.Write("Succeeded");

            return 100;
        }
    }
}
