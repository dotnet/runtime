// Dependency library exposing six runtime-async inlining candidates that cross
// the {Task, Task<int>, Task<string>} x {with-await, without-await} matrix.
// The JIT cannot inline an async method that performs an actual await
// (Compiler::impSetupAsyncCall reports CALLEE_AWAIT FATAL in importercalls.cpp;
// AsyncSuspend reports CALLEE_ASYNC_SUSPEND FATAL). Methods without an await
// are eligible for inlining like any other small method.
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class InlineCandidateMatrix
{
    // --- Awaitless variants: should be inlinable ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task ReturnTaskNoAwait()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> ReturnTaskPrimitiveNoAwait() => 42;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<string> ReturnTaskClassNoAwait() => "no_await";

    // --- Variants containing an actual await: cannot be inlined by the JIT ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task ReturnTaskWithAwait()
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> ReturnTaskPrimitiveWithAwait()
    {
        await Task.Yield();
        return 42;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<string> ReturnTaskClassWithAwait()
    {
        await Task.Yield();
        return "with_await";
    }
}
