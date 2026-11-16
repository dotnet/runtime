// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

// Loops in handlers must preserve locals and pending return values.
public class OSRHandlerLoop
{
    private const int LoopIterations = 10_000;

    [Fact]
    public static int TestEntryPoint()
    {
        int result = 0;
        int expected = 0;
        try
        {
            result++;
            expected = 704982705;
        }
        finally
        {
            for (int i = 0; i < 100_000; i++)
            {
                result += i;
            }
        }

        Console.WriteLine($"{result} expected {expected}");

        return (result == expected) ? 100 : -1;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-42)]
    [InlineData(int.MaxValue)]
    public static void ReturnValueSurvivesFinallyLoop(int value)
    {
        Assert.Equal(value, ReturnFromTryWithFinallyLoop(value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-42)]
    [InlineData(int.MaxValue)]
    public static void LocalSurvivesFinallyLoop(int value)
    {
        Assert.Equal(value, UseLocalAfterFinallyLoop(value));
    }

    [Theory]
    [InlineData(42)]
    [InlineData(0)]
    public static void ReturnValueSurvivesNestedFinallyLoop(int value)
    {
        Assert.Equal(value, ReturnFromTryWithNestedFinallyLoop(value));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(42, false)]
    [InlineData(42, true)]
    public static void ReturnValueSurvivesMixedFinallyHandlers(int value, bool loop)
    {
        Assert.Equal(value, ReturnFromMixedFinallyHandlers(value, loop));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReturnFromTryWithFinallyLoop(int value)
    {
        try
        {
            return Identity(value);
        }
        finally
        {
            for (int i = 0; i < LoopIterations; i++)
            {
                Thread.SpinWait(1);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int UseLocalAfterFinallyLoop(int value)
    {
        int saved = Identity(value);
        try
        {
            Thread.SpinWait(1);
        }
        finally
        {
            for (int i = 0; i < LoopIterations; i++)
            {
                Thread.SpinWait(1);
            }
        }

        return Identity(saved);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReturnFromTryWithNestedFinallyLoop(int value)
    {
        try
        {
            return Identity(value);
        }
        finally
        {
            int saved = Identity(value);
            try
            {
                Thread.SpinWait(1);
            }
            finally
            {
                for (int i = 0; i < LoopIterations; i++)
                {
                    Thread.SpinWait(1);
                }
            }

            Assert.Equal(value, Identity(saved));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReturnFromMixedFinallyHandlers(int value, bool loop)
    {
        if (loop)
        {
            try
            {
                return Identity(value);
            }
            finally
            {
                for (int i = 0; i < LoopIterations; i++)
                {
                    Thread.SpinWait(1);
                }
            }
        }

        try
        {
            return Identity(value);
        }
        finally
        {
            Thread.SpinWait(1);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Identity(int value) => value;
}
