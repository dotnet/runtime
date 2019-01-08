/**
* \file
* Win32 OS wait wrappers and interrupt/abort APC handling.
*
* Author:
*   Johan Lorensson (lateralusx.github@gmail.com)
*
* Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#include "mono-os-wait.h"
#include "mono-threads.h"
#include "mono-threads-debug.h"
#include "mono-logger-internals.h"
#include "mono-error-internals.h"
#include <mono/metadata/w32subset.h>

enum ThreadWaitInfo {
	THREAD_WAIT_INFO_CLEARED = 0,
	THREAD_WAIT_INFO_ALERTABLE_WAIT_SLOT = 1 << 0,
	THREAD_WAIT_INFO_PENDING_INTERRUPT_APC_SLOT = 1 << 1,
	THREAD_WAIT_INFO_PENDING_ABORT_APC_SLOT = 1 << 2
};

static inline void
request_interrupt (gpointer thread_info, HANDLE native_thread_handle, gint32 pending_apc_slot, PAPCFUNC apc_callback, DWORD tid)
{
	/*
	* On Windows platforms, an async interrupt/abort request queues an APC
	* that needs to be processed by target thread before it can return from an
	* alertable OS wait call and complete the mono interrupt/abort request.
	* Uncontrolled queuing of APC's could flood the APC queue preventing the target thread
	* to return from its alertable OS wait call, blocking the interrupt/abort requests to complete
	* This check makes sure that only one APC per type gets queued, preventing potential flooding
	* of the APC queue. NOTE, this code will execute regardless if targeted thread is currently in
	* an alertable wait or not. This is done to prevent races between interrupt/abort requests and
	* alertable wait calls. Threads already in an alertable wait should handle WAIT_IO_COMPLETION
	* return scenarios and restart the alertable wait operation if needed or take other actions
	* (like service the interrupt/abort request).
	*/
	MonoThreadInfo *info = (MonoThreadInfo *)thread_info;
	gint32 old_wait_info, new_wait_info;

	do {
		old_wait_info = mono_atomic_load_i32 (&info->thread_wait_info);
		if (old_wait_info & pending_apc_slot)
			return;

		new_wait_info = old_wait_info | pending_apc_slot;
	} while (mono_atomic_cas_i32 (&info->thread_wait_info, new_wait_info, old_wait_info) != old_wait_info);

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
	request_interrupt (thread_info, native_thread_handle, THREAD_WAIT_INFO_PENDING_INTERRUPT_APC_SLOT, interrupt_apc, tid);
}

static void CALLBACK
abort_apc (ULONG_PTR param)
{
	THREADS_INTERRUPT_DEBUG ("%06d - abort_apc () called", GetCurrentThreadId ());
}

void
mono_win32_abort_wait (PVOID thread_info, HANDLE native_thread_handle, DWORD tid)
{
	request_interrupt (thread_info, native_thread_handle, THREAD_WAIT_INFO_PENDING_ABORT_APC_SLOT, abort_apc, tid);
}

static inline void
enter_alertable_wait (MonoThreadInfo *info)
{
	// Clear any previous flags. Set alertable wait flag.
	mono_atomic_xchg_i32 (&info->thread_wait_info, THREAD_WAIT_INFO_ALERTABLE_WAIT_SLOT);
}

static inline void
leave_alertable_wait (MonoThreadInfo *info)
{
	// Clear any previous flags. Thread is exiting alertable wait state, and info around pending interrupt/abort APC's
	// can now be discarded as well, thread is out of wait operation and can proceed it's execution.
	mono_atomic_xchg_i32 (&info->thread_wait_info, THREAD_WAIT_INFO_CLEARED);
}

DWORD
mono_win32_sleep_ex (DWORD timeout, BOOL alertable)
{
	DWORD result = WAIT_FAILED;
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	if (info)
		enter_alertable_wait (info);

	result = SleepEx (timeout, alertable);

	// NOTE, leave_alertable_wait should not affect GetLastError but
	// if changed, GetLastError needs to be preserved and reset before returning.
	if (info)
		leave_alertable_wait (info);

	return result;
}

DWORD
mono_win32_wait_for_single_object_ex (HANDLE handle, DWORD timeout, BOOL alertable)
{
	DWORD result = WAIT_FAILED;
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	if (info)
		enter_alertable_wait (info);

	result = WaitForSingleObjectEx (handle, timeout, alertable);

	// NOTE, leave_alertable_wait should not affect GetLastError but
	// if changed, GetLastError needs to be preserved and reset before returning.
	if (info)
		leave_alertable_wait (info);

	return result;
}

DWORD
mono_win32_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, BOOL waitAll, DWORD timeout, BOOL alertable, MonoError *error)
{
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	if (info)
		enter_alertable_wait (info);

	DWORD const result = WaitForMultipleObjectsEx (count, handles, waitAll, timeout, alertable);

	// NOTE, leave_alertable_wait should not affect GetLastError but
	// if changed, GetLastError needs to be preserved and reset before returning.
	if (info)
		leave_alertable_wait (info);

	// This is not perfect, but it is the best you can do in usermode and matches CoreCLR.
	// i.e. handle-based instead of object-based.

	if (result == WAIT_FAILED && waitAll && error &&
			count > 1 && count <= MAXIMUM_WAIT_OBJECTS
			&& GetLastError () == ERROR_INVALID_PARAMETER) {
		gpointer handles_sorted [MAXIMUM_WAIT_OBJECTS]; // 64
		memcpy (handles_sorted, handles, count * sizeof (handles [0]));
		qsort (handles_sorted, count, sizeof (handles_sorted [0]), g_direct_equal);
		for (DWORD i = 1; i < count; ++i) {
			if (handles_sorted [i - 1] == handles_sorted [i]) {
				mono_error_set_duplicate_wait_object (error);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_HANDLE, "%s: handle %p is duplicated", __func__, handles_sorted [i]);
				// Preserve LastError, but reduce triggering write breakpoints.
				if (GetLastError () != ERROR_INVALID_PARAMETER)
					SetLastError (ERROR_INVALID_PARAMETER);
				break;
			}
		}
	}

	return result;
}

#if HAVE_API_SUPPORT_WIN32_SIGNAL_OBJECT_AND_WAIT
DWORD
mono_win32_signal_object_and_wait (HANDLE toSignal, HANDLE toWait, DWORD timeout, BOOL alertable)
{
	DWORD result = WAIT_FAILED;
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	if (info)
		enter_alertable_wait (info);

	result = SignalObjectAndWait (toSignal, toWait, timeout, alertable);

	// NOTE, leave_alertable_wait should not affect GetLastError but
	// if changed, GetLastError needs to be preserved and reset before returning.
	if (info)
		leave_alertable_wait (info);

	return result;
}

#endif /* HAVE_API_SUPPORT_WIN32_SIGNAL_OBJECT_AND_WAIT */

#if HAVE_API_SUPPORT_WIN32_MSG_WAIT_FOR_MULTIPLE_OBJECTS
DWORD
mono_win32_msg_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, DWORD timeout, DWORD wakeMask, DWORD flags)
{
	DWORD result = WAIT_FAILED;
	BOOL alertable = flags & MWMO_ALERTABLE;
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	if (info)
		enter_alertable_wait (info);

	result = MsgWaitForMultipleObjectsEx (count, handles, timeout, wakeMask, flags);

	// NOTE, leave_alertable_wait should not affect GetLastError but
	// if changed, GetLastError needs to be preserved and reset before returning.
	if (info)
		leave_alertable_wait (info);

	return result;
}
#endif /* HAVE_API_SUPPORT_WIN32_MSG_WAIT_FOR_MULTIPLE_OBJECTS */

DWORD
mono_win32_wsa_wait_for_multiple_events (DWORD count, const WSAEVENT FAR *handles, BOOL waitAll, DWORD timeout, BOOL alertable)
{
	DWORD result = WAIT_FAILED;
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	if (info)
		enter_alertable_wait (info);

	result = WSAWaitForMultipleEvents (count, handles, waitAll, timeout, alertable);

	// NOTE, leave_alertable_wait should not affect GetLastError but
	// if changed, GetLastError needs to be preserved and reset before returning.
	if (info)
		leave_alertable_wait (info);

	return result;
}
