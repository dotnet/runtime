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
#include <mono/utils/checked-build.h>
#include <mono/utils/w32subset.h>

/* Empty handler only used to detect interrupt state of current thread. */
/* Needed in order to correctly avoid entering wait methods under */
/* cooperative suspend of a thread. Under preemptive suspend a thread gets */
/* a queued APC as part of an alertable suspend request. The APC will break any wait's */
/* done by any of below methods. In hybrid suspend, if a thread gets into a GC safe area */
/* thread will be preemptive suspend as above and an APC will be queued, breaking any wait. */
/* If the thread is not within a GC safe area, a cooperative suspend will be used, but that */
/* won't queue an APC to the thread, so in cases where we enter a GC safe area and a wait */
/* using below functions, that wait won't be alerted. This could be solved using */
/* interrupt handlers. Problem with interrupt handlers on Windows together with APC is race */
/* between thread executing interrupt handler and current thread. We will need the thread */
/* alive when posting the APC, but since there is no synchronization between waiting thread */
/* and thread running interrupt handler, waiting thread could already be terminated when executing */
/* interrupt handler. There are ways to mitigate this, but using below schema is more lightweight and */
/* solves the same problem + gives some additional benefits on preemptive suspend. Wait methods */
/* will register a empty interrupt handler. This is needed in order to correctly get current alertable */
/* state of the thread when register/unregister handler. If thread is already interrupted, we can */
/* ignore call to wait method and return alertable error code. This solves the cooperative suspend */
/* scenario since we evaluate the current interrupt state inside GC safe block. If not yet interrupted, */
/* cooperative suspend will detect that thread is inside a GC safe block so it will interrupt kernel */
/* as part of suspend request (similar to preemptive suspend) queuing APC, breaking any waits. */
static void
win32_wait_interrupt_handler (gpointer ignored)
{
}

/* Evaluate if we have a pending interrupt on current thread before */
/* entering wait. If thread has been cooperative suspended, it won't */
/* always queue an APC (only when already in a GC safe block), but since */
/* we should be inside a GC safe block at this point, checking current */
/* thread's interrupt state will tell us if we have a pending interrupt. */
/* If not, we will get an APC queued to break any waits if interrupted */
/* after this check (both in cooperative and preemptive suspend modes). */
#define WIN32_CHECK_INTERRUPT(info, alertable) \
	do { \
		MONO_REQ_GC_SAFE_MODE; \
		if (alertable && info && mono_thread_info_is_interrupt_state (info)) { \
			SetLastError (WAIT_IO_COMPLETION); \
			return WAIT_IO_COMPLETION; \
		} \
	} while (0)

#define WIN32_ENTER_ALERTABLE_WAIT(info) \
	do { \
		if (info) { \
			gboolean alerted = FALSE; \
			mono_thread_info_install_interrupt (win32_wait_interrupt_handler, NULL, &alerted); \
			if (alerted) { \
				SetLastError (WAIT_IO_COMPLETION); \
				return WAIT_IO_COMPLETION; \
			} \
			mono_win32_enter_alertable_wait (info); \
		} \
	} while (0)

#define WIN32_LEAVE_ALERTABLE_WAIT(info) \
	do { \
		if (info) { \
			gboolean alerted = FALSE; \
			mono_win32_leave_alertable_wait (info); \
			mono_thread_info_uninstall_interrupt (&alerted); \
		} \
	} while (0)

static DWORD
win32_sleep_ex_interrupt_checked (MonoThreadInfo *info, DWORD timeout, BOOL alertable)
{
	WIN32_CHECK_INTERRUPT (info, alertable);
	return SleepEx (timeout, alertable);
}

static DWORD
win32_sleep_ex (DWORD timeout, BOOL alertable, BOOL cooperative)
{
	DWORD result = WAIT_FAILED;
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	WIN32_ENTER_ALERTABLE_WAIT (info);

	if (cooperative) {
		MONO_ENTER_GC_SAFE;
		result = win32_sleep_ex_interrupt_checked (info, timeout, alertable);
		MONO_EXIT_GC_SAFE;
	} else {
		result = win32_sleep_ex_interrupt_checked (info, timeout, alertable);
	}

	WIN32_LEAVE_ALERTABLE_WAIT (info);

	return result;
}

DWORD
mono_win32_sleep_ex (DWORD timeout, BOOL alertable)
{
	return win32_sleep_ex (timeout, alertable, FALSE);
}

DWORD
mono_coop_win32_sleep_ex (DWORD timeout, BOOL alertable)
{
	return win32_sleep_ex (timeout, alertable, TRUE);
}

static DWORD
win32_wait_for_single_object_ex_interrupt_checked (MonoThreadInfo *info, HANDLE handle, DWORD timeout, BOOL alertable)
{
	WIN32_CHECK_INTERRUPT (info, alertable);
	return WaitForSingleObjectEx (handle, timeout, alertable);
}

static DWORD
win32_wait_for_single_object_ex (HANDLE handle, DWORD timeout, BOOL alertable, BOOL cooperative)
{
	DWORD result = WAIT_FAILED;
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	WIN32_ENTER_ALERTABLE_WAIT (info);

	if (cooperative) {
		MONO_ENTER_GC_SAFE;
		result = win32_wait_for_single_object_ex_interrupt_checked (info, handle, timeout, alertable);
		MONO_EXIT_GC_SAFE;
	} else {
		result = win32_wait_for_single_object_ex_interrupt_checked (info, handle, timeout, alertable);
	}

	WIN32_LEAVE_ALERTABLE_WAIT (info);

	return result;
}

DWORD
mono_win32_wait_for_single_object_ex (HANDLE handle, DWORD timeout, BOOL alertable)
{
	return win32_wait_for_single_object_ex (handle, timeout, alertable, FALSE);
}

DWORD
mono_coop_win32_wait_for_single_object_ex (HANDLE handle, DWORD timeout, BOOL alertable)
{
	return win32_wait_for_single_object_ex (handle, timeout, alertable, TRUE);
}

static DWORD
win32_wait_for_multiple_objects_ex_interrupt_checked (MonoThreadInfo *info, DWORD count, CONST HANDLE *handles, BOOL waitAll, DWORD timeout, BOOL alertable)
{
	WIN32_CHECK_INTERRUPT (info, alertable);
	return WaitForMultipleObjectsEx (count, handles, waitAll, timeout, alertable);
}

static DWORD
win32_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, BOOL waitAll, DWORD timeout, BOOL alertable, MonoError *error, BOOL cooperative)
{
	DWORD result = WAIT_FAILED;
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	WIN32_ENTER_ALERTABLE_WAIT (info);

	if (cooperative) {
		MONO_ENTER_GC_SAFE;
		result = win32_wait_for_multiple_objects_ex_interrupt_checked (info, count, handles, waitAll, timeout, alertable);
		MONO_EXIT_GC_SAFE;
	} else {
		result = win32_wait_for_multiple_objects_ex_interrupt_checked (info, count, handles, waitAll, timeout, alertable);
	}

	WIN32_LEAVE_ALERTABLE_WAIT (info);

	// This is not perfect, but it is the best you can do in usermode and matches CoreCLR.
	// i.e. handle-based instead of object-based.

	if (result == WAIT_FAILED && waitAll && error &&
			count > 1 && count <= MAXIMUM_WAIT_OBJECTS
			&& GetLastError () == ERROR_INVALID_PARAMETER) {
		gpointer handles_sorted [MAXIMUM_WAIT_OBJECTS]; // 64
		memcpy (handles_sorted, handles, count * sizeof (handles [0]));
		mono_qsort (handles_sorted, count, sizeof (handles_sorted [0]), g_direct_equal);
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

DWORD
mono_win32_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, BOOL waitAll, DWORD timeout, BOOL alertable, MonoError *error)
{
	return win32_wait_for_multiple_objects_ex (count, handles, waitAll, timeout, alertable, error, FALSE);
}

DWORD
mono_coop_win32_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, BOOL waitAll, DWORD timeout, BOOL alertable, MonoError *error)
{
	return win32_wait_for_multiple_objects_ex (count, handles, waitAll, timeout, alertable, error, TRUE);
}

#if HAVE_API_SUPPORT_WIN32_SIGNAL_OBJECT_AND_WAIT
static DWORD
win32_signal_object_and_wait_interrupt_checked (MonoThreadInfo *info, HANDLE toSignal, HANDLE toWait, DWORD timeout, BOOL alertable)
{
	WIN32_CHECK_INTERRUPT (info, alertable);
	return SignalObjectAndWait (toSignal, toWait, timeout, alertable);
}

static DWORD
win32_signal_object_and_wait (HANDLE toSignal, HANDLE toWait, DWORD timeout, BOOL alertable, BOOL cooperative)
{
	DWORD result = WAIT_FAILED;
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	WIN32_ENTER_ALERTABLE_WAIT (info);

	if (cooperative) {
		MONO_ENTER_GC_SAFE;
		result = win32_signal_object_and_wait_interrupt_checked (info, toSignal, toWait, timeout, alertable);
		MONO_EXIT_GC_SAFE;
	} else {
		result = win32_signal_object_and_wait_interrupt_checked (info, toSignal, toWait, timeout, alertable);
	}

	WIN32_LEAVE_ALERTABLE_WAIT (info);

	return result;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SIGNAL_OBJECT_AND_WAIT
static DWORD
win32_signal_object_and_wait (HANDLE toSignal, HANDLE toWait, DWORD timeout, BOOL alertable, BOOL cooperative)
{
	g_unsupported_api ("SignalObjectAndWait");
	SetLastError (ERROR_NOT_SUPPORTED);
	return WAIT_FAILED;
}
#endif /* HAVE_API_SUPPORT_WIN32_SIGNAL_OBJECT_AND_WAIT */

DWORD
mono_win32_signal_object_and_wait (HANDLE toSignal, HANDLE toWait, DWORD timeout, BOOL alertable)
{
	return win32_signal_object_and_wait (toSignal, toWait, timeout, alertable, FALSE);
}

DWORD
mono_coop_win32_signal_object_and_wait (HANDLE toSignal, HANDLE toWait, DWORD timeout, BOOL alertable)
{
	return win32_signal_object_and_wait (toSignal, toWait, timeout, alertable, TRUE);
}

#if HAVE_API_SUPPORT_WIN32_MSG_WAIT_FOR_MULTIPLE_OBJECTS
static DWORD
win32_msg_wait_for_multiple_objects_ex_interrupt_checked (MonoThreadInfo *info, DWORD count, CONST HANDLE *handles, DWORD timeout, DWORD wakeMask, DWORD flags, BOOL alertable)
{
	WIN32_CHECK_INTERRUPT (info, alertable);
	return MsgWaitForMultipleObjectsEx (count, handles, timeout, wakeMask, flags);
}

static DWORD
win32_msg_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, DWORD timeout, DWORD wakeMask, DWORD flags, BOOL cooperative)
{
	DWORD result = WAIT_FAILED;
	BOOL alertable = flags & MWMO_ALERTABLE;
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	WIN32_ENTER_ALERTABLE_WAIT (info);

	if (cooperative) {
		MONO_ENTER_GC_SAFE;
		result = win32_msg_wait_for_multiple_objects_ex_interrupt_checked (info, count, handles, timeout, wakeMask, flags, alertable);
		MONO_EXIT_GC_SAFE;
	} else {
		result = win32_msg_wait_for_multiple_objects_ex_interrupt_checked (info, count, handles, timeout, wakeMask, flags, alertable);
	}

	WIN32_LEAVE_ALERTABLE_WAIT (info);

	return result;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_MSG_WAIT_FOR_MULTIPLE_OBJECTS
static DWORD
win32_msg_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, DWORD timeout, DWORD wakeMask, DWORD flags, BOOL cooperative)
{
	g_unsupported_api ("MsgWaitForMultipleObjectsEx");
	SetLastError (ERROR_NOT_SUPPORTED);
	return WAIT_FAILED;
}
#endif /* HAVE_API_SUPPORT_WIN32_MSG_WAIT_FOR_MULTIPLE_OBJECTS */

DWORD
mono_win32_msg_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, DWORD timeout, DWORD wakeMask, DWORD flags)
{
	return win32_msg_wait_for_multiple_objects_ex (count, handles, timeout, wakeMask, flags, FALSE);
}

DWORD
mono_coop_win32_msg_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, DWORD timeout, DWORD wakeMask, DWORD flags)
{
	return win32_msg_wait_for_multiple_objects_ex (count, handles, timeout, wakeMask, flags, TRUE);
}

static DWORD
win32_wsa_wait_for_multiple_events_interrupt_checked (MonoThreadInfo *info, DWORD count, const WSAEVENT FAR *handles, BOOL waitAll, DWORD timeout, BOOL alertable)
{
	WIN32_CHECK_INTERRUPT (info, alertable);
	return WSAWaitForMultipleEvents (count, handles, waitAll, timeout, alertable);
}

static DWORD
win32_wsa_wait_for_multiple_events (DWORD count, const WSAEVENT FAR *handles, BOOL waitAll, DWORD timeout, BOOL alertable, BOOL cooperative)
{
	DWORD result = WAIT_FAILED;
	MonoThreadInfo * const info = alertable ? mono_thread_info_current_unchecked () : NULL;

	WIN32_ENTER_ALERTABLE_WAIT (info);

	if (cooperative) {
		MONO_ENTER_GC_SAFE;
		result = win32_wsa_wait_for_multiple_events_interrupt_checked (info, count, handles, waitAll, timeout, alertable);
		MONO_EXIT_GC_SAFE;
	} else {
		result = win32_wsa_wait_for_multiple_events_interrupt_checked (info, count, handles, waitAll, timeout, alertable);
	}

	WIN32_LEAVE_ALERTABLE_WAIT (info);

	return result;
}

DWORD
mono_win32_wsa_wait_for_multiple_events (DWORD count, const WSAEVENT FAR *handles, BOOL waitAll, DWORD timeout, BOOL alertable)
{
	return win32_wsa_wait_for_multiple_events (count, handles, waitAll, timeout, alertable, FALSE);
}

DWORD
mono_coop_win32_wsa_wait_for_multiple_events (DWORD count, const WSAEVENT FAR *handles, BOOL waitAll, DWORD timeout, BOOL alertable)
{
	return win32_wsa_wait_for_multiple_events (count, handles, waitAll, timeout, alertable, TRUE);
}
