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
#include <dirent.h>
#include <mono/io-layer/io.h>

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
	dev_t device;
	ino_t inode;
};

struct _WapiHandlePrivate_file
{
	WapiFDMapped fd_mapped;
	gboolean async;
	WapiOverlappedCB callback;
};

struct _WapiHandle_find
{
	int dummy;
};

struct _WapiHandlePrivate_find
{
	gchar **namelist;
	gchar *dir_part;
	int num;
	size_t count;
};

G_BEGIN_DECLS
int _wapi_file_handle_to_fd (gpointer handle);
gboolean _wapi_io_add_callback (gpointer handle,
				WapiOverlappedCB callback,
				guint64 flags);
G_END_DECLS

#endif /* _WAPI_IO_PRIVATE_H_ */
