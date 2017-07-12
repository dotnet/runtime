/*
 * mono-profiler-log.c: mono log profiler
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Alex Rønne Petersen (alexrp@xamarin.com)
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>
#include "../metadata/metadata-internals.h"
#include <mono/metadata/mono-config.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/mono-perfcounters.h>
#include <mono/utils/atomic.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/lock-free-alloc.h>
#include <mono/utils/lock-free-queue.h>
#include <mono/utils/mono-conc-hashtable.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-linked-list-set.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-os-semaphore.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-api.h>
#include "log.h"

#ifdef HAVE_DLFCN_H
#include <dlfcn.h>
#endif
#include <fcntl.h>
#ifdef HAVE_LINK_H
#include <link.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#if defined(__APPLE__)
#include <mach/mach_time.h>
#endif
#include <netinet/in.h>
#ifdef HAVE_SYS_MMAN_H
#include <sys/mman.h>
#endif
#include <sys/socket.h>
#if defined (HAVE_SYS_ZLIB)
#include <zlib.h>
#endif

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
 * [major: 1 byte] [minor: 1 byte] major and minor version of the log profiler
 * [format: 1 byte] version of the data format for the rest of the file
 * [ptrsize: 1 byte] size in bytes of a pointer in the profiled program
 * [startup time: 8 bytes] time in milliseconds since the unix epoch when the program started
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
 * [ptr: sleb128] class as a byte difference from ptr_base
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
 * TYPE_THREAD, TYPE_CONTEXT
 * [pointer: sleb128] pointer of the metadata type depending on mtype
 * if mtype == TYPE_CLASS
 *	[image: sleb128] MonoImage* as a pointer difference from ptr_base
 * 	[name: string] full class name
 * if mtype == TYPE_IMAGE
 * 	[name: string] image file name
 * if mtype == TYPE_ASSEMBLY
 *	[image: sleb128] MonoImage* as a pointer difference from ptr_base
 * 	[name: string] assembly name
 * if mtype == TYPE_DOMAIN && exinfo == 0
 * 	[name: string] domain friendly name
 * if mtype == TYPE_CONTEXT
 * 	[domain: sleb128] domain id as pointer
 * if mtype == TYPE_THREAD && exinfo == 0
 * 	[name: string] thread name
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
 * exinfo: one of TYPE_HEAP_START, TYPE_HEAP_END, TYPE_HEAP_OBJECT, TYPE_HEAP_ROOT
 * if exinfo == TYPE_HEAP_OBJECT
 * 	[object: sleb128] the object as a difference from obj_base
 * 	[class: sleb128] the object MonoClass* as a difference from ptr_base
 * 	[size: uleb128] size of the object on the heap
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
 * 	[num_gc: uleb128] number of major gcs
 * 	[object: sleb128] the object as a difference from obj_base
 * 	[root_type: byte] the root_type: MonoProfileGCRootType (profiler.h)
 * 	[extra_info: uleb128] the extra_info value
 * 	object, root_type and extra_info are repeated num_roots times
 *
 * type sample format
 * type: TYPE_SAMPLE
 * exinfo: one of TYPE_SAMPLE_HIT, TYPE_SAMPLE_USYM, TYPE_SAMPLE_UBIN, TYPE_SAMPLE_COUNTERS_DESC, TYPE_SAMPLE_COUNTERS
 * if exinfo == TYPE_SAMPLE_HIT
 * 	[thread: sleb128] thread id as difference from ptr_base
 * 	[count: uleb128] number of following instruction addresses
 * 	[ip: sleb128]* instruction pointer as difference from ptr_base
 *	[mbt_count: uleb128] number of managed backtrace frames
 *	[method: sleb128]* MonoMethod* as a pointer difference from the last such
 * 	pointer or the buffer method_base (the first such method can be also indentified by ip, but this is not neccessarily true)
 * if exinfo == TYPE_SAMPLE_USYM
 * 	[address: sleb128] symbol address as a difference from ptr_base
 * 	[size: uleb128] symbol size (may be 0 if unknown)
 * 	[name: string] symbol name
 * if exinfo == TYPE_SAMPLE_UBIN
 * 	[address: sleb128] address where binary has been loaded as a difference from ptr_base
 * 	[offset: uleb128] file offset of mapping (the same file can be mapped multiple times)
 * 	[size: uleb128] memory size
 * 	[name: string] binary name
 * if exinfo == TYPE_SAMPLE_COUNTERS_DESC
 * 	[len: uleb128] number of counters
 * 	for i = 0 to len
 * 		[section: uleb128] section of counter
 * 		if section == MONO_COUNTER_PERFCOUNTERS:
 * 			[section_name: string] section name of counter
 * 		[name: string] name of counter
 * 		[type: byte] type of counter
 * 		[unit: byte] unit of counter
 * 		[variance: byte] variance of counter
 * 		[index: uleb128] unique index of counter
 * if exinfo == TYPE_SAMPLE_COUNTERS
 * 	while true:
 * 		[index: uleb128] unique index of counter
 * 		if index == 0:
 * 			break
 * 		[type: byte] type of counter value
 * 		if type == string:
 * 			if value == null:
 * 				[0: byte] 0 -> value is null
 * 			else:
 * 				[1: byte] 1 -> value is not null
 * 				[value: string] counter value
 * 		else:
 * 			[value: uleb128/sleb128/double] counter value, can be sleb128, uleb128 or double (determined by using type)
 *
 * type coverage format
 * type: TYPE_COVERAGE
 * exinfo: one of TYPE_COVERAGE_METHOD, TYPE_COVERAGE_STATEMENT, TYPE_COVERAGE_ASSEMBLY, TYPE_COVERAGE_CLASS
 * if exinfo == TYPE_COVERAGE_METHOD
 *  [assembly: string] name of assembly
 *  [class: string] name of the class
 *  [name: string] name of the method
 *  [signature: string] the signature of the method
 *  [filename: string] the file path of the file that contains this method
 *  [token: uleb128] the method token
 *  [method_id: uleb128] an ID for this data to associate with the buffers of TYPE_COVERAGE_STATEMENTS
 *  [len: uleb128] the number of TYPE_COVERAGE_BUFFERS associated with this method
 * if exinfo == TYPE_COVERAGE_STATEMENTS
 *  [method_id: uleb128] an the TYPE_COVERAGE_METHOD buffer to associate this with
 *  [offset: uleb128] the il offset relative to the previous offset
 *  [counter: uleb128] the counter for this instruction
 *  [line: uleb128] the line of filename containing this instruction
 *  [column: uleb128] the column containing this instruction
 * if exinfo == TYPE_COVERAGE_ASSEMBLY
 *  [name: string] assembly name
 *  [guid: string] assembly GUID
 *  [filename: string] assembly filename
 *  [number_of_methods: uleb128] the number of methods in this assembly
 *  [fully_covered: uleb128] the number of fully covered methods
 *  [partially_covered: uleb128] the number of partially covered methods
 *    currently partially_covered will always be 0, and fully_covered is the
 *    number of methods that are fully and partially covered.
 * if exinfo == TYPE_COVERAGE_CLASS
 *  [name: string] assembly name
 *  [class: string] class name
 *  [number_of_methods: uleb128] the number of methods in this class
 *  [fully_covered: uleb128] the number of fully covered methods
 *  [partially_covered: uleb128] the number of partially covered methods
 *    currently partially_covered will always be 0, and fully_covered is the
 *    number of methods that are fully and partially covered.
 *
 * type meta format:
 * type: TYPE_META
 * exinfo: one of: TYPE_SYNC_POINT
 * if exinfo == TYPE_SYNC_POINT
 *	[type: byte] MonoProfilerSyncPointType enum value
 */

// Statistics for internal profiler data structures.
static gint32 sample_allocations_ctr,
              buffer_allocations_ctr;

// Statistics for profiler events.
static gint32 sync_points_ctr,
              heap_objects_ctr,
              heap_starts_ctr,
              heap_ends_ctr,
              heap_roots_ctr,
              gc_events_ctr,
              gc_resizes_ctr,
              gc_allocs_ctr,
              gc_moves_ctr,
              gc_handle_creations_ctr,
              gc_handle_deletions_ctr,
              finalize_begins_ctr,
              finalize_ends_ctr,
              finalize_object_begins_ctr,
              finalize_object_ends_ctr,
              image_loads_ctr,
              image_unloads_ctr,
              assembly_loads_ctr,
              assembly_unloads_ctr,
              class_loads_ctr,
              class_unloads_ctr,
              method_entries_ctr,
              method_exits_ctr,
              method_exception_exits_ctr,
              method_jits_ctr,
              code_buffers_ctr,
              exception_throws_ctr,
              exception_clauses_ctr,
              monitor_events_ctr,
              thread_starts_ctr,
              thread_ends_ctr,
              thread_names_ctr,
              domain_loads_ctr,
              domain_unloads_ctr,
              domain_names_ctr,
              context_loads_ctr,
              context_unloads_ctr,
              sample_ubins_ctr,
              sample_usyms_ctr,
              sample_hits_ctr,
              counter_descriptors_ctr,
              counter_samples_ctr,
              perfcounter_descriptors_ctr,
              perfcounter_samples_ctr,
              coverage_methods_ctr,
              coverage_statements_ctr,
              coverage_classes_ctr,
              coverage_assemblies_ctr;

// Pending data to be written to the log, for a single thread.
// Threads periodically flush their own LogBuffers by calling safe_send
typedef struct _LogBuffer LogBuffer;
struct _LogBuffer {
	// Next (older) LogBuffer in processing queue
	LogBuffer *next;

	uint64_t time_base;
	uint64_t last_time;
	uintptr_t ptr_base;
	uintptr_t method_base;
	uintptr_t last_method;
	uintptr_t obj_base;
	uintptr_t thread_id;

	// Bytes allocated for this LogBuffer
	int size;

	// Start of currently unused space in buffer
	unsigned char* cursor;

	// Pointer to start-of-structure-plus-size (for convenience)
	unsigned char* buf_end;

	// Start of data in buffer. Contents follow "buffer format" described above.
	unsigned char buf [1];
};

typedef struct {
	MonoLinkedListSetNode node;

	// Was this thread added to the LLS?
	gboolean attached;

	// The current log buffer for this thread.
	LogBuffer *buffer;

	// Methods referenced by events in `buffer`, see `MethodInfo`.
	GPtrArray *methods;

	// Current call depth for enter/leave events.
	int call_depth;

	// Indicates whether this thread is currently writing to its `buffer`.
	gboolean busy;

	// Has this thread written a thread end event to `buffer`?
	gboolean ended;

	// Stored in `buffer_lock_state` to take the exclusive lock.
	int small_id;
} MonoProfilerThread;

// Do not use these TLS macros directly unless you know what you're doing.

#ifdef HOST_WIN32

#define PROF_TLS_SET(VAL) (TlsSetValue (profiler_tls, (VAL)))
#define PROF_TLS_GET() ((MonoProfilerThread *) TlsGetValue (profiler_tls))
#define PROF_TLS_INIT() (profiler_tls = TlsAlloc ())
#define PROF_TLS_FREE() (TlsFree (profiler_tls))

static DWORD profiler_tls;

#elif HAVE_KW_THREAD

#define PROF_TLS_SET(VAL) (profiler_tls = (VAL))
#define PROF_TLS_GET() (profiler_tls)
#define PROF_TLS_INIT()
#define PROF_TLS_FREE()

static __thread MonoProfilerThread *profiler_tls;

#else

#define PROF_TLS_SET(VAL) (pthread_setspecific (profiler_tls, (VAL)))
#define PROF_TLS_GET() ((MonoProfilerThread *) pthread_getspecific (profiler_tls))
#define PROF_TLS_INIT() (pthread_key_create (&profiler_tls, NULL))
#define PROF_TLS_FREE() (pthread_key_delete (profiler_tls))

static pthread_key_t profiler_tls;

#endif

static uintptr_t
thread_id (void)
{
	return (uintptr_t) mono_native_thread_id_get ();
}

static uintptr_t
process_id (void)
{
#ifdef HOST_WIN32
	return (uintptr_t) GetCurrentProcessId ();
#else
	return (uintptr_t) getpid ();
#endif
}

#define ENABLED(EVT) (log_config.effective_mask & (EVT))

/*
 * These macros should be used when writing an event to a log buffer. They
 * take care of a bunch of stuff that can be repetitive and error-prone, such
 * as attaching the current thread, acquiring/releasing the buffer lock,
 * incrementing the event counter, expanding the log buffer, etc. They also
 * create a scope so that it's harder to leak the LogBuffer pointer, which can
 * be problematic as the pointer is unstable when the buffer lock isn't
 * acquired.
 *
 * If the calling thread is already attached, these macros will not alter its
 * attach mode (i.e. whether it's added to the LLS). If the thread is not
 * attached, init_thread () will be called with add_to_lls = TRUE.
 */

#define ENTER_LOG(COUNTER, BUFFER, SIZE) \
	do { \
		MonoProfilerThread *thread__ = get_thread (); \
		if (thread__->attached) \
			buffer_lock (); \
		g_assert (!thread__->busy && "Why are we trying to write a new event while already writing one?"); \
		thread__->busy = TRUE; \
		InterlockedIncrement ((COUNTER)); \
		LogBuffer *BUFFER = ensure_logbuf_unsafe (thread__, (SIZE))

#define EXIT_LOG_EXPLICIT(SEND) \
		thread__->busy = FALSE; \
		if ((SEND)) \
			send_log_unsafe (TRUE); \
		if (thread__->attached) \
			buffer_unlock (); \
	} while (0)

// Pass these to EXIT_LOG_EXPLICIT () for easier reading.
#define DO_SEND TRUE
#define NO_SEND FALSE

#define EXIT_LOG EXIT_LOG_EXPLICIT (DO_SEND)

typedef struct _BinaryObject BinaryObject;
struct _BinaryObject {
	BinaryObject *next;
	void *addr;
	char *name;
};

typedef struct MonoCounterAgent {
	MonoCounter *counter;
	// MonoCounterAgent specific data :
	void *value;
	size_t value_size;
	guint32 index;
	gboolean emitted;
	struct MonoCounterAgent *next;
} MonoCounterAgent;

typedef struct _PerfCounterAgent PerfCounterAgent;
struct _PerfCounterAgent {
	PerfCounterAgent *next;
	guint32 index;
	char *category_name;
	char *name;
	gint64 value;
	gboolean emitted;
	gboolean updated;
	gboolean deleted;
};

struct _MonoProfiler {
	MonoProfilerHandle handle;

	FILE* file;
#if defined (HAVE_SYS_ZLIB)
	gzFile gzfile;
#endif

	char *args;
	uint64_t startup_time;
	int timer_overhead;

#ifdef __APPLE__
	mach_timebase_info_data_t timebase_info;
#elif defined (HOST_WIN32)
	LARGE_INTEGER pcounter_freq;
#endif

	int pipe_output;
	int command_port;
	int server_socket;
	int pipes [2];

	MonoLinkedListSet profiler_thread_list;
	volatile gint32 buffer_lock_state;
	volatile gint32 buffer_lock_exclusive_intent;

	volatile gint32 runtime_inited;
	volatile gint32 in_shutdown;

	MonoNativeThreadId helper_thread;

	MonoNativeThreadId writer_thread;
	volatile gint32 run_writer_thread;
	MonoLockFreeQueue writer_queue;
	MonoSemType writer_queue_sem;

	MonoLockFreeAllocSizeClass writer_entry_size_class;
	MonoLockFreeAllocator writer_entry_allocator;

	MonoConcurrentHashTable *method_table;
	mono_mutex_t method_table_mutex;

	MonoNativeThreadId dumper_thread;
	volatile gint32 run_dumper_thread;
	MonoLockFreeQueue dumper_queue;
	MonoSemType dumper_queue_sem;

	MonoLockFreeAllocSizeClass sample_size_class;
	MonoLockFreeAllocator sample_allocator;
	MonoLockFreeQueue sample_reuse_queue;

	BinaryObject *binary_objects;

	gboolean heapshot_requested;
	guint64 gc_count;
	guint64 last_hs_time;
	gboolean do_heap_walk;
	gboolean ignore_heap_events;

	mono_mutex_t counters_mutex;
	MonoCounterAgent *counters;
	PerfCounterAgent *perfcounters;
	guint32 counters_index;

	mono_mutex_t coverage_mutex;
	GPtrArray *coverage_data;

	GPtrArray *coverage_filters;
	MonoConcurrentHashTable *coverage_filtered_classes;
	MonoConcurrentHashTable *coverage_suppressed_assemblies;

	MonoConcurrentHashTable *coverage_methods;
	MonoConcurrentHashTable *coverage_assemblies;
	MonoConcurrentHashTable *coverage_classes;

	MonoConcurrentHashTable *coverage_image_to_methods;

	guint32 coverage_previous_offset;
	guint32 coverage_method_id;
};

static ProfilerConfig log_config;
static struct _MonoProfiler log_profiler;

typedef struct {
	MonoLockFreeQueueNode node;
	GPtrArray *methods;
	LogBuffer *buffer;
} WriterQueueEntry;

#define WRITER_ENTRY_BLOCK_SIZE (mono_pagesize ())

typedef struct {
	MonoMethod *method;
	MonoJitInfo *ji;
	uint64_t time;
} MethodInfo;

#define TICKS_PER_SEC 1000000000LL

static uint64_t
current_time (void)
{
#ifdef __APPLE__
	uint64_t time = mach_absolute_time ();

	time *= log_profiler.timebase_info.numer;
	time /= log_profiler.timebase_info.denom;

	return time;
#elif defined (HOST_WIN32)
	LARGE_INTEGER value;

	QueryPerformanceCounter (&value);

	return value.QuadPart * TICKS_PER_SEC / log_profiler.pcounter_freq.QuadPart;
#elif defined (CLOCK_MONOTONIC)
	struct timespec tspec;

	clock_gettime (CLOCK_MONOTONIC, &tspec);

	return ((uint64_t) tspec.tv_sec * TICKS_PER_SEC + tspec.tv_nsec);
#else
	struct timeval tv;

	gettimeofday (&tv, NULL);

	return ((uint64_t) tv.tv_sec * TICKS_PER_SEC + tv.tv_usec * 1000);
#endif
}

static void
init_time (void)
{
#ifdef __APPLE__
	mach_timebase_info (&log_profiler.timebase_info);
#elif defined (HOST_WIN32)
	QueryPerformanceFrequency (&log_profiler.pcounter_freq);
#endif

	uint64_t time_start = current_time ();

	for (int i = 0; i < 256; ++i)
		current_time ();

	uint64_t time_end = current_time ();

	log_profiler.timer_overhead = (time_end - time_start) / 256;
}

static char*
pstrdup (const char *s)
{
	int len = strlen (s) + 1;
	char *p = (char *) g_malloc (len);
	memcpy (p, s, len);
	return p;
}

#define BUFFER_SIZE (4096 * 16)

/* Worst-case size in bytes of a 64-bit value encoded with LEB128. */
#define LEB128_SIZE 10

/* Size of a value encoded as a single byte. */
#undef BYTE_SIZE // mach/i386/vm_param.h on OS X defines this to 8, but it isn't used for anything.
#define BYTE_SIZE 1

/* Size in bytes of the event prefix (ID + time). */
#define EVENT_SIZE (BYTE_SIZE + LEB128_SIZE)

static void *
alloc_buffer (int size)
{
	return mono_valloc (NULL, size, MONO_MMAP_READ | MONO_MMAP_WRITE | MONO_MMAP_ANON | MONO_MMAP_PRIVATE, MONO_MEM_ACCOUNT_PROFILER);
}

static void
free_buffer (void *buf, int size)
{
	mono_vfree (buf, size, MONO_MEM_ACCOUNT_PROFILER);
}

static LogBuffer*
create_buffer (uintptr_t tid, int bytes)
{
	LogBuffer* buf = (LogBuffer *) alloc_buffer (MAX (BUFFER_SIZE, bytes));

	InterlockedIncrement (&buffer_allocations_ctr);

	buf->size = BUFFER_SIZE;
	buf->time_base = current_time ();
	buf->last_time = buf->time_base;
	buf->buf_end = (unsigned char *) buf + buf->size;
	buf->cursor = buf->buf;
	buf->thread_id = tid;

	return buf;
}

/*
 * Must be called with the reader lock held if thread is the current thread, or
 * the exclusive lock if thread is a different thread. However, if thread is
 * the current thread, and init_thread () was called with add_to_lls = FALSE,
 * then no locking is necessary.
 */
static void
init_buffer_state (MonoProfilerThread *thread)
{
	thread->buffer = create_buffer (thread->node.key, 0);
	thread->methods = NULL;
}

static void
clear_hazard_pointers (MonoThreadHazardPointers *hp)
{
	mono_hazard_pointer_clear (hp, 0);
	mono_hazard_pointer_clear (hp, 1);
	mono_hazard_pointer_clear (hp, 2);
}

static MonoProfilerThread *
init_thread (gboolean add_to_lls)
{
	MonoProfilerThread *thread = PROF_TLS_GET ();

	/*
	 * Sometimes we may try to initialize a thread twice. One example is the
	 * main thread: We initialize it when setting up the profiler, but we will
	 * also get a thread_start () callback for it. Another example is when
	 * attaching new threads to the runtime: We may get a gc_alloc () callback
	 * for that thread's thread object (where we initialize it), soon followed
	 * by a thread_start () callback.
	 *
	 * These cases are harmless anyhow. Just return if we've already done the
	 * initialization work.
	 */
	if (thread)
		return thread;

	thread = g_malloc (sizeof (MonoProfilerThread));
	thread->node.key = thread_id ();
	thread->attached = add_to_lls;
	thread->call_depth = 0;
	thread->busy = 0;
	thread->ended = FALSE;

	init_buffer_state (thread);

	thread->small_id = mono_thread_info_register_small_id ();

	/*
	 * Some internal profiler threads don't need to be cleaned up
	 * by the main thread on shutdown.
	 */
	if (add_to_lls) {
		MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();
		g_assert (mono_lls_insert (&log_profiler.profiler_thread_list, hp, &thread->node) && "Why can't we insert the thread in the LLS?");
		clear_hazard_pointers (hp);
	}

	PROF_TLS_SET (thread);

	return thread;
}

// Only valid if init_thread () was called with add_to_lls = FALSE.
static void
deinit_thread (MonoProfilerThread *thread)
{
	g_assert (!thread->attached && "Why are we manually freeing an attached thread?");

	g_free (thread);
	PROF_TLS_SET (NULL);
}

static MonoProfilerThread *
get_thread (void)
{
	return init_thread (TRUE);
}

// Only valid if init_thread () was called with add_to_lls = FALSE.
static LogBuffer *
ensure_logbuf_unsafe (MonoProfilerThread *thread, int bytes)
{
	LogBuffer *old = thread->buffer;

	if (old->cursor + bytes < old->buf_end)
		return old;

	LogBuffer *new_ = create_buffer (thread->node.key, bytes);
	new_->next = old;
	thread->buffer = new_;

	return new_;
}

/*
 * This is a reader/writer spin lock of sorts used to protect log buffers.
 * When a thread modifies its own log buffer, it increments the reader
 * count. When a thread wants to access log buffers of other threads, it
 * takes the exclusive lock.
 *
 * `buffer_lock_state` holds the reader count in its lower 16 bits, and
 * the small ID of the thread currently holding the exclusive (writer)
 * lock in its upper 16 bits. Both can be zero. It's important that the
 * whole lock state is a single word that can be read/written atomically
 * to avoid race conditions where there could end up being readers while
 * the writer lock is held.
 *
 * The lock is writer-biased. When a thread wants to take the exclusive
 * lock, it increments `buffer_lock_exclusive_intent` which will make new
 * readers spin until it's back to zero, then takes the exclusive lock
 * once the reader count has reached zero. After releasing the exclusive
 * lock, it decrements `buffer_lock_exclusive_intent`, which, when it
 * reaches zero again, allows readers to increment the reader count.
 *
 * The writer bias is necessary because we take the exclusive lock in
 * `gc_event ()` during STW. If the writer bias was not there, and a
 * program had a large number of threads, STW-induced pauses could be
 * significantly longer than they have to be. Also, we emit periodic
 * sync points from the helper thread, which requires taking the
 * exclusive lock, and we need those to arrive with a reasonably
 * consistent frequency so that readers don't have to queue up too many
 * events between sync points.
 *
 * The lock does not support recursion.
 */

static void
buffer_lock (void)
{
	/*
	 * If the thread holding the exclusive lock tries to modify the
	 * reader count, just make it a no-op. This way, we also avoid
	 * invoking the GC safe point macros below, which could break if
	 * done from a thread that is currently the initiator of STW.
	 *
	 * In other words, we rely on the fact that the GC thread takes
	 * the exclusive lock in the gc_event () callback when the world
	 * is about to stop.
	 */
	if (InterlockedRead (&log_profiler.buffer_lock_state) != get_thread ()->small_id << 16) {
		MONO_ENTER_GC_SAFE;

		gint32 old, new_;

		do {
		restart:
			// Hold off if a thread wants to take the exclusive lock.
			while (InterlockedRead (&log_profiler.buffer_lock_exclusive_intent))
				mono_thread_info_yield ();

			old = InterlockedRead (&log_profiler.buffer_lock_state);

			// Is a thread holding the exclusive lock?
			if (old >> 16) {
				mono_thread_info_yield ();
				goto restart;
			}

			new_ = old + 1;
		} while (InterlockedCompareExchange (&log_profiler.buffer_lock_state, new_, old) != old);

		MONO_EXIT_GC_SAFE;
	}

	mono_memory_barrier ();
}

static void
buffer_unlock (void)
{
	mono_memory_barrier ();

	gint32 state = InterlockedRead (&log_profiler.buffer_lock_state);

	// See the comment in buffer_lock ().
	if (state == PROF_TLS_GET ()->small_id << 16)
		return;

	g_assert (state && "Why are we decrementing a zero reader count?");
	g_assert (!(state >> 16) && "Why is the exclusive lock held?");

	InterlockedDecrement (&log_profiler.buffer_lock_state);
}

static void
buffer_lock_excl (void)
{
	gint32 new_ = get_thread ()->small_id << 16;

	g_assert (InterlockedRead (&log_profiler.buffer_lock_state) != new_ && "Why are we taking the exclusive lock twice?");

	InterlockedIncrement (&log_profiler.buffer_lock_exclusive_intent);

	MONO_ENTER_GC_SAFE;

	while (InterlockedCompareExchange (&log_profiler.buffer_lock_state, new_, 0))
		mono_thread_info_yield ();

	MONO_EXIT_GC_SAFE;

	mono_memory_barrier ();
}

static void
buffer_unlock_excl (void)
{
	mono_memory_barrier ();

	gint32 state = InterlockedRead (&log_profiler.buffer_lock_state);
	gint32 excl = state >> 16;

	g_assert (excl && "Why is the exclusive lock not held?");
	g_assert (excl == PROF_TLS_GET ()->small_id && "Why does another thread hold the exclusive lock?");
	g_assert (!(state & 0xFFFF) && "Why are there readers when the exclusive lock is held?");

	InterlockedWrite (&log_profiler.buffer_lock_state, 0);
	InterlockedDecrement (&log_profiler.buffer_lock_exclusive_intent);
}

static void
encode_uleb128 (uint64_t value, uint8_t *buf, uint8_t **endbuf)
{
	uint8_t *p = buf;

	do {
		uint8_t b = value & 0x7f;
		value >>= 7;

		if (value != 0) /* more bytes to come */
			b |= 0x80;

		*p ++ = b;
	} while (value);

	*endbuf = p;
}

static void
encode_sleb128 (intptr_t value, uint8_t *buf, uint8_t **endbuf)
{
	int more = 1;
	int negative = (value < 0);
	unsigned int size = sizeof (intptr_t) * 8;
	uint8_t byte;
	uint8_t *p = buf;

	while (more) {
		byte = value & 0x7f;
		value >>= 7;

		/* the following is unnecessary if the
		 * implementation of >>= uses an arithmetic rather
		 * than logical shift for a signed left operand
		 */
		if (negative)
			/* sign extend */
			value |= - ((intptr_t) 1 <<(size - 7));

		/* sign bit of byte is second high order bit (0x40) */
		if ((value == 0 && !(byte & 0x40)) ||
		    (value == -1 && (byte & 0x40)))
			more = 0;
		else
			byte |= 0x80;

		*p ++= byte;
	}

	*endbuf = p;
}

static void
emit_byte (LogBuffer *logbuffer, int value)
{
	logbuffer->cursor [0] = value;
	logbuffer->cursor++;

	g_assert (logbuffer->cursor <= logbuffer->buf_end && "Why are we writing past the buffer end?");
}

static void
emit_value (LogBuffer *logbuffer, int value)
{
	encode_uleb128 (value, logbuffer->cursor, &logbuffer->cursor);

	g_assert (logbuffer->cursor <= logbuffer->buf_end && "Why are we writing past the buffer end?");
}

static void
emit_time (LogBuffer *logbuffer, uint64_t value)
{
	uint64_t tdiff = value - logbuffer->last_time;
	encode_uleb128 (tdiff, logbuffer->cursor, &logbuffer->cursor);
	logbuffer->last_time = value;

	g_assert (logbuffer->cursor <= logbuffer->buf_end && "Why are we writing past the buffer end?");
}

static void
emit_event_time (LogBuffer *logbuffer, int event, uint64_t time)
{
	emit_byte (logbuffer, event);
	emit_time (logbuffer, time);
}

static void
emit_event (LogBuffer *logbuffer, int event)
{
	emit_event_time (logbuffer, event, current_time ());
}

static void
emit_svalue (LogBuffer *logbuffer, int64_t value)
{
	encode_sleb128 (value, logbuffer->cursor, &logbuffer->cursor);

	g_assert (logbuffer->cursor <= logbuffer->buf_end && "Why are we writing past the buffer end?");
}

static void
emit_uvalue (LogBuffer *logbuffer, uint64_t value)
{
	encode_uleb128 (value, logbuffer->cursor, &logbuffer->cursor);

	g_assert (logbuffer->cursor <= logbuffer->buf_end && "Why are we writing past the buffer end?");
}

static void
emit_ptr (LogBuffer *logbuffer, const void *ptr)
{
	if (!logbuffer->ptr_base)
		logbuffer->ptr_base = (uintptr_t) ptr;

	emit_svalue (logbuffer, (intptr_t) ptr - logbuffer->ptr_base);

	g_assert (logbuffer->cursor <= logbuffer->buf_end && "Why are we writing past the buffer end?");
}

static void
emit_method_inner (LogBuffer *logbuffer, void *method)
{
	if (!logbuffer->method_base) {
		logbuffer->method_base = (intptr_t) method;
		logbuffer->last_method = (intptr_t) method;
	}

	encode_sleb128 ((intptr_t) ((char *) method - (char *) logbuffer->last_method), logbuffer->cursor, &logbuffer->cursor);
	logbuffer->last_method = (intptr_t) method;

	g_assert (logbuffer->cursor <= logbuffer->buf_end && "Why are we writing past the buffer end?");
}

// The reader lock must be held.
static void
register_method_local (MonoMethod *method, MonoJitInfo *ji)
{
	MonoProfilerThread *thread = get_thread ();

	if (!mono_conc_hashtable_lookup (log_profiler.method_table, method)) {
		MethodInfo *info = (MethodInfo *) g_malloc (sizeof (MethodInfo));

		info->method = method;
		info->ji = ji;
		info->time = current_time ();

		GPtrArray *arr = thread->methods ? thread->methods : (thread->methods = g_ptr_array_new ());
		g_ptr_array_add (arr, info);
	}
}

static void
emit_method (LogBuffer *logbuffer, MonoMethod *method)
{
	register_method_local (method, NULL);
	emit_method_inner (logbuffer, method);
}

static void
emit_obj (LogBuffer *logbuffer, void *ptr)
{
	if (!logbuffer->obj_base)
		logbuffer->obj_base = (uintptr_t) ptr >> 3;

	emit_svalue (logbuffer, ((uintptr_t) ptr >> 3) - logbuffer->obj_base);

	g_assert (logbuffer->cursor <= logbuffer->buf_end && "Why are we writing past the buffer end?");
}

static void
emit_string (LogBuffer *logbuffer, const char *str, size_t size)
{
	size_t i = 0;
	if (str) {
		for (; i < size; i++) {
			if (str[i] == '\0')
				break;
			emit_byte (logbuffer, str [i]);
		}
	}
	emit_byte (logbuffer, '\0');
}

static void
emit_double (LogBuffer *logbuffer, double value)
{
	int i;
	unsigned char buffer[8];
	memcpy (buffer, &value, 8);
#if G_BYTE_ORDER == G_BIG_ENDIAN
	for (i = 7; i >= 0; i--)
#else
	for (i = 0; i < 8; i++)
#endif
		emit_byte (logbuffer, buffer[i]);
}

static char*
write_int16 (char *buf, int32_t value)
{
	int i;
	for (i = 0; i < 2; ++i) {
		buf [i] = value;
		value >>= 8;
	}
	return buf + 2;
}

static char*
write_int32 (char *buf, int32_t value)
{
	int i;
	for (i = 0; i < 4; ++i) {
		buf [i] = value;
		value >>= 8;
	}
	return buf + 4;
}

static char*
write_int64 (char *buf, int64_t value)
{
	int i;
	for (i = 0; i < 8; ++i) {
		buf [i] = value;
		value >>= 8;
	}
	return buf + 8;
}

static char *
write_header_string (char *p, const char *str)
{
	size_t len = strlen (str) + 1;

	p = write_int32 (p, len);
	strcpy (p, str);

	return p + len;
}

static void
dump_header (void)
{
	const char *args = log_profiler.args;
	const char *arch = mono_config_get_cpu ();
	const char *os = mono_config_get_os ();

	char *hbuf = g_malloc (
		sizeof (gint32) /* header id */ +
		sizeof (gint8) /* major version */ +
		sizeof (gint8) /* minor version */ +
		sizeof (gint8) /* data version */ +
		sizeof (gint8) /* word size */ +
		sizeof (gint64) /* startup time */ +
		sizeof (gint32) /* timer overhead */ +
		sizeof (gint32) /* flags */ +
		sizeof (gint32) /* process id */ +
		sizeof (gint16) /* command port */ +
		sizeof (gint32) + strlen (args) + 1 /* arguments */ +
		sizeof (gint32) + strlen (arch) + 1 /* architecture */ +
		sizeof (gint32) + strlen (os) + 1 /* operating system */
	);
	char *p = hbuf;

	p = write_int32 (p, LOG_HEADER_ID);
	*p++ = LOG_VERSION_MAJOR;
	*p++ = LOG_VERSION_MINOR;
	*p++ = LOG_DATA_VERSION;
	*p++ = sizeof (void *);
	p = write_int64 (p, ((uint64_t) time (NULL)) * 1000);
	p = write_int32 (p, log_profiler.timer_overhead);
	p = write_int32 (p, 0); /* flags */
	p = write_int32 (p, process_id ());
	p = write_int16 (p, log_profiler.command_port);
	p = write_header_string (p, args);
	p = write_header_string (p, arch);
	p = write_header_string (p, os);

#if defined (HAVE_SYS_ZLIB)
	if (log_profiler.gzfile) {
		gzwrite (log_profiler.gzfile, hbuf, p - hbuf);
	} else
#endif
	{
		fwrite (hbuf, p - hbuf, 1, log_profiler.file);
		fflush (log_profiler.file);
	}

	g_free (hbuf);
}

/*
 * Must be called with the reader lock held if thread is the current thread, or
 * the exclusive lock if thread is a different thread. However, if thread is
 * the current thread, and init_thread () was called with add_to_lls = FALSE,
 * then no locking is necessary.
 */
static void
send_buffer (MonoProfilerThread *thread)
{
	WriterQueueEntry *entry = mono_lock_free_alloc (&log_profiler.writer_entry_allocator);
	entry->methods = thread->methods;
	entry->buffer = thread->buffer;

	mono_lock_free_queue_node_init (&entry->node, FALSE);

	mono_lock_free_queue_enqueue (&log_profiler.writer_queue, &entry->node);
	mono_os_sem_post (&log_profiler.writer_queue_sem);
}

static void
free_thread (gpointer p)
{
	MonoProfilerThread *thread = p;

	if (!thread->ended) {
		/*
		 * The thread is being cleaned up by the main thread during
		 * shutdown. This typically happens for internal runtime
		 * threads. We need to synthesize a thread end event.
		 */

		InterlockedIncrement (&thread_ends_ctr);

		if (ENABLED (PROFLOG_THREAD_EVENTS)) {
			LogBuffer *buf = ensure_logbuf_unsafe (thread,
				EVENT_SIZE /* event */ +
				BYTE_SIZE /* type */ +
				LEB128_SIZE /* tid */
			);

			emit_event (buf, TYPE_END_UNLOAD | TYPE_METADATA);
			emit_byte (buf, TYPE_THREAD);
			emit_ptr (buf, (void *) thread->node.key);
		}
	}

	send_buffer (thread);

	g_free (thread);
}

static void
remove_thread (MonoProfilerThread *thread)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();

	if (mono_lls_remove (&log_profiler.profiler_thread_list, hp, &thread->node))
		mono_thread_hazardous_try_free (thread, free_thread);

	clear_hazard_pointers (hp);
}

static void
dump_buffer (LogBuffer *buf)
{
	char hbuf [128];
	char *p = hbuf;

	if (buf->next)
		dump_buffer (buf->next);

	if (buf->cursor - buf->buf) {
		p = write_int32 (p, BUF_ID);
		p = write_int32 (p, buf->cursor - buf->buf);
		p = write_int64 (p, buf->time_base);
		p = write_int64 (p, buf->ptr_base);
		p = write_int64 (p, buf->obj_base);
		p = write_int64 (p, buf->thread_id);
		p = write_int64 (p, buf->method_base);

#if defined (HAVE_SYS_ZLIB)
		if (log_profiler.gzfile) {
			gzwrite (log_profiler.gzfile, hbuf, p - hbuf);
			gzwrite (log_profiler.gzfile, buf->buf, buf->cursor - buf->buf);
		} else
#endif
		{
			fwrite (hbuf, p - hbuf, 1, log_profiler.file);
			fwrite (buf->buf, buf->cursor - buf->buf, 1, log_profiler.file);
			fflush (log_profiler.file);
		}
	}

	free_buffer (buf, buf->size);
}

static void
dump_buffer_threadless (LogBuffer *buf)
{
	for (LogBuffer *iter = buf; iter; iter = iter->next)
		iter->thread_id = 0;

	dump_buffer (buf);
}

// Only valid if init_thread () was called with add_to_lls = FALSE.
static void
send_log_unsafe (gboolean if_needed)
{
	MonoProfilerThread *thread = PROF_TLS_GET ();

	if (!if_needed || (if_needed && thread->buffer->next)) {
		if (!thread->attached)
			for (LogBuffer *iter = thread->buffer; iter; iter = iter->next)
				iter->thread_id = 0;

		send_buffer (thread);
		init_buffer_state (thread);
	}
}

// Assumes that the exclusive lock is held.
static void
sync_point_flush (void)
{
	g_assert (InterlockedRead (&log_profiler.buffer_lock_state) == PROF_TLS_GET ()->small_id << 16 && "Why don't we hold the exclusive lock?");

	MONO_LLS_FOREACH_SAFE (&log_profiler.profiler_thread_list, MonoProfilerThread, thread) {
		g_assert (thread->attached && "Why is a thread in the LLS not attached?");

		send_buffer (thread);
		init_buffer_state (thread);
	} MONO_LLS_FOREACH_SAFE_END
}

// Assumes that the exclusive lock is held.
static void
sync_point_mark (MonoProfilerSyncPointType type)
{
	g_assert (InterlockedRead (&log_profiler.buffer_lock_state) == PROF_TLS_GET ()->small_id << 16 && "Why don't we hold the exclusive lock?");

	ENTER_LOG (&sync_points_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* type */
	);

	emit_event (logbuffer, TYPE_META | TYPE_SYNC_POINT);
	emit_byte (logbuffer, type);

	EXIT_LOG_EXPLICIT (NO_SEND);

	send_log_unsafe (FALSE);
}

// Assumes that the exclusive lock is held.
static void
sync_point (MonoProfilerSyncPointType type)
{
	sync_point_flush ();
	sync_point_mark (type);
}

static int
gc_reference (MonoObject *obj, MonoClass *klass, uintptr_t size, uintptr_t num, MonoObject **refs, uintptr_t *offsets, void *data)
{
	/* account for object alignment in the heap */
	size += 7;
	size &= ~7;

	ENTER_LOG (&heap_objects_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* obj */ +
		LEB128_SIZE /* klass */ +
		LEB128_SIZE /* size */ +
		LEB128_SIZE /* num */ +
		num * (
			LEB128_SIZE /* offset */ +
			LEB128_SIZE /* ref */
		)
	);

	emit_event (logbuffer, TYPE_HEAP_OBJECT | TYPE_HEAP);
	emit_obj (logbuffer, obj);
	emit_ptr (logbuffer, klass);
	emit_value (logbuffer, size);
	emit_value (logbuffer, num);

	uintptr_t last_offset = 0;

	for (int i = 0; i < num; ++i) {
		emit_value (logbuffer, offsets [i] - last_offset);
		last_offset = offsets [i];
		emit_obj (logbuffer, refs [i]);
	}

	EXIT_LOG;

	return 0;
}

static void
gc_roots (MonoProfiler *prof, MonoObject *const *objects, const MonoProfilerGCRootType *root_types, const uintptr_t *extra_info, uint64_t num)
{
	if (log_profiler.ignore_heap_events)
		return;

	ENTER_LOG (&heap_roots_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* num */ +
		LEB128_SIZE /* collections */ +
		num * (
			LEB128_SIZE /* object */ +
			LEB128_SIZE /* root type */ +
			LEB128_SIZE /* extra info */
		)
	);

	emit_event (logbuffer, TYPE_HEAP_ROOT | TYPE_HEAP);
	emit_value (logbuffer, num);
	emit_value (logbuffer, mono_gc_collection_count (mono_gc_max_generation ()));

	for (int i = 0; i < num; ++i) {
		emit_obj (logbuffer, objects [i]);
		emit_byte (logbuffer, root_types [i]);
		emit_value (logbuffer, extra_info [i]);
	}

	EXIT_LOG;
}


static void
trigger_on_demand_heapshot (void)
{
	if (log_profiler.heapshot_requested)
		mono_gc_collect (mono_gc_max_generation ());
}

#define ALL_GC_EVENTS_MASK (PROFLOG_GC_MOVES_EVENTS | PROFLOG_GC_ROOT_EVENTS | PROFLOG_GC_EVENTS | PROFLOG_HEAPSHOT_FEATURE)

static void
gc_event (MonoProfiler *profiler, MonoProfilerGCEvent ev, uint32_t generation)
{
	if (ev == MONO_GC_EVENT_START) {
		uint64_t now = current_time ();

		if (log_config.hs_mode_ms && (now - log_profiler.last_hs_time) / 1000 * 1000 >= log_config.hs_mode_ms)
			log_profiler.do_heap_walk = TRUE;
		else if (log_config.hs_mode_gc && !(log_profiler.gc_count % log_config.hs_mode_gc))
			log_profiler.do_heap_walk = TRUE;
		else if (log_config.hs_mode_ondemand)
			log_profiler.do_heap_walk = log_profiler.heapshot_requested;
		else if (!log_config.hs_mode_ms && !log_config.hs_mode_gc && generation == mono_gc_max_generation ())
			log_profiler.do_heap_walk = TRUE;

		//If using heapshot, ignore events for collections we don't care
		if (ENABLED (PROFLOG_HEAPSHOT_FEATURE)) {
			// Ignore events generated during the collection itself (IE GC ROOTS)
			log_profiler.ignore_heap_events = !log_profiler.do_heap_walk;
		}
	}


	if (ENABLED (PROFLOG_GC_EVENTS)) {
		ENTER_LOG (&gc_events_ctr, logbuffer,
			EVENT_SIZE /* event */ +
			BYTE_SIZE /* gc event */ +
			BYTE_SIZE /* generation */
		);

		emit_event (logbuffer, TYPE_GC_EVENT | TYPE_GC);
		emit_byte (logbuffer, ev);
		emit_byte (logbuffer, generation);

		EXIT_LOG;
	}

	switch (ev) {
	case MONO_GC_EVENT_START:
		if (generation == mono_gc_max_generation ())
			log_profiler.gc_count++;

		break;
	case MONO_GC_EVENT_PRE_STOP_WORLD_LOCKED:
		/*
		 * Ensure that no thread can be in the middle of writing to
		 * a buffer when the world stops...
		 */
		buffer_lock_excl ();
		break;
	case MONO_GC_EVENT_POST_STOP_WORLD:
		/*
		 * ... So that we now have a consistent view of all buffers.
		 * This allows us to flush them. We need to do this because
		 * they may contain object allocation events that need to be
		 * committed to the log file before any object move events
		 * that will be produced during this GC.
		 */
		if (ENABLED (ALL_GC_EVENTS_MASK))
			sync_point (SYNC_POINT_WORLD_STOP);

		/*
		 * All heap events are surrounded by a HEAP_START and a HEAP_ENV event.
		 * Right now, that's the case for GC Moves, GC Roots or heapshots.
		 */
		if (ENABLED (PROFLOG_GC_MOVES_EVENTS | PROFLOG_GC_ROOT_EVENTS) || log_profiler.do_heap_walk) {
			ENTER_LOG (&heap_starts_ctr, logbuffer,
				EVENT_SIZE /* event */
			);

			emit_event (logbuffer, TYPE_HEAP_START | TYPE_HEAP);

			EXIT_LOG;
		}

		break;
	case MONO_GC_EVENT_PRE_START_WORLD:
		if (ENABLED (PROFLOG_HEAPSHOT_FEATURE) && log_profiler.do_heap_walk)
			mono_gc_walk_heap (0, gc_reference, NULL);

		/* Matching HEAP_END to the HEAP_START from above */
		if (ENABLED (PROFLOG_GC_MOVES_EVENTS | PROFLOG_GC_ROOT_EVENTS) || log_profiler.do_heap_walk) {
			ENTER_LOG (&heap_ends_ctr, logbuffer,
				EVENT_SIZE /* event */
			);

			emit_event (logbuffer, TYPE_HEAP_END | TYPE_HEAP);

			EXIT_LOG;
		}

		if (ENABLED (PROFLOG_HEAPSHOT_FEATURE) && log_profiler.do_heap_walk) {
			log_profiler.do_heap_walk = FALSE;
			log_profiler.heapshot_requested = FALSE;
			log_profiler.last_hs_time = current_time ();
		}

		/*
		 * Similarly, we must now make sure that any object moves
		 * written to the GC thread's buffer are flushed. Otherwise,
		 * object allocation events for certain addresses could come
		 * after the move events that made those addresses available.
		 */
		if (ENABLED (ALL_GC_EVENTS_MASK))
			sync_point_mark (SYNC_POINT_WORLD_START);
		break;
	case MONO_GC_EVENT_POST_START_WORLD_UNLOCKED:
		/*
		 * Finally, it is safe to allow other threads to write to
		 * their buffers again.
		 */
		buffer_unlock_excl ();
		break;
	default:
		break;
	}
}

static void
gc_resize (MonoProfiler *profiler, uintptr_t new_size)
{
	ENTER_LOG (&gc_resizes_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* new size */
	);

	emit_event (logbuffer, TYPE_GC_RESIZE | TYPE_GC);
	emit_value (logbuffer, new_size);

	EXIT_LOG;
}

typedef struct {
	int count;
	MonoMethod* methods [MAX_FRAMES];
	int32_t il_offsets [MAX_FRAMES];
	int32_t native_offsets [MAX_FRAMES];
} FrameData;

static mono_bool
walk_stack (MonoMethod *method, int32_t native_offset, int32_t il_offset, mono_bool managed, void* data)
{
	FrameData *frame = (FrameData *)data;
	if (method && frame->count < log_config.num_frames) {
		frame->il_offsets [frame->count] = il_offset;
		frame->native_offsets [frame->count] = native_offset;
		frame->methods [frame->count++] = method;
	}
	return frame->count == log_config.num_frames;
}

/*
 * a note about stack walks: they can cause more profiler events to fire,
 * so we need to make sure they don't happen after we started emitting an
 * event, hence the collect_bt/emit_bt split.
 */
static void
collect_bt (FrameData *data)
{
	data->count = 0;
	mono_stack_walk_no_il (walk_stack, data);
}

static void
emit_bt (LogBuffer *logbuffer, FrameData *data)
{
	emit_value (logbuffer, data->count);

	while (data->count)
		emit_method (logbuffer, data->methods [--data->count]);
}

static void
gc_alloc (MonoProfiler *prof, MonoObject *obj)
{
	int do_bt = (!ENABLED (PROFLOG_CALL_EVENTS) && InterlockedRead (&log_profiler.runtime_inited) && !log_config.notraces) ? TYPE_ALLOC_BT : 0;
	FrameData data;
	uintptr_t len = mono_object_get_size (obj);
	/* account for object alignment in the heap */
	len += 7;
	len &= ~7;

	if (do_bt)
		collect_bt (&data);

	ENTER_LOG (&gc_allocs_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* klass */ +
		LEB128_SIZE /* obj */ +
		LEB128_SIZE /* size */ +
		(do_bt ? (
			LEB128_SIZE /* count */ +
			data.count * (
				LEB128_SIZE /* method */
			)
		) : 0)
	);

	emit_event (logbuffer, do_bt | TYPE_ALLOC);
	emit_ptr (logbuffer, mono_object_get_class (obj));
	emit_obj (logbuffer, obj);
	emit_value (logbuffer, len);

	if (do_bt)
		emit_bt (logbuffer, &data);

	EXIT_LOG;
}

static void
gc_moves (MonoProfiler *prof, MonoObject *const *objects, uint64_t num)
{
	ENTER_LOG (&gc_moves_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* num */ +
		num * (
			LEB128_SIZE /* object */
		)
	);

	emit_event (logbuffer, TYPE_GC_MOVE | TYPE_GC);
	emit_value (logbuffer, num);

	for (int i = 0; i < num; ++i)
		emit_obj (logbuffer, objects [i]);

	EXIT_LOG;
}

static void
gc_handle (MonoProfiler *prof, int op, MonoGCHandleType type, uint32_t handle, MonoObject *obj)
{
	int do_bt = !ENABLED (PROFLOG_CALL_EVENTS) && InterlockedRead (&log_profiler.runtime_inited) && !log_config.notraces;
	FrameData data;

	if (do_bt)
		collect_bt (&data);

	gint32 *ctr = op == MONO_PROFILER_GC_HANDLE_CREATED ? &gc_handle_creations_ctr : &gc_handle_deletions_ctr;

	ENTER_LOG (ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* type */ +
		LEB128_SIZE /* handle */ +
		(op == MONO_PROFILER_GC_HANDLE_CREATED ? (
			LEB128_SIZE /* obj */
		) : 0) +
		(do_bt ? (
			LEB128_SIZE /* count */ +
			data.count * (
				LEB128_SIZE /* method */
			)
		) : 0)
	);

	if (op == MONO_PROFILER_GC_HANDLE_CREATED)
		emit_event (logbuffer, (do_bt ? TYPE_GC_HANDLE_CREATED_BT : TYPE_GC_HANDLE_CREATED) | TYPE_GC);
	else if (op == MONO_PROFILER_GC_HANDLE_DESTROYED)
		emit_event (logbuffer, (do_bt ? TYPE_GC_HANDLE_DESTROYED_BT : TYPE_GC_HANDLE_DESTROYED) | TYPE_GC);
	else
		g_assert_not_reached ();

	emit_value (logbuffer, type);
	emit_value (logbuffer, handle);

	if (op == MONO_PROFILER_GC_HANDLE_CREATED)
		emit_obj (logbuffer, obj);

	if (do_bt)
		emit_bt (logbuffer, &data);

	EXIT_LOG;
}

static void
gc_handle_created (MonoProfiler *prof, uint32_t handle, MonoGCHandleType type, MonoObject *obj)
{
	gc_handle (prof, MONO_PROFILER_GC_HANDLE_CREATED, type, handle, obj);
}

static void
gc_handle_deleted (MonoProfiler *prof, uint32_t handle, MonoGCHandleType type)
{
	gc_handle (prof, MONO_PROFILER_GC_HANDLE_DESTROYED, type, handle, NULL);
}

static void
finalize_begin (MonoProfiler *prof)
{
	ENTER_LOG (&finalize_begins_ctr, buf,
		EVENT_SIZE /* event */
	);

	emit_event (buf, TYPE_GC_FINALIZE_START | TYPE_GC);

	EXIT_LOG;
}

static void
finalize_end (MonoProfiler *prof)
{
	trigger_on_demand_heapshot ();
	if (ENABLED (PROFLOG_FINALIZATION_EVENTS)) {
		ENTER_LOG (&finalize_ends_ctr, buf,
			EVENT_SIZE /* event */
		);

		emit_event (buf, TYPE_GC_FINALIZE_END | TYPE_GC);

		EXIT_LOG;
	}
}

static void
finalize_object_begin (MonoProfiler *prof, MonoObject *obj)
{
	ENTER_LOG (&finalize_object_begins_ctr, buf,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* obj */
	);

	emit_event (buf, TYPE_GC_FINALIZE_OBJECT_START | TYPE_GC);
	emit_obj (buf, obj);

	EXIT_LOG;
}

static void
finalize_object_end (MonoProfiler *prof, MonoObject *obj)
{
	ENTER_LOG (&finalize_object_ends_ctr, buf,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* obj */
	);

	emit_event (buf, TYPE_GC_FINALIZE_OBJECT_END | TYPE_GC);
	emit_obj (buf, obj);

	EXIT_LOG;
}

static char*
push_nesting (char *p, MonoClass *klass)
{
	MonoClass *nesting;
	const char *name;
	const char *nspace;
	nesting = mono_class_get_nesting_type (klass);
	if (nesting) {
		p = push_nesting (p, nesting);
		*p++ = '/';
		*p = 0;
	}
	name = mono_class_get_name (klass);
	nspace = mono_class_get_namespace (klass);
	if (*nspace) {
		strcpy (p, nspace);
		p += strlen (nspace);
		*p++ = '.';
		*p = 0;
	}
	strcpy (p, name);
	p += strlen (name);
	return p;
}

static char*
type_name (MonoClass *klass)
{
	char buf [1024];
	char *p;
	push_nesting (buf, klass);
	p = (char *) g_malloc (strlen (buf) + 1);
	strcpy (p, buf);
	return p;
}

static void
image_loaded (MonoProfiler *prof, MonoImage *image)
{
	const char *name = mono_image_get_filename (image);
	int nlen = strlen (name) + 1;

	ENTER_LOG (&image_loads_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* image */ +
		nlen /* name */
	);

	emit_event (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_IMAGE);
	emit_ptr (logbuffer, image);
	memcpy (logbuffer->cursor, name, nlen);
	logbuffer->cursor += nlen;

	EXIT_LOG;
}

static void
image_unloaded (MonoProfiler *prof, MonoImage *image)
{
	const char *name = mono_image_get_filename (image);
	int nlen = strlen (name) + 1;

	ENTER_LOG (&image_unloads_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* image */ +
		nlen /* name */
	);

	emit_event (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_IMAGE);
	emit_ptr (logbuffer, image);
	memcpy (logbuffer->cursor, name, nlen);
	logbuffer->cursor += nlen;

	EXIT_LOG;
}

static void
assembly_loaded (MonoProfiler *prof, MonoAssembly *assembly)
{
	char *name = mono_stringify_assembly_name (mono_assembly_get_name (assembly));
	int nlen = strlen (name) + 1;
	MonoImage *image = mono_assembly_get_image (assembly);

	ENTER_LOG (&assembly_loads_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* assembly */ +
		LEB128_SIZE /* image */ +
		nlen /* name */
	);

	emit_event (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_ASSEMBLY);
	emit_ptr (logbuffer, assembly);
	emit_ptr (logbuffer, image);
	memcpy (logbuffer->cursor, name, nlen);
	logbuffer->cursor += nlen;

	EXIT_LOG;

	mono_free (name);
}

static void
assembly_unloaded (MonoProfiler *prof, MonoAssembly *assembly)
{
	char *name = mono_stringify_assembly_name (mono_assembly_get_name (assembly));
	int nlen = strlen (name) + 1;
	MonoImage *image = mono_assembly_get_image (assembly);

	ENTER_LOG (&assembly_unloads_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* assembly */ +
		LEB128_SIZE /* image */ +
		nlen /* name */
	);

	emit_event (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_ASSEMBLY);
	emit_ptr (logbuffer, assembly);
	emit_ptr (logbuffer, image);
	memcpy (logbuffer->cursor, name, nlen);
	logbuffer->cursor += nlen;

	EXIT_LOG;

	mono_free (name);
}

static void
class_loaded (MonoProfiler *prof, MonoClass *klass)
{
	char *name;

	if (InterlockedRead (&log_profiler.runtime_inited))
		name = mono_type_get_name (mono_class_get_type (klass));
	else
		name = type_name (klass);

	int nlen = strlen (name) + 1;
	MonoImage *image = mono_class_get_image (klass);

	ENTER_LOG (&class_loads_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* klass */ +
		LEB128_SIZE /* image */ +
		nlen /* name */
	);

	emit_event (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_CLASS);
	emit_ptr (logbuffer, klass);
	emit_ptr (logbuffer, image);
	memcpy (logbuffer->cursor, name, nlen);
	logbuffer->cursor += nlen;

	EXIT_LOG;

	if (InterlockedRead (&log_profiler.runtime_inited))
		mono_free (name);
	else
		g_free (name);
}

static void
method_enter (MonoProfiler *prof, MonoMethod *method)
{
	if (get_thread ()->call_depth++ <= log_config.max_call_depth) {
		ENTER_LOG (&method_entries_ctr, logbuffer,
			EVENT_SIZE /* event */ +
			LEB128_SIZE /* method */
		);

		emit_event (logbuffer, TYPE_ENTER | TYPE_METHOD);
		emit_method (logbuffer, method);

		EXIT_LOG;
	}
}

static void
method_leave (MonoProfiler *prof, MonoMethod *method)
{
	if (--get_thread ()->call_depth <= log_config.max_call_depth) {
		ENTER_LOG (&method_exits_ctr, logbuffer,
			EVENT_SIZE /* event */ +
			LEB128_SIZE /* method */
		);

		emit_event (logbuffer, TYPE_LEAVE | TYPE_METHOD);
		emit_method (logbuffer, method);

		EXIT_LOG;
	}
}

static void
method_exc_leave (MonoProfiler *prof, MonoMethod *method, MonoObject *exc)
{
	if (--get_thread ()->call_depth <= log_config.max_call_depth) {
		ENTER_LOG (&method_exception_exits_ctr, logbuffer,
			EVENT_SIZE /* event */ +
			LEB128_SIZE /* method */
		);

		emit_event (logbuffer, TYPE_EXC_LEAVE | TYPE_METHOD);
		emit_method (logbuffer, method);

		EXIT_LOG;
	}
}

static MonoProfilerCallInstrumentationFlags
method_filter (MonoProfiler *prof, MonoMethod *method)
{
	return MONO_PROFILER_CALL_INSTRUMENTATION_PROLOGUE | MONO_PROFILER_CALL_INSTRUMENTATION_EPILOGUE;
}

static void
method_jitted (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *ji)
{
	buffer_lock ();

	register_method_local (method, ji);

	buffer_unlock ();
}

static void
code_buffer_new (MonoProfiler *prof, const mono_byte *buffer, uint64_t size, MonoProfilerCodeBufferType type, const void *data)
{
	const char *name;
	int nlen;

	if (type == MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE) {
		name = (const char *) data;
		nlen = strlen (name) + 1;
	} else {
		name = NULL;
		nlen = 0;
	}

	ENTER_LOG (&code_buffers_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* buffer */ +
		LEB128_SIZE /* size */ +
		(name ? (
			nlen /* name */
		) : 0)
	);

	emit_event (logbuffer, TYPE_JITHELPER | TYPE_RUNTIME);
	emit_byte (logbuffer, type);
	emit_ptr (logbuffer, buffer);
	emit_value (logbuffer, size);

	if (name) {
		memcpy (logbuffer->cursor, name, nlen);
		logbuffer->cursor += nlen;
	}

	EXIT_LOG;
}

static void
throw_exc (MonoProfiler *prof, MonoObject *object)
{
	int do_bt = (!ENABLED (PROFLOG_CALL_EVENTS) && InterlockedRead (&log_profiler.runtime_inited) && !log_config.notraces) ? TYPE_THROW_BT : 0;
	FrameData data;

	if (do_bt)
		collect_bt (&data);

	ENTER_LOG (&exception_throws_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* object */ +
		(do_bt ? (
			LEB128_SIZE /* count */ +
			data.count * (
				LEB128_SIZE /* method */
			)
		) : 0)
	);

	emit_event (logbuffer, do_bt | TYPE_EXCEPTION);
	emit_obj (logbuffer, object);

	if (do_bt)
		emit_bt (logbuffer, &data);

	EXIT_LOG;
}

static void
clause_exc (MonoProfiler *prof, MonoMethod *method, uint32_t clause_num, MonoExceptionEnum clause_type, MonoObject *exc)
{
	ENTER_LOG (&exception_clauses_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* clause type */ +
		LEB128_SIZE /* clause num */ +
		LEB128_SIZE /* method */
	);

	emit_event (logbuffer, TYPE_EXCEPTION | TYPE_CLAUSE);
	emit_byte (logbuffer, clause_type);
	emit_value (logbuffer, clause_num);
	emit_method (logbuffer, method);
	emit_obj (logbuffer, exc);

	EXIT_LOG;
}

static void
monitor_event (MonoProfiler *profiler, MonoObject *object, MonoProfilerMonitorEvent ev)
{
	int do_bt = (!ENABLED (PROFLOG_CALL_EVENTS) && InterlockedRead (&log_profiler.runtime_inited) && !log_config.notraces) ? TYPE_MONITOR_BT : 0;
	FrameData data;

	if (do_bt)
		collect_bt (&data);

	ENTER_LOG (&monitor_events_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* ev */ +
		LEB128_SIZE /* object */ +
		(do_bt ? (
			LEB128_SIZE /* count */ +
			data.count * (
				LEB128_SIZE /* method */
			)
		) : 0)
	);

	emit_event (logbuffer, do_bt | TYPE_MONITOR);
	emit_byte (logbuffer, ev);
	emit_obj (logbuffer, object);

	if (do_bt)
		emit_bt (logbuffer, &data);

	EXIT_LOG;
}

static void
monitor_contention (MonoProfiler *prof, MonoObject *object)
{
	monitor_event (prof, object, MONO_PROFILER_MONITOR_CONTENTION);
}

static void
monitor_acquired (MonoProfiler *prof, MonoObject *object)
{
	monitor_event (prof, object, MONO_PROFILER_MONITOR_DONE);
}

static void
monitor_failed (MonoProfiler *prof, MonoObject *object)
{
	monitor_event (prof, object, MONO_PROFILER_MONITOR_FAIL);
}

static void
thread_start (MonoProfiler *prof, uintptr_t tid)
{
	if (ENABLED (PROFLOG_THREAD_EVENTS)) {
		ENTER_LOG (&thread_starts_ctr, logbuffer,
			EVENT_SIZE /* event */ +
			BYTE_SIZE /* type */ +
			LEB128_SIZE /* tid */
		);

		emit_event (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
		emit_byte (logbuffer, TYPE_THREAD);
		emit_ptr (logbuffer, (void*) tid);

		EXIT_LOG;
	}
}

static void
thread_end (MonoProfiler *prof, uintptr_t tid)
{
	if (ENABLED (PROFLOG_THREAD_EVENTS)) {
		ENTER_LOG (&thread_ends_ctr, logbuffer,
			EVENT_SIZE /* event */ +
			BYTE_SIZE /* type */ +
			LEB128_SIZE /* tid */
		);

		emit_event (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
		emit_byte (logbuffer, TYPE_THREAD);
		emit_ptr (logbuffer, (void*) tid);

		EXIT_LOG_EXPLICIT (NO_SEND);
	}

	MonoProfilerThread *thread = get_thread ();

	thread->ended = TRUE;
	remove_thread (thread);

	PROF_TLS_SET (NULL);
}

static void
thread_name (MonoProfiler *prof, uintptr_t tid, const char *name)
{
	int len = strlen (name) + 1;

	if (ENABLED (PROFLOG_THREAD_EVENTS)) {
		ENTER_LOG (&thread_names_ctr, logbuffer,
			EVENT_SIZE /* event */ +
			BYTE_SIZE /* type */ +
			LEB128_SIZE /* tid */ +
			len /* name */
		);

		emit_event (logbuffer, TYPE_METADATA);
		emit_byte (logbuffer, TYPE_THREAD);
		emit_ptr (logbuffer, (void*)tid);
		memcpy (logbuffer->cursor, name, len);
		logbuffer->cursor += len;

		EXIT_LOG;
	}
}

static void
domain_loaded (MonoProfiler *prof, MonoDomain *domain)
{
	ENTER_LOG (&domain_loads_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* domain id */
	);

	emit_event (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_DOMAIN);
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_domain_get_id (domain));

	EXIT_LOG;
}

static void
domain_unloaded (MonoProfiler *prof, MonoDomain *domain)
{
	ENTER_LOG (&domain_unloads_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* domain id */
	);

	emit_event (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_DOMAIN);
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_domain_get_id (domain));

	EXIT_LOG;
}

static void
domain_name (MonoProfiler *prof, MonoDomain *domain, const char *name)
{
	int nlen = strlen (name) + 1;

	ENTER_LOG (&domain_names_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* domain id */ +
		nlen /* name */
	);

	emit_event (logbuffer, TYPE_METADATA);
	emit_byte (logbuffer, TYPE_DOMAIN);
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_domain_get_id (domain));
	memcpy (logbuffer->cursor, name, nlen);
	logbuffer->cursor += nlen;

	EXIT_LOG;
}

static void
context_loaded (MonoProfiler *prof, MonoAppContext *context)
{
	ENTER_LOG (&context_loads_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* context id */ +
		LEB128_SIZE /* domain id */
	);

	emit_event (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_CONTEXT);
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_context_get_id (context));
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_context_get_domain_id (context));

	EXIT_LOG;
}

static void
context_unloaded (MonoProfiler *prof, MonoAppContext *context)
{
	ENTER_LOG (&context_unloads_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* context id */ +
		LEB128_SIZE /* domain id */
	);

	emit_event (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_CONTEXT);
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_context_get_id (context));
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_context_get_domain_id (context));

	EXIT_LOG;
}

typedef struct {
	MonoMethod *method;
	MonoDomain *domain;
	void *base_address;
	int offset;
} AsyncFrameInfo;

typedef struct {
	MonoLockFreeQueueNode node;
	uint64_t time;
	uintptr_t tid;
	const void *ip;
	int count;
	AsyncFrameInfo frames [MONO_ZERO_LEN_ARRAY];
} SampleHit;

static mono_bool
async_walk_stack (MonoMethod *method, MonoDomain *domain, void *base_address, int offset, void *data)
{
	SampleHit *sample = (SampleHit *) data;

	if (sample->count < log_config.num_frames) {
		int i = sample->count;

		sample->frames [i].method = method;
		sample->frames [i].domain = domain;
		sample->frames [i].base_address = base_address;
		sample->frames [i].offset = offset;

		sample->count++;
	}

	return sample->count == log_config.num_frames;
}

#define SAMPLE_SLOT_SIZE(FRAMES) (sizeof (SampleHit) + sizeof (AsyncFrameInfo) * (FRAMES - MONO_ZERO_LEN_ARRAY))
#define SAMPLE_BLOCK_SIZE (mono_pagesize ())

static void
enqueue_sample_hit (gpointer p)
{
	SampleHit *sample = p;

	mono_lock_free_queue_node_unpoison (&sample->node);
	mono_lock_free_queue_enqueue (&log_profiler.dumper_queue, &sample->node);
	mono_os_sem_post (&log_profiler.dumper_queue_sem);
}

static void
mono_sample_hit (MonoProfiler *profiler, const mono_byte *ip, const void *context)
{
	/*
	 * Please note: We rely on the runtime loading the profiler with
	 * MONO_DL_EAGER (RTLD_NOW) so that references to runtime functions within
	 * this function (and its siblings) are resolved when the profiler is
	 * loaded. Otherwise, we would potentially invoke the dynamic linker when
	 * invoking runtime functions, which is not async-signal-safe.
	 */

	if (InterlockedRead (&log_profiler.in_shutdown))
		return;

	SampleHit *sample = (SampleHit *) mono_lock_free_queue_dequeue (&profiler->sample_reuse_queue);

	if (!sample) {
		/*
		 * If we're out of reusable sample events and we're not allowed to
		 * allocate more, we have no choice but to drop the event.
		 */
		if (InterlockedRead (&sample_allocations_ctr) >= log_config.max_allocated_sample_hits)
			return;

		sample = mono_lock_free_alloc (&profiler->sample_allocator);
		mono_lock_free_queue_node_init (&sample->node, TRUE);

		InterlockedIncrement (&sample_allocations_ctr);
	}

	sample->count = 0;
	mono_stack_walk_async_safe (&async_walk_stack, (void *) context, sample);

	sample->time = current_time ();
	sample->tid = thread_id ();
	sample->ip = ip;

	mono_thread_hazardous_try_free (sample, enqueue_sample_hit);
}

static uintptr_t *code_pages = 0;
static int num_code_pages = 0;
static int size_code_pages = 0;
#define CPAGE_SHIFT (9)
#define CPAGE_SIZE (1 << CPAGE_SHIFT)
#define CPAGE_MASK (~(CPAGE_SIZE - 1))
#define CPAGE_ADDR(p) ((p) & CPAGE_MASK)

static uintptr_t
add_code_page (uintptr_t *hash, uintptr_t hsize, uintptr_t page)
{
	uintptr_t i;
	uintptr_t start_pos;
	start_pos = (page >> CPAGE_SHIFT) % hsize;
	i = start_pos;
	do {
		if (hash [i] && CPAGE_ADDR (hash [i]) == CPAGE_ADDR (page)) {
			return 0;
		} else if (!hash [i]) {
			hash [i] = page;
			return 1;
		}
		/* wrap around */
		if (++i == hsize)
			i = 0;
	} while (i != start_pos);
	g_assert_not_reached ();
	return 0;
}

static void
add_code_pointer (uintptr_t ip)
{
	uintptr_t i;
	if (num_code_pages * 2 >= size_code_pages) {
		uintptr_t *n;
		uintptr_t old_size = size_code_pages;
		size_code_pages *= 2;
		if (size_code_pages == 0)
			size_code_pages = 16;
		n = (uintptr_t *) g_calloc (sizeof (uintptr_t) * size_code_pages, 1);
		for (i = 0; i < old_size; ++i) {
			if (code_pages [i])
				add_code_page (n, size_code_pages, code_pages [i]);
		}
		if (code_pages)
			g_free (code_pages);
		code_pages = n;
	}
	num_code_pages += add_code_page (code_pages, size_code_pages, ip & CPAGE_MASK);
}

/* ELF code crashes on some systems. */
//#if defined(HAVE_DL_ITERATE_PHDR) && defined(ELFMAG0)
#if 0
static void
dump_ubin (const char *filename, uintptr_t load_addr, uint64_t offset, uintptr_t size)
{
	int len = strlen (filename) + 1;

	ENTER_LOG (&sample_ubins_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* load address */ +
		LEB128_SIZE /* offset */ +
		LEB128_SIZE /* size */ +
		nlen /* file name */
	);

	emit_event (logbuffer, TYPE_SAMPLE | TYPE_SAMPLE_UBIN);
	emit_ptr (logbuffer, load_addr);
	emit_uvalue (logbuffer, offset);
	emit_uvalue (logbuffer, size);
	memcpy (logbuffer->cursor, filename, len);
	logbuffer->cursor += len;

	EXIT_LOG;
}
#endif

static void
dump_usym (const char *name, uintptr_t value, uintptr_t size)
{
	int len = strlen (name) + 1;

	ENTER_LOG (&sample_usyms_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* value */ +
		LEB128_SIZE /* size */ +
		len /* name */
	);

	emit_event (logbuffer, TYPE_SAMPLE | TYPE_SAMPLE_USYM);
	emit_ptr (logbuffer, (void*)value);
	emit_value (logbuffer, size);
	memcpy (logbuffer->cursor, name, len);
	logbuffer->cursor += len;

	EXIT_LOG;
}

/* ELF code crashes on some systems. */
//#if defined(ELFMAG0)
#if 0

#if SIZEOF_VOID_P == 4
#define ELF_WSIZE 32
#else
#define ELF_WSIZE 64
#endif
#ifndef ElfW
#define ElfW(type)      _ElfW (Elf, ELF_WSIZE, type)
#define _ElfW(e,w,t)    _ElfW_1 (e, w, _##t)
#define _ElfW_1(e,w,t)  e##w##t
#endif

static void
dump_elf_symbols (ElfW(Sym) *symbols, int num_symbols, const char *strtab, void *load_addr)
{
	int i;
	for (i = 0; i < num_symbols; ++i) {
		const char* sym;
		sym =  strtab + symbols [i].st_name;
		if (!symbols [i].st_name || !symbols [i].st_size || (symbols [i].st_info & 0xf) != STT_FUNC)
			continue;
		dump_usym (sym, (uintptr_t)load_addr + symbols [i].st_value, symbols [i].st_size);
	}
}

static int
read_elf_symbols (MonoProfiler *prof, const char *filename, void *load_addr)
{
	int fd, i;
	void *data;
	struct stat statb;
	uint64_t file_size;
	ElfW(Ehdr) *header;
	ElfW(Shdr) *sheader;
	ElfW(Shdr) *shstrtabh;
	ElfW(Shdr) *symtabh = NULL;
	ElfW(Shdr) *strtabh = NULL;
	ElfW(Sym) *symbols = NULL;
	const char *strtab;
	int num_symbols;

	fd = open (filename, O_RDONLY);
	if (fd < 0)
		return 0;
	if (fstat (fd, &statb) != 0) {
		close (fd);
		return 0;
	}
	file_size = statb.st_size;
	data = mmap (NULL, file_size, PROT_READ, MAP_PRIVATE, fd, 0);
	close (fd);
	if (data == MAP_FAILED)
		return 0;
	header = data;
	if (header->e_ident [EI_MAG0] != ELFMAG0 ||
			header->e_ident [EI_MAG1] != ELFMAG1 ||
			header->e_ident [EI_MAG2] != ELFMAG2 ||
			header->e_ident [EI_MAG3] != ELFMAG3 ) {
		munmap (data, file_size);
		return 0;
	}
	sheader = (void*)((char*)data + header->e_shoff);
	shstrtabh = (void*)((char*)sheader + (header->e_shentsize * header->e_shstrndx));
	strtab = (const char*)data + shstrtabh->sh_offset;
	for (i = 0; i < header->e_shnum; ++i) {
		if (sheader->sh_type == SHT_SYMTAB) {
			symtabh = sheader;
			strtabh = (void*)((char*)data + header->e_shoff + sheader->sh_link * header->e_shentsize);
			break;
		}
		sheader = (void*)((char*)sheader + header->e_shentsize);
	}
	if (!symtabh || !strtabh) {
		munmap (data, file_size);
		return 0;
	}
	strtab = (const char*)data + strtabh->sh_offset;
	num_symbols = symtabh->sh_size / symtabh->sh_entsize;
	symbols = (void*)((char*)data + symtabh->sh_offset);
	dump_elf_symbols (symbols, num_symbols, strtab, load_addr);
	munmap (data, file_size);
	return 1;
}
#endif

/* ELF code crashes on some systems. */
//#if defined(HAVE_DL_ITERATE_PHDR) && defined(ELFMAG0)
#if 0
static int
elf_dl_callback (struct dl_phdr_info *info, size_t size, void *data)
{
	char buf [256];
	const char *filename;
	BinaryObject *obj;
	char *a = (void*)info->dlpi_addr;
	int i, num_sym;
	ElfW(Dyn) *dyn = NULL;
	ElfW(Sym) *symtab = NULL;
	ElfW(Word) *hash_table = NULL;
	ElfW(Ehdr) *header = NULL;
	const char* strtab = NULL;
	for (obj = log_profiler.binary_objects; obj; obj = obj->next) {
		if (obj->addr == a)
			return 0;
	}
	filename = info->dlpi_name;
	if (!filename)
		return 0;
	if (!info->dlpi_addr && !filename [0]) {
		int l = readlink ("/proc/self/exe", buf, sizeof (buf) - 1);
		if (l > 0) {
			buf [l] = 0;
			filename = buf;
		}
	}
	obj = g_calloc (sizeof (BinaryObject), 1);
	obj->addr = (void*)info->dlpi_addr;
	obj->name = pstrdup (filename);
	obj->next = log_profiler.binary_objects;
	log_profiler.binary_objects = obj;
	a = NULL;
	for (i = 0; i < info->dlpi_phnum; ++i) {
		if (info->dlpi_phdr[i].p_type == PT_LOAD && !header) {
			header = (ElfW(Ehdr)*)(info->dlpi_addr + info->dlpi_phdr[i].p_vaddr);
			if (header->e_ident [EI_MAG0] != ELFMAG0 ||
					header->e_ident [EI_MAG1] != ELFMAG1 ||
					header->e_ident [EI_MAG2] != ELFMAG2 ||
					header->e_ident [EI_MAG3] != ELFMAG3 ) {
				header = NULL;
			}
			dump_ubin (filename, info->dlpi_addr + info->dlpi_phdr[i].p_vaddr, info->dlpi_phdr[i].p_offset, info->dlpi_phdr[i].p_memsz);
		} else if (info->dlpi_phdr[i].p_type == PT_DYNAMIC) {
			dyn = (ElfW(Dyn) *)(info->dlpi_addr + info->dlpi_phdr[i].p_vaddr);
		}
	}
	if (read_elf_symbols (filename, (void*)info->dlpi_addr))
		return 0;
	if (!info->dlpi_name || !info->dlpi_name[0])
		return 0;
	if (!dyn)
		return 0;
	for (i = 0; dyn [i].d_tag != DT_NULL; ++i) {
		if (dyn [i].d_tag == DT_SYMTAB) {
			symtab = (ElfW(Sym) *)(a + dyn [i].d_un.d_ptr);
		} else if (dyn [i].d_tag == DT_HASH) {
			hash_table = (ElfW(Word) *)(a + dyn [i].d_un.d_ptr);
		} else if (dyn [i].d_tag == DT_STRTAB) {
			strtab = (const char*)(a + dyn [i].d_un.d_ptr);
		}
	}
	if (!hash_table)
		return 0;
	num_sym = hash_table [1];
	dump_elf_symbols (symtab, num_sym, strtab, (void*)info->dlpi_addr);
	return 0;
}

static int
load_binaries (void)
{
	dl_iterate_phdr (elf_dl_callback, NULL);
	return 1;
}
#else
static int
load_binaries (void)
{
	return 0;
}
#endif

static const char*
symbol_for (uintptr_t code)
{
#ifdef HAVE_DLADDR
	void *ip = (void*)code;
	Dl_info di;
	if (dladdr (ip, &di)) {
		if (di.dli_sname)
			return di.dli_sname;
	} else {
	/*	char **names;
		names = backtrace_symbols (&ip, 1);
		if (names) {
			const char* p = names [0];
			g_free (names);
			return p;
		}
		*/
	}
#endif
	return NULL;
}

static void
dump_unmanaged_coderefs (void)
{
	int i;
	const char* last_symbol;
	uintptr_t addr, page_end;

	if (load_binaries ())
		return;
	for (i = 0; i < size_code_pages; ++i) {
		const char* sym;
		if (!code_pages [i] || code_pages [i] & 1)
			continue;
		last_symbol = NULL;
		addr = CPAGE_ADDR (code_pages [i]);
		page_end = addr + CPAGE_SIZE;
		code_pages [i] |= 1;
		/* we dump the symbols for the whole page */
		for (; addr < page_end; addr += 16) {
			sym = symbol_for (addr);
			if (sym && sym == last_symbol)
				continue;
			last_symbol = sym;
			if (!sym)
				continue;
			dump_usym (sym, addr, 0); /* let's not guess the size */
		}
	}
}

static void
counters_add_agent (MonoCounter *counter)
{
	if (InterlockedRead (&log_profiler.in_shutdown))
		return;

	MonoCounterAgent *agent, *item;

	mono_os_mutex_lock (&log_profiler.counters_mutex);

	for (agent = log_profiler.counters; agent; agent = agent->next) {
		if (agent->counter == counter) {
			agent->value_size = 0;
			if (agent->value) {
				g_free (agent->value);
				agent->value = NULL;
			}
			goto done;
		}
	}

	agent = (MonoCounterAgent *) g_malloc (sizeof (MonoCounterAgent));
	agent->counter = counter;
	agent->value = NULL;
	agent->value_size = 0;
	agent->index = log_profiler.counters_index++;
	agent->emitted = FALSE;
	agent->next = NULL;

	if (!log_profiler.counters) {
		log_profiler.counters = agent;
	} else {
		item = log_profiler.counters;
		while (item->next)
			item = item->next;
		item->next = agent;
	}

done:
	mono_os_mutex_unlock (&log_profiler.counters_mutex);
}

static mono_bool
counters_init_foreach_callback (MonoCounter *counter, gpointer data)
{
	counters_add_agent (counter);
	return TRUE;
}

static void
counters_init (void)
{
	mono_os_mutex_init (&log_profiler.counters_mutex);

	log_profiler.counters_index = 1;

	mono_counters_on_register (&counters_add_agent);
	mono_counters_foreach (counters_init_foreach_callback, NULL);
}

static void
counters_emit (void)
{
	MonoCounterAgent *agent;
	int len = 0;
	int size =
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* len */
	;

	mono_os_mutex_lock (&log_profiler.counters_mutex);

	for (agent = log_profiler.counters; agent; agent = agent->next) {
		if (agent->emitted)
			continue;

		size +=
			LEB128_SIZE /* section */ +
			strlen (mono_counter_get_name (agent->counter)) + 1 /* name */ +
			BYTE_SIZE /* type */ +
			BYTE_SIZE /* unit */ +
			BYTE_SIZE /* variance */ +
			LEB128_SIZE /* index */
		;

		len++;
	}

	if (!len)
		goto done;

	ENTER_LOG (&counter_descriptors_ctr, logbuffer, size);

	emit_event (logbuffer, TYPE_SAMPLE_COUNTERS_DESC | TYPE_SAMPLE);
	emit_value (logbuffer, len);

	for (agent = log_profiler.counters; agent; agent = agent->next) {
		const char *name;

		if (agent->emitted)
			continue;

		name = mono_counter_get_name (agent->counter);
		emit_value (logbuffer, mono_counter_get_section (agent->counter));
		emit_string (logbuffer, name, strlen (name) + 1);
		emit_byte (logbuffer, mono_counter_get_type (agent->counter));
		emit_byte (logbuffer, mono_counter_get_unit (agent->counter));
		emit_byte (logbuffer, mono_counter_get_variance (agent->counter));
		emit_value (logbuffer, agent->index);

		agent->emitted = TRUE;
	}

	EXIT_LOG;

done:
	mono_os_mutex_unlock (&log_profiler.counters_mutex);
}

static void
counters_sample (uint64_t timestamp)
{
	MonoCounterAgent *agent;
	MonoCounter *counter;
	int type;
	int buffer_size;
	void *buffer;
	int size;

	counters_emit ();

	buffer_size = 8;
	buffer = g_calloc (1, buffer_size);

	mono_os_mutex_lock (&log_profiler.counters_mutex);

	size =
		EVENT_SIZE /* event */
	;

	for (agent = log_profiler.counters; agent; agent = agent->next) {
		size +=
			LEB128_SIZE /* index */ +
			BYTE_SIZE /* type */ +
			mono_counter_get_size (agent->counter) /* value */
		;
	}

	size +=
		LEB128_SIZE /* stop marker */
	;

	ENTER_LOG (&counter_samples_ctr, logbuffer, size);

	emit_event_time (logbuffer, TYPE_SAMPLE_COUNTERS | TYPE_SAMPLE, timestamp);

	for (agent = log_profiler.counters; agent; agent = agent->next) {
		size_t size;

		counter = agent->counter;

		size = mono_counter_get_size (counter);

		if (size > buffer_size) {
			buffer_size = size;
			buffer = g_realloc (buffer, buffer_size);
		}

		memset (buffer, 0, buffer_size);

		g_assert (mono_counters_sample (counter, buffer, size));

		type = mono_counter_get_type (counter);

		if (!agent->value) {
			agent->value = g_calloc (1, size);
			agent->value_size = size;
		} else {
			if (type == MONO_COUNTER_STRING) {
				if (strcmp (agent->value, buffer) == 0)
					continue;
			} else {
				if (agent->value_size == size && memcmp (agent->value, buffer, size) == 0)
					continue;
			}
		}

		emit_uvalue (logbuffer, agent->index);
		emit_byte (logbuffer, type);
		switch (type) {
		case MONO_COUNTER_INT:
#if SIZEOF_VOID_P == 4
		case MONO_COUNTER_WORD:
#endif
			emit_svalue (logbuffer, *(int*)buffer - *(int*)agent->value);
			break;
		case MONO_COUNTER_UINT:
			emit_uvalue (logbuffer, *(guint*)buffer - *(guint*)agent->value);
			break;
		case MONO_COUNTER_TIME_INTERVAL:
		case MONO_COUNTER_LONG:
#if SIZEOF_VOID_P == 8
		case MONO_COUNTER_WORD:
#endif
			emit_svalue (logbuffer, *(gint64*)buffer - *(gint64*)agent->value);
			break;
		case MONO_COUNTER_ULONG:
			emit_uvalue (logbuffer, *(guint64*)buffer - *(guint64*)agent->value);
			break;
		case MONO_COUNTER_DOUBLE:
			emit_double (logbuffer, *(double*)buffer);
			break;
		case MONO_COUNTER_STRING:
			if (size == 0) {
				emit_byte (logbuffer, 0);
			} else {
				emit_byte (logbuffer, 1);
				emit_string (logbuffer, (char*)buffer, size);
			}
			break;
		default:
			g_assert_not_reached ();
		}

		if (type == MONO_COUNTER_STRING && size > agent->value_size) {
			agent->value = g_realloc (agent->value, size);
			agent->value_size = size;
		}

		if (size > 0)
			memcpy (agent->value, buffer, size);
	}
	g_free (buffer);

	emit_value (logbuffer, 0);

	EXIT_LOG;

	mono_os_mutex_unlock (&log_profiler.counters_mutex);
}

static void
perfcounters_emit (void)
{
	PerfCounterAgent *pcagent;
	int len = 0;
	int size =
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* len */
	;

	for (pcagent = log_profiler.perfcounters; pcagent; pcagent = pcagent->next) {
		if (pcagent->emitted)
			continue;

		size +=
			LEB128_SIZE /* section */ +
			strlen (pcagent->category_name) + 1 /* category name */ +
			strlen (pcagent->name) + 1 /* name */ +
			BYTE_SIZE /* type */ +
			BYTE_SIZE /* unit */ +
			BYTE_SIZE /* variance */ +
			LEB128_SIZE /* index */
		;

		len++;
	}

	if (!len)
		return;

	ENTER_LOG (&perfcounter_descriptors_ctr, logbuffer, size);

	emit_event (logbuffer, TYPE_SAMPLE_COUNTERS_DESC | TYPE_SAMPLE);
	emit_value (logbuffer, len);

	for (pcagent = log_profiler.perfcounters; pcagent; pcagent = pcagent->next) {
		if (pcagent->emitted)
			continue;

		emit_value (logbuffer, MONO_COUNTER_PERFCOUNTERS);
		emit_string (logbuffer, pcagent->category_name, strlen (pcagent->category_name) + 1);
		emit_string (logbuffer, pcagent->name, strlen (pcagent->name) + 1);
		emit_byte (logbuffer, MONO_COUNTER_LONG);
		emit_byte (logbuffer, MONO_COUNTER_RAW);
		emit_byte (logbuffer, MONO_COUNTER_VARIABLE);
		emit_value (logbuffer, pcagent->index);

		pcagent->emitted = TRUE;
	}

	EXIT_LOG;
}

static gboolean
perfcounters_foreach (char *category_name, char *name, unsigned char type, gint64 value, gpointer user_data)
{
	PerfCounterAgent *pcagent;

	for (pcagent = log_profiler.perfcounters; pcagent; pcagent = pcagent->next) {
		if (strcmp (pcagent->category_name, category_name) != 0 || strcmp (pcagent->name, name) != 0)
			continue;
		if (pcagent->value == value)
			return TRUE;

		pcagent->value = value;
		pcagent->updated = TRUE;
		pcagent->deleted = FALSE;
		return TRUE;
	}

	pcagent = g_new0 (PerfCounterAgent, 1);
	pcagent->next = log_profiler.perfcounters;
	pcagent->index = log_profiler.counters_index++;
	pcagent->category_name = g_strdup (category_name);
	pcagent->name = g_strdup (name);
	pcagent->value = value;
	pcagent->emitted = FALSE;
	pcagent->updated = TRUE;
	pcagent->deleted = FALSE;

	log_profiler.perfcounters = pcagent;

	return TRUE;
}

static void
perfcounters_sample (uint64_t timestamp)
{
	PerfCounterAgent *pcagent;
	int len = 0;
	int size;

	mono_os_mutex_lock (&log_profiler.counters_mutex);

	/* mark all perfcounters as deleted, foreach will unmark them as necessary */
	for (pcagent = log_profiler.perfcounters; pcagent; pcagent = pcagent->next)
		pcagent->deleted = TRUE;

	mono_perfcounter_foreach (perfcounters_foreach, NULL);

	perfcounters_emit ();

	size =
		EVENT_SIZE /* event */
	;

	for (pcagent = log_profiler.perfcounters; pcagent; pcagent = pcagent->next) {
		if (pcagent->deleted || !pcagent->updated)
			continue;

		size +=
			LEB128_SIZE /* index */ +
			BYTE_SIZE /* type */ +
			LEB128_SIZE /* value */
		;

		len++;
	}

	if (!len)
		goto done;

	size +=
		LEB128_SIZE /* stop marker */
	;

	ENTER_LOG (&perfcounter_samples_ctr, logbuffer, size);

	emit_event_time (logbuffer, TYPE_SAMPLE_COUNTERS | TYPE_SAMPLE, timestamp);

	for (pcagent = log_profiler.perfcounters; pcagent; pcagent = pcagent->next) {
		if (pcagent->deleted || !pcagent->updated)
			continue;
		emit_uvalue (logbuffer, pcagent->index);
		emit_byte (logbuffer, MONO_COUNTER_LONG);
		emit_svalue (logbuffer, pcagent->value);

		pcagent->updated = FALSE;
	}

	emit_value (logbuffer, 0);

	EXIT_LOG;

done:
	mono_os_mutex_unlock (&log_profiler.counters_mutex);
}

static void
counters_and_perfcounters_sample (void)
{
	uint64_t now = current_time ();

	counters_sample (now);
	perfcounters_sample (now);
}

typedef struct {
	MonoLockFreeQueueNode node;
	MonoMethod *method;
} MethodNode;

typedef struct {
	int offset;
	int counter;
	char *filename;
	int line;
	int column;
} CoverageEntry;

static void
free_coverage_entry (gpointer data, gpointer userdata)
{
	CoverageEntry *entry = (CoverageEntry *)data;
	g_free (entry->filename);
	g_free (entry);
}

static void
obtain_coverage_for_method (MonoProfiler *prof, const MonoProfilerCoverageData *entry)
{
	int offset = entry->il_offset - log_profiler.coverage_previous_offset;
	CoverageEntry *e = g_new (CoverageEntry, 1);

	log_profiler.coverage_previous_offset = entry->il_offset;

	e->offset = offset;
	e->counter = entry->counter;
	e->filename = g_strdup(entry->file_name ? entry->file_name : "");
	e->line = entry->line;
	e->column = entry->column;

	g_ptr_array_add (log_profiler.coverage_data, e);
}

static char *
parse_generic_type_names(char *name)
{
	char *new_name, *ret;
	int within_generic_declaration = 0, generic_members = 1;

	if (name == NULL || *name == '\0')
		return g_strdup ("");

	if (!(ret = new_name = (char *) g_calloc (strlen (name) * 4 + 1, sizeof (char))))
		return NULL;

	do {
		switch (*name) {
			case '<':
				within_generic_declaration = 1;
				break;

			case '>':
				within_generic_declaration = 0;

				if (*(name - 1) != '<') {
					*new_name++ = '`';
					*new_name++ = '0' + generic_members;
				} else {
					memcpy (new_name, "&lt;&gt;", 8);
					new_name += 8;
				}

				generic_members = 0;
				break;

			case ',':
				generic_members++;
				break;

			default:
				if (!within_generic_declaration)
					*new_name++ = *name;

				break;
		}
	} while (*name++);

	return ret;
}

static void
build_method_buffer (gpointer key, gpointer value, gpointer userdata)
{
	MonoMethod *method = (MonoMethod *)value;
	MonoClass *klass;
	MonoImage *image;
	char *class_name;
	const char *image_name, *method_name, *sig, *first_filename;
	guint i;

	log_profiler.coverage_previous_offset = 0;
	log_profiler.coverage_data = g_ptr_array_new ();

	mono_profiler_get_coverage_data (log_profiler.handle, method, obtain_coverage_for_method);

	klass = mono_method_get_class (method);
	image = mono_class_get_image (klass);
	image_name = mono_image_get_name (image);

	sig = mono_signature_get_desc (mono_method_signature (method), TRUE);
	class_name = parse_generic_type_names (mono_type_get_name (mono_class_get_type (klass)));
	method_name = mono_method_get_name (method);

	if (log_profiler.coverage_data->len != 0) {
		CoverageEntry *entry = (CoverageEntry *)log_profiler.coverage_data->pdata[0];
		first_filename = entry->filename ? entry->filename : "";
	} else
		first_filename = "";

	image_name = image_name ? image_name : "";
	sig = sig ? sig : "";
	method_name = method_name ? method_name : "";

	ENTER_LOG (&coverage_methods_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		strlen (image_name) + 1 /* image name */ +
		strlen (class_name) + 1 /* class name */ +
		strlen (method_name) + 1 /* method name */ +
		strlen (sig) + 1 /* signature */ +
		strlen (first_filename) + 1 /* first file name */ +
		LEB128_SIZE /* token */ +
		LEB128_SIZE /* method id */ +
		LEB128_SIZE /* entries */
	);

	emit_event (logbuffer, TYPE_COVERAGE_METHOD | TYPE_COVERAGE);
	emit_string (logbuffer, image_name, strlen (image_name) + 1);
	emit_string (logbuffer, class_name, strlen (class_name) + 1);
	emit_string (logbuffer, method_name, strlen (method_name) + 1);
	emit_string (logbuffer, sig, strlen (sig) + 1);
	emit_string (logbuffer, first_filename, strlen (first_filename) + 1);

	emit_uvalue (logbuffer, mono_method_get_token (method));
	emit_uvalue (logbuffer, log_profiler.coverage_method_id);
	emit_value (logbuffer, log_profiler.coverage_data->len);

	EXIT_LOG;

	for (i = 0; i < log_profiler.coverage_data->len; i++) {
		CoverageEntry *entry = (CoverageEntry *)log_profiler.coverage_data->pdata[i];

		ENTER_LOG (&coverage_statements_ctr, logbuffer,
			EVENT_SIZE /* event */ +
			LEB128_SIZE /* method id */ +
			LEB128_SIZE /* offset */ +
			LEB128_SIZE /* counter */ +
			LEB128_SIZE /* line */ +
			LEB128_SIZE /* column */
		);

		emit_event (logbuffer, TYPE_COVERAGE_STATEMENT | TYPE_COVERAGE);
		emit_uvalue (logbuffer, log_profiler.coverage_method_id);
		emit_uvalue (logbuffer, entry->offset);
		emit_uvalue (logbuffer, entry->counter);
		emit_uvalue (logbuffer, entry->line);
		emit_uvalue (logbuffer, entry->column);

		EXIT_LOG;
	}

	log_profiler.coverage_method_id++;

	g_free (class_name);

	g_ptr_array_foreach (log_profiler.coverage_data, free_coverage_entry, NULL);
	g_ptr_array_free (log_profiler.coverage_data, TRUE);
}

/* This empties the queue */
static guint
count_queue (MonoLockFreeQueue *queue)
{
	MonoLockFreeQueueNode *node;
	guint count = 0;

	while ((node = mono_lock_free_queue_dequeue (queue))) {
		count++;
		mono_thread_hazardous_try_free (node, g_free);
	}

	return count;
}

static void
build_class_buffer (gpointer key, gpointer value, gpointer userdata)
{
	MonoClass *klass = (MonoClass *)key;
	MonoLockFreeQueue *class_methods = (MonoLockFreeQueue *)value;
	MonoImage *image;
	char *class_name;
	const char *assembly_name;
	int number_of_methods, partially_covered;
	guint fully_covered;

	image = mono_class_get_image (klass);
	assembly_name = mono_image_get_name (image);
	class_name = mono_type_get_name (mono_class_get_type (klass));

	assembly_name = assembly_name ? assembly_name : "";
	number_of_methods = mono_class_num_methods (klass);
	fully_covered = count_queue (class_methods);
	/* We don't handle partial covered yet */
	partially_covered = 0;

	ENTER_LOG (&coverage_classes_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		strlen (assembly_name) + 1 /* assembly name */ +
		strlen (class_name) + 1 /* class name */ +
		LEB128_SIZE /* no. methods */ +
		LEB128_SIZE /* fully covered */ +
		LEB128_SIZE /* partially covered */
	);

	emit_event (logbuffer, TYPE_COVERAGE_CLASS | TYPE_COVERAGE);
	emit_string (logbuffer, assembly_name, strlen (assembly_name) + 1);
	emit_string (logbuffer, class_name, strlen (class_name) + 1);
	emit_uvalue (logbuffer, number_of_methods);
	emit_uvalue (logbuffer, fully_covered);
	emit_uvalue (logbuffer, partially_covered);

	EXIT_LOG;

	g_free (class_name);
}

static void
get_coverage_for_image (MonoImage *image, int *number_of_methods, guint *fully_covered, int *partially_covered)
{
	MonoLockFreeQueue *image_methods = (MonoLockFreeQueue *)mono_conc_hashtable_lookup (log_profiler.coverage_image_to_methods, image);

	*number_of_methods = mono_image_get_table_rows (image, MONO_TABLE_METHOD);
	if (image_methods)
		*fully_covered = count_queue (image_methods);
	else
		*fully_covered = 0;

	// FIXME: We don't handle partially covered yet.
	*partially_covered = 0;
}

static void
build_assembly_buffer (gpointer key, gpointer value, gpointer userdata)
{
	MonoAssembly *assembly = (MonoAssembly *)value;
	MonoImage *image = mono_assembly_get_image (assembly);
	const char *name, *guid, *filename;
	int number_of_methods = 0, partially_covered = 0;
	guint fully_covered = 0;

	name = mono_image_get_name (image);
	guid = mono_image_get_guid (image);
	filename = mono_image_get_filename (image);

	name = name ? name : "";
	guid = guid ? guid : "";
	filename = filename ? filename : "";

	get_coverage_for_image (image, &number_of_methods, &fully_covered, &partially_covered);

	ENTER_LOG (&coverage_assemblies_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		strlen (name) + 1 /* name */ +
		strlen (guid) + 1 /* guid */ +
		strlen (filename) + 1 /* file name */ +
		LEB128_SIZE /* no. methods */ +
		LEB128_SIZE /* fully covered */ +
		LEB128_SIZE /* partially covered */
	);

	emit_event (logbuffer, TYPE_COVERAGE_ASSEMBLY | TYPE_COVERAGE);
	emit_string (logbuffer, name, strlen (name) + 1);
	emit_string (logbuffer, guid, strlen (guid) + 1);
	emit_string (logbuffer, filename, strlen (filename) + 1);
	emit_uvalue (logbuffer, number_of_methods);
	emit_uvalue (logbuffer, fully_covered);
	emit_uvalue (logbuffer, partially_covered);

	EXIT_LOG;
}

static void
dump_coverage (void)
{
	mono_os_mutex_lock (&log_profiler.coverage_mutex);
	mono_conc_hashtable_foreach (log_profiler.coverage_assemblies, build_assembly_buffer, NULL);
	mono_conc_hashtable_foreach (log_profiler.coverage_classes, build_class_buffer, NULL);
	mono_conc_hashtable_foreach (log_profiler.coverage_methods, build_method_buffer, NULL);
	mono_os_mutex_unlock (&log_profiler.coverage_mutex);
}

static MonoLockFreeQueueNode *
create_method_node (MonoMethod *method)
{
	MethodNode *node = (MethodNode *) g_malloc (sizeof (MethodNode));
	mono_lock_free_queue_node_init ((MonoLockFreeQueueNode *) node, FALSE);
	node->method = method;

	return (MonoLockFreeQueueNode *) node;
}

static gboolean
coverage_filter (MonoProfiler *prof, MonoMethod *method)
{
	MonoError error;
	MonoClass *klass;
	MonoImage *image;
	MonoAssembly *assembly;
	MonoMethodHeader *header;
	guint32 iflags, flags, code_size;
	char *fqn, *classname;
	gboolean has_positive, found;
	MonoLockFreeQueue *image_methods, *class_methods;
	MonoLockFreeQueueNode *node;

	flags = mono_method_get_flags (method, &iflags);
	if ((iflags & 0x1000 /*METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL*/) ||
	    (flags & 0x2000 /*METHOD_ATTRIBUTE_PINVOKE_IMPL*/))
		return FALSE;

	// Don't need to do anything else if we're already tracking this method
	if (mono_conc_hashtable_lookup (log_profiler.coverage_methods, method))
		return TRUE;

	klass = mono_method_get_class (method);
	image = mono_class_get_image (klass);

	// Don't handle coverage for the core assemblies
	if (mono_conc_hashtable_lookup (log_profiler.coverage_suppressed_assemblies, (gpointer) mono_image_get_name (image)) != NULL)
		return FALSE;

	if (prof->coverage_filters) {
		/* Check already filtered classes first */
		if (mono_conc_hashtable_lookup (log_profiler.coverage_filtered_classes, klass))
			return FALSE;

		classname = mono_type_get_name (mono_class_get_type (klass));

		fqn = g_strdup_printf ("[%s]%s", mono_image_get_name (image), classname);

		// Check positive filters first
		has_positive = FALSE;
		found = FALSE;
		for (guint i = 0; i < prof->coverage_filters->len; ++i) {
			char *filter = (char *)g_ptr_array_index (prof->coverage_filters, i);

			if (filter [0] == '+') {
				filter = &filter [1];

				if (strstr (fqn, filter) != NULL)
					found = TRUE;

				has_positive = TRUE;
			}
		}

		if (has_positive && !found) {
			mono_os_mutex_lock (&log_profiler.coverage_mutex);
			mono_conc_hashtable_insert (log_profiler.coverage_filtered_classes, klass, klass);
			mono_os_mutex_unlock (&log_profiler.coverage_mutex);
			g_free (fqn);
			g_free (classname);

			return FALSE;
		}

		for (guint i = 0; i < prof->coverage_filters->len; ++i) {
			// FIXME: Is substring search sufficient?
			char *filter = (char *)g_ptr_array_index (prof->coverage_filters, i);
			if (filter [0] == '+')
				continue;

			// Skip '-'
			filter = &filter [1];

			if (strstr (fqn, filter) != NULL) {
				mono_os_mutex_lock (&log_profiler.coverage_mutex);
				mono_conc_hashtable_insert (log_profiler.coverage_filtered_classes, klass, klass);
				mono_os_mutex_unlock (&log_profiler.coverage_mutex);
				g_free (fqn);
				g_free (classname);

				return FALSE;
			}
		}

		g_free (fqn);
		g_free (classname);
	}

	header = mono_method_get_header_checked (method, &error);
	mono_error_cleanup (&error);

	mono_method_header_get_code (header, &code_size, NULL);

	assembly = mono_image_get_assembly (image);

	// Need to keep the assemblies around for as long as they are kept in the hashtable
	// Nunit, for example, has a habit of unloading them before the coverage statistics are
	// generated causing a crash. See https://bugzilla.xamarin.com/show_bug.cgi?id=39325
	mono_assembly_addref (assembly);

	mono_os_mutex_lock (&log_profiler.coverage_mutex);
	mono_conc_hashtable_insert (log_profiler.coverage_methods, method, method);
	mono_conc_hashtable_insert (log_profiler.coverage_assemblies, assembly, assembly);
	mono_os_mutex_unlock (&log_profiler.coverage_mutex);

	image_methods = (MonoLockFreeQueue *)mono_conc_hashtable_lookup (log_profiler.coverage_image_to_methods, image);

	if (image_methods == NULL) {
		image_methods = (MonoLockFreeQueue *) g_malloc (sizeof (MonoLockFreeQueue));
		mono_lock_free_queue_init (image_methods);
		mono_os_mutex_lock (&log_profiler.coverage_mutex);
		mono_conc_hashtable_insert (log_profiler.coverage_image_to_methods, image, image_methods);
		mono_os_mutex_unlock (&log_profiler.coverage_mutex);
	}

	node = create_method_node (method);
	mono_lock_free_queue_enqueue (image_methods, node);

	class_methods = (MonoLockFreeQueue *)mono_conc_hashtable_lookup (log_profiler.coverage_classes, klass);

	if (class_methods == NULL) {
		class_methods = (MonoLockFreeQueue *) g_malloc (sizeof (MonoLockFreeQueue));
		mono_lock_free_queue_init (class_methods);
		mono_os_mutex_lock (&log_profiler.coverage_mutex);
		mono_conc_hashtable_insert (log_profiler.coverage_classes, klass, class_methods);
		mono_os_mutex_unlock (&log_profiler.coverage_mutex);
	}

	node = create_method_node (method);
	mono_lock_free_queue_enqueue (class_methods, node);

	return TRUE;
}

#define LINE_BUFFER_SIZE 4096
/* Max file limit of 128KB */
#define MAX_FILE_SIZE 128 * 1024
static char *
get_file_content (FILE *stream)
{
	char *buffer;
	ssize_t bytes_read;
	long filesize;
	int res, offset = 0;

	res = fseek (stream, 0, SEEK_END);
	if (res < 0)
	  return NULL;

	filesize = ftell (stream);
	if (filesize < 0)
	  return NULL;

	res = fseek (stream, 0, SEEK_SET);
	if (res < 0)
	  return NULL;

	if (filesize > MAX_FILE_SIZE)
	  return NULL;

	buffer = (char *) g_malloc ((filesize + 1) * sizeof (char));
	while ((bytes_read = fread (buffer + offset, 1, LINE_BUFFER_SIZE, stream)) > 0)
		offset += bytes_read;

	/* NULL terminate our buffer */
	buffer[filesize] = '\0';
	return buffer;
}

static char *
get_next_line (char *contents, char **next_start)
{
	char *p = contents;

	if (p == NULL || *p == '\0') {
		*next_start = NULL;
		return NULL;
	}

	while (*p != '\n' && *p != '\0')
		p++;

	if (*p == '\n') {
		*p = '\0';
		*next_start = p + 1;
	} else
		*next_start = NULL;

	return contents;
}

static void
init_suppressed_assemblies (void)
{
	char *content;
	char *line;
	FILE *sa_file;

	log_profiler.coverage_suppressed_assemblies = mono_conc_hashtable_new (g_str_hash, g_str_equal);
	sa_file = fopen (SUPPRESSION_DIR "/mono-profiler-log.suppression", "r");
	if (sa_file == NULL)
		return;

	/* Don't need to free @content as it is referred to by the lines stored in @suppressed_assemblies */
	content = get_file_content (sa_file);
	if (content == NULL)
		g_error ("mono-profiler-log.suppression is greater than 128kb - aborting.");

	while ((line = get_next_line (content, &content))) {
		line = g_strchomp (g_strchug (line));
		/* No locking needed as we're doing initialization */
		mono_conc_hashtable_insert (log_profiler.coverage_suppressed_assemblies, line, line);
	}

	fclose (sa_file);
}

static void
parse_cov_filter_file (GPtrArray *filters, const char *file)
{
	FILE *filter_file = fopen (file, "r");

	if (filter_file == NULL) {
		mono_profiler_printf_err ("Could not open coverage filter file '%s'.", file);
		return;
	}

	/* Don't need to free content as it is referred to by the lines stored in @filters */
	char *content = get_file_content (filter_file);

	if (content == NULL)
		mono_profiler_printf_err ("Coverage filter file '%s' is larger than 128kb - ignoring.", file);

	char *line;

	while ((line = get_next_line (content, &content)))
		g_ptr_array_add (filters, g_strchug (g_strchomp (line)));

	fclose (filter_file);
}

static void
coverage_init (void)
{
	mono_os_mutex_init (&log_profiler.coverage_mutex);
	log_profiler.coverage_methods = mono_conc_hashtable_new (NULL, NULL);
	log_profiler.coverage_assemblies = mono_conc_hashtable_new (NULL, NULL);
	log_profiler.coverage_classes = mono_conc_hashtable_new (NULL, NULL);
	log_profiler.coverage_filtered_classes = mono_conc_hashtable_new (NULL, NULL);
	log_profiler.coverage_image_to_methods = mono_conc_hashtable_new (NULL, NULL);
	init_suppressed_assemblies ();
}

static void
unref_coverage_assemblies (gpointer key, gpointer value, gpointer userdata)
{
	MonoAssembly *assembly = (MonoAssembly *)value;
	mono_assembly_close (assembly);
}

static void
free_sample_hit (gpointer p)
{
	mono_lock_free_free (p, SAMPLE_BLOCK_SIZE);
}

static void
cleanup_reusable_samples (void)
{
	SampleHit *sample;

	while ((sample = (SampleHit *) mono_lock_free_queue_dequeue (&log_profiler.sample_reuse_queue)))
		mono_thread_hazardous_try_free (sample, free_sample_hit);
}

static void
log_shutdown (MonoProfiler *prof)
{
	InterlockedWrite (&log_profiler.in_shutdown, 1);

	if (ENABLED (PROFLOG_COUNTER_EVENTS))
		counters_and_perfcounters_sample ();

	if (ENABLED (PROFLOG_CODE_COV_FEATURE))
		dump_coverage ();

	char c = 1;

	if (write (prof->pipes [1], &c, 1) != 1) {
		mono_profiler_printf_err ("Could not write to log profiler pipe: %s", strerror (errno));
		exit (1);
	}

	mono_native_thread_join (prof->helper_thread);

	mono_os_mutex_destroy (&log_profiler.counters_mutex);

	MonoCounterAgent *mc_next;

	for (MonoCounterAgent *cur = log_profiler.counters; cur; cur = mc_next) {
		mc_next = cur->next;
		g_free (cur);
	}

	PerfCounterAgent *pc_next;

	for (PerfCounterAgent *cur = log_profiler.perfcounters; cur; cur = pc_next) {
		pc_next = cur->next;
		g_free (cur);
	}

	/*
	 * Ensure that we empty the LLS completely, even if some nodes are
	 * not immediately removed upon calling mono_lls_remove (), by
	 * iterating until the head is NULL.
	 */
	while (log_profiler.profiler_thread_list.head) {
		MONO_LLS_FOREACH_SAFE (&log_profiler.profiler_thread_list, MonoProfilerThread, thread) {
			g_assert (thread->attached && "Why is a thread in the LLS not attached?");

			remove_thread (thread);
		} MONO_LLS_FOREACH_SAFE_END
	}

	/*
	 * Ensure that all threads have been freed, so that we don't miss any
	 * buffers when we shut down the writer thread below.
	 */
	mono_thread_hazardous_try_free_all ();

	InterlockedWrite (&prof->run_dumper_thread, 0);
	mono_os_sem_post (&prof->dumper_queue_sem);
	mono_native_thread_join (prof->dumper_thread);
	mono_os_sem_destroy (&prof->dumper_queue_sem);

	InterlockedWrite (&prof->run_writer_thread, 0);
	mono_os_sem_post (&prof->writer_queue_sem);
	mono_native_thread_join (prof->writer_thread);
	mono_os_sem_destroy (&prof->writer_queue_sem);

	/*
	 * Free all writer queue entries, and ensure that all sample hits will be
	 * added to the sample reuse queue.
	 */
	mono_thread_hazardous_try_free_all ();

	cleanup_reusable_samples ();

	/*
	 * Finally, make sure that all sample hits are freed. This should cover all
	 * hazardous data from the profiler. We can now be sure that the runtime
	 * won't later invoke free functions in the profiler library after it has
	 * been unloaded.
	 */
	mono_thread_hazardous_try_free_all ();

	gint32 state = InterlockedRead (&log_profiler.buffer_lock_state);

	g_assert (!(state & 0xFFFF) && "Why is the reader count still non-zero?");
	g_assert (!(state >> 16) && "Why is the exclusive lock still held?");

#if defined (HAVE_SYS_ZLIB)
	if (prof->gzfile)
		gzclose (prof->gzfile);
#endif
	if (prof->pipe_output)
		pclose (prof->file);
	else
		fclose (prof->file);

	mono_conc_hashtable_destroy (prof->method_table);
	mono_os_mutex_destroy (&prof->method_table_mutex);

	if (ENABLED (PROFLOG_CODE_COV_FEATURE)) {
		mono_os_mutex_lock (&log_profiler.coverage_mutex);
		mono_conc_hashtable_foreach (log_profiler.coverage_assemblies, unref_coverage_assemblies, NULL);
		mono_os_mutex_unlock (&log_profiler.coverage_mutex);

		mono_conc_hashtable_destroy (log_profiler.coverage_methods);
		mono_conc_hashtable_destroy (log_profiler.coverage_assemblies);
		mono_conc_hashtable_destroy (log_profiler.coverage_classes);
		mono_conc_hashtable_destroy (log_profiler.coverage_filtered_classes);

		mono_conc_hashtable_destroy (log_profiler.coverage_image_to_methods);
		mono_conc_hashtable_destroy (log_profiler.coverage_suppressed_assemblies);
		mono_os_mutex_destroy (&log_profiler.coverage_mutex);
	}

	PROF_TLS_FREE ();

	g_free (prof->args);
}

static char*
new_filename (const char* filename)
{
	time_t t = time (NULL);
	int pid = process_id ();
	char pid_buf [16];
	char time_buf [16];
	char *res, *d;
	const char *p;
	int count_dates = 0;
	int count_pids = 0;
	int s_date, s_pid;
	struct tm *ts;
	for (p = filename; *p; p++) {
		if (*p != '%')
			continue;
		p++;
		if (*p == 't')
			count_dates++;
		else if (*p == 'p')
			count_pids++;
		else if (*p == 0)
			break;
	}
	if (!count_dates && !count_pids)
		return pstrdup (filename);
	snprintf (pid_buf, sizeof (pid_buf), "%d", pid);
	ts = gmtime (&t);
	snprintf (time_buf, sizeof (time_buf), "%d%02d%02d%02d%02d%02d",
		1900 + ts->tm_year, 1 + ts->tm_mon, ts->tm_mday, ts->tm_hour, ts->tm_min, ts->tm_sec);
	s_date = strlen (time_buf);
	s_pid = strlen (pid_buf);
	d = res = (char *) g_malloc (strlen (filename) + s_date * count_dates + s_pid * count_pids);
	for (p = filename; *p; p++) {
		if (*p != '%') {
			*d++ = *p;
			continue;
		}
		p++;
		if (*p == 't') {
			strcpy (d, time_buf);
			d += s_date;
			continue;
		} else if (*p == 'p') {
			strcpy (d, pid_buf);
			d += s_pid;
			continue;
		} else if (*p == '%') {
			*d++ = '%';
			continue;
		} else if (*p == 0)
			break;
		*d++ = '%';
		*d++ = *p;
	}
	*d = 0;
	return res;
}

static void
add_to_fd_set (fd_set *set, int fd, int *max_fd)
{
	/*
	 * This should only trigger for the basic FDs (server socket, pipes) at
	 * startup if for some mysterious reason they're too large. In this case,
	 * the profiler really can't function, and we're better off printing an
	 * error and exiting.
	 */
	if (fd >= FD_SETSIZE) {
		mono_profiler_printf_err ("File descriptor is out of bounds for fd_set: %d", fd);
		exit (1);
	}

	FD_SET (fd, set);

	if (*max_fd < fd)
		*max_fd = fd;
}

static void *
helper_thread (void *arg)
{
	mono_threads_attach_tools_thread ();
	mono_native_thread_set_name (mono_native_thread_id_get (), "Profiler helper");

	MonoProfilerThread *thread = init_thread (FALSE);

	GArray *command_sockets = g_array_new (FALSE, FALSE, sizeof (int));

	while (1) {
		fd_set rfds;
		int max_fd = -1;

		FD_ZERO (&rfds);

		add_to_fd_set (&rfds, log_profiler.server_socket, &max_fd);
		add_to_fd_set (&rfds, log_profiler.pipes [0], &max_fd);

		for (gint i = 0; i < command_sockets->len; i++)
			add_to_fd_set (&rfds, g_array_index (command_sockets, int, i), &max_fd);

		struct timeval tv = { .tv_sec = 1, .tv_usec = 0 };

		// Sleep for 1sec or until a file descriptor has data.
		if (select (max_fd + 1, &rfds, NULL, NULL, &tv) == -1) {
			if (errno == EINTR)
				continue;

			mono_profiler_printf_err ("Could not poll in log profiler helper thread: %s", strerror (errno));
			exit (1);
		}

		if (ENABLED (PROFLOG_COUNTER_EVENTS))
			counters_and_perfcounters_sample ();

		buffer_lock_excl ();

		sync_point (SYNC_POINT_PERIODIC);

		buffer_unlock_excl ();

		// Are we shutting down?
		if (FD_ISSET (log_profiler.pipes [0], &rfds)) {
			char c;
			read (log_profiler.pipes [0], &c, 1);
			break;
		}

		for (gint i = 0; i < command_sockets->len; i++) {
			int fd = g_array_index (command_sockets, int, i);

			if (!FD_ISSET (fd, &rfds))
				continue;

			char buf [64];
			int len = read (fd, buf, sizeof (buf) - 1);

			if (len == -1)
				continue;

			if (!len) {
				// The other end disconnected.
				g_array_remove_index (command_sockets, i);
				close (fd);

				continue;
			}

			buf [len] = 0;

			if (!strcmp (buf, "heapshot\n") && log_config.hs_mode_ondemand) {
				// Rely on the finalization callback triggering a GC.
				log_profiler.heapshot_requested = TRUE;
				mono_gc_finalize_notify ();
			}
		}

		if (FD_ISSET (log_profiler.server_socket, &rfds)) {
			int fd = accept (log_profiler.server_socket, NULL, NULL);

			if (fd != -1) {
				if (fd >= FD_SETSIZE)
					close (fd);
				else
					g_array_append_val (command_sockets, fd);
			}
		}
	}

	for (gint i = 0; i < command_sockets->len; i++)
		close (g_array_index (command_sockets, int, i));

	g_array_free (command_sockets, TRUE);

	send_log_unsafe (FALSE);
	deinit_thread (thread);

	mono_thread_info_detach ();

	return NULL;
}

static void
start_helper_thread (void)
{
	if (pipe (log_profiler.pipes) == -1) {
		mono_profiler_printf_err ("Could not create log profiler pipe: %s", strerror (errno));
		exit (1);
	}

	log_profiler.server_socket = socket (PF_INET, SOCK_STREAM, 0);

	if (log_profiler.server_socket == -1) {
		mono_profiler_printf_err ("Could not create log profiler server socket: %s", strerror (errno));
		exit (1);
	}

	struct sockaddr_in server_address;

	memset (&server_address, 0, sizeof (server_address));
	server_address.sin_family = AF_INET;
	server_address.sin_addr.s_addr = INADDR_ANY;
	server_address.sin_port = htons (log_profiler.command_port);

	if (bind (log_profiler.server_socket, (struct sockaddr *) &server_address, sizeof (server_address)) == -1) {
		mono_profiler_printf_err ("Could not bind log profiler server socket on port %d: %s", log_profiler.command_port, strerror (errno));
		close (log_profiler.server_socket);
		exit (1);
	}

	if (listen (log_profiler.server_socket, 1) == -1) {
		mono_profiler_printf_err ("Could not listen on log profiler server socket: %s", strerror (errno));
		close (log_profiler.server_socket);
		exit (1);
	}

	socklen_t slen = sizeof (server_address);

	if (getsockname (log_profiler.server_socket, (struct sockaddr *) &server_address, &slen)) {
		mono_profiler_printf_err ("Could not retrieve assigned port for log profiler server socket: %s", strerror (errno));
		close (log_profiler.server_socket);
		exit (1);
	}

	log_profiler.command_port = ntohs (server_address.sin_port);

	if (!mono_native_thread_create (&log_profiler.helper_thread, helper_thread, NULL)) {
		mono_profiler_printf_err ("Could not start log profiler helper thread");
		close (log_profiler.server_socket);
		exit (1);
	}
}

static void
free_writer_entry (gpointer p)
{
	mono_lock_free_free (p, WRITER_ENTRY_BLOCK_SIZE);
}

static gboolean
handle_writer_queue_entry (void)
{
	WriterQueueEntry *entry;

	if ((entry = (WriterQueueEntry *) mono_lock_free_queue_dequeue (&log_profiler.writer_queue))) {
		if (!entry->methods)
			goto no_methods;

		gboolean wrote_methods = FALSE;

		/*
		 * Encode the method events in a temporary log buffer that we
		 * flush to disk before the main buffer, ensuring that all
		 * methods have metadata emitted before they're referenced.
		 *
		 * We use a 'proper' thread-local buffer for this as opposed
		 * to allocating and freeing a buffer by hand because the call
		 * to mono_method_full_name () below may trigger class load
		 * events when it retrieves the signature of the method. So a
		 * thread-local buffer needs to exist when such events occur.
		 */
		for (guint i = 0; i < entry->methods->len; i++) {
			MethodInfo *info = (MethodInfo *) g_ptr_array_index (entry->methods, i);

			if (mono_conc_hashtable_lookup (log_profiler.method_table, info->method))
				goto free_info; // This method already has metadata emitted.

			/*
			 * Other threads use this hash table to get a general
			 * idea of whether a method has already been emitted to
			 * the stream. Due to the way we add to this table, it
			 * can easily happen that multiple threads queue up the
			 * same methods, but that's OK since eventually all
			 * methods will be in this table and the thread-local
			 * method lists will just be empty for the rest of the
			 * app's lifetime.
			 */
			mono_os_mutex_lock (&log_profiler.method_table_mutex);
			mono_conc_hashtable_insert (log_profiler.method_table, info->method, info->method);
			mono_os_mutex_unlock (&log_profiler.method_table_mutex);

			char *name = mono_method_full_name (info->method, 1);
			int nlen = strlen (name) + 1;
			void *cstart = info->ji ? mono_jit_info_get_code_start (info->ji) : NULL;
			int csize = info->ji ? mono_jit_info_get_code_size (info->ji) : 0;

			ENTER_LOG (&method_jits_ctr, logbuffer,
				EVENT_SIZE /* event */ +
				LEB128_SIZE /* method */ +
				LEB128_SIZE /* start */ +
				LEB128_SIZE /* size */ +
				nlen /* name */
			);

			emit_event_time (logbuffer, TYPE_JIT | TYPE_METHOD, info->time);
			emit_method_inner (logbuffer, info->method);
			emit_ptr (logbuffer, cstart);
			emit_value (logbuffer, csize);

			memcpy (logbuffer->cursor, name, nlen);
			logbuffer->cursor += nlen;

			EXIT_LOG_EXPLICIT (NO_SEND);

			mono_free (name);

			wrote_methods = TRUE;

		free_info:
			g_free (info);
		}

		g_ptr_array_free (entry->methods, TRUE);

		if (wrote_methods) {
			MonoProfilerThread *thread = PROF_TLS_GET ();

			dump_buffer_threadless (thread->buffer);
			init_buffer_state (thread);
		}

	no_methods:
		dump_buffer (entry->buffer);

		mono_thread_hazardous_try_free (entry, free_writer_entry);

		return TRUE;
	}

	return FALSE;
}

static void *
writer_thread (void *arg)
{
	mono_threads_attach_tools_thread ();
	mono_native_thread_set_name (mono_native_thread_id_get (), "Profiler writer");

	dump_header ();

	MonoProfilerThread *thread = init_thread (FALSE);

	while (InterlockedRead (&log_profiler.run_writer_thread)) {
		mono_os_sem_wait (&log_profiler.writer_queue_sem, MONO_SEM_FLAGS_NONE);
		handle_writer_queue_entry ();
	}

	/* Drain any remaining entries on shutdown. */
	while (handle_writer_queue_entry ());

	free_buffer (thread->buffer, thread->buffer->size);
	deinit_thread (thread);

	mono_thread_info_detach ();

	return NULL;
}

static void
start_writer_thread (void)
{
	InterlockedWrite (&log_profiler.run_writer_thread, 1);

	if (!mono_native_thread_create (&log_profiler.writer_thread, writer_thread, NULL)) {
		mono_profiler_printf_err ("Could not start log profiler writer thread");
		exit (1);
	}
}

static void
reuse_sample_hit (gpointer p)
{
	SampleHit *sample = p;

	mono_lock_free_queue_node_unpoison (&sample->node);
	mono_lock_free_queue_enqueue (&log_profiler.sample_reuse_queue, &sample->node);
}

static gboolean
handle_dumper_queue_entry (void)
{
	SampleHit *sample;

	if ((sample = (SampleHit *) mono_lock_free_queue_dequeue (&log_profiler.dumper_queue))) {
		for (int i = 0; i < sample->count; ++i) {
			MonoMethod *method = sample->frames [i].method;
			MonoDomain *domain = sample->frames [i].domain;
			void *address = sample->frames [i].base_address;

			if (!method) {
				g_assert (domain && "What happened to the domain pointer?");
				g_assert (address && "What happened to the instruction pointer?");

				MonoJitInfo *ji = mono_jit_info_table_find (domain, (char *) address);

				if (ji)
					sample->frames [i].method = mono_jit_info_get_method (ji);
			}
		}

		ENTER_LOG (&sample_hits_ctr, logbuffer,
			EVENT_SIZE /* event */ +
			LEB128_SIZE /* tid */ +
			LEB128_SIZE /* count */ +
			1 * (
				LEB128_SIZE /* ip */
			) +
			LEB128_SIZE /* managed count */ +
			sample->count * (
				LEB128_SIZE /* method */
			)
		);

		emit_event_time (logbuffer, TYPE_SAMPLE | TYPE_SAMPLE_HIT, sample->time);
		emit_ptr (logbuffer, (void *) sample->tid);
		emit_value (logbuffer, 1);

		// TODO: Actual native unwinding.
		for (int i = 0; i < 1; ++i) {
			emit_ptr (logbuffer, sample->ip);
			add_code_pointer ((uintptr_t) sample->ip);
		}

		/* new in data version 6 */
		emit_uvalue (logbuffer, sample->count);

		for (int i = 0; i < sample->count; ++i)
			emit_method (logbuffer, sample->frames [i].method);

		EXIT_LOG;

		mono_thread_hazardous_try_free (sample, reuse_sample_hit);

		dump_unmanaged_coderefs ();
	}

	return FALSE;
}

static void *
dumper_thread (void *arg)
{
	mono_threads_attach_tools_thread ();
	mono_native_thread_set_name (mono_native_thread_id_get (), "Profiler dumper");

	MonoProfilerThread *thread = init_thread (FALSE);

	while (InterlockedRead (&log_profiler.run_dumper_thread)) {
		/*
		 * Flush samples every second so it doesn't seem like the profiler is
		 * not working if the program is mostly idle.
		 */
		if (mono_os_sem_timedwait (&log_profiler.dumper_queue_sem, 1000, MONO_SEM_FLAGS_NONE) == MONO_SEM_TIMEDWAIT_RET_TIMEDOUT)
			send_log_unsafe (FALSE);

		handle_dumper_queue_entry ();
	}

	/* Drain any remaining entries on shutdown. */
	while (handle_dumper_queue_entry ());

	send_log_unsafe (FALSE);
	deinit_thread (thread);

	mono_thread_info_detach ();

	return NULL;
}

static void
start_dumper_thread (void)
{
	InterlockedWrite (&log_profiler.run_dumper_thread, 1);

	if (!mono_native_thread_create (&log_profiler.dumper_thread, dumper_thread, NULL)) {
		mono_profiler_printf_err ("Could not start log profiler dumper thread");
		exit (1);
	}
}

static void
register_counter (const char *name, gint32 *counter)
{
	mono_counters_register (name, MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, counter);
}

static void
runtime_initialized (MonoProfiler *profiler)
{
	InterlockedWrite (&log_profiler.runtime_inited, 1);

	register_counter ("Sample events allocated", &sample_allocations_ctr);
	register_counter ("Log buffers allocated", &buffer_allocations_ctr);

	register_counter ("Event: Sync points", &sync_points_ctr);
	register_counter ("Event: Heap objects", &heap_objects_ctr);
	register_counter ("Event: Heap starts", &heap_starts_ctr);
	register_counter ("Event: Heap ends", &heap_ends_ctr);
	register_counter ("Event: Heap roots", &heap_roots_ctr);
	register_counter ("Event: GC events", &gc_events_ctr);
	register_counter ("Event: GC resizes", &gc_resizes_ctr);
	register_counter ("Event: GC allocations", &gc_allocs_ctr);
	register_counter ("Event: GC moves", &gc_moves_ctr);
	register_counter ("Event: GC handle creations", &gc_handle_creations_ctr);
	register_counter ("Event: GC handle deletions", &gc_handle_deletions_ctr);
	register_counter ("Event: GC finalize starts", &finalize_begins_ctr);
	register_counter ("Event: GC finalize ends", &finalize_ends_ctr);
	register_counter ("Event: GC finalize object starts", &finalize_object_begins_ctr);
	register_counter ("Event: GC finalize object ends", &finalize_object_ends_ctr);
	register_counter ("Event: Image loads", &image_loads_ctr);
	register_counter ("Event: Image unloads", &image_unloads_ctr);
	register_counter ("Event: Assembly loads", &assembly_loads_ctr);
	register_counter ("Event: Assembly unloads", &assembly_unloads_ctr);
	register_counter ("Event: Class loads", &class_loads_ctr);
	register_counter ("Event: Class unloads", &class_unloads_ctr);
	register_counter ("Event: Method entries", &method_entries_ctr);
	register_counter ("Event: Method exits", &method_exits_ctr);
	register_counter ("Event: Method exception leaves", &method_exception_exits_ctr);
	register_counter ("Event: Method JITs", &method_jits_ctr);
	register_counter ("Event: Code buffers", &code_buffers_ctr);
	register_counter ("Event: Exception throws", &exception_throws_ctr);
	register_counter ("Event: Exception clauses", &exception_clauses_ctr);
	register_counter ("Event: Monitor events", &monitor_events_ctr);
	register_counter ("Event: Thread starts", &thread_starts_ctr);
	register_counter ("Event: Thread ends", &thread_ends_ctr);
	register_counter ("Event: Thread names", &thread_names_ctr);
	register_counter ("Event: Domain loads", &domain_loads_ctr);
	register_counter ("Event: Domain unloads", &domain_unloads_ctr);
	register_counter ("Event: Domain names", &domain_names_ctr);
	register_counter ("Event: Context loads", &context_loads_ctr);
	register_counter ("Event: Context unloads", &context_unloads_ctr);
	register_counter ("Event: Sample binaries", &sample_ubins_ctr);
	register_counter ("Event: Sample symbols", &sample_usyms_ctr);
	register_counter ("Event: Sample hits", &sample_hits_ctr);
	register_counter ("Event: Counter descriptors", &counter_descriptors_ctr);
	register_counter ("Event: Counter samples", &counter_samples_ctr);
	register_counter ("Event: Performance counter descriptors", &perfcounter_descriptors_ctr);
	register_counter ("Event: Performance counter samples", &perfcounter_samples_ctr);
	register_counter ("Event: Coverage methods", &coverage_methods_ctr);
	register_counter ("Event: Coverage statements", &coverage_statements_ctr);
	register_counter ("Event: Coverage classes", &coverage_classes_ctr);
	register_counter ("Event: Coverage assemblies", &coverage_assemblies_ctr);

	counters_init ();

	/*
	 * We must start the helper thread before the writer thread. This is
	 * because the helper thread sets up the command port which is written to
	 * the log header by the writer thread.
	 */
	start_helper_thread ();
	start_writer_thread ();
	start_dumper_thread ();
}

static void
create_profiler (const char *args, const char *filename, GPtrArray *filters)
{
	char *nf;

	log_profiler.args = pstrdup (args);
	log_profiler.command_port = log_config.command_port;

	//If filename begin with +, append the pid at the end
	if (filename && *filename == '+')
		filename = g_strdup_printf ("%s.%d", filename + 1, getpid ());

	if (!filename) {
		if (log_config.do_report)
			filename = "|mprof-report -";
		else
			filename = "output.mlpd";
		nf = (char*)filename;
	} else {
		nf = new_filename (filename);
		if (log_config.do_report) {
			int s = strlen (nf) + 32;
			char *p = (char *) g_malloc (s);
			snprintf (p, s, "|mprof-report '--out=%s' -", nf);
			g_free (nf);
			nf = p;
		}
	}
	if (*nf == '|') {
		log_profiler.file = popen (nf + 1, "w");
		log_profiler.pipe_output = 1;
	} else if (*nf == '#') {
		int fd = strtol (nf + 1, NULL, 10);
		log_profiler.file = fdopen (fd, "a");
	} else
		log_profiler.file = fopen (nf, "wb");

	if (!log_profiler.file) {
		mono_profiler_printf_err ("Could not create log profiler output file '%s'.", nf);
		exit (1);
	}

#if defined (HAVE_SYS_ZLIB)
	if (log_config.use_zip)
		log_profiler.gzfile = gzdopen (fileno (log_profiler.file), "wb");
#endif

	/*
	 * If you hit this assert while increasing MAX_FRAMES, you need to increase
	 * SAMPLE_BLOCK_SIZE as well.
	 */
	g_assert (SAMPLE_SLOT_SIZE (MAX_FRAMES) * 2 < LOCK_FREE_ALLOC_SB_USABLE_SIZE (SAMPLE_BLOCK_SIZE));

	// FIXME: We should free this stuff too.
	mono_lock_free_allocator_init_size_class (&log_profiler.sample_size_class, SAMPLE_SLOT_SIZE (log_config.num_frames), SAMPLE_BLOCK_SIZE);
	mono_lock_free_allocator_init_allocator (&log_profiler.sample_allocator, &log_profiler.sample_size_class, MONO_MEM_ACCOUNT_PROFILER);

	mono_lock_free_queue_init (&log_profiler.sample_reuse_queue);

	g_assert (sizeof (WriterQueueEntry) * 2 < LOCK_FREE_ALLOC_SB_USABLE_SIZE (WRITER_ENTRY_BLOCK_SIZE));

	// FIXME: We should free this stuff too.
	mono_lock_free_allocator_init_size_class (&log_profiler.writer_entry_size_class, sizeof (WriterQueueEntry), WRITER_ENTRY_BLOCK_SIZE);
	mono_lock_free_allocator_init_allocator (&log_profiler.writer_entry_allocator, &log_profiler.writer_entry_size_class, MONO_MEM_ACCOUNT_PROFILER);

	mono_lock_free_queue_init (&log_profiler.writer_queue);
	mono_os_sem_init (&log_profiler.writer_queue_sem, 0);

	mono_lock_free_queue_init (&log_profiler.dumper_queue);
	mono_os_sem_init (&log_profiler.dumper_queue_sem, 0);

	mono_os_mutex_init (&log_profiler.method_table_mutex);
	log_profiler.method_table = mono_conc_hashtable_new (NULL, NULL);

	if (ENABLED (PROFLOG_CODE_COV_FEATURE))
		coverage_init ();

	log_profiler.coverage_filters = filters;

	log_profiler.startup_time = current_time ();
}

/*
 * declaration to silence the compiler: this is the entry point that
 * mono will load from the shared library and call.
 */
extern void
mono_profiler_init (const char *desc);

extern void
mono_profiler_init_log (const char *desc);

/*
 * this is the entry point that will be used when the profiler
 * is embedded inside the main executable.
 */
void
mono_profiler_init_log (const char *desc)
{
	mono_profiler_init (desc);
}

void
mono_profiler_init (const char *desc)
{
	GPtrArray *filters = NULL;

	proflog_parse_args (&log_config, desc [3] == ':' ? desc + 4 : "");

	if (log_config.cov_filter_files) {
		filters = g_ptr_array_new ();
		int i;
		for (i = 0; i < log_config.cov_filter_files->len; ++i) {
			const char *name = log_config.cov_filter_files->pdata [i];
			parse_cov_filter_file (filters, name);
		}
	}

	init_time ();

	PROF_TLS_INIT ();

	create_profiler (desc, log_config.output_filename, filters);

	mono_lls_init (&log_profiler.profiler_thread_list, NULL);

	MonoProfilerHandle handle = log_profiler.handle = mono_profiler_install (&log_profiler);

	//Required callbacks
	mono_profiler_set_runtime_shutdown_end_callback (handle, log_shutdown);
	mono_profiler_set_runtime_initialized_callback (handle, runtime_initialized);

	mono_profiler_set_gc_event_callback (handle, gc_event);
	mono_profiler_set_gc_resize_callback (handle, gc_resize);
	mono_profiler_set_thread_started_callback (handle, thread_start);
	mono_profiler_set_thread_stopped_callback (handle, thread_end);

	//It's questionable whether we actually want this to be mandatory, maybe put it behind the actual event?
	mono_profiler_set_thread_name_callback (handle, thread_name);

	if (log_config.effective_mask & PROFLOG_DOMAIN_EVENTS) {
		mono_profiler_set_domain_loaded_callback (handle, domain_loaded);
		mono_profiler_set_domain_unloading_callback (handle, domain_unloaded);
		mono_profiler_set_domain_name_callback (handle, domain_name);
	}

	if (log_config.effective_mask & PROFLOG_ASSEMBLY_EVENTS) {
		mono_profiler_set_assembly_loaded_callback (handle, assembly_loaded);
		mono_profiler_set_assembly_unloading_callback (handle, assembly_unloaded);
	}

	if (log_config.effective_mask & PROFLOG_MODULE_EVENTS) {
		mono_profiler_set_image_loaded_callback (handle, image_loaded);
		mono_profiler_set_image_unloading_callback (handle, image_unloaded);
	}

	if (log_config.effective_mask & PROFLOG_CLASS_EVENTS)
		mono_profiler_set_class_loaded_callback (handle, class_loaded);

	if (log_config.effective_mask & PROFLOG_JIT_COMPILATION_EVENTS) {
		mono_profiler_set_jit_done_callback (handle, method_jitted);
		mono_profiler_set_jit_code_buffer_callback (handle, code_buffer_new);
	}

	if (log_config.effective_mask & PROFLOG_EXCEPTION_EVENTS) {
		mono_profiler_set_exception_throw_callback (handle, throw_exc);
		mono_profiler_set_exception_clause_callback (handle, clause_exc);
	}

	if (log_config.effective_mask & PROFLOG_ALLOCATION_EVENTS) {
		mono_profiler_enable_allocations ();
		mono_profiler_set_gc_allocation_callback (handle, gc_alloc);
	}

	//PROFLOG_GC_EVENTS is mandatory
	//PROFLOG_THREAD_EVENTS is mandatory

	if (log_config.effective_mask & PROFLOG_CALL_EVENTS) {
		mono_profiler_set_call_instrumentation_filter_callback (handle, method_filter);
		mono_profiler_set_method_enter_callback (handle, method_enter);
		mono_profiler_set_method_leave_callback (handle, method_leave);
		mono_profiler_set_method_exception_leave_callback (handle, method_exc_leave);
	}

	if (log_config.effective_mask & PROFLOG_INS_COVERAGE_EVENTS)
		mono_profiler_set_coverage_filter_callback (handle, coverage_filter);

	if (log_config.effective_mask & PROFLOG_SAMPLING_EVENTS) {
		mono_profiler_enable_sampling (handle);

		if (!mono_profiler_set_sample_mode (handle, log_config.sampling_mode, log_config.sample_freq))
			mono_profiler_printf_err ("Another profiler controls sampling parameters; the log profiler will not be able to modify them.");

		mono_profiler_set_sample_hit_callback (handle, mono_sample_hit);
	}

	if (log_config.effective_mask & PROFLOG_MONITOR_EVENTS) {
		mono_profiler_set_monitor_contention_callback (handle, monitor_contention);
		mono_profiler_set_monitor_acquired_callback (handle, monitor_acquired);
		mono_profiler_set_monitor_failed_callback (handle, monitor_failed);
	}

	if (log_config.effective_mask & PROFLOG_GC_MOVES_EVENTS)
		mono_profiler_set_gc_moves_callback (handle, gc_moves);

	if (log_config.effective_mask & PROFLOG_GC_ROOT_EVENTS)
		mono_profiler_set_gc_roots_callback (handle, gc_roots);

	if (log_config.effective_mask & PROFLOG_CONTEXT_EVENTS) {
		mono_profiler_set_context_loaded_callback (handle, context_loaded);
		mono_profiler_set_context_unloaded_callback (handle, context_unloaded);
	}

	if (log_config.effective_mask & PROFLOG_FINALIZATION_EVENTS) {
		mono_profiler_set_gc_finalizing_callback (handle, finalize_begin);
		mono_profiler_set_gc_finalized_callback (handle, finalize_end);
		mono_profiler_set_gc_finalizing_object_callback (handle, finalize_object_begin);
		mono_profiler_set_gc_finalized_object_callback (handle, finalize_object_end);
	} else if (ENABLED (PROFLOG_HEAPSHOT_FEATURE) && log_config.hs_mode_ondemand) {
		//On Demand heapshot uses the finalizer thread to force a collection and thus a heapshot
		mono_profiler_set_gc_finalized_callback (handle, finalize_end);
	}

	//PROFLOG_COUNTER_EVENTS is a pseudo event controled by the no_counters global var

	if (log_config.effective_mask & PROFLOG_GC_HANDLE_EVENTS) {
		mono_profiler_set_gc_handle_created_callback (handle, gc_handle_created);
		mono_profiler_set_gc_handle_deleted_callback (handle, gc_handle_deleted);
	}
}
