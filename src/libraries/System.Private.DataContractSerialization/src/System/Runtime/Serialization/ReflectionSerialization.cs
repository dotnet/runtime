// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Runtime.Serialization;

internal static class ReflectionSerialization
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Complete(ValueTask operation)
    {
        Debug.Assert(operation.IsCompletedSuccessfully, "Synchronous serialization must succeed or throw before returning.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Complete<T>(ValueTask<T> operation)
    {
        Debug.Assert(operation.IsCompleted, "Synchronous serialization must not suspend.");
        return operation.Result;
    }
}
