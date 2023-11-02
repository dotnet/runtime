// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
public class Async2SharedGeneric
{
    static Type s_type;
    [Fact]
    public static void TestEntryPoint()
    {
        int[] arr = new int[1];
        AsyncTestEntryPoint<int>().Wait();
        Assert.Equal(typeof(int), s_type);
        AsyncTestEntryPoint<string>().Wait();
        Assert.Equal(typeof(string), s_type);
        AsyncTestEntryPoint<object>().Wait();
        Assert.Equal(typeof(object), s_type);
    }
    private static async Task AsyncTestEntryPoint<T>()
    {
        await Async2TestEntryPoint<T>();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async2 void Async2TestEntryPoint<T>()
    {
        s_type = typeof(T);
    }
}
