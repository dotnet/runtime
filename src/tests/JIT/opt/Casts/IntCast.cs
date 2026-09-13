// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public class IntCast
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Cast_Short_To_Long(short value)
        {
            // X64-NOT: cdqe
            return (long)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Cast_Short_To_Long_Add(short value1, short value2)
        {
            // X64:     movsx
            // X64-NOT: cdqe
            // X64:     movsx
            // X64-NOT: movsxd

            return (long)value1 + (long)value2;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            foreach (int value in new[] { int.MinValue, -65537, -32769, -129, -128, -1, 0, 127, 128, 32767, 32768, 65535, int.MaxValue })
            {
                Assert.Equal((int)(sbyte)value, NarrowByte(value));
                Assert.Equal((int)(short)value, NarrowShort(value));
                Assert.Equal((long)(sbyte)value, NarrowByteToLong(value));
                Assert.Equal((long)(short)value, NarrowShortToLong(value));
                Assert.Equal((long)(uint)(sbyte)value, NarrowByteToUIntThenLong(value));
                Assert.Equal((long)(uint)(short)value, NarrowShortToUIntThenLong(value));
                Assert.Equal((int)(sbyte)value, NarrowByteFromMemory(new[] { value }));
                Assert.Equal((int)(short)value, NarrowShortFromMemory(new[] { value }));
            }
            if (Cast_Short_To_Long(Int16.MaxValue) != 32767)
                return 0;

            if (Cast_Short_To_Long_Add(Int16.MaxValue, Int16.MaxValue) != 65534)
                return 0;

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NarrowByte(int value)
        {
            // X64-FULL-LINE: movsx eax, {{[a-z0-9]+}}
            return (sbyte)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NarrowShort(int value)
        {
            // X64-FULL-LINE: movsx eax, {{[a-z0-9]+}}
            return (short)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long NarrowByteToLong(int value)
        {
            // X64-FULL-LINE: movsx rax, {{[a-z0-9]+}}
            return (sbyte)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long NarrowShortToLong(int value)
        {
            // X64-FULL-LINE: movsx rax, {{[a-z0-9]+}}
            return (short)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long NarrowByteToUIntThenLong(int value) => (uint)(sbyte)value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long NarrowShortToUIntThenLong(int value) => (uint)(short)value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NarrowByteFromMemory(int[] value) => (sbyte)value[0];

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NarrowShortFromMemory(int[] value) => (short)value[0];
    }
}
