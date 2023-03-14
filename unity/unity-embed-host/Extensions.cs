// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unity.CoreCLRHelpers;

static class Extensions
{
    public static Type TypeFromHandleIntPtr(this nint intPtrToTypeHandle)
        => Type.GetTypeFromHandle(RuntimeTypeHandle.FromIntPtr(intPtrToTypeHandle));

    public static nint TypeHandleIntPtr(this object obj)
        => obj.GetType().TypeHandleIntPtr();

    public static nint TypeHandleIntPtr(this Type type)
        => RuntimeTypeHandle.ToIntPtr(type.TypeHandle);

    public static T UnsafeAs<T>(this nint intPtr)
        => Unsafe.As<nint, T>(ref intPtr);

    public static nint UnsafeAsIntPtr(this object obj)
        => obj.UnsafeAs<nint>();

    public static T UnsafeAs<T>(this object obj)
        => Unsafe.As<object, T>(ref obj);

    public static GCHandle ToGCHandle(this nint intPtr)
        => GCHandle.FromIntPtr(intPtr);
}
