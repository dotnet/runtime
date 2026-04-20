// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncTransitiveMain
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallTransitiveValueAsync()
    {
        return await AsyncTransitiveLib.GetExternalValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallTransitiveLabelAsync()
    {
        return await AsyncTransitiveLib.GetExternalLabelAsync();
    }
}
