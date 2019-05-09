// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Managed structure used by CrossLoaderAllocatorHeap to isolate per LoaderAllocator
    /// data. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal class LAHashDependentHashTracker
    {
        GCHandle _dependentHandle;
        IntPtr _loaderAllocator;

        ~LAHashDependentHashTracker()
        {
            if (_dependentHandle.IsAllocated)
                _dependentHandle.Free();
        }
    }

    /// <summary>
    /// Managed structure used by CrossLoaderAllocatorHeap to hold a set of references
    /// to LAHashDependentHashTracker's
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal class LAHashKeyToTrackers
    {
        object _trackerOrTrackerSet = null!;
        object _laLocalKeyValueStore = null!;
    }
}
