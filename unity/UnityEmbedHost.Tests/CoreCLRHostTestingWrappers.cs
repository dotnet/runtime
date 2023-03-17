// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

/// <summary>
/// Wrappers around CoreCLRHost methods that make it easier to focus on writing tests
/// </summary>
static class CoreCLRHostTestingWrappers
{
    public static nint gchandle_new_v2(object obj, bool pinned)
        => CoreCLRHost.gchandle_new_v2(obj.ToNativeRepresentation(), pinned);

    public static object? gchandle_get_target_v2(nint handleIn)
        => CoreCLRHost.gchandle_get_target_v2(handleIn).ToManagedRepresentation();

    public static object? object_isinst(object obj, Type klass)
        => CoreCLRHost.object_isinst(obj.ToNativeRepresentation(), klass.TypeHandleIntPtr()).ToManagedRepresentation();

    public static Type object_get_class(object obj)
        => CoreCLRHost.object_get_class(obj.ToNativeRepresentation()).TypeFromHandleIntPtr();
}
