/*
 * rawbuffer.c: Manages buffers that might have been mmapped or malloced
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <unistd.h>
#include <sys/mman.h>
#include <sys/types.h>
#include <glib.h>
#include "rawbuffer.h"

#define PAGESIZE 8192

static GHashTable *malloc_map = NULL;

void *
mono_raw_buffer_load (int fd, int is_writable, guint32 base, size_t size)
{
	size_t start, end;
	int prot = PROT_READ;
	int flags = 0;
	void *ptr, *mmap_ptr;
	
	if (is_writable){
		prot |= PROT_WRITE;
	}
	flags = MAP_PRIVATE;

	start = base & ~(PAGESIZE - 1);
	end = (base + size + PAGESIZE - 1) & ~(PAGESIZE - 1);

	/*
	 * Apparently on cygwin the mmap succedes, but not all the
	 * area is mapped in and we get segfaults later.
	 */
#ifdef __CYGWIN__
	mmap_ptr = (void *) -1;
#else
	mmap_ptr = mmap (0, end - start, prot, flags, fd, start);
#endif
	if (mmap_ptr == (void *) -1){
		ptr = g_malloc (size);
		if (ptr == NULL)
			return NULL;
		if (lseek (fd, base, 0) == (off_t) -1)
			return NULL;
		read (fd, ptr, size);
		return ptr;
	}
	if (malloc_map == NULL)
		malloc_map = g_hash_table_new (g_direct_hash, g_direct_equal);

	g_hash_table_insert (malloc_map, mmap_ptr, GINT_TO_POINTER (size));

	return ((char *)mmap_ptr) + (base - start);
}

void
mono_raw_buffer_free (void *buffer)
{
	int size, diff;
	char *base;
	
	if (!malloc_map){
		g_free (buffer);
		return;
	}

	diff = ((unsigned int) buffer) & (PAGESIZE - 1);
	base = ((char *)buffer) - diff;
	
	size = GPOINTER_TO_INT (g_hash_table_lookup (malloc_map, base));
	munmap (base, size);
}
