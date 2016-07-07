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

#include "wapi-private.h"

struct _WapiHandle_mutex
{
	pthread_t tid;
	guint32 recursion;
};

struct _WapiHandle_namedmutex 
{
	struct _WapiHandle_mutex m;
	WapiSharedNamespace sharedns;
};

void
_wapi_mutex_init (void);

#endif /* _WAPI_MUTEX_PRIVATE_H_ */
