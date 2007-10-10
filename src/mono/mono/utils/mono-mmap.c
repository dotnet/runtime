#include "config.h"

#ifdef PLATFORM_WIN32
#include <windows.h>
#include <io.h>
#else
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/mman.h>
#include <fcntl.h>
#include <string.h>
#include <unistd.h>
#include <stdlib.h>
#endif

#include "mono-mmap.h"

#ifndef MAP_ANONYMOUS
#define MAP_ANONYMOUS MAP_ANON
#endif

#ifndef MAP_32BIT
#define MAP_32BIT 0
#endif

#ifdef PLATFORM_WIN32

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
	int mflags = MEM_COMMIT;
	int prot = prot_from_flags (flags);
	/* translate the flags */

	ptr = VirtualAlloc (addr, length, mflags, prot);
	return ptr;
}

int
mono_vfree (void *addr, size_t length)
{
	int res = VirtualFree (addr, 0, MEM_RELEASE);

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

#elif defined(HAVE_MMAP)

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
	if (ptr == (void*)-1) {
		int fd = open ("/dev/zero", O_RDONLY);
		if (fd != -1) {
			ptr = mmap (addr, length, prot, mflags, fd, 0);
			close (fd);
		}
		if (ptr == (void*)-1)
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
	if (ptr == (void*)-1)
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
		madvise (addr, length, MADV_DONTNEED);
		madvise (addr, length, MADV_FREE);
#endif
	}
	return mprotect (addr, length, prot);
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

int
mono_vfree (void *addr, size_t length)
{
	free (addr);
	return 0;
}

void*
mono_file_map (size_t length, int flags, int fd, guint64 offset, void **ret_handle)
{
	guint64 cur_offset;
	size_t bytes_read;
	void *ptr = malloc (length);
	if (!ptr)
		return NULL;
	cur_offset = lseek (fd, 0, SEEK_CUR);
	if (lseek (fd, offset, SEEK_SET) != offset) {
		free (ptr);
		return NULL;
	}
	bytes_read = read (fd, ptr, length);
	lseek (fd, cur_offset, SEEK_SET);
	*ret_handle = NULL;
	return ptr;
}

int
mono_file_unmap (void *addr, void *handle)
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

#endif

