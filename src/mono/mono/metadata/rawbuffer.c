/*
 * rawbuffer.c: Manages buffers that might have been mmapped or malloced
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#if defined(PLATFORM_WIN32)
#define USE_WIN32_API		1
#endif

#include <unistd.h>
#ifdef USE_WIN32_API
#include <windows.h>
#include <io.h>
#else
#include <sys/mman.h>
#endif
#include <sys/types.h>
#include <glib.h>
#include "rawbuffer.h"

#define ROUND_DOWN(VALUE,SIZE)	((VALUE) & ~((SIZE) - 1))
#define ROUND_UP(VALUE,SIZE)	(ROUND_DOWN((VALUE) + (SIZE) - 1, (SIZE)))

static GHashTable *mmap_map = NULL;
static size_t alignment = 0;

static void
get_alignment ()
{
#ifdef USE_WIN32_API
	SYSTEM_INFO info;

	GetSystemInfo (&info);
	alignment = info.dwAllocationGranularity;
#else
	alignment = getpagesize ();
#endif
}

static void *
mono_raw_buffer_load_malloc (int fd, int is_writable, guint32 base, size_t size)
{
	void *ptr;

	ptr = g_malloc (size);
	if (ptr == NULL)
		return NULL;

	if (lseek (fd, base, 0) == (off_t) -1) {
		g_free (ptr);
		return NULL;
	}

	read (fd, ptr, size);
	return ptr;
}

static void
mono_raw_buffer_free_malloc (void *base)
{
	g_free (base);
}

static void *
mono_raw_buffer_load_mmap (int fd, int is_writable, guint32 base, size_t size)
{
#ifdef USE_WIN32_API
	/* FileMapping implementation */

	DWORD start, end;
	int prot, access;
	void *ptr;
	HANDLE file, mapping;

	if (alignment == 0)
		get_alignment ();
	start = ROUND_DOWN (base, alignment);
	end = base + size;
	
	if (is_writable) {
		prot = PAGE_WRITECOPY;
		access = FILE_MAP_COPY;
	}
	else {
		prot = PAGE_READONLY;
		access = FILE_MAP_READ;
	}

	file = (HANDLE) _get_osfhandle (fd);
	mapping = CreateFileMapping (file, NULL, prot, 0, 0, NULL);
	if (mapping == NULL)
		return 0;

	ptr = MapViewOfFile (mapping, access, 0, start, end - start);
	if (ptr == NULL) {
		CloseHandle (mapping);
		return 0;
	}

	if (mmap_map == NULL)
		mmap_map = g_hash_table_new (g_direct_hash, g_direct_equal);

	g_hash_table_insert (mmap_map, ptr, GINT_TO_POINTER (mapping));
	
	return ((char *)ptr) + (base - start);

#else
	/* mmap implementation */


	size_t start, end;
	int prot = PROT_READ;
	int flags = 0;
	void *ptr;

	if (alignment == 0)
		get_alignment ();
	start = ROUND_DOWN (base, alignment);
	end = ROUND_UP (base + size, alignment);

	if (is_writable){
		prot |= PROT_WRITE;
		flags = MAP_SHARED;
	} else {
		flags = MAP_PRIVATE;
	}

	ptr = mmap (0, end - start, prot, flags, fd, start);

	if (ptr == (void *) -1)
		return 0;
	
	if (mmap_map == NULL)
		mmap_map = g_hash_table_new (g_direct_hash, g_direct_equal);
	
	g_hash_table_insert (mmap_map, ptr, GINT_TO_POINTER (size));

	return ((char *)ptr) + (base - start);
#endif
}

static void
mono_raw_buffer_free_mmap (void *base)
{
	int value;

	value = GPOINTER_TO_INT (g_hash_table_lookup (mmap_map, base));

#ifdef USE_WIN32_API
	UnmapViewOfFile (base);
	CloseHandle ((HANDLE) value);
#else
	munmap (base, value);
#endif
}

static void
mono_raw_buffer_update_mmap (void *base, size_t size)
{
#ifdef USE_WIN32_API
	FlushViewOfFile (base, size);
#else
	msync (base, size, MS_SYNC);
#endif
}

void *
mono_raw_buffer_load (int fd, int is_writable, guint32 base, size_t size)
{
	void *ptr;

	ptr = mono_raw_buffer_load_mmap (fd, is_writable, base, size);
	if (ptr == 0)
		ptr = mono_raw_buffer_load_malloc (fd, is_writable, base, size);
	
	return ptr;
}

void
mono_raw_buffer_update (void *buffer, size_t size)
{
	char *mmap_base;

	mmap_base = GINT_TO_POINTER (ROUND_DOWN (GPOINTER_TO_INT (buffer), alignment));
	
	if (mmap_map && g_hash_table_lookup (mmap_map, mmap_base))
		mono_raw_buffer_update_mmap (mmap_base, size);
}

void
mono_raw_buffer_free (void *buffer)
{
	char *mmap_base;

	mmap_base = GINT_TO_POINTER (ROUND_DOWN (GPOINTER_TO_INT (buffer), alignment));
	
	if (mmap_map && g_hash_table_lookup (mmap_map, mmap_base))
		mono_raw_buffer_free_mmap (mmap_base);
	else
		mono_raw_buffer_free_malloc (buffer);
}

