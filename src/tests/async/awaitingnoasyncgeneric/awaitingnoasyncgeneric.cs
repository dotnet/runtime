// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class AwaitingNoAsyncGeneric
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal("hello", new Caller<string>().RunAsync("hello").GetAwaiter().GetResult());
        Assert.Equal(42, new Caller<int>().RunAsync(42).GetAwaiter().GetResult());
    }
}

public class Caller<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask<T> RunAsync(T value)
    {
        return await Helper.GetValueAsync(value).ConfigureAwait(false);
    }
}

public static class Helper
{
    // Non-async method returning ValueTask<T>, so the compiler does not emit
    // MethodImplAttributes.Async. This is the condition that can trigger a
    // scanner/JIT mismatch.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ValueTask<T> GetValueAsync<T>(T value)
    {
        return new ValueTask<T>(value);
    }
}
