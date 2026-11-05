// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134891
{
    private const int Bound = 0x1000000;

    [Fact]
    public static void TestEntryPoint()
    {
        nint handle = GetHandle();
        Assert.Equal(handle > Bound, Greater());
        Assert.Equal(handle >= Bound, GreaterOrEqual());
        Assert.Equal(handle < Bound, Less());
        Assert.Equal(handle <= Bound, LessOrEqual());
        Assert.Equal(Bound > handle, GreaterReversed());
        Assert.Equal(Bound >= handle, GreaterOrEqualReversed());
        Assert.Equal(Bound < handle, LessReversed());
        Assert.Equal(Bound <= handle, LessOrEqualReversed());
        Assert.Equal((nuint)handle > Bound, GreaterUnsigned());
        Assert.Equal((nuint)handle < Bound, LessUnsigned());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nint GetHandle() => typeof(Runtime_134891).TypeHandle.Value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Greater() => typeof(Runtime_134891).TypeHandle.Value > Bound;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GreaterOrEqual() => typeof(Runtime_134891).TypeHandle.Value >= Bound;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Less() => typeof(Runtime_134891).TypeHandle.Value < Bound;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessOrEqual() => typeof(Runtime_134891).TypeHandle.Value <= Bound;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GreaterReversed() => Bound > typeof(Runtime_134891).TypeHandle.Value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GreaterOrEqualReversed() => Bound >= typeof(Runtime_134891).TypeHandle.Value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessReversed() => Bound < typeof(Runtime_134891).TypeHandle.Value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessOrEqualReversed() => Bound <= typeof(Runtime_134891).TypeHandle.Value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GreaterUnsigned() => (nuint)typeof(Runtime_134891).TypeHandle.Value > Bound;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessUnsigned() => (nuint)typeof(Runtime_134891).TypeHandle.Value < Bound;
}
