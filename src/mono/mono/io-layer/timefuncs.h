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

typedef struct 
{
	guint32 dwLowDateTime;
	guint32 dwHighDateTime;
} WapiFileTime;

extern gboolean QueryPerformanceCounter(WapiLargeInteger *count);
extern gboolean QueryPerformanceFrequency(WapiLargeInteger *freq);
extern guint32 GetTickCount (void);

#endif /* _WAPI_TIME_H_ */
