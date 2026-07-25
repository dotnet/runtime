// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class CompositeAsyncGenericTypesMain
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallGenericContainerInt()
    {
        var c = new GenericContainer<int>(42);
        return await c.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallGenericContainerString()
    {
        var c = new GenericContainer<string>("hello");
        return await c.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallGenericMethodOnGenericTypeIntLong()
    {
        var c = new GenericContainer<int>(7);
        return await c.CombineAsync<long>(11L);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallGenericMethodOnGenericTypeStringObject()
    {
        var c = new GenericContainer<string>("k");
        return await c.CombineAsync<object>("v");
    }
}
