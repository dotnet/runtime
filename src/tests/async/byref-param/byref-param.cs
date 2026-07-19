// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2ByrefParam
{
    [Fact]
    public static int TestByrefParam()
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

    [Fact]
    public static int TestByrefParamWithSuspension()
    {
        return TestWithSuspension().GetAwaiter().GetResult();
    }

    private static async Task<int> TestWithSuspension()
    {
        await Verify(new string('a', 100), out int val);
        return val;
    }

    // NoInlining ensures a suspension point in Verify
    // (otherwise this would just tail-await)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task Verify(string source, out int length)
    {
        length = source.Length;
        return DoAsync();
    }

    private static async Task DoAsync() => await Task.Yield();
}
