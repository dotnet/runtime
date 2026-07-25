// Dependency library for composite async tests.
// Contains runtime-async methods called from another assembly in composite mode.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncCompositeLib
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
        return "composite_async";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValueSync() => 99;
}
