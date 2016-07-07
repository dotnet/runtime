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

static void thread_details (gpointer data);
static const gchar* thread_typename (void);
static gsize thread_typesize (void);

static MonoW32HandleOps _wapi_thread_ops = {
	NULL,				/* close */
	NULL,				/* signal */
	NULL,				/* own */
	NULL,				/* is_owned */
	NULL,				/* special_wait */
	NULL,				/* prewait */
	thread_details,		/* details */
	thread_typename,	/* typename */
	thread_typesize,	/* typesize */
};

void
_wapi_thread_init (void)
{
	mono_w32handle_register_ops (MONO_W32HANDLE_THREAD, &_wapi_thread_ops);

	mono_w32handle_register_capabilities (MONO_W32HANDLE_THREAD, MONO_W32HANDLE_CAP_WAIT);
}

static void thread_details (gpointer data)
{
	MonoW32HandleThread *thread = (MonoW32HandleThread*) data;
	g_print ("id: %p, owned_mutexes: %d, priority: %d",
		thread->id, thread->owned_mutexes->len, thread->priority);
}

static const gchar* thread_typename (void)
{
	return "Thread";
}

static gsize thread_typesize (void)
{
	return sizeof (MonoW32HandleThread);
}

void
_wapi_thread_cleanup (void)
{
}
