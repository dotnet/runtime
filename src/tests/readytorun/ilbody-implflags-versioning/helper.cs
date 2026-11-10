// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators

// Each method has the same IL in V1 and V2; only an impl flag differs.
public static class ImplFlagHelper
{
    // Awaiting a non-runtime-async method yields null; awaiting a runtime-async one yields the returned Task.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if V2
    public static async Task<object> BecomesAsync()
#else
    public static Task<object> BecomesAsync()
#endif
    {
        return Task.FromResult<object>(null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if V2
    public static Task<object> StopsBeingAsync()
#else
    public static async Task<object> StopsBeingAsync()
#endif
    {
        return Task.FromResult<object>(null);
    }

#if V2
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.Synchronized)]
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static bool BecomesSynchronized()
    {
        return Monitor.IsEntered(typeof(ImplFlagHelper));
    }
}
