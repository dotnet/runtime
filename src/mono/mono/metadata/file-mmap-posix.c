/**
 * \file
 * File mmap internal calls
 *
 * Author:
 *	Rodrigo Kumpera
 *
 * Copyright 2014 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#ifndef HOST_WIN32

#include <glib.h>
#include <string.h>
#include <errno.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_SYS_STAT_H
#include <sys/stat.h>
#endif
#ifdef HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif
#if HAVE_SYS_MMAN_H
#include <sys/mman.h>
#endif

#include <fcntl.h>


#include <mono/metadata/object.h>
#include <mono/metadata/w32file.h>
#include <mono/metadata/file-mmap.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/mono-threads.h>

typedef struct {
	int kind;
	int ref_count;
	size_t capacity;
	char *name;
	int fd;
} MmapHandle;

typedef struct {
	void *address;
	void *free_handle;
	size_t length;
} MmapInstance;

enum {
	BAD_CAPACITY_FOR_FILE_BACKED = 1,
	CAPACITY_SMALLER_THAN_FILE_SIZE,
	FILE_NOT_FOUND,
	FILE_ALREADY_EXISTS,
	PATH_TOO_LONG,
	COULD_NOT_OPEN,
	CAPACITY_MUST_BE_POSITIVE,
	INVALID_FILE_MODE,
	COULD_NOT_MAP_MEMORY,
	ACCESS_DENIED,
	CAPACITY_LARGER_THAN_LOGICAL_ADDRESS_SPACE
};

enum {
	FILE_MODE_CREATE_NEW = 1,
	FILE_MODE_CREATE = 2,
	FILE_MODE_OPEN = 3,
	FILE_MODE_OPEN_OR_CREATE = 4,
	FILE_MODE_TRUNCATE = 5,
	FILE_MODE_APPEND = 6,
};

enum {
	MMAP_FILE_ACCESS_READ_WRITE = 0,
	MMAP_FILE_ACCESS_READ = 1,
	MMAP_FILE_ACCESS_WRITE = 2,
	MMAP_FILE_ACCESS_COPY_ON_WRITE = 3,
	MMAP_FILE_ACCESS_READ_EXECUTE = 4,
	MMAP_FILE_ACCESS_READ_WRITE_EXECUTE = 5,
};

#ifdef DEFFILEMODE
#define DEFAULT_FILEMODE DEFFILEMODE
#else
#define DEFAULT_FILEMODE 0666
#endif

static int mmap_init_state;
static MonoCoopMutex named_regions_mutex;
static GHashTable *named_regions;


static gint64
align_up_to_page_size (gint64 size)
{
	gint64 page_size = mono_pagesize ();
	return (size + page_size - 1) & ~(page_size - 1);
}

static gint64
align_down_to_page_size (gint64 size)
{
	gint64 page_size = mono_pagesize ();
	return size & ~(page_size - 1);
}

static void
file_mmap_init (void)
{
retry:	
	switch (mmap_init_state) {
	case  0:
		if (mono_atomic_cas_i32 (&mmap_init_state, 1, 0) != 0)
			goto retry;
		named_regions = g_hash_table_new_full (g_str_hash, g_str_equal, NULL, NULL);
		mono_coop_mutex_init (&named_regions_mutex);

		mono_atomic_store_release (&mmap_init_state, 2);
		break;

	case 1:
		do {
			mono_thread_info_sleep (1, NULL); /* Been init'd by other threads, this is very rare. */
		} while (mmap_init_state != 2);
		break;
	case 2:
		break;
	default:
		g_error ("Invalid init state %d", mmap_init_state);
	}
}

static void
named_regions_lock (void)
{
	file_mmap_init ();
	mono_coop_mutex_lock (&named_regions_mutex);
}

static void
named_regions_unlock (void)
{
	mono_coop_mutex_unlock (&named_regions_mutex);
}


static int
file_mode_to_unix (int mode)
{
	switch (mode) {
	case FILE_MODE_CREATE_NEW:
        return O_CREAT | O_EXCL; 
	case FILE_MODE_CREATE:
        return O_CREAT | O_TRUNC;
	case FILE_MODE_OPEN:
		return 0;
	case FILE_MODE_OPEN_OR_CREATE:
        return O_CREAT;
	case FILE_MODE_TRUNCATE:
        return O_TRUNC;
	case FILE_MODE_APPEND:
		return O_APPEND;
	default:
		g_error ("unknown FileMode %d", mode);
	}
}

static int
access_mode_to_unix (int access)
{
	switch (access) {
	case MMAP_FILE_ACCESS_READ_WRITE:
	case MMAP_FILE_ACCESS_COPY_ON_WRITE:
	case MMAP_FILE_ACCESS_READ_WRITE_EXECUTE:
		return O_RDWR;
	case MMAP_FILE_ACCESS_READ:
	case MMAP_FILE_ACCESS_READ_EXECUTE:
		return O_RDONLY;
	case MMAP_FILE_ACCESS_WRITE:
		return O_WRONLY;
	default:
		g_error ("unknown MemoryMappedFileAccess %d", access);
	}
}

static int
acess_to_mmap_flags (int access)
{
	switch (access) {
	case MMAP_FILE_ACCESS_READ_WRITE:
        return MONO_MMAP_WRITE | MONO_MMAP_READ | MONO_MMAP_SHARED;
        
	case MMAP_FILE_ACCESS_WRITE:
        return MONO_MMAP_WRITE | MONO_MMAP_SHARED;
        
	case MMAP_FILE_ACCESS_COPY_ON_WRITE:
        return MONO_MMAP_WRITE | MONO_MMAP_READ | MONO_MMAP_PRIVATE;
        
	case MMAP_FILE_ACCESS_READ_EXECUTE:
        return MONO_MMAP_EXEC | MONO_MMAP_PRIVATE | MONO_MMAP_SHARED;
        
	case MMAP_FILE_ACCESS_READ_WRITE_EXECUTE:
        return MONO_MMAP_WRITE | MONO_MMAP_READ | MONO_MMAP_EXEC | MONO_MMAP_SHARED;
        
	case MMAP_FILE_ACCESS_READ:
        return MONO_MMAP_READ | MONO_MMAP_SHARED;
	default:
		g_error ("unknown MemoryMappedFileAccess %d", access);
	}
}

/*
This allow us to special case zero size files that can be arbitrarily mapped.
*/
static gboolean
is_special_zero_size_file (struct stat *buf)
{
	return buf->st_size == 0 && (buf->st_mode & (S_IFCHR | S_IFBLK | S_IFIFO | S_IFSOCK)) != 0;
}

/*
XXX implement options
*/
static void*
open_file_map (const char *c_path, int input_fd, int mode, gint64 *capacity, int access, int options, int *ioerror)
{
	struct stat buf;
	MmapHandle *handle = NULL;
	int result, fd;

	if (c_path)
		result = stat (c_path, &buf);
	else
		result = fstat (input_fd, &buf);

	if (mode == FILE_MODE_TRUNCATE || mode == FILE_MODE_APPEND || mode == FILE_MODE_OPEN) {
		if (result == -1) { //XXX translate errno?
			*ioerror = FILE_NOT_FOUND;
			goto done;
		}
	}

	if (mode == FILE_MODE_CREATE_NEW && result == 0) {
		*ioerror = FILE_ALREADY_EXISTS;
		goto done;
	}

	if (result == 0) {
		if (*capacity == 0) {
			/**
			 * Special files such as FIFOs, sockets, and devices can have a size of 0. Specifying a capacity for these
			 * also makes little sense, so don't do the check if th file is one of these.
			 */
			if (buf.st_size == 0 && !is_special_zero_size_file (&buf)) {
				*ioerror = CAPACITY_SMALLER_THAN_FILE_SIZE;
				goto done;
			}
			*capacity = buf.st_size;
		} else if (*capacity < buf.st_size) {
			*ioerror = CAPACITY_SMALLER_THAN_FILE_SIZE;
			goto done;
		}
	} else {
		if (mode == FILE_MODE_CREATE_NEW && *capacity == 0) {
			*ioerror = CAPACITY_SMALLER_THAN_FILE_SIZE;
			goto done;
		}
	}

	if (c_path) //FIXME use io portability?
		fd = open (c_path, file_mode_to_unix (mode) | access_mode_to_unix (access), DEFAULT_FILEMODE);
	else
		fd = dup (input_fd);

	if (fd == -1) { //XXX translate errno?
		*ioerror = COULD_NOT_OPEN;
		goto done;
	}

	if (result != 0 || *capacity > buf.st_size) {
		int unused G_GNUC_UNUSED = ftruncate (fd, (off_t)*capacity);
	}

	handle = g_new0 (MmapHandle, 1);
	handle->ref_count = 1;
	handle->capacity = *capacity;
	handle->fd = fd;

done:
	return (void*)handle;
}

#define MONO_ANON_FILE_TEMPLATE "/mono.anonmap.XXXXXXXXX"
static void*
open_memory_map (const char *c_mapName, int mode, gint64 *capacity, int access, int options, int *ioerror)
{
	MmapHandle *handle;
	if (*capacity <= 0 && mode != FILE_MODE_OPEN) {
		*ioerror = CAPACITY_MUST_BE_POSITIVE;
		return NULL;
	}
#if SIZEOF_VOID_P == 4
	if (*capacity > UINT32_MAX) {
		*ioerror = CAPACITY_LARGER_THAN_LOGICAL_ADDRESS_SPACE;
		return NULL;
	}
#endif

	if (!(mode == FILE_MODE_CREATE_NEW || mode == FILE_MODE_OPEN_OR_CREATE || mode == FILE_MODE_OPEN)) {
		*ioerror = INVALID_FILE_MODE;
		return NULL;
	}

	named_regions_lock ();
	handle = (MmapHandle*)g_hash_table_lookup (named_regions, c_mapName);
	if (handle) {
		if (mode == FILE_MODE_CREATE_NEW) {
			*ioerror = FILE_ALREADY_EXISTS;
			goto done;
		}

		handle->ref_count++;
		//XXX should we ftruncate if the file is smaller than capacity?
	} else {
		int fd;
		char *file_name;
		const char *tmp_dir;
		int unused G_GNUC_UNUSED, alloc_size;

		if (mode == FILE_MODE_OPEN) {
			*ioerror = FILE_NOT_FOUND;
			goto done;
		}
		*capacity = align_up_to_page_size (*capacity);

		tmp_dir = g_get_tmp_dir ();
		alloc_size = strlen (tmp_dir) + strlen (MONO_ANON_FILE_TEMPLATE) + 1;
		if (alloc_size > 1024) {//rather fail that stack overflow
			*ioerror = COULD_NOT_MAP_MEMORY;
			goto done;
		}
		file_name = (char *)alloca (alloc_size);
		strcpy (file_name, tmp_dir);
		strcat (file_name, MONO_ANON_FILE_TEMPLATE);

		fd = mkstemp (file_name);
		if (fd == -1) {
			*ioerror = COULD_NOT_MAP_MEMORY;
			goto done;
		}

		unlink (file_name);
		unused = ftruncate (fd, (off_t)*capacity);

		handle = g_new0 (MmapHandle, 1);
		handle->ref_count = 1;
		handle->capacity = *capacity;
		handle->fd = fd;
		handle->name = g_strdup (c_mapName);

		g_hash_table_insert (named_regions, handle->name, handle);

	}

done:
	named_regions_unlock ();

	return handle;
}


/* This is an icall */
void *
mono_mmap_open_file (MonoString *path, int mode, MonoString *mapName, gint64 *capacity, int access, int options, int *ioerror)
{
	MonoError error;
	MmapHandle *handle = NULL;
	g_assert (path || mapName);

	if (!mapName) {
		char * c_path = mono_string_to_utf8_checked (path, &error);
		if (mono_error_set_pending_exception (&error))
			return NULL;
		handle = open_file_map (c_path, -1, mode, capacity, access, options, ioerror);
		g_free (c_path);
		return handle;
	}

	char *c_mapName = mono_string_to_utf8_checked (mapName, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;

	if (path) {
		named_regions_lock ();
		handle = (MmapHandle*)g_hash_table_lookup (named_regions, c_mapName);
		if (handle) {
			*ioerror = FILE_ALREADY_EXISTS;
			handle = NULL;
		} else {
			char *c_path = mono_string_to_utf8_checked (path, &error);
			if (is_ok (&error)) {
				handle = (MmapHandle *)open_file_map (c_path, -1, mode, capacity, access, options, ioerror);
				if (handle) {
					handle->name = g_strdup (c_mapName);
					g_hash_table_insert (named_regions, handle->name, handle);
				}
			} else {
				handle = NULL;
			}
			g_free (c_path);
		}
		named_regions_unlock ();
	} else
		handle = open_memory_map (c_mapName, mode, capacity, access, options, ioerror);

	g_free (c_mapName);
	return handle;
}

/* this is an icall */
void *
mono_mmap_open_handle (void *input_fd, MonoString *mapName, gint64 *capacity, int access, int options, int *ioerror)
{
	MonoError error;
	MmapHandle *handle;
	if (!mapName) {
		handle = (MmapHandle *)open_file_map (NULL, GPOINTER_TO_INT (input_fd), FILE_MODE_OPEN, capacity, access, options, ioerror);
	} else {
		char *c_mapName = mono_string_to_utf8_checked (mapName, &error);
		if (mono_error_set_pending_exception (&error))
			return NULL;

		named_regions_lock ();
		handle = (MmapHandle*)g_hash_table_lookup (named_regions, c_mapName);
		if (handle) {
			*ioerror = FILE_ALREADY_EXISTS;
			handle = NULL;
		} else {
			//XXX we're exploiting wapi HANDLE == FD equivalence. THIS IS FRAGILE, create a _wapi_handle_to_fd call
			handle = (MmapHandle *)open_file_map (NULL, GPOINTER_TO_INT (input_fd), FILE_MODE_OPEN, capacity, access, options, ioerror);
			handle->name = g_strdup (c_mapName);
			g_hash_table_insert (named_regions, handle->name, handle);
		}
		named_regions_unlock ();

		g_free (c_mapName);
	}
	return handle;
}

void
mono_mmap_close (void *mmap_handle)
{
	MmapHandle *handle = (MmapHandle *)mmap_handle;

	named_regions_lock ();
	--handle->ref_count;
	if (handle->ref_count == 0) {
		if (handle->name)
			g_hash_table_remove (named_regions, handle->name);

		g_free (handle->name);
		close (handle->fd);
		g_free (handle);
	}
	named_regions_unlock ();
}

void
mono_mmap_configure_inheritability (void *mmap_handle, gboolean inheritability)
{
	MmapHandle *h = (MmapHandle *)mmap_handle;
	int fd, flags;

	fd = h->fd;
	flags = fcntl (fd, F_GETFD, 0);
	if (inheritability)
		flags &= ~FD_CLOEXEC;
	else
		flags |= FD_CLOEXEC;
	fcntl (fd, F_SETFD, flags);	
}

void
mono_mmap_flush (void *mmap_handle)
{
	MmapInstance *h = (MmapInstance *)mmap_handle;

	if (h)
		msync (h->address, h->length, MS_SYNC);
}

int
mono_mmap_map (void *handle, gint64 offset, gint64 *size, int access, void **mmap_handle, void **base_address)
{
	gint64 mmap_offset = 0;
	MmapHandle *fh = (MmapHandle *)handle;
	MmapInstance res = { 0 };
	size_t eff_size = *size;
	struct stat buf = { 0 };
	fstat (fh->fd, &buf); //FIXME error handling

	*mmap_handle = NULL;
	*base_address = NULL;

	if (offset > buf.st_size || ((eff_size + offset) > buf.st_size && !is_special_zero_size_file (&buf)))
		return ACCESS_DENIED;
	/**
	  * We use the file size if one of the following conditions is true:
	  *  -input size is zero
	  *  -input size is bigger than the file and the file is not a magical zero size file such as /dev/mem.
	  */
	if (eff_size == 0)
		eff_size = align_up_to_page_size (buf.st_size) - offset;
	*size = eff_size;

	mmap_offset = align_down_to_page_size (offset);
	eff_size += (offset - mmap_offset);
	//FIXME translate some interesting errno values
	res.address = mono_file_map ((size_t)eff_size, acess_to_mmap_flags (access), fh->fd, mmap_offset, &res.free_handle);
	res.length = eff_size;

	if (res.address) {
		*mmap_handle = g_memdup (&res, sizeof (MmapInstance));
		*base_address = (char*)res.address + (offset - mmap_offset);
		return 0;
	}

	return COULD_NOT_MAP_MEMORY;
}

gboolean
mono_mmap_unmap (void *mmap_handle)
{
	int res = 0;
	MmapInstance *h = (MmapInstance *)mmap_handle;

	res = mono_file_unmap (h->address, h->free_handle);

	g_free (h);
	return res == 0;
}

#endif
