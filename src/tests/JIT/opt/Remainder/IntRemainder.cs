// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace CodeGenTests
{
    static class IntRemainder
    {
        static int _fieldValue = 123;
        static uint _fieldValueUnsigned = 123;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_RemainderByOne()
        {
            // X64-FULL-LINE:      call CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
            // X64-FULL-LINE-NEXT: xor [[REG0:[a-z]+]], [[REG0]]

            // ARM64-FULL-LINE:      bl CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
            // ARM64-FULL-LINE-NEXT: mov [[REG0:[a-z0-9]+]], wzr

            return _fieldValue % 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_RemainderByOneWithValue(int value)
        {
            // X64-FULL-LINE: xor [[REG0:[a-z]+]], [[REG0]]

            // ARM64-FULL-LINE: mov [[REG0:[a-z0-9]+]], wzr

            return value % 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte Byte_RemainderByMaxValuePlusOne(uint value)
        {
            // X64-NOT: and {{[a-z]+}}

            // X64: movzx {{[a-z]+}}, {{[a-z]+}}

            return (byte)(value % (Byte.MaxValue + 1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ushort UInt16_RemainderByMaxValuePlusOne(uint value)
        {
            // X64-NOT: and {{[a-z]+}}

            // X64: movzx {{[a-z]+}}, {{[a-z]+}}

            return (ushort)(value % (UInt16.MaxValue + 1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint Byte_RemainderByMaxValuePlusOne_Return_UInt32(uint value)
        {
            // X64-NOT: and {{[a-z]+}}

            // X64: movzx {{[a-z]+}}, {{[a-z]+}}

            return (value % (Byte.MaxValue + 1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint UInt16_RemainderByMaxValuePlusOne_Return_UInt32(uint value)
        {
            // X64-NOT: and {{[a-z]+}}

            // X64: movzx {{[a-z]+}}, {{[a-z]+}}

            return (value % (UInt16.MaxValue + 1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte Byte_RemainderByMaxValuePlusOne_WithField()
        {
            // X64-NOT: and {{[a-z]+}}

            // X64: movzx {{[a-z]+}}, {{[a-z]+}}

            return (byte)(_fieldValueUnsigned % (Byte.MaxValue + 1));
        }

        static int Main()
        {
            if (Int32_RemainderByOne() != 0)
                return 0;

            if (Int32_RemainderByOneWithValue(-123) != 0)
                return 0;

            if (Byte_RemainderByMaxValuePlusOne(68000) != 160)
                return 0;

            if (UInt16_RemainderByMaxValuePlusOne(68000) != 2464)
                return 0;

            if (Byte_RemainderByMaxValuePlusOne_Return_UInt32(68000) != 160)
                return 0;

            if (UInt16_RemainderByMaxValuePlusOne_Return_UInt32(68000) != 2464)
                return 0;

            if (Byte_RemainderByMaxValuePlusOne_WithField() != 123)
                return 0;

            return 100;
        }
    }
}