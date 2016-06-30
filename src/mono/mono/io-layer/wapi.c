
#include "wapi.h"

#include "handles-private.h"
#include "process-private.h"
#include "thread-private.h"

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
