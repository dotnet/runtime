// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace CodeGenTests
{
    static class IntMultiply
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint UInt32_MultiplyWithUInt32MaxValue(uint value)
        {
            // X64:      mov [[REG0:[a-z]+]], [[REG1:[a-z]+]]
            // X64-NEXT: neg [[REG0]]
            return value * UInt32.MaxValue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWithUInt32MaxValue(ulong value)
        {
            // X64:      mov [[REG0:[a-z]+]], [[REG1:[a-z]+]]
            // X64-NEXT: shl [[REG0]], 32
            // X64-NEXT: sub [[REG0]], [[REG1]]
            return value * (ulong)UInt32.MaxValue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWithUInt32MaxValuePlusOne(ulong value)
        {
            // X64:      mov [[REG0:[a-z]+]], [[REG1:[a-z]+]]
            // X64-NEXT: shl [[REG0]], 32
            return value * ((ulong)UInt32.MaxValue + 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWithUInt32MaxValuePlusTwo(ulong value)
        {
            // X64:      mov [[REG0:[a-z]+]], [[REG1:[a-z]+]]
            // X64-NEXT: shl [[REG0]], 32
            // X64-NEXT: add [[REG0]], [[REG1]]
            return value * ((ulong)UInt32.MaxValue + 2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith2(ulong value)
        {
            // X64: lea [[REG0:[a-z]+]], {{\[}}[[REG1:[a-z]+]]+[[REG1]]{{\]}}
            return value * 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith3(ulong value)
        {
            // X64: lea [[REG0:[a-z]+]], {{\[}}[[REG1:[a-z]+]]+2*[[REG1]]{{\]}}
            return value * 3;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith4(ulong value)
        {
            // X64:      mov [[REG0:[a-z]+]], [[REG1:[a-z]+]]
            // X64-NEXT: shl [[REG0]], 2
            return value * 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith5(ulong value)
        {
            // X64: lea [[REG0:[a-z]+]], {{\[}}[[REG1:[a-z]+]]+4*[[REG1]]{{\]}}
            return value * 5;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith6(ulong value)
        {
            // X64:      lea [[REG0:[a-z]+]], {{\[}}[[REG1:[a-z]+]]+2*[[REG1]]{{\]}}
            // X64-NEXT: add [[REG0]], [[REG0]]
            return value * 6;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith7(ulong value)
        {
            // X64:      mov [[REG0:[a-z]+]], [[REG1:[a-z]+]]
            // X64-NEXT: shl [[REG0]], 3
            // X64-NEXT: sub [[REG0]], [[REG1]]
            return value * 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith8(ulong value)
        {
            // X64:      mov [[REG0:[a-z]+]], [[REG1:[a-z]+]]
            // X64-NEXT: shl [[REG0]], 3
            return value * 8;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith9(ulong value)
        {
            // X64: lea [[REG0:[a-z]+]], {{\[}}[[REG1:[a-z]+]]+8*[[REG1]]{{\]}}
            return value * 9;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith15(ulong value)
        {
            // X64:      mov [[REG0:[a-z]+]], [[REG1:[a-z]+]]
            // X64-NEXT: shl [[REG0]], 4
            // X64-NEXT: sub [[REG0]], [[REG1]]
            return value * 15;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith16(ulong value)
        {
            // X64:      mov [[REG0:[a-z]+]], [[REG1:[a-z]+]]
            // X64-NEXT: shl [[REG0]], 4
            return value * 16;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith17(ulong value)
        {
            // X64:      mov [[REG0:[a-z]+]], [[REG1:[a-z]+]]
            // X64-NEXT: shl [[REG0]], 4
            // X64-NEXT: add [[REG0]], [[REG1]]
            return value * 17;
        }

        static int Main()
        {
            if (UInt32_MultiplyWithUInt32MaxValue(1) != UInt32.MaxValue)
                return 0;

            if (UInt64_MultiplyWithUInt32MaxValue(1) != (ulong)UInt32.MaxValue)
                return 0;

            if (UInt64_MultiplyWithUInt32MaxValuePlusOne(1) != ((ulong)UInt32.MaxValue + 1))
                return 0;

            if (UInt64_MultiplyWithUInt32MaxValuePlusTwo(1) != ((ulong)UInt32.MaxValue + 2))
                return 0;

            if (UInt64_MultiplyWith2(1) != 2)
                return 0;

            if (UInt64_MultiplyWith3(1) != 3)
                return 0;

            if (UInt64_MultiplyWith4(1) != 4)
                return 0;

            if (UInt64_MultiplyWith5(1) != 5)
                return 0;

            if (UInt64_MultiplyWith6(1) != 6)
                return 0;

            if (UInt64_MultiplyWith7(1) != 7)
                return 0;

            if (UInt64_MultiplyWith8(1) != 8)
                return 0;

            if (UInt64_MultiplyWith9(1) != 9)
                return 0;

            if (UInt64_MultiplyWith15(1) != 15)
                return 0;

            if (UInt64_MultiplyWith16(1) != 16)
                return 0;

            if (UInt64_MultiplyWith17(1) != 17)
                return 0;

            return 100;
        }
    }
}
