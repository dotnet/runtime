#ifndef _WAPI_SEMAPHORE_PRIVATE_H_
#define _WAPI_SEMAPHORE_PRIVATE_H_

#include <config.h>
#include <glib.h>

/* emulate sem_t, so that we can prod the internal state more easily */
struct _WapiHandle_sem
{
	guint32 val;
	gint32 max;
};

struct _WapiHandlePrivate_sem
{
	int dummy;
};

#endif /* _WAPI_SEMAPHORE_PRIVATE_H_ */
