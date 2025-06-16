// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

public class Async2Reflection
{
    [Fact]
    public static void TestEntryPoint()
    {
        var mi = typeof(Async2Reflection).GetMethod("Foo", BindingFlags.Static | BindingFlags.NonPublic)!;
        Task<int> r = (Task<int>)mi.Invoke(null, null)!;

        dynamic d = new Async2Reflection();

        Assert.Equal(100, (int)(r.Result + d.Bar().Result));
    }

    private static async Task<int> Foo()
    {
        await Task.Yield();
        return 90;
    }

    private async Task<int> Bar()
    {
        await Task.Yield();
        return 10;
    }
}
