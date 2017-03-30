/**
 * \file
 * Windows support for mapping code into the process address space
 *
 * Author:
 *   Mono Team (mono-list@lists.ximian.com)
 *
 * Copyright 2001-2008 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#if defined(HOST_WIN32)
#include <windows.h>
#include "mono/utils/mono-mmap-windows-internals.h"
#include <mono/utils/mono-counters.h>
#include <io.h>

static void *malloced_shared_area = NULL;

int
mono_pagesize (void)
{
	SYSTEM_INFO info;
	static int saved_pagesize = 0;
	if (saved_pagesize)
		return saved_pagesize;
	GetSystemInfo (&info);
	saved_pagesize = info.dwPageSize;
	return saved_pagesize;
}

int
mono_valloc_granule (void)
{
	SYSTEM_INFO info;
	static int saved_valloc_granule = 0;
	if (saved_valloc_granule)
		return saved_valloc_granule;
	GetSystemInfo (&info);
	saved_valloc_granule = info.dwAllocationGranularity;
	return saved_valloc_granule;
}

int
mono_mmap_win_prot_from_flags (int flags)
{
	int prot = flags & (MONO_MMAP_READ|MONO_MMAP_WRITE|MONO_MMAP_EXEC);
	switch (prot) {
	case 0: prot = PAGE_NOACCESS; break;
	case MONO_MMAP_READ: prot = PAGE_READONLY; break;
	case MONO_MMAP_READ|MONO_MMAP_EXEC: prot = PAGE_EXECUTE_READ; break;
	case MONO_MMAP_READ|MONO_MMAP_WRITE: prot = PAGE_READWRITE; break;
	case MONO_MMAP_READ|MONO_MMAP_WRITE|MONO_MMAP_EXEC: prot = PAGE_EXECUTE_READWRITE; break;
	case MONO_MMAP_WRITE: prot = PAGE_READWRITE; break;
	case MONO_MMAP_WRITE|MONO_MMAP_EXEC: prot = PAGE_EXECUTE_READWRITE; break;
	case MONO_MMAP_EXEC: prot = PAGE_EXECUTE; break;
	default:
		g_assert_not_reached ();
	}
	return prot;
}

void*
mono_valloc (void *addr, size_t length, int flags, MonoMemAccountType type)
{
	void *ptr;
	int mflags = MEM_RESERVE|MEM_COMMIT;
	int prot = mono_mmap_win_prot_from_flags (flags);
	/* translate the flags */

	if (!mono_valloc_can_alloc (length))
		return NULL;

	ptr = VirtualAlloc (addr, length, mflags, prot);

	account_mem (type, (ssize_t)length);

	return ptr;
}

void*
mono_valloc_aligned (size_t length, size_t alignment, int flags, MonoMemAccountType type)
{
	int prot = mono_mmap_win_prot_from_flags (flags);
	char *mem = VirtualAlloc (NULL, length + alignment, MEM_RESERVE, prot);
	char *aligned;

	if (!mem)
		return NULL;

	if (!mono_valloc_can_alloc (length))
		return NULL;

	aligned = aligned_address (mem, length, alignment);

	aligned = VirtualAlloc (aligned, length, MEM_COMMIT, prot);
	g_assert (aligned);

	account_mem (type, (ssize_t)length);

	return aligned;
}

int
mono_vfree (void *addr, size_t length, MonoMemAccountType type)
{
	MEMORY_BASIC_INFORMATION mbi;
	SIZE_T query_result = VirtualQuery (addr, &mbi, sizeof (mbi));
	BOOL res;

	g_assert (query_result);

	res = VirtualFree (mbi.AllocationBase, 0, MEM_RELEASE);

	g_assert (res);

	account_mem (type, -(ssize_t)length);

	return 0;
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
void*
mono_file_map (size_t length, int flags, int fd, guint64 offset, void **ret_handle)
{
	void *ptr;
	int mflags = 0;
	HANDLE file, mapping;
	int prot = mono_mmap_win_prot_from_flags (flags);
	/* translate the flags */
	/*if (flags & MONO_MMAP_PRIVATE)
		mflags |= MAP_PRIVATE;
	if (flags & MONO_MMAP_SHARED)
		mflags |= MAP_SHARED;
	if (flags & MONO_MMAP_ANON)
		mflags |= MAP_ANONYMOUS;
	if (flags & MONO_MMAP_FIXED)
		mflags |= MAP_FIXED;
	if (flags & MONO_MMAP_32BIT)
		mflags |= MAP_32BIT;*/

	mflags = FILE_MAP_READ;
	if (flags & MONO_MMAP_WRITE)
		mflags = FILE_MAP_COPY;

	file = (HANDLE) _get_osfhandle (fd);

	mapping = CreateFileMapping (file, NULL, prot, 0, 0, NULL);

	if (mapping == NULL)
		return NULL;

	ptr = MapViewOfFile (mapping, mflags, 0, offset, length);

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
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

int
mono_mprotect (void *addr, size_t length, int flags)
{
	DWORD oldprot;
	int prot = mono_mmap_win_prot_from_flags (flags);

	if (flags & MONO_MMAP_DISCARD) {
		VirtualFree (addr, length, MEM_DECOMMIT);
		VirtualAlloc (addr, length, MEM_COMMIT, prot);
		return 0;
	}
	return VirtualProtect (addr, length, prot, &oldprot) == 0;
}

void*
mono_shared_area (void)
{
	if (!malloced_shared_area)
		malloced_shared_area = malloc_shared_area (0);
	/* get the pid here */
	return malloced_shared_area;
}

void
mono_shared_area_remove (void)
{
	if (malloced_shared_area)
		g_free (malloced_shared_area);
	malloced_shared_area = NULL;
}

void*
mono_shared_area_for_pid (void *pid)
{
	return NULL;
}

void
mono_shared_area_unload (void *area)
{
}

int
mono_shared_area_instances (void **array, int count)
{
	return 0;
}

#endif
