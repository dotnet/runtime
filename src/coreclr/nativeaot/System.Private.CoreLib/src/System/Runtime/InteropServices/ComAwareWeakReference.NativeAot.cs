// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace System
{
    internal sealed partial class ComAwareWeakReference
    {
        internal static unsafe object? ComWeakRefToObject(IntPtr pComWeakRef, object? context)
        {
            return ComWeakRefToComWrappersObject(pComWeakRef, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool PossiblyComObject(object target)
        {
            return PossiblyComWrappersObject(target);
        }

        internal static unsafe IntPtr ObjectToComWeakRef(object target, out object? context)
        {
            return ComWrappersObjectToComWeakRef(target, out context);
        }
    }
}
