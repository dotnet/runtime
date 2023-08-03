// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using static System.Runtime.InteropServices.ComWrappers;

namespace System.Runtime.InteropServices
{
    // Defined in windows.ui.xaml.hosting.referencetracker.h.
    enum XAML_REFERENCETRACKER_DISCONNECT
    {
        // Indicates the disconnect is during a suspend and a GC can be trigger.
        XAML_REFERENCETRACKER_DISCONNECT_SUSPEND = 0x00000001
    };

    internal struct HostServices
    {
        internal static IntPtr s_globalHostServices = CreateHostServices();

        private static unsafe IntPtr CreateHostServices()
        {
            IntPtr wrapperMem = (IntPtr)NativeMemory.Alloc(
                (nuint)sizeof(HostServices) + (nuint)sizeof(IntPtr) + (nuint)sizeof(HostServices*));

            *(IntPtr*)(wrapperMem + sizeof(HostServices)) = ComWrappers.DefaultIReferenceTrackerHostVftblPtr;
            *(IntPtr*)(wrapperMem + sizeof(HostServices) + sizeof(IntPtr)) = wrapperMem;
            return wrapperMem;
        }

        public static void DisconnectUnusedReferenceSources(uint flags)
        {
            if ((((XAML_REFERENCETRACKER_DISCONNECT)flags) & XAML_REFERENCETRACKER_DISCONNECT.XAML_REFERENCETRACKER_DISCONNECT_SUSPEND) != 0)
            {
                // In CoreCLR, low_memory_p is also set to true which it isn't here in AOT currently via this code path.
                // If that ends up being needed, a new export would need to be added to achieve that.
                GC.Collect(2, GCCollectionMode.Optimized, true);
            }
            else
            {
                GC.Collect();
            }
        }

        public static void ReleaseDisconnectedReferenceSources()
        {
            GC.WaitForPendingFinalizers();
        }

        public static void NotifyEndOfReferenceTrackingOnThread()
        {
            ComWrappers.ReleaseExternalObjectsFromCurrentThread();
        }

        // Creates a proxy object (managed object wrapper) that points to the given IUnknown.
        // The proxy represents the following:
        //   1. Has a managed reference pointing to the external object
        //      and therefore forms a cycle that can be resolved by GC.
        //   2. Forwards data binding requests.
        //
        // For example:
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
        public static void GetTrackerTarget(IntPtr punk, out IntPtr ppNewReference)
        {
            if (punk == IntPtr.Zero)
            {
                throw new ArgumentException();
            }

            if (Marshal.QueryInterface(punk, in ComWrappers.IID_IUnknown, out nint ppv) != 0)
            {
                throw new InvalidCastException();
            }

            IntPtr trackerTarget = ComWrappers.GetOrCreateTrackerTarget(ppv);
            if (Marshal.QueryInterface(trackerTarget, in ComWrappers.IID_IReferenceTrackerTarget, out ppNewReference) != 0)
            {
                throw new InvalidCastException();
            }
        }

        public static void AddMemoryPressure(long bytesAllocated)
        {
            GC.AddMemoryPressure(bytesAllocated);
        }

        public static void RemoveMemoryPressure(long bytesAllocated)
        {
            GC.RemoveMemoryPressure(bytesAllocated);
        }
    }

    internal static class TrackerObjectManager
    {
        internal static volatile IntPtr s_TrackerManager;
        internal static volatile bool s_HasTrackingStarted;
        internal static volatile bool s_IsGlobalPeggingOn = true;

        // Indicates if walking the external objects is needed.
        // (i.e. Have any IReferenceTracker instances been found?)
        public static bool ShouldWalkExternalObjects()
        {
            return s_TrackerManager != IntPtr.Zero;
        }

        // Called when an IReferenceTracker instance is found.
        public static void OnIReferenceTrackerFound(IntPtr referenceTracker)
        {
            Debug.Assert(referenceTracker != IntPtr.Zero);
            if (s_TrackerManager != IntPtr.Zero)
            {
                return;
            }

            IReferenceTracker.GetReferenceTrackerManager(referenceTracker, out IntPtr referenceTrackerManager);

            // Attempt to set the tracker instance.
            // If set, the ownership of referenceTrackerManager has been transferred
            if (Interlocked.CompareExchange(ref s_TrackerManager, referenceTrackerManager, IntPtr.Zero) == IntPtr.Zero)
            {
                IReferenceTrackerManager.SetReferenceTrackerHost(s_TrackerManager, HostServices.s_globalHostServices);
            }
            else
            {
                Marshal.Release(referenceTrackerManager);
            }
        }

        // Called after wrapper has been created.
        public static void AfterWrapperCreated(IntPtr referenceTracker)
        {
            Debug.Assert(referenceTracker != IntPtr.Zero);

            // Notify tracker runtime that we've created a new wrapper for this object.
            // To avoid surprises, we should notify them before we fire the first AddRefFromTrackerSource.
            IReferenceTracker.ConnectFromTrackerSource(referenceTracker);

            // Send out AddRefFromTrackerSource callbacks to notify tracker runtime we've done AddRef()
            // for certain interfaces. We should do this *after* we made a AddRef() because we should never
            // be in a state where report refs > actual refs
            IReferenceTracker.AddRefFromTrackerSource(referenceTracker); // IUnknown
            IReferenceTracker.AddRefFromTrackerSource(referenceTracker); // IReferenceTracker
        }

        // Called before wrapper is about to be finalized (the same lifetime as short weak handle).
        public static void BeforeWrapperFinalized(IntPtr referenceTracker)
        {
            Debug.Assert(referenceTracker != IntPtr.Zero);

            // Notify tracker runtime that we are about to finalize a wrapper
            // (same timing as short weak handle) for this object.
            // They need this information to disconnect weak refs and stop firing events,
            // so that they can avoid resurrecting the object.
            IReferenceTracker.DisconnectFromTrackerSource(referenceTracker);
        }

        // Begin the reference tracking process for external objects.
        public static void BeginReferenceTracking(/*InteropLibImports::RuntimeCallContext* cxt*/)
        {
            // Debug.Assert(cxt != IntPtr.Zero);
            if (!ShouldWalkExternalObjects())
            {
                return;
            }

            Debug.Assert(s_HasTrackingStarted == false);
            Debug.Assert(s_IsGlobalPeggingOn);

            s_HasTrackingStarted = true;

            // Let the tracker runtime know we are about to walk external objects so that
            // they can lock their reference cache. Note that the tracker runtime doesn't need to
            // unpeg all external objects at this point and they can do the pegging/unpegging.
            // in FindTrackerTargetsCompleted.
            Debug.Assert(s_TrackerManager != IntPtr.Zero);
            IReferenceTrackerManager.ReferenceTrackingStarted(s_TrackerManager);

            // From this point, the tracker runtime decides whether a target
            // should be pegged or not as the global pegging flag is now off.
            s_IsGlobalPeggingOn = false;

            // Time to walk the external objects
            WalkExternalTrackerObjects(cxt);
        }

        private static void WalkExternalTrackerObjects()
        {
            bool walkFailed = false;

        }

        // End the reference tracking process for external object.
        public static void EndReferenceTracking()
        {
            if (!s_HasTrackingStarted || !ShouldWalkExternalObjects())
            {
                return;
            }

            // Let the tracker runtime know the external object walk is done and they need to:
            // 1. Unpeg all managed object wrappers (mow) if the (mow) needs to be unpegged
            //       (i.e. when the (mow) is only reachable by other external tracker objects).
            // 2. Peg all mows if the mow needs to be pegged (i.e. when the above condition is not true)
            // 3. Unlock reference cache when they are done.
            Debug.Assert(s_TrackerManager != IntPtr.Zero);
            IReferenceTrackerManager.ReferenceTrackingCompleted(s_TrackerManager);

            s_IsGlobalPeggingOn = true;
            s_HasTrackingStarted = false;
        }
    }

    // Wrapper for IReferenceTrackerManager
    internal static unsafe class IReferenceTrackerManager
    {
        public static void ReferenceTrackingStarted(IntPtr pThis)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)pThis)[3](pThis));
        }

        public static void FindTrackerTargetsCompleted(IntPtr pThis, bool walkFailed)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, bool, int>**)pThis)[4](pThis, walkFailed));
        }

        public static void ReferenceTrackingCompleted(IntPtr pThis)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)pThis)[5](pThis));
        }

        public static void SetReferenceTrackerHost(IntPtr pThis, IntPtr referenceTrackerHost)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>**)pThis)[6](pThis, referenceTrackerHost));
        }
    }

    // Wrapper for IReferenceTracker
    internal static unsafe class IReferenceTracker
    {
        public static void ConnectFromTrackerSource(IntPtr pThis)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)pThis)[3](pThis));
        }

        public static void DisconnectFromTrackerSource(IntPtr pThis)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)pThis)[4](pThis));
        }

        public static void FindTrackerTargets(IntPtr pThis, IntPtr findReferenceTargetsCallback)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>**)pThis)[5](pThis, findReferenceTargetsCallback));
        }

        public static void GetReferenceTrackerManager(IntPtr pThis, out IntPtr referenceTrackerManager)
        {
            fixed (IntPtr* ptr = &referenceTrackerManager)
                Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)pThis)[6](pThis, ptr));
        }

        public static void AddRefFromTrackerSource(IntPtr pThis)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)pThis)[7](pThis));
        }

        public static void ReleaseFromTrackerSource(IntPtr pThis)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)pThis)[8](pThis));
        }

        public static void PegFromTrackerSource(IntPtr pThis)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)pThis)[9](pThis));
        }
    }

    // Wrapper for IReferenceTracker
    internal static unsafe class IFindReferenceTargetsCallback
    {
        public static void FoundTrackerTarget(IntPtr pThis, IntPtr referenceTrackerTarget)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>**)pThis)[3](pThis, referenceTrackerTarget));
        }
    }
}
