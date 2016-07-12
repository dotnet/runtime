/*
 * io-private.h:  Private definitions for file, console and find handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _WAPI_IO_PRIVATE_H_
#define _WAPI_IO_PRIVATE_H_

#include <config.h>
#include <glib.h>
#ifdef HAVE_DIRENT_H
#include <dirent.h>
#endif

#include <mono/io-layer/io.h>
#include <mono/io-layer/wapi-private.h>

extern gboolean _wapi_lock_file_region (int fd, off_t offset, off_t length);
extern gboolean _wapi_unlock_file_region (int fd, off_t offset, off_t length);
extern gpointer _wapi_stdhandle_create (int fd, const gchar *name);

/* Currently used for both FILE, CONSOLE and PIPE handle types.  This may
 * have to change in future.
 */
struct _WapiHandle_file
{
	gchar *filename;
	struct _WapiFileShare *share_info;	/* Pointer into shared mem */
	int fd;
	guint32 security_attributes;
	guint32 fileaccess;
	guint32 sharemode;
	guint32 attrs;
};

struct _WapiHandle_find
{
	gchar **namelist;
	gchar *dir_part;
	int num;
	size_t count;
};

#endif /* _WAPI_IO_PRIVATE_H_ */
