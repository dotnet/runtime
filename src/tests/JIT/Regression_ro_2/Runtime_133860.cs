// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133860
{
    private class C
    {
        public object F;
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 5)]
    public static void TestEntryPoint(int length, int index)
    {
        Assert.Throws<NullReferenceException>(() => Store(length, index, null));
        Assert.Throws<NullReferenceException>(() => StoreWithIndexExpression(length, index, 1, null));
        Assert.Throws<DivideByZeroException>(() => StoreWithIndexExpression(length, index, 0, null));

        C c = new C { F = new object() };
        if ((uint)index < (uint)length)
        {
            Assert.Same(c.F, Store(length, index, c)[index]);
            Assert.Same(c.F, StoreWithIndexExpression(length, index, 1, c)[index]);
        }
        else
        {
            Assert.Throws<IndexOutOfRangeException>(() => Store(length, index, c));
            Assert.Throws<IndexOutOfRangeException>(() => StoreWithIndexExpression(length, index, 1, c));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object[] Make(int length) => new object[length];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static object[] Store(int length, int index, C c)
    {
        object[] array = Make(length);
        array[index] = c.F;
        return array;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static object[] StoreWithIndexExpression(int length, int index, int divisor, C c)
    {
        object[] array = Make(length);
        array[index / divisor] = c.F;
        return array;
    }
}
