// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncTransitiveLib
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> GetExternalValueAsync()
    {
        await Task.Yield();
        return AsyncExternalLib.ExternalValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<string> GetExternalLabelAsync()
    {
        var ext = new AsyncExternalLib.AsyncExternalType();
        await Task.Yield();
        return ext.Label;
    }
}
