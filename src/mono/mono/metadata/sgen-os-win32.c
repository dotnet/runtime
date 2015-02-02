#include "config.h"

#if defined(HAVE_SGEN_GC) && defined(HOST_WIN32)

#include "io-layer/io-layer.h"

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

#if !defined(MONO_CROSS_COMPILE)
#ifdef USE_MONO_CTX
	memset (&info->ctx, 0, sizeof (MonoContext));
#ifdef TARGET_AMD64
	info->ctx.rip = context.Rip;
	info->ctx.rax = context.Rax;
	info->ctx.rcx = context.Rcx;
	info->ctx.rdx = context.Rdx;
	info->ctx.rbx = context.Rbx;
	info->ctx.rsp = context.Rsp;
	info->ctx.rbp = context.Rbp;
	info->ctx.rsi = context.Rsi;
	info->ctx.rdi = context.Rdi;
	info->ctx.r8 = context.R8;
	info->ctx.r9 = context.R9;
	info->ctx.r10 = context.R10;
	info->ctx.r11 = context.R11;
	info->ctx.r12 = context.R12;
	info->ctx.r13 = context.R13;
	info->ctx.r14 = context.R14;
	info->ctx.r15 = context.R15;
	info->stopped_ip = info->ctx.rip;
	info->stack_start = (char*)info->ctx.rsp - REDZONE_SIZE;
#else
	info->ctx.edi = context.Edi;
	info->ctx.esi = context.Esi;
	info->ctx.ebx = context.Ebx;
	info->ctx.edx = context.Edx;
	info->ctx.ecx = context.Ecx;
	info->ctx.eax = context.Eax;
	info->ctx.ebp = context.Ebp;
	info->ctx.esp = context.Esp;
	info->stopped_ip = (gpointer)context.Eip;
	info->stack_start = (char*)context.Esp - REDZONE_SIZE;
#endif

#else
	info->regs [0] = context.Edi;
	info->regs [1] = context.Esi;
	info->regs [2] = context.Ebx;
	info->regs [3] = context.Edx;
	info->regs [4] = context.Ecx;
	info->regs [5] = context.Eax;
	info->regs [6] = context.Ebp;
	info->regs [7] = context.Esp;
	info->stopped_ip = (gpointer)context.Eip;
	info->stack_start = (char*)context.Esp - REDZONE_SIZE;
#endif
#endif

	/* Notify the JIT */
	if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
		mono_gc_get_gc_callbacks ()->thread_suspend_func (info->runtime_data, NULL, NULL);

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

	current->suspend_done = TRUE;
	FOREACH_THREAD_SAFE (info) {
		if (info == current)
			continue;
		info->suspend_done = FALSE;
		if (info->gc_disabled)
			continue;
		if (suspend) {
			if (!sgen_suspend_thread (info))
				continue;
		} else {
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
