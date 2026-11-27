// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_134055;

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Long CCMP chains keep a register condition on GT_SELECT, which is then
// lowered into GT_SELECT_INC/GT_SELECT_NEG/GT_SELECT_INV on arm64.
public class Runtime_134055
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestInc(int a) =>
        (a > 0 & a > 1 & a > 2 & a > 3 & a > 4 & a > 5 &
         a > 6 & a > 7 & a > 8 & a > 9 & a > 10 & a > 11) ? 5 : 4;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestNeg(int a, int b) =>
        (a > 0 & a > 1 & a > 2 & a > 3 & a > 4 & a > 5 &
         a > 6 & a > 7 & a > 8 & a > 9 & a > 10 & a > 11) ? b : -a;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestInv(int a, int b) =>
        (a > 0 & a > 1 & a > 2 & a > 3 & a > 4 & a > 5 &
         a > 6 & a > 7 & a > 8 & a > 9 & a > 10 & a > 11) ? b : ~a;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestAdd(int a, int b) =>
        (a > 0 & a > 1 & a > 2 & a > 3 & a > 4 & a > 5 &
         a > 6 & a > 7 & a > 8 & a > 9 & a > 10 & a > 11) ? b : a + 1;

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(5, TestInc(100));
        Assert.Equal(4, TestInc(0));
        Assert.Equal(-3, TestNeg(3, 7));
        Assert.Equal(7, TestNeg(100, 7));
        Assert.Equal(-4, TestInv(3, 7));
        Assert.Equal(7, TestInv(100, 7));
        Assert.Equal(4, TestAdd(3, 7));
        Assert.Equal(7, TestAdd(100, 7));
    }
}
