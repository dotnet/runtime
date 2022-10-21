// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
namespace System
{
    internal sealed partial class ComAwareWeakReference
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? ComWeakRefToObject(IntPtr pComWeakRef, long wrapperId);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr ObjectToComWeakRef(object target, out long wrapperId);
    }
}
#endif
