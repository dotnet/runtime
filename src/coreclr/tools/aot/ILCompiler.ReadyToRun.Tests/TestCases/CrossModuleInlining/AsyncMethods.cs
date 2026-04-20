// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncMethods
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> TestAsyncInline()
    {
        return await AsyncInlineableLib.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> TestAsyncStringInline()
    {
        return await AsyncInlineableLib.GetStringAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestSyncFromAsyncLib()
    {
        return AsyncInlineableLib.GetValueSync();
    }
}
