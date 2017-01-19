/*
 * wapi-private.h:  internal definitions of handles and shared memory layout
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Novell, Inc.
 */

#ifndef _WAPI_PRIVATE_H_
#define _WAPI_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <sys/stat.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/io.h>

#include <mono/utils/mono-os-mutex.h>

/* There doesn't seem to be a defined symbol for this */
#define _WAPI_THREAD_CURRENT (gpointer)0xFFFFFFFE

extern gboolean _wapi_has_shut_down;

#include <mono/io-layer/io-private.h>
#include <mono/metadata/w32handle.h>

struct _WapiHandle_shared_ref
{
	/* This will be split 16:16 with the shared file segment in
	 * the top half, when I implement space increases
	 */
	guint32 offset;
};

struct _WapiFileShare
{
#ifdef WAPI_FILE_SHARE_PLATFORM_EXTRA_DATA
	WAPI_FILE_SHARE_PLATFORM_EXTRA_DATA
#endif
	guint64 device;
	guint64 inode;
	pid_t opened_by_pid;
	guint32 sharemode;
	guint32 access;
	guint32 handle_refs;
	guint32 timestamp;
};

typedef struct _WapiFileShare _WapiFileShare;

#endif /* _WAPI_PRIVATE_H_ */
