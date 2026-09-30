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

    static async Task CheckNoThrow(string testName, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAILED: {testName} — threw {ex.GetType().Name}: {ex.Message}");
            s_failed = true;
        }
    }

    [RuntimeAsyncMethodGeneration(false)] // Use async1 codegen so calls exercise the Task/ValueTask-returning entry points rather than runtime-async variants.
    public static async Task<int> Main()
    {
        // Task-returning thunk inlining (non-async caller → async callee)
        Check("TaskReturningThunk", 20, await CallAsyncAndGetResult(10));
        await CheckNoThrow("TaskReturningThunkVoid", CallAsyncVoid);
        Check("ValueTaskReturningThunk", 10, await CallAsyncValueTask(5));

        // Async variant inlining (async caller → async callee)
        Check("AsyncVariant", 14, await AsyncCallerOfSmallAsync(7));
        await CheckNoThrow("AsyncVariantVoid", AsyncCallerOfSmallAsyncVoid);
        Check("AsyncVariantValueTask", 6, await AsyncCallerOfSmallValueTaskAsync(3));

        // Non-runtime-async (traditional state machine) inlining
        Check("NonRuntimeAsyncNotAwaited", 8, await CallNonRuntimeAsync(4));
        Check("NonRuntimeAsyncAwaited", 12, await AwaitNonRuntimeAsync(6));
        Check("NonRuntimeValueTaskNotAwaited", 18, await CallNonRuntimeValueTaskAsync(9));
        Check("NonRuntimeValueTaskAwaited", 22, await AwaitNonRuntimeValueTaskAsync(11));

        // Cross-module async inlining (helper.dll)
        Check("CrossModuleAsyncVariant", 15, await CrossModuleAsyncCaller(5));
        await CheckNoThrow("CrossModuleAsyncVariantVoid", CrossModuleAsyncCallerVoid);
        Check("CrossModuleAsyncVariantValueTask", 12, await CrossModuleAsyncCallerValueTask(4));
        Check("CrossModuleTaskReturningThunk", 24, await CrossModuleCallAsync(8));

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
