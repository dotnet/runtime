/**
 * \file
 * Low-level threading, windows version
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#include <mono/utils/mono-threads.h>

#if defined(USE_WINDOWS_BACKEND)

#include <glib.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/mono-threads-debug.h>
#include <mono/utils/mono-os-wait.h>
#include <mono/utils/mono-context.h>
#include <mono/utils/w32subset.h>
#include <limits.h>

enum Win32APCInfo {
	WIN32_APC_INFO_CLEARED = 0,
	WIN32_APC_INFO_ALERTABLE_WAIT_SLOT = 1 << 0,
	WIN32_APC_INFO_BLOCKING_IO_SLOT = 1 << 1,
	WIN32_APC_INFO_PENDING_INTERRUPT_SLOT = 1 << 2,
	WIN32_APC_INFO_PENDING_ABORT_SLOT = 1 << 3
};

static void
request_interrupt (gpointer thread_info, HANDLE native_thread_handle, gint32 pending_apc_slot, PAPCFUNC apc_callback, DWORD tid)
{
	/*
	* On Windows platforms, an async interrupt/abort request queues an APC
	* that needs to be processed by target thread before it can return from an
	* alertable OS wait call and complete the mono interrupt/abort request.
	* Uncontrolled queuing of APC's could flood the APC queue preventing the target thread
	* to return from its alertable OS wait call, blocking the interrupt/abort requests to complete.
	* This check makes sure that only one APC per type gets queued, preventing potential flooding
	* of the APC queue. NOTE, this code will execute regardless if targeted thread is currently in
	* an alertable wait or not. This is done to prevent races between interrupt/abort requests and
	* alertable wait calls. Threads already in an alertable wait should handle WAIT_IO_COMPLETION
	* return scenarios and restart the alertable wait operation if needed or take other actions
	* (like service the interrupt/abort request).
	*/
	MonoThreadInfo *info = (MonoThreadInfo *)thread_info;
	gint32 old_apc_info, new_apc_info;

	do {
		old_apc_info = mono_atomic_load_i32 (&info->win32_apc_info);
		if (old_apc_info & pending_apc_slot)
			return;

		new_apc_info = old_apc_info | pending_apc_slot;
	} while (mono_atomic_cas_i32 (&info->win32_apc_info, new_apc_info, old_apc_info) != old_apc_info);

	THREADS_INTERRUPT_DEBUG ("%06d - Interrupting/Aborting syscall in thread %06d", GetCurrentThreadId (), tid);
	QueueUserAPC (apc_callback, native_thread_handle, (ULONG_PTR)NULL);
}

static void CALLBACK
interrupt_apc (ULONG_PTR param)
{
	THREADS_INTERRUPT_DEBUG ("%06d - interrupt_apc () called", GetCurrentThreadId ());
}

void
mono_win32_interrupt_wait (PVOID thread_info, HANDLE native_thread_handle, DWORD tid)
{
	request_interrupt (thread_info, native_thread_handle, WIN32_APC_INFO_PENDING_INTERRUPT_SLOT, interrupt_apc, tid);
}

static void CALLBACK
abort_apc (ULONG_PTR param)
{
	THREADS_INTERRUPT_DEBUG ("%06d - abort_apc () called", GetCurrentThreadId ());

#if HAVE_API_SUPPORT_WIN32_CANCEL_IO || HAVE_API_SUPPORT_WIN32_CANCEL_IO_EX
	MonoThreadInfo *info = mono_thread_info_current_unchecked ();
	if (info) {
		// Check if pending interrupt is still relevant and current thread has not left alertable wait region.
		// NOTE, can only be reset by current thread, currently running this APC.
		gint32 win32_apc_info = mono_atomic_load_i32 (&info->win32_apc_info);
		if (win32_apc_info & WIN32_APC_INFO_BLOCKING_IO_SLOT) {
			// Check if current thread registered an IO handle when entering alertable wait (blocking IO call).
			// No need for CAS on win32_apc_info_io_handle since its only loaded/stored by current thread
			// currently running APC.
			HANDLE io_handle = (HANDLE)info->win32_apc_info_io_handle;
			if (io_handle != INVALID_HANDLE_VALUE) {
				// In order to break IO waits, cancel all outstanding IO requests.
				// NOTE, this is NOT a blocking call.
#if HAVE_API_SUPPORT_WIN32_CANCEL_IO
				// Start to cancel IO requests for the registered IO handle issued by current thread.
				CancelIo (io_handle);
#elif HAVE_API_SUPPORT_WIN32_CANCEL_IO_EX
				CancelIoEx (io_handle, NULL);
#endif
			}
		}
	}
#endif /* HAVE_API_SUPPORT_WIN32_CANCEL_IO || HAVE_API_SUPPORT_WIN32_CANCEL_IO_EX */
}

// Attempt to cancel sync blocking IO on abort syscall requests.
// NOTE, the effect of the canceled IO operation is unknown so the caller need
// to close used resources (file, socket) to get back to a known state. The need
// to abort blocking IO calls is normally part of doing a thread abort, then the
// thread is going away meaning that no more IO calls will be issued against the
// same resource that was part of the cancelation. Current implementation of
// .NET Framework and .NET Core currently don't support the ability to abort a thread
// blocked on sync IO calls, see https://github.com/dotnet/runtime/issues/16236.
// Since there is no solution covering all scenarios aborting blocking syscall this
// will be on best effort and there might still be a slight risk that the blocking call
// won't abort (depending on overlapped IO support for current file, socket).
static void
suspend_abort_syscall (PVOID thread_info, HANDLE native_thread_handle, DWORD tid)
{
	request_interrupt (thread_info, native_thread_handle, WIN32_APC_INFO_PENDING_ABORT_SLOT, abort_apc, tid);
}

static void
enter_alertable_wait_ex (MonoThreadInfo *info, HANDLE io_handle)
{
	// Only loaded/stored by current thread, here or in APC (also running on current thread).
	g_assert (info->win32_apc_info_io_handle == (gpointer)INVALID_HANDLE_VALUE);
	info->win32_apc_info_io_handle = io_handle;

	//Set alertable wait flag.
	mono_atomic_xchg_i32 (&info->win32_apc_info, (io_handle == INVALID_HANDLE_VALUE) ? WIN32_APC_INFO_ALERTABLE_WAIT_SLOT : WIN32_APC_INFO_BLOCKING_IO_SLOT);
}

static void
leave_alertable_wait_ex (MonoThreadInfo *info, HANDLE io_handle)
{
	// Clear any previous flags. Thread is exiting alertable wait region, and info around pending interrupt/abort APC's
	// can now be discarded, thread is out of wait operation and can proceed execution.
	mono_atomic_xchg_i32 (&info->win32_apc_info, WIN32_APC_INFO_CLEARED);

	// Only loaded/stored by current thread, here or in APC (also running on current thread).
	g_assert (info->win32_apc_info_io_handle == io_handle);
	info->win32_apc_info_io_handle = (gpointer)INVALID_HANDLE_VALUE;
}

void
mono_win32_enter_alertable_wait (THREAD_INFO_TYPE *info)
{
	if (info)
		enter_alertable_wait_ex (info, INVALID_HANDLE_VALUE);
}

void
mono_win32_leave_alertable_wait (THREAD_INFO_TYPE *info)
{
	if (info)
		leave_alertable_wait_ex (info, INVALID_HANDLE_VALUE);
}

void
mono_win32_enter_blocking_io_call (THREAD_INFO_TYPE *info, HANDLE io_handle)
{
	if (info)
		enter_alertable_wait_ex (info, io_handle);
}

void
mono_win32_leave_blocking_io_call (THREAD_INFO_TYPE *info, HANDLE io_handle)
{
	if (info)
		leave_alertable_wait_ex (info, io_handle);
}

void
mono_threads_suspend_init (void)
{
}

gboolean
mono_threads_suspend_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	DWORD id = mono_thread_info_get_tid (info);
	HANDLE handle;
	DWORD result;

	handle = info->native_handle;
	g_assert (handle);

	result = SuspendThread (handle);
	THREADS_SUSPEND_DEBUG ("SUSPEND %p -> %u\n", GUINT_TO_POINTER (id), result);
	if (result == (DWORD)-1) {
		if (!mono_threads_transition_abort_async_suspend (info)) {
			/* We raced with self suspend and lost so suspend can continue. */
			g_assert (mono_threads_is_hybrid_suspension_enabled ());
			info->suspend_can_continue = TRUE;
			THREADS_SUSPEND_DEBUG ("\tlost race with self suspend %p\n", mono_thread_info_get_tid (info));
			return TRUE;
		}
		THREADS_SUSPEND_DEBUG ("SUSPEND FAILED, id=%p, err=%u\n", GUINT_TO_POINTER (id), GetLastError ());
		return FALSE;
	}

	/* Suspend logic assumes thread is really suspended before continuing below. Surprisingly SuspendThread */
	/* is just an async request to the scheduler, meaning that the suspended thread can continue to run */
	/* user mode code until scheduler gets around and process the request. This will cause a thread state race */
	/* in mono's thread state machine implementation on Windows. By requesting a threads context after issuing a */
	/* suspended request, this will wait until thread is suspended and thread context has been collected */
	/* and returned. */
#if defined(MONO_HAVE_SIMD_REG_AVX) && HAVE_API_SUPPORT_WIN32_CONTEXT_XSTATE
	BYTE context_buffer [2048];
	DWORD context_buffer_len = G_N_ELEMENTS (context_buffer);
	PCONTEXT context = NULL;
	BOOL success = InitializeContext (context_buffer, CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_CONTROL | CONTEXT_XSTATE, &context, &context_buffer_len);
	success &= SetXStateFeaturesMask (context, XSTATE_MASK_AVX);
	g_assert (success == TRUE);
#else
	CONTEXT context_buffer;
	PCONTEXT context = &context_buffer;
	context->ContextFlags = CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_CONTROL;
#endif
	if (!GetThreadContext (handle, context)) {
		result = ResumeThread (handle);
		g_assert (result == 1);
		if (!mono_threads_transition_abort_async_suspend (info)) {
			/* We raced with self suspend and lost so suspend can continue. */
			g_assert (mono_threads_is_hybrid_suspension_enabled ());
			info->suspend_can_continue = TRUE;
			THREADS_SUSPEND_DEBUG ("\tlost race with self suspend %p\n", mono_thread_info_get_tid (info));
			return TRUE;
		}
		THREADS_SUSPEND_DEBUG ("SUSPEND FAILED (GetThreadContext), id=%p, err=%u\n", GUINT_TO_POINTER (id), GetLastError ());
		return FALSE;
	}

	if (!mono_threads_transition_finish_async_suspend (info)) {
		/* We raced with self-suspend and lost.  Resume the native
		 * thread.  It is still self-suspended, waiting to be resumed.
		 * So suspend can continue.
		 */
		result = ResumeThread (handle);
		g_assert (result == 1);
		info->suspend_can_continue = TRUE;
		THREADS_SUSPEND_DEBUG ("\tlost race with self suspend %p\n", GUINT_TO_POINTER (id));
		g_assert (mono_threads_is_hybrid_suspension_enabled ());
		//XXX interrupt_kernel doesn't make sense in this case as the target is not in a syscall
		return TRUE;
	}
	info->suspend_can_continue = mono_threads_get_runtime_callbacks ()->thread_state_init_from_handle (&info->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX], info, context);
	THREADS_SUSPEND_DEBUG ("thread state %p -> %u\n", GUINT_TO_POINTER (id), result);
	if (info->suspend_can_continue) {
		if (interrupt_kernel)
			suspend_abort_syscall (info, handle, id);
	} else {
		THREADS_SUSPEND_DEBUG ("FAILSAFE RESUME/2 %p -> %u\n", GUINT_TO_POINTER (id), 0);
	}

	return TRUE;
}

gboolean
mono_threads_suspend_check_suspend_result (MonoThreadInfo *info)
{
	return info->suspend_can_continue;
}

void
mono_threads_suspend_abort_syscall (MonoThreadInfo *info)
{
	DWORD id = mono_thread_info_get_tid(info);
	g_assert (info->native_handle);
	suspend_abort_syscall (info, info->native_handle, id);
}

void
mono_win32_abort_blocking_io_call (MonoThreadInfo *info)
{
#if HAVE_API_SUPPORT_WIN32_CANCEL_SYNCHRONOUS_IO
	// In case thread is blocked on sync IO preventing it from running above queued APC, cancel
	// all outputstanding sync IO for target thread. If its not blocked on a sync IO request, below
	// call will just fail and nothing will be canceled. If thread is waiting on overlapped IO,
	// the queued APC will take care of cancel specific outstanding IO requests.
	gint32 win32_apc_info = mono_atomic_load_i32 (&info->win32_apc_info);
	if (win32_apc_info & WIN32_APC_INFO_BLOCKING_IO_SLOT) {
		CancelSynchronousIo (info->native_handle);
	}
#endif
}

gboolean
mono_threads_suspend_begin_async_resume (MonoThreadInfo *info)
{
	HANDLE handle;
	DWORD result;

	handle = info->native_handle;
	g_assert (handle);

	if (info->async_target) {
#if HAVE_API_SUPPORT_WIN32_SET_THREAD_CONTEXT
		MonoContext ctx;
		CONTEXT context;
		gboolean res;

		ctx = info->thread_saved_state [ASYNC_SUSPEND_STATE_INDEX].ctx;
		mono_threads_get_runtime_callbacks ()->setup_async_callback (&ctx, info->async_target, info->user_data);
		info->async_target = NULL;
		info->user_data = NULL;

		// When using MONO_HAVE_SIMD_REG_AVX, Mono won't change YMM (read only), so no need to
		// read extended context state.
		context.ContextFlags = CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_CONTROL;

		if (!GetThreadContext (handle, &context)) {
			THREADS_SUSPEND_DEBUG ("RESUME FAILED (GetThreadContext), id=%p, err=%u\n", GUINT_TO_POINTER (mono_thread_info_get_tid (info)), GetLastError ());
			return FALSE;
		}

		mono_monoctx_to_sigctx (&ctx, &context);

		// When using MONO_HAVE_SIMD_REG_AVX, Mono won't change YMM (read only), so no need to
		// write extended context state.
		context.ContextFlags = CONTEXT_INTEGER | CONTEXT_FLOATING_POINT | CONTEXT_CONTROL;
		res = SetThreadContext (handle, &context);
		if (!res) {
			THREADS_SUSPEND_DEBUG ("RESUME FAILED (SetThreadContext), id=%p, err=%u\n", GUINT_TO_POINTER (mono_thread_info_get_tid (info)), GetLastError ());
			return FALSE;
		}
#else
		g_error ("Not implemented due to lack of SetThreadContext");
#endif
	}

	result = ResumeThread (handle);
	THREADS_SUSPEND_DEBUG ("RESUME %p -> %u\n", GUINT_TO_POINTER (mono_thread_info_get_tid (info)), result);

	return result != (DWORD)-1;
}

void
mono_threads_suspend_register (MonoThreadInfo *info)
{
	g_assert (!info->native_handle);
	info->native_handle = mono_threads_open_native_thread_handle (GetCurrentThread ());
}

void
mono_threads_suspend_free (MonoThreadInfo *info)
{
	mono_threads_close_native_thread_handle (info->native_handle);
	info->native_handle = NULL;
}

void
mono_threads_suspend_init_signals (void)
{
}

gint
mono_threads_suspend_search_alternative_signal (void)
{
	g_assert_not_reached ();
}

gint
mono_threads_suspend_get_suspend_signal (void)
{
	return -1;
}

gint
mono_threads_suspend_get_restart_signal (void)
{
	return -1;
}

gint
mono_threads_suspend_get_abort_signal (void)
{
	return -1;
}

#endif

#if defined (HOST_WIN32)

// Use default stack size on netcore.
#define MONO_WIN32_DEFAULT_NATIVE_STACK_SIZE 0

gboolean
mono_thread_platform_create_thread (MonoThreadStart thread_fn, gpointer thread_data, gsize* const stack_size, MonoNativeThreadId *tid)
{
	HANDLE result;
	DWORD thread_id;
	gsize set_stack_size = MONO_WIN32_DEFAULT_NATIVE_STACK_SIZE;

	if (stack_size && *stack_size)
		set_stack_size = *stack_size;

	result = CreateThread (NULL, set_stack_size, (LPTHREAD_START_ROUTINE) thread_fn, thread_data, 0, &thread_id);
	if (!result)
		return FALSE;

	/* A new handle is open when attaching
	 * the thread, so we don't need this one */
	CloseHandle (result);

	if (tid)
		*tid = thread_id;

	if (stack_size) {
		// TOOD: Use VirtualQuery to get correct value
		// http://stackoverflow.com/questions/2480095/thread-stack-size-on-windows-visual-c
		*stack_size = set_stack_size;
	}

	return TRUE;
}


MonoNativeThreadId
mono_native_thread_id_get (void)
{
	return GetCurrentThreadId ();
}

guint64
mono_native_thread_os_id_get (void)
{
	return (guint64)GetCurrentThreadId ();
}

gboolean
mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2)
{
	return id1 == id2;
}

gboolean
mono_native_thread_create (MonoNativeThreadId *tid, gpointer func, gpointer arg)
{
	return CreateThread (NULL, MONO_WIN32_DEFAULT_NATIVE_STACK_SIZE, (LPTHREAD_START_ROUTINE)func, arg, 0, tid) != NULL;
}

gboolean
mono_native_thread_join_handle (HANDLE thread_handle, gboolean close_handle)
{
	DWORD res = WaitForSingleObject (thread_handle, INFINITE);

	if (close_handle)
		CloseHandle (thread_handle);

	return res != WAIT_FAILED;
}

#if HAVE_API_SUPPORT_WIN32_OPEN_THREAD
gboolean
mono_native_thread_join (MonoNativeThreadId tid)
{
	HANDLE handle;

	if (!(handle = OpenThread (SYNCHRONIZE, TRUE, tid)))
		return FALSE;

	return mono_native_thread_join_handle (handle, TRUE);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_OPEN_THREAD
gboolean
mono_native_thread_join (MonoNativeThreadId tid)
{
	g_unsupported_api ("OpenThread");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif

void
mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize)
{
#if _WIN32_WINNT >= 0x0602 // Windows 8 or newer and very fast, just a few instructions, no syscall.
	ULONG_PTR low;
	ULONG_PTR high;
	GetCurrentThreadStackLimits (&low, &high);
	*staddr = (guint8*)low;
	*stsize = high - low;
#else // Win7 and older (or newer, still works, but much slower).
	MEMORY_BASIC_INFORMATION info;
	// Windows stacks are committed on demand, one page at time.
	// teb->StackBase is the top from which it grows down.
	// teb->StackLimit is committed, the lowest it has gone so far.
	// info.AllocationBase is reserved, the lowest it can go.
	//
	VirtualQuery (&info, &info, sizeof (info));
	*staddr = (guint8*)info.AllocationBase;
	// TEB starts with TIB. TIB is public, TEB is not.
	*stsize = (size_t)((NT_TIB*)NtCurrentTeb ())->StackBase - (size_t)info.AllocationBase;
#endif
}

#if SIZEOF_VOID_P == 4 && HAVE_API_SUPPORT_WIN32_IS_WOW64_PROCESS
typedef BOOL (WINAPI *LPFN_ISWOW64PROCESS) (HANDLE, PBOOL);
static gboolean is_wow64 = FALSE;
#endif

gboolean
mono_thread_platform_external_eventloop_keepalive_check (void)
{
	/* We don't support thread creation with an external eventloop on WIN32: when the thread start
	   function returns, the thread is done.
	*/
	return FALSE;
}

/* We do this at init time to avoid potential races with module opening */
void
mono_threads_platform_init (void)
{
#if SIZEOF_VOID_P == 4 && HAVE_API_SUPPORT_WIN32_IS_WOW64_PROCESS
	LPFN_ISWOW64PROCESS is_wow64_func = (LPFN_ISWOW64PROCESS) GetProcAddress (GetModuleHandle (TEXT ("kernel32")), "IsWow64Process");
	if (is_wow64_func)
		is_wow64_func (GetCurrentProcess (), &is_wow64);
#endif
}

static gboolean
thread_is_cooperative_suspend_aware (MonoThreadInfo *info)
{
	return (mono_threads_is_cooperative_suspension_enabled () || mono_atomic_load_i32 (&(info->coop_aware_thread)));
}

/*
 * When running x86 process under x64 system syscalls are done through WoW64. This
 * needs to do a transition from x86 mode to x64 so it can syscall into the x64 system.
 * Apparently this transition invalidates the ESP that we would get from calling
 * GetThreadContext, so we would fail to scan parts of the thread stack. We attempt
 * to query whether the thread is in such a transition so we try to restart it later.
 * We check CONTEXT_EXCEPTION_ACTIVE for this, which is highly undocumented.
 */
gboolean
mono_threads_platform_in_critical_region (THREAD_INFO_TYPE *info)
{
	gboolean ret = FALSE;
#if SIZEOF_VOID_P == 4 && HAVE_API_SUPPORT_WIN32_IS_WOW64_PROCESS && HAVE_API_SUPPORT_WIN32_OPEN_THREAD
/* FIXME On cygwin these are not defined */
#if defined(CONTEXT_EXCEPTION_REQUEST) && defined(CONTEXT_EXCEPTION_REPORTING) && defined(CONTEXT_EXCEPTION_ACTIVE)
	if (is_wow64 && thread_is_cooperative_suspend_aware (info)) {
		/* Cooperative suspended threads will block at well-defined locations. */
		return FALSE;
	} else if (is_wow64 && mono_threads_is_hybrid_suspension_enabled ()) {
		/* If thread is cooperative suspended, we shouldn't validate context */
		/* since thread could still be running towards it's wait state. Calling */
		/* GetThreadContext on a running thread (before calling SuspendThread) */
		/* could return incorrect results and since cooperative suspended threads */
		/* will block at well defined-locations, no need to do so. */
		int thread_state = mono_thread_info_current_state (info);
		if (thread_state == STATE_SELF_SUSPENDED || thread_state == STATE_BLOCKING_SELF_SUSPENDED)
			return FALSE;
	}

	if (is_wow64) {
		HANDLE handle = OpenThread (THREAD_ALL_ACCESS, FALSE, mono_thread_info_get_tid (info));
		if (handle) {
			CONTEXT context;
			ZeroMemory (&context, sizeof (CONTEXT));
			context.ContextFlags = CONTEXT_EXCEPTION_REQUEST;
			if (GetThreadContext (handle, &context)) {
				if ((context.ContextFlags & CONTEXT_EXCEPTION_REPORTING) &&
						(context.ContextFlags & CONTEXT_EXCEPTION_ACTIVE))
					ret = TRUE;
			}
			CloseHandle (handle);
		}
	}
#endif
#endif
	return ret;
}

gboolean
mono_threads_platform_yield (void)
{
	return SwitchToThread ();
}

void
mono_threads_platform_exit (gsize exit_code)
{
	ExitThread ((DWORD)exit_code);
}

int
mono_thread_info_get_system_max_stack_size (void)
{
	//FIXME
	return INT_MAX;
}

void
mono_memory_barrier_process_wide (void)
{
	FlushProcessWriteBuffers ();
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_threads_windows);

#endif
