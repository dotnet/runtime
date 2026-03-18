// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace System
{
    internal sealed partial class ComAwareWeakReference
    {
        // We don't want to consult ComWrappers if no RCW objects have been created.
        // In addition we don't want a direct reference to ComWrappers to allow for better
        // trimming.  So we instead make use of delegates that ComWrappers registers when
        // it is used.
        private static unsafe delegate*<IntPtr, object?, object?> s_comWeakRefToObjectCallback;
        private static unsafe delegate*<object, bool> s_possiblyComObjectCallback;
        private static unsafe delegate*<object, out object?, IntPtr> s_objectToComWeakRefCallback;

        internal static unsafe void InitializeCallbacks(
            delegate*<IntPtr, object?, object?> comWeakRefToObject,
            delegate*<object, bool> possiblyComObject,
            delegate*<object, out object?, IntPtr> objectToComWeakRef)
        {
            s_comWeakRefToObjectCallback = comWeakRefToObject;
            s_objectToComWeakRefCallback = objectToComWeakRef;
            s_possiblyComObjectCallback = possiblyComObject;
        }

        internal static unsafe object? ComWeakRefToComWrappersObject(IntPtr pComWeakRef, object? context)
        {
            return s_comWeakRefToObjectCallback != null ? s_comWeakRefToObjectCallback(pComWeakRef, context) : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool PossiblyComWrappersObject(object target)
        {
            return s_possiblyComObjectCallback != null ? s_possiblyComObjectCallback(target) : false;
        }

        internal static unsafe IntPtr ComWrappersObjectToComWeakRef(object target, out object? context)
        {
            context = null;
            return s_objectToComWeakRefCallback != null ? s_objectToComWeakRefCallback(target, out context) : IntPtr.Zero;
        }
    }
}
