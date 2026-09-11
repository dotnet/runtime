// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133716
{
    private static S s_value;

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Main()
    {
        s_value.X = 1;

        Assert.True(s_value.Equals(Mutate()));
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Mutate()
    {
        s_value.X = 7;
        return new S { X = 7 };
    }

    private struct S
    {
        public int X;
    }
}
