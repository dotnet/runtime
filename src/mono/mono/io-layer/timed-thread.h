/*
 * timed-thread.h:  Implementation of timed thread joining
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_TIMED_THREAD_H_
#define _WAPI_TIMED_THREAD_H_

#include <config.h>
#include <glib.h>
#include <pthread.h>
#ifdef HAVE_SEMAPHORE_H
#include <semaphore.h>
#endif

#include "mono-mutex.h"

typedef struct
{
	pthread_t id;
	mono_mutex_t join_mutex;
	pthread_cond_t exit_cond;
	guint32 create_flags;
	sem_t suspend_sem;
	guint32 (*start_routine)(gpointer arg);
	void (*exit_routine)(guint32 exitstatus, gpointer userdata);
	gpointer arg;
	gpointer exit_userdata;
	guint32 exitstatus;
	gboolean exiting;
} TimedThread;

extern void _wapi_timed_thread_exit(guint32 exitstatus) G_GNUC_NORETURN;
extern int _wapi_timed_thread_create(TimedThread **threadp,
				     const pthread_attr_t *attr,
				     guint32 create_flags,
				     guint32 (*start_routine)(gpointer),
				     void (*exit_routine)(guint32, gpointer),
				     gpointer arg, gpointer exit_userdata);
extern int _wapi_timed_thread_attach(TimedThread **threadp,
				     void (*exit_routine)(guint32, gpointer),
				     gpointer exit_userdata);
extern int _wapi_timed_thread_join(TimedThread *thread,
				   struct timespec *timeout,
				   guint32 *exitstatus);
extern void _wapi_timed_thread_destroy (TimedThread *thread);
extern void _wapi_timed_thread_suspend (TimedThread *thread);
extern void _wapi_timed_thread_resume (TimedThread *thread);

#endif /* _WAPI_TIMED_THREAD_H_ */
