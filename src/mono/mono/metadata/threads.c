/**
 * \file
 * Thread support internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *	Paolo Molaro (lupus@ximian.com)
 *	Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#include <glib.h>
#include <string.h>

#include <mono/metadata/object.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/debug-internals.h>
#include <mono/utils/monobitset.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/os-event.h>
#include <mono/utils/mono-threads-debug.h>
#include <mono/metadata/w32handle.h>
#include <mono/metadata/w32event.h>
#include <mono/metadata/w32mutex.h>

#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/w32error.h>
#include <mono/utils/w32api.h>

#ifdef HAVE_SIGNAL_H
#include <signal.h>
#endif

#if defined(HOST_WIN32)
#include <objbase.h>
#endif

#if defined(PLATFORM_ANDROID) && !defined(TARGET_ARM64) && !defined(TARGET_AMD64)
#define USE_TKILL_ON_ANDROID 1
#endif

#ifdef PLATFORM_ANDROID
#include <errno.h>

#ifdef USE_TKILL_ON_ANDROID
extern int tkill (pid_t tid, int signal);
#endif
#endif

/*#define THREAD_DEBUG(a) do { a; } while (0)*/
#define THREAD_DEBUG(a)
/*#define THREAD_WAIT_DEBUG(a) do { a; } while (0)*/
#define THREAD_WAIT_DEBUG(a)
/*#define LIBGC_DEBUG(a) do { a; } while (0)*/
#define LIBGC_DEBUG(a)

#define SPIN_TRYLOCK(i) (InterlockedCompareExchange (&(i), 1, 0) == 0)
#define SPIN_LOCK(i) do { \
				if (SPIN_TRYLOCK (i)) \
					break; \
			} while (1)

#define SPIN_UNLOCK(i) i = 0

#define LOCK_THREAD(thread) lock_thread((thread))
#define UNLOCK_THREAD(thread) unlock_thread((thread))

typedef union {
	gint32 ival;
	gfloat fval;
} IntFloatUnion;

typedef union {
	gint64 ival;
	gdouble fval;
} LongDoubleUnion;
 
typedef struct _StaticDataFreeList StaticDataFreeList;
struct _StaticDataFreeList {
	StaticDataFreeList *next;
	guint32 offset;
	guint32 size;
};

typedef struct {
	int idx;
	int offset;
	StaticDataFreeList *freelist;
} StaticDataInfo;

/* Controls access to the 'threads' hash table */
static void mono_threads_lock (void);
static void mono_threads_unlock (void);
static MonoCoopMutex threads_mutex;

/* Controls access to the 'joinable_threads' hash table */
#define joinable_threads_lock() mono_os_mutex_lock (&joinable_threads_mutex)
#define joinable_threads_unlock() mono_os_mutex_unlock (&joinable_threads_mutex)
static mono_mutex_t joinable_threads_mutex;

/* Holds current status of static data heap */
static StaticDataInfo thread_static_info;
static StaticDataInfo context_static_info;

/* The hash of existing threads (key is thread ID, value is
 * MonoInternalThread*) that need joining before exit
 */
static MonoGHashTable *threads=NULL;

/* List of app context GC handles.
 * Added to from mono_threads_register_app_context ().
 */
static GHashTable *contexts = NULL;

/* Cleanup queue for contexts. */
static MonoReferenceQueue *context_queue;

/*
 * Threads which are starting up and they are not in the 'threads' hash yet.
 * When mono_thread_attach_internal is called for a thread, it will be removed from this hash table.
 * Protected by mono_threads_lock ().
 */
static MonoGHashTable *threads_starting_up = NULL;

/* Contains tids */
/* Protected by the threads lock */
static GHashTable *joinable_threads;
static int joinable_thread_count;

#define SET_CURRENT_OBJECT(x) mono_tls_set_thread (x)
#define GET_CURRENT_OBJECT() (MonoInternalThread*) mono_tls_get_thread ()

/* function called at thread start */
static MonoThreadStartCB mono_thread_start_cb = NULL;

/* function called at thread attach */
static MonoThreadAttachCB mono_thread_attach_cb = NULL;

/* function called at thread cleanup */
static MonoThreadCleanupFunc mono_thread_cleanup_fn = NULL;

/* The default stack size for each thread */
static guint32 default_stacksize = 0;
#define default_stacksize_for_thread(thread) ((thread)->stack_size? (thread)->stack_size: default_stacksize)

static void context_adjust_static_data (MonoAppContext *ctx);
static void mono_free_static_data (gpointer* static_data);
static void mono_init_static_data_info (StaticDataInfo *static_data);
static guint32 mono_alloc_static_data_slot (StaticDataInfo *static_data, guint32 size, guint32 align);
static gboolean mono_thread_resume (MonoInternalThread* thread);
static void async_abort_internal (MonoInternalThread *thread, gboolean install_async_abort);
static void self_abort_internal (MonoError *error);
static void async_suspend_internal (MonoInternalThread *thread, gboolean interrupt);
static void self_suspend_internal (void);

static MonoException* mono_thread_execute_interruption (void);
static void ref_stack_destroy (gpointer rs);

/* Spin lock for InterlockedXXX 64 bit functions */
#define mono_interlocked_lock() mono_os_mutex_lock (&interlocked_mutex)
#define mono_interlocked_unlock() mono_os_mutex_unlock (&interlocked_mutex)
static mono_mutex_t interlocked_mutex;

/* global count of thread interruptions requested */
static gint32 thread_interruption_requested = 0;

/* Event signaled when a thread changes its background mode */
static MonoOSEvent background_change_event;

static gboolean shutting_down = FALSE;

static gint32 managed_thread_id_counter = 0;

/* Class lazy loading functions */
static GENERATE_GET_CLASS_WITH_CACHE (appdomain_unloaded_exception, "System", "AppDomainUnloadedException")

static void
mono_threads_lock (void)
{
	mono_locks_coop_acquire (&threads_mutex, ThreadsLock);
}

static void
mono_threads_unlock (void)
{
	mono_locks_coop_release (&threads_mutex, ThreadsLock);
}


static guint32
get_next_managed_thread_id (void)
{
	return InterlockedIncrement (&managed_thread_id_counter);
}

/*
 * We separate interruptions/exceptions into either sync (they can be processed anytime,
 * normally as soon as they are set, and are set by the same thread) and async (they can't
 * be processed inside abort protected blocks and are normally set by other threads). We
 * can have both a pending sync and async interruption. In this case, the sync exception is
 * processed first. Since we clean sync flag first, mono_thread_execute_interruption must
 * also handle all sync type exceptions before the async type exceptions.
 */
enum {
	INTERRUPT_SYNC_REQUESTED_BIT = 0x1,
	INTERRUPT_ASYNC_REQUESTED_BIT = 0x2,
	INTERRUPT_REQUESTED_MASK = 0x3,
	ABORT_PROT_BLOCK_SHIFT = 2,
	ABORT_PROT_BLOCK_BITS = 8,
	ABORT_PROT_BLOCK_MASK = (((1 << ABORT_PROT_BLOCK_BITS) - 1) << ABORT_PROT_BLOCK_SHIFT)
};

static int
mono_thread_get_abort_prot_block_count (MonoInternalThread *thread)
{
	gsize state = thread->thread_state;
	return (state & ABORT_PROT_BLOCK_MASK) >> ABORT_PROT_BLOCK_SHIFT;
}

void
mono_threads_begin_abort_protected_block (void)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	gsize old_state, new_state;
	int new_val;
	do {
		old_state = thread->thread_state;

		new_val = ((old_state & ABORT_PROT_BLOCK_MASK) >> ABORT_PROT_BLOCK_SHIFT) + 1;
		//bounds check abort_prot_count
		g_assert (new_val > 0);
		g_assert (new_val < (1 << ABORT_PROT_BLOCK_BITS));

		new_state = old_state + (1 << ABORT_PROT_BLOCK_SHIFT);
	} while (InterlockedCompareExchangePointer ((volatile gpointer)&thread->thread_state, (gpointer)new_state, (gpointer)old_state) != (gpointer)old_state);

	/* Defer async request since we won't be able to process until exiting the block */
	if (new_val == 1 && (new_state & INTERRUPT_ASYNC_REQUESTED_BIT)) {
		InterlockedDecrement (&thread_interruption_requested);
		THREADS_INTERRUPT_DEBUG ("[%d] begin abort protected block old_state %ld new_state %ld, defer tir %d\n", thread->small_id, old_state, new_state, thread_interruption_requested);
		if (thread_interruption_requested < 0)
			g_warning ("bad thread_interruption_requested state");
	} else {
		THREADS_INTERRUPT_DEBUG ("[%d] begin abort protected block old_state %ld new_state %ld, tir %d\n", thread->small_id, old_state, new_state, thread_interruption_requested);
	}
}

static gboolean
mono_thread_state_has_interruption (gsize state)
{
	/* pending exception, self abort */
	if (state & INTERRUPT_SYNC_REQUESTED_BIT)
		return TRUE;

	/* abort, interruption, suspend */
	if ((state & INTERRUPT_ASYNC_REQUESTED_BIT) && !(state & ABORT_PROT_BLOCK_MASK))
		return TRUE;

	return FALSE;
}

gboolean
mono_threads_end_abort_protected_block (void)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	gsize old_state, new_state;
	int new_val;
	do {
		old_state = thread->thread_state;

		//bounds check abort_prot_count
		new_val = ((old_state & ABORT_PROT_BLOCK_MASK) >> ABORT_PROT_BLOCK_SHIFT) - 1;
		g_assert (new_val >= 0);
		g_assert (new_val < (1 << ABORT_PROT_BLOCK_BITS));

		new_state = old_state - (1 << ABORT_PROT_BLOCK_SHIFT);
	} while (InterlockedCompareExchangePointer ((volatile gpointer)&thread->thread_state, (gpointer)new_state, (gpointer)old_state) != (gpointer)old_state);

	if (new_val == 0 && (new_state & INTERRUPT_ASYNC_REQUESTED_BIT)) {
		InterlockedIncrement (&thread_interruption_requested);
		THREADS_INTERRUPT_DEBUG ("[%d] end abort protected block old_state %ld new_state %ld, restore tir %d\n", thread->small_id, old_state, new_state, thread_interruption_requested);
	} else {
		THREADS_INTERRUPT_DEBUG ("[%d] end abort protected block old_state %ld new_state %ld, tir %d\n", thread->small_id, old_state, new_state, thread_interruption_requested);
	}

	return mono_thread_state_has_interruption (new_state);
}

static gboolean
mono_thread_get_interruption_requested (MonoInternalThread *thread)
{
	gsize state = thread->thread_state;

	return mono_thread_state_has_interruption (state);
}

/*
 * Returns TRUE is there was a state change
 * We clear a single interruption request, sync has priority.
 */
static gboolean
mono_thread_clear_interruption_requested (MonoInternalThread *thread)
{
	gsize old_state, new_state;
	do {
		old_state = thread->thread_state;

		// no interruption to process
		if (!(old_state & INTERRUPT_SYNC_REQUESTED_BIT) &&
				(!(old_state & INTERRUPT_ASYNC_REQUESTED_BIT) || (old_state & ABORT_PROT_BLOCK_MASK)))
			return FALSE;

		if (old_state & INTERRUPT_SYNC_REQUESTED_BIT)
			new_state = old_state & ~INTERRUPT_SYNC_REQUESTED_BIT;
		else
			new_state = old_state & ~INTERRUPT_ASYNC_REQUESTED_BIT;
	} while (InterlockedCompareExchangePointer ((volatile gpointer)&thread->thread_state, (gpointer)new_state, (gpointer)old_state) != (gpointer)old_state);

	InterlockedDecrement (&thread_interruption_requested);
	THREADS_INTERRUPT_DEBUG ("[%d] clear interruption old_state %ld new_state %ld, tir %d\n", thread->small_id, old_state, new_state, thread_interruption_requested);
	if (thread_interruption_requested < 0)
		g_warning ("bad thread_interruption_requested state");
	return TRUE;
}

/* Returns TRUE is there was a state change and the interruption can be processed */
static gboolean
mono_thread_set_interruption_requested (MonoInternalThread *thread)
{
	//always force when the current thread is doing it to itself.
	gboolean sync = thread == mono_thread_internal_current ();
	gsize old_state, new_state;
	do {
		old_state = thread->thread_state;

		//Already set
		if ((sync && (old_state & INTERRUPT_SYNC_REQUESTED_BIT)) ||
				(!sync && (old_state & INTERRUPT_ASYNC_REQUESTED_BIT)))
			return FALSE;

		if (sync)
			new_state = old_state | INTERRUPT_SYNC_REQUESTED_BIT;
		else
			new_state = old_state | INTERRUPT_ASYNC_REQUESTED_BIT;
	} while (InterlockedCompareExchangePointer ((volatile gpointer)&thread->thread_state, (gpointer)new_state, (gpointer)old_state) != (gpointer)old_state);

	if (sync || !(new_state & ABORT_PROT_BLOCK_MASK)) {
		InterlockedIncrement (&thread_interruption_requested);
		THREADS_INTERRUPT_DEBUG ("[%d] set interruption on [%d] old_state %ld new_state %ld, tir %d\n", mono_thread_internal_current ()->small_id, thread->small_id, old_state, new_state, thread_interruption_requested);
	} else {
		THREADS_INTERRUPT_DEBUG ("[%d] set interruption on [%d] old_state %ld new_state %ld, tir deferred %d\n", mono_thread_internal_current ()->small_id, thread->small_id, old_state, new_state, thread_interruption_requested);
	}

	return sync || !(new_state & ABORT_PROT_BLOCK_MASK);
}

static inline MonoNativeThreadId
thread_get_tid (MonoInternalThread *thread)
{
	/* We store the tid as a guint64 to keep the object layout constant between platforms */
	return MONO_UINT_TO_NATIVE_THREAD_ID (thread->tid);
}

static void ensure_synch_cs_set (MonoInternalThread *thread)
{
	MonoCoopMutex *synch_cs;

	if (thread->synch_cs != NULL) {
		return;
	}

	synch_cs = g_new0 (MonoCoopMutex, 1);
	mono_coop_mutex_init_recursive (synch_cs);

	if (InterlockedCompareExchangePointer ((gpointer *)&thread->synch_cs,
					       synch_cs, NULL) != NULL) {
		/* Another thread must have installed this CS */
		mono_coop_mutex_destroy (synch_cs);
		g_free (synch_cs);
	}
}

static inline void
lock_thread (MonoInternalThread *thread)
{
	if (!thread->synch_cs)
		ensure_synch_cs_set (thread);

	g_assert (thread->synch_cs);

	mono_coop_mutex_lock (thread->synch_cs);
}

static inline void
unlock_thread (MonoInternalThread *thread)
{
	mono_coop_mutex_unlock (thread->synch_cs);
}

static inline gboolean
is_appdomainunloaded_exception (MonoClass *klass)
{
	return klass == mono_class_get_appdomain_unloaded_exception_class ();
}

static inline gboolean
is_threadabort_exception (MonoClass *klass)
{
	return klass == mono_defaults.threadabortexception_class;
}

/*
 * A special static data offset (guint32) consists of 3 parts:
 *
 * [0]   6-bit index into the array of chunks.
 * [6]   25-bit offset into the array.
 * [31]  Bit indicating thread or context static.
 */

typedef union {
	struct {
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
		guint32 type : 1;
		guint32 offset : 25;
		guint32 index : 6;
#else
		guint32 index : 6;
		guint32 offset : 25;
		guint32 type : 1;
#endif
	} fields;
	guint32 raw;
} SpecialStaticOffset;

#define SPECIAL_STATIC_OFFSET_TYPE_THREAD 0
#define SPECIAL_STATIC_OFFSET_TYPE_CONTEXT 1

#define MAKE_SPECIAL_STATIC_OFFSET(index, offset, type) \
	((SpecialStaticOffset) { .fields = { (index), (offset), (type) } }.raw)
#define ACCESS_SPECIAL_STATIC_OFFSET(x,f) \
	(((SpecialStaticOffset *) &(x))->fields.f)

static gpointer
get_thread_static_data (MonoInternalThread *thread, guint32 offset)
{
	g_assert (ACCESS_SPECIAL_STATIC_OFFSET (offset, type) == SPECIAL_STATIC_OFFSET_TYPE_THREAD);

	int idx = ACCESS_SPECIAL_STATIC_OFFSET (offset, index);
	int off = ACCESS_SPECIAL_STATIC_OFFSET (offset, offset);

	return ((char *) thread->static_data [idx]) + off;
}

static gpointer
get_context_static_data (MonoAppContext *ctx, guint32 offset)
{
	g_assert (ACCESS_SPECIAL_STATIC_OFFSET (offset, type) == SPECIAL_STATIC_OFFSET_TYPE_CONTEXT);

	int idx = ACCESS_SPECIAL_STATIC_OFFSET (offset, index);
	int off = ACCESS_SPECIAL_STATIC_OFFSET (offset, offset);

	return ((char *) ctx->static_data [idx]) + off;
}

static MonoThread**
get_current_thread_ptr_for_domain (MonoDomain *domain, MonoInternalThread *thread)
{
	static MonoClassField *current_thread_field = NULL;

	guint32 offset;

	if (!current_thread_field) {
		current_thread_field = mono_class_get_field_from_name (mono_defaults.thread_class, "current_thread");
		g_assert (current_thread_field);
	}

	mono_class_vtable (domain, mono_defaults.thread_class);
	mono_domain_lock (domain);
	offset = GPOINTER_TO_UINT (g_hash_table_lookup (domain->special_static_fields, current_thread_field));
	mono_domain_unlock (domain);
	g_assert (offset);

	return (MonoThread **)get_thread_static_data (thread, offset);
}

static void
set_current_thread_for_domain (MonoDomain *domain, MonoInternalThread *thread, MonoThread *current)
{
	MonoThread **current_thread_ptr = get_current_thread_ptr_for_domain (domain, thread);

	g_assert (current->obj.vtable->domain == domain);

	g_assert (!*current_thread_ptr);
	*current_thread_ptr = current;
}

static MonoThread*
create_thread_object (MonoDomain *domain, MonoInternalThread *internal)
{
	MonoThread *thread;
	MonoVTable *vtable;
	MonoError error;

	vtable = mono_class_vtable (domain, mono_defaults.thread_class);
	g_assert (vtable);

	thread = (MonoThread*)mono_object_new_mature (vtable, &error);
	/* only possible failure mode is OOM, from which we don't expect to recover. */
	mono_error_assert_ok (&error);

	MONO_OBJECT_SETREF (thread, internal_thread, internal);

	return thread;
}

static MonoInternalThread*
create_internal_thread_object (void)
{
	MonoError error;
	MonoInternalThread *thread;
	MonoVTable *vt;

	vt = mono_class_vtable (mono_get_root_domain (), mono_defaults.internal_thread_class);
	thread = (MonoInternalThread*) mono_object_new_mature (vt, &error);
	/* only possible failure mode is OOM, from which we don't exect to recover */
	mono_error_assert_ok (&error);

	thread->synch_cs = g_new0 (MonoCoopMutex, 1);
	mono_coop_mutex_init_recursive (thread->synch_cs);

	thread->apartment_state = ThreadApartmentState_Unknown;
	thread->managed_id = get_next_managed_thread_id ();
	if (mono_gc_is_moving ()) {
		thread->thread_pinning_ref = thread;
		MONO_GC_REGISTER_ROOT_PINNING (thread->thread_pinning_ref, MONO_ROOT_SOURCE_THREADING, "thread pinning reference");
	}

	thread->priority = MONO_THREAD_PRIORITY_NORMAL;

	thread->suspended = g_new0 (MonoOSEvent, 1);
	mono_os_event_init (thread->suspended, TRUE);

	return thread;
}

static void
mono_thread_internal_set_priority (MonoInternalThread *internal, MonoThreadPriority priority)
{
	g_assert (internal);

	g_assert (priority >= MONO_THREAD_PRIORITY_LOWEST);
	g_assert (priority <= MONO_THREAD_PRIORITY_HIGHEST);
	g_assert (MONO_THREAD_PRIORITY_LOWEST < MONO_THREAD_PRIORITY_HIGHEST);

#ifdef HOST_WIN32
	BOOL res;

	g_assert (internal->native_handle);

	res = SetThreadPriority (internal->native_handle, priority - 2);
	if (!res)
		g_error ("%s: SetThreadPriority failed, error %d", __func__, GetLastError ());
#else /* HOST_WIN32 */
	pthread_t tid;
	int policy;
	struct sched_param param;
	gint res;

	tid = thread_get_tid (internal);

	res = pthread_getschedparam (tid, &policy, &param);
	if (res != 0)
		g_error ("%s: pthread_getschedparam failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);

#ifdef _POSIX_PRIORITY_SCHEDULING
	int max, min;

	/* Necessary to get valid priority range */

	min = sched_get_priority_min (policy);
	max = sched_get_priority_max (policy);

	if (max > 0 && min >= 0 && max > min) {
		double srange, drange, sposition, dposition;
		srange = MONO_THREAD_PRIORITY_HIGHEST - MONO_THREAD_PRIORITY_LOWEST;
		drange = max - min;
		sposition = priority - MONO_THREAD_PRIORITY_LOWEST;
		dposition = (sposition / srange) * drange;
		param.sched_priority = (int)(dposition + min);
	} else
#endif
	{
		switch (policy) {
		case SCHED_FIFO:
		case SCHED_RR:
			param.sched_priority = 50;
			break;
#ifdef SCHED_BATCH
		case SCHED_BATCH:
#endif
		case SCHED_OTHER:
			param.sched_priority = 0;
			break;
		default:
			g_warning ("%s: unknown policy %d", __func__, policy);
			return;
		}
	}

	res = pthread_setschedparam (tid, policy, &param);
	if (res != 0) {
		if (res == EPERM) {
			g_warning ("%s: pthread_setschedparam failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);
			return;
		}
		g_error ("%s: pthread_setschedparam failed, error: \"%s\" (%d)", __func__, g_strerror (res), res);
	}
#endif /* HOST_WIN32 */
}

static void 
mono_alloc_static_data (gpointer **static_data_ptr, guint32 offset, gboolean threadlocal);

static gboolean
mono_thread_attach_internal (MonoThread *thread, gboolean force_attach, gboolean force_domain)
{
	MonoThreadInfo *info;
	MonoInternalThread *internal;
	MonoDomain *domain, *root_domain;

	g_assert (thread);

	info = mono_thread_info_current ();
	g_assert (info);

	internal = thread->internal_thread;
	g_assert (internal);

	/* It is needed to store the MonoInternalThread on the MonoThreadInfo, because of the following case:
	 *  - the MonoInternalThread TLS key is destroyed: set it to NULL
	 *  - the MonoThreadInfo TLS key is destroyed: calls mono_thread_info_detach
	 *    - it calls MonoThreadInfoCallbacks.thread_detach
	 *      - mono_thread_internal_current returns NULL -> fails to detach the MonoInternalThread. */
	mono_thread_info_set_internal_thread_gchandle (info, mono_gchandle_new ((MonoObject*) internal, FALSE));

	internal->handle = mono_threads_open_thread_handle (info->handle);
#ifdef HOST_WIN32
	internal->native_handle = OpenThread (THREAD_ALL_ACCESS, FALSE, GetCurrentThreadId ());
#endif
	internal->tid = MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ());
	internal->thread_info = info;
	internal->small_id = info->small_id;

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Setting current_object_key to %p", __func__, mono_native_thread_id_get (), internal));

	SET_CURRENT_OBJECT (internal);

	domain = mono_object_domain (thread);

	mono_thread_push_appdomain_ref (domain);
	if (!mono_domain_set (domain, force_domain)) {
		mono_thread_pop_appdomain_ref ();
		return FALSE;
	}

	mono_threads_lock ();

	if (threads_starting_up)
		mono_g_hash_table_remove (threads_starting_up, thread);

	if (shutting_down && !force_attach) {
		mono_threads_unlock ();
		mono_thread_pop_appdomain_ref ();
		return FALSE;
	}

	if (!threads) {
		threads = mono_g_hash_table_new_type (NULL, NULL, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_THREADING, "threads table");
	}

	/* We don't need to duplicate thread->handle, because it is
	 * only closed when the thread object is finalized by the GC. */
	mono_g_hash_table_insert (threads, (gpointer)(gsize)(internal->tid), internal);

	/* We have to do this here because mono_thread_start_cb
	 * requires that root_domain_thread is set up. */
	if (thread_static_info.offset || thread_static_info.idx > 0) {
		/* get the current allocated size */
		guint32 offset = MAKE_SPECIAL_STATIC_OFFSET (thread_static_info.idx, thread_static_info.offset, 0);
		mono_alloc_static_data (&internal->static_data, offset, TRUE);
	}

	mono_threads_unlock ();

	root_domain = mono_get_root_domain ();

	g_assert (!internal->root_domain_thread);
	if (domain != root_domain)
		MONO_OBJECT_SETREF (internal, root_domain_thread, create_thread_object (root_domain, internal));
	else
		MONO_OBJECT_SETREF (internal, root_domain_thread, thread);

	if (domain != root_domain)
		set_current_thread_for_domain (root_domain, internal, internal->root_domain_thread);

	set_current_thread_for_domain (domain, internal, thread);

	THREAD_DEBUG (g_message ("%s: Attached thread ID %"G_GSIZE_FORMAT" (handle %p)", __func__, internal->tid, internal->handle));

	return TRUE;
}

typedef struct {
	gint32 ref;
	MonoThread *thread;
	MonoObject *start_delegate;
	MonoObject *start_delegate_arg;
	MonoThreadStart start_func;
	gpointer start_func_arg;
	gboolean force_attach;
	gboolean failed;
	MonoCoopSem registered;
} StartInfo;

static guint32 WINAPI start_wrapper_internal(StartInfo *start_info, gsize *stack_ptr)
{
	MonoError error;
	MonoThreadStart start_func;
	void *start_func_arg;
	gsize tid;
	/* 
	 * We don't create a local to hold start_info->thread, so hopefully it won't get pinned during a
	 * GC stack walk.
	 */
	MonoThread *thread;
	MonoInternalThread *internal;
	MonoObject *start_delegate;
	MonoObject *start_delegate_arg;
	MonoDomain *domain;

	thread = start_info->thread;
	internal = thread->internal_thread;
	domain = mono_object_domain (start_info->thread);

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Start wrapper", __func__, mono_native_thread_id_get ()));

	if (!mono_thread_attach_internal (thread, start_info->force_attach, FALSE)) {
		start_info->failed = TRUE;

		mono_coop_sem_post (&start_info->registered);

		if (InterlockedDecrement (&start_info->ref) == 0) {
			mono_coop_sem_destroy (&start_info->registered);
			g_free (start_info);
		}

		return 0;
	}

	mono_thread_internal_set_priority (internal, internal->priority);

	tid = internal->tid;

	start_delegate = start_info->start_delegate;
	start_delegate_arg = start_info->start_delegate_arg;
	start_func = start_info->start_func;
	start_func_arg = start_info->start_func_arg;

	/* This MUST be called before any managed code can be
	 * executed, as it calls the callback function that (for the
	 * jit) sets the lmf marker.
	 */

	if (mono_thread_start_cb)
		mono_thread_start_cb (tid, stack_ptr, start_func);

	/* On 2.0 profile (and higher), set explicitly since state might have been
	   Unknown */
	if (internal->apartment_state == ThreadApartmentState_Unknown)
		internal->apartment_state = ThreadApartmentState_MTA;

	mono_thread_init_apartment_state ();

	/* Let the thread that called Start() know we're ready */
	mono_coop_sem_post (&start_info->registered);

	if (InterlockedDecrement (&start_info->ref) == 0) {
		mono_coop_sem_destroy (&start_info->registered);
		g_free (start_info);
	}

	/* start_info is not valid anymore */
	start_info = NULL;

	/* 
	 * Call this after calling start_notify, since the profiler callback might want
	 * to lock the thread, and the lock is held by thread_start () which waits for
	 * start_notify.
	 */
	MONO_PROFILER_RAISE (thread_started, (tid));

	/* if the name was set before starting, we didn't invoke the profiler callback */
	if (internal->name) {
		char *tname = g_utf16_to_utf8 (internal->name, internal->name_len, NULL, NULL, NULL);
		MONO_PROFILER_RAISE (thread_name, (internal->tid, tname));
		mono_native_thread_set_name (MONO_UINT_TO_NATIVE_THREAD_ID (internal->tid), tname);
		g_free (tname);
	}

	/* start_func is set only for unmanaged start functions */
	if (start_func) {
		start_func (start_func_arg);
	} else {
		void *args [1];

		g_assert (start_delegate != NULL);

		/* we may want to handle the exception here. See comment below on unhandled exceptions */
		args [0] = (gpointer) start_delegate_arg;
		mono_runtime_delegate_invoke_checked (start_delegate, args, &error);

		if (!mono_error_ok (&error)) {
			MonoException *ex = mono_error_convert_to_exception (&error);

			g_assert (ex != NULL);
			MonoClass *klass = mono_object_get_class (&ex->object);
			if ((mono_runtime_unhandled_exception_policy_get () != MONO_UNHANDLED_POLICY_LEGACY) &&
			    !is_threadabort_exception (klass)) {
				mono_unhandled_exception (&ex->object);
				mono_invoke_unhandled_exception_hook (&ex->object);
				g_assert_not_reached ();
			}
		} else {
			mono_error_cleanup (&error);
		}
	}

	/* If the thread calls ExitThread at all, this remaining code
	 * will not be executed, but the main thread will eventually
	 * call mono_thread_detach_internal() on this thread's behalf.
	 */

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Start wrapper terminating", __func__, mono_native_thread_id_get ()));

	/* Do any cleanup needed for apartment state. This
	 * cannot be done in mono_thread_detach_internal since
	 * mono_thread_detach_internal could be  called for a
	 * thread other than the current thread.
	 * mono_thread_cleanup_apartment_state cleans up apartment
	 * for the current thead */
	mono_thread_cleanup_apartment_state ();

	mono_thread_detach_internal (internal);

	internal->tid = 0;

	return(0);
}

static gsize WINAPI
start_wrapper (gpointer data)
{
	StartInfo *start_info;
	MonoThreadInfo *info;
	gsize res;

	start_info = (StartInfo*) data;
	g_assert (start_info);

	info = mono_thread_info_attach ();
	info->runtime_thread = TRUE;

	/* Run the actual main function of the thread */
	res = start_wrapper_internal (start_info, info->stack_end);

	mono_thread_info_exit (res);

	g_assert_not_reached ();
}

/*
 * create_thread:
 *
 *   Common thread creation code.
 * LOCKING: Acquires the threads lock.
 */
static gboolean
create_thread (MonoThread *thread, MonoInternalThread *internal, MonoObject *start_delegate, MonoThreadStart start_func, gpointer start_func_arg,
	MonoThreadCreateFlags flags, MonoError *error)
{
	StartInfo *start_info = NULL;
	MonoNativeThreadId tid;
	gboolean ret;
	gsize stack_set_size;

	if (start_delegate)
		g_assert (!start_func && !start_func_arg);
	if (start_func)
		g_assert (!start_delegate);

	if (flags & MONO_THREAD_CREATE_FLAGS_THREADPOOL) {
		g_assert (!(flags & MONO_THREAD_CREATE_FLAGS_DEBUGGER));
		g_assert (!(flags & MONO_THREAD_CREATE_FLAGS_FORCE_CREATE));
	}
	if (flags & MONO_THREAD_CREATE_FLAGS_DEBUGGER) {
		g_assert (!(flags & MONO_THREAD_CREATE_FLAGS_THREADPOOL));
		g_assert (!(flags & MONO_THREAD_CREATE_FLAGS_FORCE_CREATE));
	}

	/*
	 * Join joinable threads to prevent running out of threads since the finalizer
	 * thread might be blocked/backlogged.
	 */
	mono_threads_join_threads ();

	error_init (error);

	mono_threads_lock ();
	if (shutting_down && !(flags & MONO_THREAD_CREATE_FLAGS_FORCE_CREATE)) {
		mono_threads_unlock ();
		return FALSE;
	}
	if (threads_starting_up == NULL) {
		threads_starting_up = mono_g_hash_table_new_type (NULL, NULL, MONO_HASH_KEY_VALUE_GC, MONO_ROOT_SOURCE_THREADING, "starting threads table");
	}
	mono_g_hash_table_insert (threads_starting_up, thread, thread);
	mono_threads_unlock ();

	internal->threadpool_thread = flags & MONO_THREAD_CREATE_FLAGS_THREADPOOL;
	if (internal->threadpool_thread)
		mono_thread_set_state (internal, ThreadState_Background);

	internal->debugger_thread = flags & MONO_THREAD_CREATE_FLAGS_DEBUGGER;

	start_info = g_new0 (StartInfo, 1);
	start_info->ref = 2;
	start_info->thread = thread;
	start_info->start_delegate = start_delegate;
	start_info->start_delegate_arg = thread->start_obj;
	start_info->start_func = start_func;
	start_info->start_func_arg = start_func_arg;
	start_info->force_attach = flags & MONO_THREAD_CREATE_FLAGS_FORCE_CREATE;
	start_info->failed = FALSE;
	mono_coop_sem_init (&start_info->registered, 0);

	if (flags != MONO_THREAD_CREATE_FLAGS_SMALL_STACK)
		stack_set_size = default_stacksize_for_thread (internal);
	else
		stack_set_size = 0;

	if (!mono_thread_platform_create_thread (start_wrapper, start_info, &stack_set_size, &tid)) {
		/* The thread couldn't be created, so set an exception */
		mono_threads_lock ();
		mono_g_hash_table_remove (threads_starting_up, thread);
		mono_threads_unlock ();
		mono_error_set_execution_engine (error, "Couldn't create thread. Error 0x%x", mono_w32error_get_last());
		/* ref is not going to be decremented in start_wrapper_internal */
		InterlockedDecrement (&start_info->ref);
		ret = FALSE;
		goto done;
	}

	internal->stack_size = (int) stack_set_size;

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Launching thread %p (%"G_GSIZE_FORMAT")", __func__, mono_native_thread_id_get (), internal, (gsize)internal->tid));

	/*
	 * Wait for the thread to set up its TLS data etc, so
	 * theres no potential race condition if someone tries
	 * to look up the data believing the thread has
	 * started
	 */

	mono_coop_sem_wait (&start_info->registered, MONO_SEM_FLAGS_NONE);

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Done launching thread %p (%"G_GSIZE_FORMAT")", __func__, mono_native_thread_id_get (), internal, (gsize)internal->tid));

	ret = !start_info->failed;

done:
	if (InterlockedDecrement (&start_info->ref) == 0) {
		mono_coop_sem_destroy (&start_info->registered);
		g_free (start_info);
	}

	return ret;
}

/**
 * mono_thread_new_init:
 */
void
mono_thread_new_init (intptr_t tid, gpointer stack_start, gpointer func)
{
	if (mono_thread_start_cb) {
		mono_thread_start_cb (tid, stack_start, func);
	}
}

/**
 * mono_threads_set_default_stacksize:
 */
void
mono_threads_set_default_stacksize (guint32 stacksize)
{
	default_stacksize = stacksize;
}

/**
 * mono_threads_get_default_stacksize:
 */
guint32
mono_threads_get_default_stacksize (void)
{
	return default_stacksize;
}

/*
 * mono_thread_create_internal:
 *
 *   ARG should not be a GC reference.
 */
MonoInternalThread*
mono_thread_create_internal (MonoDomain *domain, gpointer func, gpointer arg, MonoThreadCreateFlags flags, MonoError *error)
{
	MonoThread *thread;
	MonoInternalThread *internal;
	gboolean res;

	error_init (error);

	internal = create_internal_thread_object ();

	thread = create_thread_object (domain, internal);

	LOCK_THREAD (internal);

	res = create_thread (thread, internal, NULL, (MonoThreadStart) func, arg, flags, error);

	UNLOCK_THREAD (internal);

	return_val_if_nok (error, NULL);
	return internal;
}

/**
 * mono_thread_create:
 */
void
mono_thread_create (MonoDomain *domain, gpointer func, gpointer arg)
{
	MonoError error;
	if (!mono_thread_create_checked (domain, func, arg, &error))
		mono_error_cleanup (&error);
}

gboolean
mono_thread_create_checked (MonoDomain *domain, gpointer func, gpointer arg, MonoError *error)
{
	return (NULL != mono_thread_create_internal (domain, func, arg, MONO_THREAD_CREATE_FLAGS_NONE, error));
}

static MonoThread *
mono_thread_attach_full (MonoDomain *domain, gboolean force_attach)
{
	MonoInternalThread *internal;
	MonoThread *thread;
	MonoThreadInfo *info;
	MonoNativeThreadId tid;

	if (mono_thread_internal_current_is_attached ()) {
		if (domain != mono_domain_get ())
			mono_domain_set (domain, TRUE);
		/* Already attached */
		return mono_thread_current ();
	}

	info = mono_thread_info_attach ();
	g_assert (info);

	tid=mono_native_thread_id_get ();

	internal = create_internal_thread_object ();

	thread = create_thread_object (domain, internal);

	if (!mono_thread_attach_internal (thread, force_attach, TRUE)) {
		/* Mono is shutting down, so just wait for the end */
		for (;;)
			mono_thread_info_sleep (10000, NULL);
	}

	THREAD_DEBUG (g_message ("%s: Attached thread ID %"G_GSIZE_FORMAT" (handle %p)", __func__, tid, internal->handle));

	if (mono_thread_attach_cb)
		mono_thread_attach_cb (MONO_NATIVE_THREAD_ID_TO_UINT (tid), info->stack_end);

	/* Can happen when we attach the profiler helper thread in order to heapshot. */
	if (!mono_thread_info_current ()->tools_thread)
		MONO_PROFILER_RAISE (thread_started, (MONO_NATIVE_THREAD_ID_TO_UINT (tid)));

	return thread;
}

/**
 * mono_thread_attach:
 */
MonoThread *
mono_thread_attach (MonoDomain *domain)
{
	return mono_thread_attach_full (domain, FALSE);
}

void
mono_thread_detach_internal (MonoInternalThread *thread)
{
	gboolean removed;

	g_assert (thread != NULL);
	SET_CURRENT_OBJECT (thread);

	THREAD_DEBUG (g_message ("%s: mono_thread_detach for %p (%"G_GSIZE_FORMAT")", __func__, thread, (gsize)thread->tid));

#ifndef HOST_WIN32
	mono_w32mutex_abandon ();
#endif

	if (thread->abort_state_handle) {
		mono_gchandle_free (thread->abort_state_handle);
		thread->abort_state_handle = 0;
	}

	thread->abort_exc = NULL;
	thread->current_appcontext = NULL;

	/*
	 * thread->synch_cs can be NULL if this was called after
	 * ves_icall_System_Threading_InternalThread_Thread_free_internal.
	 * This can happen only during shutdown.
	 * The shutting_down flag is not always set, so we can't assert on it.
	 */
	if (thread->synch_cs)
		LOCK_THREAD (thread);

	thread->state |= ThreadState_Stopped;
	thread->state &= ~ThreadState_Background;

	if (thread->synch_cs)
		UNLOCK_THREAD (thread);

	/*
	An interruption request has leaked to cleanup. Adjust the global counter.

	This can happen is the abort source thread finds the abortee (this) thread
	in unmanaged code. If this thread never trips back to managed code or check
	the local flag it will be left set and positively unbalance the global counter.
	
	Leaving the counter unbalanced will cause a performance degradation since all threads
	will now keep checking their local flags all the time.
	*/
	mono_thread_clear_interruption_requested (thread);

	mono_threads_lock ();

	if (!threads) {
		removed = FALSE;
	} else if (mono_g_hash_table_lookup (threads, (gpointer)thread->tid) != thread) {
		/* We have to check whether the thread object for the
		 * tid is still the same in the table because the
		 * thread might have been destroyed and the tid reused
		 * in the meantime, in which case the tid would be in
		 * the table, but with another thread object.
		 */
		removed = FALSE;
	} else {
		mono_g_hash_table_remove (threads, (gpointer)thread->tid);
		removed = TRUE;
	}

	mono_threads_unlock ();

	/* Don't close the handle here, wait for the object finalizer
	 * to do it. Otherwise, the following race condition applies:
	 *
	 * 1) Thread exits (and mono_thread_detach_internal() closes the handle)
	 *
	 * 2) Some other handle is reassigned the same slot
	 *
	 * 3) Another thread tries to join the first thread, and
	 * blocks waiting for the reassigned handle to be signalled
	 * (which might never happen).  This is possible, because the
	 * thread calling Join() still has a reference to the first
	 * thread's object.
	 */

	/* if the thread is not in the hash it has been removed already */
	if (!removed) {
		mono_domain_unset ();
		mono_memory_barrier ();

		if (mono_thread_cleanup_fn)
			mono_thread_cleanup_fn (thread_get_tid (thread));

		goto done;
	}

	mono_release_type_locks (thread);

	/* Can happen when we attach the profiler helper thread in order to heapshot. */
	if (!mono_thread_info_lookup (MONO_UINT_TO_NATIVE_THREAD_ID (thread->tid))->tools_thread)
		MONO_PROFILER_RAISE (thread_stopped, (thread->tid));

	mono_hazard_pointer_clear (mono_hazard_pointer_get (), 1);

	/*
	 * This will signal async signal handlers that the thread has exited.
	 * The profiler callback needs this to be set, so it cannot be done earlier.
	 */
	mono_domain_unset ();
	mono_memory_barrier ();

	if (thread == mono_thread_internal_current ())
		mono_thread_pop_appdomain_ref ();

	mono_free_static_data (thread->static_data);
	thread->static_data = NULL;
	ref_stack_destroy (thread->appdomain_refs);
	thread->appdomain_refs = NULL;

	g_assert (thread->suspended);
	mono_os_event_destroy (thread->suspended);
	g_free (thread->suspended);
	thread->suspended = NULL;

	if (mono_thread_cleanup_fn)
		mono_thread_cleanup_fn (thread_get_tid (thread));

	mono_memory_barrier ();

	if (mono_gc_is_moving ()) {
		MONO_GC_UNREGISTER_ROOT (thread->thread_pinning_ref);
		thread->thread_pinning_ref = NULL;
	}

done:
	SET_CURRENT_OBJECT (NULL);
	mono_domain_unset ();

	mono_thread_info_unset_internal_thread_gchandle ((MonoThreadInfo*) thread->thread_info);

	/* Don't need to close the handle to this thread, even though we took a
	 * reference in mono_thread_attach (), because the GC will do it
	 * when the Thread object is finalised.
	 */
}

/**
 * mono_thread_detach:
 */
void
mono_thread_detach (MonoThread *thread)
{
	if (thread)
		mono_thread_detach_internal (thread->internal_thread);
}

/**
 * mono_thread_detach_if_exiting:
 *
 * Detach the current thread from the runtime if it is exiting, i.e. it is running pthread dtors.
 * This should be used at the end of embedding code which calls into managed code, and which
 * can be called from pthread dtors, like <code>dealloc:</code> implementations in Objective-C.
 */
mono_bool
mono_thread_detach_if_exiting (void)
{
	if (mono_thread_info_is_exiting ()) {
		MonoInternalThread *thread;

		thread = mono_thread_internal_current ();
		if (thread) {
			mono_thread_detach_internal (thread);
			mono_thread_info_detach ();
			return TRUE;
		}
	}
	return FALSE;
}

gboolean
mono_thread_internal_current_is_attached (void)
{
	MonoInternalThread *internal;

	internal = GET_CURRENT_OBJECT ();
	if (!internal)
		return FALSE;

	return TRUE;
}

/**
 * mono_thread_exit:
 */
void
mono_thread_exit (void)
{
	MonoInternalThread *thread = mono_thread_internal_current ();

	THREAD_DEBUG (g_message ("%s: mono_thread_exit for %p (%"G_GSIZE_FORMAT")", __func__, thread, (gsize)thread->tid));

	mono_thread_detach_internal (thread);

	/* we could add a callback here for embedders to use. */
	if (mono_thread_get_main () && (thread == mono_thread_get_main ()->internal_thread))
		exit (mono_environment_exitcode_get ());

	mono_thread_info_exit (0);
}

void
ves_icall_System_Threading_Thread_ConstructInternalThread (MonoThread *this_obj)
{
	MonoInternalThread *internal;

	internal = create_internal_thread_object ();

	internal->state = ThreadState_Unstarted;

	InterlockedCompareExchangePointer ((volatile gpointer *)&this_obj->internal_thread, internal, NULL);
}

MonoThread *
ves_icall_System_Threading_Thread_GetCurrentThread (void)
{
	return mono_thread_current ();
}

HANDLE
ves_icall_System_Threading_Thread_Thread_internal (MonoThread *this_obj,
												   MonoObject *start)
{
	MonoError error;
	MonoInternalThread *internal;
	gboolean res;

	THREAD_DEBUG (g_message("%s: Trying to start a new thread: this (%p) start (%p)", __func__, this_obj, start));

	if (!this_obj->internal_thread)
		ves_icall_System_Threading_Thread_ConstructInternalThread (this_obj);
	internal = this_obj->internal_thread;

	LOCK_THREAD (internal);

	if ((internal->state & ThreadState_Unstarted) == 0) {
		UNLOCK_THREAD (internal);
		mono_set_pending_exception (mono_get_exception_thread_state ("Thread has already been started."));
		return NULL;
	}

	if ((internal->state & ThreadState_Aborted) != 0) {
		UNLOCK_THREAD (internal);
		return this_obj;
	}

	res = create_thread (this_obj, internal, start, NULL, NULL, MONO_THREAD_CREATE_FLAGS_NONE, &error);
	if (!res) {
		mono_error_cleanup (&error);
		UNLOCK_THREAD (internal);
		return NULL;
	}

	internal->state &= ~ThreadState_Unstarted;

	THREAD_DEBUG (g_message ("%s: Started thread ID %"G_GSIZE_FORMAT" (handle %p)", __func__, tid, thread));

	UNLOCK_THREAD (internal);
	return internal->handle;
}

/*
 * This is called from the finalizer of the internal thread object.
 */
void
ves_icall_System_Threading_InternalThread_Thread_free_internal (MonoInternalThread *this_obj)
{
	THREAD_DEBUG (g_message ("%s: Closing thread %p, handle %p", __func__, this, this_obj->handle));

	/*
	 * Since threads keep a reference to their thread object while running, by
	 * the time this function is called, the thread has already exited/detached,
	 * i.e. mono_thread_detach_internal () has ran. The exception is during
	 * shutdown, when mono_thread_detach_internal () can be called after this.
	 */
	if (this_obj->handle) {
		mono_threads_close_thread_handle (this_obj->handle);
		this_obj->handle = NULL;
	}

#if HOST_WIN32
	CloseHandle (this_obj->native_handle);
#endif

	if (this_obj->synch_cs) {
		MonoCoopMutex *synch_cs = this_obj->synch_cs;
		this_obj->synch_cs = NULL;
		mono_coop_mutex_destroy (synch_cs);
		g_free (synch_cs);
	}

	if (this_obj->name) {
		void *name = this_obj->name;
		this_obj->name = NULL;
		g_free (name);
	}
}

void
ves_icall_System_Threading_Thread_Sleep_internal(gint32 ms)
{
	guint32 res;
	MonoInternalThread *thread = mono_thread_internal_current ();

	THREAD_DEBUG (g_message ("%s: Sleeping for %d ms", __func__, ms));

	if (mono_thread_current_check_pending_interrupt ())
		return;

	while (TRUE) {
		gboolean alerted = FALSE;

		mono_thread_set_state (thread, ThreadState_WaitSleepJoin);

		res = mono_thread_info_sleep (ms, &alerted);

		mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);

		if (alerted) {
			MonoException* exc = mono_thread_execute_interruption ();
			if (exc) {
				mono_raise_exception (exc);
			} else {
				// FIXME: !MONO_INFINITE_WAIT
				if (ms != MONO_INFINITE_WAIT)
					break;
			}
		} else {
			break;
		}
	}
}

void ves_icall_System_Threading_Thread_SpinWait_nop (void)
{
}

gint32
ves_icall_System_Threading_Thread_GetDomainID (void) 
{
	return mono_domain_get()->domain_id;
}

gboolean 
ves_icall_System_Threading_Thread_Yield (void)
{
	return mono_thread_info_yield ();
}

/*
 * mono_thread_get_name:
 *
 *   Return the name of the thread. NAME_LEN is set to the length of the name.
 * Return NULL if the thread has no name. The returned memory is owned by the
 * caller.
 */
gunichar2*
mono_thread_get_name (MonoInternalThread *this_obj, guint32 *name_len)
{
	gunichar2 *res;

	LOCK_THREAD (this_obj);
	
	if (!this_obj->name) {
		*name_len = 0;
		res = NULL;
	} else {
		*name_len = this_obj->name_len;
		res = g_new (gunichar2, this_obj->name_len);
		memcpy (res, this_obj->name, sizeof (gunichar2) * this_obj->name_len);
	}
	
	UNLOCK_THREAD (this_obj);

	return res;
}

/**
 * mono_thread_get_name_utf8:
 * \returns the name of the thread in UTF-8.
 * Return NULL if the thread has no name.
 * The returned memory is owned by the caller.
 */
char *
mono_thread_get_name_utf8 (MonoThread *thread)
{
	if (thread == NULL)
		return NULL;

	MonoInternalThread *internal = thread->internal_thread;
	if (internal == NULL)
		return NULL;

	LOCK_THREAD (internal);

	char *tname = g_utf16_to_utf8 (internal->name, internal->name_len, NULL, NULL, NULL);

	UNLOCK_THREAD (internal);

	return tname;
}

/**
 * mono_thread_get_managed_id:
 * \returns the \c Thread.ManagedThreadId value of \p thread.
 * Returns \c -1 if \p thread is NULL.
 */
int32_t
mono_thread_get_managed_id (MonoThread *thread)
{
	if (thread == NULL)
		return -1;

	MonoInternalThread *internal = thread->internal_thread;
	if (internal == NULL)
		return -1;

	int32_t id = internal->managed_id;

	return id;
}

MonoString* 
ves_icall_System_Threading_Thread_GetName_internal (MonoInternalThread *this_obj)
{
	MonoError error;
	MonoString* str;

	error_init (&error);

	LOCK_THREAD (this_obj);
	
	if (!this_obj->name)
		str = NULL;
	else
		str = mono_string_new_utf16_checked (mono_domain_get (), this_obj->name, this_obj->name_len, &error);
	
	UNLOCK_THREAD (this_obj);

	if (mono_error_set_pending_exception (&error))
		return NULL;
	
	return str;
}

void 
mono_thread_set_name_internal (MonoInternalThread *this_obj, MonoString *name, gboolean permanent, gboolean reset, MonoError *error)
{
	LOCK_THREAD (this_obj);

	error_init (error);

	if (reset) {
		this_obj->flags &= ~MONO_THREAD_FLAG_NAME_SET;
	} else if (this_obj->flags & MONO_THREAD_FLAG_NAME_SET) {
		UNLOCK_THREAD (this_obj);
		
		mono_error_set_invalid_operation (error, "Thread.Name can only be set once.");
		return;
	}
	if (this_obj->name) {
		g_free (this_obj->name);
		this_obj->name_len = 0;
	}
	if (name) {
		this_obj->name = g_memdup (mono_string_chars (name), mono_string_length (name) * sizeof (gunichar2));
		this_obj->name_len = mono_string_length (name);

		if (permanent)
			this_obj->flags |= MONO_THREAD_FLAG_NAME_SET;
	}
	else
		this_obj->name = NULL;

	
	UNLOCK_THREAD (this_obj);

	if (this_obj->name && this_obj->tid) {
		char *tname = mono_string_to_utf8_checked (name, error);
		return_if_nok (error);
		MONO_PROFILER_RAISE (thread_name, (this_obj->tid, tname));
		mono_native_thread_set_name (thread_get_tid (this_obj), tname);
		mono_free (tname);
	}
}

void 
ves_icall_System_Threading_Thread_SetName_internal (MonoInternalThread *this_obj, MonoString *name)
{
	MonoError error;
	mono_thread_set_name_internal (this_obj, name, TRUE, FALSE, &error);
	mono_error_set_pending_exception (&error);
}

/*
 * ves_icall_System_Threading_Thread_GetPriority_internal:
 * @param this_obj: The MonoInternalThread on which to operate.
 *
 * Gets the priority of the given thread.
 * @return: The priority of the given thread.
 */
int
ves_icall_System_Threading_Thread_GetPriority (MonoThread *this_obj)
{
	gint32 priority;
	MonoInternalThread *internal = this_obj->internal_thread;

	LOCK_THREAD (internal);
	priority = internal->priority;
	UNLOCK_THREAD (internal);

	return priority;
}

/* 
 * ves_icall_System_Threading_Thread_SetPriority_internal:
 * @param this_obj: The MonoInternalThread on which to operate.
 * @param priority: The priority to set.
 *
 * Sets the priority of the given thread.
 */
void
ves_icall_System_Threading_Thread_SetPriority (MonoThread *this_obj, int priority)
{
	MonoInternalThread *internal = this_obj->internal_thread;

	LOCK_THREAD (internal);
	internal->priority = priority;
	if (internal->thread_info != NULL)
		mono_thread_internal_set_priority (internal, priority);
	UNLOCK_THREAD (internal);
}

/* If the array is already in the requested domain, we just return it,
   otherwise we return a copy in that domain. */
static MonoArray*
byte_array_to_domain (MonoArray *arr, MonoDomain *domain, MonoError *error)
{
	MonoArray *copy;

	error_init (error);
	if (!arr)
		return NULL;

	if (mono_object_domain (arr) == domain)
		return arr;

	copy = mono_array_new_checked (domain, mono_defaults.byte_class, arr->max_length, error);
	memmove (mono_array_addr (copy, guint8, 0), mono_array_addr (arr, guint8, 0), arr->max_length);
	return copy;
}

MonoArray*
ves_icall_System_Threading_Thread_ByteArrayToRootDomain (MonoArray *arr)
{
	MonoError error;
	MonoArray *result = byte_array_to_domain (arr, mono_get_root_domain (), &error);
	mono_error_set_pending_exception (&error);
	return result;
}

MonoArray*
ves_icall_System_Threading_Thread_ByteArrayToCurrentDomain (MonoArray *arr)
{
	MonoError error;
	MonoArray *result = byte_array_to_domain (arr, mono_domain_get (), &error);
	mono_error_set_pending_exception (&error);
	return result;
}

/**
 * mono_thread_current:
 */
MonoThread *
mono_thread_current (void)
{
	MonoDomain *domain = mono_domain_get ();
	MonoInternalThread *internal = mono_thread_internal_current ();
	MonoThread **current_thread_ptr;

	g_assert (internal);
	current_thread_ptr = get_current_thread_ptr_for_domain (domain, internal);

	if (!*current_thread_ptr) {
		g_assert (domain != mono_get_root_domain ());
		*current_thread_ptr = create_thread_object (domain, internal);
	}
	return *current_thread_ptr;
}

/* Return the thread object belonging to INTERNAL in the current domain */
static MonoThread *
mono_thread_current_for_thread (MonoInternalThread *internal)
{
	MonoDomain *domain = mono_domain_get ();
	MonoThread **current_thread_ptr;

	g_assert (internal);
	current_thread_ptr = get_current_thread_ptr_for_domain (domain, internal);

	if (!*current_thread_ptr) {
		g_assert (domain != mono_get_root_domain ());
		*current_thread_ptr = create_thread_object (domain, internal);
	}
	return *current_thread_ptr;
}

MonoInternalThread*
mono_thread_internal_current (void)
{
	MonoInternalThread *res = GET_CURRENT_OBJECT ();
	THREAD_DEBUG (g_message ("%s: returning %p", __func__, res));
	return res;
}

static MonoThreadInfoWaitRet
mono_join_uninterrupted (MonoThreadHandle* thread_to_join, gint32 ms, MonoError *error)
{
	MonoException *exc;
	MonoThreadInfoWaitRet ret;
	gint64 start;
	gint32 diff_ms;
	gint32 wait = ms;

	error_init (error);

	start = (ms == -1) ? 0 : mono_msec_ticks ();
	for (;;) {
		MONO_ENTER_GC_SAFE;
		ret = mono_thread_info_wait_one_handle (thread_to_join, ms, TRUE);
		MONO_EXIT_GC_SAFE;

		if (ret != MONO_THREAD_INFO_WAIT_RET_ALERTED)
			return ret;

		exc = mono_thread_execute_interruption ();
		if (exc) {
			mono_error_set_exception_instance (error, exc);
			return ret;
		}

		if (ms == -1)
			continue;

		/* Re-calculate ms according to the time passed */
		diff_ms = (gint32)(mono_msec_ticks () - start);
		if (diff_ms >= ms) {
			ret = MONO_THREAD_INFO_WAIT_RET_TIMEOUT;
			return ret;
		}
		wait = ms - diff_ms;
	}

	return ret;
}

gboolean
ves_icall_System_Threading_Thread_Join_internal(MonoThread *this_obj, int ms)
{
	MonoInternalThread *thread = this_obj->internal_thread;
	MonoThreadHandle *handle = thread->handle;
	MonoInternalThread *cur_thread = mono_thread_internal_current ();
	gboolean ret;
	MonoError error;

	if (mono_thread_current_check_pending_interrupt ())
		return FALSE;

	LOCK_THREAD (thread);
	
	if ((thread->state & ThreadState_Unstarted) != 0) {
		UNLOCK_THREAD (thread);
		
		mono_set_pending_exception (mono_get_exception_thread_state ("Thread has not been started."));
		return FALSE;
	}

	UNLOCK_THREAD (thread);

	if(ms== -1) {
		ms=MONO_INFINITE_WAIT;
	}
	THREAD_DEBUG (g_message ("%s: joining thread handle %p, %d ms", __func__, handle, ms));
	
	mono_thread_set_state (cur_thread, ThreadState_WaitSleepJoin);

	ret=mono_join_uninterrupted (handle, ms, &error);

	mono_thread_clr_state (cur_thread, ThreadState_WaitSleepJoin);

	mono_error_set_pending_exception (&error);

	if(ret==MONO_THREAD_INFO_WAIT_RET_SUCCESS_0) {
		THREAD_DEBUG (g_message ("%s: join successful", __func__));

		return(TRUE);
	}
	
	THREAD_DEBUG (g_message ("%s: join failed", __func__));

	return(FALSE);
}

#define MANAGED_WAIT_FAILED 0x7fffffff

static gint32
map_native_wait_result_to_managed (MonoW32HandleWaitRet val, gsize numobjects)
{
	if (val >= MONO_W32HANDLE_WAIT_RET_SUCCESS_0 && val < MONO_W32HANDLE_WAIT_RET_SUCCESS_0 + numobjects) {
		return WAIT_OBJECT_0 + (val - MONO_W32HANDLE_WAIT_RET_SUCCESS_0);
	} else if (val >= MONO_W32HANDLE_WAIT_RET_ABANDONED_0 && val < MONO_W32HANDLE_WAIT_RET_ABANDONED_0 + numobjects) {
		return WAIT_ABANDONED_0 + (val - MONO_W32HANDLE_WAIT_RET_ABANDONED_0);
	} else if (val == MONO_W32HANDLE_WAIT_RET_ALERTED) {
		return WAIT_IO_COMPLETION;
	} else if (val == MONO_W32HANDLE_WAIT_RET_TIMEOUT) {
		return WAIT_TIMEOUT;
	} else if (val == MONO_W32HANDLE_WAIT_RET_FAILED) {
		/* WAIT_FAILED in waithandle.cs is different from WAIT_FAILED in Win32 API */
		return MANAGED_WAIT_FAILED;
	} else {
		g_error ("%s: unknown val value %d", __func__, val);
	}
}

gint32
ves_icall_System_Threading_WaitHandle_Wait_internal (gpointer *handles, gint32 numhandles, MonoBoolean waitall, gint32 timeout, MonoError *error)
{
	MonoW32HandleWaitRet ret;
	MonoInternalThread *thread;
	MonoException *exc;
	gint64 start;
	guint32 timeoutLeft;

	/* Do this WaitSleepJoin check before creating objects */
	if (mono_thread_current_check_pending_interrupt ())
		return map_native_wait_result_to_managed (MONO_W32HANDLE_WAIT_RET_FAILED, 0);

	thread = mono_thread_internal_current ();

	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);

	if (timeout == -1)
		timeout = MONO_INFINITE_WAIT;
	if (timeout != MONO_INFINITE_WAIT)
		start = mono_msec_ticks ();

	timeoutLeft = timeout;

	for (;;) {
		MONO_ENTER_GC_SAFE;
#ifdef HOST_WIN32
		if (numhandles != 1)
			ret = mono_w32handle_convert_wait_ret (WaitForMultipleObjectsEx (numhandles, handles, waitall, timeoutLeft, TRUE), numhandles);
		else
			ret = mono_w32handle_convert_wait_ret (WaitForSingleObjectEx (handles [0], timeoutLeft, TRUE), 1);
#else
		/* mono_w32handle_wait_multiple optimizes the case for numhandles == 1 */
		ret = mono_w32handle_wait_multiple (handles, numhandles, waitall, timeoutLeft, TRUE);
#endif /* HOST_WIN32 */
		MONO_EXIT_GC_SAFE;

		if (ret != MONO_W32HANDLE_WAIT_RET_ALERTED)
			break;

		exc = mono_thread_execute_interruption ();
		if (exc) {
			mono_error_set_exception_instance (error, exc);
			break;
		}

		if (timeout != MONO_INFINITE_WAIT) {
			gint64 elapsed;

			elapsed = mono_msec_ticks () - start;
			if (elapsed >= timeout) {
				ret = MONO_W32HANDLE_WAIT_RET_TIMEOUT;
				break;
			}

			timeoutLeft = timeout - elapsed;
		}
	}

	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);

	return map_native_wait_result_to_managed (ret, numhandles);
}

gint32
ves_icall_System_Threading_WaitHandle_SignalAndWait_Internal (gpointer toSignal, gpointer toWait, gint32 ms, MonoError *error)
{
	MonoW32HandleWaitRet ret;
	MonoInternalThread *thread = mono_thread_internal_current ();

	if (ms == -1)
		ms = MONO_INFINITE_WAIT;

	if (mono_thread_current_check_pending_interrupt ())
		return map_native_wait_result_to_managed (MONO_W32HANDLE_WAIT_RET_FAILED, 0);

	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);
	
	MONO_ENTER_GC_SAFE;
#ifdef HOST_WIN32
	ret = mono_w32handle_convert_wait_ret (SignalObjectAndWait (toSignal, toWait, ms, TRUE), 1);
#else
	ret = mono_w32handle_signal_and_wait (toSignal, toWait, ms, TRUE);
#endif
	MONO_EXIT_GC_SAFE;
	
	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);

	return map_native_wait_result_to_managed (ret, 1);
}

gint32 ves_icall_System_Threading_Interlocked_Increment_Int (gint32 *location)
{
	return InterlockedIncrement (location);
}

gint64 ves_icall_System_Threading_Interlocked_Increment_Long (gint64 *location)
{
#if SIZEOF_VOID_P == 4
	if (G_UNLIKELY ((size_t)location & 0x7)) {
		gint64 ret;
		mono_interlocked_lock ();
		(*location)++;
		ret = *location;
		mono_interlocked_unlock ();
		return ret;
	}
#endif
	return InterlockedIncrement64 (location);
}

gint32 ves_icall_System_Threading_Interlocked_Decrement_Int (gint32 *location)
{
	return InterlockedDecrement(location);
}

gint64 ves_icall_System_Threading_Interlocked_Decrement_Long (gint64 * location)
{
#if SIZEOF_VOID_P == 4
	if (G_UNLIKELY ((size_t)location & 0x7)) {
		gint64 ret;
		mono_interlocked_lock ();
		(*location)--;
		ret = *location;
		mono_interlocked_unlock ();
		return ret;
	}
#endif
	return InterlockedDecrement64 (location);
}

gint32 ves_icall_System_Threading_Interlocked_Exchange_Int (gint32 *location, gint32 value)
{
	return InterlockedExchange(location, value);
}

MonoObject * ves_icall_System_Threading_Interlocked_Exchange_Object (MonoObject **location, MonoObject *value)
{
	MonoObject *res;
	res = (MonoObject *) InterlockedExchangePointer((gpointer *) location, value);
	mono_gc_wbarrier_generic_nostore (location);
	return res;
}

gpointer ves_icall_System_Threading_Interlocked_Exchange_IntPtr (gpointer *location, gpointer value)
{
	return InterlockedExchangePointer(location, value);
}

gfloat ves_icall_System_Threading_Interlocked_Exchange_Single (gfloat *location, gfloat value)
{
	IntFloatUnion val, ret;

	val.fval = value;
	ret.ival = InterlockedExchange((gint32 *) location, val.ival);

	return ret.fval;
}

gint64 
ves_icall_System_Threading_Interlocked_Exchange_Long (gint64 *location, gint64 value)
{
#if SIZEOF_VOID_P == 4
	if (G_UNLIKELY ((size_t)location & 0x7)) {
		gint64 ret;
		mono_interlocked_lock ();
		ret = *location;
		*location = value;
		mono_interlocked_unlock ();
		return ret;
	}
#endif
	return InterlockedExchange64 (location, value);
}

gdouble 
ves_icall_System_Threading_Interlocked_Exchange_Double (gdouble *location, gdouble value)
{
	LongDoubleUnion val, ret;

	val.fval = value;
	ret.ival = (gint64)InterlockedExchange64((gint64 *) location, val.ival);

	return ret.fval;
}

gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int(gint32 *location, gint32 value, gint32 comparand)
{
	return InterlockedCompareExchange(location, value, comparand);
}

gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int_Success(gint32 *location, gint32 value, gint32 comparand, MonoBoolean *success)
{
	gint32 r = InterlockedCompareExchange(location, value, comparand);
	*success = r == comparand;
	return r;
}

MonoObject * ves_icall_System_Threading_Interlocked_CompareExchange_Object (MonoObject **location, MonoObject *value, MonoObject *comparand)
{
	MonoObject *res;
	res = (MonoObject *) InterlockedCompareExchangePointer((gpointer *) location, value, comparand);
	mono_gc_wbarrier_generic_nostore (location);
	return res;
}

gpointer ves_icall_System_Threading_Interlocked_CompareExchange_IntPtr(gpointer *location, gpointer value, gpointer comparand)
{
	return InterlockedCompareExchangePointer(location, value, comparand);
}

gfloat ves_icall_System_Threading_Interlocked_CompareExchange_Single (gfloat *location, gfloat value, gfloat comparand)
{
	IntFloatUnion val, ret, cmp;

	val.fval = value;
	cmp.fval = comparand;
	ret.ival = InterlockedCompareExchange((gint32 *) location, val.ival, cmp.ival);

	return ret.fval;
}

gdouble
ves_icall_System_Threading_Interlocked_CompareExchange_Double (gdouble *location, gdouble value, gdouble comparand)
{
#if SIZEOF_VOID_P == 8
	LongDoubleUnion val, comp, ret;

	val.fval = value;
	comp.fval = comparand;
	ret.ival = (gint64)InterlockedCompareExchangePointer((gpointer *) location, (gpointer)val.ival, (gpointer)comp.ival);

	return ret.fval;
#else
	gdouble old;

	mono_interlocked_lock ();
	old = *location;
	if (old == comparand)
		*location = value;
	mono_interlocked_unlock ();

	return old;
#endif
}

gint64 
ves_icall_System_Threading_Interlocked_CompareExchange_Long (gint64 *location, gint64 value, gint64 comparand)
{
#if SIZEOF_VOID_P == 4
	if (G_UNLIKELY ((size_t)location & 0x7)) {
		gint64 old;
		mono_interlocked_lock ();
		old = *location;
		if (old == comparand)
			*location = value;
		mono_interlocked_unlock ();
		return old;
	}
#endif
	return InterlockedCompareExchange64 (location, value, comparand);
}

MonoObject*
ves_icall_System_Threading_Interlocked_CompareExchange_T (MonoObject **location, MonoObject *value, MonoObject *comparand)
{
	MonoObject *res;
	res = (MonoObject *)InterlockedCompareExchangePointer ((volatile gpointer *)location, value, comparand);
	mono_gc_wbarrier_generic_nostore (location);
	return res;
}

MonoObject*
ves_icall_System_Threading_Interlocked_Exchange_T (MonoObject **location, MonoObject *value)
{
	MonoObject *res;
	MONO_CHECK_NULL (location, NULL);
	res = (MonoObject *)InterlockedExchangePointer ((volatile gpointer *)location, value);
	mono_gc_wbarrier_generic_nostore (location);
	return res;
}

gint32 
ves_icall_System_Threading_Interlocked_Add_Int (gint32 *location, gint32 value)
{
	return InterlockedAdd (location, value);
}

gint64 
ves_icall_System_Threading_Interlocked_Add_Long (gint64 *location, gint64 value)
{
#if SIZEOF_VOID_P == 4
	if (G_UNLIKELY ((size_t)location & 0x7)) {
		gint64 ret;
		mono_interlocked_lock ();
		*location += value;
		ret = *location;
		mono_interlocked_unlock ();
		return ret;
	}
#endif
	return InterlockedAdd64 (location, value);
}

gint64 
ves_icall_System_Threading_Interlocked_Read_Long (gint64 *location)
{
#if SIZEOF_VOID_P == 4
	if (G_UNLIKELY ((size_t)location & 0x7)) {
		gint64 ret;
		mono_interlocked_lock ();
		ret = *location;
		mono_interlocked_unlock ();
		return ret;
	}
#endif
	return InterlockedRead64 (location);
}

void
ves_icall_System_Threading_Thread_MemoryBarrier (void)
{
	mono_memory_barrier ();
}

void
ves_icall_System_Threading_Thread_ClrState (MonoInternalThread* this_obj, guint32 state)
{
	mono_thread_clr_state (this_obj, (MonoThreadState)state);

	if (state & ThreadState_Background) {
		/* If the thread changes the background mode, the main thread has to
		 * be notified, since it has to rebuild the list of threads to
		 * wait for.
		 */
		mono_os_event_set (&background_change_event);
	}
}

void
ves_icall_System_Threading_Thread_SetState (MonoInternalThread* this_obj, guint32 state)
{
	mono_thread_set_state (this_obj, (MonoThreadState)state);
	
	if (state & ThreadState_Background) {
		/* If the thread changes the background mode, the main thread has to
		 * be notified, since it has to rebuild the list of threads to
		 * wait for.
		 */
		mono_os_event_set (&background_change_event);
	}
}

guint32
ves_icall_System_Threading_Thread_GetState (MonoInternalThread* this_obj)
{
	guint32 state;

	LOCK_THREAD (this_obj);
	
	state = this_obj->state;

	UNLOCK_THREAD (this_obj);
	
	return state;
}

void ves_icall_System_Threading_Thread_Interrupt_internal (MonoThread *this_obj)
{
	MonoInternalThread *current;
	gboolean throw_;
	MonoInternalThread *thread = this_obj->internal_thread;

	LOCK_THREAD (thread);

	current = mono_thread_internal_current ();

	thread->thread_interrupt_requested = TRUE;
	throw_ = current != thread && (thread->state & ThreadState_WaitSleepJoin);

	UNLOCK_THREAD (thread);

	if (throw_) {
		async_abort_internal (thread, FALSE);
	}
}

/**
 * mono_thread_current_check_pending_interrupt:
 * Checks if there's a interruption request and set the pending exception if so.
 * \returns true if a pending exception was set
 */
gboolean
mono_thread_current_check_pending_interrupt (void)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	gboolean throw_ = FALSE;

	LOCK_THREAD (thread);
	
	if (thread->thread_interrupt_requested) {
		throw_ = TRUE;
		thread->thread_interrupt_requested = FALSE;
	}
	
	UNLOCK_THREAD (thread);

	if (throw_)
		mono_set_pending_exception (mono_get_exception_thread_interrupted ());
	return throw_;
}

static gboolean
request_thread_abort (MonoInternalThread *thread, MonoObject *state)
{
	LOCK_THREAD (thread);
	
	if (thread->state & (ThreadState_AbortRequested | ThreadState_Stopped))
	{
		UNLOCK_THREAD (thread);
		return FALSE;
	}

	if ((thread->state & ThreadState_Unstarted) != 0) {
		thread->state |= ThreadState_Aborted;
		UNLOCK_THREAD (thread);
		return FALSE;
	}

	thread->state |= ThreadState_AbortRequested;
	if (thread->abort_state_handle)
		mono_gchandle_free (thread->abort_state_handle);
	if (state) {
		thread->abort_state_handle = mono_gchandle_new (state, FALSE);
		g_assert (thread->abort_state_handle);
	} else {
		thread->abort_state_handle = 0;
	}
	thread->abort_exc = NULL;

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Abort requested for %p (%"G_GSIZE_FORMAT")", __func__, mono_native_thread_id_get (), thread, (gsize)thread->tid));

	/* During shutdown, we can't wait for other threads */
	if (!shutting_down)
		/* Make sure the thread is awake */
		mono_thread_resume (thread);

	UNLOCK_THREAD (thread);
	return TRUE;
}

void
ves_icall_System_Threading_Thread_Abort (MonoInternalThread *thread, MonoObject *state)
{
	if (!request_thread_abort (thread, state))
		return;

	if (thread == mono_thread_internal_current ()) {
		MonoError error;
		self_abort_internal (&error);
		mono_error_set_pending_exception (&error);
	} else {
		async_abort_internal (thread, TRUE);
	}
}

/**
 * mono_thread_internal_abort:
 * Request thread \p thread to be aborted.
 * \p thread MUST NOT be the current thread.
 */
void
mono_thread_internal_abort (MonoInternalThread *thread)
{
	g_assert (thread != mono_thread_internal_current ());

	if (!request_thread_abort (thread, NULL))
		return;
	async_abort_internal (thread, TRUE);
}

void
ves_icall_System_Threading_Thread_ResetAbort (MonoThread *this_obj)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	gboolean was_aborting;

	LOCK_THREAD (thread);
	was_aborting = thread->state & ThreadState_AbortRequested;
	thread->state &= ~ThreadState_AbortRequested;
	UNLOCK_THREAD (thread);

	if (!was_aborting) {
		const char *msg = "Unable to reset abort because no abort was requested";
		mono_set_pending_exception (mono_get_exception_thread_state (msg));
		return;
	}

	mono_get_eh_callbacks ()->mono_clear_abort_threshold ();
	thread->abort_exc = NULL;
	if (thread->abort_state_handle) {
		mono_gchandle_free (thread->abort_state_handle);
		/* This is actually not necessary - the handle
		   only counts if the exception is set */
		thread->abort_state_handle = 0;
	}
}

void
mono_thread_internal_reset_abort (MonoInternalThread *thread)
{
	LOCK_THREAD (thread);

	thread->state &= ~ThreadState_AbortRequested;

	if (thread->abort_exc) {
		mono_get_eh_callbacks ()->mono_clear_abort_threshold ();
		thread->abort_exc = NULL;
		if (thread->abort_state_handle) {
			mono_gchandle_free (thread->abort_state_handle);
			/* This is actually not necessary - the handle
			   only counts if the exception is set */
			thread->abort_state_handle = 0;
		}
	}

	UNLOCK_THREAD (thread);
}

MonoObject*
ves_icall_System_Threading_Thread_GetAbortExceptionState (MonoThread *this_obj)
{
	MonoError error;
	MonoInternalThread *thread = this_obj->internal_thread;
	MonoObject *state, *deserialized = NULL;
	MonoDomain *domain;

	if (!thread->abort_state_handle)
		return NULL;

	state = mono_gchandle_get_target (thread->abort_state_handle);
	g_assert (state);

	domain = mono_domain_get ();
	if (mono_object_domain (state) == domain)
		return state;

	deserialized = mono_object_xdomain_representation (state, domain, &error);

	if (!deserialized) {
		MonoException *invalid_op_exc = mono_get_exception_invalid_operation ("Thread.ExceptionState cannot access an ExceptionState from a different AppDomain");
		if (!is_ok (&error)) {
			MonoObject *exc = (MonoObject*)mono_error_convert_to_exception (&error);
			MONO_OBJECT_SETREF (invalid_op_exc, inner_ex, exc);
		}
		mono_set_pending_exception (invalid_op_exc);
		return NULL;
	}

	return deserialized;
}

static gboolean
mono_thread_suspend (MonoInternalThread *thread)
{
	LOCK_THREAD (thread);

	if (thread->state & (ThreadState_Unstarted | ThreadState_Aborted | ThreadState_Stopped))
	{
		UNLOCK_THREAD (thread);
		return FALSE;
	}

	if (thread->state & (ThreadState_Suspended | ThreadState_SuspendRequested | ThreadState_AbortRequested))
	{
		UNLOCK_THREAD (thread);
		return TRUE;
	}
	
	thread->state |= ThreadState_SuspendRequested;
	mono_os_event_reset (thread->suspended);

	if (thread == mono_thread_internal_current ()) {
		/* calls UNLOCK_THREAD (thread) */
		self_suspend_internal ();
	} else {
		/* calls UNLOCK_THREAD (thread) */
		async_suspend_internal (thread, FALSE);
	}

	return TRUE;
}

void
ves_icall_System_Threading_Thread_Suspend (MonoThread *this_obj)
{
	if (!mono_thread_suspend (this_obj->internal_thread)) {
		mono_set_pending_exception (mono_get_exception_thread_state ("Thread has not been started, or is dead."));
		return;
	}
}

/* LOCKING: LOCK_THREAD(thread) must be held */
static gboolean
mono_thread_resume (MonoInternalThread *thread)
{
	if ((thread->state & ThreadState_SuspendRequested) != 0) {
		// MOSTLY_ASYNC_SAFE_PRINTF ("RESUME (1) thread %p\n", thread_get_tid (thread));
		thread->state &= ~ThreadState_SuspendRequested;
		mono_os_event_set (thread->suspended);
		return TRUE;
	}

	if ((thread->state & ThreadState_Suspended) == 0 ||
		(thread->state & ThreadState_Unstarted) != 0 || 
		(thread->state & ThreadState_Aborted) != 0 || 
		(thread->state & ThreadState_Stopped) != 0)
	{
		// MOSTLY_ASYNC_SAFE_PRINTF ("RESUME (2) thread %p\n", thread_get_tid (thread));
		return FALSE;
	}

	// MOSTLY_ASYNC_SAFE_PRINTF ("RESUME (3) thread %p\n", thread_get_tid (thread));

	mono_os_event_set (thread->suspended);

	if (!thread->self_suspended) {
		UNLOCK_THREAD (thread);

		/* Awake the thread */
		if (!mono_thread_info_resume (thread_get_tid (thread)))
			return FALSE;

		LOCK_THREAD (thread);
	}

	thread->state &= ~ThreadState_Suspended;

	return TRUE;
}

void
ves_icall_System_Threading_Thread_Resume (MonoThread *thread)
{
	if (!thread->internal_thread) {
		mono_set_pending_exception (mono_get_exception_thread_state ("Thread has not been started, or is dead."));
	} else {
		LOCK_THREAD (thread->internal_thread);
		if (!mono_thread_resume (thread->internal_thread))
			mono_set_pending_exception (mono_get_exception_thread_state ("Thread has not been started, or is dead."));
		UNLOCK_THREAD (thread->internal_thread);
	}
}

static gboolean
mono_threads_is_critical_method (MonoMethod *method)
{
	switch (method->wrapper_type) {
	case MONO_WRAPPER_RUNTIME_INVOKE:
	case MONO_WRAPPER_XDOMAIN_INVOKE:
	case MONO_WRAPPER_XDOMAIN_DISPATCH:	
		return TRUE;
	}
	return FALSE;
}

static gboolean
find_wrapper (MonoMethod *m, gint no, gint ilo, gboolean managed, gpointer data)
{
	if (managed)
		return TRUE;

	if (mono_threads_is_critical_method (m)) {
		*((gboolean*)data) = TRUE;
		return TRUE;
	}
	return FALSE;
}

static gboolean 
is_running_protected_wrapper (void)
{
	gboolean found = FALSE;
	mono_stack_walk (find_wrapper, &found);
	return found;
}

/**
 * mono_thread_stop:
 */
void
mono_thread_stop (MonoThread *thread)
{
	MonoInternalThread *internal = thread->internal_thread;

	if (!request_thread_abort (internal, NULL))
		return;

	if (internal == mono_thread_internal_current ()) {
		MonoError error;
		self_abort_internal (&error);
		/*
		This function is part of the embeding API and has no way to return the exception
		to be thrown. So what we do is keep the old behavior and raise the exception.
		*/
		mono_error_raise_exception (&error); /* OK to throw, see note */
	} else {
		async_abort_internal (internal, TRUE);
	}
}

gint8
ves_icall_System_Threading_Thread_VolatileRead1 (void *ptr)
{
	gint8 tmp = *(volatile gint8 *)ptr;
	mono_memory_barrier ();
	return tmp;
}

gint16
ves_icall_System_Threading_Thread_VolatileRead2 (void *ptr)
{
	gint16 tmp = *(volatile gint16 *)ptr;
	mono_memory_barrier ();
	return tmp;
}

gint32
ves_icall_System_Threading_Thread_VolatileRead4 (void *ptr)
{
	gint32 tmp = *(volatile gint32 *)ptr;
	mono_memory_barrier ();
	return tmp;
}

gint64
ves_icall_System_Threading_Thread_VolatileRead8 (void *ptr)
{
	gint64 tmp = *(volatile gint64 *)ptr;
	mono_memory_barrier ();
	return tmp;
}

void *
ves_icall_System_Threading_Thread_VolatileReadIntPtr (void *ptr)
{
	volatile void *tmp = *(volatile void **)ptr;
	mono_memory_barrier ();
	return (void *) tmp;
}

void *
ves_icall_System_Threading_Thread_VolatileReadObject (void *ptr)
{
	volatile MonoObject *tmp = *(volatile MonoObject **)ptr;
	mono_memory_barrier ();
	return (MonoObject *) tmp;
}

double
ves_icall_System_Threading_Thread_VolatileReadDouble (void *ptr)
{
	double tmp = *(volatile double *)ptr;
	mono_memory_barrier ();
	return tmp;
}

float
ves_icall_System_Threading_Thread_VolatileReadFloat (void *ptr)
{
	float tmp = *(volatile float *)ptr;
	mono_memory_barrier ();
	return tmp;
}

gint8
ves_icall_System_Threading_Volatile_Read1 (void *ptr)
{
	return InterlockedRead8 ((volatile gint8 *)ptr);
}

gint16
ves_icall_System_Threading_Volatile_Read2 (void *ptr)
{
	return InterlockedRead16 ((volatile gint16 *)ptr);
}

gint32
ves_icall_System_Threading_Volatile_Read4 (void *ptr)
{
	return InterlockedRead ((volatile gint32 *)ptr);
}

gint64
ves_icall_System_Threading_Volatile_Read8 (void *ptr)
{
#if SIZEOF_VOID_P == 4
	if (G_UNLIKELY ((size_t)ptr & 0x7)) {
		gint64 val;
		mono_interlocked_lock ();
		val = *(gint64*)ptr;
		mono_interlocked_unlock ();
		return val;
	}
#endif
	return InterlockedRead64 ((volatile gint64 *)ptr);
}

void *
ves_icall_System_Threading_Volatile_ReadIntPtr (void *ptr)
{
	return InterlockedReadPointer ((volatile gpointer *)ptr);
}

double
ves_icall_System_Threading_Volatile_ReadDouble (void *ptr)
{
	LongDoubleUnion u;

#if SIZEOF_VOID_P == 4
	if (G_UNLIKELY ((size_t)ptr & 0x7)) {
		double val;
		mono_interlocked_lock ();
		val = *(double*)ptr;
		mono_interlocked_unlock ();
		return val;
	}
#endif

	u.ival = InterlockedRead64 ((volatile gint64 *)ptr);

	return u.fval;
}

float
ves_icall_System_Threading_Volatile_ReadFloat (void *ptr)
{
	IntFloatUnion u;

	u.ival = InterlockedRead ((volatile gint32 *)ptr);

	return u.fval;
}

MonoObject*
ves_icall_System_Threading_Volatile_Read_T (void *ptr)
{
	return (MonoObject *)InterlockedReadPointer ((volatile gpointer *)ptr);
}

void
ves_icall_System_Threading_Thread_VolatileWrite1 (void *ptr, gint8 value)
{
	mono_memory_barrier ();
	*(volatile gint8 *)ptr = value;
}

void
ves_icall_System_Threading_Thread_VolatileWrite2 (void *ptr, gint16 value)
{
	mono_memory_barrier ();
	*(volatile gint16 *)ptr = value;
}

void
ves_icall_System_Threading_Thread_VolatileWrite4 (void *ptr, gint32 value)
{
	mono_memory_barrier ();
	*(volatile gint32 *)ptr = value;
}

void
ves_icall_System_Threading_Thread_VolatileWrite8 (void *ptr, gint64 value)
{
	mono_memory_barrier ();
	*(volatile gint64 *)ptr = value;
}

void
ves_icall_System_Threading_Thread_VolatileWriteIntPtr (void *ptr, void *value)
{
	mono_memory_barrier ();
	*(volatile void **)ptr = value;
}

void
ves_icall_System_Threading_Thread_VolatileWriteObject (void *ptr, MonoObject *value)
{
	mono_memory_barrier ();
	mono_gc_wbarrier_generic_store (ptr, value);
}

void
ves_icall_System_Threading_Thread_VolatileWriteDouble (void *ptr, double value)
{
	mono_memory_barrier ();
	*(volatile double *)ptr = value;
}

void
ves_icall_System_Threading_Thread_VolatileWriteFloat (void *ptr, float value)
{
	mono_memory_barrier ();
	*(volatile float *)ptr = value;
}

void
ves_icall_System_Threading_Volatile_Write1 (void *ptr, gint8 value)
{
	InterlockedWrite8 ((volatile gint8 *)ptr, value);
}

void
ves_icall_System_Threading_Volatile_Write2 (void *ptr, gint16 value)
{
	InterlockedWrite16 ((volatile gint16 *)ptr, value);
}

void
ves_icall_System_Threading_Volatile_Write4 (void *ptr, gint32 value)
{
	InterlockedWrite ((volatile gint32 *)ptr, value);
}

void
ves_icall_System_Threading_Volatile_Write8 (void *ptr, gint64 value)
{
#if SIZEOF_VOID_P == 4
	if (G_UNLIKELY ((size_t)ptr & 0x7)) {
		mono_interlocked_lock ();
		*(gint64*)ptr = value;
		mono_interlocked_unlock ();
		return;
	}
#endif

	InterlockedWrite64 ((volatile gint64 *)ptr, value);
}

void
ves_icall_System_Threading_Volatile_WriteIntPtr (void *ptr, void *value)
{
	InterlockedWritePointer ((volatile gpointer *)ptr, value);
}

void
ves_icall_System_Threading_Volatile_WriteDouble (void *ptr, double value)
{
	LongDoubleUnion u;

#if SIZEOF_VOID_P == 4
	if (G_UNLIKELY ((size_t)ptr & 0x7)) {
		mono_interlocked_lock ();
		*(double*)ptr = value;
		mono_interlocked_unlock ();
		return;
	}
#endif

	u.fval = value;

	InterlockedWrite64 ((volatile gint64 *)ptr, u.ival);
}

void
ves_icall_System_Threading_Volatile_WriteFloat (void *ptr, float value)
{
	IntFloatUnion u;

	u.fval = value;

	InterlockedWrite ((volatile gint32 *)ptr, u.ival);
}

void
ves_icall_System_Threading_Volatile_Write_T (void *ptr, MonoObject *value)
{
	mono_gc_wbarrier_generic_store_atomic (ptr, value);
}

static void
free_context (void *user_data)
{
	ContextStaticData *data = user_data;

	mono_threads_lock ();

	/*
	 * There is no guarantee that, by the point this reference queue callback
	 * has been invoked, the GC handle associated with the object will fail to
	 * resolve as one might expect. So if we don't free and remove the GC
	 * handle here, free_context_static_data_helper () could end up resolving
	 * a GC handle to an actually-dead context which would contain a pointer
	 * to an already-freed static data segment, resulting in a crash when
	 * accessing it.
	 */
	g_hash_table_remove (contexts, GUINT_TO_POINTER (data->gc_handle));

	mono_threads_unlock ();

	mono_gchandle_free (data->gc_handle);
	mono_free_static_data (data->static_data);
	g_free (data);
}

void
mono_threads_register_app_context (MonoAppContext *ctx, MonoError *error)
{
	error_init (error);
	mono_threads_lock ();

	//g_print ("Registering context %d in domain %d\n", ctx->context_id, ctx->domain_id);

	if (!contexts)
		contexts = g_hash_table_new (NULL, NULL);

	if (!context_queue)
		context_queue = mono_gc_reference_queue_new (free_context);

	gpointer gch = GUINT_TO_POINTER (mono_gchandle_new_weakref (&ctx->obj, FALSE));
	g_hash_table_insert (contexts, gch, gch);

	/*
	 * We use this intermediate structure to contain a duplicate pointer to
	 * the static data because we can't rely on being able to resolve the GC
	 * handle in the reference queue callback.
	 */
	ContextStaticData *data = g_new0 (ContextStaticData, 1);
	data->gc_handle = GPOINTER_TO_UINT (gch);
	ctx->data = data;

	context_adjust_static_data (ctx);
	mono_gc_reference_queue_add (context_queue, &ctx->obj, data);

	mono_threads_unlock ();

	MONO_PROFILER_RAISE (context_loaded, (ctx));
}

void
ves_icall_System_Runtime_Remoting_Contexts_Context_RegisterContext (MonoAppContextHandle ctx, MonoError *error)
{
	error_init (error);
	mono_threads_register_app_context (MONO_HANDLE_RAW (ctx), error); /* FIXME use handles in mono_threads_register_app_context */
}

void
mono_threads_release_app_context (MonoAppContext* ctx, MonoError *error)
{
	/*
	 * NOTE: Since finalizers are unreliable for the purposes of ensuring
	 * cleanup in exceptional circumstances, we don't actually do any
	 * cleanup work here. We instead do this via a reference queue.
	 */

	//g_print ("Releasing context %d in domain %d\n", ctx->context_id, ctx->domain_id);

	MONO_PROFILER_RAISE (context_unloaded, (ctx));
}

void
ves_icall_System_Runtime_Remoting_Contexts_Context_ReleaseContext (MonoAppContextHandle ctx, MonoError *error)
{
	error_init (error);
	mono_threads_release_app_context (MONO_HANDLE_RAW (ctx), error); /* FIXME use handles in mono_threads_release_app_context */
}

void mono_thread_init (MonoThreadStartCB start_cb,
		       MonoThreadAttachCB attach_cb)
{
	mono_coop_mutex_init_recursive (&threads_mutex);

	mono_os_mutex_init_recursive(&interlocked_mutex);
	mono_os_mutex_init_recursive(&joinable_threads_mutex);
	
	mono_os_event_init (&background_change_event, FALSE);
	
	mono_init_static_data_info (&thread_static_info);
	mono_init_static_data_info (&context_static_info);

	mono_thread_start_cb = start_cb;
	mono_thread_attach_cb = attach_cb;
}

static gpointer
thread_attach (MonoThreadInfo *info)
{
	return mono_gc_thread_attach (info);
}

static void
thread_detach (MonoThreadInfo *info)
{
	MonoInternalThread *internal;
	guint32 gchandle;

	/* If a delegate is passed to native code and invoked on a thread we dont
	 * know about, marshal will register it with mono_threads_attach_coop, but
	 * we have no way of knowing when that thread goes away.  SGen has a TSD
	 * so we assume that if the domain is still registered, we can detach
	 * the thread */

	g_assert (info);

	if (!mono_thread_info_try_get_internal_thread_gchandle (info, &gchandle))
		return;

	internal = (MonoInternalThread*) mono_gchandle_get_target (gchandle);
	g_assert (internal);

	mono_gchandle_free (gchandle);

	mono_thread_detach_internal (internal);
}

static void
thread_detach_with_lock (MonoThreadInfo *info)
{
	return mono_gc_thread_detach_with_lock (info);
}

static gboolean
thread_in_critical_region (MonoThreadInfo *info)
{
	return mono_gc_thread_in_critical_region (info);
}

static gboolean
ip_in_critical_region (MonoDomain *domain, gpointer ip)
{
	MonoJitInfo *ji;
	MonoMethod *method;

	/*
	 * We pass false for 'try_aot' so this becomes async safe.
	 * It won't find aot methods whose jit info is not yet loaded,
	 * so we preload their jit info in the JIT.
	 */
	ji = mono_jit_info_table_find_internal (domain, ip, FALSE, FALSE);
	if (!ji)
		return FALSE;

	method = mono_jit_info_get_method (ji);
	g_assert (method);

	return mono_gc_is_critical_method (method);
}

void
mono_thread_callbacks_init (void)
{
	MonoThreadInfoCallbacks cb;

	memset (&cb, 0, sizeof(cb));
	cb.thread_attach = thread_attach;
	cb.thread_detach = thread_detach;
	cb.thread_detach_with_lock = thread_detach_with_lock;
	cb.ip_in_critical_region = ip_in_critical_region;
	cb.thread_in_critical_region = thread_in_critical_region;
	mono_thread_info_callbacks_init (&cb);
}

/**
 * mono_thread_cleanup:
 */
void
mono_thread_cleanup (void)
{
#if !defined(RUN_IN_SUBTHREAD) && !defined(HOST_WIN32)
	/* The main thread must abandon any held mutexes (particularly
	 * important for named mutexes as they are shared across
	 * processes, see bug 74680.)  This will happen when the
	 * thread exits, but if it's not running in a subthread it
	 * won't exit in time.
	 */
	mono_w32mutex_abandon ();
#endif

#if 0
	/* This stuff needs more testing, it seems one of these
	 * critical sections can be locked when mono_thread_cleanup is
	 * called.
	 */
	mono_coop_mutex_destroy (&threads_mutex);
	mono_os_mutex_destroy (&interlocked_mutex);
	mono_os_mutex_destroy (&delayed_free_table_mutex);
	mono_os_mutex_destroy (&small_id_mutex);
	mono_os_event_destroy (&background_change_event);
#endif
}

void
mono_threads_install_cleanup (MonoThreadCleanupFunc func)
{
	mono_thread_cleanup_fn = func;
}

/**
 * mono_thread_set_manage_callback:
 */
void
mono_thread_set_manage_callback (MonoThread *thread, MonoThreadManageCallback func)
{
	thread->internal_thread->manage_callback = func;
}

G_GNUC_UNUSED
static void print_tids (gpointer key, gpointer value, gpointer user)
{
	/* GPOINTER_TO_UINT breaks horribly if sizeof(void *) >
	 * sizeof(uint) and a cast to uint would overflow
	 */
	/* Older versions of glib don't have G_GSIZE_FORMAT, so just
	 * print this as a pointer.
	 */
	g_message ("Waiting for: %p", key);
}

struct wait_data 
{
	MonoThreadHandle *handles[MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS];
	MonoInternalThread *threads[MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS];
	guint32 num;
};

static void
wait_for_tids (struct wait_data *wait, guint32 timeout, gboolean check_state_change)
{
	guint32 i;
	MonoThreadInfoWaitRet ret;
	
	THREAD_DEBUG (g_message("%s: %d threads to wait for in this batch", __func__, wait->num));

	/* Add the thread state change event, so it wakes
	 * up if a thread changes to background mode. */

	MONO_ENTER_GC_SAFE;
	if (check_state_change)
		ret = mono_thread_info_wait_multiple_handle (wait->handles, wait->num, &background_change_event, FALSE, timeout, TRUE);
	else
		ret = mono_thread_info_wait_multiple_handle (wait->handles, wait->num, NULL, TRUE, timeout, TRUE);
	MONO_EXIT_GC_SAFE;

	if (ret == MONO_THREAD_INFO_WAIT_RET_FAILED) {
		/* See the comment in build_wait_tids() */
		THREAD_DEBUG (g_message ("%s: Wait failed", __func__));
		return;
	}
	
	for( i = 0; i < wait->num; i++)
		mono_threads_close_thread_handle (wait->handles [i]);

	if (ret == MONO_THREAD_INFO_WAIT_RET_TIMEOUT)
		return;
	
	if (ret < wait->num) {
		MonoInternalThread *internal;

		internal = wait->threads [ret];

		mono_threads_lock ();
		if (mono_g_hash_table_lookup (threads, (gpointer) internal->tid) == internal)
			g_error ("%s: failed to call mono_thread_detach_internal on thread %p, InternalThread: %p", __func__, internal->tid, internal);
		mono_threads_unlock ();
	}
}

static void build_wait_tids (gpointer key, gpointer value, gpointer user)
{
	struct wait_data *wait=(struct wait_data *)user;

	if(wait->num<MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS - 1) {
		MonoInternalThread *thread=(MonoInternalThread *)value;

		/* Ignore background threads, we abort them later */
		/* Do not lock here since it is not needed and the caller holds threads_lock */
		if (thread->state & ThreadState_Background) {
			THREAD_DEBUG (g_message ("%s: ignoring background thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
			return; /* just leave, ignore */
		}
		
		if (mono_gc_is_finalizer_internal_thread (thread)) {
			THREAD_DEBUG (g_message ("%s: ignoring finalizer thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
			return;
		}

		if (thread == mono_thread_internal_current ()) {
			THREAD_DEBUG (g_message ("%s: ignoring current thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
			return;
		}

		if (mono_thread_get_main () && (thread == mono_thread_get_main ()->internal_thread)) {
			THREAD_DEBUG (g_message ("%s: ignoring main thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
			return;
		}

		if (thread->flags & MONO_THREAD_FLAG_DONT_MANAGE) {
			THREAD_DEBUG (g_message ("%s: ignoring thread %" G_GSIZE_FORMAT "with DONT_MANAGE flag set.", __func__, (gsize)thread->tid));
			return;
		}

		THREAD_DEBUG (g_message ("%s: Invoking mono_thread_manage callback on thread %p", __func__, thread));
		if ((thread->manage_callback == NULL) || (thread->manage_callback (thread->root_domain_thread) == TRUE)) {
			wait->handles[wait->num]=mono_threads_open_thread_handle (thread->handle);
			wait->threads[wait->num]=thread;
			wait->num++;

			THREAD_DEBUG (g_message ("%s: adding thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
		} else {
			THREAD_DEBUG (g_message ("%s: ignoring (because of callback) thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
		}
		
		
	} else {
		/* Just ignore the rest, we can't do anything with
		 * them yet
		 */
	}
}

static gboolean
remove_and_abort_threads (gpointer key, gpointer value, gpointer user)
{
	struct wait_data *wait=(struct wait_data *)user;
	MonoNativeThreadId self = mono_native_thread_id_get ();
	MonoInternalThread *thread = (MonoInternalThread *)value;

	if (wait->num >= MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS)
		return FALSE;

	if (mono_native_thread_id_equals (thread_get_tid (thread), self))
		return FALSE;
	if (mono_gc_is_finalizer_internal_thread (thread))
		return FALSE;

	if ((thread->state & ThreadState_Background) && !(thread->flags & MONO_THREAD_FLAG_DONT_MANAGE)) {
		wait->handles[wait->num] = mono_threads_open_thread_handle (thread->handle);
		wait->threads[wait->num] = thread;
		wait->num++;

		THREAD_DEBUG (g_print ("%s: Aborting id: %"G_GSIZE_FORMAT"\n", __func__, (gsize)thread->tid));
		mono_thread_internal_abort (thread);
	}

	return TRUE;
}

/** 
 * mono_threads_set_shutting_down:
 *
 * Is called by a thread that wants to shut down Mono. If the runtime is already
 * shutting down, the calling thread is suspended/stopped, and this function never
 * returns.
 */
void
mono_threads_set_shutting_down (void)
{
	MonoInternalThread *current_thread = mono_thread_internal_current ();

	mono_threads_lock ();

	if (shutting_down) {
		mono_threads_unlock ();

		/* Make sure we're properly suspended/stopped */

		LOCK_THREAD (current_thread);

		if (current_thread->state & (ThreadState_SuspendRequested | ThreadState_AbortRequested)) {
			UNLOCK_THREAD (current_thread);
			mono_thread_execute_interruption ();
		} else {
			UNLOCK_THREAD (current_thread);
		}

		/*since we're killing the thread, detach it.*/
		mono_thread_detach_internal (current_thread);

		/* Wake up other threads potentially waiting for us */
		mono_thread_info_exit (0);
	} else {
		shutting_down = TRUE;

		/* Not really a background state change, but this will
		 * interrupt the main thread if it is waiting for all
		 * the other threads.
		 */
		mono_os_event_set (&background_change_event);
		
		mono_threads_unlock ();
	}
}

/**
 * mono_thread_manage:
 */
void
mono_thread_manage (void)
{
	struct wait_data wait_data;
	struct wait_data *wait = &wait_data;

	memset (wait, 0, sizeof (struct wait_data));
	/* join each thread that's still running */
	THREAD_DEBUG (g_message ("%s: Joining each running thread...", __func__));
	
	mono_threads_lock ();
	if(threads==NULL) {
		THREAD_DEBUG (g_message("%s: No threads", __func__));
		mono_threads_unlock ();
		return;
	}
	mono_threads_unlock ();
	
	do {
		mono_threads_lock ();
		if (shutting_down) {
			/* somebody else is shutting down */
			mono_threads_unlock ();
			break;
		}
		THREAD_DEBUG (g_message ("%s: There are %d threads to join", __func__, mono_g_hash_table_size (threads));
			mono_g_hash_table_foreach (threads, print_tids, NULL));
	
		mono_os_event_reset (&background_change_event);
		wait->num=0;
		/* We must zero all InternalThread pointers to avoid making the GC unhappy. */
		memset (wait->threads, 0, MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS * SIZEOF_VOID_P);
		mono_g_hash_table_foreach (threads, build_wait_tids, wait);
		mono_threads_unlock ();
		if (wait->num > 0)
			/* Something to wait for */
			wait_for_tids (wait, MONO_INFINITE_WAIT, TRUE);
		THREAD_DEBUG (g_message ("%s: I have %d threads after waiting.", __func__, wait->num));
	} while(wait->num>0);

	/* Mono is shutting down, so just wait for the end */
	if (!mono_runtime_try_shutdown ()) {
		/*FIXME mono_thread_suspend probably should call mono_thread_execute_interruption when self interrupting. */
		mono_thread_suspend (mono_thread_internal_current ());
		mono_thread_execute_interruption ();
	}

	/* 
	 * Remove everything but the finalizer thread and self.
	 * Also abort all the background threads
	 * */
	do {
		mono_threads_lock ();

		wait->num = 0;
		/*We must zero all InternalThread pointers to avoid making the GC unhappy.*/
		memset (wait->threads, 0, MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS * SIZEOF_VOID_P);
		mono_g_hash_table_foreach_remove (threads, remove_and_abort_threads, wait);

		mono_threads_unlock ();

		THREAD_DEBUG (g_message ("%s: wait->num is now %d", __func__, wait->num));
		if (wait->num > 0) {
			/* Something to wait for */
			wait_for_tids (wait, MONO_INFINITE_WAIT, FALSE);
		}
	} while (wait->num > 0);
	
	/* 
	 * give the subthreads a chance to really quit (this is mainly needed
	 * to get correct user and system times from getrusage/wait/time(1)).
	 * This could be removed if we avoid pthread_detach() and use pthread_join().
	 */
	mono_thread_info_yield ();
}

static void
collect_threads_for_suspend (gpointer key, gpointer value, gpointer user_data)
{
	MonoInternalThread *thread = (MonoInternalThread*)value;
	struct wait_data *wait = (struct wait_data*)user_data;

	/* 
	 * We try to exclude threads early, to avoid running into the MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS
	 * limitation.
	 * This needs no locking.
	 */
	if ((thread->state & ThreadState_Suspended) != 0 || 
		(thread->state & ThreadState_Stopped) != 0)
		return;

	if (wait->num<MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS) {
		wait->handles [wait->num] = mono_threads_open_thread_handle (thread->handle);
		wait->threads [wait->num] = thread;
		wait->num++;
	}
}

/*
 * mono_thread_suspend_all_other_threads:
 *
 *  Suspend all managed threads except the finalizer thread and this thread. It is
 * not possible to resume them later.
 */
void mono_thread_suspend_all_other_threads (void)
{
	struct wait_data wait_data;
	struct wait_data *wait = &wait_data;
	int i;
	MonoNativeThreadId self = mono_native_thread_id_get ();
	guint32 eventidx = 0;
	gboolean starting, finished;

	memset (wait, 0, sizeof (struct wait_data));
	/*
	 * The other threads could be in an arbitrary state at this point, i.e.
	 * they could be starting up, shutting down etc. This means that there could be
	 * threads which are not even in the threads hash table yet.
	 */

	/* 
	 * First we set a barrier which will be checked by all threads before they
	 * are added to the threads hash table, and they will exit if the flag is set.
	 * This ensures that no threads could be added to the hash later.
	 * We will use shutting_down as the barrier for now.
	 */
	g_assert (shutting_down);

	/*
	 * We make multiple calls to WaitForMultipleObjects since:
	 * - we can only wait for MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS threads
	 * - some threads could exit without becoming suspended
	 */
	finished = FALSE;
	while (!finished) {
		/*
		 * Make a copy of the hashtable since we can't do anything with
		 * threads while threads_mutex is held.
		 */
		wait->num = 0;
		/*We must zero all InternalThread pointers to avoid making the GC unhappy.*/
		memset (wait->threads, 0, MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS * SIZEOF_VOID_P);
		mono_threads_lock ();
		mono_g_hash_table_foreach (threads, collect_threads_for_suspend, wait);
		mono_threads_unlock ();

		eventidx = 0;
		/* Get the suspended events that we'll be waiting for */
		for (i = 0; i < wait->num; ++i) {
			MonoInternalThread *thread = wait->threads [i];

			if (mono_native_thread_id_equals (thread_get_tid (thread), self)
			     || mono_gc_is_finalizer_internal_thread (thread)
			     || (thread->flags & MONO_THREAD_FLAG_DONT_MANAGE)
			) {
				mono_threads_close_thread_handle (wait->handles [i]);
				wait->threads [i] = NULL;
				continue;
			}

			LOCK_THREAD (thread);

			if (thread->state & (ThreadState_Suspended | ThreadState_Stopped)) {
				UNLOCK_THREAD (thread);
				mono_threads_close_thread_handle (wait->handles [i]);
				wait->threads [i] = NULL;
				continue;
			}

			++eventidx;

			/* Convert abort requests into suspend requests */
			if ((thread->state & ThreadState_AbortRequested) != 0)
				thread->state &= ~ThreadState_AbortRequested;
			
			thread->state |= ThreadState_SuspendRequested;
			mono_os_event_reset (thread->suspended);

			/* Signal the thread to suspend + calls UNLOCK_THREAD (thread) */
			async_suspend_internal (thread, TRUE);

			mono_threads_close_thread_handle (wait->handles [i]);
			wait->threads [i] = NULL;
		}
		if (eventidx <= 0) {
			/* 
			 * If there are threads which are starting up, we wait until they
			 * are suspended when they try to register in the threads hash.
			 * This is guaranteed to finish, since the threads which can create new
			 * threads get suspended after a while.
			 * FIXME: The finalizer thread can still create new threads.
			 */
			mono_threads_lock ();
			if (threads_starting_up)
				starting = mono_g_hash_table_size (threads_starting_up) > 0;
			else
				starting = FALSE;
			mono_threads_unlock ();
			if (starting)
				mono_thread_info_sleep (100, NULL);
			else
				finished = TRUE;
		}
	}
}

typedef struct {
	MonoInternalThread *thread;
	MonoStackFrameInfo *frames;
	int nframes, max_frames;
	int nthreads, max_threads;
	MonoInternalThread **threads;
} ThreadDumpUserData;

static gboolean thread_dump_requested;

/* This needs to be async safe */
static gboolean
collect_frame (MonoStackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	ThreadDumpUserData *ud = (ThreadDumpUserData *)data;

	if (ud->nframes < ud->max_frames) {
		memcpy (&ud->frames [ud->nframes], frame, sizeof (MonoStackFrameInfo));
		ud->nframes ++;
	}

	return FALSE;
}

/* This needs to be async safe */
static SuspendThreadResult
get_thread_dump (MonoThreadInfo *info, gpointer ud)
{
	ThreadDumpUserData *user_data = (ThreadDumpUserData *)ud;
	MonoInternalThread *thread = user_data->thread;

#if 0
/* This no longer works with remote unwinding */
	g_string_append_printf (text, " tid=0x%p this=0x%p ", (gpointer)(gsize)thread->tid, thread);
	mono_thread_internal_describe (thread, text);
	g_string_append (text, "\n");
#endif

	if (thread == mono_thread_internal_current ())
		mono_get_eh_callbacks ()->mono_walk_stack_with_ctx (collect_frame, NULL, MONO_UNWIND_SIGNAL_SAFE, ud);
	else
		mono_get_eh_callbacks ()->mono_walk_stack_with_state (collect_frame, mono_thread_info_get_suspend_state (info), MONO_UNWIND_SIGNAL_SAFE, ud);

	return MonoResumeThread;
}

typedef struct {
	int nthreads, max_threads;
	MonoInternalThread **threads;
} CollectThreadsUserData;

static void
collect_thread (gpointer key, gpointer value, gpointer user)
{
	CollectThreadsUserData *ud = (CollectThreadsUserData *)user;
	MonoInternalThread *thread = (MonoInternalThread *)value;

	if (ud->nthreads < ud->max_threads)
		ud->threads [ud->nthreads ++] = thread;
}

/*
 * Collect running threads into the THREADS array.
 * THREADS should be an array allocated on the stack.
 */
static int
collect_threads (MonoInternalThread **thread_array, int max_threads)
{
	CollectThreadsUserData ud;

	memset (&ud, 0, sizeof (ud));
	/* This array contains refs, but its on the stack, so its ok */
	ud.threads = thread_array;
	ud.max_threads = max_threads;

	mono_threads_lock ();
	mono_g_hash_table_foreach (threads, collect_thread, &ud);
	mono_threads_unlock ();

	return ud.nthreads;
}

static void
dump_thread (MonoInternalThread *thread, ThreadDumpUserData *ud)
{
	GString* text = g_string_new (0);
	char *name;
	GError *error = NULL;
	int i;

	ud->thread = thread;
	ud->nframes = 0;

	/* Collect frames for the thread */
	if (thread == mono_thread_internal_current ()) {
		get_thread_dump (mono_thread_info_current (), ud);
	} else {
		mono_thread_info_safe_suspend_and_run (thread_get_tid (thread), FALSE, get_thread_dump, ud);
	}

	/*
	 * Do all the non async-safe work outside of get_thread_dump.
	 */
	if (thread->name) {
		name = g_utf16_to_utf8 (thread->name, thread->name_len, NULL, NULL, &error);
		g_assert (!error);
		g_string_append_printf (text, "\n\"%s\"", name);
		g_free (name);
	}
	else if (thread->threadpool_thread) {
		g_string_append (text, "\n\"<threadpool thread>\"");
	} else {
		g_string_append (text, "\n\"<unnamed thread>\"");
	}

	for (i = 0; i < ud->nframes; ++i) {
		MonoStackFrameInfo *frame = &ud->frames [i];
		MonoMethod *method = NULL;

		if (frame->type == FRAME_TYPE_MANAGED)
			method = mono_jit_info_get_method (frame->ji);

		if (method) {
			gchar *location = mono_debug_print_stack_frame (method, frame->native_offset, frame->domain);
			g_string_append_printf (text, "  %s\n", location);
			g_free (location);
		} else {
			g_string_append_printf (text, "  at <unknown> <0x%05x>\n", frame->native_offset);
		}
	}

	fprintf (stdout, "%s", text->str);

#if PLATFORM_WIN32 && TARGET_WIN32 && _DEBUG
	OutputDebugStringA(text->str);
#endif

	g_string_free (text, TRUE);
	fflush (stdout);
}

void
mono_threads_perform_thread_dump (void)
{
	ThreadDumpUserData ud;
	MonoInternalThread *thread_array [128];
	int tindex, nthreads;

	if (!thread_dump_requested)
		return;

	printf ("Full thread dump:\n");

	/* Make a copy of the threads hash to avoid doing work inside threads_lock () */
	nthreads = collect_threads (thread_array, 128);

	memset (&ud, 0, sizeof (ud));
	ud.frames = g_new0 (MonoStackFrameInfo, 256);
	ud.max_frames = 256;

	for (tindex = 0; tindex < nthreads; ++tindex)
		dump_thread (thread_array [tindex], &ud);

	g_free (ud.frames);

	thread_dump_requested = FALSE;
}

/* Obtain the thread dump of all threads */
static gboolean
mono_threads_get_thread_dump (MonoArray **out_threads, MonoArray **out_stack_frames, MonoError *error)
{

	ThreadDumpUserData ud;
	MonoInternalThread *thread_array [128];
	MonoDomain *domain = mono_domain_get ();
	MonoDebugSourceLocation *location;
	int tindex, nthreads;

	error_init (error);
	
	*out_threads = NULL;
	*out_stack_frames = NULL;

	/* Make a copy of the threads hash to avoid doing work inside threads_lock () */
	nthreads = collect_threads (thread_array, 128);

	memset (&ud, 0, sizeof (ud));
	ud.frames = g_new0 (MonoStackFrameInfo, 256);
	ud.max_frames = 256;

	*out_threads = mono_array_new_checked (domain, mono_defaults.thread_class, nthreads, error);
	if (!is_ok (error))
		goto leave;
	*out_stack_frames = mono_array_new_checked (domain, mono_defaults.array_class, nthreads, error);
	if (!is_ok (error))
		goto leave;

	for (tindex = 0; tindex < nthreads; ++tindex) {
		MonoInternalThread *thread = thread_array [tindex];
		MonoArray *thread_frames;
		int i;

		ud.thread = thread;
		ud.nframes = 0;

		/* Collect frames for the thread */
		if (thread == mono_thread_internal_current ()) {
			get_thread_dump (mono_thread_info_current (), &ud);
		} else {
			mono_thread_info_safe_suspend_and_run (thread_get_tid (thread), FALSE, get_thread_dump, &ud);
		}

		mono_array_setref_fast (*out_threads, tindex, mono_thread_current_for_thread (thread));

		thread_frames = mono_array_new_checked (domain, mono_defaults.stack_frame_class, ud.nframes, error);
		if (!is_ok (error))
			goto leave;
		mono_array_setref_fast (*out_stack_frames, tindex, thread_frames);

		for (i = 0; i < ud.nframes; ++i) {
			MonoStackFrameInfo *frame = &ud.frames [i];
			MonoMethod *method = NULL;
			MonoStackFrame *sf = (MonoStackFrame *)mono_object_new_checked (domain, mono_defaults.stack_frame_class, error);
			if (!is_ok (error))
				goto leave;

			sf->native_offset = frame->native_offset;

			if (frame->type == FRAME_TYPE_MANAGED)
				method = mono_jit_info_get_method (frame->ji);

			if (method) {
				sf->method_address = (gsize) frame->ji->code_start;

				MonoReflectionMethod *rm = mono_method_get_object_checked (domain, method, NULL, error);
				if (!is_ok (error))
					goto leave;
				MONO_OBJECT_SETREF (sf, method, rm);

				location = mono_debug_lookup_source_location (method, frame->native_offset, domain);
				if (location) {
					sf->il_offset = location->il_offset;

					if (location && location->source_file) {
						MonoString *filename = mono_string_new_checked (domain, location->source_file, error);
						if (!is_ok (error))
							goto leave;
						MONO_OBJECT_SETREF (sf, filename, filename);
						sf->line = location->row;
						sf->column = location->column;
					}
					mono_debug_free_source_location (location);
				} else {
					sf->il_offset = -1;
				}
			}
			mono_array_setref (thread_frames, i, sf);
		}
	}

leave:
	g_free (ud.frames);
	return is_ok (error);
}

/**
 * mono_threads_request_thread_dump:
 *
 *   Ask all threads except the current to print their stacktrace to stdout.
 */
void
mono_threads_request_thread_dump (void)
{
	/*The new thread dump code runs out of the finalizer thread. */
	thread_dump_requested = TRUE;
	mono_gc_finalize_notify ();
}

struct ref_stack {
	gpointer *refs;
	gint allocated; /* +1 so that refs [allocated] == NULL */
	gint bottom;
};

typedef struct ref_stack RefStack;

static RefStack *
ref_stack_new (gint initial_size)
{
	RefStack *rs;

	initial_size = MAX (initial_size, 16) + 1;
	rs = g_new0 (RefStack, 1);
	rs->refs = g_new0 (gpointer, initial_size);
	rs->allocated = initial_size;
	return rs;
}

static void
ref_stack_destroy (gpointer ptr)
{
	RefStack *rs = (RefStack *)ptr;

	if (rs != NULL) {
		g_free (rs->refs);
		g_free (rs);
	}
}

static void
ref_stack_push (RefStack *rs, gpointer ptr)
{
	g_assert (rs != NULL);

	if (rs->bottom >= rs->allocated) {
		rs->refs = (void **)g_realloc (rs->refs, rs->allocated * 2 * sizeof (gpointer) + 1);
		rs->allocated <<= 1;
		rs->refs [rs->allocated] = NULL;
	}
	rs->refs [rs->bottom++] = ptr;
}

static void
ref_stack_pop (RefStack *rs)
{
	if (rs == NULL || rs->bottom == 0)
		return;

	rs->bottom--;
	rs->refs [rs->bottom] = NULL;
}

static gboolean
ref_stack_find (RefStack *rs, gpointer ptr)
{
	gpointer *refs;

	if (rs == NULL)
		return FALSE;

	for (refs = rs->refs; refs && *refs; refs++) {
		if (*refs == ptr)
			return TRUE;
	}
	return FALSE;
}

/*
 * mono_thread_push_appdomain_ref:
 *
 *   Register that the current thread may have references to objects in domain 
 * @domain on its stack. Each call to this function should be paired with a 
 * call to pop_appdomain_ref.
 */
void 
mono_thread_push_appdomain_ref (MonoDomain *domain)
{
	MonoInternalThread *thread = mono_thread_internal_current ();

	if (thread) {
		/* printf ("PUSH REF: %"G_GSIZE_FORMAT" -> %s.\n", (gsize)thread->tid, domain->friendly_name); */
		SPIN_LOCK (thread->lock_thread_id);
		if (thread->appdomain_refs == NULL)
			thread->appdomain_refs = ref_stack_new (16);
		ref_stack_push ((RefStack *)thread->appdomain_refs, domain);
		SPIN_UNLOCK (thread->lock_thread_id);
	}
}

void
mono_thread_pop_appdomain_ref (void)
{
	MonoInternalThread *thread = mono_thread_internal_current ();

	if (thread) {
		/* printf ("POP REF: %"G_GSIZE_FORMAT" -> %s.\n", (gsize)thread->tid, ((MonoDomain*)(thread->appdomain_refs->data))->friendly_name); */
		SPIN_LOCK (thread->lock_thread_id);
		ref_stack_pop ((RefStack *)thread->appdomain_refs);
		SPIN_UNLOCK (thread->lock_thread_id);
	}
}

gboolean
mono_thread_internal_has_appdomain_ref (MonoInternalThread *thread, MonoDomain *domain)
{
	gboolean res;
	SPIN_LOCK (thread->lock_thread_id);
	res = ref_stack_find ((RefStack *)thread->appdomain_refs, domain);
	SPIN_UNLOCK (thread->lock_thread_id);
	return res;
}

gboolean
mono_thread_has_appdomain_ref (MonoThread *thread, MonoDomain *domain)
{
	return mono_thread_internal_has_appdomain_ref (thread->internal_thread, domain);
}

typedef struct abort_appdomain_data {
	struct wait_data wait;
	MonoDomain *domain;
} abort_appdomain_data;

static void
collect_appdomain_thread (gpointer key, gpointer value, gpointer user_data)
{
	MonoInternalThread *thread = (MonoInternalThread*)value;
	abort_appdomain_data *data = (abort_appdomain_data*)user_data;
	MonoDomain *domain = data->domain;

	if (mono_thread_internal_has_appdomain_ref (thread, domain)) {
		/* printf ("ABORTING THREAD %p BECAUSE IT REFERENCES DOMAIN %s.\n", thread->tid, domain->friendly_name); */

		if(data->wait.num<MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS) {
			data->wait.handles [data->wait.num] = mono_threads_open_thread_handle (thread->handle);
			data->wait.threads [data->wait.num] = thread;
			data->wait.num++;
		} else {
			/* Just ignore the rest, we can't do anything with
			 * them yet
			 */
		}
	}
}

/*
 * mono_threads_abort_appdomain_threads:
 *
 *   Abort threads which has references to the given appdomain.
 */
gboolean
mono_threads_abort_appdomain_threads (MonoDomain *domain, int timeout)
{
	abort_appdomain_data user_data;
	gint64 start_time;
	int orig_timeout = timeout;
	int i;

	THREAD_DEBUG (g_message ("%s: starting abort", __func__));

	start_time = mono_msec_ticks ();
	do {
		mono_threads_lock ();

		user_data.domain = domain;
		user_data.wait.num = 0;
		/* This shouldn't take any locks */
		mono_g_hash_table_foreach (threads, collect_appdomain_thread, &user_data);
		mono_threads_unlock ();

		if (user_data.wait.num > 0) {
			/* Abort the threads outside the threads lock */
			for (i = 0; i < user_data.wait.num; ++i)
				mono_thread_internal_abort (user_data.wait.threads [i]);

			/*
			 * We should wait for the threads either to abort, or to leave the
			 * domain. We can't do the latter, so we wait with a timeout.
			 */
			wait_for_tids (&user_data.wait, 100, FALSE);
		}

		/* Update remaining time */
		timeout -= mono_msec_ticks () - start_time;
		start_time = mono_msec_ticks ();

		if (orig_timeout != -1 && timeout < 0)
			return FALSE;
	}
	while (user_data.wait.num > 0);

	THREAD_DEBUG (g_message ("%s: abort done", __func__));

	return TRUE;
}

/*
 * mono_thread_get_undeniable_exception:
 *
 *   Return an exception which needs to be raised when leaving a catch clause.
 * This is used for undeniable exception propagation.
 */
MonoException*
mono_thread_get_undeniable_exception (void)
{
	MonoInternalThread *thread = mono_thread_internal_current ();

	if (!(thread && thread->abort_exc && !is_running_protected_wrapper ()))
		return NULL;

	// We don't want to have our exception effect calls made by
	// the catching block

	if (!mono_get_eh_callbacks ()->mono_above_abort_threshold ())
		return NULL;

	/*
	 * FIXME: Clear the abort exception and return an AppDomainUnloaded 
	 * exception if the thread no longer references a dying appdomain.
	 */ 
	thread->abort_exc->trace_ips = NULL;
	thread->abort_exc->stack_trace = NULL;
	return thread->abort_exc;
}

#if MONO_SMALL_CONFIG
#define NUM_STATIC_DATA_IDX 4
static const int static_data_size [NUM_STATIC_DATA_IDX] = {
	64, 256, 1024, 4096
};
#else
#define NUM_STATIC_DATA_IDX 8
static const int static_data_size [NUM_STATIC_DATA_IDX] = {
	1024, 4096, 16384, 65536, 262144, 1048576, 4194304, 16777216
};
#endif

static MonoBitSet *thread_reference_bitmaps [NUM_STATIC_DATA_IDX];
static MonoBitSet *context_reference_bitmaps [NUM_STATIC_DATA_IDX];

static void
mark_slots (void *addr, MonoBitSet **bitmaps, MonoGCMarkFunc mark_func, void *gc_data)
{
	gpointer *static_data = (gpointer *)addr;

	for (int i = 0; i < NUM_STATIC_DATA_IDX; ++i) {
		void **ptr = (void **)static_data [i];

		if (!ptr)
			continue;

		MONO_BITSET_FOREACH (bitmaps [i], idx, {
			void **p = ptr + idx;

			if (*p)
				mark_func ((MonoObject**)p, gc_data);
		});
	}
}

static void
mark_tls_slots (void *addr, MonoGCMarkFunc mark_func, void *gc_data)
{
	mark_slots (addr, thread_reference_bitmaps, mark_func, gc_data);
}

static void
mark_ctx_slots (void *addr, MonoGCMarkFunc mark_func, void *gc_data)
{
	mark_slots (addr, context_reference_bitmaps, mark_func, gc_data);
}

/*
 *  mono_alloc_static_data
 *
 *   Allocate memory blocks for storing threads or context static data
 */
static void 
mono_alloc_static_data (gpointer **static_data_ptr, guint32 offset, gboolean threadlocal)
{
	guint idx = ACCESS_SPECIAL_STATIC_OFFSET (offset, index);
	int i;

	gpointer* static_data = *static_data_ptr;
	if (!static_data) {
		static MonoGCDescriptor tls_desc = MONO_GC_DESCRIPTOR_NULL;
		static MonoGCDescriptor ctx_desc = MONO_GC_DESCRIPTOR_NULL;

		if (mono_gc_user_markers_supported ()) {
			if (tls_desc == MONO_GC_DESCRIPTOR_NULL)
				tls_desc = mono_gc_make_root_descr_user (mark_tls_slots);

			if (ctx_desc == MONO_GC_DESCRIPTOR_NULL)
				ctx_desc = mono_gc_make_root_descr_user (mark_ctx_slots);
		}

		static_data = (void **)mono_gc_alloc_fixed (static_data_size [0], threadlocal ? tls_desc : ctx_desc,
			threadlocal ? MONO_ROOT_SOURCE_THREAD_STATIC : MONO_ROOT_SOURCE_CONTEXT_STATIC,
			threadlocal ? "managed thread-static variables" : "managed context-static variables");
		*static_data_ptr = static_data;
		static_data [0] = static_data;
	}

	for (i = 1; i <= idx; ++i) {
		if (static_data [i])
			continue;

		if (mono_gc_user_markers_supported ())
			static_data [i] = g_malloc0 (static_data_size [i]);
		else
			static_data [i] = mono_gc_alloc_fixed (static_data_size [i], MONO_GC_DESCRIPTOR_NULL,
				threadlocal ? MONO_ROOT_SOURCE_THREAD_STATIC : MONO_ROOT_SOURCE_CONTEXT_STATIC,
				threadlocal ? "managed thread-static variables" : "managed context-static variables");
	}
}

static void 
mono_free_static_data (gpointer* static_data)
{
	int i;
	for (i = 1; i < NUM_STATIC_DATA_IDX; ++i) {
		gpointer p = static_data [i];
		if (!p)
			continue;
		/*
		 * At this point, the static data pointer array is still registered with the
		 * GC, so must ensure that mark_tls_slots() will not encounter any invalid
		 * data.  Freeing the individual arrays without first nulling their slots
		 * would make it possible for mark_tls/ctx_slots() to encounter a pointer to
		 * such an already freed array.  See bug #13813.
		 */
		static_data [i] = NULL;
		mono_memory_write_barrier ();
		if (mono_gc_user_markers_supported ())
			g_free (p);
		else
			mono_gc_free_fixed (p);
	}
	mono_gc_free_fixed (static_data);
}

/*
 *  mono_init_static_data_info
 *
 *   Initializes static data counters
 */
static void mono_init_static_data_info (StaticDataInfo *static_data)
{
	static_data->idx = 0;
	static_data->offset = 0;
	static_data->freelist = NULL;
}

/*
 *  mono_alloc_static_data_slot
 *
 *   Generates an offset for static data. static_data contains the counters
 *  used to generate it.
 */
static guint32
mono_alloc_static_data_slot (StaticDataInfo *static_data, guint32 size, guint32 align)
{
	if (!static_data->idx && !static_data->offset) {
		/* 
		 * we use the first chunk of the first allocation also as
		 * an array for the rest of the data 
		 */
		static_data->offset = sizeof (gpointer) * NUM_STATIC_DATA_IDX;
	}
	static_data->offset += align - 1;
	static_data->offset &= ~(align - 1);
	if (static_data->offset + size >= static_data_size [static_data->idx]) {
		static_data->idx ++;
		g_assert (size <= static_data_size [static_data->idx]);
		g_assert (static_data->idx < NUM_STATIC_DATA_IDX);
		static_data->offset = 0;
	}
	guint32 offset = MAKE_SPECIAL_STATIC_OFFSET (static_data->idx, static_data->offset, 0);
	static_data->offset += size;
	return offset;
}

/*
 * LOCKING: requires that threads_mutex is held
 */
static void
context_adjust_static_data (MonoAppContext *ctx)
{
	if (context_static_info.offset || context_static_info.idx > 0) {
		guint32 offset = MAKE_SPECIAL_STATIC_OFFSET (context_static_info.idx, context_static_info.offset, 0);
		mono_alloc_static_data (&ctx->static_data, offset, FALSE);
		ctx->data->static_data = ctx->static_data;
	}
}

/*
 * LOCKING: requires that threads_mutex is held
 */
static void 
alloc_thread_static_data_helper (gpointer key, gpointer value, gpointer user)
{
	MonoInternalThread *thread = (MonoInternalThread *)value;
	guint32 offset = GPOINTER_TO_UINT (user);

	mono_alloc_static_data (&(thread->static_data), offset, TRUE);
}

/*
 * LOCKING: requires that threads_mutex is held
 */
static void
alloc_context_static_data_helper (gpointer key, gpointer value, gpointer user)
{
	MonoAppContext *ctx = (MonoAppContext *) mono_gchandle_get_target (GPOINTER_TO_INT (key));

	if (!ctx)
		return;

	guint32 offset = GPOINTER_TO_UINT (user);
	mono_alloc_static_data (&ctx->static_data, offset, FALSE);
	ctx->data->static_data = ctx->static_data;
}

static StaticDataFreeList*
search_slot_in_freelist (StaticDataInfo *static_data, guint32 size, guint32 align)
{
	StaticDataFreeList* prev = NULL;
	StaticDataFreeList* tmp = static_data->freelist;
	while (tmp) {
		if (tmp->size == size) {
			if (prev)
				prev->next = tmp->next;
			else
				static_data->freelist = tmp->next;
			return tmp;
		}
		prev = tmp;
		tmp = tmp->next;
	}
	return NULL;
}

#if SIZEOF_VOID_P == 4
#define ONE_P 1
#else
#define ONE_P 1ll
#endif

static void
update_reference_bitmap (MonoBitSet **sets, guint32 offset, uintptr_t *bitmap, int numbits)
{
	int idx = ACCESS_SPECIAL_STATIC_OFFSET (offset, index);
	if (!sets [idx])
		sets [idx] = mono_bitset_new (static_data_size [idx] / sizeof (uintptr_t), 0);
	MonoBitSet *rb = sets [idx];
	offset = ACCESS_SPECIAL_STATIC_OFFSET (offset, offset);
	offset /= sizeof (uintptr_t);
	/* offset is now the bitmap offset */
	for (int i = 0; i < numbits; ++i) {
		if (bitmap [i / sizeof (uintptr_t)] & (ONE_P << (i & (sizeof (uintptr_t) * 8 -1))))
			mono_bitset_set_fast (rb, offset + i);
	}
}

static void
clear_reference_bitmap (MonoBitSet **sets, guint32 offset, guint32 size)
{
	int idx = ACCESS_SPECIAL_STATIC_OFFSET (offset, index);
	MonoBitSet *rb = sets [idx];
	offset = ACCESS_SPECIAL_STATIC_OFFSET (offset, offset);
	offset /= sizeof (uintptr_t);
	/* offset is now the bitmap offset */
	for (int i = 0; i < size / sizeof (uintptr_t); i++)
		mono_bitset_clear_fast (rb, offset + i);
}

guint32
mono_alloc_special_static_data (guint32 static_type, guint32 size, guint32 align, uintptr_t *bitmap, int numbits)
{
	g_assert (static_type == SPECIAL_STATIC_THREAD || static_type == SPECIAL_STATIC_CONTEXT);

	StaticDataInfo *info;
	MonoBitSet **sets;

	if (static_type == SPECIAL_STATIC_THREAD) {
		info = &thread_static_info;
		sets = thread_reference_bitmaps;
	} else {
		info = &context_static_info;
		sets = context_reference_bitmaps;
	}

	mono_threads_lock ();

	StaticDataFreeList *item = search_slot_in_freelist (info, size, align);
	guint32 offset;

	if (item) {
		offset = item->offset;
		g_free (item);
	} else {
		offset = mono_alloc_static_data_slot (info, size, align);
	}

	update_reference_bitmap (sets, offset, bitmap, numbits);

	if (static_type == SPECIAL_STATIC_THREAD) {
		/* This can be called during startup */
		if (threads != NULL)
			mono_g_hash_table_foreach (threads, alloc_thread_static_data_helper, GUINT_TO_POINTER (offset));
	} else {
		if (contexts != NULL)
			g_hash_table_foreach (contexts, alloc_context_static_data_helper, GUINT_TO_POINTER (offset));

		ACCESS_SPECIAL_STATIC_OFFSET (offset, type) = SPECIAL_STATIC_OFFSET_TYPE_CONTEXT;
	}

	mono_threads_unlock ();

	return offset;
}

gpointer
mono_get_special_static_data_for_thread (MonoInternalThread *thread, guint32 offset)
{
	guint32 static_type = ACCESS_SPECIAL_STATIC_OFFSET (offset, type);

	if (static_type == SPECIAL_STATIC_OFFSET_TYPE_THREAD) {
		return get_thread_static_data (thread, offset);
	} else {
		return get_context_static_data (thread->current_appcontext, offset);
	}
}

gpointer
mono_get_special_static_data (guint32 offset)
{
	return mono_get_special_static_data_for_thread (mono_thread_internal_current (), offset);
}

typedef struct {
	guint32 offset;
	guint32 size;
} OffsetSize;

/*
 * LOCKING: requires that threads_mutex is held
 */
static void 
free_thread_static_data_helper (gpointer key, gpointer value, gpointer user)
{
	MonoInternalThread *thread = (MonoInternalThread *)value;
	OffsetSize *data = (OffsetSize *)user;
	int idx = ACCESS_SPECIAL_STATIC_OFFSET (data->offset, index);
	int off = ACCESS_SPECIAL_STATIC_OFFSET (data->offset, offset);
	char *ptr;

	if (!thread->static_data || !thread->static_data [idx])
		return;
	ptr = ((char*) thread->static_data [idx]) + off;
	mono_gc_bzero_atomic (ptr, data->size);
}

/*
 * LOCKING: requires that threads_mutex is held
 */
static void
free_context_static_data_helper (gpointer key, gpointer value, gpointer user)
{
	MonoAppContext *ctx = (MonoAppContext *) mono_gchandle_get_target (GPOINTER_TO_INT (key));

	if (!ctx)
		return;

	OffsetSize *data = (OffsetSize *)user;
	int idx = ACCESS_SPECIAL_STATIC_OFFSET (data->offset, index);
	int off = ACCESS_SPECIAL_STATIC_OFFSET (data->offset, offset);
	char *ptr;

	if (!ctx->static_data || !ctx->static_data [idx])
		return;

	ptr = ((char*) ctx->static_data [idx]) + off;
	mono_gc_bzero_atomic (ptr, data->size);
}

static void
do_free_special_slot (guint32 offset, guint32 size)
{
	guint32 static_type = ACCESS_SPECIAL_STATIC_OFFSET (offset, type);
	MonoBitSet **sets;
	StaticDataInfo *info;

	if (static_type == SPECIAL_STATIC_OFFSET_TYPE_THREAD) {
		info = &thread_static_info;
		sets = thread_reference_bitmaps;
	} else {
		info = &context_static_info;
		sets = context_reference_bitmaps;
	}

	guint32 data_offset = offset;
	ACCESS_SPECIAL_STATIC_OFFSET (data_offset, type) = 0;
	OffsetSize data = { data_offset, size };

	clear_reference_bitmap (sets, data.offset, data.size);

	if (static_type == SPECIAL_STATIC_OFFSET_TYPE_THREAD) {
		if (threads != NULL)
			mono_g_hash_table_foreach (threads, free_thread_static_data_helper, &data);
	} else {
		if (contexts != NULL)
			g_hash_table_foreach (contexts, free_context_static_data_helper, &data);
	}

	if (!mono_runtime_is_shutting_down ()) {
		StaticDataFreeList *item = g_new0 (StaticDataFreeList, 1);

		item->offset = offset;
		item->size = size;

		item->next = info->freelist;
		info->freelist = item;
	}
}

static void
do_free_special (gpointer key, gpointer value, gpointer data)
{
	MonoClassField *field = (MonoClassField *)key;
	guint32 offset = GPOINTER_TO_UINT (value);
	gint32 align;
	guint32 size;
	size = mono_type_size (field->type, &align);
	do_free_special_slot (offset, size);
}

void
mono_alloc_special_static_data_free (GHashTable *special_static_fields)
{
	mono_threads_lock ();

	g_hash_table_foreach (special_static_fields, do_free_special, NULL);

	mono_threads_unlock ();
}

#ifdef HOST_WIN32
static void CALLBACK dummy_apc (ULONG_PTR param)
{
}
#endif

/*
 * mono_thread_execute_interruption
 * 
 * Performs the operation that the requested thread state requires (abort,
 * suspend or stop)
 */
static MonoException*
mono_thread_execute_interruption (void)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	MonoThread *sys_thread = mono_thread_current ();

	LOCK_THREAD (thread);

	/* MonoThread::interruption_requested can only be changed with atomics */
	if (!mono_thread_clear_interruption_requested (thread)) {
		UNLOCK_THREAD (thread);
		return NULL;
	}

	/* this will consume pending APC calls */
#ifdef HOST_WIN32
	WaitForSingleObjectEx (GetCurrentThread(), 0, TRUE);
#endif
	/* Clear the interrupted flag of the thread so it can wait again */
	mono_thread_info_clear_self_interrupt ();

	/* If there's a pending exception and an AbortRequested, the pending exception takes precedence */
	if (sys_thread->pending_exception) {
		MonoException *exc;

		exc = sys_thread->pending_exception;
		sys_thread->pending_exception = NULL;

		UNLOCK_THREAD (thread);
		return exc;
	} else if (thread->state & (ThreadState_AbortRequested)) {
		UNLOCK_THREAD (thread);
		g_assert (sys_thread->pending_exception == NULL);
		if (thread->abort_exc == NULL) {
			/* 
			 * This might be racy, but it has to be called outside the lock
			 * since it calls managed code.
			 */
			MONO_OBJECT_SETREF (thread, abort_exc, mono_get_exception_thread_abort ());
		}
		return thread->abort_exc;
	} else if (thread->state & (ThreadState_SuspendRequested)) {
		/* calls UNLOCK_THREAD (thread) */
		self_suspend_internal ();
		return NULL;
	} else if (thread->thread_interrupt_requested) {

		thread->thread_interrupt_requested = FALSE;
		UNLOCK_THREAD (thread);
		
		return(mono_get_exception_thread_interrupted ());
	}
	
	UNLOCK_THREAD (thread);
	
	return NULL;
}

/*
 * mono_thread_request_interruption
 *
 * A signal handler can call this method to request the interruption of a
 * thread. The result of the interruption will depend on the current state of
 * the thread. If the result is an exception that needs to be throw, it is 
 * provided as return value.
 */
MonoException*
mono_thread_request_interruption (gboolean running_managed)
{
	MonoInternalThread *thread = mono_thread_internal_current ();

	/* The thread may already be stopping */
	if (thread == NULL) 
		return NULL;

	if (!mono_thread_set_interruption_requested (thread))
		return NULL;

	if (!running_managed || is_running_protected_wrapper ()) {
		/* Can't stop while in unmanaged code. Increase the global interruption
		   request count. When exiting the unmanaged method the count will be
		   checked and the thread will be interrupted. */

		/* this will awake the thread if it is in WaitForSingleObject 
		   or similar */
		/* Our implementation of this function ignores the func argument */
#ifdef HOST_WIN32
		QueueUserAPC ((PAPCFUNC)dummy_apc, thread->native_handle, (ULONG_PTR)NULL);
#else
		mono_thread_info_self_interrupt ();
#endif
		return NULL;
	}
	else {
		return mono_thread_execute_interruption ();
	}
}

/*This function should be called by a thread after it has exited all of
 * its handle blocks at interruption time.*/
MonoException*
mono_thread_resume_interruption (gboolean exec)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	gboolean still_aborting;

	/* The thread may already be stopping */
	if (thread == NULL)
		return NULL;

	LOCK_THREAD (thread);
	still_aborting = (thread->state & (ThreadState_AbortRequested)) != 0;
	UNLOCK_THREAD (thread);

	/*This can happen if the protected block called Thread::ResetAbort*/
	if (!still_aborting)
		return NULL;

	if (!mono_thread_set_interruption_requested (thread))
		return NULL;

	mono_thread_info_self_interrupt ();

	if (exec)
		return mono_thread_execute_interruption ();
	else
		return NULL;
}

gboolean mono_thread_interruption_requested ()
{
	if (thread_interruption_requested) {
		MonoInternalThread *thread = mono_thread_internal_current ();
		/* The thread may already be stopping */
		if (thread != NULL) 
			return mono_thread_get_interruption_requested (thread);
	}
	return FALSE;
}

static MonoException*
mono_thread_interruption_checkpoint_request (gboolean bypass_abort_protection)
{
	MonoInternalThread *thread = mono_thread_internal_current ();

	/* The thread may already be stopping */
	if (!thread)
		return NULL;
	if (!mono_thread_get_interruption_requested (thread))
		return NULL;
	if (!bypass_abort_protection && is_running_protected_wrapper ())
		return NULL;

	return mono_thread_execute_interruption ();
}

/*
 * Performs the interruption of the current thread, if one has been requested,
 * and the thread is not running a protected wrapper.
 * Return the exception which needs to be thrown, if any.
 */
MonoException*
mono_thread_interruption_checkpoint (void)
{
	return mono_thread_interruption_checkpoint_request (FALSE);
}

/*
 * Performs the interruption of the current thread, if one has been requested.
 * Return the exception which needs to be thrown, if any.
 */
MonoException*
mono_thread_force_interruption_checkpoint_noraise (void)
{
	return mono_thread_interruption_checkpoint_request (TRUE);
}

/*
 * mono_set_pending_exception:
 *
 *   Set the pending exception of the current thread to EXC.
 * The exception will be thrown when execution returns to managed code.
 */
void
mono_set_pending_exception (MonoException *exc)
{
	MonoThread *thread = mono_thread_current ();

	/* The thread may already be stopping */
	if (thread == NULL)
		return;

	MONO_OBJECT_SETREF (thread, pending_exception, exc);

    mono_thread_request_interruption (FALSE);
}

/**
 * mono_thread_interruption_request_flag:
 *
 * Returns the address of a flag that will be non-zero if an interruption has
 * been requested for a thread. The thread to interrupt may not be the current
 * thread, so an additional call to mono_thread_interruption_requested() or
 * mono_thread_interruption_checkpoint() is allways needed if the flag is not
 * zero.
 */
gint32* mono_thread_interruption_request_flag ()
{
	return &thread_interruption_requested;
}

void 
mono_thread_init_apartment_state (void)
{
#ifdef HOST_WIN32
	MonoInternalThread* thread = mono_thread_internal_current ();

	/* Positive return value indicates success, either
	 * S_OK if this is first CoInitialize call, or
	 * S_FALSE if CoInitialize already called, but with same
	 * threading model. A negative value indicates failure,
	 * probably due to trying to change the threading model.
	 */
	if (CoInitializeEx(NULL, (thread->apartment_state == ThreadApartmentState_STA) 
			? COINIT_APARTMENTTHREADED 
			: COINIT_MULTITHREADED) < 0) {
		thread->apartment_state = ThreadApartmentState_Unknown;
	}
#endif
}

void 
mono_thread_cleanup_apartment_state (void)
{
#ifdef HOST_WIN32
	MonoInternalThread* thread = mono_thread_internal_current ();

	if (thread && thread->apartment_state != ThreadApartmentState_Unknown) {
		CoUninitialize ();
	}
#endif
}

void
mono_thread_set_state (MonoInternalThread *thread, MonoThreadState state)
{
	LOCK_THREAD (thread);
	thread->state |= state;
	UNLOCK_THREAD (thread);
}

/**
 * mono_thread_test_and_set_state:
 * Test if current state of \p thread include \p test. If it does not, OR \p set into the state.
 * \returns TRUE if \p set was OR'd in.
 */
gboolean
mono_thread_test_and_set_state (MonoInternalThread *thread, MonoThreadState test, MonoThreadState set)
{
	LOCK_THREAD (thread);

	if ((thread->state & test) != 0) {
		UNLOCK_THREAD (thread);
		return FALSE;
	}

	thread->state |= set;
	UNLOCK_THREAD (thread);

	return TRUE;
}

void
mono_thread_clr_state (MonoInternalThread *thread, MonoThreadState state)
{
	LOCK_THREAD (thread);
	thread->state &= ~state;
	UNLOCK_THREAD (thread);
}

gboolean
mono_thread_test_state (MonoInternalThread *thread, MonoThreadState test)
{
	gboolean ret = FALSE;

	LOCK_THREAD (thread);

	if ((thread->state & test) != 0) {
		ret = TRUE;
	}
	
	UNLOCK_THREAD (thread);
	
	return ret;
}

static void
self_interrupt_thread (void *_unused)
{
	MonoException *exc;
	MonoThreadInfo *info;

	exc = mono_thread_execute_interruption ();
	if (!exc) {
		if (mono_threads_is_coop_enabled ()) {
			/* We can return from an async call in coop, as
			 * it's simply called when exiting the safepoint */
			return;
		}

		g_error ("%s: we can't resume from an async call", __func__);
	}

	info = mono_thread_info_current ();

	/* We must use _with_context since we didn't trampoline into the runtime */
	mono_raise_exception_with_context (exc, &info->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX].ctx); /* FIXME using thread_saved_state [ASYNC_SUSPEND_STATE_INDEX] can race with another suspend coming in. */
}

static gboolean
mono_jit_info_match (MonoJitInfo *ji, gpointer ip)
{
	if (!ji)
		return FALSE;
	return ji->code_start <= ip && (char*)ip < (char*)ji->code_start + ji->code_size;
}

static gboolean
last_managed (MonoStackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	MonoJitInfo **dest = (MonoJitInfo **)data;
	*dest = frame->ji;
	return TRUE;
}

static MonoJitInfo*
mono_thread_info_get_last_managed (MonoThreadInfo *info)
{
	MonoJitInfo *ji = NULL;
	if (!info)
		return NULL;

	/*
	 * The suspended thread might be holding runtime locks. Make sure we don't try taking
	 * any runtime locks while unwinding. In coop case we shouldn't safepoint in regions
	 * where we hold runtime locks.
	 */
	if (!mono_threads_is_coop_enabled ())
		mono_thread_info_set_is_async_context (TRUE);
	mono_get_eh_callbacks ()->mono_walk_stack_with_state (last_managed, mono_thread_info_get_suspend_state (info), MONO_UNWIND_SIGNAL_SAFE, &ji);
	if (!mono_threads_is_coop_enabled ())
		mono_thread_info_set_is_async_context (FALSE);
	return ji;
}

typedef struct {
	MonoInternalThread *thread;
	gboolean install_async_abort;
	MonoThreadInfoInterruptToken *interrupt_token;
} AbortThreadData;

static SuspendThreadResult
async_abort_critical (MonoThreadInfo *info, gpointer ud)
{
	AbortThreadData *data = (AbortThreadData *)ud;
	MonoInternalThread *thread = data->thread;
	MonoJitInfo *ji = NULL;
	gboolean protected_wrapper;
	gboolean running_managed;

	if (mono_get_eh_callbacks ()->mono_install_handler_block_guard (mono_thread_info_get_suspend_state (info)))
		return MonoResumeThread;

	/*someone is already interrupting it*/
	if (!mono_thread_set_interruption_requested (thread))
		return MonoResumeThread;

	ji = mono_thread_info_get_last_managed (info);
	protected_wrapper = ji && !ji->is_trampoline && !ji->async && mono_threads_is_critical_method (mono_jit_info_get_method (ji));
	running_managed = mono_jit_info_match (ji, MONO_CONTEXT_GET_IP (&mono_thread_info_get_suspend_state (info)->ctx));

	if (!protected_wrapper && running_managed) {
		/*We are in managed code*/
		/*Set the thread to call */
		if (data->install_async_abort)
			mono_thread_info_setup_async_call (info, self_interrupt_thread, NULL);
		return MonoResumeThread;
	} else {
		/* 
		 * This will cause waits to be broken.
		 * It will also prevent the thread from entering a wait, so if the thread returns
		 * from the wait before it receives the abort signal, it will just spin in the wait
		 * functions in the io-layer until the signal handler calls QueueUserAPC which will
		 * make it return.
		 */
		data->interrupt_token = mono_thread_info_prepare_interrupt (info);

		return MonoResumeThread;
	}
}

static void
async_abort_internal (MonoInternalThread *thread, gboolean install_async_abort)
{
	AbortThreadData data;

	g_assert (thread != mono_thread_internal_current ());

	data.thread = thread;
	data.install_async_abort = install_async_abort;
	data.interrupt_token = NULL;

	mono_thread_info_safe_suspend_and_run (thread_get_tid (thread), TRUE, async_abort_critical, &data);
	if (data.interrupt_token)
		mono_thread_info_finish_interrupt (data.interrupt_token);
	/*FIXME we need to wait for interruption to complete -- figure out how much into interruption we should wait for here*/
}

static void
self_abort_internal (MonoError *error)
{
	MonoException *exc;

	error_init (error);

	/* FIXME this is insanely broken, it doesn't cause interruption to happen synchronously
	 * since passing FALSE to mono_thread_request_interruption makes sure it returns NULL */

	/*
	Self aborts ignore the protected block logic and raise the TAE regardless. This is verified by one of the tests in mono/tests/abort-cctor.cs.
	*/
	exc = mono_thread_request_interruption (TRUE);
	if (exc)
		mono_error_set_exception_instance (error, exc);
	else
		mono_thread_info_self_interrupt ();
}

typedef struct {
	MonoInternalThread *thread;
	gboolean interrupt;
	MonoThreadInfoInterruptToken *interrupt_token;
} SuspendThreadData;

static SuspendThreadResult
async_suspend_critical (MonoThreadInfo *info, gpointer ud)
{
	SuspendThreadData *data = (SuspendThreadData *)ud;
	MonoInternalThread *thread = data->thread;
	MonoJitInfo *ji = NULL;
	gboolean protected_wrapper;
	gboolean running_managed;

	ji = mono_thread_info_get_last_managed (info);
	protected_wrapper = ji && !ji->is_trampoline && !ji->async && mono_threads_is_critical_method (mono_jit_info_get_method (ji));
	running_managed = mono_jit_info_match (ji, MONO_CONTEXT_GET_IP (&mono_thread_info_get_suspend_state (info)->ctx));

	if (running_managed && !protected_wrapper) {
		if (mono_threads_is_coop_enabled ()) {
			mono_thread_info_setup_async_call (info, self_interrupt_thread, NULL);
			return MonoResumeThread;
		} else {
			thread->state &= ~ThreadState_SuspendRequested;
			thread->state |= ThreadState_Suspended;
			return KeepSuspended;
		}
	} else {
		mono_thread_set_interruption_requested (thread);
		if (data->interrupt)
			data->interrupt_token = mono_thread_info_prepare_interrupt ((MonoThreadInfo *)thread->thread_info);

		return MonoResumeThread;
	}
}

/* LOCKING: called with @thread synch_cs held, and releases it */
static void
async_suspend_internal (MonoInternalThread *thread, gboolean interrupt)
{
	SuspendThreadData data;

	g_assert (thread != mono_thread_internal_current ());

	// MOSTLY_ASYNC_SAFE_PRINTF ("ASYNC SUSPEND thread %p\n", thread_get_tid (thread));

	thread->self_suspended = FALSE;

	data.thread = thread;
	data.interrupt = interrupt;
	data.interrupt_token = NULL;

	mono_thread_info_safe_suspend_and_run (thread_get_tid (thread), interrupt, async_suspend_critical, &data);
	if (data.interrupt_token)
		mono_thread_info_finish_interrupt (data.interrupt_token);

	UNLOCK_THREAD (thread);
}

/* LOCKING: called with @thread synch_cs held, and releases it */
static void
self_suspend_internal (void)
{
	MonoInternalThread *thread;
	MonoOSEvent *event;
	MonoOSEventWaitRet res;

	thread = mono_thread_internal_current ();

	// MOSTLY_ASYNC_SAFE_PRINTF ("SELF SUSPEND thread %p\n", thread_get_tid (thread));

	thread->self_suspended = TRUE;

	thread->state &= ~ThreadState_SuspendRequested;
	thread->state |= ThreadState_Suspended;

	UNLOCK_THREAD (thread);

	event = thread->suspended;

	MONO_ENTER_GC_SAFE;
	res = mono_os_event_wait_one (event, MONO_INFINITE_WAIT, TRUE);
	g_assert (res == MONO_OS_EVENT_WAIT_RET_SUCCESS_0 || res == MONO_OS_EVENT_WAIT_RET_ALERTED);
	MONO_EXIT_GC_SAFE;
}

static void
suspend_for_shutdown_async_call (gpointer unused)
{
	for (;;)
		mono_thread_info_yield ();
}

static SuspendThreadResult
suspend_for_shutdown_critical (MonoThreadInfo *info, gpointer unused)
{
	mono_thread_info_setup_async_call (info, suspend_for_shutdown_async_call, NULL);
	return MonoResumeThread;
}

void
mono_thread_internal_suspend_for_shutdown (MonoInternalThread *thread)
{
	g_assert (thread != mono_thread_internal_current ());

	mono_thread_info_safe_suspend_and_run (thread_get_tid (thread), FALSE, suspend_for_shutdown_critical, NULL);
}

/**
 * mono_thread_is_foreign:
 * \param thread the thread to query
 *
 * This function allows one to determine if a thread was created by the mono runtime and has
 * a well defined lifecycle or it's a foreign one, created by the native environment.
 *
 * \returns TRUE if \p thread was not created by the runtime.
 */
mono_bool
mono_thread_is_foreign (MonoThread *thread)
{
	MonoThreadInfo *info = (MonoThreadInfo *)thread->internal_thread->thread_info;
	return info->runtime_thread == FALSE;
}

/*
 * mono_add_joinable_thread:
 *
 *   Add TID to the list of joinable threads.
 * LOCKING: Acquires the threads lock.
 */
void
mono_threads_add_joinable_thread (gpointer tid)
{
#ifndef HOST_WIN32
	/*
	 * We cannot detach from threads because it causes problems like
	 * 2fd16f60/r114307. So we collect them and join them when
	 * we have time (in he finalizer thread).
	 */
	joinable_threads_lock ();
	if (!joinable_threads)
		joinable_threads = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (joinable_threads, tid, tid);
	joinable_thread_count ++;
	joinable_threads_unlock ();

	mono_gc_finalize_notify ();
#endif
}

/*
 * mono_threads_join_threads:
 *
 *   Join all joinable threads. This is called from the finalizer thread.
 * LOCKING: Acquires the threads lock.
 */
void
mono_threads_join_threads (void)
{
#ifndef HOST_WIN32
	GHashTableIter iter;
	gpointer key;
	gpointer tid;
	pthread_t thread;
	gboolean found;

	/* Fastpath */
	if (!joinable_thread_count)
		return;

	while (TRUE) {
		joinable_threads_lock ();
		found = FALSE;
		if (g_hash_table_size (joinable_threads)) {
			g_hash_table_iter_init (&iter, joinable_threads);
			g_hash_table_iter_next (&iter, &key, (void**)&tid);
			thread = (pthread_t)tid;
			g_hash_table_remove (joinable_threads, key);
			joinable_thread_count --;
			found = TRUE;
		}
		joinable_threads_unlock ();
		if (found) {
			if (thread != pthread_self ()) {
				MONO_ENTER_GC_SAFE;
				/* This shouldn't block */
				mono_threads_join_lock ();
				mono_native_thread_join (thread);
				mono_threads_join_unlock ();
				MONO_EXIT_GC_SAFE;
			}
		} else {
			break;
		}
	}
#endif
}

/*
 * mono_thread_join:
 *
 *   Wait for thread TID to exit.
 * LOCKING: Acquires the threads lock.
 */
void
mono_thread_join (gpointer tid)
{
#ifndef HOST_WIN32
	pthread_t thread;
	gboolean found = FALSE;

	joinable_threads_lock ();
	if (!joinable_threads)
		joinable_threads = g_hash_table_new (NULL, NULL);
	if (g_hash_table_lookup (joinable_threads, tid)) {
		g_hash_table_remove (joinable_threads, tid);
		joinable_thread_count --;
		found = TRUE;
	}
	joinable_threads_unlock ();
	if (!found)
		return;
	thread = (pthread_t)tid;
	MONO_ENTER_GC_SAFE;
	mono_native_thread_join (thread);
	MONO_EXIT_GC_SAFE;
#endif
}

void
mono_thread_internal_unhandled_exception (MonoObject* exc)
{
	MonoClass *klass = exc->vtable->klass;
	if (is_threadabort_exception (klass)) {
		mono_thread_internal_reset_abort (mono_thread_internal_current ());
	} else if (!is_appdomainunloaded_exception (klass)
		&& mono_runtime_unhandled_exception_policy_get () == MONO_UNHANDLED_POLICY_CURRENT) {
		mono_unhandled_exception (exc);
		if (mono_environment_exitcode_get () == 1) {
			mono_environment_exitcode_set (255);
			mono_invoke_unhandled_exception_hook (exc);
			g_assert_not_reached ();
		}
	}
}

void
ves_icall_System_Threading_Thread_GetStackTraces (MonoArray **out_threads, MonoArray **out_stack_traces)
{
	MonoError error;
	mono_threads_get_thread_dump (out_threads, out_stack_traces, &error);
	mono_error_set_pending_exception (&error);
}

/*
 * mono_threads_attach_coop: called by native->managed wrappers
 *
 *  - @dummy:
 *    - blocking mode: contains gc unsafe transition cookie
 *    - non-blocking mode: contains random data
 *  - @return: the original domain which needs to be restored, or NULL.
 */
gpointer
mono_threads_attach_coop (MonoDomain *domain, gpointer *dummy)
{
	MonoDomain *orig;
	MonoThreadInfo *info;
	gboolean external;

	orig = mono_domain_get ();

	if (!domain) {
		/* Happens when called from AOTed code which is only used in the root domain. */
		domain = mono_get_root_domain ();
		g_assert (domain);
	}

	/* On coop, when we detached, we moved the thread from  RUNNING->BLOCKING.
	 * If we try to reattach we do a BLOCKING->RUNNING transition.  If the thread
	 * is fresh, mono_thread_attach() will do a STARTING->RUNNING transition so
	 * we're only responsible for making the cookie. */
	if (mono_threads_is_blocking_transition_enabled ())
		external = !(info = mono_thread_info_current_unchecked ()) || !mono_thread_info_is_live (info);

	if (!mono_thread_internal_current ()) {
		mono_thread_attach_full (domain, FALSE);

		// #678164
		mono_thread_set_state (mono_thread_internal_current (), ThreadState_Background);
	}

	if (orig != domain)
		mono_domain_set (domain, TRUE);

	if (mono_threads_is_blocking_transition_enabled ()) {
		if (external) {
			/* mono_thread_attach put the thread in RUNNING mode from STARTING, but we need to
			 * return the right cookie. */
			*dummy = mono_threads_enter_gc_unsafe_region_cookie ();
		} else {
			/* thread state (BLOCKING|RUNNING) -> RUNNING */
			*dummy = mono_threads_enter_gc_unsafe_region (dummy);
		}
	}

	return orig;
}

/*
 * mono_threads_detach_coop: called by native->managed wrappers
 *
 *  - @cookie: the original domain which needs to be restored, or NULL.
 *  - @dummy:
 *    - blocking mode: contains gc unsafe transition cookie
 *    - non-blocking mode: contains random data
 */
void
mono_threads_detach_coop (gpointer cookie, gpointer *dummy)
{
	MonoDomain *domain, *orig;

	orig = (MonoDomain*) cookie;

	domain = mono_domain_get ();
	g_assert (domain);

	if (mono_threads_is_blocking_transition_enabled ()) {
		/* it won't do anything if cookie is NULL
		 * thread state RUNNING -> (RUNNING|BLOCKING) */
		mono_threads_exit_gc_unsafe_region (*dummy, dummy);
	}

	if (orig != domain) {
		if (!orig)
			mono_domain_unset ();
		else
			mono_domain_set (orig, TRUE);
	}
}

#if 0
/* Returns TRUE if the current thread is ready to be interrupted. */
gboolean
mono_threads_is_ready_to_be_interrupted (void)
{
	MonoInternalThread *thread;

	thread = mono_thread_internal_current ();
	LOCK_THREAD (thread);
	if (thread->state & (ThreadState_SuspendRequested | ThreadState_AbortRequested)) {
		UNLOCK_THREAD (thread);
		return FALSE;
	}

	if (mono_thread_get_abort_prot_block_count (thread) || mono_get_eh_callbacks ()->mono_current_thread_has_handle_block_guard ()) {
		UNLOCK_THREAD (thread);
		return FALSE;
	}

	UNLOCK_THREAD (thread);
	return TRUE;
}
#endif

void
mono_thread_internal_describe (MonoInternalThread *internal, GString *text)
{
	g_string_append_printf (text, ", thread handle : %p", internal->handle);

	if (internal->thread_info) {
		g_string_append (text, ", state : ");
		mono_thread_info_describe_interrupt_token ((MonoThreadInfo*) internal->thread_info, text);
	}

	if (internal->owned_mutexes) {
		int i;

		g_string_append (text, ", owns : [");
		for (i = 0; i < internal->owned_mutexes->len; i++)
			g_string_append_printf (text, i == 0 ? "%p" : ", %p", g_ptr_array_index (internal->owned_mutexes, i));
		g_string_append (text, "]");
	}
}

gboolean
mono_thread_internal_is_current (MonoInternalThread *internal)
{
	g_assert (internal);
	return mono_native_thread_id_equals (mono_native_thread_id_get (), MONO_UINT_TO_NATIVE_THREAD_ID (internal->tid));
}
