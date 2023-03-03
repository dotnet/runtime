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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool PossiblyComObject(object target)
        {
            // see: syncblk.h
            const int IS_HASHCODE_BIT_NUMBER = 26;
            const int BIT_SBLK_IS_HASHCODE = 1 << IS_HASHCODE_BIT_NUMBER;
            const int BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX = 0x08000000;

            fixed (byte* pRawData = &target.GetRawData())
            {
                // The header is 4 bytes before MT field on all architectures
                int header = *(int*)(pRawData - sizeof(IntPtr) - sizeof(int));
                // common case: target does not have a syncblock, so there is no interop info
                return (header & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_IS_HASHCODE)) == BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool HasInteropInfo(object target);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjectToComWeakRef")]
        private static partial IntPtr ObjectToComWeakRef(ObjectHandleOnStack retRcw, out long wrapperId);

        internal static nint ObjectToComWeakRef(object target, out long wrapperId)
        {
            if (HasInteropInfo(target))
            {
                return ObjectToComWeakRef(ObjectHandleOnStack.Create(ref target), out wrapperId);
            }

            wrapperId = 0;
            return IntPtr.Zero;
        }
    }
}
#endif
