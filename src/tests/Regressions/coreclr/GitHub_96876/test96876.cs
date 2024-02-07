// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test96876
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.True(foo<string>(new string[1]));
        Assert.False(foo<object>(new string[1]));

        Assert.True(foo2<string>());
        Assert.False(foo2<object>());
    }

    // Validate that the type equality involving shared array types is handled correctly
    // in shared generic code.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool foo<T>(string[] list) => typeof(T[]) == list.GetType();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool foo2<T>() => typeof(T[]) == typeof(string[]);
}
