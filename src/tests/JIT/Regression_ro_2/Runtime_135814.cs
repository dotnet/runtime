// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135814
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.True(TestGt(3));
        Assert.False(TestLt(3));
        Assert.True(TestRor(3));
        Assert.Equal(-1, TestUInt());
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool TestGt(int y) => y > int.RotateLeft(1, 31);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool TestLt(int y) => y < int.RotateLeft(1, 31);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool TestRor(int y) => y > int.RotateRight(1, 1);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestUInt() => (int)BitOperations.RotateRight(0xFFFFFFFFu, 3);
}
