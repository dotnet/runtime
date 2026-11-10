// Test: Composite mode with runtime-async methods across assemblies.
// Validates that async methods produce [ASYNC] variants in composite output.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class CompositeAsyncMain
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallCompositeAsync()
    {
        return await AsyncCompositeLib.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallCompositeStringAsync()
    {
        return await AsyncCompositeLib.GetStringAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallCompositeSync()
    {
        return AsyncCompositeLib.GetValueSync();
    }
}
