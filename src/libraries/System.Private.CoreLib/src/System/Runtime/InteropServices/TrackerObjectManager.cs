// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Runtime.InteropServices.ComWrappers;

namespace System.Runtime.InteropServices
{
    internal static partial class TrackerObjectManager
    {
        internal static readonly GCHandleSet s_referenceTrackerNativeObjectWrapperCache = new GCHandleSet();

        internal static void OnIReferenceTrackerFound(IntPtr referenceTracker)
        {
            Debug.Assert(referenceTracker != IntPtr.Zero);
            if (HasReferenceTrackerManager)
            {
                return;
            }

            IReferenceTracker.GetReferenceTrackerManager(referenceTracker, out IntPtr referenceTrackerManager);

            // Attempt to set the tracker instance.
            // If set, the ownership of referenceTrackerManager has been transferred
            if (TryRegisterReferenceTrackerManager(referenceTrackerManager))
            {
                ReferenceTrackerHost.SetReferenceTrackerHost(referenceTrackerManager);
                RegisterGCCallbacks();
            }
            else
            {
                Marshal.Release(referenceTrackerManager);
            }
        }

        internal static void AfterWrapperCreated(IntPtr referenceTracker)
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

        internal static void ReleaseExternalObjectsFromCurrentThread()
        {
            if (GlobalInstanceForTrackerSupport == null)
            {
                throw new NotSupportedException(SR.InvalidOperation_ComInteropRequireComWrapperTrackerInstance);
            }

            IntPtr contextToken = GetContextToken();

            List<object> objects = new List<object>();

            // Here we aren't part of a GC callback, so other threads can still be running
            // who are adding and removing from the collection. This means we can possibly race
            // with a handle being removed and freed and we can end up accessing a freed handle.
            // To avoid this, we take a lock on modifications to the collection while we gather
            // the objects.
            using (s_referenceTrackerNativeObjectWrapperCache.ModificationLock.EnterScope())
            {
                foreach (GCHandle weakNativeObjectWrapperHandle in s_referenceTrackerNativeObjectWrapperCache)
                {
                    ReferenceTrackerNativeObjectWrapper? nativeObjectWrapper = Unsafe.As<ReferenceTrackerNativeObjectWrapper>(weakNativeObjectWrapperHandle.Target);
                    if (nativeObjectWrapper != null &&
                        nativeObjectWrapper._contextToken == contextToken)
                    {
                        object? target = nativeObjectWrapper.ProxyHandle.Target;
                        if (target != null)
                        {
                            objects.Add(target);
                        }

                        // Separate the wrapper from the tracker runtime prior to
                        // passing them.
                        nativeObjectWrapper.DisconnectTracker();
                    }
                }
            }

            GlobalInstanceForTrackerSupport.ReleaseObjects(objects);
        }

        internal static IntPtr GetContextToken()
        {
#if TARGET_WINDOWS
            Interop.Ole32.CoGetContextToken(out IntPtr contextToken);
            return contextToken;
#else
            return IntPtr.Zero;
#endif
        }
    }
}
