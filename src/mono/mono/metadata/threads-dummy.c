/*
 * threads-dummy.c: System-specific thread support dummy routines
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>

static MonoObject *main_thread;

void ves_icall_System_Threading_Thread_Start_internal(MonoObject *this,
						      MonoObject *start)
{
}

gint32 ves_icall_System_Threading_Thread_Sleep_internal(gint32 ms)
{
	return(0);
}

void ves_icall_System_Threading_Thread_Schedule_internal(void)
{
}

MonoObject *ves_icall_System_Threading_Thread_CurrentThread_internal(void)
{
	return(main_thread);
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
}
