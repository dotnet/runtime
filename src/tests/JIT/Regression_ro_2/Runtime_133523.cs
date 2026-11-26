// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

// Forward substitution can put several stack-allocated array allocations into a single
// statement. The stack array expansion phase used to expand only the first one of them,
// leaving the rest as malformed NEWARR helper calls (still carrying the StackArrayLocal
// arg), which crashed the process.
public class Runtime_133523
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TwoArrays(int index)
    {
        int[] first = new int[3];
        int[] second = new int[3];
        return first[second[index]];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int ThreeArrays(int index)
    {
        int[] a = new int[3];
        int[] b = new int[3];
        int[] c = new int[3];
        a[2] = 42;
        return a[b[c[index]]] + a[2];
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(0, TwoArrays(0));
        Assert.Equal(42, ThreeArrays(0));
    }
}
