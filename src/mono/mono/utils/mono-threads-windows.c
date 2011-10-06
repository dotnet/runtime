/*
 * mono-threads-windows.c: Low-level threading, windows version
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#include "config.h"

#if defined(HOST_WIN32)

#include <mono/utils/mono-threads.h>


void
mono_threads_init_platform (void)
{
}

void
mono_threads_core_interrupt (MonoThreadInfo *info)
{
	g_assert (0);
}

void
mono_threads_core_self_suspend (MonoThreadInfo *info)
{
	g_assert (0);
}

gboolean
mono_threads_core_suspend (MonoThreadInfo *info)
{
	g_assert (0);
}

gboolean
mono_threads_core_resume (MonoThreadInfo *info)
{
	g_assert (0);
}

void
mono_threads_platform_register (MonoThreadInfo *info)
{
}

void
mono_threads_platform_free (MonoThreadInfo *info)
{
}

typedef struct {
	LPTHREAD_START_ROUTINE start_routine;
	void *arg;
	MonoSemType registered;
	gboolean suspend;
} ThreadStartInfo;

static DWORD WINAPI
inner_start_thread (LPVOID arg)
{
	ThreadStartInfo *start_info = arg;
	void *t_arg = start_info->arg;
	int post_result;
	LPTHREAD_START_ROUTINE start_func = start_info->start_routine;
	DWORD result;

	mono_thread_info_attach (&result);

	post_result = MONO_SEM_POST (&(start_info->registered));
	g_assert (!post_result);

	if (start_info->suspend)
		SuspendThread (GetCurrentThread ());

	result = start_func (t_arg);

	g_assert (!mono_domain_get ());

	return result;
}

HANDLE
mono_threads_CreateThread (LPSECURITY_ATTRIBUTES attributes, SIZE_T stack_size, LPTHREAD_START_ROUTINE start_routine,
		LPVOID arg, DWORD creation_flags, LPDWORD thread_id)
{
	ThreadStartInfo *start_info;
	HANDLE result;

	start_info = g_malloc0 (sizeof (ThreadStartInfo));
	if (!start_info)
		return NULL;
	MONO_SEM_INIT (&(start_info->registered), 0);
	start_info->arg = arg;
	start_info->start_routine = start_routine;
	start_info->suspend = creation_flags & CREATE_SUSPENDED;
	creation_flags &= ~CREATE_SUSPENDED;

	result = CreateThread (attributes, stack_size, inner_start_thread, start_info, creation_flags, thread_id);

	if (result) {
		while (MONO_SEM_WAIT (&(start_info->registered)) != 0) {
			/*if (EINTR != errno) ABORT("sem_wait failed"); */
		}
	}
	MONO_SEM_DESTROY (&(start_info->registered));
	g_free (start_info);
	return result;
}


MonoNativeThreadId
mono_native_thread_id_get (void)
{
	return GetCurrentThreadId ();
}

gboolean
mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2)
{
	return id1 == id2;
}

gboolean
mono_native_thread_create (MonoNativeThreadId *tid, gpointer func, gpointer arg)
{
	return CreateThread (NULL, 0, (func), (arg), 0, (tid)) != NULL;
}

#endif
