// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

public class Runtime_134304
{
    [Fact]
    public static void TestEntryPoint()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    private static ValueTask DisposeAsync()
    {
        return Await(Task.CompletedTask);
    }

    private static async ValueTask Await(Task task)
    {
        await task.ConfigureAwait(false);
    }
}
