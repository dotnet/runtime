// Dependency library for composite async continuation tests.
// Contains runtime-async methods that capture GC refs across await points.
// Used in composite mode to exercise MutableModule token encoding for
// cross-module async continuation layouts and resumption stubs.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncCompositeContLib
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CaptureRefComposite()
    {
        object o = new object();
        string s = "composite_ref";
        await Task.Yield();
        return s + o.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CaptureArrayComposite()
    {
        int[] arr = new int[] { 5, 10, 15 };
        string label = "total";
        await Task.Yield();
        return arr[0] + arr[1] + label.Length;
    }
}
