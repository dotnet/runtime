// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public readonly struct GCMemoryInfo
    {
        /// <summary>
        /// High memory load threshold when the last GC occured
        /// </summary>
        public long HighMemoryLoadThresholdBytes { get; }

        /// <summary>
        /// Memory load when the last GC ocurred
        /// </summary>
        public long MemoryLoadBytes { get; }

        /// <summary>
        /// Total available memory for the GC to use when the last GC ocurred.
        ///
        /// If the environment variable COMPlus_GCHeapHardLimit is set,
        /// or "Server.GC.HeapHardLimit" is in runtimeconfig.json, this will come from that.
        /// If the program is run in a container, this will be an implementation-defined fraction of the container's size.
        /// Else, this is the physical memory on the machine that was available for the GC to use when the last GC occurred.
        /// </summary>
        public long TotalAvailableMemoryBytes { get; }

        /// <summary>
        /// The total heap size when the last GC ocurred
        /// </summary>
        public long HeapSizeBytes { get; }

        /// <summary>
        /// The total fragmentation when the last GC ocurred
        ///
        /// Let's take the example below:
        ///  | OBJ_A |     OBJ_B     | OBJ_C |   OBJ_D   | OBJ_E |
        ///
        /// Let's say OBJ_B, OBJ_C and and OBJ_E are garbage and get collected, but the heap does not get compacted, the resulting heap will look like the following:
        ///  | OBJ_A |           F           |   OBJ_D   |
        ///
        /// The memory between OBJ_A and OBJ_D marked `F` is considered part of the FragmentedBytes, and will be used to allocate new objects. The memory after OBJ_D will not be
        /// considered part of the FragmentedBytes, and will also be used to allocate new objects
        /// </summary>
        public long FragmentedBytes { get; }

        internal GCMemoryInfo(long highMemoryLoadThresholdBytes,
                              long memoryLoadBytes,
                              long totalAvailableMemoryBytes,
                              long heapSizeBytes,
                              long fragmentedBytes)
        {
            HighMemoryLoadThresholdBytes = highMemoryLoadThresholdBytes;
            MemoryLoadBytes = memoryLoadBytes;
            TotalAvailableMemoryBytes = totalAvailableMemoryBytes;
            HeapSizeBytes = heapSizeBytes;
            FragmentedBytes = fragmentedBytes;
        }
    }
}
