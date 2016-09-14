#ifndef __MONO_PROFLOG_H__
#define __MONO_PROFLOG_H__

#define BUF_ID 0x4D504C01
#define LOG_HEADER_ID 0x4D505A01
#define LOG_VERSION_MAJOR 1
#define LOG_VERSION_MINOR 1
#define LOG_DATA_VERSION 13

/*
 * Changes in major/minor versions:
 * version 1.0: removed sysid field from header
 *              added args, arch, os fields to header
 *
 * Changes in data versions:
 * version 2: added offsets in heap walk
 * version 3: added GC roots
 * version 4: added sample/statistical profiling
 * version 5: added counters sampling
 * version 6: added optional backtrace in sampling info
 * version 8: added TYPE_RUNTIME and JIT helpers/trampolines
 * version 9: added MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING
 * version 10: added TYPE_COVERAGE
 * version 11: added thread ID to TYPE_SAMPLE_HIT
               added more load/unload events
                   unload for class
                   unload for image
                   load/unload for appdomain
                   load/unload for contexts
                   load/unload/name for assemblies
               removed TYPE_LOAD_ERR flag (profiler never generated it, now removed from the format itself)
               added TYPE_GC_HANDLE_{CREATED,DESTROYED}_BT
               TYPE_JIT events are no longer guaranteed to have code start/size info (can be zero)
 * version 12: added MONO_COUNTER_PROFILER
 * version 13: added MONO_GC_EVENT_{PRE_STOP_WORLD_LOCKED,POST_START_WORLD_UNLOCKED}
               added TYPE_META + TYPE_SYNC_POINT
               removed il and native offset in TYPE_SAMPLE_HIT
               methods in backtraces are now encoded as proper method pointers
               removed flags in backtrace format
               removed flags in metadata events
               changed the following fields to a single byte rather than leb128
                   TYPE_GC_EVENT: event_type, generation
                   TYPE_HEAP_ROOT: root_type
                   TYPE_JITHELPER: type
                   TYPE_SAMPLE_HIT: sample_type
                   TYPE_CLAUSE: clause_type
                   TYPE_SAMPLE_COUNTERS_DESC: type, unit, variance
                   TYPE_SAMPLE_COUNTERS: type
               added time fields to all events that were missing one
                   TYPE_HEAP_OBJECT
                   TYPE_HEAP_ROOT
                   TYPE_SAMPLE_USYM
                   TYPE_SAMPLE_COUNTERS_DESC
                   TYPE_COVERAGE_METHOD
                   TYPE_COVERAGE_STATEMENT
                   TYPE_COVERAGE_CLASS
                   TYPE_COVERAGE_ASSEMBLY
               moved the time field in TYPE_SAMPLE_HIT to right after the event byte, now encoded as a regular time field
               changed the time field in TYPE_SAMPLE_COUNTERS to be encoded as a regular time field (in nanoseconds)
               added TYPE_GC_FINALIZE_{START,END,OBJECT_START,OBJECT_END}
 */

enum {
	TYPE_ALLOC,
	TYPE_GC,
	TYPE_METADATA,
	TYPE_METHOD,
	TYPE_EXCEPTION,
	TYPE_MONITOR,
	TYPE_HEAP,
	TYPE_SAMPLE,
	TYPE_RUNTIME,
	TYPE_COVERAGE,
	TYPE_META,
	/* extended type for TYPE_HEAP */
	TYPE_HEAP_START  = 0 << 4,
	TYPE_HEAP_END    = 1 << 4,
	TYPE_HEAP_OBJECT = 2 << 4,
	TYPE_HEAP_ROOT   = 3 << 4,
	/* extended type for TYPE_METADATA */
	TYPE_END_LOAD     = 2 << 4,
	TYPE_END_UNLOAD   = 4 << 4,
	/* extended type for TYPE_GC */
	TYPE_GC_EVENT  = 1 << 4,
	TYPE_GC_RESIZE = 2 << 4,
	TYPE_GC_MOVE   = 3 << 4,
	TYPE_GC_HANDLE_CREATED      = 4 << 4,
	TYPE_GC_HANDLE_DESTROYED    = 5 << 4,
	TYPE_GC_HANDLE_CREATED_BT   = 6 << 4,
	TYPE_GC_HANDLE_DESTROYED_BT = 7 << 4,
	TYPE_GC_FINALIZE_START = 8 << 4,
	TYPE_GC_FINALIZE_END = 9 << 4,
	TYPE_GC_FINALIZE_OBJECT_START = 10 << 4,
	TYPE_GC_FINALIZE_OBJECT_END = 11 << 4,
	/* extended type for TYPE_METHOD */
	TYPE_LEAVE     = 1 << 4,
	TYPE_ENTER     = 2 << 4,
	TYPE_EXC_LEAVE = 3 << 4,
	TYPE_JIT       = 4 << 4,
	/* extended type for TYPE_EXCEPTION */
	TYPE_THROW_NO_BT = 0 << 7,
	TYPE_THROW_BT    = 1 << 7,
	TYPE_CLAUSE      = 1 << 4,
	/* extended type for TYPE_ALLOC */
	TYPE_ALLOC_NO_BT  = 0 << 4,
	TYPE_ALLOC_BT     = 1 << 4,
	/* extended type for TYPE_MONITOR */
	TYPE_MONITOR_NO_BT  = 0 << 7,
	TYPE_MONITOR_BT     = 1 << 7,
	/* extended type for TYPE_SAMPLE */
	TYPE_SAMPLE_HIT           = 0 << 4,
	TYPE_SAMPLE_USYM          = 1 << 4,
	TYPE_SAMPLE_UBIN          = 2 << 4,
	TYPE_SAMPLE_COUNTERS_DESC = 3 << 4,
	TYPE_SAMPLE_COUNTERS      = 4 << 4,
	/* extended type for TYPE_RUNTIME */
	TYPE_JITHELPER = 1 << 4,
	/* extended type for TYPE_COVERAGE */
	TYPE_COVERAGE_ASSEMBLY = 0 << 4,
	TYPE_COVERAGE_METHOD   = 1 << 4,
	TYPE_COVERAGE_STATEMENT = 2 << 4,
	TYPE_COVERAGE_CLASS = 3 << 4,
	/* extended type for TYPE_META */
	TYPE_SYNC_POINT = 0 << 4,
	TYPE_END
};

enum {
	/* metadata type byte for TYPE_METADATA */
	TYPE_CLASS    = 1,
	TYPE_IMAGE    = 2,
	TYPE_ASSEMBLY = 3,
	TYPE_DOMAIN   = 4,
	TYPE_THREAD   = 5,
	TYPE_CONTEXT  = 6,
};

typedef enum {
	SYNC_POINT_PERIODIC,
	SYNC_POINT_WORLD_STOP,
	SYNC_POINT_WORLD_START
} MonoProfilerSyncPointType;

// Sampling sources
// Unless you have compiled with --enable-perf-events, only SAMPLE_CYCLES is available
enum {
	SAMPLE_CYCLES = 1,
	SAMPLE_INSTRUCTIONS,
	SAMPLE_CACHE_MISSES,
	SAMPLE_CACHE_REFS,
	SAMPLE_BRANCHES,
	SAMPLE_BRANCH_MISSES,
	SAMPLE_LAST
};

#endif /* __MONO_PROFLOG_H__ */
