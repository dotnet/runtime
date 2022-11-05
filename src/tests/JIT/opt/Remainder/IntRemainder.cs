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
            // X64:      call CORINFO
            // X64-NEXT: xor [[REG0:[a-z]+]], [[REG0]]

            // ARM64:      bl CORINFO
            // ARM64-NEXT: mov [[REG0:[a-z0-9]+]], wzr
            return _fieldValue % 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_RemainderByOneWithValue(int value)
        {
            // X64: xor [[REG0:[a-z]+]], [[REG0]]

            // ARM64: mov [[REG0:[a-z0-9]+]], wzr
            return value % 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_RemainderByNegativeOne()
        {
            // X64:      call CORINFO
            // X64-NEXT: xor [[REG0:[a-z]+]], [[REG0]]

            // ARM64:      bl CORINFO
            // ARM64-NEXT: mov [[REG0:[a-z0-9]+]], wzr
            return _fieldValue % -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_RemainderByNegativeOneWithValue(int value)
        {
            // X64: xor [[REG0:[a-z]+]], [[REG0]]

            // ARM64: mov [[REG0:[a-z0-9]+]], wzr
            return value % -1;
        }

        static int Main()
        {
            if (Int32_RemainderByOne() != 0)
                return 0;

            if (Int32_RemainderByOneWithValue(-123) != 0)
                return 0;

            if (Int32_RemainderByNegativeOne() != 0)
                return 0;

            if (Int32_RemainderByNegativeOneWithValue(-123) != 0)
                return 0;

            return 100;
        }
    }
}