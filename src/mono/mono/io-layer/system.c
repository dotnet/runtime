/*
 * system.c:  System information
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
#include <unistd.h>

#include "mono/io-layer/wapi.h"

void GetSystemInfo(WapiSystemInfo *info)
{
	info->dwPageSize=getpagesize();

	/* Fill in the rest of this junk. Maybe with libgtop */
#ifdef _SC_NPROCESSORS_CONF
	info->dwNumberOfProcessors = sysconf (_SC_NPROCESSORS_CONF);
#else
	info->dwNumberOfProcessors = 1;
#endif
}


