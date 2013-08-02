#ifndef __MONO_UTILS_MMAP_H__
#define __MONO_UTILS_MMAP_H__

#include <glib.h>
#include <mono/utils/mono-publib.h>

enum {
	/* protection */
	MONO_MMAP_NONE = 0,
	MONO_MMAP_READ    = 1 << 0,
	MONO_MMAP_WRITE   = 1 << 1,
	MONO_MMAP_EXEC    = 1 << 2,
	/* make the OS discard the dirty data and fill with 0 */
	MONO_MMAP_DISCARD = 1 << 3,
	/* other flags (add commit, sync) */
	MONO_MMAP_PRIVATE = 1 << 4,
	MONO_MMAP_SHARED  = 1 << 5,
	MONO_MMAP_ANON    = 1 << 6,
	MONO_MMAP_FIXED   = 1 << 7,
	MONO_MMAP_32BIT   = 1 << 8
};

/*
 * A simple interface to fopen/fstat/fileno
 */
typedef struct _MonoFileMap MonoFileMap;

MONO_API MonoFileMap *mono_file_map_open  (const char* name);
MONO_API guint64      mono_file_map_size  (MonoFileMap *fmap);
MONO_API int          mono_file_map_fd    (MonoFileMap *fmap);
MONO_API int          mono_file_map_close (MonoFileMap *fmap);

MONO_API int   mono_pagesize   (void);
MONO_API void* mono_valloc     (void *addr, size_t length, int flags);
MONO_API void* mono_valloc_aligned (size_t length, size_t alignment, int flags);
MONO_API int   mono_vfree      (void *addr, size_t length);
MONO_API void* mono_file_map   (size_t length, int flags, int fd, guint64 offset, void **ret_handle);
MONO_API int   mono_file_unmap (void *addr, void *handle);
#ifndef HOST_WIN32
MONO_API void* mono_file_map_fileio   (size_t length, int flags, int fd, guint64 offset, void **ret_handle);
MONO_API int   mono_file_unmap_fileio (void *addr, void *handle);
#endif
MONO_API int   mono_mprotect   (void *addr, size_t length, int flags);

MONO_API void* mono_shared_area         (void);
MONO_API void  mono_shared_area_remove  (void);
MONO_API void* mono_shared_area_for_pid (void *pid);
MONO_API void  mono_shared_area_unload  (void *area);
MONO_API int   mono_shared_area_instances (void **array, int count);

/*
 * On systems where we have to load code into memory instead of mmaping
 * we allow for the allocator to be set.   This function is only
 * defined on those platforms.
 */
typedef void *(*mono_file_map_alloc_fn)   (size_t length);
typedef void  (*mono_file_map_release_fn) (void *addr);

MONO_API void mono_file_map_set_allocator (mono_file_map_alloc_fn alloc, mono_file_map_release_fn release);
				  
#endif /* __MONO_UTILS_MMAP_H__ */

