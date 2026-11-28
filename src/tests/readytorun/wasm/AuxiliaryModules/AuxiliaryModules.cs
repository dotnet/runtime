// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

/// <summary>
/// Runs ordinary code from an image compiled with --auxiliary-modules. Every method is still in the
/// primary module, so this checks that the extra metadata leaves the image loadable and runnable.
/// </summary>
public class AuxiliaryModules
{
    private interface IShape
    {
        int Area();
    }

    private sealed class Rectangle(int width, int height) : IShape
    {
        public int Area() => width * height;
    }

    private struct Square(int side) : IShape
    {
        public int Area() => side * side;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Fibonacci(int n) => n < 2 ? n : Fibonacci(n - 1) + Fibonacci(n - 2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Max<T>(T left, T right) where T : IComparable<T> => left.CompareTo(right) >= 0 ? left : right;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumAreas<T>(IEnumerable<T> shapes) where T : IShape
    {
        int total = 0;
        foreach (T shape in shapes)
        {
            total += shape.Area();
        }

        return total;
    }

    private static int s_finallyCount;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ThrowAndCatch(int value)
    {
        try
        {
            throw new InvalidOperationException(value.ToString());
        }
        catch (InvalidOperationException ex)
        {
            return int.Parse(ex.Message) + 1;
        }
        finally
        {
            s_finallyCount++;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(55, Fibonacci(10));
        Assert.Equal(7, Max(3, 7));
        Assert.Equal("pear", Max("apple", "pear"));
        Assert.Equal(26, SumAreas(new IShape[] { new Rectangle(2, 5), new Square(4) }));
        Assert.Equal(13, SumAreas(new List<Square> { new Square(2), new Square(3) }));
        Assert.Equal(42, ThrowAndCatch(41));
        Assert.Equal(1, s_finallyCount);

        Func<int, int> doubler = static x => x * 2;
        Assert.Equal(84, doubler(42));
    }
}
