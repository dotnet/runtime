/*
 * util.c: Simple runtime tools for the Win32 platform
 *
 * Author:
 *   Miguel de Icaza
 *
 * (C) 2002 Ximian, Inc. (http://www.ximian.com)
 */
#include <config.h>
#include <windows.h>
#include <mono/metadata/metadata.h>
#include <mono/os/util.h>

#ifdef UNDER_CE
#undef GetModuleFileName
#define GetModuleFileName ceGetModuleFileNameA

DWORD ceGetModuleFileNameA(HMODULE hModule, char* lpFilename, DWORD nSize)
{
	DWORD res = 0;
	wchar_t* wbuff = (wchar_t*)LocalAlloc(LPTR, nSize*2);
	res = GetModuleFileNameW(hModule, wbuff, nSize);
	if (res) {
		int len = wcslen(wbuff);
		WideCharToMultiByte(CP_ACP, 0, wbuff, len, lpFilename, len, NULL, NULL);
	}
	LocalFree(wbuff);
	return res;
}
#endif

/*
 * mono_set_rootdir:
 *
 * Informs the runtime of the root directory for the Mono installation,
 * the vm_file
 */
void
mono_set_rootdir (void)
{
	char moddir[MAXPATHLEN], *bindir, *installdir, *root;

	GetModuleFileName (NULL, moddir, sizeof(moddir));
	bindir = g_path_get_dirname (moddir);
	installdir = g_path_get_dirname (bindir);
	root = g_build_path (G_DIR_SEPARATOR_S, installdir, "lib", NULL);

	mono_assembly_setrootdir (root);
	g_free (root);
	g_free (installdir);
	g_free (bindir);
}

 
