// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
namespace System
{
    internal sealed partial class ComAwareWeakReference
    {
        internal static object? ComWeakRefToObject(IntPtr pComWeakRef, long wrapperId)
        {
            // NativeAOT support for COM WeakReference is NYI
            throw new NotImplementedException();
        }

        internal static bool PossiblyComObject(object target)
        {
            // NativeAOT support for COM WeakReference is NYI
            return false;
        }

        internal static IntPtr ObjectToComWeakRef(object target, out long wrapperId)
        {
            // NativeAOT support for COM WeakReference is NYI
            wrapperId = 0;
            return 0;
        }
    }
}
#endif
