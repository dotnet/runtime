// Inlinable runtime-async methods used to exercise cross-module inlining
// and composite-mode emission of async variants.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class InlinableAsyncMethods
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> GetValueAsync()
    {
        await Task.Yield();
        return 42;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<string> GetStringAsync()
    {
        await Task.Yield();
        return "hello_from_async";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValueSync() => 99;
}
