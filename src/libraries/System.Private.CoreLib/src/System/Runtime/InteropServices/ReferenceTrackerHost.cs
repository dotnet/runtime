// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices
{
    internal static class ReferenceTrackerHost
    {
        [FixedAddressValueType]
        private static readonly unsafe IntPtr s_globalHostServices = (IntPtr)Unsafe.AsPointer(in HostServices.Vftbl);

        // Called when an IReferenceTracker instance is found.
        public static unsafe void SetReferenceTrackerHost(IntPtr trackerManager)
        {
            IReferenceTrackerManager.SetReferenceTrackerHost(trackerManager, (IntPtr)Unsafe.AsPointer(in s_globalHostServices));
        }

#pragma warning disable IDE0060, CS3016
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        internal static int IReferenceTrackerHost_DisconnectUnusedReferenceSources(IntPtr pThis, uint flags)
#pragma warning restore IDE0060, CS3016
        {
            try
            {
                // Defined in windows.ui.xaml.hosting.referencetracker.h.
                const uint XAML_REFERENCETRACKER_DISCONNECT_SUSPEND = 0x00000001;

                if ((flags & XAML_REFERENCETRACKER_DISCONNECT_SUSPEND) != 0)
                {
                    GC.Collect(2, GCCollectionMode.Optimized, blocking: true, compacting: false, lowMemoryPressure: true);
                }
                else
                {
                    GC.Collect();
                }
                return HResults.S_OK;
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }
        }

#pragma warning disable IDE0060, CS3016
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        internal static int IReferenceTrackerHost_ReleaseDisconnectedReferenceSources(IntPtr pThis)
#pragma warning restore IDE0060, CS3016
        {
            // We'd like to call GC.WaitForPendingFinalizers() here, but this could lead to deadlock
            // if the finalizer thread is trying to get back to this thread, because we are not pumping
            // anymore. Disable this for now. See: https://github.com/dotnet/runtime/issues/109538.
            return HResults.S_OK;
        }

#pragma warning disable IDE0060, CS3016
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        internal static int IReferenceTrackerHost_NotifyEndOfReferenceTrackingOnThread(IntPtr pThis)
#pragma warning restore IDE0060, CS3016
        {
            try
            {
                TrackerObjectManager.ReleaseExternalObjectsFromCurrentThread();
                return HResults.S_OK;
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }
        }

#pragma warning disable IDE0060, CS3016
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        internal static unsafe int IReferenceTrackerHost_GetTrackerTarget(IntPtr pThis, IntPtr punk, IntPtr* ppNewReference)
#pragma warning restore IDE0060, CS3016
        {
            if (punk == IntPtr.Zero)
            {
                return HResults.E_INVALIDARG;
            }

            if (Marshal.QueryInterface(punk, ComWrappers.IID_IUnknown, out IntPtr ppv) != HResults.S_OK)
            {
                return HResults.COR_E_INVALIDCAST;
            }

            try
            {
                using ComHolder identity = new ComHolder(ppv);
                using ComHolder trackerTarget = new ComHolder(ComWrappers.GetOrCreateTrackerTarget(identity.Ptr));
                return Marshal.QueryInterface(trackerTarget.Ptr, ComWrappers.IID_IReferenceTrackerTarget, out *ppNewReference);
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }
        }

#pragma warning disable IDE0060, CS3016
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        internal static int IReferenceTrackerHost_AddMemoryPressure(IntPtr pThis, long bytesAllocated)
#pragma warning restore IDE0060, CS3016
        {
            try
            {
                GC.AddMemoryPressure(bytesAllocated);
                return HResults.S_OK;
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }
        }

#pragma warning disable IDE0060, CS3016
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        internal static int IReferenceTrackerHost_RemoveMemoryPressure(IntPtr pThis, long bytesAllocated)
#pragma warning restore IDE0060, CS3016
        {
            try
            {
                GC.RemoveMemoryPressure(bytesAllocated);
                return HResults.S_OK;
            }
            catch (Exception e)
            {
                return Marshal.GetHRForException(e);
            }
        }

#pragma warning disable CS3016
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        internal static unsafe int IReferenceTrackerHost_QueryInterface(IntPtr pThis, Guid* guid, IntPtr* ppObject)
#pragma warning restore CS3016
        {
            if (*guid == ComWrappers.IID_IReferenceTrackerHost || *guid == ComWrappers.IID_IUnknown)
            {
                *ppObject = pThis;
                Marshal.AddRef(pThis);
                return HResults.S_OK;
            }
            else
            {
                return HResults.COR_E_INVALIDCAST;
            }
        }

        private unsafe struct IReferenceTrackerHostVftbl
        {
            public delegate* unmanaged[MemberFunction]<IntPtr, Guid*, IntPtr*, int> QueryInterface;
            public delegate* unmanaged[MemberFunction]<IntPtr, uint> AddRef;
            public delegate* unmanaged[MemberFunction]<IntPtr, uint> Release;
            public delegate* unmanaged[MemberFunction]<IntPtr, uint, int> DisconnectUnusedReferenceSources;
            public delegate* unmanaged[MemberFunction]<IntPtr, int> ReleaseDisconnectedReferenceSources;
            public delegate* unmanaged[MemberFunction]<IntPtr, int> NotifyEndOfReferenceTrackingOnThread;
            public delegate* unmanaged[MemberFunction]<IntPtr, IntPtr, IntPtr*, int> GetTrackerTarget;
            public delegate* unmanaged[MemberFunction]<IntPtr, long, int> AddMemoryPressure;
            public delegate* unmanaged[MemberFunction]<IntPtr, long, int> RemoveMemoryPressure;
        }

        private static class HostServices
        {
            [FixedAddressValueType]
            public static readonly IReferenceTrackerHostVftbl Vftbl;

            static unsafe HostServices()
            {
                Vftbl.QueryInterface = &IReferenceTrackerHost_QueryInterface;
                ComWrappers.GetUntrackedIUnknownImpl(out Vftbl.AddRef, out Vftbl.Release);
                Vftbl.DisconnectUnusedReferenceSources = &IReferenceTrackerHost_DisconnectUnusedReferenceSources;
                Vftbl.ReleaseDisconnectedReferenceSources = &IReferenceTrackerHost_ReleaseDisconnectedReferenceSources;
                Vftbl.NotifyEndOfReferenceTrackingOnThread = &IReferenceTrackerHost_NotifyEndOfReferenceTrackingOnThread;
                Vftbl.GetTrackerTarget = &IReferenceTrackerHost_GetTrackerTarget;
                Vftbl.AddMemoryPressure = &IReferenceTrackerHost_AddMemoryPressure;
                Vftbl.RemoveMemoryPressure = &IReferenceTrackerHost_RemoveMemoryPressure;
            }
        }
    }

    // Wrapper for IReferenceTrackerManager
    internal static unsafe class IReferenceTrackerManager
    {
        // Used during GC callback
        public static int ReferenceTrackingStarted(IntPtr pThis)
        {
            return (*(delegate* unmanaged<IntPtr, int>**)pThis)[3](pThis);
        }

        // Used during GC callback
        public static int FindTrackerTargetsCompleted(IntPtr pThis, bool walkFailed)
        {
            return (*(delegate* unmanaged<IntPtr, bool, int>**)pThis)[4](pThis, walkFailed);
        }

        // Used during GC callback
        public static int ReferenceTrackingCompleted(IntPtr pThis)
        {
            return (*(delegate* unmanaged<IntPtr, int>**)pThis)[5](pThis);
        }

        public static void SetReferenceTrackerHost(IntPtr pThis, IntPtr referenceTrackerHost)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged<IntPtr, IntPtr, int>**)pThis)[6](pThis, referenceTrackerHost));
        }
    }
}
