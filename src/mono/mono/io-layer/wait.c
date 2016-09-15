/*
 * wait.c:  wait for handles to become signalled
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Novell, Inc.
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <errno.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/utils/w32handle.h>

/**
 * WaitForSingleObjectEx:
 * @handle: an object to wait for
 * @timeout: the maximum time in milliseconds to wait for
 * @alertable: if TRUE, the wait can be interrupted by an APC call
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
 * occurred. %WAIT_IO_COMPLETION - the wait was ended by an APC.
 */
guint32 WaitForSingleObjectEx(gpointer handle, guint32 timeout, gboolean alertable)
{
	MonoW32HandleWaitRet ret;

	ret = mono_w32handle_wait_one (handle, timeout, alertable);
	if (ret == MONO_W32HANDLE_WAIT_RET_SUCCESS_0)
		return WAIT_OBJECT_0;
	else if (ret == MONO_W32HANDLE_WAIT_RET_ABANDONED_0)
		return WAIT_ABANDONED_0;
	else if (ret == MONO_W32HANDLE_WAIT_RET_ALERTED)
		return WAIT_IO_COMPLETION;
	else if (ret == MONO_W32HANDLE_WAIT_RET_TIMEOUT)
		return WAIT_TIMEOUT;
	else if (ret == MONO_W32HANDLE_WAIT_RET_FAILED)
		return WAIT_FAILED;
	else
		g_error ("%s: unknown ret value %d", __func__, ret);
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
	MonoW32HandleWaitRet ret;

	ret = mono_w32handle_signal_and_wait (signal_handle, wait, timeout, alertable);
	if (ret == MONO_W32HANDLE_WAIT_RET_SUCCESS_0)
		return WAIT_OBJECT_0;
	else if (ret == MONO_W32HANDLE_WAIT_RET_ABANDONED_0)
		return WAIT_ABANDONED_0;
	else if (ret == MONO_W32HANDLE_WAIT_RET_ALERTED)
		return WAIT_IO_COMPLETION;
	else if (ret == MONO_W32HANDLE_WAIT_RET_TIMEOUT)
		return WAIT_TIMEOUT;
	else if (ret == MONO_W32HANDLE_WAIT_RET_FAILED)
		return WAIT_FAILED;
	else
		g_error ("%s: unknown ret value %d", __func__, ret);
}

/**
 * WaitForMultipleObjectsEx:
 * @numobjects: The number of objects in @handles. The maximum allowed
 * is %MAXIMUM_WAIT_OBJECTS.
 * @handles: An array of object handles.  Duplicates are not allowed.
 * @waitall: If %TRUE, this function waits until all of the handles
 * are signalled.  If %FALSE, this function returns when any object is
 * signalled.
 * @timeout: The maximum time in milliseconds to wait for.
 * @alertable: if TRUE, the wait can be interrupted by an APC call
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
 * %WAIT_IO_COMPLETION - the wait was ended by an APC.
 */
guint32 WaitForMultipleObjectsEx(guint32 numobjects, gpointer *handles,
				 gboolean waitall, guint32 timeout,
				 gboolean alertable)
{
	MonoW32HandleWaitRet ret;

	ret = mono_w32handle_wait_multiple (handles, numobjects, waitall, timeout, alertable);
	if (ret >= MONO_W32HANDLE_WAIT_RET_SUCCESS_0 && ret <= MONO_W32HANDLE_WAIT_RET_SUCCESS_0 + numobjects - 1)
		return WAIT_OBJECT_0 + (ret - MONO_W32HANDLE_WAIT_RET_SUCCESS_0);
	else if (ret >= MONO_W32HANDLE_WAIT_RET_ABANDONED_0 && ret <= MONO_W32HANDLE_WAIT_RET_ABANDONED_0 + numobjects - 1)
		return WAIT_ABANDONED_0 + (ret - MONO_W32HANDLE_WAIT_RET_ABANDONED_0);
	else if (ret == MONO_W32HANDLE_WAIT_RET_ALERTED)
		return WAIT_IO_COMPLETION;
	else if (ret == MONO_W32HANDLE_WAIT_RET_TIMEOUT)
		return WAIT_TIMEOUT;
	else if (ret == MONO_W32HANDLE_WAIT_RET_FAILED)
		return WAIT_FAILED;
	else
		g_error ("%s: unknown ret value %d", __func__, ret);
}

