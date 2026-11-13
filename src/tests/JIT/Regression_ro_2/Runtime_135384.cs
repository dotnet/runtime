// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_135384;

public static class Runtime_135384
{
    private static int s_selector;
    private static int s_sink;
    private static int s_dummy;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReturnInt() => s_selector;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object ReturnObject() => new object();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConsumeInt(int value) => s_sink = value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int LateRemoval(object argument)
    {
        int value = ReturnInt();
        if (value != 0)
        {
            GC.KeepAlive(argument);
            goto Join;
        }
        GC.KeepAlive(ReturnObject());
    Join:
        ConsumeInt(value);

        return value;
    }

    [Theory]
    [InlineData(123)]
    [InlineData(-1)]
    public static void CallReturnGcState(int value)
    {
        object argument = new object();
        // GCStress must visit the shared return PC on the scalar path first.
        s_selector = value;
        Assert.Equal(value, LateRemoval(argument));
        Assert.Equal(value, s_sink);

        s_selector = 0;
        Assert.Equal(0, LateRemoval(argument));
        Assert.Equal(0, s_sink);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Throw() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallAtEndOfTry()
    {
        int result = 0;
        try
        {
            Throw();
            goto Join;
        }
        catch (InvalidOperationException)
        {
            result = 1;
        }
        _ = s_dummy;
    Join:
        try
        {
            ConsumeInt(10);
        }
        catch (InvalidOperationException)
        {
            result = -100;
        }

        return result + s_sink;
    }

    [Fact]
    public static void CallReturnEhRegion()
    {
        Assert.Equal(11, CallAtEndOfTry());
    }
}
