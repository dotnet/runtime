// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

using static System.Runtime.InteropServices.ComWrappers;

namespace System.Runtime.InteropServices
{
    internal static class TrackerObjectManager
    {
        internal static readonly IntPtr s_findReferencesTargetCallback = FindReferenceTargetsCallback.CreateFindReferenceTargetsCallback();
        internal static readonly IntPtr s_globalHostServices = CreateHostServices();

        internal static volatile IntPtr s_trackerManager;
        internal static volatile bool s_hasTrackingStarted;
        internal static volatile bool s_isGlobalPeggingOn = true;

        internal static DependentHandleList s_referenceCache;

        // Used during GC callback
        // Indicates if walking the external objects is needed.
        // (i.e. Have any IReferenceTracker instances been found?)
        public static bool ShouldWalkExternalObjects()
        {
            return s_trackerManager != IntPtr.Zero;
        }

        // Called when an IReferenceTracker instance is found.
        public static void OnIReferenceTrackerFound(IntPtr referenceTracker)
        {
            Debug.Assert(referenceTracker != IntPtr.Zero);
            if (s_trackerManager != IntPtr.Zero)
            {
                return;
            }

            IReferenceTracker.GetReferenceTrackerManager(referenceTracker, out IntPtr referenceTrackerManager);

            // Attempt to set the tracker instance.
            // If set, the ownership of referenceTrackerManager has been transferred
            if (Interlocked.CompareExchange(ref s_trackerManager, referenceTrackerManager, IntPtr.Zero) == IntPtr.Zero)
            {
                IReferenceTrackerManager.SetReferenceTrackerHost(s_trackerManager, s_globalHostServices);

                // Our GC callbacks are used only for reference walk of tracker objects, so register it here
                // when we find our first tracker object.
                RegisterGCCallbacks();
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

        // Used during GC callback
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

        // Used during GC callback
        // Begin the reference tracking process for external objects.
        public static void BeginReferenceTracking()
        {
            if (!ShouldWalkExternalObjects())
            {
                return;
            }

            Debug.Assert(!s_hasTrackingStarted);
            Debug.Assert(s_isGlobalPeggingOn);

            s_hasTrackingStarted = true;

            // Let the tracker runtime know we are about to walk external objects so that
            // they can lock their reference cache. Note that the tracker runtime doesn't need to
            // unpeg all external objects at this point and they can do the pegging/unpegging.
            // in FindTrackerTargetsCompleted.
            Debug.Assert(s_trackerManager != IntPtr.Zero);
            IReferenceTrackerManager.ReferenceTrackingStarted(s_trackerManager);

            // From this point, the tracker runtime decides whether a target
            // should be pegged or not as the global pegging flag is now off.
            s_isGlobalPeggingOn = false;

            // Time to walk the external objects
            WalkExternalTrackerObjects();
        }

        // Used during GC callback
        // End the reference tracking process for external object.
        public static void EndReferenceTracking()
        {
            if (!s_hasTrackingStarted || !ShouldWalkExternalObjects())
            {
                return;
            }

            // Let the tracker runtime know the external object walk is done and they need to:
            // 1. Unpeg all managed object wrappers (mow) if the (mow) needs to be unpegged
            //       (i.e. when the (mow) is only reachable by other external tracker objects).
            // 2. Peg all mows if the mow needs to be pegged (i.e. when the above condition is not true)
            // 3. Unlock reference cache when they are done.
            Debug.Assert(s_trackerManager != IntPtr.Zero);
            IReferenceTrackerManager.ReferenceTrackingCompleted(s_trackerManager);

            s_isGlobalPeggingOn = true;
            s_hasTrackingStarted = false;
        }

        public static unsafe void RegisterGCCallbacks()
        {
            delegate* unmanaged<int, void> gcStartCallback = &GCStartCollection;
            delegate* unmanaged<int, void> gcStopCallback = &GCStopCollection;
            delegate* unmanaged<int, void> gcAfterMarkCallback = &GCAfterMarkPhase;

            if (!RuntimeImports.RhRegisterGcCallout(RuntimeImports.GcRestrictedCalloutKind.StartCollection, (IntPtr)gcStartCallback) ||
                !RuntimeImports.RhRegisterGcCallout(RuntimeImports.GcRestrictedCalloutKind.EndCollection, (IntPtr)gcStopCallback) ||
                !RuntimeImports.RhRegisterGcCallout(RuntimeImports.GcRestrictedCalloutKind.AfterMarkPhase, (IntPtr)gcAfterMarkCallback))
            {
                throw new OutOfMemoryException();
            }
        }

        public static bool AddReferencePath(object target, object foundReference)
        {
            return s_referenceCache.AddDependentHandle(target, foundReference);
        }

        // Used during GC callback
        [UnmanagedCallersOnly]
        private static void GCStartCollection(int condemnedGeneration)
        {
            if (condemnedGeneration >= 2)
            {
                s_referenceCache.Reset();

                BeginReferenceTracking();
            }
        }

        // Used during GC callback
        [UnmanagedCallersOnly]
        private static void GCStopCollection(int condemnedGeneration)
        {
            if (condemnedGeneration >= 2)
            {
                EndReferenceTracking();
            }
        }

        // Used during GC callback
        [UnmanagedCallersOnly]
        private static void GCAfterMarkPhase(int condemnedGeneration)
        {
            DetachNonPromotedObjects();
        }

        private static unsafe IntPtr CreateHostServices()
        {
            IntPtr* wrapperMem = (IntPtr*)NativeMemory.Alloc((nuint)sizeof(IntPtr));
            wrapperMem[0] = CreateDefaultIReferenceTrackerHostVftbl();
            return (IntPtr)wrapperMem;
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

    // Wrapper for IReferenceTracker
    internal static unsafe class IReferenceTracker
    {
        public static void ConnectFromTrackerSource(IntPtr pThis)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged<IntPtr, int>**)pThis)[3](pThis));
        }

        // Used during GC callback
        public static int DisconnectFromTrackerSource(IntPtr pThis)
        {
            return (*(delegate* unmanaged<IntPtr, int>**)pThis)[4](pThis);
        }

        // Used during GC callback
        public static int FindTrackerTargets(IntPtr pThis, IntPtr findReferenceTargetsCallback)
        {
            return (*(delegate* unmanaged<IntPtr, IntPtr, int>**)pThis)[5](pThis, findReferenceTargetsCallback);
        }

        public static void GetReferenceTrackerManager(IntPtr pThis, out IntPtr referenceTrackerManager)
        {
            fixed (IntPtr* ptr = &referenceTrackerManager)
                Marshal.ThrowExceptionForHR((*(delegate* unmanaged<IntPtr, IntPtr*, int>**)pThis)[6](pThis, ptr));
        }

        public static void AddRefFromTrackerSource(IntPtr pThis)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged<IntPtr, int>**)pThis)[7](pThis));
        }

        public static void ReleaseFromTrackerSource(IntPtr pThis)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged<IntPtr, int>**)pThis)[8](pThis));
        }

        public static void PegFromTrackerSource(IntPtr pThis)
        {
            Marshal.ThrowExceptionForHR((*(delegate* unmanaged<IntPtr, int>**)pThis)[9](pThis));
        }
    }

    // Callback implementation of IFindReferenceTargetsCallback
    internal static unsafe class FindReferenceTargetsCallback
    {
        internal static GCHandle s_currentRootObjectHandle;

        [UnmanagedCallersOnly]
        private static unsafe int IFindReferenceTargetsCallback_QueryInterface(IntPtr pThis, Guid* guid, IntPtr* ppObject)
        {
            if (*guid == IID_IFindReferenceTargetsCallback || *guid == IID_IUnknown)
            {
                *ppObject = pThis;
                return HResults.S_OK;
            }
            else
            {
                return HResults.COR_E_INVALIDCAST;
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe int IFindReferenceTargetsCallback_FoundTrackerTarget(IntPtr pThis, IntPtr referenceTrackerTarget)
        {
            if (referenceTrackerTarget == IntPtr.Zero)
            {
                return HResults.E_INVALIDARG;
            }

            if (TryGetObject(referenceTrackerTarget, out object? foundObject))
            {
                // Notify the runtime a reference path was found.
                return TrackerObjectManager.AddReferencePath(s_currentRootObjectHandle.Target, foundObject) ? HResults.S_OK : HResults.S_FALSE;
            }

            return HResults.S_OK;
        }

        private static unsafe IntPtr CreateDefaultIFindReferenceTargetsCallbackVftbl()
        {
            IntPtr* vftbl = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(FindReferenceTargetsCallback), 4 * sizeof(IntPtr));
            vftbl[0] = (IntPtr)(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&IFindReferenceTargetsCallback_QueryInterface;
            vftbl[1] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.Untracked_AddRef;
            vftbl[2] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&ComWrappers.Untracked_Release;
            vftbl[3] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&IFindReferenceTargetsCallback_FoundTrackerTarget;
            return (IntPtr)vftbl;
        }

        internal static unsafe IntPtr CreateFindReferenceTargetsCallback()
        {
            IntPtr* wrapperMem = (IntPtr*)NativeMemory.Alloc((nuint)sizeof(IntPtr));
            wrapperMem[0] = CreateDefaultIFindReferenceTargetsCallbackVftbl();
            return (IntPtr)wrapperMem;
        }
    }

    internal readonly struct ComHolder : IDisposable
    {
        private readonly IntPtr _ptr;

        internal readonly IntPtr Ptr => _ptr;

        public ComHolder(IntPtr ptr)
        {
            _ptr = ptr;
        }

        public readonly void Dispose()
        {
            Marshal.Release(_ptr);
        }
    }

    // This is used during a GC callback so it needs to be free of any managed allocations.
    internal unsafe struct DependentHandleList
    {
        private int _freeIndex;                // The next available slot
        private int _capacity;                 // Total numbers of slots available in the list
        private IntPtr* _pHandles;             // All handles
        private int _shrinkHint;               // How many times we've consistently seen "hints" that a
                                               // shrink is needed

        private const int DefaultCapacity = 100;       // Default initial capacity of this list
        private const int ShrinkHintThreshold = 10;    // The number of hints we've seen before we really
                                                       // shrink the list

        public bool AddDependentHandle(object target, object dependent)
        {
            if (_freeIndex >= _capacity)
            {
                // We need a bigger dependent handle array
                if (!Grow())
                    return false;
            }

            IntPtr handle = _pHandles[_freeIndex];
            if (handle != default)
            {
                RuntimeImports.RhHandleSet(handle, target);
                RuntimeImports.RhHandleSetDependentSecondary(handle, dependent);
            }
            else
            {
                _pHandles[_freeIndex] = RuntimeImports.RhpHandleAllocDependent(target, dependent);
                if (_pHandles[_freeIndex] == default)
                {
                    return false;
                }
            }

            _freeIndex++;
            return true;
        }

        public bool Reset()
        {
            // Allocation for the first time
            if (_pHandles == null)
            {
                _capacity = DefaultCapacity;
#if TARGET_WINDOWS
                _pHandles = (IntPtr*)Interop.Ucrtbase.calloc((nuint)_capacity, (nuint)sizeof(IntPtr));
#else
                _pHandles = (IntPtr*)Interop.Sys.Calloc((nuint)_capacity, (nuint)sizeof(IntPtr));
#endif

                return _pHandles != null;
            }

            // If we are not using half of the handles last time, it is a hint that probably we need to shrink
            if (_freeIndex < _capacity / 2 && _capacity > DefaultCapacity)
            {
                _shrinkHint++;

                // Only shrink if we consistently seen such hint more than ShrinkHintThreshold times
                if (_shrinkHint > ShrinkHintThreshold)
                {
                    Shrink();
                    _shrinkHint = 0;
                }
            }
            else
            {
                // Reset shrink hint and start over the counting
                _shrinkHint = 0;
            }

            // Clear all the handles that were used
            for (int index = 0; index < _freeIndex; index++)
            {
                IntPtr handle = _pHandles[index];
                if (handle != default)
                {
                    RuntimeImports.RhHandleSet(handle, null);
                    RuntimeImports.RhHandleSetDependentSecondary(handle, null);
                }
            }

            _freeIndex = 0;
            return true;
        }

        private bool Shrink()
        {
            int newCapacity = _capacity / 2;

            // Free all handles that will go away
            for (int index = newCapacity; index < _capacity; index++)
            {
                if (_pHandles[index] != default)
                {
                    RuntimeImports.RhHandleFree(_pHandles[index]);
                    // Assign them back to null in case the reallocation fails
                    _pHandles[index] = default;
                }
            }

            // Shrink the size of the memory
#if TARGET_WINDOWS
            IntPtr* pNewHandles = (IntPtr*)Interop.Ucrtbase.realloc(_pHandles, (nuint)(newCapacity * sizeof(IntPtr)));
#else
            IntPtr* pNewHandles = (IntPtr*)Interop.Sys.Realloc(_pHandles, (nuint)(newCapacity * sizeof(IntPtr)));
#endif
            if (pNewHandles == null)
                return false;

            _pHandles = pNewHandles;
            _capacity = newCapacity;

            return true;
        }

        private bool Grow()
        {
            int newCapacity = _capacity * 2;
#if TARGET_WINDOWS
            IntPtr* pNewHandles = (IntPtr*)Interop.Ucrtbase.realloc(_pHandles, (nuint)(newCapacity * sizeof(IntPtr)));
#else
            IntPtr* pNewHandles = (IntPtr*)Interop.Sys.Realloc(_pHandles, (nuint)(newCapacity * sizeof(IntPtr)));
#endif
            if (pNewHandles == null)
                return false;

            for (int index = _capacity; index < newCapacity; index++)
            {
                pNewHandles[index] = default;
            }

            _pHandles = pNewHandles;
            _capacity = newCapacity;

            return true;
        }
    }
}
