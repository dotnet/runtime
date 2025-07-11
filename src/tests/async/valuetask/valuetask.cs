// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2valuetask
{
    [Fact]
    public static int TestEntryPoint()
    {
        return (int)AsyncTestEntryPoint(100).Result;
    }

    private static ValueTask<int> AsyncTestEntryPoint(int arg)
    {
        return M1(arg);
    }

    private static async ValueTask<int> M1(int arg)
    {
        await Task.Yield();
        return arg;
    }
}
