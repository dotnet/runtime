// Awaits a small subset of InlineCandidateMatrix methods (one Task<int>, one
// Task<string>, one sync) so that cross-module and composite tests can observe
// async-variant emission and cross-module inlining of inlinable async methods.
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AwaitsInlinableAsync
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallGetValueAsync()
    {
        return await InlineCandidateMatrix.ReturnTaskPrimitiveWithAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallGetStringAsync()
    {
        return await InlineCandidateMatrix.ReturnTaskClassWithAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallGetValueSync()
    {
        return InlineCandidateMatrix.GetValueSync();
    }
}
