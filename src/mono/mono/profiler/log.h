#ifndef __MONO_PROFLOG_H__
#define __MONO_PROFLOG_H__

#include <glib.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/callspec.h>

#define BUF_ID 0x4D504C01
#define LOG_HEADER_ID 0x4D505A01
#define LOG_VERSION_MAJOR 3
#define LOG_VERSION_MINOR 0
#define LOG_DATA_VERSION 17

/*
 * Changes in major/minor versions:
 * version 1.0: removed sysid field from header
 *              added args, arch, os fields to header
 * version 3.0: added nanoseconds startup time to header
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
 * version 14: added event field to TYPE_MONITOR instead of encoding it in the extended info
               all TYPE_MONITOR events can now contain backtraces
               changed address field in TYPE_SAMPLE_UBIN to be based on ptr_base
               added an image pointer field to assembly load events
               added an exception object field to TYPE_CLAUSE
               class unload events no longer exist (they were never emitted)
               removed type field from TYPE_SAMPLE_HIT
               removed MONO_GC_EVENT_{MARK,RECLAIM}_{START,END}
               reverted the root_type field back to uleb128
               removed MONO_PROFILER_CODE_BUFFER_UNKNOWN (was never used)
               renumbered the MonoProfilerCodeBufferType enum
 * version 15: reverted the type, unit, and variance fields back to uleb128
               added TYPE_HEAP_ROOT_{REGISTER,UNREGISTER}
               TYPE_HEAP_ROOT now has a different, saner format
               added TYPE_VTABLE metadata load event
               changed TYPE_ALLOC and TYPE_HEAP_OBJECT to include a vtable pointer instead of a class pointer
               added MONO_ROOT_SOURCE_EPHEMERON
 * version 16: removed TYPE_COVERAGE
               added mvid to image load events
               added generation field to TYPE_HEAO_OBJECT
               added TYPE_AOT_ID
               removed TYPE_SAMPLE_UBIN
 * version 17: MONO_PROFILER_CODE_BUFFER_{METHOD_TRAMPOLINE,MONITOR} are no longer produced
 */

/*
 * file format:
 * [header] [buffer]*
 *
 * The file is composed by a header followed by 0 or more buffers.
 * Each buffer contains events that happened on a thread: for a given thread
 * buffers that appear later in the file are guaranteed to contain events
 * that happened later in time. Buffers from separate threads could be interleaved,
 * though.
 * Buffers are not required to be aligned.
 *
 * header format:
 * [id: 4 bytes] constant value: LOG_HEADER_ID
 * [major: 1 byte] major version of the log profiler
 * [minor: 1 byte] minor version of the log profiler
 * [format: 1 byte] version of the data format for the rest of the file
 * [ptrsize: 1 byte] size in bytes of a pointer in the profiled program
 * [startup time: 8 bytes] time in milliseconds since the unix epoch when the program started
 * [ns startup time: 8 bytes] time in nanoseconds since an unspecified epoch when the program started
 * [timer overhead: 4 bytes] approximate overhead in nanoseconds of the timer
 * [flags: 4 bytes] file format flags, should be 0 for now
 * [pid: 4 bytes] pid of the profiled process
 * [port: 2 bytes] tcp port for server if != 0
 * [args size: 4 bytes] size of args
 * [args: string] arguments passed to the profiler
 * [arch size: 4 bytes] size of arch
 * [arch: string] architecture the profiler is running on
 * [os size: 4 bytes] size of os
 * [os: string] operating system the profiler is running on
 *
 * The multiple byte integers are in little-endian format.
 *
 * buffer format:
 * [buffer header] [event]*
 * Buffers have a fixed-size header followed by 0 or more bytes of event data.
 * Timing information and other values in the event data are usually stored
 * as uleb128 or sleb128 integers. To save space, as noted for each item below,
 * some data is represented as a difference between the actual value and
 * either the last value of the same type (like for timing information) or
 * as the difference from a value stored in a buffer header.
 *
 * For timing information the data is stored as uleb128, since timing
 * increases in a monotonic way in each thread: the value is the number of
 * nanoseconds to add to the last seen timing data in a buffer. The first value
 * in a buffer will be calculated from the time_base field in the buffer head.
 *
 * Object or heap sizes are stored as uleb128.
 * Pointer differences are stored as sleb128, instead.
 *
 * If an unexpected value is found, the rest of the buffer should be ignored,
 * as generally the later values need the former to be interpreted correctly.
 *
 * buffer header format:
 * [bufid: 4 bytes] constant value: BUF_ID
 * [len: 4 bytes] size of the data following the buffer header
 * [time_base: 8 bytes] time base in nanoseconds since an unspecified epoch
 * [ptr_base: 8 bytes] base value for pointers
 * [obj_base: 8 bytes] base value for object addresses
 * [thread id: 8 bytes] system-specific thread ID (pthread_t for example)
 * [method_base: 8 bytes] base value for MonoMethod pointers
 *
 * event format:
 * [extended info: upper 4 bits] [type: lower 4 bits]
 * [time diff: uleb128] nanoseconds since last timing
 * [data]*
 * The data that follows depends on type and the extended info.
 * Type is one of the enum values in mono-profiler-log.h: TYPE_ALLOC, TYPE_GC,
 * TYPE_METADATA, TYPE_METHOD, TYPE_EXCEPTION, TYPE_MONITOR, TYPE_HEAP.
 * The extended info bits are interpreted based on type, see
 * each individual event description below.
 * strings are represented as a 0-terminated utf8 sequence.
 *
 * backtrace format:
 * [num: uleb128] number of frames following
 * [frame: sleb128]* mum MonoMethod* as a pointer difference from the last such
 * pointer or the buffer method_base
 *
 * type alloc format:
 * type: TYPE_ALLOC
 * exinfo: zero or TYPE_ALLOC_BT
 * [vtable: sleb128] MonoVTable* as a pointer difference from ptr_base
 * [obj: sleb128] object address as a byte difference from obj_base
 * [size: uleb128] size of the object in the heap
 * If exinfo == TYPE_ALLOC_BT, a backtrace follows.
 *
 * type GC format:
 * type: TYPE_GC
 * exinfo: one of TYPE_GC_EVENT, TYPE_GC_RESIZE, TYPE_GC_MOVE, TYPE_GC_HANDLE_CREATED[_BT],
 * TYPE_GC_HANDLE_DESTROYED[_BT], TYPE_GC_FINALIZE_START, TYPE_GC_FINALIZE_END,
 * TYPE_GC_FINALIZE_OBJECT_START, TYPE_GC_FINALIZE_OBJECT_END
 * if exinfo == TYPE_GC_RESIZE
 *	[heap_size: uleb128] new heap size
 * if exinfo == TYPE_GC_EVENT
 *	[event type: byte] GC event (MONO_GC_EVENT_* from profiler.h)
 *	[generation: byte] GC generation event refers to
 * if exinfo == TYPE_GC_MOVE
 *	[num_objects: uleb128] number of object moves that follow
 *	[objaddr: sleb128]+ num_objects object pointer differences from obj_base
 *	num is always an even number: the even items are the old
 *	addresses, the odd numbers are the respective new object addresses
 * if exinfo == TYPE_GC_HANDLE_CREATED[_BT]
 *	[handle_type: uleb128] MonoGCHandleType enum value
 *	upper bits reserved as flags
 *	[handle: uleb128] GC handle value
 *	[objaddr: sleb128] object pointer differences from obj_base
 * 	If exinfo == TYPE_GC_HANDLE_CREATED_BT, a backtrace follows.
 * if exinfo == TYPE_GC_HANDLE_DESTROYED[_BT]
 *	[handle_type: uleb128] MonoGCHandleType enum value
 *	upper bits reserved as flags
 *	[handle: uleb128] GC handle value
 * 	If exinfo == TYPE_GC_HANDLE_DESTROYED_BT, a backtrace follows.
 * if exinfo == TYPE_GC_FINALIZE_OBJECT_{START,END}
 * 	[object: sleb128] the object as a difference from obj_base
 *
 * type metadata format:
 * type: TYPE_METADATA
 * exinfo: one of: TYPE_END_LOAD, TYPE_END_UNLOAD (optional for TYPE_THREAD and TYPE_DOMAIN,
 * doesn't occur for TYPE_CLASS)
 * [mtype: byte] metadata type, one of: TYPE_CLASS, TYPE_IMAGE, TYPE_ASSEMBLY, TYPE_DOMAIN,
 * TYPE_THREAD, TYPE_CONTEXT, TYPE_VTABLE
 * [pointer: sleb128] pointer of the metadata type depending on mtype
 * if mtype == TYPE_CLASS
 *	[image: sleb128] MonoImage* as a pointer difference from ptr_base
 * 	[name: string] full class name
 * if mtype == TYPE_IMAGE
 * 	[name: string] image file name
 * 	[mvid: string] image mvid, can be empty for dynamic images
 * if mtype == TYPE_ASSEMBLY
 *	[image: sleb128] MonoImage* as a pointer difference from ptr_base
 * 	[name: string] assembly name
 * if mtype == TYPE_DOMAIN && exinfo == 0
 * 	[name: string] domain friendly name
 * if mtype == TYPE_CONTEXT
 * 	[domain: sleb128] domain id as pointer difference from ptr_base
 * if mtype == TYPE_THREAD && exinfo == 0
 * 	[name: string] thread name
 * if mtype == TYPE_VTABLE
 * 	[domain: sleb128] domain id as pointer difference from ptr_base, can be zero for proxy VTables
 * 	[class: sleb128] MonoClass* as a pointer difference from ptr_base
 *
 * type method format:
 * type: TYPE_METHOD
 * exinfo: one of: TYPE_LEAVE, TYPE_ENTER, TYPE_EXC_LEAVE, TYPE_JIT
 * [method: sleb128] MonoMethod* as a pointer difference from the last such
 * pointer or the buffer method_base
 * if exinfo == TYPE_JIT
 *	[code address: sleb128] pointer to the native code as a diff from ptr_base
 *	[code size: uleb128] size of the generated code
 *	[name: string] full method name
 *
 * type exception format:
 * type: TYPE_EXCEPTION
 * exinfo: zero, TYPE_CLAUSE, or TYPE_THROW_BT
 * if exinfo == TYPE_CLAUSE
 * 	[clause type: byte] MonoExceptionEnum enum value
 * 	[clause index: uleb128] index of the current clause
 * 	[method: sleb128] MonoMethod* as a pointer difference from the last such
 * 	pointer or the buffer method_base
 * 	[object: sleb128] the exception object as a difference from obj_base
 * else
 * 	[object: sleb128] the exception object as a difference from obj_base
 * 	If exinfo == TYPE_THROW_BT, a backtrace follows.
 *
 * type runtime format:
 * type: TYPE_RUNTIME
 * exinfo: one of: TYPE_JITHELPER
 * if exinfo == TYPE_JITHELPER
 *	[type: byte] MonoProfilerCodeBufferType enum value
 *	[buffer address: sleb128] pointer to the native code as a diff from ptr_base
 *	[buffer size: uleb128] size of the generated code
 *	if type == MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE
 *		[name: string] buffer description name
 *
 * type monitor format:
 * type: TYPE_MONITOR
 * exinfo: zero or TYPE_MONITOR_BT
 * [type: byte] MonoProfilerMonitorEvent enum value
 * [object: sleb128] the lock object as a difference from obj_base
 * If exinfo == TYPE_MONITOR_BT, a backtrace follows.
 *
 * type heap format
 * type: TYPE_HEAP
 * exinfo: one of TYPE_HEAP_START, TYPE_HEAP_END, TYPE_HEAP_OBJECT, TYPE_HEAP_ROOT, TYPE_HEAP_ROOT_REGISTER, TYPE_HEAP_ROOT_UNREGISTER
 * if exinfo == TYPE_HEAP_OBJECT
 * 	[object: sleb128] the object as a difference from obj_base
 * 	[vtable: sleb128] MonoVTable* as a pointer difference from ptr_base
 * 	[size: uleb128] size of the object on the heap
 * 	[generation: byte] generation the object is in
 * 	[num_refs: uleb128] number of object references
 * 	each referenced objref is preceded by a uleb128 encoded offset: the
 * 	first offset is from the object address and each next offset is relative
 * 	to the previous one
 * 	[objrefs: sleb128]+ object referenced as a difference from obj_base
 * 	The same object can appear multiple times, but only the first time
 * 	with size != 0: in the other cases this data will only be used to
 * 	provide additional referenced objects.
 * if exinfo == TYPE_HEAP_ROOT
 * 	[num_roots: uleb128] number of root references
 * 	for i = 0 to num_roots
 * 		[address: sleb128] the root address as a difference from ptr_base
 * 		[object: sleb128] the object address as a difference from obj_base
 * if exinfo == TYPE_HEAP_ROOT_REGISTER
 * 	[start: sleb128] start address as a difference from ptr_base
 * 	[size: uleb] size of the root region
 * 	[source: byte] MonoGCRootSource enum value
 * 	[key: sleb128] root key, meaning dependent on type, value as a difference from ptr_base
 * 	[desc: string] description of the root
 * if exinfo == TYPE_HEAP_ROOT_UNREGISTER
 * 	[start: sleb128] start address as a difference from ptr_base
 *
 * type sample format
 * type: TYPE_SAMPLE
 * exinfo: one of TYPE_SAMPLE_HIT, TYPE_SAMPLE_USYM, TYPE_SAMPLE_COUNTERS_DESC, TYPE_SAMPLE_COUNTERS
 * if exinfo == TYPE_SAMPLE_HIT
 * 	[thread: sleb128] thread id as difference from ptr_base
 * 	[count: uleb128] number of following instruction addresses
 * 	[ip: sleb128]* instruction pointer as difference from ptr_base
 *	[mbt_count: uleb128] number of managed backtrace frames
 *	[method: sleb128]* MonoMethod* as a pointer difference from the last such
 * 	pointer or the buffer method_base (the first such method can be also indentified by ip, but this is not necessarily true)
 * if exinfo == TYPE_SAMPLE_USYM
 * 	[address: sleb128] symbol address as a difference from ptr_base
 * 	[size: uleb128] symbol size (may be 0 if unknown)
 * 	[name: string] symbol name
 * if exinfo == TYPE_SAMPLE_COUNTERS_DESC
 * 	[len: uleb128] number of counters
 * 	for i = 0 to len
 * 		[section: uleb128] section of counter
 * 		if section == MONO_COUNTER_PERFCOUNTERS:
 * 			[section_name: string] section name of counter
 * 		[name: string] name of counter
 * 		[type: uleb128] type of counter
 * 		[unit: uleb128] unit of counter
 * 		[variance: uleb128] variance of counter
 * 		[index: uleb128] unique index of counter
 * if exinfo == TYPE_SAMPLE_COUNTERS
 * 	while true:
 * 		[index: uleb128] unique index of counter
 * 		if index == 0:
 * 			break
 * 		[type: uleb128] type of counter value
 * 		if type == string:
 * 			if value == null:
 * 				[0: byte] 0 -> value is null
 * 			else:
 * 				[1: byte] 1 -> value is not null
 * 				[value: string] counter value
 * 		else:
 * 			[value: uleb128/sleb128/double] counter value, can be sleb128, uleb128 or double (determined by using type)
 *
 * type meta format:
 * type: TYPE_META
 * exinfo: one of: TYPE_SYNC_POINT, TYPE_AOT_ID
 * if exinfo == TYPE_SYNC_POINT
 *	[type: byte] MonoProfilerSyncPointType enum value
 * if exinfo == TYPE_AOT_ID
 * 	[aot id: string] current runtime's AOT ID
 */

enum {
	TYPE_ALLOC = 0,
	TYPE_GC = 1,
	TYPE_METADATA = 2,
	TYPE_METHOD = 3,
	TYPE_EXCEPTION = 4,
	TYPE_MONITOR = 5,
	TYPE_HEAP = 6,
	TYPE_SAMPLE = 7,
	TYPE_RUNTIME = 8,
	TYPE_META = 10,
	/* extended type for TYPE_HEAP */
	TYPE_HEAP_START  = 0 << 4,
	TYPE_HEAP_END    = 1 << 4,
	TYPE_HEAP_OBJECT = 2 << 4,
	TYPE_HEAP_ROOT   = 3 << 4,
	TYPE_HEAP_ROOT_REGISTER = 4 << 4,
	TYPE_HEAP_ROOT_UNREGISTER = 5 << 4,
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
	TYPE_SAMPLE_COUNTERS_DESC = 3 << 4,
	TYPE_SAMPLE_COUNTERS      = 4 << 4,
	/* extended type for TYPE_RUNTIME */
	TYPE_JITHELPER = 1 << 4,
	/* extended type for TYPE_META */
	TYPE_SYNC_POINT = 0 << 4,
	TYPE_AOT_ID     = 1 << 4,
};

enum {
	/* metadata type byte for TYPE_METADATA */
	TYPE_CLASS    = 1,
	TYPE_IMAGE    = 2,
	TYPE_ASSEMBLY = 3,
	TYPE_DOMAIN   = 4,
	TYPE_THREAD   = 5,
	TYPE_CONTEXT  = 6,
	TYPE_VTABLE   = 7,
};

typedef enum {
	SYNC_POINT_PERIODIC = 0,
	SYNC_POINT_WORLD_STOP = 1,
	SYNC_POINT_WORLD_START = 2,
} MonoProfilerSyncPointType;

typedef enum {
	MONO_PROFILER_MONITOR_CONTENTION = 1,
	MONO_PROFILER_MONITOR_DONE = 2,
	MONO_PROFILER_MONITOR_FAIL = 3,
} MonoProfilerMonitorEvent;

enum {
	MONO_PROFILER_GC_HANDLE_CREATED = 0,
	MONO_PROFILER_GC_HANDLE_DESTROYED = 1,
};

typedef enum {
	MONO_PROFILER_HEAPSHOT_NONE = 0,
	MONO_PROFILER_HEAPSHOT_MAJOR = 1,
	MONO_PROFILER_HEAPSHOT_ON_DEMAND = 2,
	MONO_PROFILER_HEAPSHOT_X_GC = 3,
	MONO_PROFILER_HEAPSHOT_X_MS = 4,
} MonoProfilerHeapshotMode;

// If you alter MAX_FRAMES, you may need to alter SAMPLE_BLOCK_SIZE too.
#define MAX_FRAMES 32

//The following flags control emitting individual events
#define PROFLOG_EXCEPTION_EVENTS (1 << 0)
#define PROFLOG_MONITOR_EVENTS (1 << 1)
#define PROFLOG_GC_EVENTS (1 << 2)
#define PROFLOG_GC_ALLOCATION_EVENTS (1 << 3)
#define PROFLOG_GC_MOVE_EVENTS (1 << 4)
#define PROFLOG_GC_ROOT_EVENTS (1 << 5)
#define PROFLOG_GC_HANDLE_EVENTS (1 << 6)
#define PROFLOG_GC_FINALIZATION_EVENTS (1 << 7)
#define PROFLOG_COUNTER_EVENTS (1 << 8)
#define PROFLOG_SAMPLE_EVENTS (1 << 9)
#define PROFLOG_JIT_EVENTS (1 << 10)

#define PROFLOG_ALLOC_ALIAS (PROFLOG_GC_EVENTS | PROFLOG_GC_ALLOCATION_EVENTS | PROFLOG_GC_MOVE_EVENTS)
#define PROFLOG_HEAPSHOT_ALIAS (PROFLOG_GC_EVENTS | PROFLOG_GC_ROOT_EVENTS)
#define PROFLOG_LEGACY_ALIAS (PROFLOG_EXCEPTION_EVENTS | PROFLOG_MONITOR_EVENTS | PROFLOG_GC_EVENTS | PROFLOG_GC_MOVE_EVENTS | PROFLOG_GC_ROOT_EVENTS | PROFLOG_GC_HANDLE_EVENTS | PROFLOG_GC_FINALIZATION_EVENTS | PROFLOG_COUNTER_EVENTS)

typedef struct {
	//Events explicitly enabled
	int enable_mask;

	//Events explicitly disabled
	int disable_mask;

	// Actual mask the profiler should use. Can be changed at runtime.
	int effective_mask;

	// Whether to do method prologue/epilogue instrumentation. Only used at startup.
	gboolean enter_leave;

	//Emit a report at the end of execution
	gboolean do_report;

	//Enable profiler internal debugging
	gboolean do_debug;

	//Where to compress the output file
	gboolean use_zip;

	// Heapshot mode (every major, on demand, XXgc, XXms). Can be changed at runtime.
	MonoProfilerHeapshotMode hs_mode;

	// Heapshot frequency in milliseconds (for MONO_HEAPSHOT_X_MS). Can be changed at runtime.
	unsigned int hs_freq_ms;

	// Heapshot frequency in number of collections (for MONO_HEAPSHOT_X_GC). Can be changed at runtime.
	unsigned int hs_freq_gc;

	// Should root reports be done even outside of heapshots?
	gboolean always_do_root_report;

	// Whether to do a heapshot on shutdown.
	gboolean hs_on_shutdown;

	// Sample frequency in Hertz. Only used at startup.
	int sample_freq;

	// Maximum number of frames to collect. Can be changed at runtime.
	int num_frames;

	// Max depth to record enter/leave events. Can be changed at runtime.
	int max_call_depth;

	//Name of the generated mlpd file
	const char *output_filename;

	// Port to listen for profiling commands (e.g. "heapshot" for on-demand heapshot).
	int command_port;

	// Maximum number of SampleHit structures. We'll drop samples if this number is not sufficient.
	int max_allocated_sample_hits;

	// Sample mode. Only used at startup.
	MonoProfilerSampleMode sampling_mode;

	// Callspec config - which methods are to be instrumented
	MonoCallSpec callspec;
} ProfilerConfig;

void proflog_parse_args (ProfilerConfig *config, const char *desc);

#endif /* __MONO_PROFLOG_H__ */
