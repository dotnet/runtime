#ifndef _WAPI_EVENT_PRIVATE_H_
#define _WAPI_EVENT_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <pthread.h>

#include <mono/io-layer/mono-mutex.h>

struct _WapiHandle_event
{
	gboolean manual;
};

struct _WapiHandlePrivate_event
{
	int dummy;
};

#endif /* _WAPI_EVENT_PRIVATE_H_ */
