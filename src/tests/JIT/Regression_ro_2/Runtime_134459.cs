// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134459
{
    [Fact]
    public static void TestEntryPoint()
    {
        int[] arr = new int[8];
        Test(arr);
        Assert.Equal(new int[] { 1, 2, 3, 4, 0, 0, 0, 0 }, arr);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Test(int[] arr)
    {
        arr[(nint)0] = 1;
        arr[(nint)1] = 2;
        arr[(nint)2] = 3;
        arr[(nint)3] = 4;
    }
}
