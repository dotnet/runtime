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

guint32 ves_icall_System_Threading_Thread_Thread_internal(MonoObject *this,
							  MonoObject *start)
{
	return(0);
}

void ves_icall_System_Threading_Thread_Start_internal(MonoObject *this,
						      guint32 id)
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

gboolean ves_icall_System_Threading_Thread_Join_internal(MonoObject *this,
							 int ms, guint32 tid)
{
	return(TRUE);
}

void ves_icall_System_Threading_Thread_DataSlot_register(MonoObject *slot)
{
}

void ves_icall_System_Threading_Thread_DataSlot_store(MonoObject *slot,
						      MonoObject *data)
{
}

MonoObject *ves_icall_System_Threading_Thread_DataSlot_retrieve(MonoObject *slot)
{
	return(NULL);
}

void ves_icall_System_LocalDataStoreSlot_DataSlot_unregister(MonoObject *this)
{
}

void ves_icall_System_Threading_Monitor_Monitor_enter(MonoObject *obj)
{
}

void ves_icall_System_Threading_Monitor_Monitor_exit(MonoObject *obj)
{
}

gboolean ves_icall_System_Threading_Monitor_Monitor_test_owner(MonoObject *obj)
{
	return(FALSE);
}

gboolean ves_icall_System_Threading_Monitor_Monitor_test_synchronised(MonoObject *obj)
{
	return(FALSE);
}

void ves_icall_System_Threading_Monitor_Monitor_pulse(MonoObject *obj)
{
}

void ves_icall_System_Threading_Monitor_Monitor_pulse_all(MonoObject *obj)
{
}

gboolean ves_icall_System_Threading_Monitor_Monitor_try_enter(MonoObject *obj, int ms)
{
	return(FALSE);
}

gboolean ves_icall_System_Threading_Monitor_Monitor_wait(MonoObject *obj, int ms)
{
	return(FALSE);
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
