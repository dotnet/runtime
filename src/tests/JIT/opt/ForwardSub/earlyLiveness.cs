// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

#nullable enable

public class EarlyLiveness_ForwardSub
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 100;
        int test1 = Test1();
        if (test1 != 0)
        {
            Console.WriteLine("Test1 returned {0}", test1);
            result = -1;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test1()
    {
        S1 s1 = new();
        S1 s2 = s1;
        return Foo(s1) + Foo(s2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Foo(S1 s)
    {
        int result = s.A;
        s.A = 1234;
        Consume(s);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T value) { }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public static void CompareAndPattern(int sample)
    {
        object? value = sample switch
        {
            0 => null,
            1 => 1,
            2 => 1u,
            3 => 1L,
            4 => "x",
            5 => 1.0,
            _ => new object(),
        };

        Assert.Equal(sample is 1 or 2 or 3, IsInteger(value));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsInteger(object? value)
    {
        bool result = value is not null;
        if (result)
        {
            result = value is int or uint or long;
        }

        return result;
    }

    [Theory]
    [InlineData(-1, -1)]
    [InlineData(-1, 0)]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, -1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public static void CompareResults(int left, int right)
    {
        Assert.Equal(left > 0 && right > 0, CompareAnd(left, right));
        Assert.Equal(left > 0 || right > 0, CompareOr(left, right));
        Assert.Equal(left > 0 && right > 0, AddressExposedCompare(left, right));
        Assert.Equal(left > 0, HandlerCompare(left, right));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CompareAnd(int left, int right)
    {
        bool result = left > 0;
        if (result)
        {
            result = right > 0;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CompareOr(int left, int right)
    {
        bool result = left > 0;
        if (!result)
        {
            result = right > 0;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AddressExposedCompare(int left, int right)
    {
        bool result = left > 0;
        if (result)
        {
            result = right > 0;
        }

        return Read(ref result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Read(ref bool value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool HandlerCompare(int left, int right)
    {
        bool result = left > 0;
        try
        {
            if (result)
            {
                Consume(1 / right);
            }
        }
        catch (DivideByZeroException)
        {
            return result;
        }

        return result;
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.NaN)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 10)]
    public static void FloatingCompareResults(double left, double right)
    {
        Assert.Equal(left < right && right < 10, FloatingCompareAnd(left, right));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool FloatingCompareAnd(double left, double right)
    {
        bool result = left < right;
        if (result)
        {
            result = right < 10;
        }

        return result;
    }

    private struct S1
    {
        public int A, B, C, D, E;
    }
}
