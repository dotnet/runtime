// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;

using Internal.Runtime;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Class for managing wrappers of COM IUnknown types.
    /// </summary>
    public abstract partial class ComWrappers
    {
        internal sealed unsafe partial class ManagedObjectWrapperHolder
        {
            static partial void RegisterIsRootedCallback()
            {
                delegate* unmanaged<IntPtr, bool> callback = &IsRootedCallback;
                if (!RuntimeImports.RhRegisterRefCountedHandleCallback((nint)callback, MethodTable.Of<ManagedObjectWrapperHolder>()))
                {
                    throw new OutOfMemoryException();
                }
            }

            [UnmanagedCallersOnly]
            private static bool IsRootedCallback(IntPtr pObj)
            {
                // We are paused in the GC, so this is safe.
                ManagedObjectWrapperHolder* holder = (ManagedObjectWrapperHolder*)&pObj;
                return holder->_wrapper->IsRooted;
            }

            private static partial IntPtr AllocateRefCountedHandle(ManagedObjectWrapperHolder holder)
            {
                return RuntimeImports.RhHandleAllocRefCounted(holder);
            }
        }

        /// <summary>
        /// Get the runtime provided IUnknown implementation.
        /// </summary>
        /// <param name="fpQueryInterface">Function pointer to QueryInterface.</param>
        /// <param name="fpAddRef">Function pointer to AddRef.</param>
        /// <param name="fpRelease">Function pointer to Release.</param>
        public static unsafe partial void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
        {
            fpQueryInterface = (IntPtr)(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&ComWrappers.IUnknown_QueryInterface;
            fpAddRef = RuntimeImports.RhGetIUnknownAddRef(); // Implemented in C/C++ to avoid GC transitions
            fpRelease = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IUnknown_Release;
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IUnknown_QueryInterface(IntPtr pThis, Guid* guid, IntPtr* ppObject)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->QueryInterface(in *guid, out *ppObject);
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint IUnknown_Release(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            uint refcount = wrapper->Release();
            return refcount;
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerTarget_QueryInterface(IntPtr pThis, Guid* guid, IntPtr* ppObject)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->QueryInterfaceForTracker(in *guid, out *ppObject);
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint IReferenceTrackerTarget_AddRefFromReferenceTracker(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->AddRefFromReferenceTracker();
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint IReferenceTrackerTarget_ReleaseFromReferenceTracker(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->ReleaseFromReferenceTracker();
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint IReferenceTrackerTarget_Peg(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->Peg();
        }

        [UnmanagedCallersOnly]
        internal static unsafe uint IReferenceTrackerTarget_Unpeg(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->Unpeg();
        }

        private static partial IntPtr CreateDefaultIReferenceTrackerTargetVftbl()
        {
            unsafe
            {
                IntPtr* vftbl = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComWrappers), 7 * sizeof(IntPtr));
                vftbl[0] = (IntPtr)(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&ComWrappers.IReferenceTrackerTarget_QueryInterface;
                GetIUnknownImpl(out _, out vftbl[1], out vftbl[2]);
                vftbl[3] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IReferenceTrackerTarget_AddRefFromReferenceTracker;
                vftbl[4] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IReferenceTrackerTarget_ReleaseFromReferenceTracker;
                vftbl[5] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IReferenceTrackerTarget_Peg;
                vftbl[6] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.IReferenceTrackerTarget_Unpeg;
                return (IntPtr)vftbl;
            }
        }



        [UnmanagedCallersOnly]
        internal static unsafe int ITaggedImpl_IsCurrentVersion(IntPtr pThis, IntPtr version)
        {
            return version == (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&ITaggedImpl_IsCurrentVersion
                ? HResults.S_OK
                : HResults.E_FAIL;
        }

        private static partial IntPtr GetTaggedImplCurrentVersion()
        {
            unsafe
            {
                return (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&ITaggedImpl_IsCurrentVersion;
            }
        }
    }
}
