// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

public class Async2Reflection
{
    [Fact]
    public static void DynamicInvoke()
    {
        var mi = typeof(Async2Reflection).GetMethod("Foo", BindingFlags.Static | BindingFlags.NonPublic)!;
        Task<int> r = (Task<int>)mi.Invoke(null, null)!;

        dynamic d = new Async2Reflection();

        Assert.Equal(100, (int)(r.Result + d.Bar().Result));
    }

#pragma warning disable SYSLIB5007 // 'System.Runtime.CompilerServices.AsyncHelpers' is for evaluation purposes only
    [Fact]
    public static void DynamicInvokeAsync()
    {
        var mi = typeof(System.Runtime.CompilerServices.AsyncHelpers).GetMethod("Await", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(Task) })!;
        Assert.NotNull(mi);
        Assert.Throws<TargetInvocationException>(() => mi.Invoke(null, new object[] { FooTask() }));

        // Sadly the following does not throw and results in UB
        // We cannot completely prevent putting a token of an Async method into IL stream.
        // CONSIDER: perhaps JIT could throw?
        //
        // dynamic d = FooTask();
        // System.Runtime.CompilerServices.AsyncHelpers.Await(d);
    }
#pragma warning restore SYSLIB5007

    private static async Task<int> Foo()
    {
        await Task.Yield();
        return 90;
    }

    private static async Task FooTask()
    {
        await Task.Yield();
    }

    private async Task<int> Bar()
    {
        await Task.Yield();
        return 10;
    }

    [Fact]
    public static void DynamicLambda()
    {
        var expr1 = (Expression<Func<Task<int>>>)(() => Task.FromResult(42));
        var del = expr1.Compile();
        Assert.Equal(42, del().Result);

        AwaitF(42, del).GetAwaiter().GetResult();
    }

    static async Task AwaitF<T>(T expected, Func<Task<T>> f)
    {
        var res = await f.Invoke();
        Assert.Equal(expected, res);
    }
}
