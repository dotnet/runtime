// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133559
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int CompareTypes(string a, string b)
    {
        if (a.GetType() != b.GetType())
        {
            return 2;
        }

        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool SameType(string a, string b)
    {
        return a.GetType() == b.GetType();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TypeHandleOnly(string a)
    {
        // The comparison result is unused, but the load still dereferences "a".
        return a.GetType() == typeof(string) ? 1 : 1;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // string is sealed, so value numbering folds both GetType() loads to a constant
        // type handle. The loads still dereference their operand, so they must raise
        // NullReferenceException rather than being optimized away.
        Assert.Throws<NullReferenceException>(() => CompareTypes(null, "text"));
        Assert.Throws<NullReferenceException>(() => CompareTypes("text", null));
        Assert.Throws<NullReferenceException>(() => SameType(null, "text"));
        Assert.Throws<NullReferenceException>(() => SameType("text", null));
        Assert.Throws<NullReferenceException>(() => TypeHandleOnly(null));

        Assert.Equal(3, CompareTypes("text", "other"));
        Assert.True(SameType("text", "other"));
        Assert.Equal(1, TypeHandleOnly("text"));
    }
}
