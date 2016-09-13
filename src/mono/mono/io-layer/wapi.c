
#include "wapi.h"

#include "io-trace.h"
#include "io.h"
#include "process-private.h"
#include "socket-private.h"

#include "mono/utils/mono-lazy-init.h"
#include "mono/utils/w32handle.h"

gboolean _wapi_has_shut_down = FALSE;

void
wapi_init (void)
{
	_wapi_io_init ();
	_wapi_processes_init ();
	_wapi_socket_init ();
}

void
wapi_cleanup (void)
{
	g_assert (_wapi_has_shut_down == FALSE);
	_wapi_has_shut_down = TRUE;

	_wapi_error_cleanup ();
	wapi_processes_cleanup ();
	_wapi_io_cleanup ();
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
wapi_getpid (void)
{
	mono_lazy_initialize (&_wapi_pid_init_lazy, _wapi_pid_init);
	return _wapi_pid;
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
	} else {
		mono_w32handle_ref (src);
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
	if (handle == INVALID_HANDLE_VALUE){
		SetLastError (ERROR_INVALID_PARAMETER);
		return FALSE;
	}
	if (handle == (gpointer)0 && mono_w32handle_get_type (handle) != MONO_W32HANDLE_CONSOLE) {
		/* Problem: because we map file descriptors to the
		 * same-numbered handle we can't tell the difference
		 * between a bogus handle and the handle to stdin.
		 * Assume that it's the console handle if that handle
		 * exists...
		 */
		SetLastError (ERROR_INVALID_PARAMETER);
		return FALSE;
	}

	mono_w32handle_unref (handle);
	return TRUE;
}
