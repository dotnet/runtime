// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_137637
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Callee(char c, int e) { }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Test(int i, int j, int a)
    {
        Vector128<int> v = Vector128.Create(a, a + 1, a + 2, a + 3);
        Callee("abc"[i], v.GetElement(j) * a + v.GetElement(1) * 5 + v.GetElement(2) * j);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Test(100, 100, 7));
    }
}
