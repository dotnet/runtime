// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class AwaitNotAsync
{
    [Fact]
    public static void TestEntryPoint()
    {
        AsyncEntryPoint().Wait();
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    private static async Task<T> GetTask<T>(T arg)
    {
        await Task.Yield();
        return arg;
    }

    // TODO: switch every other scenario to use ValueTask
    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    private static async ValueTask<T> GetValueTask<T>(T arg)
    {
        await Task.Yield();
        return arg;
    }

    private static Task<int> sField;

    private static Task<int> sProp => GetTask(6);

    private static T sIdentity<T>(T arg) => arg;

    private static async Task AsyncEntryPoint()
    {
        // static field
        sField = GetTask(5);
        Assert.Equal(5, await sField);

        // property
        Assert.Equal(6, await sProp);

        // generic identity
        Assert.Equal(6, await sIdentity(sProp));

        // await(await ...))
        Assert.Equal(7, await await await GetTask(GetTask(GetTask(7))));

    }
}
