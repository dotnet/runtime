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
#include <string.h>

#include "misc-private.h"

void _wapi_calc_timeout(struct timespec *timeout, guint32 ms)
{
	struct timeval now;
	div_t ms_divvy, overflow_divvy;
	
	gettimeofday (&now, NULL);

	ms_divvy = div (ms, 1000);
	overflow_divvy = div ((now.tv_usec / 1000) + ms_divvy.rem, 1000);
		
	timeout->tv_sec = now.tv_sec + ms_divvy.quot + overflow_divvy.quot;
	timeout->tv_nsec = overflow_divvy.rem * 1000000;
}
