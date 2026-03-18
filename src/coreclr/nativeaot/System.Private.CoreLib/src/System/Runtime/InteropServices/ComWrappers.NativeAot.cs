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
            private static void RegisterIsRootedCallback()
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

            private static IntPtr AllocateRefCountedHandle(ManagedObjectWrapperHolder holder)
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
        public static unsafe void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
        {
            fpQueryInterface = (IntPtr)(delegate* unmanaged[MemberFunction]<IntPtr, Guid*, IntPtr*, int>)&ComWrappers.IUnknown_QueryInterface;
            fpAddRef = (IntPtr)(delegate*<IntPtr, uint>)&RuntimeImports.RhIUnknown_AddRef; // Implemented in C/C++ to avoid GC transitions
            fpRelease = (IntPtr)(delegate* unmanaged[MemberFunction]<IntPtr, uint>)&ComWrappers.IUnknown_Release;
        }

        internal static unsafe void GetUntrackedIUnknownImpl(out delegate* unmanaged[MemberFunction]<IntPtr, uint> fpAddRef, out delegate* unmanaged[MemberFunction]<IntPtr, uint> fpRelease)
        {
            // Implemented in C/C++ to avoid GC transitions during shutdown
            fpAddRef = (delegate* unmanaged[MemberFunction]<IntPtr, uint>)(void*)(delegate*<IntPtr, uint>)&RuntimeImports.RhUntracked_AddRefRelease;
            fpRelease = (delegate* unmanaged[MemberFunction]<IntPtr, uint>)(void*)(delegate*<IntPtr, uint>)&RuntimeImports.RhUntracked_AddRefRelease;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        internal static unsafe int IUnknown_QueryInterface(IntPtr pThis, Guid* guid, IntPtr* ppObject)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            return wrapper->QueryInterface(in *guid, out *ppObject);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        internal static unsafe uint IUnknown_Release(IntPtr pThis)
        {
            ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
            uint refcount = wrapper->Release();
            return refcount;
        }

        private static IntPtr GetTaggedImplCurrentVersion()
        {
            unsafe
            {
                return (IntPtr)(delegate* unmanaged[MemberFunction]<IntPtr, IntPtr, int>)&VtableImplementations.ITaggedImpl_IsCurrentVersion;
            }
        }

        internal static unsafe IntPtr DefaultIUnknownVftblPtr => (IntPtr)Unsafe.AsPointer(in VtableImplementations.IUnknown);
        internal static unsafe IntPtr TaggedImplVftblPtr => (IntPtr)Unsafe.AsPointer(in VtableImplementations.ITaggedImpl);
        internal static unsafe IntPtr DefaultIReferenceTrackerTargetVftblPtr => (IntPtr)Unsafe.AsPointer(in VtableImplementations.IReferenceTrackerTarget);

        /// <summary>
        /// Define the vtable layout for the COM interfaces we provide.
        /// </summary>
        /// <remarks>
        /// This is defined as a nested class to ensure that the vtable types are the only things initialized in the class's static constructor.
        /// As long as that's the case, we can easily guarantee that they are pre-initialized and that we don't end up having startup code
        /// needed to set up the vtable layouts.
        /// </remarks>
        private static class VtableImplementations
        {
            public unsafe struct IUnknownVftbl
            {
                public delegate* unmanaged[MemberFunction]<IntPtr, Guid*, IntPtr*, int> QueryInterface;
                public delegate* unmanaged[MemberFunction]<IntPtr, int> AddRef;
                public delegate* unmanaged[MemberFunction]<IntPtr, uint> Release;
            }

            public unsafe struct IReferenceTrackerTargetVftbl
            {
                public delegate* unmanaged[MemberFunction]<IntPtr, Guid*, IntPtr*, int> QueryInterface;
                public delegate* unmanaged[MemberFunction]<IntPtr, int> AddRef;
                public delegate* unmanaged[MemberFunction]<IntPtr, uint> Release;
                public delegate* unmanaged[MemberFunction]<IntPtr, uint> AddRefFromReferenceTracker;
                public delegate* unmanaged[MemberFunction]<IntPtr, uint> ReleaseFromReferenceTracker;
                public delegate* unmanaged[MemberFunction]<IntPtr, uint> Peg;
                public delegate* unmanaged[MemberFunction]<IntPtr, uint> Unpeg;
            }

            public unsafe struct ITaggedImplVftbl
            {
                public delegate* unmanaged[MemberFunction]<IntPtr, Guid*, IntPtr*, int> QueryInterface;
                public delegate* unmanaged[MemberFunction]<IntPtr, int> AddRef;
                public delegate* unmanaged[MemberFunction]<IntPtr, uint> Release;
                public delegate* unmanaged[MemberFunction]<IntPtr, IntPtr, int> IsCurrentVersion;
            }

            [FixedAddressValueType]
            public static readonly IUnknownVftbl IUnknown;

            [FixedAddressValueType]
            public static readonly IReferenceTrackerTargetVftbl IReferenceTrackerTarget;

            [FixedAddressValueType]
            public static readonly ITaggedImplVftbl ITaggedImpl;

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            internal static unsafe int IReferenceTrackerTarget_QueryInterface(IntPtr pThis, Guid* guid, IntPtr* ppObject)
            {
                ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
                return wrapper->QueryInterfaceForTracker(in *guid, out *ppObject);
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            internal static unsafe uint IReferenceTrackerTarget_AddRefFromReferenceTracker(IntPtr pThis)
            {
                ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
                return wrapper->AddRefFromReferenceTracker();
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            internal static unsafe uint IReferenceTrackerTarget_ReleaseFromReferenceTracker(IntPtr pThis)
            {
                ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
                return wrapper->ReleaseFromReferenceTracker();
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            internal static unsafe uint IReferenceTrackerTarget_Peg(IntPtr pThis)
            {
                ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
                return wrapper->Peg();
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            internal static unsafe uint IReferenceTrackerTarget_Unpeg(IntPtr pThis)
            {
                ManagedObjectWrapper* wrapper = ComInterfaceDispatch.ToManagedObjectWrapper((ComInterfaceDispatch*)pThis);
                return wrapper->Unpeg();
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            internal static unsafe int ITaggedImpl_IsCurrentVersion(IntPtr pThis, IntPtr version)
            {
                return version == (IntPtr)(delegate* unmanaged[MemberFunction]<IntPtr, IntPtr, int>)&ITaggedImpl_IsCurrentVersion
                    ? HResults.S_OK
                    : HResults.E_FAIL;
            }

            static unsafe VtableImplementations()
            {
                // Use the "pre-inited vtable" pattern to ensure that ILC can pre-compile these vtables.
                GetIUnknownImpl(
                    fpQueryInterface: out *(nint*)&((IUnknownVftbl*)Unsafe.AsPointer(ref IUnknown))->QueryInterface,
                    fpAddRef: out *(nint*)&((IUnknownVftbl*)Unsafe.AsPointer(ref IUnknown))->AddRef,
                    fpRelease: out *(nint*)&((IUnknownVftbl*)Unsafe.AsPointer(ref IUnknown))->Release);

                IReferenceTrackerTarget.QueryInterface = (delegate* unmanaged[MemberFunction]<IntPtr, Guid*, IntPtr*, int>)&IReferenceTrackerTarget_QueryInterface;
                GetIUnknownImpl(
                    fpQueryInterface: out _,
                    fpAddRef: out *(nint*)&((IReferenceTrackerTargetVftbl*)Unsafe.AsPointer(ref IReferenceTrackerTarget))->AddRef,
                    fpRelease: out *(nint*)&((IReferenceTrackerTargetVftbl*)Unsafe.AsPointer(ref IReferenceTrackerTarget))->Release);
                IReferenceTrackerTarget.AddRefFromReferenceTracker = (delegate* unmanaged[MemberFunction]<IntPtr, uint>)&IReferenceTrackerTarget_AddRefFromReferenceTracker;
                IReferenceTrackerTarget.ReleaseFromReferenceTracker = (delegate* unmanaged[MemberFunction]<IntPtr, uint>)&IReferenceTrackerTarget_ReleaseFromReferenceTracker;
                IReferenceTrackerTarget.Peg = (delegate* unmanaged[MemberFunction]<IntPtr, uint>)&IReferenceTrackerTarget_Peg;
                IReferenceTrackerTarget.Unpeg = (delegate* unmanaged[MemberFunction]<IntPtr, uint>)&IReferenceTrackerTarget_Unpeg;

                GetIUnknownImpl(
                    fpQueryInterface: out *(nint*)&((ITaggedImplVftbl*)Unsafe.AsPointer(ref ITaggedImpl))->QueryInterface,
                    fpAddRef: out *(nint*)&((ITaggedImplVftbl*)Unsafe.AsPointer(ref ITaggedImpl))->AddRef,
                    fpRelease: out *(nint*)&((ITaggedImplVftbl*)Unsafe.AsPointer(ref ITaggedImpl))->Release);
                ITaggedImpl.IsCurrentVersion = (delegate* unmanaged[MemberFunction]<IntPtr, IntPtr, int>)&ITaggedImpl_IsCurrentVersion;
            }
        }
    }
}
