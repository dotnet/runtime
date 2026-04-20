// Runtime-async methods that capture GC refs across an await point.
// These force ContinuationLayout fixup emission when they are called
// (and optionally cross-module inlined) by another assembly.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class LocalsCapturedAcrossAwait
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CaptureRefAcrossAwait()
    {
        object o = new object();
        string s = "captured";
        await Task.Yield();
        return s + o.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CaptureArrayAcrossAwait()
    {
        int[] arr = new int[] { 10, 20, 30 };
        string label = "sum";
        await Task.Yield();
        return arr[0] + arr[1] + label.Length;
    }
}
