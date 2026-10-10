// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    // Casts from int to long of values that are known to never be negative
    // should use a zero-extension instead of a sign-extension.
    public class NeverNegativeCast
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static nint TrailingZeroCount_Div(uint mask)
        {
            // X64-NOT: movsxd
            // X64-NOT: cdqe
            return BitOperations.TrailingZeroCount(mask) / sizeof(ushort);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long LeadingZeroCount_Rsh(uint value)
        {
            // X64-NOT: movsxd
            // X64-NOT: cdqe
            return BitOperations.LeadingZeroCount(value) >> 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long PopCount_Rsz(uint value)
        {
            // X64-NOT: movsxd
            // X64-NOT: cdqe
            return BitOperations.PopCount(value) >>> 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Rsz_ByConstant(int value)
        {
            // X64-NOT: movsxd
            // X64-NOT: cdqe
            return value >>> 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long UDiv_ByConstant(int value)
        {
            // X64-NOT: movsxd
            // X64-NOT: cdqe
            return (int)((uint)value / 3);
        }

        [Fact]
        public static void TestEntryPoint()
        {
            Assert.Equal(0, TrailingZeroCount_Div(1));
            Assert.Equal(2, TrailingZeroCount_Div(0x10));
            Assert.Equal(16, TrailingZeroCount_Div(0));

            Assert.Equal(16, LeadingZeroCount_Rsh(0));
            Assert.Equal(0, LeadingZeroCount_Rsh(uint.MaxValue));

            Assert.Equal(8, PopCount_Rsz(uint.MaxValue));
            Assert.Equal(0, PopCount_Rsz(0));

            Assert.Equal(int.MaxValue, Rsz_ByConstant(-1));
            Assert.Equal(0x40000000, Rsz_ByConstant(int.MinValue));
            Assert.Equal(21, Rsz_ByConstant(42));

            Assert.Equal(1431655765, UDiv_ByConstant(-1));
            Assert.Equal(14, UDiv_ByConstant(42));
        }
    }
}
