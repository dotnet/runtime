// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_137680
{
    private static ReadOnlySpan<int> Idx => [0x3FFFFFF8];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static char[] Test() => "ab".AsSpan(Idx[0], 2).ToArray();

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Test());
    }
}
