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
#ifdef HAVE_SEMAPHORE_H
#include <semaphore.h>
#endif
#include <errno.h>
#include <string.h>
#include <sys/time.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/semaphore-private.h>

#if 0
#define DEBUG(...) g_message(__VA_ARGS__)
#else
#define DEBUG(...)
#endif

static void sema_signal(gpointer handle);
static gboolean sema_own (gpointer handle);

static void namedsema_signal (gpointer handle);
static gboolean namedsema_own (gpointer handle);

struct _WapiHandleOps _wapi_sem_ops = {
	NULL,			/* close */
	sema_signal,		/* signal */
	sema_own,		/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL			/* prewait */
};

void _wapi_sem_details (gpointer handle_info)
{
	struct _WapiHandle_sem *sem = (struct _WapiHandle_sem *)handle_info;
	
	g_print ("val: %5u, max: %5d", sem->val, sem->max);
}

struct _WapiHandleOps _wapi_namedsem_ops = {
	NULL,			/* close */
	namedsema_signal,	/* signal */
	namedsema_own,		/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL			/* prewait */
};

static gboolean sem_release (gpointer handle, gint32 count, gint32 *prev);
static gboolean namedsem_release (gpointer handle, gint32 count, gint32 *prev);

static struct 
{
	gboolean (*release)(gpointer handle, gint32 count, gint32 *prev);
} sem_ops[WAPI_HANDLE_COUNT] = {
	{NULL},
	{NULL},
	{NULL},
	{NULL},
	{sem_release},
	{NULL},
	{NULL},
	{NULL},
	{NULL},
	{NULL},
	{NULL},
	{NULL},
	{namedsem_release},
};

static mono_once_t sem_ops_once=MONO_ONCE_INIT;

static void sem_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_SEM,
					    WAPI_HANDLE_CAP_WAIT |
					    WAPI_HANDLE_CAP_SIGNAL);
	_wapi_handle_register_capabilities (WAPI_HANDLE_NAMEDSEM,
					    WAPI_HANDLE_CAP_WAIT |
					    WAPI_HANDLE_CAP_SIGNAL);
}

static void sema_signal(gpointer handle)
{
	ReleaseSemaphore(handle, 1, NULL);
}

static gboolean sema_own (gpointer handle)
{
	struct _WapiHandle_sem *sem_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_SEM,
				(gpointer *)&sem_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up sem handle %p", __func__,
			   handle);
		return(FALSE);
	}
	
	DEBUG("%s: owning sem handle %p", __func__, handle);

	sem_handle->val--;
	
	DEBUG ("%s: sem %p val now %d", __func__, handle, sem_handle->val);

	if(sem_handle->val==0) {
		_wapi_handle_set_signal_state (handle, FALSE, FALSE);
	}

	return(TRUE);
}

static void namedsema_signal (gpointer handle)
{
	ReleaseSemaphore (handle, 1, NULL);
}

/* NB, always called with the shared handle lock held */
static gboolean namedsema_own (gpointer handle)
{
	struct _WapiHandle_namedsem *namedsem_handle;
	gboolean ok;
	
	DEBUG ("%s: owning named sem handle %p", __func__, handle);

	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_NAMEDSEM,
				  (gpointer *)&namedsem_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up named sem handle %p",
			   __func__, handle);
		return (FALSE);
	}
	
	namedsem_handle->val--;
	
	DEBUG ("%s: named sem %p val now %d", __func__, handle,
		   namedsem_handle->val);

	if (namedsem_handle->val == 0) {
		_wapi_shared_handle_set_signal_state (handle, FALSE);
	}
	
	return (TRUE);
}
static gpointer sem_create (WapiSecurityAttributes *security G_GNUC_UNUSED,
			    gint32 initial, gint32 max)
{
	struct _WapiHandle_sem sem_handle = {0};
	gpointer handle;
	int thr_ret;
	
	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if a
	 * semaphore was freshly created
	 */
	SetLastError (ERROR_SUCCESS);
	
	sem_handle.val = initial;
	sem_handle.max = max;

	handle = _wapi_handle_new (WAPI_HANDLE_SEM, &sem_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating semaphore handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		return(NULL);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	if (initial != 0) {
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}

	DEBUG ("%s: Created semaphore handle %p initial %d max %d",
		   __func__, handle, initial, max);

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);

	return(handle);
}

static gpointer namedsem_create (WapiSecurityAttributes *security G_GNUC_UNUSED, gint32 initial, gint32 max, const gunichar2 *name G_GNUC_UNUSED)
{
	struct _WapiHandle_namedsem namedsem_handle = {{{0}}, 0};
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;
	gpointer ret = NULL;
	guint32 namelen;
	gint32 offset;
	
	/* w32 seems to guarantee that opening named objects can't
	 * race each other
	 */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);
	
	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if a
	 * semaphore was freshly created
	 */
	SetLastError (ERROR_SUCCESS);

	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);
	
	DEBUG ("%s: Creating named sem [%s]", __func__, utf8_name);

	offset = _wapi_search_handle_namespace (WAPI_HANDLE_NAMEDSEM,
						utf8_name);
	if (offset == -1) {
		/* The name has already been used for a different
		 * object.
		 */
		SetLastError (ERROR_INVALID_HANDLE);
		goto cleanup;
	} else if (offset != 0) {
		/* Not an error, but this is how the caller is
		 * informed that the semaphore wasn't freshly created
		 */
		SetLastError (ERROR_ALREADY_EXISTS);
	}
	/* Fall through to create the semaphore handle */

	if (offset == 0) {
		/* A new named semaphore, so create both the private
		 * and shared parts
		 */
		if (strlen (utf8_name) < MAX_PATH) {
			namelen = strlen (utf8_name);
		} else {
			namelen = MAX_PATH;
		}
	
		memcpy (&namedsem_handle.sharedns.name, utf8_name, namelen);
	
		namedsem_handle.val = initial;
		namedsem_handle.max = max;

		handle = _wapi_handle_new (WAPI_HANDLE_NAMEDSEM,
					   &namedsem_handle);
	} else {
		/* A new reference to an existing named semaphore, so
		 * just create the private part
		 */
		handle = _wapi_handle_new_from_offset (WAPI_HANDLE_NAMEDSEM,
						       offset, TRUE);
	}
	
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating named sem handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		goto cleanup;
	}
	ret = handle;
	
	if (offset == 0) {
		/* Set the initial state, as this is a completely new
		 * handle
		 */
		thr_ret = _wapi_handle_lock_shared_handles ();
		g_assert (thr_ret == 0);
		
		if (initial != 0) {
			_wapi_shared_handle_set_signal_state (handle, TRUE);
		}
		
		_wapi_handle_unlock_shared_handles ();
	}
	
	DEBUG ("%s: returning named sem handle %p", __func__, handle);

cleanup:
	g_free (utf8_name);
	
	_wapi_namespace_unlock (NULL);
	
	return (ret);
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
gpointer CreateSemaphore(WapiSecurityAttributes *security G_GNUC_UNUSED, gint32 initial, gint32 max, const gunichar2 *name)
{
	mono_once (&sem_ops_once, sem_ops_init);
	
	if (max <= 0) {
		DEBUG ("%s: max <= 0", __func__);

		SetLastError (ERROR_INVALID_PARAMETER);
		return(NULL);
	}
	
	if (initial > max || initial < 0) {
		DEBUG ("%s: initial>max or < 0", __func__);

		SetLastError (ERROR_INVALID_PARAMETER);
		return(NULL);
	}

	if (name == NULL) {
		return (sem_create (security, initial, max));
	} else {
		return (namedsem_create (security, initial, max, name));
	}
}

static gboolean sem_release (gpointer handle, gint32 count, gint32 *prevcount)
{
	struct _WapiHandle_sem *sem_handle;
	gboolean ok;
	gboolean ret=FALSE;
	int thr_ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_SEM,
				  (gpointer *)&sem_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up sem handle %p", __func__,
			   handle);
		return(FALSE);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	DEBUG ("%s: sem %p val %d count %d", __func__, handle,
		   sem_handle->val, count);
	
	/* Do this before checking for count overflow, because overflowing max
	 * is a listed technique for finding the current value
	 */
	if (prevcount != NULL) {
		*prevcount = sem_handle->val;
	}
	
	/* No idea why max is signed, but thats the spec :-( */
	if (sem_handle->val + count > (guint32)sem_handle->max) {
		DEBUG ("%s: sem %p max value would be exceeded: max %d current %d count %d", __func__, handle, sem_handle->max, sem_handle->val, count);

		goto end;
	}
	
	sem_handle->val += count;
	_wapi_handle_set_signal_state (handle, TRUE, TRUE);
	
	ret = TRUE;

	DEBUG ("%s: sem %p val now %d", __func__, handle, sem_handle->val);
	
end:
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);

	return(ret);
}

static gboolean namedsem_release (gpointer handle, gint32 count,
				  gint32 *prevcount)
{
	struct _WapiHandle_namedsem *sem_handle;
	gboolean ok;
	gboolean ret=FALSE;
	int thr_ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_NAMEDSEM,
				  (gpointer *)&sem_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up sem handle %p", __func__,
			   handle);
		return(FALSE);
	}

	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);

	DEBUG("%s: named sem %p val %d count %d", __func__, handle,
		  sem_handle->val, count);
	
	/* Do this before checking for count overflow, because overflowing max
	 * is a listed technique for finding the current value
	 */
	if (prevcount != NULL) {
		*prevcount = sem_handle->val;
	}
	
	/* No idea why max is signed, but thats the spec :-( */
	if (sem_handle->val + count > (guint32)sem_handle->max) {
		DEBUG ("%s: named sem %p max value would be exceeded: max %d current %d count %d", __func__, handle, sem_handle->max, sem_handle->val, count);

		goto end;
	}
	
	sem_handle->val += count;
	_wapi_shared_handle_set_signal_state (handle, TRUE);
	
	ret = TRUE;

	DEBUG("%s: named sem %p val now %d", __func__, handle,
		  sem_handle->val);
	
end:
	_wapi_handle_unlock_shared_handles ();

	return(ret);
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
	WapiHandleType type;
	
	if (handle == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return (FALSE);
	}
	
	type = _wapi_handle_type (handle);
	
	if (sem_ops[type].release == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return (FALSE);
	}
	
	return (sem_ops[type].release (handle, count, prevcount));
}

gpointer OpenSemaphore (guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED,
			const gunichar2 *name)
{
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;
	gpointer ret = NULL;
	gint32 offset;

	mono_once (&sem_ops_once, sem_ops_init);
	
	/* w32 seems to guarantee that opening named objects can't
	 * race each other
	 */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);
	
	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);
	
	DEBUG ("%s: Opening named sem [%s]", __func__, utf8_name);

	offset = _wapi_search_handle_namespace (WAPI_HANDLE_NAMEDSEM,
						utf8_name);
	if (offset == -1) {
		/* The name has already been used for a different
		 * object.
		 */
		SetLastError (ERROR_INVALID_HANDLE);
		goto cleanup;
	} else if (offset == 0) {
		/* This name doesn't exist */
		SetLastError (ERROR_FILE_NOT_FOUND);	/* yes, really */
		goto cleanup;
	}

	/* A new reference to an existing named semaphore, so just
	 * create the private part
	 */
	handle = _wapi_handle_new_from_offset (WAPI_HANDLE_NAMEDSEM, offset,
					       TRUE);
	
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error opening named sem handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		goto cleanup;
	}
	ret = handle;
	
	DEBUG ("%s: returning named sem handle %p", __func__, handle);

cleanup:
	g_free (utf8_name);
	
	_wapi_namespace_unlock (NULL);
	
	return (ret);
}
