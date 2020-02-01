/**
 * \file
 * DllMain entry point.
 *
 * (C) 2002-2003 Ximian, Inc.
 * (C) 2003-2006 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mini-runtime.h"

#ifdef HOST_WIN32
#include "mini-windows.h"
#include <windows.h>

MONO_EXTERN_C
BOOL APIENTRY DllMain (HMODULE module_handle, DWORD reason, LPVOID reserved);

MONO_EXTERN_C
BOOL APIENTRY DllMain (HMODULE module_handle, DWORD reason, LPVOID reserved)
{
	return mono_win32_runtime_tls_callback (module_handle, reason, reserved, MONO_WIN32_TLS_CALLBACK_TYPE_DLL);
}
#endif
