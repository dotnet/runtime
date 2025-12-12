// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Varargs
{
    [Fact]
    public static void TestTaskDirectCall()
    {
        TaskWithArglist(42, __arglist(100, 200)).Wait();
    }

    [Fact]
    public static void TestTaskAwait()
    {
        TestTaskAwaitAsync().Wait();
    }

    private static async Task TestTaskAwaitAsync()
    {
        await TaskWithArglist(42, __arglist(100, 200));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task TaskWithArglist(int fixedArg, __arglist)
    {
        await Task.Yield();
        Assert.Equal(42, fixedArg);

        ArgIterator args = new ArgIterator(__arglist);
        Assert.Equal(2, args.GetRemainingCount());

        int arg1 = __refvalue(args.GetNextArg(), int);
        int arg2 = __refvalue(args.GetNextArg(), int);

        Assert.Equal(100, arg1);
        Assert.Equal(200, arg2);
    }

    [Fact]
    public static void TestValueTaskDirectCall()
    {
        int result = ValueTaskWithArglist(42, __arglist(100, 200)).Result;
        Assert.Equal(342, result);
    }

    [Fact]
    public static void TestValueTaskAwait()
    {
        TestValueTaskAwaitAsync().Wait();
    }

    private static async Task TestValueTaskAwaitAsync()
    {
        int result = await ValueTaskWithArglist(42, __arglist(100, 200));
        Assert.Equal(342, result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async ValueTask<int> ValueTaskWithArglist(int fixedArg, __arglist)
    {
        await Task.Yield();
        Assert.Equal(42, fixedArg);

        ArgIterator args = new ArgIterator(__arglist);
        Assert.Equal(2, args.GetRemainingCount());

        int arg1 = __refvalue(args.GetNextArg(), int);
        int arg2 = __refvalue(args.GetNextArg(), int);

        Assert.Equal(100, arg1);
        Assert.Equal(200, arg2);

        return fixedArg + arg1 + arg2;
    }
}
