/*
 * io-private.h:  Private definitions for file, console and find handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_IO_PRIVATE_H_
#define _WAPI_IO_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <glob.h>

extern struct _WapiHandleOps _wapi_file_ops;
extern struct _WapiHandleOps _wapi_console_ops;
extern struct _WapiHandleOps _wapi_find_ops;
extern struct _WapiHandleOps _wapi_pipe_ops;

/* Currently used for both FILE, CONSOLE and PIPE handle types.  This may
 * have to change in future.
 */
struct _WapiHandle_file
{
	guint32 filename;
	guint32 security_attributes;
	guint32 fileaccess;
	guint32 sharemode;
	guint32 attrs;
};

struct _WapiHandlePrivate_file
{
	int fd;
};

struct _WapiHandle_find
{
	glob_t glob;
	size_t count;
};

struct _WapiHandlePrivate_find
{
	int dummy;
};

extern int _wapi_file_handle_to_fd (gpointer handle);

#endif /* _WAPI_IO_PRIVATE_H_ */
