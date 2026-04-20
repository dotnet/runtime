// Awaits each method in InlinableAsyncMethods so that callers see both
// cross-module inlining opportunities and composite-mode async variant
// emission, depending on how the tests wire the compilation.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AwaitsInlinableAsync
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallGetValueAsync()
    {
        return await InlinableAsyncMethods.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallGetStringAsync()
    {
        return await InlinableAsyncMethods.GetStringAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallGetValueSync()
    {
        return InlinableAsyncMethods.GetValueSync();
    }
}
