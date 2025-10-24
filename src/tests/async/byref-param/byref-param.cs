// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

public class Async2ByrefParam
{
    [Fact]
    public static int TestEntryPoint()
    {
        return Test().GetAwaiter().GetResult();
    }

    private static async Task<int> Test()
    {
        return await HasByrefParam(out int val) + val;
    }

    private static Task<int> HasByrefParam(out int foo)
    {
        foo = 47;
        return Task.FromResult(53);
    }
}
