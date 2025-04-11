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
        internal static readonly IntPtr s_globalHostServices = CreateHostServices();

        // Called when an IReferenceTracker instance is found.
        public static void SetReferenceTrackerHost(IntPtr trackerManager)
        {
            IReferenceTrackerManager.SetReferenceTrackerHost(trackerManager, s_globalHostServices);
        }

        private static unsafe IntPtr CreateHostServices()
        {
            IntPtr* wrapperMem = (IntPtr*)NativeMemory.Alloc((nuint)sizeof(IntPtr));
            wrapperMem[0] = CreateDefaultIReferenceTrackerHostVftbl();
            return (IntPtr)wrapperMem;
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_DisconnectUnusedReferenceSources(IntPtr pThis, uint flags)
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

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_ReleaseDisconnectedReferenceSources(IntPtr pThis)
        {
            // We'd like to call GC.WaitForPendingFinalizers() here, but this could lead to deadlock
            // if the finalizer thread is trying to get back to this thread, because we are not pumping
            // anymore. Disable this for now. See: https://github.com/dotnet/runtime/issues/109538.
            return HResults.S_OK;
        }

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_NotifyEndOfReferenceTrackingOnThread(IntPtr pThis)
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

        // Creates a proxy object (managed object wrapper) that points to the given IUnknown.
        // The proxy represents the following:
        //   1. Has a managed reference pointing to the external object
        //      and therefore forms a cycle that can be resolved by GC.
        //   2. Forwards data binding requests.
        //
        // For example:
        // NoCW = Native Object Com Wrapper also known as RCW
        //
        // Grid <---- NoCW             Grid <-------- NoCW
        // | ^                         |              ^
        // | |             Becomes     |              |
        // v |                         v              |
        // Rectangle                  Rectangle ----->Proxy
        //
        // Arguments
        //   obj        - An IUnknown* where a NoCW points to (Grid, in this case)
        //                    Notes:
        //                    1. We can either create a new NoCW or get back an old one from the cache.
        //                    2. This obj could be a regular tracker runtime object for data binding.
        //  ppNewReference  - The IReferenceTrackerTarget* for the proxy created
        //                    The tracker runtime will call IReferenceTrackerTarget to establish a reference.
        //
        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_GetTrackerTarget(IntPtr pThis, IntPtr punk, IntPtr* ppNewReference)
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

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_AddMemoryPressure(IntPtr pThis, long bytesAllocated)
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

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_RemoveMemoryPressure(IntPtr pThis, long bytesAllocated)
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

        [UnmanagedCallersOnly]
        internal static unsafe int IReferenceTrackerHost_QueryInterface(IntPtr pThis, Guid* guid, IntPtr* ppObject)
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

        internal static unsafe IntPtr CreateDefaultIReferenceTrackerHostVftbl()
        {
            IntPtr* vftbl = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ReferenceTrackerHost), 9 * sizeof(IntPtr));
            vftbl[0] = (IntPtr)(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&IReferenceTrackerHost_QueryInterface;
            vftbl[1] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.Untracked_AddRef;
            vftbl[2] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.Untracked_Release;
            vftbl[3] = (IntPtr)(delegate* unmanaged<IntPtr, uint, int>)&IReferenceTrackerHost_DisconnectUnusedReferenceSources;
            vftbl[4] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&IReferenceTrackerHost_ReleaseDisconnectedReferenceSources;
            vftbl[5] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&IReferenceTrackerHost_NotifyEndOfReferenceTrackingOnThread;
            vftbl[6] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr*, int>)&IReferenceTrackerHost_GetTrackerTarget;
            vftbl[7] = (IntPtr)(delegate* unmanaged<IntPtr, long, int>)&IReferenceTrackerHost_AddMemoryPressure;
            vftbl[8] = (IntPtr)(delegate* unmanaged<IntPtr, long, int>)&IReferenceTrackerHost_RemoveMemoryPressure;
            return (IntPtr)vftbl;
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
