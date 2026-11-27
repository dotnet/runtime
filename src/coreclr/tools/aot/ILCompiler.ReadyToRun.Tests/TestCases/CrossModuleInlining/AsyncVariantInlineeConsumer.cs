// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public struct LocalValue { public int Value; }

/// <summary>
/// Consumer that first inlines AsyncVariantInlineeLib's task-returning methods synchronously, which
/// rewrites their IL into the mutable module, and then awaits them from a generic runtime-async method
/// that is only compiled in a later wave, so that the async variants wrap the rewritten IL.
/// </summary>
public static class AsyncVariantInlineeConsumer
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task<int> ReturnTask() => AsyncVariantInlineeLib.GetValueTask(1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ValueTask<int> ReturnValueTask(CancellationToken cancellationToken) => AsyncVariantInlineeLib.GetCanceledOrValue(2, cancellationToken);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> AwaitInlined<T>(T value, CancellationToken cancellationToken)
    {
        int result = await AsyncVariantInlineeLib.GetValueTask(41);
        result += await AsyncVariantInlineeLib.GetCanceledOrValue(42, cancellationToken);
        return result + (value is null ? 0 : 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> AwaitLocal(CancellationToken cancellationToken)
    {
        return await AwaitInlined(new LocalValue { Value = 3 }, cancellationToken);
    }
}
