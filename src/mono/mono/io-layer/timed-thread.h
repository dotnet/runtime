#ifndef _WAPI_TIMED_THREAD_H_
#define _WAPI_TIMED_THREAD_H_

#include <config.h>
#include <glib.h>
#include <pthread.h>

#include "mono-mutex.h"

typedef struct
{
	pthread_t id;
	mono_mutex_t join_mutex;
	pthread_cond_t exit_cond;
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
				     guint32 (*start_routine)(gpointer),
				     void (*exit_routine)(guint32, gpointer),
				     gpointer arg, gpointer exit_userdata);
extern int _wapi_timed_thread_join(TimedThread *thread,
				   struct timespec *timeout,
				   guint32 *exitstatus);

#endif /* _WAPI_TIMED_THREAD_H_ */
