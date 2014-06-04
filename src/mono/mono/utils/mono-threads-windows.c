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
#include <limits.h>


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
mono_threads_core_abort_syscall (MonoThreadInfo *info)
{
}

gboolean
mono_threads_core_needs_abort_syscall (void)
{
	return FALSE;
}

void
mono_threads_core_self_suspend (MonoThreadInfo *info)
{
	g_assert_not_reached ();
}

gboolean
mono_threads_core_suspend (MonoThreadInfo *info)
{
	g_assert_not_reached ();
}

gboolean
mono_threads_core_resume (MonoThreadInfo *info)
{
	g_assert_not_reached ();
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
	HANDLE suspend_event;
} ThreadStartInfo;

static DWORD WINAPI
inner_start_thread (LPVOID arg)
{
	ThreadStartInfo *start_info = arg;
	void *t_arg = start_info->arg;
	int post_result;
	LPTHREAD_START_ROUTINE start_func = start_info->start_routine;
	DWORD result;
	gboolean suspend = start_info->suspend;
	HANDLE suspend_event = start_info->suspend_event;
	MonoThreadInfo *info;

	info = mono_thread_info_attach (&result);
	info->runtime_thread = TRUE;
	info->create_suspended = suspend;

	post_result = MONO_SEM_POST (&(start_info->registered));
	g_assert (!post_result);

	if (suspend) {
		WaitForSingleObject (suspend_event, INFINITE); /* caller will suspend the thread before setting the event. */
		CloseHandle (suspend_event);
	}

	result = start_func (t_arg);

	mono_thread_info_detach ();

	return result;
}

HANDLE
mono_threads_core_create_thread (LPTHREAD_START_ROUTINE start_routine, gpointer arg, guint32 stack_size, guint32 creation_flags, MonoNativeThreadId *out_tid)
{
	ThreadStartInfo *start_info;
	HANDLE result;
	DWORD thread_id;

	start_info = g_malloc0 (sizeof (ThreadStartInfo));
	if (!start_info)
		return NULL;
	MONO_SEM_INIT (&(start_info->registered), 0);
	start_info->arg = arg;
	start_info->start_routine = start_routine;
	start_info->suspend = creation_flags & CREATE_SUSPENDED;
	creation_flags &= ~CREATE_SUSPENDED;
	if (start_info->suspend) {
		start_info->suspend_event = CreateEvent (NULL, TRUE, FALSE, NULL);
		if (!start_info->suspend_event)
			return NULL;
	}

	result = CreateThread (NULL, stack_size, inner_start_thread, start_info, creation_flags, &thread_id);
	if (result) {
		while (MONO_SEM_WAIT (&(start_info->registered)) != 0) {
			/*if (EINTR != errno) ABORT("sem_wait failed"); */
		}
		if (start_info->suspend) {
			g_assert (SuspendThread (result) != (DWORD)-1);
			SetEvent (start_info->suspend_event);
		}
	} else if (start_info->suspend) {
		CloseHandle (start_info->suspend_event);
	}
	if (out_tid)
		*out_tid = thread_id;
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

void
mono_threads_core_resume_created (MonoThreadInfo *info, MonoNativeThreadId tid)
{
	HANDLE handle;

	handle = OpenThread (THREAD_ALL_ACCESS, TRUE, tid);
	g_assert (handle);
	ResumeThread (handle);
	CloseHandle (handle);
}

#if HAVE_DECL___READFSDWORD==0
static __inline__ __attribute__((always_inline))
unsigned long long
__readfsdword (unsigned long offset)
{
	unsigned long value;
	//	__asm__("movl %%fs:%a[offset], %k[value]" : [value] "=q" (value) : [offset] "irm" (offset));
   __asm__ volatile ("movl    %%fs:%1,%0"
     : "=r" (value) ,"=m" ((*(volatile long *) offset)));
	return value;
}
#endif

void
mono_threads_core_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	MEMORY_BASIC_INFORMATION meminfo;
#ifdef TARGET_AMD64
	/* win7 apis */
	NT_TIB* tib = (NT_TIB*)NtCurrentTeb();
	guint8 *stackTop = (guint8*)tib->StackBase;
	guint8 *stackBottom = (guint8*)tib->StackLimit;
#else
	/* http://en.wikipedia.org/wiki/Win32_Thread_Information_Block */
	void* tib = (void*)__readfsdword(0x18);
	guint8 *stackTop = (guint8*)*(int*)((char*)tib + 4);
	guint8 *stackBottom = (guint8*)*(int*)((char*)tib + 8);
#endif
	/*
	Windows stacks are expanded on demand, one page at time. The TIB reports
	only the currently allocated amount.
	VirtualQuery will return the actual limit for the bottom, which is what we want.
	*/
	if (VirtualQuery (&meminfo, &meminfo, sizeof (meminfo)) == sizeof (meminfo))
		stackBottom = MIN (stackBottom, (guint8*)meminfo.AllocationBase);

	*staddr = stackBottom;
	*stsize = stackTop - stackBottom;

}

gboolean
mono_threads_core_yield (void)
{
	return SwitchToThread ();
}

void
mono_threads_core_exit (int exit_code)
{
	ExitThread (exit_code);
}

void
mono_threads_core_unregister (MonoThreadInfo *info)
{
}

HANDLE
mono_threads_core_open_handle (void)
{
	HANDLE thread_handle;

	thread_handle = GetCurrentThread ();
	g_assert (thread_handle);

	/*
	 * The handle returned by GetCurrentThread () is a pseudo handle, so it can't be used to
	 * refer to the thread from other threads for things like aborting.
	 */
	DuplicateHandle (GetCurrentProcess (), thread_handle, GetCurrentProcess (), &thread_handle,
					 THREAD_ALL_ACCESS, TRUE, 0);

	return thread_handle;
}

int
mono_threads_get_max_stack_size (void)
{
	//FIXME
	return INT_MAX;
}

HANDLE
mono_threads_core_open_thread_handle (HANDLE handle, MonoNativeThreadId tid)
{
	return OpenThread (THREAD_ALL_ACCESS, TRUE, tid);
}

void
mono_threads_core_set_name (MonoNativeThreadId tid, const char *name)
{
}

#endif
