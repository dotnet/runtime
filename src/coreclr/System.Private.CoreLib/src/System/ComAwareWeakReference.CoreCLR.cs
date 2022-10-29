// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
namespace System
{
    internal sealed partial class ComAwareWeakReference
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWeakRefToObject")]
        private static partial void ComWeakRefToObject(IntPtr pComWeakRef, long wrapperId, ObjectHandleOnStack retRcw);

        internal static object? ComWeakRefToObject(IntPtr pComWeakRef, long wrapperId)
        {
            object? retRcw = null;
            ComWeakRefToObject(pComWeakRef, wrapperId, ObjectHandleOnStack.Create(ref retRcw));
            return retRcw;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr ObjectToComWeakRef(object target, out long wrapperId);
    }
}
#endif
