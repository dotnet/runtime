#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <string.h>

#include "mono/io-layer/wapi.h"
#include "wapi-private.h"
#include "wait-private.h"
#include "handles-private.h"
#include "misc-private.h"

#include "mono-mutex.h"

#undef DEBUG

/* event_wait() uses the event-private condition to signal that the
 * event has been set
 *
 * Hold mutex before setting the event, and before the final test
 * Hold rwlock for reading while testing the event
 * Hold rwlock for writing before resetting the event
 */
struct _WapiHandle_event
{
	WapiHandle handle;
	mono_mutex_t mutex;
	pthread_cond_t cond;
	pthread_rwlock_t rwlock;
	gboolean manual;
};

/* event_wait_multiple() uses the global condition to signal that an
 * event has been set
 */
static mono_mutex_t event_signal_mutex = MONO_MUTEX_INITIALIZER;
static pthread_cond_t event_signal_cond = PTHREAD_COND_INITIALIZER;

static void event_close(WapiHandle *handle);
static gboolean event_wait(WapiHandle *handle, WapiHandle *signal, guint32 ms);
static guint32 event_wait_multiple(gpointer data);
static void event_signal(WapiHandle *handle);

static struct _WapiHandleOps event_ops = {
	event_close,		/* close */
	NULL,			/* getfiletype */
	NULL,			/* readfile */
	NULL,			/* writefile */
	NULL,			/* flushfile */
	NULL,			/* seek */
	NULL,			/* setendoffile */
	NULL,			/* getfilesize */
	NULL,			/* getfiletime */
	NULL,			/* setfiletime */
	event_wait,		/* wait */
	event_wait_multiple,	/* wait_multiple */
	event_signal,		/* signal */
};

static void event_close(WapiHandle *handle)
{
	struct _WapiHandle_event *event_handle=(struct _WapiHandle_event *)handle;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": closing event handle %p",
		  event_handle);
#endif

	mono_mutex_destroy(&event_handle->mutex);
	pthread_cond_destroy(&event_handle->cond);
	pthread_rwlock_destroy(&event_handle->rwlock);
}

static gboolean event_wait(WapiHandle *handle, WapiHandle *signal, guint32 ms)
{
	struct _WapiHandle_event *event_handle=(struct _WapiHandle_event *)handle;
	struct timespec timeout;
	int ret;

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": waiting on event handle %p for %d ms", handle, ms);
#endif

	mono_mutex_lock(&event_handle->mutex);

	/* Signal this handle after we have obtained the event lock */
	if(signal!=NULL) {
		signal->ops->signal(signal);
	}
	
	/* First check if the handle is already signalled */
	if(handle->signalled==TRUE) {
		/* If this is an auto-reset event, reset the state to
		 * unsignalled
		 */
	
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": event handle %p already signalled", handle);
#endif

		if(event_handle->manual==FALSE) {
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION
				  ": resetting auto event handle %p", handle);
#endif
			handle->signalled=FALSE;
		}
		mono_mutex_unlock(&event_handle->mutex);
		
		return(TRUE);
	}

	/* We'll have to wait for it then */
	if(ms!=INFINITE) {
		_wapi_calc_timeout(&timeout, ms);
	}
	
again:
	/* Acquire a read lock so that the signal status can't be
	 * reset without us noticing. (PulseEvent and ResetEvent will
	 * gain a write lock before changing the status to
	 * unsignalled, which will block while one or more threads
	 * hold a read lock.)
	 */
	pthread_rwlock_rdlock(&event_handle->rwlock);
		
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": waiting for event handle %p to be signalled", handle);
#endif

	if(ms==INFINITE) {
		ret=mono_cond_wait(&event_handle->cond,
				   &event_handle->mutex);
	} else {
		ret=mono_cond_timedwait(&event_handle->cond,
					&event_handle->mutex, &timeout);
	}

	if(ret==0) {
		/* Condition was signalled, so hopefully event is
		 * signalled now.  (It might not be if its an
		 * auto-reset event and someone else got in before
		 * us.)
		 */
	
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": event handle %p signalled",
			  handle);
#endif
		if(handle->signalled==TRUE) {
	
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION
				  ": event handle %p still signalled", handle);
#endif
			/* If this is an auto-reset event, reset the
			 * state to unsignalled
			 */
			if(event_handle->manual==FALSE) {
#ifdef DEBUG
				g_message(G_GNUC_PRETTY_FUNCTION
					  ": resetting auto event handle %p",
					  handle);
#endif
				handle->signalled=FALSE;
			}
			pthread_rwlock_unlock(&event_handle->rwlock);
			mono_mutex_unlock(&event_handle->mutex);
			
			return(TRUE);
		}
	
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": event handle %p no longer signalled", handle);
#endif

		/* Better luck next time */
		
		/* Drop the rwlock briefly so that another thread has
		 * a chance to reset the event
		 */
		pthread_rwlock_unlock(&event_handle->rwlock);
		goto again;
	}

	/* Timeout or other error */
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": wait on event handle %p error: %s", handle,
		  strerror(ret));
#endif
	
	pthread_rwlock_unlock(&event_handle->rwlock);
	mono_mutex_unlock(&event_handle->mutex);
	
	return(FALSE);
}

static gboolean event_count_signalled(WaitQueueItem *item, guint32 numhandles,
				      gboolean waitall, guint32 *retcount)
{
	guint32 count, i;
	gboolean ret;
	
	/* Lock all the handles, with backoff */
again:
	for(i=0; i<numhandles; i++) {
		struct _WapiHandle_event *event_handle;
		
		event_handle=g_ptr_array_index(
			item->handles[WAPI_HANDLE_EVENT], i);
		
		ret=mono_mutex_trylock(&event_handle->mutex);
		if(ret!=0) {
			/* Bummer */
			while(i--) {
				event_handle=g_ptr_array_index(
					item->handles[WAPI_HANDLE_EVENT], i);
				mono_mutex_unlock(&event_handle->mutex);
			}

			/* It's not possible for two threads calling
			 * WaitForMultipleObjects to both be calling
			 * this function simultaneously, because the
			 * global event_signal_mutex is held.
			 * Therefore any collision is with a single
			 * lock from one of the functions that deal
			 * with single event handles.  It's just about
			 * theoretically possible for the other
			 * threads to keep locking an event in a tight
			 * loop but eventually we will get the lock.
			 */
			sched_yield();
			
			goto again;
		}
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Locked all event handles");
#endif
	
	count=_wapi_handle_count_signalled(item, WAPI_HANDLE_EVENT);
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": %d event handles signalled",
		  count);
#endif
	
	if((waitall==TRUE && count==numhandles) ||
	   (waitall==FALSE && count>0)) {
		/* done */
		ret=TRUE;
	} else {
		ret=FALSE;
	}
	
	for(i=0; i<numhandles; i++) {
		struct _WapiHandle_event *event_handle;
		
		event_handle=g_ptr_array_index(
			item->handles[WAPI_HANDLE_EVENT], i);
		
		mono_mutex_unlock(&event_handle->mutex);
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Returning %d", ret);
#endif

	*retcount=count;
	return(ret);
}

static guint32 event_wait_multiple(gpointer data)
{
	WaitQueueItem *item=(WaitQueueItem *)data;
	struct timespec timeout;
	guint32 iterations;
	guint32 numhandles, count, i;
	gboolean done;
	int ret;

	numhandles=item->handles[WAPI_HANDLE_EVENT]->len;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": waiting on %d event handles for %d ms", numhandles,
		  item->timeout);
#endif

	/* First, check if any of the handles are already
	 * signalled. If waitall is specified we only return if all
	 * handles have been signalled.
	 */
	done=event_count_signalled(item, numhandles, item->waitall, &count);
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Preliminary check found %d handles signalled", count);
#endif

	if(done==TRUE) {
		item->waited[WAPI_HANDLE_EVENT]=TRUE;
		item->waitcount[WAPI_HANDLE_EVENT]=count;
		
		return(count);
	}
	
	/* We'll have to wait then */
	
	mono_mutex_lock(&event_signal_mutex);
	
	iterations=0;
	do {
		iterations++;
		
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Wait iteration %d",
			  iterations);
#endif

		/* If the timeout isnt INFINITE but greater than 1s,
		 * split the timeout into 1s chunks.
		 *
		 * This is so that ResetEvent() wont block forever if
		 * another thread is waiting on multiple events, with
		 * some already signalled, and ResetEvent() wants to
		 * reset one of the signalled ones.  (1s is a bit of a
		 * long wait too, this might need to be tuned.)
		 */

		if((item->timeout!=INFINITE) &&
		   (item->timeout < (iterations*1000))) {
			_wapi_calc_timeout(
				&timeout, item->timeout-((iterations-1)*1000));
		} else {
			_wapi_calc_timeout(&timeout, 1000);
		}
		
		/* Acquire a read lock on all handles so that the
		 * signal status can't be reset without us
		 * noticing. (PulseEvent and ResetEvent will gain a
		 * write lock before changing the status to
		 * unsignalled, which will block while one or more
		 * threads hold a read lock.)
		 */
		for(i=0; i<numhandles; i++) {
			struct _WapiHandle_event *event_handle;
			
			event_handle=g_ptr_array_index(
				item->handles[WAPI_HANDLE_EVENT], i);
			
			pthread_rwlock_rdlock(&event_handle->rwlock);
		}
		
		ret=mono_cond_timedwait(&event_signal_cond,
					   &event_signal_mutex, &timeout);

		if(ret==0) {
			/* Condition was signalled, so hopefully an
			 * event is signalled now.  (It might not be
			 * if it was an auto-reset event and someone
			 * else got in before us.)
			 */
			done=event_count_signalled(item, numhandles,
						   item->waitall, &count);
	
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION
				  ": signal check found %d handles signalled",
				  count);
#endif
			
			if(done==TRUE) {
#ifdef DEBUG
				g_message(G_GNUC_PRETTY_FUNCTION
					  ": Returning wait success");
#endif

				for(i=0; i<numhandles; i++) {
					struct _WapiHandle_event *event_handle;
				
					event_handle=g_ptr_array_index(item->handles[WAPI_HANDLE_EVENT], i);
			
					pthread_rwlock_unlock(&event_handle->rwlock);
				}

				item->waited[WAPI_HANDLE_EVENT]=TRUE;
				item->waitcount[WAPI_HANDLE_EVENT]=count;
				
				return(count);
			}
		} else {
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION ": Wait error %s",
				  strerror(ret));
#endif
		}

#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Still waiting for more event handles");
#endif
		/* Drop the rwlocks briefly so that another thread has
		 * a chance to reset any of the events
		 */
		for(i=0; i<numhandles; i++) {
			struct _WapiHandle_event *event_handle;
				
			event_handle=g_ptr_array_index(
				item->handles[WAPI_HANDLE_EVENT], i);
			
			pthread_rwlock_unlock(&event_handle->rwlock);
		}
	} while((item->timeout==INFINITE) ||
		(item->timeout > (iterations * 1000)));

	/* Timeout or other error */
	
	for(i=0; i<numhandles; i++) {
		struct _WapiHandle_event *event_handle;
		
		event_handle=g_ptr_array_index(
			item->handles[WAPI_HANDLE_EVENT], i);
		
		pthread_rwlock_unlock(&event_handle->rwlock);
	}

	mono_mutex_unlock(&event_signal_mutex);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Returning wait failed");
#endif
	
	item->waited[WAPI_HANDLE_EVENT]=TRUE;
	item->waitcount[WAPI_HANDLE_MUTEX]=0;
	
	return(0);
}

static void event_signal(WapiHandle *handle)
{
	ResetEvent(handle);
}

/**
 * CreateEvent:
 * @security: Ignored for now.
 * @manual: Specifies whether the new event handle has manual or auto
 * reset behaviour.
 * @initial: Specifies whether the new event handle is initially
 * signalled or not.
 * @name:Pointer to a string specifying the name of this name, or
 * %NULL.  Currently ignored.
 *
 * Creates a new event handle.
 *
 * An event handle is signalled with SetEvent().  If the new handle is
 * a manual reset event handle, it remains signalled until it is reset
 * with ResetEvent().  An auto reset event remains signalled until a
 * single thread has waited for it, at which time the event handle is
 * automatically reset to unsignalled.
 *
 * Return value: A new handle, or %NULL on error.
 */
WapiHandle *CreateEvent(WapiSecurityAttributes *security G_GNUC_UNUSED, gboolean manual,
			gboolean initial, const guchar *name G_GNUC_UNUSED)
{
	struct _WapiHandle_event *event_handle;
	WapiHandle *handle;
	
	event_handle=(struct _WapiHandle_event *)g_new0(struct _WapiHandle_event, 1);
	handle=(WapiHandle *)event_handle;
	_WAPI_HANDLE_INIT(handle, WAPI_HANDLE_EVENT, event_ops);
	
	mono_mutex_init(&event_handle->mutex, NULL);
	pthread_cond_init(&event_handle->cond, NULL);
	pthread_rwlock_init(&event_handle->rwlock, NULL);
	event_handle->manual=manual;

	if(initial==TRUE) {
		handle->signalled=TRUE;
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": created new event handle %p",
		  handle);
#endif

	return(handle);
}

/**
 * PulseEvent:
 * @handle: The event handle.
 *
 * Sets the event handle @handle to the signalled state, and then
 * resets it to unsignalled after informing any waiting threads.
 *
 * If @handle is a manual reset event, all waiting threads that can be
 * released immediately are released.  @handle is then reset.  If
 * @handle is an auto reset event, one waiting thread is released even
 * if multiple threads are waiting.
 *
 * Return value: %TRUE on success, %FALSE otherwise.  (Currently only
 * ever returns %TRUE).
 */
gboolean PulseEvent(WapiHandle *handle)
{
	struct _WapiHandle_event *event_handle=(struct _WapiHandle_event *)handle;
	
	mono_mutex_lock(&event_handle->mutex);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Pulsing event handle %p", handle);
#endif

	handle->signalled=TRUE;
	
	/* Tell everyone blocking on WaitForSingleObject */
	if(event_handle->manual==TRUE) {
		pthread_cond_broadcast(&event_handle->cond);
	} else {
		pthread_cond_signal(&event_handle->cond);
	}
	mono_mutex_unlock(&event_handle->mutex);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Informed single waits for event handle %p", handle);
#endif
	
	/* Tell everyone blocking on WaitForMultipleObjects */
	mono_mutex_lock(&event_signal_mutex);
	pthread_cond_broadcast(&event_signal_cond);
	mono_mutex_unlock(&event_signal_mutex);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Informed multiple waits for event handles");
#endif
	
	/* Reset the handle signal state */

	/* This rwlock blocks until no other thread holds a read lock.
	 * This ensures that we can't reset the event until every
	 * waiting thread has had a chance to examine it
	 */
	pthread_rwlock_wrlock(&event_handle->rwlock);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Obtained write lock on event handle %p", handle);
#endif

	handle->signalled=FALSE;
	pthread_rwlock_unlock(&event_handle->rwlock);

	return(TRUE);
}

/**
 * ResetEvent:
 * @handle: The event handle.
 *
 * Resets the event handle @handle to the unsignalled state.
 *
 * Return value: %TRUE on success, %FALSE otherwise.  (Currently only
 * ever returns %TRUE).
 */
gboolean ResetEvent(WapiHandle *handle)
{
	struct _WapiHandle_event *event_handle=(struct _WapiHandle_event *)handle;

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Resetting event handle %p",
		  handle);
#endif

	/* Test for the current state, because another thread might be
	 * waiting forever on an unsignalled event with the read lock
	 * held.  Theres no point going for the write lock if we dont
	 * need it.
	 */
	mono_mutex_lock(&event_handle->mutex);
	if(handle->signalled==FALSE) {

#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": No need to reset event handle %p", handle);
#endif

		mono_mutex_unlock(&event_handle->mutex);
		return(TRUE);
	}
	mono_mutex_unlock(&event_handle->mutex);
	
	pthread_rwlock_wrlock(&event_handle->rwlock);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Obtained write lock on event handle %p", handle);
#endif

	handle->signalled=FALSE;
	pthread_rwlock_unlock(&event_handle->rwlock);
	
	return(TRUE);
}

/**
 * SetEvent:
 * @handle: The event handle
 *
 * Sets the event handle @handle to the signalled state.
 *
 * If @handle is a manual reset event, it remains signalled until it
 * is reset with ResetEvent().  An auto reset event remains signalled
 * until a single thread has waited for it, at which time @handle is
 * automatically reset to unsignalled.
 *
 * Return value: %TRUE on success, %FALSE otherwise.  (Currently only
 * ever returns %TRUE).
 */
gboolean SetEvent(WapiHandle *handle)
{
	struct _WapiHandle_event *event_handle=(struct _WapiHandle_event *)handle;
	
	mono_mutex_lock(&event_handle->mutex);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Setting event handle %p", handle);
#endif

	handle->signalled=TRUE;
	
	/* Tell everyone blocking on WaitForSingleObject */
	if(event_handle->manual==TRUE) {
		pthread_cond_broadcast(&event_handle->cond);
	} else {
		pthread_cond_signal(&event_handle->cond);
	}
	mono_mutex_unlock(&event_handle->mutex);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Informed single waits for event handle %p", handle);
#endif
	
	/* Tell everyone blocking on WaitForMultipleObjects */
	mono_mutex_lock(&event_signal_mutex);
	pthread_cond_broadcast(&event_signal_cond);
	mono_mutex_unlock(&event_signal_mutex);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Informed multiple waits for event handles");
#endif
	
	return(TRUE);
}

