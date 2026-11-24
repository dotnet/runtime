// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134473
{
    private const int PaddingSize = 100_000;

    [InlineArray(PaddingSize)]
    private struct Buffer
    {
        private byte _element0;
    }

    private struct Big
    {
        public Buffer Padding;
        public int Value;
    }

    private class C
    {
        public int Small;
        public Big Big;
    }

    [Theory]
    [InlineData(0, 45)]
    [InlineData(1, 58)]
    [InlineData(2, 58)]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void TestEntryPoint(int test, int expected)
    {
        var value = new C { Small = 7, Big = new Big { Value = 11 } };
        var other = new C { Small = 13 };
        Func<C, C, int, int> invoke = test switch
        {
            0 => NonThrowingDependent,
            1 => ThrowingDependent,
            _ => MultipleGuards,
        };

        Assert.Equal(expected, invoke(value, other, 5));
        Assert.Throws<NullReferenceException>(() => invoke(null, other, 5));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NonThrowingDependent(C value, C other, int addend)
    {
        return Add(value.Small, value.Big.Value * 3 + addend);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int ThrowingDependent(C value, C other, int addend)
    {
        return Add(value.Small, value.Big.Value * 3 + other.Small + addend);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int MultipleGuards(C value, C other, int addend)
    {
        return Add(value.Small, other.Small, value.Big.Value * 3 + addend);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Add(int first, int second)
    {
        return first + second;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Add(int first, int second, int third)
    {
        return first + second + third;
    }
}
