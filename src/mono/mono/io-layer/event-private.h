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

#include <mono/utils/mono-mutex.h>

extern struct _WapiHandleOps _wapi_event_ops;
extern struct _WapiHandleOps _wapi_namedevent_ops;

extern void _wapi_event_details (gpointer handle_info);

struct _WapiHandle_event
{
	gboolean manual;
	guint32 set_count;
};

struct _WapiHandle_namedevent
{
	WapiSharedNamespace sharedns;
	gboolean manual;
	guint32 set_count;
};

#endif /* _WAPI_EVENT_PRIVATE_H_ */
