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

#include <config.h>
#include <glib.h>

#ifdef HAVE_DIRENT_H
#include <dirent.h>
#endif
#include <unistd.h>
#include <utime.h>
#include <sys/types.h>
#include <sys/stat.h>

#include <mono/io-layer/wapi-remap.h>
#include <mono/io-layer/error.h>
#include <mono/utils/mono-logger-internals.h>

G_BEGIN_DECLS

#define WAIT_FAILED        ((gint) 0xFFFFFFFF)
#define WAIT_OBJECT_0      ((gint) 0x00000000)
#define WAIT_ABANDONED_0   ((gint) 0x00000080)
#define WAIT_TIMEOUT       ((gint) 0x00000102)
#define WAIT_IO_COMPLETION ((gint) 0x000000C0)

#ifdef DISABLE_IO_LAYER_TRACE
#define MONO_TRACE(...)
#else
#define MONO_TRACE(...) mono_trace (__VA_ARGS__)
#endif

#define WINAPI

typedef guint32 DWORD;
typedef gboolean BOOL;
typedef gint32 LONG;
typedef guint32 ULONG;
typedef guint UINT;

typedef gpointer HANDLE;
typedef gpointer HMODULE;

gboolean
CloseHandle (gpointer handle);

pid_t
wapi_getpid (void);

G_END_DECLS

#endif /* _WAPI_WAPI_H_ */
