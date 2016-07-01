
#include "wapi.h"

#include "handles-private.h"
#include "process-private.h"
#include "thread-private.h"
#include "io-trace.h"

#include "mono/utils/mono-lazy-init.h"

gboolean _wapi_has_shut_down = FALSE;

void
wapi_init (void)
{
	_wapi_handle_init ();
	_wapi_shm_semaphores_init ();
	_wapi_io_init ();
	_wapi_processes_init ();
	_wapi_thread_init ();
	_wapi_semaphore_init ();
	_wapi_mutex_init ();
	_wapi_event_init ();
	_wapi_socket_init ();
}

void
wapi_cleanup (void)
{
	g_assert (_wapi_has_shut_down == FALSE);
	_wapi_has_shut_down = TRUE;

	_wapi_error_cleanup ();
	_wapi_thread_cleanup ();
	wapi_processes_cleanup ();
	_wapi_io_cleanup ();
	_wapi_handle_cleanup ();
}

/* Use this instead of getpid(), to cope with linuxthreads.  It's a
 * function rather than a variable lookup because we need to get at
 * this before share_init() might have been called. */
static mono_lazy_init_t _wapi_pid_init_lazy = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
static pid_t _wapi_pid;

static void
_wapi_pid_init (void)
{
	_wapi_pid = getpid ();
}

pid_t
_wapi_getpid (void)
{
	mono_lazy_initialize (&_wapi_pid_init_lazy, _wapi_pid_init);
	return _wapi_pid;
}

typedef struct {
	gpointer ret;
	WapiHandleType type;
	gchar *utf8_name;
} _WapiSearchHandleNamespaceData;

static gboolean _wapi_search_handle_namespace_callback (gpointer handle, gpointer data, gpointer user_data)
{
	_WapiSearchHandleNamespaceData *search_data;
	WapiHandleType type;
	WapiSharedNamespace *sharedns;

	type = _wapi_handle_type (handle);
	if (!_WAPI_SHARED_NAMESPACE (type))
		return FALSE;

	search_data = (_WapiSearchHandleNamespaceData*) user_data;

	switch (type) {
	case WAPI_HANDLE_NAMEDMUTEX: sharedns = &((struct _WapiHandle_namedmutex*) data)->sharedns; break;
	case WAPI_HANDLE_NAMEDSEM:   sharedns = &((struct _WapiHandle_namedsem*)   data)->sharedns; break;
	case WAPI_HANDLE_NAMEDEVENT: sharedns = &((struct _WapiHandle_namedevent*) data)->sharedns; break;
	default:
		g_assert_not_reached ();
	}

	if (strcmp (sharedns->name, search_data->utf8_name) == 0) {
		if (type != search_data->type) {
			/* Its the wrong type, so fail now */
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: handle %p matches name but is wrong type: %s",
				__func__, handle, _wapi_handle_ops_typename (type));
			search_data->ret = _WAPI_HANDLE_INVALID;
		} else {
			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: handle %p matches name and type",
				__func__, handle);
			search_data->ret = handle;
		}

		return TRUE;
	}

	return FALSE;
}

/* Returns the offset of the metadata array, or _WAPI_HANDLE_INVALID on error, or NULL for
 * not found
 */
gpointer _wapi_search_handle_namespace (WapiHandleType type, gchar *utf8_name)
{
	_WapiSearchHandleNamespaceData search_data;

	g_assert(_WAPI_SHARED_NAMESPACE(type));

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Lookup for handle named [%s] type %s",
		__func__, utf8_name, _wapi_handle_ops_typename (type));

	search_data.ret = NULL;
	search_data.type = type;
	search_data.utf8_name = utf8_name;
	_wapi_handle_foreach (_wapi_search_handle_namespace_callback, &search_data);
	return search_data.ret;
}

/* Lots more to implement here, but this is all we need at the moment */
gboolean
DuplicateHandle (gpointer srcprocess, gpointer src, gpointer targetprocess, gpointer *target,
	guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, guint32 options G_GNUC_UNUSED)
{
	if (srcprocess != _WAPI_PROCESS_CURRENT || targetprocess != _WAPI_PROCESS_CURRENT) {
		/* Duplicating other process's handles is not supported */
		SetLastError (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (src == _WAPI_PROCESS_CURRENT) {
		*target = _wapi_process_duplicate ();
	} else if (src == _WAPI_THREAD_CURRENT) {
		g_assert_not_reached ();
	} else {
		_wapi_handle_ref (src);
		*target = src;
	}

	return TRUE;
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
gboolean CloseHandle(gpointer handle)
{
	if (handle == _WAPI_HANDLE_INVALID){
		SetLastError (ERROR_INVALID_PARAMETER);
		return FALSE;
	}
	if (handle == (gpointer)0 && _wapi_handle_type (handle) != WAPI_HANDLE_CONSOLE) {
		/* Problem: because we map file descriptors to the
		 * same-numbered handle we can't tell the difference
		 * between a bogus handle and the handle to stdin.
		 * Assume that it's the console handle if that handle
		 * exists...
		 */
		SetLastError (ERROR_INVALID_PARAMETER);
		return FALSE;
	}

	_wapi_handle_unref (handle);
	return TRUE;
}
