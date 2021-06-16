
#include <config.h>

#ifdef HOST_WASM

#include <sys/types.h>
#if HAVE_SYS_STAT_H
#include <sys/stat.h>
#endif
#if HAVE_SYS_MMAN_H
#include <sys/mman.h>
#endif
#ifdef HAVE_SYS_SYSCTL_H
#include <sys/sysctl.h>
#endif
#include <signal.h>
#include <fcntl.h>
#include <string.h>
#include <unistd.h>
#include <stdlib.h>
#include <errno.h>

#include "mono-mmap.h"
#include "mono-mmap-internals.h"
#include "mono-proclib.h"
#include <mono/utils/mono-threads.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-counters.h>

#define BEGIN_CRITICAL_SECTION do { \
	MonoThreadInfo *__info = mono_thread_info_current_unchecked (); \
	if (__info) __info->inside_critical_region = TRUE;	\

#define END_CRITICAL_SECTION \
	if (__info) __info->inside_critical_region = FALSE;	\
} while (0)	\

static void* malloced_shared_area = NULL;

int
mono_pagesize (void)
{
	static int saved_pagesize = 0;

	if (saved_pagesize)
		return saved_pagesize;

	// Prefer sysconf () as it's signal safe.
#if defined (HAVE_SYSCONF) && defined (_SC_PAGESIZE)
	saved_pagesize = sysconf (_SC_PAGESIZE);
#else
	saved_pagesize = getpagesize ();
#endif

	return saved_pagesize;
}

int
mono_valloc_granule (void)
{
	return mono_pagesize ();
}

static int
prot_from_flags (int flags)
{
	int prot = PROT_NONE;
	/* translate the protection bits */
	if (flags & MONO_MMAP_READ)
		prot |= PROT_READ;
	if (flags & MONO_MMAP_WRITE)
		prot |= PROT_WRITE;
	if (flags & MONO_MMAP_EXEC)
		prot |= PROT_EXEC;
	return prot;
}

/**
 * mono_setmmapjit:
 * \param flag indicating whether to enable or disable the use of MAP_JIT in mmap
 *
 * Call this method to enable or disable the use of MAP_JIT to create the pages
 * for the JIT to use.   This is only needed for scenarios where Mono is bundled
 * as an App in MacOS
 */
void
mono_setmmapjit (int flag)
{
	/* Ignored on HOST_WASM */
}

void*
mono_valloc (void *addr, size_t size, int flags, MonoMemAccountType type)
{
	void *ptr;
	int mflags = 0;
	int prot = prot_from_flags (flags);

	if (!mono_valloc_can_alloc (size))
		return NULL;

	if (size == 0)
		/* emscripten throws an exception on 0 length */
		return NULL;

	mflags |= MAP_ANONYMOUS;
	mflags |= MAP_PRIVATE;

	BEGIN_CRITICAL_SECTION;
	ptr = mmap (addr, size, prot, mflags, -1, 0);
	END_CRITICAL_SECTION;

	if (ptr == MAP_FAILED)
		return NULL;

	mono_account_mem (type, (ssize_t)size);

	return ptr;
}

static GHashTable *valloc_hash;

typedef struct {
	void *addr;
	int size;
} VallocInfo;

void*
mono_valloc_aligned (size_t size, size_t alignment, int flags, MonoMemAccountType type)
{
	/* Allocate twice the memory to be able to put the block on an aligned address */
	char *mem = (char *) mono_valloc (NULL, size + alignment, flags, type);
	char *aligned;

	if (!mem)
		return NULL;

	aligned = mono_aligned_address (mem, size, alignment);

	/* The mmap implementation in emscripten cannot unmap parts of regions */
	/* Free the other two parts in when 'aligned' is freed */
	// FIXME: This doubles the memory usage
	if (!valloc_hash)
		valloc_hash = g_hash_table_new (NULL, NULL);
	VallocInfo *info = g_new0 (VallocInfo, 1);
	info->addr = mem;
	info->size = size + alignment;
	g_hash_table_insert (valloc_hash, aligned, info);

	return aligned;
}

int
mono_vfree (void *addr, size_t length, MonoMemAccountType type)
{
	VallocInfo *info = (VallocInfo*)(valloc_hash ? g_hash_table_lookup (valloc_hash, addr) : NULL);
	int res;

	if (info) {
		/*
		 * We are passed the aligned address in the middle of the mapping allocated by
		 * mono_valloc_align (), free the original mapping.
		 */
		BEGIN_CRITICAL_SECTION;
		res = munmap (info->addr, info->size);
		END_CRITICAL_SECTION;
		g_free (info);
		g_hash_table_remove (valloc_hash, addr);
	} else {
		BEGIN_CRITICAL_SECTION;
		res = munmap (addr, length);
		END_CRITICAL_SECTION;
	}

	mono_account_mem (type, -(ssize_t)length);

	return 0;
}

void*
mono_file_map (size_t length, int flags, int fd, guint64 offset, void **ret_handle)
{
	void *ptr;
	int mflags = 0;
	int prot = prot_from_flags (flags);
	/* translate the flags */
	if (flags & MONO_MMAP_PRIVATE)
		mflags |= MAP_PRIVATE;
	if (flags & MONO_MMAP_SHARED)
		mflags |= MAP_SHARED;
	if (flags & MONO_MMAP_FIXED)
		mflags |= MAP_FIXED;

	if (length == 0)
		/* emscripten throws an exception on 0 length */
		return NULL;

	BEGIN_CRITICAL_SECTION;
	ptr = mmap (0, length, prot, mflags, fd, offset);
	END_CRITICAL_SECTION;
	if (ptr == MAP_FAILED)
		return NULL;
	*ret_handle = (void*)length;
	return ptr;
}

void*
mono_file_map_error (size_t length, int flags, int fd, guint64 offset, void **ret_handle,
	const char *filepath, char **error_message)
{
	return mono_file_map (length, flags, fd, offset, ret_handle);
}

int
mono_file_unmap (void *addr, void *handle)
{
	int res;

	BEGIN_CRITICAL_SECTION;
	res = munmap (addr, (size_t)handle);
	END_CRITICAL_SECTION;

	return res;
}

int
mono_mprotect (void *addr, size_t length, int flags)
{
	return 0;
}

void*
mono_shared_area (void)
{
	if (!malloced_shared_area)
		malloced_shared_area = mono_malloc_shared_area (getpid ());
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
