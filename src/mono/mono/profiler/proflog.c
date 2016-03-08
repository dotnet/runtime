/*
 * proflog.c: mono log profiler
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Alex Rønne Petersen (alexrp@xamarin.com)
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 */

#include <config.h>
#include "../mini/jit.h"
#include <mono/metadata/profiler.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-perfcounters.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-conc-hashtable.h>
#include <mono/utils/lock-free-queue.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <glib.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_SCHED_GETAFFINITY
#include <sched.h>
#endif
#include <fcntl.h>
#include <errno.h>
#if defined(HOST_WIN32) || defined(DISABLE_SOCKETS)
#define DISABLE_HELPER_THREAD 1
#endif

#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif
#ifdef HAVE_DLFCN_H
#include <dlfcn.h>
#endif
#ifdef HAVE_EXECINFO_H
#include <execinfo.h>
#endif
#ifdef HAVE_LINK_H
#include <link.h>
#endif

#ifndef DISABLE_HELPER_THREAD
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <sys/select.h>
#endif

#ifdef HOST_WIN32
#include <windows.h>
#else
#include <pthread.h>
#endif

#ifdef HAVE_SYS_STAT_H
#include <sys/stat.h>
#endif

#include "utils.c"
#include "proflog.h"

#if defined (HAVE_SYS_ZLIB)
#include <zlib.h>
#endif

#if defined(__linux__)

#include <unistd.h>
#include <sys/syscall.h>

#ifdef ENABLE_PERF_EVENTS
#include <linux/perf_event.h>

#define USE_PERF_EVENTS 1

static int read_perf_mmap (MonoProfiler* prof, int cpu);
#endif

#endif

#define BUFFER_SIZE (4096 * 16)

/* Worst-case size in bytes of a 64-bit value encoded with LEB128. */
#define LEB128_SIZE 10
/* Size in bytes of the event ID prefix. */
#define EVENT_SIZE 1

static int nocalls = 0;
static int notraces = 0;
static int use_zip = 0;
static int do_report = 0;
static int do_heap_shot = 0;
static int max_call_depth = 100;
static volatile int runtime_inited = 0;
static int need_helper_thread = 0;
static int command_port = 0;
static int heapshot_requested = 0;
static int sample_type = 0;
static int sample_freq = 0;
static int do_mono_sample = 0;
static int in_shutdown = 0;
static int do_debug = 0;
static int do_counters = 0;
static int do_coverage = 0;
static gboolean debug_coverage = FALSE;
static MonoProfileSamplingMode sampling_mode = MONO_PROFILER_STAT_MODE_PROCESS;

typedef struct _LogBuffer LogBuffer;

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
 * [sysid: 2 bytes] operating system and architecture identifier
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
 * [extended info: upper 4 bits] [type: lower 4 bits] [data]*
 * The data that follows depends on type and the extended info.
 * Type is one of the enum values in proflog.h: TYPE_ALLOC, TYPE_GC,
 * TYPE_METADATA, TYPE_METHOD, TYPE_EXCEPTION, TYPE_MONITOR, TYPE_HEAP.
 * The extended info bits are interpreted based on type, see
 * each individual event description below.
 * strings are represented as a 0-terminated utf8 sequence.
 *
 * backtrace format:
 * [flags: uleb128] must be 0
 * [num: uleb128] number of frames following
 * [frame: sleb128]* num MonoMethod pointers as differences from ptr_base
 *
 * type alloc format:
 * type: TYPE_ALLOC
 * exinfo: flags: TYPE_ALLOC_BT
 * [time diff: uleb128] nanoseconds since last timing
 * [ptr: sleb128] class as a byte difference from ptr_base
 * [obj: sleb128] object address as a byte difference from obj_base
 * [size: uleb128] size of the object in the heap
 * If the TYPE_ALLOC_BT flag is set, a backtrace follows.
 *
 * type GC format:
 * type: TYPE_GC
 * exinfo: one of TYPE_GC_EVENT, TYPE_GC_RESIZE, TYPE_GC_MOVE, TYPE_GC_HANDLE_CREATED[_BT],
 * TYPE_GC_HANDLE_DESTROYED[_BT]
 * [time diff: uleb128] nanoseconds since last timing
 * if exinfo == TYPE_GC_RESIZE
 *	[heap_size: uleb128] new heap size
 * if exinfo == TYPE_GC_EVENT
 *	[event type: uleb128] GC event (MONO_GC_EVENT_* from profiler.h)
 *	[generation: uleb128] GC generation event refers to
 * if exinfo == TYPE_GC_MOVE
 *	[num_objects: uleb128] number of object moves that follow
 *	[objaddr: sleb128]+ num_objects object pointer differences from obj_base
 *	num is always an even number: the even items are the old
 *	addresses, the odd numbers are the respective new object addresses
 * if exinfo == TYPE_GC_HANDLE_CREATED[_BT]
 *	[handle_type: uleb128] GC handle type (System.Runtime.InteropServices.GCHandleType)
 *	upper bits reserved as flags
 *	[handle: uleb128] GC handle value
 *	[objaddr: sleb128] object pointer differences from obj_base
 * 	If exinfo == TYPE_GC_HANDLE_CREATED_BT, a backtrace follows.
 * if exinfo == TYPE_GC_HANDLE_DESTROYED[_BT]
 *	[handle_type: uleb128] GC handle type (System.Runtime.InteropServices.GCHandleType)
 *	upper bits reserved as flags
 *	[handle: uleb128] GC handle value
 * 	If exinfo == TYPE_GC_HANDLE_DESTROYED_BT, a backtrace follows.
 *
 * type metadata format:
 * type: TYPE_METADATA
 * exinfo: one of: TYPE_END_LOAD, TYPE_END_UNLOAD (optional for TYPE_THREAD and TYPE_DOMAIN)
 * [time diff: uleb128] nanoseconds since last timing
 * [mtype: byte] metadata type, one of: TYPE_CLASS, TYPE_IMAGE, TYPE_ASSEMBLY, TYPE_DOMAIN,
 * TYPE_THREAD, TYPE_CONTEXT
 * [pointer: sleb128] pointer of the metadata type depending on mtype
 * if mtype == TYPE_CLASS
 *	[image: sleb128] MonoImage* as a pointer difference from ptr_base
 * 	[flags: uleb128] must be 0
 * 	[name: string] full class name
 * if mtype == TYPE_IMAGE
 * 	[flags: uleb128] must be 0
 * 	[name: string] image file name
 * if mtype == TYPE_ASSEMBLY
 * 	[flags: uleb128] must be 0
 * 	[name: string] assembly name
 * if mtype == TYPE_DOMAIN
 * 	[flags: uleb128] must be 0
 * if mtype == TYPE_DOMAIN && exinfo == 0
 * 	[name: string] domain friendly name
 * if mtype == TYPE_CONTEXT
 * 	[flags: uleb128] must be 0
 * 	[domain: sleb128] domain id as pointer
 * if mtype == TYPE_THREAD && (format_version < 11 || (format_version > 10 && exinfo == 0))
 * 	[flags: uleb128] must be 0
 * 	[name: string] thread name
 *
 * type method format:
 * type: TYPE_METHOD
 * exinfo: one of: TYPE_LEAVE, TYPE_ENTER, TYPE_EXC_LEAVE, TYPE_JIT
 * [time diff: uleb128] nanoseconds since last timing
 * [method: sleb128] MonoMethod* as a pointer difference from the last such
 * pointer or the buffer method_base
 * if exinfo == TYPE_JIT
 *	[code address: sleb128] pointer to the native code as a diff from ptr_base
 *	[code size: uleb128] size of the generated code
 *	[name: string] full method name
 *
 * type runtime format:
 * type: TYPE_RUNTIME
 * exinfo: one of: TYPE_JITHELPER
 * [time diff: uleb128] nanoseconds since last timing
 * if exinfo == TYPE_JITHELPER
 *	[type: uleb128] MonoProfilerCodeBufferType enum value
 *	[buffer address: sleb128] pointer to the native code as a diff from ptr_base
 *	[buffer size: uleb128] size of the generated code
 *	if type == MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE
 *		[name: string] buffer description name
 *
 * type monitor format:
 * type: TYPE_MONITOR
 * exinfo: TYPE_MONITOR_BT flag and one of: MONO_PROFILER_MONITOR_(CONTENTION|FAIL|DONE)
 * [time diff: uleb128] nanoseconds since last timing
 * [object: sleb128] the lock object as a difference from obj_base
 * if exinfo.low3bits == MONO_PROFILER_MONITOR_CONTENTION
 *	If the TYPE_MONITOR_BT flag is set, a backtrace follows.
 *
 * type heap format
 * type: TYPE_HEAP
 * exinfo: one of TYPE_HEAP_START, TYPE_HEAP_END, TYPE_HEAP_OBJECT, TYPE_HEAP_ROOT
 * if exinfo == TYPE_HEAP_START
 * 	[time diff: uleb128] nanoseconds since last timing
 * if exinfo == TYPE_HEAP_END
 * 	[time diff: uleb128] nanoseconds since last timing
 * if exinfo == TYPE_HEAP_OBJECT
 * 	[object: sleb128] the object as a difference from obj_base
 * 	[class: sleb128] the object MonoClass* as a difference from ptr_base
 * 	[size: uleb128] size of the object on the heap
 * 	[num_refs: uleb128] number of object references
 * 	if (format version > 1) each referenced objref is preceded by a
 *	uleb128 encoded offset: the first offset is from the object address
 *	and each next offset is relative to the previous one
 * 	[objrefs: sleb128]+ object referenced as a difference from obj_base
 * 	The same object can appear multiple times, but only the first time
 * 	with size != 0: in the other cases this data will only be used to
 * 	provide additional referenced objects.
 * if exinfo == TYPE_HEAP_ROOT
 * 	[num_roots: uleb128] number of root references
 * 	[num_gc: uleb128] number of major gcs
 * 	[object: sleb128] the object as a difference from obj_base
 * 	[root_type: uleb128] the root_type: MonoProfileGCRootType (profiler.h)
 * 	[extra_info: uleb128] the extra_info value
 * 	object, root_type and extra_info are repeated num_roots times
 *
 * type sample format
 * type: TYPE_SAMPLE
 * exinfo: one of TYPE_SAMPLE_HIT, TYPE_SAMPLE_USYM, TYPE_SAMPLE_UBIN, TYPE_SAMPLE_COUNTERS_DESC, TYPE_SAMPLE_COUNTERS
 * if exinfo == TYPE_SAMPLE_HIT
 * 	[sample_type: uleb128] type of sample (SAMPLE_*)
 * 	[timestamp: uleb128] nanoseconds since startup (note: different from other timestamps!)
 * 	if (format_version > 10)
 * 		[thread: sleb128] thread id as difference from ptr_base
 * 	[count: uleb128] number of following instruction addresses
 * 	[ip: sleb128]* instruction pointer as difference from ptr_base
 *	if (format_version > 5)
 *		[mbt_count: uleb128] number of managed backtrace info triplets (method + IL offset + native offset)
 *		[method: sleb128]* MonoMethod* as a pointer difference from the last such
 * 		pointer or the buffer method_base (the first such method can be also indentified by ip, but this is not neccessarily true)
 *		[il_offset: sleb128]* IL offset inside method where the hit occurred
 *		[native_offset: sleb128]* native offset inside method where the hit occurred
 * if exinfo == TYPE_SAMPLE_USYM
 * 	[address: sleb128] symbol address as a difference from ptr_base
 * 	[size: uleb128] symbol size (may be 0 if unknown)
 * 	[name: string] symbol name
 * if exinfo == TYPE_SAMPLE_UBIN
 * 	[time diff: uleb128] nanoseconds since last timing
 * 	[address: sleb128] address where binary has been loaded
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
 * 		[type: uleb128] type of counter
 * 		[unit: uleb128] unit of counter
 * 		[variance: uleb128] variance of counter
 * 		[index: uleb128] unique index of counter
 * if exinfo == TYPE_SAMPLE_COUNTERS
 * 	[timestamp: uleb128] sampling timestamp
 * 	while true:
 * 		[index: uleb128] unique index of counter
 * 		if index == 0:
 * 			break
 * 		[type: uleb128] type of counter value
 * 		if type == string:
 * 			if value == null:
 * 				[0: uleb128] 0 -> value is null
 * 			else:
 * 				[1: uleb128] 1 -> value is not null
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
 */

/*
 * Format oddities that we ought to fix:
 *
 * - Methods written in emit_bt () should be based on the buffer's base
 *   method instead of the base pointer.
 * - The TYPE_SAMPLE_HIT event contains (currently) pointless data like
 *   always-one unmanaged frame count and always-zero IL offsets.
 *
 * These are mostly small things and are not worth a format change by
 * themselves. They should be done when some other major change has to
 * be done to the format.
 */

struct _LogBuffer {
	LogBuffer *next;
	uint64_t time_base;
	uint64_t last_time;
	uintptr_t ptr_base;
	uintptr_t method_base;
	uintptr_t last_method;
	uintptr_t obj_base;
	uintptr_t thread_id;
	unsigned char* data_end;
	unsigned char* data;
	int locked;
	int size;
	int call_depth;
	unsigned char buf [1];
};

static inline void
ign_res (int G_GNUC_UNUSED unused, ...)
{
}

#define ENTER_LOG(lb,str) if ((lb)->locked) {ign_res (write(2, str, strlen(str))); ign_res (write(2, "\n", 1));return;} else {(lb)->locked++;}
#define EXIT_LOG(lb) (lb)->locked--;

typedef struct _StatBuffer StatBuffer;
struct _StatBuffer {
	StatBuffer *next;
	uintptr_t size;
	uintptr_t *data_end;
	uintptr_t *data;
	uintptr_t buf [1];
};

typedef struct _BinaryObject BinaryObject;

struct _BinaryObject {
	BinaryObject *next;
	void *addr;
	char *name;
};

struct _MonoProfiler {
	StatBuffer *stat_buffers;
	FILE* file;
#if defined (HAVE_SYS_ZLIB)
	gzFile gzfile;
#endif
	uint64_t startup_time;
	int pipe_output;
	int last_gc_gen_started;
	int command_port;
	int server_socket;
	int pipes [2];
#ifndef HOST_WIN32
	pthread_t helper_thread;
	pthread_t writer_thread;
#endif
	volatile gint32 run_writer_thread;
	MonoLockFreeQueue writer_queue;
	MonoConcurrentHashTable *method_table;
	mono_mutex_t method_table_mutex;
	BinaryObject *binary_objects;
	GPtrArray *coverage_filters;
	GPtrArray *sorted_sample_events;
};

typedef struct _WriterQueueEntry WriterQueueEntry;
struct _WriterQueueEntry {
	MonoLockFreeQueueNode node;
	GPtrArray *methods;
	LogBuffer *buffer;
};

typedef struct _MethodInfo MethodInfo;
struct _MethodInfo {
	MonoMethod *method;
	MonoJitInfo *ji;
	uint64_t time;
};

#ifdef TLS_INIT
#undef TLS_INIT
#endif

#ifdef HOST_WIN32
#define TLS_SET(x,y) (TlsSetValue (x, y))
#define TLS_GET(t,x) ((t *) TlsGetValue (x))
#define TLS_INIT(x) (x = TlsAlloc ())
static int tlsbuffer;
static int tlsmethodlist;
#elif HAVE_KW_THREAD
#define TLS_SET(x,y) (x = y)
#define TLS_GET(t,x) (x)
#define TLS_INIT(x)
static __thread LogBuffer* tlsbuffer = NULL;
static __thread GPtrArray* tlsmethodlist = NULL;
#else
#define TLS_SET(x,y) (pthread_setspecific (x, y))
#define TLS_GET(t,x) ((t *) pthread_getspecific (x))
#define TLS_INIT(x) (pthread_key_create (&x, NULL))
static pthread_key_t tlsbuffer;
static pthread_key_t tlsmethodlist;
#endif

static void safe_send (MonoProfiler *profiler, LogBuffer *logbuffer);

static char*
pstrdup (const char *s)
{
	int len = strlen (s) + 1;
	char *p = (char *)malloc (len);
	memcpy (p, s, len);
	return p;
}

static StatBuffer*
create_stat_buffer (void)
{
	StatBuffer* buf = (StatBuffer *)alloc_buffer (BUFFER_SIZE);
	buf->size = BUFFER_SIZE;
	buf->data_end = (uintptr_t*)((unsigned char*)buf + buf->size);
	buf->data = buf->buf;
	return buf;
}

static LogBuffer*
create_buffer (void)
{
	LogBuffer* buf = (LogBuffer *)alloc_buffer (BUFFER_SIZE);
	buf->size = BUFFER_SIZE;
	buf->time_base = current_time ();
	buf->last_time = buf->time_base;
	buf->data_end = (unsigned char*)buf + buf->size;
	buf->data = buf->buf;
	return buf;
}

static void
init_thread (void)
{
	if (!TLS_GET (LogBuffer, tlsbuffer)) {
		LogBuffer *logbuffer = create_buffer ();
		TLS_SET (tlsbuffer, logbuffer);
		logbuffer->thread_id = thread_id ();
	}
	if (!TLS_GET (GPtrArray, tlsmethodlist)) {
		GPtrArray *methodlist = g_ptr_array_new ();
		TLS_SET (tlsmethodlist, methodlist);
	}

	//printf ("thread %p at time %llu\n", (void*)logbuffer->thread_id, logbuffer->time_base);
}

static LogBuffer *
ensure_logbuf_inner (LogBuffer *old, int bytes)
{
	if (old && old->data + bytes + 100 < old->data_end)
		return old;

	LogBuffer *new_ = (LogBuffer *)create_buffer ();
	new_->thread_id = thread_id ();
	new_->next = old;

	if (old)
		new_->call_depth = old->call_depth;

	return new_;
}

static LogBuffer*
ensure_logbuf (int bytes)
{
	LogBuffer *old = TLS_GET (LogBuffer, tlsbuffer);
	LogBuffer *new_ = ensure_logbuf_inner (old, bytes);

	if (new_ == old)
		return old; // Still enough space.

	TLS_SET (tlsbuffer, new_);
	init_thread ();

	return new_;
}

static void
emit_byte (LogBuffer *logbuffer, int value)
{
	logbuffer->data [0] = value;
	logbuffer->data++;
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_value (LogBuffer *logbuffer, int value)
{
	encode_uleb128 (value, logbuffer->data, &logbuffer->data);
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_time (LogBuffer *logbuffer, uint64_t value)
{
	uint64_t tdiff = value - logbuffer->last_time;
	//if (value < logbuffer->last_time)
	//	printf ("time went backwards\n");
	//if (tdiff > 1000000)
	//	printf ("large time offset: %llu\n", tdiff);
	encode_uleb128 (tdiff, logbuffer->data, &logbuffer->data);
	/*if (tdiff != decode_uleb128 (p, &p))
		printf ("incorrect encoding: %llu\n", tdiff);*/
	logbuffer->last_time = value;
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_svalue (LogBuffer *logbuffer, int64_t value)
{
	encode_sleb128 (value, logbuffer->data, &logbuffer->data);
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_uvalue (LogBuffer *logbuffer, uint64_t value)
{
	encode_uleb128 (value, logbuffer->data, &logbuffer->data);
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_ptr (LogBuffer *logbuffer, void *ptr)
{
	if (!logbuffer->ptr_base)
		logbuffer->ptr_base = (uintptr_t)ptr;
	emit_svalue (logbuffer, (intptr_t)ptr - logbuffer->ptr_base);
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_method_inner (LogBuffer *logbuffer, void *method)
{
	if (!logbuffer->method_base) {
		logbuffer->method_base = (intptr_t)method;
		logbuffer->last_method = (intptr_t)method;
	}
	encode_sleb128 ((intptr_t)((char*)method - (char*)logbuffer->last_method), logbuffer->data, &logbuffer->data);
	logbuffer->last_method = (intptr_t)method;
	assert (logbuffer->data <= logbuffer->data_end);
}

/*
typedef struct {
	MonoMethod *method;
	MonoJitInfo *found;
} MethodSearch;

static void
find_method (MonoDomain *domain, void *user_data)
{
	MethodSearch *search = user_data;

	if (search->found)
		return;

	MonoJitInfo *ji = mono_get_jit_info_from_method (domain, search->method);

	// It could be AOT'd, so we need to get it from the AOT runtime's cache.
	if (!ji) {
		void *ip = mono_aot_get_method (domain, search->method);

		// Avoid a slow path in mono_jit_info_table_find ().
		if (ip)
			ji = mono_jit_info_table_find (domain, ip);
	}

	if (ji)
		search->found = ji;
}
*/

static void
register_method_local (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *ji)
{
	if (!mono_conc_hashtable_lookup (prof->method_table, method)) {
		/*
		 * FIXME: In some cases, we crash while looking up JIT info for AOT'd methods.
		 * This usually happens for static constructors. This code is disabled for now
		 * as we don't need this info for anything critical.
		 *
		 * https://bugzilla.xamarin.com/show_bug.cgi?id=35171
		 */
		/*
		if (!ji) {
			MethodSearch search = { method, NULL };

			mono_domain_foreach (find_method, &search);

			ji = search.found;
		}
		*/

		/*
		 * FIXME: We can't always find JIT info for a generic shared method, especially
		 * if we obtained the MonoMethod during an async stack walk. For now, we deal
		 * with this by giving the generic shared method name and dummy code start/size
		 * information (i.e. zeroes).
		 */
		//g_assert (ji);

		MethodInfo *info = (MethodInfo *)malloc (sizeof (MethodInfo));

		info->method = method;
		info->ji = ji;
		info->time = current_time ();

		g_ptr_array_add (TLS_GET (GPtrArray, tlsmethodlist), info);
	}
}

static void
emit_method (MonoProfiler *prof, LogBuffer *logbuffer, MonoMethod *method)
{
	register_method_local (prof, method, NULL);
	emit_method_inner (logbuffer, method);
}

static void
emit_method_as_ptr (MonoProfiler *prof, LogBuffer *logbuffer, MonoMethod *method)
{
	register_method_local (prof, method, NULL);
	emit_ptr (logbuffer, method);
}

static void
emit_obj (LogBuffer *logbuffer, void *ptr)
{
	if (!logbuffer->obj_base)
		logbuffer->obj_base = (uintptr_t)ptr >> 3;
	emit_svalue (logbuffer, ((uintptr_t)ptr >> 3) - logbuffer->obj_base);
	assert (logbuffer->data <= logbuffer->data_end);
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

static void
dump_header (MonoProfiler *profiler)
{
	char hbuf [128];
	char *p = hbuf;
	p = write_int32 (p, LOG_HEADER_ID);
	*p++ = LOG_VERSION_MAJOR;
	*p++ = LOG_VERSION_MINOR;
	*p++ = LOG_DATA_VERSION;
	*p++ = sizeof (void*);
	p = write_int64 (p, ((uint64_t)time (NULL)) * 1000); /* startup time */
	p = write_int32 (p, get_timer_overhead ()); /* timer overhead */
	p = write_int32 (p, 0); /* flags */
	p = write_int32 (p, process_id ()); /* pid */
	p = write_int16 (p, profiler->command_port); /* port */
	p = write_int16 (p, 0); /* opsystem */
#if defined (HAVE_SYS_ZLIB)
	if (profiler->gzfile) {
		gzwrite (profiler->gzfile, hbuf, p - hbuf);
	} else {
		fwrite (hbuf, p - hbuf, 1, profiler->file);
	}
#else
	fwrite (hbuf, p - hbuf, 1, profiler->file);
	fflush (profiler->file);
#endif
}

static void
send_buffer (MonoProfiler *prof, GPtrArray *methods, LogBuffer *buffer)
{
	WriterQueueEntry *entry = (WriterQueueEntry *)calloc (1, sizeof (WriterQueueEntry));
	mono_lock_free_queue_node_init (&entry->node, FALSE);
	entry->methods = methods;
	entry->buffer = buffer;
	mono_lock_free_queue_enqueue (&prof->writer_queue, &entry->node);
}

static void
dump_buffer (MonoProfiler *profiler, LogBuffer *buf)
{
	char hbuf [128];
	char *p = hbuf;
	if (buf->next)
		dump_buffer (profiler, buf->next);
	p = write_int32 (p, BUF_ID);
	p = write_int32 (p, buf->data - buf->buf);
	p = write_int64 (p, buf->time_base);
	p = write_int64 (p, buf->ptr_base);
	p = write_int64 (p, buf->obj_base);
	p = write_int64 (p, buf->thread_id);
	p = write_int64 (p, buf->method_base);
#if defined (HAVE_SYS_ZLIB)
	if (profiler->gzfile) {
		gzwrite (profiler->gzfile, hbuf, p - hbuf);
		gzwrite (profiler->gzfile, buf->buf, buf->data - buf->buf);
	} else {
#endif
		fwrite (hbuf, p - hbuf, 1, profiler->file);
		fwrite (buf->buf, buf->data - buf->buf, 1, profiler->file);
		fflush (profiler->file);
#if defined (HAVE_SYS_ZLIB)
	}
#endif
	free_buffer (buf, buf->size);
}

static void
process_requests (MonoProfiler *profiler)
{
	if (heapshot_requested)
		mono_gc_collect (mono_gc_max_generation ());
}

static void counters_init (MonoProfiler *profiler);
static void counters_sample (MonoProfiler *profiler, uint64_t timestamp);

/*
 * Can be called only at safe callback locations.
 */
static void
safe_send (MonoProfiler *profiler, LogBuffer *logbuffer)
{
	/* We need the runtime initialized so that we have threads and hazard
	 * pointers available. Otherwise, the lock free queue will not work and
	 * there won't be a thread to process the data.
	 *
	 * While the runtime isn't initialized, we just accumulate data in the
	 * thread local buffer list.
	 */
	if (!InterlockedRead (&runtime_inited))
		return;

	int cd = logbuffer->call_depth;

	send_buffer (profiler, TLS_GET (GPtrArray, tlsmethodlist), TLS_GET (LogBuffer, tlsbuffer));

	TLS_SET (tlsbuffer, NULL);
	TLS_SET (tlsmethodlist, NULL);

	init_thread ();

	TLS_GET (LogBuffer, tlsbuffer)->call_depth = cd;
}

static int
gc_reference (MonoObject *obj, MonoClass *klass, uintptr_t size, uintptr_t num, MonoObject **refs, uintptr_t *offsets, void *data)
{
	int i;
	uintptr_t last_offset = 0;
	//const char *name = mono_class_get_name (klass);
	LogBuffer *logbuffer = ensure_logbuf (
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
	emit_byte (logbuffer, TYPE_HEAP_OBJECT | TYPE_HEAP);
	emit_obj (logbuffer, obj);
	emit_ptr (logbuffer, klass);
	/* account for object alignment in the heap */
	size += 7;
	size &= ~7;
	emit_value (logbuffer, size);
	emit_value (logbuffer, num);
	for (i = 0; i < num; ++i) {
		emit_value (logbuffer, offsets [i] - last_offset);
		last_offset = offsets [i];
		emit_obj (logbuffer, refs [i]);
	}
	//if (num)
	//	printf ("obj: %p, klass: %s, refs: %d, size: %d\n", obj, name, (int)num, (int)size);
	return 0;
}

static unsigned int hs_mode_ms = 0;
static unsigned int hs_mode_gc = 0;
static unsigned int hs_mode_ondemand = 0;
static unsigned int gc_count = 0;
static uint64_t last_hs_time = 0;

static void
heap_walk (MonoProfiler *profiler)
{
	int do_walk = 0;
	uint64_t now;
	LogBuffer *logbuffer;
	if (!do_heap_shot)
		return;
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */
	);
	now = current_time ();
	if (hs_mode_ms && (now - last_hs_time)/1000000 >= hs_mode_ms)
		do_walk = 1;
	else if (hs_mode_gc && (gc_count % hs_mode_gc) == 0)
		do_walk = 1;
	else if (hs_mode_ondemand)
		do_walk = heapshot_requested;
	else if (!hs_mode_ms && !hs_mode_gc && profiler->last_gc_gen_started == mono_gc_max_generation ())
		do_walk = 1;

	if (!do_walk)
		return;
	heapshot_requested = 0;
	emit_byte (logbuffer, TYPE_HEAP_START | TYPE_HEAP);
	emit_time (logbuffer, now);
	mono_gc_walk_heap (0, gc_reference, NULL);
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */
	);
	now = current_time ();
	emit_byte (logbuffer, TYPE_HEAP_END | TYPE_HEAP);
	emit_time (logbuffer, now);
	last_hs_time = now;
}

static void
gc_event (MonoProfiler *profiler, MonoGCEvent ev, int generation) {
	uint64_t now;
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* gc event */ +
		LEB128_SIZE /* generation */
	);
	now = current_time ();
	ENTER_LOG (logbuffer, "gcevent");
	emit_byte (logbuffer, TYPE_GC_EVENT | TYPE_GC);
	emit_time (logbuffer, now);
	emit_value (logbuffer, ev);
	emit_value (logbuffer, generation);
	/* to deal with nested gen1 after gen0 started */
	if (ev == MONO_GC_EVENT_START) {
		profiler->last_gc_gen_started = generation;
		if (generation == mono_gc_max_generation ())
			gc_count++;
	}
	if (ev == MONO_GC_EVENT_PRE_START_WORLD)
		heap_walk (profiler);
	EXIT_LOG (logbuffer);
	if (ev == MONO_GC_EVENT_POST_START_WORLD)
		safe_send (profiler, logbuffer);
	//printf ("gc event %d for generation %d\n", ev, generation);
}

static void
gc_resize (MonoProfiler *profiler, int64_t new_size) {
	uint64_t now;
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* new size */
	);
	now = current_time ();
	ENTER_LOG (logbuffer, "gcresize");
	emit_byte (logbuffer, TYPE_GC_RESIZE | TYPE_GC);
	emit_time (logbuffer, now);
	emit_value (logbuffer, new_size);
	//printf ("gc resized to %lld\n", new_size);
	EXIT_LOG (logbuffer);
}

#define MAX_FRAMES 32
typedef struct {
	int count;
	MonoMethod* methods [MAX_FRAMES];
	int32_t il_offsets [MAX_FRAMES];
	int32_t native_offsets [MAX_FRAMES];
} FrameData;
static int num_frames = MAX_FRAMES;

static mono_bool
walk_stack (MonoMethod *method, int32_t native_offset, int32_t il_offset, mono_bool managed, void* data)
{
	FrameData *frame = (FrameData *)data;
	if (method && frame->count < num_frames) {
		frame->il_offsets [frame->count] = il_offset;
		frame->native_offsets [frame->count] = native_offset;
		frame->methods [frame->count++] = method;
		//printf ("In %d %s at %d (native: %d)\n", frame->count, mono_method_get_name (method), il_offset, native_offset);
	}
	return frame->count == num_frames;
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
emit_bt (MonoProfiler *prof, LogBuffer *logbuffer, FrameData *data)
{
	/* FIXME: this is actually tons of data and we should
	 * just output it the first time and use an id the next
	 */
	if (data->count > num_frames)
		printf ("bad num frames: %d\n", data->count);
	emit_value (logbuffer, 0); /* flags */
	emit_value (logbuffer, data->count);
	//if (*p != data.count) {
	//	printf ("bad num frames enc at %d: %d -> %d\n", count, data.count, *p); printf ("frames end: %p->%p\n", p, logbuffer->data); exit(0);}
	while (data->count) {
		emit_method_as_ptr (prof, logbuffer, data->methods [--data->count]);
	}
}

static void
gc_alloc (MonoProfiler *prof, MonoObject *obj, MonoClass *klass)
{
	uint64_t now;
	uintptr_t len;
	int do_bt = (nocalls && InterlockedRead (&runtime_inited) && !notraces)? TYPE_ALLOC_BT: 0;
	FrameData data;
	LogBuffer *logbuffer;
	len = mono_object_get_size (obj);
	/* account for object alignment in the heap */
	len += 7;
	len &= ~7;
	if (do_bt)
		collect_bt (&data);
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* klass */ +
		LEB128_SIZE /* obj */ +
		LEB128_SIZE /* size */ +
		(do_bt ? (
			LEB128_SIZE /* flags */ +
			LEB128_SIZE /* count */ +
			data.count * (
				LEB128_SIZE /* method */
			)
		) : 0)
	);
	now = current_time ();
	ENTER_LOG (logbuffer, "gcalloc");
	emit_byte (logbuffer, do_bt | TYPE_ALLOC);
	emit_time (logbuffer, now);
	emit_ptr (logbuffer, klass);
	emit_obj (logbuffer, obj);
	emit_value (logbuffer, len);
	if (do_bt)
		emit_bt (prof, logbuffer, &data);
	EXIT_LOG (logbuffer);
	if (logbuffer->next)
		safe_send (prof, logbuffer);
	process_requests (prof);
	//printf ("gc alloc %s at %p\n", mono_class_get_name (klass), obj);
}

static void
gc_moves (MonoProfiler *prof, void **objects, int num)
{
	int i;
	uint64_t now;
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* num */ +
		num * (
			LEB128_SIZE /* object */
		)
	);
	now = current_time ();
	ENTER_LOG (logbuffer, "gcmove");
	emit_byte (logbuffer, TYPE_GC_MOVE | TYPE_GC);
	emit_time (logbuffer, now);
	emit_value (logbuffer, num);
	for (i = 0; i < num; ++i)
		emit_obj (logbuffer, objects [i]);
	//printf ("gc moved %d objects\n", num/2);
	EXIT_LOG (logbuffer);
}

static void
gc_roots (MonoProfiler *prof, int num, void **objects, int *root_types, uintptr_t *extra_info)
{
	int i;
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* num */ +
		LEB128_SIZE /* collections */ +
		num * (
			LEB128_SIZE /* object */ +
			LEB128_SIZE /* root type */ +
			LEB128_SIZE /* extra info */
		)
	);
	ENTER_LOG (logbuffer, "gcroots");
	emit_byte (logbuffer, TYPE_HEAP_ROOT | TYPE_HEAP);
	emit_value (logbuffer, num);
	emit_value (logbuffer, mono_gc_collection_count (mono_gc_max_generation ()));
	for (i = 0; i < num; ++i) {
		emit_obj (logbuffer, objects [i]);
		emit_value (logbuffer, root_types [i]);
		emit_value (logbuffer, extra_info [i]);
	}
	EXIT_LOG (logbuffer);
}

static void
gc_handle (MonoProfiler *prof, int op, int type, uintptr_t handle, MonoObject *obj)
{
	int do_bt = nocalls && InterlockedRead (&runtime_inited) && !notraces;
	uint64_t now;
	FrameData data;

	if (do_bt)
		collect_bt (&data);

	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* type */ +
		LEB128_SIZE /* handle */ +
		(op == MONO_PROFILER_GC_HANDLE_CREATED ? (
			LEB128_SIZE /* obj */
		) : 0) +
		(do_bt ? (
			LEB128_SIZE /* flags */ +
			LEB128_SIZE /* count */ +
			data.count * (
				LEB128_SIZE /* method */
			)
		) : 0)
	);

	now = current_time ();
	ENTER_LOG (logbuffer, "gchandle");

	if (op == MONO_PROFILER_GC_HANDLE_CREATED)
		emit_byte (logbuffer, (do_bt ? TYPE_GC_HANDLE_CREATED_BT : TYPE_GC_HANDLE_CREATED) | TYPE_GC);
	else if (op == MONO_PROFILER_GC_HANDLE_DESTROYED)
		emit_byte (logbuffer, (do_bt ? TYPE_GC_HANDLE_DESTROYED_BT : TYPE_GC_HANDLE_DESTROYED) | TYPE_GC);
	else
		g_assert_not_reached ();

	emit_time (logbuffer, now);
	emit_value (logbuffer, type);
	emit_value (logbuffer, handle);

	if (op == MONO_PROFILER_GC_HANDLE_CREATED)
		emit_obj (logbuffer, obj);

	if (do_bt)
		emit_bt (prof, logbuffer, &data);

	EXIT_LOG (logbuffer);
	process_requests (prof);
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
	p = (char *)malloc (strlen (buf) + 1);
	strcpy (p, buf);
	return p;
}

static void
image_loaded (MonoProfiler *prof, MonoImage *image, int result)
{
	uint64_t now;
	const char *name;
	int nlen;
	LogBuffer *logbuffer;
	if (result != MONO_PROFILE_OK)
		return;
	name = mono_image_get_filename (image);
	nlen = strlen (name) + 1;
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* image */ +
		LEB128_SIZE /* flags */ +
		nlen /* name */
	);
	now = current_time ();
	ENTER_LOG (logbuffer, "image");
	emit_byte (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_IMAGE);
	emit_ptr (logbuffer, image);
	emit_value (logbuffer, 0); /* flags */
	memcpy (logbuffer->data, name, nlen);
	logbuffer->data += nlen;
	//printf ("loaded image %p (%s)\n", image, name);
	EXIT_LOG (logbuffer);
	if (logbuffer->next)
		safe_send (prof, logbuffer);
	process_requests (prof);
}

static void
image_unloaded (MonoProfiler *prof, MonoImage *image)
{
	const char *name = mono_image_get_filename (image);
	int nlen = strlen (name) + 1;
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* image */ +
		LEB128_SIZE /* flags */ +
		nlen /* name */
	);
	uint64_t now = current_time ();

	ENTER_LOG (logbuffer, "image-unload");
	emit_byte (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_IMAGE);
	emit_ptr (logbuffer, image);
	emit_value (logbuffer, 0); /* flags */
	memcpy (logbuffer->data, name, nlen);
	logbuffer->data += nlen;
	EXIT_LOG (logbuffer);

	if (logbuffer->next)
		safe_send (prof, logbuffer);

	process_requests (prof);
}

static void
assembly_loaded (MonoProfiler *prof, MonoAssembly *assembly, int result)
{
	if (result != MONO_PROFILE_OK)
		return;

	char *name = mono_stringify_assembly_name (mono_assembly_get_name (assembly));
	int nlen = strlen (name) + 1;
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* assembly */ +
		LEB128_SIZE /* flags */ +
		nlen /* name */
	);
	uint64_t now = current_time ();

	ENTER_LOG (logbuffer, "assembly-load");
	emit_byte (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_ASSEMBLY);
	emit_ptr (logbuffer, assembly);
	emit_value (logbuffer, 0); /* flags */
	memcpy (logbuffer->data, name, nlen);
	logbuffer->data += nlen;
	EXIT_LOG (logbuffer);

	mono_free (name);

	if (logbuffer->next)
		safe_send (prof, logbuffer);

	process_requests (prof);
}

static void
assembly_unloaded (MonoProfiler *prof, MonoAssembly *assembly)
{
	char *name = mono_stringify_assembly_name (mono_assembly_get_name (assembly));
	int nlen = strlen (name) + 1;
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* assembly */ +
		LEB128_SIZE /* flags */ +
		nlen /* name */
	);
	uint64_t now = current_time ();

	ENTER_LOG (logbuffer, "assembly-unload");
	emit_byte (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_ASSEMBLY);
	emit_ptr (logbuffer, assembly);
	emit_value (logbuffer, 0); /* flags */
	memcpy (logbuffer->data, name, nlen);
	logbuffer->data += nlen;
	EXIT_LOG (logbuffer);

	mono_free (name);

	if (logbuffer->next)
		safe_send (prof, logbuffer);

	process_requests (prof);
}

static void
class_loaded (MonoProfiler *prof, MonoClass *klass, int result)
{
	uint64_t now;
	char *name;
	int nlen;
	MonoImage *image;
	LogBuffer *logbuffer;
	if (result != MONO_PROFILE_OK)
		return;
	if (InterlockedRead (&runtime_inited))
		name = mono_type_get_name (mono_class_get_type (klass));
	else
		name = type_name (klass);
	nlen = strlen (name) + 1;
	image = mono_class_get_image (klass);
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* klass */ +
		LEB128_SIZE /* image */ +
		LEB128_SIZE /* flags */ +
		nlen /* name */
	);
	now = current_time ();
	ENTER_LOG (logbuffer, "class");
	emit_byte (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_CLASS);
	emit_ptr (logbuffer, klass);
	emit_ptr (logbuffer, image);
	emit_value (logbuffer, 0); /* flags */
	memcpy (logbuffer->data, name, nlen);
	logbuffer->data += nlen;
	//printf ("loaded class %p (%s)\n", klass, name);
	if (runtime_inited)
		mono_free (name);
	else
		free (name);
	EXIT_LOG (logbuffer);
	if (logbuffer->next)
		safe_send (prof, logbuffer);
	process_requests (prof);
}

static void
class_unloaded (MonoProfiler *prof, MonoClass *klass)
{
	char *name;

	if (InterlockedRead (&runtime_inited))
		name = mono_type_get_name (mono_class_get_type (klass));
	else
		name = type_name (klass);

	int nlen = strlen (name) + 1;
	MonoImage *image = mono_class_get_image (klass);
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* klass */ +
		LEB128_SIZE /* image */ +
		LEB128_SIZE /* flags */ +
		nlen /* name */
	);
	uint64_t now = current_time ();

	ENTER_LOG (logbuffer, "class-unload");
	emit_byte (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_CLASS);
	emit_ptr (logbuffer, klass);
	emit_ptr (logbuffer, image);
	emit_value (logbuffer, 0); /* flags */
	memcpy (logbuffer->data, name, nlen);
	logbuffer->data += nlen;
	EXIT_LOG (logbuffer);

	if (runtime_inited)
		mono_free (name);
	else
		free (name);

	if (logbuffer->next)
		safe_send (prof, logbuffer);

	process_requests (prof);
}

#ifndef DISABLE_HELPER_THREAD
static void process_method_enter_coverage (MonoProfiler *prof, MonoMethod *method);
#endif /* DISABLE_HELPER_THREAD */

static void
method_enter (MonoProfiler *prof, MonoMethod *method)
{
	uint64_t now = current_time ();

#ifndef DISABLE_HELPER_THREAD
	process_method_enter_coverage (prof, method);
#endif /* DISABLE_HELPER_THREAD */

	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* method */
	);
	if (logbuffer->call_depth++ > max_call_depth)
		return;
	ENTER_LOG (logbuffer, "enter");
	emit_byte (logbuffer, TYPE_ENTER | TYPE_METHOD);
	emit_time (logbuffer, now);
	emit_method (prof, logbuffer, method);
	EXIT_LOG (logbuffer);

	process_requests (prof);
}

static void
method_leave (MonoProfiler *prof, MonoMethod *method)
{
	uint64_t now;
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* method */
	);
	if (--logbuffer->call_depth > max_call_depth)
		return;
	now = current_time ();
	ENTER_LOG (logbuffer, "leave");
	emit_byte (logbuffer, TYPE_LEAVE | TYPE_METHOD);
	emit_time (logbuffer, now);
	emit_method (prof, logbuffer, method);
	EXIT_LOG (logbuffer);
	if (logbuffer->next)
		safe_send (prof, logbuffer);
	process_requests (prof);
}

static void
method_exc_leave (MonoProfiler *prof, MonoMethod *method)
{
	uint64_t now;
	LogBuffer *logbuffer;
	if (nocalls)
		return;
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* method */
	);
	if (--logbuffer->call_depth > max_call_depth)
		return;
	now = current_time ();
	ENTER_LOG (logbuffer, "eleave");
	emit_byte (logbuffer, TYPE_EXC_LEAVE | TYPE_METHOD);
	emit_time (logbuffer, now);
	emit_method (prof, logbuffer, method);
	EXIT_LOG (logbuffer);
	process_requests (prof);
}

static void
method_jitted (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *ji, int result)
{
	if (result != MONO_PROFILE_OK)
		return;

	register_method_local (prof, method, ji);

	process_requests (prof);
}

static void
code_buffer_new (MonoProfiler *prof, void *buffer, int size, MonoProfilerCodeBufferType type, void *data)
{
	uint64_t now;
	int nlen;
	char *name;
	LogBuffer *logbuffer;
	if (type == MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE) {
		name = (char *)data;
		nlen = strlen (name) + 1;
	} else {
		name = NULL;
		nlen = 0;
	}
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* type */ +
		LEB128_SIZE /* buffer */ +
		LEB128_SIZE /* size */ +
		(name ? (
			nlen /* name */
		) : 0)
	);
	now = current_time ();
	ENTER_LOG (logbuffer, "code buffer");
	emit_byte (logbuffer, TYPE_JITHELPER | TYPE_RUNTIME);
	emit_time (logbuffer, now);
	emit_value (logbuffer, type);
	emit_ptr (logbuffer, buffer);
	emit_value (logbuffer, size);
	if (name) {
		memcpy (logbuffer->data, name, nlen);
		logbuffer->data += nlen;
	}
	EXIT_LOG (logbuffer);
	process_requests (prof);
}

static void
throw_exc (MonoProfiler *prof, MonoObject *object)
{
	int do_bt = (nocalls && InterlockedRead (&runtime_inited) && !notraces)? TYPE_EXCEPTION_BT: 0;
	uint64_t now;
	FrameData data;
	LogBuffer *logbuffer;
	if (do_bt)
		collect_bt (&data);
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* object */ +
		(do_bt ? (
			LEB128_SIZE /* flags */ +
			LEB128_SIZE /* count */ +
			data.count * (
				LEB128_SIZE /* method */
			)
		) : 0)
	);
	now = current_time ();
	ENTER_LOG (logbuffer, "throw");
	emit_byte (logbuffer, do_bt | TYPE_EXCEPTION);
	emit_time (logbuffer, now);
	emit_obj (logbuffer, object);
	if (do_bt)
		emit_bt (prof, logbuffer, &data);
	EXIT_LOG (logbuffer);
	process_requests (prof);
}

static void
clause_exc (MonoProfiler *prof, MonoMethod *method, int clause_type, int clause_num)
{
	uint64_t now;
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* clause type */ +
		LEB128_SIZE /* clause num */ +
		LEB128_SIZE /* method */
	);
	now = current_time ();
	ENTER_LOG (logbuffer, "clause");
	emit_byte (logbuffer, TYPE_EXCEPTION | TYPE_CLAUSE);
	emit_time (logbuffer, now);
	emit_value (logbuffer, clause_type);
	emit_value (logbuffer, clause_num);
	emit_method (prof, logbuffer, method);
	EXIT_LOG (logbuffer);

	process_requests (prof);
}

static void
monitor_event (MonoProfiler *profiler, MonoObject *object, MonoProfilerMonitorEvent event)
{
	int do_bt = (nocalls && InterlockedRead (&runtime_inited) && !notraces && event == MONO_PROFILER_MONITOR_CONTENTION)? TYPE_MONITOR_BT: 0;
	uint64_t now;
	FrameData data;
	LogBuffer *logbuffer;
	if (do_bt)
		collect_bt (&data);
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* object */ +
		(do_bt ? (
			LEB128_SIZE /* flags */ +
			LEB128_SIZE /* count */ +
			data.count * (
				LEB128_SIZE /* method */
			)
		) : 0)
	);
	now = current_time ();
	ENTER_LOG (logbuffer, "monitor");
	emit_byte (logbuffer, (event << 4) | do_bt | TYPE_MONITOR);
	emit_time (logbuffer, now);
	emit_obj (logbuffer, object);
	if (do_bt)
		emit_bt (profiler, logbuffer, &data);
	EXIT_LOG (logbuffer);
	process_requests (profiler);
}

static void
thread_start (MonoProfiler *prof, uintptr_t tid)
{
	//printf ("thread start %p\n", (void*)tid);
	init_thread ();

	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* tid */ +
		LEB128_SIZE /* flags */
	);
	uint64_t now = current_time ();

	ENTER_LOG (logbuffer, "thread-start");
	emit_byte (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_THREAD);
	emit_ptr (logbuffer, (void*) tid);
	emit_value (logbuffer, 0); /* flags */
	EXIT_LOG (logbuffer);

	if (logbuffer->next)
		safe_send (prof, logbuffer);

	process_requests (prof);
}

static void
thread_end (MonoProfiler *prof, uintptr_t tid)
{
	if (TLS_GET (LogBuffer, tlsbuffer)) {
		LogBuffer *logbuffer = ensure_logbuf (
			EVENT_SIZE /* event */ +
			LEB128_SIZE /* time */ +
			EVENT_SIZE /* type */ +
			LEB128_SIZE /* tid */ +
			LEB128_SIZE /* flags */
		);
		uint64_t now = current_time ();

		ENTER_LOG (logbuffer, "thread-end");
		emit_byte (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
		emit_time (logbuffer, now);
		emit_byte (logbuffer, TYPE_THREAD);
		emit_ptr (logbuffer, (void*) tid);
		emit_value (logbuffer, 0); /* flags */
		EXIT_LOG (logbuffer);

		send_buffer (prof, TLS_GET (GPtrArray, tlsmethodlist), logbuffer);

		/* Don't process requests as the thread is detached from the runtime. */
	}

	TLS_SET (tlsbuffer, NULL);
	TLS_SET (tlsmethodlist, NULL);
}

static void
domain_loaded (MonoProfiler *prof, MonoDomain *domain, int result)
{
	if (result != MONO_PROFILE_OK)
		return;

	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* domain id */ +
		LEB128_SIZE /* flags */
	);
	uint64_t now = current_time ();

	ENTER_LOG (logbuffer, "domain-start");
	emit_byte (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_DOMAIN);
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_domain_get_id (domain));
	emit_value (logbuffer, 0); /* flags */
	EXIT_LOG (logbuffer);

	if (logbuffer->next)
		safe_send (prof, logbuffer);

	process_requests (prof);
}

static void
domain_unloaded (MonoProfiler *prof, MonoDomain *domain)
{
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* domain id */ +
		LEB128_SIZE /* flags */
	);
	uint64_t now = current_time ();

	ENTER_LOG (logbuffer, "domain-end");
	emit_byte (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_DOMAIN);
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_domain_get_id (domain));
	emit_value (logbuffer, 0); /* flags */
	EXIT_LOG (logbuffer);

	if (logbuffer->next)
		safe_send (prof, logbuffer);

	process_requests (prof);
}

static void
domain_name (MonoProfiler *prof, MonoDomain *domain, const char *name)
{
	int nlen = strlen (name) + 1;
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* domain id */ +
		LEB128_SIZE /* flags */ +
		nlen /* name */
	);
	uint64_t now = current_time ();

	ENTER_LOG (logbuffer, "domain-name");
	emit_byte (logbuffer, TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_DOMAIN);
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_domain_get_id (domain));
	emit_value (logbuffer, 0); /* flags */
	memcpy (logbuffer->data, name, nlen);
	logbuffer->data += nlen;
	EXIT_LOG (logbuffer);

	if (logbuffer->next)
		safe_send (prof, logbuffer);

	process_requests (prof);
}

static void
context_loaded (MonoProfiler *prof, MonoAppContext *context)
{
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* context id */ +
		LEB128_SIZE /* flags */ +
		LEB128_SIZE /* domain id */
	);
	uint64_t now = current_time ();

	ENTER_LOG (logbuffer, "context-start");
	emit_byte (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_CONTEXT);
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_context_get_id (context));
	emit_value (logbuffer, 0); /* flags */
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_context_get_domain_id (context));
	EXIT_LOG (logbuffer);

	if (logbuffer->next)
		safe_send (prof, logbuffer);

	process_requests (prof);
}

static void
context_unloaded (MonoProfiler *prof, MonoAppContext *context)
{
	LogBuffer *logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* context id */ +
		LEB128_SIZE /* flags */ +
		LEB128_SIZE /* domain id */
	);
	uint64_t now = current_time ();

	ENTER_LOG (logbuffer, "context-end");
	emit_byte (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_CONTEXT);
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_context_get_id (context));
	emit_value (logbuffer, 0); /* flags */
	emit_ptr (logbuffer, (void*)(uintptr_t) mono_context_get_domain_id (context));
	EXIT_LOG (logbuffer);

	if (logbuffer->next)
		safe_send (prof, logbuffer);

	process_requests (prof);
}

static void
thread_name (MonoProfiler *prof, uintptr_t tid, const char *name)
{
	int len = strlen (name) + 1;
	uint64_t now;
	LogBuffer *logbuffer;
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		EVENT_SIZE /* type */ +
		LEB128_SIZE /* tid */ +
		LEB128_SIZE /* flags */ +
		len /* name */
	);
	now = current_time ();
	ENTER_LOG (logbuffer, "tname");
	emit_byte (logbuffer, TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_THREAD);
	emit_ptr (logbuffer, (void*)tid);
	emit_value (logbuffer, 0); /* flags */
	memcpy (logbuffer->data, name, len);
	logbuffer->data += len;
	EXIT_LOG (logbuffer);

	if (logbuffer->next)
		safe_send (prof, logbuffer);

	process_requests (prof);
}

typedef struct {
	MonoMethod *method;
	MonoDomain *domain;
	void *base_address;
	int offset;
} AsyncFrameInfo;

typedef struct {
	int count;
	AsyncFrameInfo *data;
} AsyncFrameData;

static mono_bool
async_walk_stack (MonoMethod *method, MonoDomain *domain, void *base_address, int offset, void *data)
{
	AsyncFrameData *frame = (AsyncFrameData *)data;
	if (frame->count < num_frames) {
		frame->data [frame->count].method = method;
		frame->data [frame->count].domain = domain;
		frame->data [frame->count].base_address = base_address;
		frame->data [frame->count].offset = offset;
		// printf ("In %d at %p (dom %p) (native: %p)\n", frame->count, method, domain, base_address);
		frame->count++;
	}
	return frame->count == num_frames;
}

/*
(type | frame count), tid, time, ip, [method, domain, base address, offset] * frames
*/
#define SAMPLE_EVENT_SIZE_IN_SLOTS(FRAMES) (4 + (FRAMES) * 4)

static void
mono_sample_hit (MonoProfiler *profiler, unsigned char *ip, void *context)
{
	StatBuffer *sbuf;
	AsyncFrameInfo frames [num_frames];
	AsyncFrameData bt_data = { 0, &frames [0]};
	uint64_t now;
	uintptr_t *data, *new_data, *old_data;
	uintptr_t elapsed;
	int timedout = 0;
	int i;
	if (in_shutdown)
		return;
	now = current_time ();

	mono_stack_walk_async_safe (&async_walk_stack, context, &bt_data);

	elapsed = (now - profiler->startup_time) / 10000;
	if (do_debug) {
		int len;
		char buf [256];
		snprintf (buf, sizeof (buf), "hit at %p in thread %p after %llu ms\n", ip, (void*)thread_id (), (unsigned long long int)elapsed/100);
		len = strlen (buf);
		ign_res (write (2, buf, len));
	}
	sbuf = profiler->stat_buffers;
	if (!sbuf)
		return;
	/* flush the buffer at 1 second intervals */
	if (sbuf->data > sbuf->buf && (elapsed - sbuf->buf [2]) > 100000) {
		timedout = 1;
	}
	/* overflow: 400 slots is a big enough number to reduce the chance of losing this event if many
	 * threads hit this same spot at the same time
	 */
	if (timedout || (sbuf->data + 400 >= sbuf->data_end)) {
		StatBuffer *oldsb, *foundsb;
		sbuf = create_stat_buffer ();
		do {
			oldsb = profiler->stat_buffers;
			sbuf->next = oldsb;
			foundsb = (StatBuffer *)InterlockedCompareExchangePointer ((void * volatile*)&profiler->stat_buffers, sbuf, oldsb);
		} while (foundsb != oldsb);
		if (do_debug)
			ign_res (write (2, "overflow\n", 9));
		/* notify the helper thread */
		if (sbuf->next->next) {
			char c = 0;
			ign_res (write (profiler->pipes [1], &c, 1));
			if (do_debug)
				ign_res (write (2, "notify\n", 7));
		}
	}
	do {
		old_data = sbuf->data;
		new_data = old_data + SAMPLE_EVENT_SIZE_IN_SLOTS (bt_data.count);
		data = (uintptr_t *)InterlockedCompareExchangePointer ((void * volatile*)&sbuf->data, new_data, old_data);
	} while (data != old_data);
	if (old_data >= sbuf->data_end)
		return; /* lost event */
	old_data [0] = 1 | (sample_type << 16) | (bt_data.count << 8);
	old_data [1] = thread_id ();
	old_data [2] = elapsed;
	old_data [3] = (uintptr_t)ip;
	for (i = 0; i < bt_data.count; ++i) {
		old_data [4 + 4 * i + 0] = (uintptr_t)frames [i].method;
		old_data [4 + 4 * i + 1] = (uintptr_t)frames [i].domain;
		old_data [4 + 4 * i + 2] = (uintptr_t)frames [i].base_address;
		old_data [4 + 4 * i + 3] = (uintptr_t)frames [i].offset;
	}
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
	/* should not happen */
	printf ("failed code page store\n");
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
		n = (uintptr_t *)calloc (sizeof (uintptr_t) * size_code_pages, 1);
		for (i = 0; i < old_size; ++i) {
			if (code_pages [i])
				add_code_page (n, size_code_pages, code_pages [i]);
		}
		if (code_pages)
			free (code_pages);
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
	uint64_t now;
	LogBuffer *logbuffer;
	int len;
	len = strlen (filename) + 1;
	now = current_time ();
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */ +
		LEB128_SIZE /* load address */ +
		LEB128_SIZE /* offset */ +
		LEB128_SIZE /* size */ +
		nlen /* file name */
	);
	emit_byte (logbuffer, TYPE_SAMPLE | TYPE_SAMPLE_UBIN);
	emit_time (logbuffer, now);
	emit_svalue (logbuffer, load_addr);
	emit_uvalue (logbuffer, offset);
	emit_uvalue (logbuffer, size);
	memcpy (logbuffer->data, filename, len);
	logbuffer->data += len;
}
#endif

static void
dump_usym (const char *name, uintptr_t value, uintptr_t size)
{
	LogBuffer *logbuffer;
	int len;
	len = strlen (name) + 1;
	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* value */ +
		LEB128_SIZE /* size */ +
		len /* name */
	);
	emit_byte (logbuffer, TYPE_SAMPLE | TYPE_SAMPLE_USYM);
	emit_ptr (logbuffer, (void*)value);
	emit_value (logbuffer, size);
	memcpy (logbuffer->data, name, len);
	logbuffer->data += len;
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
		//printf ("symbol %s at %d\n", sym, symbols [i].st_value);
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
		//printf ("section header: %d\n", sheader->sh_type);
		if (sheader->sh_type == SHT_SYMTAB) {
			symtabh = sheader;
			strtabh = (void*)((char*)data + header->e_shoff + sheader->sh_link * header->e_shentsize);
			/*printf ("symtab section header: %d, .strstr: %d\n", i, sheader->sh_link);*/
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
	MonoProfiler *prof = data;
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
	for (obj = prof->binary_objects; obj; obj = obj->next) {
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
	obj = calloc (sizeof (BinaryObject), 1);
	obj->addr = (void*)info->dlpi_addr;
	obj->name = pstrdup (filename);
	obj->next = prof->binary_objects;
	prof->binary_objects = obj;
	//printf ("loaded file: %s at %p, segments: %d\n", filename, (void*)info->dlpi_addr, info->dlpi_phnum);
	a = NULL;
	for (i = 0; i < info->dlpi_phnum; ++i) {
		//printf ("segment type %d file offset: %d, size: %d\n", info->dlpi_phdr[i].p_type, info->dlpi_phdr[i].p_offset, info->dlpi_phdr[i].p_memsz);
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
	if (read_elf_symbols (prof, filename, (void*)info->dlpi_addr))
		return 0;
	if (!info->dlpi_name || !info->dlpi_name[0])
		return 0;
	if (!dyn)
		return 0;
	for (i = 0; dyn [i].d_tag != DT_NULL; ++i) {
		if (dyn [i].d_tag == DT_SYMTAB) {
			if (symtab && do_debug)
				printf ("multiple symtabs: %d\n", i);
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
load_binaries (MonoProfiler *prof)
{
	dl_iterate_phdr (elf_dl_callback, prof);
	return 1;
}
#else
static int
load_binaries (MonoProfiler *prof)
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
			free (names);
			return p;
		}
		*/
	}
#endif
	return NULL;
}

static void
dump_unmanaged_coderefs (MonoProfiler *prof)
{
	int i;
	const char* last_symbol;
	uintptr_t addr, page_end;

	if (load_binaries (prof))
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
			//printf ("found symbol at %p: %s\n", (void*)addr, sym);
		}
	}
}

static gint
compare_sample_events (gconstpointer a, gconstpointer b)
{
	uintptr_t tid1 = (*(uintptr_t **) a) [1];
	uintptr_t tid2 = (*(uintptr_t **) b) [1];

	return tid1 > tid2 ? 1 :
	       tid1 < tid2 ? -1 :
	       0;
}

static void
dump_sample_hits (MonoProfiler *prof, StatBuffer *sbuf)
{
	LogBuffer *logbuffer;
	if (!sbuf)
		return;
	if (sbuf->next) {
		dump_sample_hits (prof, sbuf->next);
		free_buffer (sbuf->next, sbuf->next->size);
		sbuf->next = NULL;
	}

	g_ptr_array_set_size (prof->sorted_sample_events, 0);

	for (uintptr_t *sample = sbuf->buf; sample < sbuf->data;) {
		int count = sample [0] & 0xff;
		int mbt_count = (sample [0] & 0xff00) >> 8;

		if (sample + SAMPLE_EVENT_SIZE_IN_SLOTS (mbt_count) > sbuf->data)
			break;

		g_ptr_array_add (prof->sorted_sample_events, sample);

		sample += count + 3 + 4 * mbt_count;
	}

	g_ptr_array_sort (prof->sorted_sample_events, compare_sample_events);

	for (guint sidx = 0; sidx < prof->sorted_sample_events->len; sidx++) {
		uintptr_t *sample = (uintptr_t *)g_ptr_array_index (prof->sorted_sample_events, sidx);
		int count = sample [0] & 0xff;
		int mbt_count = (sample [0] & 0xff00) >> 8;
		int type = sample [0] >> 16;
		uintptr_t *managed_sample_base = sample + count + 3;
		uintptr_t thread_id = sample [1];

		for (int i = 0; i < mbt_count; ++i) {
			MonoMethod *method = (MonoMethod*)managed_sample_base [i * 4 + 0];
			MonoDomain *domain = (MonoDomain*)managed_sample_base [i * 4 + 1];
			void *address = (void*)managed_sample_base [i * 4 + 2];

			if (!method) {
				MonoJitInfo *ji = mono_jit_info_table_find (domain, (char *)address);

				if (ji)
					managed_sample_base [i * 4 + 0] = (uintptr_t)mono_jit_info_get_method (ji);
			}
		}

		logbuffer = ensure_logbuf (
			EVENT_SIZE /* event */ +
			LEB128_SIZE /* type */ +
			LEB128_SIZE /* time */ +
			LEB128_SIZE /* tid */ +
			LEB128_SIZE /* count */ +
			count * (
				LEB128_SIZE /* ip */
			) +
			LEB128_SIZE /* managed count */ +
			mbt_count * (
				LEB128_SIZE /* method */ +
				LEB128_SIZE /* il offset */ +
				LEB128_SIZE /* native offset */
			)
		);
		emit_byte (logbuffer, TYPE_SAMPLE | TYPE_SAMPLE_HIT);
		emit_value (logbuffer, type);
		emit_uvalue (logbuffer, prof->startup_time + (uint64_t)sample [2] * (uint64_t)10000);
		emit_ptr (logbuffer, (void *) thread_id);
		emit_value (logbuffer, count);
		for (int i = 0; i < count; ++i) {
			emit_ptr (logbuffer, (void*)sample [i + 3]);
			add_code_pointer (sample [i + 3]);
		}

		sample += count + 3;
		/* new in data version 6 */
		emit_uvalue (logbuffer, mbt_count);
		for (int i = 0; i < mbt_count; ++i) {
			MonoMethod *method = (MonoMethod *) sample [i * 4 + 0];
			uintptr_t native_offset = sample [i * 4 + 3];

			emit_method (prof, logbuffer, method);
			emit_svalue (logbuffer, 0); /* il offset will always be 0 from now on */
			emit_svalue (logbuffer, native_offset);
		}
	}

	dump_unmanaged_coderefs (prof);
}

#if USE_PERF_EVENTS

static int
mono_cpu_count (void)
{
	int count = 0;
#ifdef PLATFORM_ANDROID
	/* Android tries really hard to save power by powering off CPUs on SMP phones which
	 * means the normal way to query cpu count returns a wrong value with userspace API.
	 * Instead we use /sys entries to query the actual hardware CPU count.
	 */
	char buffer[8] = {'\0'};
	int present = open ("/sys/devices/system/cpu/present", O_RDONLY);
	/* Format of the /sys entry is a cpulist of indexes which in the case
	 * of present is always of the form "0-(n-1)" when there is more than
	 * 1 core, n being the number of CPU cores in the system. Otherwise
	 * the value is simply 0
	 */
	if (present != -1 && read (present, (char*)buffer, sizeof (buffer)) > 3)
		count = strtol (((char*)buffer) + 2, NULL, 10);
	if (present != -1)
		close (present);
	if (count > 0)
		return count + 1;
#endif
#ifdef HAVE_SCHED_GETAFFINITY
	{
		cpu_set_t set;
		if (sched_getaffinity (getpid (), sizeof (set), &set) == 0)
			return CPU_COUNT (&set);
	}
#endif
#ifdef _SC_NPROCESSORS_ONLN
	count = sysconf (_SC_NPROCESSORS_ONLN);
	if (count > 0)
		return count;
#endif
#ifdef USE_SYSCTL
	{
		int mib [2];
		size_t len = sizeof (int);
		mib [0] = CTL_HW;
		mib [1] = HW_NCPU;
		if (sysctl (mib, 2, &count, &len, NULL, 0) == 0)
			return count;
	}
#endif
#ifdef HOST_WIN32
	{
		SYSTEM_INFO info;
		GetSystemInfo (&info);
		return info.dwNumberOfProcessors;
	}
#endif
	/* FIXME: warn */
	return 1;
}

typedef struct {
	int perf_fd;
	unsigned int prev_pos;
	void *mmap_base;
	struct perf_event_mmap_page *page_desc;
} PerfData ;

static PerfData *perf_data = NULL;
static int num_perf;
#define PERF_PAGES_SHIFT 4
static int num_pages = 1 << PERF_PAGES_SHIFT;
static unsigned int mmap_mask;

typedef struct {
	struct perf_event_header h;
	uint64_t ip;
	uint32_t pid;
	uint32_t tid;
	uint64_t timestamp;
	uint64_t period;
	uint64_t nframes;
} PSample;

static int
perf_event_syscall (struct perf_event_attr *attr, pid_t pid, int cpu, int group_fd, unsigned long flags)
{
	attr->size = PERF_ATTR_SIZE_VER0;
	//printf ("perf attr size: %d\n", attr->size);
#if defined(__x86_64__)
	return syscall(/*__NR_perf_event_open*/ 298, attr, pid, cpu, group_fd, flags);
#elif defined(__i386__)
	return syscall(/*__NR_perf_event_open*/ 336, attr, pid, cpu, group_fd, flags);
#elif defined(__arm__) || defined (__aarch64__)
	return syscall(/*__NR_perf_event_open*/ 364, attr, pid, cpu, group_fd, flags);
#else
	return -1;
#endif
}

static int
setup_perf_map (PerfData *perf)
{
	perf->mmap_base = mmap (NULL, (num_pages + 1) * getpagesize (), PROT_READ|PROT_WRITE, MAP_SHARED, perf->perf_fd, 0);
	if (perf->mmap_base == MAP_FAILED) {
		if (do_debug)
			printf ("failed mmap\n");
		return 0;
	}
	perf->page_desc = perf->mmap_base;
	if (do_debug)
		printf ("mmap version: %d\n", perf->page_desc->version);
	return 1;
}

static void
dump_perf_hits (MonoProfiler *prof, void *buf, int size)
{
	LogBuffer *logbuffer;
	int count = 1;
	int mbt_count = 0;
	void *end = (char*)buf + size;
	int samples = 0;
	int pid = getpid ();

	while (buf < end) {
		PSample *s = buf;
		if (s->h.size == 0)
			break;
		if (pid != s->pid) {
			if (do_debug)
				printf ("event for different pid: %d\n", s->pid);
			buf = (char*)buf + s->h.size;
			continue;
		}
		/*ip = (void*)s->ip;
		printf ("sample: %d, size: %d, ip: %p (%s), timestamp: %llu, nframes: %llu\n",
			s->h.type, s->h.size, ip, symbol_for (ip), s->timestamp, s->nframes);*/
		logbuffer = ensure_logbuf (
			EVENT_SIZE /* event */ +
			LEB128_SIZE /* type */ +
			LEB128_SIZE /* time */ +
			LEB128_SIZE /* tid */ +
			LEB128_SIZE /* count */ +
			count * (
				LEB128_SIZE /* ip */
			) +
			LEB128_SIZE /* managed count */ +
			mbt_count * (
				LEB128_SIZE /* method */ +
				LEB128_SIZE /* il offset */ +
				LEB128_SIZE /* native offset */
			)
		);
		emit_byte (logbuffer, TYPE_SAMPLE | TYPE_SAMPLE_HIT);
		emit_value (logbuffer, sample_type);
		emit_uvalue (logbuffer, s->timestamp - prof->startup_time);
		/*
		 * No useful thread ID to write here, since throughout the
		 * profiler we use pthread_self () but the ID we get from
		 * perf is the kernel's thread ID.
		 */
		emit_ptr (logbuffer, 0);
		emit_value (logbuffer, count);
		emit_ptr (logbuffer, (void*)(uintptr_t)s->ip);
		add_code_pointer (s->ip);
		/* no support here yet for the managed backtrace */
		emit_uvalue (logbuffer, mbt_count);
		buf = (char*)buf + s->h.size;
		samples++;
	}
	if (do_debug)
		printf ("dumped %d samples\n", samples);
	dump_unmanaged_coderefs (prof);
}

/* read events from the ring buffer */
static int
read_perf_mmap (MonoProfiler* prof, int cpu)
{
	PerfData *perf = perf_data + cpu;
	unsigned char *buf;
	unsigned char *data = (unsigned char*)perf->mmap_base + getpagesize ();
	unsigned int head = perf->page_desc->data_head;
	int diff, size;
	unsigned int old;

	mono_memory_read_barrier ();

	old = perf->prev_pos;
	diff = head - old;
	if (diff < 0) {
		if (do_debug)
			printf ("lost mmap events: old: %d, head: %d\n", old, head);
		old = head;
	}
	size = head - old;
	if ((old & mmap_mask) + size != (head & mmap_mask)) {
		buf = data + (old & mmap_mask);
		size = mmap_mask + 1 - (old & mmap_mask);
		old += size;
		/* size bytes at buf */
		if (do_debug)
			printf ("found1 bytes of events: %d\n", size);
		dump_perf_hits (prof, buf, size);
	}
	buf = data + (old & mmap_mask);
	size = head - old;
	/* size bytes at buf */
	if (do_debug)
		printf ("found bytes of events: %d\n", size);
	dump_perf_hits (prof, buf, size);
	old += size;
	perf->prev_pos = old;
	perf->page_desc->data_tail = old;
	return 0;
}

static int
setup_perf_event_for_cpu (PerfData *perf, int cpu)
{
	struct perf_event_attr attr;
	memset (&attr, 0, sizeof (attr));
	attr.type = PERF_TYPE_HARDWARE;
	switch (sample_type) {
	case SAMPLE_CYCLES: attr.config = PERF_COUNT_HW_CPU_CYCLES; break;
	case SAMPLE_INSTRUCTIONS: attr.config = PERF_COUNT_HW_INSTRUCTIONS; break;
	case SAMPLE_CACHE_MISSES: attr.config = PERF_COUNT_HW_CACHE_MISSES; break;
	case SAMPLE_CACHE_REFS: attr.config = PERF_COUNT_HW_CACHE_REFERENCES; break;
	case SAMPLE_BRANCHES: attr.config = PERF_COUNT_HW_BRANCH_INSTRUCTIONS; break;
	case SAMPLE_BRANCH_MISSES: attr.config = PERF_COUNT_HW_BRANCH_MISSES; break;
	default: attr.config = PERF_COUNT_HW_CPU_CYCLES; break;
	}
	attr.sample_type = PERF_SAMPLE_IP | PERF_SAMPLE_TID | PERF_SAMPLE_PERIOD | PERF_SAMPLE_TIME;
//	attr.sample_type |= PERF_SAMPLE_CALLCHAIN;
	attr.read_format = PERF_FORMAT_TOTAL_TIME_ENABLED | PERF_FORMAT_TOTAL_TIME_RUNNING | PERF_FORMAT_ID;
	attr.inherit = 1;
	attr.freq = 1;
	attr.sample_freq = sample_freq;

	perf->perf_fd = perf_event_syscall (&attr, getpid (), cpu, -1, 0);
	if (do_debug)
		printf ("perf fd: %d, freq: %d, event: %llu\n", perf->perf_fd, sample_freq, attr.config);
	if (perf->perf_fd < 0) {
		if (perf->perf_fd == -EPERM) {
			fprintf (stderr, "Perf syscall denied, do \"echo 1 > /proc/sys/kernel/perf_event_paranoid\" as root to enable.\n");
		} else {
			if (do_debug)
				perror ("open perf event");
		}
		return 0;
	}
	if (!setup_perf_map (perf)) {
		close (perf->perf_fd);
		perf->perf_fd = -1;
		return 0;
	}
	return 1;
}

static int
setup_perf_event (void)
{
	int i, count = 0;
	mmap_mask = num_pages * getpagesize () - 1;
	num_perf = mono_cpu_count ();
	perf_data = calloc (num_perf, sizeof (PerfData));
	for (i = 0; i < num_perf; ++i) {
		count += setup_perf_event_for_cpu (perf_data + i, i);
	}
	if (count)
		return 1;
	free (perf_data);
	perf_data = NULL;
	return 0;
}

#endif /* USE_PERF_EVENTS */

#ifndef DISABLE_HELPER_THREAD

typedef struct MonoCounterAgent {
	MonoCounter *counter;
	// MonoCounterAgent specific data :
	void *value;
	size_t value_size;
	short index;
	short emitted;
	struct MonoCounterAgent *next;
} MonoCounterAgent;

static MonoCounterAgent* counters;
static gboolean counters_initialized = FALSE;
static int counters_index = 1;
static mono_mutex_t counters_mutex;

static void
counters_add_agent (MonoCounter *counter)
{
	MonoCounterAgent *agent, *item;

	if (!counters_initialized)
		return;

	mono_os_mutex_lock (&counters_mutex);

	for (agent = counters; agent; agent = agent->next) {
		if (agent->counter == counter) {
			agent->value_size = 0;
			if (agent->value) {
				free (agent->value);
				agent->value = NULL;
			}
			mono_os_mutex_unlock (&counters_mutex);
			return;
		}
	}

	agent = (MonoCounterAgent *)malloc (sizeof (MonoCounterAgent));
	agent->counter = counter;
	agent->value = NULL;
	agent->value_size = 0;
	agent->index = counters_index++;
	agent->emitted = 0;
	agent->next = NULL;

	if (!counters) {
		counters = agent;
	} else {
		item = counters;
		while (item->next)
			item = item->next;
		item->next = agent;
	}

	mono_os_mutex_unlock (&counters_mutex);
}

static mono_bool
counters_init_foreach_callback (MonoCounter *counter, gpointer data)
{
	counters_add_agent (counter);
	return TRUE;
}

static void
counters_init (MonoProfiler *profiler)
{
	assert (!counters_initialized);

	mono_os_mutex_init (&counters_mutex);

	counters_initialized = TRUE;

	mono_counters_on_register (&counters_add_agent);
	mono_counters_foreach (counters_init_foreach_callback, NULL);
}

static void
counters_emit (MonoProfiler *profiler)
{
	MonoCounterAgent *agent;
	LogBuffer *logbuffer;
	int len = 0;
	int size =
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* len */
	;

	if (!counters_initialized)
		return;

	mono_os_mutex_lock (&counters_mutex);

	for (agent = counters; agent; agent = agent->next) {
		if (agent->emitted)
			continue;

		size +=
			LEB128_SIZE /* section */ +
			strlen (mono_counter_get_name (agent->counter)) + 1 /* name */ +
			LEB128_SIZE /* type */ +
			LEB128_SIZE /* unit */ +
			LEB128_SIZE /* variance */ +
			LEB128_SIZE /* index */
		;

		len += 1;
	}

	if (!len) {
		mono_os_mutex_unlock (&counters_mutex);
		return;
	}

	logbuffer = ensure_logbuf (size);

	ENTER_LOG (logbuffer, "counters");
	emit_byte (logbuffer, TYPE_SAMPLE_COUNTERS_DESC | TYPE_SAMPLE);
	emit_value (logbuffer, len);
	for (agent = counters; agent; agent = agent->next) {
		const char *name;

		if (agent->emitted)
			continue;

		name = mono_counter_get_name (agent->counter);
		emit_value (logbuffer, mono_counter_get_section (agent->counter));
		emit_string (logbuffer, name, strlen (name) + 1);
		emit_value (logbuffer, mono_counter_get_type (agent->counter));
		emit_value (logbuffer, mono_counter_get_unit (agent->counter));
		emit_value (logbuffer, mono_counter_get_variance (agent->counter));
		emit_value (logbuffer, agent->index);

		agent->emitted = 1;
	}
	EXIT_LOG (logbuffer);

	safe_send (profiler, logbuffer);

	mono_os_mutex_unlock (&counters_mutex);
}

static void
counters_sample (MonoProfiler *profiler, uint64_t timestamp)
{
	MonoCounterAgent *agent;
	MonoCounter *counter;
	LogBuffer *logbuffer;
	int type;
	int buffer_size;
	void *buffer;
	int size;

	if (!counters_initialized)
		return;

	counters_emit (profiler);

	buffer_size = 8;
	buffer = calloc (1, buffer_size);

	mono_os_mutex_lock (&counters_mutex);

	size =
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */
	;

	for (agent = counters; agent; agent = agent->next) {
		size +=
			LEB128_SIZE /* index */ +
			LEB128_SIZE /* type */ +
			mono_counter_get_size (agent->counter) /* value */
		;
	}

	size +=
		LEB128_SIZE /* stop marker */
	;

	logbuffer = ensure_logbuf (size);

	ENTER_LOG (logbuffer, "counters");
	emit_byte (logbuffer, TYPE_SAMPLE_COUNTERS | TYPE_SAMPLE);
	emit_uvalue (logbuffer, timestamp);
	for (agent = counters; agent; agent = agent->next) {
		size_t size;

		counter = agent->counter;

		size = mono_counter_get_size (counter);
		if (size < 0) {
			continue; // FIXME error
		} else if (size > buffer_size) {
			buffer_size = size;
			buffer = realloc (buffer, buffer_size);
		}

		memset (buffer, 0, buffer_size);

		if (mono_counters_sample (counter, buffer, size) < 0)
			continue; // FIXME error

		type = mono_counter_get_type (counter);

		if (!agent->value) {
			agent->value = calloc (1, size);
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
		emit_uvalue (logbuffer, type);
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
			assert (0);
		}

		if (type == MONO_COUNTER_STRING && size > agent->value_size) {
			agent->value = realloc (agent->value, size);
			agent->value_size = size;
		}

		if (size > 0)
			memcpy (agent->value, buffer, size);
	}
	free (buffer);

	emit_value (logbuffer, 0);
	EXIT_LOG (logbuffer);

	safe_send (profiler, logbuffer);

	mono_os_mutex_unlock (&counters_mutex);
}

typedef struct _PerfCounterAgent PerfCounterAgent;
struct _PerfCounterAgent {
	PerfCounterAgent *next;
	int index;
	char *category_name;
	char *name;
	int type;
	gint64 value;
	guint8 emitted;
	guint8 updated;
	guint8 deleted;
};

static PerfCounterAgent *perfcounters = NULL;

static void
perfcounters_emit (MonoProfiler *profiler)
{
	PerfCounterAgent *pcagent;
	LogBuffer *logbuffer;
	int len = 0;
	int size =
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* len */
	;

	for (pcagent = perfcounters; pcagent; pcagent = pcagent->next) {
		if (pcagent->emitted)
			continue;

		size +=
			LEB128_SIZE /* section */ +
			strlen (pcagent->category_name) + 1 /* category name */ +
			strlen (pcagent->name) + 1 /* name */ +
			LEB128_SIZE /* type */ +
			LEB128_SIZE /* unit */ +
			LEB128_SIZE /* variance */ +
			LEB128_SIZE /* index */
		;

		len += 1;
	}

	if (!len)
		return;

	logbuffer = ensure_logbuf (size);

	ENTER_LOG (logbuffer, "perfcounters");
	emit_byte (logbuffer, TYPE_SAMPLE_COUNTERS_DESC | TYPE_SAMPLE);
	emit_value (logbuffer, len);
	for (pcagent = perfcounters; pcagent; pcagent = pcagent->next) {
		if (pcagent->emitted)
			continue;

		emit_value (logbuffer, MONO_COUNTER_PERFCOUNTERS);
		emit_string (logbuffer, pcagent->category_name, strlen (pcagent->category_name) + 1);
		emit_string (logbuffer, pcagent->name, strlen (pcagent->name) + 1);
		emit_value (logbuffer, MONO_COUNTER_LONG);
		emit_value (logbuffer, MONO_COUNTER_RAW);
		emit_value (logbuffer, MONO_COUNTER_VARIABLE);
		emit_value (logbuffer, pcagent->index);

		pcagent->emitted = 1;
	}
	EXIT_LOG (logbuffer);

	safe_send (profiler, logbuffer);
}

static gboolean
perfcounters_foreach (char *category_name, char *name, unsigned char type, gint64 value, gpointer user_data)
{
	PerfCounterAgent *pcagent;

	for (pcagent = perfcounters; pcagent; pcagent = pcagent->next) {
		if (strcmp (pcagent->category_name, category_name) != 0 || strcmp (pcagent->name, name) != 0)
			continue;
		if (pcagent->value == value)
			return TRUE;

		pcagent->value = value;
		pcagent->updated = 1;
		pcagent->deleted = 0;
		return TRUE;
	}

	pcagent = g_new0 (PerfCounterAgent, 1);
	pcagent->next = perfcounters;
	pcagent->index = counters_index++;
	pcagent->category_name = g_strdup (category_name);
	pcagent->name = g_strdup (name);
	pcagent->type = (int) type;
	pcagent->value = value;
	pcagent->emitted = 0;
	pcagent->updated = 1;
	pcagent->deleted = 0;

	perfcounters = pcagent;

	return TRUE;
}

static void
perfcounters_sample (MonoProfiler *profiler, uint64_t timestamp)
{
	PerfCounterAgent *pcagent;
	LogBuffer *logbuffer;
	int size;

	if (!counters_initialized)
		return;

	mono_os_mutex_lock (&counters_mutex);

	/* mark all perfcounters as deleted, foreach will unmark them as necessary */
	for (pcagent = perfcounters; pcagent; pcagent = pcagent->next)
		pcagent->deleted = 1;

	mono_perfcounter_foreach (perfcounters_foreach, perfcounters);

	perfcounters_emit (profiler);

	size =
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* time */
	;

	for (pcagent = perfcounters; pcagent; pcagent = pcagent->next) {
		if (pcagent->deleted || !pcagent->updated)
			continue;

		size +=
			LEB128_SIZE /* index */ +
			LEB128_SIZE /* type */ +
			LEB128_SIZE /* value */
		;
	}

	size +=
		LEB128_SIZE /* stop marker */
	;

	logbuffer = ensure_logbuf (size);

	ENTER_LOG (logbuffer, "perfcounters");
	emit_byte (logbuffer, TYPE_SAMPLE_COUNTERS | TYPE_SAMPLE);
	emit_uvalue (logbuffer, timestamp);
	for (pcagent = perfcounters; pcagent; pcagent = pcagent->next) {
		if (pcagent->deleted || !pcagent->updated)
			continue;
		emit_uvalue (logbuffer, pcagent->index);
		emit_uvalue (logbuffer, MONO_COUNTER_LONG);
		emit_svalue (logbuffer, pcagent->value);

		pcagent->updated = 0;
	}

	emit_value (logbuffer, 0);
	EXIT_LOG (logbuffer);

	safe_send (profiler, logbuffer);

	mono_os_mutex_unlock (&counters_mutex);
}

static void
counters_and_perfcounters_sample (MonoProfiler *prof)
{
	static uint64_t start = -1;
	uint64_t now;

	if (start == -1)
		start = current_time ();

	now = current_time ();
	counters_sample (prof, (now - start) / 1000/ 1000);
	perfcounters_sample (prof, (now - start) / 1000/ 1000);
}

#define COVERAGE_DEBUG(x) if (debug_coverage) {x}
static mono_mutex_t coverage_mutex;
static MonoConcurrentHashTable *coverage_methods = NULL;
static MonoConcurrentHashTable *coverage_assemblies = NULL;
static MonoConcurrentHashTable *coverage_classes = NULL;

static MonoConcurrentHashTable *filtered_classes = NULL;
static MonoConcurrentHashTable *entered_methods = NULL;
static MonoConcurrentHashTable *image_to_methods = NULL;
static MonoConcurrentHashTable *suppressed_assemblies = NULL;
static gboolean coverage_initialized = FALSE;

static GPtrArray *coverage_data = NULL;
static int previous_offset = 0;

typedef struct _MethodNode MethodNode;
struct _MethodNode {
	MonoLockFreeQueueNode node;
	MonoMethod *method;
};

typedef struct _CoverageEntry CoverageEntry;
struct _CoverageEntry {
	int offset;
	int counter;
	char *filename;
	int line;
	int column;
};

static void
free_coverage_entry (gpointer data, gpointer userdata)
{
	CoverageEntry *entry = (CoverageEntry *)data;
	g_free (entry->filename);
	g_free (entry);
}

static void
obtain_coverage_for_method (MonoProfiler *prof, const MonoProfileCoverageEntry *entry)
{
	int offset = entry->iloffset - previous_offset;
	CoverageEntry *e = g_new (CoverageEntry, 1);

	previous_offset = entry->iloffset;

	e->offset = offset;
	e->counter = entry->counter;
	e->filename = g_strdup(entry->filename ? entry->filename : "");
	e->line = entry->line;
	e->column = entry->col;

	g_ptr_array_add (coverage_data, e);
}

static char *
parse_generic_type_names(char *name)
{
	char *new_name, *ret;
	int within_generic_declaration = 0, generic_members = 1;

	if (name == NULL || *name == '\0')
		return g_strdup ("");

	if (!(ret = new_name = (char *)calloc (strlen (name) * 4 + 1, sizeof (char))))
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

static int method_id;
static void
build_method_buffer (gpointer key, gpointer value, gpointer userdata)
{
	MonoMethod *method = (MonoMethod *)value;
	MonoProfiler *prof = (MonoProfiler *)userdata;
	MonoClass *klass;
	MonoImage *image;
	char *class_name;
	const char *image_name, *method_name, *sig, *first_filename;
	LogBuffer *logbuffer;
	guint i;

	previous_offset = 0;
	coverage_data = g_ptr_array_new ();

	mono_profiler_coverage_get (prof, method, obtain_coverage_for_method);

	klass = mono_method_get_class (method);
	image = mono_class_get_image (klass);
	image_name = mono_image_get_name (image);

	sig = mono_signature_get_desc (mono_method_signature (method), TRUE);
	class_name = parse_generic_type_names (mono_type_get_name (mono_class_get_type (klass)));
	method_name = mono_method_get_name (method);

	if (coverage_data->len != 0) {
		CoverageEntry *entry = (CoverageEntry *)coverage_data->pdata[0];
		first_filename = entry->filename ? entry->filename : "";
	} else
		first_filename = "";

	image_name = image_name ? image_name : "";
	sig = sig ? sig : "";
	method_name = method_name ? method_name : "";

	logbuffer = ensure_logbuf (
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
	ENTER_LOG (logbuffer, "coverage-methods");

	emit_byte (logbuffer, TYPE_COVERAGE_METHOD | TYPE_COVERAGE);
	emit_string (logbuffer, image_name, strlen (image_name) + 1);
	emit_string (logbuffer, class_name, strlen (class_name) + 1);
	emit_string (logbuffer, method_name, strlen (method_name) + 1);
	emit_string (logbuffer, sig, strlen (sig) + 1);
	emit_string (logbuffer, first_filename, strlen (first_filename) + 1);

	emit_uvalue (logbuffer, mono_method_get_token (method));
	emit_uvalue (logbuffer, method_id);
	emit_value (logbuffer, coverage_data->len);

	EXIT_LOG (logbuffer);
	safe_send (prof, logbuffer);

	for (i = 0; i < coverage_data->len; i++) {
		CoverageEntry *entry = (CoverageEntry *)coverage_data->pdata[i];

		logbuffer = ensure_logbuf (
			EVENT_SIZE /* event */ +
			LEB128_SIZE /* method id */ +
			LEB128_SIZE /* offset */ +
			LEB128_SIZE /* counter */ +
			LEB128_SIZE /* line */ +
			LEB128_SIZE /* column */
		);
		ENTER_LOG (logbuffer, "coverage-statement");

		emit_byte (logbuffer, TYPE_COVERAGE_STATEMENT | TYPE_COVERAGE);
		emit_uvalue (logbuffer, method_id);
		emit_uvalue (logbuffer, entry->offset);
		emit_uvalue (logbuffer, entry->counter);
		emit_uvalue (logbuffer, entry->line);
		emit_uvalue (logbuffer, entry->column);

		EXIT_LOG (logbuffer);
		safe_send (prof, logbuffer);
	}

	method_id++;

	g_free (class_name);

	g_ptr_array_foreach (coverage_data, free_coverage_entry, NULL);
	g_ptr_array_free (coverage_data, TRUE);
	coverage_data = NULL;
}

/* This empties the queue */
static guint
count_queue (MonoLockFreeQueue *queue)
{
	MonoLockFreeQueueNode *node;
	guint count = 0;

	while ((node = mono_lock_free_queue_dequeue (queue))) {
		count++;
		mono_lock_free_queue_node_free (node);
	}

	return count;
}

static void
build_class_buffer (gpointer key, gpointer value, gpointer userdata)
{
	MonoClass *klass = (MonoClass *)key;
	MonoLockFreeQueue *class_methods = (MonoLockFreeQueue *)value;
	MonoProfiler *prof = (MonoProfiler *)userdata;
	MonoImage *image;
	char *class_name;
	const char *assembly_name;
	int number_of_methods, partially_covered;
	guint fully_covered;
	LogBuffer *logbuffer;

	image = mono_class_get_image (klass);
	assembly_name = mono_image_get_name (image);
	class_name = mono_type_get_name (mono_class_get_type (klass));

	assembly_name = assembly_name ? assembly_name : "";
	number_of_methods = mono_class_num_methods (klass);
	fully_covered = count_queue (class_methods);
	/* We don't handle partial covered yet */
	partially_covered = 0;

	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		strlen (assembly_name) + 1 /* assembly name */ +
		strlen (class_name) + 1 /* class name */ +
		LEB128_SIZE /* no. methods */ +
		LEB128_SIZE /* fully covered */ +
		LEB128_SIZE /* partially covered */
	);

	ENTER_LOG (logbuffer, "coverage-class");
	emit_byte (logbuffer, TYPE_COVERAGE_CLASS | TYPE_COVERAGE);
	emit_string (logbuffer, assembly_name, strlen (assembly_name) + 1);
	emit_string (logbuffer, class_name, strlen (class_name) + 1);
	emit_uvalue (logbuffer, number_of_methods);
	emit_uvalue (logbuffer, fully_covered);
	emit_uvalue (logbuffer, partially_covered);
	EXIT_LOG (logbuffer);

	safe_send (prof, logbuffer);

	g_free (class_name);
}

static void
get_coverage_for_image (MonoImage *image, int *number_of_methods, guint *fully_covered, int *partially_covered)
{
	MonoLockFreeQueue *image_methods = (MonoLockFreeQueue *)mono_conc_hashtable_lookup (image_to_methods, image);

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
	MonoProfiler *prof = (MonoProfiler *)userdata;
	MonoImage *image = mono_assembly_get_image (assembly);
	LogBuffer *logbuffer;
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

	logbuffer = ensure_logbuf (
		EVENT_SIZE /* event */ +
		strlen (name) + 1 /* name */ +
		strlen (guid) + 1 /* guid */ +
		strlen (filename) + 1 /* file name */ +
		LEB128_SIZE /* no. methods */ +
		LEB128_SIZE /* fully covered */ +
		LEB128_SIZE /* partially covered */
	);

	ENTER_LOG (logbuffer, "coverage-assemblies");
	emit_byte (logbuffer, TYPE_COVERAGE_ASSEMBLY | TYPE_COVERAGE);
	emit_string (logbuffer, name, strlen (name) + 1);
	emit_string (logbuffer, guid, strlen (guid) + 1);
	emit_string (logbuffer, filename, strlen (filename) + 1);
	emit_uvalue (logbuffer, number_of_methods);
	emit_uvalue (logbuffer, fully_covered);
	emit_uvalue (logbuffer, partially_covered);
	EXIT_LOG (logbuffer);

	safe_send (prof, logbuffer);
}

static void
dump_coverage (MonoProfiler *prof)
{
	if (!coverage_initialized)
		return;

	COVERAGE_DEBUG(fprintf (stderr, "Coverage: Started dump\n");)
	method_id = 0;

	mono_os_mutex_lock (&coverage_mutex);
	mono_conc_hashtable_foreach (coverage_assemblies, build_assembly_buffer, prof);
	mono_conc_hashtable_foreach (coverage_classes, build_class_buffer, prof);
	mono_conc_hashtable_foreach (coverage_methods, build_method_buffer, prof);
	mono_os_mutex_unlock (&coverage_mutex);

	COVERAGE_DEBUG(fprintf (stderr, "Coverage: Finished dump\n");)
}

static void
process_method_enter_coverage (MonoProfiler *prof, MonoMethod *method)
{
	MonoClass *klass;
	MonoImage *image;

	if (!coverage_initialized)
		return;

	klass = mono_method_get_class (method);
	image = mono_class_get_image (klass);

	if (mono_conc_hashtable_lookup (suppressed_assemblies, (gpointer) mono_image_get_name (image)))
		return;

	mono_os_mutex_lock (&coverage_mutex);
	mono_conc_hashtable_insert (entered_methods, method, method);
	mono_os_mutex_unlock (&coverage_mutex);
}

static MonoLockFreeQueueNode *
create_method_node (MonoMethod *method)
{
	MethodNode *node = (MethodNode *)g_malloc (sizeof (MethodNode));
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

	if (!coverage_initialized)
		return FALSE;

	COVERAGE_DEBUG(fprintf (stderr, "Coverage filter for %s\n", mono_method_get_name (method));)

	flags = mono_method_get_flags (method, &iflags);
	if ((iflags & 0x1000 /*METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL*/) ||
	    (flags & 0x2000 /*METHOD_ATTRIBUTE_PINVOKE_IMPL*/)) {
		COVERAGE_DEBUG(fprintf (stderr, "   Internal call or pinvoke - ignoring\n");)
		return FALSE;
	}

	// Don't need to do anything else if we're already tracking this method
	if (mono_conc_hashtable_lookup (coverage_methods, method)) {
		COVERAGE_DEBUG(fprintf (stderr, "   Already tracking\n");)
		return TRUE;
	}

	klass = mono_method_get_class (method);
	image = mono_class_get_image (klass);

	// Don't handle coverage for the core assemblies
	if (mono_conc_hashtable_lookup (suppressed_assemblies, (gpointer) mono_image_get_name (image)) != NULL)
		return FALSE;

	if (prof->coverage_filters) {
		/* Check already filtered classes first */
		if (mono_conc_hashtable_lookup (filtered_classes, klass)) {
			COVERAGE_DEBUG(fprintf (stderr, "   Already filtered\n");)
			return FALSE;
		}

		classname = mono_type_get_name (mono_class_get_type (klass));

		fqn = g_strdup_printf ("[%s]%s", mono_image_get_name (image), classname);

		COVERAGE_DEBUG(fprintf (stderr, "   Looking for %s in filter\n", fqn);)
		// Check positive filters first
		has_positive = FALSE;
		found = FALSE;
		for (guint i = 0; i < prof->coverage_filters->len; ++i) {
			char *filter = (char *)g_ptr_array_index (prof->coverage_filters, i);

			if (filter [0] == '+') {
				filter = &filter [1];

				COVERAGE_DEBUG(fprintf (stderr, "   Checking against +%s ...", filter);)

				if (strstr (fqn, filter) != NULL) {
					COVERAGE_DEBUG(fprintf (stderr, "matched\n");)
					found = TRUE;
				} else
					COVERAGE_DEBUG(fprintf (stderr, "no match\n");)

				has_positive = TRUE;
			}
		}

		if (has_positive && !found) {
			COVERAGE_DEBUG(fprintf (stderr, "   Positive match was not found\n");)

			mono_os_mutex_lock (&coverage_mutex);
			mono_conc_hashtable_insert (filtered_classes, klass, klass);
			mono_os_mutex_unlock (&coverage_mutex);
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
			COVERAGE_DEBUG(fprintf (stderr, "   Checking against -%s ...", filter);)

			if (strstr (fqn, filter) != NULL) {
				COVERAGE_DEBUG(fprintf (stderr, "matched\n");)

				mono_os_mutex_lock (&coverage_mutex);
				mono_conc_hashtable_insert (filtered_classes, klass, klass);
				mono_os_mutex_unlock (&coverage_mutex);
				g_free (fqn);
				g_free (classname);

				return FALSE;
			} else
				COVERAGE_DEBUG(fprintf (stderr, "no match\n");)

		}

		g_free (fqn);
		g_free (classname);
	}

	COVERAGE_DEBUG(fprintf (stderr, "   Handling coverage for %s\n", mono_method_get_name (method));)
	header = mono_method_get_header_checked (method, &error);
	mono_error_cleanup (&error);

	mono_method_header_get_code (header, &code_size, NULL);

	assembly = mono_image_get_assembly (image);

	mono_os_mutex_lock (&coverage_mutex);
	mono_conc_hashtable_insert (coverage_methods, method, method);
	mono_conc_hashtable_insert (coverage_assemblies, assembly, assembly);
	mono_os_mutex_unlock (&coverage_mutex);

	image_methods = (MonoLockFreeQueue *)mono_conc_hashtable_lookup (image_to_methods, image);

	if (image_methods == NULL) {
		image_methods = (MonoLockFreeQueue *)g_malloc (sizeof (MonoLockFreeQueue));
		mono_lock_free_queue_init (image_methods);
		mono_os_mutex_lock (&coverage_mutex);
		mono_conc_hashtable_insert (image_to_methods, image, image_methods);
		mono_os_mutex_unlock (&coverage_mutex);
	}

	node = create_method_node (method);
	mono_lock_free_queue_enqueue (image_methods, node);

	class_methods = (MonoLockFreeQueue *)mono_conc_hashtable_lookup (coverage_classes, klass);

	if (class_methods == NULL) {
		class_methods = (MonoLockFreeQueue *)g_malloc (sizeof (MonoLockFreeQueue));
		mono_lock_free_queue_init (class_methods);
		mono_os_mutex_lock (&coverage_mutex);
		mono_conc_hashtable_insert (coverage_classes, klass, class_methods);
		mono_os_mutex_unlock (&coverage_mutex);
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

	buffer = (char *)g_malloc ((filesize + 1) * sizeof (char));
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

	suppressed_assemblies = mono_conc_hashtable_new (g_str_hash, g_str_equal);
	sa_file = fopen (SUPPRESSION_DIR "/mono-profiler-log.suppression", "r");
	if (sa_file == NULL)
		return;

	/* Don't need to free @content as it is referred to by the lines stored in @suppressed_assemblies */
	content = get_file_content (sa_file);
	if (content == NULL) {
		g_error ("mono-profiler-log.suppression is greater than 128kb - aborting\n");
	}

	while ((line = get_next_line (content, &content))) {
		line = g_strchomp (g_strchug (line));
		/* No locking needed as we're doing initialization */
		mono_conc_hashtable_insert (suppressed_assemblies, line, line);
	}

	fclose (sa_file);
}

#endif /* DISABLE_HELPER_THREAD */

static void
coverage_init (MonoProfiler *prof)
{
#ifndef DISABLE_HELPER_THREAD
	assert (!coverage_initialized);

	COVERAGE_DEBUG(fprintf (stderr, "Coverage initialized\n");)

	mono_os_mutex_init (&coverage_mutex);
	coverage_methods = mono_conc_hashtable_new (NULL, NULL);
	coverage_assemblies = mono_conc_hashtable_new (NULL, NULL);
	coverage_classes = mono_conc_hashtable_new (NULL, NULL);
	filtered_classes = mono_conc_hashtable_new (NULL, NULL);
	entered_methods = mono_conc_hashtable_new (NULL, NULL);
	image_to_methods = mono_conc_hashtable_new (NULL, NULL);
	init_suppressed_assemblies ();

	coverage_initialized = TRUE;
#endif /* DISABLE_HELPER_THREAD */
}

static void
log_shutdown (MonoProfiler *prof)
{
	void *res;

	in_shutdown = 1;
#ifndef DISABLE_HELPER_THREAD
	counters_and_perfcounters_sample (prof);

	dump_coverage (prof);

	if (prof->command_port) {
		char c = 1;
		ign_res (write (prof->pipes [1], &c, 1));
		pthread_join (prof->helper_thread, &res);
	}
#endif
#if USE_PERF_EVENTS
	if (perf_data) {
		int i;
		for (i = 0; i < num_perf; ++i)
			read_perf_mmap (prof, i);
	}
#endif

	g_ptr_array_free (prof->sorted_sample_events, TRUE);

	if (TLS_GET (LogBuffer, tlsbuffer))
		send_buffer (prof, TLS_GET (GPtrArray, tlsmethodlist), TLS_GET (LogBuffer, tlsbuffer));

	TLS_SET (tlsbuffer, NULL);
	TLS_SET (tlsmethodlist, NULL);

	InterlockedWrite (&prof->run_writer_thread, 0);
	pthread_join (prof->writer_thread, &res);

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

	if (coverage_initialized) {
		mono_conc_hashtable_destroy (coverage_methods);
		mono_conc_hashtable_destroy (coverage_assemblies);
		mono_conc_hashtable_destroy (coverage_classes);
		mono_conc_hashtable_destroy (filtered_classes);

		mono_conc_hashtable_destroy (entered_methods);
		mono_conc_hashtable_destroy (image_to_methods);
		mono_conc_hashtable_destroy (suppressed_assemblies);
		mono_os_mutex_destroy (&coverage_mutex);
	}

	free (prof);
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
	d = res = (char *)malloc (strlen (filename) + s_date * count_dates + s_pid * count_pids);
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

//this is exposed by the JIT, but it's not meant to be a supported API for now.
extern void mono_threads_attach_tools_thread (void);

#ifndef DISABLE_HELPER_THREAD

static void*
helper_thread (void* arg)
{
	MonoProfiler* prof = (MonoProfiler *)arg;
	int command_socket;
	int len;
	char buf [64];
	MonoThread *thread = NULL;

	mono_threads_attach_tools_thread ();
	//fprintf (stderr, "Server listening\n");
	command_socket = -1;
	while (1) {
		fd_set rfds;
		struct timeval tv;
		int max_fd = -1;
		FD_ZERO (&rfds);
		FD_SET (prof->server_socket, &rfds);
		max_fd = prof->server_socket;
		FD_SET (prof->pipes [0], &rfds);
		if (max_fd < prof->pipes [0])
			max_fd = prof->pipes [0];
		if (command_socket >= 0) {
			FD_SET (command_socket, &rfds);
			if (max_fd < command_socket)
				max_fd = command_socket;
		}
#if USE_PERF_EVENTS
		if (perf_data) {
			int i;
			for ( i = 0; i < num_perf; ++i) {
				if (perf_data [i].perf_fd < 0)
					continue;
				FD_SET (perf_data [i].perf_fd, &rfds);
				if (max_fd < perf_data [i].perf_fd)
					max_fd = perf_data [i].perf_fd;
			}
		}
#endif

		counters_and_perfcounters_sample (prof);

		tv.tv_sec = 1;
		tv.tv_usec = 0;
		len = select (max_fd + 1, &rfds, NULL, NULL, &tv);

		if (len < 0) {
			if (errno == EINTR)
				continue;

			g_warning ("Error in proflog server: %s", strerror (errno));
			return NULL;
		}

		if (FD_ISSET (prof->pipes [0], &rfds)) {
			char c;
			int r = read (prof->pipes [0], &c, 1);
			if (r == 1 && c == 0) {
				StatBuffer *sbufbase = prof->stat_buffers;
				StatBuffer *sbuf;
				if (!sbufbase->next)
					continue;
				sbuf = sbufbase->next->next;
				sbufbase->next->next = NULL;
				if (do_debug)
					fprintf (stderr, "stat buffer dump\n");
				if (sbuf) {
					dump_sample_hits (prof, sbuf);
					free_buffer (sbuf, sbuf->size);
					safe_send (prof, ensure_logbuf (0));
				}
				continue;
			}
			/* time to shut down */
			dump_sample_hits (prof, prof->stat_buffers);
			if (thread)
				mono_thread_detach (thread);
			if (do_debug)
				fprintf (stderr, "helper shutdown\n");
#if USE_PERF_EVENTS
			if (perf_data) {
				int i;
				for ( i = 0; i < num_perf; ++i) {
					if (perf_data [i].perf_fd < 0)
						continue;
					if (FD_ISSET (perf_data [i].perf_fd, &rfds))
						read_perf_mmap (prof, i);
				}
			}
#endif
			safe_send (prof, ensure_logbuf (0));
			return NULL;
		}
#if USE_PERF_EVENTS
		if (perf_data) {
			int i;
			for ( i = 0; i < num_perf; ++i) {
				if (perf_data [i].perf_fd < 0)
					continue;
				if (FD_ISSET (perf_data [i].perf_fd, &rfds)) {
					read_perf_mmap (prof, i);
					safe_send (prof, ensure_logbuf (0));
				}
			}
		}
#endif
		if (command_socket >= 0 && FD_ISSET (command_socket, &rfds)) {
			len = read (command_socket, buf, sizeof (buf) - 1);
			if (len < 0)
				continue;
			if (len == 0) {
				close (command_socket);
				command_socket = -1;
				continue;
			}
			buf [len] = 0;
			if (strcmp (buf, "heapshot\n") == 0) {
				heapshot_requested = 1;
				//fprintf (stderr, "perform heapshot\n");
				if (InterlockedRead (&runtime_inited) && !thread) {
					thread = mono_thread_attach (mono_get_root_domain ());
					/*fprintf (stderr, "attached\n");*/
				}
				if (thread) {
					process_requests (prof);
					mono_thread_detach (thread);
					thread = NULL;
				}
			}
			continue;
		}
		if (!FD_ISSET (prof->server_socket, &rfds)) {
			continue;
		}
		command_socket = accept (prof->server_socket, NULL, NULL);
		if (command_socket < 0)
			continue;
		//fprintf (stderr, "Accepted connection\n");
	}
	return NULL;
}

static int
start_helper_thread (MonoProfiler* prof)
{
	struct sockaddr_in server_address;
	int r;
	socklen_t slen;
	if (pipe (prof->pipes) < 0) {
		fprintf (stderr, "Cannot create pipe\n");
		return 0;
	}
	prof->server_socket = socket (PF_INET, SOCK_STREAM, 0);
	if (prof->server_socket < 0) {
		fprintf (stderr, "Cannot create server socket\n");
		return 0;
	}
	memset (&server_address, 0, sizeof (server_address));
	server_address.sin_family = AF_INET;
	server_address.sin_addr.s_addr = INADDR_ANY;
	server_address.sin_port = htons (prof->command_port);
	if (bind (prof->server_socket, (struct sockaddr *) &server_address, sizeof (server_address)) < 0) {
		fprintf (stderr, "Cannot bind server socket, port: %d: %s\n", prof->command_port, strerror (errno));
		close (prof->server_socket);
		return 0;
	}
	if (listen (prof->server_socket, 1) < 0) {
		fprintf (stderr, "Cannot listen server socket\n");
		close (prof->server_socket);
		return 0;
	}
	slen = sizeof (server_address);
	if (getsockname (prof->server_socket, (struct sockaddr *)&server_address, &slen) == 0) {
		prof->command_port = ntohs (server_address.sin_port);
		/*fprintf (stderr, "Assigned server port: %d\n", prof->command_port);*/
	}

	r = pthread_create (&prof->helper_thread, NULL, helper_thread, prof);
	if (r) {
		close (prof->server_socket);
		return 0;
	}
	return 1;
}
#endif

static void *
writer_thread (void *arg)
{
	MonoProfiler *prof = (MonoProfiler *)arg;

	mono_threads_attach_tools_thread ();

	dump_header (prof);

	while (InterlockedRead (&prof->run_writer_thread)) {
		WriterQueueEntry *entry;

		while ((entry = (WriterQueueEntry *) mono_lock_free_queue_dequeue (&prof->writer_queue))) {
			LogBuffer *method_buffer = NULL;
			gboolean new_methods = FALSE;

			if (entry->methods->len)
				method_buffer = create_buffer ();

			/*
			 * Encode the method events in a temporary log buffer that we
			 * flush to disk before the main buffer, ensuring that all
			 * methods have metadata emitted before they're referenced.
			 */
			for (guint i = 0; i < entry->methods->len; i++) {
				MethodInfo *info = (MethodInfo *)g_ptr_array_index (entry->methods, i);

				if (mono_conc_hashtable_lookup (prof->method_table, info->method))
					continue;

				new_methods = TRUE;

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
				mono_os_mutex_lock (&prof->method_table_mutex);
				mono_conc_hashtable_insert (prof->method_table, info->method, info->method);
				mono_os_mutex_unlock (&prof->method_table_mutex);

				char *name = mono_method_full_name (info->method, 1);
				int nlen = strlen (name) + 1;
				void *cstart = info->ji ? mono_jit_info_get_code_start (info->ji) : NULL;
				int csize = info->ji ? mono_jit_info_get_code_size (info->ji) : 0;

				method_buffer = ensure_logbuf_inner (method_buffer,
					EVENT_SIZE /* event */ +
					LEB128_SIZE /* time */ +
					LEB128_SIZE /* method */ +
					LEB128_SIZE /* start */ +
					LEB128_SIZE /* size */ +
					nlen /* name */
				);

				emit_byte (method_buffer, TYPE_JIT | TYPE_METHOD);
				emit_time (method_buffer, info->time);
				emit_method_inner (method_buffer, info->method);
				emit_ptr (method_buffer, cstart);
				emit_value (method_buffer, csize);

				memcpy (method_buffer->data, name, nlen);
				method_buffer->data += nlen;

				mono_free (name);
				free (info);
			}

			g_ptr_array_free (entry->methods, TRUE);

			if (new_methods)
				dump_buffer (prof, method_buffer);
			else if (method_buffer)
				free_buffer (method_buffer, method_buffer->size);

			dump_buffer (prof, entry->buffer);

			free (entry);
		}
	}

	return NULL;
}

static int
start_writer_thread (MonoProfiler* prof)
{
	InterlockedWrite (&prof->run_writer_thread, 1);

	return !pthread_create (&prof->writer_thread, NULL, writer_thread, prof);
}

static void
runtime_initialized (MonoProfiler *profiler)
{
#ifndef DISABLE_HELPER_THREAD
	if (hs_mode_ondemand || need_helper_thread) {
		if (!start_helper_thread (profiler))
			profiler->command_port = 0;
	}
#endif

	start_writer_thread (profiler);

	InterlockedWrite (&runtime_inited, 1);
#ifndef DISABLE_HELPER_THREAD
	counters_init (profiler);
	counters_sample (profiler, 0);
#endif
	/* ensure the main thread data and startup are available soon */
	safe_send (profiler, ensure_logbuf (0));
}

static MonoProfiler*
create_profiler (const char *filename, GPtrArray *filters)
{
	MonoProfiler *prof;
	char *nf;
	int force_delete = 0;
	prof = (MonoProfiler *)calloc (1, sizeof (MonoProfiler));

	prof->command_port = command_port;
	if (filename && *filename == '-') {
		force_delete = 1;
		filename++;
	}
	if (!filename) {
		if (do_report)
			filename = "|mprof-report -";
		else
			filename = "output.mlpd";
		nf = (char*)filename;
	} else {
		nf = new_filename (filename);
		if (do_report) {
			int s = strlen (nf) + 32;
			char *p = (char *)malloc (s);
			snprintf (p, s, "|mprof-report '--out=%s' -", nf);
			free (nf);
			nf = p;
		}
	}
	if (*nf == '|') {
		prof->file = popen (nf + 1, "w");
		prof->pipe_output = 1;
	} else if (*nf == '#') {
		int fd = strtol (nf + 1, NULL, 10);
		prof->file = fdopen (fd, "a");
	} else {
		if (force_delete)
			unlink (nf);
		prof->file = fopen (nf, "wb");
	}
	if (!prof->file) {
		fprintf (stderr, "Cannot create profiler output: %s\n", nf);
		exit (1);
	}
#if defined (HAVE_SYS_ZLIB)
	if (use_zip)
		prof->gzfile = gzdopen (fileno (prof->file), "wb");
#endif
#if USE_PERF_EVENTS
	if (sample_type && !do_mono_sample)
		need_helper_thread = setup_perf_event ();
	if (!perf_data) {
		/* FIXME: warn if different freq or sample type */
		do_mono_sample = 1;
	}
#endif
	if (do_mono_sample) {
		prof->stat_buffers = create_stat_buffer ();
		need_helper_thread = 1;
	}
	if (do_counters && !need_helper_thread) {
		need_helper_thread = 1;
	}

	prof->sorted_sample_events = g_ptr_array_sized_new (BUFFER_SIZE / SAMPLE_EVENT_SIZE_IN_SLOTS (0));

#ifdef DISABLE_HELPER_THREAD
	if (hs_mode_ondemand)
		fprintf (stderr, "Ondemand heapshot unavailable on this arch.\n");

	if (do_coverage)
		fprintf (stderr, "Coverage unavailable on this arch.\n");

#endif

	mono_lock_free_queue_init (&prof->writer_queue);
	mono_os_mutex_init (&prof->method_table_mutex);
	prof->method_table = mono_conc_hashtable_new (NULL, NULL);

	if (do_coverage)
		coverage_init (prof);
	prof->coverage_filters = filters;

	prof->startup_time = current_time ();
	return prof;
}

static void
usage (int do_exit)
{
	printf ("Log profiler version %d.%d (format: %d)\n", LOG_VERSION_MAJOR, LOG_VERSION_MINOR, LOG_DATA_VERSION);
	printf ("Usage: mono --profile=log[:OPTION1[,OPTION2...]] program.exe\n");
	printf ("Options:\n");
	printf ("\thelp                 show this usage info\n");
	printf ("\t[no]alloc            enable/disable recording allocation info\n");
	printf ("\t[no]calls            enable/disable recording enter/leave method events\n");
	printf ("\theapshot[=MODE]      record heap shot info (by default at each major collection)\n");
	printf ("\t                     MODE: every XXms milliseconds, every YYgc collections, ondemand\n");
	printf ("\tcounters             sample counters every 1s\n");
	printf ("\tsample[=TYPE]        use statistical sampling mode (by default cycles/1000)\n");
	printf ("\t                     TYPE: cycles,instr,cacherefs,cachemiss,branches,branchmiss\n");
	printf ("\t                     TYPE can be followed by /FREQUENCY\n");
	printf ("\ttime=fast            use a faster (but more inaccurate) timer\n");
	printf ("\tmaxframes=NUM        collect up to NUM stack frames\n");
	printf ("\tcalldepth=NUM        ignore method events for call chain depth bigger than NUM\n");
	printf ("\toutput=FILENAME      write the data to file FILENAME (-FILENAME to overwrite)\n");
	printf ("\toutput=|PROGRAM      write the data to the stdin of PROGRAM\n");
	printf ("\t                     %%t is subtituted with date and time, %%p with the pid\n");
	printf ("\treport               create a report instead of writing the raw data to a file\n");
	printf ("\tzip                  compress the output data\n");
	printf ("\tport=PORTNUM         use PORTNUM for the listening command server\n");
	printf ("\tcoverage             enable collection of code coverage data\n");
	printf ("\tcovfilter=ASSEMBLY   add an assembly to the code coverage filters\n");
	printf ("\t                     add a + to include the assembly or a - to exclude it\n");
	printf ("\t                     filter=-mscorlib\n");
	printf ("\tcovfilter-file=FILE  use FILE to generate the list of assemblies to be filtered\n");
	if (do_exit)
		exit (1);
}

static const char*
match_option (const char* p, const char *opt, char **rval)
{
	int len = strlen (opt);
	if (strncmp (p, opt, len) == 0) {
		if (rval) {
			if (p [len] == '=' && p [len + 1]) {
				const char *opt = p + len + 1;
				const char *end = strchr (opt, ',');
				char *val;
				int l;
				if (end == NULL) {
					l = strlen (opt);
				} else {
					l = end - opt;
				}
				val = (char *)malloc (l + 1);
				memcpy (val, opt, l);
				val [l] = 0;
				*rval = val;
				return opt + l;
			}
			if (p [len] == 0 || p [len] == ',') {
				*rval = NULL;
				return p + len + (p [len] == ',');
			}
			usage (1);
		} else {
			if (p [len] == 0)
				return p + len;
			if (p [len] == ',')
				return p + len + 1;
		}
	}
	return p;
}

typedef struct {
	const char *name;
	int sample_mode;
} SampleMode;

static const SampleMode sample_modes [] = {
	{"cycles", SAMPLE_CYCLES},
	{"instr", SAMPLE_INSTRUCTIONS},
	{"cachemiss", SAMPLE_CACHE_MISSES},
	{"cacherefs", SAMPLE_CACHE_REFS},
	{"branches", SAMPLE_BRANCHES},
	{"branchmiss", SAMPLE_BRANCH_MISSES},
	{NULL, 0}
};

static void
set_sample_mode (char* val, int allow_empty)
{
	char *end;
	char *maybe_freq = NULL;
	unsigned int count;
	const SampleMode *smode = sample_modes;
#ifndef USE_PERF_EVENTS
	do_mono_sample = 1;
#endif
	if (allow_empty && !val) {
		sample_type = SAMPLE_CYCLES;
		sample_freq = 1000;
		return;
	}
	if (strcmp (val, "mono") == 0) {
		do_mono_sample = 1;
		sample_type = SAMPLE_CYCLES;
		free (val);
		return;
	}
	for (smode = sample_modes; smode->name; smode++) {
		int l = strlen (smode->name);
		if (strncmp (val, smode->name, l) == 0) {
			sample_type = smode->sample_mode;
			maybe_freq = val + l;
			break;
		}
	}
	if (!smode->name)
		usage (1);
	if (*maybe_freq == '/') {
		count = strtoul (maybe_freq + 1, &end, 10);
		if (maybe_freq + 1 == end)
			usage (1);
		sample_freq = count;
	} else if (*maybe_freq != 0) {
		usage (1);
	} else {
		sample_freq = 1000;
	}
	free (val);
}

static void
set_hsmode (char* val, int allow_empty)
{
	char *end;
	unsigned int count;
	if (allow_empty && !val)
		return;
	if (strcmp (val, "ondemand") == 0) {
		hs_mode_ondemand = 1;
		free (val);
		return;
	}
	count = strtoul (val, &end, 10);
	if (val == end)
		usage (1);
	if (strcmp (end, "ms") == 0)
		hs_mode_ms = count;
	else if (strcmp (end, "gc") == 0)
		hs_mode_gc = count;
	else
		usage (1);
	free (val);
}

/*
 * declaration to silence the compiler: this is the entry point that
 * mono will load from the shared library and call.
 */
extern void
mono_profiler_startup (const char *desc);

extern void
mono_profiler_startup_log (const char *desc);

/*
 * this is the entry point that will be used when the profiler
 * is embedded inside the main executable.
 */
void
mono_profiler_startup_log (const char *desc)
{
	mono_profiler_startup (desc);
}

void
mono_profiler_startup (const char *desc)
{
	MonoProfiler *prof;
	GPtrArray *filters = NULL;
	char *filename = NULL;
	const char *p;
	const char *opt;
	int fast_time = 0;
	int calls_enabled = 0;
	int allocs_enabled = 0;
	int only_counters = 0;
	int only_coverage = 0;
	int events = MONO_PROFILE_GC|MONO_PROFILE_ALLOCATIONS|
		MONO_PROFILE_GC_MOVES|MONO_PROFILE_CLASS_EVENTS|MONO_PROFILE_THREADS|
		MONO_PROFILE_ENTER_LEAVE|MONO_PROFILE_JIT_COMPILATION|MONO_PROFILE_EXCEPTIONS|
		MONO_PROFILE_MONITOR_EVENTS|MONO_PROFILE_MODULE_EVENTS|MONO_PROFILE_GC_ROOTS|
		MONO_PROFILE_INS_COVERAGE|MONO_PROFILE_APPDOMAIN_EVENTS|MONO_PROFILE_CONTEXT_EVENTS|
		MONO_PROFILE_ASSEMBLY_EVENTS;

	p = desc;
	if (strncmp (p, "log", 3))
		usage (1);
	p += 3;
	if (*p == ':')
		p++;
	for (; *p; p = opt) {
		char *val;
		if (*p == ',') {
			opt = p + 1;
			continue;
		}
		if ((opt = match_option (p, "help", NULL)) != p) {
			usage (0);
			continue;
		}
		if ((opt = match_option (p, "calls", NULL)) != p) {
			calls_enabled = 1;
			continue;
		}
		if ((opt = match_option (p, "nocalls", NULL)) != p) {
			events &= ~MONO_PROFILE_ENTER_LEAVE;
			nocalls = 1;
			continue;
		}
		if ((opt = match_option (p, "alloc", NULL)) != p) {
			allocs_enabled = 1;
			continue;
		}
		if ((opt = match_option (p, "noalloc", NULL)) != p) {
			events &= ~MONO_PROFILE_ALLOCATIONS;
			continue;
		}
		if ((opt = match_option (p, "time", &val)) != p) {
			if (strcmp (val, "fast") == 0)
				fast_time = 1;
			else if (strcmp (val, "null") == 0)
				fast_time = 2;
			else
				usage (1);
			free (val);
			continue;
		}
		if ((opt = match_option (p, "report", NULL)) != p) {
			do_report = 1;
			continue;
		}
		if ((opt = match_option (p, "debug", NULL)) != p) {
			do_debug = 1;
			continue;
		}
		if ((opt = match_option (p, "sampling-real", NULL)) != p) {
			sampling_mode = MONO_PROFILER_STAT_MODE_REAL;
			continue;
		}
		if ((opt = match_option (p, "sampling-process", NULL)) != p) {
			sampling_mode = MONO_PROFILER_STAT_MODE_PROCESS;
			continue;
		}
		if ((opt = match_option (p, "heapshot", &val)) != p) {
			events &= ~MONO_PROFILE_ALLOCATIONS;
			events &= ~MONO_PROFILE_ENTER_LEAVE;
			nocalls = 1;
			do_heap_shot = 1;
			set_hsmode (val, 1);
			continue;
		}
		if ((opt = match_option (p, "sample", &val)) != p) {
			events &= ~MONO_PROFILE_ALLOCATIONS;
			events &= ~MONO_PROFILE_ENTER_LEAVE;
			nocalls = 1;
			set_sample_mode (val, 1);
			continue;
		}
		if ((opt = match_option (p, "hsmode", &val)) != p) {
			fprintf (stderr, "The hsmode profiler option is obsolete, use heapshot=MODE.\n");
			set_hsmode (val, 0);
			continue;
		}
		if ((opt = match_option (p, "zip", NULL)) != p) {
			use_zip = 1;
			continue;
		}
		if ((opt = match_option (p, "output", &val)) != p) {
			filename = val;
			continue;
		}
		if ((opt = match_option (p, "port", &val)) != p) {
			char *end;
			command_port = strtoul (val, &end, 10);
			free (val);
			continue;
		}
		if ((opt = match_option (p, "maxframes", &val)) != p) {
			char *end;
			num_frames = strtoul (val, &end, 10);
			if (num_frames > MAX_FRAMES)
				num_frames = MAX_FRAMES;
			free (val);
			notraces = num_frames == 0;
			continue;
		}
		if ((opt = match_option (p, "calldepth", &val)) != p) {
			char *end;
			max_call_depth = strtoul (val, &end, 10);
			free (val);
			continue;
		}
		if ((opt = match_option (p, "counters", NULL)) != p) {
			do_counters = 1;
			continue;
		}
		if ((opt = match_option (p, "countersonly", NULL)) != p) {
			only_counters = 1;
			continue;
		}
		if ((opt = match_option (p, "coverage", NULL)) != p) {
			do_coverage = 1;
			events |= MONO_PROFILE_ENTER_LEAVE;
			debug_coverage = (g_getenv ("MONO_PROFILER_DEBUG_COVERAGE") != NULL);
			continue;
		}
		if ((opt = match_option (p, "onlycoverage", NULL)) != p) {
			only_coverage = 1;
			continue;
		}
		if ((opt = match_option (p, "covfilter-file", &val)) != p) {
			FILE *filter_file;
			char *line, *content;

			if (filters == NULL)
				filters = g_ptr_array_new ();

			filter_file = fopen (val, "r");
			if (filter_file == NULL) {
				fprintf (stderr, "Unable to open %s\n", val);
				exit (0);
			}

			/* Don't need to free content as it is referred to by the lines stored in @filters */
			content = get_file_content (filter_file);
			if (content == NULL)
				fprintf (stderr, "WARNING: %s is greater than 128kb - ignoring\n", val);

			while ((line = get_next_line (content, &content)))
				g_ptr_array_add (filters, g_strchug (g_strchomp (line)));

			fclose (filter_file);
			continue;
		}
		if ((opt = match_option (p, "covfilter", &val)) != p) {
			if (filters == NULL)
				filters = g_ptr_array_new ();

			g_ptr_array_add (filters, val);
			continue;
		}
		if (opt == p) {
			usage (0);
			exit (0);
		}
	}
	if (calls_enabled) {
		events |= MONO_PROFILE_ENTER_LEAVE;
		nocalls = 0;
	}
	if (allocs_enabled)
		events |= MONO_PROFILE_ALLOCATIONS;
	if (only_counters)
		events = 0;
	if (only_coverage)
		events = MONO_PROFILE_ENTER_LEAVE | MONO_PROFILE_INS_COVERAGE;

	utils_init (fast_time);

	prof = create_profiler (filename, filters);
	if (!prof)
		return;
	init_thread ();

	mono_profiler_install (prof, log_shutdown);
	mono_profiler_install_gc (gc_event, gc_resize);
	mono_profiler_install_allocation (gc_alloc);
	mono_profiler_install_gc_moves (gc_moves);
	mono_profiler_install_gc_roots (gc_handle, gc_roots);
	mono_profiler_install_appdomain (NULL, domain_loaded, NULL, domain_unloaded);
	mono_profiler_install_appdomain_name (domain_name);
	mono_profiler_install_context (context_loaded, context_unloaded);
	mono_profiler_install_class (NULL, class_loaded, NULL, class_unloaded);
	mono_profiler_install_module (NULL, image_loaded, NULL, image_unloaded);
	mono_profiler_install_assembly (NULL, assembly_loaded, assembly_unloaded, NULL);
	mono_profiler_install_thread (thread_start, thread_end);
	mono_profiler_install_thread_name (thread_name);
	mono_profiler_install_enter_leave (method_enter, method_leave);
	mono_profiler_install_jit_end (method_jitted);
	mono_profiler_install_code_buffer_new (code_buffer_new);
	mono_profiler_install_exception (throw_exc, method_exc_leave, clause_exc);
	mono_profiler_install_monitor (monitor_event);
	mono_profiler_install_runtime_initialized (runtime_initialized);
	if (do_coverage)
		mono_profiler_install_coverage_filter (coverage_filter);

	if (do_mono_sample && sample_type == SAMPLE_CYCLES && !only_counters) {
		events |= MONO_PROFILE_STATISTICAL;
		mono_profiler_set_statistical_mode (sampling_mode, 1000000 / sample_freq);
		mono_profiler_install_statistical (mono_sample_hit);
	}

	mono_profiler_set_events ((MonoProfileFlags)events);

	TLS_INIT (tlsbuffer);
	TLS_INIT (tlsmethodlist);
}
