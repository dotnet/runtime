/*
 * timefuncs.c:  performance timer and other time functions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <sys/time.h>
#include <stdlib.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/timefuncs-private.h>

void _wapi_time_t_to_filetime (time_t timeval, WapiFileTime *filetime)
{
	guint64 ticks;
	
	ticks = ((guint64)timeval * 10000000) + 116444736000000000UL;
	filetime->dwLowDateTime = ticks & 0xFFFFFFFF;
	filetime->dwHighDateTime = ticks >> 32;
}

void _wapi_timeval_to_filetime (struct timeval *tv, WapiFileTime *filetime)
{
	guint64 ticks;
	
	ticks = ((guint64)tv->tv_sec * 10000000) +
		((guint64)tv->tv_usec * 10) + 116444736000000000UL;
	filetime->dwLowDateTime = ticks & 0xFFFFFFFF;
	filetime->dwHighDateTime = ticks >> 32;
}

gboolean QueryPerformanceCounter(WapiLargeInteger *count G_GNUC_UNUSED)
{
	return(FALSE);
}

gboolean QueryPerformanceFrequency(WapiLargeInteger *freq G_GNUC_UNUSED)
{
	return(FALSE);
}

