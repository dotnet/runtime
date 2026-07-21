// Test: crossgen2 --strip-il-bodies IL preservation.
// Validates that non-async Task/ValueTask-returning methods, generic methods,
// and methods on generic types keep their IL, while plain and async methods are stripped.
// Also validates that a non-async Task-returning method whose async variant is compiled
// (because a runtime-async method awaits it) has its IL stripped, since the IL is no longer
// needed to synthesize the async variant at runtime.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class StripILBodies
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task<int> SyncTaskOfTForwarder(int value)
    {
        return Task.FromResult(value + 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ValueTask<int> SyncValueTaskOfTForwarder(int value)
    {
        return new ValueTask<int>(value + 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task SyncTaskForwarder()
    {
        return Task.CompletedTask;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ValueTask SyncValueTaskForwarder()
    {
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T GenericIdentity<T>(T value)
    {
        return value;
    }

    public static class GenericHolder<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int MethodOnGenericType(int a, int b)
        {
            return a + b;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> AsyncTaskMethod()
    {
        await Task.Yield();
        return 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async ValueTask AsyncValueTaskMethod()
    {
        await Task.Yield();
    }

    // Non-async Task-returning method whose async variant is compiled because
    // AwaitSyncTaskForcingAsyncVariant awaits it. Its IL should be stripped.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task<int> SyncTaskWithCompiledAsyncVariant(int value)
    {
        return Task.FromResult(value + 4);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> AwaitSyncTaskForcingAsyncVariant()
    {
        return await SyncTaskWithCompiledAsyncVariant(10);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int PlainStrippableMethod(int a, int b)
    {
        return a + b + ComputeTag();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ComputeTag()
    {
        return 12345;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Root()
    {
        return GenericIdentity<int>(1) + GenericHolder<string>.MethodOnGenericType(2, 3);
    }
}
