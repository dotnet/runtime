// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncCrossModuleContinuation
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallCrossModuleCaptureRef()
    {
        return await AsyncDepLibContinuation.CaptureRefAcrossAwait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallCrossModuleCaptureArray()
    {
        return await AsyncDepLibContinuation.CaptureArrayAcrossAwait();
    }
}
