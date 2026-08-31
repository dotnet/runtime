// Test: Basic async method emission in R2R
// Validates that runtime-async methods produce [ASYNC] variant entries
// in the R2R image.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class BasicAsyncEmission
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> SimpleAsyncMethod()
    {
        await Task.Yield();
        return 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task AsyncVoidReturn()
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async ValueTask<string> ValueTaskMethod()
    {
        await Task.Yield();
        return "hello";
    }

    // Non-async method that returns Task (no await) — should NOT get async variant
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task<int> SyncTaskReturning()
    {
        return Task.FromResult(1);
    }
}
