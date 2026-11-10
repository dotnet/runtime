// Middle library in async transitive chain: AsyncTransitiveMain → AsyncTransitiveLib → AsyncExternalLib.
// Contains runtime-async methods that reference types from AsyncExternalLib.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncTransitiveLib
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> GetExternalValueAsync()
    {
        await Task.Yield();
        return AsyncExternalLib.ExternalValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<string> GetExternalLabelAsync()
    {
        var ext = new AsyncExternalLib.AsyncExternalType();
        await Task.Yield();
        return ext.Label;
    }
}
