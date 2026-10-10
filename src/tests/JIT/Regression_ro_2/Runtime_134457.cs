// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134457
{
    private static int s_sink;

    [Fact]
    public static void TestEntryPoint()
    {
        s_sink = -1;
        Assert.Equal(6, Test(5, argumentException: false));
        Assert.Equal(6, s_sink);

        s_sink = -1;
        Assert.Equal(0, Test(5, argumentException: true));
        Assert.Equal(0, s_sink);

        s_sink = -1;
        Assert.Equal(6, TestWithEnclosedFinally(5, argumentException: false));
        Assert.Equal(6, s_sink);

        s_sink = -1;
        Assert.Equal(0, TestWithEnclosedFinally(5, argumentException: true));
        Assert.Equal(0, s_sink);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Throw(bool argumentException)
    {
        if (argumentException)
        {
            throw new ArgumentException();
        }

        throw new InvalidOperationException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Work(int value) => s_sink = value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int arg, bool argumentException)
    {
        int x = 0;
        try
        {
            Throw(argumentException);
        }
        catch (ArgumentException)
        {
            try
            {
                Work(arg);
            }
            finally
            {
                s_sink = x;
            }
        }
        catch (Exception) when ((x = arg + 1) > 0)
        {
            s_sink = x;
        }

        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestWithEnclosedFinally(int arg, bool argumentException)
    {
        int x = 0;
        try
        {
            try
            {
                Throw(true);
            }
            catch (ArgumentException)
            {
                try
                {
                    Throw(argumentException);
                }
                finally
                {
                    s_sink = x;
                }
            }
        }
        catch (ArgumentException)
        {
            try
            {
                Work(arg);
            }
            finally
            {
                s_sink = x;
            }
        }
        catch (Exception) when ((x = arg + 1) > 0)
        {
            Assert.Equal(x, s_sink);
        }

        return x;
    }
}
