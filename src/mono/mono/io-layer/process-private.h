/*
 * process-private.h: Private definitions for process handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_PROCESS_PRIVATE_H_
#define _WAPI_PROCESS_PRIVATE_H_

#include <config.h>
#include <glib.h>

extern struct _WapiHandleOps _wapi_process_ops;

struct _WapiHandle_process
{
	pid_t id;
	guint32 exec_errno;
	guint32 exitstatus;
	gpointer main_thread;
	guint32 env;
	WapiFileTime create_time;
	WapiFileTime exit_time;
	guint32 proc_name;
	size_t min_working_set;
	size_t max_working_set;
};

struct _WapiHandlePrivate_process
{
	int dummy;
};

#endif /* _WAPI_PROCESS_PRIVATE_H_ */
