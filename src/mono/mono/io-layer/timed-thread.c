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
#include <errno.h>

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
static mono_mutex_t apc_mutex;


static void timed_thread_init(void)
{
	int thr_ret;
	
	thr_ret = pthread_key_create(&timed_thread_key, NULL);
	g_assert (thr_ret == 0);
	
	thr_ret = mono_mutex_init(&apc_mutex, NULL);
	g_assert (thr_ret == 0);
}

void _wapi_timed_thread_exit(guint32 exitstatus)
{
	TimedThread *thread;
	void *specific;
	int thr_ret;
	
	if((specific = pthread_getspecific(timed_thread_key)) == NULL) {
		/* Handle cases which won't happen with correct usage.
		 */
		pthread_exit(NULL);
	}
	
	thread=(TimedThread *)specific;

	if(thread->exit_routine!=NULL) {
		thread->exit_routine(exitstatus, thread->exit_userdata);
	}
	
	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&thread->join_mutex);
	thr_ret = mono_mutex_lock(&thread->join_mutex);
	g_assert (thr_ret == 0);
	
	/* Tell a joiner that we're exiting.
	 */
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Setting thread %p id %ld exit status to %d",
		  thread, thread->id, exitstatus);
#endif

	thread->exitstatus=exitstatus;
	thread->exiting=TRUE;
	
	thr_ret = pthread_cond_signal(&thread->exit_cond);
	g_assert (thr_ret == 0);
	
	thr_ret = mono_mutex_unlock(&thread->join_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
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
	int thr_ret;
	
	mono_once(&timed_thread_once, timed_thread_init);
	thr_ret = pthread_setspecific(timed_thread_key, (void *)thread);
	g_assert (thr_ret == 0);

	/* This used to be pthread_detach(thread->id);
	 *
	 * thread->id is set in _wapi_timed_thread_create:
	 *
	 * if((result = pthread_create(&thread->id, attr,
	 *			    timed_thread_start_routine,
	 *			    (void *)thread)) != 0) {
	 *
	 * Strangeness happened: if _wapi_timed_thread_create was
	 * called directly, then thread->id was always set here.
	 * However, if _wapi_timed_thread_create was called via
	 * another function that did nothing but call
	 * _wapi_timed_thread_create, thread->id was not ever set,
	 * leading to the thread's 2M stack being wasted as it was not
	 * detached.
	 *
	 * This was 100% reproducible on Debian Woody with gcc 2.95.4,
	 * and on Red Hat 9 with gcc 3.2.2.
	 */
	thr_ret = pthread_detach(pthread_self ());
	g_assert (thr_ret == 0);

	if(thread->create_flags & CREATE_SUSPENDED) {
		thread->suspend_count = 1;
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
	int thr_ret;
	
	thread=(TimedThread *)g_new0(TimedThread, 1);
	
	thr_ret = mono_mutex_init(&thread->join_mutex, NULL);
	g_assert (thr_ret == 0);
	
	thr_ret = pthread_cond_init(&thread->exit_cond, NULL);
	g_assert (thr_ret == 0);
	
	thread->create_flags = create_flags;
	MONO_SEM_INIT (&thread->suspend_sem, 0);
	MONO_SEM_INIT (&thread->suspended_sem, 0);
	thread->start_routine = start_routine;
	thread->exit_routine = exit_routine;
	thread->arg = arg;
	thread->exit_userdata = exit_userdata;
	thread->exitstatus = 0;
	thread->exiting = FALSE;
	thread->apc_queue = NULL;
	
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
	int thr_ret;
	
	thread=(TimedThread *)g_new0(TimedThread, 1);

	thr_ret = mono_mutex_init(&thread->join_mutex, NULL);
	g_assert (thr_ret == 0);
	
	thr_ret = pthread_cond_init(&thread->exit_cond, NULL);
	g_assert (thr_ret == 0);
	
	thr_ret = MONO_SEM_INIT (&thread->suspend_sem, 0);
	g_assert (thr_ret != -1);
	
	thr_ret = MONO_SEM_INIT (&thread->suspended_sem, 0);
	g_assert (thr_ret != -1);
	
	thread->exit_routine = exit_routine;
	thread->exit_userdata = exit_userdata;
	thread->exitstatus = 0;
	thread->exiting = FALSE;
	thread->id = pthread_self();

	/* Make sure the timed-thread initialisation that the start
	 * routing does happens here too (we might be first to be
	 * called)
	 */
	mono_once(&timed_thread_once, timed_thread_init);
	thr_ret = pthread_setspecific(timed_thread_key, (void *)thread);
	g_assert (thr_ret == 0);

	*threadp = thread;

	return(0);
}

int _wapi_timed_thread_join(TimedThread *thread, struct timespec *timeout,
			    guint32 *exitstatus)
{
	int result;
	int thr_ret;
	
	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&thread->join_mutex);
	thr_ret = mono_mutex_lock(&thread->join_mutex);
	g_assert (thr_ret == 0);
	
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
	
	thr_ret = mono_mutex_unlock(&thread->join_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	if(result == 0 && thread->exiting) {
		if(exitstatus!=NULL) {
			*exitstatus = thread->exitstatus;
		}

		_wapi_timed_thread_destroy (thread);
	}
	return(result);
}

void _wapi_timed_thread_destroy (TimedThread *thread)
{
	mono_mutex_destroy (&thread->join_mutex);
	pthread_cond_destroy (&thread->exit_cond);
	MONO_SEM_DESTROY (&thread->suspend_sem);
	MONO_SEM_DESTROY (&thread->suspended_sem);
	
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

	while (MONO_SEM_WAIT (&thread->suspend_sem) != 0 && errno == EINTR);
}

void _wapi_timed_thread_resume (TimedThread *thread)
{
	MONO_SEM_POST (&thread->suspend_sem);
}

void _wapi_timed_thread_queue_apc (TimedThread *thread, 
	guint32 (*apc_callback)(gpointer), gpointer param)
{
	ApcInfo *apc;
	int thr_ret;
	
	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&apc_mutex);
	thr_ret = mono_mutex_lock(&apc_mutex);
	g_assert (thr_ret == 0);
	
	apc = (ApcInfo *)g_new(ApcInfo, 1);
	apc->callback = apc_callback;
	apc->param = param;
	thread->apc_queue = g_slist_append (thread->apc_queue, apc);

	thr_ret = mono_mutex_unlock(&apc_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
}

gboolean _wapi_timed_thread_apc_pending (TimedThread *thread)
{
	return thread->apc_queue != NULL;
}

void _wapi_timed_thread_dispatch_apc_queue (TimedThread *thread)
{
	ApcInfo* apc;
	GSList *list;
	int thr_ret;

	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&apc_mutex);
	thr_ret = mono_mutex_lock(&apc_mutex);
	g_assert (thr_ret == 0);
	
	list = thread->apc_queue;
	thread->apc_queue = NULL;

	thr_ret = mono_mutex_unlock(&apc_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	while (list != NULL) {
		apc = (ApcInfo*)list->data;
		apc->callback (apc->param);
		g_free (apc);
		list = g_slist_next (list);
	}
	g_slist_free (list);
}

