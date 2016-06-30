
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
	wapi_processes_init ();
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
