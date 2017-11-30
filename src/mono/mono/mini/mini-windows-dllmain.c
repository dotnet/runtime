/**
 * \file
 * DllMain entry point.
 *
 * (C) 2002-2003 Ximian, Inc.
 * (C) 2003-2006 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <mono/metadata/coree.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/utils/mono-threads.h>
#include "mini.h"
#include "mini-runtime.h"

#ifdef HOST_WIN32
#include <windows.h>

BOOL APIENTRY DllMain (HMODULE module_handle, DWORD reason, LPVOID reserved)
{
	if (!mono_gc_dllmain (module_handle, reason, reserved))
		return FALSE;

	switch (reason)
	{
	case DLL_PROCESS_ATTACH:
		mono_install_runtime_load (mini_init);
		break;
	case DLL_PROCESS_DETACH:
		if (coree_module_handle)
			FreeLibrary (coree_module_handle);
		break;
	case DLL_THREAD_DETACH:
		mono_thread_info_detach ();
		break;
	
	}
	return TRUE;
}
#endif

