/*
 * event-private.h:  Private definitions for event handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_EVENT_PRIVATE_H_
#define _WAPI_EVENT_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <pthread.h>

#include <mono/io-layer/mono-mutex.h>

extern struct _WapiHandleOps _wapi_event_ops;

struct _WapiHandle_event
{
	gboolean manual;
};

struct _WapiHandlePrivate_event
{
	int dummy;
};

#endif /* _WAPI_EVENT_PRIVATE_H_ */
