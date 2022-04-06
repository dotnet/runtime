/*
 * log.c: mono log profiler
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Alex Rønne Petersen (alexrp@xamarin.com)
 *   Johan Lorensson (lateralusx.github@gmail.com)
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <gmodule.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/icall-internals.h>
#include <mono/metadata/jit-info.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/loader-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/jit/jit.h>
#include <mono/utils/atomic.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/lock-free-alloc.h>
#include <mono/utils/lock-free-queue.h>
#include <mono/utils/mono-conc-hashtable.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-linked-list-set.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-os-semaphore.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-api.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-publib.h>
#include <mono/utils/os-event.h>
#include <mono/utils/w32subset.h>
#include "log.h"
#include "helper.h"

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
#ifndef HOST_WIN32
#include <netinet/in.h>
#endif
#ifdef HAVE_SYS_MMAN_H
#include <sys/mman.h>
#endif
#ifndef HOST_WIN32
#include <sys/socket.h>
#endif
#ifndef DISABLE_LOG_PROFILER_GZ
#ifdef INTERNAL_ZLIB
#include <external/zlib/zlib.h>
#else
#include <zlib.h>
#endif
#endif

#ifdef HOST_WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#endif

#ifndef HOST_WIN32
#define HAVE_COMMAND_PIPES 1
#endif

// Statistics for internal profiler data structures.
static gint32 sample_allocations_ctr,
              buffer_allocations_ctr;

// Statistics for profiler events.
static gint32 sync_points_ctr,
              aot_ids_ctr,
              heap_objects_ctr,
              heap_starts_ctr,
              heap_ends_ctr,
              heap_roots_ctr,
              heap_root_registers_ctr,
              heap_root_unregisters_ctr,
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
              vtable_loads_ctr,
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
              sample_usyms_ctr,
              sample_hits_ctr;

// Pending data to be written to the log, for a single thread.
// Threads periodically flush their own LogBuffers by calling safe_send
typedef struct _LogBuffer LogBuffer;
struct _LogBuffer {
	// Next (older) LogBuffer in processing queue
	LogBuffer *next;

	uint64_t time_base;
	uint64_t last_time;
	gboolean has_ptr_base;
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

	// Did this thread detach from the runtime? Only used for internal profiler threads.
	gboolean did_detach;

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

// Default value in `profiler_tls` for new threads.
#define MONO_PROFILER_THREAD_ZERO ((MonoProfilerThread *) NULL)

// This is written to `profiler_tls` to indicate that a thread has stopped.
#define MONO_PROFILER_THREAD_DEAD ((MonoProfilerThread *) -1)

// Do not use these TLS macros directly unless you know what you're doing.

#define PROF_TLS_SET(VAL) mono_thread_info_set_tools_data (VAL)
#define PROF_TLS_GET mono_thread_info_get_tools_data

static int32_t
domain_get_id (MonoDomain *domain)
{
	return 1;
}

static int32_t
context_get_id (MonoAppContext *context)
{
	return 1;
}

static int32_t
context_get_domain_id (MonoAppContext *context)
{
	return 1;
}

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

#define ENABLED(EVT) (!!(log_config.effective_mask & (EVT)))
#define ENABLE(EVT) do { log_config.effective_mask |= (EVT); } while (0)
#define DISABLE(EVT) do { log_config.effective_mask &= ~(EVT); } while (0)

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
		g_assert (!thread__->busy && "Why are we trying to write a new event while already writing one?"); \
		thread__->busy = TRUE; \
		mono_atomic_inc_i32 ((COUNTER)); \
		if (thread__->attached) \
			buffer_lock (); \
		LogBuffer *BUFFER = ensure_logbuf_unsafe (thread__, (SIZE))

#define EXIT_LOG_EXPLICIT(SEND) \
		if ((SEND)) \
			send_log_unsafe (TRUE); \
		if (thread__->attached) \
			buffer_unlock (); \
		thread__->busy = FALSE; \
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

struct _MonoProfiler {
	MonoProfilerHandle handle;

	FILE* file;
#ifndef DISABLE_LOG_PROFILER_GZ
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

#if HAVE_API_SUPPORT_WIN32_PIPE_OPEN_CLOSE && !defined (HOST_WIN32)
	int pipe_output;
#endif
	int command_port;
	int server_socket;

#ifdef HAVE_COMMAND_PIPES
	int pipes [2];
#else
	int pipe_command;
#endif

	MonoLinkedListSet profiler_thread_list;
	volatile gint32 buffer_lock_state;
	volatile gint32 buffer_lock_exclusive_intent;

	volatile gint32 runtime_inited;
	volatile gint32 detach_threads;
	volatile gint32 in_shutdown;

	MonoSemType attach_threads_sem;
	MonoSemType detach_threads_sem;

	MonoNativeThreadId helper_thread;
	MonoOSEvent helper_thread_exited;

	MonoNativeThreadId writer_thread;
	volatile gint32 run_writer_thread;
	MonoOSEvent writer_thread_exited;
	MonoLockFreeQueue writer_queue;
	MonoSemType writer_queue_sem;

	MonoLockFreeAllocSizeClass writer_entry_size_class;
	MonoLockFreeAllocator writer_entry_allocator;

	MonoConcurrentHashTable *method_table;
	mono_mutex_t method_table_mutex;

	MonoNativeThreadId dumper_thread;
	volatile gint32 run_dumper_thread;
	MonoOSEvent dumper_thread_exited;
	MonoLockFreeQueue dumper_queue;
	MonoSemType dumper_queue_sem;

	MonoLockFreeAllocSizeClass sample_size_class;
	MonoLockFreeAllocator sample_allocator;
	MonoLockFreeQueue sample_reuse_queue;

	BinaryObject *binary_objects;

	volatile gint32 heapshot_requested;
	guint64 gc_count;
	guint64 last_hs_time;
	gboolean do_heap_walk;

	MonoCoopMutex api_mutex;
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
#define TICKS_PER_MSEC (TICKS_PER_SEC / 1000)

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

	mono_atomic_inc_i32 (&buffer_allocations_ctr);

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

	g_assert (thread != MONO_PROFILER_THREAD_DEAD && "Why are we trying to resurrect a stopped thread?");

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
	if (thread != MONO_PROFILER_THREAD_ZERO)
		return thread;

	thread = g_malloc (sizeof (MonoProfilerThread));
	thread->node.key = thread_id ();
	thread->attached = add_to_lls;
	thread->did_detach = FALSE;
	thread->call_depth = 0;
	thread->busy = FALSE;
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

	g_assert (PROF_TLS_SET (thread));

	return thread;
}

// Only valid if init_thread () was called with add_to_lls = FALSE.
static void
deinit_thread (MonoProfilerThread *thread)
{
	g_assert (!thread->attached && "Why are we manually freeing an attached thread?");

	g_free (thread);
	PROF_TLS_SET (MONO_PROFILER_THREAD_DEAD);
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
buffer_lock_helper (void);

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
	if (mono_atomic_load_i32 (&log_profiler.buffer_lock_state) != get_thread ()->small_id << 16) {
		/* We can get some sgen events (for example gc_handle_deleted)
		 * from threads that are unattached to the runtime (but that
		 * are attached to the profiler).  In that case, avoid mono
		 * thread state transition to GC Safe around the loop, since
		 * the thread won't be participating in Mono's suspension
		 * mechianism anyway.
		 */
		MonoThreadInfo *info = mono_thread_info_current_unchecked ();
		if (info) {
			/* Why do we enter Unsafe and then Safe?  Because we
			 * might be called from a native-to-managed wrapper
			 * from a P/Invoke.  In that case the thread is already
			 * in GC Safe, and the state machine doesn't allow
			 * recursive GC Safe transitions.  (On the other hand
			 * it's ok to enter GC Unsafe multiple times - the
			 * state machine will tell us it's a noop.).
			 */
			MONO_ENTER_GC_UNSAFE_WITH_INFO (info);
			MONO_ENTER_GC_SAFE_WITH_INFO (info);

			buffer_lock_helper ();

			MONO_EXIT_GC_SAFE_WITH_INFO;
			MONO_EXIT_GC_UNSAFE_WITH_INFO;
		} else
			buffer_lock_helper ();
	}

	mono_memory_barrier ();
}

static void
buffer_lock_helper (void)
{
	gint32 old, new_;

	do {
	restart:
		// Hold off if a thread wants to take the exclusive lock.
		while (mono_atomic_load_i32 (&log_profiler.buffer_lock_exclusive_intent))
			mono_thread_info_yield ();

		old = mono_atomic_load_i32 (&log_profiler.buffer_lock_state);

		// Is a thread holding the exclusive lock?
		if (old >> 16) {
			mono_thread_info_yield ();
			goto restart;
		}

		new_ = old + 1;
	} while (mono_atomic_cas_i32 (&log_profiler.buffer_lock_state, new_, old) != old);
}

static void
buffer_unlock (void)
{
	mono_memory_barrier ();

	gint32 state = mono_atomic_load_i32 (&log_profiler.buffer_lock_state);

	// See the comment in buffer_lock ().
	if (state == get_thread ()->small_id << 16)
		return;

	g_assert (state && "Why are we decrementing a zero reader count?");
	g_assert (!(state >> 16) && "Why is the exclusive lock held?");

	mono_atomic_dec_i32 (&log_profiler.buffer_lock_state);
}

static void
buffer_lock_excl (void)
{
	gint32 new_ = get_thread ()->small_id << 16;

	g_assert (mono_atomic_load_i32 (&log_profiler.buffer_lock_state) != new_ && "Why are we taking the exclusive lock twice?");

	mono_atomic_inc_i32 (&log_profiler.buffer_lock_exclusive_intent);

	MONO_ENTER_GC_SAFE;

	while (mono_atomic_cas_i32 (&log_profiler.buffer_lock_state, new_, 0))
		mono_thread_info_yield ();

	MONO_EXIT_GC_SAFE;

	mono_memory_barrier ();
}

static void
buffer_unlock_excl (void)
{
	mono_memory_barrier ();

	gint32 state = mono_atomic_load_i32 (&log_profiler.buffer_lock_state);
	gint32 excl = state >> 16;

	g_assert (excl && "Why is the exclusive lock not held?");
	g_assert (excl == get_thread ()->small_id && "Why does another thread hold the exclusive lock?");
	g_assert (!(state & 0xFFFF) && "Why are there readers when the exclusive lock is held?");

	mono_atomic_store_i32 (&log_profiler.buffer_lock_state, 0);
	mono_atomic_dec_i32 (&log_profiler.buffer_lock_exclusive_intent);
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
	if (!logbuffer->has_ptr_base) {
		logbuffer->ptr_base = (uintptr_t) ptr;
		logbuffer->has_ptr_base = TRUE;
	}

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

static void
inc_method_ref_count (MonoMethod *method)
{
	mono_image_addref (mono_class_get_image (mono_method_get_class (method)));
}

static void
dec_method_ref_count (MonoMethod *method)
{
	mono_image_close (mono_class_get_image (mono_method_get_class (method)));
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
		inc_method_ref_count (method);
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
		sizeof (gint64) /* startup time (nanoseconds) */ +
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
	p = write_int64 (p, current_time ());
	p = write_int32 (p, log_profiler.timer_overhead);
	p = write_int32 (p, 0); /* flags */
	p = write_int32 (p, process_id ());
	p = write_int16 (p, log_profiler.command_port);
	p = write_header_string (p, args);
	p = write_header_string (p, arch);
	p = write_header_string (p, os);

#ifndef DISABLE_LOG_PROFILER_GZ
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

		mono_atomic_inc_i32 (&thread_ends_ctr);

		LogBuffer *buf = ensure_logbuf_unsafe (thread,
			EVENT_SIZE /* event */ +
			BYTE_SIZE /* type */ +
			LEB128_SIZE /* tid */
		);

		emit_event (buf, TYPE_END_UNLOAD | TYPE_METADATA);
		emit_byte (buf, TYPE_THREAD);
		emit_ptr (buf, (void *) thread->node.key);
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

#ifndef DISABLE_LOG_PROFILER_GZ
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

// Only valid if init_thread () was called with add_to_lls = FALSE.
static void
send_log_unsafe (gboolean if_needed)
{
	MonoProfilerThread *thread = get_thread ();

	if (!if_needed || (if_needed && thread->buffer->next)) {
		send_buffer (thread);
		init_buffer_state (thread);
	}
}

static void
dump_aot_id (void)
{
	const char *aotid = mono_runtime_get_aotid ();

	if (!aotid)
		return;

	int alen = strlen (aotid) + 1;

	ENTER_LOG (&aot_ids_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		alen /* aot id */
	);

	emit_event (logbuffer, TYPE_META | TYPE_AOT_ID);
	memcpy (logbuffer->cursor, aotid, alen);
	logbuffer->cursor += alen;

	EXIT_LOG;
}

// Assumes that the exclusive lock is held.
static void
sync_point_flush (void)
{
	g_assert (mono_atomic_load_i32 (&log_profiler.buffer_lock_state) == get_thread ()->small_id << 16 && "Why don't we hold the exclusive lock?");

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
	g_assert (mono_atomic_load_i32 (&log_profiler.buffer_lock_state) == get_thread ()->small_id << 16 && "Why don't we hold the exclusive lock?");

	ENTER_LOG (&sync_points_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */
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
		LEB128_SIZE /* vtable */ +
		LEB128_SIZE /* size */ +
		BYTE_SIZE /* generation */ +
		LEB128_SIZE /* num */ +
		num * (
			LEB128_SIZE /* offset */ +
			LEB128_SIZE /* ref */
		)
	);

	emit_event (logbuffer, TYPE_HEAP_OBJECT | TYPE_HEAP);
	emit_obj (logbuffer, obj);
	emit_ptr (logbuffer, mono_object_get_vtable_internal (obj));
	emit_value (logbuffer, size);
	emit_byte (logbuffer, mono_gc_get_generation (obj));
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
gc_roots (MonoProfiler *prof, uint64_t num, const mono_byte *const *addresses, MonoObject *const *objects)
{
	ENTER_LOG (&heap_roots_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* num */ +
		num * (
			LEB128_SIZE /* address */ +
			LEB128_SIZE /* object */
		)
	);

	emit_event (logbuffer, TYPE_HEAP_ROOT | TYPE_HEAP);
	emit_value (logbuffer, num);

	for (int i = 0; i < num; ++i) {
		emit_ptr (logbuffer, addresses [i]);
		emit_obj (logbuffer, objects [i]);
	}

	EXIT_LOG;
}

static void
gc_root_register (MonoProfiler *prof, const mono_byte *start, size_t size, MonoGCRootSource source, const void *key, const char *name)
{
	// We don't write raw domain/context pointers in metadata events.
	switch (source) {
	case MONO_ROOT_SOURCE_DOMAIN:
		if (key)
			key = (void *)(uintptr_t) domain_get_id ((MonoDomain *) key);
		break;
	case MONO_ROOT_SOURCE_CONTEXT_STATIC:
		key = (void *)(uintptr_t) context_get_id ((MonoAppContext *) key);
		break;
	default:
		break;
	}

	int name_len = name ? strlen (name) + 1 : 0;

	ENTER_LOG (&heap_root_registers_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* start */ +
		LEB128_SIZE /* size */ +
		BYTE_SIZE /* source */ +
		LEB128_SIZE /* key */ +
		name_len /* name */
	);

	emit_event (logbuffer, TYPE_HEAP_ROOT_REGISTER | TYPE_HEAP);
	emit_ptr (logbuffer, start);
	emit_uvalue (logbuffer, size);
	emit_byte (logbuffer, source);
	emit_ptr (logbuffer, key);
	emit_string (logbuffer, name, name_len);

	EXIT_LOG;
}

static void
gc_root_deregister (MonoProfiler *prof, const mono_byte *start)
{
	ENTER_LOG (&heap_root_unregisters_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* start */
	);

	emit_event (logbuffer, TYPE_HEAP_ROOT_UNREGISTER | TYPE_HEAP);
	emit_ptr (logbuffer, start);

	EXIT_LOG;
}

static void
trigger_heapshot (void)
{
	// Rely on the finalization callback triggering a GC.
	mono_atomic_store_i32 (&log_profiler.heapshot_requested, 1);
	mono_gc_finalize_notify ();
}

static void
process_heapshot (void)
{
	if (mono_atomic_load_i32 (&log_profiler.heapshot_requested))
		mono_gc_collect (mono_gc_max_generation ());
}

#define ALL_GC_EVENTS_MASK (PROFLOG_GC_EVENTS | PROFLOG_GC_MOVE_EVENTS | PROFLOG_GC_ROOT_EVENTS)

static void
gc_event (MonoProfiler *profiler, MonoProfilerGCEvent ev, uint32_t generation, gboolean is_serial)
{
	gboolean is_major = generation == mono_gc_max_generation ();

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
	case MONO_GC_EVENT_PRE_STOP_WORLD_LOCKED:
		switch (log_config.hs_mode) {
		case MONO_PROFILER_HEAPSHOT_NONE:
			log_profiler.do_heap_walk = FALSE;
			break;
		case MONO_PROFILER_HEAPSHOT_MAJOR:
			log_profiler.do_heap_walk = is_major;
			break;
		case MONO_PROFILER_HEAPSHOT_ON_DEMAND:
			// Handled below.
			break;
		case MONO_PROFILER_HEAPSHOT_X_GC:
			log_profiler.do_heap_walk = !(log_profiler.gc_count % log_config.hs_freq_gc);
			break;
		case MONO_PROFILER_HEAPSHOT_X_MS:
			log_profiler.do_heap_walk = (current_time () - log_profiler.last_hs_time) / TICKS_PER_MSEC >= log_config.hs_freq_ms;
			break;
		default:
			g_assert_not_reached ();
		}

		/*
		 * heapshot_requested is set either because a heapshot was triggered
		 * manually (through the API or command server) or because we're doing
		 * a shutdown heapshot. Either way, a manually triggered heapshot
		 * overrides any decision we made in the switch above.
		 */
		if (is_major && is_serial && mono_atomic_load_i32 (&log_profiler.heapshot_requested)) {
			log_profiler.do_heap_walk = TRUE;
		} else if (log_profiler.do_heap_walk && (!is_major || !is_serial)) {
			/* Do a heap walk later, when we get invoked from the finalizer in serial mode */
			trigger_heapshot ();
			log_profiler.do_heap_walk = FALSE;
		}

		if (ENABLED (PROFLOG_GC_ROOT_EVENTS) &&
		    (log_config.always_do_root_report || log_profiler.do_heap_walk))
			mono_profiler_set_gc_roots_callback (log_profiler.handle, gc_roots);

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

		// Surround heapshots with HEAP_START/HEAP_END events.
		if (log_profiler.do_heap_walk) {
			ENTER_LOG (&heap_starts_ctr, logbuffer,
				EVENT_SIZE /* event */
			);

			emit_event (logbuffer, TYPE_HEAP_START | TYPE_HEAP);

			EXIT_LOG;
		}

		break;
	case MONO_GC_EVENT_START:
		if (is_major)
			log_profiler.gc_count++;

		break;
	case MONO_GC_EVENT_PRE_START_WORLD:
		mono_profiler_set_gc_roots_callback (log_profiler.handle, NULL);

		if (log_profiler.do_heap_walk) {
			g_assert (is_major && is_serial);
			mono_gc_walk_heap (0, gc_reference, NULL);

			ENTER_LOG (&heap_ends_ctr, logbuffer,
				EVENT_SIZE /* event */
			);

			emit_event (logbuffer, TYPE_HEAP_END | TYPE_HEAP);

			EXIT_LOG;

			log_profiler.do_heap_walk = FALSE;
			log_profiler.last_hs_time = current_time ();

			mono_atomic_store_i32 (&log_profiler.heapshot_requested, 0);
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
	emit_uvalue (logbuffer, new_size);

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
	int do_bt = (!log_config.enter_leave && mono_atomic_load_i32 (&log_profiler.runtime_inited) && log_config.num_frames) ? TYPE_ALLOC_BT : 0;
	FrameData data;
	uintptr_t len = mono_object_get_size_internal (obj);
	/* account for object alignment in the heap */
	len += 7;
	len &= ~7;

	if (do_bt)
		collect_bt (&data);

	ENTER_LOG (&gc_allocs_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* vtable */ +
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
	emit_ptr (logbuffer, mono_object_get_vtable_internal (obj));
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
	int do_bt = !log_config.enter_leave && mono_atomic_load_i32 (&log_profiler.runtime_inited) && log_config.num_frames;
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
	process_heapshot ();

	if (ENABLED (PROFLOG_GC_FINALIZATION_EVENTS)) {
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
	name = m_class_get_name (klass);
	nspace = m_class_get_name_space (klass);
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
	const char *guid = mono_image_get_guid (image);

	// Dynamic images don't have a GUID set.
	if (!guid)
		guid = "";

	int glen = strlen (guid) + 1;

	ENTER_LOG (&image_loads_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* image */ +
		nlen /* name */ +
		glen /* guid */
	);

	emit_event (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_IMAGE);
	emit_ptr (logbuffer, image);
	memcpy (logbuffer->cursor, name, nlen);
	logbuffer->cursor += nlen;
	memcpy (logbuffer->cursor, guid, glen);
	logbuffer->cursor += glen;

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
	char *name = mono_stringify_assembly_name (mono_assembly_get_name_internal (assembly));
	int nlen = strlen (name) + 1;
	MonoImage *image = mono_assembly_get_image_internal (assembly);

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
	char *name = mono_stringify_assembly_name (mono_assembly_get_name_internal (assembly));
	int nlen = strlen (name) + 1;
	MonoImage *image = mono_assembly_get_image_internal (assembly);

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

	if (mono_atomic_load_i32 (&log_profiler.runtime_inited))
		name = mono_type_get_name (m_class_get_byval_arg (klass));
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

	if (mono_atomic_load_i32 (&log_profiler.runtime_inited))
		mono_free (name);
	else
		g_free (name);
}

static void
vtable_loaded (MonoProfiler *prof, MonoVTable *vtable)
{
	MonoClass *klass = mono_vtable_class_internal (vtable);
	MonoDomain *domain = mono_vtable_domain_internal (vtable);
	uint32_t domain_id = domain ? domain_get_id (domain) : 0;

	ENTER_LOG (&vtable_loads_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* vtable */ +
		LEB128_SIZE /* domain id */ +
		LEB128_SIZE /* klass */
	);

	emit_event (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_VTABLE);
	emit_ptr (logbuffer, vtable);
	emit_ptr (logbuffer, (void *)(uintptr_t) domain_id);
	emit_ptr (logbuffer, klass);

	EXIT_LOG;
}

static void
method_enter (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
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
method_leave (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
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
tailcall (MonoProfiler *prof, MonoMethod *method, MonoMethod *target)
{
	method_leave (prof, method, NULL);
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
	if (log_config.callspec.len > 0 &&
	    !mono_callspec_eval (method, &log_config.callspec))
		return MONO_PROFILER_CALL_INSTRUMENTATION_NONE;

	return MONO_PROFILER_CALL_INSTRUMENTATION_ENTER |
	       MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE |
	       MONO_PROFILER_CALL_INSTRUMENTATION_TAIL_CALL |
	       MONO_PROFILER_CALL_INSTRUMENTATION_EXCEPTION_LEAVE;
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
	int do_bt = (!log_config.enter_leave && mono_atomic_load_i32 (&log_profiler.runtime_inited) && log_config.num_frames) ? TYPE_THROW_BT : 0;
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
		LEB128_SIZE /* method */ +
		LEB128_SIZE /* exc */
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
	int do_bt = (!log_config.enter_leave && mono_atomic_load_i32 (&log_profiler.runtime_inited) && log_config.num_frames) ? TYPE_MONITOR_BT : 0;
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

static void
thread_end (MonoProfiler *prof, uintptr_t tid)
{
	ENTER_LOG (&thread_ends_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		BYTE_SIZE /* type */ +
		LEB128_SIZE /* tid */
	);

	emit_event (logbuffer, TYPE_END_UNLOAD | TYPE_METADATA);
	emit_byte (logbuffer, TYPE_THREAD);
	emit_ptr (logbuffer, (void*) tid);

	EXIT_LOG_EXPLICIT (NO_SEND);

	MonoProfilerThread *thread = get_thread ();

	// Internal profiler threads will clean up manually.
	if (thread->attached) {
		thread->ended = TRUE;
		remove_thread (thread);

		PROF_TLS_SET (MONO_PROFILER_THREAD_DEAD);
	}
}

static void
thread_name (MonoProfiler *prof, uintptr_t tid, const char *name)
{
	int len = strlen (name) + 1;

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
	emit_ptr (logbuffer, (void*)(uintptr_t) domain_get_id (domain));

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
	emit_ptr (logbuffer, (void*)(uintptr_t) domain_get_id (domain));

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
	emit_ptr (logbuffer, (void*)(uintptr_t) domain_get_id (domain));
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
	emit_ptr (logbuffer, (void*)(uintptr_t) context_get_id (context));
	emit_ptr (logbuffer, (void*)(uintptr_t) context_get_domain_id (context));

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
	emit_ptr (logbuffer, (void*)(uintptr_t) context_get_id (context));
	emit_ptr (logbuffer, (void*)(uintptr_t) context_get_domain_id (context));

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

		if (method)
			inc_method_ref_count (method);

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

	if (mono_atomic_load_i32 (&log_profiler.in_shutdown))
		return;

	SampleHit *sample = (SampleHit *) mono_lock_free_queue_dequeue (&profiler->sample_reuse_queue);

	if (!sample) {
		/*
		 * If we're out of reusable sample events and we're not allowed to
		 * allocate more, we have no choice but to drop the event.
		 */
		if (mono_atomic_load_i32 (&sample_allocations_ctr) >= log_config.max_allocated_sample_hits)
			return;

		sample = mono_lock_free_alloc (&profiler->sample_allocator);
		mono_lock_free_queue_node_init (&sample->node, TRUE);

		mono_atomic_inc_i32 (&sample_allocations_ctr);
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

static void
dump_usym (char *name, uintptr_t value, uintptr_t size)
{
	int len = strlen (name) + 1;

	ENTER_LOG (&sample_usyms_ctr, logbuffer,
		EVENT_SIZE /* event */ +
		LEB128_SIZE /* value */ +
		LEB128_SIZE /* size */ +
		len /* name */
	);

	emit_event (logbuffer, TYPE_SAMPLE | TYPE_SAMPLE_USYM);
	emit_ptr (logbuffer, (void *) value);
	emit_value (logbuffer, size);
	memcpy (logbuffer->cursor, name, len);
	logbuffer->cursor += len;

	EXIT_LOG;
}

static gboolean
symbol_for (uintptr_t code, char *sname, size_t slen)
{
	return g_module_address ((void *) code, NULL, 0, NULL, sname, slen, NULL);
}

static void
dump_unmanaged_coderefs (void)
{
	int i;
	char last_symbol [256];
	uintptr_t addr, page_end;

	for (i = 0; i < size_code_pages; ++i) {
		char sym [256];
		if (!code_pages [i] || code_pages [i] & 1)
			continue;
		last_symbol [0] = '\0';
		addr = CPAGE_ADDR (code_pages [i]);
		page_end = addr + CPAGE_SIZE;
		code_pages [i] |= 1;
		/* we dump the symbols for the whole page */
		for (; addr < page_end; addr += 16) {
			gboolean symret = symbol_for (addr, sym, 256);
			if (symret && strncmp (sym, last_symbol, 256) == 0)
				continue;
			g_strlcpy (last_symbol, sym, 256);
			if (sym [0] == '\0')
				continue;
			dump_usym (sym, addr, 0); /* let's not guess the size */
		}
	}
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
signal_helper_thread (char c)
{
#ifdef HAVE_COMMAND_PIPES
	if (write (log_profiler.pipes [1], &c, 1) != 1) {
		mono_profiler_printf_err ("Could not write to log profiler pipe: %s", g_strerror (errno));
		exit (1);
	}
#else
	/*
	* On Windows we can't use pipes together with sockets in select. Instead of
	* re-writing the whole logic, the Windows implementation will replace use of command pipe
	* with simple command buffer and a dummy connect to localhost, making sure
	* helper thread will pick up command and process is. Since the dummy connection will
	* be closed right away by client, it will be discarded by helper thread.
	*/
	mono_atomic_store_i32(&log_profiler.pipe_command, c);

	SOCKET client_socket;
	client_socket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if (client_socket != INVALID_SOCKET) {
		struct sockaddr_in client_addr;
		client_addr.sin_family = AF_INET;
		client_addr.sin_port = htons(log_profiler.command_port);
		inet_pton (client_addr.sin_family, "127.0.0.1", &client_addr.sin_addr);

		gulong non_blocking = 1;
		ioctlsocket (client_socket, FIONBIO, &non_blocking);
		if (connect (client_socket, (SOCKADDR *)&client_addr, sizeof (client_addr)) == SOCKET_ERROR) {
			if (WSAGetLastError () == WSAEWOULDBLOCK) {
				fd_set wfds;
				int max_fd = -1;

				FD_ZERO (&wfds);
				FD_SET (client_socket, &wfds);

				/*
				* Include timeout to prevent hanging on connect call.
				*/
				struct timeval tv = { .tv_sec = 1, .tv_usec = 0 };
				select (client_socket + 1, NULL, &wfds, NULL, &tv);
			}
		}

		mono_profhelper_close_socket_fd (client_socket);
	}
#endif
}

static void
log_early_shutdown (MonoProfiler *prof)
{
	dump_aot_id ();

	if (log_config.hs_on_shutdown) {
		mono_atomic_store_i32 (&log_profiler.heapshot_requested, 1);
		mono_gc_collect (mono_gc_max_generation ());
	}

	/*
	 * We need to detach the internal threads early on. log_shutdown () is
	 * called after the threading subsystem has been cleaned up, so detaching
	 * there would crash.
	 */
	mono_os_sem_init (&log_profiler.detach_threads_sem, 0);
	mono_atomic_store_i32 (&log_profiler.detach_threads, 1);

	signal_helper_thread (2);
	mono_os_sem_post (&prof->dumper_queue_sem);
	mono_os_sem_post (&prof->writer_queue_sem);

	for (int i = 0; i < 3; i++)
		mono_os_sem_wait (&log_profiler.detach_threads_sem, MONO_SEM_FLAGS_NONE);

	mono_os_sem_destroy (&log_profiler.detach_threads_sem);
}

static void
log_shutdown (MonoProfiler *prof)
{
	mono_atomic_store_i32 (&log_profiler.in_shutdown, 1);

	signal_helper_thread (1);
	mono_os_event_wait_one (&prof->helper_thread_exited, MONO_INFINITE_WAIT, FALSE);
	mono_os_event_destroy (&prof->helper_thread_exited);

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

	mono_atomic_store_i32 (&prof->run_dumper_thread, 0);
	mono_os_sem_post (&prof->dumper_queue_sem);
	mono_os_event_wait_one (&prof->dumper_thread_exited, MONO_INFINITE_WAIT, FALSE);
	mono_os_event_destroy (&prof->dumper_thread_exited);
	mono_os_sem_destroy (&prof->dumper_queue_sem);

	mono_atomic_store_i32 (&prof->run_writer_thread, 0);
	mono_os_sem_post (&prof->writer_queue_sem);
	mono_os_event_wait_one (&prof->writer_thread_exited, MONO_INFINITE_WAIT, FALSE);
	mono_os_event_destroy (&prof->writer_thread_exited);
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

	gint32 state = mono_atomic_load_i32 (&log_profiler.buffer_lock_state);

	g_assert (!(state & 0xFFFF) && "Why is the reader count still non-zero?");
	g_assert (!(state >> 16) && "Why is the exclusive lock still held?");

#ifndef DISABLE_LOG_PROFILER_GZ
	if (prof->gzfile)
		gzclose (prof->gzfile);
#endif
#if HAVE_API_SUPPORT_WIN32_PIPE_OPEN_CLOSE && !defined (HOST_WIN32)
	if (prof->pipe_output)
		pclose (prof->file);
	else
#endif
		fclose (prof->file);

	mono_conc_hashtable_destroy (prof->method_table);
	mono_os_mutex_destroy (&prof->method_table_mutex);

	mono_coop_mutex_destroy (&log_profiler.api_mutex);

	g_free (prof->args);

#ifdef HOST_WIN32
	/*
	* We depend on socket support in this profiler provider we need to
	* make sure we keep a reference on WSA for the lifetime of this provider.
	*/
	WSACleanup ();
#endif
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

static MonoProfilerThread *
profiler_thread_begin_function (const char *name8, const gunichar2* name16, size_t name_length, gboolean send)
{
	mono_thread_info_attach ();
	MonoProfilerThread *thread = init_thread (FALSE);

	mono_thread_internal_attach (mono_get_root_domain ());

	MonoInternalThread *internal = mono_thread_internal_current ();

	/*
	 * Don't let other threads try to suspend internal profiler threads during
	 * shutdown. This can happen if a program calls Environment.Exit () which
	 * calls mono_thread_suspend_all_other_threads ().
	 */
	internal->flags |= MONO_THREAD_FLAG_DONT_MANAGE;

	mono_thread_set_name (internal, name8, name_length, name16, MonoSetThreadNameFlag_Constant, NULL);

	mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NO_GC | MONO_THREAD_INFO_FLAGS_NO_SAMPLE);

	if (!send) {
		dump_buffer (thread->buffer);
		init_buffer_state (thread);
	} else
		send_log_unsafe (FALSE);

	mono_os_sem_post (&log_profiler.attach_threads_sem);

	return thread;
}

#define profiler_thread_begin(name, send)							\
	profiler_thread_begin_function (name, MONO_THREAD_NAME_WINDOWS_CONSTANT (name), STRING_LENGTH (name), (send))

static void
profiler_thread_end (MonoProfilerThread *thread, MonoOSEvent *event, gboolean send)
{
	if (send)
		send_log_unsafe (FALSE);
	else
		dump_buffer (thread->buffer);

	deinit_thread (thread);

	mono_os_event_set (event);
}

static void
profiler_thread_check_detach (MonoProfilerThread *thread)
{
	if (mono_atomic_load_i32 (&log_profiler.detach_threads) && !thread->did_detach) {
		thread->did_detach = TRUE;

		mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NONE);
		mono_thread_internal_detach (mono_thread_current ());

		mono_os_sem_post (&log_profiler.detach_threads_sem);
	}
}

static void *
helper_thread (void *arg)
{
	MonoProfilerThread *thread = profiler_thread_begin ("Profiler Helper", TRUE);

	GArray *command_sockets = g_array_new (FALSE, FALSE, sizeof (int));

	while (1) {
		fd_set rfds;
		int max_fd = -1;

		FD_ZERO (&rfds);

		mono_profhelper_add_to_fd_set (&rfds, log_profiler.server_socket, &max_fd);

#ifdef HAVE_COMMAND_PIPES
		mono_profhelper_add_to_fd_set (&rfds, log_profiler.pipes [0], &max_fd);
#endif

		for (gint i = 0; i < command_sockets->len; i++)
			mono_profhelper_add_to_fd_set (&rfds, g_array_index (command_sockets, int, i), &max_fd);

		struct timeval tv = { .tv_sec = 1, .tv_usec = 0 };

		// Sleep for 1sec or until a file descriptor has data.
		if (select (max_fd + 1, &rfds, NULL, NULL, &tv) == -1) {
			if (errno == EINTR)
				continue;

			mono_profiler_printf_err ("Could not poll in log profiler helper thread: %s", g_strerror (errno));
			exit (1);
		}

		buffer_lock_excl ();

		sync_point (SYNC_POINT_PERIODIC);

		buffer_unlock_excl ();

		// Did we get a shutdown or detach signal?
#ifdef HAVE_COMMAND_PIPES
		if (FD_ISSET (log_profiler.pipes [0], &rfds)) {
			char c;
			read (log_profiler.pipes [0], &c, 1);
			if (c == 1)
				break;
		}
#else
		int value = mono_atomic_load_i32(&log_profiler.pipe_command);
		if (value != 0) {
			while (mono_atomic_cas_i32 (&log_profiler.pipe_command, 0, value) != value)
				value = mono_atomic_load_i32(&log_profiler.pipe_command);

			char c = (char)value;
			if (c == 1)
				break;
		}
#endif

		for (gint i = 0; i < command_sockets->len; i++) {
			int fd = g_array_index (command_sockets, int, i);

			if (!FD_ISSET (fd, &rfds))
				continue;

			char buf [64];
#ifdef HOST_WIN32
			int len = recv (fd, buf, sizeof (buf) - 1, 0);
#else
			int len = read (fd, buf, sizeof (buf) - 1);
#endif
			if (len == -1)
				continue;

			if (!len) {
				// The other end disconnected.
				g_array_remove_index (command_sockets, i);
				mono_profhelper_close_socket_fd (fd);

				continue;
			}

			buf [len] = 0;

			if (!strcmp (buf, "heapshot\n"))
				trigger_heapshot ();
		}

		if (FD_ISSET (log_profiler.server_socket, &rfds)) {
			int fd = accept (log_profiler.server_socket, NULL, NULL);

			if (fd != -1) {
#ifndef HOST_WIN32
				if (fd >= FD_SETSIZE)
					mono_profhelper_close_socket_fd (fd);
				else
#endif
					g_array_append_val (command_sockets, fd);
			}
		}

		profiler_thread_check_detach (thread);
	}

	for (gint i = 0; i < command_sockets->len; i++)
		mono_profhelper_close_socket_fd (g_array_index (command_sockets, int, i));

	g_array_free (command_sockets, TRUE);

	profiler_thread_end (thread, &log_profiler.helper_thread_exited, TRUE);

	return NULL;
}

static void
start_helper_thread (void)
{
#ifdef HAVE_COMMAND_PIPES
	if (pipe (log_profiler.pipes) == -1) {
		mono_profiler_printf_err ("Could not create log profiler pipe: %s", g_strerror (errno));
		exit (1);
	}
#endif

	mono_profhelper_setup_command_server (&log_profiler.server_socket, &log_profiler.command_port, "log");

	if (!mono_native_thread_create (&log_profiler.helper_thread, helper_thread, NULL)) {
		mono_profiler_printf_err ("Could not start log profiler helper thread");
		mono_profhelper_close_socket_fd (log_profiler.server_socket);
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
			dec_method_ref_count (info->method);
			g_free (info);
		}

		g_ptr_array_free (entry->methods, TRUE);

		if (wrote_methods) {
			MonoProfilerThread *thread = get_thread ();

			dump_buffer (thread->buffer);
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
	dump_header ();

	MonoProfilerThread *thread = profiler_thread_begin ("Profiler Writer", FALSE);

	while (mono_atomic_load_i32 (&log_profiler.run_writer_thread)) {
		MONO_ENTER_GC_SAFE;
		mono_os_sem_wait (&log_profiler.writer_queue_sem, MONO_SEM_FLAGS_NONE);
		MONO_EXIT_GC_SAFE;
		handle_writer_queue_entry ();

		profiler_thread_check_detach (thread);
	}

	/* Drain any remaining entries on shutdown. */
	while (handle_writer_queue_entry ());

	profiler_thread_end (thread, &log_profiler.writer_thread_exited, FALSE);

	return NULL;
}

static void
start_writer_thread (void)
{
	mono_atomic_store_i32 (&log_profiler.run_writer_thread, 1);

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

				MonoJitInfo *ji = mono_jit_info_table_find_internal (address, TRUE, FALSE);

				if (ji)
					method = mono_jit_info_get_method (ji);

				if (method)
					inc_method_ref_count (method);

				sample->frames [i].method = method;
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

		for (int i = 0; i < sample->count; ++i) {
			MonoMethod *method = sample->frames [i].method;

			if (method)
				dec_method_ref_count (method);
		}

		mono_thread_hazardous_try_free (sample, reuse_sample_hit);

		dump_unmanaged_coderefs ();
	}

	return FALSE;
}

static void *
dumper_thread (void *arg)
{
	MonoProfilerThread *thread = profiler_thread_begin ("Profiler Dumper", TRUE);

	while (mono_atomic_load_i32 (&log_profiler.run_dumper_thread)) {
		gboolean timedout = FALSE;
		MONO_ENTER_GC_SAFE;
		/*
		 * Flush samples every second so it doesn't seem like the profiler is
		 * not working if the program is mostly idle.
		 */
		timedout = mono_os_sem_timedwait (&log_profiler.dumper_queue_sem, 1000, MONO_SEM_FLAGS_NONE) == MONO_SEM_TIMEDWAIT_RET_TIMEDOUT;
		MONO_EXIT_GC_SAFE;
		if (timedout)
			send_log_unsafe (FALSE);

		handle_dumper_queue_entry ();

		profiler_thread_check_detach (thread);
	}

	/* Drain any remaining entries on shutdown. */
	while (handle_dumper_queue_entry ());

	profiler_thread_end (thread, &log_profiler.dumper_thread_exited, TRUE);

	return NULL;
}

static void
start_dumper_thread (void)
{
	mono_atomic_store_i32 (&log_profiler.run_dumper_thread, 1);

	if (!mono_native_thread_create (&log_profiler.dumper_thread, dumper_thread, NULL)) {
		mono_profiler_printf_err ("Could not start log profiler dumper thread");
		exit (1);
	}
}

#ifdef __GNUC__
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wmissing-prototypes"
#endif

ICALL_EXPORT gint32
proflog_icall_GetMaxStackTraceFrames (void)
{
	return MAX_FRAMES;
}

ICALL_EXPORT gint32
proflog_icall_GetStackTraceFrames (void)
{
	return log_config.num_frames;
}

ICALL_EXPORT void
proflog_icall_SetStackTraceFrames (gint32 value)
{
	log_config.num_frames = value;
}

ICALL_EXPORT MonoProfilerHeapshotMode
proflog_icall_GetHeapshotMode (void)
{
	return log_config.hs_mode;
}

ICALL_EXPORT void
proflog_icall_SetHeapshotMode (MonoProfilerHeapshotMode value)
{
	log_config.hs_mode = value;
}

ICALL_EXPORT gint32
proflog_icall_GetHeapshotMillisecondsFrequency (void)
{
	return log_config.hs_freq_ms;
}

ICALL_EXPORT void
proflog_icall_SetHeapshotMillisecondsFrequency (gint32 value)
{
	log_config.hs_freq_ms = value;
}

ICALL_EXPORT gint32
proflog_icall_GetHeapshotCollectionsFrequency (void)
{
	return log_config.hs_freq_gc;
}

ICALL_EXPORT void
proflog_icall_SetHeapshotCollectionsFrequency (gint32 value)
{
	log_config.hs_freq_gc = value;
}

ICALL_EXPORT void
proflog_icall_TriggerHeapshot (void)
{
	trigger_heapshot ();
}

ICALL_EXPORT gint32
proflog_icall_GetCallDepth (void)
{
	return log_config.max_call_depth;
}

ICALL_EXPORT void
proflog_icall_SetCallDepth (gint32 value)
{
	log_config.max_call_depth = value;
}

ICALL_EXPORT void
proflog_icall_GetSampleMode (MonoProfilerSampleMode *mode, gint32 *frequency)
{
	uint32_t freq;

	mono_profiler_get_sample_mode (log_profiler.handle, mode, &freq);

	*frequency = freq;
}

ICALL_EXPORT MonoBoolean
proflog_icall_SetSampleMode (MonoProfilerSampleMode mode, gint32 frequency)
{
	mono_coop_mutex_lock (&log_profiler.api_mutex);

	mono_bool result = mono_profiler_set_sample_mode (log_profiler.handle, mode, frequency);

	if (mode != MONO_PROFILER_SAMPLE_MODE_NONE) {
		ENABLE (PROFLOG_SAMPLE_EVENTS);
		mono_profiler_set_sample_hit_callback (log_profiler.handle, mono_sample_hit);
	} else {
		DISABLE (PROFLOG_SAMPLE_EVENTS);
		mono_profiler_set_sample_hit_callback (log_profiler.handle, NULL);
	}

	mono_coop_mutex_unlock (&log_profiler.api_mutex);

	return result;
}

ICALL_EXPORT MonoBoolean
proflog_icall_GetExceptionEvents (void)
{
	return ENABLED (PROFLOG_EXCEPTION_EVENTS);
}

ICALL_EXPORT void
proflog_icall_SetExceptionEvents (MonoBoolean value)
{
	mono_coop_mutex_lock (&log_profiler.api_mutex);

	if (value) {
		ENABLE (PROFLOG_EXCEPTION_EVENTS);
		mono_profiler_set_exception_throw_callback (log_profiler.handle, throw_exc);
		mono_profiler_set_exception_clause_callback (log_profiler.handle, clause_exc);
	} else {
		DISABLE (PROFLOG_EXCEPTION_EVENTS);
		mono_profiler_set_exception_throw_callback (log_profiler.handle, NULL);
		mono_profiler_set_exception_clause_callback (log_profiler.handle, NULL);
	}

	mono_coop_mutex_unlock (&log_profiler.api_mutex);
}

ICALL_EXPORT MonoBoolean
proflog_icall_GetMonitorEvents (void)
{
	return ENABLED (PROFLOG_MONITOR_EVENTS);
}

ICALL_EXPORT void
proflog_icall_SetMonitorEvents (MonoBoolean value)
{
	mono_coop_mutex_lock (&log_profiler.api_mutex);

	if (value) {
		ENABLE (PROFLOG_MONITOR_EVENTS);
		mono_profiler_set_monitor_contention_callback (log_profiler.handle, monitor_contention);
		mono_profiler_set_monitor_acquired_callback (log_profiler.handle, monitor_acquired);
		mono_profiler_set_monitor_failed_callback (log_profiler.handle, monitor_failed);
	} else {
		DISABLE (PROFLOG_MONITOR_EVENTS);
		mono_profiler_set_monitor_contention_callback (log_profiler.handle, NULL);
		mono_profiler_set_monitor_acquired_callback (log_profiler.handle, NULL);
		mono_profiler_set_monitor_failed_callback (log_profiler.handle, NULL);
	}

	mono_coop_mutex_unlock (&log_profiler.api_mutex);
}

ICALL_EXPORT MonoBoolean
proflog_icall_GetGCEvents (void)
{
	return ENABLED (PROFLOG_GC_EVENTS);
}

ICALL_EXPORT void
proflog_icall_SetGCEvents (MonoBoolean value)
{
	mono_coop_mutex_lock (&log_profiler.api_mutex);

	if (value)
		ENABLE (PROFLOG_GC_EVENTS);
	else
		DISABLE (PROFLOG_GC_EVENTS);

	mono_coop_mutex_unlock (&log_profiler.api_mutex);
}

ICALL_EXPORT MonoBoolean
proflog_icall_GetGCAllocationEvents (void)
{
	return ENABLED (PROFLOG_GC_ALLOCATION_EVENTS);
}

ICALL_EXPORT void
proflog_icall_SetGCAllocationEvents (MonoBoolean value)
{
	mono_coop_mutex_lock (&log_profiler.api_mutex);

	if (value) {
		ENABLE (PROFLOG_GC_ALLOCATION_EVENTS);
		mono_profiler_set_gc_allocation_callback (log_profiler.handle, gc_alloc);
	} else {
		DISABLE (PROFLOG_GC_ALLOCATION_EVENTS);
		mono_profiler_set_gc_allocation_callback (log_profiler.handle, NULL);
	}

	mono_coop_mutex_unlock (&log_profiler.api_mutex);
}

ICALL_EXPORT MonoBoolean
proflog_icall_GetGCMoveEvents (void)
{
	return ENABLED (PROFLOG_GC_MOVE_EVENTS);
}

ICALL_EXPORT void
proflog_icall_SetGCMoveEvents (MonoBoolean value)
{
	mono_coop_mutex_lock (&log_profiler.api_mutex);

	if (value) {
		ENABLE (PROFLOG_GC_MOVE_EVENTS);
		mono_profiler_set_gc_moves_callback (log_profiler.handle, gc_moves);
	} else {
		DISABLE (PROFLOG_GC_MOVE_EVENTS);
		mono_profiler_set_gc_moves_callback (log_profiler.handle, NULL);
	}

	mono_coop_mutex_unlock (&log_profiler.api_mutex);
}

ICALL_EXPORT MonoBoolean
proflog_icall_GetGCRootEvents (void)
{
	return ENABLED (PROFLOG_GC_ROOT_EVENTS);
}

ICALL_EXPORT void
proflog_icall_SetGCRootEvents (MonoBoolean value)
{
	mono_coop_mutex_lock (&log_profiler.api_mutex);

	if (value)
		ENABLE (PROFLOG_GC_ROOT_EVENTS);
	else
		DISABLE (PROFLOG_GC_ROOT_EVENTS);

	mono_coop_mutex_unlock (&log_profiler.api_mutex);
}

ICALL_EXPORT MonoBoolean
proflog_icall_GetGCHandleEvents (void)
{
	return ENABLED (PROFLOG_GC_HANDLE_EVENTS);
}

ICALL_EXPORT void
proflog_icall_SetGCHandleEvents (MonoBoolean value)
{
	mono_coop_mutex_lock (&log_profiler.api_mutex);

	if (value) {
		ENABLE (PROFLOG_GC_HANDLE_EVENTS);
		mono_profiler_set_gc_handle_created_callback (log_profiler.handle, gc_handle_created);
		mono_profiler_set_gc_handle_deleted_callback (log_profiler.handle, gc_handle_deleted);
	} else {
		DISABLE (PROFLOG_GC_HANDLE_EVENTS);
		mono_profiler_set_gc_handle_created_callback (log_profiler.handle, NULL);
		mono_profiler_set_gc_handle_deleted_callback (log_profiler.handle, NULL);
	}

	mono_coop_mutex_unlock (&log_profiler.api_mutex);
}

ICALL_EXPORT MonoBoolean
proflog_icall_GetGCFinalizationEvents (void)
{
	return ENABLED (PROFLOG_GC_FINALIZATION_EVENTS);
}

ICALL_EXPORT void
proflog_icall_SetGCFinalizationEvents (MonoBoolean value)
{
	mono_coop_mutex_lock (&log_profiler.api_mutex);

	if (value) {
		ENABLE (PROFLOG_GC_FINALIZATION_EVENTS);
		mono_profiler_set_gc_finalizing_callback (log_profiler.handle, finalize_begin);
		mono_profiler_set_gc_finalizing_object_callback (log_profiler.handle, finalize_object_begin);
		mono_profiler_set_gc_finalized_object_callback (log_profiler.handle, finalize_object_end);
	} else {
		DISABLE (PROFLOG_GC_FINALIZATION_EVENTS);
		mono_profiler_set_gc_finalizing_callback (log_profiler.handle, NULL);
		mono_profiler_set_gc_finalizing_object_callback (log_profiler.handle, NULL);
		mono_profiler_set_gc_finalized_object_callback (log_profiler.handle, NULL);
	}

	mono_coop_mutex_unlock (&log_profiler.api_mutex);
}

ICALL_EXPORT MonoBoolean
proflog_icall_GetJitEvents (void)
{
	return ENABLED (PROFLOG_JIT_EVENTS);
}

ICALL_EXPORT void
proflog_icall_SetJitEvents (MonoBoolean value)
{
	mono_coop_mutex_lock (&log_profiler.api_mutex);

	if (value) {
		ENABLE (PROFLOG_JIT_EVENTS);
		mono_profiler_set_jit_code_buffer_callback (log_profiler.handle, code_buffer_new);
	} else {
		DISABLE (PROFLOG_JIT_EVENTS);
		mono_profiler_set_jit_code_buffer_callback (log_profiler.handle, NULL);
	}

	mono_coop_mutex_unlock (&log_profiler.api_mutex);
}

#ifdef __GNUC__
#pragma GCC diagnostic pop
#endif

static void
runtime_initialized (MonoProfiler *profiler)
{
#ifdef HOST_WIN32
	/*
	* We depend on socket support in this profiler provider we need to
	* make sure we keep a reference on WSA for the lifetime of this provider.
	*/
	WSADATA wsadata;
	int err;

	err = WSAStartup (2 /* 2.0 */, &wsadata);
	if (err) {
		mono_profiler_printf_err ("Couldn't initialise networking.");
		exit (1);
	}
#endif
	mono_atomic_store_i32 (&log_profiler.runtime_inited, 1);

	mono_os_sem_init (&log_profiler.attach_threads_sem, 0);

	/*
	 * We must start the helper thread before the writer thread. This is
	 * because start_helper_thread () sets up the command port which is written
	 * to the log header by the writer thread.
	 */
	start_helper_thread ();
	start_writer_thread ();
	start_dumper_thread ();

	/*
	 * Wait for all the internal threads to be started. If we don't do this, we
	 * might shut down before they finish initializing, which could lead to
	 * various deadlocks when waiting for them to exit during shutdown.
	 */
	for (int i = 0; i < 3; i++)
		mono_os_sem_wait (&log_profiler.attach_threads_sem, MONO_SEM_FLAGS_NONE);

	mono_os_sem_destroy (&log_profiler.attach_threads_sem);

	mono_coop_mutex_init (&log_profiler.api_mutex);

#define ADD_ICALL(NAME) \
	mono_add_internal_call_internal ("Mono.Profiler.Log.LogProfiler::" EGLIB_STRINGIFY (NAME), proflog_icall_ ## NAME);

	ADD_ICALL (GetMaxStackTraceFrames);
	ADD_ICALL (GetStackTraceFrames);
	ADD_ICALL (SetStackTraceFrames);
	ADD_ICALL (GetHeapshotMode);
	ADD_ICALL (SetHeapshotMode);
	ADD_ICALL (GetHeapshotMillisecondsFrequency);
	ADD_ICALL (SetHeapshotMillisecondsFrequency);
	ADD_ICALL (GetHeapshotCollectionsFrequency);
	ADD_ICALL (SetHeapshotCollectionsFrequency);
	ADD_ICALL (TriggerHeapshot);
	ADD_ICALL (GetCallDepth);
	ADD_ICALL (SetCallDepth);
	ADD_ICALL (GetSampleMode);
	ADD_ICALL (SetSampleMode);
	ADD_ICALL (GetExceptionEvents);
	ADD_ICALL (SetExceptionEvents);
	ADD_ICALL (GetMonitorEvents);
	ADD_ICALL (SetMonitorEvents);
	ADD_ICALL (GetGCEvents);
	ADD_ICALL (SetGCEvents);
	ADD_ICALL (GetGCAllocationEvents);
	ADD_ICALL (SetGCAllocationEvents);
	ADD_ICALL (GetGCMoveEvents);
	ADD_ICALL (SetGCMoveEvents);
	ADD_ICALL (GetGCRootEvents);
	ADD_ICALL (SetGCRootEvents);
	ADD_ICALL (GetGCHandleEvents);
	ADD_ICALL (SetGCHandleEvents);
	ADD_ICALL (GetGCFinalizationEvents);
	ADD_ICALL (SetGCFinalizationEvents);
	ADD_ICALL (GetJitEvents);
	ADD_ICALL (SetJitEvents);

#undef ADD_ICALL
}

static void
create_profiler (const char *args, const char *filename, GPtrArray *filters)
{
	char *nf;

	log_profiler.args = pstrdup (args);
	log_profiler.command_port = log_config.command_port;

	//If filename begin with +, append the pid at the end
	if (filename && *filename == '+')
		filename = g_strdup_printf ("%s.%d", filename + 1, (int)process_id ());

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
#if HAVE_API_SUPPORT_WIN32_PIPE_OPEN_CLOSE && !defined (HOST_WIN32)
		log_profiler.file = popen (nf + 1, "w");
		log_profiler.pipe_output = 1;
#else
		mono_profiler_printf_err ("Platform doesn't support popen");
#endif
	} else if (*nf == '#') {
		int fd = strtol (nf + 1, NULL, 10);
		log_profiler.file = fdopen (fd, "a");
	} else
		log_profiler.file = fopen (nf, "wb");

	if (!log_profiler.file) {
		mono_profiler_printf_err ("Could not create log profiler output file '%s': %s", nf, g_strerror (errno));
		exit (1);
	}

#ifndef DISABLE_LOG_PROFILER_GZ
	if (log_config.use_zip)
		log_profiler.gzfile = gzdopen (fileno (log_profiler.file), "wb");
#endif

	// FIXME: We should free this stuff too.
	mono_lock_free_allocator_init_size_class (&log_profiler.sample_size_class, SAMPLE_SLOT_SIZE (log_config.num_frames), SAMPLE_BLOCK_SIZE);
	mono_lock_free_allocator_init_allocator (&log_profiler.sample_allocator, &log_profiler.sample_size_class, MONO_MEM_ACCOUNT_PROFILER);

	mono_lock_free_queue_init (&log_profiler.sample_reuse_queue);

	// FIXME: We should free this stuff too.
	mono_lock_free_allocator_init_size_class (&log_profiler.writer_entry_size_class, sizeof (WriterQueueEntry), WRITER_ENTRY_BLOCK_SIZE);
	mono_lock_free_allocator_init_allocator (&log_profiler.writer_entry_allocator, &log_profiler.writer_entry_size_class, MONO_MEM_ACCOUNT_PROFILER);

	mono_os_event_init (&log_profiler.helper_thread_exited, FALSE);

	mono_os_event_init (&log_profiler.writer_thread_exited, FALSE);
	mono_lock_free_queue_init (&log_profiler.writer_queue);
	mono_os_sem_init (&log_profiler.writer_queue_sem, 0);

	mono_os_event_init (&log_profiler.dumper_thread_exited, FALSE);
	mono_lock_free_queue_init (&log_profiler.dumper_queue);
	mono_os_sem_init (&log_profiler.dumper_queue_sem, 0);

	mono_os_mutex_init (&log_profiler.method_table_mutex);
	log_profiler.method_table = mono_conc_hashtable_new (NULL, NULL);

	log_profiler.startup_time = current_time ();
}

MONO_API void
mono_profiler_init_log (const char *desc);

void
mono_profiler_init_log (const char *desc)
{
	/*
	 * If you hit this assert while increasing MAX_FRAMES, you need to increase
	 * SAMPLE_BLOCK_SIZE as well.
	 */
	g_assert (SAMPLE_SLOT_SIZE (MAX_FRAMES) * 2 < LOCK_FREE_ALLOC_SB_USABLE_SIZE (SAMPLE_BLOCK_SIZE));
	g_assert (sizeof (WriterQueueEntry) * 2 < LOCK_FREE_ALLOC_SB_USABLE_SIZE (WRITER_ENTRY_BLOCK_SIZE));

	GPtrArray *filters = NULL;

	proflog_parse_args (&log_config, desc [3] == ':' ? desc + 4 : "");

	MonoProfilerHandle handle = log_profiler.handle = mono_profiler_create (&log_profiler);

	if (log_config.enter_leave)
		mono_profiler_set_call_instrumentation_filter_callback (handle, method_filter);

	/*
	 * If the runtime was invoked for the purpose of AOT compilation only, the
	 * only thing we want to do is install the call instrumentation filter.
	 */
	if (mono_jit_aot_compiling ())
		goto done;

	init_time ();

	create_profiler (desc, log_config.output_filename, filters);

	mono_lls_init (&log_profiler.profiler_thread_list, NULL);

	/*
	 * Required callbacks. These are either necessary for the profiler itself
	 * to function, or provide metadata that's needed if other events (e.g.
	 * allocations, exceptions) are dynamically enabled/disabled.
	 */

	mono_profiler_set_runtime_initialized_callback (handle, runtime_initialized);

	mono_profiler_set_gc_event_callback (handle, gc_event);

	mono_profiler_set_thread_started_callback (handle, thread_start);
	mono_profiler_set_thread_exited_callback (handle, thread_end);
	mono_profiler_set_thread_name_callback (handle, thread_name);

	mono_profiler_set_domain_loaded_callback (handle, domain_loaded);
	mono_profiler_set_domain_unloading_callback (handle, domain_unloaded);
	mono_profiler_set_domain_name_callback (handle, domain_name);

	mono_profiler_set_context_loaded_callback (handle, context_loaded);
	mono_profiler_set_context_unloaded_callback (handle, context_unloaded);

	mono_profiler_set_assembly_loaded_callback (handle, assembly_loaded);
	mono_profiler_set_assembly_unloading_callback (handle, assembly_unloaded);

	mono_profiler_set_image_loaded_callback (handle, image_loaded);
	mono_profiler_set_image_unloading_callback (handle, image_unloaded);

	mono_profiler_set_class_loaded_callback (handle, class_loaded);

	mono_profiler_set_vtable_loaded_callback (handle, vtable_loaded);

	mono_profiler_set_jit_done_callback (handle, method_jitted);

	mono_profiler_set_gc_root_register_callback (handle, gc_root_register);
	mono_profiler_set_gc_root_unregister_callback (handle, gc_root_deregister);

	if (ENABLED (PROFLOG_EXCEPTION_EVENTS)) {
		mono_profiler_set_exception_throw_callback (handle, throw_exc);
		mono_profiler_set_exception_clause_callback (handle, clause_exc);
	}

	if (ENABLED (PROFLOG_MONITOR_EVENTS)) {
		mono_profiler_set_monitor_contention_callback (handle, monitor_contention);
		mono_profiler_set_monitor_acquired_callback (handle, monitor_acquired);
		mono_profiler_set_monitor_failed_callback (handle, monitor_failed);
	}

	if (ENABLED (PROFLOG_GC_EVENTS))
		mono_profiler_set_gc_resize_callback (handle, gc_resize);

	if (ENABLED (PROFLOG_GC_ALLOCATION_EVENTS))
		mono_profiler_set_gc_allocation_callback (handle, gc_alloc);

	if (ENABLED (PROFLOG_GC_MOVE_EVENTS))
		mono_profiler_set_gc_moves_callback (handle, gc_moves);

	if (ENABLED (PROFLOG_GC_HANDLE_EVENTS)) {
		mono_profiler_set_gc_handle_created_callback (handle, gc_handle_created);
		mono_profiler_set_gc_handle_deleted_callback (handle, gc_handle_deleted);
	}

	if (ENABLED (PROFLOG_GC_FINALIZATION_EVENTS)) {
		mono_profiler_set_gc_finalizing_callback (handle, finalize_begin);
		mono_profiler_set_gc_finalized_callback (handle, finalize_end);
		mono_profiler_set_gc_finalizing_object_callback (handle, finalize_object_begin);
		mono_profiler_set_gc_finalized_object_callback (handle, finalize_object_end);
	}

	//On Demand heapshot uses the finalizer thread to force a collection and thus a heapshot
	mono_profiler_set_gc_finalized_callback (handle, finalize_end);

	if (ENABLED (PROFLOG_SAMPLE_EVENTS))
		mono_profiler_set_sample_hit_callback (handle, mono_sample_hit);

	if (ENABLED (PROFLOG_JIT_EVENTS))
		mono_profiler_set_jit_code_buffer_callback (handle, code_buffer_new);

	if (log_config.enter_leave) {
		mono_profiler_set_method_enter_callback (handle, method_enter);
		mono_profiler_set_method_leave_callback (handle, method_leave);
		mono_profiler_set_method_tail_call_callback (handle, tailcall);
		mono_profiler_set_method_exception_leave_callback (handle, method_exc_leave);
	}

	mono_profiler_enable_allocations ();
	mono_profiler_enable_clauses ();
	mono_profiler_enable_sampling (handle);

	/*
	 * If no sample option was given by the user, this just leaves the sampling
	 * thread in idle mode. We do this even if no option was given so that we
	 * can warn if another profiler controls sampling parameters.
	 */
	if (!mono_profiler_set_sample_mode (handle, log_config.sampling_mode, log_config.sample_freq))
		mono_profiler_printf_err ("Another profiler controls sampling parameters; the log profiler will not be able to modify them.");

done:
	;
}
