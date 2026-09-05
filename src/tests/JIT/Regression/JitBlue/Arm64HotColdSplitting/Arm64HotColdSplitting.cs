// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Arm64HotColdSplitting
{
    private sealed class MarkerException : Exception
    {
        public MarkerException(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private static volatile int s_sink;
    private static volatile int s_handlerExecutions;
    private static volatile int s_finallyExecutions;
    private static string s_caughtStackTrace;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Mix(int value, int salt)
    {
        return ((value << 5) + value) ^ salt;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RootAndCatch(int mode)
    {
        int value = s_sink;
        value = Mix(value, 1);
        value = Mix(value, 2);
        value = Mix(value, 3);
        value = Mix(value, 4);
        value = Mix(value, 5);
        value = Mix(value, 6);
        value = Mix(value, 7);
        value = Mix(value, 8);

        try
        {
            if (mode == 0)
            {
                s_sink = value;
                return value;
            }

            value = Mix(value, 9);
            value = Mix(value, 10);
            value = Mix(value, 11);
            value = Mix(value, 12);
            value = Mix(value, 13);
            value = Mix(value, 14);
            value = Mix(value, 15);
            value = Mix(value, 16);
            throw new MarkerException(value);
        }
        catch (MarkerException ex)
        {
            s_handlerExecutions++;
            value = Mix(ex.Value, 17);
            value = Mix(value, 18);
            value = Mix(value, 19);
            value = Mix(value, 20);
            value = Mix(value, 21);
            value = Mix(value, 22);
            value = Mix(value, 23);
            value = Mix(value, 24);
            s_caughtStackTrace = ex.StackTrace;

            if (mode == 2)
            {
                ThrowFromHandler(value);
            }

            s_sink = value;
            return value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FuncletsOnly(int mode)
    {
        int value = s_sink;
        value = Mix(value, 25);
        value = Mix(value, 26);
        value = Mix(value, 27);
        value = Mix(value, 28);
        value = Mix(value, 29);
        value = Mix(value, 30);
        value = Mix(value, 31);
        value = Mix(value, 32);

        try
        {
            return MayThrow(value, mode);
        }
        catch (MarkerException ex)
        {
            s_handlerExecutions++;
            value = Mix(ex.Value, 33);
            value = Mix(value, 34);
            value = Mix(value, 35);
            value = Mix(value, 36);
            value = Mix(value, 37);
            value = Mix(value, 38);
            value = Mix(value, 39);
            value = Mix(value, 40);

            if (mode == 2)
            {
                ThrowFromHandler(value);
            }

            s_sink = value;
            return value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RareFinally(bool runRarePath, bool throwFromFinally)
    {
        int value = s_sink;
        value = Mix(value, 41);
        value = Mix(value, 42);
        value = Mix(value, 43);
        value = Mix(value, 44);
        value = Mix(value, 45);
        value = Mix(value, 46);
        value = Mix(value, 47);
        value = Mix(value, 48);

        if (!runRarePath)
        {
            s_sink = value;
            return value;
        }

        try
        {
            value = Mix(value, 49);
            value = Mix(value, 50);
            value = Mix(value, 51);
            value = Mix(value, 52);
            value = Mix(value, 53);
            value = Mix(value, 54);
            value = Mix(value, 55);
            value = Mix(value, 56);
            throw new MarkerException(value);
        }
        finally
        {
            s_finallyExecutions++;
            value = Mix(value, 57);
            value = Mix(value, 58);
            value = Mix(value, 59);
            value = Mix(value, 60);
            value = Mix(value, 61);
            value = Mix(value, 62);
            value = Mix(value, 63);
            value = Mix(value, 64);
            s_sink = value;

            if (throwFromFinally)
            {
                ThrowFromFinally(value);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MayThrow(int value, int mode)
    {
        if (mode != 0)
        {
            throw new MarkerException(value);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowFromHandler(int value)
    {
        throw new InvalidOperationException($"handler {value}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowFromFinally(int value)
    {
        throw new InvalidOperationException($"finally {value}");
    }

    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 1_000; i++)
        {
            RootAndCatch(0);
            FuncletsOnly(0);
            RareFinally(false, false);
            System.Threading.Thread.Sleep(1);
        }

        RootAndCatch(1);
        Assert.Equal(1, s_handlerExecutions);
        Assert.Contains(nameof(RootAndCatch), s_caughtStackTrace);

        InvalidOperationException handlerException =
            Assert.Throws<InvalidOperationException>(() => RootAndCatch(2));
        Assert.Contains(nameof(RootAndCatch), handlerException.StackTrace);
        Assert.Contains(nameof(ThrowFromHandler), handlerException.StackTrace);

        FuncletsOnly(1);
        Assert.Equal(3, s_handlerExecutions);
        handlerException = Assert.Throws<InvalidOperationException>(() => FuncletsOnly(2));
        Assert.Contains(nameof(FuncletsOnly), handlerException.StackTrace);
        Assert.Contains(nameof(ThrowFromHandler), handlerException.StackTrace);

        MarkerException finallyException = Assert.Throws<MarkerException>(() => RareFinally(true, false));
        Assert.Equal(1, s_finallyExecutions);
        Assert.Contains(nameof(RareFinally), finallyException.StackTrace);

        InvalidOperationException nestedFinallyException =
            Assert.Throws<InvalidOperationException>(() => RareFinally(true, true));
        Assert.Contains(nameof(RareFinally), nestedFinallyException.StackTrace);
        Assert.Contains(nameof(ThrowFromFinally), nestedFinallyException.StackTrace);
        Assert.Equal(2, s_finallyExecutions);
    }
}
