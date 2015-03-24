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

/* There doesn't seem to be a defined symbol for this */
#define _WAPI_PROCESS_CURRENT (gpointer)0xFFFFFFFF

/*
 * Handles > _WAPI_PROCESS_UNHANDLED are pseudo handles which represent processes
 * not started by the runtime.
 */
/* This marks a system process that we don't have a handle on */
/* FIXME: Cope with PIDs > sizeof guint */
#define _WAPI_PROCESS_UNHANDLED (1 << (8*sizeof(pid_t)-1))
#define _WAPI_PROCESS_UNHANDLED_PID_MASK (-1 & ~_WAPI_PROCESS_UNHANDLED)
#define WAPI_IS_PSEUDO_PROCESS_HANDLE(handle) ((GPOINTER_TO_UINT(handle) & _WAPI_PROCESS_UNHANDLED) == _WAPI_PROCESS_UNHANDLED)
#define WAPI_PID_TO_HANDLE(pid) GINT_TO_POINTER (_WAPI_PROCESS_UNHANDLED + (pid))
#define WAPI_HANDLE_TO_PID(handle) (GPOINTER_TO_UINT ((handle)) - _WAPI_PROCESS_UNHANDLED)

void wapi_processes_init (void);
extern gpointer _wapi_process_duplicate (void);
extern void wapi_processes_cleanup (void);

extern struct _WapiHandleOps _wapi_process_ops;

/*
 * MonoProcess describes processes we create.
 * It contains a semaphore that can be waited on in order to wait
 * for process termination. It's accessed in our SIGCHLD handler,
 * when status is updated (and pid cleared, to not clash with 
 * subsequent processes that may get executed).
 */
struct MonoProcess {
	pid_t pid; /* the pid of the process. This value is only valid until the process has exited. */
	MonoSemType exit_sem; /* this semaphore will be released when the process exits */
	int status; /* the exit status */
	gint32 handle_count; /* the number of handles to this mono_process instance */
	/* we keep a ref to the creating _WapiHandle_process handle until
	 * the process has exited, so that the information there isn't lost.
	 */
	gpointer handle;
	gboolean freeable;
	struct MonoProcess *next;
};

typedef struct MonoProcess MonoProcess;

/*
 * _WapiHandle_process is a structure containing all the required information
 * for process handling.
 */
struct _WapiHandle_process
{
	pid_t id;
	guint32 exitstatus;
	gpointer main_thread;
	WapiFileTime create_time;
	WapiFileTime exit_time;
	char *proc_name;
	size_t min_working_set;
	size_t max_working_set;
	gboolean exited;
	struct MonoProcess *mono_process;
};

typedef struct _WapiHandle_process WapiHandle_process;

#endif /* _WAPI_PROCESS_PRIVATE_H_ */
