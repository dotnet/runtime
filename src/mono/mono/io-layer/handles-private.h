#ifndef _WAPI_HANDLES_PRIVATE_H_
#define _WAPI_HANDLES_PRIVATE_H_

#include <config.h>
#include <glib.h>

#include "wait-private.h"

extern guint32 _wapi_handle_count_signalled(WaitQueueItem *item, WapiHandleType type);
extern void _wapi_handle_set_lowest(WaitQueueItem *item, guint32 idx);

#endif /* _WAPI_HANDLES_PRIVATE_H_ */
