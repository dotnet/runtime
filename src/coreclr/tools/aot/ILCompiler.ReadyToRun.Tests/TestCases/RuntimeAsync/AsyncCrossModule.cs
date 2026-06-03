// Test: Cross-module async method inlining
// Validates that cross-module async compilation produces manifest refs
// and [ASYNC] variants for methods calling into a dependency library.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncCrossModule
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallCrossModuleAsync()
    {
        return await AsyncDepLib.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallCrossModuleStringAsync()
    {
        return await AsyncDepLib.GetStringAsync();
    }

    // Call a non-async sync method from async lib
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallCrossModuleSync()
    {
        return AsyncDepLib.GetValueSync();
    }
}
