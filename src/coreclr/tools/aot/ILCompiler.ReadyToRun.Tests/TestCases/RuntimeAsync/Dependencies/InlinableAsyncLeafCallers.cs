// Inlinable async helpers that forward through to SyncLeafMethods.
// Used to exercise transitive cross-module inlining for runtime-async.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class InlinableAsyncLeafCallers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> GetExternalValueAsync()
    {
        await Task.Yield();
        return SyncLeafMethods.ExternalValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<string> GetExternalLabelAsync()
    {
        var ext = new SyncLeafMethods.ExternalType();
        await Task.Yield();
        return ext.Label;
    }
}
