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

struct _WapiHandle_mutex
{
	WapiSharedNamespace sharedns;
	pid_t pid;
	pthread_t tid;
	guint32 recursion;
};

struct _WapiHandlePrivate_mutex
{
	int dummy;
};

#endif /* _WAPI_MUTEX_PRIVATE_H_ */
