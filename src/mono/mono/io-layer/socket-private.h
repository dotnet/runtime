#ifndef _WAPI_SOCKET_PRIVATE_H_
#define _WAPI_SOCKET_PRIVATE_H_

#include <config.h>
#include <glib.h>

extern struct _WapiHandleOps _wapi_socket_ops;

struct _WapiHandle_socket
{
	int dummy;
};

struct _WapiHandlePrivate_socket
{
	int fd;
};

#endif /* _WAPI_SOCKET_PRIVATE_H_ */
