/*
 * wait.c:  wait for handles to become signalled
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <errno.h>

#include <mono/os/gc_wrapper.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/misc-private.h>

#undef DEBUG

/**
 * WaitForSingleObject:
 * @handle: an object to wait for
 * @timeout: the maximum time in milliseconds to wait for
 *
 * This function returns when either @handle is signalled, or @timeout
 * ms elapses.  If @timeout is zero, the object's state is tested and
 * the function returns immediately.  If @timeout is %INFINITE, the
 * function waits forever.
 *
 * Return value: %WAIT_ABANDONED - @handle is a mutex that was not
 * released by the owning thread when it exited.  Ownership of the
 * mutex object is granted to the calling thread and the mutex is set
 * to nonsignalled.  %WAIT_OBJECT_0 - The state of @handle is
 * signalled.  %WAIT_TIMEOUT - The @timeout interval elapsed and
 * @handle's state is still not signalled.  %WAIT_FAILED - an error
 * occurred.
 */
guint32 WaitForSingleObject(gpointer handle, guint32 timeout)
{
	guint32 ret, waited;
	struct timespec abstime;
	
	if(_wapi_handle_test_capabilities (handle,
					   WAPI_HANDLE_CAP_WAIT)==FALSE) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": handle %p can't be waited for", handle);
#endif

		return(WAIT_FAILED);
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": locking handle %p", handle);
#endif

	_wapi_handle_lock_handle (handle);

	if(_wapi_handle_test_capabilities (handle,
					   WAPI_HANDLE_CAP_OWN)==TRUE) {
		if(_wapi_handle_ops_isowned (handle)==TRUE) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": handle %p already owned", handle);
#endif
			_wapi_handle_ops_own (handle);
			ret=WAIT_OBJECT_0;
			goto done;
		}
	}
	
	if(_wapi_handle_issignalled (handle)) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": handle %p already signalled", handle);
#endif

		_wapi_handle_ops_own (handle);
		ret=WAIT_OBJECT_0;
		goto done;
	}

	/* Have to wait for it */
	if(timeout!=INFINITE) {
		_wapi_calc_timeout (&abstime, timeout);
	}
	
	do {
		if(timeout==INFINITE) {
			waited=_wapi_handle_wait_signal_handle (handle);
		} else {
			waited=_wapi_handle_timedwait_signal_handle (handle,
								     &abstime);
		}

		if(waited==0) {
			/* Condition was signalled, so hopefully
			 * handle is signalled now.  (It might not be
			 * if someone else got in before us.)
			 */
			if(_wapi_handle_issignalled (handle)) {
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION
					   ": handle %p signalled", handle);
#endif

				_wapi_handle_ops_own (handle);
				ret=WAIT_OBJECT_0;
				goto done;
			}
		
			/* Better luck next time */
		}
	} while(waited==0);

	/* Timeout or other error */
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": wait on handle %p error: %s",
		   handle, strerror (ret));
#endif

	ret=WAIT_TIMEOUT;
	
done:

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": unlocking handle %p", handle);
#endif
	
	_wapi_handle_unlock_handle (handle);
	return(ret);
}

/**
 * SignalObjectAndWait:
 * @signal_handle: An object to signal
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
guint32 SignalObjectAndWait(gpointer signal_handle, gpointer wait,
			    guint32 timeout, gboolean alertable)
{
	guint32 ret, waited;
	struct timespec abstime;
	
	if(_wapi_handle_test_capabilities (signal_handle,
					   WAPI_HANDLE_CAP_SIGNAL)==FALSE) {
		return(WAIT_FAILED);
	}
	
	if(_wapi_handle_test_capabilities (wait,
					   WAPI_HANDLE_CAP_WAIT)==FALSE) {
		return(WAIT_FAILED);
	}

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": locking handle %p", wait);
#endif

	_wapi_handle_lock_handle (wait);

	_wapi_handle_ops_signal (signal_handle);

	if(_wapi_handle_test_capabilities (wait, WAPI_HANDLE_CAP_OWN)==TRUE) {
		if(_wapi_handle_ops_isowned (wait)==TRUE) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": handle %p already owned", wait);
#endif
			_wapi_handle_ops_own (wait);
			ret=WAIT_OBJECT_0;
			goto done;
		}
	}
	
	if(_wapi_handle_issignalled (wait)) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": handle %p already signalled", wait);
#endif

		_wapi_handle_ops_own (wait);
		ret=WAIT_OBJECT_0;
		goto done;
	}

	/* Have to wait for it */
	if(timeout!=INFINITE) {
		_wapi_calc_timeout (&abstime, timeout);
	}
	
	do {
		if(timeout==INFINITE) {
			waited=_wapi_handle_wait_signal_handle (wait);
		} else {
			waited=_wapi_handle_timedwait_signal_handle (wait,
								     &abstime);
		}

		if(waited==0) {
			/* Condition was signalled, so hopefully
			 * handle is signalled now.  (It might not be
			 * if someone else got in before us.)
			 */
			if(_wapi_handle_issignalled (wait)) {
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION
					   ": handle %p signalled", wait);
#endif

				_wapi_handle_ops_own (wait);
				ret=WAIT_OBJECT_0;
				goto done;
			}
		
			/* Better luck next time */
		}
	} while(waited==0);

	/* Timeout or other error */
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": wait on handle %p error: %s",
		   wait, strerror (ret));
#endif

	ret=WAIT_TIMEOUT;
	
done:

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": unlocking handle %p", wait);
#endif

	_wapi_handle_unlock_handle (wait);

	if(alertable==TRUE) {
		/* Deal with queued APC or IO completion routines */
	}
	
	return(ret);
}

/**
 * WaitForMultipleObjects:
 * @numobjects: The number of objects in @handles. The maximum allowed
 * is %MAXIMUM_WAIT_OBJECTS.
 * @handles: An array of object handles.  Duplicates are not allowed.
 * @waitall: If %TRUE, this function waits until all of the handles
 * are signalled.  If %FALSE, this function returns when any object is
 * signalled.
 * @timeout: The maximum time in milliseconds to wait for.
 * 
 * This function returns when either one or more of @handles is
 * signalled, or @timeout ms elapses.  If @timeout is zero, the state
 * of each item of @handles is tested and the function returns
 * immediately.  If @timeout is %INFINITE, the function waits forever.
 *
 * Return value: %WAIT_OBJECT_0 to %WAIT_OBJECT_0 + @numobjects - 1 -
 * if @waitall is %TRUE, indicates that all objects are signalled.  If
 * @waitall is %FALSE, the return value minus %WAIT_OBJECT_0 indicates
 * the first index into @handles of the objects that are signalled.
 * %WAIT_ABANDONED_0 to %WAIT_ABANDONED_0 + @numobjects - 1 - if
 * @waitall is %TRUE, indicates that all objects are signalled, and at
 * least one object is an abandoned mutex object (See
 * WaitForSingleObject() for a description of abandoned mutexes.)  If
 * @waitall is %FALSE, the return value minus %WAIT_ABANDONED_0
 * indicates the first index into @handles of an abandoned mutex.
 * %WAIT_TIMEOUT - The @timeout interval elapsed and no objects in
 * @handles are signalled.  %WAIT_FAILED - an error occurred.
 */
guint32 WaitForMultipleObjects(guint32 numobjects, gpointer *handles,
			       gboolean waitall, guint32 timeout)
{
	GHashTable *dups;
	gboolean duplicate=FALSE, bogustype=FALSE, done;
	guint32 count, lowest;
	struct timespec abstime;
	guint i;
	guint32 ret;
	
	if(numobjects>MAXIMUM_WAIT_OBJECTS) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Too many handles: %d",
			  numobjects);
#endif

		return(WAIT_FAILED);
	}
	
	/* Check for duplicates */
	dups=g_hash_table_new(g_direct_hash, g_direct_equal);
	for(i=0; i<numobjects; i++) {
		gpointer exists=g_hash_table_lookup(dups, handles[i]);
		if(exists!=NULL) {
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION
				  ": Handle %p duplicated", handles[i]);
#endif

			duplicate=TRUE;
			break;
		}

		if(_wapi_handle_test_capabilities (handles[i], WAPI_HANDLE_CAP_WAIT)==FALSE) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Handle %p can't be waited for",
				   handles[i]);
#endif

			bogustype=TRUE;
		}

		g_hash_table_insert(dups, handles[i], handles[i]);
	}
	g_hash_table_destroy(dups);

	if(duplicate==TRUE) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Returning due to duplicates");
#endif

		return(WAIT_FAILED);
	}

	if(bogustype==TRUE) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Returning due to bogus type");
#endif

		return(WAIT_FAILED);
	}

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": locking handles");
#endif

	done=_wapi_handle_count_signalled_handles (numobjects, handles,
						   waitall, &count, &lowest);
	if(done==TRUE) {
		for(i=0; i<numobjects; i++) {
			_wapi_handle_ops_own (handles[i]);
		}
		
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": unlocking handles");
#endif

		_wapi_handle_unlock_handles (numobjects, handles);
		return(WAIT_OBJECT_0+lowest);
	}
	
	/* Have to wait for some or all handles to become signalled
	 */

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": unlocking handles");
#endif

	_wapi_handle_unlock_handles (numobjects, handles);

	if(timeout!=INFINITE) {
		_wapi_calc_timeout (&abstime, timeout);
	}

	while(1) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": locking signal mutex");
#endif

		_wapi_handle_lock_signal_mutex ();

		if(timeout==INFINITE) {
			ret=_wapi_handle_wait_signal ();
		} else {
			ret=_wapi_handle_timedwait_signal (&abstime);
		}

#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": unlocking signal mutex");
#endif

		_wapi_handle_unlock_signal_mutex ();
		
		if(ret==0) {
			/* Something was signalled ... */
			done=_wapi_handle_count_signalled_handles (numobjects,
								   handles,
								   waitall,
								   &count,
								   &lowest);
			if(done==TRUE) {
				for(i=0; i<numobjects; i++) {
					_wapi_handle_ops_own (handles[i]);
				}

#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION ": unlocking handles");
#endif

				_wapi_handle_unlock_handles (numobjects,
							     handles);

				return(WAIT_OBJECT_0+lowest);
			}

#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": unlocking handles");
#endif

			_wapi_handle_unlock_handles (numobjects, handles);
		} else {
			/* Timeout or other error */
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": wait returned error: %s", strerror (ret));
#endif

			if(ret==ETIMEDOUT) {
				return(WAIT_TIMEOUT);
			} else {
				return(WAIT_FAILED);
			}
		}
	}
}
