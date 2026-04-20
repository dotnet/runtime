// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class CompositeAsyncMain
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallCompositeAsync()
    {
        return await AsyncCompositeLib.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallCompositeStringAsync()
    {
        return await AsyncCompositeLib.GetStringAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallCompositeSync()
    {
        return AsyncCompositeLib.GetValueSync();
    }
}
