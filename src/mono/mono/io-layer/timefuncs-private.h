/*
 * timefuncs-private.h:  performance timer and other time private functions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_TIMEFUNCS_PRIVATE_H_
#define _WAPI_TIMEFUNCS_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <sys/time.h>

extern void _wapi_time_t_to_filetime (time_t timeval, WapiFileTime *filetime);
extern void _wapi_timeval_to_filetime (struct timeval *tv,
				       WapiFileTime *filetime);

#endif /* _WAPI_TIMEFUNCS_PRIVATE_H_ */
