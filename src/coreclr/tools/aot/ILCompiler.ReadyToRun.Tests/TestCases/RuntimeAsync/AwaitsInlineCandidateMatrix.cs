// Six call sites that each invoke one of the InlineCandidateMatrix methods.
// Each caller is marked NoInlining so it stays put as the inliner; the test
// then asserts which candidates get inlined into their respective callers.
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AwaitsInlineCandidateMatrix
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task CallReturnTaskNoAwait()
    {
        await InlineCandidateMatrix.ReturnTaskNoAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallReturnTaskPrimitiveNoAwait()
    {
        return await InlineCandidateMatrix.ReturnTaskPrimitiveNoAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallReturnTaskClassNoAwait()
    {
        return await InlineCandidateMatrix.ReturnTaskClassNoAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task CallReturnTaskWithAwait()
    {
        await InlineCandidateMatrix.ReturnTaskWithAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallReturnTaskPrimitiveWithAwait()
    {
        return await InlineCandidateMatrix.ReturnTaskPrimitiveWithAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallReturnTaskClassWithAwait()
    {
        return await InlineCandidateMatrix.ReturnTaskClassWithAwait();
    }
}
