// Test: Async method that captures GC refs across await
// This forces the compiler to emit a ContinuationLayout fixup.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncWithContinuation
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CaptureObjectAcrossAwait()
    {
        object o = new object();
        string s = "hello";
        await Task.Yield();
        return s + o.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CaptureMultipleRefsAcrossAwait()
    {
        int[] arr = new int[] { 1, 2, 3 };
        string text = "world";
        await Task.Yield();
        return arr[0] + text.Length;
    }
}
