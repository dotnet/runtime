// Awaits through InlinableAsyncLeafCallers so the transitive reference to
// SyncLeafMethods is exercised under runtime-async.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AwaitsTransitiveAsync
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallTransitiveValueAsync()
    {
        return await InlinableAsyncLeafCallers.GetExternalValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallTransitiveLabelAsync()
    {
        return await InlinableAsyncLeafCallers.GetExternalLabelAsync();
    }
}
