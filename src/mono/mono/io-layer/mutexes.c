#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <string.h>
#include <unistd.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/wait-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/mutex-private.h>

#undef DEBUG

static void mutex_close(gpointer handle);
static void mutex_signal(gpointer handle);
static void mutex_own (gpointer handle);
static gboolean mutex_is_owned (gpointer handle);

static struct _WapiHandleOps mutex_ops = {
	mutex_close,		/* close */
	NULL,			/* getfiletype */
	NULL,			/* readfile */
	NULL,			/* writefile */
	NULL,			/* flushfile */
	NULL,			/* seek */
	NULL,			/* setendoffile */
	NULL,			/* getfilesize */
	NULL,			/* getfiletime */
	NULL,			/* setfiletime */
	mutex_signal,		/* signal */
	mutex_own,		/* own */
	mutex_is_owned,		/* is_owned */
};

static pthread_once_t mutex_ops_once=PTHREAD_ONCE_INIT;

static void mutex_ops_init (void)
{
	_wapi_handle_register_ops (WAPI_HANDLE_MUTEX, &mutex_ops);
	_wapi_handle_register_capabilities (WAPI_HANDLE_MUTEX,
					    WAPI_HANDLE_CAP_WAIT |
					    WAPI_HANDLE_CAP_SIGNAL |
					    WAPI_HANDLE_CAP_OWN);
}

static void mutex_close(gpointer handle)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				(gpointer *)&mutex_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up mutex handle %p", handle);
		return;
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": closing mutex handle %p", handle);
#endif

	if(mutex_handle->name!=0) {
		_wapi_handle_scratch_delete (mutex_handle->name);
		mutex_handle->name=0;
	}
}

static void mutex_signal(gpointer handle)
{
	ReleaseMutex(handle);
}

static void mutex_own (gpointer handle)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				(gpointer *)&mutex_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up mutex handle %p", handle);
		return;
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": owning mutex handle %p", handle);
#endif

	_wapi_handle_set_signal_state (handle, FALSE, FALSE);
	
	mutex_handle->pid=getpid ();
	mutex_handle->tid=pthread_self ();
	mutex_handle->recursion++;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": mutex handle %p locked %d times by %d:%ld", handle,
		   mutex_handle->recursion, mutex_handle->pid,
		   mutex_handle->tid);
#endif
}

static gboolean mutex_is_owned (gpointer handle)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				(gpointer *)&mutex_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up mutex handle %p", handle);
		return(FALSE);
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": testing ownership mutex handle %p", handle);
#endif

	if(mutex_handle->recursion>0 &&
	   mutex_handle->pid==getpid () &&
	   mutex_handle->tid==pthread_self ()) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": mutex handle %p owned by %d:%ld", handle,
			   getpid (), pthread_self ());
#endif

		return(TRUE);
	} else {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": mutex handle %p not owned by %d:%ld", handle,
			   getpid (), pthread_self ());
#endif

		return(FALSE);
	}
}

/**
 * CreateMutex:
 * @security: Ignored for now.
 * @owned: If %TRUE, the mutex is created with the calling thread
 * already owning the mutex.
 * @name:Pointer to a string specifying the name of this mutex, or
 * %NULL.
 *
 * Creates a new mutex handle.  A mutex is signalled when no thread
 * owns it.  A thread acquires ownership of the mutex by waiting for
 * it with WaitForSingleObject() or WaitForMultipleObjects().  A
 * thread relinquishes ownership with ReleaseMutex().
 *
 * A thread that owns a mutex can specify the same mutex in repeated
 * wait function calls without blocking.  The thread must call
 * ReleaseMutex() an equal number of times to release the mutex.
 *
 * Return value: A new handle, or %NULL on error.
 */
gpointer CreateMutex(WapiSecurityAttributes *security G_GNUC_UNUSED, gboolean owned,
			const guchar *name)
{
	struct _WapiHandle_mutex *mutex_handle;
	gpointer handle;
	gboolean ok;
	
	pthread_once (&mutex_ops_once, mutex_ops_init);
	
	handle=_wapi_handle_new (WAPI_HANDLE_MUTEX);
	if(handle==_WAPI_HANDLE_INVALID) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error creating mutex handle");
		return(NULL);
	}

	_wapi_handle_lock_handle (handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				(gpointer *)&mutex_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up mutex handle %p", handle);
		_wapi_handle_unlock_handle (handle);
		return(NULL);
	}

	if(name!=NULL) {
		mutex_handle->name=_wapi_handle_scratch_store (name,
							       strlen (name));
	}
	
	if(owned==TRUE) {
		mutex_own (handle);
	} else {
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": returning mutex handle %p",
		   handle);
#endif

	_wapi_handle_unlock_handle (handle);

	return(handle);
}

/**
 * ReleaseMutex:
 * @handle: The mutex handle.
 *
 * Releases ownership if the mutex handle @handle.
 *
 * Return value: %TRUE on success, %FALSE otherwise.  This function
 * fails if the calling thread does not own the mutex @handle.
 */
gboolean ReleaseMutex(gpointer handle)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	pthread_t tid=pthread_self();
	pid_t pid=getpid ();
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				(gpointer *)&mutex_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up mutex handle %p", handle);
		return(FALSE);
	}

	_wapi_handle_lock_handle (handle);
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Releasing mutex handle %p",
		  handle);
#endif

	if(mutex_handle->tid!=tid || mutex_handle->pid!=pid) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": We don't own mutex handle %p (owned by %d:%ld, me %d:%ld)", handle, mutex_handle->pid, mutex_handle->tid, pid, tid);
#endif

		_wapi_handle_unlock_handle (handle);
		return(FALSE);
	}

	/* OK, we own this mutex */
	mutex_handle->recursion--;
	
	if(mutex_handle->recursion==0) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Unlocking mutex handle %p",
			  handle);
#endif

		mutex_handle->pid=0;
		mutex_handle->tid=0;
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}

	_wapi_handle_unlock_handle (handle);
	
	return(TRUE);
}
