/*
 * semaphores.c:  Semaphore handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <semaphore.h>
#include <errno.h>
#include <string.h>
#include <sys/time.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/semaphore-private.h>

#undef DEBUG

static void sema_close_shared (gpointer handle);
static void sema_signal(gpointer handle);
static void sema_own (gpointer handle);

struct _WapiHandleOps _wapi_sem_ops = {
	sema_close_shared,	/* close_shared */
	NULL,			/* close_private */
	sema_signal,		/* signal */
	sema_own,		/* own */
	NULL,			/* is_owned */
};

static mono_once_t sem_ops_once=MONO_ONCE_INIT;

static void sem_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_SEM,
					    WAPI_HANDLE_CAP_WAIT |
					    WAPI_HANDLE_CAP_SIGNAL);
}

static void sema_close_shared (gpointer handle G_GNUC_UNUSED)
{
	/* Not really much to do here */
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": closing sem handle %p", handle);
#endif
}

static void sema_signal(gpointer handle)
{
	ReleaseSemaphore(handle, 1, NULL);
}

static void sema_own (gpointer handle)
{
	struct _WapiHandle_sem *sem_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_SEM,
				(gpointer *)&sem_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up sem handle %p", handle);
		return;
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": owning sem handle %p", handle);
#endif

	sem_handle->val--;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": sem %p val now %d", handle,
		   sem_handle->val);
#endif

	if(sem_handle->val==0) {
		_wapi_handle_set_signal_state (handle, FALSE, FALSE);
	}
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
gpointer CreateSemaphore(WapiSecurityAttributes *security G_GNUC_UNUSED, gint32 initial, gint32 max, const guchar *name G_GNUC_UNUSED)
{
	struct _WapiHandle_sem *sem_handle;
	gpointer handle;
	gboolean ok;
	
	mono_once (&sem_ops_once, sem_ops_init);
	
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
	
	handle=_wapi_handle_new (WAPI_HANDLE_SEM);
	if(handle==_WAPI_HANDLE_INVALID) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error creating semaphore handle");
		return(NULL);
	}

	_wapi_handle_lock_handle (handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_SEM,
				(gpointer *)&sem_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error lookup up semaphore handle %p", handle);
		_wapi_handle_unlock_handle (handle);
		return(NULL);
	}

	sem_handle->val=initial;
	sem_handle->max=max;

	if(initial!=0) {
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Created semaphore handle %p",
		  handle);
#endif

	_wapi_handle_unlock_handle (handle);
	
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
gboolean ReleaseSemaphore(gpointer handle, gint32 count, gint32 *prevcount)
{
	struct _WapiHandle_sem *sem_handle;
	gboolean ok;
	gboolean ret=FALSE;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_SEM,
				(gpointer *)&sem_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up sem handle %p", handle);
		return(FALSE);
	}

	_wapi_handle_lock_handle (handle);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": sem %p val %d count %d",
		  handle, sem_handle->val, count);
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
	_wapi_handle_set_signal_state (handle, TRUE, TRUE);
	
	ret=TRUE;

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": sem %p val now %d", handle,
		  sem_handle->val);
#endif
	
end:
	_wapi_handle_unlock_handle (handle);

	return(ret);
}
