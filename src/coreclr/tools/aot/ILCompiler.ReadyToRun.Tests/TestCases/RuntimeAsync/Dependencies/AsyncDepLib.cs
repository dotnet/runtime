// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncDepLib
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> GetValueAsync()
    {
        await Task.Yield();
        return 42;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<string> GetStringAsync()
    {
        await Task.Yield();
        return "async_hello";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValueSync()
    {
        return 99;
    }
}
