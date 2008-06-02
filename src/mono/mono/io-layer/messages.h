/*
 * messages.h:  Error message handling
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2008 Novell, Inc.
 */

#ifndef _WAPI_MESSAGES_H_
#define _WAPI_MESSAGES_H_

#include <glib.h>
#include <stdarg.h>

typedef enum {
	FORMAT_MESSAGE_MAX_WIDTH_MASK	= 0x000000FF,
	FORMAT_MESSAGE_ALLOCATE_BUFFER	= 0x00000100,
	FORMAT_MESSAGE_IGNORE_INSERTS	= 0x00000200,
	FORMAT_MESSAGE_FROM_STRING	= 0x00000400,
	FORMAT_MESSAGE_FROM_HMODULE	= 0x00000800,
	FORMAT_MESSAGE_FROM_SYSTEM	= 0x00001000,
	FORMAT_MESSAGE_ARGUMENT_ARRAY	= 0x00002000,
} WapiFormatMessageFlags;


extern guint32 FormatMessage (guint32 flags, gconstpointer source,
			      guint32 messageid, guint32 languageid,
			      gunichar2 *buf, guint32 size, ...);

#endif /* _WAPI_MESSAGES_H_ */
