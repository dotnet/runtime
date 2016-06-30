
#include "wapi.h"

#include "handles-private.h"
#include "process-private.h"
#include "thread-private.h"

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