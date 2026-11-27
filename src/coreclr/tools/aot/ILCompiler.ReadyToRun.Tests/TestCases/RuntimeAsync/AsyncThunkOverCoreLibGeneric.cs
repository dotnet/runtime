// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test: Task-returning thunk of an in-module runtime-async generic method
// instantiated over a shared System.Private.CoreLib generic struct.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncThunkOverCoreLibGeneric
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<T> PassThrough<T>(T value)
    {
        await Task.Yield();
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task<ValueTuple<object, object>> CallPassThrough(object first, object second)
        => PassThrough((first, second));
}
