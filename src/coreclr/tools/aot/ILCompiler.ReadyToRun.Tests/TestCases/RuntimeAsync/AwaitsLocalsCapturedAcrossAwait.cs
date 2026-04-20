// Awaits the methods in LocalsCapturedAcrossAwait, plus a self-contained
// async method that captures locals across an await in this assembly itself.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AwaitsLocalsCapturedAcrossAwait
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallCaptureRefAcrossAwait()
    {
        return await LocalsCapturedAcrossAwait.CaptureRefAcrossAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallCaptureArrayAcrossAwait()
    {
        return await LocalsCapturedAcrossAwait.CaptureArrayAcrossAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> LocalCaptureAcrossAwait()
    {
        object o = new object();
        string s = "local";
        await Task.Yield();
        return s.Length + o.GetHashCode();
    }
}
