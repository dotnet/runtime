// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    // !!!!!!!!!!!!!!!!!!!!!!!
    // make sure you change the def in vm\comutilnative.h
    // if you change this!
    //
    /// <summary>
    /// Represents the size and the fragmenation of a generation on entry and on exit
    /// of the GC reported in <see cref="GCMemoryInfo"/>.
    /// </summary>
    public readonly struct GCGenerationInfo
    {
        /// <summary>Size in bytes on entry to the reported collection.</summary>
        public long SizeBeforeBytes { get; }
        /// <summary>Fragmentation in bytes on entry to the reported collection.</summary>
        public long FragmentationBeforeBytes { get; }
        /// <summary>Size in bytes on exit from the reported collection.</summary>
        public long SizeAfterBytes { get; }
        /// <summary>Fragmentation in bytes on exit from the reported collection.</summary>
        public long FragmentationAfterBytes { get; }
    }

    // !!!!!!!!!!!!!!!!!!!!!!!
    // make sure you change the def in gc\gcinterface.h
    // if you change this!
    //
    /// <summary>Specifies the kind of a garbage collection.</summary>
    /// <remarks>
    /// A GC can be one of the 3 kinds - ephemeral, full blocking or background.
    /// Their frequencies are very different. Ephemeral GCs happen much more often than
    /// the other two kinds. Background GCs usually happen infrequently, and
    /// full blocking GCs usually happen very infrequently. In order to sample the very
    /// infrequent GCs, collections are separated into kinds so callers can ask for all three kinds while maintaining
    /// a reasonable sampling rate, e.g. if you are sampling once every second, without this
    /// distinction, you may never observe a background GC. With this distinction, you can
    /// always get info of the last GC of the kind you specify.
    /// </remarks>
    public enum GCKind
    {
        /// <summary>Any kind of collection.</summary>
        Any = 0,
        /// <summary>A gen0 or gen1 collection.</summary>
        Ephemeral = 1,
        /// <summary>A blocking gen2 collection.</summary>
        FullBlocking = 2,
        /// <summary>A background collection.</summary>
        /// <remarks>This is always a gen2 collection.</remarks>
        Background = 3
    };

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class GCMemoryInfoData
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
        internal byte _compacted;
        internal byte _concurrent;

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

    /// <summary>Provides a set of APIs that can be used to retrieve garbage collection information.</summary>
    /// <remarks>
    /// A GC is identified by its Index. which starts from 1 and increases with each GC (see more explanation
    /// of it in the Index prooperty).
    /// If you are asking for a GC that does not exist, eg, you called the GC.GetGCMemoryInfo API
    /// before a GC happened, or you are asking for a GC of GCKind.FullBlocking and no full blocking
    /// GCs have happened, you will get all 0's in the info, including the Index. So you can use Index 0
    /// to detect that no GCs, or no GCs of the kind you specified have happened.
    /// </remarks>
    public readonly struct GCMemoryInfo
    {
        private readonly GCMemoryInfoData _data;

        internal GCMemoryInfo(GCMemoryInfoData data)
        {
            _data = data;
        }

        /// <summary>
        /// High memory load threshold when this GC occurred
        /// </summary>
        public long HighMemoryLoadThresholdBytes => _data._highMemoryLoadThresholdBytes;

        /// <summary>
        /// Memory load when this GC occurred
        /// </summary>
        public long MemoryLoadBytes => _data._memoryLoadBytes;

        /// <summary>
        /// Total available memory for the GC to use when this GC occurred.
        ///
        /// If the environment variable COMPlus_GCHeapHardLimit is set,
        /// or "Server.GC.HeapHardLimit" is in runtimeconfig.json, this will come from that.
        /// If the program is run in a container, this will be an implementation-defined fraction of the container's size.
        /// Else, this is the physical memory on the machine that was available for the GC to use when this GC occurred.
        /// </summary>
        public long TotalAvailableMemoryBytes => _data._totalAvailableMemoryBytes;

        /// <summary>
        /// The total heap size when this GC occurred
        /// </summary>
        public long HeapSizeBytes => _data._heapSizeBytes;

        /// <summary>
        /// The total fragmentation when this GC occurred
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
        public bool Compacted => _data._compacted != 0;

        /// <summary>
        /// Is this a concurrent GC (BGC) or not.
        /// </summary>
        public bool Concurrent => _data._concurrent != 0;

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
