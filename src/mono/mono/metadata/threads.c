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
#ifdef PLATFORM_WIN32
#define _WIN32_WINNT 0x0500
#endif

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
#include <mono/utils/mono-compiler.h>

#include <mono/os/gc_wrapper.h>

/*#define THREAD_DEBUG(a) do { a; } while (0)*/
#define THREAD_DEBUG(a)
/*#define THREAD_WAIT_DEBUG(a) do { a; } while (0)*/
#define THREAD_WAIT_DEBUG(a)
/*#define LIBGC_DEBUG(a) do { a; } while (0)*/
#define LIBGC_DEBUG(a)

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
 
typedef struct {
	int idx;
	int offset;
} StaticDataInfo;

/* Number of cached culture objects in the MonoThread->culture_info array */
#define NUM_CACHED_CULTURES 4

/*
 * The "os_handle" field of the WaitHandle class.
 */
static MonoClassField *wait_handle_os_handle_field = NULL;

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
static MonoThreadCleanupFunc mono_thread_cleanup = NULL;

/* function called when a new thread has been created */
static MonoThreadCallbacks *mono_thread_callbacks = NULL;

/* The default stack size for each thread */
static guint32 default_stacksize = 0;
#define default_stacksize_for_thread(thread) ((thread)->stack_size? (thread)->stack_size: default_stacksize)

static void thread_adjust_static_data (MonoThread *thread);
static void mono_init_static_data_info (StaticDataInfo *static_data);
static guint32 mono_alloc_static_data_slot (StaticDataInfo *static_data, guint32 size, guint32 align);
static gboolean mono_thread_resume (MonoThread* thread);
static void mono_thread_start (MonoThread *thread);

/* Spin lock for InterlockedXXX 64 bit functions */
#define mono_interlocked_lock() EnterCriticalSection (&interlocked_mutex)
#define mono_interlocked_unlock() LeaveCriticalSection (&interlocked_mutex)
static CRITICAL_SECTION interlocked_mutex;

/* global count of thread interruptions requested */
static gint32 thread_interruption_requested = 0;

/* Event signaled when a thread changes its background mode */
static HANDLE background_change_event;

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
 */
static void handle_store(MonoThread *thread)
{
	mono_threads_lock ();

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": thread %p ID %"G_GSIZE_FORMAT, thread, (gsize)thread->tid));

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
}

static void handle_remove(gsize tid)
{
	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": thread ID %"G_GSIZE_FORMAT, tid));

	mono_threads_lock ();
	

	if (threads)
		mono_g_hash_table_remove (threads, (gpointer)tid);
	
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
}

static void thread_cleanup (MonoThread *thread)
{
	g_assert (thread != NULL);
	
	mono_release_type_locks (thread);

	if (!mono_monitor_enter (thread->synch_lock))
		return;

	thread->state |= ThreadState_Stopped;
	mono_monitor_exit (thread->synch_lock);

	mono_profiler_thread_end (thread->tid);
	handle_remove (thread->tid);

	mono_thread_pop_appdomain_ref ();

	if (thread->serialized_culture_info)
		g_free (thread->serialized_culture_info);

	mono_gc_free_fixed (thread->culture_info);
	mono_gc_free_fixed (thread->ui_culture_info);

	if (mono_thread_cleanup)
		mono_thread_cleanup (thread);
}

static guint32 WINAPI start_wrapper(void *data)
{
	struct StartInfo *start_info=(struct StartInfo *)data;
	guint32 (*start_func)(void *);
	void *start_arg;
	guint32 tid;
	MonoThread *thread=start_info->obj;
	MonoObject *start_delegate = start_info->delegate;

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") Start wrapper", GetCurrentThreadId ()));
	
	/* We can be sure start_info->obj->tid and
	 * start_info->obj->handle have been set, because the thread
	 * was created suspended, and these values were set before the
	 * thread resumed
	 */

	tid=thread->tid;

	SET_CURRENT_OBJECT (thread);
	
	if (!mono_domain_set (start_info->domain, FALSE)) {
		/* No point in raising an appdomain_unloaded exception here */
		/* FIXME: Cleanup here */
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

	LIBGC_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION
		   ": (%"G_GSIZE_FORMAT",%d) Setting thread stack to %p",
		   GetCurrentThreadId (), getpid (), thread->stack_ptr));

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION
		   ": (%"G_GSIZE_FORMAT") Setting current_object_key to %p",
		   GetCurrentThreadId (), thread));

	mono_profiler_thread_start (tid);

	if(thread->start_notify!=NULL) {
		/* Let the thread that called Start() know we're
		 * ready
		 */
		ReleaseSemaphore (thread->start_notify, 1, NULL);
	}
	
	g_free (start_info);

	/* Every thread references the appdomain which created it */
	mono_thread_push_appdomain_ref (mono_domain_get ());

	thread_adjust_static_data (thread);
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION "start_wrapper for %"G_GSIZE_FORMAT,
		   thread->tid);
#endif

	/* start_func is set only for unamanged start functions */
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

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") Start wrapper terminating", GetCurrentThreadId ()));

	/* Remove the reference to the thread object in the TLS data,
	 * so the thread object can be finalized.  This won't be
	 * reached if the thread threw an uncaught exception, so those
	 * thread handles will stay referenced :-( (This is due to
	 * missing support for scanning thread-specific data in the
	 * Boehm GC - the io-layer keeps a GC-visible hash of pointers
	 * to TLS data.)
	 */
	SET_CURRENT_OBJECT (NULL);

	thread_cleanup (thread);

	return(0);
}

void mono_thread_new_init (gsize tid, gpointer stack_start, gpointer func)
{
	if (mono_thread_start_cb) {
		mono_thread_start_cb (tid, stack_start, func);
	}

	if (mono_thread_callbacks)
		(* mono_thread_callbacks->thread_created) (tid, stack_start, func);
}

void mono_threads_set_default_stacksize (guint32 stacksize)
{
	default_stacksize = stacksize;
}

guint32 mono_threads_get_default_stacksize (void)
{
	return default_stacksize;
}

void mono_thread_create (MonoDomain *domain, gpointer func, gpointer arg)
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
	
	/* Create suspended, so we can do some housekeeping before the thread
	 * starts
	 */
	thread_handle = CreateThread(NULL, default_stacksize_for_thread (thread), (LPTHREAD_START_ROUTINE)start_wrapper, start_info,
				     CREATE_SUSPENDED, &tid);
	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": Started thread ID %"G_GSIZE_FORMAT" (handle %p)",
		  tid, thread_handle));
	if (thread_handle == NULL) {
		/* The thread couldn't be created, so throw an exception */
		mono_raise_exception (mono_get_exception_execution_engine ("Couldn't create thread"));
		return;
	}

	thread->handle=thread_handle;
	thread->tid=tid;

	thread->synch_lock=mono_object_new (domain, mono_defaults.object_class);
						  
	handle_store(thread);

	ResumeThread (thread_handle);
}

MonoThread *
mono_thread_attach (MonoDomain *domain)
{
	MonoThread *thread;
	HANDLE thread_handle;
	gsize tid;

	if ((thread = mono_thread_current ())) {
		/* Already attached */
		return thread;
	}

	if (!mono_gc_register_thread (&domain)) {
		g_error ("Thread %p calling into managed code is not registered with the GC. On UNIX, this can be fixed by #include-ing <gc.h> before <pthread.h> in the file containing the thread creation code.", GetCurrentThread ());
	}

	thread = (MonoThread *)mono_object_new (domain,
						mono_defaults.thread_class);

	thread_handle = GetCurrentThread ();
	g_assert (thread_handle);

	tid=GetCurrentThreadId ();

#ifdef PLATFORM_WIN32
	/* 
	 * The handle returned by GetCurrentThread () is a pseudo handle, so it can't be used to
	 * refer to the thread from other threads for things like aborting.
	 */
	DuplicateHandle (GetCurrentProcess (), thread_handle, GetCurrentProcess (), &thread_handle, 
					 THREAD_ALL_ACCESS, TRUE, 0);
#endif

	thread->handle=thread_handle;
	thread->tid=tid;
	thread->synch_lock=mono_object_new (domain, mono_defaults.object_class);

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": Attached thread ID %"G_GSIZE_FORMAT" (handle %p)",
		  tid, thread_handle));

	handle_store(thread);

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") Setting current_object_key to %p",
		   GetCurrentThreadId (), thread));

	SET_CURRENT_OBJECT (thread);
	mono_domain_set (domain, TRUE);

	thread_adjust_static_data (thread);

	if (mono_thread_attach_cb) {
		mono_thread_attach_cb (tid, &tid);
	}

	return(thread);
}

void
mono_thread_detach (MonoThread *thread)
{
	g_return_if_fail (thread != NULL);

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION "mono_thread_detach for %"G_GSIZE_FORMAT, thread->tid));
	SET_CURRENT_OBJECT (NULL);
	
	thread_cleanup (thread);
}

void
mono_thread_exit ()
{
	MonoThread *thread = mono_thread_current ();

	SET_CURRENT_OBJECT (NULL);
	thread_cleanup (thread);

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

	THREAD_DEBUG (g_message(G_GNUC_PRETTY_FUNCTION
		  ": Trying to start a new thread: this (%p) start (%p)", this, start));

	mono_monitor_enter (this->synch_lock);

	if ((this->state & ThreadState_Unstarted) == 0) {
		mono_monitor_exit (this->synch_lock);
		mono_raise_exception (mono_get_exception_thread_state ("Thread has already been started."));
		return NULL;
	}

	if ((this->state & ThreadState_Aborted) != 0) {
		mono_monitor_exit (this->synch_lock);
		return this;
	}
	start_func = NULL;
	{
		/* This is freed in start_wrapper */
		start_info = g_new0 (struct StartInfo, 1);
		start_info->func = start_func;
		start_info->start_arg = this->start_obj;
		start_info->delegate = start;
		start_info->obj = this;
		start_info->domain = mono_domain_get ();

		this->start_notify=CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
		if(this->start_notify==NULL) {
			mono_monitor_exit (this->synch_lock);
			g_warning (G_GNUC_PRETTY_FUNCTION ": CreateSemaphore error 0x%x", GetLastError ());
			return(NULL);
		}

		thread=CreateThread(NULL, default_stacksize_for_thread (this), (LPTHREAD_START_ROUTINE)start_wrapper, start_info,
				    CREATE_SUSPENDED, &tid);
		if(thread==NULL) {
			mono_monitor_exit (this->synch_lock);
			g_warning(G_GNUC_PRETTY_FUNCTION
				  ": CreateThread error 0x%x", GetLastError());
			return(NULL);
		}
		
		this->handle=thread;
		this->tid=tid;

		/* Don't call handle_store() here, delay it to Start.
		 * We can't join a thread (trying to will just block
		 * forever) until it actually starts running, so don't
		 * store the handle till then.
		 */

		mono_thread_start (this);
		
		this->state &= ~ThreadState_Unstarted;

		THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": Started thread ID %"G_GSIZE_FORMAT" (handle %p)", tid, thread));

		mono_monitor_exit (this->synch_lock);
		return(thread);
	}
}

void ves_icall_System_Threading_Thread_Thread_free_internal (MonoThread *this,
							     HANDLE thread)
{
	MONO_ARCH_SAVE_REGS;

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": Closing thread %p, handle %p", this, thread));

	CloseHandle (thread);
}

static void mono_thread_start (MonoThread *thread)
{
	MONO_ARCH_SAVE_REGS;

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") Launching thread %p (%"G_GSIZE_FORMAT")", GetCurrentThreadId (), thread, (gsize)thread->tid));

	/* Only store the handle when the thread is about to be
	 * launched, to avoid the main thread deadlocking while trying
	 * to clean up a thread that will never be signalled.
	 */
	handle_store (thread);

	if (mono_thread_callbacks)
		(* mono_thread_callbacks->start_resume) (thread->tid);

	ResumeThread (thread->handle);

	if (mono_thread_callbacks)
		(* mono_thread_callbacks->end_resume) (thread->tid);

	if(thread->start_notify!=NULL) {
		/* Wait for the thread to set up its TLS data etc, so
		 * theres no potential race condition if someone tries
		 * to look up the data believing the thread has
		 * started
		 */

		THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") waiting for thread %p (%"G_GSIZE_FORMAT") to start", GetCurrentThreadId (), thread, (gsize)thread->tid));

		WaitForSingleObjectEx (thread->start_notify, INFINITE, FALSE);
		CloseHandle (thread->start_notify);
		thread->start_notify = NULL;
	}

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") Done launching thread %p (%"G_GSIZE_FORMAT")", GetCurrentThreadId (), thread, (gsize)thread->tid));
}

void ves_icall_System_Threading_Thread_Sleep_internal(gint32 ms)
{
	MonoThread *thread = mono_thread_current ();
	
	MONO_ARCH_SAVE_REGS;

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": Sleeping for %d ms", ms));

	mono_monitor_enter (thread->synch_lock);
	thread->state |= ThreadState_WaitSleepJoin;
	mono_monitor_exit (thread->synch_lock);
	
	SleepEx(ms,TRUE);
	
	mono_monitor_enter (thread->synch_lock);
	thread->state &= ~ThreadState_WaitSleepJoin;
	mono_monitor_exit (thread->synch_lock);
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
	mono_monitor_enter (this_obj->synch_lock);
	
	if (!this_obj->name)
		str = NULL;
	else
		str = mono_string_new_utf16 (mono_domain_get (), this_obj->name, this_obj->name_len);
	
	mono_monitor_exit (this_obj->synch_lock);
	return str;
}

void 
ves_icall_System_Threading_Thread_SetName_internal (MonoThread *this_obj, MonoString *name)
{
	mono_monitor_enter (this_obj->synch_lock);
	
	if (this_obj->name) {
		mono_monitor_exit (this_obj->synch_lock);
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
	
	mono_monitor_exit (this_obj->synch_lock);
}

MonoObject*
ves_icall_System_Threading_Thread_GetCachedCurrentCulture (MonoThread *this)
{
	MonoObject *res;
	MonoDomain *domain;
	int i;

	/* No need to lock here */
	if (this->culture_info) {
		domain = mono_domain_get ();
		for (i = 0; i < NUM_CACHED_CULTURES; ++i) {
			res = this->culture_info [i];
			if (res && res->vtable->domain == domain)
				return res;
		}
	}

	return NULL;
}

MonoArray*
ves_icall_System_Threading_Thread_GetSerializedCurrentCulture (MonoThread *this)
{
	MonoArray *res;

	mono_monitor_enter (this->synch_lock);
	if (this->serialized_culture_info) {
		res = mono_array_new (mono_domain_get (), mono_defaults.byte_class, this->serialized_culture_info_len);
		memcpy (mono_array_addr (res, guint8, 0), this->serialized_culture_info, this->serialized_culture_info_len);
	}
	else
		res = NULL;
	mono_monitor_exit (this->synch_lock);

	return res;
}

void
ves_icall_System_Threading_Thread_SetCachedCurrentCulture (MonoThread *this, MonoObject *culture)
{
	int i;
	MonoDomain *domain = mono_domain_get ();

	mono_monitor_enter (this->synch_lock);
	if (!this->culture_info) {
		this->culture_info = mono_gc_alloc_fixed (sizeof (MonoObject*) * NUM_CACHED_CULTURES, NULL);
	}

	for (i = 0; i < NUM_CACHED_CULTURES; ++i) {
		if (this->culture_info [i]) {
			if (this->culture_info [i]->vtable->domain == domain)
				/* Replace */
				break;
		}
		else
			/* Free entry */
			break;
	}
	if (i < NUM_CACHED_CULTURES)
		this->culture_info [i] = culture;
	mono_monitor_exit (this->synch_lock);
}

void
ves_icall_System_Threading_Thread_SetSerializedCurrentCulture (MonoThread *this, MonoArray *arr)
{
	mono_monitor_enter (this->synch_lock);
	if (this->serialized_culture_info)
		g_free (this->serialized_culture_info);
	this->serialized_culture_info = g_new0 (guint8, mono_array_length (arr));
	this->serialized_culture_info_len = mono_array_length (arr);
	memcpy (this->serialized_culture_info, mono_array_addr (arr, guint8, 0), mono_array_length (arr));
	mono_monitor_exit (this->synch_lock);
}


MonoObject*
ves_icall_System_Threading_Thread_GetCachedCurrentUICulture (MonoThread *this)
{
	MonoObject *res;
	MonoDomain *domain;
	int i;

	/* No need to lock here */
	if (this->ui_culture_info) {
		domain = mono_domain_get ();
		for (i = 0; i < NUM_CACHED_CULTURES; ++i) {
			res = this->ui_culture_info [i];
			if (res && res->vtable->domain == domain)
				return res;
		}
	}

	return NULL;
}

MonoArray*
ves_icall_System_Threading_Thread_GetSerializedCurrentUICulture (MonoThread *this)
{
	MonoArray *res;

	mono_monitor_enter (this->synch_lock);
	if (this->serialized_ui_culture_info) {
		res = mono_array_new (mono_domain_get (), mono_defaults.byte_class, this->serialized_ui_culture_info_len);
		memcpy (mono_array_addr (res, guint8, 0), this->serialized_ui_culture_info, this->serialized_ui_culture_info_len);
	}
	else
		res = NULL;
	mono_monitor_exit (this->synch_lock);

	return res;
}

void
ves_icall_System_Threading_Thread_SetCachedCurrentUICulture (MonoThread *this, MonoObject *culture)
{
	int i;
	MonoDomain *domain = mono_domain_get ();

	mono_monitor_enter (this->synch_lock);
	if (!this->ui_culture_info) {
		this->ui_culture_info = mono_gc_alloc_fixed (sizeof (MonoObject*) * NUM_CACHED_CULTURES, NULL);
	}

	for (i = 0; i < NUM_CACHED_CULTURES; ++i) {
		if (this->ui_culture_info [i]) {
			if (this->ui_culture_info [i]->vtable->domain == domain)
				/* Replace */
				break;
		}
		else
			/* Free entry */
			break;
	}
	if (i < NUM_CACHED_CULTURES)
		this->ui_culture_info [i] = culture;
	mono_monitor_exit (this->synch_lock);
}

void
ves_icall_System_Threading_Thread_SetSerializedCurrentUICulture (MonoThread *this, MonoArray *arr)
{
	mono_monitor_enter (this->synch_lock);
	if (this->serialized_ui_culture_info)
		g_free (this->serialized_ui_culture_info);
	this->serialized_ui_culture_info = g_new0 (guint8, mono_array_length (arr));
	this->serialized_ui_culture_info_len = mono_array_length (arr);
	memcpy (this->serialized_ui_culture_info, mono_array_addr (arr, guint8, 0), mono_array_length (arr));
	mono_monitor_exit (this->synch_lock);
}

/* the jit may read the compiled code of this function */
MonoThread *
mono_thread_current (void)
{
	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": returning %p", GET_CURRENT_OBJECT ()));
	return GET_CURRENT_OBJECT ();
}

gboolean ves_icall_System_Threading_Thread_Join_internal(MonoThread *this,
							 int ms, HANDLE thread)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	mono_monitor_enter (this->synch_lock);
	
	if ((this->state & ThreadState_Unstarted) != 0) {
		mono_monitor_exit (this->synch_lock);
		mono_raise_exception (mono_get_exception_thread_state ("Thread has not been started."));
		return FALSE;
	}
	
	this->state |= ThreadState_WaitSleepJoin;
	mono_monitor_exit (this->synch_lock);

	if(ms== -1) {
		ms=INFINITE;
	}
	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": joining thread handle %p, %d ms",
		   thread, ms));
	
	ret=WaitForSingleObjectEx (thread, ms, TRUE);

	mono_monitor_enter (this->synch_lock);
	this->state &= ~ThreadState_WaitSleepJoin;
	mono_monitor_exit (this->synch_lock);
	
	if(ret==WAIT_OBJECT_0) {
		THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": join successful"));

		return(TRUE);
	}
	
	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": join failed"));

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
	MonoClass *klass;
	MonoThread *thread = mono_thread_current ();
		
	MONO_ARCH_SAVE_REGS;

	numhandles = mono_array_length(mono_handles);
	handles = g_new0(HANDLE, numhandles);

	if (wait_handle_os_handle_field == 0) {
		/* Get the field os_handle which will contain the actual handle */
		klass = mono_class_from_name(mono_defaults.corlib, "System.Threading", "WaitHandle");	
		wait_handle_os_handle_field = mono_class_get_field_from_name(klass, "os_handle");
	}
		
	for(i = 0; i < numhandles; i++) {	
		waitHandle = mono_array_get(mono_handles, MonoObject*, i);		
		mono_field_get_value(waitHandle, wait_handle_os_handle_field, &handles[i]);
	}
	
	if(ms== -1) {
		ms=INFINITE;
	}

	mono_monitor_enter (thread->synch_lock);
	thread->state |= ThreadState_WaitSleepJoin;
	mono_monitor_exit (thread->synch_lock);
	
	ret=WaitForMultipleObjectsEx(numhandles, handles, TRUE, ms, TRUE);

	mono_monitor_enter (thread->synch_lock);
	thread->state &= ~ThreadState_WaitSleepJoin;
	mono_monitor_exit (thread->synch_lock);

	g_free(handles);

	if(ret==WAIT_FAILED) {
		THREAD_WAIT_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") Wait failed", GetCurrentThreadId ()));
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT || ret == WAIT_IO_COMPLETION) {
		/* Do we want to try again if we get
		 * WAIT_IO_COMPLETION? The documentation for
		 * WaitHandle doesn't give any clues.  (We'd have to
		 * fiddle with the timeout if we retry.)
		 */
		THREAD_WAIT_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") Wait timed out", GetCurrentThreadId ()));
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
	MonoClass *klass;
	MonoThread *thread = mono_thread_current ();
		
	MONO_ARCH_SAVE_REGS;

	numhandles = mono_array_length(mono_handles);
	handles = g_new0(HANDLE, numhandles);

	if (wait_handle_os_handle_field == 0) {
		/* Get the field os_handle which will contain the actual handle */
		klass = mono_class_from_name(mono_defaults.corlib, "System.Threading", "WaitHandle");	
		wait_handle_os_handle_field = mono_class_get_field_from_name(klass, "os_handle");
	}
		
	for(i = 0; i < numhandles; i++) {	
		waitHandle = mono_array_get(mono_handles, MonoObject*, i);		
		mono_field_get_value(waitHandle, wait_handle_os_handle_field, &handles[i]);
	}
	
	if(ms== -1) {
		ms=INFINITE;
	}

	mono_monitor_enter (thread->synch_lock);
	thread->state |= ThreadState_WaitSleepJoin;
	mono_monitor_exit (thread->synch_lock);

	ret=WaitForMultipleObjectsEx(numhandles, handles, FALSE, ms, TRUE);

	mono_monitor_enter (thread->synch_lock);
	thread->state &= ~ThreadState_WaitSleepJoin;
	mono_monitor_exit (thread->synch_lock);

	g_free(handles);

	THREAD_WAIT_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") returning %d", GetCurrentThreadId (), ret));

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

	THREAD_WAIT_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") waiting for %p, %d ms", GetCurrentThreadId (), handle, ms));
	
	if(ms== -1) {
		ms=INFINITE;
	}
	
	mono_monitor_enter (thread->synch_lock);
	thread->state |= ThreadState_WaitSleepJoin;
	mono_monitor_exit (thread->synch_lock);

	ret=WaitForSingleObjectEx (handle, ms, TRUE);
	
	mono_monitor_enter (thread->synch_lock);
	thread->state &= ~ThreadState_WaitSleepJoin;
	mono_monitor_exit (thread->synch_lock);

	if(ret==WAIT_FAILED) {
		THREAD_WAIT_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") Wait failed", GetCurrentThreadId ()));
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT || ret == WAIT_IO_COMPLETION) {
		/* Do we want to try again if we get
		 * WAIT_IO_COMPLETION? The documentation for
		 * WaitHandle doesn't give any clues.  (We'd have to
		 * fiddle with the timeout if we retry.)
		 */
		THREAD_WAIT_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") Wait timed out", GetCurrentThreadId ()));
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

void ves_icall_System_Threading_Mutex_ReleaseMutex_internal (HANDLE handle ) { 
	MONO_ARCH_SAVE_REGS;

	ReleaseMutex(handle);
}

HANDLE ves_icall_System_Threading_Events_CreateEvent_internal (MonoBoolean manual, MonoBoolean initial, MonoString *name) {
	MONO_ARCH_SAVE_REGS;

	return(CreateEvent (NULL, manual, initial,
			    name==NULL?NULL:mono_string_chars (name)));
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

	return orig;
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

	return orig;
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
	/* Should be implemented as a JIT intrinsic */
	mono_raise_exception (mono_get_exception_not_implemented (NULL));
}

void
ves_icall_System_Threading_Thread_ClrState (MonoThread* this, guint32 state)
{
	mono_monitor_enter (this->synch_lock);
	this->state &= ~state;
	if (state & ThreadState_Background) {
		/* If the thread changes the background mode, the main thread has to
		 * be notified, since it has to rebuild the list of threads to
		 * wait for.
		 */
		SetEvent (background_change_event);
	}
	mono_monitor_exit (this->synch_lock);
}

void
ves_icall_System_Threading_Thread_SetState (MonoThread* this, guint32 state)
{
	mono_monitor_enter (this->synch_lock);
	this->state |= state;
	if (state & ThreadState_Background) {
		/* If the thread changes the background mode, the main thread has to
		 * be notified, since it has to rebuild the list of threads to
		 * wait for.
		 */
		SetEvent (background_change_event);
	}
	mono_monitor_exit (this->synch_lock);
}

guint32
ves_icall_System_Threading_Thread_GetState (MonoThread* this)
{
	guint32 state;
	mono_monitor_enter (this->synch_lock);
	state = this->state;
	mono_monitor_exit (this->synch_lock);
	return state;
}

int  
mono_thread_get_abort_signal (void)
{
#if defined (__MINGW32__) || defined (_MSC_VER)
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
#endif /*defined (__MINGW32__) || defined (_MSC_VER) */
}

#if defined (__MINGW32__) || defined (_MSC_VER)
static void CALLBACK interruption_request_apc (ULONG_PTR param)
{
	MonoException* exc = mono_thread_request_interruption (FALSE);
	if (exc) mono_raise_exception (exc);
}
#endif /* defined (__MINGW32__) || defined (_MSC_VER) */

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

#if defined (__MINGW32__) || defined (_MSC_VER)
	QueueUserAPC ((PAPCFUNC)interruption_request_apc, thread->handle, NULL);
#else
	/* fixme: store the state somewhere */
#ifdef PTHREAD_POINTER_ID
	pthread_kill ((gpointer)(gsize)(thread->tid), mono_thread_get_abort_signal ());
#else
	pthread_kill (thread->tid, mono_thread_get_abort_signal ());
#endif
#endif /* defined (__MINGW32__) || defined (__MSC_VER) */
}

void
ves_icall_System_Threading_Thread_Abort (MonoThread *thread, MonoObject *state)
{
	MONO_ARCH_SAVE_REGS;

	mono_monitor_enter (thread->synch_lock);

	if ((thread->state & ThreadState_AbortRequested) != 0 || 
		(thread->state & ThreadState_StopRequested) != 0 ||
		(thread->state & ThreadState_Stopped) != 0)
	{
		mono_monitor_exit (thread->synch_lock);
		return;
	}

	if ((thread->state & ThreadState_Unstarted) != 0) {
		thread->state |= ThreadState_Aborted;
		mono_monitor_exit (thread->synch_lock);
		return;
	}

	thread->state |= ThreadState_AbortRequested;
	thread->abort_state = state;
	thread->abort_exc = NULL;

	mono_monitor_exit (thread->synch_lock);

	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%"G_GSIZE_FORMAT") Abort requested for %p (%"G_GSIZE_FORMAT")", GetCurrentThreadId (), thread, (gsize)thread->tid));
	
	/* Make sure the thread is awake */
	mono_thread_resume (thread);
	
	signal_thread_state_change (thread);
}

void
ves_icall_System_Threading_Thread_ResetAbort (void)
{
	MonoThread *thread = mono_thread_current ();

	MONO_ARCH_SAVE_REGS;
	
	mono_monitor_enter (thread->synch_lock);

	thread->state &= ~ThreadState_AbortRequested;
	
	if (!thread->abort_exc) {
		const char *msg = "Unable to reset abort because no abort was requested";
		mono_monitor_exit (thread->synch_lock);
		mono_raise_exception (mono_get_exception_thread_state (msg));
	} else {
		thread->abort_exc = NULL;
		thread->abort_state = NULL;
	}
	
	mono_monitor_exit (thread->synch_lock);
}

static gboolean
mono_thread_suspend (MonoThread *thread)
{
	MONO_ARCH_SAVE_REGS;

	mono_monitor_enter (thread->synch_lock);

	if ((thread->state & ThreadState_Unstarted) != 0 || 
		(thread->state & ThreadState_Aborted) != 0 || 
		(thread->state & ThreadState_Stopped) != 0)
	{
		mono_monitor_exit (thread->synch_lock);
		return FALSE;
	}

	if ((thread->state & ThreadState_Suspended) != 0 || 
		(thread->state & ThreadState_SuspendRequested) != 0 ||
		(thread->state & ThreadState_StopRequested) != 0) 
	{
		mono_monitor_exit (thread->synch_lock);
		return TRUE;
	}
	
	thread->state |= ThreadState_SuspendRequested;
	mono_monitor_exit (thread->synch_lock);

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

	mono_monitor_enter (thread->synch_lock);

	if ((thread->state & ThreadState_SuspendRequested) != 0) {
		thread->state &= ~ThreadState_SuspendRequested;
		mono_monitor_exit (thread->synch_lock);
		return TRUE;
	}

	if ((thread->state & ThreadState_Suspended) == 0 ||
		(thread->state & ThreadState_Unstarted) != 0 || 
		(thread->state & ThreadState_Aborted) != 0 || 
		(thread->state & ThreadState_Stopped) != 0)
	{
		mono_monitor_exit (thread->synch_lock);
		return FALSE;
	}
	
	thread->resume_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	
	/* Awake the thread */
	SetEvent (thread->suspend_event);

	mono_monitor_exit (thread->synch_lock);

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
	mono_monitor_enter (thread->synch_lock);

	if ((thread->state & ThreadState_StopRequested) != 0 ||
		(thread->state & ThreadState_Stopped) != 0)
	{
		mono_monitor_exit (thread->synch_lock);
		return;
	}
	
	/* Make sure the thread is awake */
	mono_thread_resume (thread);

	thread->state |= ThreadState_StopRequested;
	thread->state &= ~ThreadState_AbortRequested;
	
	mono_monitor_exit (thread->synch_lock);
	
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
	InitializeCriticalSection(&threads_mutex);
	InitializeCriticalSection(&interlocked_mutex);
	InitializeCriticalSection(&contexts_mutex);
	background_change_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	
	mono_init_static_data_info (&thread_static_info);
	mono_init_static_data_info (&context_static_info);

	current_object_key=TlsAlloc();
	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": Allocated current_object_key %d",
		   current_object_key));

	mono_thread_start_cb = start_cb;
	mono_thread_attach_cb = attach_cb;

	/* Get a pseudo handle to the current process.  This is just a
	 * kludge so that wapi can build a process handle if needed.
	 * As a pseudo handle is returned, we don't need to clean
	 * anything up.
	 */
	GetCurrentProcess ();
}

void
mono_threads_install_cleanup (MonoThreadCleanupFunc func)
{
	mono_thread_cleanup = func;
}

void mono_install_thread_callbacks (MonoThreadCallbacks *callbacks)
{
	mono_thread_callbacks = callbacks;
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
	
	THREAD_DEBUG (g_message(G_GNUC_PRETTY_FUNCTION
		  ": %d threads to wait for in this batch", wait->num));

	ret=WaitForMultipleObjectsEx(wait->num, wait->handles, TRUE, timeout, FALSE);

	if(ret==WAIT_FAILED) {
		/* See the comment in build_wait_tids() */
		THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": Wait failed"));
		return;
	}
	
	for(i=0; i<wait->num; i++)
		CloseHandle (wait->handles[i]);

	if (ret == WAIT_TIMEOUT)
		return;

	for(i=0; i<wait->num; i++) {
		gsize tid = wait->threads[i]->tid;
		
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
	
			THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": cleaning up after thread %"G_GSIZE_FORMAT, tid));
			thread_cleanup (wait->threads[i]);
		}
	}
}

static void wait_for_tids_or_state_change (struct wait_data *wait, guint32 timeout)
{
	guint32 i, ret, count;
	
	THREAD_DEBUG (g_message(G_GNUC_PRETTY_FUNCTION
		  ": %d threads to wait for in this batch", wait->num));

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
		THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": Wait failed"));
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
			THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION
				   ": cleaning up after thread %"G_GSIZE_FORMAT, tid));
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
		mono_monitor_enter (thread->synch_lock);
		if (thread->state & ThreadState_Background) {
			mono_monitor_exit (thread->synch_lock);
			return; /* just leave, ignore */
		}
		mono_monitor_exit (thread->synch_lock);
		
		if (mono_gc_is_finalizer_thread (thread))
			return;

		if (thread == mono_thread_current ())
			return;

		if (thread == mono_thread_get_main ())
			return;

		handle = OpenThread (THREAD_ALL_ACCESS, TRUE, thread->tid);
		if (handle == NULL)
			return;
		
		wait->handles[wait->num]=handle;
		wait->threads[wait->num]=thread;
		wait->num++;
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
		
		if(thread->state & ThreadState_AbortRequested ||
		   thread->state & ThreadState_Aborted) {
			THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": Thread id %"G_GSIZE_FORMAT" already aborting", (gsize)thread->tid));
			return(TRUE);
		}

		/* printf ("A: %d\n", wait->num); */
		wait->handles[wait->num]=thread->handle;
		wait->threads[wait->num]=thread;
		wait->num++;

		THREAD_DEBUG (g_print (G_GNUC_PRETTY_FUNCTION ": Aborting id: %"G_GSIZE_FORMAT"\n", (gsize)thread->tid));
		mono_thread_stop (thread);
		return TRUE;
	}

	return (thread->tid != self && !mono_gc_is_finalizer_thread (thread)); 
}

void mono_thread_manage (void)
{
	struct wait_data *wait=g_new0 (struct wait_data, 1);
	
	/* join each thread that's still running */
	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": Joining each running thread..."));
	
	mono_threads_lock ();
	if(threads==NULL) {
		THREAD_DEBUG (g_message(G_GNUC_PRETTY_FUNCTION ": No threads"));
		mono_threads_unlock ();
		return;
	}
	mono_threads_unlock ();
	
	do {
		mono_threads_lock ();
		THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION
			  ":There are %d threads to join", mono_g_hash_table_size (threads));
			mono_g_hash_table_foreach (threads, print_tids, NULL));
	
		ResetEvent (background_change_event);
		wait->num=0;
		mono_g_hash_table_foreach (threads, build_wait_tids, wait);
		mono_threads_unlock ();
		if(wait->num>0) {
			/* Something to wait for */
			wait_for_tids_or_state_change (wait, INFINITE);
		}
		THREAD_DEBUG (g_message ("I have %d threads after waiting.", wait->num));
	} while(wait->num>0);

	mono_runtime_set_shutting_down ();

	THREAD_DEBUG (g_message ("threadpool cleanup"));
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

		THREAD_DEBUG (g_message ("wait->num is now %d", wait->num));
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
	THREAD_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ":There are %d threads to abort",
		  mono_g_hash_table_size (threads));
		mono_g_hash_table_foreach (threads, print_tids, NULL));

	mono_g_hash_table_foreach (threads, terminate_thread, (gpointer)self);
	
	mono_threads_unlock ();
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

/*
 * mono_thread_suspend_all_other_threads:
 *
 *  Suspend all managed threads except the finalizer thread and this thread.
 */
void mono_thread_suspend_all_other_threads (void)
{
	struct wait_data *wait = g_new0 (struct wait_data, 1);
	int i, waitnum;
	gsize self = GetCurrentThreadId ();
	gpointer *events;
	guint32 eventidx = 0;

	/* 
	 * Make a copy of the hashtable since we can't do anything with
	 * threads while threads_mutex is held.
	 */
	mono_threads_lock ();
	mono_g_hash_table_foreach (threads, collect_threads, wait);
	mono_threads_unlock ();

	events = g_new0 (gpointer, wait->num);
	waitnum = 0;
	/* Get the suspended events that we'll be waiting for */
	for (i = 0; i < wait->num; ++i) {
		MonoThread *thread = wait->threads [i];

		if ((thread->tid == self) || mono_gc_is_finalizer_thread (thread)) {
			//CloseHandle (wait->handles [i]);
			wait->threads [i] = NULL; /* ignore this thread in next loop */
			continue;
		}

		mono_monitor_enter (thread->synch_lock);

		if ((thread->state & ThreadState_Suspended) != 0 || 
			(thread->state & ThreadState_SuspendRequested) != 0 ||
			(thread->state & ThreadState_StopRequested) != 0 ||
			(thread->state & ThreadState_Stopped) != 0) {
			mono_monitor_exit (thread->synch_lock);
			CloseHandle (wait->handles [i]);
			wait->threads [i] = NULL; /* ignore this thread in next loop */
			continue;
		}

		/* Convert abort requests into suspend requests */
		if ((thread->state & ThreadState_AbortRequested) != 0)
			thread->state &= ~ThreadState_AbortRequested;
			
		thread->state |= ThreadState_SuspendRequested;

		if (thread->suspended_event == NULL)
			thread->suspended_event = CreateEvent (NULL, TRUE, FALSE, NULL);

		events [eventidx++] = thread->suspended_event;
		mono_monitor_exit (thread->synch_lock);

		/* Signal the thread to suspend */
		signal_thread_state_change (thread);
	}

	WaitForMultipleObjectsEx (eventidx, events, TRUE, INFINITE, FALSE);
	for (i = 0; i < wait->num; ++i) {
		MonoThread *thread = wait->threads [i];

		if (thread == NULL)
			continue;

		mono_monitor_enter (thread->synch_lock);
		CloseHandle (thread->suspended_event);
		thread->suspended_event = NULL;
		mono_monitor_exit (thread->synch_lock);
	}

	g_free (events);
	g_free (wait);
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
		HANDLE handle = OpenThread (THREAD_ALL_ACCESS, TRUE, thread->tid);
		if (handle == NULL)
			return;

		/* printf ("ABORTING THREAD %p BECAUSE IT REFERENCES DOMAIN %s.\n", thread->tid, domain->friendly_name); */

		ves_icall_System_Threading_Thread_Abort (thread, NULL);

		if(data->wait.num<MAXIMUM_WAIT_OBJECTS) {
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

	/* printf ("ABORT BEGIN.\n"); */

	start_time = GetTickCount ();
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
		timeout -= GetTickCount () - start_time;
		start_time = GetTickCount ();

		if (timeout < 0)
			return FALSE;
	}
	while (user_data.wait.num > 0);

	/* printf ("ABORT DONE.\n"); */

	return TRUE;
}

static void
clear_cached_culture (gpointer key, gpointer value, gpointer user_data)
{
	MonoThread *thread = (MonoThread*)value;
	MonoDomain *domain = (MonoDomain*)user_data;
	int i;

	/* No locking needed here */

	if (thread->culture_info) {
		for (i = 0; i < NUM_CACHED_CULTURES; ++i) {
			if (thread->culture_info [i] && thread->culture_info [i]->vtable->domain == domain)
				thread->culture_info [i] = NULL;
		}
	}
	if (thread->ui_culture_info) {
		for (i = 0; i < NUM_CACHED_CULTURES; ++i) {
			if (thread->ui_culture_info [i] && thread->ui_culture_info [i]->vtable->domain == domain)
				thread->ui_culture_info [i] = NULL;
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
 * mono_thread_get_pending_exception:
 *
 *   Return an exception which needs to be raised when leaving a catch clause.
 * This is used for undeniable exception propagation.
 */
MonoException*
mono_thread_get_pending_exception (void)
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
		/* 
		 * massive unloading and reloading of domains with thread-static
		 * data may eventually exceed the allocated storage...
		 * Need to check what the MS runtime does in that case.
		 * Note that for each appdomain, we need to allocate a separate
		 * thread data slot for security reasons. We could keep track
		 * of the slots per-domain and when the domain is unloaded
		 * out the slots on a sort of free list.
		 */
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
		mono_threads_lock ();
		offset = mono_alloc_static_data_slot (&thread_static_info, size, align);
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

static void gc_stop_world (gpointer key, gpointer value, gpointer user)
{
	MonoThread *thread=(MonoThread *)value;

	LIBGC_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": %"G_GSIZE_FORMAT" - %"G_GSIZE_FORMAT, (gsize)user, (gsize)thread->tid));

	if(thread->tid == (gsize)user)
		return;

	SuspendThread (thread->handle);
}

void mono_gc_stop_world (void)
{
	gsize self = GetCurrentThreadId ();

	LIBGC_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": %"G_GSIZE_FORMAT" - %p", self, threads));

	mono_threads_lock ();

	if (threads != NULL)
		mono_g_hash_table_foreach (threads, gc_stop_world, (gpointer)self);
	
	mono_threads_unlock ();
}

static void gc_start_world (gpointer key, gpointer value, gpointer user)
{
	MonoThread *thread=(MonoThread *)value;
	
	LIBGC_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": %"G_GSIZE_FORMAT" - %"G_GSIZE_FORMAT, (gsize)user, (gsize)thread->tid));

	if(thread->tid == (gsize)user)
		return;

	ResumeThread (thread->handle);
}

void mono_gc_start_world (void)
{
	gsize self = GetCurrentThreadId ();

	LIBGC_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": %"G_GSIZE_FORMAT" - %p", self, threads));

	mono_threads_lock ();

	if (threads != NULL)
		mono_g_hash_table_foreach (threads, gc_start_world, (gpointer)self);
	
	mono_threads_unlock ();
}

#ifdef __MINGW32__
static CALLBACK void dummy_apc (ULONG_PTR param)
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
	mono_monitor_enter (thread->synch_lock);

	if (thread->interruption_requested) {
		/* this will consume pending APC calls */
		WaitForSingleObjectEx (GetCurrentThread(), 0, TRUE);
		InterlockedDecrement (&thread_interruption_requested);
		thread->interruption_requested = FALSE;
	}

	if ((thread->state & ThreadState_AbortRequested) != 0) {
		if (thread->abort_exc == NULL)
			thread->abort_exc = mono_get_exception_thread_abort ();
		mono_monitor_exit (thread->synch_lock);
		return thread->abort_exc;
	}
	else if ((thread->state & ThreadState_SuspendRequested) != 0) {
		thread->state &= ~ThreadState_SuspendRequested;
		thread->state |= ThreadState_Suspended;
		thread->suspend_event = CreateEvent (NULL, TRUE, FALSE, NULL);
		if (thread->suspended_event)
			SetEvent (thread->suspended_event);
		mono_monitor_exit (thread->synch_lock);
		
		WaitForSingleObject (thread->suspend_event, INFINITE);
		
		mono_monitor_enter (thread->synch_lock);
		CloseHandle (thread->suspend_event);
		thread->suspend_event = NULL;
		thread->state &= ~ThreadState_Suspended;
	
		/* The thread that requested the resume will have replaced this event
		 * and will be waiting for it
		 */
		SetEvent (thread->resume_event);
		mono_monitor_exit (thread->synch_lock);
		return NULL;
	}
	else if ((thread->state & ThreadState_StopRequested) != 0) {
		/* FIXME: do this through the JIT? */
		mono_monitor_exit (thread->synch_lock);
		mono_thread_exit ();
		return NULL;
	}
	
	mono_monitor_exit (thread->synch_lock);
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
	
	mono_monitor_enter (thread->synch_lock);
	
	if (thread->interruption_requested) {
		mono_monitor_exit (thread->synch_lock);
		return NULL;
	}

	if (!running_managed || is_running_protected_wrapper ()) {
		/* Can't stop while in unmanaged code. Increase the global interruption
		   request count. When exiting the unmanaged method the count will be
		   checked and the thread will be interrupted. */

		InterlockedIncrement (&thread_interruption_requested);
		thread->interruption_requested = TRUE;
		mono_monitor_exit (thread->synch_lock);

		/* this will awake the thread if it is in WaitForSingleObject 
		   or similar */
		QueueUserAPC ((PAPCFUNC)dummy_apc, thread->handle, NULL);
		return NULL;
	}
	else {
		mono_monitor_exit (thread->synch_lock);
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

#ifdef WITH_INCLUDED_LIBGC

static void gc_push_all_stacks (gpointer key, gpointer value, gpointer user)
{
	MonoThread *thread=(MonoThread *)value;
	gsize *selfp = (gsize *)user, self = *selfp;

	LIBGC_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": %"G_GSIZE_FORMAT" - %"G_GSIZE_FORMAT" - %p", self, (gsize)thread->tid, thread->stack_ptr));

	if(thread->tid == self) {
		LIBGC_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": %p - %p", selfp, thread->stack_ptr));
		GC_push_all_stack (selfp, thread->stack_ptr);
		return;
	}

#ifdef PLATFORM_WIN32
	GC_win32_push_thread_stack (thread->handle, thread->stack_ptr);
#else
	mono_wapi_push_thread_stack (thread->handle, thread->stack_ptr);
#endif
}

void mono_gc_push_all_stacks (void)
{
	gsize self = GetCurrentThreadId ();

	LIBGC_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": %"G_GSIZE_FORMAT" - %p", self, threads));

	mono_threads_lock ();

	if (threads != NULL)
		mono_g_hash_table_foreach (threads, gc_push_all_stacks, &self);
	
	mono_threads_unlock ();
}

#endif /* WITH_INCLUDED_LIBGC */
