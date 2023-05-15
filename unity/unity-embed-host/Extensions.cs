// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Unity.CoreCLRHelpers;

static class Extensions
{
    public static Type TypeFromHandleIntPtr(this nint intPtrToTypeHandle)
        => Type.GetTypeFromHandle(RuntimeTypeHandle.FromIntPtr(intPtrToTypeHandle));

    public static RuntimeMethodHandle MethodHandleFromHandleIntPtr(this nint intPtrToMethodHandle)
        => RuntimeMethodHandle.FromIntPtr(intPtrToMethodHandle);

    public static RuntimeFieldHandle FieldHandleFromHandleIntPtr(this nint intPtrToTypeHandle)
        => RuntimeFieldHandle.FromIntPtr(intPtrToTypeHandle);

    public static nint TypeHandleIntPtr(this object obj)
        => obj.GetType().TypeHandleIntPtr();

    public static nint TypeHandleIntPtr(this Type type)
        => type == null ? IntPtr.Zero : RuntimeTypeHandle.ToIntPtr(type.TypeHandle);

    public static nint MethodHandleIntPtr(this RuntimeMethodHandle handle)
        => RuntimeMethodHandle.ToIntPtr(handle);

    public static nint FieldHandleIntPtr(this RuntimeFieldHandle handle)
        => RuntimeFieldHandle.ToIntPtr(handle);

    public static GCHandle ToGCHandle(this nint intPtr)
        => GCHandle.FromIntPtr(intPtr);

    public static Assembly AssemblyFromGCHandleIntPtr(this nint intPtr) => (Assembly)intPtr.ToGCHandle().Target;
}
