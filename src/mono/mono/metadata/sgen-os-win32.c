#include "config.h"

#if defined(HAVE_SGEN_GC) && defined(HOST_WIN32)

#include <windows.h>

#include "metadata/sgen-gc.h"
#include "metadata/gc-internal.h"

gboolean
sgen_resume_thread (SgenThreadInfo *info)
{
	DWORD id = mono_thread_info_get_tid (info);
	HANDLE handle = OpenThread (THREAD_ALL_ACCESS, FALSE, id);
	DWORD result;

	g_assert (handle);

	result = ResumeThread (handle);
	g_assert (result != (DWORD)-1);

	CloseHandle (handle);

	return result != (DWORD)-1;
}

gboolean
sgen_suspend_thread (SgenThreadInfo *info)
{
	DWORD id = mono_thread_info_get_tid (info);
	HANDLE handle = OpenThread (THREAD_ALL_ACCESS, FALSE, id);
	CONTEXT context;
	DWORD result;

	g_assert (id != GetCurrentThreadId ());

	g_assert (handle);

	result = SuspendThread (handle);
	if (result == (DWORD)-1) {
		fprintf (stderr, "could not suspend thread %x (handle %p): %d\n", id, handle, GetLastError ()); fflush (stderr);
		CloseHandle (handle);
		return FALSE;
	}

	context.ContextFlags = CONTEXT_INTEGER | CONTEXT_CONTROL;

	if (!GetThreadContext (handle, &context)) {
		g_assert_not_reached ();
		ResumeThread (handle);
		CloseHandle (handle);
		return FALSE;
	}

	g_assert (context.ContextFlags & CONTEXT_INTEGER);
	g_assert (context.ContextFlags & CONTEXT_CONTROL);

	CloseHandle (handle);

	info->stopped_ip = (gpointer)context.Eip;
	info->stack_start = (char*)context.Esp - REDZONE_SIZE;

	info->regs [0] = context.Edi;
	info->regs [1] = context.Esi;
	info->regs [2] = context.Ebx;
	info->regs [3] = context.Edx;
	info->regs [4] = context.Ecx;
	info->regs [5] = context.Eax;
	info->regs [6] = context.Ebp;
	info->regs [7] = context.Esp;
	info->stopped_regs = &info->regs;

	/* Notify the JIT */
	if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
		mono_gc_get_gc_callbacks ()->thread_suspend_func (info->runtime_data, NULL);

	return TRUE;
}

void
sgen_wait_for_suspend_ack (int count)
{
	/* Win32 suspend/resume is synchronous, so we don't need to wait for anything */
}

int
sgen_thread_handshake (BOOL suspend)
{
	SgenThreadInfo *info;
	SgenThreadInfo *current = mono_thread_info_current ();
	int count = 0;

	FOREACH_THREAD_SAFE (info) {
		if (info->joined_stw == suspend)
			continue;
		info->joined_stw = suspend;
		if (info == current)
			continue;
		if (info->gc_disabled)
			continue;
		if (suspend) {
			g_assert (!info->doing_handshake);
			info->doing_handshake = TRUE;

			if (!sgen_suspend_thread (info))
				continue;
		} else {
			g_assert (info->doing_handshake);
			info->doing_handshake = FALSE;

			if (!sgen_resume_thread (info))
				continue;
		}
		++count;
	} END_FOREACH_THREAD_SAFE
	return count;
}

void
sgen_os_init (void)
{
}

int
mono_gc_get_suspend_signal (void)
{
	return -1;
}

#endif
