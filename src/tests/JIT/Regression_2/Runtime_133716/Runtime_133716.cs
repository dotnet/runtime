// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133716
{
    private static S s_value;

    [Fact]
    public static int Main()
    {
        s_value.X = 1;
        Assert.True(TestBoxThis(), nameof(TestBoxThis));

        s_value.X = 1;
        Assert.True(TestLdvirtftn(ref s_value), nameof(TestLdvirtftn));

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool TestBoxThis()
    {
        return s_value.Equals(Mutate());
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool TestLdvirtftn<T>(ref T value)
        where T : I
    {
        return value.Equals<int>(Mutate());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Mutate()
    {
        s_value.X = 7;
        return new S { X = 7 };
    }

    private interface I
    {
        bool Equals<T>(object other)
        {
            return Equals(other);
        }
    }

    private struct S : I
    {
        public int X;
    }
}
