/**
 * \file
 * System.Net.Sockets.Socket support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_METADATA_W32SOCKET_H_
#define _MONO_METADATA_W32SOCKET_H_

#include <config.h>
#include <glib.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/icalls.h>

#ifndef HOST_WIN32
#define INVALID_SOCKET ((SOCKET)(guint32)(~0))
#define SOCKET_ERROR (-1)

typedef gint SOCKET;

typedef struct {
	guint32 len;
	gpointer buf;
} WSABUF, *LPWSABUF;
#endif

#endif /* _MONO_METADATA_W32SOCKET_H_ */
