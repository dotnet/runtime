/*
 * process-private.h: Private definitions for process handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Novell, Inc.
 */

#ifndef _WAPI_PROCESS_PRIVATE_H_
#define _WAPI_PROCESS_PRIVATE_H_

#include <config.h>
#include <glib.h>

extern struct _WapiHandleOps _wapi_process_ops;

#define _WAPI_PROC_NAME_MAX_LEN _POSIX_PATH_MAX

struct _WapiHandle_process
{
	pid_t id;
	guint32 exitstatus;
	gpointer main_thread;
	WapiFileTime create_time;
	WapiFileTime exit_time;
	gchar proc_name[_WAPI_PROC_NAME_MAX_LEN];
	size_t min_working_set;
	size_t max_working_set;
};

extern void _wapi_process_reap (void);
extern void _wapi_process_signal_self (void);

#endif /* _WAPI_PROCESS_PRIVATE_H_ */
