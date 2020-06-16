// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System
{
    // !!!!!!!!!!!!!!!!!!!!!!!
    // make sure you change the def in vm\comutilnative.h
    // if you change this!
    public readonly struct GCGenerationInfo
    {
        public long SizeBeforeBytes { get; }
        public long FragmentationBeforeBytes { get; }
        public long SizeAfterBytes { get; }
        public long FragmentationAfterBytes { get; }
    }

    // !!!!!!!!!!!!!!!!!!!!!!!
    // make sure you change the def in gc\gcinterface.h
    // if you change this!
    public enum GCKind
    {
        Ephemeral = 0,    // gen0 or gen1 GC
        FullBlocking = 1, // blocking gen2 GC
        Background = 2    // background GC (always gen2)
    };

    [StructLayout(LayoutKind.Sequential)]
    internal class GCMemoryInfoData
    {
        internal long _highMemoryLoadThresholdBytes;
        internal long _totalAvailableMemoryBytes;
        internal long _memoryLoadBytes;
        internal long _heapSizeBytes;
        internal long _fragmentedBytes;
        internal long _totalCommittedBytes;
        internal long _promotedBytes;
        internal long _pinnedObjectsCount;
        internal long _finalizationPendingCount;
        internal long _index;
        internal int _generation;
        internal int _pauseTimePercentage;
        internal bool _compacted;
        internal bool _concurrent;

        private GCGenerationInfo _generationInfo0;
        private GCGenerationInfo _generationInfo1;
        private GCGenerationInfo _generationInfo2;
        private GCGenerationInfo _generationInfo3;
        private GCGenerationInfo _generationInfo4;

        internal ReadOnlySpan<GCGenerationInfo> GenerationInfoAsSpan => MemoryMarshal.CreateReadOnlySpan<GCGenerationInfo>(ref _generationInfo0, 5);

        private TimeSpan _pauseDuration0;
        private TimeSpan _pauseDuration1;

        internal ReadOnlySpan<TimeSpan> PauseDurationsAsSpan => MemoryMarshal.CreateReadOnlySpan<TimeSpan>(ref _pauseDuration0, 2);
    }

    public readonly struct GCMemoryInfo
    {
        private readonly GCMemoryInfoData _data;

        internal GCMemoryInfo(GCMemoryInfoData data)
        {
            _data = data;
        }

        /// <summary>
        /// High memory load threshold when the last GC occured
        /// </summary>
        public long HighMemoryLoadThresholdBytes => _data._highMemoryLoadThresholdBytes;

        /// <summary>
        /// Memory load when the last GC ocurred
        /// </summary>
        public long MemoryLoadBytes => _data._memoryLoadBytes;

        /// <summary>
        /// Total available memory for the GC to use when the last GC ocurred.
        ///
        /// If the environment variable COMPlus_GCHeapHardLimit is set,
        /// or "Server.GC.HeapHardLimit" is in runtimeconfig.json, this will come from that.
        /// If the program is run in a container, this will be an implementation-defined fraction of the container's size.
        /// Else, this is the physical memory on the machine that was available for the GC to use when the last GC occurred.
        /// </summary>
        public long TotalAvailableMemoryBytes => _data._totalAvailableMemoryBytes;

        /// <summary>
        /// The total heap size when the last GC ocurred
        /// </summary>
        public long HeapSizeBytes => _data._heapSizeBytes;

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
        public long FragmentedBytes => _data._fragmentedBytes;

        /// <summary>
        /// The index of this GC. GC indices start with 1 and get increased at the beginning of a GC.
        /// Since the info is updated at the end of a GC, this means you can get the info for a BGC
        /// with a smaller index than a foreground GC finished earlier.
        /// </summary>
        public long Index => _data._index;

        /// <summary>
        /// The generation this GC collected. Collecting a generation means all its younger generation(s)
        /// are also collected.
        /// </summary>
        public int Generation => _data._generation;

        /// <summary>
        /// Is this a compacting GC or not.
        /// </summary>
        public bool Compacted => _data._compacted;

        /// <summary>
        /// Is this a concurrent GC (BGC) or not.
        /// </summary>
        public bool Concurrent => _data._concurrent;

        /// <summary>
        /// Total committed bytes of the managed heap.
        /// </summary>
        public long TotalCommittedBytes => _data._totalCommittedBytes;

        /// <summary>
        /// Promoted bytes for this GC.
        /// </summary>
        public long PromotedBytes => _data._promotedBytes;

        /// <summary>
        /// Number of pinned objects this GC observed.
        /// </summary>
        public long PinnedObjectsCount => _data._pinnedObjectsCount;

        /// <summary>
        /// Number of objects ready for finalization this GC observed.
        /// </summary>
        public long FinalizationPendingCount => _data._finalizationPendingCount;

        /// <summary>
        /// Pause durations. For blocking GCs there's only 1 pause; for BGC there are 2.
        /// </summary>
        public ReadOnlySpan<TimeSpan> PauseDurations => _data.PauseDurationsAsSpan;

        /// <summary>
        /// This is the % pause time in GC so far. If it's 1.2%, this number is 1.2.
        /// </summary>
        public double PauseTimePercentage => (double)_data._pauseTimePercentage / 100.0;

        /// <summary>
        /// Generation info for all generations.
        /// </summary>
        public ReadOnlySpan<GCGenerationInfo> GenerationInfo => _data.GenerationInfoAsSpan;
    }
}
