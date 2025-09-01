// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
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
}
