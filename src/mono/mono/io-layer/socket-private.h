/*
 * socket-private.h:  Private definitions for socket handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_SOCKET_PRIVATE_H_
#define _WAPI_SOCKET_PRIVATE_H_

#include <config.h>
#include <glib.h>

extern struct _WapiHandleOps _wapi_socket_ops;

struct _WapiHandle_socket
{
	int domain;
	int type;
	int protocol;
	int saved_error;
	int still_readable;
};

#endif /* _WAPI_SOCKET_PRIVATE_H_ */
