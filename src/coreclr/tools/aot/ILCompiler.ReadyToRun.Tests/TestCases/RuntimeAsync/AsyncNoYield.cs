// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncNoYield
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> AsyncButNoAwait()
    {
        return 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> AsyncWithConditionalAwait(bool doAwait)
    {
        if (doAwait)
            await Task.Yield();
        return 1;
    }
}
