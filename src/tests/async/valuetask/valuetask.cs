// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2ValueTask
{
    [Fact]
    public static int TestBasic()
    {
        return (int)AsyncTestBasicEntryPoint(100).Result;
    }

    private static ValueTask<int> AsyncTestBasicEntryPoint(int arg)
    {
        return M1(arg);
    }

    private static async ValueTask<int> M1(int arg)
    {
        await Task.Yield();
        return arg;
    }

    [Fact]
    public static void RuntimeAsyncCallableThunks()
    {
        RuntimeAsyncCallableThunksAsync().GetAwaiter().GetResult();
    }

    private static async ValueTask RuntimeAsyncCallableThunksAsync()
    {
        int result = await Foo();
        Assert.Equal(123, result);
        await Bar();
        result = await Baz();
        Assert.Equal(456, result);
        string strResult = await Beef();
        Assert.Equal("foo", strResult);
    }

    private static ValueTask<int> Foo() => new ValueTask<int>(123);
    private static ValueTask Bar() => ValueTask.CompletedTask;

    [RuntimeAsyncMethodGeneration(false)]
    private static async ValueTask<int> Baz()
    {
        await Task.Yield();
        return 456;
    }

    [RuntimeAsyncMethodGeneration(false)]
    private static async ValueTask<string> Beef()
    {
        await Task.Yield();
        return "foo";
    }
}
