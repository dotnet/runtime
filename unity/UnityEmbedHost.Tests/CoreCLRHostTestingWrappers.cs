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

    public static T[] array_new<T>(int length)
        => (T[])array_new(typeof(T), length);

    public static Array array_new(Type klass, int length)
        => (Array)CoreCLRHost.array_new(nint.Zero, klass.TypeHandleIntPtr(), length).ToManagedRepresentation();

    public static T[][] unity_array_new_2d<T>(int size0, int size1)
        => (T[][])unity_array_new_2d(typeof(T), size0, size1);

    public static Array unity_array_new_2d(Type klass, int size0, int size1)
        => (Array)CoreCLRHost.unity_array_new_2d(nint.Zero, klass.TypeHandleIntPtr(), size0, size1).ToManagedRepresentation();

    public static T[][][] unity_array_new_3d<T>(int size0, int size1, int size2)
        => (T[][][])unity_array_new_3d(typeof(T), size0, size1, size2);

    public static Array unity_array_new_3d(Type klass, int size0, int size1, int size2)
        => (Array)CoreCLRHost.unity_array_new_3d(nint.Zero, klass.TypeHandleIntPtr(), size0, size1, size2).ToManagedRepresentation();
}
