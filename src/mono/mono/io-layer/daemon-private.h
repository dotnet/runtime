/*
 * daemon-private.h:  External daemon functions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_DAEMON_PRIVATE_H_
#define _WAPI_DAEMON_PRIVATE_H_

#include <mono/io-layer/wapi-private.h>

typedef enum {
	DAEMON_STARTING = 0,
	DAEMON_RUNNING  = 1,
	DAEMON_DIED_AT_STARTUP = 2,
	DAEMON_CLOSING = 3
} _wapi_daemon_status;

extern void _wapi_daemon_main (gpointer data, gpointer scratch);

#endif /* _WAPI_DAEMON_PRIVATE_H_ */
