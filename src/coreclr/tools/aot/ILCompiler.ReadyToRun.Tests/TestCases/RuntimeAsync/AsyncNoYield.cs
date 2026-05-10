// Test: Async method without yields (no suspension point)
// When a runtime-async method never actually awaits, crossgen2 may
// omit the resumption stub. This tests that edge case.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncNoYield
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> AsyncButNoAwait()
    {
        return 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> AsyncWithConditionalAwait(bool doAwait)
    {
        if (doAwait)
            await Task.Yield();
        return 1;
    }
}
