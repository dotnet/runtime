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
#include <errno.h>
#ifdef USE_WIN32_API
#include <winsock2.h>
#include <windows.h>
#include <io.h>
#else
#include <sys/mman.h>
#endif
#include <sys/types.h>
#include <glib.h>
#include "rawbuffer.h"

#include <mono/io-layer/io-layer.h>

#define ROUND_DOWN(VALUE,SIZE)	((VALUE) & ~((SIZE) - 1))
#define ROUND_UP(VALUE,SIZE)	(ROUND_DOWN((VALUE) + (SIZE) - 1, (SIZE)))
#if SIZEOF_VOID_P == 8
#define UINTPTR_TYPE guint64
#else
#define UINTPTR_TYPE guint32
#endif

static GHashTable *mmap_map = NULL;
static size_t alignment = 0;
#define mono_mmap_lock() EnterCriticalSection (&mmap_mutex)
#define mono_mmap_unlock() LeaveCriticalSection (&mmap_mutex)
static CRITICAL_SECTION mmap_mutex;
static gboolean make_unreadable = FALSE;
static guint32 n_pagefaults = 0;

static void
get_alignment (void)
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

void
mono_raw_buffer_init (void)
{
	InitializeCriticalSection (&mmap_mutex);

	get_alignment ();

	mmap_map = g_hash_table_new (NULL, NULL);
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

	mono_mmap_lock ();
	g_hash_table_insert (mmap_map, ptr, GINT_TO_POINTER (mapping));
	mono_mmap_unlock ();
	
	return ((char *)ptr) + (base - start);

#else
	/* mmap implementation */


	size_t start, end;
	int prot = PROT_READ;
	int flags = 0;
	void *ptr;

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

	/* 
	 * This seems to prevent segmentation faults on Fedora Linux, no
	 * idea why :). See
	 * http://bugzilla.ximian.com/show_bug.cgi?id=49499
	 * for more info.
	 */
	if (mprotect (ptr, end - start, prot | PROT_EXEC) != 0)
		g_warning (G_GNUC_PRETTY_FUNCTION
				   ": mprotect failed: %s", g_strerror (errno));

	if (make_unreadable) {
		int res = mprotect (ptr, end - start, 0);
		g_assert (res == 0);
	}

	mono_mmap_lock ();
	g_hash_table_insert (mmap_map, ptr, GINT_TO_POINTER (size));
	mono_mmap_unlock ();

	return ((char *)ptr) + (base - start);
#endif
}

static void
mono_raw_buffer_free_mmap (void *base)
{
	int value;

	mono_mmap_lock ();
	value = GPOINTER_TO_INT (g_hash_table_lookup (mmap_map, base));
	mono_mmap_unlock ();

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
	gboolean exists;

	mmap_base =  (gpointer)(ROUND_DOWN ((UINTPTR_TYPE) (buffer), alignment));

	mono_mmap_lock ();
	exists = g_hash_table_lookup (mmap_map, mmap_base) != NULL;
	mono_mmap_unlock ();
	if (exists)
		mono_raw_buffer_update_mmap (mmap_base, size);
}

void
mono_raw_buffer_free (void *buffer)
{
	char *mmap_base;
	gboolean exists;

	mmap_base = (gpointer)(ROUND_DOWN ((UINTPTR_TYPE) (buffer), alignment));
	
	exists = g_hash_table_lookup (mmap_map, mmap_base) != NULL;
	if (exists)
		mono_raw_buffer_free_mmap (mmap_base);
	else
		mono_raw_buffer_free_malloc (buffer);
}

/*
 * mono_raw_buffer_set_make_unreadable:
 *
 *   Set whenever to make all mmaped memory unreadable. In conjuction with a
 * SIGSEGV handler, this is useful to find out which pages the runtime tries to read.
 */
void
mono_raw_buffer_set_make_unreadable (gboolean unreadable)
{
	make_unreadable = unreadable;
}

typedef struct {
	gboolean found;
	void *ptr;
} FindMapUserData;

static void
find_map (void *start, guint32 size, gpointer user_data)
{
	FindMapUserData *data = (FindMapUserData*)user_data;

	if (!data->found)
		if (((guint8*)data->ptr >= (guint8*)start) && ((guint8*)data->ptr < (guint8*)start + size))
			data->found = TRUE;
}

/*
 * mono_raw_buffer_is_pagefault:
 *
 *   Should be called from a SIGSEGV signal handler to find out whenever @ptr is
 * within memory allocated by this module.
 */
gboolean
mono_raw_buffer_is_pagefault (void *ptr)
{
	FindMapUserData data;

	if (!make_unreadable)
		return FALSE;

	data.found = FALSE;
	data.ptr = ptr;

	mono_mmap_lock ();
	g_hash_table_foreach (mmap_map, (GHFunc)find_map, &data);
	mono_mmap_unlock ();

	return data.found;
}

/*
 * mono_raw_buffer_handle_pagefault:
 *
 *   Handle a pagefault caused by an unreadable page by making it readable again.
 */
void
mono_raw_buffer_handle_pagefault (void *ptr)
{
#ifndef PLATFORM_WIN32
	guint8* start = (guint8*)ROUND_DOWN (((gssize)ptr), alignment);
	int res;

	mono_mmap_lock ();
	res = mprotect (start, alignment, PROT_READ);
	g_assert (res == 0);

	n_pagefaults ++;
	mono_mmap_unlock ();
#endif
}

/*
 * mono_raw_buffer_get_n_pagefaults:
 *
 *   Return the number of times handle_pagefault is called.
 * To count the number of pagefaults caused by a block of code use code like this:
 * 
 *  int prev_pagefaults = mono_raw_buffer_get_n_pagefaults ();
 *  <CODE>
 *  int new_pagefaults = mono_raw_buffer_get_n_pagefaults () - prev_pagefaults;
 */
guint32
mono_raw_buffer_get_n_pagefaults (void)
{
	return n_pagefaults;
}
