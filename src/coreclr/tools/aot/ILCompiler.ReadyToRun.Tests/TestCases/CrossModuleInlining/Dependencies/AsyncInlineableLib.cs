// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncInlineableLib
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> GetValueAsync() => 42;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<string> GetStringAsync() => "Hello from async";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValueSync() => 42;
}
