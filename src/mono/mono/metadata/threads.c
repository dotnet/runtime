/*
 * threads.c: Thread support internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *	Paolo Molaro (lupus@ximian.com)
 *	Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>

#include <glib.h>
#include <signal.h>
#include <string.h>

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
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-time.h>

#include <mono/metadata/gc-internal.h>

/*#define THREAD_DEBUG(a) do { a; } while (0)*/
#define THREAD_DEBUG(a)
/*#define THREAD_WAIT_DEBUG(a) do { a; } while (0)*/
#define THREAD_WAIT_DEBUG(a)
/*#define LIBGC_DEBUG(a) do { a; } while (0)*/
#define LIBGC_DEBUG(a)

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
	MonoDomain *domain;
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

typedef struct {
	gpointer p;
	MonoHazardousFreeFunc free_func;
} DelayedFreeItem;

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

/* The hash of existing threads (key is thread ID) that need joining
 * before exit
 */
static MonoGHashTable *threads=NULL;

/*
 * Threads which are starting up and they are not in the 'threads' hash yet.
 * When handle_store is called for a thread, it will be removed from this hash table.
 * Protected by mono_threads_lock ().
 */
static MonoGHashTable *threads_starting_up = NULL;

/* The TLS key that holds the MonoObject assigned to each thread */
static guint32 current_object_key = -1;

#ifdef HAVE_KW_THREAD
/* we need to use both the Tls* functions and __thread because
 * the gc needs to see all the threads 
 */
static __thread MonoThread * tls_current_object MONO_TLS_FAST;
#define SET_CURRENT_OBJECT(x) do { \
	tls_current_object = x; \
	TlsSetValue (current_object_key, x); \
} while (FALSE)
#define GET_CURRENT_OBJECT() tls_current_object
#else
#define SET_CURRENT_OBJECT(x) TlsSetValue (current_object_key, x);
#define GET_CURRENT_OBJECT() (MonoThread*) TlsGetValue (current_object_key);
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

static void thread_adjust_static_data (MonoThread *thread);
static void mono_init_static_data_info (StaticDataInfo *static_data);
static guint32 mono_alloc_static_data_slot (StaticDataInfo *static_data, guint32 size, guint32 align);
static gboolean mono_thread_resume (MonoThread* thread);
static void mono_thread_start (MonoThread *thread);
static void signal_thread_state_change (MonoThread *thread);

/* Spin lock for InterlockedXXX 64 bit functions */
#define mono_interlocked_lock() EnterCriticalSection (&interlocked_mutex)
#define mono_interlocked_unlock() LeaveCriticalSection (&interlocked_mutex)
static CRITICAL_SECTION interlocked_mutex;

/* global count of thread interruptions requested */
static gint32 thread_interruption_requested = 0;

/* Event signaled when a thread changes its background mode */
static HANDLE background_change_event;

/* The table for small ID assignment */
static CRITICAL_SECTION small_id_mutex;
static int small_id_table_size = 0;
static int small_id_next = 0;
static int highest_small_id = -1;
static MonoThread **small_id_table = NULL;

/* The hazard table */
#define HAZARD_TABLE_MAX_SIZE	16384 /* There cannot be more threads than this number. */
static volatile int hazard_table_size = 0;
static MonoThreadHazardPointers * volatile hazard_table = NULL;

/* The table where we keep pointers to blocks to be freed but that
   have to wait because they're guarded by a hazard pointer. */
static CRITICAL_SECTION delayed_free_table_mutex;
static GArray *delayed_free_table = NULL;

static gboolean shutting_down = FALSE;

guint32
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
static gboolean handle_store(MonoThread *thread)
{
	mono_threads_lock ();

	THREAD_DEBUG (g_message ("%s: thread %p ID %"G_GSIZE_FORMAT, __func__, thread, (gsize)thread->tid));

	if (threads_starting_up)
		mono_g_hash_table_remove (threads_starting_up, thread);

	if (shutting_down) {
		mono_threads_unlock ();
		return FALSE;
	}

	if(threads==NULL) {
		MONO_GC_REGISTER_ROOT (threads);
		threads=mono_g_hash_table_new(NULL, NULL);
	}

	/* We don't need to duplicate thread->handle, because it is
	 * only closed when the thread object is finalized by the GC.
	 */
	mono_g_hash_table_insert(threads, (gpointer)(gsize)(thread->tid),
				 thread);

	mono_threads_unlock ();

	return TRUE;
}

static gboolean handle_remove(MonoThread *thread)
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

/*
 * Allocate a small thread id.
 *
 * FIXME: The biggest part of this function is very similar to
 * domain_id_alloc() in domain.c and should be merged.
 */
static int
small_id_alloc (MonoThread *thread)
{
	int id = -1, i;

	EnterCriticalSection (&small_id_mutex);

	if (!small_id_table) {
		small_id_table_size = 2;
		small_id_table = mono_gc_alloc_fixed (small_id_table_size * sizeof (MonoThread*), NULL);
	}
	for (i = small_id_next; i < small_id_table_size; ++i) {
		if (!small_id_table [i]) {
			id = i;
			break;
		}
	}
	if (id == -1) {
		for (i = 0; i < small_id_next; ++i) {
			if (!small_id_table [i]) {
				id = i;
				break;
			}
		}
	}
	if (id == -1) {
		MonoThread **new_table;
		int new_size = small_id_table_size * 2;
		if (new_size >= (1 << 16))
			g_assert_not_reached ();
		id = small_id_table_size;
		new_table = mono_gc_alloc_fixed (new_size * sizeof (MonoThread*), NULL);
		memcpy (new_table, small_id_table, small_id_table_size * sizeof (void*));
		mono_gc_free_fixed (small_id_table);
		small_id_table = new_table;
		small_id_table_size = new_size;
	}
	thread->small_id = id;
	g_assert (small_id_table [id] == NULL);
	small_id_table [id] = thread;
	small_id_next++;
	if (small_id_next > small_id_table_size)
		small_id_next = 0;

	if (id >= hazard_table_size) {
		gpointer page_addr;
		int pagesize = mono_pagesize ();
		int num_pages = (hazard_table_size * sizeof (MonoThreadHazardPointers) + pagesize - 1) / pagesize;

		if (hazard_table == NULL) {
			hazard_table = mono_valloc (NULL,
				sizeof (MonoThreadHazardPointers) * HAZARD_TABLE_MAX_SIZE,
				MONO_MMAP_NONE);
		}

		g_assert (hazard_table != NULL);
		page_addr = (guint8*)hazard_table + num_pages * pagesize;

		g_assert (id < HAZARD_TABLE_MAX_SIZE);

		mono_mprotect (page_addr, pagesize, MONO_MMAP_READ | MONO_MMAP_WRITE);

		++num_pages;
		hazard_table_size = num_pages * pagesize / sizeof (MonoThreadHazardPointers);

		g_assert (id < hazard_table_size);

		hazard_table [id].hazard_pointers [0] = NULL;
		hazard_table [id].hazard_pointers [1] = NULL;
	}

	if (id > highest_small_id) {
		highest_small_id = id;
		mono_memory_write_barrier ();
	}

	LeaveCriticalSection (&small_id_mutex);

	return id;
}

static void
small_id_free (int id)
{
	g_assert (id >= 0 && id < small_id_table_size);
	g_assert (small_id_table [id] != NULL);

	small_id_table [id] = NULL;
}

static gboolean
is_pointer_hazardous (gpointer p)
{
	int i;
	int highest = highest_small_id;

	g_assert (highest < hazard_table_size);

	for (i = 0; i <= highest; ++i) {
		if (hazard_table [i].hazard_pointers [0] == p
				|| hazard_table [i].hazard_pointers [1] == p)
			return TRUE;
	}

	return FALSE;
}

MonoThreadHazardPointers*
mono_hazard_pointer_get (void)
{
	MonoThread *current_thread = mono_thread_current ();

	if (!(current_thread && current_thread->small_id >= 0)) {
		static MonoThreadHazardPointers emerg_hazard_table;
		g_warning ("Thread %p may have been prematurely finalized", current_thread);
		return &emerg_hazard_table;
	}

	return &hazard_table [current_thread->small_id];
}

static void
try_free_delayed_free_item (int index)
{
	if (delayed_free_table->len > index) {
		DelayedFreeItem item;

		item.p = NULL;
		EnterCriticalSection (&delayed_free_table_mutex);
		/* We have to check the length again because another
		   thread might have freed an item before we acquired
		   the lock. */
		if (delayed_free_table->len > index) {
			item = g_array_index (delayed_free_table, DelayedFreeItem, index);

			if (!is_pointer_hazardous (item.p))
				g_array_remove_index_fast (delayed_free_table, index);
			else
				item.p = NULL;
		}
		LeaveCriticalSection (&delayed_free_table_mutex);

		if (item.p != NULL)
			item.free_func (item.p);
	}
}

void
mono_thread_hazardous_free_or_queue (gpointer p, MonoHazardousFreeFunc free_func)
{
	int i;

	/* First try to free a few entries in the delayed free
	   table. */
	for (i = 2; i >= 0; --i)
		try_free_delayed_free_item (i);

	/* Now see if the pointer we're freeing is hazardous.  If it
	   isn't, free it.  Otherwise put it in the delay list. */
	if (is_pointer_hazardous (p)) {
		DelayedFreeItem item = { p, free_func };

		++mono_stats.hazardous_pointer_count;

		EnterCriticalSection (&delayed_free_table_mutex);
		g_array_append_val (delayed_free_table, item);
		LeaveCriticalSection (&delayed_free_table_mutex);
	} else
		free_func (p);
}

void
mono_thread_hazardous_try_free_all (void)
{
	int len;
	int i;

	if (!delayed_free_table)
		return;

	len = delayed_free_table->len;

	for (i = len - 1; i >= 0; --i)
		try_free_delayed_free_item (i);
}

static void ensure_synch_cs_set (MonoThread *thread)
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
static void thread_cleanup (MonoThread *thread)
{
	g_assert (thread != NULL);

	/* if the thread is not in the hash it has been removed already */
	if (!handle_remove (thread))
		return;
	mono_release_type_locks (thread);

	EnterCriticalSection (thread->synch_cs);

	thread->state |= ThreadState_Stopped;
	thread->state &= ~ThreadState_Background;

	LeaveCriticalSection (thread->synch_cs);
	
	mono_profiler_thread_end (thread->tid);

	if (thread == mono_thread_current ())
		mono_thread_pop_appdomain_ref ();

	if (thread->serialized_culture_info)
		g_free (thread->serialized_culture_info);

	thread->cached_culture_info = NULL;

	mono_gc_free_fixed (thread->static_data);
	thread->static_data = NULL;

	if (mono_thread_cleanup_fn)
		mono_thread_cleanup_fn (thread);

	small_id_free (thread->small_id);
	thread->small_id = -2;
}

static guint32 WINAPI start_wrapper(void *data)
{
	struct StartInfo *start_info=(struct StartInfo *)data;
	guint32 (*start_func)(void *);
	void *start_arg;
	gsize tid;
	MonoThread *thread=start_info->obj;
	MonoObject *start_delegate = start_info->delegate;

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Start wrapper", __func__, GetCurrentThreadId ()));

	/* We can be sure start_info->obj->tid and
	 * start_info->obj->handle have been set, because the thread
	 * was created suspended, and these values were set before the
	 * thread resumed
	 */

	tid=thread->tid;

	SET_CURRENT_OBJECT (thread);

	/* Every thread references the appdomain which created it */
	mono_thread_push_appdomain_ref (start_info->domain);
	
	if (!mono_domain_set (start_info->domain, FALSE)) {
		/* No point in raising an appdomain_unloaded exception here */
		/* FIXME: Cleanup here */
		mono_thread_pop_appdomain_ref ();
		return 0;
	}

	start_func = start_info->func;
	start_arg = start_info->start_arg;

	/* This MUST be called before any managed code can be
	 * executed, as it calls the callback function that (for the
	 * jit) sets the lmf marker.
	 */
	mono_thread_new_init (tid, &tid, start_func);
	thread->stack_ptr = &tid;

	LIBGC_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT",%d) Setting thread stack to %p", __func__, GetCurrentThreadId (), getpid (), thread->stack_ptr));

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Setting current_object_key to %p", __func__, GetCurrentThreadId (), thread));

	mono_profiler_thread_start (tid);

	/* On 2.0 profile (and higher), set explicitly since state might have been
	   Unknown */
	if (mono_get_runtime_info ()->framework_version [0] != '1') {
		if (thread->apartment_state == ThreadApartmentState_Unknown)
			thread->apartment_state = ThreadApartmentState_MTA;
	}

	mono_thread_init_apartment_state ();

	if(thread->start_notify!=NULL) {
		/* Let the thread that called Start() know we're
		 * ready
		 */
		ReleaseSemaphore (thread->start_notify, 1, NULL);
	}

	MONO_GC_UNREGISTER_ROOT (start_info->start_arg);
	g_free (start_info);

	thread_adjust_static_data (thread);
#ifdef DEBUG
	g_message ("%s: start_wrapper for %"G_GSIZE_FORMAT, __func__,
		   thread->tid);
#endif

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

	thread_cleanup (thread);

	/* Do any cleanup needed for apartment state. This
	 * cannot be done in thread_cleanup since thread_cleanup could be 
	 * called for a thread other than the current thread.
	 * mono_thread_cleanup_apartment_state cleans up apartment
	 * for the current thead */
	mono_thread_cleanup_apartment_state ();

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

void mono_thread_new_init (gsize tid, gpointer stack_start, gpointer func)
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

void mono_thread_create_internal (MonoDomain *domain, gpointer func, gpointer arg, gboolean threadpool_thread)
{
	MonoThread *thread;
	HANDLE thread_handle;
	struct StartInfo *start_info;
	gsize tid;

	thread=(MonoThread *)mono_object_new (domain,
					      mono_defaults.thread_class);

	start_info=g_new0 (struct StartInfo, 1);
	start_info->func = func;
	start_info->obj = thread;
	start_info->domain = domain;
	start_info->start_arg = arg;

	/* 
	 * The argument may be an object reference, and there is no ref to keep it alive
	 * when the new thread is started but not yet registered with the collector.
	 */
	MONO_GC_REGISTER_ROOT (start_info->start_arg);

	mono_threads_lock ();
	if (shutting_down) {
		mono_threads_unlock ();
		return;
	}
	if (threads_starting_up == NULL) {
		MONO_GC_REGISTER_ROOT (threads_starting_up);
		threads_starting_up = mono_g_hash_table_new (NULL, NULL);
	}
	mono_g_hash_table_insert (threads_starting_up, thread, thread);
	mono_threads_unlock ();	

	/* Create suspended, so we can do some housekeeping before the thread
	 * starts
	 */
	thread_handle = CreateThread(NULL, default_stacksize_for_thread (thread), (LPTHREAD_START_ROUTINE)start_wrapper, start_info,
				     CREATE_SUSPENDED, &tid);
	THREAD_DEBUG (g_message ("%s: Started thread ID %"G_GSIZE_FORMAT" (handle %p)", __func__, tid, thread_handle));
	if (thread_handle == NULL) {
		/* The thread couldn't be created, so throw an exception */
		MONO_GC_UNREGISTER_ROOT (start_info->start_arg);
		mono_threads_lock ();
		mono_g_hash_table_remove (threads_starting_up, thread);
		mono_threads_unlock ();
		g_free (start_info);
		mono_raise_exception (mono_get_exception_execution_engine ("Couldn't create thread"));
		return;
	}

	thread->handle=thread_handle;
	thread->tid=tid;
	thread->apartment_state=ThreadApartmentState_Unknown;
	small_id_alloc (thread);

	thread->synch_cs = g_new0 (CRITICAL_SECTION, 1);
	InitializeCriticalSection (thread->synch_cs);

	thread->threadpool_thread = threadpool_thread;

	if (handle_store (thread))
		ResumeThread (thread_handle);
}

void
mono_thread_create (MonoDomain *domain, gpointer func, gpointer arg)
{
	mono_thread_create_internal (domain, func, arg, FALSE);
}

/*
 * mono_thread_get_stack_bounds:
 *
 *   Return the address and size of the current threads stack. Return NULL as the stack
 * address if the stack address cannot be determined.
 */
void
mono_thread_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
#if defined(HAVE_PTHREAD_GET_STACKSIZE_NP) && defined(HAVE_PTHREAD_GET_STACKADDR_NP)
	*staddr = (guint8*)pthread_get_stackaddr_np (pthread_self ());
	*stsize = pthread_get_stacksize_np (pthread_self ());
	return;
	/* FIXME: simplify the mess below */
#elif !defined(PLATFORM_WIN32)
	pthread_attr_t attr;
	guint8 *current = (guint8*)&attr;

	pthread_attr_init (&attr);
#ifdef HAVE_PTHREAD_GETATTR_NP
		pthread_getattr_np (pthread_self(), &attr);
#else
#ifdef HAVE_PTHREAD_ATTR_GET_NP
		pthread_attr_get_np (pthread_self(), &attr);
#elif defined(sun)
		*staddr = NULL;
		pthread_attr_getstacksize (&attr, &stsize);
#else
		*staddr = NULL;
		*stsize = 0;
		return;
#endif
#endif

#ifndef sun
		pthread_attr_getstack (&attr, (void**)staddr, stsize);
		if (*staddr)
			g_assert ((current > *staddr) && (current < *staddr + *stsize));
#endif

		pthread_attr_destroy (&attr); 
#endif
}	

MonoThread *
mono_thread_attach (MonoDomain *domain)
{
	MonoThread *thread;
	HANDLE thread_handle;
	gsize tid;

	if ((thread = mono_thread_current ())) {
		if (domain != mono_domain_get ())
			mono_domain_set (domain, TRUE);
		/* Already attached */
		return thread;
	}

	if (!mono_gc_register_thread (&domain)) {
		g_error ("Thread %"G_GSIZE_FORMAT" calling into managed code is not registered with the GC. On UNIX, this can be fixed by #include-ing <gc.h> before <pthread.h> in the file containing the thread creation code.", GetCurrentThreadId ());
	}

	thread = (MonoThread *)mono_object_new (domain,
						mono_defaults.thread_class);

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
	thread->apartment_state=ThreadApartmentState_Unknown;
	small_id_alloc (thread);
	thread->stack_ptr = &tid;

	thread->synch_cs = g_new0 (CRITICAL_SECTION, 1);
	InitializeCriticalSection (thread->synch_cs);

	THREAD_DEBUG (g_message ("%s: Attached thread ID %"G_GSIZE_FORMAT" (handle %p)", __func__, tid, thread_handle));

	if (!handle_store (thread)) {
		/* Mono is shutting down, so just wait for the end */
		for (;;)
			Sleep (10000);
	}

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Setting current_object_key to %p", __func__, GetCurrentThreadId (), thread));

	SET_CURRENT_OBJECT (thread);
	mono_domain_set (domain, TRUE);

	thread_adjust_static_data (thread);

	if (mono_thread_attach_cb) {
		guint8 *staddr;
		size_t stsize;

		mono_thread_get_stack_bounds (&staddr, &stsize);

		if (staddr == NULL)
			mono_thread_attach_cb (tid, &tid);
		else
			mono_thread_attach_cb (tid, staddr + stsize);
	}

	return(thread);
}

void
mono_thread_detach (MonoThread *thread)
{
	g_return_if_fail (thread != NULL);

	THREAD_DEBUG (g_message ("%s: mono_thread_detach for %p (%"G_GSIZE_FORMAT")", __func__, thread, (gsize)thread->tid));
	
	thread_cleanup (thread);

	SET_CURRENT_OBJECT (NULL);

	/* Don't need to CloseHandle this thread, even though we took a
	 * reference in mono_thread_attach (), because the GC will do it
	 * when the Thread object is finalised.
	 */
}

void
mono_thread_exit ()
{
	MonoThread *thread = mono_thread_current ();

	THREAD_DEBUG (g_message ("%s: mono_thread_exit for %p (%"G_GSIZE_FORMAT")", __func__, thread, (gsize)thread->tid));

	thread_cleanup (thread);
	SET_CURRENT_OBJECT (NULL);

	/* we could add a callback here for embedders to use. */
	if (thread == mono_thread_get_main ())
		exit (mono_environment_exitcode_get ());
	ExitThread (-1);
}

HANDLE ves_icall_System_Threading_Thread_Thread_internal(MonoThread *this,
							 MonoObject *start)
{
	guint32 (*start_func)(void *);
	struct StartInfo *start_info;
	HANDLE thread;
	gsize tid;
	
	MONO_ARCH_SAVE_REGS;

	THREAD_DEBUG (g_message("%s: Trying to start a new thread: this (%p) start (%p)", __func__, this, start));

	ensure_synch_cs_set (this);

	EnterCriticalSection (this->synch_cs);

	if ((this->state & ThreadState_Unstarted) == 0) {
		LeaveCriticalSection (this->synch_cs);
		mono_raise_exception (mono_get_exception_thread_state ("Thread has already been started."));
		return NULL;
	}

	this->small_id = -1;

	if ((this->state & ThreadState_Aborted) != 0) {
		LeaveCriticalSection (this->synch_cs);
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
		start_info->domain = mono_domain_get ();

		this->start_notify=CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
		if(this->start_notify==NULL) {
			LeaveCriticalSection (this->synch_cs);
			g_warning ("%s: CreateSemaphore error 0x%x", __func__, GetLastError ());
			return(NULL);
		}

		mono_threads_lock ();
		if (threads_starting_up == NULL) {
			MONO_GC_REGISTER_ROOT (threads_starting_up);
			threads_starting_up = mono_g_hash_table_new (NULL, NULL);
		}
		mono_g_hash_table_insert (threads_starting_up, this, this);
		mono_threads_unlock ();	

		thread=CreateThread(NULL, default_stacksize_for_thread (this), (LPTHREAD_START_ROUTINE)start_wrapper, start_info,
				    CREATE_SUSPENDED, &tid);
		if(thread==NULL) {
			LeaveCriticalSection (this->synch_cs);
			mono_threads_lock ();
			mono_g_hash_table_remove (threads_starting_up, this);
			mono_threads_unlock ();
			g_warning("%s: CreateThread error 0x%x", __func__, GetLastError());
			return(NULL);
		}
		
		this->handle=thread;
		this->tid=tid;
		small_id_alloc (this);

		/* Don't call handle_store() here, delay it to Start.
		 * We can't join a thread (trying to will just block
		 * forever) until it actually starts running, so don't
		 * store the handle till then.
		 */

		mono_thread_start (this);
		
		this->state &= ~ThreadState_Unstarted;

		THREAD_DEBUG (g_message ("%s: Started thread ID %"G_GSIZE_FORMAT" (handle %p)", __func__, tid, thread));

		LeaveCriticalSection (this->synch_cs);
		return(thread);
	}
}

void ves_icall_System_Threading_Thread_Thread_init (MonoThread *this)
{
	MONO_ARCH_SAVE_REGS;

	ensure_synch_cs_set (this);
}

void ves_icall_System_Threading_Thread_Thread_free_internal (MonoThread *this,
							     HANDLE thread)
{
	MONO_ARCH_SAVE_REGS;

	THREAD_DEBUG (g_message ("%s: Closing thread %p, handle %p", __func__, this, thread));

	CloseHandle (thread);

	DeleteCriticalSection (this->synch_cs);
	g_free (this->synch_cs);
	this->synch_cs = NULL;
}

static void mono_thread_start (MonoThread *thread)
{
	MONO_ARCH_SAVE_REGS;

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Launching thread %p (%"G_GSIZE_FORMAT")", __func__, GetCurrentThreadId (), thread, (gsize)thread->tid));

	/* Only store the handle when the thread is about to be
	 * launched, to avoid the main thread deadlocking while trying
	 * to clean up a thread that will never be signalled.
	 */
	if (!handle_store (thread))
		return;

	ResumeThread (thread->handle);

	if(thread->start_notify!=NULL) {
		/* Wait for the thread to set up its TLS data etc, so
		 * theres no potential race condition if someone tries
		 * to look up the data believing the thread has
		 * started
		 */

		THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") waiting for thread %p (%"G_GSIZE_FORMAT") to start", __func__, GetCurrentThreadId (), thread, (gsize)thread->tid));

		WaitForSingleObjectEx (thread->start_notify, INFINITE, FALSE);
		CloseHandle (thread->start_notify);
		thread->start_notify = NULL;
	}

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Done launching thread %p (%"G_GSIZE_FORMAT")", __func__, GetCurrentThreadId (), thread, (gsize)thread->tid));
}

void ves_icall_System_Threading_Thread_Sleep_internal(gint32 ms)
{
	MonoThread *thread = mono_thread_current ();
	
	MONO_ARCH_SAVE_REGS;

	THREAD_DEBUG (g_message ("%s: Sleeping for %d ms", __func__, ms));

	mono_thread_current_check_pending_interrupt ();
	
	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);
	
	SleepEx(ms,TRUE);
	
	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);
}

void ves_icall_System_Threading_Thread_SpinWait_internal (gint32 iterations)
{
	gint32 i;
	
	for(i = 0; i < iterations; i++) {
		/* We're busy waiting, but at least we can tell the
		 * scheduler to let someone else have a go...
		 */
		Sleep (0);
	}
}

gint32
ves_icall_System_Threading_Thread_GetDomainID (void) 
{
	MONO_ARCH_SAVE_REGS;

	return mono_domain_get()->domain_id;
}

MonoString* 
ves_icall_System_Threading_Thread_GetName_internal (MonoThread *this_obj)
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
ves_icall_System_Threading_Thread_SetName_internal (MonoThread *this_obj, MonoString *name)
{
	ensure_synch_cs_set (this_obj);
	
	EnterCriticalSection (this_obj->synch_cs);
	
	if (this_obj->name) {
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
	
	LeaveCriticalSection (this_obj->synch_cs);
}

static MonoObject*
lookup_cached_culture (MonoThread *this, MonoDomain *domain, int start_idx)
{
	MonoObject *res;
	int i;

	if (this->cached_culture_info) {
		domain = mono_domain_get ();
		for (i = start_idx; i < start_idx + NUM_CACHED_CULTURES; ++i) {
			res = mono_array_get (this->cached_culture_info, MonoObject*, i);
			if (res && res->vtable->domain == domain)
				return res;
		}
	}

	return NULL;
}

MonoObject*
ves_icall_System_Threading_Thread_GetCachedCurrentCulture (MonoThread *this)
{
	return lookup_cached_culture (this, mono_domain_get (), CULTURES_START_IDX);
}

MonoArray*
ves_icall_System_Threading_Thread_GetSerializedCurrentCulture (MonoThread *this)
{
	MonoArray *res;

	ensure_synch_cs_set (this);
	
	EnterCriticalSection (this->synch_cs);
	
	if (this->serialized_culture_info) {
		res = mono_array_new (mono_domain_get (), mono_defaults.byte_class, this->serialized_culture_info_len);
		memcpy (mono_array_addr (res, guint8, 0), this->serialized_culture_info, this->serialized_culture_info_len);
	} else {
		res = NULL;
	}

	LeaveCriticalSection (this->synch_cs);

	return res;
}

static void
cache_culture (MonoThread *this, MonoObject *culture, int start_idx)
{
	int i;
	MonoDomain *domain = mono_domain_get ();
	MonoObject *obj;
	int free_slot = -1;
	int same_domain_slot = -1;

	ensure_synch_cs_set (this);
	
	EnterCriticalSection (this->synch_cs);
	
	if (!this->cached_culture_info)
		MONO_OBJECT_SETREF (this, cached_culture_info, mono_array_new (mono_object_domain (this), mono_defaults.object_class, NUM_CACHED_CULTURES * 2));

	for (i = start_idx; i < start_idx + NUM_CACHED_CULTURES; ++i) {
		obj = mono_array_get (this->cached_culture_info, MonoObject*, i);
		/* Free entry */
		if (!obj) {
			free_slot = i;
			/* we continue, because there may be a slot used with the same domain */
			continue;
		}
		/* Replace */
		if (obj->vtable->domain == domain) {
			same_domain_slot = i;
			break;
		}
	}
	if (same_domain_slot >= 0)
		mono_array_setref (this->cached_culture_info, same_domain_slot, culture);
	else if (free_slot >= 0)
		mono_array_setref (this->cached_culture_info, free_slot, culture);
	/* we may want to replace an existing entry here, even when no suitable slot is found */

	LeaveCriticalSection (this->synch_cs);
}

void
ves_icall_System_Threading_Thread_SetCachedCurrentCulture (MonoThread *this, MonoObject *culture)
{
	cache_culture (this, culture, CULTURES_START_IDX);
}

void
ves_icall_System_Threading_Thread_SetSerializedCurrentCulture (MonoThread *this, MonoArray *arr)
{
	ensure_synch_cs_set (this);
	
	EnterCriticalSection (this->synch_cs);
	
	if (this->serialized_culture_info)
		g_free (this->serialized_culture_info);
	this->serialized_culture_info = g_new0 (guint8, mono_array_length (arr));
	this->serialized_culture_info_len = mono_array_length (arr);
	memcpy (this->serialized_culture_info, mono_array_addr (arr, guint8, 0), mono_array_length (arr));

	LeaveCriticalSection (this->synch_cs);
}


MonoObject*
ves_icall_System_Threading_Thread_GetCachedCurrentUICulture (MonoThread *this)
{
	return lookup_cached_culture (this, mono_domain_get (), UICULTURES_START_IDX);
}

MonoArray*
ves_icall_System_Threading_Thread_GetSerializedCurrentUICulture (MonoThread *this)
{
	MonoArray *res;

	ensure_synch_cs_set (this);
	
	EnterCriticalSection (this->synch_cs);
	
	if (this->serialized_ui_culture_info) {
		res = mono_array_new (mono_domain_get (), mono_defaults.byte_class, this->serialized_ui_culture_info_len);
		memcpy (mono_array_addr (res, guint8, 0), this->serialized_ui_culture_info, this->serialized_ui_culture_info_len);
	} else {
		res = NULL;
	}

	LeaveCriticalSection (this->synch_cs);

	return res;
}

void
ves_icall_System_Threading_Thread_SetCachedCurrentUICulture (MonoThread *this, MonoObject *culture)
{
	cache_culture (this, culture, UICULTURES_START_IDX);
}

void
ves_icall_System_Threading_Thread_SetSerializedCurrentUICulture (MonoThread *this, MonoArray *arr)
{
	ensure_synch_cs_set (this);
	
	EnterCriticalSection (this->synch_cs);
	
	if (this->serialized_ui_culture_info)
		g_free (this->serialized_ui_culture_info);
	this->serialized_ui_culture_info = g_new0 (guint8, mono_array_length (arr));
	this->serialized_ui_culture_info_len = mono_array_length (arr);
	memcpy (this->serialized_ui_culture_info, mono_array_addr (arr, guint8, 0), mono_array_length (arr));

	LeaveCriticalSection (this->synch_cs);
}

/* the jit may read the compiled code of this function */
MonoThread *
mono_thread_current (void)
{
	THREAD_DEBUG (g_message ("%s: returning %p", __func__, GET_CURRENT_OBJECT ()));
	return GET_CURRENT_OBJECT ();
}

gboolean ves_icall_System_Threading_Thread_Join_internal(MonoThread *this,
							 int ms, HANDLE thread)
{
	MonoThread *cur_thread = mono_thread_current ();
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;
	
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

/* FIXME: exitContext isnt documented */
gboolean ves_icall_System_Threading_WaitHandle_WaitAll_internal(MonoArray *mono_handles, gint32 ms, gboolean exitContext)
{
	HANDLE *handles;
	guint32 numhandles;
	guint32 ret;
	guint32 i;
	MonoObject *waitHandle;
	MonoThread *thread = mono_thread_current ();
		
	MONO_ARCH_SAVE_REGS;

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
	
	ret=WaitForMultipleObjectsEx(numhandles, handles, TRUE, ms, TRUE);

	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);

	g_free(handles);

	if(ret==WAIT_FAILED) {
		THREAD_WAIT_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Wait failed", __func__, GetCurrentThreadId ()));
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT || ret == WAIT_IO_COMPLETION) {
		/* Do we want to try again if we get
		 * WAIT_IO_COMPLETION? The documentation for
		 * WaitHandle doesn't give any clues.  (We'd have to
		 * fiddle with the timeout if we retry.)
		 */
		THREAD_WAIT_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Wait timed out", __func__, GetCurrentThreadId ()));
		return(FALSE);
	}
	
	return(TRUE);
}

/* FIXME: exitContext isnt documented */
gint32 ves_icall_System_Threading_WaitHandle_WaitAny_internal(MonoArray *mono_handles, gint32 ms, gboolean exitContext)
{
	HANDLE *handles;
	guint32 numhandles;
	guint32 ret;
	guint32 i;
	MonoObject *waitHandle;
	MonoThread *thread = mono_thread_current ();
		
	MONO_ARCH_SAVE_REGS;

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
	
	ret=WaitForMultipleObjectsEx(numhandles, handles, FALSE, ms, TRUE);

	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);
	
	g_free(handles);

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
	MonoThread *thread = mono_thread_current ();
	
	MONO_ARCH_SAVE_REGS;

	THREAD_WAIT_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") waiting for %p, %d ms", __func__, GetCurrentThreadId (), handle, ms));
	
	if(ms== -1) {
		ms=INFINITE;
	}
	
	mono_thread_current_check_pending_interrupt ();

	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);
	
	ret=WaitForSingleObjectEx (handle, ms, TRUE);
	
	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);
	
	if(ret==WAIT_FAILED) {
		THREAD_WAIT_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Wait failed", __func__, GetCurrentThreadId ()));
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT || ret == WAIT_IO_COMPLETION) {
		/* Do we want to try again if we get
		 * WAIT_IO_COMPLETION? The documentation for
		 * WaitHandle doesn't give any clues.  (We'd have to
		 * fiddle with the timeout if we retry.)
		 */
		THREAD_WAIT_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Wait timed out", __func__, GetCurrentThreadId ()));
		return(FALSE);
	}
	
	return(TRUE);
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
	MONO_ARCH_SAVE_REGS;

	return (MonoObject *) InterlockedExchangePointer((gpointer *) location, value);
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
	MONO_ARCH_SAVE_REGS;

	return (MonoObject *) InterlockedCompareExchangePointer((gpointer *) location, value, comparand);
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
	MONO_ARCH_SAVE_REGS;

	return InterlockedCompareExchangePointer ((gpointer *)location, value, comparand);
}

MonoObject*
ves_icall_System_Threading_Interlocked_Exchange_T (MonoObject **location, MonoObject *value)
{
	MONO_ARCH_SAVE_REGS;

	return InterlockedExchangePointer ((gpointer *)location, value);
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
ves_icall_System_Threading_Thread_ClrState (MonoThread* this, guint32 state)
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
ves_icall_System_Threading_Thread_SetState (MonoThread* this, guint32 state)
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
ves_icall_System_Threading_Thread_GetState (MonoThread* this)
{
	guint32 state;

	ensure_synch_cs_set (this);
	
	EnterCriticalSection (this->synch_cs);
	
	state = this->state;

	LeaveCriticalSection (this->synch_cs);
	
	return state;
}

void ves_icall_System_Threading_Thread_Interrupt_internal (MonoThread *this)
{
	gboolean throw = FALSE;
	
	ensure_synch_cs_set (this);
	
	EnterCriticalSection (this->synch_cs);
	
	this->thread_interrupt_requested = TRUE;
	
	if (this->state & ThreadState_WaitSleepJoin) {
		throw = TRUE;
	}
	
	LeaveCriticalSection (this->synch_cs);
	
	if (throw) {
		signal_thread_state_change (this);
	}
}

void mono_thread_current_check_pending_interrupt ()
{
	MonoThread *thread = mono_thread_current ();
	gboolean throw = FALSE;

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
#ifdef PLATFORM_WIN32
	return -1;
#else
#ifndef	SIGRTMIN
	return SIGUSR1;
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
#endif
#endif /* PLATFORM_WIN32 */
}

#ifdef PLATFORM_WIN32
static void CALLBACK interruption_request_apc (ULONG_PTR param)
{
	MonoException* exc = mono_thread_request_interruption (FALSE);
	if (exc) mono_raise_exception (exc);
}
#endif /* PLATFORM_WIN32 */

/*
 * signal_thread_state_change
 *
 * Tells the thread that his state has changed and it has to enter the new
 * state as soon as possible.
 */
static void signal_thread_state_change (MonoThread *thread)
{
	if (thread == mono_thread_current ()) {
		/* Do it synchronously */
		MonoException *exc = mono_thread_request_interruption (FALSE); 
		if (exc)
			mono_raise_exception (exc);
	}

#ifdef PLATFORM_WIN32
	QueueUserAPC ((PAPCFUNC)interruption_request_apc, thread->handle, NULL);
#else
	/* fixme: store the state somewhere */
#ifdef PTHREAD_POINTER_ID
	pthread_kill ((gpointer)(gsize)(thread->tid), mono_thread_get_abort_signal ());
#else
	pthread_kill (thread->tid, mono_thread_get_abort_signal ());
#endif
#endif /* PLATFORM_WIN32 */
}

void
ves_icall_System_Threading_Thread_Abort (MonoThread *thread, MonoObject *state)
{
	MONO_ARCH_SAVE_REGS;

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
	MONO_OBJECT_SETREF (thread, abort_state, state);
	thread->abort_exc = NULL;

	LeaveCriticalSection (thread->synch_cs);

	THREAD_DEBUG (g_message ("%s: (%"G_GSIZE_FORMAT") Abort requested for %p (%"G_GSIZE_FORMAT")", __func__, GetCurrentThreadId (), thread, (gsize)thread->tid));
	
	/* Make sure the thread is awake */
	mono_thread_resume (thread);
	
	signal_thread_state_change (thread);
}

void
ves_icall_System_Threading_Thread_ResetAbort (void)
{
	MonoThread *thread = mono_thread_current ();

	MONO_ARCH_SAVE_REGS;

	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);

	thread->state &= ~ThreadState_AbortRequested;
	
	if (!thread->abort_exc) {
		const char *msg = "Unable to reset abort because no abort was requested";
		LeaveCriticalSection (thread->synch_cs);
		mono_raise_exception (mono_get_exception_thread_state (msg));
	} else {
		thread->abort_exc = NULL;
		thread->abort_state = NULL;
	}
	
	LeaveCriticalSection (thread->synch_cs);
}

static gboolean
mono_thread_suspend (MonoThread *thread)
{
	MONO_ARCH_SAVE_REGS;

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

	signal_thread_state_change (thread);
	return TRUE;
}

void
ves_icall_System_Threading_Thread_Suspend (MonoThread *thread)
{
	if (!mono_thread_suspend (thread))
		mono_raise_exception (mono_get_exception_thread_state ("Thread has not been started, or is dead."));
}

static gboolean
mono_thread_resume (MonoThread *thread)
{
	MONO_ARCH_SAVE_REGS;

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
	
	thread->resume_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	if (thread->resume_event == NULL) {
		LeaveCriticalSection (thread->synch_cs);
		return(FALSE);
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

void
ves_icall_System_Threading_Thread_Resume (MonoThread *thread)
{
	if (!mono_thread_resume (thread))
		mono_raise_exception (mono_get_exception_thread_state ("Thread has not been started, or is dead."));
}

static gboolean
find_wrapper (MonoMethod *m, gint no, gint ilo, gboolean managed, gpointer data)
{
	if (managed)
		return TRUE;

	if (m->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE ||
		m->wrapper_type == MONO_WRAPPER_XDOMAIN_INVOKE ||
		m->wrapper_type == MONO_WRAPPER_XDOMAIN_DISPATCH) 
	{
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

void mono_thread_stop (MonoThread *thread)
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
	
	signal_thread_state_change (thread);
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

void mono_thread_init (MonoThreadStartCB start_cb,
		       MonoThreadAttachCB attach_cb)
{
	MONO_GC_REGISTER_ROOT (small_id_table);
	InitializeCriticalSection(&threads_mutex);
	InitializeCriticalSection(&interlocked_mutex);
	InitializeCriticalSection(&contexts_mutex);
	InitializeCriticalSection(&delayed_free_table_mutex);
	InitializeCriticalSection(&small_id_mutex);
	
	background_change_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	g_assert(background_change_event != NULL);
	
	mono_init_static_data_info (&thread_static_info);
	mono_init_static_data_info (&context_static_info);

	current_object_key=TlsAlloc();
	THREAD_DEBUG (g_message ("%s: Allocated current_object_key %d", __func__, current_object_key));

	mono_thread_start_cb = start_cb;
	mono_thread_attach_cb = attach_cb;

	delayed_free_table = g_array_new (FALSE, FALSE, sizeof (DelayedFreeItem));

	/* Get a pseudo handle to the current process.  This is just a
	 * kludge so that wapi can build a process handle if needed.
	 * As a pseudo handle is returned, we don't need to clean
	 * anything up.
	 */
	GetCurrentProcess ();
}

void mono_thread_cleanup (void)
{
	mono_thread_hazardous_try_free_all ();

#if !defined(PLATFORM_WIN32) && !defined(RUN_IN_SUBTHREAD)
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

	g_array_free (delayed_free_table, TRUE);
	delayed_free_table = NULL;

	TlsFree (current_object_key);
}

void
mono_threads_install_cleanup (MonoThreadCleanupFunc func)
{
	mono_thread_cleanup_fn = func;
}

void
mono_thread_set_manage_callback (MonoThread *thread, MonoThreadManageCallback func)
{
	thread->manage_callback = func;
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
	MonoThread *threads[MAXIMUM_WAIT_OBJECTS];
	guint32 num;
};

static void wait_for_tids (struct wait_data *wait, guint32 timeout)
{
	guint32 i, ret;
	
	THREAD_DEBUG (g_message("%s: %d threads to wait for in this batch", __func__, wait->num));

	ret=WaitForMultipleObjectsEx(wait->num, wait->handles, TRUE, timeout, FALSE);

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

	ret=WaitForMultipleObjectsEx (count, wait->handles, FALSE, timeout, FALSE);

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
		MonoThread *thread=(MonoThread *)value;

		/* Ignore background threads, we abort them later */
		/* Do not lock here since it is not needed and the caller holds threads_lock */
		if (thread->state & ThreadState_Background) {
			THREAD_DEBUG (g_message ("%s: ignoring background thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
			return; /* just leave, ignore */
		}
		
		if (mono_gc_is_finalizer_thread (thread)) {
			THREAD_DEBUG (g_message ("%s: ignoring finalizer thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
			return;
		}

		if (thread == mono_thread_current ()) {
			THREAD_DEBUG (g_message ("%s: ignoring current thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
			return;
		}

		if (thread == mono_thread_get_main ()) {
			THREAD_DEBUG (g_message ("%s: ignoring main thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
			return;
		}

		handle = OpenThread (THREAD_ALL_ACCESS, TRUE, thread->tid);
		if (handle == NULL) {
			THREAD_DEBUG (g_message ("%s: ignoring unopenable thread %"G_GSIZE_FORMAT, __func__, (gsize)thread->tid));
			return;
		}
		
		THREAD_DEBUG (g_message ("%s: Invoking mono_thread_manage callback on thread %p", __func__, thread));
		if ((thread->manage_callback == NULL) || (thread->manage_callback (thread) == TRUE)) {
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
	MonoThread *thread = (MonoThread *) value;
	HANDLE handle;

	if (wait->num >= MAXIMUM_WAIT_OBJECTS)
		return FALSE;

	/* The finalizer thread is not a background thread */
	if (thread->tid != self && (thread->state & ThreadState_Background) != 0) {
	
		handle = OpenThread (THREAD_ALL_ACCESS, TRUE, thread->tid);
		if (handle == NULL)
			return FALSE;

		/* printf ("A: %d\n", wait->num); */
		wait->handles[wait->num]=thread->handle;
		wait->threads[wait->num]=thread;
		wait->num++;

		THREAD_DEBUG (g_print ("%s: Aborting id: %"G_GSIZE_FORMAT"\n", __func__, (gsize)thread->tid));
		mono_thread_stop (thread);
		return TRUE;
	}

	return (thread->tid != self && !mono_gc_is_finalizer_thread (thread)); 
}

static MonoException* mono_thread_execute_interruption (MonoThread *thread);

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
	MonoThread *current_thread = mono_thread_current ();

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

/** 
 * mono_threads_is_shutting_down:
 *
 * Returns whether a thread has commenced shutdown of Mono.  Note that
 * if the function returns FALSE the caller must not assume that
 * shutdown is not in progress, because the situation might have
 * changed since the function returned.  For that reason this function
 * is of very limited utility.
 */
gboolean
mono_threads_is_shutting_down (void)
{
	return shutting_down;
}

void mono_thread_manage (void)
{
	struct wait_data *wait=g_new0 (struct wait_data, 1);

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
		mono_g_hash_table_foreach (threads, build_wait_tids, wait);
		mono_threads_unlock ();
		if(wait->num>0) {
			/* Something to wait for */
			wait_for_tids_or_state_change (wait, INFINITE);
		}
		THREAD_DEBUG (g_message ("%s: I have %d threads after waiting.", __func__, wait->num));
	} while(wait->num>0);

	mono_threads_set_shutting_down ();

	/* No new threads will be created after this point */

	mono_runtime_set_shutting_down ();

	THREAD_DEBUG (g_message ("%s: threadpool cleanup", __func__));
	mono_thread_pool_cleanup ();

	/* 
	 * Remove everything but the finalizer thread and self.
	 * Also abort all the background threads
	 * */
	do {
		mono_threads_lock ();

		wait->num = 0;
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
#ifndef PLATFORM_WIN32
	sched_yield ();
#endif

	g_free (wait);
}

static void terminate_thread (gpointer key, gpointer value, gpointer user)
{
	MonoThread *thread=(MonoThread *)value;
	
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
	MonoThread *thread = (MonoThread*)value;
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
	struct wait_data *wait = g_new0 (struct wait_data, 1);
	int i;
	gsize self = GetCurrentThreadId ();
	gpointer *events;
	guint32 eventidx = 0;
	gboolean starting, finished;

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
		mono_threads_lock ();
		mono_g_hash_table_foreach (threads, collect_threads_for_suspend, wait);
		mono_threads_unlock ();

		events = g_new0 (gpointer, wait->num);
		eventidx = 0;
		/* Get the suspended events that we'll be waiting for */
		for (i = 0; i < wait->num; ++i) {
			MonoThread *thread = wait->threads [i];

			if ((thread->tid == self) || mono_gc_is_finalizer_thread (thread)) {
				//CloseHandle (wait->handles [i]);
				wait->threads [i] = NULL; /* ignore this thread in next loop */
				continue;
			}

			ensure_synch_cs_set (thread);
		
			EnterCriticalSection (thread->synch_cs);

			if ((thread->state & ThreadState_Suspended) != 0 || 
				(thread->state & ThreadState_SuspendRequested) != 0 ||
				(thread->state & ThreadState_StopRequested) != 0 ||
				(thread->state & ThreadState_Stopped) != 0) {
				LeaveCriticalSection (thread->synch_cs);
				CloseHandle (wait->handles [i]);
				wait->threads [i] = NULL; /* ignore this thread in next loop */
				continue;
			}

			/* Convert abort requests into suspend requests */
			if ((thread->state & ThreadState_AbortRequested) != 0)
				thread->state &= ~ThreadState_AbortRequested;
			
			thread->state |= ThreadState_SuspendRequested;

			if (thread->suspended_event == NULL) {
				thread->suspended_event = CreateEvent (NULL, TRUE, FALSE, NULL);
				if (thread->suspended_event == NULL) {
					/* Forget this one and go on to the next */
					LeaveCriticalSection (thread->synch_cs);
					continue;
				}
			}

			events [eventidx++] = thread->suspended_event;
			LeaveCriticalSection (thread->synch_cs);

			/* Signal the thread to suspend */
			signal_thread_state_change (thread);
		}

		if (eventidx > 0) {
			WaitForMultipleObjectsEx (eventidx, events, TRUE, 100, FALSE);
			for (i = 0; i < wait->num; ++i) {
				MonoThread *thread = wait->threads [i];

				if (thread == NULL)
					continue;
			
				EnterCriticalSection (thread->synch_cs);
				if ((thread->state & ThreadState_Suspended) != 0) {
					CloseHandle (thread->suspended_event);
					thread->suspended_event = NULL;
				}
				LeaveCriticalSection (thread->synch_cs);
			}
		} else {
			/* 
			 * If there are threads which are starting up, we wait until they
			 * are suspended when they try to register in the threads hash.
			 * This is guaranteed to finish, since the threads which can create new
			 * threads get suspended after a while.
			 * FIXME: The finalizer thread can still create new threads.
			 */
			mono_threads_lock ();
			starting = mono_g_hash_table_size (threads_starting_up) > 0;
			mono_threads_unlock ();
			if (starting)
				Sleep (100);
			else
				finished = TRUE;
		}

		g_free (events);
	}

	g_free (wait);
}

static void
collect_threads (gpointer key, gpointer value, gpointer user_data)
{
	MonoThread *thread = (MonoThread*)value;
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

/**
 * mono_threads_request_thread_dump:
 *
 *   Ask all threads except the current to print their stacktrace to stdout.
 */
void
mono_threads_request_thread_dump (void)
{
	struct wait_data *wait = g_new0 (struct wait_data, 1);
	int i;

	/* 
	 * Make a copy of the hashtable since we can't do anything with
	 * threads while threads_mutex is held.
	 */
	mono_threads_lock ();
	mono_g_hash_table_foreach (threads, collect_threads, wait);
	mono_threads_unlock ();

	for (i = 0; i < wait->num; ++i) {
		MonoThread *thread = wait->threads [i];

		if (!mono_gc_is_finalizer_thread (thread) && (thread != mono_thread_current ()) && !thread->thread_dump_requested) {
			thread->thread_dump_requested = TRUE;

			signal_thread_state_change (thread);
		}

		CloseHandle (wait->handles [i]);
	}
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
	MonoThread *thread = mono_thread_current ();

	if (thread) {
		/* printf ("PUSH REF: %"G_GSIZE_FORMAT" -> %s.\n", (gsize)thread->tid, domain->friendly_name); */
		mono_threads_lock ();
		thread->appdomain_refs = g_slist_prepend (thread->appdomain_refs, domain);
		mono_threads_unlock ();
	}
}

void
mono_thread_pop_appdomain_ref (void)
{
	MonoThread *thread = mono_thread_current ();

	if (thread) {
		/* printf ("POP REF: %"G_GSIZE_FORMAT" -> %s.\n", (gsize)thread->tid, ((MonoDomain*)(thread->appdomain_refs->data))->friendly_name); */
		mono_threads_lock ();
		/* FIXME: How can the list be empty ? */
		if (thread->appdomain_refs)
			thread->appdomain_refs = g_slist_remove (thread->appdomain_refs, thread->appdomain_refs->data);
		mono_threads_unlock ();
	}
}

gboolean
mono_thread_has_appdomain_ref (MonoThread *thread, MonoDomain *domain)
{
	gboolean res;
	mono_threads_lock ();
	res = g_slist_find (thread->appdomain_refs, domain) != NULL;
	mono_threads_unlock ();
	return res;
}

typedef struct abort_appdomain_data {
	struct wait_data wait;
	MonoDomain *domain;
} abort_appdomain_data;

static void
abort_appdomain_thread (gpointer key, gpointer value, gpointer user_data)
{
	MonoThread *thread = (MonoThread*)value;
	abort_appdomain_data *data = (abort_appdomain_data*)user_data;
	MonoDomain *domain = data->domain;

	if (mono_thread_has_appdomain_ref (thread, domain)) {
		/* printf ("ABORTING THREAD %p BECAUSE IT REFERENCES DOMAIN %s.\n", thread->tid, domain->friendly_name); */

		ves_icall_System_Threading_Thread_Abort (thread, NULL);

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
	abort_appdomain_data user_data;
	guint32 start_time;

	THREAD_DEBUG (g_message ("%s: starting abort", __func__));

	start_time = mono_msec_ticks ();
	do {
		mono_threads_lock ();

		user_data.domain = domain;
		user_data.wait.num = 0;
		mono_g_hash_table_foreach (threads, abort_appdomain_thread, &user_data);
		mono_threads_unlock ();

		if (user_data.wait.num > 0)
			/*
			 * We should wait for the threads either to abort, or to leave the
			 * domain. We can't do the latter, so we wait with a timeout.
			 */
			wait_for_tids (&user_data.wait, 100);

		/* Update remaining time */
		timeout -= mono_msec_ticks () - start_time;
		start_time = mono_msec_ticks ();

		if (timeout < 0)
			return FALSE;
	}
	while (user_data.wait.num > 0);

	THREAD_DEBUG (g_message ("%s: abort done", __func__));

	return TRUE;
}

static void
clear_cached_culture (gpointer key, gpointer value, gpointer user_data)
{
	MonoThread *thread = (MonoThread*)value;
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
	MonoThread *thread = mono_thread_current ();

	MONO_ARCH_SAVE_REGS;

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

#define NUM_STATIC_DATA_IDX 8
static const int static_data_size [NUM_STATIC_DATA_IDX] = {
	1024, 4096, 16384, 65536, 262144, 1048576, 4194304, 16777216
};


/*
 *  mono_alloc_static_data
 *
 *   Allocate memory blocks for storing threads or context static data
 */
static void 
mono_alloc_static_data (gpointer **static_data_ptr, guint32 offset)
{
	guint idx = (offset >> 24) - 1;
	int i;

	gpointer* static_data = *static_data_ptr;
	if (!static_data) {
		static_data = mono_gc_alloc_fixed (static_data_size [0], NULL);
		*static_data_ptr = static_data;
		static_data [0] = static_data;
	}

	for (i = 1; i <= idx; ++i) {
		if (static_data [i])
			continue;
		static_data [i] = mono_gc_alloc_fixed (static_data_size [i], NULL);
	}
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
thread_adjust_static_data (MonoThread *thread)
{
	guint32 offset;

	mono_threads_lock ();
	if (thread_static_info.offset || thread_static_info.idx > 0) {
		/* get the current allocated size */
		offset = thread_static_info.offset | ((thread_static_info.idx + 1) << 24);
		mono_alloc_static_data (&(thread->static_data), offset);
	}
	mono_threads_unlock ();
}

static void 
alloc_thread_static_data_helper (gpointer key, gpointer value, gpointer user)
{
	MonoThread *thread = value;
	guint32 offset = GPOINTER_TO_UINT (user);
	
	mono_alloc_static_data (&(thread->static_data), offset);
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

/*
 * The offset for a special static variable is composed of three parts:
 * a bit that indicates the type of static data (0:thread, 1:context),
 * an index in the array of chunks of memory for the thread (thread->static_data)
 * and an offset in that chunk of mem. This allows allocating less memory in the 
 * common case.
 */

guint32
mono_alloc_special_static_data (guint32 static_type, guint32 size, guint32 align)
{
	guint32 offset;
	if (static_type == SPECIAL_STATIC_THREAD)
	{
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
		/* This can be called during startup */
		if (threads != NULL)
			mono_g_hash_table_foreach (threads, alloc_thread_static_data_helper, GUINT_TO_POINTER (offset));
		mono_threads_unlock ();
	}
	else
	{
		g_assert (static_type == SPECIAL_STATIC_CONTEXT);
		mono_contexts_lock ();
		offset = mono_alloc_static_data_slot (&context_static_info, size, align);
		mono_contexts_unlock ();
		offset |= 0x80000000;	/* Set the high bit to indicate context static data */
	}
	return offset;
}

gpointer
mono_get_special_static_data (guint32 offset)
{
	/* The high bit means either thread (0) or static (1) data. */

	guint32 static_type = (offset & 0x80000000);
	int idx;

	offset &= 0x7fffffff;
	idx = (offset >> 24) - 1;

	if (static_type == 0)
	{
		MonoThread *thread = mono_thread_current ();
		return ((char*) thread->static_data [idx]) + (offset & 0xffffff);
	}
	else
	{
		/* Allocate static data block under demand, since we don't have a list
		// of contexts
		*/
		MonoAppContext *context = mono_context_get ();
		if (!context->static_data || !context->static_data [idx]) {
			mono_contexts_lock ();
			mono_alloc_static_data (&(context->static_data), offset);
			mono_contexts_unlock ();
		}
		return ((char*) context->static_data [idx]) + (offset & 0xffffff);	
	}
}

typedef struct {
	guint32 offset;
	guint32 size;
} TlsOffsetSize;

static void 
free_thread_static_data_helper (gpointer key, gpointer value, gpointer user)
{
	MonoThread *thread = value;
	TlsOffsetSize *data = user;
	int idx = (data->offset >> 24) - 1;
	char *ptr;

	if (!thread->static_data || !thread->static_data [idx])
		return;
	ptr = ((char*) thread->static_data [idx]) + (data->offset & 0xffffff);
	memset (ptr, 0, data->size);
}

static void
do_free_special (gpointer key, gpointer value, gpointer data)
{
	MonoClassField *field = key;
	guint32 offset = GPOINTER_TO_UINT (value);
	guint32 static_type = (offset & 0x80000000);
	gint32 align;
	guint32 size;
	size = mono_type_size (field->type, &align);
	/*g_print ("free %s , size: %d, offset: %x\n", field->name, size, offset);*/
	if (static_type == 0) {
		TlsOffsetSize data;
		MonoThreadDomainTls *item = g_new0 (MonoThreadDomainTls, 1);
		data.offset = offset & 0x7fffffff;
		data.size = size;
		if (threads != NULL)
			mono_g_hash_table_foreach (threads, free_thread_static_data_helper, &data);
		item->offset = offset;
		item->size = size;
		item->next = thread_static_info.freelist;
		thread_static_info.freelist = item;
	} else {
		/* FIXME: free context static data as well */
	}
}

void
mono_alloc_special_static_data_free (GHashTable *special_static_fields)
{
	mono_threads_lock ();
	g_hash_table_foreach (special_static_fields, do_free_special, NULL);
	mono_threads_unlock ();
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
	MonoThread *thread = (MonoThread*)value;
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

#ifdef PLATFORM_WIN32
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
static MonoException* mono_thread_execute_interruption (MonoThread *thread)
{
	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);

	if (thread->interruption_requested) {
		/* this will consume pending APC calls */
		WaitForSingleObjectEx (GetCurrentThread(), 0, TRUE);
		InterlockedDecrement (&thread_interruption_requested);
		thread->interruption_requested = FALSE;
	}

	if ((thread->state & ThreadState_AbortRequested) != 0) {
		if (thread->abort_exc == NULL)
			MONO_OBJECT_SETREF (thread, abort_exc, mono_get_exception_thread_abort ());
		LeaveCriticalSection (thread->synch_cs);
		return thread->abort_exc;
	}
	else if ((thread->state & ThreadState_SuspendRequested) != 0) {
		thread->state &= ~ThreadState_SuspendRequested;
		thread->state |= ThreadState_Suspended;
		thread->suspend_event = CreateEvent (NULL, TRUE, FALSE, NULL);
		if (thread->suspend_event == NULL) {
			LeaveCriticalSection (thread->synch_cs);
			return(NULL);
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
		
		return NULL;
	}
	else if ((thread->state & ThreadState_StopRequested) != 0) {
		/* FIXME: do this through the JIT? */

		LeaveCriticalSection (thread->synch_cs);
		
		mono_thread_exit ();
		return NULL;
	} else if (thread->thread_interrupt_requested) {

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
MonoException* mono_thread_request_interruption (gboolean running_managed)
{
	MonoThread *thread = mono_thread_current ();

	/* The thread may already be stopping */
	if (thread == NULL) 
		return NULL;
	
	ensure_synch_cs_set (thread);

	/* FIXME: This is NOT signal safe */
	EnterCriticalSection (thread->synch_cs);
	
	if (thread->interruption_requested) {
		LeaveCriticalSection (thread->synch_cs);
		
		return NULL;
	}

	if (!running_managed || is_running_protected_wrapper ()) {
		/* Can't stop while in unmanaged code. Increase the global interruption
		   request count. When exiting the unmanaged method the count will be
		   checked and the thread will be interrupted. */
		
		InterlockedIncrement (&thread_interruption_requested);
		thread->interruption_requested = TRUE;

		LeaveCriticalSection (thread->synch_cs);

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
		LeaveCriticalSection (thread->synch_cs);
		
		return mono_thread_execute_interruption (thread);
	}
}

gboolean mono_thread_interruption_requested ()
{
	if (thread_interruption_requested) {
		MonoThread *thread = mono_thread_current ();
		/* The thread may already be stopping */
		if (thread != NULL) 
			return (thread->interruption_requested);
	}
	return FALSE;
}

static void mono_thread_interruption_checkpoint_request (gboolean bypass_abort_protection)
{
	MonoThread *thread = mono_thread_current ();

	/* The thread may already be stopping */
	if (thread == NULL)
		return;

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
	MonoThread *thread = mono_thread_current ();

	/* The thread may already be stopping */
	if (thread == NULL)
		return NULL;

	if (thread->interruption_requested && !is_running_protected_wrapper ()) {
		return mono_thread_execute_interruption (thread);
	}
	else
		return NULL;
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
	MonoThread* thread;
	thread = mono_thread_current ();

#ifdef PLATFORM_WIN32
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
#ifdef PLATFORM_WIN32
	MonoThread* thread;
	thread = mono_thread_current ();

	if (thread && thread->apartment_state != ThreadApartmentState_Unknown) {
		CoUninitialize ();
	}
#endif
}

void
mono_thread_set_state (MonoThread *thread, MonoThreadState state)
{
	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);
	thread->state |= state;
	LeaveCriticalSection (thread->synch_cs);
}

void
mono_thread_clr_state (MonoThread *thread, MonoThreadState state)
{
	ensure_synch_cs_set (thread);
	
	EnterCriticalSection (thread->synch_cs);
	thread->state &= ~state;
	LeaveCriticalSection (thread->synch_cs);
}

gboolean
mono_thread_test_state (MonoThread *thread, MonoThreadState test)
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
