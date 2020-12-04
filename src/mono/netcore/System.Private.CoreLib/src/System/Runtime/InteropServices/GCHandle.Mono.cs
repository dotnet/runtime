// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public partial struct GCHandle
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr InternalAlloc(object? value, GCHandleType type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void InternalFree(IntPtr handle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object? InternalGet(IntPtr handle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void InternalSet(IntPtr handle, object? value);
    }
}
