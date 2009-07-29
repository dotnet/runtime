#include "config.h"

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <glib.h>

/* For dlmalloc.h */
#define USE_DL_PREFIX 1

#include "mono-codeman.h"
#include "mono-mmap.h"
#include "dlmalloc.h"
#include <mono/metadata/class-internals.h>
#include <mono/metadata/profiler-private.h>
#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif
 
/*
 * AMD64 processors maintain icache coherency only for pages which are 
 * marked executable. Also, windows DEP requires us to obtain executable memory from
 * malloc when using dynamic code managers. The system malloc can't do this so we use a 
 * slighly modified version of Doug Lea's Malloc package for this purpose:
 * http://g.oswego.edu/dl/html/malloc.html
 */

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
	int read_only;
	CodeChunk *current;
	CodeChunk *full;
};

#define ALIGN_INT(val,alignment) (((val) + (alignment - 1)) & ~(alignment - 1))

/**
 * mono_code_manager_new:
 *
 * Creates a new code manager. A code manager can be used to allocate memory
 * suitable for storing native code that can be later executed.
 * A code manager allocates memory from the operating system in large chunks
 * (typically 64KB in size) so that many methods can be allocated inside them
 * close together, improving cache locality.
 *
 * Returns: the new code manager
 */
MonoCodeManager* 
mono_code_manager_new (void)
{
	MonoCodeManager *cman = malloc (sizeof (MonoCodeManager));
	if (!cman)
		return NULL;
	cman->current = NULL;
	cman->full = NULL;
	cman->dynamic = 0;
	cman->read_only = 0;
	return cman;
}

/**
 * mono_code_manager_new_dynamic:
 *
 * Creates a new code manager suitable for holding native code that can be
 * used for single or small methods that need to be deallocated independently
 * of other native code.
 *
 * Returns: the new code manager
 */
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
		mono_profiler_code_chunk_destroy ((gpointer) dead->data);
		chunk = chunk->next;
		if (dead->flags == CODE_FLAG_MMAP) {
			mono_vfree (dead->data, dead->size);
			/* valgrind_unregister(dead->data); */
		} else if (dead->flags == CODE_FLAG_MALLOC) {
			dlfree (dead->data);
		}
		free (dead);
	}
}

/**
 * mono_code_manager_destroy:
 * @cman: a code manager
 *
 * Free all the memory associated with the code manager @cman.
 */
void
mono_code_manager_destroy (MonoCodeManager *cman)
{
	free_chunklist (cman->full);
	free_chunklist (cman->current);
	free (cman);
}

/**
 * mono_code_manager_invalidate:
 * @cman: a code manager
 *
 * Fill all the memory with an invalid native code value
 * so that any attempt to execute code allocated in the code
 * manager @cman will fail. This is used for debugging purposes.
 */
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

/**
 * mono_code_manager_set_read_only:
 * @cman: a code manager
 *
 * Make the code manager read only, so further allocation requests cause an assert.
 */
void             
mono_code_manager_set_read_only (MonoCodeManager *cman)
{
	cman->read_only = TRUE;
}

/**
 * mono_code_manager_foreach:
 * @cman: a code manager
 * @func: a callback function pointer
 * @user_data: additional data to pass to @func
 *
 * Invokes the callback @func for each different chunk of memory allocated
 * in the code manager @cman.
 */
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

	if (flags == CODE_FLAG_MALLOC) {
		ptr = dlmemalign (MIN_ALIGN, chunk_size + MIN_ALIGN - 1);
		if (!ptr)
			return NULL;
	} else {
		ptr = mono_valloc (NULL, chunk_size, MONO_PROT_RWX | ARCH_MAP_FLAGS);
		if (!ptr)
			return NULL;
	}

	if (flags == CODE_FLAG_MALLOC) {
#ifdef BIND_ROOM
		/* Make sure the thunks area is zeroed */
		memset (ptr, 0, bsize);
#endif
	}

	chunk = malloc (sizeof (CodeChunk));
	if (!chunk) {
		if (flags == CODE_FLAG_MALLOC)
			dlfree (ptr);
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
	mono_profiler_code_chunk_new((gpointer) chunk->data, chunk->size);

	/*printf ("code chunk at: %p\n", ptr);*/
	return chunk;
}

/**
 * mono_code_manager_reserve:
 * @cman: a code manager
 * @size: size of memory to allocate
 * @alignment: power of two alignment value
 *
 * Allocates at least @size bytes of memory inside the code manager @cman.
 *
 * Returns: the pointer to the allocated memory or #NULL on failure
 */
void*
mono_code_manager_reserve_align (MonoCodeManager *cman, int size, int alignment)
{
	CodeChunk *chunk, *prev;
	void *ptr;

	g_assert (!cman->read_only);

	/* eventually allow bigger alignments, but we need to fix the dynamic alloc code to
	 * handle this before
	 */
	g_assert (alignment <= MIN_ALIGN);

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
		if (ALIGN_INT (chunk->pos, alignment) + size <= chunk->size) {
			chunk->pos = ALIGN_INT (chunk->pos, alignment);
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
	chunk->pos = ALIGN_INT (chunk->pos, alignment);
	ptr = chunk->data + chunk->pos;
	chunk->pos += size;
	return ptr;
}

/**
 * mono_code_manager_reserve:
 * @cman: a code manager
 * @size: size of memory to allocate
 *
 * Allocates at least @size bytes of memory inside the code manager @cman.
 *
 * Returns: the pointer to the allocated memory or #NULL on failure
 */
void*
mono_code_manager_reserve (MonoCodeManager *cman, int size)
{
	return mono_code_manager_reserve_align (cman, size, MIN_ALIGN);
}

/**
 * mono_code_manager_commit:
 * @cman: a code manager
 * @data: the pointer returned by mono_code_manager_reserve ()
 * @size: the size requested in the call to mono_code_manager_reserve ()
 * @newsize: the new size to reserve
 *
 * If we reserved too much room for a method and we didn't allocate
 * already from the code manager, we can get back the excess allocation
 * for later use in the code manager.
 */
void
mono_code_manager_commit (MonoCodeManager *cman, void *data, int size, int newsize)
{
	g_assert (newsize <= size);

	if (cman->current && (size != newsize) && (data == cman->current->data + cman->current->pos - size)) {
		cman->current->pos -= size - newsize;
	}
}

/**
 * mono_code_manager_size:
 * @cman: a code manager
 * @used_size: pointer to an integer for the result
 *
 * This function can be used to get statistics about a code manager:
 * the integer pointed to by @used_size will contain how much
 * memory is actually used inside the code managed @cman.
 *
 * Returns: the amount of memory allocated in @cman
 */
int
mono_code_manager_size (MonoCodeManager *cman, int *used_size)
{
	CodeChunk *chunk;
	guint32 size = 0;
	guint32 used = 0;
	for (chunk = cman->current; chunk; chunk = chunk->next) {
		size += chunk->size;
		used += chunk->pos;
	}
	for (chunk = cman->full; chunk; chunk = chunk->next) {
		size += chunk->size;
		used += chunk->pos;
	}
	if (used_size)
		*used_size = used;
	return size;
}

