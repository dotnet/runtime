// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_92865
{
    [Fact]
    public static void TestEntryPoint()
    {
        long[] arr = new long[4];
        ulong data = 2251250057871360;
        Test(arr, ref data);
    }

    [MethodImpl(MethodImplOptions.NoInlining |
                MethodImplOptions.NoOptimization)]
    private static long Test(long[] arr, ref ulong data)
    {
        return arr[(int)(data >> 7)];
    }
}
