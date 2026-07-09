// Test: Non-composite runtime-async transitive cross-module inlining.
// Chain: AsyncTransitiveMain → AsyncTransitiveLib → AsyncExternalLib.
// Validates transitive manifest refs and async cross-module inlining.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncTransitiveMain
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallTransitiveValueAsync()
    {
        return await AsyncTransitiveLib.GetExternalValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallTransitiveLabelAsync()
    {
        return await AsyncTransitiveLib.GetExternalLabelAsync();
    }
}
