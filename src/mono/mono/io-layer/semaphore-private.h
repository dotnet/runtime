/*
 * semaphore-private.h:  Private definitions for semaphore handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_SEMAPHORE_PRIVATE_H_
#define _WAPI_SEMAPHORE_PRIVATE_H_

#include <config.h>
#include <glib.h>

extern struct _WapiHandleOps _wapi_sem_ops;
extern struct _WapiHandleOps _wapi_namedsem_ops;

/* emulate sem_t, so that we can prod the internal state more easily */
struct _WapiHandle_sem
{
	guint32 val;
	gint32 max;
};

struct _WapiHandle_namedsem
{
	struct _WapiHandle_sem s;
	WapiSharedNamespace sharedns;
};

#endif /* _WAPI_SEMAPHORE_PRIVATE_H_ */
