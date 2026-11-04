// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public static class IntRemainder
    {
        static int _fieldValue = 123;
        static uint _fieldValueUnsigned = 123;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_RemainderByOneWithValue(int value)
        {
            // X64-FULL-LINE: xor [[REG0:[a-z]+]], [[REG0]]

            // ARM64-FULL-LINE: mov [[REG0:[a-z0-9]+]], wzr

            return value % 1;
        }

        // The test harness does not currently run SuperFileCheck for Wasm AOT.
        // To check the WASM patterns manually, capture Crossgen2 output with JitDisasm and
        // JitDisasmTesting=1, then run SuperFileCheck with --check-prefixes=WASM.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_RemainderByThree(int value)
        {
            // WASM-NOT: i32.div_s
            // WASM: i32.rem_s
            // WASM-NOT: i32.div_s

            return value % 3;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_RemainderByNegativeThree(int value)
        {
            // WASM-NOT: i32.div_s
            // WASM: i32.rem_s
            // WASM-NOT: i32.div_s

            return value % -3;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Int64_RemainderByThree(long value)
        {
            // WASM-NOT: i64.div_s
            // WASM: i64.rem_s
            // WASM-NOT: i64.div_s

            return value % 3;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Int64_RemainderByNegativeThree(long value)
        {
            // WASM-NOT: i64.div_s
            // WASM: i64.rem_s
            // WASM-NOT: i64.div_s

            return value % -3;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_RemainderByNegativeOne(int value)
        {
            return value % -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Int64_RemainderByNegativeOne(long value)
        {
            return value % -1;
        }

        [Theory]
        [InlineData(int.MinValue, -2)]
        [InlineData(int.MinValue + 1, -1)]
        [InlineData(int.MaxValue - 1, 0)]
        [InlineData(int.MaxValue, 1)]
        public static void Int32_RemainderByConstant(int value, int expected)
        {
            Assert.Equal(expected, Int32_RemainderByThree(value));
            Assert.Equal(expected, Int32_RemainderByNegativeThree(value));

            if (value == int.MinValue)
            {
                Assert.Throws<OverflowException>(() => Int32_RemainderByNegativeOne(value));
            }
            else
            {
                Assert.Equal(0, Int32_RemainderByNegativeOne(value));
            }
        }

        [Theory]
        [InlineData(long.MinValue, -2L)]
        [InlineData(long.MinValue + 1, -1L)]
        [InlineData(long.MaxValue - 1, 0L)]
        [InlineData(long.MaxValue, 1L)]
        public static void Int64_RemainderByConstant(long value, long expected)
        {
            Assert.Equal(expected, Int64_RemainderByThree(value));
            Assert.Equal(expected, Int64_RemainderByNegativeThree(value));

            if (value == long.MinValue)
            {
                Assert.Throws<OverflowException>(() => Int64_RemainderByNegativeOne(value));
            }
            else
            {
                Assert.Equal(0L, Int64_RemainderByNegativeOne(value));
            }
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

        [Fact]
        public static int TestEntryPoint()
        {
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