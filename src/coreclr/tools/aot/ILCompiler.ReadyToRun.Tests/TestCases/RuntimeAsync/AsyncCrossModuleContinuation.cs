// Test: Non-composite runtime-async cross-module inlining with continuation layouts.
// The dependency methods capture GC refs across await points.
// Validates manifest refs and [ASYNC] variants for cross-module async calls.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncCrossModuleContinuation
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallCrossModuleCaptureRef()
    {
        return await AsyncDepLibContinuation.CaptureRefAcrossAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallCrossModuleCaptureArray()
    {
        return await AsyncDepLibContinuation.CaptureArrayAcrossAwait();
    }
}
