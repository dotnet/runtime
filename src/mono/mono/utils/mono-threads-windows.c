/*
 * mono-threads-windows.c: Low-level threading, windows version
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#include <mono/utils/mono-threads.h>

#if defined(USE_WINDOWS_BACKEND)

#include <mono/utils/mono-compiler.h>
#include <limits.h>


void
mono_threads_suspend_init (void)
{
}

static void CALLBACK
interrupt_apc (ULONG_PTR param)
{
}

gboolean
mono_threads_suspend_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	DWORD id = mono_thread_info_get_tid (info);
	HANDLE handle;
	DWORD result;

	handle = OpenThread (THREAD_ALL_ACCESS, FALSE, id);
	g_assert (handle);

	result = SuspendThread (handle);
	THREADS_SUSPEND_DEBUG ("SUSPEND %p -> %d\n", (void*)id, ret);
	if (result == (DWORD)-1) {
		CloseHandle (handle);
		return FALSE;
	}

	/* We're in the middle of a self-suspend, resume and register */
	if (!mono_threads_transition_finish_async_suspend (info)) {
		mono_threads_add_to_pending_operation_set (info);
		result = ResumeThread (handle);
		g_assert (result == 1);
		CloseHandle (handle);
		THREADS_SUSPEND_DEBUG ("FAILSAFE RESUME/1 %p -> %d\n", (void*)id, 0);
		//XXX interrupt_kernel doesn't make sense in this case as the target is not in a syscall
		return TRUE;
	}
	info->suspend_can_continue = mono_threads_get_runtime_callbacks ()->thread_state_init_from_handle (&info->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX], info);
	THREADS_SUSPEND_DEBUG ("thread state %p -> %d\n", (void*)id, res);
	if (info->suspend_can_continue) {
		//FIXME do we need to QueueUserAPC on this case?
		if (interrupt_kernel)
			QueueUserAPC ((PAPCFUNC)interrupt_apc, handle, (ULONG_PTR)NULL);
	} else {
		THREADS_SUSPEND_DEBUG ("FAILSAFE RESUME/2 %p -> %d\n", (void*)info->native_handle, 0);
	}

	CloseHandle (handle);
	return info->suspend_can_continue;
}

gboolean
mono_threads_suspend_check_suspend_result (MonoThreadInfo *info)
{
	return info->suspend_can_continue;
}

gboolean
mono_threads_suspend_begin_async_resume (MonoThreadInfo *info)
{
	DWORD id = mono_thread_info_get_tid (info);
	HANDLE handle;
	DWORD result;

	handle = OpenThread (THREAD_ALL_ACCESS, FALSE, id);
	g_assert (handle);

	if (info->async_target) {
		MonoContext ctx;
		CONTEXT context;
		gboolean res;

		ctx = info->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX].ctx;
		mono_threads_get_runtime_callbacks ()->setup_async_callback (&ctx, info->async_target, info->user_data);
		info->async_target = info->user_data = NULL;

		context.ContextFlags = CONTEXT_INTEGER | CONTEXT_CONTROL;

		if (!GetThreadContext (handle, &context)) {
			CloseHandle (handle);
			return FALSE;
		}

		g_assert (context.ContextFlags & CONTEXT_INTEGER);
		g_assert (context.ContextFlags & CONTEXT_CONTROL);

		mono_monoctx_to_sigctx (&ctx, &context);

		context.ContextFlags = CONTEXT_INTEGER | CONTEXT_CONTROL;
		res = SetThreadContext (handle, &context);
		if (!res) {
			CloseHandle (handle);
			return FALSE;
		}
	}

	result = ResumeThread (handle);
	CloseHandle (handle);

	return result != (DWORD)-1;
}


void
mono_threads_suspend_register (MonoThreadInfo *info)
{
}

void
mono_threads_suspend_free (MonoThreadInfo *info)
{
}

#endif

#if defined (HOST_WIN32)

void
mono_threads_platform_register (MonoThreadInfo *info)
{
	HANDLE thread_handle;

	thread_handle = GetCurrentThread ();
	g_assert (thread_handle);

	/* The handle returned by GetCurrentThread () is a pseudo handle, so it can't
	 * be used to refer to the thread from other threads for things like aborting. */
	DuplicateHandle (GetCurrentProcess (), thread_handle, GetCurrentProcess (), &thread_handle, THREAD_ALL_ACCESS, TRUE, 0);

	g_assert (!info->handle);
	info->handle = thread_handle;
}

int
mono_threads_platform_create_thread (MonoThreadStart thread_fn, gpointer thread_data, gsize stack_size, MonoNativeThreadId *out_tid)
{
	HANDLE result;
	DWORD thread_id;

	result = CreateThread (NULL, stack_size, (LPTHREAD_START_ROUTINE) thread_fn, thread_data, 0, &thread_id);
	if (!result)
		return -1;

	/* A new handle is open when attaching
	 * the thread, so we don't need this one */
	CloseHandle (result);

	if (out_tid)
		*out_tid = thread_id;

	return 0;
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

gboolean
mono_native_thread_join (MonoNativeThreadId tid)
{
	HANDLE handle;

	if (!(handle = OpenThread (THREAD_ALL_ACCESS, TRUE, tid)))
		return FALSE;

	DWORD res = WaitForSingleObject (handle, INFINITE);

	CloseHandle (handle);

	return res != WAIT_FAILED;
}

#if HAVE_DECL___READFSDWORD==0
static MONO_ALWAYS_INLINE unsigned long long
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
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
	MEMORY_BASIC_INFORMATION meminfo;
#ifdef _WIN64
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
mono_threads_platform_yield (void)
{
	return SwitchToThread ();
}

void
mono_threads_platform_exit (int exit_code)
{
	mono_thread_info_detach ();
	ExitThread (exit_code);
}

void
mono_threads_platform_unregister (MonoThreadInfo *info)
{
	g_assert (info->handle);

	CloseHandle (info->handle);
	info->handle = NULL;
}

int
mono_threads_get_max_stack_size (void)
{
	//FIXME
	return INT_MAX;
}

gpointer
mono_threads_platform_duplicate_handle (MonoThreadInfo *info)
{
	HANDLE thread_handle;

	g_assert (info->handle);
	DuplicateHandle (GetCurrentProcess (), info->handle, GetCurrentProcess (), &thread_handle, THREAD_ALL_ACCESS, TRUE, 0);

	return thread_handle;
}

HANDLE
mono_threads_platform_open_thread_handle (HANDLE handle, MonoNativeThreadId tid)
{
	return OpenThread (THREAD_ALL_ACCESS, TRUE, tid);
}

void
mono_threads_platform_close_thread_handle (HANDLE handle)
{
	CloseHandle (handle);
}

#if defined(_MSC_VER)
const DWORD MS_VC_EXCEPTION=0x406D1388;
#pragma pack(push,8)
typedef struct tagTHREADNAME_INFO
{
   DWORD dwType; // Must be 0x1000.
   LPCSTR szName; // Pointer to name (in user addr space).
   DWORD dwThreadID; // Thread ID (-1=caller thread).
  DWORD dwFlags; // Reserved for future use, must be zero.
} THREADNAME_INFO;
#pragma pack(pop)
#endif

void
mono_native_thread_set_name (MonoNativeThreadId tid, const char *name)
{
#if defined(_MSC_VER)
	/* http://msdn.microsoft.com/en-us/library/xcb2z8hs.aspx */
	THREADNAME_INFO info;
	info.dwType = 0x1000;
	info.szName = name;
	info.dwThreadID = tid;
	info.dwFlags = 0;

	__try {
		RaiseException( MS_VC_EXCEPTION, 0, sizeof(info)/sizeof(ULONG_PTR),       (ULONG_PTR*)&info );
	}
	__except(EXCEPTION_EXECUTE_HANDLER) {
	}
#endif
}

void
mono_threads_platform_set_exited (gpointer handle)
{
}

void
mono_threads_platform_init (void)
{
}

#endif
