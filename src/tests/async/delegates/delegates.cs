// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Delegates
{
    [Fact]
    public static void TestEntryPoint()
    {
        var p = new Async2Delegates();
        Assert.Equal(30, RunAsync(p.AddCompilerAsync).Result);
        Assert.Equal(30, RunAsync(p.AddRuntimeAsync).Result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<int> RunAsync(AddTwo del)
    {
        return await del(10, 20);
    }

    delegate Task<int> AddTwo(int x, int y);

    [RuntimeAsyncMethodGeneration(false)]
    async Task<int> AddCompilerAsync(int x, int y) => x + y;
    [RuntimeAsyncMethodGeneration(true)]
    async Task<int> AddRuntimeAsync(int x, int y) => x + y;
}
