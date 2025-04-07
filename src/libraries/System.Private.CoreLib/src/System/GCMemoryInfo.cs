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

        internal GCGenerationInfo _generationInfo0;
        internal GCGenerationInfo _generationInfo1;
        internal GCGenerationInfo _generationInfo2;
        internal GCGenerationInfo _generationInfo3;
        internal GCGenerationInfo _generationInfo4;

        internal TimeSpan _pauseDuration0;
        internal TimeSpan _pauseDuration1;
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
        private readonly long _highMemoryLoadThresholdBytes;
        private readonly long _totalAvailableMemoryBytes;
        private readonly long _memoryLoadBytes;
        private readonly long _heapSizeBytes;
        private readonly long _fragmentedBytes;
        private readonly long _totalCommittedBytes;
        private readonly long _promotedBytes;
        private readonly long _pinnedObjectsCount;
        private readonly long _finalizationPendingCount;
        private readonly long _index;
        private readonly int _generation;
        private readonly int _pauseTimePercentage;
        private readonly byte _compacted;
        private readonly byte _concurrent;
        private readonly GCGenerationInfo _generationInfo0;
        private readonly GCGenerationInfo _generationInfo1;
        private readonly GCGenerationInfo _generationInfo2;
        private readonly GCGenerationInfo _generationInfo3;
        private readonly GCGenerationInfo _generationInfo4;
        private readonly TimeSpan _pauseDuration0;
        private readonly TimeSpan _pauseDuration1;

        internal GCMemoryInfo(GCMemoryInfoData data)
        {
            _highMemoryLoadThresholdBytes = data._highMemoryLoadThresholdBytes;
            _totalAvailableMemoryBytes = data._totalAvailableMemoryBytes;
            _memoryLoadBytes = data._memoryLoadBytes;
            _heapSizeBytes = data._heapSizeBytes;
            _fragmentedBytes = data._fragmentedBytes;
            _totalCommittedBytes = data._totalCommittedBytes;
            _promotedBytes = data._promotedBytes;
            _pinnedObjectsCount = data._pinnedObjectsCount;
            _finalizationPendingCount = data._finalizationPendingCount;
            _index = data._index;
            _generation = data._generation;
            _pauseTimePercentage = data._pauseTimePercentage;
            _compacted = data._compacted;
            _concurrent = data._concurrent;
            _generationInfo0 = data._generationInfo0;
            _generationInfo1 = data._generationInfo1;
            _generationInfo2 = data._generationInfo2;
            _generationInfo3 = data._generationInfo3;
            _generationInfo4 = data._generationInfo4;
            _pauseDuration0 = data._pauseDuration0;
            _pauseDuration1 = data._pauseDuration1;
        }

        /// <summary>
        /// High memory load threshold when this GC occurred
        /// </summary>
        public long HighMemoryLoadThresholdBytes => _highMemoryLoadThresholdBytes;

        /// <summary>
        /// Memory load when this GC occurred
        /// </summary>
        public long MemoryLoadBytes => _memoryLoadBytes;

        /// <summary>
        /// Total available memory for the GC to use when this GC occurred.
        ///
        /// If the environment variable DOTNET_GCHeapHardLimit is set,
        /// or "Server.GC.HeapHardLimit" is in runtimeconfig.json, this will come from that.
        /// If the program is run in a container, this will be an implementation-defined fraction of the container's size.
        /// Else, this is the physical memory on the machine that was available for the GC to use when this GC occurred.
        /// </summary>
        public long TotalAvailableMemoryBytes => _totalAvailableMemoryBytes;

        /// <summary>
        /// The total heap size when this GC occurred
        /// </summary>
        public long HeapSizeBytes => _heapSizeBytes;

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
        public long FragmentedBytes => _fragmentedBytes;

        /// <summary>
        /// The index of this GC. GC indices start with 1 and get increased at the beginning of a GC.
        /// Since the info is updated at the end of a GC, this means you can get the info for a BGC
        /// with a smaller index than a foreground GC finished earlier.
        /// </summary>
        public long Index => _index;

        /// <summary>
        /// The generation this GC collected. Collecting a generation means all its younger generation(s)
        /// are also collected.
        /// </summary>
        public int Generation => _generation;

        /// <summary>
        /// Is this a compacting GC or not.
        /// </summary>
        public bool Compacted => _compacted != 0;

        /// <summary>
        /// Is this a concurrent GC (BGC) or not.
        /// </summary>
        public bool Concurrent => _concurrent != 0;

        /// <summary>
        /// Total committed bytes of the managed heap.
        /// </summary>
        public long TotalCommittedBytes => _totalCommittedBytes;

        /// <summary>
        /// Promoted bytes for this GC.
        /// </summary>
        public long PromotedBytes => _promotedBytes;

        /// <summary>
        /// Number of pinned objects this GC observed.
        /// </summary>
        public long PinnedObjectsCount => _pinnedObjectsCount;

        /// <summary>
        /// Number of objects ready for finalization this GC observed.
        /// </summary>
        public long FinalizationPendingCount => _finalizationPendingCount;

        /// <summary>
        /// Pause durations. For blocking GCs there's only 1 pause; for BGC there are 2.
        /// </summary>
        public ReadOnlySpan<TimeSpan> PauseDurations => MemoryMarshal.CreateReadOnlySpan(in _pauseDuration0, 2);

        /// <summary>
        /// This is the % pause time in GC so far. If it's 1.2%, this number is 1.2.
        /// </summary>
        public double PauseTimePercentage => (double)_pauseTimePercentage / 100.0;

        /// <summary>
        /// Generation info for all generations.
        /// </summary>
        public ReadOnlySpan<GCGenerationInfo> GenerationInfo => MemoryMarshal.CreateReadOnlySpan(in _generationInfo0, 5);
    }
}
