/*
 * mono-dl-windows-uwp.c: UWP dl support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <Windows.h>
#include <mono/utils/mono-mmap-windows.h>

void*
mono_file_map (size_t length, int flags, int fd, guint64 offset, void **ret_handle)
{
	void *ptr;
	int mflags = 0;
	HANDLE file, mapping;
	int prot = mono_mmap_win_prot_from_flags (flags);

	mflags = FILE_MAP_READ;
	if (flags & MONO_MMAP_WRITE)
		mflags = FILE_MAP_COPY;

	file = (HANDLE) _get_osfhandle (fd);
	mapping = CreateFileMappingFromApp (file, NULL, prot, length, NULL);

	if (mapping == NULL)
		return NULL;

	ptr = MapViewOfFileFromApp (mapping, mflags, offset, length);

	if (ptr == NULL) {
		CloseHandle (mapping);
		return NULL;
	}

	*ret_handle = (void*)mapping;
	return ptr;
}

int
mono_file_unmap (void *addr, void *handle)
{
	UnmapViewOfFile (addr);
	CloseHandle ((HANDLE)handle);
	return 0;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

#ifdef _MSC_VER
// Quiet Visual Studio linker warning, LNK4221, in cases when this source file intentional ends up empty.
void __mono_win32_mono_mmap_windows_uwp_quiet_lnk4221(void) {}
#endif
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
