/*
 * macros.h:  Useful macros
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_MACROS_H_
#define _WAPI_MACROS_H_

#include <glib.h>

#define MAKEWORD(low, high) ((guint16)(((guint8)(low)) | \
				       ((guint16)((guint8)(high))) << 8))
#define MAKELONG(low, high) ((guint32)(((guint16)(low)) | \
				       ((guint32)((guint16)(high))) << 16))
#define LOWORD(i32) ((guint16)((i32) & 0xFFFF))
#define HIWORD(i32) ((guint16)(((guint32)(i32) >> 16) & 0xFFFF))
#define LOBYTE(i16) ((guint8)((i16) & 0xFF))
#define HIBYTE(i16) ((guint8)(((guint16)(i16) >> 8) & 0xFF))

#endif /* _WAPI_MACROS_H_ */
