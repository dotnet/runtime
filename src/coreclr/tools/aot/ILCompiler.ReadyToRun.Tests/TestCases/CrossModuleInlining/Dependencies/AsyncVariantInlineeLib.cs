// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Library whose synchronous task-returning methods are awaited from runtime-async code in another
/// module. The JIT inlines their async variants, whose IL is rewritten for cross-module inlining so
/// that its tokens refer to the compilation's mutable module rather than to this module.
/// </summary>
public static class AsyncVariantInlineeLib
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<int> GetValueTask(int value)
    {
        return Task.FromResult(value + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<int> GetCanceledOrValue(int value, CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled<int>(cancellationToken)
            : new ValueTask<int>(value);
    }
}
