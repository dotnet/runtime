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

#include <mono/metadata/object.h>
#include <mono/metadata/threads-pthread.h>

pthread_t ves_icall_System_Threading_Thread_Start_internal(MonoObject *this,
							   MonoObject *start)
{
	pthread_t tid;
	MonoClassField *field;
	void *(*start_func)(void *);
	int ret;
	
	g_message("Trying to start a new thread: this (%p) start (%p)",
		  this, start);

	field=mono_class_get_field_from_name(mono_defaults.delegate_class, "method_ptr");
	start_func= *(gpointer *)(((char *)start) + field->offset);
	
	if(start_func==NULL) {
		g_warning("Can't locate start method!");
		/* Not sure if 0 can't be a valid pthread_t */
		return(0);
	} else {
		ret=pthread_create(&tid, NULL, start_func, NULL);
		if(ret!=0) {
			g_warning("pthread_create error: %s", strerror(ret));
			return(0);
		}
	
		g_message("Started thread ID %ld", tid);
		return(tid);
	}
}
