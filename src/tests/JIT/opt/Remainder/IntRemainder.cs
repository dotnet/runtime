// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace CodeGenTests
{
    static class IntRemainder
    {
        static int _fieldValue = 123;

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

        static int Main()
        {
            if (Int32_RemainderByOne() != 0)
                return 0;

            if (Int32_RemainderByOneWithValue(-123) != 0)
                return 0;

            return 100;
        }
    }
}