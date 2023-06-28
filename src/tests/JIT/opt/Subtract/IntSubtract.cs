// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public static class IntSubtract
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static sbyte Int8_Subtract(sbyte x, sbyte y)
        {
            // X64-NOT: movsx

            // X64:      sub
            // X64-NEXT: movsx

            return (sbyte)(x - y);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte UInt8_Subtract(byte x, byte y)
        {
            // X64-NOT: movzx

            // X64:      sub
            // X64-NEXT: movzx

            return (byte)(x - y);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static short Int16_Subtract(short x, short y)
        {
            // X64-NOT: movsx

            // X64:      sub
            // X64-NEXT: movsx

            // X64-NOT: cwde

            return (short)(x - y);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ushort UInt16_Subtract(ushort x, ushort y)
        {
            // X64-NOT: movzx

            // X64:      sub
            // X64-NEXT: movzx

            return (ushort)(x - y);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Int32_Subtract(int x, int y)
        {
            // X64-NOT: movsx
            
            // X64: sub
            
            // X64-NOT: movsx

            return x - y;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint UInt32_Subtract(uint x, uint y)
        {
            // X64-NOT: movzx
            
            // X64: sub
            
            // X64-NOT: movzx

            return x - y;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Int64_Subtract(long x, long y)
        {
            // X64-NOT: movsx
            
            // X64: sub
            
            // X64-NOT: movsx

            return x - y;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong UInt64_Subtract(ulong x, ulong y)
        {
            // X64-NOT: movzx
            
            // X64: sub
            
            // X64-NOT: movzx

            return x - y;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            // Int8
            if (Int8_Subtract(SByte.MinValue, 15) != 113)
                return 0;

            if (Int8_Subtract(15, SByte.MaxValue) != -112)
                return 0;

            // UInt8
            if (UInt8_Subtract(Byte.MinValue, 15) != 241)
                return 0;

            if (UInt8_Subtract(15, Byte.MaxValue) != 16)
                return 0;

            // Int16
            if (Int16_Subtract(Int16.MinValue, 15) != 32753)
                return 0;

            if (Int16_Subtract(15, Int16.MaxValue) != -32752)
                return 0;

            // UInt16
            if (UInt16_Subtract(UInt16.MinValue, 15) != 65521)
                return 0;

            if (UInt16_Subtract(15, UInt16.MaxValue) != 16)
                return 0;

            // Int32
            if (Int32_Subtract(Int32.MinValue, 15) != 2147483633)
                return 0;

            if (Int32_Subtract(15, Int32.MaxValue) != -2147483632)
                return 0;

            // UInt32
            if (UInt32_Subtract(UInt32.MinValue, 15) != 4294967281)
                return 0;

            if (UInt32_Subtract(15, UInt32.MaxValue) != 16)
                return 0;

            // Int64
            if (Int64_Subtract(Int64.MinValue, 15) != 9223372036854775793)
                return 0;

            if (Int64_Subtract(15, Int64.MaxValue) != -9223372036854775792)
                return 0;

            // UInt64
            if (UInt64_Subtract(UInt64.MinValue, 15) != 18446744073709551601)
                return 0;

            if (UInt64_Subtract(15, UInt64.MaxValue) != 16)
                return 0;

            return 100;
        }
    }
}
