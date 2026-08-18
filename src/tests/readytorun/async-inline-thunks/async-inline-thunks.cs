// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class InlineThunks
{
    static bool s_failed;

    static void Check(string testName, int expected, int actual)
    {
        if (expected != actual)
        {
            Console.WriteLine($"  FAILED: {testName} — expected {expected}, got {actual}");
            s_failed = true;
        }
    }

    static void CheckNoThrow(string testName, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAILED: {testName} — threw {ex.GetType().Name}: {ex.Message}");
            s_failed = true;
        }
    }

    public static int Main()
    {
        // Task-returning thunk inlining (non-async caller → async callee)
        Check("TaskReturningThunk", 20, CallAsyncAndGetResult(10).GetAwaiter().GetResult());
        CheckNoThrow("TaskReturningThunkVoid", () => CallAsyncVoid().GetAwaiter().GetResult());
        Check("ValueTaskReturningThunk", 10, CallAsyncValueTask(5).GetAwaiter().GetResult());

        // Async variant inlining (async caller → async callee)
        Check("AsyncVariant", 14, AsyncCallerOfSmallAsync(7).GetAwaiter().GetResult());
        CheckNoThrow("AsyncVariantVoid", () => AsyncCallerOfSmallAsyncVoid().GetAwaiter().GetResult());
        Check("AsyncVariantValueTask", 6, AsyncCallerOfSmallValueTaskAsync(3).GetAwaiter().GetResult());

        // Non-runtime-async (traditional state machine) inlining
        Check("NonRuntimeAsyncNotAwaited", 8, CallNonRuntimeAsync(4).GetAwaiter().GetResult());
        Check("NonRuntimeAsyncAwaited", 12, AwaitNonRuntimeAsync(6).GetAwaiter().GetResult());
        Check("NonRuntimeValueTaskNotAwaited", 18, CallNonRuntimeValueTaskAsync(9).GetAwaiter().GetResult());
        Check("NonRuntimeValueTaskAwaited", 22, AwaitNonRuntimeValueTaskAsync(11).GetAwaiter().GetResult());

        // Cross-module async inlining (helper.dll)
        Check("CrossModuleAsyncVariant", 15, CrossModuleAsyncCaller(5).GetAwaiter().GetResult());
        CheckNoThrow("CrossModuleAsyncVariantVoid", () => CrossModuleAsyncCallerVoid().GetAwaiter().GetResult());
        Check("CrossModuleAsyncVariantValueTask", 12, CrossModuleAsyncCallerValueTask(4).GetAwaiter().GetResult());
        Check("CrossModuleTaskReturningThunk", 24, CrossModuleCallAsync(8).GetAwaiter().GetResult());

        // Cross-module sync baseline
        Check("CrossModuleSyncBaseline", 21, CrossModuleSyncCaller(7));

        if (!s_failed)
            Console.WriteLine("PASSED");
        else
            Console.WriteLine("FAILED");

        return s_failed ? 1 : 100;
    }

    // --- Task-returning thunk inlining targets ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<int> CallAsyncAndGetResult(int x) => SmallAsyncForThunk(x);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task CallAsyncVoid() => SmallAsyncVoidForThunk();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ValueTask<int> CallAsyncValueTask(int x) => SmallValueTaskAsyncForThunk(x);

    // --- Async variant inlining targets ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> AsyncCallerOfSmallAsync(int x) => await SmallAsyncForVariant(x);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task AsyncCallerOfSmallAsyncVoid() => await SmallAsyncVoidForVariant();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> AsyncCallerOfSmallValueTaskAsync(int x) => await SmallValueTaskAsyncForVariant(x);

    // --- Non-runtime-async callers ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<int> CallNonRuntimeAsync(int x) => SmallNonRuntimeAsync(x);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> AwaitNonRuntimeAsync(int x) => await SmallNonRuntimeAsync(x);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ValueTask<int> CallNonRuntimeValueTaskAsync(int x) => SmallNonRuntimeValueTaskAsync(x);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> AwaitNonRuntimeValueTaskAsync(int x) => await SmallNonRuntimeValueTaskAsync(x);

    // --- Small async methods (runtime-async, for thunk tests) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task<int> SmallAsyncForThunk(int x) => x * 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task SmallAsyncVoidForThunk() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async ValueTask<int> SmallValueTaskAsyncForThunk(int x) => x * 2;

    // --- Small async methods (runtime-async, for variant tests) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task<int> SmallAsyncForVariant(int x) => x * 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task SmallAsyncVoidForVariant() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async ValueTask<int> SmallValueTaskAsyncForVariant(int x) => x * 2;

    // --- Small async methods (non-runtime-async / traditional state machine) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RuntimeAsyncMethodGeneration(false)]
    private static async Task<int> SmallNonRuntimeAsync(int x) => x * 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RuntimeAsyncMethodGeneration(false)]
    private static async ValueTask<int> SmallNonRuntimeValueTaskAsync(int x) => x * 2;

    // --- Cross-module async callers ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> CrossModuleAsyncCaller(int x) => await CrossModuleHelper.SmallCrossModuleAsync(x);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task CrossModuleAsyncCallerVoid() => await CrossModuleHelper.SmallCrossModuleAsyncVoid();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> CrossModuleAsyncCallerValueTask(int x) => await CrossModuleHelper.SmallCrossModuleValueTaskAsync(x);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<int> CrossModuleCallAsync(int x) => CrossModuleHelper.SmallCrossModuleAsync(x);

    // --- Cross-module sync baseline ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CrossModuleSyncCaller(int x) => CrossModuleHelper.SmallCrossModuleSync(x);
}
