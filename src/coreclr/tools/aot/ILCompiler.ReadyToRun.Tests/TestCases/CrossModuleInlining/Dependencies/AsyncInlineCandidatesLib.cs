// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncInlineCandidatesLib
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
