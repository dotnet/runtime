// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// The importer's UTF16 comparison unrolling appends statements with CHECK_SPILL_NONE, which
// used to move the receiver null check ahead of side effects still on the evaluation stack.
public class Runtime_133966
{
    private static int s_sink;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int EqualsAfterBoundsCheck(string s, int[] a, int i)
    {
        return a[i] + (s.Equals("ab") ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int StartsWithAfterDivision(string s, int x, int y)
    {
        return (x / y) + (s.StartsWith("ab", StringComparison.Ordinal) ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int EndsWithAfterDivision(string s, int x, int y)
    {
        return (x / y) + (s.EndsWith("ab", StringComparison.Ordinal) ? 1 : 0);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int[] a = new int[1];

        // The earlier side effect has to win over the unrolled comparison's null check.
        Assert.Throws<IndexOutOfRangeException>(() => { s_sink = EqualsAfterBoundsCheck(null, a, 5); });
        Assert.Throws<DivideByZeroException>(() => { s_sink = StartsWithAfterDivision(null, 1, 0); });
        Assert.Throws<DivideByZeroException>(() => { s_sink = EndsWithAfterDivision(null, 1, 0); });

        // The unrolled comparison itself must still work, and still throw NRE on its own.
        Assert.Throws<NullReferenceException>(() => { s_sink = EqualsAfterBoundsCheck(null, a, 0); });
        Assert.Equal(1, EqualsAfterBoundsCheck("ab", a, 0));
        Assert.Equal(0, EqualsAfterBoundsCheck("ba", a, 0));
        Assert.Equal(3, StartsWithAfterDivision("abc", 2, 1));
        Assert.Equal(2, StartsWithAfterDivision("cba", 2, 1));
        Assert.Equal(3, EndsWithAfterDivision("xab", 2, 1));
        Assert.Equal(2, EndsWithAfterDivision("xba", 2, 1));
    }
}
