/*
 * threads.c:  Thread handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <string.h>
#include <pthread.h>
#include <sched.h>
#include <sys/time.h>
#include <errno.h>
#include <sys/types.h>
#include <unistd.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/thread-private.h>
#include <mono/io-layer/mutex-private.h>
#include <mono/io-layer/io-trace.h>

#include <mono/utils/mono-threads.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-once.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/w32handle.h>

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

void
_wapi_thread_init (void)
{
}

void
_wapi_thread_cleanup (void)
{
}
