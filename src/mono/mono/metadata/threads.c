/*
 * threads.c: Thread support internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *	Paolo Molaro (lupus@ximian.com)
 *	Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */

#include <config.h>

#include <glib.h>
#include <signal.h>
#include <string.h>

#if defined(__OpenBSD__) || defined(__FreeBSD__)
#include <pthread.h>
#include <pthread_np.h>
#endif

#include <mono/metadata/object.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threadpool.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/runtime.h>
#include <mono/io-layer/io-layer.h>
#ifndef HOST_WIN32
#include <mono/io-layer/threads.h>
#endif
#include <mono/metadata/object-internals.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/atomic.h>

#include <mono/metadata/gc-internal.h>

#ifdef PLATFORM_ANDROID
#include <errno.h>

extern int tkill (pid_t tid, int signal);
#endif

#if defined(PLATFORM_MACOSX) && defined(HAVE_PTHREAD_GET_STACKADDR_NP)
void *pthread_get_stackaddr_np(pthread_t);
size_t pthread_get_stacksize_np(pthread_t);
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

/* Provide this for systems with glib < 2.6 */
#ifndef G_GSIZE_FORMAT
#   if GLIB_SIZEOF_LONG == 8
#       define G_GSIZE_FORMAT "lu"
#   else
#       define G_GSIZE_FORMAT "u"
#   endif
#endif

struct StartInfo 
{
	guint32 (*func)(void *);
	MonoThread *obj;
	MonoObject *delegate;
	void *start_arg;
};

typedef union {
	gint32 ival;
	gfloat fval;
} IntFloatUnion;

typedef union {
	gint64 ival;
	gdouble fval;
} LongDoubleUnion;
 
typedef struct _MonoThreadDomainTls MonoThreadDomainTls;
struct _MonoThreadDomainTls {
	MonoThreadDomainTls *next;
	guint32 offset;
	guint32 size;
};

typedef struct {
	int idx;
	int offset;
	MonoThreadDomainTls *freelist;
} StaticDataInfo;

/* Number of cached culture objects in the MonoThread->cached_culture_info array
 * (per-type): we use the first NUM entries for CultureInfo and the last for
 * UICultureInfo. So the size of the array is really NUM_CACHED_CULTURES * 2.
 */
#define NUM_CACHED_CULTURES 4
#define CULTURES_START_IDX 0
#define UICULTURES_START_IDX NUM_CACHED_CULTURES

/* Controls access to the 'threads' hash table */
#define mono_threads_lock() EnterCriticalSection (&threads_mutex)
#define mono_threads_unlock() LeaveCriticalSection (&threads_mutex)
static CRITICAL_SECTION threads_mutex;

/* Controls access to context static data */
#define mono_contexts_lock() EnterCriticalSection (&contexts_mutex)
#define mono_contexts_unlock() LeaveCriticalSection (&contexts_mutex)
static CRITICAL_SECTION contexts_mutex;

/* Holds current status of static data heap */
static StaticDataInfo thread_static_info;
static StaticDataInfo context_static_info;

/* The hash of existing threads (key is thread ID, value is
 * MonoInternalThread*) that need joining before exit
 */
static MonoGHashTable *threads=NULL;

/*
 * Threads which are starting up and they are not in the 'threads' hash yet.
 * When handle_store is called for a thread, it will be removed from this hash table.
 * Protected by mono_threads_lock ().
 */
static MonoGHashTable *threads_starting_up = NULL;
 
/* Maps a MonoThread to its start argument */
/* Protected by mono_threads_lock () */
static MonoGHashTable *thread_start_args = NULL;

/* The TLS key that holds the MonoObject assigned to each thread */
static MonoNativeTlsKey current_object_key;

#ifdef MONO_HAVE_FAST_TLS
/* we need to use both the Tls* functions and __thread because
 * the gc needs to see all the threads 
 */
MONO_FAST_TLS_DECLARE(tls_current_object);
#define SET_CURRENT_OBJECT(x) do { \
	MONO_FAST_TLS_SET (tls_current_object, x); \
	mono_native_tls_set_value (current_object_key, x); \
} while (FALSE)
#define GET_CURRENT_OBJECT() ((MonoInternalThread*) MONO_FAST_TLS_GET (tls_current_object))
#else
#define SET_CURRENT_OBJECT(x) mono_native_tls_set_value (current_object_key, x)
#define GET_CURRENT_OBJECT() (MonoInternalThread*) mono_native_tls_get_value (current_object_key)
#endif

/* function called at thread start */
static MonoThreadStartCB mono_thread_start_cb = NULL;

/* function called at thread attach */
static MonoThreadAttachCB mono_thread_attach_cb = NULL;

/* function called at thread cleanup */
static MonoThreadCleanupFunc mono_thread_cleanup_fn = NULL;

/* function called to notify the runtime about a pending exception on the current thread */
static MonoThreadNotifyPendingExcFunc mono_thread_notify_pending_exc_fn = NULL;

/* The default stack size for each thread */
static guint32 default_stacksize = 0;
#define default_stacksize_for_thread(thread) ((thread)->stack_size? (thread)->stack_size: default_stacksize)

static void thread_adjust_static_data (MonoInternalThread *thread);
static void mono_free_static_data (gpointer* static_data, gboolean threadlocal);
static void mono_init_static_data_info (StaticDataInfo *static_data);
static guint32 mono_alloc_static_data_slot (StaticDataInfo *static_data, guint32 size, guint32 align);
static gboolean mono_thread_resume (MonoInternalThread* thread);
static void mono_thread_start (MonoThread *thread);
static void signal_thread_state_change (MonoInternalThread *thread);
static void abort_thread_internal (MonoInternalThread *thread, gboolean can_raise_exception, gboolean install_async_abort);
static void suspend_thread_internal (MonoInternalThread *thread, gboolean interrupt);
static void self_suspend_internal (MonoInternalThread *thread);
static gboolean resume_thread_internal (MonoInternalThread *thread);

static MonoException* mono_thread_execute_interruption (MonoInternalThread *thread);
static void ref_stack_destroy (gpointer rs);

/* Spin lock for InterlockedXXX 64 bit functions */
#define mono_interlocked_lock() EnterCriticalSection (&interlocked_mutex)
#define mono_interlocked_unlock() LeaveCriticalSection (&interlocked_mutex)
static CRITICAL_SECTION interlocked_mutex;

/* global count of thread interruptions requested */
static gint32 thread_interruption_requested = 0;

/* Event signaled when a thread changes its background mode */
static HANDLE background_change_event;

static gboolean shutting_down = FALSE;

static gint32 managed_thread_id_counter = 0;

static guint32
get_next_managed_thread_id (void)
{
	return InterlockedIncrement (&managed_thread_id_counter);
}

MonoNativeTlsKey
mono_thread_get_tls_key (void)
{
	return current_object_key;
}

gint32
mono_thread_get_tls_offset (void)
{
	int offset;
	MONO_THREAD_VAR_OFFSET (tls_current_object,offset);
	return offset;
}

/* handle_store() and handle_remove() manage the array of threads that
 * still need to be waited for when the main thread exits.
 *
 * If handle_store() returns FALSE the thread must not be started
 * because Mono is shutting down.
 */
static gboolean handle_store(MonoThread *thread, gboolean force_attach)
{
	mono_threads_lock ();

	THREAD_DEBUG (g_message ("%s: thread %p ID %"G_GSIZE_FORMAT, __func__, thread, (gsize)thread->internal_thread->tid));

	if (threads_starting_up)
		mono_g_hash_table_remove (threads_starting_up, thread);

	if (shutting_down && !force_attach) {
		mono_threads_unlock ();
		return FALSE;
	}

	if(threads==NULL) {
		MONO_GC_REGISTER_ROOT_FIXED (threads);
		threads=mono_g_hash_table_new_type (NULL, NULL, MONO_HASH_VALUE_GC);
	}

	/* We don't need to duplicate thread->handle, because it is
	 * only closed when the thread object is finalized by the GC.
	 */
	g_assert (thread->internal_thread);
	mono_g_hash_table_insert(threads, (gpointer)(gsize)(thread->internal_thread->tid),
				 thread->internal_thread);

	mono_threads_unlock ();

	return TRUE;
}

static gboolean handle_remove(MonoInternalThread *thread)
{
	gboolean ret;
	gsize tid = thread->tid;

	THREAD_DEBUG (g_message ("%s: thread ID %"G_GSIZE_FORMAT, __func__, tid));

	mono_threads_lock ();

	if (threads) {
		/* We have to check whether the thread object for the
		 * tid is still the same in the table because the
		 * thread might have been destroyed and the tid reused
		 * in the meantime, in which case the tid would be in
		 * the table, but with another thread object.
		 */
		if (mono_g_hash_table_lookup (threads, (gpointer)tid) == thread) {
			mono_g_hash_table_remove (threads, (gpointer)tid);
			ret = TRUE;
		} else {
			ret = FALSE;
		}
	}
	else
		ret = FALSE;
	
	mono_threads_unlock ();

	/* Don't close the handle here, wait for the object finalizer
	 * to do it. Otherwise, the following race condition applies:
	 *
	 * 1) Thread exits (and handle_remove() closes the handle)
	 *
	 * 2) Some other handle is reassigned the same slot
	 *
	 * 3) Another thread tries to join the first thread, and
	 * blocks waiting for the reassigned handle to be signalled
	 * (which might never happen).  This is possible, because the
	 * thread calling Join() still has a reference to the first
	 * thread's object.
	 */
	return ret;
}

static void ensure_synch_cs_set (MonoInternalThread *thread)
{
	CRITICAL_SECTION *synch_cs;
	
	if (thread->synch_cs != NULL) {
		return;
	}
	
	synch_cs = g_new0 (CRITICAL_SECTION, 1);
	InitializeCriticalSection (synch_cs);
	
	if (InterlockedCompareExchangePointer ((gpointer *)&thread->synch_cs,
					       synch_cs, NULL) != NULL) {
		/* Another thread must have installed this CS */
		DeleteCriticalSection (synch_cs);
		g_free (synch_cs);
	}
}

/*
 * NOTE: this function can be called also for threads different from the current one:
 * make sure no code called from it will ever assume it is run on the thread that is
 * getting cleaned up.
 */
static void thread_cleanup (MonoInternalThread *thread)
{
	g_assert (thread != NULL);

	if (thread->abort_state_handle) {
		mono_gchandle_free (thread->abort_state_handle);
		thread->abort_state_handle = 0;
	}
	thread->abort_exc = NULL;
	thread->current_appcontext = NULL;

	/*
	 * This is necessary because otherwise we might have
	 * cross-domain references which will not get cleaned up when
	 * the target domain is unloaded.
	 */
	if (thread->cached_culture_info) {
		int i;
		for (i = 0; i < NUM_CACHED_CULTURES * 2; ++i)
			mono_array_set (thread->cached_culture_info, MonoObject*, i, NULL);
	}

	ensure_synch_cs_set (thread);

	EnterCriticalSection (thread->synch_cs);

	thread->state |= ThreadState_Stopped;
	thread->state &= ~ThreadState_Background;

	LeaveCriticalSection (thread->synch_cs);

	/*
	An interruption request has leaked to cleanup. Adjust the global counter.

	This can happen is the abort source thread finds the abortee (this) thread
	in unmanaged code. If this thread never trips back to managed code or check
	the local flag it will be left set and positively unbalance the global counter.
	
	Leaving the counter unbalanced will cause a performance degradation since all threads
	will now keep checking their local flags all the time.
	*/
	if (InterlockedExchange (&thread->interruption_requested, 0))
		InterlockedDecrement (&thread_interruption_requested);

	/* if the thread is not in the hash it has been removed already */
	if (!handle_remove (thread)) {
		if (thread == mono_thread_internal_current ()) {
			mono_domain_unset ();
			mono_memory_barrier ();
		}
		/* This needs to be called even if handle_remove () fails */
		if (mono_thread_cleanup_fn)
			mono_thread_cleanup_fn (thread);
		return;
	}
	mono_release_type_locks (thread);

	mono_profiler_thread_end (thread->tid);

	if (thread == mono_thread_internal_current ()) {
		/*
		 * This will signal async signal handlers that the thread has exited.
		 * The profiler callback needs this to be set, so it cannot be done earlier.
		 */
		mono_domain_unset ();
		mono_memory_barrier ();
	}

	if (thread == mono_thread_internal_current ())
		mono_thread_pop_appdomain_ref ();

	thread->cached_culture_info = NULL;

	mono_free_static_data (thread->static_data, TRUE);
	thread->static_data = NULL;
	ref_stack_destroy (thread->appdomain_refs);
	thread->appdomain_refs = NULL;

	if (mono_thread_cleanup_fn)
		mono_thread_cleanup_fn (thread);

	if (mono_gc_is_moving ()) {
		MONO_GC_UNREGISTER_ROOT (thread->thread_pinning_ref);
		thread->thread_pinning_ref = NULL;
	}
		
}

static gpointer
get_thread_static_data (MonoInternalThread *thread, guint32 offset)
{
	int idx;
	g_assert ((offset & 0x80000000) == 0);
	offset &= 0x7fffffff;
	idx = (offset >> 24) - 1;
	return ((char*) thread->static_data [idx]) + (offset & 0xffffff);
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

	return get_thread_static_data (thread, offset);
}

static void
set_current_thread_for_domain (MonoDomain *domain, MonoInternalThread *thread, MonoThread *current)
{
	MonoThread **current_thread_ptr = get_current_thread_ptr_for_domain (domain, thread);

	g_assert (current->obj.vtable->domain == domain);

	g_assert (!*current_thread_ptr);
	*current_thread_ptr = current;
}

static MonoInternalThread*
create_internal_thread_object (void)
{
	MonoVTable *vt = mono_class_vtable (mono_get_root_domain (), mono_defaults.internal_thread_class);
	return (MonoInternalThread*)mono_gc_alloc_mature (vt);
}

static MonoThread*
create_thread_object (MonoDomain *domain)
{
	MonoVTable *vt = mono_class_vtable (domain, mono_defaults.thread_class);
	return (MonoThread*)mono_gc_alloc_mature (vt);
}

static MonoThread*
new_thread_with_internal (MonoDomain *domain, MonoInternalThread *internal)
{
	MonoThread *thread = create_thread_object (domain);
	MONO_OBJECT_SETREF (thread, internal_thread, internal);
	return thread;
}

static void
init_root_domain_thread (MonoInternalThread *thread, MonoThread *candidate)
{
	MonoDomain *domain = mono_get_root_domain ();

	if (!candidate || candidate->obj.vtable->domain != domain)
		candidate = new_thread_with_internal (domain, thread);
	set_current_thread_for_domain (domain, thread, candidate);
	g_assert (!thread->root_domain_thread);
	MONO_OBJECT_SETREF (thread, root_domain_thread, candidate);
}

static guint32 WINAPI start_wrapper_internal(void *data)
{
	MonoThreadInfo *info;
	struct StartInfo *start_info=(struct StartInfo *)data;
	guint32 (*start_func)(void *);
	void *start_arg;
	gsize tid;
	/* 
	 * We don't create a local to hold start_info->obj, so hopefully it won't get pinned during a
	 * GC stack walk.
	 */
	MonoInternalThread *internal = start_info->obj->internal_thread;
	MonoObject *start_delegate = start_info->delegate;
	MonoDomain *domain = start_info->obj->obj.vtable->domain;

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Start wrapper", __func__, GetCurrentThreadId ()));

	/* We can be sure start_info->obj->tid and
	 * start_info->obj->handle have been set, because the thread
	 * was created suspended, and these values were set before the
	 * thread resumed
	 */

	info = mono_thread_info_current ();
	g_assert (info);
	internal->thread_info = info;


	tid=internal->tid;

	SET_CURRENT_OBJECT (internal);

	mono_monitor_init_tls ();

	/* Every thread references the appdomain which created it */
	mono_thread_push_appdomain_ref (domain);
	
	if (!mono_domain_set (domain, FALSE)) {
		/* No point in raising an appdomain_unloaded exception here */
		/* FIXME: Cleanup here */
		mono_thread_pop_appdomain_ref ();
		return 0;
	}

	start_func = start_info->func;
	start_arg = start_info->start_arg;

	/* We have to do this here because mono_thread_new_init()
	   requires that root_domain_thread is set up. */
	thread_adjust_static_data (internal);
	init_root_domain_thread (internal, start_info->obj);

	/* This MUST be called before any managed code can be
	 * executed, as it calls the callback function that (for the
	 * jit) sets the lmf marker.
	 */
	mono_thread_new_init (tid, &tid, start_func);
	internal->stack_ptr = &tid;

	LIBGC_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT",%d) Setting thread stack to %p", __func__, GetCurrentThreadId (), getpid (), thread->stack_ptr));

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Setting current_object_key to %p", __func__, GetCurrentThreadId (), internal));

	/* On 2.0 profile (and higher), set explicitly since state might have been
	   Unknown */
	if (internal->apartment_state == ThreadApartmentState_Unknown)
		internal->apartment_state = ThreadApartmentState_MTA;

	mono_thread_init_apartment_state ();

	if(internal->start_notify!=NULL) {
		/* Let the thread that called Start() know we're
		 * ready
		 */
		ReleaseSemaphore (internal->start_notify, 1, NULL);
	}

	mono_threads_lock ();
	mono_g_hash_table_remove (thread_start_args, start_info->obj);
	mono_threads_unlock ();

	mono_thread_set_execution_context (start_info->obj->ec_to_set);
	start_info->obj->ec_to_set = NULL;

	g_free (start_info);
	THREAD_DEBUG (g_message ("%s: start_wrapper for %"G_GSIZE_FORMAT, __func__,
							 internal->tid));

	/* 
	 * Call this after calling start_notify, since the profiler callback might want
	 * to lock the thread, and the lock is held by thread_start () which waits for
	 * start_notify.
	 */
	mono_profiler_thread_start (tid);

	/* start_func is set only for unmanaged start functions */
	if (start_func) {
		start_func (start_arg);
	} else {
		void *args [1];
		g_assert (start_delegate != NULL);
		args [0] = start_arg;
		/* we may want to handle the exception here. See comment below on unhandled exceptions */
		mono_runtime_delegate_invoke (start_delegate, args, NULL);
	}

	/* If the thread calls ExitThread at all, this remaining code
	 * will not be executed, but the main thread will eventually
	 * call thread_cleanup() on this thread's behalf.
	 */

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Start wrapper terminating", __func__, GetCurrentThreadId ()));

	/* Do any cleanup needed for apartment state. This
	 * cannot be done in thread_cleanup since thread_cleanup could be 
	 * called for a thread other than the current thread.
	 * mono_thread_cleanup_apartment_state cleans up apartment
	 * for the current thead */
	mono_thread_cleanup_apartment_state ();

	thread_cleanup (internal);

	/* Remove the reference to the thread object in the TLS data,
	 * so the thread object can be finalized.  This won't be
	 * reached if the thread threw an uncaught exception, so those
	 * thread handles will stay referenced :-( (This is due to
	 * missing support for scanning thread-specific data in the
	 * Boehm GC - the io-layer keeps a GC-visible hash of pointers
	 * to TLS data.)
	 */
	SET_CURRENT_OBJECT (NULL);

	return(0);
}

static guint32 WINAPI start_wrapper(void *data)
{
	volatile int dummy;

	/* Avoid scanning the frames above this frame during a GC */
	mono_gc_set_stack_end ((void*)&dummy);

	return start_wrapper_internal (data);
}

void mono_thread_new_init (intptr_t tid, gpointer stack_start, gpointer func)
{
	if (mono_thread_start_cb) {
		mono_thread_start_cb (tid, stack_start, func);
	}
}

void mono_threads_set_default_stacksize (guint32 stacksize)
{
	default_stacksize = stacksize;
}

guint32 mono_threads_get_default_stacksize (void)
{
	return default_stacksize;
}

/*
 * mono_create_thread:
 *
 *   This is a wrapper around CreateThread which handles differences in the type of
 * the the 'tid' argument.
 */
gpointer mono_create_thread (WapiSecurityAttributes *security,
							 guint32 stacksize, WapiThreadStart start,
							 gpointer param, guint32 create, gsize *tid)
{
	gpointer res;

#ifdef HOST_WIN32
	DWORD real_tid;

	res = mono_threads_CreateThread (security, stacksize, start, param, create, &real_tid);
	if (tid)
		*tid = real_tid;
#else
	res = CreateThread (security, stacksize, start, param, create, tid);
#endif

	return res;
}

/* 
 * The thread start argument may be an object reference, and there is
 * no ref to keep it alive when the new thread is started but not yet
 * registered with the collector. So we store it in a GC tracked hash
 * table.
 *
 * LOCKING: Assumes the threads lock is held.
 */
static void
register_thread_start_argument (MonoThread *thread, struct StartInfo *start_info)
{
	if (thread_start_args == NULL) {
		MONO_GC_REGISTER_ROOT_FIXED (thread_start_args);
		thread_start_args = mono_g_hash_table_new (NULL, NULL);
	}
	mono_g_hash_table_insert (thread_start_args, thread, start_info->start_arg);
}

/*
 * mono_thread_create_internal:
 * 
 * If NO_DETACH is TRUE, then the thread is not detached using pthread_detach (). This is needed to fix the race condition where waiting for a thred to exit only waits for its exit event to be
 * signalled, which can cause shutdown crashes if the thread shutdown code accesses data already freed by the runtime shutdown.
 * Currently, this is only used for the finalizer thread.
 */
MonoInternalThread*
mono_thread_create_internal (MonoDomain *domain, gpointer func, gpointer arg, gboolean threadpool_thread, gboolean no_detach, guint32 stack_size)
{
	MonoThread *thread;
	MonoInternalThread *internal;
	HANDLE thread_handle;
	struct StartInfo *start_info;
	gsize tid;
	guint32 create_flags;

	thread = create_thread_object (domain);
	internal = create_internal_thread_object ();
	MONO_OBJECT_SETREF (thread, internal_thread, internal);

	start_info=g_new0 (struct StartInfo, 1);
	start_info->func = func;
	start_info->obj = thread;
	start_info->start_arg = arg;

	mono_threads_lock ();
	if (shutting_down) {
		mono_threads_unlock ();
		g_free (start_info);
		return NULL;
	}
	if (threads_starting_up == NULL) {
		MONO_GC_REGISTER_ROOT_FIXED (threads_starting_up);
		threads_starting_up = mono_g_hash_table_new_type (NULL, NULL, MONO_HASH_KEY_VALUE_GC);
	}

	register_thread_start_argument (thread, start_info);
 	mono_g_hash_table_insert (threads_starting_up, thread, thread);
	mono_threads_unlock ();	

	if (stack_size == 0)
		stack_size = default_stacksize_for_thread (internal);

	/* Create suspended, so we can do some housekeeping before the thread
	 * starts
	 */
	create_flags = CREATE_SUSPENDED;
#ifndef HOST_WIN32
	if (no_detach)
		create_flags |= CREATE_NO_DETACH;
#endif
	thread_handle = mono_create_thread (NULL, stack_size, (LPTHREAD_START_ROUTINE)start_wrapper, start_info,
				     create_flags, &tid);
	THREAD_DEBUG (g_message ("%s: Started thread ID %"G_GSIZE_FORMAT" (handle %p)", __func__, tid, thread_handle));
	if (thread_handle == NULL) {
		/* The thread couldn't be created, so throw an exception */
		mono_threads_lock ();
		mono_g_hash_table_remove (threads_starting_up, thread);
		mono_threads_unlock ();
		g_free (start_info);
		mono_raise_exception (mono_get_exception_execution_engine ("Couldn't create thread"));
		return NULL;
	}

	internal->handle=thread_handle;
	internal->tid=tid;
	internal->apartment_state=ThreadApartmentState_Unknown;
	internal->managed_id = get_next_managed_thread_id ();
	if (mono_gc_is_moving ()) {
		internal->thread_pinning_ref = internal;
		MONO_GC_REGISTER_ROOT_PINNING (internal->thread_pinning_ref);
	}

	internal->synch_cs = g_new0 (CRITICAL_SECTION, 1);
	InitializeCriticalSection (internal->synch_cs);

	internal->threadpool_thread = threadpool_thread;
	if (threadpool_thread)
		mono_thread_set_state (internal, ThreadState_Background);

	if (handle_store (thread, FALSE))
		ResumeThread (thread_handle);

	/* Check that the managed and unmanaged layout of MonoInternalThread matches */
	if (mono_check_corlib_version () == NULL)
		g_assert (((char*)&internal->unused2 - (char*)internal) == mono_defaults.internal_thread_class->fields [mono_defaults.internal_thread_class->field.count - 1].offset);

	return internal;
}

void
mono_thread_create (MonoDomain *domain, gpointer func, gpointer arg)
{
	mono_thread_create_internal (domain, func, arg, FALSE, FALSE, 0);
}

#if defined(HOST_WIN32) && defined(__GNUC__)
static __inline__ __attribute__((always_inline))
/* This is not defined by gcc */
unsigned long long
__readfsdword (unsigned long long offset)
{
	unsigned long long value;
	__asm__("movl %%fs:%a[offset], %k[value]" : [value] "=q" (value) : [offset] "irm" (offset));
	return value;
}
#endif

/*
 * mono_thread_get_stack_bounds:
 *
 *   Return the address and size of the current threads stack. Return NULL as the 
 * stack address if the stack address cannot be determined.
 */
void
mono_thread_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
#if defined(HAVE_PTHREAD_GET_STACKSIZE_NP) && defined(HAVE_PTHREAD_GET_STACKADDR_NP)
	*staddr = (guint8*)pthread_get_stackaddr_np (pthread_self ());
	*stsize = pthread_get_stacksize_np (pthread_self ());

	/* staddr points to the start of the stack, not the end */
	*staddr -= *stsize;
	*staddr = (guint8*)((gssize)*staddr & ~(mono_pagesize () - 1));
	return;
	/* FIXME: simplify the mess below */
#elif defined(HOST_WIN32)
	/* http://en.wikipedia.org/wiki/Win32_Thread_Information_Block */
	void* tib = (void*)__readfsdword(0x18);
	guint8 *stackTop = (guint8*)*(int*)((char*)tib + 4);
	guint8 *stackBottom = (guint8*)*(int*)((char*)tib + 8);

	*staddr = stackBottom;
	*stsize = stackTop - stackBottom;
	return;
#else
	pthread_attr_t attr;
	guint8 *current = (guint8*)&attr;

	*staddr = NULL;
	*stsize = (size_t)-1;

	pthread_attr_init (&attr);
#  ifdef HAVE_PTHREAD_GETATTR_NP
	pthread_getattr_np (pthread_self(), &attr);
#  else
#    ifdef HAVE_PTHREAD_ATTR_GET_NP
	pthread_attr_get_np (pthread_self(), &attr);
#    elif defined(sun)
	*staddr = NULL;
	pthread_attr_getstacksize (&attr, &stsize);
#    elif defined(__OpenBSD__)
	stack_t ss;
	int rslt;

	rslt = pthread_stackseg_np(pthread_self(), &ss);
	g_assert (rslt == 0);

	*staddr = (guint8*)((size_t)ss.ss_sp - ss.ss_size);
	*stsize = ss.ss_size;
#    else
	*staddr = NULL;
	*stsize = 0;
	return;
#    endif
#  endif

#  if !defined(sun)
#    if defined(__native_client__)
	*staddr = NULL;
	pthread_attr_getstacksize (&attr, &stsize);
#    elif !defined(__OpenBSD__)
	pthread_attr_getstack (&attr, (void**)staddr, stsize);
#    endif
	if (*staddr)
		g_assert ((current > *staddr) && (current < *staddr + *stsize));
#  endif

	pthread_attr_destroy (&attr);
#endif

	/* When running under emacs, sometimes staddr is not aligned to a page size */
	*staddr = (guint8*)((gssize)*staddr & ~(mono_pagesize () - 1));
}

MonoThread *
mono_thread_attach (MonoDomain *domain)
{
	return mono_thread_attach_full (domain, FALSE);
}

MonoThread *
mono_thread_attach_full (MonoDomain *domain, gboolean force_attach)
{
	MonoThreadInfo *info;
	MonoInternalThread *thread;
	MonoThread *current_thread;
	HANDLE thread_handle;
	gsize tid;

	if ((thread = mono_thread_internal_current ())) {
		if (domain != mono_domain_get ())
			mono_domain_set (domain, TRUE);
		/* Already attached */
		return mono_thread_current ();
	}

	if (!mono_gc_register_thread (&domain)) {
		g_error ("Thread %"G_GSIZE_FORMAT" calling into managed code is not registered with the GC. On UNIX, this can be fixed by #include-ing <gc.h> before <pthread.h> in the file containing the thread creation code.", GetCurrentThreadId ());
	}

	thread = create_internal_thread_object ();

	thread_handle = GetCurrentThread ();
	g_assert (thread_handle);

	tid=GetCurrentThreadId ();

	/* 
	 * The handle returned by GetCurrentThread () is a pseudo handle, so it can't be used to
	 * refer to the thread from other threads for things like aborting.
	 */
	DuplicateHandle (GetCurrentProcess (), thread_handle, GetCurrentProcess (), &thread_handle, 
					 THREAD_ALL_ACCESS, TRUE, 0);

	thread->handle=thread_handle;
	thread->tid=tid;
#ifdef PLATFORM_ANDROID
	thread->android_tid = (gpointer) gettid ();
#endif
	thread->apartment_state=ThreadApartmentState_Unknown;
	thread->managed_id = get_next_managed_thread_id ();
	if (mono_gc_is_moving ()) {
		thread->thread_pinning_ref = thread;
		MONO_GC_REGISTER_ROOT_PINNING (thread->thread_pinning_ref);
	}

	thread->stack_ptr = &tid;

	thread->synch_cs = g_new0 (CRITICAL_SECTION, 1);
	InitializeCriticalSection (thread->synch_cs);

	THREAD_DEBUG (g_message ("%s: Attached thread ID %"G_GSIZE_FORMAT" (handle %p)", __func__, tid, thread_handle));

	info = mono_thread_info_current ();
	g_assert (info);
	thread->thread_info = info;

	current_thread = new_thread_with_internal (domain, thread);

	if (!handle_store (current_thread, force_attach)) {
		/* Mono is shutting down, so just wait for the end */
		for (;;)
			Sleep (10000);
	}

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Setting current_object_key to %p", __func__, GetCurrentThreadId (), thread));

	SET_CURRENT_OBJECT (thread);
	mono_domain_set (domain, TRUE);

	mono_monitor_init_tls ();

	thread_adjust_static_data (thread);

	init_root_domain_thread (thread, current_thread);
	if (domain != mono_get_root_domain ())
		set_current_thread_for_domain (domain, thread, current_thread);


	if (mono_thread_attach_cb) {
		guint8 *staddr;
		size_t stsize;

		mono_thread_get_stack_bounds (&staddr, &stsize);

		if (staddr == NULL)
			mono_thread_attach_cb (tid, &tid);
		else
			mono_thread_attach_cb (tid, staddr + stsize);
	}

	// FIXME: Need a separate callback
	mono_profiler_thread_start (tid);

	return current_thread;
}

void
mono_thread_detach (MonoThread *thread)
{
	g_return_if_fail (thread != NULL);

	THREAD_DEBUG (g_message ("%s: mono_thread_detach for %p (%"G_GSIZE_FORMAT")", __func__, thread, (gsize)thread->internal_thread->tid));

	thread_cleanup (thread->internal_thread);

	SET_CURRENT_OBJECT (NULL);
	mono_domain_unset ();

	/* Don't need to CloseHandle this thread, even though we took a
	 * reference in mono_thread_attach (), because the GC will do it
	 * when the Thread object is finalised.
	 */
}

void
mono_thread_exit ()
{
	MonoInternalThread *thread = mono_thread_internal_current ();

	THREAD_DEBUG (g_message ("%s: mono_thread_exit for %p (%"G_GSIZE_FORMAT")", __func__, thread, (gsize)thread->tid));

	thread_cleanup (thread);
	SET_CURRENT_OBJECT (NULL);
	mono_domain_unset ();

	/* we could add a callback here for embedders to use. */
	if (mono_thread_get_main () && (thread == mono_thread_get_main ()->internal_thread))
		exit (mono_environment_exitcode_get ());
	ExitThread (-1);
}

void
ves_icall_System_Threading_Thread_ConstructInternalThread (MonoThread *this)
{
	MonoInternalThread *internal = create_internal_thread_object ();

	internal->state = ThreadState_Unstarted;
	internal->apartment_state = ThreadApartmentState_Unknown;
	internal->managed_id = get_next_managed_thread_id ();

	InterlockedCompareExchangePointer ((gpointer)&this->internal_thread, internal, NULL);
}

HANDLE ves_icall_System_Threading_Thread_Thread_internal(MonoThread *this,
							 MonoObject *start)
{
	guint32 (*start_func)(void *);
	struct StartInfo *start_info;
	HANDLE thread;
	gsize tid;
	MonoInternalThread *internal;

	THREAD_DEBUG (g_message("%s: Trying to start a new thread: this (%p) start (%p)", __func__, this, start));

	if (!this->internal_thread)
		ves_icall_System_Threading_Thread_ConstructInternalThread (this);
	internal = this->internal_thread;

	ensure_synch_cs_set (internal);

	EnterCriticalSection (internal->synch_cs);

	if ((internal->state & ThreadState_Unstarted) == 0) {
		LeaveCriticalSection (internal->synch_cs);
		mono_raise_exception (mono_get_exception_thread_state ("Thread has already been started."));
		return NULL;
	}

	if ((internal->state & ThreadState_Aborted) != 0) {
		LeaveCriticalSection (internal->synch_cs);
		return this;
	}
	start_func = NULL;
	{
		/* This is freed in start_wrapper */
		start_info = g_new0 (struct StartInfo, 1);
		start_info->func = start_func;
		start_info->start_arg = this->start_obj; /* FIXME: GC object stored in unmanaged memory */
		start_info->delegate = start;
		start_info->obj = this;
		g_assert (this->obj.vtable->domain == mono_domain_get ());

		internal->start_notify=CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
		if (internal->start_notify==NULL) {
			LeaveCriticalSection (internal->synch_cs);
			g_warning ("%s: CreateSemaphore error 0x%x", __func__, GetLastError ());
			g_free (start_info);
			return(NULL);
		}

		mono_threads_lock ();
		register_thread_start_argument (this, start_info);
		if (threads_starting_up == NULL) {
			MONO_GC_REGISTER_ROOT_FIXED (threads_starting_up);
			threads_starting_up = mono_g_hash_table_new_type (NULL, NULL, MONO_HASH_KEY_VALUE_GC);
		}
		mono_g_hash_table_insert (threads_starting_up, this, this);
		mono_threads_unlock ();	

		thread=mono_create_thread(NULL, default_stacksize_for_thread (internal), (LPTHREAD_START_ROUTINE)start_wrapper, start_info,
				    CREATE_SUSPENDED, &tid);
		if(thread==NULL) {
			LeaveCriticalSection (internal->synch_cs);
			mono_threads_lock ();
			mono_g_hash_table_remove (threads_starting_up, this);
			mono_threads_unlock ();
			g_warning("%s: CreateThread error 0x%x", __func__, GetLastError());
			return(NULL);
		}
		
		internal->handle=thread;
		internal->tid=tid;
		if (mono_gc_is_moving ()) {
			internal->thread_pinning_ref = internal;
			MONO_GC_REGISTER_ROOT_PINNING (internal->thread_pinning_ref);
		}
		

		/* Don't call handle_store() here, delay it to Start.
		 * We can't join a thread (trying to will just block
		 * forever) until it actually starts running, so don't
		 * store the handle till then.
		 */

		mono_thread_start (this);
		
		internal->state &= ~ThreadState_Unstarted;

		THREAD_DEBUG (g_message ("%s: Started thread ID %"G_GSIZE_FORMAT" (handle %p)", __func__, tid, thread));

		LeaveCriticalSection (internal->synch_cs);
		return(thread);
	}
}

void ves_icall_System_Threading_InternalThread_Thread_free_internal (MonoInternalThread *this, HANDLE thread)
{
	MONO_ARCH_SAVE_REGS;

	THREAD_DEBUG (g_message ("%s: Closing thread %p, handle %p", __func__, this, thread));

	if (thread)
		CloseHandle (thread);

	if (this->synch_cs) {
		CRITICAL_SECTION *synch_cs = this->synch_cs;
		this->synch_cs = NULL;
		DeleteCriticalSection (synch_cs);
		g_free (synch_cs);
	}

	if (this->name) {
		void *name = this->name;
		this->name = NULL;
		g_free (name);
	}
}

static void mono_thread_start (MonoThread *thread)
{
	MonoInternalThread *internal = thread->internal_thread;

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Launching thread %p (%"G_GSIZE_FORMAT")", __func__, GetCurrentThreadId (), internal, (gsize)internal->tid));

	/* Only store the handle when the thread is about to be
	 * launched, to avoid the main thread deadlocking while trying
	 * to clean up a thread that will never be signalled.
	 */
	if (!handle_store (thread, FALSE))
		return;

	ResumeThread (internal->handle);

	if(internal->start_notify!=NULL) {
		/* Wait for the thread to set up its TLS data etc, so
		 * theres no potential race condition if someone tries
		 * to look up the data believing the thread has
		 * started
		 */

		THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") waiting for thread %p (%"G_GSIZE_FORMAT") to start", __func__, GetCurrentThreadId (), internal, (gsize)internal->tid));

		WaitForSingleObjectEx (internal->start_notify, INFINITE, FALSE);
		CloseHandle (internal->start_notify);
		internal->start_notify = NULL;
	}

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Done launching thread %p (%"G_GSIZE_FORMAT")", __func__, GetCurrentThreadId (), internal, (gsize)internal->tid));
}

void ves_icall_System_Threading_Thread_Sleep_internal(gint32 ms)
{
	guint32 res;
	MonoInternalThread *thread = mono_thread_internal_current ();

	THREAD_DEBUG (g_message ("%s: Sleeping for %d ms", __func__, ms));

	mono_thread_current_check_pending_interrupt ();
	
	while (TRUE) {
		mono_thread_set_state (thread, ThreadState_WaitSleepJoin);
	
		res = SleepEx(ms,TRUE);
	
		mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);

		if (res == WAIT_IO_COMPLETION) { /* we might have been interrupted */
			MonoException* exc = mono_thread_execute_interruption (thread);
			if (exc) {
				mono_raise_exception (exc);
			} else {
				// FIXME: !INFINITE
				if (ms != INFINITE)
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
	MONO_ARCH_SAVE_REGS;

	return mono_domain_get()->domain_id;
}

gboolean 
ves_icall_System_Threading_Thread_Yield (void)
{
#ifdef HOST_WIN32
	return SwitchToThread ();
#else
	return sched_yield () == 0;
#endif
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

	ensure_synch_cs_set (this_obj);
	
	EnterCriticalSection (this_obj->synch_cs);
	
	if (!this_obj->name) {
		*name_len = 0;
		res = NULL;
	} else {
		*name_len = this_obj->name_len;
		res = g_new (gunichar2, this_obj->name_len);
		memcpy (res, this_obj->name, sizeof (gunichar2) * this_obj->name_len);
	}
	
	LeaveCriticalSection (this_obj->synch_cs);

	return res;
}

MonoString* 
ves_icall_System_Threading_Thread_GetName_internal (MonoInternalThread *this_obj)
{
	MonoString* str;

	ensure_synch_cs_set (this_obj);
	
	EnterCriticalSection (this_obj->synch_cs);
	
	if (!this_obj->name)
		str = NULL;
	else
		str = mono_string_new_utf16 (mono_domain_get (), this_obj->name, this_obj->name_len);
	
	LeaveCriticalSection (this_obj->synch_cs);
	
	return str;
}

void 
mono_thread_set_name_internal (MonoInternalThread *this_obj, MonoString *name, gboolean managed)
{
	ensure_synch_cs_set (this_obj);
	
	EnterCriticalSection (this_obj->synch_cs);

	if (this_obj->flags & MONO_THREAD_FLAG_NAME_SET) {
		LeaveCriticalSection (this_obj->synch_cs);
		
		mono_raise_exception (mono_get_exception_invalid_operation ("Thread.Name can only be set once."));
		return;
	}
	if (name) {
		this_obj->name = g_new (gunichar2, mono_string_length (name));
		memcpy (this_obj->name, mono_string_chars (name), mono_string_length (name) * 2);
		this_obj->name_len = mono_string_length (name);
	}
	else
		this_obj->name = NULL;

	if (managed)
		this_obj->flags |= MONO_THREAD_FLAG_NAME_SET;
	
	LeaveCriticalSection (this_obj->synch_cs);
	if (this_obj->name) {
		char *tname = mono_string_to_utf8 (name);
		mono_profiler_thread_name (this_obj->tid, tname);
		mono_free (tname);
	}
}

void 
ves_icall_System_Threading_Thread_SetName_internal (MonoInternalThread *this_obj, MonoString *name)
{
	mono_thread_set_name_internal (this_obj, name, TRUE);
}

/* If the array is already in the requested domain, we just return it,
   otherwise we return a copy in that domain. */
static MonoArray*
byte_array_to_domain (MonoArray *arr, MonoDomain *domain)
{
	MonoArray *copy;

	if (!arr)
		return NULL;

	if (mono_object_domain (arr) == domain)
		return arr;

	copy = mono_array_new (domain, mono_defaults.byte_class, arr->max_length);
	mono_gc_memmove (mono_array_addr (copy, guint8, 0), mono_array_addr (arr, guint8, 0), arr->max_length);
	return copy;
}

MonoArray*
ves_icall_System_Threading_Thread_ByteArrayToRootDomain (MonoArray *arr)
{
	return byte_array_to_domain (arr, mono_get_root_domain ());
}

MonoArray*
ves_icall_System_Threading_Thread_ByteArrayToCurrentDomain (MonoArray *arr)
{
	return byte_array_to_domain (arr, mono_domain_get ());
}

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
		*current_thread_ptr = new_thread_with_internal (domain, internal);
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

gboolean ves_icall_System_Threading_Thread_Join_internal(MonoInternalThread *this,
							 int ms, HANDLE thread)
{
	MonoInternalThread *cur_thread = mono_thread_internal_current ();
	gboolean ret;

	mono_thread_current_check_pending_interrupt ();

	ensure_synch_cs_set (this);
	
	EnterCriticalSection (this->synch_cs);
	
	if ((this->state & ThreadState_Unstarted) != 0) {
		LeaveCriticalSection (this->synch_cs);
		
		mono_raise_exception (mono_get_exception_thread_state ("Thread has not been started."));
		return FALSE;
	}

	LeaveCriticalSection (this->synch_cs);

	if(ms== -1) {
		ms=INFINITE;
	}
	THREAD_DEBUG (g_message ("%s: joining thread handle %p, %d ms", __func__, thread, ms));
	
	mono_thread_set_state (cur_thread, ThreadState_WaitSleepJoin);

	ret=WaitForSingleObjectEx (thread, ms, TRUE);

	mono_thread_clr_state (cur_thread, ThreadState_WaitSleepJoin);
	
	if(ret==WAIT_OBJECT_0) {
		THREAD_DEBUG (g_message ("%s: join successful", __func__));

		return(TRUE);
	}
	
	THREAD_DEBUG (g_message ("%s: join failed", __func__));

	return(FALSE);
}

static gint32
mono_wait_uninterrupted (MonoInternalThread *thread, gboolean multiple, guint32 numhandles, gpointer *handles, gboolean waitall, gint32 ms, gboolean alertable)
{
	MonoException *exc;
	guint32 ret;
	gint64 start;
	gint32 diff_ms;
	gint32 wait = ms;

	start = (ms == -1) ? 0 : mono_100ns_ticks ();
	do {
		if (multiple)
			ret = WaitForMultipleObjectsEx (numhandles, handles, waitall, wait, alertable);
		else
			ret = WaitForSingleObjectEx (handles [0], ms, alertable);

		if (ret != WAIT_IO_COMPLETION)
			break;

		exc = mono_thread_execute_interruption (thread);
		if (exc)
			mono_raise_exception (exc);

		if (ms == -1)
			continue;

		/* Re-calculate ms according to the time passed */
		diff_ms = (mono_100ns_ticks () - start) / 10000;
		if (diff_ms >= ms) {
			ret = WAIT_TIMEOUT;
			break;
		}
		wait = ms - diff_ms;
	} while (TRUE);
	
	return ret;
}

/* FIXME: exitContext isnt documented */
gboolean ves_icall_System_Threading_WaitHandle_WaitAll_internal(MonoArray *mono_handles, gint32 ms, gboolean exitContext)
{
	HANDLE *handles;
	guint32 numhandles;
	guint32 ret;
	guint32 i;
	MonoObject *waitHandle;
	MonoInternalThread *thread = mono_thread_internal_current ();

	/* Do this WaitSleepJoin check before creating objects */
	mono_thread_current_check_pending_interrupt ();

	numhandles = mono_array_length(mono_handles);
	handles = g_new0(HANDLE, numhandles);

	for(i = 0; i < numhandles; i++) {	
		waitHandle = mono_array_get(mono_handles, MonoObject*, i);
		handles [i] = mono_wait_handle_get_handle ((MonoWaitHandle *) waitHandle);
	}
	
	if(ms== -1) {
		ms=INFINITE;
	}

	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);
	
	ret = mono_wait_uninterrupted (thread, TRUE, numhandles, handles, TRUE, ms, TRUE);

	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);

	g_free(handles);

	if(ret==WAIT_FAILED) {
		THREAD_WAIT_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Wait failed", __func__, GetCurrentThreadId ()));
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT) {
		THREAD_WAIT_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Wait timed out", __func__, GetCurrentThreadId ()));
		return(FALSE);
	}
	
	return(TRUE);
}

/* FIXME: exitContext isnt documented */
gint32 ves_icall_System_Threading_WaitHandle_WaitAny_internal(MonoArray *mono_handles, gint32 ms, gboolean exitContext)
{
	HANDLE handles [MAXIMUM_WAIT_OBJECTS];
	guint32 numhandles;
	guint32 ret;
	guint32 i;
	MonoObject *waitHandle;
	MonoInternalThread *thread = mono_thread_internal_current ();

	/* Do this WaitSleepJoin check before creating objects */
	mono_thread_current_check_pending_interrupt ();

	numhandles = mono_array_length(mono_handles);
	if (numhandles > MAXIMUM_WAIT_OBJECTS)
		return WAIT_FAILED;

	for(i = 0; i < numhandles; i++) {	
		waitHandle = mono_array_get(mono_handles, MonoObject*, i);
		handles [i] = mono_wait_handle_get_handle ((MonoWaitHandle *) waitHandle);
	}
	
	if(ms== -1) {
		ms=INFINITE;
	}

	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);

	ret = mono_wait_uninterrupted (thread, TRUE, numhandles, handles, FALSE, ms, TRUE);

	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);

	THREAD_WAIT_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") returning %d", __func__, GetCurrentThreadId (), ret));

	/*
	 * These need to be here.  See MSDN dos on WaitForMultipleObjects.
	 */
	if (ret >= WAIT_OBJECT_0 && ret <= WAIT_OBJECT_0 + numhandles - 1) {
		return ret - WAIT_OBJECT_0;
	}
	else if (ret >= WAIT_ABANDONED_0 && ret <= WAIT_ABANDONED_0 + numhandles - 1) {
		return ret - WAIT_ABANDONED_0;
	}
	else {
		return ret;
	}
}

/* FIXME: exitContext isnt documented */
gboolean ves_icall_System_Threading_WaitHandle_WaitOne_internal(MonoObject *this, HANDLE handle, gint32 ms, gboolean exitContext)
{
	guint32 ret;
	MonoInternalThread *thread = mono_thread_internal_current ();

	THREAD_WAIT_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") waiting for %p, %d ms", __func__, GetCurrentThreadId (), handle, ms));
	
	if(ms== -1) {
		ms=INFINITE;
	}
	
	mono_thread_current_check_pending_interrupt ();

	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);
	
	ret = mono_wait_uninterrupted (thread, FALSE, 1, &handle, FALSE, ms, TRUE);
	
	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);
	
	if(ret==WAIT_FAILED) {
		THREAD_WAIT_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Wait failed", __func__, GetCurrentThreadId ()));
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT) {
		THREAD_WAIT_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Wait timed out", __func__, GetCurrentThreadId ()));
		return(FALSE);
	}
	
	return(TRUE);
}

gboolean
ves_icall_System_Threading_WaitHandle_SignalAndWait_Internal (HANDLE toSignal, HANDLE toWait, gint32 ms, gboolean exitContext)
{
	guint32 ret;
	MonoInternalThread *thread = mono_thread_internal_current ();

	MONO_ARCH_SAVE_REGS;

	if (ms == -1)
		ms = INFINITE;

	mono_thread_current_check_pending_interrupt ();

	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);
	
	ret = SignalObjectAndWait (toSignal, toWait, ms, TRUE);
	
	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);

	return  (!(ret == WAIT_TIMEOUT || ret == WAIT_IO_COMPLETION || ret == WAIT_FAILED));
}

HANDLE ves_icall_System_Threading_Mutex_CreateMutex_internal (MonoBoolean owned, MonoString *name, MonoBoolean *created)
{ 
	HANDLE mutex;
	
	MONO_ARCH_SAVE_REGS;
   
	*created = TRUE;
	
	if (name == NULL) {
		mutex = CreateMutex (NULL, owned, NULL);
	} else {
		mutex = CreateMutex (NULL, owned, mono_string_chars (name));
		
		if (GetLastError () == ERROR_ALREADY_EXISTS) {
			*created = FALSE;
		}
	}

	return(mutex);
}                                                                   

MonoBoolean ves_icall_System_Threading_Mutex_ReleaseMutex_internal (HANDLE handle ) { 
	MONO_ARCH_SAVE_REGS;

	return(ReleaseMutex (handle));
}

HANDLE ves_icall_System_Threading_Mutex_OpenMutex_internal (MonoString *name,
							    gint32 rights,
							    gint32 *error)
{
	HANDLE ret;
	
	MONO_ARCH_SAVE_REGS;
	
	*error = ERROR_SUCCESS;
	
	ret = OpenMutex (rights, FALSE, mono_string_chars (name));
	if (ret == NULL) {
		*error = GetLastError ();
	}
	
	return(ret);
}


HANDLE ves_icall_System_Threading_Semaphore_CreateSemaphore_internal (gint32 initialCount, gint32 maximumCount, MonoString *name, MonoBoolean *created)
{ 
	HANDLE sem;
	
	MONO_ARCH_SAVE_REGS;
   
	*created = TRUE;
	
	if (name == NULL) {
		sem = CreateSemaphore (NULL, initialCount, maximumCount, NULL);
	} else {
		sem = CreateSemaphore (NULL, initialCount, maximumCount,
				       mono_string_chars (name));
		
		if (GetLastError () == ERROR_ALREADY_EXISTS) {
			*created = FALSE;
		}
	}

	return(sem);
}                                                                   

gint32 ves_icall_System_Threading_Semaphore_ReleaseSemaphore_internal (HANDLE handle, gint32 releaseCount, MonoBoolean *fail)
{ 
	gint32 prevcount;
	
	MONO_ARCH_SAVE_REGS;

	*fail = !ReleaseSemaphore (handle, releaseCount, &prevcount);

	return (prevcount);
}

HANDLE ves_icall_System_Threading_Semaphore_OpenSemaphore_internal (MonoString *name, gint32 rights, gint32 *error)
{
	HANDLE ret;
	
	MONO_ARCH_SAVE_REGS;
	
	*error = ERROR_SUCCESS;
	
	ret = OpenSemaphore (rights, FALSE, mono_string_chars (name));
	if (ret == NULL) {
		*error = GetLastError ();
	}
	
	return(ret);
}

HANDLE ves_icall_System_Threading_Events_CreateEvent_internal (MonoBoolean manual, MonoBoolean initial, MonoString *name, MonoBoolean *created)
{
	HANDLE event;
	
	MONO_ARCH_SAVE_REGS;

	*created = TRUE;

	if (name == NULL) {
		event = CreateEvent (NULL, manual, initial, NULL);
	} else {
		event = CreateEvent (NULL, manual, initial,
				     mono_string_chars (name));
		
		if (GetLastError () == ERROR_ALREADY_EXISTS) {
			*created = FALSE;
		}
	}
	
	return(event);
}

gboolean ves_icall_System_Threading_Events_SetEvent_internal (HANDLE handle) {
	MONO_ARCH_SAVE_REGS;

	return (SetEvent(handle));
}

gboolean ves_icall_System_Threading_Events_ResetEvent_internal (HANDLE handle) {
	MONO_ARCH_SAVE_REGS;

	return (ResetEvent(handle));
}

void
ves_icall_System_Threading_Events_CloseEvent_internal (HANDLE handle) {
	MONO_ARCH_SAVE_REGS;

	CloseHandle (handle);
}

HANDLE ves_icall_System_Threading_Events_OpenEvent_internal (MonoString *name,
							     gint32 rights,
							     gint32 *error)
{
	HANDLE ret;
	
	MONO_ARCH_SAVE_REGS;
	
	*error = ERROR_SUCCESS;
	
	ret = OpenEvent (rights, FALSE, mono_string_chars (name));
	if (ret == NULL) {
		*error = GetLastError ();
	}
	
	return(ret);
}

gint32 ves_icall_System_Threading_Interlocked_Increment_Int (gint32 *location)
{
	MONO_ARCH_SAVE_REGS;

	return InterlockedIncrement (location);
}

gint64 ves_icall_System_Threading_Interlocked_Increment_Long (gint64 *location)
{
	gint64 ret;

	MONO_ARCH_SAVE_REGS;

	mono_interlocked_lock ();

	ret = ++ *location;
	
	mono_interlocked_unlock ();

	
	return ret;
}

gint32 ves_icall_System_Threading_Interlocked_Decrement_Int (gint32 *location)
{
	MONO_ARCH_SAVE_REGS;

	return InterlockedDecrement(location);
}

gint64 ves_icall_System_Threading_Interlocked_Decrement_Long (gint64 * location)
{
	gint64 ret;

	MONO_ARCH_SAVE_REGS;

	mono_interlocked_lock ();

	ret = -- *location;
	
	mono_interlocked_unlock ();

	return ret;
}

gint32 ves_icall_System_Threading_Interlocked_Exchange_Int (gint32 *location, gint32 value)
{
	MONO_ARCH_SAVE_REGS;

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

	MONO_ARCH_SAVE_REGS;

	val.fval = value;
	ret.ival = InterlockedExchange((gint32 *) location, val.ival);

	return ret.fval;
}

gint64 
ves_icall_System_Threading_Interlocked_Exchange_Long (gint64 *location, gint64 value)
{
#if SIZEOF_VOID_P == 8
	return (gint64) InterlockedExchangePointer((gpointer *) location, (gpointer)value);
#else
	gint64 res;

	/* 
	 * According to MSDN, this function is only atomic with regards to the 
	 * other Interlocked functions on 32 bit platforms.
	 */
	mono_interlocked_lock ();
	res = *location;
	*location = value;
	mono_interlocked_unlock ();

	return res;
#endif
}

gdouble 
ves_icall_System_Threading_Interlocked_Exchange_Double (gdouble *location, gdouble value)
{
#if SIZEOF_VOID_P == 8
	LongDoubleUnion val, ret;

	val.fval = value;
	ret.ival = (gint64)InterlockedExchangePointer((gpointer *) location, (gpointer)val.ival);

	return ret.fval;
#else
	gdouble res;

	/* 
	 * According to MSDN, this function is only atomic with regards to the 
	 * other Interlocked functions on 32 bit platforms.
	 */
	mono_interlocked_lock ();
	res = *location;
	*location = value;
	mono_interlocked_unlock ();

	return res;
#endif
}

gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int(gint32 *location, gint32 value, gint32 comparand)
{
	MONO_ARCH_SAVE_REGS;

	return InterlockedCompareExchange(location, value, comparand);
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

	MONO_ARCH_SAVE_REGS;

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
#if SIZEOF_VOID_P == 8
	return (gint64)InterlockedCompareExchangePointer((gpointer *) location, (gpointer)value, (gpointer)comparand);
#else
	gint64 old;

	mono_interlocked_lock ();
	old = *location;
	if (old == comparand)
		*location = value;
	mono_interlocked_unlock ();
	
	return old;
#endif
}

MonoObject*
ves_icall_System_Threading_Interlocked_CompareExchange_T (MonoObject **location, MonoObject *value, MonoObject *comparand)
{
	MonoObject *res;
	res = InterlockedCompareExchangePointer ((gpointer *)location, value, comparand);
	mono_gc_wbarrier_generic_nostore (location);
	return res;
}

MonoObject*
ves_icall_System_Threading_Interlocked_Exchange_T (MonoObject **location, MonoObject *value)
{
	MonoObject *res;
	res = InterlockedExchangePointer ((gpointer *)location, value);
	mono_gc_wbarrier_generic_nostore (location);
	return res;
}

gint32 
ves_icall_System_Threading_Interlocked_Add_Int (gint32 *location, gint32 value)
{
#if SIZEOF_VOID_P == 8
	/* Should be implemented as a JIT intrinsic */
	mono_raise_exception (mono_get_exception_not_implemented (NULL));
	return 0;
#else
	gint32 orig;

	mono_interlocked_lock ();
	orig = *location;
	*location = orig + value;
	mono_interlocked_unlock ();

	return orig + value;
#endif
}

gint64 
ves_icall_System_Threading_Interlocked_Add_Long (gint64 *location, gint64 value)
{
#if SIZEOF_VOID_P == 8
	/* Should be implemented as a JIT intrinsic */
	mono_raise_exception (mono_get_exception_not_implemented (NULL));
	return 0;
#else
	gint64 orig;

	mono_interlocked_lock ();
	orig = *location;
	*location = orig + value;
	mono_interlocked_unlock ();

	return orig + value;
#endif
}

gint64 
ves_icall_System_Threading_Interlocked_Read_Long (gint64 *location)
{
#if SIZEOF_VOID_P == 8
	/* 64 bit reads are already atomic */
	return *location;
#else
	gint64 res;

	mono_interlocked_lock ();
	res = *location;
	mono_interlocked_unlock ();

	return res;
#endif
}

void
ves_icall_System_Threading_Thread_MemoryBarrier (void)
{
	mono_threads_lock ();
	mono_threads_unlock ();
}

void
ves_icall_System_Threading_Thread_ClrState (MonoInternalThread* this, guint32 state)
{
	mono_thread_clr_state (this, state);

	if (state & ThreadState_Background) {
		/* If the thread changes the background mode, the main thread has to
		 * be notified, since it has to rebuild the list of threads to
		 * wait for.
		 */
		SetEvent (background_change_event);
	}
}

void
ves_icall_System_Threading_Thread_SetState (MonoInternalThread* this, guint32 state)
{
	mono_thread_set_state (this, state);
	
	if (state & ThreadState_Background) {
		/* If the thread changes the background mode, the main thread has to
		 * be notified, since it has to rebuild the list of threads to
		 * wait for.
		 */
		SetEvent (background_change_event);
	}
}

guint32
ves_icall_System_Threading_Thread_GetState (MonoInternalThread* this)
{
	guint32 state;

	ensure_synch_cs_set (this);
	
	EnterCriticalSection (this->synch_cs);
	
	state = this->state;

	LeaveCriticalSection (this->synch_cs);
	
	return state;
}

void ves_icall_System_Threading_Thread_Interrupt_internal (MonoInternalThread *this)
{
	MonoInternalThread *current;
	gboolean throw;

	ensure_synch_cs_set (this);

	current = mono_thread_internal_current ();

	EnterCriticalSection (this->synch_cs);	

	this->thread_interrupt_requested = TRUE;	
	throw = current != this && (this->state & ThreadState_WaitSleepJoin);	

	LeaveCriticalSection (this->synch_cs);
	
	if (throw) {
		abort_thread_internal (this, TRUE, FALSE);
	}
}

void mono_thread_current_check_pending_interrupt ()
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	gboolean throw = FALSE;

	mono_debugger_check_interruption ();

	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);
	
	if (thread->thread_interrupt_requested) {
		throw = TRUE;
		thread->thread_interrupt_requested = FALSE;
	}
	
	LeaveCriticalSection (thread->synch_cs);

	if (throw) {
		mono_raise_exception (mono_get_exception_thread_interrupted ());
	}
}

int  
mono_thread_get_abort_signal (void)
{
#ifdef HOST_WIN32
	return -1;
#elif defined(PLATFORM_ANDROID)
	return SIGUNUSED;
#elif !defined (SIGRTMIN)
#ifdef SIGUSR1
	return SIGUSR1;
#else
	return -1;
#endif /* SIGUSR1 */
#else
	static int abort_signum = -1;
	int i;
	if (abort_signum != -1)
		return abort_signum;
	/* we try to avoid SIGRTMIN and any one that might have been set already, see bug #75387 */
	for (i = SIGRTMIN + 1; i < SIGRTMAX; ++i) {
		struct sigaction sinfo;
		sigaction (i, NULL, &sinfo);
		if (sinfo.sa_handler == SIG_DFL && (void*)sinfo.sa_sigaction == (void*)SIG_DFL) {
			abort_signum = i;
			return i;
		}
	}
	/* fallback to the old way */
	return SIGRTMIN;
#endif /* HOST_WIN32 */
}

#ifdef HOST_WIN32
static void CALLBACK interruption_request_apc (ULONG_PTR param)
{
	MonoException* exc = mono_thread_request_interruption (FALSE);
	if (exc) mono_raise_exception (exc);
}
#endif /* HOST_WIN32 */

/*
 * signal_thread_state_change
 *
 * Tells the thread that his state has changed and it has to enter the new
 * state as soon as possible.
 */
static void signal_thread_state_change (MonoInternalThread *thread)
{
	if (thread == mono_thread_internal_current ()) {
		/* Do it synchronously */
		MonoException *exc = mono_thread_request_interruption (FALSE); 
		if (exc)
			mono_raise_exception (exc);
	}

#ifdef HOST_WIN32
	QueueUserAPC ((PAPCFUNC)interruption_request_apc, thread->handle, NULL);
#else
	/* fixme: store the state somewhere */
	mono_thread_kill (thread, mono_thread_get_abort_signal ());

	/* 
	 * This will cause waits to be broken.
	 * It will also prevent the thread from entering a wait, so if the thread returns
	 * from the wait before it receives the abort signal, it will just spin in the wait
	 * functions in the io-layer until the signal handler calls QueueUserAPC which will
	 * make it return.
	 */
	wapi_interrupt_thread (thread->handle);
#endif /* HOST_WIN32 */
}

void
ves_icall_System_Threading_Thread_Abort (MonoInternalThread *thread, MonoObject *state)
{
	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);
	
	if ((thread->state & ThreadState_AbortRequested) != 0 || 
		(thread->state & ThreadState_StopRequested) != 0 ||
		(thread->state & ThreadState_Stopped) != 0)
	{
		LeaveCriticalSection (thread->synch_cs);
		return;
	}

	if ((thread->state & ThreadState_Unstarted) != 0) {
		thread->state |= ThreadState_Aborted;
		LeaveCriticalSection (thread->synch_cs);
		return;
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

	/*
	 * abort_exc is set in mono_thread_execute_interruption(),
	 * triggered by the call to signal_thread_state_change(),
	 * below.  There's a point between where we have
	 * abort_state_handle set, but abort_exc NULL, but that's not
	 * a problem.
	 */

	LeaveCriticalSection (thread->synch_cs);

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Abort requested for %p (%"G_GSIZE_FORMAT")", __func__, GetCurrentThreadId (), thread, (gsize)thread->tid));

	/* During shutdown, we can't wait for other threads */
	if (!shutting_down)
		/* Make sure the thread is awake */
		mono_thread_resume (thread);
	
	abort_thread_internal (thread, TRUE, TRUE);
}

void
ves_icall_System_Threading_Thread_ResetAbort (void)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	gboolean was_aborting;

	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);
	was_aborting = thread->state & ThreadState_AbortRequested;
	thread->state &= ~ThreadState_AbortRequested;
	LeaveCriticalSection (thread->synch_cs);

	if (!was_aborting) {
		const char *msg = "Unable to reset abort because no abort was requested";
		mono_raise_exception (mono_get_exception_thread_state (msg));
	}
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
	ensure_synch_cs_set (thread);

	EnterCriticalSection (thread->synch_cs);

	thread->state &= ~ThreadState_AbortRequested;

	if (thread->abort_exc) {
		thread->abort_exc = NULL;
		if (thread->abort_state_handle) {
			mono_gchandle_free (thread->abort_state_handle);
			/* This is actually not necessary - the handle
			   only counts if the exception is set */
			thread->abort_state_handle = 0;
		}
	}

	LeaveCriticalSection (thread->synch_cs);
}

MonoObject*
ves_icall_System_Threading_Thread_GetAbortExceptionState (MonoThread *this)
{
	MonoInternalThread *thread = this->internal_thread;
	MonoObject *state, *deserialized = NULL, *exc;
	MonoDomain *domain;

	if (!thread->abort_state_handle)
		return NULL;

	state = mono_gchandle_get_target (thread->abort_state_handle);
	g_assert (state);

	domain = mono_domain_get ();
	if (mono_object_domain (state) == domain)
		return state;

	deserialized = mono_object_xdomain_representation (state, domain, &exc);

	if (!deserialized) {
		MonoException *invalid_op_exc = mono_get_exception_invalid_operation ("Thread.ExceptionState cannot access an ExceptionState from a different AppDomain");
		if (exc)
			MONO_OBJECT_SETREF (invalid_op_exc, inner_ex, exc);
		mono_raise_exception (invalid_op_exc);
	}

	return deserialized;
}

static gboolean
mono_thread_suspend (MonoInternalThread *thread)
{
	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);

	if ((thread->state & ThreadState_Unstarted) != 0 || 
		(thread->state & ThreadState_Aborted) != 0 || 
		(thread->state & ThreadState_Stopped) != 0)
	{
		LeaveCriticalSection (thread->synch_cs);
		return FALSE;
	}

	if ((thread->state & ThreadState_Suspended) != 0 || 
		(thread->state & ThreadState_SuspendRequested) != 0 ||
		(thread->state & ThreadState_StopRequested) != 0) 
	{
		LeaveCriticalSection (thread->synch_cs);
		return TRUE;
	}
	
	thread->state |= ThreadState_SuspendRequested;

	LeaveCriticalSection (thread->synch_cs);

	suspend_thread_internal (thread, FALSE);
	return TRUE;
}

void
ves_icall_System_Threading_Thread_Suspend (MonoInternalThread *thread)
{
	if (!mono_thread_suspend (thread))
		mono_raise_exception (mono_get_exception_thread_state ("Thread has not been started, or is dead."));
}

static gboolean
mono_thread_resume (MonoInternalThread *thread)
{
	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);

	if ((thread->state & ThreadState_SuspendRequested) != 0) {
		thread->state &= ~ThreadState_SuspendRequested;
		LeaveCriticalSection (thread->synch_cs);
		return TRUE;
	}

	if ((thread->state & ThreadState_Suspended) == 0 ||
		(thread->state & ThreadState_Unstarted) != 0 || 
		(thread->state & ThreadState_Aborted) != 0 || 
		(thread->state & ThreadState_Stopped) != 0)
	{
		LeaveCriticalSection (thread->synch_cs);
		return FALSE;
	}

	return resume_thread_internal (thread);
}

void
ves_icall_System_Threading_Thread_Resume (MonoThread *thread)
{
	if (!thread->internal_thread || !mono_thread_resume (thread->internal_thread))
		mono_raise_exception (mono_get_exception_thread_state ("Thread has not been started, or is dead."));
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

void mono_thread_internal_stop (MonoInternalThread *thread)
{
	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);

	if ((thread->state & ThreadState_StopRequested) != 0 ||
		(thread->state & ThreadState_Stopped) != 0)
	{
		LeaveCriticalSection (thread->synch_cs);
		return;
	}
	
	/* Make sure the thread is awake */
	mono_thread_resume (thread);

	thread->state |= ThreadState_StopRequested;
	thread->state &= ~ThreadState_AbortRequested;
	
	LeaveCriticalSection (thread->synch_cs);
	
	abort_thread_internal (thread, TRUE, TRUE);
}

void mono_thread_stop (MonoThread *thread)
{
	mono_thread_internal_stop (thread->internal_thread);
}

gint8
ves_icall_System_Threading_Thread_VolatileRead1 (void *ptr)
{
	return *((volatile gint8 *) (ptr));
}

gint16
ves_icall_System_Threading_Thread_VolatileRead2 (void *ptr)
{
	return *((volatile gint16 *) (ptr));
}

gint32
ves_icall_System_Threading_Thread_VolatileRead4 (void *ptr)
{
	return *((volatile gint32 *) (ptr));
}

gint64
ves_icall_System_Threading_Thread_VolatileRead8 (void *ptr)
{
	return *((volatile gint64 *) (ptr));
}

void *
ves_icall_System_Threading_Thread_VolatileReadIntPtr (void *ptr)
{
	return (void *)  *((volatile void **) ptr);
}

double
ves_icall_System_Threading_Thread_VolatileReadDouble (void *ptr)
{
	return *((volatile double *) (ptr));
}

float
ves_icall_System_Threading_Thread_VolatileReadFloat (void *ptr)
{
	return *((volatile float *) (ptr));
}

MonoObject*
ves_icall_System_Threading_Volatile_Read_T (void *ptr)
{
	return (MonoObject*)*((volatile MonoObject**)ptr);
}

void
ves_icall_System_Threading_Thread_VolatileWrite1 (void *ptr, gint8 value)
{
	*((volatile gint8 *) ptr) = value;
}

void
ves_icall_System_Threading_Thread_VolatileWrite2 (void *ptr, gint16 value)
{
	*((volatile gint16 *) ptr) = value;
}

void
ves_icall_System_Threading_Thread_VolatileWrite4 (void *ptr, gint32 value)
{
	*((volatile gint32 *) ptr) = value;
}

void
ves_icall_System_Threading_Thread_VolatileWrite8 (void *ptr, gint64 value)
{
	*((volatile gint64 *) ptr) = value;
}

void
ves_icall_System_Threading_Thread_VolatileWriteIntPtr (void *ptr, void *value)
{
	*((volatile void **) ptr) = value;
}

void
ves_icall_System_Threading_Thread_VolatileWriteObject (void *ptr, void *value)
{
	mono_gc_wbarrier_generic_store (ptr, value);
}

void
ves_icall_System_Threading_Thread_VolatileWriteDouble (void *ptr, double value)
{
	*((volatile double *) ptr) = value;
}

void
ves_icall_System_Threading_Thread_VolatileWriteFloat (void *ptr, float value)
{
	*((volatile float *) ptr) = value;
}

void
ves_icall_System_Threading_Volatile_Write_T (void *ptr, MonoObject *value)
{
	*((volatile MonoObject **) ptr) = value;
	mono_gc_wbarrier_generic_nostore (ptr);
}

void
mono_thread_init_tls (void)
{
	MONO_FAST_TLS_INIT (tls_current_object);
	mono_native_tls_alloc (&current_object_key, NULL);
}

void mono_thread_init (MonoThreadStartCB start_cb,
		       MonoThreadAttachCB attach_cb)
{
	InitializeCriticalSection(&threads_mutex);
	InitializeCriticalSection(&interlocked_mutex);
	InitializeCriticalSection(&contexts_mutex);
	
	background_change_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	g_assert(background_change_event != NULL);
	
	mono_init_static_data_info (&thread_static_info);
	mono_init_static_data_info (&context_static_info);

	THREAD_DEBUG (g_message ("%s: Allocated current_object_key %d", __func__, current_object_key));

	mono_thread_start_cb = start_cb;
	mono_thread_attach_cb = attach_cb;

	/* Get a pseudo handle to the current process.  This is just a
	 * kludge so that wapi can build a process handle if needed.
	 * As a pseudo handle is returned, we don't need to clean
	 * anything up.
	 */
	GetCurrentProcess ();
}

void mono_thread_cleanup (void)
{
#if !defined(HOST_WIN32) && !defined(RUN_IN_SUBTHREAD)
	/* The main thread must abandon any held mutexes (particularly
	 * important for named mutexes as they are shared across
	 * processes, see bug 74680.)  This will happen when the
	 * thread exits, but if it's not running in a subthread it
	 * won't exit in time.
	 */
	/* Using non-w32 API is a nasty kludge, but I couldn't find
	 * anything in the documentation that would let me do this
	 * here yet still be safe to call on windows.
	 */
	_wapi_thread_signal_self (mono_environment_exitcode_get ());
#endif

#if 0
	/* This stuff needs more testing, it seems one of these
	 * critical sections can be locked when mono_thread_cleanup is
	 * called.
	 */
	DeleteCriticalSection (&threads_mutex);
	DeleteCriticalSection (&interlocked_mutex);
	DeleteCriticalSection (&contexts_mutex);
	DeleteCriticalSection (&delayed_free_table_mutex);
	DeleteCriticalSection (&small_id_mutex);
	CloseHandle (background_change_event);
#endif

	mono_native_tls_free (current_object_key);
}

void
mono_threads_install_cleanup (MonoThreadCleanupFunc func)
{
	mono_thread_cleanup_fn = func;
}

void
mono_thread_set_manage_callback (MonoThread *thread, MonoThreadManageCallback func)
{
	thread->internal_thread->manage_callback = func;
}

void mono_threads_install_notify_pending_exc (MonoThreadNotifyPendingExcFunc func)
{
	mono_thread_notify_pending_exc_fn = func;
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
	HANDLE handles[MAXIMUM_WAIT_OBJECTS];
	MonoInternalThread *threads[MAXIMUM_WAIT_OBJECTS];
	guint32 num;
};

static void wait_for_tids (struct wait_data *wait, guint32 timeout)
{
	guint32 i, ret;
	
	THREAD_DEBUG (g_message("%s: %d threads to wait for in this batch", __func__, wait->num));

	ret=WaitForMultipleObjectsEx(wait->num, wait->handles, TRUE, timeout, TRUE);

	if(ret==WAIT_FAILED) {
		/* See the comment in build_wait_tids() */
		THREAD_DEBUG (g_message ("%s: Wait failed", __func__));
		return;
	}
	
	for(i=0; i<wait->num; i++)
		CloseHandle (wait->handles[i]);

	if (ret == WAIT_TIMEOUT)
		return;

	for(i=0; i<wait->num; i++) {
		gsize tid = wait->threads[i]->tid;
		
		mono_threads_lock ();
		if(mono_g_hash_table_lookup (threads, (gpointer)tid)!=NULL) {
			/* This thread must have been killed, because
			 * it hasn't cleaned itself up. (It's just
			 * possible that the thread exited before the
			 * parent thread had a chance to store the
			 * handle, and now there is another pointer to
			 * the already-exited thread stored.  In this
			 * case, we'll just get two
			 * mono_profiler_thread_end() calls for the
			 * same thread.)
			 */
	
			mono_threads_unlock ();
			THREAD_DEBUG (g_message ("%s: cleaning up after thread %p (%"G_GSIZE_FORMAT")", __func__, wait->threads[i], tid));
			thread_cleanup (wait->threads[i]);
		} else {
			mono_threads_unlock ();
		}
	}
}

static void wait_for_tids_or_state_change (struct wait_data *wait, guint32 timeout)
{
	guint32 i, ret, count;
	
	THREAD_DEBUG (g_message("%s: %d threads to wait for in this batch", __func__, wait->num));

	/* Add the thread state change event, so it wakes up if a thread changes
	 * to background mode.
	 */
	count = wait->num;
	if (count < MAXIMUM_WAIT_OBJECTS) {
		wait->handles [count] = background_change_event;
		count++;
	}

	ret=WaitForMultipleObjectsEx (count, wait->handles, FALSE, timeout, TRUE);

	if(ret==WAIT_FAILED) {
		/* See the comment in build_wait_tids() */
		THREAD_DEBUG (g_message ("%s: Wait failed", __func__));
		return;
	}
	
	for(i=0; i<wait->num; i++)
		CloseHandle (wait->handles[i]);

	if (ret == WAIT_TIMEOUT)
		return;
	
	if (ret < wait->num) {
		gsize tid = wait->threads[ret]->tid;
		mono_threads_lock ();
		if (mono_g_hash_table_lookup (threads, (gpointer)tid)!=NULL) {
			/* See comment in wait_for_tids about thread cleanup */
			mono_threads_unlock ();
			THREAD_DEBUG (g_message ("%s: cleaning up after thread %"G_GSIZE_FORMAT, __func__, tid));
			thread_cleanup (wait->threads [ret]);
		} else
			mono_threads_unlock ();
	}
}

static void build_wait_tids (gpointer key, gpointer value, gpointer user)
{
	struct wait_data *wait=(struct wait_data *)user;

	if(wait->num<MAXIMUM_WAIT_OBJECTS) {
		HANDLE handle;
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

		handle = OpenThread (THREAD_ALL_ACCESS, TRUE, thread->tid);
		if (handle == NULL) {
			THREAD_DEBUG (g_message ("%s: ignoring unopenable thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
			return;
		}
		
		THREAD_DEBUG (g_message ("%s: Invoking mono_thread_manage callback on thread %p", __func__, thread));
		if ((thread->manage_callback == NULL) || (thread->manage_callback (thread->root_domain_thread) == TRUE)) {
			wait->handles[wait->num]=handle;
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
	gsize self = GetCurrentThreadId ();
	MonoInternalThread *thread = value;
	HANDLE handle;

	if (wait->num >= MAXIMUM_WAIT_OBJECTS)
		return FALSE;

	/* The finalizer thread is not a background thread */
	if (thread->tid != self && (thread->state & ThreadState_Background) != 0 &&
		!(thread->flags & MONO_THREAD_FLAG_DONT_MANAGE)) {
	
		handle = OpenThread (THREAD_ALL_ACCESS, TRUE, thread->tid);
		if (handle == NULL)
			return FALSE;

		/* printf ("A: %d\n", wait->num); */
		wait->handles[wait->num]=thread->handle;
		wait->threads[wait->num]=thread;
		wait->num++;

		THREAD_DEBUG (g_print ("%s: Aborting id: %"G_GSIZE_FORMAT"\n", __func__, (gsize)thread->tid));
		mono_thread_internal_stop (thread);
		return TRUE;
	}

	return (thread->tid != self && !mono_gc_is_finalizer_internal_thread (thread)); 
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

		EnterCriticalSection (current_thread->synch_cs);

		if ((current_thread->state & ThreadState_SuspendRequested) ||
		    (current_thread->state & ThreadState_AbortRequested) ||
		    (current_thread->state & ThreadState_StopRequested)) {
			LeaveCriticalSection (current_thread->synch_cs);
			mono_thread_execute_interruption (current_thread);
		} else {
			current_thread->state |= ThreadState_Stopped;
			LeaveCriticalSection (current_thread->synch_cs);
		}

		/*since we're killing the thread, unset the current domain.*/
		mono_domain_unset ();

		/* Wake up other threads potentially waiting for us */
		ExitThread (0);
	} else {
		shutting_down = TRUE;

		/* Not really a background state change, but this will
		 * interrupt the main thread if it is waiting for all
		 * the other threads.
		 */
		SetEvent (background_change_event);
		
		mono_threads_unlock ();
	}
}

void mono_thread_manage (void)
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
	
		ResetEvent (background_change_event);
		wait->num=0;
		/*We must zero all InternalThread pointers to avoid making the GC unhappy.*/
		memset (wait->threads, 0, MAXIMUM_WAIT_OBJECTS * SIZEOF_VOID_P);
		mono_g_hash_table_foreach (threads, build_wait_tids, wait);
		mono_threads_unlock ();
		if(wait->num>0) {
			/* Something to wait for */
			wait_for_tids_or_state_change (wait, INFINITE);
		}
		THREAD_DEBUG (g_message ("%s: I have %d threads after waiting.", __func__, wait->num));
	} while(wait->num>0);

	/* Mono is shutting down, so just wait for the end */
	if (!mono_runtime_try_shutdown ()) {
		/*FIXME mono_thread_suspend probably should call mono_thread_execute_interruption when self interrupting. */
		mono_thread_suspend (mono_thread_internal_current ());
		mono_thread_execute_interruption (mono_thread_internal_current ());
	}

	/* 
	 * Remove everything but the finalizer thread and self.
	 * Also abort all the background threads
	 * */
	do {
		mono_threads_lock ();

		wait->num = 0;
		/*We must zero all InternalThread pointers to avoid making the GC unhappy.*/
		memset (wait->threads, 0, MAXIMUM_WAIT_OBJECTS * SIZEOF_VOID_P);
		mono_g_hash_table_foreach_remove (threads, remove_and_abort_threads, wait);

		mono_threads_unlock ();

		THREAD_DEBUG (g_message ("%s: wait->num is now %d", __func__, wait->num));
		if(wait->num>0) {
			/* Something to wait for */
			wait_for_tids (wait, INFINITE);
		}
	} while (wait->num > 0);
	
	/* 
	 * give the subthreads a chance to really quit (this is mainly needed
	 * to get correct user and system times from getrusage/wait/time(1)).
	 * This could be removed if we avoid pthread_detach() and use pthread_join().
	 */
#ifndef HOST_WIN32
	sched_yield ();
#endif
}

static void terminate_thread (gpointer key, gpointer value, gpointer user)
{
	MonoInternalThread *thread=(MonoInternalThread *)value;
	
	if(thread->tid != (gsize)user) {
		/*TerminateThread (thread->handle, -1);*/
	}
}

void mono_thread_abort_all_other_threads (void)
{
	gsize self = GetCurrentThreadId ();

	mono_threads_lock ();
	THREAD_DEBUG (g_message ("%s: There are %d threads to abort", __func__,
				 mono_g_hash_table_size (threads));
		      mono_g_hash_table_foreach (threads, print_tids, NULL));

	mono_g_hash_table_foreach (threads, terminate_thread, (gpointer)self);
	
	mono_threads_unlock ();
}

static void
collect_threads_for_suspend (gpointer key, gpointer value, gpointer user_data)
{
	MonoInternalThread *thread = (MonoInternalThread*)value;
	struct wait_data *wait = (struct wait_data*)user_data;
	HANDLE handle;

	/* 
	 * We try to exclude threads early, to avoid running into the MAXIMUM_WAIT_OBJECTS
	 * limitation.
	 * This needs no locking.
	 */
	if ((thread->state & ThreadState_Suspended) != 0 || 
		(thread->state & ThreadState_Stopped) != 0)
		return;

	if (wait->num<MAXIMUM_WAIT_OBJECTS) {
		handle = OpenThread (THREAD_ALL_ACCESS, TRUE, thread->tid);
		if (handle == NULL)
			return;

		wait->handles [wait->num] = handle;
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
	gsize self = GetCurrentThreadId ();
	gpointer *events;
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
	 * - we can only wait for MAXIMUM_WAIT_OBJECTS threads
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
		memset (wait->threads, 0, MAXIMUM_WAIT_OBJECTS * SIZEOF_VOID_P);
		mono_threads_lock ();
		mono_g_hash_table_foreach (threads, collect_threads_for_suspend, wait);
		mono_threads_unlock ();

		events = g_new0 (gpointer, wait->num);
		eventidx = 0;
		/* Get the suspended events that we'll be waiting for */
		for (i = 0; i < wait->num; ++i) {
			MonoInternalThread *thread = wait->threads [i];
			gboolean signal_suspend = FALSE;

			if ((thread->tid == self) || mono_gc_is_finalizer_internal_thread (thread) || (thread->flags & MONO_THREAD_FLAG_DONT_MANAGE)) {
				//CloseHandle (wait->handles [i]);
				wait->threads [i] = NULL; /* ignore this thread in next loop */
				continue;
			}

			ensure_synch_cs_set (thread);
		
			EnterCriticalSection (thread->synch_cs);

			if (thread->suspended_event == NULL) {
				thread->suspended_event = CreateEvent (NULL, TRUE, FALSE, NULL);
				if (thread->suspended_event == NULL) {
					/* Forget this one and go on to the next */
					LeaveCriticalSection (thread->synch_cs);
					continue;
				}
			}

			if ((thread->state & ThreadState_Suspended) != 0 || 
				(thread->state & ThreadState_StopRequested) != 0 ||
				(thread->state & ThreadState_Stopped) != 0) {
				LeaveCriticalSection (thread->synch_cs);
				CloseHandle (wait->handles [i]);
				wait->threads [i] = NULL; /* ignore this thread in next loop */
				continue;
			}

			if ((thread->state & ThreadState_SuspendRequested) == 0)
				signal_suspend = TRUE;

			events [eventidx++] = thread->suspended_event;

			/* Convert abort requests into suspend requests */
			if ((thread->state & ThreadState_AbortRequested) != 0)
				thread->state &= ~ThreadState_AbortRequested;
			
			thread->state |= ThreadState_SuspendRequested;

			LeaveCriticalSection (thread->synch_cs);

			/* Signal the thread to suspend */
			if (mono_thread_info_new_interrupt_enabled ())
				suspend_thread_internal (thread, TRUE);
			else if (signal_suspend)
				signal_thread_state_change (thread);
		}

		/*Only wait on the suspend event if we are using the old path */
		if (eventidx > 0 && !mono_thread_info_new_interrupt_enabled ()) {
			WaitForMultipleObjectsEx (eventidx, events, TRUE, 100, FALSE);
			for (i = 0; i < wait->num; ++i) {
				MonoInternalThread *thread = wait->threads [i];

				if (thread == NULL)
					continue;

				ensure_synch_cs_set (thread);
			
				EnterCriticalSection (thread->synch_cs);
				if ((thread->state & ThreadState_Suspended) != 0) {
					CloseHandle (thread->suspended_event);
					thread->suspended_event = NULL;
				}
				LeaveCriticalSection (thread->synch_cs);
			}
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
				Sleep (100);
			else
				finished = TRUE;
		}

		g_free (events);
	}
}

static void
collect_threads (gpointer key, gpointer value, gpointer user_data)
{
	MonoInternalThread *thread = (MonoInternalThread*)value;
	struct wait_data *wait = (struct wait_data*)user_data;
	HANDLE handle;

	if (wait->num<MAXIMUM_WAIT_OBJECTS) {
		handle = OpenThread (THREAD_ALL_ACCESS, TRUE, thread->tid);
		if (handle == NULL)
			return;

		wait->handles [wait->num] = handle;
		wait->threads [wait->num] = thread;
		wait->num++;
	}
}

static gboolean thread_dump_requested;

static G_GNUC_UNUSED gboolean
print_stack_frame_to_string (MonoStackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	GString *p = (GString*)data;
	MonoMethod *method = NULL;
	if (frame->ji)
		method = frame->ji->method;

	if (method) {
		gchar *location = mono_debug_print_stack_frame (method, frame->native_offset, frame->domain);
		g_string_append_printf (p, "  %s\n", location);
		g_free (location);
	} else
		g_string_append_printf (p, "  at <unknown> <0x%05x>\n", frame->native_offset);

	return FALSE;
}

static void
print_thread_dump (MonoInternalThread *thread, MonoThreadInfo *info)
{
	GString* text = g_string_new (0);
	char *name;
	GError *error = NULL;

	if (thread->name) {
		name = g_utf16_to_utf8 (thread->name, thread->name_len, NULL, NULL, &error);
		g_assert (!error);
		g_string_append_printf (text, "\n\"%s\"", name);
		g_free (name);
	}
	else if (thread->threadpool_thread)
		g_string_append (text, "\n\"<threadpool thread>\"");
	else
		g_string_append (text, "\n\"<unnamed thread>\"");

#if 0
/* This no longer works with remote unwinding */
#ifndef HOST_WIN32
	wapi_desc = wapi_current_thread_desc ();
	g_string_append_printf (text, " tid=0x%p this=0x%p %s\n", (gpointer)(gsize)thread->tid, thread,  wapi_desc);
	free (wapi_desc);
#endif
#endif

	mono_get_eh_callbacks ()->mono_walk_stack_with_state (print_stack_frame_to_string, &info->suspend_state, MONO_UNWIND_SIGNAL_SAFE, text);
	mono_thread_info_resume (mono_thread_info_get_tid (info));

	fprintf (stdout, "%s", text->str);

#if PLATFORM_WIN32 && TARGET_WIN32 && _DEBUG
	OutputDebugStringA(text->str);
#endif

	g_string_free (text, TRUE);
	fflush (stdout);
}

static void
dump_thread (gpointer key, gpointer value, gpointer user)
{
	MonoInternalThread *thread = (MonoInternalThread *)value;
	MonoThreadInfo *info;

	if (thread == mono_thread_internal_current ())
		return;

	/*
	FIXME This still can hang if we stop a thread during malloc.
	FIXME This can hang if we suspend on a critical method and the GC kicks in. A fix might be to have function
	that takes a callback and runs it with the target suspended.
	We probably should loop a bit around trying to get it to either managed code
	or WSJ state.
	*/
	info = mono_thread_info_safe_suspend_sync ((MonoNativeThreadId)(gpointer)(gsize)thread->tid, FALSE);

	if (!info)
		return;

	print_thread_dump (thread, info);
}

void
mono_threads_perform_thread_dump (void)
{
	if (!thread_dump_requested)
		return;

	printf ("Full thread dump:\n");

	/* 
	 * Make a copy of the hashtable since we can't do anything with
	 * threads while threads_mutex is held.
	 */
	mono_threads_lock ();
	mono_g_hash_table_foreach (threads, dump_thread, NULL);
	mono_threads_unlock ();

	thread_dump_requested = FALSE;
}

/**
 * mono_threads_request_thread_dump:
 *
 *   Ask all threads except the current to print their stacktrace to stdout.
 */
void
mono_threads_request_thread_dump (void)
{
	struct wait_data wait_data;
	struct wait_data *wait = &wait_data;
	int i;

	/*The new thread dump code runs out of the finalizer thread. */
	if (mono_thread_info_new_interrupt_enabled ()) {
		thread_dump_requested = TRUE;
		mono_gc_finalize_notify ();
		return;
	}


	memset (wait, 0, sizeof (struct wait_data));

	/* 
	 * Make a copy of the hashtable since we can't do anything with
	 * threads while threads_mutex is held.
	 */
	mono_threads_lock ();
	mono_g_hash_table_foreach (threads, collect_threads, wait);
	mono_threads_unlock ();

	for (i = 0; i < wait->num; ++i) {
		MonoInternalThread *thread = wait->threads [i];

		if (!mono_gc_is_finalizer_internal_thread (thread) &&
				(thread != mono_thread_internal_current ()) &&
				!thread->thread_dump_requested) {
			thread->thread_dump_requested = TRUE;

			signal_thread_state_change (thread);
		}

		CloseHandle (wait->handles [i]);
	}
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
	RefStack *rs = ptr;

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
		rs->refs = g_realloc (rs->refs, rs->allocated * 2 * sizeof (gpointer) + 1);
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
		ref_stack_push (thread->appdomain_refs, domain);
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
		ref_stack_pop (thread->appdomain_refs);
		SPIN_UNLOCK (thread->lock_thread_id);
	}
}

gboolean
mono_thread_internal_has_appdomain_ref (MonoInternalThread *thread, MonoDomain *domain)
{
	gboolean res;
	SPIN_LOCK (thread->lock_thread_id);
	res = ref_stack_find (thread->appdomain_refs, domain);
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

		if(data->wait.num<MAXIMUM_WAIT_OBJECTS) {
			HANDLE handle = OpenThread (THREAD_ALL_ACCESS, TRUE, thread->tid);
			if (handle == NULL)
				return;
			data->wait.handles [data->wait.num] = handle;
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
#ifdef __native_client__
	return FALSE;
#endif

	abort_appdomain_data user_data;
	guint32 start_time;
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
				ves_icall_System_Threading_Thread_Abort (user_data.wait.threads [i], NULL);

			/*
			 * We should wait for the threads either to abort, or to leave the
			 * domain. We can't do the latter, so we wait with a timeout.
			 */
			wait_for_tids (&user_data.wait, 100);
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

static void
clear_cached_culture (gpointer key, gpointer value, gpointer user_data)
{
	MonoInternalThread *thread = (MonoInternalThread*)value;
	MonoDomain *domain = (MonoDomain*)user_data;
	int i;

	/* No locking needed here */
	/* FIXME: why no locking? writes to the cache are protected with synch_cs above */

	if (thread->cached_culture_info) {
		for (i = 0; i < NUM_CACHED_CULTURES * 2; ++i) {
			MonoObject *obj = mono_array_get (thread->cached_culture_info, MonoObject*, i);
			if (obj && obj->vtable->domain == domain)
				mono_array_set (thread->cached_culture_info, MonoObject*, i, NULL);
		}
	}
}
	
/*
 * mono_threads_clear_cached_culture:
 *
 *   Clear the cached_current_culture from all threads if it is in the
 * given appdomain.
 */
void
mono_threads_clear_cached_culture (MonoDomain *domain)
{
	mono_threads_lock ();
	mono_g_hash_table_foreach (threads, clear_cached_culture, domain);
	mono_threads_unlock ();
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

	if (thread && thread->abort_exc && !is_running_protected_wrapper ()) {
		/*
		 * FIXME: Clear the abort exception and return an AppDomainUnloaded 
		 * exception if the thread no longer references a dying appdomain.
		 */
		thread->abort_exc->trace_ips = NULL;
		thread->abort_exc->stack_trace = NULL;
		return thread->abort_exc;
	}

	return NULL;
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

static uintptr_t* static_reference_bitmaps [NUM_STATIC_DATA_IDX];

static void
mark_tls_slots (void *addr, MonoGCMarkFunc mark_func)
{
	int i;
	gpointer *static_data = addr;
	for (i = 0; i < NUM_STATIC_DATA_IDX; ++i) {
		int j, numwords;
		void **ptr;
		if (!static_data [i])
			continue;
		numwords = 1 + static_data_size [i] / sizeof (gpointer) / (sizeof(uintptr_t) * 8);
		ptr = static_data [i];
		for (j = 0; j < numwords; ++j, ptr += sizeof (uintptr_t) * 8) {
			uintptr_t bmap = static_reference_bitmaps [i][j];
			void ** p = ptr;
			while (bmap) {
				if ((bmap & 1) && *p) {
					mark_func (p);
				}
				p++;
				bmap >>= 1;
			}
		}
	}
}

/*
 *  mono_alloc_static_data
 *
 *   Allocate memory blocks for storing threads or context static data
 */
static void 
mono_alloc_static_data (gpointer **static_data_ptr, guint32 offset, gboolean threadlocal)
{
	guint idx = (offset >> 24) - 1;
	int i;

	gpointer* static_data = *static_data_ptr;
	if (!static_data) {
		static void* tls_desc = NULL;
		if (mono_gc_user_markers_supported () && !tls_desc)
			tls_desc = mono_gc_make_root_descr_user (mark_tls_slots);
		static_data = mono_gc_alloc_fixed (static_data_size [0], threadlocal?tls_desc:NULL);
		*static_data_ptr = static_data;
		static_data [0] = static_data;
	}

	for (i = 1; i <= idx; ++i) {
		if (static_data [i])
			continue;
		if (mono_gc_user_markers_supported () && threadlocal)
			static_data [i] = g_malloc0 (static_data_size [i]);
		else
			static_data [i] = mono_gc_alloc_fixed (static_data_size [i], NULL);
	}
}

static void 
mono_free_static_data (gpointer* static_data, gboolean threadlocal)
{
	int i;
	for (i = 1; i < NUM_STATIC_DATA_IDX; ++i) {
		if (!static_data [i])
			continue;
		if (mono_gc_user_markers_supported () && threadlocal)
			g_free (static_data [i]);
		else
			mono_gc_free_fixed (static_data [i]);
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
	guint32 offset;

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
	offset = static_data->offset | ((static_data->idx + 1) << 24);
	static_data->offset += size;
	return offset;
}

/* 
 * ensure thread static fields already allocated are valid for thread
 * This function is called when a thread is created or on thread attach.
 */
static void
thread_adjust_static_data (MonoInternalThread *thread)
{
	guint32 offset;

	mono_threads_lock ();
	if (thread_static_info.offset || thread_static_info.idx > 0) {
		/* get the current allocated size */
		offset = thread_static_info.offset | ((thread_static_info.idx + 1) << 24);
		mono_alloc_static_data (&(thread->static_data), offset, TRUE);
	}
	mono_threads_unlock ();
}

static void 
alloc_thread_static_data_helper (gpointer key, gpointer value, gpointer user)
{
	MonoInternalThread *thread = value;
	guint32 offset = GPOINTER_TO_UINT (user);

	mono_alloc_static_data (&(thread->static_data), offset, TRUE);
}

static MonoThreadDomainTls*
search_tls_slot_in_freelist (StaticDataInfo *static_data, guint32 size, guint32 align)
{
	MonoThreadDomainTls* prev = NULL;
	MonoThreadDomainTls* tmp = static_data->freelist;
	while (tmp) {
		if (tmp->size == size) {
			if (prev)
				prev->next = tmp->next;
			else
				static_data->freelist = tmp->next;
			return tmp;
		}
		tmp = tmp->next;
	}
	return NULL;
}

static void
update_tls_reference_bitmap (guint32 offset, uintptr_t *bitmap, int numbits)
{
	int i;
	int idx = (offset >> 24) - 1;
	uintptr_t *rb;
	if (!static_reference_bitmaps [idx])
		static_reference_bitmaps [idx] = g_new0 (uintptr_t, 1 + static_data_size [idx] / sizeof(gpointer) / (sizeof(uintptr_t) * 8));
	rb = static_reference_bitmaps [idx];
	offset &= 0xffffff;
	offset /= sizeof (gpointer);
	/* offset is now the bitmap offset */
	for (i = 0; i < numbits; ++i) {
		if (bitmap [i / sizeof (uintptr_t)] & (1L << (i & (sizeof (uintptr_t) * 8 -1))))
			rb [(offset + i) / (sizeof (uintptr_t) * 8)] |= (1L << ((offset + i) & (sizeof (uintptr_t) * 8 -1)));
	}
}

static void
clear_reference_bitmap (guint32 offset, guint32 size)
{
	int idx = (offset >> 24) - 1;
	uintptr_t *rb;
	rb = static_reference_bitmaps [idx];
	offset &= 0xffffff;
	offset /= sizeof (gpointer);
	size /= sizeof (gpointer);
	size += offset;
	/* offset is now the bitmap offset */
	for (; offset < size; ++offset)
		rb [offset / (sizeof (uintptr_t) * 8)] &= ~(1L << (offset & (sizeof (uintptr_t) * 8 -1)));
}

/*
 * The offset for a special static variable is composed of three parts:
 * a bit that indicates the type of static data (0:thread, 1:context),
 * an index in the array of chunks of memory for the thread (thread->static_data)
 * and an offset in that chunk of mem. This allows allocating less memory in the 
 * common case.
 */

guint32
mono_alloc_special_static_data (guint32 static_type, guint32 size, guint32 align, uintptr_t *bitmap, int numbits)
{
	guint32 offset;
	if (static_type == SPECIAL_STATIC_THREAD) {
		MonoThreadDomainTls *item;
		mono_threads_lock ();
		item = search_tls_slot_in_freelist (&thread_static_info, size, align);
		/*g_print ("TLS alloc: %d in domain %p (total: %d), cached: %p\n", size, mono_domain_get (), thread_static_info.offset, item);*/
		if (item) {
			offset = item->offset;
			g_free (item);
		} else {
			offset = mono_alloc_static_data_slot (&thread_static_info, size, align);
		}
		update_tls_reference_bitmap (offset, bitmap, numbits);
		/* This can be called during startup */
		if (threads != NULL)
			mono_g_hash_table_foreach (threads, alloc_thread_static_data_helper, GUINT_TO_POINTER (offset));
		mono_threads_unlock ();
	} else {
		g_assert (static_type == SPECIAL_STATIC_CONTEXT);
		mono_contexts_lock ();
		offset = mono_alloc_static_data_slot (&context_static_info, size, align);
		mono_contexts_unlock ();
		offset |= 0x80000000;	/* Set the high bit to indicate context static data */
	}
	return offset;
}

gpointer
mono_get_special_static_data_for_thread (MonoInternalThread *thread, guint32 offset)
{
	/* The high bit means either thread (0) or static (1) data. */

	guint32 static_type = (offset & 0x80000000);
	int idx;

	offset &= 0x7fffffff;
	idx = (offset >> 24) - 1;

	if (static_type == 0) {
		return get_thread_static_data (thread, offset);
	} else {
		/* Allocate static data block under demand, since we don't have a list
		// of contexts
		*/
		MonoAppContext *context = mono_context_get ();
		if (!context->static_data || !context->static_data [idx]) {
			mono_contexts_lock ();
			mono_alloc_static_data (&(context->static_data), offset, FALSE);
			mono_contexts_unlock ();
		}
		return ((char*) context->static_data [idx]) + (offset & 0xffffff);	
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
} TlsOffsetSize;

static void 
free_thread_static_data_helper (gpointer key, gpointer value, gpointer user)
{
	MonoInternalThread *thread = value;
	TlsOffsetSize *data = user;
	int idx = (data->offset >> 24) - 1;
	char *ptr;

	if (!thread->static_data || !thread->static_data [idx])
		return;
	ptr = ((char*) thread->static_data [idx]) + (data->offset & 0xffffff);
	mono_gc_bzero (ptr, data->size);
}

static void
do_free_special_slot (guint32 offset, guint32 size)
{
	guint32 static_type = (offset & 0x80000000);
	/*g_print ("free %s , size: %d, offset: %x\n", field->name, size, offset);*/
	if (static_type == 0) {
		TlsOffsetSize data;
		MonoThreadDomainTls *item = g_new0 (MonoThreadDomainTls, 1);
		data.offset = offset & 0x7fffffff;
		data.size = size;
		clear_reference_bitmap (data.offset, data.size);
		if (threads != NULL)
			mono_g_hash_table_foreach (threads, free_thread_static_data_helper, &data);
		item->offset = offset;
		item->size = size;

		if (!mono_runtime_is_shutting_down ()) {
			item->next = thread_static_info.freelist;
			thread_static_info.freelist = item;
		} else {
			/* We could be called during shutdown after mono_thread_cleanup () is called */
			g_free (item);
		}
	} else {
		/* FIXME: free context static data as well */
	}
}

static void
do_free_special (gpointer key, gpointer value, gpointer data)
{
	MonoClassField *field = key;
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

void
mono_special_static_data_free_slot (guint32 offset, guint32 size)
{
	mono_threads_lock ();
	do_free_special_slot (offset, size);
	mono_threads_unlock ();
}

/*
 * allocates room in the thread local area for storing an instance of the struct type
 * the allocation is kept track of in domain->tlsrec_list.
 */
uint32_t
mono_thread_alloc_tls (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get ();
	MonoClass *klass;
	MonoTlsDataRecord *tlsrec;
	int max_set = 0;
	gsize *bitmap;
	gsize default_bitmap [4] = {0};
	uint32_t tls_offset;
	guint32 size;
	gint32 align;

	klass = mono_class_from_mono_type (type->type);
	/* TlsDatum is a struct, so we subtract the object header size offset */
	bitmap = mono_class_compute_bitmap (klass, default_bitmap, sizeof (default_bitmap) * 8, - (int)(sizeof (MonoObject) / sizeof (gpointer)), &max_set, FALSE);
	size = mono_type_size (type->type, &align);
	tls_offset = mono_alloc_special_static_data (SPECIAL_STATIC_THREAD, size, align, (uintptr_t*)bitmap, max_set + 1);
	if (bitmap != default_bitmap)
		g_free (bitmap);
	tlsrec = g_new0 (MonoTlsDataRecord, 1);
	tlsrec->tls_offset = tls_offset;
	tlsrec->size = size;
	mono_domain_lock (domain);
	tlsrec->next = domain->tlsrec_list;
	domain->tlsrec_list = tlsrec;
	mono_domain_unlock (domain);
	return tls_offset;
}

void
mono_thread_destroy_tls (uint32_t tls_offset)
{
	MonoTlsDataRecord *prev = NULL;
	MonoTlsDataRecord *cur;
	guint32 size = 0;
	MonoDomain *domain = mono_domain_get ();
	mono_domain_lock (domain);
	cur = domain->tlsrec_list;
	while (cur) {
		if (cur->tls_offset == tls_offset) {
			if (prev)
				prev->next = cur->next;
			else
				domain->tlsrec_list = cur->next;
			size = cur->size;
			g_free (cur);
			break;
		}
		prev = cur;
		cur = cur->next;
	}
	mono_domain_unlock (domain);
	if (size)
		mono_special_static_data_free_slot (tls_offset, size);
}

/*
 * This is just to ensure cleanup: the finalizers should have taken care, so this is not perf-critical.
 */
void
mono_thread_destroy_domain_tls (MonoDomain *domain)
{
	while (domain->tlsrec_list)
		mono_thread_destroy_tls (domain->tlsrec_list->tls_offset);
}

static MonoClassField *local_slots = NULL;

typedef struct {
	/* local tls data to get locals_slot from a thread */
	guint32 offset;
	int idx;
	/* index in the locals_slot array */
	int slot;
} LocalSlotID;

static void
clear_local_slot (gpointer key, gpointer value, gpointer user_data)
{
	LocalSlotID *sid = user_data;
	MonoInternalThread *thread = (MonoInternalThread*)value;
	MonoArray *slots_array;
	/*
	 * the static field is stored at: ((char*) thread->static_data [idx]) + (offset & 0xffffff);
	 * it is for the right domain, so we need to check if it is allocated an initialized
	 * for the current thread.
	 */
	/*g_print ("handling thread %p\n", thread);*/
	if (!thread->static_data || !thread->static_data [sid->idx])
		return;
	slots_array = *(MonoArray **)(((char*) thread->static_data [sid->idx]) + (sid->offset & 0xffffff));
	if (!slots_array || sid->slot >= mono_array_length (slots_array))
		return;
	mono_array_set (slots_array, MonoObject*, sid->slot, NULL);
}

void
mono_thread_free_local_slot_values (int slot, MonoBoolean thread_local)
{
	MonoDomain *domain;
	LocalSlotID sid;
	sid.slot = slot;
	if (thread_local) {
		void *addr = NULL;
		if (!local_slots) {
			local_slots = mono_class_get_field_from_name (mono_defaults.thread_class, "local_slots");
			if (!local_slots) {
				g_warning ("local_slots field not found in Thread class");
				return;
			}
		}
		domain = mono_domain_get ();
		mono_domain_lock (domain);
		if (domain->special_static_fields)
			addr = g_hash_table_lookup (domain->special_static_fields, local_slots);
		mono_domain_unlock (domain);
		if (!addr)
			return;
		/*g_print ("freeing slot %d at %p\n", slot, addr);*/
		sid.offset = GPOINTER_TO_UINT (addr);
		sid.offset &= 0x7fffffff;
		sid.idx = (sid.offset >> 24) - 1;
		mono_threads_lock ();
		mono_g_hash_table_foreach (threads, clear_local_slot, &sid);
		mono_threads_unlock ();
	} else {
		/* FIXME: clear the slot for MonoAppContexts, too */
	}
}

#ifdef HOST_WIN32
static void CALLBACK dummy_apc (ULONG_PTR param)
{
}
#else
static guint32 dummy_apc (gpointer param)
{
	return 0;
}
#endif

/*
 * mono_thread_execute_interruption
 * 
 * Performs the operation that the requested thread state requires (abort,
 * suspend or stop)
 */
static MonoException* mono_thread_execute_interruption (MonoInternalThread *thread)
{
	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);

	/* MonoThread::interruption_requested can only be changed with atomics */
	if (InterlockedCompareExchange (&thread->interruption_requested, FALSE, TRUE)) {
		/* this will consume pending APC calls */
		WaitForSingleObjectEx (GetCurrentThread(), 0, TRUE);
		InterlockedDecrement (&thread_interruption_requested);
#ifndef HOST_WIN32
		/* Clear the interrupted flag of the thread so it can wait again */
		wapi_clear_interruption ();
#endif
	}

	if ((thread->state & ThreadState_AbortRequested) != 0) {
		LeaveCriticalSection (thread->synch_cs);
		if (thread->abort_exc == NULL) {
			/* 
			 * This might be racy, but it has to be called outside the lock
			 * since it calls managed code.
			 */
			MONO_OBJECT_SETREF (thread, abort_exc, mono_get_exception_thread_abort ());
		}
		return thread->abort_exc;
	}
	else if ((thread->state & ThreadState_SuspendRequested) != 0) {
		self_suspend_internal (thread);		
		return NULL;
	}
	else if ((thread->state & ThreadState_StopRequested) != 0) {
		/* FIXME: do this through the JIT? */

		LeaveCriticalSection (thread->synch_cs);
		
		mono_thread_exit ();
		return NULL;
	} else if (thread->pending_exception) {
		MonoException *exc;

		exc = thread->pending_exception;
		thread->pending_exception = NULL;

        LeaveCriticalSection (thread->synch_cs);
        return exc;
	} else if (thread->thread_interrupt_requested) {

		thread->thread_interrupt_requested = FALSE;
		LeaveCriticalSection (thread->synch_cs);
		
		return(mono_get_exception_thread_interrupted ());
	}
	
	LeaveCriticalSection (thread->synch_cs);
	
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

#ifdef HOST_WIN32
	if (thread->interrupt_on_stop && 
		thread->state & ThreadState_StopRequested && 
		thread->state & ThreadState_Background)
		ExitThread (1);
#endif
	
	if (InterlockedCompareExchange (&thread->interruption_requested, 1, 0) == 1)
		return NULL;
	InterlockedIncrement (&thread_interruption_requested);

	if (!running_managed || is_running_protected_wrapper ()) {
		/* Can't stop while in unmanaged code. Increase the global interruption
		   request count. When exiting the unmanaged method the count will be
		   checked and the thread will be interrupted. */
		

		if (mono_thread_notify_pending_exc_fn && !running_managed)
			/* The JIT will notify the thread about the interruption */
			/* This shouldn't take any locks */
			mono_thread_notify_pending_exc_fn ();

		/* this will awake the thread if it is in WaitForSingleObject 
		   or similar */
		/* Our implementation of this function ignores the func argument */
		QueueUserAPC ((PAPCFUNC)dummy_apc, thread->handle, NULL);
		return NULL;
	}
	else {
		return mono_thread_execute_interruption (thread);
	}
}

/*This function should be called by a thread after it has exited all of
 * its handle blocks at interruption time.*/
MonoException*
mono_thread_resume_interruption (void)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	gboolean still_aborting;

	/* The thread may already be stopping */
	if (thread == NULL)
		return NULL;

	ensure_synch_cs_set (thread);
	EnterCriticalSection (thread->synch_cs);
	still_aborting = (thread->state & ThreadState_AbortRequested) != 0;
	LeaveCriticalSection (thread->synch_cs);

	/*This can happen if the protected block called Thread::ResetAbort*/
	if (!still_aborting)
		return FALSE;

	if (InterlockedCompareExchange (&thread->interruption_requested, 1, 0) == 1)
		return NULL;
	InterlockedIncrement (&thread_interruption_requested);

#ifndef HOST_WIN32
	wapi_self_interrupt ();
#endif
	return mono_thread_execute_interruption (thread);
}

gboolean mono_thread_interruption_requested ()
{
	if (thread_interruption_requested) {
		MonoInternalThread *thread = mono_thread_internal_current ();
		/* The thread may already be stopping */
		if (thread != NULL) 
			return (thread->interruption_requested);
	}
	return FALSE;
}

static void mono_thread_interruption_checkpoint_request (gboolean bypass_abort_protection)
{
	MonoInternalThread *thread = mono_thread_internal_current ();

	/* The thread may already be stopping */
	if (thread == NULL)
		return;

	mono_debugger_check_interruption ();

	if (thread->interruption_requested && (bypass_abort_protection || !is_running_protected_wrapper ())) {
		MonoException* exc = mono_thread_execute_interruption (thread);
		if (exc) mono_raise_exception (exc);
	}
}

/*
 * Performs the interruption of the current thread, if one has been requested,
 * and the thread is not running a protected wrapper.
 */
void mono_thread_interruption_checkpoint ()
{
	mono_thread_interruption_checkpoint_request (FALSE);
}

/*
 * Performs the interruption of the current thread, if one has been requested.
 */
void mono_thread_force_interruption_checkpoint ()
{
	mono_thread_interruption_checkpoint_request (TRUE);
}

/*
 * mono_thread_get_and_clear_pending_exception:
 *
 *   Return any pending exceptions for the current thread and clear it as a side effect.
 */
MonoException*
mono_thread_get_and_clear_pending_exception (void)
{
	MonoInternalThread *thread = mono_thread_internal_current ();

	/* The thread may already be stopping */
	if (thread == NULL)
		return NULL;

	if (thread->interruption_requested && !is_running_protected_wrapper ()) {
		return mono_thread_execute_interruption (thread);
	}
	
	if (thread->pending_exception) {
		MonoException *exc = thread->pending_exception;

		thread->pending_exception = NULL;
		return exc;
	}

	return NULL;
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
	MonoInternalThread *thread = mono_thread_internal_current ();

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
	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);
	thread->state |= state;
	LeaveCriticalSection (thread->synch_cs);
}

void
mono_thread_clr_state (MonoInternalThread *thread, MonoThreadState state)
{
	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);
	thread->state &= ~state;
	LeaveCriticalSection (thread->synch_cs);
}

gboolean
mono_thread_test_state (MonoInternalThread *thread, MonoThreadState test)
{
	gboolean ret = FALSE;

	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);

	if ((thread->state & test) != 0) {
		ret = TRUE;
	}
	
	LeaveCriticalSection (thread->synch_cs);
	
	return ret;
}

//static MonoClassField *execution_context_field;

static MonoObject**
get_execution_context_addr (void)
{
	MonoDomain *domain = mono_domain_get ();
	guint32 offset = domain->execution_context_field_offset;

	if (!offset) {
		MonoClassField *field = mono_class_get_field_from_name (mono_defaults.thread_class, "_ec");
		g_assert (field);

		g_assert (mono_class_try_get_vtable (domain, mono_defaults.appdomain_class));

		mono_domain_lock (domain);
		offset = GPOINTER_TO_UINT (g_hash_table_lookup (domain->special_static_fields, field));
		mono_domain_unlock (domain);
		g_assert (offset);

		domain->execution_context_field_offset = offset;
	}

	return (MonoObject**) mono_get_special_static_data (offset);
}

MonoObject*
mono_thread_get_execution_context (void)
{
	return *get_execution_context_addr ();
}

void
mono_thread_set_execution_context (MonoObject *ec)
{
	*get_execution_context_addr () = ec;
}

static gboolean has_tls_get = FALSE;

void
mono_runtime_set_has_tls_get (gboolean val)
{
	has_tls_get = val;
}

gboolean
mono_runtime_has_tls_get (void)
{
	return has_tls_get;
}

int
mono_thread_kill (MonoInternalThread *thread, int signal)
{
#ifdef __native_client__
	/* Workaround pthread_kill abort() in NaCl glibc. */
	return -1;
#endif
#ifdef HOST_WIN32
	/* Win32 uses QueueUserAPC and callers of this are guarded */
	g_assert_not_reached ();
#else
#  ifdef PTHREAD_POINTER_ID
	return pthread_kill ((gpointer)(gsize)(thread->tid), mono_thread_get_abort_signal ());
#  else
#    ifdef PLATFORM_ANDROID
	if (thread->android_tid != 0) {
		int  ret;
		int  old_errno = errno;

		ret = tkill ((pid_t) thread->android_tid, signal);
		if (ret < 0) {
			ret = errno;
			errno = old_errno;
		}

		return ret;
	}
	else
		return pthread_kill (thread->tid, mono_thread_get_abort_signal ());
#    else
	return pthread_kill (thread->tid, mono_thread_get_abort_signal ());
#    endif
#  endif
#endif
}

static void
self_interrupt_thread (void *_unused)
{
	MonoThreadInfo *info = mono_thread_info_current ();
	MonoException *exc = mono_thread_execute_interruption (mono_thread_internal_current ()); 
	if (exc) /*We must use _with_context since we didn't trampoline into the runtime*/
		mono_raise_exception_with_context (exc, &info->suspend_state.ctx);
	g_assert_not_reached (); /*this MUST not happen since we can't resume from an async call*/
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
	MonoJitInfo **dest = data;
	*dest = frame->ji;
	return TRUE;
}

static MonoJitInfo*
mono_thread_info_get_last_managed (MonoThreadInfo *info)
{
	MonoJitInfo *ji = NULL;
	if (!info)
		return NULL;
	mono_get_eh_callbacks ()->mono_walk_stack_with_state (last_managed, &info->suspend_state, MONO_UNWIND_SIGNAL_SAFE, &ji);
	return ji;
}

static void
abort_thread_internal (MonoInternalThread *thread, gboolean can_raise_exception, gboolean install_async_abort)
{
	MonoJitInfo *ji;
	MonoThreadInfo *info = NULL;
	gboolean protected_wrapper;
	gboolean running_managed;

	if (!mono_thread_info_new_interrupt_enabled ()) {
		signal_thread_state_change (thread);
		return;
	}

	/*
	FIXME this is insanely broken, it doesn't cause interruption to happen
	synchronously since passing FALSE to mono_thread_request_interruption makes sure it returns NULL
	*/
	if (thread == mono_thread_internal_current ()) {
		/* Do it synchronously */
		MonoException *exc = mono_thread_request_interruption (can_raise_exception); 
		if (exc)
			mono_raise_exception (exc);
#ifndef HOST_WIN32
		wapi_interrupt_thread (thread->handle);
#endif
		return;
	}

	/*FIXME we need to check 2 conditions here, request to interrupt this thread or if the target died*/
	if (!(info = mono_thread_info_safe_suspend_sync ((MonoNativeThreadId)(gsize)thread->tid, TRUE))) {
		return;
	}

	if (mono_get_eh_callbacks ()->mono_install_handler_block_guard (&info->suspend_state)) {
		mono_thread_info_resume (mono_thread_info_get_tid (info));
		return;
	}

	/*someone is already interrupting it*/
	if (InterlockedCompareExchange (&thread->interruption_requested, 1, 0) == 1) {
		mono_thread_info_resume (mono_thread_info_get_tid (info));
		return;
	}
	InterlockedIncrement (&thread_interruption_requested);

	ji = mono_thread_info_get_last_managed (info);
	protected_wrapper = ji && mono_threads_is_critical_method (ji->method);
	running_managed = mono_jit_info_match (ji, MONO_CONTEXT_GET_IP (&info->suspend_state.ctx));

	if (!protected_wrapper && running_managed) {
		/*We are in managed code*/
		/*Set the thread to call */
		if (install_async_abort)
			mono_thread_info_setup_async_call (info, self_interrupt_thread, NULL);
		mono_thread_info_resume (mono_thread_info_get_tid (info));
	} else {
		gpointer interrupt_handle;
		/* 
		 * This will cause waits to be broken.
		 * It will also prevent the thread from entering a wait, so if the thread returns
		 * from the wait before it receives the abort signal, it will just spin in the wait
		 * functions in the io-layer until the signal handler calls QueueUserAPC which will
		 * make it return.
		 */
#ifndef HOST_WIN32
		interrupt_handle = wapi_prepare_interrupt_thread (thread->handle);
#endif
		mono_thread_info_resume (mono_thread_info_get_tid (info));
#ifndef HOST_WIN32
		wapi_finish_interrupt_thread (interrupt_handle);
#endif
	}
	/*FIXME we need to wait for interruption to complete -- figure out how much into interruption we should wait for here*/
}

static void
transition_to_suspended (MonoInternalThread *thread)
{
	if ((thread->state & ThreadState_SuspendRequested) == 0) {
		g_assert (0); /*FIXME we should not reach this */
		/*Make sure we balance the suspend count.*/
		mono_thread_info_resume ((MonoNativeThreadId)(gpointer)(gsize)thread->tid);
	} else {
		thread->state &= ~ThreadState_SuspendRequested;
		thread->state |= ThreadState_Suspended;
	}
	LeaveCriticalSection (thread->synch_cs);
}

static void
suspend_thread_internal (MonoInternalThread *thread, gboolean interrupt)
{
	if (!mono_thread_info_new_interrupt_enabled ()) {
		signal_thread_state_change (thread);
		return;
	}

	EnterCriticalSection (thread->synch_cs);
	if (thread == mono_thread_internal_current ()) {
		transition_to_suspended (thread);
		mono_thread_info_self_suspend ();
	} else {
		MonoThreadInfo *info;
		MonoJitInfo *ji;
		gboolean protected_wrapper;
		gboolean running_managed;

		/*A null info usually means the thread is already dead. */
		if (!(info = mono_thread_info_safe_suspend_sync ((MonoNativeThreadId)(gsize)thread->tid, interrupt))) {
			LeaveCriticalSection (thread->synch_cs);
			return;
		}

		ji = mono_thread_info_get_last_managed (info);
		protected_wrapper = ji && mono_threads_is_critical_method (ji->method);
		running_managed = mono_jit_info_match (ji, MONO_CONTEXT_GET_IP (&info->suspend_state.ctx));

		if (running_managed && !protected_wrapper) {
			transition_to_suspended (thread);
		} else {
			gpointer interrupt_handle;

			if (InterlockedCompareExchange (&thread->interruption_requested, 1, 0) == 0)
				InterlockedIncrement (&thread_interruption_requested);
#ifndef HOST_WIN32
			if (interrupt)
				interrupt_handle = wapi_prepare_interrupt_thread (thread->handle);
#endif
			mono_thread_info_resume (mono_thread_info_get_tid (info));
#ifndef HOST_WIN32
			if (interrupt)
				wapi_finish_interrupt_thread (interrupt_handle);
#endif
			LeaveCriticalSection (thread->synch_cs);
		}
	}
}

/*This is called with @thread synch_cs held and it must release it*/
static void
self_suspend_internal (MonoInternalThread *thread)
{
	if (!mono_thread_info_new_interrupt_enabled ()) {
		thread->state &= ~ThreadState_SuspendRequested;
		thread->state |= ThreadState_Suspended;
		thread->suspend_event = CreateEvent (NULL, TRUE, FALSE, NULL);
		if (thread->suspend_event == NULL) {
			LeaveCriticalSection (thread->synch_cs);
			return;
		}
		if (thread->suspended_event)
			SetEvent (thread->suspended_event);

		LeaveCriticalSection (thread->synch_cs);

		if (shutting_down) {
			/* After we left the lock, the runtime might shut down so everything becomes invalid */
			for (;;)
				Sleep (1000);
		}
		
		WaitForSingleObject (thread->suspend_event, INFINITE);
		
		EnterCriticalSection (thread->synch_cs);

		CloseHandle (thread->suspend_event);
		thread->suspend_event = NULL;
		thread->state &= ~ThreadState_Suspended;
	
		/* The thread that requested the resume will have replaced this event
		 * and will be waiting for it
		 */
		SetEvent (thread->resume_event);

		LeaveCriticalSection (thread->synch_cs);
		return;
	}

	transition_to_suspended (thread);
	mono_thread_info_self_suspend ();
}

/*This is called with @thread synch_cs held and it must release it*/
static gboolean
resume_thread_internal (MonoInternalThread *thread)
{
	if (!mono_thread_info_new_interrupt_enabled ()) {
		thread->resume_event = CreateEvent (NULL, TRUE, FALSE, NULL);
		if (thread->resume_event == NULL) {
			LeaveCriticalSection (thread->synch_cs);
			return FALSE;
		}

		/* Awake the thread */
		SetEvent (thread->suspend_event);

		LeaveCriticalSection (thread->synch_cs);

		/* Wait for the thread to awake */
		WaitForSingleObject (thread->resume_event, INFINITE);
		CloseHandle (thread->resume_event);
		thread->resume_event = NULL;
		return TRUE;
	}

	LeaveCriticalSection (thread->synch_cs);	
	/* Awake the thread */
	if (!mono_thread_info_resume ((MonoNativeThreadId)(gpointer)(gsize)thread->tid))
		return FALSE;
	EnterCriticalSection (thread->synch_cs);
	thread->state &= ~ThreadState_Suspended;
	LeaveCriticalSection (thread->synch_cs);
	return TRUE;
}


/*
 * mono_thread_is_foreign:
 * @thread: the thread to query
 *
 * This function allows one to determine if a thread was created by the mono runtime and has
 * a well defined lifecycle or it's a foreigh one, created by the native environment.
 *
 * Returns: true if @thread was not created by the runtime.
 */
mono_bool
mono_thread_is_foreign (MonoThread *thread)
{
	MonoThreadInfo *info = thread->internal_thread->thread_info;
	return info->runtime_thread == FALSE;
}
