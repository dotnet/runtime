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
        private static unsafe delegate*<IntPtr, long, object?> ComWeakRefToObjectCallback;
        private static unsafe delegate*<object, bool> PossiblyComObjectCallback;
        private static unsafe delegate*<object, long*, IntPtr> ObjectToComWeakRefCallback;

        internal static unsafe void InitializeCallbacks(
            delegate*<IntPtr, long, object?> comWeakRefToObject,
            delegate*<object, bool> possiblyComObject,
            delegate*<object, long*, IntPtr> objectToComWeakRef)
        {
            // PossiblyComObjectCallback is initialized last to avoid any potential races
            // where functions are being called while initialization is happening.
            ComWeakRefToObjectCallback = comWeakRefToObject;
            ObjectToComWeakRefCallback = objectToComWeakRef;
            PossiblyComObjectCallback = possiblyComObject;
        }

        internal static unsafe object? ComWeakRefToObject(IntPtr pComWeakRef, long wrapperId)
        {
            return ComWeakRefToObjectCallback != null ? ComWeakRefToObjectCallback(pComWeakRef, wrapperId) : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool PossiblyComObject(object target)
        {
            return PossiblyComObjectCallback != null ? PossiblyComObjectCallback(target) : false;
        }

        internal static unsafe IntPtr ObjectToComWeakRef(object target, out long wrapperId)
        {
            wrapperId = 0;
            if (ObjectToComWeakRefCallback != null)
            {
                fixed (long* id = &wrapperId)
                    return ObjectToComWeakRefCallback(target, id);
            }

            return IntPtr.Zero;
        }
    }
}
#endif
