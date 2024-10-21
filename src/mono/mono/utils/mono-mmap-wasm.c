
#include <config.h>

#ifdef HOST_WASM

#include <sys/types.h>
#if HAVE_SYS_STAT_H
#include <sys/stat.h>
#endif
#if HAVE_SYS_MMAN_H
#include <sys/mman.h>
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
#include <mono/utils/options.h>

#define BEGIN_CRITICAL_SECTION do { \
	MonoThreadInfo *__info = mono_thread_info_current_unchecked (); \
	if (__info) __info->inside_critical_region = TRUE;	\

#define END_CRITICAL_SECTION \
	if (__info) __info->inside_critical_region = FALSE;	\
} while (0)	\

int
mono_pagesize (void)
{
	return 16384;
}

int
mono_valloc_granule (void)
{
	return mono_pagesize ();
}

static int
prot_from_flags (int flags)
{
#if HOST_WASI
	// The mmap in wasi-sdk rejects PROT_NONE, but otherwise disregards the flags
	// We just need to pass an acceptable value
	return PROT_READ;
#endif

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
mono_valloc (void *addr, size_t length, int flags, MonoMemAccountType type)
{
	g_assert (addr == NULL);
	return mono_valloc_aligned (length, mono_pagesize (), flags, type);
}

void*
mono_valloc_aligned (size_t size, size_t alignment, int flags, MonoMemAccountType type)
{
#ifdef DISABLE_THREADS
	void *old_sbrk = NULL;
	if ((flags & MONO_MMAP_NOZERO) == 0) {
		old_sbrk = sbrk (0);
	}
#endif

	void *res = NULL;
	if (posix_memalign (&res, alignment, size))
		return NULL;

#ifdef DISABLE_THREADS
	if ((flags & MONO_MMAP_NOZERO) == 0 && old_sbrk > res) {
		// this means that we got an old block, not the new block from sbrk
		memset (res, 0, size);
	}
#else
	if ((flags & MONO_MMAP_NOZERO) == 0) {
		memset (res, 0, size);
	}
#endif

	mono_account_mem (type, (ssize_t)size);

	return res;
}

int
mono_vfree (void *addr, size_t length, MonoMemAccountType type)
{
	// NOTE: this doesn't implement partial freeing like munmap does
	// we set MS_BLOCK_ALLOC_NUM to 1 to avoid partial freeing
	g_free (addr);

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
	if (flags & MONO_MMAP_DISCARD) {
		memset (addr, 0, length);
	}
	return 0;
}

#endif
