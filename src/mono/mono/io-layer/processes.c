/*
 * processes.c:  Process handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#if HAVE_BOEHM_GC
#include <gc/gc.h>
#include "mono/utils/mono-hash.h"
#endif
#include <glib.h>
#include <string.h>
#include <pthread.h>
#include <sched.h>
#include <sys/time.h>
#include <errno.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/process-private.h>

#undef DEBUG

static void process_close_shared (gpointer handle);

struct _WapiHandleOps _wapi_process_ops = {
	process_close_shared,		/* close_shared */
	NULL,				/* close_private */
	NULL,				/* signal */
	NULL,				/* own */
	NULL,				/* is_owned */
};

static pthread_once_t process_ops_once=PTHREAD_ONCE_INIT;

static void process_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_PROCESS,
					    WAPI_HANDLE_CAP_WAIT);
}

static void process_close_shared (gpointer handle G_GNUC_UNUSED)
{
#ifdef DEBUG
	struct _WapiHandle_process *process_handle;
	gboolean ok;

	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_PROCESS,
				(gpointer *)&process_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up process handle %p", handle);
		return;
	}
#endif
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": closing process handle %p with id %ld", handle,
		   process_handle->id);
#endif
}

gboolean CreateProcess (const gunichar2 *appname G_GNUC_UNUSED, gunichar2 *cmdline G_GNUC_UNUSED,
			WapiSecurityAttributes *process_attrs G_GNUC_UNUSED,
			WapiSecurityAttributes *thread_attrs G_GNUC_UNUSED,
			gboolean inherit_handles G_GNUC_UNUSED, guint32 create_flags G_GNUC_UNUSED,
			gpointer env G_GNUC_UNUSED, const gunichar2 *cwd G_GNUC_UNUSED,
			WapiStartupInfo *startup G_GNUC_UNUSED,
			WapiProcessInformation *process_info G_GNUC_UNUSED)
{
	pthread_once (&process_ops_once, process_ops_init);
	
	return(FALSE);
}
