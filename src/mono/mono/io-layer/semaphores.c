#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <semaphore.h>
#include <errno.h>
#include <string.h>
#include <sys/time.h>

#include "mono/io-layer/wapi.h"
#include "wapi-private.h"
#include "wait-private.h"
#include "misc-private.h"
#include "handles-private.h"

#include "mono-mutex.h"

#undef DEBUG

/* emulate sem_t, so that we can prod the internal state more easily */
struct _WapiHandle_sem
{
	WapiHandle handle;
	guint32 val;
	gint32 max;
};

/* This mutex controls access to _all_ semaphores and should not be
 * locked for long periods.
 *
 * This global mutex and cond is really for wait_multiple, so we dont
 * have to try and lock multiple handle mutexes and conditions.
 */
static mono_mutex_t sem_mutex=MONO_MUTEX_INITIALIZER;
static pthread_cond_t sem_cond=PTHREAD_COND_INITIALIZER;

static void sema_close(WapiHandle *handle);
static gboolean sema_wait(WapiHandle *handle, WapiHandle *signal, guint32 ms);
static guint32 sema_wait_multiple(gpointer data);
static void sema_signal(WapiHandle *handle);

static struct _WapiHandleOps sem_ops = {
	sema_close,		/* close */
	NULL,			/* getfiletype */
	NULL,			/* readfile */
	NULL,			/* writefile */
	NULL,			/* flushfile */
	NULL,			/* seek */
	NULL,			/* setendoffile */
	NULL,			/* getfilesize */
	NULL,			/* getfiletime */
	NULL,			/* setfiletime */
	sema_wait,		/* wait */
	sema_wait_multiple,	/* wait_multiple */
	sema_signal,		/* signal */
};

static void sema_close(WapiHandle *handle G_GNUC_UNUSED)
{
	/* Not really much to do here */
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": closing sem handle %p", handle);
#endif
}

static gboolean sema_wait(WapiHandle *handle, WapiHandle *signal, guint32 ms)
{
	struct _WapiHandle_sem *sem_handle=(struct _WapiHandle_sem *)handle;
	gboolean waited;
	int ret;
	
	mono_mutex_lock(&sem_mutex);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Sem %p val %d ms %d", handle,
		  sem_handle->val, ms);
#endif
	
	/* Signal this handle after we have obtained the semaphore
	 * global lock
	 */
	if(signal!=NULL) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": signalling %p", signal);
#endif
		signal->ops->signal(signal);
	}
	
	/* Shortcut when ms==0 */
	if(ms==0) {
		/* Just poll */
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Polling");
#endif
		if(sem_handle->val>0) {
			waited=TRUE;
		} else {
			waited=FALSE;
		}
		goto end;
	}
	
	/* Check state first */
	if(sem_handle->val>0) {
		waited=TRUE;
		goto end;
	}
	
	if(ms==INFINITE) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": wait for %p INFINITE",
			  sem_handle);
#endif
	try_again_infinite:
		ret=mono_cond_wait(&sem_cond, &sem_mutex);
		if(ret==0) {
			/* See if we were signalled (it might have been
			 * another semaphore)
			 */
			if(sem_handle->val>0) {
#ifdef DEBUG
				g_message(G_GNUC_PRETTY_FUNCTION
					  ": sem %p has been signalled",
					  sem_handle);
#endif
			 	waited=TRUE;
			} else {
#ifdef DEBUG
				g_message(G_GNUC_PRETTY_FUNCTION
					  ": sem %p not signalled",
					  sem_handle);
#endif
				goto try_again_infinite;
			}
		}
	} else {
		struct timespec timeout;

#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": wait for %p for %d ms",
			  sem_handle, ms);
#endif

		_wapi_calc_timeout(&timeout, ms);
		
	try_again_timed:
		ret=mono_cond_timedwait(&sem_cond, &sem_mutex, &timeout);
		if(ret==0) {
			/* See if we were signalled (it might have been
			 * another semaphore)
			 */
			if(sem_handle->val>0) {
#ifdef DEBUG
				g_message(G_GNUC_PRETTY_FUNCTION
					  ": sem %p has been signalled",
					  sem_handle);
#endif
				waited=TRUE;
			} else {
#ifdef DEBUG
				g_message(G_GNUC_PRETTY_FUNCTION
					  ": sem %p not signalled",
					  sem_handle);
#endif
				goto try_again_timed;
			}
		} else {
			/* ret might be ETIMEDOUT for timeout, or
			 * other for error */
			waited=FALSE;
		}
	}
	
end:
	if(waited==TRUE) {
		sem_handle->val--;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Waited TRUE, sem %p val now %d", sem_handle,
			  sem_handle->val);
#endif
	}
#ifdef DEBUG
	else {
		g_message(G_GNUC_PRETTY_FUNCTION ": Waited FALSE, sem %p",
			  sem_handle);
	}
#endif
	
	mono_mutex_unlock(&sem_mutex);
	return(waited);
}

static guint32 sema_wait_multiple(gpointer data G_GNUC_UNUSED)
{
	WaitQueueItem *item=(WaitQueueItem *)data;
	guint32 numhandles, count;
	struct timespec timeout;
	guint32 i;
	int ret;
	
	numhandles=item->handles[WAPI_HANDLE_SEM]->len;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": waiting on %d sem handles for %d ms", numhandles,
		  item->timeout);
#endif

	mono_mutex_lock(&sem_mutex);
	
	/* First, check if any of the handles are already signalled.
	 * If waitall is specified we only return if all handles have
	 * been signalled.
	 */
	for(count=0, i=0; i<numhandles; i++) {
		struct _WapiHandle_sem *sem_handle;
		
		sem_handle=g_ptr_array_index(item->handles[WAPI_HANDLE_SEM],
					     i);
		if(sem_handle->val>0) {
			count++;
		}
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Preliminary check found %d handles signalled", count);
#endif

	if((item->waitall==TRUE && count==numhandles) ||
	   (item->waitall==FALSE && count>0)) {
		goto success;
	}
	
	/* OK, we need to wait for some */
	if(item->timeout!=INFINITE) {
		_wapi_calc_timeout(&timeout, item->timeout);
	}
	
	/* We can restart from here without resetting the timeout,
	 * because it is calculated from absolute time, not an offset.
	 */
again:
	if(item->timeout==INFINITE) {
		ret=mono_cond_wait(&sem_cond, &sem_mutex);
	} else {
		ret=mono_cond_timedwait(&sem_cond, &sem_mutex, &timeout);
	}
	
	if(ret==ETIMEDOUT) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Wait timed out");
#endif

		goto success;
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Sem posted, checking status");
#endif

	/* A semaphore was posted, so see if it was one we are
	 * interested in
	 */
	for(count=0, i=0; i<numhandles; i++) {
		struct _WapiHandle_sem *sem_handle;
		
		sem_handle=g_ptr_array_index(item->handles[WAPI_HANDLE_SEM],
					     i);
		if(sem_handle->val>0) {
			count++;
		}
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Check after sem post found %d handles signalled", count);
#endif

	if((item->waitall==TRUE && count==numhandles) ||
	   (item->waitall==FALSE && count>0)) {
		goto success;
	}
	
	/* Either we have waitall set with more handles to wait for, or
	 * the sem that was posted wasn't interesting to us
	 */
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Waiting a bit longer");
#endif

	goto again;
	
success:
	item->waited[WAPI_HANDLE_SEM]=TRUE;
	item->waitcount[WAPI_HANDLE_SEM]=count;

	if((item->waitall==TRUE && count==numhandles) ||
	   (item->waitall==FALSE && count>0)) {
		/* Decrease all waited semaphores */
		for(i=0; i<numhandles; i++) {
			struct _WapiHandle_sem *sem_handle;
			guint32 idx;
			
			sem_handle=g_ptr_array_index(
				item->handles[WAPI_HANDLE_SEM], i);

			idx=g_array_index(item->waitindex[WAPI_HANDLE_SEM],
					  guint32, i);
			_wapi_handle_set_lowest(item, idx);
			
			if(sem_handle->val>0) {
				sem_handle->val--;
			}
		}
	}

	mono_mutex_unlock(&sem_mutex);

	return(count);
}

static void sema_signal(WapiHandle *handle)
{
	ReleaseSemaphore(handle, 1, NULL);
}

/**
 * CreateSemaphore:
 * @security: Ignored for now.
 * @initial: The initial count for the semaphore.  The value must be
 * greater than or equal to zero, and less than or equal to @max.
 * @max: The maximum count for this semaphore.  The value must be
 * greater than zero.
 * @name: Pointer to a string specifying the name of this semaphore,
 * or %NULL.  Currently ignored.
 *
 * Creates a new semaphore handle.  A semaphore is signalled when its
 * count is greater than zero, and unsignalled otherwise.  The count
 * is decreased by one whenever a wait function releases a thread that
 * was waiting for the semaphore.  The count is increased by calling
 * ReleaseSemaphore().
 *
 * Return value: a new handle, or NULL
 */
WapiHandle *CreateSemaphore(WapiSecurityAttributes *security G_GNUC_UNUSED, gint32 initial, gint32 max, const guchar *name G_GNUC_UNUSED)
{
	struct _WapiHandle_sem *sem_handle;
	WapiHandle *handle;
	
	if(max<=0) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": max <= 0");
#endif

		return(NULL);
	}
	
	if(initial>max || initial<0) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": initial>max or < 0");
#endif

		return(NULL);
	}
	
	sem_handle=(struct _WapiHandle_sem *)g_new0(struct _WapiHandle_sem, 1);
	handle=(WapiHandle *)sem_handle;
	_WAPI_HANDLE_INIT(handle, WAPI_HANDLE_SEM, sem_ops);

	sem_handle->val=initial;
	sem_handle->max=max;

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Created semaphore handle %p",
		  handle);
#endif
	
	return(handle);
}

/**
 * ReleaseSemaphore:
 * @handle: The semaphore handle to release.
 * @count: The amount by which the semaphore's count should be
 * increased.
 * @prevcount: Pointer to a location to store the previous count of
 * the semaphore, or %NULL.
 *
 * Increases the count of semaphore @handle by @count.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean ReleaseSemaphore(WapiHandle *handle, gint32 count, gint32 *prevcount)
{
	struct _WapiHandle_sem *sem_handle=(struct _WapiHandle_sem *)handle;
	gboolean ret=FALSE;
	
	
	mono_mutex_lock(&sem_mutex);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": sem %p val %d count %d",
		  sem_handle, sem_handle->val, count);
#endif
	
	/* Do this before checking for count overflow, because overflowing max
	 * is a listed technique for finding the current value
	 */
	if(prevcount!=NULL) {
		*prevcount=sem_handle->val;
	}
	
	/* No idea why max is signed, but thats the spec :-( */
	if(sem_handle->val+count > (guint32)sem_handle->max) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": sem %p max value would be exceeded: max %d current %d count %d",
			  handle, sem_handle->max, sem_handle->val, count);
#endif

		goto end;
	}
	
	sem_handle->val+=count;
	pthread_cond_broadcast(&sem_cond);
	ret=TRUE;

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": sem %p val now %d", sem_handle,
		  sem_handle->val);
#endif
	
end:
	mono_mutex_unlock(&sem_mutex);
	return(ret);
}
