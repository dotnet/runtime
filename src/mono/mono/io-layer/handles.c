#include <config.h>
#include <glib.h>
#include <pthread.h>

#include "mono/io-layer/wapi.h"
#include "wapi-private.h"
#include "handles-private.h"

#include "mono-mutex.h"

#undef DEBUG

guint32 _wapi_handle_count_signalled(WaitQueueItem *item, WapiHandleType type)
{
	guint32 i, ret=0;
	
	/* Count how many of the interesting thread handles are signalled */
	for(i=0; i<item->handles[type]->len; i++) {
		WapiHandle *handle;

		handle=(WapiHandle *)g_ptr_array_index(item->handles[type], i);
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Checking handle %p",
			  handle);
#endif
		
		if(handle->signalled==TRUE) {
			guint32 idx;
			
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION
				  ": Handle %p signalled", handle);
#endif

			idx=g_array_index(item->waitindex[type], guint32, i);
			_wapi_handle_set_lowest(item, idx);
			
			ret++;
		}
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": %d signalled handles", ret);
#endif

	return(ret);
}

void _wapi_handle_set_lowest(WaitQueueItem *item, guint32 idx)
{
	mono_mutex_lock(&item->mutex);

	if(item->lowest_signal>idx) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Set %p lowest index to %d",
			  item, idx);
#endif
	
		item->lowest_signal=idx;
	}
	
	mono_mutex_unlock(&item->mutex);
}

/**
 * CloseHandle:
 * @handle: The handle to release
 *
 * Closes and invalidates @handle, releasing any resources it
 * consumes.  When the last handle to a temporary or non-persistent
 * object is closed, that object can be deleted.  Closing the same
 * handle twice is an error.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean CloseHandle(WapiHandle *handle)
{
	g_return_val_if_fail(handle->ref>0, FALSE);
	
	handle->ref--;
	if(handle->ref==0) {
		if(handle->ops->close!=NULL) {
			handle->ops->close(handle);
		}
		
		g_free(handle);		/* maybe this should be in
					 * ops, cuurently ops->close()
					 * is being used to free
					 * handle data
					 */
	}
	
	return(TRUE);
}

/**
 * SignalObjectAndWait:
 * @signal: An object to signal
 * @wait: An object to wait for
 * @timeout: The maximum time in milliseconds to wait for
 * @alertable: Specifies whether the function returnes when the system
 * queues an I/O completion routine or an APC for the calling thread.
 *
 * Atomically signals @signal and waits for @wait to become signalled,
 * or @timeout ms elapses.  If @timeout is zero, the object's state is
 * tested and the function returns immediately.  If @timeout is
 * %INFINITE, the function waits forever.
 *
 * @signal can be a semaphore, mutex or event object.
 *
 * If @alertable is %TRUE and the system queues an I/O completion
 * routine or an APC for the calling thread, the function returns and
 * the thread calls the completion routine or APC function.  If
 * %FALSE, the function does not return, and the thread does not call
 * the completion routine or APC function.  A completion routine is
 * queued when the ReadFileEx() or WriteFileEx() function in which it
 * was specified has completed.  The calling thread is the thread that
 * initiated the read or write operation.  An APC is queued when
 * QueueUserAPC() is called.  Currently completion routines and APC
 * functions are not supported.
 *
 * Return value: %WAIT_ABANDONED - @wait is a mutex that was not
 * released by the owning thread when it exited.  Ownershop of the
 * mutex object is granted to the calling thread and the mutex is set
 * to nonsignalled.  %WAIT_IO_COMPLETION - the wait was ended by one
 * or more user-mode asynchronous procedure calls queued to the
 * thread.  %WAIT_OBJECT_0 - The state of @wait is signalled.
 * %WAIT_TIMEOUT - The @timeout interval elapsed and @wait's state is
 * still not signalled.  %WAIT_FAILED - an error occurred.
 */
guint32 SignalObjectAndWait(WapiHandle *signal, WapiHandle *wait,
			    guint32 timeout, gboolean alertable)
{
	gboolean waited;
	guint32 ret;
	
	if(signal->ops->signal==NULL) {
		return(WAIT_FAILED);
	}
	
	if(wait->ops->wait==NULL) {
		return(WAIT_FAILED);
	}

	waited=wait->ops->wait(wait, signal, timeout);
	if(waited==TRUE) {
		/* Object signalled before timeout expired */
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Object %p signalled",
			  wait);
#endif
		ret=WAIT_OBJECT_0;
	} else {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Object %p wait timed out",
			  wait);
#endif
		ret=WAIT_TIMEOUT;
	}

	if(alertable==TRUE) {
		/* Deal with queued APC or IO completion routines */
	}
	
	return(ret);
}
