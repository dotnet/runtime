/*
 * threads.c: Thread support internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
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
#include <mono/io-layer/io-layer.h>

#include <mono/os/gc_wrapper.h>

#undef THREAD_DEBUG
#undef THREAD_LOCK_DEBUG
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
 
/* Controls access to the 'threads' array */
static CRITICAL_SECTION threads_mutex;

/* Controls access to the sync field in MonoObjects, to avoid race
 * conditions when adding sync data to an object for the first time.
 */
static CRITICAL_SECTION monitor_mutex;

/* The hash of existing threads (key is thread ID) that need joining
 * before exit
 */
static MonoGHashTable *threads=NULL;

/* The MonoObject associated with the main thread */
static MonoThread *main_thread;

/* The TLS key that holds the MonoObject assigned to each thread */
static guint32 current_object_key;

/* function called at thread start */
static MonoThreadStartCB mono_thread_start_cb = NULL;

/* function called at thread attach */
static MonoThreadStartCB mono_thread_attach_cb = NULL;

/* The TLS key that holds the LocalDataStoreSlot hash in each thread */
static guint32 slothash_key;

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
		threads=mono_g_hash_table_new(g_int_hash, g_int_equal);
	}

	/* GHashTable will remove a previous entry if a duplicate key
	 * is stored, which is exactly what we want: we store the
	 * thread both in the start_wrapper (in the subthread), and as
	 * soon as possible in the parent thread.  This is to minimise
	 * the window in which the thread exists but we haven't
	 * recorded it.
	 */
	
	/* We don't need to duplicate thread->handle, because it is
	 * only closed when the thread object is finalized by the GC.
	 */
	mono_g_hash_table_insert(threads, &thread->tid, thread);
	LeaveCriticalSection(&threads_mutex);
}

static void handle_remove(guint32 tid)
{
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": thread ID %d", tid);
#endif

	EnterCriticalSection(&threads_mutex);

	mono_g_hash_table_remove (threads, &tid);
	
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

static void thread_cleanup (guint32 tid)
{
	mono_profiler_thread_end (tid);
	handle_remove (tid);
}

static guint32 start_wrapper(void *data)
{
	struct StartInfo *start_info=(struct StartInfo *)data;
	guint32 (*start_func)(void *);
	void *this;
	guint32 tid;
	
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Start wrapper");
#endif

	/* FIXME: GC problem here with recorded object
	 * pointer!
	 *
	 * This is recorded so CurrentThread can return the
	 * Thread object.
	 */
	TlsSetValue (current_object_key, start_info->obj);
	start_func = start_info->func;
	mono_domain_set (start_info->domain);
	this = start_info->this;

	tid=GetCurrentThreadId ();
	/* Set the thread ID here as well as in the parent thread,
	 * because we don't know whether the thread object will
	 * already have its ID set before we get to it.  This isn't a
	 * race condition, because if we're not guaranteed to get the
	 * same number in both the parent and child threads, then
	 * something else is seriously broken.
	 */
	start_info->obj->tid=tid;
	
	handle_store(start_info->obj);
	g_free (start_info);

	mono_profiler_thread_start (tid);

	if (mono_thread_start_cb)
		mono_thread_start_cb (&tid);
	start_func (this);

	/* If the thread calls ExitThread at all, this remaining code
	 * will not be executed, but the main thread will eventually
	 * call thread_cleanup() on this thread's behalf.
	 */

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Start wrapper terminating");
#endif
	
	thread_cleanup (tid);
	
	return(0);
}

MonoThread *
mono_thread_create_arg (MonoDomain *domain, gpointer func, void *arg)
{
	MonoThread *thread;
	HANDLE thread_handle;
	struct StartInfo *start_info;
	guint32 tid;
	
	thread = (MonoThread *)mono_object_new (domain,
						mono_defaults.thread_class);

	start_info=g_new0 (struct StartInfo, 1);
	start_info->func = func;
	start_info->obj = thread;
	start_info->domain = domain;
	start_info->this = arg;
		
	thread_handle = CreateThread(NULL, 0, start_wrapper, start_info, 0, &tid);
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Started thread ID %d (handle %p)",
		  tid, thread_handle);
#endif
	g_assert (thread_handle);

	thread->handle=thread_handle;
	thread->tid=tid;

	handle_store(thread);

	return thread;
}

static void
mono_attached_thread_cleanup (MonoObject *obj)
{
}

MonoThread *
mono_thread_attach (MonoDomain *domain)
{
	MonoThread *thread;
	HANDLE thread_handle;
	struct StartInfo *start_info;
	guint32 tid;

	thread = (MonoThread *)mono_object_new (domain,
						mono_defaults.thread_class);

/*	start_info=g_new0 (struct StartInfo, 1);
	start_info->func = NULL;
	start_info->obj = thread;
	start_info->domain = domain;
	start_info->this = NULL;
*/
	start_info = NULL;
//	thread_handle = AttachThread (NULL, 0, start_wrapper, start_info, 0, &tid);
	thread_handle = GetCurrentThread ();
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Started thread ID %d (handle %p)",
		  tid, thread_handle);
#endif
	g_assert (thread_handle);

	thread->handle=thread_handle;
	thread->tid=tid;

	handle_store(thread);

	TlsSetValue (current_object_key, thread);
	mono_domain_set (domain);
	thread->tid=GetCurrentThreadId ();
	if (mono_thread_attach_cb)
	  mono_thread_attach_cb (-1);
	return thread;
}


MonoThread *
mono_thread_create (MonoDomain *domain, gpointer func)
{
  return mono_thread_create_arg (domain, func, NULL);
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
	
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Trying to start a new thread: this (%p) start (%p)",
		  this, start);
#endif
	
	im = mono_get_delegate_invoke (start->vtable->klass);
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
#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Closing thread %p, handle %p",
		   this, thread);
#endif

	CloseHandle (thread);
}

void ves_icall_System_Threading_Thread_Start_internal(MonoThread *this,
						      HANDLE thread)
{
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Launching thread %p", this);
#endif

	/* Only store the handle when the thread is about to be
	 * launched, to avoid the main thread deadlocking while trying
	 * to clean up a thread that will never be signalled.
	 */
	handle_store(this);

	ResumeThread(thread);
}

void ves_icall_System_Threading_Thread_Sleep_internal(gint32 ms)
{
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Sleeping for %d ms", ms);
#endif

	Sleep(ms);
}

gint32
ves_icall_System_Threading_Thread_GetDomainID (void) 
{
	return mono_domain_get()->domain_id;
}

MonoThread *
mono_thread_current (void)
{
	MonoThread *thread;
	
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
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Storing key %p", data);
#endif

	/* Object location stored here */
	TlsSetValue(slothash_key, data);
}

MonoObject *ves_icall_System_Threading_Thread_SlotHash_lookup(void)
{
	MonoObject *data;

	data=TlsGetValue(slothash_key);
	
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Retrieved key %p", data);
#endif
	
	return(data);
}

static void mon_finalize (void *o, void *unused)
{
	MonoThreadsSync *mon=(MonoThreadsSync *)o;
	
#ifdef THREAD_LOCK_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Finalizing sync");
#endif
	
	CloseHandle (mon->monitor);
	CloseHandle (mon->sema);
	CloseHandle (mon->waiters_done);
}

static MonoThreadsSync *mon_new(void)
{
	MonoThreadsSync *new;
	
#if HAVE_BOEHM_GC
	new=(MonoThreadsSync *)GC_MALLOC (sizeof(MonoThreadsSync));
	GC_REGISTER_FINALIZER (new, mon_finalize, NULL, NULL, NULL);
#else
	/* This should be freed when the object that owns it is
	 * deleted
	 */
	new=(MonoThreadsSync *)g_new0 (MonoThreadsSync, 1);
#endif
	
	new->monitor=CreateMutex(NULL, FALSE, NULL);
	if(new->monitor==NULL) {
		/* Throw some sort of system exception? (ditto for the
		 * sem and event handles below)
		 */
	}

	new->waiters_count=0;
	new->was_broadcast=FALSE;
	InitializeCriticalSection(&new->waiters_count_lock);
	new->sema=CreateSemaphore(NULL, 0, 0x7fffffff, NULL);
	new->waiters_done=CreateEvent(NULL, FALSE, FALSE, NULL);
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": ThreadsSync %p mutex created: %p, sem: %p, event: %p",
		  new, new->monitor, new->sema, new->waiters_done);
#endif
	
	return(new);
}

gboolean ves_icall_System_Threading_Monitor_Monitor_try_enter(MonoObject *obj,
							      int ms)
{
	MonoThreadsSync *mon;
	guint32 ret;
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Trying to lock object %p in thread %d", obj,
		  GetCurrentThreadId());
#endif

	EnterCriticalSection(&monitor_mutex);

	mon=obj->synchronisation;
	if(mon==NULL) {
		mon=mon_new();
		obj->synchronisation=mon;
	}
	
	/* Don't hold the monitor lock while waiting to acquire the
	 * object lock
	 */
	LeaveCriticalSection(&monitor_mutex);
	
	/* Acquire the mutex */
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Acquiring monitor mutex %p",
		  mon->monitor);
#endif
	ret=WaitForSingleObject(mon->monitor, ms);
	if(ret==WAIT_OBJECT_0) {
		mon->count++;
		mon->tid=GetCurrentThreadId();
	
#ifdef THREAD_LOCK_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": object %p now locked %d times", obj, mon->count);
#endif

		return(TRUE);
	}

	return(FALSE);
}

void ves_icall_System_Threading_Monitor_Monitor_exit(MonoObject *obj)
{
	MonoThreadsSync *mon;
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Unlocking %p in thread %d", obj,
		  GetCurrentThreadId());
#endif

	/* No need to lock monitor_mutex here because we only adjust
	 * the monitor state if this thread already owns the lock
	 */
	mon=obj->synchronisation;

	if(mon==NULL) {
		return;
	}

	if(mon->tid!=GetCurrentThreadId()) {
		return;
	}
	
	mon->count--;
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": %p now locked %d times", obj,
		  mon->count);
#endif

	if(mon->count==0) {
		mon->tid=0;	/* FIXME: check that 0 isnt a valid id */
	}
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Releasing mutex %p", mon->monitor);
#endif

	ReleaseMutex(mon->monitor);
}

gboolean ves_icall_System_Threading_Monitor_Monitor_test_owner(MonoObject *obj)
{
	MonoThreadsSync *mon;
	gboolean ret=FALSE;
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Testing if %p is owned by thread %d", obj,
		  GetCurrentThreadId());
#endif

	EnterCriticalSection(&monitor_mutex);
	
	mon=obj->synchronisation;
	if(mon==NULL) {
		goto finished;
	}

	if(mon->tid!=GetCurrentThreadId()) {
#ifdef THREAD_LOCK_DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": object %p is owned by thread %d", obj, mon->tid);
#endif

		goto finished;
	}
	
	ret=TRUE;
	
finished:
	LeaveCriticalSection(&monitor_mutex);

	return(ret);
}

gboolean ves_icall_System_Threading_Monitor_Monitor_test_synchronised(MonoObject *obj)
{
	MonoThreadsSync *mon;
	gboolean ret=FALSE;
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Testing if %p is owned by any thread", obj);
#endif

	EnterCriticalSection(&monitor_mutex);
	
	mon=obj->synchronisation;
	if(mon==NULL) {
		goto finished;
	}

	if(mon->tid==0) {
		goto finished;
	}
	
	g_assert(mon->count);
	
	ret=TRUE;
	
finished:
	LeaveCriticalSection(&monitor_mutex);

	return(ret);
}

	
void ves_icall_System_Threading_Monitor_Monitor_pulse(MonoObject *obj)
{
	gboolean have_waiters;
	MonoThreadsSync *mon;
	
#ifdef THREAD_LOCK_DEBUG
	g_message("Pulsing %p in thread %d", obj, GetCurrentThreadId());
#endif

	EnterCriticalSection(&monitor_mutex);
	
	mon=obj->synchronisation;
	if(mon==NULL) {
		LeaveCriticalSection(&monitor_mutex);
		return;
	}

	if(mon->tid!=GetCurrentThreadId()) {
		LeaveCriticalSection(&monitor_mutex);
		return;
	}
	LeaveCriticalSection(&monitor_mutex);
	
	EnterCriticalSection(&mon->waiters_count_lock);
	have_waiters=(mon->waiters_count>0);
	LeaveCriticalSection(&mon->waiters_count_lock);
	
	if(have_waiters==TRUE) {
		ReleaseSemaphore(mon->sema, 1, 0);
	}
}

void ves_icall_System_Threading_Monitor_Monitor_pulse_all(MonoObject *obj)
{
	gboolean have_waiters=FALSE;
	MonoThreadsSync *mon;
	
#ifdef THREAD_LOCK_DEBUG
	g_message("Pulsing all %p", obj);
#endif

	EnterCriticalSection(&monitor_mutex);
	
	mon=obj->synchronisation;
	if(mon==NULL) {
		LeaveCriticalSection(&monitor_mutex);
		return;
	}

	if(mon->tid!=GetCurrentThreadId()) {
		LeaveCriticalSection(&monitor_mutex);
		return;
	}
	LeaveCriticalSection(&monitor_mutex);
	
	EnterCriticalSection(&mon->waiters_count_lock);
	if(mon->waiters_count>0) {
		mon->was_broadcast=TRUE;
		have_waiters=TRUE;
	}
	
	if(have_waiters==TRUE) {
		ReleaseSemaphore(mon->sema, mon->waiters_count, 0);
		
		LeaveCriticalSection(&mon->waiters_count_lock);
		
		WaitForSingleObject(mon->waiters_done, INFINITE);
		mon->was_broadcast=FALSE;
	} else {
		LeaveCriticalSection(&mon->waiters_count_lock);
	}
}

gboolean ves_icall_System_Threading_Monitor_Monitor_wait(MonoObject *obj,
							 int ms)
{
	gboolean last_waiter;
	MonoThreadsSync *mon;
	guint32 save_count;
	
#ifdef THREAD_LOCK_DEBUG
	g_message("Trying to wait for %p in thread %d with timeout %dms", obj,
		  GetCurrentThreadId(), ms);
#endif

	EnterCriticalSection(&monitor_mutex);
	
	mon=obj->synchronisation;
	if(mon==NULL) {
		LeaveCriticalSection(&monitor_mutex);
		return(FALSE);
	}

	if(mon->tid!=GetCurrentThreadId()) {
		LeaveCriticalSection(&monitor_mutex);
		return(FALSE);
	}
	LeaveCriticalSection(&monitor_mutex);
	
	EnterCriticalSection(&mon->waiters_count_lock);
	mon->waiters_count++;
	LeaveCriticalSection(&mon->waiters_count_lock);
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": %p locked %d times", obj,
		  mon->count);
#endif

	/* We need to put the lock count back afterwards */
	save_count=mon->count;
	
	while(mon->count>1) {
#ifdef THREAD_LOCK_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Releasing mutex %p",
			  mon->monitor);
#endif

		ReleaseMutex(mon->monitor);
		mon->count--;
	}
	
	/* We're releasing this mutex */
	mon->count=0;
	mon->tid=0;
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Signalling monitor mutex %p",
		  mon->monitor);
#endif

	SignalObjectAndWait(mon->monitor, mon->sema, INFINITE, FALSE);
	
	EnterCriticalSection(&mon->waiters_count_lock);
	mon->waiters_count++;
	last_waiter=mon->was_broadcast && mon->waiters_count==0;
	LeaveCriticalSection(&mon->waiters_count_lock);
	
	if(last_waiter) {
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Waiting for monitor mutex %p",
		  mon->monitor);
#endif
		SignalObjectAndWait(mon->waiters_done, mon->monitor, INFINITE, FALSE);
	} else {
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Waiting for monitor mutex %p",
		  mon->monitor);
#endif
		WaitForSingleObject(mon->monitor, INFINITE);
	}

	/* We've reclaimed this mutex */
	mon->count=save_count;
	mon->tid=GetCurrentThreadId();

	/* Lock the mutex the required number of times */
	while(save_count>1) {
#ifdef THREAD_LOCK_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Waiting for monitor mutex %p", mon->monitor);
#endif
		WaitForSingleObject(mon->monitor, INFINITE);
		save_count--;
	}
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": %p still locked %d times", obj,
		  mon->count);
#endif
	
	return(TRUE);
}

/* FIXME: exitContext isnt documented */
gboolean ves_icall_System_Threading_WaitHandle_WaitAll_internal(MonoArray *mono_handles, gint32 ms, gboolean exitContext)
{
	HANDLE *handles;
	guint32 numhandles;
	guint32 ret;
	guint32 i;
	
	numhandles=mono_array_length(mono_handles);
	handles=g_new0(HANDLE, numhandles);
	for(i=0; i<numhandles; i++) {
		handles[i]=mono_array_get(mono_handles, HANDLE, i);
	}
	
	if(ms== -1) {
		ms=INFINITE;
	}
	
	ret=WaitForMultipleObjects(numhandles, handles, TRUE, ms);

	g_free(handles);
	
	if(ret==WAIT_FAILED) {
#ifdef THREAD_WAIT_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Wait failed");
#endif
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT) {
#ifdef THREAD_WAIT_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Wait timed out");
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
	
	numhandles=mono_array_length(mono_handles);
	handles=g_new0(HANDLE, numhandles);
	for(i=0; i<numhandles; i++) {
		handles[i]=mono_array_get(mono_handles, HANDLE, i);
	}
	
	if(ms== -1) {
		ms=INFINITE;
	}

	ret=WaitForMultipleObjects(numhandles, handles, FALSE, ms);

	g_free(handles);
	
#ifdef THREAD_WAIT_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": returning %d", ret);
#endif

	return(ret);
}

/* FIXME: exitContext isnt documented */
gboolean ves_icall_System_Threading_WaitHandle_WaitOne_internal(MonoObject *this, HANDLE handle, gint32 ms, gboolean exitContext)
{
	guint32 ret;
	
#ifdef THREAD_WAIT_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": waiting for %p", handle);
#endif
	
	if(ms== -1) {
		ms=INFINITE;
	}
	
	ret=WaitForSingleObject(handle, ms);
	if(ret==WAIT_FAILED) {
#ifdef THREAD_WAIT_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Wait failed");
#endif
		return(FALSE);
	} else if(ret==WAIT_TIMEOUT) {
#ifdef THREAD_WAIT_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Wait timed out");
#endif
		return(FALSE);
	}
	
	return(TRUE);
}

HANDLE ves_icall_System_Threading_Mutex_CreateMutex_internal (MonoBoolean owned,char *name) {    
   return(CreateMutex(NULL,owned,name));	                 
}                                                                   

void ves_icall_System_Threading_Mutex_ReleaseMutex_internal (HANDLE handle ) { 
	ReleaseMutex(handle);
}

HANDLE ves_icall_System_Threading_Events_CreateEvent_internal (MonoBoolean manual,
															  MonoBoolean initial,
															  char *name) {
	return (CreateEvent(NULL,manual,initial,name));
}

gboolean ves_icall_System_Threading_Events_SetEvent_internal (HANDLE handle) {
	return (SetEvent(handle));
}

gboolean ves_icall_System_Threading_Events_ResetEvent_internal (HANDLE handle) {
	return (ResetEvent(handle));
}

gint32 ves_icall_System_Threading_Interlocked_Increment_Int (gint32 *location)
{
	return InterlockedIncrement (location);
}

gint64 ves_icall_System_Threading_Interlocked_Increment_Long (gint64 *location)
{
	gint32 lowret;
	gint32 highret;

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
	return InterlockedDecrement(location);
}

gint64 ves_icall_System_Threading_Interlocked_Decrement_Long (gint64 * location)
{
	gint32 lowret;
	gint32 highret;

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
	return InterlockedExchange(location1, value);
}

MonoObject * ves_icall_System_Threading_Interlocked_Exchange_Object (MonoObject **location1, MonoObject *value)
{
	return (MonoObject *) InterlockedExchangePointer((gpointer *) location1, value);
}

gfloat ves_icall_System_Threading_Interlocked_Exchange_Single (gfloat *location1, gfloat value)
{
	IntFloatUnion val, ret;

	val.fval = value;
	ret.ival = InterlockedExchange((gint32 *) location1, val.ival);

	return ret.fval;
}

gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int(gint32 *location1, gint32 value, gint32 comparand)
{
	return InterlockedCompareExchange(location1, value, comparand);
}

MonoObject * ves_icall_System_Threading_Interlocked_CompareExchange_Object (MonoObject **location1, MonoObject *value, MonoObject *comparand)
{
	return (MonoObject *) InterlockedCompareExchangePointer((gpointer *) location1, value, comparand);
}

gfloat ves_icall_System_Threading_Interlocked_CompareExchange_Single (gfloat *location1, gfloat value, gfloat comparand)
{
	IntFloatUnion val, ret, cmp;

	val.fval = value;
	cmp.fval = comparand;
	ret.ival = InterlockedCompareExchange((gint32 *) location1, val.ival, cmp.ival);

	return ret.fval;
}

void
ves_icall_System_Threading_Thread_Abort (MonoThread *thread, MonoObject *state)
{
	thread->abort_state = state;
	thread->abort_exc = mono_get_exception_thread_abort ();


	/* fixme: store the state somewhere */
#ifndef __MINGW32__
#ifdef PTHREAD_POINTER_ID
	pthread_kill (GUINT_TO_POINTER(thread->tid), SIGUSR1);
#else
	pthread_kill (thread->tid, SIGUSR1);
#endif
#else
	g_assert_not_reached ();
#endif
}

void
ves_icall_System_Threading_Thread_ResetAbort (void)
{
	MonoThread *thread = mono_thread_current ();
	
	if (!thread->abort_exc) {
		const char *msg = "Unable to reset abort because no abort was requested";
		mono_raise_exception (mono_get_exception_thread_state (msg));
	} else {
		thread->abort_exc = NULL;
		thread->abort_state = NULL;
	}
}

void mono_thread_init_with_attach (MonoDomain *domain, MonoThreadStartCB start_cb, MonoThreadStartCB attach_cb)
{
	/* Build a System.Threading.Thread object instance to return
	 * for the main line's Thread.CurrentThread property.
	 */
	
	/* I wonder what happens if someone tries to destroy this
	 * object? In theory, I guess the whole program should act as
	 * though exit() were called :-)
	 */
#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Starting to build main Thread object");
#endif
	main_thread = (MonoThread *)mono_object_new (domain, mono_defaults.thread_class);

#if 0
	main_thread->handle = OpenThread(THREAD_ALL_ACCESS, FALSE, GetCurrentThreadId());
#endif

#ifdef THREAD_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Finished building main Thread object: %p", main_thread);
#endif

	InitializeCriticalSection(&threads_mutex);
	InitializeCriticalSection(&monitor_mutex);
	InitializeCriticalSection(&interlocked_mutex);
	
	current_object_key=TlsAlloc();
#ifdef THREAD_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Allocated current_object_key %d",
		   current_object_key);
#endif

	TlsSetValue(current_object_key, main_thread);

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

void mono_thread_init(MonoDomain *domain, MonoThreadStartCB start_cb)
{
  mono_thread_init_with_attach (domain, start_cb, NULL);
}

#ifdef THREAD_DEBUG
static void print_tids (gpointer key, gpointer value, gpointer user)
{
	g_message ("Waiting for: %d", *(guint32 *)key);
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
	g_message(G_GNUC_PRETTY_FUNCTION ": %d threads to wait for in this batch", wait->num);
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
		
		if(mono_g_hash_table_lookup (threads, &tid)!=NULL) {
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
			thread_cleanup (tid);
		}
	}
}

static void build_wait_tids (gpointer key, gpointer value, gpointer user)
{
	struct wait_data *wait=(struct wait_data *)user;

	if(wait->num<MAXIMUM_WAIT_OBJECTS) {
		MonoThread *thread=(MonoThread *)value;
		
		/* Theres a theoretical chance that thread->handle
		 * might be NULL if the child thread has called
		 * handle_store() but the parent thread hasn't set the
		 * handle pointer yet.  WaitForMultipleObjects will
		 * fail, and we'll just go round the loop again.  By
		 * that time the handle should be stored properly.
		 */
		wait->handles[wait->num]=thread->handle;
		wait->threads[wait->num]=thread;
		wait->num++;
	} else {
		/* Just ignore the rest, we can't do anything with
		 * them yet
		 */
	}
}

void mono_thread_cleanup(void)
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
