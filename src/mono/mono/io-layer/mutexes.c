#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <string.h>

#include "mono/io-layer/wapi.h"
#include "wapi-private.h"
#include "wait-private.h"
#include "misc-private.h"
#include "handles-private.h"

#include "mono-mutex.h"

#undef DEBUG

struct _WapiHandle_mutex
{
	WapiHandle handle;
	mono_mutex_t mutex;
	pthread_t tid;
	guint32 recursion;
};

static void mutex_close(WapiHandle *handle);
static gboolean mutex_wait(WapiHandle *handle, WapiHandle *signal, guint32 ms);
static guint32 mutex_wait_multiple(gpointer data);
static void mutex_signal(WapiHandle *handle);

static struct _WapiHandleOps mutex_ops = {
	mutex_close,		/* close */
	NULL,			/* getfiletype */
	NULL,			/* readfile */
	NULL,			/* writefile */
	NULL,			/* seek */
	NULL,			/* setendoffile */
	NULL,			/* getfilesize */
	NULL,			/* getfiletime */
	NULL,			/* setfiletime */
	mutex_wait,		/* wait */
	mutex_wait_multiple,	/* wait_multiple */
	mutex_signal,		/* signal */
};

static void mutex_close(WapiHandle *handle)
{
	struct _WapiHandle_mutex *mutex_handle=(struct _WapiHandle_mutex *)handle;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": closing mutex handle %p",
		  mutex_handle);
#endif

	mono_mutex_destroy(&mutex_handle->mutex);
}

static gboolean mutex_wait(WapiHandle *handle, WapiHandle *signal, guint32 ms)
{
	struct _WapiHandle_mutex *mutex_handle=(struct _WapiHandle_mutex *)handle;
	pthread_t tid=pthread_self();
	int ret;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": waiting for mutex handle %p",
		  mutex_handle);
#endif

	/* Signal this handle now.  It really doesn't matter if some
	 * other thread grabs the mutex before we can
	 */
	if(signal!=NULL) {
		signal->ops->signal(signal);
	}

	if(mutex_handle->tid==tid) {
		/* We already own this mutex, so just increase the count and
		 * return TRUE
		 */

		mutex_handle->recursion++;
	
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Already own mutex handle %p (recursion %d)",
			  mutex_handle, mutex_handle->recursion);
#endif

		return(TRUE);
	}
	
	if(ms==INFINITE) {
		ret=mono_mutex_lock(&mutex_handle->mutex);
	} else {
		struct timespec timeout;
		
		_wapi_calc_timeout(&timeout, ms);
		
		ret=mono_mutex_timedlock(&mutex_handle->mutex, &timeout);
	}

	if(ret==0) {
		/* Mutex locked */
	
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Locking mutex handle %p",
			  mutex_handle);
#endif

		mutex_handle->tid=tid;
		mutex_handle->recursion=1;

		return(TRUE);
	} else {
		/* ret might be ETIMEDOUT for timeout, or other for error */
	
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Failed to lock mutex handle %p: %s", mutex_handle,
			  strerror(ret));
#endif
		return(FALSE);
	}
}

static guint32 mutex_wait_multiple(gpointer data G_GNUC_UNUSED)
{
	WaitQueueItem *item=(WaitQueueItem *)data;
	GPtrArray *needed;
	int ret;
	guint32 numhandles;
	struct timespec timeout;
	pthread_t tid=pthread_self();
	guint32 i, iterations;
	
	numhandles=item->handles[WAPI_HANDLE_MUTEX]->len;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": waiting on %d mutex handles for %d ms", numhandles,
		  item->timeout);
#endif

	/*
	 * See which ones we need to lock
	 */
	needed=g_ptr_array_new();
	for(i=0; i<numhandles; i++) {
		struct _WapiHandle_mutex *mutex_handle;
		
		mutex_handle=g_ptr_array_index(
			item->handles[WAPI_HANDLE_MUTEX], i);
		
		if(mutex_handle->tid!=tid) {
			/* We don't have this one, so add it to the list */
			g_ptr_array_add(needed, mutex_handle);
		}
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": need to lock %d mutex handles",
		  needed->len);
#endif
	
	iterations=0;
	do {
		iterations++;
		
		/* If the timeout isnt INFINITE but greater than 1s,
		 * split the timeout into 1s chunks
		 */
		if((item->timeout!=INFINITE) &&
		   (item->timeout < (iterations*1000))) {
			_wapi_calc_timeout(
				&timeout, item->timeout-((iterations-1)*1000));
		} else {
			_wapi_calc_timeout(&timeout, 1000);
		}
	
		/* Try and lock as many mutexes as we can until we run
		 * out of time, but to avoid deadlocks back off if we
		 * fail to lock one
		 */
		for(i=0; i<needed->len; i++) {
			struct _WapiHandle_mutex *mutex_handle;

			mutex_handle=g_ptr_array_index(needed, i);
		
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION
				  ": Locking %d mutex %p (owner %ld, me %ld)",
				  i, mutex_handle, mutex_handle->tid, tid);
#endif

			ret=mono_mutex_timedlock(&mutex_handle->mutex,
						 &timeout);

#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION ": timedlock ret %s",
				  strerror(ret));
#endif

			if(ret!=0) {
				/* ETIMEDOUT is the most likely, but
				 * fail on other error too
				 */

#ifdef DEBUG
				g_message(G_GNUC_PRETTY_FUNCTION
					  ": Lock %d mutex failed: %s", i,
					  strerror(ret));
#endif

				while(i--) {
#ifdef DEBUG
					g_message(G_GNUC_PRETTY_FUNCTION
						  ": Releasing %d mutex", i);
#endif
					mutex_handle=g_ptr_array_index(needed,
								       i);
					mono_mutex_unlock(&mutex_handle->mutex);
				}

				break;
			}

			/* OK, got that one. Don't record it as ours
			 * though until we get them all
			 */
		}

		if(i==needed->len) {
			/* We've locked all the mutexes.  Update the
			 * ones we already had, and record that the
			 * new ones belong to us
			 */
			for(i=0; i<numhandles; i++) {
				struct _WapiHandle_mutex *mutex_handle;
				guint32 idx;

				mutex_handle=g_ptr_array_index(
					item->handles[WAPI_HANDLE_MUTEX], i);
		
				idx=g_array_index(
					item->waitindex[WAPI_HANDLE_MUTEX],
					guint32, i);
				_wapi_handle_set_lowest(item, idx);
				
#ifdef DEBUG
				g_message(G_GNUC_PRETTY_FUNCTION
					  ": Updating mutex %p", mutex_handle);
#endif
				
				if(mutex_handle->tid==tid) {
					/* We already own this mutex,
					 * so just increase the count
					 */
					mutex_handle->recursion++;
				} else {
					mutex_handle->tid=tid;
					mutex_handle->recursion=1;
				}
			}
		
			g_ptr_array_free(needed, FALSE);

			item->waited[WAPI_HANDLE_MUTEX]=TRUE;
			item->waitcount[WAPI_HANDLE_MUTEX]=numhandles;
			
			return(numhandles);
		}
	} while((item->timeout==INFINITE) ||
		(item->timeout > (iterations * 1000)));

	/* Didn't get all the locks, and timeout isn't INFINITE */
		
	g_ptr_array_free(needed, FALSE);

	item->waited[WAPI_HANDLE_MUTEX]=TRUE;
	item->waitcount[WAPI_HANDLE_MUTEX]=0;
	
	return(0);
}

static void mutex_signal(WapiHandle *handle)
{
	ReleaseMutex(handle);
}

/**
 * CreateMutex:
 * @security: Ignored for now.
 * @owned: If %TRUE, the mutex is created with the calling thread
 * already owning the mutex.
 * @name:Pointer to a string specifying the name of this mutex, or
 * %NULL.  Currently ignored.
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
WapiHandle *CreateMutex(WapiSecurityAttributes *security G_GNUC_UNUSED, gboolean owned G_GNUC_UNUSED,
			const guchar *name G_GNUC_UNUSED)
{
	struct _WapiHandle_mutex *mutex_handle;
	WapiHandle *handle;
	
	mutex_handle=(struct _WapiHandle_mutex *)g_new0(struct _WapiHandle_mutex, 1);
	handle=(WapiHandle *)mutex_handle;
	_WAPI_HANDLE_INIT(handle, WAPI_HANDLE_MUTEX, mutex_ops);

	mono_mutex_init(&mutex_handle->mutex, NULL);
	if(owned==TRUE) {
		pthread_t tid=pthread_self();
		
		mono_mutex_lock(&mutex_handle->mutex);
		
		mutex_handle->tid=tid;
		mutex_handle->recursion=1;
	}
	
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
gboolean ReleaseMutex(WapiHandle *handle)
{
	struct _WapiHandle_mutex *mutex_handle=(struct _WapiHandle_mutex *)handle;
	pthread_t tid=pthread_self();
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Releasing mutex handle %p",
		  mutex_handle);
#endif

	if(mutex_handle->tid!=tid) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": We don't own mutex handle %p (owned by %ld, me %ld)", mutex_handle, mutex_handle->tid, tid);
#endif

		return(FALSE);
	}

	/* OK, we own this mutex */
	mutex_handle->recursion--;
	
	if(mutex_handle->recursion==0) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Unlocking mutex handle %p",
			  mutex_handle);
#endif

		mutex_handle->tid=0;
		mono_mutex_unlock(&mutex_handle->mutex);
	}
	
	return(TRUE);
}
