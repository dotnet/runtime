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

#include <mono/os/gc_wrapper.h>

#undef THREAD_DEBUG
#undef THREAD_WAIT_DEBUG

struct StartInfo 
{
	guint32 (*func)(void *);
	MonoThread *obj;
	void *this;
	MonoDomain *domain;
};

typedef union {
	gint32 ival;
	gfloat fval;
} IntFloatUnion;
 
typedef struct {
	int idx;
	int offset;
} StaticDataInfo;

/*
 * The "os_handle" field of the WaitHandle class.
 */
static MonoClassField *wait_handle_os_handle_field = NULL;

/* Controls access to the 'threads' hash table */
static CRITICAL_SECTION threads_mutex;

/* Controls access to context static data */
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
static __thread MonoThread * tls_current_object;
#define SET_CURRENT_OBJECT(x) do { \
	tls_current_object = x; \
	TlsSetValue (current_object_key, thread); \
} while (FALSE)
#define GET_CURRENT_OBJECT() tls_current_object
#else
#define SET_CURRENT_OBJECT(x) TlsSetValue (current_object_key, thread);
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

/* The TLS key that holds the LocalDataStoreSlot hash in each thread */
static guint32 slothash_key = -1;

/* The default stack size for each thread */
static guint32 default_stacksize = 0;

static void thread_adjust_static_data (MonoThread *thread);
static void mono_init_static_data_info (StaticDataInfo *static_data);
static guint32 mono_alloc_static_data_slot (StaticDataInfo *static_data, guint32 size, guint32 align);

/* Spin lock for InterlockedXXX 64 bit functions */
static CRITICAL_SECTION interlocked_mutex;

/* Controls access to interruption flag */
static CRITICAL_SECTION interruption_mutex;

/* global count of thread interruptions requested */
static gint32 thread_interruption_requested = 0;


/* handle_store() and handle_remove() manage the array of threads that
 * still need to be waited for when the main thread exits.
 */
static void handle_store(MonoThread *thread)
{
	EnterCriticalSection(&threads_mutex);

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": thread %p ID %d", thread,
		  thread->tid);
#endif

	if(threads==NULL) {
		MONO_GC_REGISTER_ROOT (threads);
		threads=mono_g_hash_table_new(NULL, NULL);
	}

	/* We don't need to duplicate thread->handle, because it is
	 * only closed when the thread object is finalized by the GC.
	 */
	mono_g_hash_table_insert(threads, GUINT_TO_POINTER(thread->tid), thread);
	LeaveCriticalSection(&threads_mutex);
}

static void handle_remove(guint32 tid)
{
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": thread ID %d", tid);
#endif

	EnterCriticalSection(&threads_mutex);

	if (threads)
		mono_g_hash_table_remove (threads, GUINT_TO_POINTER(tid));
	
	LeaveCriticalSection(&threads_mutex);

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
	if (!mono_monitor_enter (thread->synch_lock))
		return;

	thread->state |= ThreadState_Stopped;
	mono_monitor_exit (thread->synch_lock);

	mono_profiler_thread_end (thread->tid);
	handle_remove (thread->tid);

	mono_thread_pop_appdomain_ref ();

	if (mono_thread_cleanup)
		mono_thread_cleanup (thread);
}

static guint32 start_wrapper(void *data)
{
	struct StartInfo *start_info=(struct StartInfo *)data;
	guint32 (*start_func)(void *);
	void *this;
	guint32 tid;
	MonoThread *thread=start_info->obj;
	
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Start wrapper",
		  GetCurrentThreadId ());
#endif
	
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
	this = start_info->this;

	/* This MUST be called before any managed code can be
	 * executed, as it calls the callback function that (for the
	 * jit) sets the lmf marker.
	 */
	mono_thread_new_init (tid, &tid, start_func);
	thread->stack_ptr = &tid;

#ifdef LIBGC_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": (%d,%d) Setting thread stack to %p",
		   GetCurrentThreadId (), getpid (), thread->stack_ptr);
#endif

#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": (%d) Setting current_object_key to %p",
		   GetCurrentThreadId (), thread);
#endif

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
	g_message (G_GNUC_PRETTY_FUNCTION "start_wrapper for %d\n", thread->tid);
#endif

	start_func (this);
#ifdef PLATFORM_WIN32
	/* If the thread calls ExitThread at all, this remaining code
	 * will not be executed, but the main thread will eventually
	 * call thread_cleanup() on this thread's behalf.
	 */

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Start wrapper terminating",
		  GetCurrentThreadId ());
#endif

	/* Remove the reference to the thread object in the TLS data,
	 * so the thread object can be finalized.  This won't be
	 * reached if the thread threw an uncaught exception, so those
	 * thread handles will stay referenced :-( (This is due to
	 * missing support for scanning thread-specific data in the
	 * Boehm GC - the io-layer keeps a GC-visible hash of pointers
	 * to TLS data.)
	 */
	SET_CURRENT_OBJECT (NULL);
#endif
	
	thread_cleanup (thread);

	return(0);
}

void mono_thread_new_init (guint32 tid, gpointer stack_start, gpointer func)
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
	guint32 tid;
	
	thread=(MonoThread *)mono_object_new (domain,
					      mono_defaults.thread_class);

	start_info=g_new0 (struct StartInfo, 1);
	start_info->func = func;
	start_info->obj = thread;
	start_info->domain = domain;
	start_info->this = arg;
	
	/* Create suspended, so we can do some housekeeping before the thread
	 * starts
	 */
#if defined(PLATFORM_WIN32) && defined(HAVE_BOEHM_GC)
	thread_handle = GC_CreateThread(NULL, default_stacksize, start_wrapper, start_info,
				     CREATE_SUSPENDED, &tid);
#else
	thread_handle = CreateThread(NULL, default_stacksize, start_wrapper, start_info,
				     CREATE_SUSPENDED, &tid);
#endif
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Started thread ID %d (handle %p)",
		  tid, thread_handle);
#endif
	g_assert (thread_handle);

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
	guint32 tid;

	if ((thread = mono_thread_current ())) {
		/* Already attached */
		return thread;
	}

	thread = (MonoThread *)mono_object_new (domain,
						mono_defaults.thread_class);

	thread_handle = GetCurrentThread ();
	g_assert (thread_handle);

	tid=GetCurrentThreadId ();

	thread->handle=thread_handle;
	thread->tid=tid;

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Attached thread ID %d (handle %p)",
		  tid, thread_handle);
#endif

	handle_store(thread);

#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": (%d) Setting current_object_key to %p",
		   GetCurrentThreadId (), thread);
#endif

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

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION "mono_thread_detach for %d\n", thread->tid);
#endif
	SET_CURRENT_OBJECT (NULL);
	
	thread_cleanup (thread);
}

HANDLE ves_icall_System_Threading_Thread_Thread_internal(MonoThread *this,
							 MonoObject *start)
{
	MonoMulticastDelegate *delegate = (MonoMulticastDelegate*)start;
	guint32 (*start_func)(void *);
	struct StartInfo *start_info;
	MonoMethod *im;
	HANDLE thread;
	guint32 tid;
	
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Trying to start a new thread: this (%p) start (%p)",
		  this, start);
#endif
	
	im = mono_get_delegate_invoke (start->vtable->klass);
	im = mono_marshal_get_delegate_invoke (im);
	if (mono_thread_callbacks)
		start_func = (* mono_thread_callbacks->thread_start_compile_func) (im);
	else
		start_func = mono_compile_method (im);

	if(start_func==NULL) {
		g_warning(G_GNUC_PRETTY_FUNCTION
			  ": Can't locate start method!");
		return(NULL);
	} else {
		/* This is freed in start_wrapper */
		start_info = g_new0 (struct StartInfo, 1);
		start_info->func = start_func;
		start_info->this = delegate;
		start_info->obj = this;
		start_info->domain = mono_domain_get ();

		this->start_notify=CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
		if(this->start_notify==NULL) {
			g_warning (G_GNUC_PRETTY_FUNCTION ": CreateSemaphore error 0x%x", GetLastError ());
			return(NULL);
		}

#if defined(PLATFORM_WIN32) && defined(HAVE_BOEHM_GC)
		thread=GC_CreateThread(NULL, default_stacksize, start_wrapper, start_info,
				    CREATE_SUSPENDED, &tid);
#else
		thread=CreateThread(NULL, default_stacksize, start_wrapper, start_info,
				    CREATE_SUSPENDED, &tid);
#endif
		if(thread==NULL) {
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

#ifdef THREAD_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Started thread ID %d (handle %p)", tid, thread);
#endif

		return(thread);
	}
}

void ves_icall_System_Threading_Thread_Thread_free_internal (MonoThread *this,
							     HANDLE thread)
{
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Closing thread %p, handle %p",
		   this, thread);
#endif

	CloseHandle (thread);
}

void ves_icall_System_Threading_Thread_Start_internal(MonoThread *this,
						      HANDLE thread)
{
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Launching thread %p (%d)",
		  GetCurrentThreadId (), this, this->tid);
#endif

	/* Only store the handle when the thread is about to be
	 * launched, to avoid the main thread deadlocking while trying
	 * to clean up a thread that will never be signalled.
	 */
	handle_store(this);

	if (mono_thread_callbacks)
		(* mono_thread_callbacks->start_resume) (this->tid);

	ResumeThread(thread);

	if (mono_thread_callbacks)
		(* mono_thread_callbacks->end_resume) (this->tid);

	if(this->start_notify!=NULL) {
		/* Wait for the thread to set up its TLS data etc, so
		 * theres no potential race condition if someone tries
		 * to look up the data believing the thread has
		 * started
		 */

#ifdef THREAD_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": (%d) waiting for thread %p (%d) to start",
			  GetCurrentThreadId (), this, this->tid);
#endif

		WaitForSingleObjectEx (this->start_notify, INFINITE, FALSE);
		CloseHandle (this->start_notify);
		this->start_notify=NULL;
	}

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": (%d) Done launching thread %p (%d)",
		  GetCurrentThreadId (), this, this->tid);
#endif
}

void ves_icall_System_Threading_Thread_Sleep_internal(gint32 ms)
{
	MonoThread *thread = mono_thread_current ();
	
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Sleeping for %d ms", ms);
#endif

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
	if (!this_obj->name)
		return NULL;
	else
		return mono_string_new_utf16 (mono_domain_get (), this_obj->name, this_obj->name_len);
}

void 
ves_icall_System_Threading_Thread_SetName_internal (MonoThread *this_obj, MonoString *name)
{
	if (this_obj->name)
		g_free (this_obj->name);
	if (name) {
		this_obj->name = g_new (gunichar2, mono_string_length (name));
		memcpy (this_obj->name, mono_string_chars (name), mono_string_length (name) * 2);
		this_obj->name_len = mono_string_length (name);
	}
	else
		this_obj->name = NULL;
}

/* the jit may read the compiled code of this function */
MonoThread *
mono_thread_current (void)
{
#ifdef THREAD_DEBUG
	MonoThread *thread;
	MONO_ARCH_SAVE_REGS;
	thread = GET_CURRENT_OBJECT ();
	g_message (G_GNUC_PRETTY_FUNCTION ": returning %p", thread);
	return thread;
#else
	MONO_ARCH_SAVE_REGS;
	return GET_CURRENT_OBJECT ();
#endif
}

gboolean ves_icall_System_Threading_Thread_Join_internal(MonoThread *this,
							 int ms, HANDLE thread)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	mono_monitor_enter (this->synch_lock);
	this->state |= ThreadState_WaitSleepJoin;
	mono_monitor_exit (this->synch_lock);

	if(ms== -1) {
		ms=INFINITE;
	}
#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": joining thread handle %p, %d ms",
		   thread, ms);
#endif
	
	ret=WaitForSingleObjectEx (thread, ms, TRUE);

	mono_monitor_enter (this->synch_lock);
	this->state &= ~ThreadState_WaitSleepJoin;
	mono_monitor_exit (this->synch_lock);
	
	if(ret==WAIT_OBJECT_0) {
#ifdef THREAD_DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": join successful");
#endif

		return(TRUE);
	}
	
#ifdef THREAD_DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": join failed");
#endif

	return(FALSE);
}

void ves_icall_System_Threading_Thread_SlotHash_store(MonoObject *data)
{
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Storing key %p", data);
#endif

	/* Object location stored here */
	TlsSetValue(slothash_key, data);
}

MonoObject *ves_icall_System_Threading_Thread_SlotHash_lookup(void)
{
	MonoObject *data;

	MONO_ARCH_SAVE_REGS;

	data=TlsGetValue(slothash_key);
	
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Retrieved key %p", data);
#endif
	
	return(data);
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
	
	ret=WaitForMultipleObjectsEx(numhandles, handles, TRUE, ms, TRUE);

	g_free(handles);

	if(ret==WAIT_FAILED) {
#ifdef THREAD_WAIT_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Wait failed",
			  GetCurrentThreadId ());
#endif
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT || ret == WAIT_IO_COMPLETION) {
		/* Do we want to try again if we get
		 * WAIT_IO_COMPLETION? The documentation for
		 * WaitHandle doesn't give any clues.  (We'd have to
		 * fiddle with the timeout if we retry.)
		 */
#ifdef THREAD_WAIT_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Wait timed out",
			  GetCurrentThreadId ());
#endif
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

	ret=WaitForMultipleObjectsEx(numhandles, handles, FALSE, ms, TRUE);

	g_free(handles);

#ifdef THREAD_WAIT_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) returning %d",
		  GetCurrentThreadId (), ret);
#endif

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
	
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_WAIT_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) waiting for %p, %d ms",
		  GetCurrentThreadId (), handle, ms);
#endif
	
	if(ms== -1) {
		ms=INFINITE;
	}
	
	ret=WaitForSingleObjectEx (handle, ms, TRUE);

	if(ret==WAIT_FAILED) {
#ifdef THREAD_WAIT_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Wait failed",
			  GetCurrentThreadId ());
#endif
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT || ret == WAIT_IO_COMPLETION) {
		/* Do we want to try again if we get
		 * WAIT_IO_COMPLETION? The documentation for
		 * WaitHandle doesn't give any clues.  (We'd have to
		 * fiddle with the timeout if we retry.)
		 */
#ifdef THREAD_WAIT_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Wait timed out",
			  GetCurrentThreadId ());
#endif
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
	gint32 lowret;
	gint32 highret;

	MONO_ARCH_SAVE_REGS;

	EnterCriticalSection(&interlocked_mutex);

	lowret = InterlockedIncrement((gint32 *) location);
	if (0 == lowret)
		highret = InterlockedIncrement((gint32 *) location + 1);
	else
		highret = *((gint32 *) location + 1);

	LeaveCriticalSection(&interlocked_mutex);

	return (gint64) highret << 32 | (gint64) lowret;
}

gint32 ves_icall_System_Threading_Interlocked_Decrement_Int (gint32 *location)
{
	MONO_ARCH_SAVE_REGS;

	return InterlockedDecrement(location);
}

gint64 ves_icall_System_Threading_Interlocked_Decrement_Long (gint64 * location)
{
	gint32 lowret;
	gint32 highret;

	MONO_ARCH_SAVE_REGS;

	EnterCriticalSection(&interlocked_mutex);

	lowret = InterlockedDecrement((gint32 *) location);
	if (-1 == lowret)
		highret = InterlockedDecrement((gint32 *) location + 1);
	else
		highret = *((gint32 *) location + 1);

	LeaveCriticalSection(&interlocked_mutex);

	return (gint64) highret << 32 | (gint64) lowret;
}

gint32 ves_icall_System_Threading_Interlocked_Exchange_Int (gint32 *location1, gint32 value)
{
	MONO_ARCH_SAVE_REGS;

	return InterlockedExchange(location1, value);
}

MonoObject * ves_icall_System_Threading_Interlocked_Exchange_Object (MonoObject **location1, MonoObject *value)
{
	MONO_ARCH_SAVE_REGS;

	return (MonoObject *) InterlockedExchangePointer((gpointer *) location1, value);
}

gfloat ves_icall_System_Threading_Interlocked_Exchange_Single (gfloat *location1, gfloat value)
{
	IntFloatUnion val, ret;

	MONO_ARCH_SAVE_REGS;

	val.fval = value;
	ret.ival = InterlockedExchange((gint32 *) location1, val.ival);

	return ret.fval;
}

gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int(gint32 *location1, gint32 value, gint32 comparand)
{
	MONO_ARCH_SAVE_REGS;

	return InterlockedCompareExchange(location1, value, comparand);
}

MonoObject * ves_icall_System_Threading_Interlocked_CompareExchange_Object (MonoObject **location1, MonoObject *value, MonoObject *comparand)
{
	MONO_ARCH_SAVE_REGS;

	return (MonoObject *) InterlockedCompareExchangePointer((gpointer *) location1, value, comparand);
}

gfloat ves_icall_System_Threading_Interlocked_CompareExchange_Single (gfloat *location1, gfloat value, gfloat comparand)
{
	IntFloatUnion val, ret, cmp;

	MONO_ARCH_SAVE_REGS;

	val.fval = value;
	cmp.fval = comparand;
	ret.ival = InterlockedCompareExchange((gint32 *) location1, val.ival, cmp.ival);

	return ret.fval;
}

int  
mono_thread_get_abort_signal (void)
{
#ifdef __MINGW32__
	return -1;
#else
#ifndef	SIGRTMIN
	return SIGUSR1;
#else
	return SIGRTMIN;
#endif
#endif /* __MINGW32__ */
}

#ifdef __MINGW32__
static guint32 interruption_request_apc (gpointer param)
{
	MonoException* exc = mono_thread_request_interruption (FALSE);
	if (exc) mono_raise_exception (exc);
	return 0;
}
#endif /* __MINGW32__ */

/*
 * signal_thread_state_change
 *
 * Tells the thread that his state has changed and it has to enter the new
 * state as soon as possible.
 */
static void signal_thread_state_change (MonoThread *thread)
{
#ifdef __MINGW32__
	QueueUserAPC (interruption_request_apc, thread->handle, NULL);
#else
	/* fixme: store the state somewhere */
#ifdef PTHREAD_POINTER_ID
	pthread_kill (GUINT_TO_POINTER(thread->tid), mono_thread_get_abort_signal ());
#else
	pthread_kill (thread->tid, mono_thread_get_abort_signal ());
#endif
#endif /* __MINGW32__ */
}

void
ves_icall_System_Threading_Thread_Abort (MonoThread *thread, MonoObject *state)
{
	MONO_ARCH_SAVE_REGS;

	mono_monitor_enter (thread->synch_lock);

	if ((thread->state & ThreadState_AbortRequested) != 0 || 
		(thread->state & ThreadState_StopRequested) != 0) 
	{
		mono_monitor_exit (thread->synch_lock);
		return;
	}

	thread->state |= ThreadState_AbortRequested;
	thread->abort_state = state;
	thread->abort_exc = NULL;

	mono_monitor_exit (thread->synch_lock);

#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": (%d) Abort requested for %p (%d)", GetCurrentThreadId (),
		   thread, thread->tid);
#endif
	
	/* Make sure the thread is awake */
	ves_icall_System_Threading_Thread_Resume (thread);
	
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

void
ves_icall_System_Threading_Thread_Suspend (MonoThread *thread)
{
	MONO_ARCH_SAVE_REGS;

	mono_monitor_enter (thread->synch_lock);

	if ((thread->state & ThreadState_Suspended) != 0 || 
		(thread->state & ThreadState_SuspendRequested) != 0 ||
		(thread->state & ThreadState_StopRequested) != 0) 
	{
		mono_monitor_exit (thread->synch_lock);
		return;
	}
	
	thread->state |= ThreadState_SuspendRequested;
	mono_monitor_exit (thread->synch_lock);

	signal_thread_state_change (thread);
}

void
ves_icall_System_Threading_Thread_Resume (MonoThread *thread)
{
	MONO_ARCH_SAVE_REGS;

	mono_monitor_enter (thread->synch_lock);

	if ((thread->state & ThreadState_SuspendRequested) != 0) {
		thread->state &= ~ThreadState_SuspendRequested;
		mono_monitor_exit (thread->synch_lock);
		return;
	}
		
	if ((thread->state & ThreadState_Suspended) == 0) 
	{
		mono_monitor_exit (thread->synch_lock);
		return;
	}
	
	thread->resume_event = CreateEvent (NULL, TRUE, FALSE, NULL);
	
	/* Awake the thread */
	SetEvent (thread->suspend_event);

	mono_monitor_exit (thread->synch_lock);

	/* Wait for the thread to awake */
	WaitForSingleObject (thread->resume_event, INFINITE);
	CloseHandle (thread->resume_event);
	thread->resume_event = NULL;
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
	ves_icall_System_Threading_Thread_Resume (thread);
	
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
	InitializeCriticalSection(&interruption_mutex);
	
	mono_init_static_data_info (&thread_static_info);
	mono_init_static_data_info (&context_static_info);

	current_object_key=TlsAlloc();
#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Allocated current_object_key %d",
		   current_object_key);
#endif

	mono_thread_start_cb = start_cb;
	mono_thread_attach_cb = attach_cb;

	slothash_key=TlsAlloc();

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

#ifdef THREAD_DEBUG
static void print_tids (gpointer key, gpointer value, gpointer user)
{
	g_message ("Waiting for: %d", GPOINTER_TO_UINT(key));
}
#endif

struct wait_data 
{
	HANDLE handles[MAXIMUM_WAIT_OBJECTS];
	MonoThread *threads[MAXIMUM_WAIT_OBJECTS];
	guint32 num;
};

static void wait_for_tids (struct wait_data *wait, guint32 timeout)
{
	guint32 i, ret;
	
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": %d threads to wait for in this batch", wait->num);
#endif

	ret=WaitForMultipleObjectsEx(wait->num, wait->handles, TRUE, timeout, FALSE);

	if(ret==WAIT_FAILED) {
		/* See the comment in build_wait_tids() */
#ifdef THREAD_DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Wait failed");
#endif
		return;
	}
	

	for(i=0; i<wait->num; i++) {
		guint32 tid=wait->threads[i]->tid;
		CloseHandle (wait->handles[i]);
		
		if(mono_g_hash_table_lookup (threads, GUINT_TO_POINTER(tid))!=NULL) {
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
	
#ifdef THREAD_DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": cleaning up after thread %d", tid);
#endif
			thread_cleanup (wait->threads[i]);
		}
	}
}

static void build_wait_tids (gpointer key, gpointer value, gpointer user)
{
	struct wait_data *wait=(struct wait_data *)user;

	if(wait->num<MAXIMUM_WAIT_OBJECTS) {
		HANDLE handle;
		MonoThread *thread=(MonoThread *)value;

		/* Ignore background threads, we abort them later */
		if (thread->state & ThreadState_Background)
			return; /* just leave, ignore */
		
		if (mono_gc_is_finalizer_thread (thread))
			return;

		if (thread == mono_thread_current ())
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
	guint32 self = GetCurrentThreadId ();
	MonoThread *thread = (MonoThread *) value;
	HANDLE handle;

	/* The finalizer thread is not a background thread */
	if (thread->tid != self && thread->state & ThreadState_Background) {
	
		handle = OpenThread (THREAD_ALL_ACCESS, TRUE, thread->tid);
		if (handle == NULL)
			return FALSE;
		
		wait->handles[wait->num]=thread->handle;
		wait->threads[wait->num]=thread;
		wait->num++;
	
		if(thread->state & ThreadState_AbortRequested ||
		   thread->state & ThreadState_Aborted) {
#ifdef THREAD_DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": Thread id %d already aborting", thread->tid);
#endif
			return(TRUE);
		}
		
#ifdef THREAD_DEBUG
		g_print (G_GNUC_PRETTY_FUNCTION ": Aborting id: %d\n", thread->tid);
#endif
		mono_thread_stop (thread);
		return TRUE;
	}

	return (thread->tid != self && !mono_gc_is_finalizer_thread (thread)); 
}

void mono_thread_manage (void)
{
	struct wait_data *wait=g_new0 (struct wait_data, 1);
	
	/* join each thread that's still running */
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Joining each running thread...");
#endif
	
	EnterCriticalSection (&threads_mutex);
	if(threads==NULL) {
#ifdef THREAD_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": No threads");
#endif
		LeaveCriticalSection (&threads_mutex);
		return;
	}
	LeaveCriticalSection (&threads_mutex);
	
	do {
		EnterCriticalSection (&threads_mutex);
#ifdef THREAD_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ":There are %d threads to join",
			  mono_g_hash_table_size (threads));
		mono_g_hash_table_foreach (threads, print_tids, NULL);
#endif
	
		wait->num=0;
		mono_g_hash_table_foreach (threads, build_wait_tids, wait);
		LeaveCriticalSection (&threads_mutex);
		if(wait->num>0) {
			/* Something to wait for */
			wait_for_tids (wait, INFINITE);
		}
	} while(wait->num>0);
	
	mono_thread_pool_cleanup ();

	EnterCriticalSection(&threads_mutex);

	/* 
	 * Remove everything but the finalizer thread and self.
	 * Also abort all the background threads
	 * */
	wait->num = 0;
	mono_g_hash_table_foreach_remove (threads, remove_and_abort_threads, wait);

	LeaveCriticalSection(&threads_mutex);

	if(wait->num>0) {
		/* Something to wait for */
		wait_for_tids (wait, INFINITE);
	}
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
	guint32 self=GPOINTER_TO_UINT (user);
	
	if(thread->tid!=self) {
		/*TerminateThread (thread->handle, -1);*/
	}
}

void mono_thread_abort_all_other_threads (void)
{
	guint32 self=GetCurrentThreadId ();

	EnterCriticalSection (&threads_mutex);
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ":There are %d threads to abort",
		  mono_g_hash_table_size (threads));
	mono_g_hash_table_foreach (threads, print_tids, NULL);
#endif

	mono_g_hash_table_foreach (threads, terminate_thread,
				   GUINT_TO_POINTER (self));
	
	LeaveCriticalSection (&threads_mutex);
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
		/* printf ("PUSH REF: %p -> %s.\n", thread, domain->friendly_name); */
		EnterCriticalSection (&threads_mutex);
		thread->appdomain_refs = g_slist_prepend (thread->appdomain_refs, domain);
		LeaveCriticalSection (&threads_mutex);
	}
}

void
mono_thread_pop_appdomain_ref (void)
{
	MonoThread *thread = mono_thread_current ();

	if (thread) {
		/* printf ("POP REF: %p -> %s.\n", thread, ((MonoDomain*)(thread->appdomain_refs->data))->friendly_name); */
		EnterCriticalSection (&threads_mutex);
		/* FIXME: How can the list be empty ? */
		if (thread->appdomain_refs)
			thread->appdomain_refs = g_slist_remove (thread->appdomain_refs, thread->appdomain_refs->data);
		LeaveCriticalSection (&threads_mutex);
	}
}

static gboolean
mono_thread_has_appdomain_ref (MonoThread *thread, MonoDomain *domain)
{
	gboolean res;
	EnterCriticalSection (&threads_mutex);
	res = g_slist_find (thread->appdomain_refs, domain) != NULL;
	LeaveCriticalSection (&threads_mutex);
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
		/* printf ("ABORTING THREAD %p BECAUSE IT REFERENCES DOMAIN %s.\n", thread, domain->friendly_name); */
		HANDLE handle = OpenThread (THREAD_ALL_ACCESS, TRUE, thread->tid);
		if (handle == NULL)
			return;

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
		EnterCriticalSection (&threads_mutex);

		user_data.domain = domain;
		user_data.wait.num = 0;
		mono_g_hash_table_foreach (threads, abort_appdomain_thread, &user_data);
		LeaveCriticalSection (&threads_mutex);

		if (user_data.wait.num > 0)
			wait_for_tids (&user_data.wait, timeout);

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

	if (thread && thread->abort_exc) {
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
#if HAVE_BOEHM_GC
		static_data = GC_MALLOC (static_data_size [0]);
#else
		static_data = g_malloc0 (static_data_size [0]);
#endif
		*static_data_ptr = static_data;
		static_data [0] = static_data;
	}
	
	for (i = 1; i < idx; ++i) {
		if (static_data [i])
			continue;
#if HAVE_BOEHM_GC
		static_data [i] = GC_MALLOC (static_data_size [i]);
#else
		static_data [i] = g_malloc0 (static_data_size [i]);
#endif
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

	EnterCriticalSection (&threads_mutex);
	if (thread_static_info.offset || thread_static_info.idx > 0) {
		/* get the current allocated size */
		offset = thread_static_info.offset | ((thread_static_info.idx + 1) << 24);
		mono_alloc_static_data (&(thread->static_data), offset);
	}
	LeaveCriticalSection (&threads_mutex);
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
		EnterCriticalSection (&threads_mutex);
		offset = mono_alloc_static_data_slot (&thread_static_info, size, align);
		/* This can be called during startup */
		if (threads != NULL)
			mono_g_hash_table_foreach (threads, alloc_thread_static_data_helper, GUINT_TO_POINTER (offset));
		LeaveCriticalSection (&threads_mutex);
	}
	else
	{
		g_assert (static_type == SPECIAL_STATIC_CONTEXT);
		EnterCriticalSection (&contexts_mutex);
		offset = mono_alloc_static_data_slot (&context_static_info, size, align);
		LeaveCriticalSection (&contexts_mutex);
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
			EnterCriticalSection (&contexts_mutex);
			mono_alloc_static_data (&(context->static_data), offset);
			LeaveCriticalSection (&contexts_mutex);
		}
		return ((char*) context->static_data [idx]) + (offset & 0xffffff);	
	}
}

static void gc_stop_world (gpointer key, gpointer value, gpointer user)
{
	MonoThread *thread=(MonoThread *)value;
	guint32 self=GPOINTER_TO_UINT (user);

#ifdef LIBGC_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": %d - %d", self, thread->tid);
#endif
	
	if(thread->tid==self)
		return;

	SuspendThread (thread->handle);
}

void mono_gc_stop_world (void)
{
	guint32 self=GetCurrentThreadId ();

#ifdef LIBGC_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": %d - %p", self, threads);
#endif

	EnterCriticalSection (&threads_mutex);

	if (threads != NULL)
		mono_g_hash_table_foreach (threads, gc_stop_world, GUINT_TO_POINTER (self));
	
	LeaveCriticalSection (&threads_mutex);
}

static void gc_start_world (gpointer key, gpointer value, gpointer user)
{
	MonoThread *thread=(MonoThread *)value;
	guint32 self=GPOINTER_TO_UINT (user);
	
#ifdef LIBGC_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": %d - %d", self, thread->tid);
#endif
	
	if(thread->tid==self)
		return;

	ResumeThread (thread->handle);
}

void mono_gc_start_world (void)
{
	guint32 self=GetCurrentThreadId ();

#ifdef LIBGC_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": %d - %p", self, threads);
#endif

	EnterCriticalSection (&threads_mutex);

	if (threads != NULL)
		mono_g_hash_table_foreach (threads, gc_start_world, GUINT_TO_POINTER (self));
	
	LeaveCriticalSection (&threads_mutex);
}


static guint32 dummy_apc (gpointer param)
{
	return 0;
}

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
		EnterCriticalSection (&interruption_mutex);
		thread_interruption_requested--;
		LeaveCriticalSection (&interruption_mutex);
		thread->interruption_requested = FALSE;
	}

	if ((thread->state & ThreadState_AbortRequested) != 0) {
		thread->abort_exc = mono_get_exception_thread_abort ();
		mono_monitor_exit (thread->synch_lock);
		return thread->abort_exc;
	}
	else if ((thread->state & ThreadState_SuspendRequested) != 0) {
		thread->state &= ~ThreadState_SuspendRequested;
		thread->state |= ThreadState_Suspended;
		thread->suspend_event = CreateEvent (NULL, TRUE, FALSE, NULL);
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
		ExitThread (-1);
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

	if (!running_managed) {
		/* Can't stop while in unmanaged code. Increase the global interruption
		   request count. When exiting the unmanaged method the count will be
		   checked and the thread will be interrupted. */
		
		EnterCriticalSection (&interruption_mutex);
		thread_interruption_requested++;
		LeaveCriticalSection (&interruption_mutex);
		
		thread->interruption_requested = TRUE;
		mono_monitor_exit (thread->synch_lock);
		
		/* this will awake the thread if it is in WaitForSingleObject 
	       or similar */
		QueueUserAPC (dummy_apc, thread->handle, NULL);
		
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

/*
 * Performs the interruption of the current thread, if one has been requested.
 */
void mono_thread_interruption_checkpoint ()
{
	MonoThread *thread = mono_thread_current ();
	
	/* The thread may already be stopping */
	if (thread == NULL) 
		return;
	
	if (thread->interruption_requested) {
		MonoException* exc = mono_thread_execute_interruption (thread);
		if (exc) mono_raise_exception (exc);
	}
}

/*
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
	guint32 *selfp=(guint32 *)user, self = *selfp;

#ifdef LIBGC_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": %d - %d - %p", self, thread->tid, thread->stack_ptr);
#endif
	
	if(thread->tid==self) {
#ifdef LIBGC_DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": %p - %p", selfp, thread->stack_ptr);
#endif
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
	guint32 self=GetCurrentThreadId ();

#ifdef LIBGC_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": %d - %p", self, threads);
#endif

	EnterCriticalSection (&threads_mutex);

	if (threads != NULL)
		mono_g_hash_table_foreach (threads, gc_push_all_stacks, &self);
	
	LeaveCriticalSection (&threads_mutex);
}

#endif /* WITH_INCLUDED_LIBGC */
