/*
 * threads-pthread.c: System-specific thread support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <pthread.h>
#include <stdlib.h>
#include <time.h>
#include <sys/time.h>
#include <errno.h>
#include <sched.h>

#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>

/*
 * Implementation of timed thread joining from the P1003.1d/D14 (July 1999)
 * draft spec, figure B-6.
 */
typedef struct {
	pthread_t id;
	MonoObject *object;
	pthread_mutex_t launch_mutex;
	pthread_cond_t launch_cond;
	pthread_mutex_t join_mutex;
	pthread_cond_t exit_cond;
	void *(*start_routine)(void *arg);
	void *arg;
	void *status;
	gboolean exiting;
	gboolean launch_ack;
} ThreadInfo;

static pthread_key_t timed_thread_key;
static pthread_once_t timed_thread_once = PTHREAD_ONCE_INIT;

static pthread_mutex_t threads_mutex;
static GHashTable *threads=NULL;
static MonoObject *main_thread;

/* As an alternative to storing this hash (and which would solve the
 * GC-moving-stuff-around problem), we could define a thread-system
 * specific struct and add a wrapper to it in the LocalDataStoreSlot
 * class
 */
typedef struct 
{
	MonoObject *slot;
	pthread_key_t key;
} DataSlot;

static pthread_mutex_t data_slots_mutex;
static GHashTable *data_slots=NULL;

#ifndef HAVE_PTHREAD_MUTEX_TIMEDLOCK
/*
 * This stuff should go in a separate support code library if it gets any
 * bigger.
 */

/*
 * Implementation of timed mutex locking also from the P1003.1d/D14
 * (July 1999) draft spec, figure B-4.
 */
static int pthread_mutex_timedlock(pthread_mutex_t *mutex,
				   const struct timespec *timeout)
{
	struct timeval timenow;
	struct timespec sleepytime;

	/* This is just to avoid a completely busy wait */
	sleepytime.tv_sec=0;
	sleepytime.tv_nsec=10000;	/* 10ms */
	
	while(pthread_mutex_trylock(mutex)==EBUSY) {
		gettimeofday(&timenow, NULL);
		
		if(timenow.tv_sec >= timeout->tv_sec &&
		   (timenow.tv_usec * 1000) >= timeout->tv_nsec) {
			return(ETIMEDOUT);
		}
		
		nanosleep(&sleepytime, NULL);
	}
	
	return(0);
}
#endif /* !HAVE_PTHREAD_MUTEX_TIMEDLOCK */

static void timed_thread_init()
{
	pthread_key_create(&timed_thread_key, NULL);
}

static void timed_thread_exit(void *status)
{
	ThreadInfo *thread;
	void *specific;
	
	if((specific = pthread_getspecific(timed_thread_key)) == NULL) {
		/* Handle cases which won't happen with correct usage.
		 */
		pthread_exit(NULL);
	}
	
	thread=(ThreadInfo *)specific;
	
	pthread_mutex_lock(&thread->join_mutex);
	
	/* Tell a joiner that we're exiting.
	 */
	thread->status = status;
	thread->exiting = TRUE;

	pthread_cond_signal(&thread->exit_cond);
	pthread_mutex_unlock(&thread->join_mutex);
	
	/* Call pthread_exit() to call destructors and really exit the
	 * thread.
	 */
	pthread_exit(NULL);
}

/* Routine to establish thread specific data value and run the actual
 * thread start routine which was supplied to timed_thread_create()
 */
static void *timed_thread_start_routine(void *args)
{
	ThreadInfo *thread = (ThreadInfo *)args;
	
	/* Wait for the launch condition to be signalled */
	pthread_mutex_lock(&thread->launch_mutex);
	pthread_cond_wait(&thread->launch_cond, &thread->launch_mutex);
	thread->launch_ack=TRUE;
	pthread_mutex_unlock(&thread->launch_mutex);
	
	pthread_once(&timed_thread_once, timed_thread_init);
	pthread_setspecific(timed_thread_key, (void *)thread);
	timed_thread_exit((thread->start_routine)(thread->arg));

	/* pthread_create routine has to return something to keep gcc
	 * quiet
	 */
	return(NULL);
}

/* Allocate a thread which can be used with timed_thread_join().
 */
static int timed_thread_create(ThreadInfo **threadp,
			       const pthread_attr_t *attr,
			       void *(*start_routine)(void *), void *arg)
{
	ThreadInfo *thread;
	int result;
	
	thread=(ThreadInfo *)g_new0(ThreadInfo, 1);
	pthread_mutex_init(&thread->launch_mutex, NULL);
	pthread_cond_init(&thread->launch_cond, NULL);
	pthread_mutex_init(&thread->join_mutex, NULL);
	pthread_cond_init(&thread->exit_cond, NULL);
	thread->start_routine = start_routine;
	thread->arg = arg;
	thread->status = NULL;
	thread->exiting = FALSE;
	thread->launch_ack = FALSE;
	
	if((result = pthread_create(&thread->id, attr,
				    timed_thread_start_routine,
				    (void *)thread)) != 0) {
		g_free(thread);
		return(result);
	}
	
	pthread_detach(thread->id);
	*threadp = thread;
	return(0);
}

static int timed_thread_join(ThreadInfo *thread, struct timespec *timeout,
			     void **status)
{
	int result;
	
	pthread_mutex_lock(&thread->join_mutex);
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
	
	pthread_mutex_unlock(&thread->join_mutex);
	if(result == 0 && thread->exiting) {
		if(status!=NULL) {
			*status = thread->status;
		}
	}
	return(result);
}

pthread_t ves_icall_System_Threading_Thread_Thread_internal(MonoObject *this,
							    MonoObject *start)
{
	MonoClassField *field;
	void *(*start_func)(void *);
	ThreadInfo *thread;
	int ret;
	
#ifdef THREAD_DEBUG
	g_message("Trying to start a new thread: this (%p) start (%p)",
		  this, start);
#endif

	field=mono_class_get_field_from_name(mono_defaults.delegate_class, "method_ptr");
	start_func= *(gpointer *)(((char *)start) + field->offset);
	
	if(start_func==NULL) {
		g_warning("Can't locate start method!");
		/* Not sure if 0 can't be a valid pthread_t.  Calling
		 * pthread_self() on the main thread seems to always
		 * return 1024 for me
		 */
		return(0);
	} else {
		ret=timed_thread_create(&thread, NULL, start_func, NULL);
		if(ret!=0) {
			g_warning("pthread_create error: %s", strerror(ret));
			return(0);
		}
	
#ifdef THREAD_DEBUG
		g_message("Started thread ID %ld", thread->id);
#endif

		/* Store tid for cleanup later */
		pthread_mutex_lock(&threads_mutex);
		if(threads==NULL) {
			threads=g_hash_table_new(g_int_hash, g_int_equal);
		}

		/* FIXME: GC problem here with recorded object
		 * pointer!
		 *
		 * This is recorded so CurrentThread can return the
		 * Thread object.
		 */
		thread->object=this;
		
		g_hash_table_insert(threads, &thread->id, thread);
		pthread_mutex_unlock(&threads_mutex);
		
		return(thread->id);
	}
}

void ves_icall_System_Threading_Thread_Start_internal(MonoObject *this, pthread_t tid)
{
	ThreadInfo *thread;
	pthread_t myid;
	
#ifdef THREAD_DEBUG
	g_message("Launching thread %p id %ld", this, tid);
#endif

	myid=pthread_self();
	if(myid==tid) {
		g_warning("Trying to launch my own thread!");
		return;
	}
	
	pthread_mutex_lock(&threads_mutex);
	if(threads!=NULL) {
		thread=g_hash_table_lookup(threads, &tid);
	} else {
		/* No threads running yet! */
		thread=NULL;
	}
	pthread_mutex_unlock(&threads_mutex);
	
	if(thread==NULL) {
		g_warning("Can't find thread id %ld", tid);
		return;
	}

	/* Tell the thread to get moving */
	while(thread->launch_ack==FALSE) {
		pthread_mutex_lock(&thread->launch_mutex);
		pthread_cond_signal(&thread->launch_cond);
		pthread_mutex_unlock(&thread->launch_mutex);

		/* Try to avoid a race condition between creating a
		 * thread and getting it going far enough to wait for
		 * the go condition to be signalled
		 */
		sched_yield();
	}
	

#ifdef THREAD_DEBUG
	g_message("Launched thread id %ld", tid);
#endif
}


gint32 ves_icall_System_Threading_Thread_Sleep_internal(gint32 ms)
{
	struct timespec req, rem;
	div_t divvy;
	int ret;
	
#ifdef THREAD_DEBUG
	g_message("Sleeping for %d ms", ms);
#endif

	divvy=div(ms, 1000);
	
	req.tv_sec=divvy.quot;
	req.tv_nsec=divvy.rem*1000;
	
	ret=nanosleep(&req, &rem);
	if(ret<0) {
		/* Sleep interrupted with rem time remaining */
		gint32 rems=rem.tv_sec*1000 + rem.tv_nsec/1000;
		
#ifdef THREAD_DEBUG
		g_message("Returning %dms early", rems);
#endif
		return(rems);
	}
	
#ifdef THREAD_DEBUG
	g_message("Slept");
#endif
	
	return(0);
}

void ves_icall_System_Threading_Thread_Schedule_internal(void)
{
	/* Give up the timeslice. pthread_yield() is the standard
	 * function but glibc seems to insist it's a GNU
	 * extension. However, all it does at the moment is
	 * sched_yield() anyway, and sched_yield() is a POSIX standard
	 * function.
	 */
	
	sched_yield();
}

MonoObject *ves_icall_System_Threading_Thread_CurrentThread_internal(void)
{
	pthread_t tid;
	ThreadInfo *thread_info;
	
	/* Find the current thread id */
	tid=pthread_self();
	
	/* Look it up in the threads hash */
	pthread_mutex_lock(&threads_mutex);
	if(threads!=NULL) {
		thread_info=g_hash_table_lookup(threads, &tid);
	} else {
		/* No threads running yet! */
		thread_info=NULL;
	}
	pthread_mutex_unlock(&threads_mutex);
	
	/* Return the object associated with it */
	if(thread_info==NULL) {
		/* If we can't find our own thread ID, assume it's the
		 * main thread
		 */
		return(main_thread);
	} else {
		return(thread_info->object);
	}
}

/* The threads_mutex must be locked before calling this function */
static void delete_thread(ThreadInfo *thread)
{
	g_assert(threads!=NULL);
	g_assert(thread!=NULL);
	
	g_hash_table_remove(threads, &thread->id);
	g_free(thread);
}

gboolean ves_icall_System_Threading_Thread_Join_internal(MonoObject *this,
							 int ms, pthread_t tid)
{
	ThreadInfo *thread;
	pthread_t myid;
	int ret;
	
#ifdef THREAD_DEBUG
	g_message("Joining with thread %p id %ld, waiting for %dms", this,
		  tid, ms);
#endif

	myid=pthread_self();
	if(myid==tid) {
		/* .net doesnt spot this and proceeds to
		 * deadlock. This will have to be commented out if we
		 * want to be bug-compatible :-(
		 */
		g_warning("Can't join my own thread!");
		return(FALSE);
	}
	
	pthread_mutex_lock(&threads_mutex);
	if(threads!=NULL) {
		thread=g_hash_table_lookup(threads, &tid);
	} else {
		/* No threads running yet! */
		thread=NULL;
	}
	pthread_mutex_unlock(&threads_mutex);
	
	if(thread==NULL) {
		g_warning("Can't find thread id %ld", tid);
		return(FALSE);
	}
	
	if(ms==0) {
		/* block until thread exits */
		ret=timed_thread_join(thread, NULL, NULL);
		
		if(ret==0) {
			pthread_mutex_lock(&threads_mutex);
			delete_thread(thread);
			pthread_mutex_unlock(&threads_mutex);
			
			return(TRUE);
		} else {
			g_warning("Join error: %s", strerror(ret));
			return(FALSE);
		}
	} else {
		/* timeout in ms milliseconds */
		struct timespec timeout;
		struct timeval now;
		div_t divvy;
		
		divvy=div(ms, 1000);
		gettimeofday(&now, NULL);
		
		timeout.tv_sec=now.tv_sec+divvy.quot;
		timeout.tv_nsec=(now.tv_usec+divvy.rem)*1000;
		
		ret=timed_thread_join(thread, &timeout, NULL);
		if(ret==0) {
			pthread_mutex_lock(&threads_mutex);
			delete_thread(thread);
			pthread_mutex_unlock(&threads_mutex);
			
			return(TRUE);
		} else {
			if(ret!=ETIMEDOUT) {
				g_warning("Timed join error: %s",
					  strerror(ret));
			}
			return(FALSE);
		}
	}
}

void ves_icall_System_Threading_Thread_DataSlot_register(MonoObject *slot)
{
	DataSlot *data;
	
#ifdef THREAD_DEBUG
	g_message("DataSlot register: slot %p", slot);
#endif

	pthread_mutex_lock(&data_slots_mutex);
	if(data_slots==NULL) {
		data_slots=g_hash_table_new(g_direct_hash, g_direct_equal);
	}
	
	data=g_new0(DataSlot, 1);

	/* FIXME: GC problem here with recorded object pointer! */
	data->slot=slot;
	pthread_key_create(&data->key, NULL);

	g_hash_table_insert(data_slots, data->slot, data);
	
	pthread_mutex_unlock(&data_slots_mutex);
}

void ves_icall_System_LocalDataStoreSlot_DataSlot_unregister(MonoObject *this)
{
	/* Remove the DataSlot from the hash */
	DataSlot *data_slot;
	
#ifdef THREAD_DEBUG
	g_message("DataSlot unregister: slot %p", this);
#endif

	pthread_mutex_lock(&data_slots_mutex);
	if(data_slots!=NULL) {
		data_slot=g_hash_table_lookup(data_slots, this);
	} else {
		data_slot=NULL;
	}
	pthread_mutex_unlock(&data_slots_mutex);
	
	if(data_slot==NULL) {
		g_warning("Can't find data slot %p", this);
		return;
	}

	pthread_mutex_lock(&data_slots_mutex);
	g_hash_table_remove(data_slots, &data_slot);
	pthread_mutex_unlock(&data_slots_mutex);

	g_free(data_slot);
}

void ves_icall_System_Threading_Thread_DataSlot_store(MonoObject *slot,
						      MonoObject *data)
{
	DataSlot *data_slot;
	
#ifdef THREAD_DEBUG
	g_message("DataSlot store: slot %p data %p", slot, data);
#endif

	pthread_mutex_lock(&data_slots_mutex);
	if(data_slots!=NULL) {
		data_slot=g_hash_table_lookup(data_slots, slot);
	} else {
		data_slot=NULL;
	}
	pthread_mutex_unlock(&data_slots_mutex);
	
	if(data_slot==NULL) {
		g_warning("Can't find data slot %p", slot);
		return;
	}

	/* FIXME: GC problem here with recorded object pointer! */
	pthread_setspecific(data_slot->key, data);
}

MonoObject *ves_icall_System_Threading_Thread_DataSlot_retrieve(MonoObject *slot)
{
	DataSlot *data_slot;
	MonoObject *ret;
	
#ifdef THREAD_DEBUG
	g_message("DataSlot retrieve: slot %p", slot);
#endif

	pthread_mutex_lock(&data_slots_mutex);
	if(data_slots!=NULL) {
		data_slot=g_hash_table_lookup(data_slots, slot);
	} else {
		data_slot=NULL;
	}
	pthread_mutex_unlock(&data_slots_mutex);
	
	if(data_slot==NULL) {
		g_warning("Can't find data slot %p", slot);
		return(NULL);
	}

	ret=pthread_getspecific(data_slot->key);

#ifdef THREAD_DEBUG
	g_message("DataSlot retrieve: returning %p", ret);
#endif
	
	return(ret);
}

void ves_icall_System_Threading_Monitor_Monitor_enter(MonoObject *obj)
{
	pthread_t tid=pthread_self();
	
#ifdef THREAD_LOCK_DEBUG
	g_message("Locking %p in thread %ld", obj, tid);
#endif
	
	if(obj->synchronisation.sync_tid==tid) {
		obj->synchronisation.recursion_count++;
		return;
	}
	
	pthread_mutex_lock(&obj->synchronisation.mutex);

	obj->synchronisation.sync_tid=tid;
	obj->synchronisation.recursion_count=1;
}

void ves_icall_System_Threading_Monitor_Monitor_exit(MonoObject *obj)
{
	pthread_t tid=pthread_self();
	
	g_assert(obj->synchronisation.sync_tid==tid);
	g_assert(obj->synchronisation.recursion_count>0);
	
#ifdef THREAD_LOCK_DEBUG
	g_message("Unlocking %p in thread %ld", obj, tid);
#endif
	
	obj->synchronisation.recursion_count--;
	
	if(obj->synchronisation.recursion_count==0) {
		obj->synchronisation.sync_tid=0;
		pthread_mutex_unlock(&obj->synchronisation.mutex);
	}
}

gboolean ves_icall_System_Threading_Monitor_Monitor_test_owner(MonoObject *obj)
{
	pthread_t tid=pthread_self();
	
	if(obj->synchronisation.sync_tid==tid) {
		return(TRUE);
	} else {
		return(FALSE);
	}
}

gboolean ves_icall_System_Threading_Monitor_Monitor_test_synchronised(MonoObject *obj)
{
	if(obj->synchronisation.sync_tid!=0) {
		return(TRUE);
	} else {
		return(FALSE);
	}
}

void ves_icall_System_Threading_Monitor_Monitor_pulse(MonoObject *obj)
{
	pthread_t tid=pthread_self();
	
	g_assert(obj->synchronisation.sync_tid==tid);

#ifdef THREAD_LOCK_DEBUG
	g_message("Pulsing %p", obj);
#endif
	
	pthread_cond_signal(&obj->synchronisation.cond);
}

void ves_icall_System_Threading_Monitor_Monitor_pulse_all(MonoObject *obj)
{
	pthread_t tid=pthread_self();
	
	g_assert(obj->synchronisation.sync_tid==tid);
	
#ifdef THREAD_LOCK_DEBUG
	g_message("Pulsing all %p", obj);
#endif
	
	pthread_cond_broadcast(&obj->synchronisation.cond);
}

gboolean ves_icall_System_Threading_Monitor_Monitor_try_enter(MonoObject *obj,
							      int ms)
{
	pthread_t tid=pthread_self();
	int ret;
	
#ifdef THREAD_LOCK_DEBUG
	g_message("Trying to lock %p in thread %ld with timeout %dms", obj,
		  tid, ms);
#endif
	
	if(obj->synchronisation.sync_tid==tid) {
		obj->synchronisation.recursion_count++;
		return(TRUE);
	}

	if(ms==0) {
		ret=pthread_mutex_trylock(&obj->synchronisation.mutex);
		if(ret==0) {
			/* Locked */
			obj->synchronisation.sync_tid=tid;
			obj->synchronisation.recursion_count=1;
			return(TRUE);
		} else {
#ifdef THREAD_LOCK_DEBUG
			g_message("Not locked (%s)", strerror(ret));
#endif
			return(FALSE);
		}
	} else {
		struct timespec timeout;
		struct timeval now;
		div_t divvy;
		
		divvy=div(ms, 1000);
		gettimeofday(&now, NULL);
		
		timeout.tv_sec=now.tv_sec+divvy.quot;
		timeout.tv_nsec=(now.tv_usec+divvy.rem)*1000;
		
		ret=pthread_mutex_timedlock(&obj->synchronisation.mutex,
					    &timeout);
		if(ret==0) {
			/* Locked */
			obj->synchronisation.sync_tid=tid;
			obj->synchronisation.recursion_count=1;
			return(TRUE);
		} else {
#ifdef THREAD_LOCK_DEBUG
			g_message("Not locked (%s)", strerror(ret));
#endif
			return(FALSE);
		}
	}
}

gboolean ves_icall_System_Threading_Monitor_Monitor_wait(MonoObject *obj,
							 int ms)
{
	pthread_t tid=pthread_self();
	guint save_recursion_count;
	int ret;
	
	g_assert(obj->synchronisation.sync_tid==tid);
	
#ifdef THREAD_LOCK_DEBUG
	g_message("Trying to wait for %p in thread %ld with timeout %dms", obj,
		  tid, ms);
#endif
	
	/* The mutex is released while the thread is waiting for the
	 * condition.  Another thread will try and acquire it, so
	 * release the thread pointer here.
	 */
	save_recursion_count=obj->synchronisation.recursion_count;
	obj->synchronisation.sync_tid=0;
	obj->synchronisation.recursion_count=0;
	
	if(ms==0) {
		ret=pthread_cond_wait(&obj->synchronisation.cond,
				      &obj->synchronisation.mutex);
		/* Signalled */
		/* Regain the thread pointer now the mutex is locked
		 * again */
		obj->synchronisation.sync_tid=tid;
		obj->synchronisation.recursion_count=save_recursion_count;
		
		return(TRUE);
	} else {
		struct timespec timeout;
		struct timeval now;
		div_t divvy;
		
		divvy=div(ms, 1000);
		gettimeofday(&now, NULL);
		
		timeout.tv_sec=now.tv_sec+divvy.quot;
		timeout.tv_nsec=(now.tv_usec+divvy.rem)*1000;
		
		ret=pthread_cond_timedwait(&obj->synchronisation.cond,
					   &obj->synchronisation.mutex,
					   &timeout);
		/* Regain the thread pointer now the mutex is locked
		 * again (mutex is locked even if the wait times out)
		 */
		obj->synchronisation.sync_tid=tid;
		obj->synchronisation.recursion_count=save_recursion_count;
		
		if(ret==0) {
			/* Signalled */
			return(TRUE);
		} else {
#ifdef THREAD_LOCK_DEBUG
			g_message("Not signalled (%s)", strerror(ret));
#endif
			return(FALSE);
		}
	}
}

static void join_all_threads(gpointer key, gpointer value, gpointer user)
{
	ThreadInfo *thread_info=(ThreadInfo *)value;
	
#ifdef THREAD_DEBUG
	g_message("[%ld]", thread_info->id);
#endif

	if(thread_info->launch_ack==TRUE) {
		/* Only try and join a thread that's actually been started */
		timed_thread_join(thread_info, NULL, NULL);
	}
}

static gboolean free_all_threadinfo(gpointer key, gpointer value, gpointer user)
{
	g_free(value);
	
	return(TRUE);
}

void mono_thread_init(void)
{
	MonoClass *thread_class;
	
	/* Build a System.Threading.Thread object instance to return
	 * for the main line's Thread.CurrentThread property.
	 */
	thread_class=mono_class_from_name(mono_defaults.corlib, "System.Threading", "Thread");
	
	/* I wonder what happens if someone tries to destroy this
	 * object? In theory, I guess the whole program should act as
	 * though exit() were called :-)
	 */
	main_thread = mono_object_new (thread_class);

	pthread_mutex_init(&threads_mutex, NULL);
	pthread_mutex_init(&data_slots_mutex, NULL);
}


void mono_thread_cleanup(void)
{
	/* join each thread that's still running */
#ifdef THREAD_DEBUG
	g_message("Joining each running thread...");
#endif
	
	if(threads==NULL) {
#ifdef THREAD_DEBUG
		g_message("No threads");
#endif
		return;
	}
	
	g_hash_table_foreach(threads, join_all_threads, NULL);

	g_hash_table_foreach_remove(threads, free_all_threadinfo, NULL);
	g_hash_table_destroy(threads);
	threads=NULL;
}
