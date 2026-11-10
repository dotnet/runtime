// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133860
{
    private class Value
    {
        public object Field;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<NullReferenceException>(() => Store(1, 5, null));
        Assert.Throws<IndexOutOfRangeException>(() => Store(1, 5, new Value()));
        Assert.Throws<NullReferenceException>(() => Store(1, 0, null));
        Store(1, 0, new Value { Field = new object() });
        Assert.Throws<DivideByZeroException>(() => StoreWithIndex(5, 0, null));
        Assert.Throws<NullReferenceException>(() => StoreWithIndex(5, 1, null));
        Assert.Throws<IndexOutOfRangeException>(() => StoreWithIndex(5, 1, new Value()));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object[] Make(int length) => new object[length];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Use(object[] array) { }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Store(int length, int index, Value value)
    {
        object[] array = Make(length);
        array[index] = value.Field;
        Use(array);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void StoreWithIndex(int dividend, int divisor, Value value)
    {
        object[] array = Make(1);
        array[dividend / divisor] = value.Field;
        Use(array);
    }
}
