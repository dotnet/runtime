// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// A check in a try that throws has not been survived when the finally runs, so a repeat of it in the
// finally must still throw before any side effect that follows. The finally is entered from the try's
// exceptional exit as well as from its normal one.
public class ExceptionSetsFinally
{
    private static int s_marker;
    private static int s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Swallow()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void NullCheck(int[] a)
    {
        try
        {
            s_sink = a.Length;
        }
        finally
        {
            if (a.Length == a.Length)
            {
                s_marker = 1;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Division(int d)
    {
        try
        {
            s_sink = 42 / d;
        }
        finally
        {
            if (42 / d == 42 / d)
            {
                s_marker = 1;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Cast(object a)
    {
        try
        {
            s_sink = ((string)a).Length;
        }
        finally
        {
            if (((string)a).Length >= 0)
            {
                s_marker = 1;
            }
        }
    }

    // A finally with its own exception handling is not cloned, independent of configuration.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void NullCheckNestedHandler(int[] a)
    {
        try
        {
            s_sink = a.Length;
        }
        finally
        {
            if (a.Length == a.Length)
            {
                s_marker = 1;
            }

            try
            {
                Swallow();
            }
            catch (InvalidOperationException)
            {
                s_marker = 2;
            }
        }
    }

    // The check after a try that completed normally may still rely on the try's check.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NullCheckAfterTry(int[] a)
    {
        int length;
        try
        {
            length = a.Length;
        }
        finally
        {
            Swallow();
        }

        return length + a.Length;
    }

    [Fact]
    public static void NullCheckInFinally()
    {
        s_marker = 0;
        Assert.Throws<NullReferenceException>(() => NullCheck(null));
        Assert.Equal(0, s_marker);
    }

    [Fact]
    public static void DivisionInFinally()
    {
        s_marker = 0;
        Assert.Throws<DivideByZeroException>(() => Division(0));
        Assert.Equal(0, s_marker);
    }

    [Fact]
    public static void CastInFinally()
    {
        s_marker = 0;
        Assert.Throws<InvalidCastException>(() => Cast(new object()));
        Assert.Equal(0, s_marker);
    }

    [Fact]
    public static void NullCheckInFinallyWithNestedHandler()
    {
        s_marker = 0;
        Assert.Throws<NullReferenceException>(() => NullCheckNestedHandler(null));
        Assert.Equal(0, s_marker);
    }

    [Fact]
    public static void NullCheckAfterCompletedTry()
    {
        Assert.Equal(6, NullCheckAfterTry(new int[3]));
        Assert.Throws<NullReferenceException>(() => NullCheckAfterTry(null));
    }
}
