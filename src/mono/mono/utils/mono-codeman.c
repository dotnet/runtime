#include "config.h"

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <glib.h>

#include "mono-codeman.h"
#include "mono-mmap.h"
#include <mono/metadata/class-internals.h>
#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#define MIN_PAGES 16

#if defined(__ia64__) || defined(__x86_64__)
/*
 * We require 16 byte alignment on amd64 so the fp literals embedded in the code are 
 * properly aligned for SSE2.
 */
#define MIN_ALIGN 16
#else
#define MIN_ALIGN 8
#endif

/* if a chunk has less than this amount of free space it's considered full */
#define MAX_WASTAGE 32
#define MIN_BSIZE 32

#ifdef __x86_64__
#define ARCH_MAP_FLAGS MONO_MMAP_32BIT
#else
#define ARCH_MAP_FLAGS 0
#endif

#define MONO_PROT_RWX (MONO_MMAP_READ|MONO_MMAP_WRITE|MONO_MMAP_EXEC)

typedef struct _CodeChunck CodeChunk;

enum {
	CODE_FLAG_MMAP,
	CODE_FLAG_MALLOC
};

struct _CodeChunck {
	char *data;
	int pos;
	int size;
	CodeChunk *next;
	unsigned int flags: 8;
	/* this number of bytes is available to resolve addresses far in memory */
	unsigned int bsize: 24;
};

struct _MonoCodeManager {
	int dynamic;
	CodeChunk *current;
	CodeChunk *full;
};

MonoCodeManager* 
mono_code_manager_new (void)
{
	MonoCodeManager *cman = malloc (sizeof (MonoCodeManager));
	if (!cman)
		return NULL;
	cman->current = NULL;
	cman->full = NULL;
	cman->dynamic = 0;
	return cman;
}

MonoCodeManager* 
mono_code_manager_new_dynamic (void)
{
	MonoCodeManager *cman = mono_code_manager_new ();
	cman->dynamic = 1;
	return cman;
}


static void
free_chunklist (CodeChunk *chunk)
{
	CodeChunk *dead;
	
#if defined(HAVE_VALGRIND_MEMCHECK_H) && defined (VALGRIND_JIT_UNREGISTER_MAP)
	int valgrind_unregister = 0;
	if (RUNNING_ON_VALGRIND)
		valgrind_unregister = 1;
#define valgrind_unregister(x) do { if (valgrind_unregister) { VALGRIND_JIT_UNREGISTER_MAP(NULL,x); } } while (0) 
#else
#define valgrind_unregister(x)
#endif

	for (; chunk; ) {
		dead = chunk;
		chunk = chunk->next;
		if (dead->flags == CODE_FLAG_MMAP) {
			mono_vfree (dead->data, dead->size);
			/* valgrind_unregister(dead->data); */
		} else if (dead->flags == CODE_FLAG_MALLOC) {
			free (dead->data);
		}
		free (dead);
	}
}

void
mono_code_manager_destroy (MonoCodeManager *cman)
{
	free_chunklist (cman->full);
	free_chunklist (cman->current);
	free (cman);
}

/* fill all the memory with the 0x2a (42) value */
void             
mono_code_manager_invalidate (MonoCodeManager *cman)
{
	CodeChunk *chunk;

#if defined(__i386__) || defined(__x86_64__)
	int fill_value = 0xcc; /* x86 break */
#else
	int fill_value = 0x2a;
#endif

	for (chunk = cman->current; chunk; chunk = chunk->next)
		memset (chunk->data, fill_value, chunk->size);
	for (chunk = cman->full; chunk; chunk = chunk->next)
		memset (chunk->data, fill_value, chunk->size);
}

void
mono_code_manager_foreach (MonoCodeManager *cman, MonoCodeManagerFunc func, void *user_data)
{
	CodeChunk *chunk;
	for (chunk = cman->current; chunk; chunk = chunk->next) {
		if (func (chunk->data, chunk->size, chunk->bsize, user_data))
			return;
	}
	for (chunk = cman->full; chunk; chunk = chunk->next) {
		if (func (chunk->data, chunk->size, chunk->bsize, user_data))
			return;
	}
}

/* BIND_ROOM is the divisor for the chunck of code size dedicated
 * to binding branches (branches not reachable with the immediate displacement)
 * bind_size = size/BIND_ROOM;
 * we should reduce it and make MIN_PAGES bigger for such systems
 */
#if defined(__ppc__) || defined(__powerpc__)
#define BIND_ROOM 4
#endif
#if defined(__arm__)
#define BIND_ROOM 8
#endif

static CodeChunk*
new_codechunk (int dynamic, int size)
{
	int minsize, flags = CODE_FLAG_MMAP;
	int chunk_size, bsize = 0;
	int pagesize;
	CodeChunk *chunk;
	void *ptr;

#ifdef FORCE_MALLOC
	flags = CODE_FLAG_MALLOC;
#endif

	pagesize = mono_pagesize ();

	if (dynamic) {
		chunk_size = size;
		flags = CODE_FLAG_MALLOC;
	} else {
		minsize = pagesize * MIN_PAGES;
		if (size < minsize)
			chunk_size = minsize;
		else {
			chunk_size = size;
			chunk_size += pagesize - 1;
			chunk_size &= ~ (pagesize - 1);
		}
	}
#ifdef BIND_ROOM
	bsize = chunk_size / BIND_ROOM;
	if (bsize < MIN_BSIZE)
		bsize = MIN_BSIZE;
	bsize += MIN_ALIGN -1;
	bsize &= ~ (MIN_ALIGN - 1);
	if (chunk_size - size < bsize) {
		chunk_size = size + bsize;
		chunk_size += pagesize - 1;
		chunk_size &= ~ (pagesize - 1);
	}
#endif

	/* does it make sense to use the mmap-like API? */
	if (flags == CODE_FLAG_MALLOC) {
		ptr = malloc (chunk_size);
		if (!ptr)
			return NULL;
	} else {
		ptr = mono_valloc (NULL, chunk_size, MONO_PROT_RWX | ARCH_MAP_FLAGS);
		if (!ptr)
			return NULL;
	}

	if (flags == CODE_FLAG_MALLOC) {
		/*
		 * AMD64 processors maintain icache coherency only for pages which are 
		 * marked executable.
		 */
#ifndef PLATFORM_WIN32
		{
			char *page_start = (char *) (((gssize) (ptr)) & ~ (pagesize - 1));
			int pages = ((char*)ptr + chunk_size - page_start + pagesize - 1) / pagesize;
			int err = mono_mprotect (page_start, pages * pagesize, MONO_PROT_RWX);
			assert (!err);
		}
#endif

#ifdef BIND_ROOM
			/* Make sure the thunks area is zeroed */
			memset (ptr, 0, bsize);
#endif
	}

	chunk = malloc (sizeof (CodeChunk));
	if (!chunk) {
		if (flags == CODE_FLAG_MALLOC)
			free (ptr);
		else
			mono_vfree (ptr, chunk_size);
		return NULL;
	}
	chunk->next = NULL;
	chunk->size = chunk_size;
	chunk->data = ptr;
	chunk->flags = flags;
	chunk->pos = bsize;
	chunk->bsize = bsize;

	/*printf ("code chunk at: %p\n", ptr);*/
	return chunk;
}

void*
mono_code_manager_reserve (MonoCodeManager *cman, int size)
{
	CodeChunk *chunk, *prev;
	void *ptr;
	
	size += MIN_ALIGN;
	size &= ~ (MIN_ALIGN - 1);

	if (cman->dynamic) {
		++mono_stats.dynamic_code_alloc_count;
		mono_stats.dynamic_code_bytes_count += size;
	}

	if (!cman->current) {
		cman->current = new_codechunk (cman->dynamic, size);
		if (!cman->current)
			return NULL;
	}

	for (chunk = cman->current; chunk; chunk = chunk->next) {
		if (chunk->pos + size <= chunk->size) {
			ptr = chunk->data + chunk->pos;
			chunk->pos += size;
			return ptr;
		}
	}
	/* 
	 * no room found, move one filled chunk to cman->full 
	 * to keep cman->current from growing too much
	 */
	prev = NULL;
	for (chunk = cman->current; chunk; prev = chunk, chunk = chunk->next) {
		if (chunk->pos + MIN_ALIGN * 4 <= chunk->size)
			continue;
		if (prev) {
			prev->next = chunk->next;
		} else {
			cman->current = chunk->next;
		}
		chunk->next = cman->full;
		cman->full = chunk;
		break;
	}
	chunk = new_codechunk (cman->dynamic, size);
	if (!chunk)
		return NULL;
	chunk->next = cman->current;
	cman->current = chunk;
	ptr = chunk->data + chunk->pos;
	chunk->pos += size;
	return ptr;
}

/* 
 * if we reserved too much room for a method and we didn't allocate
 * already from the code manager, we can get back the excess allocation.
 */
void
mono_code_manager_commit (MonoCodeManager *cman, void *data, int size, int newsize)
{
	newsize += MIN_ALIGN;
	newsize &= ~ (MIN_ALIGN - 1);
	size += MIN_ALIGN;
	size &= ~ (MIN_ALIGN - 1);

	if (cman->current && (size != newsize) && (data == cman->current->data + cman->current->pos - size)) {
		cman->current->pos -= size - newsize;
	}
}

