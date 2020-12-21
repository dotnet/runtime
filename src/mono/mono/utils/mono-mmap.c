/**
 * \file
 * Support for mapping code into the process address space
 *
 * Author:
 *   Mono Team (mono-list@lists.ximian.com)
 *
 * Copyright 2001-2008 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#ifndef HOST_WIN32
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
#endif /* !HOST_WIN32 */

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

#ifndef MAP_ANONYMOUS
#define MAP_ANONYMOUS MAP_ANON
#endif

#if !defined(__APPLE__)  // returning virtual addresses <4G requires entitlement on Apple platforms, do not use it
#ifndef MAP_32BIT
#define MAP_32BIT 0
#endif
#endif

typedef struct {
	int size;
	int pid;
	int reserved;
	short stats_start;
	short stats_end;
} SAreaHeader;

void*
mono_malloc_shared_area (int pid)
{
	int size = mono_pagesize ();
	SAreaHeader *sarea = (SAreaHeader *) g_malloc0 (size);
	sarea->size = size;
	sarea->pid = pid;
	sarea->stats_start = sizeof (SAreaHeader);
	sarea->stats_end = sizeof (SAreaHeader);

	return sarea;
}

char*
mono_aligned_address (char *mem, size_t size, size_t alignment)
{
	char *aligned = (char*)((size_t)(mem + (alignment - 1)) & ~(alignment - 1));
	g_assert (aligned >= mem && aligned + size <= mem + size + alignment && !((size_t)aligned & (alignment - 1)));
	return aligned;
}

static size_t allocation_count [MONO_MEM_ACCOUNT_MAX];
static size_t total_allocation_count;
static size_t alloc_limit;

void
mono_account_mem (MonoMemAccountType type, ssize_t size)
{
	mono_atomic_fetch_add_word (&allocation_count [type], size);
	mono_atomic_fetch_add_word (&total_allocation_count, size);
}

void
mono_valloc_set_limit (size_t size)
{
	alloc_limit = size;
}

gboolean
mono_valloc_can_alloc (size_t size)
{
	if (alloc_limit)
		return (total_allocation_count + size) < alloc_limit;
	return TRUE;
}

const char*
mono_mem_account_type_name (MonoMemAccountType type)
{
	static const char *names[] = {
		"code",
		"hazard pointers",
		"domain",
		"SGen internal",
		"SGen nursery",
		"SGen LOS",
		"SGen mark&sweep",
		"SGen card table",
		"SGen shadow card table",
		"SGen debugging",
		"SGen binary protocol",
		"exceptions",
		"profiler",
		"interp stack",
		"other"
	};

	return names [type];
}

void
mono_mem_account_register_counters (void)
{
	for (int i = 0; i < MONO_MEM_ACCOUNT_MAX; ++i) {
		const char *prefix = "Valloc ";
		const char *name = mono_mem_account_type_name ((MonoMemAccountType)i);
		char descr [128];
		g_assert (strlen (prefix) + strlen (name) < sizeof (descr));
		sprintf (descr, "%s%s", prefix, name);
		mono_counters_register (descr, MONO_COUNTER_WORD | MONO_COUNTER_RUNTIME | MONO_COUNTER_BYTES | MONO_COUNTER_VARIABLE, (void*)&allocation_count [i]);
	}
}

#ifdef HOST_WIN32
// Windows specific implementation in mono-mmap-windows.c
#define HAVE_VALLOC_ALIGNED

#elif defined(HOST_WASM)
// WebAssembly implementation in mono-mmap-wasm.c
#define HAVE_VALLOC_ALIGNED

#else

static void* malloced_shared_area = NULL;
#if defined(HAVE_MMAP)

/**
 * mono_pagesize:
 * Get the page size in use on the system. Addresses and sizes in the
 * mono_mmap(), mono_munmap() and mono_mprotect() calls must be pagesize
 * aligned.
 *
 * Returns: the page size in bytes.
 */
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


	// While this could not happen in any of the Mono supported
	// systems, this ensures this function never returns -1, and
	// reduces the number of false positives
	// that Coverity finds in consumer code.

	if (saved_pagesize == -1)
		return 64*1024;

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

#if defined(__APPLE__)

#define DARWIN_VERSION_MOJAVE 18

static guint32
get_darwin_version (void)
{
	static guint32 version;

	/* This doesn't need locking */
	if (!version) {
		char str[256] = {0};
		size_t size = sizeof(str);
		int err = sysctlbyname("kern.osrelease", str, &size, NULL, 0);
		g_assert (err == 0);
		err = sscanf (str, "%d", &version);
		g_assert (err == 1);
		g_assert (version > 0);
	}
	return version;
}
#endif

static int use_mmap_jit;

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
	use_mmap_jit = flag;
}

/**
 * mono_valloc:
 * \param addr memory address
 * \param length memory area size
 * \param flags protection flags
 * Allocates \p length bytes of virtual memory with the \p flags
 * protection. \p addr can be a preferred memory address or a
 * mandatory one if MONO_MMAP_FIXED is set in \p flags.
 * \p addr must be pagesize aligned and can be NULL.
 * \p length must be a multiple of pagesize.
 * \returns NULL on failure, the address of the memory area otherwise
 */
void*
mono_valloc (void *addr, size_t length, int flags, MonoMemAccountType type)
{
	void *ptr;
	int mflags = 0;
	int prot = prot_from_flags (flags);

	if (!mono_valloc_can_alloc (length))
		return NULL;

	/* translate the flags */
	if (flags & MONO_MMAP_FIXED)
		mflags |= MAP_FIXED;
#if !defined(__APPLE__)  // returning virtual addresses <4G requires entitlement on Apple platforms, do not use it
	if (flags & MONO_MMAP_32BIT)
		mflags |= MAP_32BIT;
#endif

#ifdef HOST_WASM
	if (length == 0)
		/* emscripten throws an exception on 0 length */
		return NULL;
#endif

#if defined(__APPLE__) && defined(MAP_JIT) && defined(TARGET_OSX)
	if (get_darwin_version () >= DARWIN_VERSION_MOJAVE) {
		/* Check for hardened runtime */
		static int is_hardened_runtime;

		if (is_hardened_runtime == 0 && !use_mmap_jit) {
			ptr = mmap (NULL, getpagesize (), PROT_WRITE|PROT_EXEC, MAP_PRIVATE|MAP_ANONYMOUS, -1, 0);
			if (ptr == MAP_FAILED) {
				is_hardened_runtime = 1;
			} else {
				is_hardened_runtime = 2;
				munmap (ptr, getpagesize ());
			}
		}
		if ((flags & MONO_MMAP_JIT) && (use_mmap_jit || is_hardened_runtime == 1))
			mflags |= MAP_JIT;
#if defined(HOST_ARM64)
		/* Patching code on apple silicon seems to cause random crashes without this flag */
		/* No __builtin_available in old versions of Xcode that could be building Mono on x86 or amd64 */
		if (__builtin_available (macOS 11, *))
			mflags |= MAP_JIT;
#endif
	}
#endif

	mflags |= MAP_ANONYMOUS;
	mflags |= MAP_PRIVATE;

	BEGIN_CRITICAL_SECTION;
	ptr = mmap (addr, length, prot, mflags, -1, 0);
	if (ptr == MAP_FAILED) {
		int fd = open ("/dev/zero", O_RDONLY);
		if (fd != -1) {
			ptr = mmap (addr, length, prot, mflags, fd, 0);
			close (fd);
		}
	}
	END_CRITICAL_SECTION;

	if (ptr == MAP_FAILED)
		return NULL;

	mono_account_mem (type, (ssize_t)length);

	return ptr;
}

/**
 * mono_vfree:
 * \param addr memory address returned by mono_valloc ()
 * \param length size of memory area
 * Remove the memory mapping at the address \p addr.
 * \returns \c 0 on success.
 */
int
mono_vfree (void *addr, size_t length, MonoMemAccountType type)
{
	int res;
	BEGIN_CRITICAL_SECTION;
	res = munmap (addr, length);
	END_CRITICAL_SECTION;

	mono_account_mem (type, -(ssize_t)length);

	return res;
}

/**
 * mono_file_map:
 * \param length size of data to map
 * \param flags protection flags
 * \param fd file descriptor
 * \param offset offset in the file
 * \param ret_handle pointer to storage for returning a handle for the map
 * Map the area of the file pointed to by the file descriptor \p fd, at offset
 * \p offset and of size \p length in memory according to the protection flags
 * \p flags.
 * \p offset and \p length must be multiples of the page size.
 * \p ret_handle must point to a void*: this value must be used when unmapping
 * the memory area using \c mono_file_unmap().
 */
void*
mono_file_map (size_t length, int flags, int fd, guint64 offset, void **ret_handle)
{
	return mono_file_map_error (length, flags, fd, offset, ret_handle, NULL, NULL);
}

void*
mono_file_map_error (size_t length, int flags, int fd, guint64 offset, void **ret_handle,
	const char *filepath, char **error_message)
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
#if !defined(__APPLE__)  // returning virtual addresses <4G requires entitlement on Apple platforms, do not use it
	if (flags & MONO_MMAP_32BIT)
		mflags |= MAP_32BIT;
#endif

#ifdef HOST_WASM
	if (length == 0)
		/* emscripten throws an exception on 0 length */
		*error_message = g_stdrup_printf ("%s failed file:%s length:0x%" G_GSIZE_FORMAT "x offset:0x%" PRIu64 "X error:%s\n",
			__func__, filepath ? filepath : "", length, (guint64)offset, "mmaps of zero length are not permitted with emscripten");
		return NULL;
#endif

	// No GC safe transition because this is called early in main.c
	BEGIN_CRITICAL_SECTION;
	ptr = mmap (0, length, prot, mflags, fd, offset);
	END_CRITICAL_SECTION;
	if (ptr == MAP_FAILED) {
		if (error_message) {
			*error_message = g_strdup_printf ("%s failed file:%s length:0x%" G_GSIZE_FORMAT "X offset:0x%" PRIu64 "X error:%s(0x%X)\n",
				__func__, filepath ? filepath : "", length, (guint64)offset, g_strerror (errno), errno);
		}
		return NULL;
	}
	*ret_handle = (void*)length;
	return ptr;
}

/**
 * mono_file_unmap:
 * \param addr memory address returned by mono_file_map ()
 * \param handle handle of memory map
 * Remove the memory mapping at the address \p addr.
 * \p handle must be the value returned in ret_handle by \c mono_file_map().
 * \returns \c 0 on success.
 */
int
mono_file_unmap (void *addr, void *handle)
{
	int res;

	// No GC safe transition because this is called early in driver.c via mono_debug_init (with a few layers of indirection)
	BEGIN_CRITICAL_SECTION;
	res = munmap (addr, (size_t)handle);
	END_CRITICAL_SECTION;

	return res;
}

/**
 * mono_mprotect:
 * \param addr memory address
 * \param length size of memory area
 * \param flags new protection flags
 * Change the protection for the memory area at \p addr for \p length bytes
 * to matche the supplied \p flags.
 * If \p flags includes MON_MMAP_DISCARD the pages are discarded from memory
 * and the area is cleared to zero.
 * \p addr must be aligned to the page size.
 * \p length must be a multiple of the page size.
 * \returns \c 0 on success.
 */
int
mono_mprotect (void *addr, size_t length, int flags)
{
	int prot = prot_from_flags (flags);

	if (flags & MONO_MMAP_DISCARD) {
		/* on non-linux the pages are not guaranteed to be zeroed (*bsd, osx at least) */
#ifdef __linux__
		if (madvise (addr, length, MADV_DONTNEED))
			memset (addr, 0, length);
#else
		memset (addr, 0, length);
/* some OSes (like AIX) have madvise but no MADV_FREE */
#if defined(HAVE_MADVISE) && defined(MADV_FREE)
		madvise (addr, length, MADV_DONTNEED);
		madvise (addr, length, MADV_FREE);
#else
		posix_madvise (addr, length, POSIX_MADV_DONTNEED);
#endif
#endif
	}
	// No GC safe transition because this is called early in mini_init via mono_arch_init (with a few layers of indirection)
	return mprotect (addr, length, prot);
}

#else

/* dummy malloc-based implementation */
int
mono_pagesize (void)
{
	return 4096;
}

int
mono_valloc_granule (void)
{
	return mono_pagesize ();
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
	void *res = NULL;
	if (posix_memalign (&res, alignment, size))
		return NULL;

	memset (res, 0, size);
	return res;
}

#define HAVE_VALLOC_ALIGNED

int
mono_vfree (void *addr, size_t length, MonoMemAccountType type)
{
	g_free (addr);
	return 0;
}

int
mono_mprotect (void *addr, size_t length, int flags)
{
	if (flags & MONO_MMAP_DISCARD) {
		memset (addr, 0, length);
	}
	return 0;
}

#endif // HAVE_MMAP

#if defined(HAVE_SHM_OPEN) && !defined (DISABLE_SHARED_PERFCOUNTERS)

static int use_shared_area;

static gboolean
shared_area_disabled (void)
{
	if (!use_shared_area) {
		if (g_hasenv ("MONO_DISABLE_SHARED_AREA"))
			use_shared_area = -1;
		else
			use_shared_area = 1;
	}
	return use_shared_area == -1;
}

static int
mono_shared_area_instances_slow (void **array, int count, gboolean cleanup)
{
	int i, j = 0;
	int num;
	void *data;
	gpointer *processes = mono_process_list (&num);
	for (i = 0; i < num; ++i) {
		data = mono_shared_area_for_pid (processes [i]);
		if (!data)
			continue;
		mono_shared_area_unload (data);
		if (!cleanup) {
			if (j < count)
				array [j++] = processes [i];
			else
				break;
		}
	}
	g_free (processes);
	return j;
}

static int
mono_shared_area_instances_helper (void **array, int count, gboolean cleanup)
{
	const char *name;
	int i = 0;
	int curpid = getpid ();
	GDir *dir = g_dir_open ("/dev/shm/", 0, NULL);
	if (!dir)
		return mono_shared_area_instances_slow (array, count, cleanup);
	while ((name = g_dir_read_name (dir))) {
		int pid;
		char *nend;
		if (strncmp (name, "mono.", 5))
			continue;
		pid = strtol (name + 5, &nend, 10);
		if (pid <= 0 || nend == name + 5 || *nend)
			continue;
		if (!cleanup) {
			if (i < count)
				array [i++] = GINT_TO_POINTER (pid);
			else
				break;
		}
		if (curpid != pid && kill (pid, 0) == -1 && (errno == ESRCH || errno == ENOMEM)) {
			char buf [128];
			g_snprintf (buf, sizeof (buf), "/mono.%d", pid);
			shm_unlink (buf);
		}
	}
	g_dir_close (dir);
	return i;
}

void*
mono_shared_area (void)
{
	int fd;
	int pid = getpid ();
	/* we should allow the user to configure the size */
	int size = mono_pagesize ();
	char buf [128];
	void *res;
	SAreaHeader *header;

	if (shared_area_disabled ()) {
		if (!malloced_shared_area)
			malloced_shared_area = mono_malloc_shared_area (0);
		/* get the pid here */
		return malloced_shared_area;
	}

	/* perform cleanup of segments left over from dead processes */
	mono_shared_area_instances_helper (NULL, 0, TRUE);

	g_snprintf (buf, sizeof (buf), "/mono.%d", pid);

	fd = shm_open (buf, O_CREAT|O_EXCL|O_RDWR, S_IRUSR|S_IWUSR|S_IRGRP);
	if (fd == -1 && errno == EEXIST) {
		/* leftover */
		shm_unlink (buf);
		fd = shm_open (buf, O_CREAT|O_EXCL|O_RDWR, S_IRUSR|S_IWUSR|S_IRGRP);
	}
	/* in case of failure we try to return a memory area anyway,
	 * even if it means the data can't be read by other processes
	 */
	if (fd == -1)
		return mono_malloc_shared_area (pid);
	if (ftruncate (fd, size) != 0) {
		shm_unlink (buf);
		close (fd);
	}
	BEGIN_CRITICAL_SECTION;
	res = mmap (NULL, size, PROT_READ|PROT_WRITE, MAP_SHARED, fd, 0);
	END_CRITICAL_SECTION;

	if (res == MAP_FAILED) {
		shm_unlink (buf);
		close (fd);
		return mono_malloc_shared_area (pid);
	}
	/* we don't need the file descriptor anymore */
	close (fd);
	header = (SAreaHeader *) res;
	header->size = size;
	header->pid = pid;
	header->stats_start = sizeof (SAreaHeader);
	header->stats_end = sizeof (SAreaHeader);

	mono_atexit (mono_shared_area_remove);
	return res;
}

void
mono_shared_area_remove (void)
{
	char buf [128];

	if (shared_area_disabled ()) {
		if (malloced_shared_area)
			g_free (malloced_shared_area);
		return;
	}

	g_snprintf (buf, sizeof (buf), "/mono.%d", getpid ());
	shm_unlink (buf);
	if (malloced_shared_area)
		g_free (malloced_shared_area);
}

void*
mono_shared_area_for_pid (void *pid)
{
	int fd;
	/* we should allow the user to configure the size */
	int size = mono_pagesize ();
	char buf [128];
	void *res;

	if (shared_area_disabled ())
		return NULL;

	g_snprintf (buf, sizeof (buf), "/mono.%d", GPOINTER_TO_INT (pid));

	fd = shm_open (buf, O_RDONLY, S_IRUSR|S_IRGRP);
	if (fd == -1)
		return NULL;
	BEGIN_CRITICAL_SECTION;
	res = mmap (NULL, size, PROT_READ, MAP_SHARED, fd, 0);
	END_CRITICAL_SECTION;

	if (res == MAP_FAILED) {
		close (fd);
		return NULL;
	}
	/* FIXME: validate the area */
	/* we don't need the file descriptor anymore */
	close (fd);
	return res;
}

void
mono_shared_area_unload  (void *area)
{
	/* FIXME: currently we load only a page */
	BEGIN_CRITICAL_SECTION;
	munmap (area, mono_pagesize ());
	END_CRITICAL_SECTION;
}

int
mono_shared_area_instances (void **array, int count)
{
	return mono_shared_area_instances_helper (array, count, FALSE);
}
#else
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

#endif // HAVE_SHM_OPEN

#endif // HOST_WIN32

#ifndef HAVE_VALLOC_ALIGNED
void*
mono_valloc_aligned (size_t size, size_t alignment, int flags, MonoMemAccountType type)
{
	/* Allocate twice the memory to be able to put the block on an aligned address */
	char *mem = (char *) mono_valloc (NULL, size + alignment, flags, type);
	char *aligned;

	if (!mem)
		return NULL;

	aligned = mono_aligned_address (mem, size, alignment);

	if (aligned > mem)
		mono_vfree (mem, aligned - mem, type);
	if (aligned + size < mem + size + alignment)
		mono_vfree (aligned + size, (mem + size + alignment) - (aligned + size), type);

	return aligned;
}
#endif
