/**
* \file
*/

#ifndef _MONO_UTILS_OS_WAIT_H_
#define _MONO_UTILS_OS_WAIT_H_

#include <config.h>
#ifdef HOST_WIN32

#include <winsock2.h>
#include <windows.h>
#include "mono-error.h"

DWORD
mono_win32_sleep_ex (DWORD timeout, BOOL alertable);

DWORD
mono_coop_win32_sleep_ex (DWORD timeout, BOOL alertable);

DWORD
mono_win32_wait_for_single_object_ex (HANDLE handle, DWORD timeout, BOOL alertable);

DWORD
mono_coop_win32_wait_for_single_object_ex (HANDLE handle, DWORD timeout, BOOL alertable);

DWORD
mono_win32_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, BOOL waitAll, DWORD timeout, BOOL alertable, MonoError *error);

DWORD
mono_coop_win32_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, BOOL waitAll, DWORD timeout, BOOL alertable, MonoError *error);

DWORD
mono_win32_signal_object_and_wait (HANDLE toSignal, HANDLE toWait, DWORD timeout, BOOL alertable);

DWORD
mono_coop_win32_signal_object_and_wait (HANDLE toSignal, HANDLE toWait, DWORD timeout, BOOL alertable);

DWORD
mono_win32_msg_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, DWORD timeout, DWORD wakeMask, DWORD flags);

DWORD
mono_coop_win32_msg_wait_for_multiple_objects_ex (DWORD count, CONST HANDLE *handles, DWORD timeout, DWORD wakeMask, DWORD flags);

DWORD
mono_win32_wsa_wait_for_multiple_events (DWORD count, const WSAEVENT FAR *handles, BOOL waitAll, DWORD timeout, BOOL alertable);

DWORD
mono_coop_win32_wsa_wait_for_multiple_events (DWORD count, const WSAEVENT FAR *handles, BOOL waitAll, DWORD timeout, BOOL alertable);

#endif

#endif /* _MONO_UTILS_OS_WAIT_H_ */
