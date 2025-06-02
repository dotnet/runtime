// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

using static System.Runtime.InteropServices.ComWrappers;

namespace System.Runtime.InteropServices
{
    internal static partial class TrackerObjectManager
    {
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
        public static bool AddReferencePath(object target, object foundReference)
        {
            return s_referenceCache.AddDependentHandle(target, foundReference);
        }

        private static bool HasReferenceTrackerManager
            => s_trackerManager != IntPtr.Zero;

        private static bool TryRegisterReferenceTrackerManager(IntPtr referenceTrackerManager)
        {
            return Interlocked.CompareExchange(ref s_trackerManager, referenceTrackerManager, IntPtr.Zero) == IntPtr.Zero;
        }

        internal static bool IsGlobalPeggingEnabled => s_isGlobalPeggingOn;

        private static void RegisterGCCallbacks()
        {
            unsafe
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

        // Used during GC callback
        internal static unsafe void WalkExternalTrackerObjects()
        {
            bool walkFailed = false;

            foreach (GCHandle weakNativeObjectWrapperHandle in s_referenceTrackerNativeObjectWrapperCache)
            {
                ReferenceTrackerNativeObjectWrapper? nativeObjectWrapper = Unsafe.As<ReferenceTrackerNativeObjectWrapper?>(weakNativeObjectWrapperHandle.Target);
                if (nativeObjectWrapper != null &&
                    nativeObjectWrapper.TrackerObject != IntPtr.Zero)
                {
                    FindReferenceTargetsCallback.Instance callback = new(nativeObjectWrapper.ProxyHandle);
                    int hr = IReferenceTracker.FindTrackerTargets(nativeObjectWrapper.TrackerObject, (IntPtr)(void*)&callback);
                    if (hr < 0)
                    {
                        walkFailed = true;
                        break;
                    }
                }
            }

            // Report whether walking failed or not.
            if (walkFailed)
            {
                s_isGlobalPeggingOn = true;
            }
            IReferenceTrackerManager.FindTrackerTargetsCompleted(s_trackerManager, walkFailed);
        }

        // Used during GC callback
        internal static void DetachNonPromotedObjects()
        {
            foreach (GCHandle weakNativeObjectWrapperHandle in s_referenceTrackerNativeObjectWrapperCache)
            {
                ReferenceTrackerNativeObjectWrapper? nativeObjectWrapper = Unsafe.As<ReferenceTrackerNativeObjectWrapper?>(weakNativeObjectWrapperHandle.Target);
                if (nativeObjectWrapper != null &&
                    nativeObjectWrapper.TrackerObject != IntPtr.Zero &&
                    !RuntimeImports.RhIsPromoted(nativeObjectWrapper.ProxyHandle.Target))
                {
                    // Notify the wrapper it was not promoted and is being collected.
                    BeforeWrapperFinalized(nativeObjectWrapper.TrackerObject);
                }
            }
        }
    }

    // Callback implementation of IFindReferenceTargetsCallback
    internal static unsafe class FindReferenceTargetsCallback
    {
        // Define an on-stack compatible COM instance to avoid allocating
        // a temporary instance.
        [StructLayout(LayoutKind.Sequential)]
        internal ref struct Instance
        {
            private readonly IntPtr _vtable; // First field is IUnknown based vtable.
            public GCHandle RootObject;

            public Instance(GCHandle handle)
            {
                _vtable = (IntPtr)Unsafe.AsPointer(in FindReferenceTargetsCallback.Vftbl);
                RootObject = handle;
            }
        }

#pragma warning disable CS3016
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
#pragma warning restore CS3016
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

#pragma warning disable CS3016
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
#pragma warning restore CS3016
        private static unsafe int IFindReferenceTargetsCallback_FoundTrackerTarget(IntPtr pThis, IntPtr referenceTrackerTarget)
        {
            if (referenceTrackerTarget == IntPtr.Zero)
            {
                return HResults.E_POINTER;
            }

            object sourceObject = ((FindReferenceTargetsCallback.Instance*)pThis)->RootObject.Target!;

            if (!TryGetObject(referenceTrackerTarget, out object? targetObject))
            {
                return HResults.S_FALSE;
            }

            if (sourceObject == targetObject)
            {
                return HResults.S_FALSE;
            }

            // Notify the runtime a reference path was found.
            return TrackerObjectManager.AddReferencePath(sourceObject, targetObject) ? HResults.S_OK : HResults.S_FALSE;
        }

        internal struct ReferenceTargetsVftbl
        {
            public delegate* unmanaged[MemberFunction]<IntPtr, Guid*, IntPtr*, int> QueryInterface;
            public delegate* unmanaged[MemberFunction]<IntPtr, uint> AddRef;
            public delegate* unmanaged[MemberFunction]<IntPtr, uint> Release;
            public delegate* unmanaged[MemberFunction]<IntPtr, IntPtr, int> FoundTrackerTarget;
        }

        [FixedAddressValueType]
        internal static readonly ReferenceTargetsVftbl Vftbl;

#pragma warning disable CA1810 // Initialize reference type static fields inline
        // We want this to be explicitly written out to ensure we match the "pre-inited vtable" pattern.
        static FindReferenceTargetsCallback()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            ComWrappers.GetUntrackedIUnknownImpl(out Vftbl.AddRef, out Vftbl.Release);
            Vftbl.QueryInterface = &IFindReferenceTargetsCallback_QueryInterface;
            Vftbl.FoundTrackerTarget = &IFindReferenceTargetsCallback_FoundTrackerTarget;
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
