// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators

// Each method has the same IL in V1 and V2; only the async impl flag differs. When the method is not
// runtime-async, awaiting it yields null. When it is runtime-async, the returned Task is the result.
public static class AsyncFlagHelper
{
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
}
