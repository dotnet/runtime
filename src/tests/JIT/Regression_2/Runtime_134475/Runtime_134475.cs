// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134475
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public static void TestEntryPoint(int value)
    {
        nint x = value;
        nint handle = RuntimeTypeHandle.ToIntPtr(typeof(string).TypeHandle);
        Assert.Equal(x + 11, PlainConstants(x));
        Assert.Equal(x + 8, HandleLast(x) - handle);
        Assert.Equal(x + 8, HandleFirst(x) - handle);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static nint HandleFirst(nint x) => (x + RuntimeTypeHandle.ToIntPtr(typeof(string).TypeHandle)) + 8;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static nint HandleLast(nint x) => (x + 8) + RuntimeTypeHandle.ToIntPtr(typeof(string).TypeHandle);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static nint PlainConstants(nint x) => (x + 3) + 8;
}
