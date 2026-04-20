// Test: Cross-module inlining of async methods
// Validates that async methods from AsyncInlineableLib are cross-module
// inlined into this assembly with CHECK_IL_BODY fixups.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncMethods
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> TestAsyncInline()
    {
        return await AsyncInlineableLib.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> TestAsyncStringInline()
    {
        return await AsyncInlineableLib.GetStringAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestSyncFromAsyncLib()
    {
        return AsyncInlineableLib.GetValueSync();
    }
}
