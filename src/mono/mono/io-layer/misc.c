/*
 * misc.c:  Miscellaneous internal support functions
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

#include "misc-private.h"

void _wapi_calc_timeout(struct timespec *timeout, guint32 ms)
{
	struct timeval now;
	div_t divvy;
		
	gettimeofday(&now, NULL);
	divvy=div(now.tv_usec+1000*ms, 1000000);
		
	timeout->tv_sec=now.tv_sec+divvy.quot;
	timeout->tv_nsec=divvy.rem*1000;
}
