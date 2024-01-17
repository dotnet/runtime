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
    }

    // Validate that the type equality involving shared array types is handled correctly
    // in shared generic code.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool foo<T>(string[] list) => typeof(T[]) == list.GetType();
}
