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

#include <mono/io-layer/wapi-remap.h>
#include <mono/io-layer/types.h>
#include <mono/io-layer/macros.h>
#include <mono/io-layer/io.h>
#include <mono/io-layer/access.h>
#include <mono/io-layer/context.h>
#include <mono/io-layer/error.h>
#include <mono/io-layer/messages.h>
#include <mono/io-layer/processes.h>
#include <mono/io-layer/security.h>
#include <mono/io-layer/sockets.h>
#include <mono/io-layer/status.h>
#include <mono/io-layer/timefuncs.h>
#include <mono/io-layer/versioninfo.h>
#include <mono/io-layer/wait.h>

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

#endif /* _WAPI_WAPI_H_ */
