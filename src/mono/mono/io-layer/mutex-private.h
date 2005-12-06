/*
 * mutex-private.h:  Private definitions for mutex handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_MUTEX_PRIVATE_H_
#define _WAPI_MUTEX_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <sys/types.h>

extern struct _WapiHandleOps _wapi_mutex_ops;
extern struct _WapiHandleOps _wapi_namedmutex_ops;

extern void _wapi_mutex_details (gpointer handle_info);

struct _WapiHandle_mutex
{
	pid_t pid;
	pthread_t tid;
	guint32 recursion;
};

struct _WapiHandle_namedmutex 
{
	WapiSharedNamespace sharedns;
	pid_t pid;
	pthread_t tid;
	guint32 recursion;
};

extern void _wapi_mutex_abandon (gpointer data, pid_t pid, pthread_t tid);

#endif /* _WAPI_MUTEX_PRIVATE_H_ */
