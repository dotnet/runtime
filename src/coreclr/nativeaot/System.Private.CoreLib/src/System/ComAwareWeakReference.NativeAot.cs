// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS

namespace System
{
    internal sealed partial class ComAwareWeakReference
    {
        // We don't want to consult ComWrappers if no RCW objects have been created.
        // In addition we don't want a direct reference to ComWrappers to allow for better
        // trimming.  So we instead make use of delegates that ComWrappers registers when
        // it is used.
        private static unsafe delegate*<IntPtr, long, object?> s_comWeakRefToObjectCallback;
        private static unsafe delegate*<object, bool> s_possiblyComObjectCallback;
        private static unsafe delegate*<object, out long, IntPtr> s_objectToComWeakRefCallback;

        internal static unsafe void InitializeCallbacks(
            delegate*<IntPtr, long, object?> comWeakRefToObject,
            delegate*<object, bool> possiblyComObject,
            delegate*<object, out long, IntPtr> objectToComWeakRef)
        {
            s_comWeakRefToObjectCallback = comWeakRefToObject;
            s_objectToComWeakRefCallback = objectToComWeakRef;
            s_possiblyComObjectCallback = possiblyComObject;
        }

        internal static unsafe object? ComWeakRefToObject(IntPtr pComWeakRef, long wrapperId)
        {
            return s_comWeakRefToObjectCallback != null ? s_comWeakRefToObjectCallback(pComWeakRef, wrapperId) : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool PossiblyComObject(object target)
        {
            return s_possiblyComObjectCallback != null ? s_possiblyComObjectCallback(target) : false;
        }

        internal static unsafe IntPtr ObjectToComWeakRef(object target, out long wrapperId)
        {
            wrapperId = 0;
            return s_objectToComWeakRefCallback != null ? s_objectToComWeakRefCallback(target, out wrapperId) : IntPtr.Zero;
        }
    }
}
#endif
