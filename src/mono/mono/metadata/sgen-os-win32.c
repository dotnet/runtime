#include "config.h"

#if defined(HAVE_SGEN_GC) && !defined(USE_COOP_GC) && defined(HOST_WIN32)

#include "io-layer/io-layer.h"

#include "sgen/sgen-gc.h"
#include "metadata/gc-internals.h"

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
	memset (&info->client_info.ctx, 0, sizeof (MonoContext));
#ifdef TARGET_AMD64
    info->client_info.ctx.gregs[AMD64_RIP] = context.Rip;
    info->client_info.ctx.gregs[AMD64_RAX] = context.Rax;
    info->client_info.ctx.gregs[AMD64_RCX] = context.Rcx;
    info->client_info.ctx.gregs[AMD64_RDX] = context.Rdx;
    info->client_info.ctx.gregs[AMD64_RBX] = context.Rbx;
    info->client_info.ctx.gregs[AMD64_RSP] = context.Rsp;
    info->client_info.ctx.gregs[AMD64_RBP] = context.Rbp;
    info->client_info.ctx.gregs[AMD64_RSI] = context.Rsi;
    info->client_info.ctx.gregs[AMD64_RDI] = context.Rdi;
    info->client_info.ctx.gregs[AMD64_R8] = context.R8;
    info->client_info.ctx.gregs[AMD64_R9] = context.R9;
    info->client_info.ctx.gregs[AMD64_R10] = context.R10;
    info->client_info.ctx.gregs[AMD64_R11] = context.R11;
    info->client_info.ctx.gregs[AMD64_R12] = context.R12;
    info->client_info.ctx.gregs[AMD64_R13] = context.R13;
    info->client_info.ctx.gregs[AMD64_R14] = context.R14;
    info->client_info.ctx.gregs[AMD64_R15] = context.R15;
    info->client_info.stopped_ip = info->client_info.ctx.gregs[AMD64_RIP];
    info->client_info.stack_start = (char*)info->client_info.ctx.gregs[AMD64_RSP] - REDZONE_SIZE;
#else
	info->client_info.ctx.edi = context.Edi;
	info->client_info.ctx.esi = context.Esi;
	info->client_info.ctx.ebx = context.Ebx;
	info->client_info.ctx.edx = context.Edx;
	info->client_info.ctx.ecx = context.Ecx;
	info->client_info.ctx.eax = context.Eax;
	info->client_info.ctx.ebp = context.Ebp;
	info->client_info.ctx.esp = context.Esp;
	info->client_info.stopped_ip = (gpointer)context.Eip;
	info->client_info.stack_start = (char*)context.Esp - REDZONE_SIZE;
#endif

#else
	info->client_info.regs [0] = context.Edi;
	info->client_info.regs [1] = context.Esi;
	info->client_info.regs [2] = context.Ebx;
	info->client_info.regs [3] = context.Edx;
	info->client_info.regs [4] = context.Ecx;
	info->client_info.regs [5] = context.Eax;
	info->client_info.regs [6] = context.Ebp;
	info->client_info.regs [7] = context.Esp;
	info->client_info.stopped_ip = (gpointer)context.Eip;
	info->client_info.stack_start = (char*)context.Esp - REDZONE_SIZE;
#endif
#endif

	/* Notify the JIT */
	if (mono_gc_get_gc_callbacks ()->thread_suspend_func)
		mono_gc_get_gc_callbacks ()->thread_suspend_func (info->client_info.runtime_data, NULL, NULL);

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
	SgenThreadInfo *current = mono_thread_info_current ();
	int count = 0;

	current->client_info.suspend_done = TRUE;
	FOREACH_THREAD_SAFE (info) {
		if (info == current)
			continue;
		info->client_info.suspend_done = FALSE;
		if (info->client_info.gc_disabled)
			continue;
		if (suspend) {
			if (!sgen_suspend_thread (info))
				continue;
		} else {
			if (!sgen_resume_thread (info))
				continue;
		}
		++count;
	} FOREACH_THREAD_SAFE_END
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
