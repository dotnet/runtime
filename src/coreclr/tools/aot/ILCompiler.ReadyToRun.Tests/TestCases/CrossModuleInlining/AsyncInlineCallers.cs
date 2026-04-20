// Six call sites that each invoke one of the AsyncInlineCandidatesLib methods.
// Each caller is marked NoInlining so it stays put as the inliner; the test
// then asserts which candidates get inlined into their respective callers.
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncInlineCallers
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task CallReturnTaskNoAwait()
    {
        await AsyncInlineCandidatesLib.ReturnTaskNoAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallReturnTaskPrimitiveNoAwait()
    {
        return await AsyncInlineCandidatesLib.ReturnTaskPrimitiveNoAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallReturnTaskClassNoAwait()
    {
        return await AsyncInlineCandidatesLib.ReturnTaskClassNoAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task CallReturnTaskWithAwait()
    {
        await AsyncInlineCandidatesLib.ReturnTaskWithAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallReturnTaskPrimitiveWithAwait()
    {
        return await AsyncInlineCandidatesLib.ReturnTaskPrimitiveWithAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallReturnTaskClassWithAwait()
    {
        return await AsyncInlineCandidatesLib.ReturnTaskClassWithAwait();
    }
}
