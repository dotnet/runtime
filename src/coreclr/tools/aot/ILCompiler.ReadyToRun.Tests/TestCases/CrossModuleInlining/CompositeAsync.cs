// Test: Composite mode with runtime-async methods across assemblies.
// Validates that async methods produce [ASYNC] variants in composite output.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class CompositeAsyncMain
{
    public static int Main()
    {
        int sync = CallCompositeSync();
        if (sync != 99)
            return 1;

        return 0;
    }

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
