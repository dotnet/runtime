#ifndef _WAPI_PROCESS_PRIVATE_H_
#define _WAPI_PROCESS_PRIVATE_H_

#include <config.h>
#include <glib.h>

extern struct _WapiHandleOps _wapi_process_ops;

struct _WapiHandle_process
{
	pid_t id;
};

struct _WapiHandlePrivate_process
{
	int dummy;
};

#endif /* _WAPI_PROCESS_PRIVATE_H_ */
