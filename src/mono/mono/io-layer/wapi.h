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
#include <mono/io-layer/io.h>
#include <mono/io-layer/io-portability.h>
#include <mono/io-layer/error.h>

G_BEGIN_DECLS

#define WAIT_FAILED        ((int) 0xFFFFFFFF)
#define WAIT_OBJECT_0      ((int) 0x00000000)
#define WAIT_ABANDONED_0   ((int) 0x00000080)
#define WAIT_TIMEOUT       ((int) 0x00000102)
#define WAIT_IO_COMPLETION ((int) 0x000000C0)

void
wapi_init (void);

void
wapi_cleanup (void);

gboolean
CloseHandle (gpointer handle);

pid_t
wapi_getpid (void);

G_END_DECLS

#endif /* _WAPI_WAPI_H_ */
