// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_119654
{
    [Fact]
    public static void TestEntryPoint()
    {
        if (ShouldReturnOne(-4294967296L) != 1)
        {
            throw new Exception("Test failed");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining |
                MethodImplOptions.AggressiveOptimization)]
    private static int ShouldReturnOne(long availableSigned)
    {
        if (availableSigned == 0L)
            return 0;
        return (uint)(ulong)availableSigned != 0 ? 111 : 1;
    }
}
