// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncCrossModule
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallCrossModuleAsync()
    {
        return await AsyncDepLib.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallCrossModuleStringAsync()
    {
        return await AsyncDepLib.GetStringAsync();
    }

    // Call a non-async sync method from async lib
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallCrossModuleSync()
    {
        return AsyncDepLib.GetValueSync();
    }
}
