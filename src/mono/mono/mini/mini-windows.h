/**
 * \file
 */

#ifndef __MONO_MINI_WINDOWS_H__
#define __MONO_MINI_WINDOWS_H__

#include <config.h>
#include <glib.h>

#ifdef HOST_WIN32
#include "windows.h"
#include "mini.h"
#include "mono/utils/mono-context.h"

gboolean
mono_setup_thread_context(DWORD thread_id, MonoContext *mono_context);

typedef enum {
	MONO_WIN32_TLS_CALLBACK_TYPE_NONE,
	MONO_WIN32_TLS_CALLBACK_TYPE_DLL,
	MONO_WIN32_TLS_CALLBACK_TYPE_LIB
} MonoWin32TLSCallbackType;

gboolean
mono_win32_handle_tls_callback_type (MonoWin32TLSCallbackType);

BOOL
mono_win32_runtime_tls_callback (HMODULE module_handle, DWORD reason, LPVOID reserved, MonoWin32TLSCallbackType callback_type);

#endif /* HOST_WIN32 */
#endif /* __MONO_MINI_WINDOWS_H__ */
