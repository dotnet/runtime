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
#include <mono/io-layer/semaphore-private.h>
#include <mono/io-layer/io-trace.h>
#include <mono/utils/mono-once.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/w32handle.h>

static void sema_signal(gpointer handle);
static gboolean sema_own (gpointer handle);
static void sema_details (gpointer data);
static const gchar* sema_typename (void);
static gsize sema_typesize (void);

static void namedsema_signal (gpointer handle);
static gboolean namedsema_own (gpointer handle);
static void namedsema_details (gpointer data);
static const gchar* namedsema_typename (void);
static gsize namedsema_typesize (void);

static MonoW32HandleOps _wapi_sem_ops = {
	NULL,			/* close */
	sema_signal,		/* signal */
	sema_own,		/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL,			/* prewait */
	sema_details,	/* details */
	sema_typename,	/* typename */
	sema_typesize,	/* typesize */
};

static MonoW32HandleOps _wapi_namedsem_ops = {
	NULL,			/* close */
	namedsema_signal,	/* signal */
	namedsema_own,		/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL,			/* prewait */
	namedsema_details,	/* details */
	namedsema_typename,	/* typename */
	namedsema_typesize,	/* typesize */
};

void
_wapi_semaphore_init (void)
{
	mono_w32handle_register_ops (MONO_W32HANDLE_SEM,      &_wapi_sem_ops);
	mono_w32handle_register_ops (MONO_W32HANDLE_NAMEDSEM, &_wapi_namedsem_ops);

	mono_w32handle_register_capabilities (MONO_W32HANDLE_SEM,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL));
	mono_w32handle_register_capabilities (MONO_W32HANDLE_NAMEDSEM,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL));
}

static const char* sem_handle_type_to_string (MonoW32HandleType type)
{
	switch (type) {
	case MONO_W32HANDLE_SEM: return "sem";
	case MONO_W32HANDLE_NAMEDSEM: return "named sem";
	default:
		g_assert_not_reached ();
	}
}

static gboolean sem_handle_own (gpointer handle, MonoW32HandleType type)
{
	struct _WapiHandle_sem *sem_handle;

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&sem_handle)) {
		g_warning ("%s: error looking up %s handle %p",
			__func__, sem_handle_type_to_string (type), handle);
		return FALSE;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: owning %s handle %p",
		__func__, sem_handle_type_to_string (type), handle);

	sem_handle->val--;

	if (sem_handle->val == 0)
		mono_w32handle_set_signal_state (handle, FALSE, FALSE);

	return TRUE;
}

static void sema_signal(gpointer handle)
{
	ReleaseSemaphore(handle, 1, NULL);
}

static gboolean sema_own (gpointer handle)
{
	return sem_handle_own (handle, MONO_W32HANDLE_SEM);
}

static void namedsema_signal (gpointer handle)
{
	ReleaseSemaphore (handle, 1, NULL);
}

/* NB, always called with the shared handle lock held */
static gboolean namedsema_own (gpointer handle)
{
	return sem_handle_own (handle, MONO_W32HANDLE_NAMEDSEM);
}

static void sema_details (gpointer data)
{
	struct _WapiHandle_sem *sem = (struct _WapiHandle_sem *)data;
	g_print ("val: %5u, max: %5d", sem->val, sem->max);
}

static void namedsema_details (gpointer data)
{
	struct _WapiHandle_namedsem *namedsem = (struct _WapiHandle_namedsem *)data;
	g_print ("val: %5u, max: %5d, name: \"%s\"", namedsem->s.val, namedsem->s.max, namedsem->sharedns.name);
}

static const gchar* sema_typename (void)
{
	return "Semaphore";
}

static gsize sema_typesize (void)
{
	return sizeof (struct _WapiHandle_sem);
}

static const gchar* namedsema_typename (void)
{
	return "N.Semaphore";
}

static gsize namedsema_typesize (void)
{
	return sizeof (struct _WapiHandle_namedsem);
}

static gpointer sem_handle_create (struct _WapiHandle_sem *sem_handle, MonoW32HandleType type, gint32 initial, gint32 max)
{
	gpointer handle;
	int thr_ret;

	sem_handle->val = initial;
	sem_handle->max = max;

	handle = mono_w32handle_new (type, sem_handle);
	if (handle == INVALID_HANDLE_VALUE) {
		g_warning ("%s: error creating %s handle",
			__func__, sem_handle_type_to_string (type));
		SetLastError (ERROR_GEN_FAILURE);
		return NULL;
	}

	thr_ret = mono_w32handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	if (initial != 0)
		mono_w32handle_set_signal_state (handle, TRUE, FALSE);

	thr_ret = mono_w32handle_unlock_handle (handle);
	g_assert (thr_ret == 0);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: created %s handle %p",
		__func__, sem_handle_type_to_string (type), handle);

	return handle;
}

static gpointer sem_create (gint32 initial, gint32 max)
{
	struct _WapiHandle_sem sem_handle;
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: creating %s handle, initial %d max %d",
		__func__, sem_handle_type_to_string (MONO_W32HANDLE_SEM), initial, max);
	return sem_handle_create (&sem_handle, MONO_W32HANDLE_SEM, initial, max);
}

static gpointer namedsem_create (gint32 initial, gint32 max, const gunichar2 *name)
{
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: creating %s handle, initial %d max %d name \"%s\"",
		__func__, sem_handle_type_to_string (MONO_W32HANDLE_NAMEDSEM), initial, max, name);

	/* w32 seems to guarantee that opening named objects can't race each other */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);

	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Creating named sem name [%s] initial %d max %d", __func__, utf8_name, initial, max);

	handle = _wapi_search_handle_namespace (MONO_W32HANDLE_NAMEDSEM, utf8_name);
	if (handle == INVALID_HANDLE_VALUE) {
		/* The name has already been used for a different object. */
		handle = NULL;
		SetLastError (ERROR_INVALID_HANDLE);
	} else if (handle) {
		/* Not an error, but this is how the caller is informed that the semaphore wasn't freshly created */
		SetLastError (ERROR_ALREADY_EXISTS);

		/* this is used as creating a new handle */
		mono_w32handle_ref (handle);
	} else {
		/* A new named semaphore */
		struct _WapiHandle_namedsem namedsem_handle;

		strncpy (&namedsem_handle.sharedns.name [0], utf8_name, MAX_PATH);
		namedsem_handle.sharedns.name [MAX_PATH] = '\0';

		handle = sem_handle_create ((struct _WapiHandle_sem*) &namedsem_handle, MONO_W32HANDLE_NAMEDSEM, initial, max);
	}

	g_free (utf8_name);

	thr_ret = _wapi_namespace_unlock (NULL);
	g_assert (thr_ret == 0);

	return handle;
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
	if (max <= 0) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: max <= 0", __func__);

		SetLastError (ERROR_INVALID_PARAMETER);
		return(NULL);
	}
	
	if (initial > max || initial < 0) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: initial>max or < 0", __func__);

		SetLastError (ERROR_INVALID_PARAMETER);
		return(NULL);
	}

	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if a
	 * semaphore was freshly created
	 */
	SetLastError (ERROR_SUCCESS);

	return name ? namedsem_create (initial, max, name) : sem_create (initial, max);
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
	MonoW32HandleType type;
	struct _WapiHandle_sem *sem_handle;
	int thr_ret;
	gboolean ret;

	if (!handle) {
		SetLastError (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	switch (type = mono_w32handle_get_type (handle)) {
	case MONO_W32HANDLE_SEM:
	case MONO_W32HANDLE_NAMEDSEM:
		break;
	default:
		SetLastError (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&sem_handle)) {
		g_warning ("%s: error looking up sem handle %p", __func__, handle);
		return FALSE;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: releasing %s handle %p",
		__func__, sem_handle_type_to_string (type), handle);

	thr_ret = mono_w32handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	/* Do this before checking for count overflow, because overflowing
	 * max is a listed technique for finding the current value */
	if (prevcount)
		*prevcount = sem_handle->val;

	/* No idea why max is signed, but thats the spec :-( */
	if (sem_handle->val + count > (guint32)sem_handle->max) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: %s handle %p val %d count %d max %d, max value would be exceeded",
			__func__, sem_handle_type_to_string (type), handle, sem_handle->val, count, sem_handle->max, count);

		ret = FALSE;
	} else {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: %s handle %p val %d count %d max %d",
			__func__, sem_handle_type_to_string (type), handle, sem_handle->val, count, sem_handle->max, count);

		sem_handle->val += count;
		mono_w32handle_set_signal_state (handle, TRUE, TRUE);

		ret = TRUE;
	}

	thr_ret = mono_w32handle_unlock_handle (handle);
	g_assert (thr_ret == 0);

	return ret;
}

gpointer OpenSemaphore (guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED,
			const gunichar2 *name)
{
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;

	/* w32 seems to guarantee that opening named objects can't
	 * race each other
	 */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);
	
	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Opening named sem [%s]", __func__, utf8_name);

	handle = _wapi_search_handle_namespace (MONO_W32HANDLE_NAMEDSEM,
						utf8_name);
	if (handle == INVALID_HANDLE_VALUE) {
		/* The name has already been used for a different
		 * object.
		 */
		SetLastError (ERROR_INVALID_HANDLE);
		goto cleanup;
	} else if (!handle) {
		/* This name doesn't exist */
		SetLastError (ERROR_FILE_NOT_FOUND);	/* yes, really */
		goto cleanup;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: returning named sem handle %p", __func__, handle);

cleanup:
	g_free (utf8_name);
	
	_wapi_namespace_unlock (NULL);
	
	return handle;
}
