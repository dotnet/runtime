/*
 * timed-thread.c:  Implementation of timed thread joining
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <mono/os/gc_wrapper.h>
#include <pthread.h>
#ifdef HAVE_SEMAPHORE_H
#include <semaphore.h>
#endif

#include <mono/io-layer/processes.h>

#include "timed-thread.h"

#include "mono-mutex.h"

#undef DEBUG

/*
 * Implementation of timed thread joining from the P1003.1d/D14 (July 1999)
 * draft spec, figure B-6.
 */

static pthread_key_t timed_thread_key;
static mono_once_t timed_thread_once = MONO_ONCE_INIT;

static void timed_thread_init(void)
{
	pthread_key_create(&timed_thread_key, NULL);
}

void _wapi_timed_thread_exit(guint32 exitstatus)
{
	TimedThread *thread;
	void *specific;
	
	if((specific = pthread_getspecific(timed_thread_key)) == NULL) {
		/* Handle cases which won't happen with correct usage.
		 */
		pthread_exit(NULL);
	}
	
	thread=(TimedThread *)specific;
	
	mono_mutex_lock(&thread->join_mutex);
	
	/* Tell a joiner that we're exiting.
	 */
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Setting thread %p id %ld exit status to %d",
		  thread, thread->id, exitstatus);
#endif

	thread->exitstatus=exitstatus;
	thread->exiting=TRUE;

	if(thread->exit_routine!=NULL) {
		thread->exit_routine(exitstatus, thread->exit_userdata);
	}
	
	pthread_cond_signal(&thread->exit_cond);
	mono_mutex_unlock(&thread->join_mutex);
	
	/* Call pthread_exit() to call destructors and really exit the
	 * thread.
	 */
	pthread_exit(NULL);
}

/* Routine to establish thread specific data value and run the actual
 * thread start routine which was supplied to timed_thread_create()
 */
static void *timed_thread_start_routine(gpointer args) G_GNUC_NORETURN;
static void *timed_thread_start_routine(gpointer args)
{
	TimedThread *thread = (TimedThread *)args;
	
	mono_once(&timed_thread_once, timed_thread_init);
	pthread_setspecific(timed_thread_key, (void *)thread);
	pthread_detach(thread->id);

	if(thread->create_flags & CREATE_SUSPENDED) {
		_wapi_timed_thread_suspend (thread);
	}
	
	_wapi_timed_thread_exit(thread->start_routine(thread->arg));
}

/* Allocate a thread which can be used with timed_thread_join().
 */
int _wapi_timed_thread_create(TimedThread **threadp,
			      const pthread_attr_t *attr,
			      guint32 create_flags,
			      guint32 (*start_routine)(gpointer),
			      void (*exit_routine)(guint32, gpointer),
			      gpointer arg, gpointer exit_userdata)
{
	TimedThread *thread;
	int result;
	
	thread=(TimedThread *)g_new0(TimedThread, 1);
	
	mono_mutex_init(&thread->join_mutex, NULL);
	pthread_cond_init(&thread->exit_cond, NULL);
	thread->create_flags = create_flags;
	sem_init (&thread->suspend_sem, 0, 0);
	thread->start_routine = start_routine;
	thread->exit_routine = exit_routine;
	thread->arg = arg;
	thread->exit_userdata = exit_userdata;
	thread->exitstatus = 0;
	thread->exiting = FALSE;
	
	*threadp = thread;

	if((result = pthread_create(&thread->id, attr,
				    timed_thread_start_routine,
				    (void *)thread)) != 0) {
		g_free(thread);
		return(result);
	}
	
	return(0);
}

int _wapi_timed_thread_attach(TimedThread **threadp,
			      void (*exit_routine)(guint32, gpointer),
			      gpointer exit_userdata)
{
	TimedThread *thread;

	thread=(TimedThread *)g_new0(TimedThread, 1);

	mono_mutex_init(&thread->join_mutex, NULL);
	pthread_cond_init(&thread->exit_cond, NULL);
	sem_init (&thread->suspend_sem, 0, 0);
	thread->exit_routine = exit_routine;
	thread->exit_userdata = exit_userdata;
	thread->exitstatus = 0;
	thread->exiting = FALSE;
	thread->id = pthread_self();

	*threadp = thread;

	return(0);
}

int _wapi_timed_thread_join(TimedThread *thread, struct timespec *timeout,
			    guint32 *exitstatus)
{
	int result;
	
	mono_mutex_lock(&thread->join_mutex);
	result=0;
	
	/* Wait until the thread announces that it's exiting, or until
	 * timeout.
	 */
	while(result == 0 && !thread->exiting) {
		if(timeout == NULL) {
			result = pthread_cond_wait(&thread->exit_cond,
						   &thread->join_mutex);
		} else {
			result = pthread_cond_timedwait(&thread->exit_cond,
							&thread->join_mutex,
							timeout);
		}
	}
	
	mono_mutex_unlock(&thread->join_mutex);
	if(result == 0 && thread->exiting) {
		if(exitstatus!=NULL) {
			*exitstatus = thread->exitstatus;
		}
	}
	return(result);
}

void _wapi_timed_thread_destroy (TimedThread *thread)
{
	mono_mutex_destroy (&thread->join_mutex);
	pthread_cond_destroy (&thread->exit_cond);
	sem_destroy (&thread->suspend_sem);
	
	g_free(thread);
}

/* I was going to base thread suspending on the algorithm presented at
 * http://home.earthlink.net/~anneart/family/Threads/code/susp.c
 *
 * Unfortunately the Boehm GC library also wants to use this technique
 * to stop the world, and will deadlock if a thread has already been
 * suspended when it tries.
 *
 * While Mono is still using libgc this will just have to be a kludge
 * to implement suspended creation of threads, rather than the general
 * purpose thread suspension.
 */
void _wapi_timed_thread_suspend (TimedThread *thread)
{
	TimedThread *self;
	void *specific;
	
	if((specific = pthread_getspecific (timed_thread_key))==NULL) {
		g_warning (G_GNUC_PRETTY_FUNCTION ": thread lookup failed");
		return;
	}
	self=(TimedThread *)specific;
	
	if(thread != self) {
		g_error (G_GNUC_PRETTY_FUNCTION
			 ": attempt to suspend a different thread!");
		exit (-1);
	}
	
	sem_wait (&thread->suspend_sem);
}

void _wapi_timed_thread_resume (TimedThread *thread)
{
	sem_post (&thread->suspend_sem);
}
