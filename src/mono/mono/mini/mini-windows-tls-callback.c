/**
 * Copyright 2019 Microsoft Corporation
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mini-runtime.h"

#if defined(HOST_WIN32)
#include "mini-windows.h"
#include <windows.h>

/* Only reset by initial callback from OS loader with reason DLL_PROCESS_ATTACH */
/* both for DLL and LIB callbacks, so no need to protect this variable. */
static MonoWin32TLSCallbackType mono_win32_tls_callback_type = MONO_WIN32_TLS_CALLBACK_TYPE_NONE;

MONO_DISABLE_WARNING(4189) /* local variable is initialized but not referenced */

/* NOTE, this function needs to be in this source file to make sure linker */
/* resolve this symbol from mini-windows.c, picking up callback and */
/* included it in TLS Directory PE header. Function makes sure we only activate */
/* one of the supported mono callback types at runtime (if multiple one has been used) */
gboolean
mono_win32_handle_tls_callback_type (MonoWin32TLSCallbackType callback_type)
{
	/* Makes sure our tls callback doesn't get optimized away. */
	extern const PIMAGE_TLS_CALLBACK __mono_win32_tls_callback;
	const volatile PIMAGE_TLS_CALLBACK __tls_callback = __mono_win32_tls_callback;

	if (mono_win32_tls_callback_type == MONO_WIN32_TLS_CALLBACK_TYPE_NONE)
		mono_win32_tls_callback_type = callback_type;

	if (callback_type != mono_win32_tls_callback_type)
		return FALSE;

	return TRUE;
}

MONO_RESTORE_WARNING

VOID NTAPI mono_win32_tls_callback (PVOID module_handle, DWORD reason, PVOID reserved);

VOID NTAPI mono_win32_tls_callback (PVOID module_handle, DWORD reason, PVOID reserved)
{
	mono_win32_runtime_tls_callback ((HMODULE)module_handle, reason, reserved, MONO_WIN32_TLS_CALLBACK_TYPE_LIB);
}

/* If we are building a static library that won't have access to DllMain we can't */
/* correctly detach a thread before it terminates. The MSVC linker + runtime (also applies to MINGW) */
/* uses a set of predefined segments/sections where callbacks can be stored and called */
/* by OS loader (part of TLS Directory PE header), regardless if runtime is used */
/* as a static or dynamic library. */
#if (_MSC_VER >= 1400)
#pragma const_seg (".CRT$XLX")
extern const PIMAGE_TLS_CALLBACK __mono_win32_tls_callback = mono_win32_tls_callback;
#pragma const_seg ()
#elif defined(__MINGW64__) || (__MINGW32__)
extern const PIMAGE_TLS_CALLBACK __mono_win32_tls_callback __attribute__ ((section (".CRT$XLX"))) = mono_win32_tls_callback;
#else
#pragma message ("TLS callback support not included in .CRT$XLX segment. Static linked runtime won't add callback into PE header.")
#endif
#endif
