// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Port of gcconfig.h / gcconfig.cpp. The C++ version generates the accessors and the backing
// fields from the GC_CONFIGURATION_KEYS macro table; this file spells the same thing out, one
// config at a time, in the same order, so the two stay diffable.
//
// Booleans are byte-sized, as they are in C++, because their addresses are handed to the EE.
// Config keys are UTF-8 literals with an explicit terminator, so taking their address yields the
// null-terminated `const char*` the EE expects without allocating.

namespace Internal.Runtime.GarbageCollection
{
    /// <summary>
    /// Flags that may inhabit the number returned for the HeapVerifyLevel config option. Keep this in sync with vm/eeconfig.h if this ever changes.
    /// </summary>
    internal enum HeapVerifyFlags
    {
        HEAPVERIFY_NONE = 0,
        HEAPVERIFY_GC = 1,
        HEAPVERIFY_BARRIERCHECK = 2,
        HEAPVERIFY_SYNCBLK = 4,
        HEAPVERIFY_NO_RANGE_CHECKS = 0x10,
        HEAPVERIFY_NO_MEM_FILL = 0x20,
        HEAPVERIFY_POST_GC_ONLY = 0x40,
        HEAPVERIFY_DEEP_ON_COMPACT = 0x80,
    }

    /// <summary>
    /// Port of the C++ WriteBarrierFlavor enum from gcconfig.h.
    /// </summary>
    internal enum WriteBarrierFlavor
    {
        WRITE_BARRIER_DEFAULT = 0,
        WRITE_BARRIER_REGION_BIT = 1,
        WRITE_BARRIER_REGION_BYTE = 2,
        WRITE_BARRIER_SERVER = 3,
    }

    /// <summary>
    /// Retrieves configuration information for how the GC should operate.
    /// </summary>
    internal static unsafe class GCConfig
    {
        // The default of every config is spelled out below exactly as it appears in the C++
        // GC_CONFIGURATION_KEYS table, including the ones that happen to be zero, so that the two
        // tables can be diffed against each other.
#pragma warning disable CA1805 // Do not initialize unnecessarily

        /// <summary>Whether we should be using Server GC</summary>
        public static byte GetServerGC() => s_ServerGC;

        /// <summary>Whether we should be using Server GC, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetServerGC(byte defaultValue) => s_ServerGCProvided != 0 ? s_ServerGC : defaultValue;

        /// <summary>Records the value of ServerGC reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetServerGC(byte value) => s_UpdatedServerGC = value;

        private static byte s_ServerGC = 0;
        private static byte s_ServerGCProvided;
        private static byte s_UpdatedServerGC = 0;

        /// <summary>Whether we should be using Concurrent GC</summary>
        public static byte GetConcurrentGC() => s_ConcurrentGC;

        /// <summary>Whether we should be using Concurrent GC, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetConcurrentGC(byte defaultValue) => s_ConcurrentGCProvided != 0 ? s_ConcurrentGC : defaultValue;

        /// <summary>Records the value of ConcurrentGC reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetConcurrentGC(byte value) => s_UpdatedConcurrentGC = value;

        private static byte s_ConcurrentGC = 1;
        private static byte s_ConcurrentGCProvided;
        private static byte s_UpdatedConcurrentGC = 1;

        /// <summary>Enables/Disables conservative GC</summary>
        public static byte GetConservativeGC() => s_ConservativeGC;

        /// <summary>Enables/Disables conservative GC, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetConservativeGC(byte defaultValue) => s_ConservativeGCProvided != 0 ? s_ConservativeGC : defaultValue;

        /// <summary>Records the value of ConservativeGC reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetConservativeGC(byte value) => s_UpdatedConservativeGC = value;

        private static byte s_ConservativeGC = 0;
        private static byte s_ConservativeGCProvided;
        private static byte s_UpdatedConservativeGC = 0;

        /// <summary>When set to true, always do compacting GC</summary>
        public static byte GetForceCompact() => s_ForceCompact;

        /// <summary>When set to true, always do compacting GC, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetForceCompact(byte defaultValue) => s_ForceCompactProvided != 0 ? s_ForceCompact : defaultValue;

        /// <summary>Records the value of ForceCompact reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetForceCompact(byte value) => s_UpdatedForceCompact = value;

        private static byte s_ForceCompact = 0;
        private static byte s_ForceCompactProvided;
        private static byte s_UpdatedForceCompact = 0;

        /// <summary>When set we put the segments that should be deleted on a standby list (instead of releasing them back to the OS) which will be considered to satisfy new segment requests (note that the same thing can be specified via API which is the supported way)</summary>
        public static byte GetRetainVM() => s_RetainVM;

        /// <summary>When set we put the segments that should be deleted on a standby list (instead of releasing them back to the OS) which will be considered to satisfy new segment requests (note that the same thing can be specified via API which is the supported way), or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetRetainVM(byte defaultValue) => s_RetainVMProvided != 0 ? s_RetainVM : defaultValue;

        /// <summary>Records the value of RetainVM reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetRetainVM(byte value) => s_UpdatedRetainVM = value;

        private static byte s_RetainVM = 0;
        private static byte s_RetainVMProvided;
        private static byte s_UpdatedRetainVM = 0;

        /// <summary>Does a DebugBreak at the soonest time we detect an OOM</summary>
        public static byte GetBreakOnOOM() => s_BreakOnOOM;

        /// <summary>Does a DebugBreak at the soonest time we detect an OOM, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetBreakOnOOM(byte defaultValue) => s_BreakOnOOMProvided != 0 ? s_BreakOnOOM : defaultValue;

        /// <summary>Records the value of BreakOnOOM reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBreakOnOOM(byte value) => s_UpdatedBreakOnOOM = value;

        private static byte s_BreakOnOOM = 0;
        private static byte s_BreakOnOOMProvided;
        private static byte s_UpdatedBreakOnOOM = 0;

        /// <summary>If set, do not affinitize server GC threads</summary>
        public static byte GetNoAffinitize() => s_NoAffinitize;

        /// <summary>If set, do not affinitize server GC threads, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetNoAffinitize(byte defaultValue) => s_NoAffinitizeProvided != 0 ? s_NoAffinitize : defaultValue;

        /// <summary>Records the value of NoAffinitize reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetNoAffinitize(byte value) => s_UpdatedNoAffinitize = value;

        private static byte s_NoAffinitize = 0;
        private static byte s_NoAffinitizeProvided;
        private static byte s_UpdatedNoAffinitize = 0;

        /// <summary>Specifies if you want to turn on logging in GC</summary>
        public static byte GetLogEnabled() => s_LogEnabled;

        /// <summary>Specifies if you want to turn on logging in GC, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetLogEnabled(byte defaultValue) => s_LogEnabledProvided != 0 ? s_LogEnabled : defaultValue;

        /// <summary>Records the value of LogEnabled reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetLogEnabled(byte value) => s_UpdatedLogEnabled = value;

        private static byte s_LogEnabled = 0;
        private static byte s_LogEnabledProvided;
        private static byte s_UpdatedLogEnabled = 0;

        /// <summary>Specifies the name of the GC config log file</summary>
        public static byte GetConfigLogEnabled() => s_ConfigLogEnabled;

        /// <summary>Specifies the name of the GC config log file, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetConfigLogEnabled(byte defaultValue) => s_ConfigLogEnabledProvided != 0 ? s_ConfigLogEnabled : defaultValue;

        /// <summary>Records the value of ConfigLogEnabled reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetConfigLogEnabled(byte value) => s_UpdatedConfigLogEnabled = value;

        private static byte s_ConfigLogEnabled = 0;
        private static byte s_ConfigLogEnabledProvided;
        private static byte s_UpdatedConfigLogEnabled = 0;

        /// <summary>Enables numa allocations in the GC</summary>
        public static byte GetGCNumaAware() => s_GCNumaAware;

        /// <summary>Enables numa allocations in the GC, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetGCNumaAware(byte defaultValue) => s_GCNumaAwareProvided != 0 ? s_GCNumaAware : defaultValue;

        /// <summary>Records the value of GCNumaAware reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCNumaAware(byte value) => s_UpdatedGCNumaAware = value;

        private static byte s_GCNumaAware = 1;
        private static byte s_GCNumaAwareProvided;
        private static byte s_UpdatedGCNumaAware = 1;

        /// <summary>Enables CPU groups in the GC</summary>
        public static byte GetGCCpuGroup() => s_GCCpuGroup;

        /// <summary>Enables CPU groups in the GC, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetGCCpuGroup(byte defaultValue) => s_GCCpuGroupProvided != 0 ? s_GCCpuGroup : defaultValue;

        /// <summary>Records the value of GCCpuGroup reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCCpuGroup(byte value) => s_UpdatedGCCpuGroup = value;

        private static byte s_GCCpuGroup = 0;
        private static byte s_GCCpuGroupProvided;
        private static byte s_UpdatedGCCpuGroup = 0;

        /// <summary>Enables Large Pages in the GC (1=real large pages, 2=emulation mode for testing)</summary>
        public static long GetGCLargePages() => s_GCLargePages;

        /// <summary>Enables Large Pages in the GC (1=real large pages, 2=emulation mode for testing), or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCLargePages(long defaultValue) => s_GCLargePagesProvided != 0 ? s_GCLargePages : defaultValue;

        /// <summary>Records the value of GCLargePages reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCLargePages(long value) => s_UpdatedGCLargePages = value;

        private static long s_GCLargePages = 0;
        private static byte s_GCLargePagesProvided;
        private static long s_UpdatedGCLargePages = 0;

        /// <summary>When set verifies the integrity of the managed heap on entry and exit of each GC</summary>
        public static long GetHeapVerifyLevel() => s_HeapVerifyLevel;

        /// <summary>When set verifies the integrity of the managed heap on entry and exit of each GC, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetHeapVerifyLevel(long defaultValue) => s_HeapVerifyLevelProvided != 0 ? s_HeapVerifyLevel : defaultValue;

        /// <summary>Records the value of HeapVerifyLevel reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetHeapVerifyLevel(long value) => s_UpdatedHeapVerifyLevel = value;

        private static long s_HeapVerifyLevel = (long)HeapVerifyFlags.HEAPVERIFY_NONE;
        private static byte s_HeapVerifyLevelProvided;
        private static long s_UpdatedHeapVerifyLevel = (long)HeapVerifyFlags.HEAPVERIFY_NONE;

        /// <summary>Specifies the LOH compaction mode</summary>
        public static long GetLOHCompactionMode() => s_LOHCompactionMode;

        /// <summary>Specifies the LOH compaction mode, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetLOHCompactionMode(long defaultValue) => s_LOHCompactionModeProvided != 0 ? s_LOHCompactionMode : defaultValue;

        /// <summary>Records the value of LOHCompactionMode reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetLOHCompactionMode(long value) => s_UpdatedLOHCompactionMode = value;

        private static long s_LOHCompactionMode = 0;
        private static byte s_LOHCompactionModeProvided;
        private static long s_UpdatedLOHCompactionMode = 0;

        /// <summary>Specifies the size that will make objects go on LOH</summary>
        public static long GetLOHThreshold() => s_LOHThreshold;

        /// <summary>Specifies the size that will make objects go on LOH, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetLOHThreshold(long defaultValue) => s_LOHThresholdProvided != 0 ? s_LOHThreshold : defaultValue;

        /// <summary>Records the value of LOHThreshold reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetLOHThreshold(long value) => s_UpdatedLOHThreshold = value;

        private static long s_LOHThreshold = 85000;
        private static byte s_LOHThresholdProvided;
        private static long s_UpdatedLOHThreshold = 85000;

        /// <summary>Specifies the bgc spin count</summary>
        public static long GetBGCSpinCount() => s_BGCSpinCount;

        /// <summary>Specifies the bgc spin count, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCSpinCount(long defaultValue) => s_BGCSpinCountProvided != 0 ? s_BGCSpinCount : defaultValue;

        /// <summary>Records the value of BGCSpinCount reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCSpinCount(long value) => s_UpdatedBGCSpinCount = value;

        private static long s_BGCSpinCount = 140;
        private static byte s_BGCSpinCountProvided;
        private static long s_UpdatedBGCSpinCount = 140;

        /// <summary>Specifies the bgc spin time</summary>
        public static long GetBGCSpin() => s_BGCSpin;

        /// <summary>Specifies the bgc spin time, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCSpin(long defaultValue) => s_BGCSpinProvided != 0 ? s_BGCSpin : defaultValue;

        /// <summary>Records the value of BGCSpin reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCSpin(long value) => s_UpdatedBGCSpin = value;

        private static long s_BGCSpin = 2;
        private static byte s_BGCSpinProvided;
        private static long s_UpdatedBGCSpin = 2;

        /// <summary>Specifies the number of server GC heaps</summary>
        public static long GetHeapCount() => s_HeapCount;

        /// <summary>Specifies the number of server GC heaps, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetHeapCount(long defaultValue) => s_HeapCountProvided != 0 ? s_HeapCount : defaultValue;

        /// <summary>Records the value of HeapCount reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetHeapCount(long value) => s_UpdatedHeapCount = value;

        private static long s_HeapCount = 0;
        private static byte s_HeapCountProvided;
        private static long s_UpdatedHeapCount = 0;

        /// <summary>Specifies the max number of server GC heaps to adjust to</summary>
        public static long GetMaxHeapCount() => s_MaxHeapCount;

        /// <summary>Specifies the max number of server GC heaps to adjust to, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetMaxHeapCount(long defaultValue) => s_MaxHeapCountProvided != 0 ? s_MaxHeapCount : defaultValue;

        /// <summary>Records the value of MaxHeapCount reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetMaxHeapCount(long value) => s_UpdatedMaxHeapCount = value;

        private static long s_MaxHeapCount = 0;
        private static byte s_MaxHeapCountProvided;
        private static long s_UpdatedMaxHeapCount = 0;

        /// <summary>Specifies the smallest gen0 budget</summary>
        public static long GetGen0Size() => s_Gen0Size;

        /// <summary>Specifies the smallest gen0 budget, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGen0Size(long defaultValue) => s_Gen0SizeProvided != 0 ? s_Gen0Size : defaultValue;

        /// <summary>Records the value of Gen0Size reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGen0Size(long value) => s_UpdatedGen0Size = value;

        private static long s_Gen0Size = 0;
        private static byte s_Gen0SizeProvided;
        private static long s_UpdatedGen0Size = 0;

        /// <summary>Specifies the managed heap segment size</summary>
        public static long GetSegmentSize() => s_SegmentSize;

        /// <summary>Specifies the managed heap segment size, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetSegmentSize(long defaultValue) => s_SegmentSizeProvided != 0 ? s_SegmentSize : defaultValue;

        /// <summary>Records the value of SegmentSize reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetSegmentSize(long value) => s_UpdatedSegmentSize = value;

        private static long s_SegmentSize = 0;
        private static byte s_SegmentSizeProvided;
        private static long s_UpdatedSegmentSize = 0;

        /// <summary>Specifies the GC latency mode - batch, interactive or low latency (note that the same thing can be specified via API which is the supported way</summary>
        public static long GetLatencyMode() => s_LatencyMode;

        /// <summary>Specifies the GC latency mode - batch, interactive or low latency (note that the same thing can be specified via API which is the supported way, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetLatencyMode(long defaultValue) => s_LatencyModeProvided != 0 ? s_LatencyMode : defaultValue;

        /// <summary>Records the value of LatencyMode reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetLatencyMode(long value) => s_UpdatedLatencyMode = value;

        private static long s_LatencyMode = -1;
        private static byte s_LatencyModeProvided;
        private static long s_UpdatedLatencyMode = -1;

        /// <summary>Specifies the GC latency level that you want to optimize for. Must be a number from 0 to 3. See documentation for more details on each level.</summary>
        public static long GetLatencyLevel() => s_LatencyLevel;

        /// <summary>Specifies the GC latency level that you want to optimize for. Must be a number from 0 to 3. See documentation for more details on each level., or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetLatencyLevel(long defaultValue) => s_LatencyLevelProvided != 0 ? s_LatencyLevel : defaultValue;

        /// <summary>Records the value of LatencyLevel reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetLatencyLevel(long value) => s_UpdatedLatencyLevel = value;

        private static long s_LatencyLevel = 1;
        private static byte s_LatencyLevelProvided;
        private static long s_UpdatedLatencyLevel = 1;

        /// <summary>Specifies the GC log file size</summary>
        public static long GetLogFileSize() => s_LogFileSize;

        /// <summary>Specifies the GC log file size, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetLogFileSize(long defaultValue) => s_LogFileSizeProvided != 0 ? s_LogFileSize : defaultValue;

        /// <summary>Records the value of LogFileSize reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetLogFileSize(long value) => s_UpdatedLogFileSize = value;

        private static long s_LogFileSize = 0;
        private static byte s_LogFileSizeProvided;
        private static long s_UpdatedLogFileSize = 0;

        /// <summary>Specifies the ratio compacting GCs vs sweeping</summary>
        public static long GetCompactRatio() => s_CompactRatio;

        /// <summary>Specifies the ratio compacting GCs vs sweeping, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetCompactRatio(long defaultValue) => s_CompactRatioProvided != 0 ? s_CompactRatio : defaultValue;

        /// <summary>Records the value of CompactRatio reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetCompactRatio(long value) => s_UpdatedCompactRatio = value;

        private static long s_CompactRatio = 0;
        private static byte s_CompactRatioProvided;
        private static long s_UpdatedCompactRatio = 0;

        /// <summary>Specifies processor mask for Server GC threads</summary>
        public static long GetGCHeapAffinitizeMask() => s_GCHeapAffinitizeMask;

        /// <summary>Specifies processor mask for Server GC threads, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCHeapAffinitizeMask(long defaultValue) => s_GCHeapAffinitizeMaskProvided != 0 ? s_GCHeapAffinitizeMask : defaultValue;

        /// <summary>Records the value of GCHeapAffinitizeMask reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCHeapAffinitizeMask(long value) => s_UpdatedGCHeapAffinitizeMask = value;

        private static long s_GCHeapAffinitizeMask = 0;
        private static byte s_GCHeapAffinitizeMaskProvided;
        private static long s_UpdatedGCHeapAffinitizeMask = 0;

        /// <summary>Specifies list of processors for Server GC threads. The format is a comma separated list of processor numbers or ranges of processor numbers. On Windows, each entry is prefixed by the CPU group number. Example: Unix - 1,3,5,7-9,12, Windows - 0:1,1:7-9</summary>
        /// <remarks>The returned string is owned by the EE and must be released with <see cref="GCToEEInterface.FreeStringConfigValue"/>.</remarks>
        public static byte* GetGCHeapAffinitizeRanges()
        {
            byte* resultStr = null;
            fixed (byte* privateKey = "GCHeapAffinitizeRanges\0"u8)
            fixed (byte* publicKey = "System.GC.HeapAffinitizeRanges\0"u8)
                GCToEEInterface.GetStringConfigValue(privateKey, publicKey, &resultStr);
            return resultStr;
        }

        /// <summary>Specifies the percent youngest gen to keep during trimming</summary>
        public static long GetGCTrimYoungestKeepPercent() => s_GCTrimYoungestKeepPercent;

        /// <summary>Specifies the percent youngest gen to keep during trimming, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCTrimYoungestKeepPercent(long defaultValue) => s_GCTrimYoungestKeepPercentProvided != 0 ? s_GCTrimYoungestKeepPercent : defaultValue;

        /// <summary>Records the value of GCTrimYoungestKeepPercent reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCTrimYoungestKeepPercent(long value) => s_UpdatedGCTrimYoungestKeepPercent = value;

        private static long s_GCTrimYoungestKeepPercent = 10;
        private static byte s_GCTrimYoungestKeepPercentProvided;
        private static long s_UpdatedGCTrimYoungestKeepPercent = 10;

        /// <summary>The percent for GC to consider as high memory</summary>
        public static long GetGCHighMemPercent() => s_GCHighMemPercent;

        /// <summary>The percent for GC to consider as high memory, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCHighMemPercent(long defaultValue) => s_GCHighMemPercentProvided != 0 ? s_GCHighMemPercent : defaultValue;

        /// <summary>Records the value of GCHighMemPercent reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCHighMemPercent(long value) => s_UpdatedGCHighMemPercent = value;

        private static long s_GCHighMemPercent = 0;
        private static byte s_GCHighMemPercentProvided;
        private static long s_UpdatedGCHighMemPercent = 0;

        /// <summary>Stress the provisional modes</summary>
        public static long GetGCProvModeStress() => s_GCProvModeStress;

        /// <summary>Stress the provisional modes, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCProvModeStress(long defaultValue) => s_GCProvModeStressProvided != 0 ? s_GCProvModeStress : defaultValue;

        /// <summary>Records the value of GCProvModeStress reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCProvModeStress(long value) => s_UpdatedGCProvModeStress = value;

        private static long s_GCProvModeStress = 0;
        private static byte s_GCProvModeStressProvided;
        private static long s_UpdatedGCProvModeStress = 0;

        /// <summary>Specifies the largest gen0 allocation budget</summary>
        public static long GetGCGen0MaxBudget() => s_GCGen0MaxBudget;

        /// <summary>Specifies the largest gen0 allocation budget, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCGen0MaxBudget(long defaultValue) => s_GCGen0MaxBudgetProvided != 0 ? s_GCGen0MaxBudget : defaultValue;

        /// <summary>Records the value of GCGen0MaxBudget reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCGen0MaxBudget(long value) => s_UpdatedGCGen0MaxBudget = value;

        private static long s_GCGen0MaxBudget = 0;
        private static byte s_GCGen0MaxBudgetProvided;
        private static long s_UpdatedGCGen0MaxBudget = 0;

        /// <summary>Specifies the largest gen1 allocation budget</summary>
        public static long GetGCGen1MaxBudget() => s_GCGen1MaxBudget;

        /// <summary>Specifies the largest gen1 allocation budget, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCGen1MaxBudget(long defaultValue) => s_GCGen1MaxBudgetProvided != 0 ? s_GCGen1MaxBudget : defaultValue;

        /// <summary>Records the value of GCGen1MaxBudget reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCGen1MaxBudget(long value) => s_UpdatedGCGen1MaxBudget = value;

        private static long s_GCGen1MaxBudget = 0;
        private static byte s_GCGen1MaxBudgetProvided;
        private static long s_UpdatedGCGen1MaxBudget = 0;

        /// <summary>Specifies the low generation skip ratio</summary>
        public static long GetGCLowSkipRatio() => s_GCLowSkipRatio;

        /// <summary>Specifies the low generation skip ratio, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCLowSkipRatio(long defaultValue) => s_GCLowSkipRatioProvided != 0 ? s_GCLowSkipRatio : defaultValue;

        /// <summary>Records the value of GCLowSkipRatio reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCLowSkipRatio(long value) => s_UpdatedGCLowSkipRatio = value;

        private static long s_GCLowSkipRatio = 30;
        private static byte s_GCLowSkipRatioProvided;
        private static long s_UpdatedGCLowSkipRatio = 30;

        /// <summary>Specifies a hard limit for the GC heap</summary>
        public static long GetGCHeapHardLimit() => s_GCHeapHardLimit;

        /// <summary>Specifies a hard limit for the GC heap, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCHeapHardLimit(long defaultValue) => s_GCHeapHardLimitProvided != 0 ? s_GCHeapHardLimit : defaultValue;

        /// <summary>Records the value of GCHeapHardLimit reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCHeapHardLimit(long value) => s_UpdatedGCHeapHardLimit = value;

        private static long s_GCHeapHardLimit = 0;
        private static byte s_GCHeapHardLimitProvided;
        private static long s_UpdatedGCHeapHardLimit = 0;

        /// <summary>Specifies the GC heap usage as a percentage of the total memory</summary>
        public static long GetGCHeapHardLimitPercent() => s_GCHeapHardLimitPercent;

        /// <summary>Specifies the GC heap usage as a percentage of the total memory, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCHeapHardLimitPercent(long defaultValue) => s_GCHeapHardLimitPercentProvided != 0 ? s_GCHeapHardLimitPercent : defaultValue;

        /// <summary>Records the value of GCHeapHardLimitPercent reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCHeapHardLimitPercent(long value) => s_UpdatedGCHeapHardLimitPercent = value;

        private static long s_GCHeapHardLimitPercent = 0;
        private static byte s_GCHeapHardLimitPercentProvided;
        private static long s_UpdatedGCHeapHardLimitPercent = 0;

        /// <summary>Specifies what the GC should consider to be total physical memory</summary>
        public static long GetGCTotalPhysicalMemory() => s_GCTotalPhysicalMemory;

        /// <summary>Specifies what the GC should consider to be total physical memory, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCTotalPhysicalMemory(long defaultValue) => s_GCTotalPhysicalMemoryProvided != 0 ? s_GCTotalPhysicalMemory : defaultValue;

        /// <summary>Records the value of GCTotalPhysicalMemory reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCTotalPhysicalMemory(long value) => s_UpdatedGCTotalPhysicalMemory = value;

        private static long s_GCTotalPhysicalMemory = 0;
        private static byte s_GCTotalPhysicalMemoryProvided;
        private static long s_UpdatedGCTotalPhysicalMemory = 0;

        /// <summary>Specifies the range for the GC heap</summary>
        public static long GetGCRegionRange() => s_GCRegionRange;

        /// <summary>Specifies the range for the GC heap, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCRegionRange(long defaultValue) => s_GCRegionRangeProvided != 0 ? s_GCRegionRange : defaultValue;

        /// <summary>Records the value of GCRegionRange reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCRegionRange(long value) => s_UpdatedGCRegionRange = value;

        private static long s_GCRegionRange = 0;
        private static byte s_GCRegionRangeProvided;
        private static long s_UpdatedGCRegionRange = 0;

        /// <summary>Specifies the size for a basic GC region</summary>
        public static long GetGCRegionSize() => s_GCRegionSize;

        /// <summary>Specifies the size for a basic GC region, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCRegionSize(long defaultValue) => s_GCRegionSizeProvided != 0 ? s_GCRegionSize : defaultValue;

        /// <summary>Records the value of GCRegionSize reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCRegionSize(long value) => s_UpdatedGCRegionSize = value;

        private static long s_GCRegionSize = 0;
        private static byte s_GCRegionSizeProvided;
        private static long s_UpdatedGCRegionSize = 0;

        /// <summary>Specifies to enable special handling some regions like SIP</summary>
        public static long GetGCEnableSpecialRegions() => s_GCEnableSpecialRegions;

        /// <summary>Specifies to enable special handling some regions like SIP, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCEnableSpecialRegions(long defaultValue) => s_GCEnableSpecialRegionsProvided != 0 ? s_GCEnableSpecialRegions : defaultValue;

        /// <summary>Records the value of GCEnableSpecialRegions reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCEnableSpecialRegions(long value) => s_UpdatedGCEnableSpecialRegions = value;

        private static long s_GCEnableSpecialRegions = 0;
        private static byte s_GCEnableSpecialRegionsProvided;
        private static long s_UpdatedGCEnableSpecialRegions = 0;

        /// <summary>Specifies the name of the GC log file</summary>
        /// <remarks>The returned string is owned by the EE and must be released with <see cref="GCToEEInterface.FreeStringConfigValue"/>.</remarks>
        public static byte* GetLogFile()
        {
            byte* resultStr = null;
            fixed (byte* privateKey = "GCLogFile\0"u8)
                GCToEEInterface.GetStringConfigValue(privateKey, null, &resultStr);
            return resultStr;
        }

        /// <summary>Specifies the name of the GC config log file</summary>
        /// <remarks>The returned string is owned by the EE and must be released with <see cref="GCToEEInterface.FreeStringConfigValue"/>.</remarks>
        public static byte* GetConfigLogFile()
        {
            byte* resultStr = null;
            fixed (byte* privateKey = "GCConfigLogFile\0"u8)
                GCToEEInterface.GetStringConfigValue(privateKey, null, &resultStr);
            return resultStr;
        }

        /// <summary>Enables FL tuning</summary>
        public static long GetBGCFLTuningEnabled() => s_BGCFLTuningEnabled;

        /// <summary>Enables FL tuning, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLTuningEnabled(long defaultValue) => s_BGCFLTuningEnabledProvided != 0 ? s_BGCFLTuningEnabled : defaultValue;

        /// <summary>Records the value of BGCFLTuningEnabled reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLTuningEnabled(long value) => s_UpdatedBGCFLTuningEnabled = value;

        private static long s_BGCFLTuningEnabled = 0;
        private static byte s_BGCFLTuningEnabledProvided;
        private static long s_UpdatedBGCFLTuningEnabled = 0;

        /// <summary>Specifies the physical memory load goal</summary>
        public static long GetBGCMemGoal() => s_BGCMemGoal;

        /// <summary>Specifies the physical memory load goal, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCMemGoal(long defaultValue) => s_BGCMemGoalProvided != 0 ? s_BGCMemGoal : defaultValue;

        /// <summary>Records the value of BGCMemGoal reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCMemGoal(long value) => s_UpdatedBGCMemGoal = value;

        private static long s_BGCMemGoal = 75;
        private static byte s_BGCMemGoalProvided;
        private static long s_UpdatedBGCMemGoal = 75;

        /// <summary>Specifies comfort zone of going above goal</summary>
        public static long GetBGCMemGoalSlack() => s_BGCMemGoalSlack;

        /// <summary>Specifies comfort zone of going above goal, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCMemGoalSlack(long defaultValue) => s_BGCMemGoalSlackProvided != 0 ? s_BGCMemGoalSlack : defaultValue;

        /// <summary>Records the value of BGCMemGoalSlack reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCMemGoalSlack(long value) => s_UpdatedBGCMemGoalSlack = value;

        private static long s_BGCMemGoalSlack = 10;
        private static byte s_BGCMemGoalSlackProvided;
        private static long s_UpdatedBGCMemGoalSlack = 10;

        /// <summary>Specifies the gen2 sweep FL ratio goal</summary>
        public static long GetBGCFLSweepGoal() => s_BGCFLSweepGoal;

        /// <summary>Specifies the gen2 sweep FL ratio goal, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLSweepGoal(long defaultValue) => s_BGCFLSweepGoalProvided != 0 ? s_BGCFLSweepGoal : defaultValue;

        /// <summary>Records the value of BGCFLSweepGoal reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLSweepGoal(long value) => s_UpdatedBGCFLSweepGoal = value;

        private static long s_BGCFLSweepGoal = 0;
        private static byte s_BGCFLSweepGoalProvided;
        private static long s_UpdatedBGCFLSweepGoal = 0;

        /// <summary>Specifies the LOH sweep FL ratio goal</summary>
        public static long GetBGCFLSweepGoalLOH() => s_BGCFLSweepGoalLOH;

        /// <summary>Specifies the LOH sweep FL ratio goal, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLSweepGoalLOH(long defaultValue) => s_BGCFLSweepGoalLOHProvided != 0 ? s_BGCFLSweepGoalLOH : defaultValue;

        /// <summary>Records the value of BGCFLSweepGoalLOH reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLSweepGoalLOH(long value) => s_UpdatedBGCFLSweepGoalLOH = value;

        private static long s_BGCFLSweepGoalLOH = 0;
        private static byte s_BGCFLSweepGoalLOHProvided;
        private static long s_UpdatedBGCFLSweepGoalLOH = 0;

        /// <summary>Specifies kp for above goal tuning</summary>
        public static long GetBGCFLkp() => s_BGCFLkp;

        /// <summary>Specifies kp for above goal tuning, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLkp(long defaultValue) => s_BGCFLkpProvided != 0 ? s_BGCFLkp : defaultValue;

        /// <summary>Records the value of BGCFLkp reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLkp(long value) => s_UpdatedBGCFLkp = value;

        private static long s_BGCFLkp = 6000;
        private static byte s_BGCFLkpProvided;
        private static long s_UpdatedBGCFLkp = 6000;

        /// <summary>Specifies ki for above goal tuning</summary>
        public static long GetBGCFLki() => s_BGCFLki;

        /// <summary>Specifies ki for above goal tuning, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLki(long defaultValue) => s_BGCFLkiProvided != 0 ? s_BGCFLki : defaultValue;

        /// <summary>Records the value of BGCFLki reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLki(long value) => s_UpdatedBGCFLki = value;

        private static long s_BGCFLki = 1000;
        private static byte s_BGCFLkiProvided;
        private static long s_UpdatedBGCFLki = 1000;

        /// <summary>Specifies kd for above goal tuning</summary>
        public static long GetBGCFLkd() => s_BGCFLkd;

        /// <summary>Specifies kd for above goal tuning, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLkd(long defaultValue) => s_BGCFLkdProvided != 0 ? s_BGCFLkd : defaultValue;

        /// <summary>Records the value of BGCFLkd reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLkd(long value) => s_UpdatedBGCFLkd = value;

        private static long s_BGCFLkd = 11;
        private static byte s_BGCFLkdProvided;
        private static long s_UpdatedBGCFLkd = 11;

        /// <summary>Specifies ff ratio</summary>
        public static long GetBGCFLff() => s_BGCFLff;

        /// <summary>Specifies ff ratio, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLff(long defaultValue) => s_BGCFLffProvided != 0 ? s_BGCFLff : defaultValue;

        /// <summary>Records the value of BGCFLff reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLff(long value) => s_UpdatedBGCFLff = value;

        private static long s_BGCFLff = 100;
        private static byte s_BGCFLffProvided;
        private static long s_UpdatedBGCFLff = 100;

        /// <summary>Smoothing over these</summary>
        public static long GetBGCFLSmoothFactor() => s_BGCFLSmoothFactor;

        /// <summary>Smoothing over these, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLSmoothFactor(long defaultValue) => s_BGCFLSmoothFactorProvided != 0 ? s_BGCFLSmoothFactor : defaultValue;

        /// <summary>Records the value of BGCFLSmoothFactor reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLSmoothFactor(long value) => s_UpdatedBGCFLSmoothFactor = value;

        private static long s_BGCFLSmoothFactor = 150;
        private static byte s_BGCFLSmoothFactorProvided;
        private static long s_UpdatedBGCFLSmoothFactor = 150;

        /// <summary>Enable gradual D instead of cutting off at the value</summary>
        public static long GetBGCFLGradualD() => s_BGCFLGradualD;

        /// <summary>Enable gradual D instead of cutting off at the value, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLGradualD(long defaultValue) => s_BGCFLGradualDProvided != 0 ? s_BGCFLGradualD : defaultValue;

        /// <summary>Records the value of BGCFLGradualD reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLGradualD(long value) => s_UpdatedBGCFLGradualD = value;

        private static long s_BGCFLGradualD = 0;
        private static byte s_BGCFLGradualDProvided;
        private static long s_UpdatedBGCFLGradualD = 0;

        /// <summary>Specifies kp for ML tuning</summary>
        public static long GetBGCMLkp() => s_BGCMLkp;

        /// <summary>Specifies kp for ML tuning, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCMLkp(long defaultValue) => s_BGCMLkpProvided != 0 ? s_BGCMLkp : defaultValue;

        /// <summary>Records the value of BGCMLkp reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCMLkp(long value) => s_UpdatedBGCMLkp = value;

        private static long s_BGCMLkp = 1000;
        private static byte s_BGCMLkpProvided;
        private static long s_UpdatedBGCMLkp = 1000;

        /// <summary>Specifies ki for ML tuning</summary>
        public static long GetBGCMLki() => s_BGCMLki;

        /// <summary>Specifies ki for ML tuning, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCMLki(long defaultValue) => s_BGCMLkiProvided != 0 ? s_BGCMLki : defaultValue;

        /// <summary>Records the value of BGCMLki reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCMLki(long value) => s_UpdatedBGCMLki = value;

        private static long s_BGCMLki = 16;
        private static byte s_BGCMLkiProvided;
        private static long s_UpdatedBGCMLki = 16;

        /// <summary>Enables ki for above goal tuning</summary>
        public static long GetBGCFLEnableKi() => s_BGCFLEnableKi;

        /// <summary>Enables ki for above goal tuning, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLEnableKi(long defaultValue) => s_BGCFLEnableKiProvided != 0 ? s_BGCFLEnableKi : defaultValue;

        /// <summary>Records the value of BGCFLEnableKi reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLEnableKi(long value) => s_UpdatedBGCFLEnableKi = value;

        private static long s_BGCFLEnableKi = 1;
        private static byte s_BGCFLEnableKiProvided;
        private static long s_UpdatedBGCFLEnableKi = 1;

        /// <summary>Enables kd for above goal tuning</summary>
        public static long GetBGCFLEnableKd() => s_BGCFLEnableKd;

        /// <summary>Enables kd for above goal tuning, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLEnableKd(long defaultValue) => s_BGCFLEnableKdProvided != 0 ? s_BGCFLEnableKd : defaultValue;

        /// <summary>Records the value of BGCFLEnableKd reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLEnableKd(long value) => s_UpdatedBGCFLEnableKd = value;

        private static long s_BGCFLEnableKd = 0;
        private static byte s_BGCFLEnableKdProvided;
        private static long s_UpdatedBGCFLEnableKd = 0;

        /// <summary>Enables smoothing</summary>
        public static long GetBGCFLEnableSmooth() => s_BGCFLEnableSmooth;

        /// <summary>Enables smoothing, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLEnableSmooth(long defaultValue) => s_BGCFLEnableSmoothProvided != 0 ? s_BGCFLEnableSmooth : defaultValue;

        /// <summary>Records the value of BGCFLEnableSmooth reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLEnableSmooth(long value) => s_UpdatedBGCFLEnableSmooth = value;

        private static long s_BGCFLEnableSmooth = 0;
        private static byte s_BGCFLEnableSmoothProvided;
        private static long s_UpdatedBGCFLEnableSmooth = 0;

        /// <summary>Enables TBH</summary>
        public static long GetBGCFLEnableTBH() => s_BGCFLEnableTBH;

        /// <summary>Enables TBH, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLEnableTBH(long defaultValue) => s_BGCFLEnableTBHProvided != 0 ? s_BGCFLEnableTBH : defaultValue;

        /// <summary>Records the value of BGCFLEnableTBH reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLEnableTBH(long value) => s_UpdatedBGCFLEnableTBH = value;

        private static long s_BGCFLEnableTBH = 0;
        private static byte s_BGCFLEnableTBHProvided;
        private static long s_UpdatedBGCFLEnableTBH = 0;

        /// <summary>Enables FF</summary>
        public static long GetBGCFLEnableFF() => s_BGCFLEnableFF;

        /// <summary>Enables FF, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCFLEnableFF(long defaultValue) => s_BGCFLEnableFFProvided != 0 ? s_BGCFLEnableFF : defaultValue;

        /// <summary>Records the value of BGCFLEnableFF reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCFLEnableFF(long value) => s_UpdatedBGCFLEnableFF = value;

        private static long s_BGCFLEnableFF = 0;
        private static byte s_BGCFLEnableFFProvided;
        private static long s_UpdatedBGCFLEnableFF = 0;

        /// <summary>Ratio correction factor for ML loop</summary>
        public static long GetBGCG2RatioStep() => s_BGCG2RatioStep;

        /// <summary>Ratio correction factor for ML loop, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetBGCG2RatioStep(long defaultValue) => s_BGCG2RatioStepProvided != 0 ? s_BGCG2RatioStep : defaultValue;

        /// <summary>Records the value of BGCG2RatioStep reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetBGCG2RatioStep(long value) => s_UpdatedBGCG2RatioStep = value;

        private static long s_BGCG2RatioStep = 5;
        private static byte s_BGCG2RatioStepProvided;
        private static long s_UpdatedBGCG2RatioStep = 5;

        /// <summary>UOH allocation during a BGC waits till end of BGC after UOH increases by this percent</summary>
        public static long GetUOHWaitBGCSizeIncPercent() => s_UOHWaitBGCSizeIncPercent;

        /// <summary>UOH allocation during a BGC waits till end of BGC after UOH increases by this percent, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetUOHWaitBGCSizeIncPercent(long defaultValue) => s_UOHWaitBGCSizeIncPercentProvided != 0 ? s_UOHWaitBGCSizeIncPercent : defaultValue;

        /// <summary>Records the value of UOHWaitBGCSizeIncPercent reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetUOHWaitBGCSizeIncPercent(long value) => s_UpdatedUOHWaitBGCSizeIncPercent = value;

        private static long s_UOHWaitBGCSizeIncPercent = -1;
        private static byte s_UOHWaitBGCSizeIncPercentProvided;
        private static long s_UpdatedUOHWaitBGCSizeIncPercent = -1;

        /// <summary>Specifies a hard limit for the GC heap SOH</summary>
        public static long GetGCHeapHardLimitSOH() => s_GCHeapHardLimitSOH;

        /// <summary>Specifies a hard limit for the GC heap SOH, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCHeapHardLimitSOH(long defaultValue) => s_GCHeapHardLimitSOHProvided != 0 ? s_GCHeapHardLimitSOH : defaultValue;

        /// <summary>Records the value of GCHeapHardLimitSOH reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCHeapHardLimitSOH(long value) => s_UpdatedGCHeapHardLimitSOH = value;

        private static long s_GCHeapHardLimitSOH = 0;
        private static byte s_GCHeapHardLimitSOHProvided;
        private static long s_UpdatedGCHeapHardLimitSOH = 0;

        /// <summary>Specifies a hard limit for the GC heap LOH</summary>
        public static long GetGCHeapHardLimitLOH() => s_GCHeapHardLimitLOH;

        /// <summary>Specifies a hard limit for the GC heap LOH, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCHeapHardLimitLOH(long defaultValue) => s_GCHeapHardLimitLOHProvided != 0 ? s_GCHeapHardLimitLOH : defaultValue;

        /// <summary>Records the value of GCHeapHardLimitLOH reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCHeapHardLimitLOH(long value) => s_UpdatedGCHeapHardLimitLOH = value;

        private static long s_GCHeapHardLimitLOH = 0;
        private static byte s_GCHeapHardLimitLOHProvided;
        private static long s_UpdatedGCHeapHardLimitLOH = 0;

        /// <summary>Specifies a hard limit for the GC heap POH</summary>
        public static long GetGCHeapHardLimitPOH() => s_GCHeapHardLimitPOH;

        /// <summary>Specifies a hard limit for the GC heap POH, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCHeapHardLimitPOH(long defaultValue) => s_GCHeapHardLimitPOHProvided != 0 ? s_GCHeapHardLimitPOH : defaultValue;

        /// <summary>Records the value of GCHeapHardLimitPOH reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCHeapHardLimitPOH(long value) => s_UpdatedGCHeapHardLimitPOH = value;

        private static long s_GCHeapHardLimitPOH = 0;
        private static byte s_GCHeapHardLimitPOHProvided;
        private static long s_UpdatedGCHeapHardLimitPOH = 0;

        /// <summary>Specifies the GC heap SOH usage as a percentage of the total memory</summary>
        public static long GetGCHeapHardLimitSOHPercent() => s_GCHeapHardLimitSOHPercent;

        /// <summary>Specifies the GC heap SOH usage as a percentage of the total memory, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCHeapHardLimitSOHPercent(long defaultValue) => s_GCHeapHardLimitSOHPercentProvided != 0 ? s_GCHeapHardLimitSOHPercent : defaultValue;

        /// <summary>Records the value of GCHeapHardLimitSOHPercent reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCHeapHardLimitSOHPercent(long value) => s_UpdatedGCHeapHardLimitSOHPercent = value;

        private static long s_GCHeapHardLimitSOHPercent = 0;
        private static byte s_GCHeapHardLimitSOHPercentProvided;
        private static long s_UpdatedGCHeapHardLimitSOHPercent = 0;

        /// <summary>Specifies the GC heap LOH usage as a percentage of the total memory</summary>
        public static long GetGCHeapHardLimitLOHPercent() => s_GCHeapHardLimitLOHPercent;

        /// <summary>Specifies the GC heap LOH usage as a percentage of the total memory, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCHeapHardLimitLOHPercent(long defaultValue) => s_GCHeapHardLimitLOHPercentProvided != 0 ? s_GCHeapHardLimitLOHPercent : defaultValue;

        /// <summary>Records the value of GCHeapHardLimitLOHPercent reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCHeapHardLimitLOHPercent(long value) => s_UpdatedGCHeapHardLimitLOHPercent = value;

        private static long s_GCHeapHardLimitLOHPercent = 0;
        private static byte s_GCHeapHardLimitLOHPercentProvided;
        private static long s_UpdatedGCHeapHardLimitLOHPercent = 0;

        /// <summary>Specifies the GC heap POH usage as a percentage of the total memory</summary>
        public static long GetGCHeapHardLimitPOHPercent() => s_GCHeapHardLimitPOHPercent;

        /// <summary>Specifies the GC heap POH usage as a percentage of the total memory, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCHeapHardLimitPOHPercent(long defaultValue) => s_GCHeapHardLimitPOHPercentProvided != 0 ? s_GCHeapHardLimitPOHPercent : defaultValue;

        /// <summary>Records the value of GCHeapHardLimitPOHPercent reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCHeapHardLimitPOHPercent(long value) => s_UpdatedGCHeapHardLimitPOHPercent = value;

        private static long s_GCHeapHardLimitPOHPercent = 0;
        private static byte s_GCHeapHardLimitPOHPercentProvided;
        private static long s_UpdatedGCHeapHardLimitPOHPercent = 0;

        /// <summary>Specifies whether GC can use AVX2 or AVX512F - 0 for neither, 1 for AVX2, 3 for AVX512F</summary>
        public static long GetGCEnabledInstructionSets() => s_GCEnabledInstructionSets;

        /// <summary>Specifies whether GC can use AVX2 or AVX512F - 0 for neither, 1 for AVX2, 3 for AVX512F, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCEnabledInstructionSets(long defaultValue) => s_GCEnabledInstructionSetsProvided != 0 ? s_GCEnabledInstructionSets : defaultValue;

        /// <summary>Records the value of GCEnabledInstructionSets reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCEnabledInstructionSets(long value) => s_UpdatedGCEnabledInstructionSets = value;

        private static long s_GCEnabledInstructionSets = -1;
        private static byte s_GCEnabledInstructionSetsProvided;
        private static long s_UpdatedGCEnabledInstructionSets = -1;

        /// <summary>Specifies how hard GC should try to conserve memory - values 0-9</summary>
        public static long GetGCConserveMem() => s_GCConserveMem;

        /// <summary>Specifies how hard GC should try to conserve memory - values 0-9, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCConserveMem(long defaultValue) => s_GCConserveMemProvided != 0 ? s_GCConserveMem : defaultValue;

        /// <summary>Records the value of GCConserveMem reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCConserveMem(long value) => s_UpdatedGCConserveMem = value;

        private static long s_GCConserveMem = 0;
        private static byte s_GCConserveMemProvided;
        private static long s_UpdatedGCConserveMem = 0;

        /// <summary>Specifies whether GC should use more precise but slower write barrier</summary>
        public static long GetGCWriteBarrier() => s_GCWriteBarrier;

        /// <summary>Specifies whether GC should use more precise but slower write barrier, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCWriteBarrier(long defaultValue) => s_GCWriteBarrierProvided != 0 ? s_GCWriteBarrier : defaultValue;

        /// <summary>Records the value of GCWriteBarrier reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCWriteBarrier(long value) => s_UpdatedGCWriteBarrier = value;

        private static long s_GCWriteBarrier = 0;
        private static byte s_GCWriteBarrierProvided;
        private static long s_UpdatedGCWriteBarrier = 0;

        /// <summary>Specifies the name of the standalone GC implementation.</summary>
        /// <remarks>The returned string is owned by the EE and must be released with <see cref="GCToEEInterface.FreeStringConfigValue"/>.</remarks>
        public static byte* GetGCName()
        {
            byte* resultStr = null;
            fixed (byte* privateKey = "GCName\0"u8)
            fixed (byte* publicKey = "System.GC.Name\0"u8)
                GCToEEInterface.GetStringConfigValue(privateKey, publicKey, &resultStr);
            return resultStr;
        }

        /// <summary>Specifies the path of the standalone GC implementation.</summary>
        /// <remarks>The returned string is owned by the EE and must be released with <see cref="GCToEEInterface.FreeStringConfigValue"/>.</remarks>
        public static byte* GetGCPath()
        {
            byte* resultStr = null;
            fixed (byte* privateKey = "GCPath\0"u8)
            fixed (byte* publicKey = "System.GC.Path\0"u8)
                GCToEEInterface.GetStringConfigValue(privateKey, publicKey, &resultStr);
            return resultStr;
        }

        /// <summary>Specifies the spin count unit used by the GC.</summary>
        public static long GetGCSpinCountUnit() => s_GCSpinCountUnit;

        /// <summary>Specifies the spin count unit used by the GC., or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCSpinCountUnit(long defaultValue) => s_GCSpinCountUnitProvided != 0 ? s_GCSpinCountUnit : defaultValue;

        /// <summary>Records the value of GCSpinCountUnit reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCSpinCountUnit(long value) => s_UpdatedGCSpinCountUnit = value;

        private static long s_GCSpinCountUnit = 0;
        private static byte s_GCSpinCountUnitProvided;
        private static long s_UpdatedGCSpinCountUnit = 0;

        /// <summary>Enable the GC to dynamically adapt to application sizes.</summary>
        public static long GetGCDynamicAdaptationMode() => s_GCDynamicAdaptationMode;

        /// <summary>Enable the GC to dynamically adapt to application sizes., or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCDynamicAdaptationMode(long defaultValue) => s_GCDynamicAdaptationModeProvided != 0 ? s_GCDynamicAdaptationMode : defaultValue;

        /// <summary>Records the value of GCDynamicAdaptationMode reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCDynamicAdaptationMode(long value) => s_UpdatedGCDynamicAdaptationMode = value;

        private static long s_GCDynamicAdaptationMode = 1;
        private static byte s_GCDynamicAdaptationModeProvided;
        private static long s_UpdatedGCDynamicAdaptationMode = 1;

        /// <summary>Specifies the target tcp for DATAS</summary>
        public static long GetGCDTargetTCP() => s_GCDTargetTCP;

        /// <summary>Specifies the target tcp for DATAS, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCDTargetTCP(long defaultValue) => s_GCDTargetTCPProvided != 0 ? s_GCDTargetTCP : defaultValue;

        /// <summary>Records the value of GCDTargetTCP reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCDTargetTCP(long value) => s_UpdatedGCDTargetTCP = value;

        private static long s_GCDTargetTCP = 0;
        private static byte s_GCDTargetTCPProvided;
        private static long s_UpdatedGCDTargetTCP = 0;

        /// <summary>Specifies the ratio of BGC to NGC2 for HC change</summary>
        public static long GetGCDBGCRatio() => s_GCDBGCRatio;

        /// <summary>Specifies the ratio of BGC to NGC2 for HC change, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCDBGCRatio(long defaultValue) => s_GCDBGCRatioProvided != 0 ? s_GCDBGCRatio : defaultValue;

        /// <summary>Records the value of GCDBGCRatio reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCDBGCRatio(long value) => s_UpdatedGCDBGCRatio = value;

        private static long s_GCDBGCRatio = 0;
        private static byte s_GCDBGCRatioProvided;
        private static long s_UpdatedGCDBGCRatio = 0;

        /// <summary>Specifies the percentage of the default growth factor</summary>
        public static long GetGCDGen0GrowthPercent() => s_GCDGen0GrowthPercent;

        /// <summary>Specifies the percentage of the default growth factor, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCDGen0GrowthPercent(long defaultValue) => s_GCDGen0GrowthPercentProvided != 0 ? s_GCDGen0GrowthPercent : defaultValue;

        /// <summary>Records the value of GCDGen0GrowthPercent reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCDGen0GrowthPercent(long value) => s_UpdatedGCDGen0GrowthPercent = value;

        private static long s_GCDGen0GrowthPercent = 0;
        private static byte s_GCDGen0GrowthPercentProvided;
        private static long s_UpdatedGCDGen0GrowthPercent = 0;

        /// <summary>Specifies the minimum growth factor in permil</summary>
        public static long GetGCDGen0GrowthMinFactor() => s_GCDGen0GrowthMinFactor;

        /// <summary>Specifies the minimum growth factor in permil, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCDGen0GrowthMinFactor(long defaultValue) => s_GCDGen0GrowthMinFactorProvided != 0 ? s_GCDGen0GrowthMinFactor : defaultValue;

        /// <summary>Records the value of GCDGen0GrowthMinFactor reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCDGen0GrowthMinFactor(long value) => s_UpdatedGCDGen0GrowthMinFactor = value;

        private static long s_GCDGen0GrowthMinFactor = 0;
        private static byte s_GCDGen0GrowthMinFactorProvided;
        private static long s_UpdatedGCDGen0GrowthMinFactor = 0;

        /// <summary>Specifies the maximum growth factor in permil</summary>
        public static long GetGCDGen0GrowthMaxFactor() => s_GCDGen0GrowthMaxFactor;

        /// <summary>Specifies the maximum growth factor in permil, or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static long GetGCDGen0GrowthMaxFactor(long defaultValue) => s_GCDGen0GrowthMaxFactorProvided != 0 ? s_GCDGen0GrowthMaxFactor : defaultValue;

        /// <summary>Records the value of GCDGen0GrowthMaxFactor reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCDGen0GrowthMaxFactor(long value) => s_UpdatedGCDGen0GrowthMaxFactor = value;

        private static long s_GCDGen0GrowthMaxFactor = 0;
        private static byte s_GCDGen0GrowthMaxFactorProvided;
        private static long s_UpdatedGCDGen0GrowthMaxFactor = 0;

        /// <summary>Specifies using sysconf to retrieve the last level cache size for Unix.</summary>
        public static byte GetGCCacheSizeFromSysConf() => s_GCCacheSizeFromSysConf;

        /// <summary>Specifies using sysconf to retrieve the last level cache size for Unix., or <paramref name="defaultValue"/> if it was not configured.</summary>
        public static byte GetGCCacheSizeFromSysConf(byte defaultValue) => s_GCCacheSizeFromSysConfProvided != 0 ? s_GCCacheSizeFromSysConf : defaultValue;

        /// <summary>Records the value of GCCacheSizeFromSysConf reported by <see cref="EnumerateConfigurationValues"/>.</summary>
        public static void SetGCCacheSizeFromSysConf(byte value) => s_UpdatedGCCacheSizeFromSysConf = value;

        private static byte s_GCCacheSizeFromSysConf = 0;
        private static byte s_GCCacheSizeFromSysConfProvided;
        private static byte s_UpdatedGCCacheSizeFromSysConf = 0;

#pragma warning restore CA1805

        /// <summary>
        /// Initializes the GCConfig subsystem. Must be called before accessing any configuration
        /// information.
        /// </summary>
        public static void Initialize()
        {
            fixed (byte* privateKey = "gcServer\0"u8)
            fixed (byte* publicKey = "System.GC.Server\0"u8)
            {
                byte value = s_ServerGC;
                s_ServerGCProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, publicKey, &value);
                s_ServerGC = value;
            }
            s_UpdatedServerGC = s_ServerGC;

            fixed (byte* privateKey = "gcConcurrent\0"u8)
            fixed (byte* publicKey = "System.GC.Concurrent\0"u8)
            {
                byte value = s_ConcurrentGC;
                s_ConcurrentGCProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, publicKey, &value);
                s_ConcurrentGC = value;
            }
            s_UpdatedConcurrentGC = s_ConcurrentGC;

            fixed (byte* privateKey = "gcConservative\0"u8)
            {
                byte value = s_ConservativeGC;
                s_ConservativeGCProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, null, &value);
                s_ConservativeGC = value;
            }
            s_UpdatedConservativeGC = s_ConservativeGC;

            fixed (byte* privateKey = "gcForceCompact\0"u8)
            {
                byte value = s_ForceCompact;
                s_ForceCompactProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, null, &value);
                s_ForceCompact = value;
            }
            s_UpdatedForceCompact = s_ForceCompact;

            fixed (byte* privateKey = "GCRetainVM\0"u8)
            fixed (byte* publicKey = "System.GC.RetainVM\0"u8)
            {
                byte value = s_RetainVM;
                s_RetainVMProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, publicKey, &value);
                s_RetainVM = value;
            }
            s_UpdatedRetainVM = s_RetainVM;

            fixed (byte* privateKey = "GCBreakOnOOM\0"u8)
            {
                byte value = s_BreakOnOOM;
                s_BreakOnOOMProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, null, &value);
                s_BreakOnOOM = value;
            }
            s_UpdatedBreakOnOOM = s_BreakOnOOM;

            fixed (byte* privateKey = "GCNoAffinitize\0"u8)
            fixed (byte* publicKey = "System.GC.NoAffinitize\0"u8)
            {
                byte value = s_NoAffinitize;
                s_NoAffinitizeProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, publicKey, &value);
                s_NoAffinitize = value;
            }
            s_UpdatedNoAffinitize = s_NoAffinitize;

            fixed (byte* privateKey = "GCLogEnabled\0"u8)
            {
                byte value = s_LogEnabled;
                s_LogEnabledProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, null, &value);
                s_LogEnabled = value;
            }
            s_UpdatedLogEnabled = s_LogEnabled;

            fixed (byte* privateKey = "GCConfigLogEnabled\0"u8)
            {
                byte value = s_ConfigLogEnabled;
                s_ConfigLogEnabledProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, null, &value);
                s_ConfigLogEnabled = value;
            }
            s_UpdatedConfigLogEnabled = s_ConfigLogEnabled;

            fixed (byte* privateKey = "GCNumaAware\0"u8)
            {
                byte value = s_GCNumaAware;
                s_GCNumaAwareProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, null, &value);
                s_GCNumaAware = value;
            }
            s_UpdatedGCNumaAware = s_GCNumaAware;

            fixed (byte* privateKey = "GCCpuGroup\0"u8)
            fixed (byte* publicKey = "System.GC.CpuGroup\0"u8)
            {
                byte value = s_GCCpuGroup;
                s_GCCpuGroupProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, publicKey, &value);
                s_GCCpuGroup = value;
            }
            s_UpdatedGCCpuGroup = s_GCCpuGroup;

            fixed (byte* privateKey = "GCLargePages\0"u8)
            fixed (byte* publicKey = "System.GC.LargePages\0"u8)
            {
                long value = s_GCLargePages;
                s_GCLargePagesProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCLargePages = value;
            }
            s_UpdatedGCLargePages = s_GCLargePages;

            fixed (byte* privateKey = "HeapVerify\0"u8)
            {
                long value = s_HeapVerifyLevel;
                s_HeapVerifyLevelProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_HeapVerifyLevel = value;
            }
            s_UpdatedHeapVerifyLevel = s_HeapVerifyLevel;

            fixed (byte* privateKey = "GCLOHCompact\0"u8)
            {
                long value = s_LOHCompactionMode;
                s_LOHCompactionModeProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_LOHCompactionMode = value;
            }
            s_UpdatedLOHCompactionMode = s_LOHCompactionMode;

            fixed (byte* privateKey = "GCLOHThreshold\0"u8)
            fixed (byte* publicKey = "System.GC.LOHThreshold\0"u8)
            {
                long value = s_LOHThreshold;
                s_LOHThresholdProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_LOHThreshold = value;
            }
            s_UpdatedLOHThreshold = s_LOHThreshold;

            fixed (byte* privateKey = "BGCSpinCount\0"u8)
            {
                long value = s_BGCSpinCount;
                s_BGCSpinCountProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCSpinCount = value;
            }
            s_UpdatedBGCSpinCount = s_BGCSpinCount;

            fixed (byte* privateKey = "BGCSpin\0"u8)
            {
                long value = s_BGCSpin;
                s_BGCSpinProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCSpin = value;
            }
            s_UpdatedBGCSpin = s_BGCSpin;

            fixed (byte* privateKey = "GCHeapCount\0"u8)
            fixed (byte* publicKey = "System.GC.HeapCount\0"u8)
            {
                long value = s_HeapCount;
                s_HeapCountProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_HeapCount = value;
            }
            s_UpdatedHeapCount = s_HeapCount;

            fixed (byte* privateKey = "GCMaxHeapCount\0"u8)
            fixed (byte* publicKey = "System.GC.MaxHeapCount\0"u8)
            {
                long value = s_MaxHeapCount;
                s_MaxHeapCountProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_MaxHeapCount = value;
            }
            s_UpdatedMaxHeapCount = s_MaxHeapCount;

            fixed (byte* privateKey = "GCgen0size\0"u8)
            {
                long value = s_Gen0Size;
                s_Gen0SizeProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_Gen0Size = value;
            }
            s_UpdatedGen0Size = s_Gen0Size;

            fixed (byte* privateKey = "GCSegmentSize\0"u8)
            {
                long value = s_SegmentSize;
                s_SegmentSizeProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_SegmentSize = value;
            }
            s_UpdatedSegmentSize = s_SegmentSize;

            fixed (byte* privateKey = "GCLatencyMode\0"u8)
            {
                long value = s_LatencyMode;
                s_LatencyModeProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_LatencyMode = value;
            }
            s_UpdatedLatencyMode = s_LatencyMode;

            fixed (byte* privateKey = "GCLatencyLevel\0"u8)
            {
                long value = s_LatencyLevel;
                s_LatencyLevelProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_LatencyLevel = value;
            }
            s_UpdatedLatencyLevel = s_LatencyLevel;

            fixed (byte* privateKey = "GCLogFileSize\0"u8)
            {
                long value = s_LogFileSize;
                s_LogFileSizeProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_LogFileSize = value;
            }
            s_UpdatedLogFileSize = s_LogFileSize;

            fixed (byte* privateKey = "GCCompactRatio\0"u8)
            {
                long value = s_CompactRatio;
                s_CompactRatioProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_CompactRatio = value;
            }
            s_UpdatedCompactRatio = s_CompactRatio;

            fixed (byte* privateKey = "GCHeapAffinitizeMask\0"u8)
            fixed (byte* publicKey = "System.GC.HeapAffinitizeMask\0"u8)
            {
                long value = s_GCHeapAffinitizeMask;
                s_GCHeapAffinitizeMaskProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapAffinitizeMask = value;
            }
            s_UpdatedGCHeapAffinitizeMask = s_GCHeapAffinitizeMask;

            fixed (byte* privateKey = "GCTrimYoungestKeepPercent\0"u8)
            {
                long value = s_GCTrimYoungestKeepPercent;
                s_GCTrimYoungestKeepPercentProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_GCTrimYoungestKeepPercent = value;
            }
            s_UpdatedGCTrimYoungestKeepPercent = s_GCTrimYoungestKeepPercent;

            fixed (byte* privateKey = "GCHighMemPercent\0"u8)
            fixed (byte* publicKey = "System.GC.HighMemoryPercent\0"u8)
            {
                long value = s_GCHighMemPercent;
                s_GCHighMemPercentProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHighMemPercent = value;
            }
            s_UpdatedGCHighMemPercent = s_GCHighMemPercent;

            fixed (byte* privateKey = "GCProvModeStress\0"u8)
            {
                long value = s_GCProvModeStress;
                s_GCProvModeStressProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_GCProvModeStress = value;
            }
            s_UpdatedGCProvModeStress = s_GCProvModeStress;

            fixed (byte* privateKey = "GCGen0MaxBudget\0"u8)
            fixed (byte* publicKey = "System.GC.Gen0MaxBudget\0"u8)
            {
                long value = s_GCGen0MaxBudget;
                s_GCGen0MaxBudgetProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCGen0MaxBudget = value;
            }
            s_UpdatedGCGen0MaxBudget = s_GCGen0MaxBudget;

            fixed (byte* privateKey = "GCGen1MaxBudget\0"u8)
            {
                long value = s_GCGen1MaxBudget;
                s_GCGen1MaxBudgetProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_GCGen1MaxBudget = value;
            }
            s_UpdatedGCGen1MaxBudget = s_GCGen1MaxBudget;

            fixed (byte* privateKey = "GCLowSkipRatio\0"u8)
            {
                long value = s_GCLowSkipRatio;
                s_GCLowSkipRatioProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_GCLowSkipRatio = value;
            }
            s_UpdatedGCLowSkipRatio = s_GCLowSkipRatio;

            fixed (byte* privateKey = "GCHeapHardLimit\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimit\0"u8)
            {
                long value = s_GCHeapHardLimit;
                s_GCHeapHardLimitProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimit = value;
            }
            s_UpdatedGCHeapHardLimit = s_GCHeapHardLimit;

            fixed (byte* privateKey = "GCHeapHardLimitPercent\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitPercent\0"u8)
            {
                long value = s_GCHeapHardLimitPercent;
                s_GCHeapHardLimitPercentProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitPercent = value;
            }
            s_UpdatedGCHeapHardLimitPercent = s_GCHeapHardLimitPercent;

            fixed (byte* privateKey = "GCTotalPhysicalMemory\0"u8)
            {
                long value = s_GCTotalPhysicalMemory;
                s_GCTotalPhysicalMemoryProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_GCTotalPhysicalMemory = value;
            }
            s_UpdatedGCTotalPhysicalMemory = s_GCTotalPhysicalMemory;

            fixed (byte* privateKey = "GCRegionRange\0"u8)
            fixed (byte* publicKey = "System.GC.RegionRange\0"u8)
            {
                long value = s_GCRegionRange;
                s_GCRegionRangeProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCRegionRange = value;
            }
            s_UpdatedGCRegionRange = s_GCRegionRange;

            fixed (byte* privateKey = "GCRegionSize\0"u8)
            fixed (byte* publicKey = "System.GC.RegionSize\0"u8)
            {
                long value = s_GCRegionSize;
                s_GCRegionSizeProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCRegionSize = value;
            }
            s_UpdatedGCRegionSize = s_GCRegionSize;

            fixed (byte* privateKey = "GCEnableSpecialRegions\0"u8)
            {
                long value = s_GCEnableSpecialRegions;
                s_GCEnableSpecialRegionsProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_GCEnableSpecialRegions = value;
            }
            s_UpdatedGCEnableSpecialRegions = s_GCEnableSpecialRegions;

            fixed (byte* privateKey = "BGCFLTuningEnabled\0"u8)
            {
                long value = s_BGCFLTuningEnabled;
                s_BGCFLTuningEnabledProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLTuningEnabled = value;
            }
            s_UpdatedBGCFLTuningEnabled = s_BGCFLTuningEnabled;

            fixed (byte* privateKey = "BGCMemGoal\0"u8)
            {
                long value = s_BGCMemGoal;
                s_BGCMemGoalProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCMemGoal = value;
            }
            s_UpdatedBGCMemGoal = s_BGCMemGoal;

            fixed (byte* privateKey = "BGCMemGoalSlack\0"u8)
            {
                long value = s_BGCMemGoalSlack;
                s_BGCMemGoalSlackProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCMemGoalSlack = value;
            }
            s_UpdatedBGCMemGoalSlack = s_BGCMemGoalSlack;

            fixed (byte* privateKey = "BGCFLSweepGoal\0"u8)
            {
                long value = s_BGCFLSweepGoal;
                s_BGCFLSweepGoalProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLSweepGoal = value;
            }
            s_UpdatedBGCFLSweepGoal = s_BGCFLSweepGoal;

            fixed (byte* privateKey = "BGCFLSweepGoalLOH\0"u8)
            {
                long value = s_BGCFLSweepGoalLOH;
                s_BGCFLSweepGoalLOHProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLSweepGoalLOH = value;
            }
            s_UpdatedBGCFLSweepGoalLOH = s_BGCFLSweepGoalLOH;

            fixed (byte* privateKey = "BGCFLkp\0"u8)
            {
                long value = s_BGCFLkp;
                s_BGCFLkpProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLkp = value;
            }
            s_UpdatedBGCFLkp = s_BGCFLkp;

            fixed (byte* privateKey = "BGCFLki\0"u8)
            {
                long value = s_BGCFLki;
                s_BGCFLkiProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLki = value;
            }
            s_UpdatedBGCFLki = s_BGCFLki;

            fixed (byte* privateKey = "BGCFLkd\0"u8)
            {
                long value = s_BGCFLkd;
                s_BGCFLkdProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLkd = value;
            }
            s_UpdatedBGCFLkd = s_BGCFLkd;

            fixed (byte* privateKey = "BGCFLff\0"u8)
            {
                long value = s_BGCFLff;
                s_BGCFLffProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLff = value;
            }
            s_UpdatedBGCFLff = s_BGCFLff;

            fixed (byte* privateKey = "BGCFLSmoothFactor\0"u8)
            {
                long value = s_BGCFLSmoothFactor;
                s_BGCFLSmoothFactorProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLSmoothFactor = value;
            }
            s_UpdatedBGCFLSmoothFactor = s_BGCFLSmoothFactor;

            fixed (byte* privateKey = "BGCFLGradualD\0"u8)
            {
                long value = s_BGCFLGradualD;
                s_BGCFLGradualDProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLGradualD = value;
            }
            s_UpdatedBGCFLGradualD = s_BGCFLGradualD;

            fixed (byte* privateKey = "BGCMLkp\0"u8)
            {
                long value = s_BGCMLkp;
                s_BGCMLkpProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCMLkp = value;
            }
            s_UpdatedBGCMLkp = s_BGCMLkp;

            fixed (byte* privateKey = "BGCMLki\0"u8)
            {
                long value = s_BGCMLki;
                s_BGCMLkiProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCMLki = value;
            }
            s_UpdatedBGCMLki = s_BGCMLki;

            fixed (byte* privateKey = "BGCFLEnableKi\0"u8)
            {
                long value = s_BGCFLEnableKi;
                s_BGCFLEnableKiProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLEnableKi = value;
            }
            s_UpdatedBGCFLEnableKi = s_BGCFLEnableKi;

            fixed (byte* privateKey = "BGCFLEnableKd\0"u8)
            {
                long value = s_BGCFLEnableKd;
                s_BGCFLEnableKdProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLEnableKd = value;
            }
            s_UpdatedBGCFLEnableKd = s_BGCFLEnableKd;

            fixed (byte* privateKey = "BGCFLEnableSmooth\0"u8)
            {
                long value = s_BGCFLEnableSmooth;
                s_BGCFLEnableSmoothProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLEnableSmooth = value;
            }
            s_UpdatedBGCFLEnableSmooth = s_BGCFLEnableSmooth;

            fixed (byte* privateKey = "BGCFLEnableTBH\0"u8)
            {
                long value = s_BGCFLEnableTBH;
                s_BGCFLEnableTBHProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLEnableTBH = value;
            }
            s_UpdatedBGCFLEnableTBH = s_BGCFLEnableTBH;

            fixed (byte* privateKey = "BGCFLEnableFF\0"u8)
            {
                long value = s_BGCFLEnableFF;
                s_BGCFLEnableFFProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCFLEnableFF = value;
            }
            s_UpdatedBGCFLEnableFF = s_BGCFLEnableFF;

            fixed (byte* privateKey = "BGCG2RatioStep\0"u8)
            {
                long value = s_BGCG2RatioStep;
                s_BGCG2RatioStepProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_BGCG2RatioStep = value;
            }
            s_UpdatedBGCG2RatioStep = s_BGCG2RatioStep;

            fixed (byte* privateKey = "UOHWaitBGCSizeIncPercent\0"u8)
            fixed (byte* publicKey = "System.GC.UOHWaitBGCSizeIncPercent\0"u8)
            {
                long value = s_UOHWaitBGCSizeIncPercent;
                s_UOHWaitBGCSizeIncPercentProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_UOHWaitBGCSizeIncPercent = value;
            }
            s_UpdatedUOHWaitBGCSizeIncPercent = s_UOHWaitBGCSizeIncPercent;

            fixed (byte* privateKey = "GCHeapHardLimitSOH\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitSOH\0"u8)
            {
                long value = s_GCHeapHardLimitSOH;
                s_GCHeapHardLimitSOHProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitSOH = value;
            }
            s_UpdatedGCHeapHardLimitSOH = s_GCHeapHardLimitSOH;

            fixed (byte* privateKey = "GCHeapHardLimitLOH\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitLOH\0"u8)
            {
                long value = s_GCHeapHardLimitLOH;
                s_GCHeapHardLimitLOHProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitLOH = value;
            }
            s_UpdatedGCHeapHardLimitLOH = s_GCHeapHardLimitLOH;

            fixed (byte* privateKey = "GCHeapHardLimitPOH\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitPOH\0"u8)
            {
                long value = s_GCHeapHardLimitPOH;
                s_GCHeapHardLimitPOHProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitPOH = value;
            }
            s_UpdatedGCHeapHardLimitPOH = s_GCHeapHardLimitPOH;

            fixed (byte* privateKey = "GCHeapHardLimitSOHPercent\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitSOHPercent\0"u8)
            {
                long value = s_GCHeapHardLimitSOHPercent;
                s_GCHeapHardLimitSOHPercentProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitSOHPercent = value;
            }
            s_UpdatedGCHeapHardLimitSOHPercent = s_GCHeapHardLimitSOHPercent;

            fixed (byte* privateKey = "GCHeapHardLimitLOHPercent\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitLOHPercent\0"u8)
            {
                long value = s_GCHeapHardLimitLOHPercent;
                s_GCHeapHardLimitLOHPercentProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitLOHPercent = value;
            }
            s_UpdatedGCHeapHardLimitLOHPercent = s_GCHeapHardLimitLOHPercent;

            fixed (byte* privateKey = "GCHeapHardLimitPOHPercent\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitPOHPercent\0"u8)
            {
                long value = s_GCHeapHardLimitPOHPercent;
                s_GCHeapHardLimitPOHPercentProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitPOHPercent = value;
            }
            s_UpdatedGCHeapHardLimitPOHPercent = s_GCHeapHardLimitPOHPercent;

            fixed (byte* privateKey = "GCEnabledInstructionSets\0"u8)
            {
                long value = s_GCEnabledInstructionSets;
                s_GCEnabledInstructionSetsProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_GCEnabledInstructionSets = value;
            }
            s_UpdatedGCEnabledInstructionSets = s_GCEnabledInstructionSets;

            fixed (byte* privateKey = "GCConserveMemory\0"u8)
            fixed (byte* publicKey = "System.GC.ConserveMemory\0"u8)
            {
                long value = s_GCConserveMem;
                s_GCConserveMemProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCConserveMem = value;
            }
            s_UpdatedGCConserveMem = s_GCConserveMem;

            fixed (byte* privateKey = "GCWriteBarrier\0"u8)
            {
                long value = s_GCWriteBarrier;
                s_GCWriteBarrierProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_GCWriteBarrier = value;
            }
            s_UpdatedGCWriteBarrier = s_GCWriteBarrier;

            fixed (byte* privateKey = "GCSpinCountUnit\0"u8)
            {
                long value = s_GCSpinCountUnit;
                s_GCSpinCountUnitProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_GCSpinCountUnit = value;
            }
            s_UpdatedGCSpinCountUnit = s_GCSpinCountUnit;

            fixed (byte* privateKey = "GCDynamicAdaptationMode\0"u8)
            fixed (byte* publicKey = "System.GC.DynamicAdaptationMode\0"u8)
            {
                long value = s_GCDynamicAdaptationMode;
                s_GCDynamicAdaptationModeProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCDynamicAdaptationMode = value;
            }
            s_UpdatedGCDynamicAdaptationMode = s_GCDynamicAdaptationMode;

            fixed (byte* privateKey = "GCDTargetTCP\0"u8)
            fixed (byte* publicKey = "System.GC.DTargetTCP\0"u8)
            {
                long value = s_GCDTargetTCP;
                s_GCDTargetTCPProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCDTargetTCP = value;
            }
            s_UpdatedGCDTargetTCP = s_GCDTargetTCP;

            fixed (byte* privateKey = "GCDBGCRatio\0"u8)
            {
                long value = s_GCDBGCRatio;
                s_GCDBGCRatioProvided = GCToEEInterface.GetIntConfigValue(privateKey, null, &value);
                s_GCDBGCRatio = value;
            }
            s_UpdatedGCDBGCRatio = s_GCDBGCRatio;

            fixed (byte* privateKey = "GCDGen0GrowthPercent\0"u8)
            fixed (byte* publicKey = "System.GC.DGen0GrowthPercent\0"u8)
            {
                long value = s_GCDGen0GrowthPercent;
                s_GCDGen0GrowthPercentProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCDGen0GrowthPercent = value;
            }
            s_UpdatedGCDGen0GrowthPercent = s_GCDGen0GrowthPercent;

            fixed (byte* privateKey = "GCDGen0GrowthMinFactor\0"u8)
            fixed (byte* publicKey = "System.GC.DGen0GrowthMinFactor\0"u8)
            {
                long value = s_GCDGen0GrowthMinFactor;
                s_GCDGen0GrowthMinFactorProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCDGen0GrowthMinFactor = value;
            }
            s_UpdatedGCDGen0GrowthMinFactor = s_GCDGen0GrowthMinFactor;

            fixed (byte* privateKey = "GCDGen0GrowthMaxFactor\0"u8)
            fixed (byte* publicKey = "System.GC.DGen0GrowthMaxFactor\0"u8)
            {
                long value = s_GCDGen0GrowthMaxFactor;
                s_GCDGen0GrowthMaxFactorProvided = GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCDGen0GrowthMaxFactor = value;
            }
            s_UpdatedGCDGen0GrowthMaxFactor = s_GCDGen0GrowthMaxFactor;

            fixed (byte* privateKey = "GCCacheSizeFromSysConf\0"u8)
            {
                byte value = s_GCCacheSizeFromSysConf;
                s_GCCacheSizeFromSysConfProvided = GCToEEInterface.GetBooleanConfigValue(privateKey, null, &value);
                s_GCCacheSizeFromSysConf = value;
            }
            s_UpdatedGCCacheSizeFromSysConf = s_GCCacheSizeFromSysConf;

        }

        /// <summary>Re-reads the heap hard limit configs, which can change at runtime.</summary>
        public static void RefreshHeapHardLimitSettings()
        {
            fixed (byte* privateKey = "GCHeapHardLimit\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimit\0"u8)
            {
                long value = s_GCHeapHardLimit;
                GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimit = value;
            }
            s_UpdatedGCHeapHardLimit = s_GCHeapHardLimit;

            fixed (byte* privateKey = "GCHeapHardLimitPercent\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitPercent\0"u8)
            {
                long value = s_GCHeapHardLimitPercent;
                GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitPercent = value;
            }
            s_UpdatedGCHeapHardLimitPercent = s_GCHeapHardLimitPercent;

            fixed (byte* privateKey = "GCHeapHardLimitSOH\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitSOH\0"u8)
            {
                long value = s_GCHeapHardLimitSOH;
                GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitSOH = value;
            }
            s_UpdatedGCHeapHardLimitSOH = s_GCHeapHardLimitSOH;

            fixed (byte* privateKey = "GCHeapHardLimitLOH\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitLOH\0"u8)
            {
                long value = s_GCHeapHardLimitLOH;
                GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitLOH = value;
            }
            s_UpdatedGCHeapHardLimitLOH = s_GCHeapHardLimitLOH;

            fixed (byte* privateKey = "GCHeapHardLimitPOH\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitPOH\0"u8)
            {
                long value = s_GCHeapHardLimitPOH;
                GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitPOH = value;
            }
            s_UpdatedGCHeapHardLimitPOH = s_GCHeapHardLimitPOH;

            fixed (byte* privateKey = "GCHeapHardLimitSOHPercent\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitSOHPercent\0"u8)
            {
                long value = s_GCHeapHardLimitSOHPercent;
                GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitSOHPercent = value;
            }
            s_UpdatedGCHeapHardLimitSOHPercent = s_GCHeapHardLimitSOHPercent;

            fixed (byte* privateKey = "GCHeapHardLimitLOHPercent\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitLOHPercent\0"u8)
            {
                long value = s_GCHeapHardLimitLOHPercent;
                GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitLOHPercent = value;
            }
            s_UpdatedGCHeapHardLimitLOHPercent = s_GCHeapHardLimitLOHPercent;

            fixed (byte* privateKey = "GCHeapHardLimitPOHPercent\0"u8)
            fixed (byte* publicKey = "System.GC.HeapHardLimitPOHPercent\0"u8)
            {
                long value = s_GCHeapHardLimitPOHPercent;
                GCToEEInterface.GetIntConfigValue(privateKey, publicKey, &value);
                s_GCHeapHardLimitPOHPercent = value;
            }
            s_UpdatedGCHeapHardLimitPOHPercent = s_GCHeapHardLimitPOHPercent;

        }

        /// <summary>
        /// Reports every configuration value, with its public name and current value, to
        /// <paramref name="configurationValueFunc"/>.
        /// </summary>
        public static void EnumerateConfigurationValues(void* context, delegate* unmanaged<void*, byte*, byte*, GCConfigurationType, long, void> configurationValueFunc)
        {
            fixed (byte* configNameServerGC = "ServerGC\0"u8)
            fixed (byte* publicKeyServerGC = "System.GC.Server\0"u8)
                configurationValueFunc(context, configNameServerGC, publicKeyServerGC, GCConfigurationType.Boolean, (long)s_UpdatedServerGC);

            fixed (byte* configNameConcurrentGC = "ConcurrentGC\0"u8)
            fixed (byte* publicKeyConcurrentGC = "System.GC.Concurrent\0"u8)
                configurationValueFunc(context, configNameConcurrentGC, publicKeyConcurrentGC, GCConfigurationType.Boolean, (long)s_UpdatedConcurrentGC);

            fixed (byte* configNameConservativeGC = "ConservativeGC\0"u8)
                configurationValueFunc(context, configNameConservativeGC, null, GCConfigurationType.Boolean, (long)s_UpdatedConservativeGC);

            fixed (byte* configNameForceCompact = "ForceCompact\0"u8)
                configurationValueFunc(context, configNameForceCompact, null, GCConfigurationType.Boolean, (long)s_UpdatedForceCompact);

            fixed (byte* configNameRetainVM = "RetainVM\0"u8)
            fixed (byte* publicKeyRetainVM = "System.GC.RetainVM\0"u8)
                configurationValueFunc(context, configNameRetainVM, publicKeyRetainVM, GCConfigurationType.Boolean, (long)s_UpdatedRetainVM);

            fixed (byte* configNameBreakOnOOM = "BreakOnOOM\0"u8)
                configurationValueFunc(context, configNameBreakOnOOM, null, GCConfigurationType.Boolean, (long)s_UpdatedBreakOnOOM);

            fixed (byte* configNameNoAffinitize = "NoAffinitize\0"u8)
            fixed (byte* publicKeyNoAffinitize = "System.GC.NoAffinitize\0"u8)
                configurationValueFunc(context, configNameNoAffinitize, publicKeyNoAffinitize, GCConfigurationType.Boolean, (long)s_UpdatedNoAffinitize);

            fixed (byte* configNameLogEnabled = "LogEnabled\0"u8)
                configurationValueFunc(context, configNameLogEnabled, null, GCConfigurationType.Boolean, (long)s_UpdatedLogEnabled);

            fixed (byte* configNameConfigLogEnabled = "ConfigLogEnabled\0"u8)
                configurationValueFunc(context, configNameConfigLogEnabled, null, GCConfigurationType.Boolean, (long)s_UpdatedConfigLogEnabled);

            fixed (byte* configNameGCNumaAware = "GCNumaAware\0"u8)
                configurationValueFunc(context, configNameGCNumaAware, null, GCConfigurationType.Boolean, (long)s_UpdatedGCNumaAware);

            fixed (byte* configNameGCCpuGroup = "GCCpuGroup\0"u8)
            fixed (byte* publicKeyGCCpuGroup = "System.GC.CpuGroup\0"u8)
                configurationValueFunc(context, configNameGCCpuGroup, publicKeyGCCpuGroup, GCConfigurationType.Boolean, (long)s_UpdatedGCCpuGroup);

            fixed (byte* configNameGCLargePages = "GCLargePages\0"u8)
            fixed (byte* publicKeyGCLargePages = "System.GC.LargePages\0"u8)
                configurationValueFunc(context, configNameGCLargePages, publicKeyGCLargePages, GCConfigurationType.Int64, (long)s_UpdatedGCLargePages);

            fixed (byte* configNameHeapVerifyLevel = "HeapVerifyLevel\0"u8)
                configurationValueFunc(context, configNameHeapVerifyLevel, null, GCConfigurationType.Int64, (long)s_UpdatedHeapVerifyLevel);

            fixed (byte* configNameLOHCompactionMode = "LOHCompactionMode\0"u8)
                configurationValueFunc(context, configNameLOHCompactionMode, null, GCConfigurationType.Int64, (long)s_UpdatedLOHCompactionMode);

            fixed (byte* configNameLOHThreshold = "LOHThreshold\0"u8)
            fixed (byte* publicKeyLOHThreshold = "System.GC.LOHThreshold\0"u8)
                configurationValueFunc(context, configNameLOHThreshold, publicKeyLOHThreshold, GCConfigurationType.Int64, (long)s_UpdatedLOHThreshold);

            fixed (byte* configNameBGCSpinCount = "BGCSpinCount\0"u8)
                configurationValueFunc(context, configNameBGCSpinCount, null, GCConfigurationType.Int64, (long)s_UpdatedBGCSpinCount);

            fixed (byte* configNameBGCSpin = "BGCSpin\0"u8)
                configurationValueFunc(context, configNameBGCSpin, null, GCConfigurationType.Int64, (long)s_UpdatedBGCSpin);

            fixed (byte* configNameHeapCount = "HeapCount\0"u8)
            fixed (byte* publicKeyHeapCount = "System.GC.HeapCount\0"u8)
                configurationValueFunc(context, configNameHeapCount, publicKeyHeapCount, GCConfigurationType.Int64, (long)s_UpdatedHeapCount);

            fixed (byte* configNameMaxHeapCount = "MaxHeapCount\0"u8)
            fixed (byte* publicKeyMaxHeapCount = "System.GC.MaxHeapCount\0"u8)
                configurationValueFunc(context, configNameMaxHeapCount, publicKeyMaxHeapCount, GCConfigurationType.Int64, (long)s_UpdatedMaxHeapCount);

            fixed (byte* configNameGen0Size = "Gen0Size\0"u8)
                configurationValueFunc(context, configNameGen0Size, null, GCConfigurationType.Int64, (long)s_UpdatedGen0Size);

            fixed (byte* configNameSegmentSize = "SegmentSize\0"u8)
                configurationValueFunc(context, configNameSegmentSize, null, GCConfigurationType.Int64, (long)s_UpdatedSegmentSize);

            fixed (byte* configNameLatencyMode = "LatencyMode\0"u8)
                configurationValueFunc(context, configNameLatencyMode, null, GCConfigurationType.Int64, (long)s_UpdatedLatencyMode);

            fixed (byte* configNameLatencyLevel = "LatencyLevel\0"u8)
                configurationValueFunc(context, configNameLatencyLevel, null, GCConfigurationType.Int64, (long)s_UpdatedLatencyLevel);

            fixed (byte* configNameLogFileSize = "LogFileSize\0"u8)
                configurationValueFunc(context, configNameLogFileSize, null, GCConfigurationType.Int64, (long)s_UpdatedLogFileSize);

            fixed (byte* configNameCompactRatio = "CompactRatio\0"u8)
                configurationValueFunc(context, configNameCompactRatio, null, GCConfigurationType.Int64, (long)s_UpdatedCompactRatio);

            fixed (byte* configNameGCHeapAffinitizeMask = "GCHeapAffinitizeMask\0"u8)
            fixed (byte* publicKeyGCHeapAffinitizeMask = "System.GC.HeapAffinitizeMask\0"u8)
                configurationValueFunc(context, configNameGCHeapAffinitizeMask, publicKeyGCHeapAffinitizeMask, GCConfigurationType.Int64, (long)s_UpdatedGCHeapAffinitizeMask);

            {
                byte* resultStr = null;
                fixed (byte* privateKey = "GCHeapAffinitizeRanges\0"u8)
                fixed (byte* publicKey = "System.GC.HeapAffinitizeRanges\0"u8)
                    GCToEEInterface.GetStringConfigValue(privateKey, publicKey, &resultStr);
                fixed (byte* configName = "GCHeapAffinitizeRanges\0"u8)
                fixed (byte* publicKey = "System.GC.HeapAffinitizeRanges\0"u8)
                    configurationValueFunc(context, configName, publicKey, GCConfigurationType.StringUtf8, (long)resultStr);
                GCToEEInterface.FreeStringConfigValue(resultStr);
            }

            fixed (byte* configNameGCTrimYoungestKeepPercent = "GCTrimYoungestKeepPercent\0"u8)
                configurationValueFunc(context, configNameGCTrimYoungestKeepPercent, null, GCConfigurationType.Int64, (long)s_UpdatedGCTrimYoungestKeepPercent);

            fixed (byte* configNameGCHighMemPercent = "GCHighMemPercent\0"u8)
            fixed (byte* publicKeyGCHighMemPercent = "System.GC.HighMemoryPercent\0"u8)
                configurationValueFunc(context, configNameGCHighMemPercent, publicKeyGCHighMemPercent, GCConfigurationType.Int64, (long)s_UpdatedGCHighMemPercent);

            fixed (byte* configNameGCProvModeStress = "GCProvModeStress\0"u8)
                configurationValueFunc(context, configNameGCProvModeStress, null, GCConfigurationType.Int64, (long)s_UpdatedGCProvModeStress);

            fixed (byte* configNameGCGen0MaxBudget = "GCGen0MaxBudget\0"u8)
            fixed (byte* publicKeyGCGen0MaxBudget = "System.GC.Gen0MaxBudget\0"u8)
                configurationValueFunc(context, configNameGCGen0MaxBudget, publicKeyGCGen0MaxBudget, GCConfigurationType.Int64, (long)s_UpdatedGCGen0MaxBudget);

            fixed (byte* configNameGCGen1MaxBudget = "GCGen1MaxBudget\0"u8)
                configurationValueFunc(context, configNameGCGen1MaxBudget, null, GCConfigurationType.Int64, (long)s_UpdatedGCGen1MaxBudget);

            fixed (byte* configNameGCLowSkipRatio = "GCLowSkipRatio\0"u8)
                configurationValueFunc(context, configNameGCLowSkipRatio, null, GCConfigurationType.Int64, (long)s_UpdatedGCLowSkipRatio);

            fixed (byte* configNameGCHeapHardLimit = "GCHeapHardLimit\0"u8)
            fixed (byte* publicKeyGCHeapHardLimit = "System.GC.HeapHardLimit\0"u8)
                configurationValueFunc(context, configNameGCHeapHardLimit, publicKeyGCHeapHardLimit, GCConfigurationType.Int64, (long)s_UpdatedGCHeapHardLimit);

            fixed (byte* configNameGCHeapHardLimitPercent = "GCHeapHardLimitPercent\0"u8)
            fixed (byte* publicKeyGCHeapHardLimitPercent = "System.GC.HeapHardLimitPercent\0"u8)
                configurationValueFunc(context, configNameGCHeapHardLimitPercent, publicKeyGCHeapHardLimitPercent, GCConfigurationType.Int64, (long)s_UpdatedGCHeapHardLimitPercent);

            fixed (byte* configNameGCTotalPhysicalMemory = "GCTotalPhysicalMemory\0"u8)
                configurationValueFunc(context, configNameGCTotalPhysicalMemory, null, GCConfigurationType.Int64, (long)s_UpdatedGCTotalPhysicalMemory);

            fixed (byte* configNameGCRegionRange = "GCRegionRange\0"u8)
            fixed (byte* publicKeyGCRegionRange = "System.GC.RegionRange\0"u8)
                configurationValueFunc(context, configNameGCRegionRange, publicKeyGCRegionRange, GCConfigurationType.Int64, (long)s_UpdatedGCRegionRange);

            fixed (byte* configNameGCRegionSize = "GCRegionSize\0"u8)
            fixed (byte* publicKeyGCRegionSize = "System.GC.RegionSize\0"u8)
                configurationValueFunc(context, configNameGCRegionSize, publicKeyGCRegionSize, GCConfigurationType.Int64, (long)s_UpdatedGCRegionSize);

            fixed (byte* configNameGCEnableSpecialRegions = "GCEnableSpecialRegions\0"u8)
                configurationValueFunc(context, configNameGCEnableSpecialRegions, null, GCConfigurationType.Int64, (long)s_UpdatedGCEnableSpecialRegions);

            {
                byte* resultStr = null;
                fixed (byte* privateKey = "GCLogFile\0"u8)
                    GCToEEInterface.GetStringConfigValue(privateKey, null, &resultStr);
                fixed (byte* configName = "LogFile\0"u8)
                    configurationValueFunc(context, configName, null, GCConfigurationType.StringUtf8, (long)resultStr);
                GCToEEInterface.FreeStringConfigValue(resultStr);
            }

            {
                byte* resultStr = null;
                fixed (byte* privateKey = "GCConfigLogFile\0"u8)
                    GCToEEInterface.GetStringConfigValue(privateKey, null, &resultStr);
                fixed (byte* configName = "ConfigLogFile\0"u8)
                    configurationValueFunc(context, configName, null, GCConfigurationType.StringUtf8, (long)resultStr);
                GCToEEInterface.FreeStringConfigValue(resultStr);
            }

            fixed (byte* configNameBGCFLTuningEnabled = "BGCFLTuningEnabled\0"u8)
                configurationValueFunc(context, configNameBGCFLTuningEnabled, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLTuningEnabled);

            fixed (byte* configNameBGCMemGoal = "BGCMemGoal\0"u8)
                configurationValueFunc(context, configNameBGCMemGoal, null, GCConfigurationType.Int64, (long)s_UpdatedBGCMemGoal);

            fixed (byte* configNameBGCMemGoalSlack = "BGCMemGoalSlack\0"u8)
                configurationValueFunc(context, configNameBGCMemGoalSlack, null, GCConfigurationType.Int64, (long)s_UpdatedBGCMemGoalSlack);

            fixed (byte* configNameBGCFLSweepGoal = "BGCFLSweepGoal\0"u8)
                configurationValueFunc(context, configNameBGCFLSweepGoal, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLSweepGoal);

            fixed (byte* configNameBGCFLSweepGoalLOH = "BGCFLSweepGoalLOH\0"u8)
                configurationValueFunc(context, configNameBGCFLSweepGoalLOH, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLSweepGoalLOH);

            fixed (byte* configNameBGCFLkp = "BGCFLkp\0"u8)
                configurationValueFunc(context, configNameBGCFLkp, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLkp);

            fixed (byte* configNameBGCFLki = "BGCFLki\0"u8)
                configurationValueFunc(context, configNameBGCFLki, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLki);

            fixed (byte* configNameBGCFLkd = "BGCFLkd\0"u8)
                configurationValueFunc(context, configNameBGCFLkd, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLkd);

            fixed (byte* configNameBGCFLff = "BGCFLff\0"u8)
                configurationValueFunc(context, configNameBGCFLff, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLff);

            fixed (byte* configNameBGCFLSmoothFactor = "BGCFLSmoothFactor\0"u8)
                configurationValueFunc(context, configNameBGCFLSmoothFactor, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLSmoothFactor);

            fixed (byte* configNameBGCFLGradualD = "BGCFLGradualD\0"u8)
                configurationValueFunc(context, configNameBGCFLGradualD, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLGradualD);

            fixed (byte* configNameBGCMLkp = "BGCMLkp\0"u8)
                configurationValueFunc(context, configNameBGCMLkp, null, GCConfigurationType.Int64, (long)s_UpdatedBGCMLkp);

            fixed (byte* configNameBGCMLki = "BGCMLki\0"u8)
                configurationValueFunc(context, configNameBGCMLki, null, GCConfigurationType.Int64, (long)s_UpdatedBGCMLki);

            fixed (byte* configNameBGCFLEnableKi = "BGCFLEnableKi\0"u8)
                configurationValueFunc(context, configNameBGCFLEnableKi, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLEnableKi);

            fixed (byte* configNameBGCFLEnableKd = "BGCFLEnableKd\0"u8)
                configurationValueFunc(context, configNameBGCFLEnableKd, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLEnableKd);

            fixed (byte* configNameBGCFLEnableSmooth = "BGCFLEnableSmooth\0"u8)
                configurationValueFunc(context, configNameBGCFLEnableSmooth, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLEnableSmooth);

            fixed (byte* configNameBGCFLEnableTBH = "BGCFLEnableTBH\0"u8)
                configurationValueFunc(context, configNameBGCFLEnableTBH, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLEnableTBH);

            fixed (byte* configNameBGCFLEnableFF = "BGCFLEnableFF\0"u8)
                configurationValueFunc(context, configNameBGCFLEnableFF, null, GCConfigurationType.Int64, (long)s_UpdatedBGCFLEnableFF);

            fixed (byte* configNameBGCG2RatioStep = "BGCG2RatioStep\0"u8)
                configurationValueFunc(context, configNameBGCG2RatioStep, null, GCConfigurationType.Int64, (long)s_UpdatedBGCG2RatioStep);

            fixed (byte* configNameUOHWaitBGCSizeIncPercent = "UOHWaitBGCSizeIncPercent\0"u8)
            fixed (byte* publicKeyUOHWaitBGCSizeIncPercent = "System.GC.UOHWaitBGCSizeIncPercent\0"u8)
                configurationValueFunc(context, configNameUOHWaitBGCSizeIncPercent, publicKeyUOHWaitBGCSizeIncPercent, GCConfigurationType.Int64, (long)s_UpdatedUOHWaitBGCSizeIncPercent);

            fixed (byte* configNameGCHeapHardLimitSOH = "GCHeapHardLimitSOH\0"u8)
            fixed (byte* publicKeyGCHeapHardLimitSOH = "System.GC.HeapHardLimitSOH\0"u8)
                configurationValueFunc(context, configNameGCHeapHardLimitSOH, publicKeyGCHeapHardLimitSOH, GCConfigurationType.Int64, (long)s_UpdatedGCHeapHardLimitSOH);

            fixed (byte* configNameGCHeapHardLimitLOH = "GCHeapHardLimitLOH\0"u8)
            fixed (byte* publicKeyGCHeapHardLimitLOH = "System.GC.HeapHardLimitLOH\0"u8)
                configurationValueFunc(context, configNameGCHeapHardLimitLOH, publicKeyGCHeapHardLimitLOH, GCConfigurationType.Int64, (long)s_UpdatedGCHeapHardLimitLOH);

            fixed (byte* configNameGCHeapHardLimitPOH = "GCHeapHardLimitPOH\0"u8)
            fixed (byte* publicKeyGCHeapHardLimitPOH = "System.GC.HeapHardLimitPOH\0"u8)
                configurationValueFunc(context, configNameGCHeapHardLimitPOH, publicKeyGCHeapHardLimitPOH, GCConfigurationType.Int64, (long)s_UpdatedGCHeapHardLimitPOH);

            fixed (byte* configNameGCHeapHardLimitSOHPercent = "GCHeapHardLimitSOHPercent\0"u8)
            fixed (byte* publicKeyGCHeapHardLimitSOHPercent = "System.GC.HeapHardLimitSOHPercent\0"u8)
                configurationValueFunc(context, configNameGCHeapHardLimitSOHPercent, publicKeyGCHeapHardLimitSOHPercent, GCConfigurationType.Int64, (long)s_UpdatedGCHeapHardLimitSOHPercent);

            fixed (byte* configNameGCHeapHardLimitLOHPercent = "GCHeapHardLimitLOHPercent\0"u8)
            fixed (byte* publicKeyGCHeapHardLimitLOHPercent = "System.GC.HeapHardLimitLOHPercent\0"u8)
                configurationValueFunc(context, configNameGCHeapHardLimitLOHPercent, publicKeyGCHeapHardLimitLOHPercent, GCConfigurationType.Int64, (long)s_UpdatedGCHeapHardLimitLOHPercent);

            fixed (byte* configNameGCHeapHardLimitPOHPercent = "GCHeapHardLimitPOHPercent\0"u8)
            fixed (byte* publicKeyGCHeapHardLimitPOHPercent = "System.GC.HeapHardLimitPOHPercent\0"u8)
                configurationValueFunc(context, configNameGCHeapHardLimitPOHPercent, publicKeyGCHeapHardLimitPOHPercent, GCConfigurationType.Int64, (long)s_UpdatedGCHeapHardLimitPOHPercent);

            fixed (byte* configNameGCEnabledInstructionSets = "GCEnabledInstructionSets\0"u8)
                configurationValueFunc(context, configNameGCEnabledInstructionSets, null, GCConfigurationType.Int64, (long)s_UpdatedGCEnabledInstructionSets);

            fixed (byte* configNameGCConserveMem = "GCConserveMem\0"u8)
            fixed (byte* publicKeyGCConserveMem = "System.GC.ConserveMemory\0"u8)
                configurationValueFunc(context, configNameGCConserveMem, publicKeyGCConserveMem, GCConfigurationType.Int64, (long)s_UpdatedGCConserveMem);

            fixed (byte* configNameGCWriteBarrier = "GCWriteBarrier\0"u8)
                configurationValueFunc(context, configNameGCWriteBarrier, null, GCConfigurationType.Int64, (long)s_UpdatedGCWriteBarrier);

            {
                byte* resultStr = null;
                fixed (byte* privateKey = "GCName\0"u8)
                fixed (byte* publicKey = "System.GC.Name\0"u8)
                    GCToEEInterface.GetStringConfigValue(privateKey, publicKey, &resultStr);
                fixed (byte* configName = "GCName\0"u8)
                fixed (byte* publicKey = "System.GC.Name\0"u8)
                    configurationValueFunc(context, configName, publicKey, GCConfigurationType.StringUtf8, (long)resultStr);
                GCToEEInterface.FreeStringConfigValue(resultStr);
            }

            {
                byte* resultStr = null;
                fixed (byte* privateKey = "GCPath\0"u8)
                fixed (byte* publicKey = "System.GC.Path\0"u8)
                    GCToEEInterface.GetStringConfigValue(privateKey, publicKey, &resultStr);
                fixed (byte* configName = "GCPath\0"u8)
                fixed (byte* publicKey = "System.GC.Path\0"u8)
                    configurationValueFunc(context, configName, publicKey, GCConfigurationType.StringUtf8, (long)resultStr);
                GCToEEInterface.FreeStringConfigValue(resultStr);
            }

            fixed (byte* configNameGCSpinCountUnit = "GCSpinCountUnit\0"u8)
                configurationValueFunc(context, configNameGCSpinCountUnit, null, GCConfigurationType.Int64, (long)s_UpdatedGCSpinCountUnit);

            fixed (byte* configNameGCDynamicAdaptationMode = "GCDynamicAdaptationMode\0"u8)
            fixed (byte* publicKeyGCDynamicAdaptationMode = "System.GC.DynamicAdaptationMode\0"u8)
                configurationValueFunc(context, configNameGCDynamicAdaptationMode, publicKeyGCDynamicAdaptationMode, GCConfigurationType.Int64, (long)s_UpdatedGCDynamicAdaptationMode);

            fixed (byte* configNameGCDTargetTCP = "GCDTargetTCP\0"u8)
            fixed (byte* publicKeyGCDTargetTCP = "System.GC.DTargetTCP\0"u8)
                configurationValueFunc(context, configNameGCDTargetTCP, publicKeyGCDTargetTCP, GCConfigurationType.Int64, (long)s_UpdatedGCDTargetTCP);

            fixed (byte* configNameGCDBGCRatio = "GCDBGCRatio\0"u8)
                configurationValueFunc(context, configNameGCDBGCRatio, null, GCConfigurationType.Int64, (long)s_UpdatedGCDBGCRatio);

            fixed (byte* configNameGCDGen0GrowthPercent = "GCDGen0GrowthPercent\0"u8)
            fixed (byte* publicKeyGCDGen0GrowthPercent = "System.GC.DGen0GrowthPercent\0"u8)
                configurationValueFunc(context, configNameGCDGen0GrowthPercent, publicKeyGCDGen0GrowthPercent, GCConfigurationType.Int64, (long)s_UpdatedGCDGen0GrowthPercent);

            fixed (byte* configNameGCDGen0GrowthMinFactor = "GCDGen0GrowthMinFactor\0"u8)
            fixed (byte* publicKeyGCDGen0GrowthMinFactor = "System.GC.DGen0GrowthMinFactor\0"u8)
                configurationValueFunc(context, configNameGCDGen0GrowthMinFactor, publicKeyGCDGen0GrowthMinFactor, GCConfigurationType.Int64, (long)s_UpdatedGCDGen0GrowthMinFactor);

            fixed (byte* configNameGCDGen0GrowthMaxFactor = "GCDGen0GrowthMaxFactor\0"u8)
            fixed (byte* publicKeyGCDGen0GrowthMaxFactor = "System.GC.DGen0GrowthMaxFactor\0"u8)
                configurationValueFunc(context, configNameGCDGen0GrowthMaxFactor, publicKeyGCDGen0GrowthMaxFactor, GCConfigurationType.Int64, (long)s_UpdatedGCDGen0GrowthMaxFactor);

            fixed (byte* configNameGCCacheSizeFromSysConf = "GCCacheSizeFromSysConf\0"u8)
                configurationValueFunc(context, configNameGCCacheSizeFromSysConf, null, GCConfigurationType.Boolean, (long)s_UpdatedGCCacheSizeFromSysConf);

        }
    }
}
