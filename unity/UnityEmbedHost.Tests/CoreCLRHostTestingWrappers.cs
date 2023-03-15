// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

/// <summary>
/// Wrappers around CoreCLRHost methods that make it easier to focus on writing tests
/// </summary>
static class CoreCLRHostTestingWrappers
{
    public static nint gchandle_new_v2(object obj, bool pinned)
        => CoreCLRHost.gchandle_new_v2(Unsafe.As<object, IntPtr>(ref obj), pinned);

    public static object? gchandle_get_target_v2(nint handleIn)
    {
        var result = CoreCLRHost.gchandle_get_target_v2(handleIn);
        return Unsafe.As<IntPtr, object>(ref result);
    }
}
