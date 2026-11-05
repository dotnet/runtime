// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133715
{
    [Fact]
    public static void TestEntryPoint()
    {
        Test<int>();
        Test<Guid>();
    }

    private static void Test<T>()
    {
        T[] array = new T[1];
        Assert.Equal(typeof(T), Get(array, 0));
        Assert.Throws<IndexOutOfRangeException>(() => Get(array, -1));
        Assert.Throws<IndexOutOfRangeException>(() => Get(array, 1));
        Assert.Throws<NullReferenceException>(() => Get<T>(null, 0));
        Assert.Throws<NullReferenceException>(() => Get(ref Unsafe.NullRef<T>()));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Type Get<T>(T[] array, int index) => array[index].GetType();

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Type Get<T>(ref T value) => value.GetType();
}
