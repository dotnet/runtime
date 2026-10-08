// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
