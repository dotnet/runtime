// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

public class Runtime_135922
{
    [Fact]
    public static async Task TestEntryPoint()
    {
#pragma warning disable xUnit1030 // The compiler-inserted TransparentAwait call this test exercises is only produced for the ConfigureAwait(false) shape.
        // Regression: Task<ValueTask>
        var tcs = new TaskCompletionSource<ValueTask>();
        tcs.SetResult(ValueTask.CompletedTask);

        ValueTask inner = await tcs.Task.ConfigureAwait(false);
        await inner.ConfigureAwait(false);

        // Regression: Task<ValueTask<int>>
        var genericTcs = new TaskCompletionSource<ValueTask<int>>();
        genericTcs.SetResult(new ValueTask<int>(42));

        ValueTask<int> genericInner =
            await genericTcs.Task.ConfigureAwait(false);

        Assert.Equal(42, await genericInner.ConfigureAwait(false));
#pragma warning restore xUnit1030
    }
}
