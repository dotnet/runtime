/*
 * timefuncs.h:  performance timer and other time functions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_TIME_H_
#define _WAPI_TIME_H_

#include <glib.h>

#include "mono/io-layer/wapi.h"

G_BEGIN_DECLS

/* The typical idiom for this struct is to cast it to and from 64bit
 * ints, hence the endian switch.
 */
typedef struct 
{
#if G_BYTE_ORDER == G_BIG_ENDIAN
	guint32 dwHighDateTime;
	guint32 dwLowDateTime;
#else
	guint32 dwLowDateTime;
	guint32 dwHighDateTime;
#endif
} WapiFileTime;

extern gboolean QueryPerformanceCounter(WapiLargeInteger *count);
extern gboolean QueryPerformanceFrequency(WapiLargeInteger *freq);
extern guint32 GetTickCount (void);

G_END_DECLS
#endif /* _WAPI_TIME_H_ */
