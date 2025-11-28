// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class AsyncCancellation
{
    [Fact]
    public static void TestEntryPoint()
    {
        Task t1 = M1();
        Assert.True(t1.IsCanceled);

        Task t2 = M2();
        Assert.True(t2.IsCanceled);

        ValueTask t1v = M1v();
        Assert.True(t1v.IsCanceled);

        ValueTask t2v = M2v();
        Assert.True(t2v.IsCanceled);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task M1()
    {
        await M2();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async Task M2()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        throw new OperationCanceledException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async ValueTask M1v()
    {
        await M2v();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    static async ValueTask M2v()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        throw new OperationCanceledException();
    }
}
