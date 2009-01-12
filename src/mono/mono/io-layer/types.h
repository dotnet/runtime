/*
 * types.h:  Generic type definitions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_TYPES_H_
#define _WAPI_TYPES_H_

#include <glib.h>

typedef union {
	struct {
		guint32 LowPart;
		gint32 HighPart;
	} u;
	guint64 QuadPart;
} WapiLargeInteger;

typedef union {
	struct {
		guint32 LowPart;
		guint32 HighPart;
	} u;
	guint64 QuadPart;
} WapiULargeInteger;

#endif /* _WAPI_TYPES_H_ */
