// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public static class Runtime_93698
{
    [Fact]
    public static void TestShiftLeftLogicalOvershift()
    {
        if (Sse.IsSupported)
        {
            var result1 = Sse2.ShiftLeftLogical(Vector128.Create(-1, +2, -3, +4), 32);
            Assert.Equal(Vector128<int>.Zero, result1);

            var result2 = Sse2.ShiftLeftLogical(Vector128.Create(-5, +6, -7, +8), Vector128.Create(0, 32, 0, 0));
            Assert.Equal(Vector128<int>.Zero, result2);
        }
    }

    [Fact]
    public static void TestShiftRightLogicalOvershift()
    {
        if (Sse.IsSupported)
        {
            var result1 = Sse2.ShiftRightLogical(Vector128.Create(-1, +2, -3, +4), 32);
            Assert.Equal(Vector128<int>.Zero, result1);

            var result2 = Sse2.ShiftRightLogical(Vector128.Create(-5, +6, -7, +8), Vector128.Create(0, 32, 0, 0));
            Assert.Equal(Vector128<int>.Zero, result2);
        }
    }

    [Fact]
    public static void TestShiftRightArithmeticOvershift()
    {
        if (Sse.IsSupported)
        {
            var result1 = Sse2.ShiftRightArithmetic(Vector128.Create(-1, +2, -3, +4), 32);
            Assert.Equal(Vector128.Create(-1, 0, -1, 0), result1);

            var result2 = Sse2.ShiftRightArithmetic(Vector128.Create(-5, +6, -7, +8), Vector128.Create(0, 32, 0, 0));
            Assert.Equal(Vector128.Create(-1, 0, -1, 0), result2);
        }
    }
}
