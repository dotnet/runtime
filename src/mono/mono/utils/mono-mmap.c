/*
 * mono-mmap.c: Support for mapping code into the process address space
 *
 * Author:
 *   Mono Team (mono-list@lists.ximian.com)
 *
 * Copyright 2001-2008 Novell, Inc.
 */

#include "config.h"

#ifdef HOST_WIN32
#include <windows.h>
#include <io.h>
#else
#include <sys/types.h>
#if HAVE_SYS_STAT_H
#include <sys/stat.h>
#endif
#if HAVE_SYS_MMAN_H
#include <sys/mman.h>
#endif
#include <fcntl.h>
#include <string.h>
#include <unistd.h>
#include <stdlib.h>
#include <signal.h>
#include <errno.h>
#endif

#include "mono-mmap.h"
#include "mono-mmap-internal.h"
#include "mono-proclib.h"

#ifndef MAP_ANONYMOUS
#define MAP_ANONYMOUS MAP_ANON
#endif

#ifndef MAP_32BIT
#define MAP_32BIT 0
#endif

typedef struct {
	int size;
	int pid;
	int reserved;
	short stats_start;
	short stats_end;
} SAreaHeader;

static void* malloced_shared_area = NULL;

static void*
malloc_shared_area (int pid)
{
	int size = mono_pagesize ();
	SAreaHeader *sarea = g_malloc0 (size);
	sarea->size = size;
	sarea->pid = pid;
	sarea->stats_start = sizeof (SAreaHeader);
	sarea->stats_end = sizeof (SAreaHeader);

	return sarea;
}

static char*
aligned_address (char *mem, size_t size, size_t alignment)
{
	char *aligned = (char*)((size_t)(mem + (alignment - 1)) & ~(alignment - 1));
	g_assert (aligned >= mem && aligned + size <= mem + size + alignment && !((size_t)aligned & (alignment - 1)));
	return aligned;
}

#ifdef HOST_WIN32

int
mono_pagesize (void)
{
	SYSTEM_INFO info;
	static int saved_pagesize = 0;
	if (saved_pagesize)
		return saved_pagesize;
	GetSystemInfo (&info);
	saved_pagesize = info.dwAllocationGranularity;
	return saved_pagesize;
}

static int
prot_from_flags (int flags)
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
mono_valloc (void *addr, size_t length, int flags)
{
	void *ptr;
	int mflags = MEM_RESERVE|MEM_COMMIT;
	int prot = prot_from_flags (flags);
	/* translate the flags */

	ptr = VirtualAlloc (addr, length, mflags, prot);
	return ptr;
}

void*
mono_valloc_aligned (size_t length, size_t alignment, int flags)
{
	int prot = prot_from_flags (flags);
	char *mem = VirtualAlloc (NULL, length + alignment, MEM_RESERVE, prot);
	char *aligned;

	if (!mem)
		return NULL;

	aligned = aligned_address (mem, length, alignment);

	aligned = VirtualAlloc (aligned, length, MEM_COMMIT, prot);
	g_assert (aligned);

	return aligned;
}

#define HAVE_VALLOC_ALIGNED

int
mono_vfree (void *addr, size_t length)
{
	MEMORY_BASIC_INFORMATION mbi;
	SIZE_T query_result = VirtualQuery (addr, &mbi, sizeof (mbi));
	BOOL res;

	g_assert (query_result);

	res = VirtualFree (mbi.AllocationBase, 0, MEM_RELEASE);

	g_assert (res);

	return 0;
}

void*
mono_file_map (size_t length, int flags, int fd, guint64 offset, void **ret_handle)
{
	void *ptr;
	int mflags = 0;
	HANDLE file, mapping;
	int prot = prot_from_flags (flags);
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

int
mono_mprotect (void *addr, size_t length, int flags)
{
	DWORD oldprot;
	int prot = prot_from_flags (flags);

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

int
mono_pages_not_faulted (void *addr, size_t length)
{
	return -1;
}

#else
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
	saved_pagesize = getpagesize ();
	return saved_pagesize;
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
 * mono_valloc:
 * @addr: memory address
 * @length: memory area size
 * @flags: protection flags
 *
 * Allocates @length bytes of virtual memory with the @flags
 * protection. @addr can be a preferred memory address or a
 * mandatory one if MONO_MMAP_FIXED is set in @flags.
 * @addr must be pagesize aligned and can be NULL.
 * @length must be a multiple of pagesize.
 *
 * Returns: NULL on failure, the address of the memory area otherwise
 */
void*
mono_valloc (void *addr, size_t length, int flags)
{
	void *ptr;
	int mflags = 0;
	int prot = prot_from_flags (flags);
	/* translate the flags */
	if (flags & MONO_MMAP_FIXED)
		mflags |= MAP_FIXED;
	if (flags & MONO_MMAP_32BIT)
		mflags |= MAP_32BIT;

	mflags |= MAP_ANONYMOUS;
	mflags |= MAP_PRIVATE;

	ptr = mmap (addr, length, prot, mflags, -1, 0);
	if (ptr == MAP_FAILED) {
		int fd = open ("/dev/zero", O_RDONLY);
		if (fd != -1) {
			ptr = mmap (addr, length, prot, mflags, fd, 0);
			close (fd);
		}
		if (ptr == MAP_FAILED)
			return NULL;
	}
	return ptr;
}

/**
 * mono_vfree:
 * @addr: memory address returned by mono_valloc ()
 * @length: size of memory area
 *
 * Remove the memory mapping at the address @addr.
 *
 * Returns: 0 on success.
 */
int
mono_vfree (void *addr, size_t length)
{
	return munmap (addr, length);
}

/**
 * mono_file_map:
 * @length: size of data to map
 * @flags: protection flags
 * @fd: file descriptor
 * @offset: offset in the file
 * @ret_handle: pointer to storage for returning a handle for the map
 *
 * Map the area of the file pointed to by the file descriptor @fd, at offset
 * @offset and of size @length in memory according to the protection flags
 * @flags.
 * @offset and @length must be multiples of the page size.
 * @ret_handle must point to a void*: this value must be used when unmapping
 * the memory area using mono_file_unmap ().
 *
 */
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
	if (flags & MONO_MMAP_32BIT)
		mflags |= MAP_32BIT;

	ptr = mmap (0, length, prot, mflags, fd, offset);
	if (ptr == MAP_FAILED)
		return NULL;
	*ret_handle = (void*)length;
	return ptr;
}

/**
 * mono_file_unmap:
 * @addr: memory address returned by mono_file_map ()
 * @handle: handle of memory map
 *
 * Remove the memory mapping at the address @addr.
 * @handle must be the value returned in ret_handle by mono_file_map ().
 *
 * Returns: 0 on success.
 */
int
mono_file_unmap (void *addr, void *handle)
{
	return munmap (addr, (size_t)handle);
}

/**
 * mono_mprotect:
 * @addr: memory address
 * @length: size of memory area
 * @flags: new protection flags
 *
 * Change the protection for the memory area at @addr for @length bytes
 * to matche the supplied @flags.
 * If @flags includes MON_MMAP_DISCARD the pages are discarded from memory
 * and the area is cleared to zero.
 * @addr must be aligned to the page size.
 * @length must be a multiple of the page size.
 *
 * Returns: 0 on success.
 */
#if defined(__native_client__)
int
mono_mprotect (void *addr, size_t length, int flags)
{
	int prot = prot_from_flags (flags);
	void *new_addr;

	if (flags & MONO_MMAP_DISCARD) memset (addr, 0, length);

	new_addr = mmap(addr, length, prot, MAP_PRIVATE | MAP_FIXED | MAP_ANONYMOUS, -1, 0);
	if (new_addr == addr) return 0;
        return -1;
}
#else
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
#ifdef HAVE_MADVISE
		madvise (addr, length, MADV_DONTNEED);
		madvise (addr, length, MADV_FREE);
#else
		posix_madvise (addr, length, POSIX_MADV_DONTNEED);
#endif
#endif
	}
	return mprotect (addr, length, prot);
}
#endif // __native_client__

int
mono_pages_not_faulted (void *addr, size_t size)
{
	int i;
	gint64 count;
	int pagesize = mono_pagesize ();
	int npages = (size + pagesize - 1) / pagesize;
	char *faulted = g_malloc0 (sizeof (char*) * npages);

	if (mincore (addr, size, faulted) != 0) {
		count = -1;
	} else {
		count = 0;
		for (i = 0; i < npages; ++i) {
			if (faulted [i] != 0)
				++count;
		}
	}

	g_free (faulted);

	return count;
}

#else

/* dummy malloc-based implementation */
int
mono_pagesize (void)
{
	return 4096;
}

void*
mono_valloc (void *addr, size_t length, int flags)
{
	return malloc (length);
}

void*
mono_valloc_aligned (size_t length, size_t alignment, int flags)
{
	g_assert_not_reached ();
}

#define HAVE_VALLOC_ALIGNED

int
mono_vfree (void *addr, size_t length)
{
	free (addr);
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

int
mono_pages_not_faulted (void *addr, size_t length)
{
	return -1;
}

#endif // HAVE_MMAP

#if defined(HAVE_SHM_OPEN) && !defined (DISABLE_SHARED_PERFCOUNTERS)

static int use_shared_area;

static gboolean
shared_area_disabled (void)
{
	if (!use_shared_area) {
		if (g_getenv ("MONO_DISABLE_SHARED_AREA"))
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
			malloced_shared_area = malloc_shared_area (0);
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
		return malloc_shared_area (pid);
	if (ftruncate (fd, size) != 0) {
		shm_unlink (buf);
		close (fd);
	}
	res = mmap (NULL, size, PROT_READ|PROT_WRITE, MAP_SHARED, fd, 0);
	if (res == MAP_FAILED) {
		shm_unlink (buf);
		close (fd);
		return malloc_shared_area (pid);
	}
	/* we don't need the file descriptor anymore */
	close (fd);
	header = res;
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
	res = mmap (NULL, size, PROT_READ, MAP_SHARED, fd, 0);
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
	munmap (area, mono_pagesize ());
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
		malloced_shared_area = malloc_shared_area (getpid ());
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
mono_valloc_aligned (size_t size, size_t alignment, int flags)
{
	/* Allocate twice the memory to be able to put the block on an aligned address */
	char *mem = mono_valloc (NULL, size + alignment, flags);
	char *aligned;

	if (!mem)
		return NULL;

	aligned = aligned_address (mem, size, alignment);

	if (aligned > mem)
		mono_vfree (mem, aligned - mem);
	if (aligned + size < mem + size + alignment)
		mono_vfree (aligned + size, (mem + size + alignment) - (aligned + size));

	return aligned;
}
#endif
