// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public static class IntAdd
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static sbyte Int8_Add(sbyte x, sbyte y)
        {
            // X64-NOT: movsx

            // X64:      add
            // X64-NEXT: movsx

            return (sbyte)(x + y);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte UInt8_Add(byte x, byte y)
        {
            // X64-NOT: movzx

            // X64:      add
            // X64-NEXT: movzx

            return (byte)(x + y);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static short Int16_Add(short x, short y)
        {
            // X64-NOT: movsx

            // X64:      add
            // X64-NEXT: movsx

            // X64-NOT: cwde

            return (short)(x + y);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ushort UInt16_Add(ushort x, ushort y)
        {
            // X64-NOT: movzx

            // X64:      add
            // X64-NEXT: movzx

            return (ushort)(x + y);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_Add(int x, int y)
        {
            // X64: lea

            return x + y;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint UInt32_Add(uint x, uint y)
        {
            // X64: lea

            return x + y;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Int64_Add(long x, long y)
        {
            // X64: lea

            return x + y;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_Add(ulong x, ulong y)
        {
            // X64: lea

            return x + y;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            // Int8
            if (Int8_Add(SByte.MaxValue, 15) != -114)
                return 0;

            if (Int8_Add(15, SByte.MaxValue) != -114)
                return 0;

            // UInt8
            if (UInt8_Add(Byte.MaxValue, 15) != 14)
                return 0;

            if (UInt8_Add(15, Byte.MaxValue) != 14)
                return 0;

            // Int16
            if (Int16_Add(Int16.MaxValue, 15) != -32754)
                return 0;

            if (Int16_Add(15, Int16.MaxValue) != -32754)
                return 0;

            // UInt16
            if (UInt16_Add(UInt16.MaxValue, 15) != 14)
                return 0;

            if (UInt16_Add(15, UInt16.MaxValue) != 14)
                return 0;

            // Int32
            if (Int32_Add(Int32.MaxValue, 15) != -2147483634)
                return 0;

            if (Int32_Add(15, Int32.MaxValue) != -2147483634)
                return 0;

            // UInt32
            if (UInt32_Add(UInt32.MaxValue, 15) != 14)
                return 0;

            if (UInt32_Add(15, UInt32.MaxValue) != 14)
                return 0;

            // Int64
            if (Int64_Add(Int64.MaxValue, 15) != -9223372036854775794)
                return 0;

            if (Int64_Add(15, Int64.MaxValue) != -9223372036854775794)
                return 0;

            // UInt64
            if (UInt64_Add(UInt64.MaxValue, 15) != 14)
                return 0;

            if (UInt64_Add(15, UInt64.MaxValue) != 14)
                return 0;

            return 100;
        }
    }
}