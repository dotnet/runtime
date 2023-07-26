// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public static class IntMultiply
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
            // X64: lea [[REG0:[a-z]+]], {{\[}}4*[[REG1:[a-z]+]]{{\]}}
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
            // X64:      lea [[REG0:[a-z]+]], {{\[}}8*[[REG1:[a-z]+]]{{\]}}
            // X64-NEXT: sub [[REG0]], [[REG1]]
            return value * 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith8(ulong value)
        {
            // X64: lea [[REG0:[a-z]+]], {{\[}}8*[[REG1:[a-z]+]]{{\]}}
            return value * 8;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith9(ulong value)
        {
            // X64: lea [[REG0:[a-z]+]], {{\[}}[[REG1:[a-z]+]]+8*[[REG1]]{{\]}}
            return value * 9;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith10(ulong value)
        {
            // X64:      lea [[REG0:[a-z]+]], {{\[}}[[REG1:[a-z]+]]+4*[[REG1]]{{\]}}
            // X64-NEXT: add [[REG0]], [[REG0]]
            return value * 10;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith11(ulong value)
        {
            // We expect 'imul' since the alternative replacement sequence would require 2 three-component LEA instructions which is slower.

            // X64: imul
            return value * 11;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith12(ulong value)
        {
            // X64:      lea [[REG0:[a-z]+]], {{\[}}[[REG1:[a-z]+]]+2*[[REG1]]{{\]}}
            // X64-NEXT: shl [[REG0]], 2
            return value * 12;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith13(ulong value)
        {
            // We expect 'imul' since the alternative replacement sequence would require 2 three-component LEA instructions which is slower.

            // X64: imul
            return value * 13;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith14(ulong value)
        {
            // We expect 'imul' since the alternative replacement sequence would require 4 instructions which is too slow.

            // X64: imul
            return value * 14;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith15(ulong value)
        {
            // We expect these instructions since the alternative replacement sequence would require 2 three-component LEA instructions which is slower.

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith18(ulong value)
        {
            // X64:      lea [[REG0:[a-z]+]], {{\[}}[[REG1:[a-z]+]]+8*[[REG1]]{{\]}}
            // X64-NEXT: add [[REG0]], [[REG0]]
            return value * 18;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith19(ulong value)
        {
            // We expect 'imul' since the alternative replacement sequence would require 2 three-component LEA instructions which is slower.

            // X64: imul
            return value * 19;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith20(ulong value)
        {
            // X64:      lea [[REG0:[a-z]+]], {{\[}}[[REG1:[a-z]+]]+4*[[REG1]]{{\]}}
            // X64-NEXT: shl [[REG0]], 2
            return value * 20;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith21(ulong value)
        {
            // We expect 'imul' since the alternative replacement sequence would require 2 three-component LEA instructions which is slower.

            // X64: imul
            return value * 21;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith22(ulong value)
        {
            // We expect 'imul' since the alternative replacement sequence would require 2 three-component LEA instructions and 1 ADD instruction which is slower.

            // X64: imul
            return value * 22;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith23(ulong value)
        {
            // We expect 'imul' since the alternative replacement sequence would require 1 three-component LEA instruction, 1 SHL instruction, and 1 ADD instruction which is slower.

            // X64: imul
            return value * 23;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith24(ulong value)
        {
            // X64:      lea [[REG0:[a-z]+]], {{\[}}[[REG1:[a-z]+]]+2*[[REG1]]{{\]}}
            // X64-NEXT: shl [[REG0]], 3
            return value * 24;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith25(ulong value)
        {
            // We expect 'imul' since the alternative replacement sequence would require 2 three-component LEA instructions which is slower.

            // X64: imul
            return value * 25;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_MultiplyWith5_AddressExposed(ulong value)
        {
            // X64:      mov [[REG0:[a-z]+]], qword ptr
            // X64-NOT:  mov
            // X64-NEXT: lea [[REG1:[a-z]+]], {{\[}}[[REG0]]+4*[[REG0]]{{\]}}
            var value2 = value * 5;
            UInt64_AddressExposed(ref value);
            return value2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void UInt64_AddressExposed(ref ulong value)
        {

        }

        [Fact]
        public static int TestEntryPoint()
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

            if (UInt64_MultiplyWith10(1) != 10)
                return 0;

            if (UInt64_MultiplyWith11(1) != 11)
                return 0;

            if (UInt64_MultiplyWith12(1) != 12)
                return 0;

            if (UInt64_MultiplyWith13(1) != 13)
                return 0;

            if (UInt64_MultiplyWith14(1) != 14)
                return 0;

            if (UInt64_MultiplyWith15(1) != 15)
                return 0;

            if (UInt64_MultiplyWith16(1) != 16)
                return 0;

            if (UInt64_MultiplyWith17(1) != 17)
                return 0;

            if (UInt64_MultiplyWith18(1) != 18)
                return 0;

            if (UInt64_MultiplyWith19(1) != 19)
                return 0;

            if (UInt64_MultiplyWith20(1) != 20)
                return 0;

            if (UInt64_MultiplyWith21(1) != 21)
                return 0;

            if (UInt64_MultiplyWith22(1) != 22)
                return 0;

            if (UInt64_MultiplyWith23(1) != 23)
                return 0;

            if (UInt64_MultiplyWith24(1) != 24)
                return 0;

            if (UInt64_MultiplyWith25(1) != 25)
                return 0;

            if (UInt64_MultiplyWith5_AddressExposed(1) != 5)
                return 0;

            return 100;
        }
    }
}
