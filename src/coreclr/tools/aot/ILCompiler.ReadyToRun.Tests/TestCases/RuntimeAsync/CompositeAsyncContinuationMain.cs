// Test: Composite mode async with continuation layouts and resumption stubs.
// Calls async methods from AsyncCompositeContLib that capture GC refs across
// await points, exercising composite-mode ContinuationLayout and RESUME stub emission.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class CompositeAsyncContinuationMain
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallCaptureRefComposite()
    {
        return await AsyncCompositeContLib.CaptureRefComposite();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallCaptureArrayComposite()
    {
        return await AsyncCompositeContLib.CaptureArrayComposite();
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
