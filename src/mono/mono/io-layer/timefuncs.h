/*
 * timefuncs.h:  performance timer functions
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

gboolean QueryPerformanceCounter(WapiLargeInteger *count);
gboolean QueryPerformanceFrequency(WapiLargeInteger *freq);

#endif /* _WAPI_TIME_H_ */
