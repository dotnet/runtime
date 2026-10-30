// Dependency library for async cross-module continuation tests.
// Contains runtime-async methods that capture GC refs across await points,
// forcing ContinuationLayout fixup emission when cross-module inlined.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncDepLibContinuation
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<string> CaptureRefAcrossAwait()
    {
        object o = new object();
        string s = "cross_module";
        await Task.Yield();
        return s + o.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> CaptureArrayAcrossAwait()
    {
        int[] arr = new int[] { 10, 20, 30 };
        string label = "sum";
        await Task.Yield();
        return arr[0] + arr[1] + label.Length;
    }
}
