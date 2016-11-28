/*
 * wapi.h:  Public include files
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_WAPI_H_
#define _WAPI_WAPI_H_

#include <glib.h>

#include <sys/types.h>

#include <mono/io-layer/wapi-remap.h>
#include <mono/io-layer/types.h>
#include <mono/io-layer/macros.h>
#include <mono/io-layer/io.h>
#include <mono/io-layer/error.h>
#include <mono/io-layer/messages.h>
#include <mono/io-layer/security.h>
#include <mono/io-layer/sockets.h>
#include <mono/io-layer/status.h>
#include <mono/io-layer/timefuncs.h>
#include <mono/io-layer/versioninfo.h>

G_BEGIN_DECLS

#define WAIT_FAILED		0xFFFFFFFF
#define WAIT_OBJECT_0		((STATUS_WAIT_0) +0)
#define WAIT_ABANDONED		((STATUS_ABANDONED_WAIT_0) +0)
#define WAIT_ABANDONED_0	((STATUS_ABANDONED_WAIT_0) +0)
#define WAIT_TIMEOUT		STATUS_TIMEOUT
#define WAIT_IO_COMPLETION	STATUS_USER_APC

void
wapi_init (void);

void
wapi_cleanup (void);

gboolean
CloseHandle (gpointer handle);

gboolean
DuplicateHandle (gpointer srcprocess, gpointer src, gpointer targetprocess, gpointer *target,
	guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, guint32 options G_GNUC_UNUSED);

pid_t
wapi_getpid (void);

G_END_DECLS

#endif /* _WAPI_WAPI_H_ */
