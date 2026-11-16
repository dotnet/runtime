// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_137641;

public static class Runtime_137641
{
    private static volatile int s_v;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int One() => 1;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test() => s_v + checked(int.MaxValue + One());

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<OverflowException>(() => Test());
    }
}
