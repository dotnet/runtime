#include "config.h"
#include <unistd.h>
#include <stdlib.h>
#include <string.h>

#ifdef PLATFORM_WIN32
#include <windows.h>
#include <io.h>
#else
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/mman.h>
#include <fcntl.h>
#endif

#include "mono-codeman.h"

#define MIN_PAGES 4
#define MIN_ALIGN 8
/* if a chunk has less than this amount of free space it's considered full */
#define MAX_WASTAGE 32

#ifndef MAP_ANONYMOUS
#define MAP_ANONYMOUS MAP_ANON
#endif

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
	int flags;
};

struct _MonoCodeManager {
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
	return cman;
}

static void
free_chunklist (CodeChunk *chunk)
{
	CodeChunk *dead;
	for (; chunk; ) {
		dead = chunk;
		chunk = chunk->next;
		if (dead->flags == CODE_FLAG_MMAP) {
#ifdef PLATFORM_WIN32
#else
			munmap (dead->data, dead->size);
#endif
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
	for (chunk = cman->current; chunk; chunk = chunk->next)
		memset (chunk->data, 42, chunk->size);
	for (chunk = cman->full; chunk; chunk = chunk->next)
		memset (chunk->data, 42, chunk->size);
}

static int
query_pagesize (void)
{
#ifdef PLATFORM_WIN32
	SYSTEM_INFO info;
	GetSystemInfo (&info);
	return info.dwAllocationGranularity;
#else
	return getpagesize ();
#endif
}

static CodeChunk*
new_codechunk (int size)
{
	static int pagesize = 0;
	int minsize, flags = CODE_FLAG_MMAP;
	CodeChunk *chunk;
	void *ptr;

	if (!pagesize)
		pagesize = query_pagesize ();

	minsize = pagesize * MIN_PAGES;
	if (size < minsize)
		size = minsize;
	else {
		size += pagesize - 1;
		size &= ~ (pagesize - 1);
	}
#ifdef PLATFORM_WIN32
	/* does it make sense to use the mmap-like API? */
	ptr = malloc (size);
	if (!ptr)
		return NULL;
	flags = CODE_FLAG_MALLOC;
#else
	ptr = mmap (0, size, PROT_READ|PROT_WRITE|PROT_EXEC, MAP_PRIVATE|MAP_ANONYMOUS, 0, 0);
	if (ptr == (void*)-1) {
		int fd = open ("/dev/zero", O_RDONLY);
		if (fd != -1) {
			ptr = mmap (0, size, PROT_READ|PROT_WRITE|PROT_EXEC, MAP_PRIVATE, fd, 0);
			close (fd);
		}
		if (ptr == (void*)-1) {
			ptr = malloc (size);
			if (!ptr)
				return NULL;
			flags = CODE_FLAG_MALLOC;
		}
	}
#endif

	chunk = malloc (sizeof (CodeChunk));
	if (!chunk) {
		if (flags == CODE_FLAG_MALLOC)
			free (ptr);
#ifndef PLATFORM_WIN32
		else
			munmap (ptr, size);
#endif
		return NULL;
	}
	chunk->next = NULL;
	chunk->size = size;
	chunk->data = ptr;
	chunk->flags = flags;
	chunk->pos = 0;

	return chunk;
}

void*
mono_code_manager_reserve (MonoCodeManager *cman, int size)
{
	CodeChunk *chunk, *prev;
	void *ptr;
	
	size += MIN_ALIGN;
	size &= ~ (MIN_ALIGN - 1);

	if (!cman->current) {
		cman->current = new_codechunk (size);
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
	chunk = new_codechunk (size);
	if (!chunk)
		return NULL;
	chunk->next = cman->current;
	cman->current = chunk;
	chunk->pos += size;
	return chunk->data;
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

