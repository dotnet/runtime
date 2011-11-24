/*
 * posix.c:  Posix-specific support.
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright (c) 2002-2009 Novell, Inc.
 * Copyright 2011 Xamarin Inc
 */

#include <config.h>
#include <glib.h>
#include <fcntl.h>
#include <unistd.h>
#include <errno.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <stdio.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/io-private.h>

#if 0
#define DEBUG(...) g_message(__VA_ARGS__)
#else
#define DEBUG(...)
#endif

static guint32
convert_from_flags(int flags)
{
	guint32 fileaccess=0;
	
#ifndef O_ACCMODE
#define O_ACCMODE (O_RDONLY|O_WRONLY|O_RDWR)
#endif

	if((flags & O_ACCMODE) == O_RDONLY) {
		fileaccess=GENERIC_READ;
	} else if ((flags & O_ACCMODE) == O_WRONLY) {
		fileaccess=GENERIC_WRITE;
	} else if ((flags & O_ACCMODE) == O_RDWR) {
		fileaccess=GENERIC_READ|GENERIC_WRITE;
	} else {
		DEBUG("%s: Can't figure out flags 0x%x", __func__, flags);
	}

	/* Maybe sort out create mode too */

	return(fileaccess);
}


gpointer _wapi_stdhandle_create (int fd, const gchar *name)
{
	struct _WapiHandle_file file_handle = {0};
	gpointer handle;
	int flags;
	
	DEBUG("%s: creating standard handle type %s, fd %d", __func__,
		  name, fd);

#if !defined(__native_client__)	
	/* Check if fd is valid */
	do {
		flags=fcntl(fd, F_GETFL);
	} while (flags == -1 && errno == EINTR);

	if(flags==-1) {
		/* Invalid fd.  Not really much point checking for EBADF
		 * specifically
		 */
		DEBUG("%s: fcntl error on fd %d: %s", __func__, fd,
			  strerror(errno));

		SetLastError (_wapi_get_win32_file_error (errno));
		return(INVALID_HANDLE_VALUE);
	}
	file_handle.fileaccess=convert_from_flags(flags);
#else
	/* 
	 * fcntl will return -1 in nacl, as there is no real file system API. 
	 * Yet, standard streams are available.
	 */
	file_handle.fileaccess = (fd == STDIN_FILENO) ? GENERIC_READ : GENERIC_WRITE;
#endif

	file_handle.fd = fd;
	file_handle.filename = g_strdup(name);
	/* some default security attributes might be needed */
	file_handle.security_attributes=0;

	/* Apparently input handles can't be written to.  (I don't
	 * know if output or error handles can't be read from.)
	 */
	if (fd == 0) {
		file_handle.fileaccess &= ~GENERIC_WRITE;
	}
	
	file_handle.sharemode=0;
	file_handle.attrs=0;

	handle = _wapi_handle_new_fd (WAPI_HANDLE_CONSOLE, fd, &file_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating file handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		return(INVALID_HANDLE_VALUE);
	}
	
	DEBUG("%s: returning handle %p", __func__, handle);

	return(handle);
}

