// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __GCCONFIG_H__
#define __GCCONFIG_H__

// gcconfig.h - GC configuration management and retrieval.
//
// This file and the GCConfig class are designed to be the primary entry point
// for querying configuration information from within the GC.

// GCConfigStringHolder is a wrapper around a configuration string obtained
// from the EE. Such strings must be disposed using GCToEEInterface::FreeStringConfigValue,
// so this class ensures that is done correctly.
//
// The name is unfortunately a little long, but "ConfigStringHolder" is already taken by the
// EE's config mechanism.
class GCConfigStringHolder
{
private:
    const char* m_str;

public:
    // Constructs a new GCConfigStringHolder around a string obtained from
    // GCToEEInterface::GetStringConfigValue.
    explicit GCConfigStringHolder(const char* str)
      : m_str(str) {}

    // No copy operators - this type cannot be copied.
    GCConfigStringHolder(const GCConfigStringHolder&) = delete;
    GCConfigStringHolder& operator=(const GCConfigStringHolder&) = delete;

    // This type is returned by-value by string config functions, so it
    // requires a move constructor.
    GCConfigStringHolder(GCConfigStringHolder&&) = default;

    // Frees a string config value by delegating to GCToEEInterface::FreeStringConfigValue.
    ~GCConfigStringHolder()
    {
        if (m_str)
        {
            GCToEEInterface::FreeStringConfigValue(m_str);
        }

        m_str = nullptr;
    }

    // Retrieves the wrapped config string.
    const char* Get() const { return m_str; }
};

// Note that the configs starting BGCFLTuningEnabled ending BGCG2RatioStep are for BGC servo
// tuning which is currently an experimental feature which is why I'm not putting any of these
// in clrconfigvalues.h (yet).
// The only public facing configs are BGCFLTuningEnabled and BGCMemGoal.
// Currently the set point is the BGCMemGoal you specify, which indicates what physical memory
// load you'd like GC to maintain. And BGCFLTuningEnabled is to enable/disable this tuning.
//
// The value for BGCG2RatioStep, BGCFLkd and BGCFLSmoothFactor and BGCFLff will be divided by 100.
// The value for BGCMLkp and BGCMLki will be divided by 1000.
// The value for BGCFLkp and BGCFLki will be divided by 1000000.

// Each one of these keys produces a method on GCConfig with the name "Get{name}", where {name}
// is the first parameter of the *_CONFIG macros below.
#define GC_CONFIGURATION_KEYS \
    BOOL_CONFIG(ServerGC,     "gcServer",     false, "Whether we should be using Server GC")     \
    BOOL_CONFIG(ConcurrentGC, "gcConcurrent", true,  "Whether we should be using Concurrent GC") \
    BOOL_CONFIG(ConservativeGC, "gcConservative", false, "Enables/Disables conservative GC")     \
    BOOL_CONFIG(ForceCompact, "gcForceCompact", false,                                           \
        "When set to true, always do compacting GC")                                             \
    BOOL_CONFIG(RetainVM,     "GCRetainVM",   false,                                             \
        "When set we put the segments that should be deleted on a standby list (instead of "     \
        "releasing them back to the OS) which will be considered to satisfy new segment requests"\
        " (note that the same thing can be specified via API which is the supported way)")        \
    BOOL_CONFIG(StressMix,    "GCStressMix",  false,                                             \
        "Specifies whether the GC mix mode is enabled or not")                                   \
    BOOL_CONFIG(BreakOnOOM,   "GCBreakOnOOM", false,                                             \
        "Does a DebugBreak at the soonest time we detect an OOM")                                \
    BOOL_CONFIG(NoAffinitize, "GCNoAffinitize", false,                                           \
        "If set, do not affinitize server GC threads")                                           \
    BOOL_CONFIG(LogEnabled,   "GCLogEnabled", false,                                             \
        "Specifies if you want to turn on logging in GC")                                        \
    BOOL_CONFIG(ConfigLogEnabled, "GCConfigLogEnabled", false,                                   \
        "Specifies the name of the GC config log file")                                          \
    BOOL_CONFIG(GCNumaAware,   "GCNumaAware", true, "Enables numa allocations in the GC")        \
    BOOL_CONFIG(GCCpuGroup,    "GCCpuGroup", false, "Enables CPU groups in the GC")              \
    BOOL_CONFIG(GCLargePages,  "GCLargePages", false, "Enables using Large Pages in the GC")     \
    INT_CONFIG(HeapVerifyLevel, "HeapVerify", HEAPVERIFY_NONE,                                   \
        "When set verifies the integrity of the managed heap on entry and exit of each GC")      \
    INT_CONFIG(LOHCompactionMode, "GCLOHCompact", 0, "Specifies the LOH compaction mode")        \
    INT_CONFIG(LOHThreshold, "GCLOHThreshold", LARGE_OBJECT_SIZE,                                \
        "Specifies the size that will make objects go on LOH")                                   \
    INT_CONFIG(BGCSpinCount,  "BGCSpinCount", 140, "Specifies the bgc spin count")               \
    INT_CONFIG(BGCSpin,       "BGCSpin",      2,   "Specifies the bgc spin time")                \
    INT_CONFIG(HeapCount,     "GCHeapCount",  0,   "Specifies the number of server GC heaps")    \
    INT_CONFIG(Gen0Size,      "GCgen0size",   0, "Specifies the smallest gen0 size")             \
    INT_CONFIG(SegmentSize,   "GCSegmentSize", 0, "Specifies the managed heap segment size")     \
    INT_CONFIG(LatencyMode,   "GCLatencyMode", -1,                                               \
        "Specifies the GC latency mode - batch, interactive or low latency (note that the same " \
        "thing can be specified via API which is the supported way")                             \
    INT_CONFIG(LatencyLevel,  "GCLatencyLevel", 1,                                               \
        "Specifies the GC latency level that you want to optimize for. Must be a number from 0"  \
        "3. See documentation for more details on each level.")                                  \
    INT_CONFIG(LogFileSize,   "GCLogFileSize", 0, "Specifies the GC log file size")              \
    INT_CONFIG(CompactRatio,  "GCCompactRatio", 0,                                               \
        "Specifies the ratio compacting GCs vs sweeping")                                        \
    INT_CONFIG(GCHeapAffinitizeMask, "GCHeapAffinitizeMask", 0,                                  \
        "Specifies processor mask for Server GC threads")                                        \
    STRING_CONFIG(GCHeapAffinitizeRanges, "GCHeapAffinitizeRanges",                              \
        "Specifies list of processors for Server GC threads. The format is a comma separated "   \
        "list of processor numbers or ranges of processor numbers. On Windows, each entry is "   \
        "prefixed by the CPU group number. Example: Unix - 1,3,5,7-9,12, Windows - 0:1,1:7-9")   \
    INT_CONFIG(GCHighMemPercent, "GCHighMemPercent", 0,                                          \
        "The percent for GC to consider as high memory")                                         \
    INT_CONFIG(GCProvModeStress, "GCProvModeStress", 0,                                          \
        "Stress the provisional modes")                                                          \
    INT_CONFIG(GCGen0MaxBudget, "GCGen0MaxBudget", 0,                                            \
        "Specifies the largest gen0 allocation budget")                                          \
    INT_CONFIG(GCHeapHardLimit, "GCHeapHardLimit", 0,                                            \
        "Specifies a hard limit for the GC heap")                                                \
    INT_CONFIG(GCHeapHardLimitPercent, "GCHeapHardLimitPercent", 0,                              \
        "Specifies the GC heap usage as a percentage of the total memory")                       \
    STRING_CONFIG(LogFile,    "GCLogFile",    "Specifies the name of the GC log file")           \
    STRING_CONFIG(ConfigLogFile, "GCConfigLogFile",                                              \
        "Specifies the name of the GC config log file")                                          \
    STRING_CONFIG(MixLogFile, "GCMixLog",                                                        \
        "Specifies the name of the log file for GC mix statistics")                              \
    INT_CONFIG(BGCFLTuningEnabled, "BGCFLTuningEnabled", 0, "Enables FL tuning")                 \
    INT_CONFIG(BGCMemGoal, "BGCMemGoal", 75, "Specifies the physical memory load goal")          \
    INT_CONFIG(BGCMemGoalSlack, "BGCMemGoalSlack", 10,                                           \
        "Specifies comfort zone of going above goal")                                            \
    INT_CONFIG(BGCFLSweepGoal, "BGCFLSweepGoal", 0,                                              \
        "Specifies the gen2 sweep FL ratio goal")                                                \
    INT_CONFIG(BGCFLSweepGoalLOH, "BGCFLSweepGoalLOH", 0,                                        \
        "Specifies the LOH sweep FL ratio goal")                                                 \
    INT_CONFIG(BGCFLkp, "BGCFLkp", 6000, "Specifies kp for above goal tuning")                   \
    INT_CONFIG(BGCFLki, "BGCFLki", 1000, "Specifies ki for above goal tuning")                   \
    INT_CONFIG(BGCFLkd, "BGCFLkd", 11, "Specifies kd for above goal tuning")                     \
    INT_CONFIG(BGCFLff, "BGCFLff", 100, "Specifies ff ratio")                                    \
    INT_CONFIG(BGCFLSmoothFactor, "BGCFLSmoothFactor", 150, "Smoothing over these")              \
    INT_CONFIG(BGCFLGradualD, "BGCFLGradualD", 0,                                                \
        "Enable gradual D instead of cutting of at the value")                                   \
    INT_CONFIG(BGCMLkp, "BGCMLkp", 1000, "Specifies kp for ML tuning")                           \
    INT_CONFIG(BGCMLki, "BGCMLki", 16, "Specifies ki for ML tuning")                          \
    INT_CONFIG(BGCFLEnableKi, "BGCFLEnableKi", 1, "Enables ki for above goal tuning")            \
    INT_CONFIG(BGCFLEnableKd, "BGCFLEnableKd", 0, "Enables kd for above goal tuning")            \
    INT_CONFIG(BGCFLEnableSmooth, "BGCFLEnableSmooth", 0, "Enables smoothing")                   \
    INT_CONFIG(BGCFLEnableTBH, "BGCFLEnableTBH", 0, "Enables TBH")                               \
    INT_CONFIG(BGCFLEnableFF, "BGCFLEnableFF", 0, "Enables FF")                                  \
    INT_CONFIG(BGCG2RatioStep, "BGCG2RatioStep", 5, "Ratio correction factor for ML loop")

// This class is responsible for retreiving configuration information
// for how the GC should operate.
class GCConfig
{
#define BOOL_CONFIG(name, unused_key, unused_default, unused_doc) \
  public: static bool Get##name();                                \
  private: static bool s_##name;
#define INT_CONFIG(name, unused_key, unused_default, unused_doc) \
  public: static int64_t Get##name();                            \
  private: static int64_t s_##name;
#define STRING_CONFIG(name, unused_key, unused_doc) \
  public: static GCConfigStringHolder Get##name();
GC_CONFIGURATION_KEYS
#undef BOOL_CONFIG
#undef INT_CONFIG
#undef STRING_CONFIG

public:
// Flags that may inhabit the number returned for the HeapVerifyLevel config option.
// Keep this in sync with vm\eeconfig.h if this ever changes.
enum HeapVerifyFlags {
    HEAPVERIFY_NONE             = 0,
    HEAPVERIFY_GC               = 1,   // Verify the heap at beginning and end of GC
    HEAPVERIFY_BARRIERCHECK     = 2,   // Verify the brick table
    HEAPVERIFY_SYNCBLK          = 4,   // Verify sync block scanning

    // the following options can be used to mitigate some of the overhead introduced
    // by heap verification.  some options might cause heap verifiction to be less
    // effective depending on the scenario.

    HEAPVERIFY_NO_RANGE_CHECKS  = 0x10,   // Excludes checking if an OBJECTREF is within the bounds of the managed heap
    HEAPVERIFY_NO_MEM_FILL      = 0x20,   // Excludes filling unused segment portions with fill pattern
    HEAPVERIFY_POST_GC_ONLY     = 0x40,   // Performs heap verification post-GCs only (instead of before and after each GC)
    HEAPVERIFY_DEEP_ON_COMPACT  = 0x80    // Performs deep object verfication only on compacting GCs.
};

// Initializes the GCConfig subsystem. Must be called before accessing any
// configuration information.
static void Initialize();

};

bool ParseGCHeapAffinitizeRanges(const char* cpu_index_ranges, AffinitySet* config_affinity_set);

#endif // __GCCONFIG_H__
