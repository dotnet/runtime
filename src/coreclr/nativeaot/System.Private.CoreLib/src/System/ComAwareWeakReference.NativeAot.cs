// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS

namespace System
{
    internal sealed partial class ComAwareWeakReference
    {
        private static readonly Guid IID_IInspectable = new Guid(0xAF86E2E0, 0xB12D, 0x4c6a, 0x9C, 0x5A, 0xD7, 0xAA, 0x65, 0x10, 0x1E, 0x90);
        private static readonly Guid IID_IWeakReferenceSource = new Guid(0x00000038, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);

        // We don't want to consult the ComWrappers if no RCW objects have been created.
        // So we instead use this variable which is set by ComWrappers to determine
        // whether RCW objects have been created before consulting it as part of PossiblyComObject.
        internal static bool ComWrappersRcwInitialized;

        internal static object? ComWeakRefToObject(IntPtr pComWeakRef, long wrapperId)
        {
            if (wrapperId == 0)
            {
                return null;
            }

            // Using the IWeakReference*, get ahold of the target native COM object's IInspectable*.  If this resolve fails or
            // returns null, then we assume that the underlying native COM object is no longer alive, and thus we cannot create a
            // new RCW for it.
            if (IWeakReference.Resolve(pComWeakRef, IID_IInspectable, out IntPtr targetPtr) == HResults.S_OK &&
                targetPtr != IntPtr.Zero)
            {
                using ComHolder target = new ComHolder(targetPtr);
                if (Marshal.QueryInterface(target.Ptr, in ComWrappers.IID_IUnknown, out IntPtr targetIdentityPtr) == HResults.S_OK)
                {
                    using ComHolder targetIdentity = new ComHolder(targetIdentityPtr);
                    return ComWrappers.GetOrCreateObjectFromWrapper(wrapperId, targetIdentity.Ptr);
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool PossiblyComObject(object target)
        {
            return ComWrappersRcwInitialized && ComWrappers.IsRcwObject(target);
        }

        internal static IntPtr ObjectToComWeakRef(object target, out long wrapperId)
        {
            if (ComWrappers.TryGetComInstanceForIID(
                target,
                IID_IWeakReferenceSource,
                out IntPtr weakReferenceSourcePtr,
                out bool isAggregated,
                out wrapperId))
            {
                // If the RCW is an aggregated RCW, then the managed object cannot be recreated from the IUnknown
                // as the outer IUnknown wraps the managed object. In this case, don't create a weak reference backed
                // by a COM weak reference.
                using ComHolder weakReferenceSource = new ComHolder(weakReferenceSourcePtr);
                if (!isAggregated && IWeakReferenceSource.GetWeakReference(weakReferenceSource.Ptr, out IntPtr weakReference) == HResults.S_OK)
                {
                    return weakReference;
                }
            }

            wrapperId = 0;
            return IntPtr.Zero;
        }
    }

    // Wrapper for IWeakReference
    internal static unsafe class IWeakReference
    {
        public static int Resolve(IntPtr pThis, Guid guid, out IntPtr inspectable)
        {
            fixed (IntPtr* inspectablePtr = &inspectable)
                return (*(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>**)pThis)[3](pThis, &guid, inspectablePtr);
        }
    }

    // Wrapper for IWeakReferenceSource
    internal static unsafe class IWeakReferenceSource
    {
        public static int GetWeakReference(IntPtr pThis, out IntPtr weakReference)
        {
            fixed (IntPtr* weakReferencePtr = &weakReference)
                return (*(delegate* unmanaged<IntPtr, IntPtr*, int>**)pThis)[3](pThis, weakReferencePtr);
        }
    }
}
#endif
