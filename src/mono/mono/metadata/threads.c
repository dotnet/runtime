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

#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/monitor.h>
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
 
/*
 * The "os_handle" field of the WaitHandle class.
 */
static MonoClassField *wait_handle_os_handle_field = NULL;

/* Controls access to the 'threads' hash table */
static CRITICAL_SECTION threads_mutex;

/* The hash of existing threads (key is thread ID) that need joining
 * before exit
 */
static MonoGHashTable *threads=NULL;

/* The TLS key that holds the MonoObject assigned to each thread */
static guint32 current_object_key;

/* function called at thread start */
static MonoThreadStartCB mono_thread_start_cb = NULL;

/* function called at thread attach */
static MonoThreadAttachCB mono_thread_attach_cb = NULL;

/* function called when a new thread has been created */
static MonoThreadCallbacks *mono_thread_callbacks = NULL;

/* The TLS key that holds the LocalDataStoreSlot hash in each thread */
static guint32 slothash_key;

static void thread_adjust_static_data (MonoThread *thread);

/* Spin lock for InterlockedXXX 64 bit functions */
static CRITICAL_SECTION interlocked_mutex;

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
		threads=mono_g_hash_table_new(g_direct_hash, g_direct_equal);
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
	mono_monitor_try_enter ((MonoObject *)thread, INFINITE);
	thread->state |= ThreadState_Stopped;
	mono_monitor_exit ((MonoObject *)thread);
	
	mono_profiler_thread_end (thread->tid);
	handle_remove (thread->tid);
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
	
	mono_domain_set (start_info->domain);

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

	TlsSetValue (current_object_key, thread);

	mono_profiler_thread_start (tid);

	if(thread->start_notify!=NULL) {
		/* Let the thread that called Start() know we're
		 * ready
		 */
		ReleaseSemaphore (thread->start_notify, 1, NULL);
	}
	
	g_free (start_info);

	thread_adjust_static_data (thread);

	start_func (this);

	/* If the thread calls ExitThread at all, this remaining code
	 * will not be executed, but the main thread will eventually
	 * call thread_cleanup() on this thread's behalf.
	 */

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Start wrapper terminating",
		  GetCurrentThreadId ());
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
	thread_handle = CreateThread(NULL, 0, start_wrapper, start_info,
				     CREATE_SUSPENDED, &tid);
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Started thread ID %d (handle %p)",
		  tid, thread_handle);
#endif
	g_assert (thread_handle);

	thread->handle=thread_handle;
	thread->tid=tid;

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
		g_warning ("mono_thread_attach called for an already attached thread");
		if (mono_thread_attach_cb) {
			mono_thread_attach_cb (tid, &tid);
		}
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

	TlsSetValue (current_object_key, thread);
	mono_domain_set (domain);

	thread_adjust_static_data (thread);

	if (mono_thread_attach_cb) {
		mono_thread_attach_cb (tid, &tid);
	}

	return(thread);
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

		thread=CreateThread(NULL, 0, start_wrapper, start_info,
				    CREATE_SUSPENDED, &tid);
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

		WaitForSingleObject (this->start_notify, INFINITE);
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
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Sleeping for %d ms", ms);
#endif

	Sleep(ms);
}

gint32
ves_icall_System_Threading_Thread_GetDomainID (void) 
{
	MONO_ARCH_SAVE_REGS;

	return mono_domain_get()->domain_id;
}

MonoThread *
mono_thread_current (void)
{
	MonoThread *thread;
	
	MONO_ARCH_SAVE_REGS;

	/* Find the current thread object */
	thread=TlsGetValue (current_object_key);
	
#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": returning %p", thread);
#endif

	return (thread);
}

gboolean ves_icall_System_Threading_Thread_Join_internal(MonoThread *this,
							 int ms, HANDLE thread)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	if(ms== -1) {
		ms=INFINITE;
	}
#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": joining thread handle %p, %d ms",
		   thread, ms);
#endif
	
	ret=WaitForSingleObject(thread, ms);
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
	
	ret=WaitForMultipleObjects(numhandles, handles, TRUE, ms);

	g_free(handles);
	
	if(ret==WAIT_FAILED) {
#ifdef THREAD_WAIT_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Wait failed",
			  GetCurrentThreadId ());
#endif
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT) {
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

	ret=WaitForMultipleObjects(numhandles, handles, FALSE, ms);

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
	
	ret=WaitForSingleObject(handle, ms);
	if(ret==WAIT_FAILED) {
#ifdef THREAD_WAIT_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Wait failed",
			  GetCurrentThreadId ());
#endif
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT) {
#ifdef THREAD_WAIT_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Wait timed out",
			  GetCurrentThreadId ());
#endif
		return(FALSE);
	}
	
	return(TRUE);
}

HANDLE ves_icall_System_Threading_Mutex_CreateMutex_internal (MonoBoolean owned,char *name) { 
	MONO_ARCH_SAVE_REGS;
   
	return(CreateMutex(NULL,owned,name));	                 
}                                                                   

void ves_icall_System_Threading_Mutex_ReleaseMutex_internal (HANDLE handle ) { 
	MONO_ARCH_SAVE_REGS;

	ReleaseMutex(handle);
}

HANDLE ves_icall_System_Threading_Events_CreateEvent_internal (MonoBoolean manual, MonoBoolean initial, char *name) {
	MONO_ARCH_SAVE_REGS;

	return (CreateEvent(NULL,manual,initial,name));
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

void
ves_icall_System_Threading_Thread_Abort (MonoThread *thread, MonoObject *state)
{
	MONO_ARCH_SAVE_REGS;

	thread->abort_state = state;
	thread->abort_exc = mono_get_exception_thread_abort ();

#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": (%d) Abort requested for %p (%d)", GetCurrentThreadId (),
		   thread, thread->tid);
#endif
	
#ifdef __MINGW32__
	g_assert_not_reached ();
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
ves_icall_System_Threading_Thread_ResetAbort (void)
{
	MonoThread *thread = mono_thread_current ();
	
	MONO_ARCH_SAVE_REGS;

	if (!thread->abort_exc) {
		const char *msg = "Unable to reset abort because no abort was requested";
		mono_raise_exception (mono_get_exception_thread_state (msg));
	} else {
		thread->abort_exc = NULL;
		thread->abort_state = NULL;
	}
}

void mono_thread_init (MonoThreadStartCB start_cb,
		       MonoThreadAttachCB attach_cb)
{
	InitializeCriticalSection(&threads_mutex);
	InitializeCriticalSection(&interlocked_mutex);
	
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

static void wait_for_tids (struct wait_data *wait)
{
	guint32 i, ret;
	
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": %d threads to wait for in this batch", wait->num);
#endif

	ret=WaitForMultipleObjects(wait->num, wait->handles, TRUE, INFINITE);
	if(ret==WAIT_FAILED) {
		/* See the comment in build_wait_tids() */
#ifdef THREAD_DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Wait failed");
#endif
		return;
	}
	

	for(i=0; i<wait->num; i++) {
		guint32 tid=wait->threads[i]->tid;
		
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
		MonoThread *thread=(MonoThread *)value;

		/* BUG: For now we just ignore background threads, we should abort them
		*/
		if (thread->state & ThreadState_Background)
			return; /* just leave, ignore */
		
		wait->handles[wait->num]=thread->handle;
		wait->threads[wait->num]=thread;
		wait->num++;
	} else {
		/* Just ignore the rest, we can't do anything with
		 * them yet
		 */
	}
}

void mono_thread_manage (void)
{
	struct wait_data *wait=g_new0 (struct wait_data, 1);
	
	/* join each thread that's still running */
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Joining each running thread...");
#endif
	
	if(threads==NULL) {
#ifdef THREAD_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": No threads");
#endif
		return;
	}
	
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
			wait_for_tids (wait);
		}
	} while(wait->num>0);
	
	g_free (wait);
	
	mono_g_hash_table_destroy(threads);
	threads=NULL;
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

static int static_data_idx = 0;
static int static_data_offset = 0;
#define NUM_STATIC_DATA_IDX 8
static const int static_data_size [NUM_STATIC_DATA_IDX] = {
	1024, 4096, 16384, 65536, 262144, 1048576, 4194304, 16777216
};

static void 
thread_alloc_static_data (MonoThread *thread, guint32 offset)
{
	guint idx = (offset >> 24) - 1;
	int i;

	if (!thread->static_data) {
#if HAVE_BOEHM_GC
		thread->static_data = GC_MALLOC (static_data_size [0]);
#else
		thread->static_data = g_malloc0 (static_data_size [0]);
#endif
		thread->static_data [0] = thread->static_data;
	}
	for (i = 1; i < idx; ++i) {
		if (thread->static_data [i])
			continue;
#if HAVE_BOEHM_GC
		thread->static_data [i] = GC_MALLOC (static_data_size [i]);
#else
		thread->static_data [i] = g_malloc0 (static_data_size [i]);
#endif
	}
	
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
	if (static_data_offset || static_data_idx > 0) {
		/* get the current allocated size */
		offset = static_data_offset | ((static_data_idx + 1) << 24);
		thread_alloc_static_data (thread, offset);
	}
	LeaveCriticalSection (&threads_mutex);
}

static void 
alloc_thread_static_data_helper (gpointer key, gpointer value, gpointer user)
{
	MonoThread *thread = value;
	guint32 offset = GPOINTER_TO_UINT (user);
	
	thread_alloc_static_data (thread, offset);
}

/*
 * The offset for a thread static variable is composed of two parts:
 * an index in the array of chunks of memory for the thread (thread->static_data)
 * and an offset in that chunk of mem. This allows allocating less memory in the 
 * common case.
 */
guint32
mono_threads_alloc_static_data (guint32 size, guint32 align)
{
	guint32 offset;
	
	EnterCriticalSection (&threads_mutex);

	if (!static_data_idx && !static_data_offset) {
		/* 
		 * we use the first chunk of the first allocation also as
		 * an array for the rest of the data 
		 */
		static_data_offset = sizeof (gpointer) * NUM_STATIC_DATA_IDX;
	}
	static_data_offset += align - 1;
	static_data_offset &= ~(align - 1);
	if (static_data_offset + size >= static_data_size [static_data_idx]) {
		static_data_idx ++;
		g_assert (size <= static_data_size [static_data_idx]);
		/* 
		 * massive unloading and reloading of domains with thread-static
		 * data may eventually exceed the allocated storage...
		 * Need to check what the MS runtime does in that case.
		 * Note that for each appdomain, we need to allocate a separate
		 * thread data slot for security reasons. We could keep track
		 * of the slots per-domain and when the domain is unloaded
		 * out the slots on a sort of free list.
		 */
		g_assert (static_data_idx < NUM_STATIC_DATA_IDX);
		static_data_offset = 0;
	}
	offset = static_data_offset | ((static_data_idx + 1) << 24);
	static_data_offset += size;
	
	mono_g_hash_table_foreach (threads, alloc_thread_static_data_helper, GUINT_TO_POINTER (offset));

	LeaveCriticalSection (&threads_mutex);
	return offset;
}

gpointer
mono_threads_get_static_data (guint32 offset)
{
	MonoThread *thread = mono_thread_current ();
	int idx = offset >> 24;
	
	return ((char*) thread->static_data [idx - 1]) + (offset & 0xffffff);
}

#ifdef WITH_INCLUDED_LIBGC

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
