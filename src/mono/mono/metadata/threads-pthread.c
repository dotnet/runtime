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

#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>

typedef struct 
{
	pthread_t id;
	MonoObject *object;
} ThreadInfo;

static GHashTable *threads=NULL;
static MonoObject *main_thread;

pthread_t ves_icall_System_Threading_Thread_Start_internal(MonoObject *this,
							   MonoObject *start)
{
	pthread_t tid;
	MonoClassField *field;
	void *(*start_func)(void *);
	ThreadInfo *thread_info;
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
		ret=pthread_create(&tid, NULL, start_func, NULL);
		if(ret!=0) {
			g_warning("pthread_create error: %s", strerror(ret));
			return(0);
		}
	
#ifdef THREAD_DEBUG
		g_message("Started thread ID %ld", tid);
#endif

		/* Store tid for cleanup later */
		if(threads==NULL) {
			threads=g_hash_table_new(g_int_hash, g_int_equal);
		}

		thread_info=g_new0(ThreadInfo, 1);
		thread_info->id=tid;
		thread_info->object=this;
		
		/* FIXME: need some locking around here */
		g_hash_table_insert(threads, &thread_info->id, thread_info);
		
		return(tid);
	}
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
	/* Give up the timeslice */
	sched_yield();
}

MonoObject *ves_icall_System_Threading_Thread_CurrentThread_internal(void)
{
	pthread_t tid;
	ThreadInfo *thread_info;
	
	/* Find the current thread id */
	tid=pthread_self();
	
	/* Look it up in the threads hash */
	if(threads!=NULL) {
		thread_info=g_hash_table_lookup(threads, &tid);
	} else {
		/* No threads running yet! */
		thread_info=NULL;
	}
	
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

static void join_all_threads(gpointer key, gpointer value, gpointer user)
{
	ThreadInfo *thread_info=(ThreadInfo *)value;
	
#ifdef THREAD_DEBUG
	g_message("[%ld]", thread_info->id);
#endif
	pthread_join(thread_info->id, NULL);
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
	main_thread=mono_new_object(thread_class);
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
