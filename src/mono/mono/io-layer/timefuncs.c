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
#include <stdio.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/timefuncs-private.h>

#undef DEBUG

void _wapi_time_t_to_filetime (time_t timeval, WapiFileTime *filetime)
{
	guint64 ticks;
	
	ticks = ((guint64)timeval * 10000000) + 116444736000000000ULL;
	filetime->dwLowDateTime = ticks & 0xFFFFFFFF;
	filetime->dwHighDateTime = ticks >> 32;
}

void _wapi_timeval_to_filetime (struct timeval *tv, WapiFileTime *filetime)
{
	guint64 ticks;
	
	ticks = ((guint64)tv->tv_sec * 10000000) +
		((guint64)tv->tv_usec * 10) + 116444736000000000ULL;
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

static void
get_uptime (struct timeval *start_tv)
{
	FILE *uptime = fopen ("/proc/uptime", "r");
	if (uptime) {
		double upt;
		if (fscanf (uptime, "%lf", &upt) == 1) {
			gettimeofday (start_tv, NULL);
			start_tv->tv_sec -= (int)upt;
			start_tv->tv_usec = 0;
			fclose (uptime);
			return;
		}
		fclose (uptime);
	}
	/* a made up uptime */
	gettimeofday (start_tv, NULL);
	start_tv->tv_sec -= 300;
}

guint32 GetTickCount (void)
{
	struct timeval tv;
	static struct timeval start_tv = {0};
	guint32 ret;

	if (!start_tv.tv_sec)
		get_uptime (&start_tv);
	ret=gettimeofday (&tv, NULL);
	if(ret==-1) {
		return(0);
	}
	
	tv.tv_sec -= start_tv.tv_sec;
	tv.tv_usec -= start_tv.tv_usec;
	if (tv.tv_usec < 0) {
		tv.tv_sec++;
		tv.tv_usec += 1000000;
	}
	ret=(guint32)((tv.tv_sec * 1000) + (tv.tv_usec / 1000));

#ifdef DEBUG
	g_message ("%s: returning %d", __func__, ret);
#endif

	return(ret);
}
