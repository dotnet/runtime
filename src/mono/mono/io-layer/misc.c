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

/* This is used instead of g_renew when we need to keep unused
 * elements NULL, because g_renew doesn't initialize the memory it
 * returns.
 */
gpointer _wapi_g_renew0 (gpointer mem, gulong old_len, gulong new_len)
{
	gpointer new_mem=g_malloc0 (new_len);
	memcpy (new_mem, mem, old_len);
	g_free (mem);
	return(new_mem);
}
