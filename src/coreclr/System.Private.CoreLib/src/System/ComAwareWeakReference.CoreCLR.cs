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
        private static partial void ComWeakRefToObject(IntPtr pComWeakRef, ObjectHandleOnStack retRcw);

        internal static object? ComWeakRefToObject(IntPtr pComWeakRef, object? context)
        {
#if FEATURE_COMINTEROP
            if (context is null)
            {
                // This wrapper was not created by ComWrappers, so we try to rehydrate using built-in COM.
                object? retRcw = null;
                ComWeakRefToObject(pComWeakRef, ObjectHandleOnStack.Create(ref retRcw));
                return retRcw;
            }
#endif // FEATURE_COMINTEROP

            return ComWeakRefToComWrappersObject(pComWeakRef, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool PossiblyComObject(object target)
        {
#if FEATURE_COMINTEROP
            return target is __ComObject || PossiblyComWrappersObject(target);
#else // !FEATURE_COMINTEROP
            // If we are not using built-in COM, then we can only be a ComWrappers object.
            return PossiblyComWrappersObject(target);
#endif // FEATURE_COMINTEROP
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjectToComWeakRef")]
        private static partial IntPtr ObjectToComWeakRef(ObjectHandleOnStack retRcw);

        internal static nint ObjectToComWeakRef(object target, out object? context)
        {
#if FEATURE_COMINTEROP
            if (target is __ComObject)
            {
                // This object is using built-in COM, so use built-in COM to create the weak reference.
                context = null;
                return ObjectToComWeakRef(ObjectHandleOnStack.Create(ref target));
            }
#endif // FEATURE_COMINTEROP

            if (PossiblyComWrappersObject(target))
            {
                return ComWrappersObjectToComWeakRef(target, out context);
            }

            // This object is not produced using built-in COM or ComWrappers
            // or is an aggregated object, so we cannot create a weak reference.
            context = null;
            return IntPtr.Zero;
        }
    }
}
#endif
