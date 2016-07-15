/*
 * thread-private.h:  Private definitions for thread handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_THREAD_PRIVATE_H_
#define _WAPI_THREAD_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <pthread.h>

#include "wapi-private.h"

void
_wapi_thread_init (void);

extern void _wapi_thread_cleanup (void);

#endif /* _WAPI_THREAD_PRIVATE_H_ */
