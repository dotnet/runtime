/**
 * \file
 */

#include <config.h>
#include <glib.h>

#include <stdlib.h>
#include <fcntl.h>
#include <unistd.h>
#include <errno.h>
#include <string.h>
#include <sys/stat.h>
#ifdef HAVE_SYS_STATVFS_H
#include <sys/statvfs.h>
#endif
#if defined(HAVE_SYS_STATFS_H)
#include <sys/statfs.h>
#endif
#if defined(HAVE_SYS_PARAM_H) && defined(HAVE_SYS_MOUNT_H)
#include <sys/param.h>
#include <sys/mount.h>
#endif
#include <sys/types.h>
#include <stdio.h>
#include <utime.h>
#ifdef __linux__
#include <sys/ioctl.h>
#include <linux/fs.h>
#include <mono/utils/linux_magic.h>
#endif
#include <sys/time.h>
#ifdef HAVE_DIRENT_H
# include <dirent.h>
#endif

#include "w32file.h"
#include "w32file-internals.h"

#include "w32file-unix-glob.h"
#include "w32error.h"
#include "fdhandle.h"
#include "utils/mono-io-portability.h"
#include "utils/mono-logger-internals.h"
#include "utils/mono-os-mutex.h"
#include "utils/mono-threads.h"
#include "utils/mono-threads-api.h"
#include "utils/strenc.h"
#include "utils/refcount.h"

#define INVALID_HANDLE_VALUE (GINT_TO_POINTER (-1))

typedef struct {
	guint64 device;
	guint64 inode;
	guint32 sharemode;
	guint32 access;
	guint32 handle_refs;
	guint32 timestamp;
} FileShare;

/* Currently used for both FILE, CONSOLE and PIPE handle types.
 * This may have to change in future. */
typedef struct {
	MonoFDHandle fdhandle;
	gchar *filename;
	FileShare *share_info;	/* Pointer into shared mem */
	guint32 security_attributes;
	guint32 fileaccess;
	guint32 sharemode;
	guint32 attrs;
} FileHandle;

typedef struct {
	MonoRefCount ref;
	MonoCoopMutex mutex;
	gchar **namelist;
	gchar *dir_part;
	gint num;
	gsize count;
} FindHandle;

/*
 * If SHM is disabled, this will point to a hash of FileShare structures, otherwise
 * it will be NULL. We use this instead of _wapi_fileshare_layout to avoid allocating a
 * 4MB array.
 */
static GHashTable *file_share_table;
static MonoCoopMutex file_share_mutex;

static GHashTable *finds;
static MonoCoopMutex finds_mutex;

static void
time_t_to_filetime (time_t timeval, FILETIME *filetime)
{
	guint64 ticks;
	
	ticks = ((guint64)timeval * 10000000) + 116444736000000000ULL;
	filetime->dwLowDateTime = ticks & 0xFFFFFFFF;
	filetime->dwHighDateTime = ticks >> 32;
}

static FileHandle*
file_data_create (MonoFDType type, gint fd)
{
	FileHandle *filehandle;

	filehandle = g_new0 (FileHandle, 1);
	mono_fdhandle_init ((MonoFDHandle*) filehandle, type, fd);

	return filehandle;
}

static gint
_wapi_unlink (const gchar *pathname);

static void
file_share_release (FileShare *share_info);

static void
file_data_close (MonoFDHandle *fdhandle)
{
	FileHandle* filehandle;

	filehandle = (FileHandle*) fdhandle;
	g_assert (filehandle);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: closing fd %d", __func__, ((MonoFDHandle*) filehandle)->fd);

	if (((MonoFDHandle*) filehandle)->type == MONO_FDTYPE_FILE && (filehandle->attrs & FILE_FLAG_DELETE_ON_CLOSE)) {
		_wapi_unlink (filehandle->filename);
	}

	if (((MonoFDHandle*) filehandle)->type != MONO_FDTYPE_CONSOLE || ((MonoFDHandle*) filehandle)->fd > 2) {
		if (filehandle->share_info) {
			file_share_release (filehandle->share_info);
			filehandle->share_info = NULL;
		}

		MONO_ENTER_GC_SAFE;
		close (((MonoFDHandle*) filehandle)->fd);
		MONO_EXIT_GC_SAFE;
	}
}

static void
file_data_destroy (MonoFDHandle *fdhandle)
{
	FileHandle *filehandle;

	filehandle = (FileHandle*) fdhandle;
	g_assert (filehandle);

	if (filehandle->filename)
		g_free (filehandle->filename);

	g_free (filehandle);
}

static void
file_share_release (FileShare *share_info)
{
	/* Prevent new entries racing with us */
	mono_coop_mutex_lock (&file_share_mutex);

	g_assert (share_info->handle_refs > 0);
	share_info->handle_refs -= 1;

	if (share_info->handle_refs == 0) {
		g_hash_table_remove (file_share_table, share_info);
		// g_free (share_info);
	}

	mono_coop_mutex_unlock (&file_share_mutex);
}

static gint
file_share_equal (gconstpointer ka, gconstpointer kb)
{
	const FileShare *s1 = (const FileShare *)ka;
	const FileShare *s2 = (const FileShare *)kb;

	return (s1->device == s2->device && s1->inode == s2->inode) ? 1 : 0;
}

static guint
file_share_hash (gconstpointer data)
{
	const FileShare *s = (const FileShare *)data;

	return s->inode;
}

static gboolean
file_share_get (guint64 device, guint64 inode, guint32 new_sharemode, guint32 new_access,
	guint32 *old_sharemode, guint32 *old_access, FileShare **share_info)
{
	FileShare *file_share;
	gboolean exists = FALSE;

	/* Prevent new entries racing with us */
	mono_coop_mutex_lock (&file_share_mutex);

	FileShare tmp;

	/*
	 * Instead of allocating a 4MB array, we use a hash table to keep track of this
	 * info. This is needed even if SHM is disabled, to track sharing inside
	 * the current process.
	 */
	if (!file_share_table)
		file_share_table = g_hash_table_new_full (file_share_hash, file_share_equal, NULL, g_free);

	tmp.device = device;
	tmp.inode = inode;

	file_share = (FileShare *)g_hash_table_lookup (file_share_table, &tmp);
	if (file_share) {
		*old_sharemode = file_share->sharemode;
		*old_access = file_share->access;
		*share_info = file_share;

		g_assert (file_share->handle_refs > 0);
		file_share->handle_refs += 1;

		exists = TRUE;
	} else {
		file_share = g_new0 (FileShare, 1);

		file_share->device = device;
		file_share->inode = inode;
		file_share->sharemode = new_sharemode;
		file_share->access = new_access;
		file_share->handle_refs = 1;
		*share_info = file_share;

		g_hash_table_insert (file_share_table, file_share, file_share);
	}

	mono_coop_mutex_unlock (&file_share_mutex);

	return(exists);
}

static gint
_wapi_open (const gchar *pathname, gint flags, mode_t mode)
{
	gint fd;
	gchar *located_filename;

	if (flags & O_CREAT) {
		located_filename = mono_portability_find_file (pathname, FALSE);
		if (located_filename == NULL) {
			MONO_ENTER_GC_SAFE;
			fd = open (pathname, flags, mode);
			MONO_EXIT_GC_SAFE;
		} else {
			MONO_ENTER_GC_SAFE;
			fd = open (located_filename, flags, mode);
			MONO_EXIT_GC_SAFE;
			g_free (located_filename);
		}
	} else {
		MONO_ENTER_GC_SAFE;
		fd = open (pathname, flags, mode);
		MONO_EXIT_GC_SAFE;
		if (fd == -1 && (errno == ENOENT || errno == ENOTDIR) && IS_PORTABILITY_SET) {
			gint saved_errno = errno;
			located_filename = mono_portability_find_file (pathname, TRUE);

			if (located_filename == NULL) {
				errno = saved_errno;
				return -1;
			}

			MONO_ENTER_GC_SAFE;
			fd = open (located_filename, flags, mode);
			MONO_EXIT_GC_SAFE;
			g_free (located_filename);
		}
	}

	return(fd);
}

static gint
_wapi_access (const gchar *pathname, gint mode)
{
	gint ret;

	MONO_ENTER_GC_SAFE;
	ret = access (pathname, mode);
	MONO_EXIT_GC_SAFE;
	if (ret == -1 && (errno == ENOENT || errno == ENOTDIR) && IS_PORTABILITY_SET) {
		gint saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (pathname, TRUE);

		if (located_filename == NULL) {
			errno = saved_errno;
			return -1;
		}

		MONO_ENTER_GC_SAFE;
		ret = access (located_filename, mode);
		MONO_EXIT_GC_SAFE;
		g_free (located_filename);
	}

	return ret;
}

static gint
_wapi_chmod (const gchar *pathname, mode_t mode)
{
	gint ret;

	MONO_ENTER_GC_SAFE;
	ret = chmod (pathname, mode);
	MONO_EXIT_GC_SAFE;
	if (ret == -1 && (errno == ENOENT || errno == ENOTDIR) && IS_PORTABILITY_SET) {
		gint saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (pathname, TRUE);

		if (located_filename == NULL) {
			errno = saved_errno;
			return -1;
		}

		MONO_ENTER_GC_SAFE;
		ret = chmod (located_filename, mode);
		MONO_EXIT_GC_SAFE;
		g_free (located_filename);
	}

	return ret;
}

static gint
_wapi_utime (const gchar *filename, const struct utimbuf *buf)
{
	gint ret;

	MONO_ENTER_GC_SAFE;
	ret = utime (filename, buf);
	MONO_EXIT_GC_SAFE;
	if (ret == -1 && errno == ENOENT && IS_PORTABILITY_SET) {
		gint saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (filename, TRUE);

		if (located_filename == NULL) {
			errno = saved_errno;
			return -1;
		}

		MONO_ENTER_GC_SAFE;
		ret = utime (located_filename, buf);
		MONO_EXIT_GC_SAFE;
		g_free (located_filename);
	}

	return ret;
}

static gint
_wapi_unlink (const gchar *pathname)
{
	gint ret;

	MONO_ENTER_GC_SAFE;
	ret = unlink (pathname);
	MONO_EXIT_GC_SAFE;
	if (ret == -1 && (errno == ENOENT || errno == ENOTDIR || errno == EISDIR) && IS_PORTABILITY_SET) {
		gint saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (pathname, TRUE);

		if (located_filename == NULL) {
			errno = saved_errno;
			return -1;
		}

		MONO_ENTER_GC_SAFE;
		ret = unlink (located_filename);
		MONO_EXIT_GC_SAFE;
		g_free (located_filename);
	}

	return ret;
}

static gint
_wapi_rename (const gchar *oldpath, const gchar *newpath)
{
	gint ret;
	gchar *located_newpath = mono_portability_find_file (newpath, FALSE);

	if (located_newpath == NULL) {
		MONO_ENTER_GC_SAFE;
		ret = rename (oldpath, newpath);
		MONO_EXIT_GC_SAFE;
	} else {
		MONO_ENTER_GC_SAFE;
		ret = rename (oldpath, located_newpath);
		MONO_EXIT_GC_SAFE;

		if (ret == -1 && (errno == EISDIR || errno == ENAMETOOLONG || errno == ENOENT || errno == ENOTDIR || errno == EXDEV) && IS_PORTABILITY_SET) {
			gint saved_errno = errno;
			gchar *located_oldpath = mono_portability_find_file (oldpath, TRUE);

			if (located_oldpath == NULL) {
				g_free (located_oldpath);
				g_free (located_newpath);

				errno = saved_errno;
				return -1;
			}

			MONO_ENTER_GC_SAFE;
			ret = rename (located_oldpath, located_newpath);
			MONO_EXIT_GC_SAFE;
			g_free (located_oldpath);
		}
		g_free (located_newpath);
	}

	return ret;
}

static gint
_wapi_stat (const gchar *path, struct stat *buf)
{
	gint ret;

	MONO_ENTER_GC_SAFE;
	ret = stat (path, buf);
	MONO_EXIT_GC_SAFE;
	if (ret == -1 && (errno == ENOENT || errno == ENOTDIR) && IS_PORTABILITY_SET) {
		gint saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (path, TRUE);

		if (located_filename == NULL) {
			errno = saved_errno;
			return -1;
		}

		MONO_ENTER_GC_SAFE;
		ret = stat (located_filename, buf);
		MONO_EXIT_GC_SAFE;
		g_free (located_filename);
	}

	return ret;
}

static gint
_wapi_lstat (const gchar *path, struct stat *buf)
{
	gint ret;

	MONO_ENTER_GC_SAFE;
	ret = lstat (path, buf);
	MONO_EXIT_GC_SAFE;
	if (ret == -1 && (errno == ENOENT || errno == ENOTDIR) && IS_PORTABILITY_SET) {
		gint saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (path, TRUE);

		if (located_filename == NULL) {
			errno = saved_errno;
			return -1;
		}

		ret = lstat (located_filename, buf);
		g_free (located_filename);
	}

	return ret;
}

static gint
_wapi_mkdir (const gchar *pathname, mode_t mode)
{
	gint ret;
	gchar *located_filename = mono_portability_find_file (pathname, FALSE);

	if (located_filename == NULL) {
		MONO_ENTER_GC_SAFE;
		ret = mkdir (pathname, mode);
		MONO_EXIT_GC_SAFE;
	} else {
		MONO_ENTER_GC_SAFE;
		ret = mkdir (located_filename, mode);
		MONO_EXIT_GC_SAFE;
		g_free (located_filename);
	}

	return ret;
}

static gint
_wapi_rmdir (const gchar *pathname)
{
	gint ret;

	MONO_ENTER_GC_SAFE;
	ret = rmdir (pathname);
	MONO_EXIT_GC_SAFE;
	if (ret == -1 && (errno == ENOENT || errno == ENOTDIR || errno == ENAMETOOLONG) && IS_PORTABILITY_SET) {
		gint saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (pathname, TRUE);

		if (located_filename == NULL) {
			errno = saved_errno;
			return -1;
		}

		MONO_ENTER_GC_SAFE;
		ret = rmdir (located_filename);
		MONO_EXIT_GC_SAFE;
		g_free (located_filename);
	}

	return ret;
}

static gint
_wapi_chdir (const gchar *path)
{
	gint ret;

	MONO_ENTER_GC_SAFE;
	ret = chdir (path);
	MONO_EXIT_GC_SAFE;
	if (ret == -1 && (errno == ENOENT || errno == ENOTDIR || errno == ENAMETOOLONG) && IS_PORTABILITY_SET) {
		gint saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (path, TRUE);

		if (located_filename == NULL) {
			errno = saved_errno;
			return -1;
		}

		MONO_ENTER_GC_SAFE;
		ret = chdir (located_filename);
		MONO_EXIT_GC_SAFE;
		g_free (located_filename);
	}

	return ret;
}

static gchar*
_wapi_basename (const gchar *filename)
{
	gchar *new_filename = g_strdup (filename), *ret;

	if (IS_PORTABILITY_SET) {
		g_strdelimit (new_filename, "\\", '/');
	}

	if (IS_PORTABILITY_DRIVE && g_ascii_isalpha (new_filename[0]) && (new_filename[1] == ':')) {
		gint len = strlen (new_filename);

		g_memmove (new_filename, new_filename + 2, len - 2);
		new_filename[len - 2] = '\0';
	}

	ret = g_path_get_basename (new_filename);
	g_free (new_filename);

	return ret;
}

static gchar*
_wapi_dirname (const gchar *filename)
{
	gchar *new_filename = g_strdup (filename), *ret;

	if (IS_PORTABILITY_SET) {
		g_strdelimit (new_filename, "\\", '/');
	}

	if (IS_PORTABILITY_DRIVE && g_ascii_isalpha (new_filename[0]) && (new_filename[1] == ':')) {
		gint len = strlen (new_filename);

		g_memmove (new_filename, new_filename + 2, len - 2);
		new_filename[len - 2] = '\0';
	}

	ret = g_path_get_dirname (new_filename);
	g_free (new_filename);

	return ret;
}

static GDir*
_wapi_g_dir_open (const gchar *path, guint flags, GError **error)
{
	GDir *ret;

	MONO_ENTER_GC_SAFE;
	ret = g_dir_open (path, flags, error);
	MONO_EXIT_GC_SAFE;
	if (ret == NULL && ((*error)->code == G_FILE_ERROR_NOENT || (*error)->code == G_FILE_ERROR_NOTDIR || (*error)->code == G_FILE_ERROR_NAMETOOLONG) && IS_PORTABILITY_SET) {
		gchar *located_filename = mono_portability_find_file (path, TRUE);
		GError *tmp_error = NULL;

		if (located_filename == NULL) {
			return(NULL);
		}

		MONO_ENTER_GC_SAFE;
		ret = g_dir_open (located_filename, flags, &tmp_error);
		MONO_EXIT_GC_SAFE;
		g_free (located_filename);
		if (tmp_error == NULL) {
			g_clear_error (error);
		}
	}

	return ret;
}

static gint
get_errno_from_g_file_error (gint error)
{
	switch (error) {
#ifdef EACCES
	case G_FILE_ERROR_ACCES: return EACCES;
#endif
#ifdef ENAMETOOLONG
	case G_FILE_ERROR_NAMETOOLONG: return ENAMETOOLONG;
#endif
#ifdef ENOENT
	case G_FILE_ERROR_NOENT: return ENOENT;
#endif
#ifdef ENOTDIR
	case G_FILE_ERROR_NOTDIR: return ENOTDIR;
#endif
#ifdef ENXIO
	case G_FILE_ERROR_NXIO: return ENXIO;
#endif
#ifdef ENODEV
	case G_FILE_ERROR_NODEV: return ENODEV;
#endif
#ifdef EROFS
	case G_FILE_ERROR_ROFS: return EROFS;
#endif
#ifdef ETXTBSY
	case G_FILE_ERROR_TXTBSY: return ETXTBSY;
#endif
#ifdef EFAULT
	case G_FILE_ERROR_FAULT: return EFAULT;
#endif
#ifdef ELOOP
	case G_FILE_ERROR_LOOP: return ELOOP;
#endif
#ifdef ENOSPC
	case G_FILE_ERROR_NOSPC: return ENOSPC;
#endif
#ifdef ENOMEM
	case G_FILE_ERROR_NOMEM: return ENOMEM;
#endif
#ifdef EMFILE
	case G_FILE_ERROR_MFILE: return EMFILE;
#endif
#ifdef ENFILE
	case G_FILE_ERROR_NFILE: return ENFILE;
#endif
#ifdef EBADF
	case G_FILE_ERROR_BADF: return EBADF;
#endif
#ifdef EINVAL
	case G_FILE_ERROR_INVAL: return EINVAL;
#endif
#ifdef EPIPE
	case G_FILE_ERROR_PIPE: return EPIPE;
#endif
#ifdef EAGAIN
	case G_FILE_ERROR_AGAIN: return EAGAIN;
#endif
#ifdef EINTR
	case G_FILE_ERROR_INTR: return EINTR;
#endif
#ifdef EIO
	case G_FILE_ERROR_IO: return EIO;
#endif
#ifdef EPERM
	case G_FILE_ERROR_PERM: return EPERM;
#endif
	case G_FILE_ERROR_FAILED: return ERROR_INVALID_PARAMETER;
	default:
		g_assert_not_reached ();
	}
}

static gint
file_compare (gconstpointer a, gconstpointer b)
{
	gchar *astr = *(gchar **) a;
	gchar *bstr = *(gchar **) b;

	return strcmp (astr, bstr);
}

/* scandir using glib */
static gint
_wapi_io_scandir (const gchar *dirname, const gchar *pattern, gchar ***namelist)
{
	GError *error = NULL;
	GDir *dir;
	GPtrArray *names;
	gint result;
	mono_w32file_unix_glob_t glob_buf;
	gint flags = 0, i;

	dir = _wapi_g_dir_open (dirname, 0, &error);
	if (dir == NULL) {
		/* g_dir_open returns ENOENT on directories on which we don't
		 * have read/x permission */
		gint errnum = get_errno_from_g_file_error (error->code);
		g_error_free (error);
		if (errnum == ENOENT &&
		    !_wapi_access (dirname, F_OK) &&
		    _wapi_access (dirname, R_OK|X_OK)) {
			errnum = EACCES;
		}

		errno = errnum;
		return -1;
	}

	if (IS_PORTABILITY_CASE) {
		flags = W32FILE_UNIX_GLOB_IGNORECASE;
	}

	result = mono_w32file_unix_glob (dir, pattern, flags, &glob_buf);
	if (g_str_has_suffix (pattern, ".*")) {
		/* Special-case the patterns ending in '.*', as
		 * windows also matches entries with no extension with
		 * this pattern.
		 *
		 * TODO: should this be a MONO_IOMAP option?
		 */
		gchar *pattern2 = g_strndup (pattern, strlen (pattern) - 2);
		gint result2;

		MONO_ENTER_GC_SAFE;
		g_dir_rewind (dir);
		MONO_EXIT_GC_SAFE;
		result2 = mono_w32file_unix_glob (dir, pattern2, flags | W32FILE_UNIX_GLOB_APPEND | W32FILE_UNIX_GLOB_UNIQUE, &glob_buf);

		g_free (pattern2);

		if (result != 0) {
			result = result2;
		}
	}

	MONO_ENTER_GC_SAFE;
	g_dir_close (dir);
	MONO_EXIT_GC_SAFE;
	if (glob_buf.gl_pathc == 0) {
		return(0);
	} else if (result != 0) {
		return -1;
	}

	names = g_ptr_array_new ();
	for (i = 0; i < glob_buf.gl_pathc; i++) {
		g_ptr_array_add (names, g_strdup (glob_buf.gl_pathv[i]));
	}

	mono_w32file_unix_globfree (&glob_buf);

	result = names->len;
	if (result > 0) {
		g_ptr_array_sort (names, file_compare);
		g_ptr_array_set_size (names, result + 1);

		*namelist = (gchar **) g_ptr_array_free (names, FALSE);
	} else {
		g_ptr_array_free (names, TRUE);
	}

	return result;
}

static gboolean
_wapi_lock_file_region (gint fd, off_t offset, off_t length)
{
	struct flock lock_data;
	gint ret;

	if (offset < 0 || length < 0) {
		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		return FALSE;
	}

	lock_data.l_type = F_WRLCK;
	lock_data.l_whence = SEEK_SET;
	lock_data.l_start = offset;
	lock_data.l_len = length;

	do {
		ret = fcntl (fd, F_SETLK, &lock_data);
	} while(ret == -1 && errno == EINTR);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fcntl returns %d", __func__, ret);

	if (ret == -1) {
		/*
		 * if locks are not available (NFS for example),
		 * ignore the error
		 */
		if (errno == ENOLCK
#ifdef EOPNOTSUPP
		    || errno == EOPNOTSUPP
#endif
#ifdef ENOTSUP
		    || errno == ENOTSUP
#endif
		   ) {
			return TRUE;
		}

		mono_w32error_set_last (ERROR_LOCK_VIOLATION);
		return FALSE;
	}

	return TRUE;
}

static gboolean
_wapi_unlock_file_region (gint fd, off_t offset, off_t length)
{
	struct flock lock_data;
	gint ret;

	lock_data.l_type = F_UNLCK;
	lock_data.l_whence = SEEK_SET;
	lock_data.l_start = offset;
	lock_data.l_len = length;

	do {
		ret = fcntl (fd, F_SETLK, &lock_data);
	} while(ret == -1 && errno == EINTR);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fcntl returns %d", __func__, ret);

	if (ret == -1) {
		/*
		 * if locks are not available (NFS for example),
		 * ignore the error
		 */
		if (errno == ENOLCK
#ifdef EOPNOTSUPP
		    || errno == EOPNOTSUPP
#endif
#ifdef ENOTSUP
		    || errno == ENOTSUP
#endif
		   ) {
			return TRUE;
		}

		mono_w32error_set_last (ERROR_LOCK_VIOLATION);
		return FALSE;
	}

	return TRUE;
}

static gboolean lock_while_writing = FALSE;

/* Some utility functions.
 */

/*
 * Check if a file is writable by the current user.
 *
 * This is is a best effort kind of thing. It assumes a reasonable sane set
 * of permissions by the underlying OS.
 *
 * We generally assume that basic unix permission bits are authoritative. Which might not
 * be the case under systems with extended permissions systems (posix ACLs, SELinux, OSX/iOS sandboxing, etc)
 *
 * The choice of access as the fallback is due to the expected lower overhead compared to trying to open the file.
 *
 * The only expected problem with using access are for root, setuid or setgid programs as access is not consistent
 * under those situations. It's to be expected that this should not happen in practice as those bits are very dangerous
 * and should not be used with a dynamic runtime.
 */
static gboolean
is_file_writable (struct stat *st, const gchar *path)
{
#if __APPLE__
	// OS X Finder "locked" or `ls -lO` "uchg".
	// This only covers one of several cases where an OS X file could be unwritable through special flags.
	if (st->st_flags & (UF_IMMUTABLE|SF_IMMUTABLE))
		return 0;
#endif

	/* Is it globally writable? */
	if (st->st_mode & S_IWOTH)
		return 1;

	/* Am I the owner? */
	if ((st->st_uid == geteuid ()) && (st->st_mode & S_IWUSR))
		return 1;

	/* Am I in the same group? */
	if ((st->st_gid == getegid ()) && (st->st_mode & S_IWGRP))
		return 1;

	/* Fallback to using access(2). It's not ideal as it might not take into consideration euid/egid
	 * but it's the only sane option we have on unix.
	 */
	return access (path, W_OK) == 0;
}


static guint32 _wapi_stat_to_file_attributes (const gchar *pathname,
					      struct stat *buf,
					      struct stat *lbuf)
{
	guint32 attrs = 0;
	gchar *filename;
	
	/* FIXME: this could definitely be better, but there seems to
	 * be no pattern to the attributes that are set
	 */

	/* Sockets (0140000) != Directory (040000) + Regular file (0100000) */
	if (S_ISSOCK (buf->st_mode))
		buf->st_mode &= ~S_IFSOCK; /* don't consider socket protection */

	filename = _wapi_basename (pathname);

	if (S_ISDIR (buf->st_mode)) {
		attrs = FILE_ATTRIBUTE_DIRECTORY;
		if (!is_file_writable (buf, pathname)) {
			attrs |= FILE_ATTRIBUTE_READONLY;
		}
		if (filename[0] == '.') {
			attrs |= FILE_ATTRIBUTE_HIDDEN;
		}
	} else {
		if (!is_file_writable (buf, pathname)) {
			attrs = FILE_ATTRIBUTE_READONLY;

			if (filename[0] == '.') {
				attrs |= FILE_ATTRIBUTE_HIDDEN;
			}
		} else if (filename[0] == '.') {
			attrs = FILE_ATTRIBUTE_HIDDEN;
		} else {
			attrs = FILE_ATTRIBUTE_NORMAL;
		}
	}

	if (lbuf != NULL) {
		if (S_ISLNK (lbuf->st_mode)) {
			attrs |= FILE_ATTRIBUTE_REPARSE_POINT;
		}
	}
	
	g_free (filename);
	
	return attrs;
}

static void
_wapi_set_last_error_from_errno (void)
{
	mono_w32error_set_last (mono_w32error_unix_to_win32 (errno));
}

static void _wapi_set_last_path_error_from_errno (const gchar *dir,
						  const gchar *path)
{
	if (errno == ENOENT) {
		/* Check the path - if it's a missing directory then
		 * we need to set PATH_NOT_FOUND not FILE_NOT_FOUND
		 */
		gchar *dirname;


		if (dir == NULL) {
			dirname = _wapi_dirname (path);
		} else {
			dirname = g_strdup (dir);
		}
		
		if (_wapi_access (dirname, F_OK) == 0) {
			mono_w32error_set_last (ERROR_FILE_NOT_FOUND);
		} else {
			mono_w32error_set_last (ERROR_PATH_NOT_FOUND);
		}

		g_free (dirname);
	} else {
		_wapi_set_last_error_from_errno ();
	}
}

static gboolean
file_read(FileHandle *filehandle, gpointer buffer, guint32 numbytes, guint32 *bytesread)
{
	gint ret;
	MonoThreadInfo *info = mono_thread_info_current ();

	if(bytesread!=NULL) {
		*bytesread=0;
	}
	
	if(!(filehandle->fileaccess & (GENERIC_READ | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_READ access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(FALSE);
	}

	do {
		MONO_ENTER_GC_SAFE;
		ret = read (((MonoFDHandle*) filehandle)->fd, buffer, numbytes);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EINTR &&
		 !mono_thread_info_is_interrupt_state (info));
			
	if(ret==-1) {
		gint err = errno;

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: read of fd %d error: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(err));
		mono_w32error_set_last (mono_w32error_unix_to_win32 (err));
		return(FALSE);
	}
		
	if (bytesread != NULL) {
		*bytesread = ret;
	}
		
	return(TRUE);
}

static gboolean
file_write(FileHandle *filehandle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten)
{
	gint ret;
	off_t current_pos = 0;
	MonoThreadInfo *info = mono_thread_info_current ();
	
	if(byteswritten!=NULL) {
		*byteswritten=0;
	}
	
	if(!(filehandle->fileaccess & GENERIC_WRITE) && !(filehandle->fileaccess & GENERIC_ALL)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_WRITE access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
	if (lock_while_writing) {
		/* Need to lock the region we're about to write to,
		 * because we only do advisory locking on POSIX
		 * systems
		 */
		MONO_ENTER_GC_SAFE;
		current_pos = lseek (((MonoFDHandle*) filehandle)->fd, (off_t)0, SEEK_CUR);
		MONO_EXIT_GC_SAFE;
		if (current_pos == -1) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d lseek failed: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror (errno));
			_wapi_set_last_error_from_errno ();
			return(FALSE);
		}
		
		if (_wapi_lock_file_region (((MonoFDHandle*) filehandle)->fd, current_pos, numbytes) == FALSE) {
			/* The error has already been set */
			return(FALSE);
		}
	}
		
	do {
		MONO_ENTER_GC_SAFE;
		ret = write (((MonoFDHandle*) filehandle)->fd, buffer, numbytes);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EINTR &&
		 !mono_thread_info_is_interrupt_state (info));
	
	if (lock_while_writing) {
		_wapi_unlock_file_region (((MonoFDHandle*) filehandle)->fd, current_pos, numbytes);
	}

	if (ret == -1) {
		if (errno == EINTR) {
			ret = 0;
		} else {
			_wapi_set_last_error_from_errno ();
				
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: write of fd %d error: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

			return(FALSE);
		}
	}
	if (byteswritten != NULL) {
		*byteswritten = ret;
	}
	return(TRUE);
}

static gboolean file_flush(FileHandle *filehandle)
{
	gint ret;

	if(!(filehandle->fileaccess & (GENERIC_WRITE | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_WRITE access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(FALSE);
	}

	MONO_ENTER_GC_SAFE;
	ret=fsync(((MonoFDHandle*) filehandle)->fd);
	MONO_EXIT_GC_SAFE;
	if (ret==-1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fsync of fd %d error: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}
	
	return(TRUE);
}

static guint32 file_seek(FileHandle *filehandle, gint32 movedistance,
			 gint32 *highmovedistance, gint method)
{
	gint64 offset, newpos;
	gint whence;
	guint32 ret;

	if(!(filehandle->fileaccess & (GENERIC_READ | GENERIC_WRITE | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_READ or GENERIC_WRITE access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(INVALID_SET_FILE_POINTER);
	}

	switch(method) {
	case FILE_BEGIN:
		whence=SEEK_SET;
		break;
	case FILE_CURRENT:
		whence=SEEK_CUR;
		break;
	case FILE_END:
		whence=SEEK_END;
		break;
	default:
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: invalid seek type %d", __func__, method);

		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		return(INVALID_SET_FILE_POINTER);
	}

#ifdef HAVE_LARGE_FILE_SUPPORT
	if(highmovedistance==NULL) {
		offset=movedistance;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: setting offset to %" G_GINT64_FORMAT " (low %" G_GINT32_FORMAT ")", __func__,
			  offset, movedistance);
	} else {
		offset=((gint64) *highmovedistance << 32) | (guint32)movedistance;
		
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: setting offset to %" G_GINT64_FORMAT " 0x%" PRIx64 " (high %" G_GINT32_FORMAT " 0x%" PRIx32 ", low %" G_GINT32_FORMAT " 0x%" PRIx32 ")",
			  __func__, offset, offset, *highmovedistance, *highmovedistance, movedistance, movedistance);
	}
#else
	offset=movedistance;
#endif

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: moving fd %d by %" G_GINT64_FORMAT " bytes from %d", __func__, ((MonoFDHandle*) filehandle)->fd, offset, whence);

#ifdef HOST_ANDROID
	/* bionic doesn't support -D_FILE_OFFSET_BITS=64 */
	MONO_ENTER_GC_SAFE;
	newpos=lseek64(((MonoFDHandle*) filehandle)->fd, offset, whence);
	MONO_EXIT_GC_SAFE;
#else
	MONO_ENTER_GC_SAFE;
	newpos=lseek(((MonoFDHandle*) filehandle)->fd, offset, whence);
	MONO_EXIT_GC_SAFE;
#endif
	if(newpos==-1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: lseek on fd %d returned error %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

		_wapi_set_last_error_from_errno ();
		return(INVALID_SET_FILE_POINTER);
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: lseek returns %" G_GINT64_FORMAT, __func__, newpos);

#ifdef HAVE_LARGE_FILE_SUPPORT
	ret=newpos & 0xFFFFFFFF;
	if(highmovedistance!=NULL) {
		*highmovedistance=newpos>>32;
	}
#else
	ret=newpos;
	if(highmovedistance!=NULL) {
		/* Accurate, but potentially dodgy :-) */
		*highmovedistance=0;
	}
#endif

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: move of fd %d returning %" G_GUINT32_FORMAT "/%" G_GINT32_FORMAT, __func__, ((MonoFDHandle*) filehandle)->fd, ret, highmovedistance==NULL?0:*highmovedistance);

	return(ret);
}

static gboolean file_setendoffile(FileHandle *filehandle)
{
	struct stat statbuf;
	off_t pos;
	gint ret;
	MonoThreadInfo *info = mono_thread_info_current ();
	
	if(!(filehandle->fileaccess & (GENERIC_WRITE | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_WRITE access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(FALSE);
	}

	/* Find the current file position, and the file length.  If
	 * the file position is greater than the length, write to
	 * extend the file with a hole.  If the file position is less
	 * than the length, truncate the file.
	 */
	
	MONO_ENTER_GC_SAFE;
	ret=fstat(((MonoFDHandle*) filehandle)->fd, &statbuf);
	MONO_EXIT_GC_SAFE;
	if(ret==-1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d fstat failed: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}

	MONO_ENTER_GC_SAFE;
	pos=lseek(((MonoFDHandle*) filehandle)->fd, (off_t)0, SEEK_CUR);
	MONO_EXIT_GC_SAFE;
	if(pos==-1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d lseek failed: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}
	
#ifdef FTRUNCATE_DOESNT_EXTEND
	off_t size = statbuf.st_size;
	/* I haven't bothered to write the configure.ac stuff for this
	 * because I don't know if any platform needs it.  I'm leaving
	 * this code just in case though
	 */
	if(pos>size) {
		/* Extend the file.  Use write() here, because some
		 * manuals say that ftruncate() behaviour is undefined
		 * when the file needs extending.  The POSIX spec says
		 * that on XSI-conformant systems it extends, so if
		 * every system we care about conforms, then we can
		 * drop this write.
		 */
		do {
			MONO_ENTER_GC_SAFE;
			ret = write (((MonoFDHandle*) filehandle)->fd, "", 1);
			MONO_EXIT_GC_SAFE;
		} while (ret == -1 && errno == EINTR &&
			 !mono_thread_info_is_interrupt_state (info));

		if(ret==-1) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d extend write failed: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

			_wapi_set_last_error_from_errno ();
			return(FALSE);
		}

		/* And put the file position back after the write */
		MONO_ENTER_GC_SAFE;
		ret = lseek (((MonoFDHandle*) filehandle)->fd, pos, SEEK_SET);
		MONO_EXIT_GC_SAFE;
		if (ret == -1) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d second lseek failed: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

			_wapi_set_last_error_from_errno ();
			return(FALSE);
		}
	}
#endif

	/* always truncate, because the extend write() adds an extra
	 * byte to the end of the file
	 */
	do {
		MONO_ENTER_GC_SAFE;
		ret=ftruncate(((MonoFDHandle*) filehandle)->fd, pos);
		MONO_EXIT_GC_SAFE;
	}
	while (ret==-1 && errno==EINTR && !mono_thread_info_is_interrupt_state (info)); 
	if(ret==-1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d ftruncate failed: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));
		
		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}
		
	return(TRUE);
}

static guint32 file_getfilesize(FileHandle *filehandle, guint32 *highsize)
{
	struct stat statbuf;
	guint32 size;
	gint ret;
	
	if(!(filehandle->fileaccess & (GENERIC_READ | GENERIC_WRITE | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_READ or GENERIC_WRITE access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(INVALID_FILE_SIZE);
	}

	/* If the file has a size with the low bits 0xFFFFFFFF the
	 * caller can't tell if this is an error, so clear the error
	 * value
	 */
	mono_w32error_set_last (ERROR_SUCCESS);
	
	MONO_ENTER_GC_SAFE;
	ret = fstat(((MonoFDHandle*) filehandle)->fd, &statbuf);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d fstat failed: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

		_wapi_set_last_error_from_errno ();
		return(INVALID_FILE_SIZE);
	}
	
	/* fstat indicates block devices as zero-length, so go a different path */
#ifdef BLKGETSIZE64
	if (S_ISBLK(statbuf.st_mode)) {
		guint64 bigsize;
		gint res;
		MONO_ENTER_GC_SAFE;
		res = ioctl (((MonoFDHandle*) filehandle)->fd, BLKGETSIZE64, &bigsize);
		MONO_EXIT_GC_SAFE;
		if (res < 0) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d ioctl BLKGETSIZE64 failed: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

			_wapi_set_last_error_from_errno ();
			return(INVALID_FILE_SIZE);
		}
		
		size = bigsize & 0xFFFFFFFF;
		if (highsize != NULL) {
			*highsize = bigsize>>32;
		}

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Returning block device size %" G_GUINT32_FORMAT "/%" G_GUINT32_FORMAT,
			   __func__, size, *highsize);
	
		return(size);
	}
#endif
	
#ifdef HAVE_LARGE_FILE_SUPPORT
	size = statbuf.st_size & 0xFFFFFFFF;
	if (highsize != NULL) {
		*highsize = statbuf.st_size>>32;
	}
#else
	if (highsize != NULL) {
		/* Accurate, but potentially dodgy :-) */
		*highsize = 0;
	}
	size = statbuf.st_size;
#endif

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Returning size %" G_GUINT32_FORMAT "/%" G_GUINT32_FORMAT, __func__, size, *highsize);
	
	return(size);
}

static gboolean file_getfiletime(FileHandle *filehandle, FILETIME *create_time,
				 FILETIME *access_time,
				 FILETIME *write_time)
{
	struct stat statbuf;
	guint64 create_ticks, access_ticks, write_ticks;
	gint ret;

	if(!(filehandle->fileaccess & (GENERIC_READ | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_READ access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
	MONO_ENTER_GC_SAFE;
	ret=fstat(((MonoFDHandle*) filehandle)->fd, &statbuf);
	MONO_EXIT_GC_SAFE;
	if(ret==-1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d fstat failed: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: atime: %ld ctime: %ld mtime: %ld", __func__,
		  statbuf.st_atime, statbuf.st_ctime,
		  statbuf.st_mtime);

	/* Try and guess a meaningful create time by using the older
	 * of atime or ctime
	 */
	/* The magic constant comes from msdn documentation
	 * "Converting a time_t Value to a File Time"
	 */
	if(statbuf.st_atime < statbuf.st_ctime) {
		create_ticks=((guint64)statbuf.st_atime*10000000)
			+ 116444736000000000ULL;
	} else {
		create_ticks=((guint64)statbuf.st_ctime*10000000)
			+ 116444736000000000ULL;
	}
	
	access_ticks=((guint64)statbuf.st_atime*10000000)+116444736000000000ULL;
	write_ticks=((guint64)statbuf.st_mtime*10000000)+116444736000000000ULL;
	
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: aticks: %" G_GUINT64_FORMAT " cticks: %" G_GUINT64_FORMAT " wticks: %" G_GUINT64_FORMAT, __func__,
		  access_ticks, create_ticks, write_ticks);

	if(create_time!=NULL) {
		create_time->dwLowDateTime = create_ticks & 0xFFFFFFFF;
		create_time->dwHighDateTime = create_ticks >> 32;
	}
	
	if(access_time!=NULL) {
		access_time->dwLowDateTime = access_ticks & 0xFFFFFFFF;
		access_time->dwHighDateTime = access_ticks >> 32;
	}
	
	if(write_time!=NULL) {
		write_time->dwLowDateTime = write_ticks & 0xFFFFFFFF;
		write_time->dwHighDateTime = write_ticks >> 32;
	}

	return(TRUE);
}

static gboolean file_setfiletime(FileHandle *filehandle,
				 const FILETIME *create_time G_GNUC_UNUSED,
				 const FILETIME *access_time,
				 const FILETIME *write_time)
{
	struct utimbuf utbuf;
	struct stat statbuf;
	guint64 access_ticks, write_ticks;
	gint ret;
	
	
	if(!(filehandle->fileaccess & (GENERIC_WRITE | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_WRITE access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(FALSE);
	}

	if(filehandle->filename == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d unknown filename", __func__, ((MonoFDHandle*) filehandle)->fd);

		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	/* Get the current times, so we can put the same times back in
	 * the event that one of the FileTime structs is NULL
	 */
	MONO_ENTER_GC_SAFE;
	ret=fstat (((MonoFDHandle*) filehandle)->fd, &statbuf);
	MONO_EXIT_GC_SAFE;
	if(ret==-1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d fstat failed: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		return(FALSE);
	}

	if(access_time!=NULL) {
		access_ticks=((guint64)access_time->dwHighDateTime << 32) +
			access_time->dwLowDateTime;
		/* This is (time_t)0.  We can actually go to INT_MIN,
		 * but this will do for now.
		 */
		if (access_ticks < 116444736000000000ULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: attempt to set access time too early",
				   __func__);
			mono_w32error_set_last (ERROR_INVALID_PARAMETER);
			return(FALSE);
		}

		if (sizeof (utbuf.actime) == 4 && ((access_ticks - 116444736000000000ULL) / 10000000) > INT_MAX) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: attempt to set write time that is too big for a 32bits time_t",
				   __func__);
			mono_w32error_set_last (ERROR_INVALID_PARAMETER);
			return(FALSE);
		}

		utbuf.actime=(access_ticks - 116444736000000000ULL) / 10000000;
	} else {
		utbuf.actime=statbuf.st_atime;
	}

	if(write_time!=NULL) {
		write_ticks=((guint64)write_time->dwHighDateTime << 32) +
			write_time->dwLowDateTime;
		/* This is (time_t)0.  We can actually go to INT_MIN,
		 * but this will do for now.
		 */
		if (write_ticks < 116444736000000000ULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: attempt to set write time too early",
				   __func__);
			mono_w32error_set_last (ERROR_INVALID_PARAMETER);
			return(FALSE);
		}
		if (sizeof (utbuf.modtime) == 4 && ((write_ticks - 116444736000000000ULL) / 10000000) > INT_MAX) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: attempt to set write time that is too big for a 32bits time_t",
				   __func__);
			mono_w32error_set_last (ERROR_INVALID_PARAMETER);
			return(FALSE);
		}
		
		utbuf.modtime=(write_ticks - 116444736000000000ULL) / 10000000;
	} else {
		utbuf.modtime=statbuf.st_mtime;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: setting fd %d access %ld write %ld", __func__,
		   ((MonoFDHandle*) filehandle)->fd, utbuf.actime, utbuf.modtime);

	ret = _wapi_utime (filehandle->filename, &utbuf);
	if (ret == -1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d [%s] utime failed: %s", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->filename, g_strerror(errno));

		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		return(FALSE);
	}
	
	return(TRUE);
}

static gboolean
console_read(FileHandle *filehandle, gpointer buffer, guint32 numbytes, guint32 *bytesread)
{
	gint ret;
	MonoThreadInfo *info = mono_thread_info_current ();

	if(bytesread!=NULL) {
		*bytesread=0;
	}
	
	if(!(filehandle->fileaccess & (GENERIC_READ | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_READ access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
	do {
		MONO_ENTER_GC_SAFE;
		ret=read(((MonoFDHandle*) filehandle)->fd, buffer, numbytes);
		MONO_EXIT_GC_SAFE;
	} while (ret==-1 && errno==EINTR && !mono_thread_info_is_interrupt_state (info));

	if(ret==-1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: read of fd %d error: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}
	
	if(bytesread!=NULL) {
		*bytesread=ret;
	}
	
	return(TRUE);
}

static gboolean
console_write(FileHandle *filehandle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten)
{
	gint ret;
	MonoThreadInfo *info = mono_thread_info_current ();
	
	if(byteswritten!=NULL) {
		*byteswritten=0;
	}
	
	if(!(filehandle->fileaccess & (GENERIC_WRITE | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_WRITE access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
	do {
		MONO_ENTER_GC_SAFE;
		ret = write(((MonoFDHandle*) filehandle)->fd, buffer, numbytes);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EINTR &&
		 !mono_thread_info_is_interrupt_state (info));

	if (ret == -1) {
		if (errno == EINTR) {
			ret = 0;
		} else {
			_wapi_set_last_error_from_errno ();
			
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: write of fd %d error: %s", __func__, ((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

			return(FALSE);
		}
	}
	if(byteswritten!=NULL) {
		*byteswritten=ret;
	}
	
	return(TRUE);
}

static gboolean
pipe_read (FileHandle *filehandle, gpointer buffer, guint32 numbytes, guint32 *bytesread)
{
	gint ret;
	MonoThreadInfo *info = mono_thread_info_current ();

	if(bytesread!=NULL) {
		*bytesread=0;
	}
	
	if(!(filehandle->fileaccess & (GENERIC_READ | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_READ access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: reading up to %" G_GUINT32_FORMAT " bytes from pipe %d", __func__, numbytes, ((MonoFDHandle*) filehandle)->fd);

	do {
		MONO_ENTER_GC_SAFE;
		ret=read(((MonoFDHandle*) filehandle)->fd, buffer, numbytes);
		MONO_EXIT_GC_SAFE;
	} while (ret==-1 && errno==EINTR && !mono_thread_info_is_interrupt_state (info));
		
	if (ret == -1) {
		if (errno == EINTR) {
			ret = 0;
		} else {
			_wapi_set_last_error_from_errno ();
			
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: read of fd %d error: %s", __func__,((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

			return(FALSE);
		}
	}
	
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: read %d bytes from pipe %d", __func__, ret, ((MonoFDHandle*) filehandle)->fd);

	if(bytesread!=NULL) {
		*bytesread=ret;
	}
	
	return(TRUE);
}

static gboolean
pipe_write(FileHandle *filehandle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten)
{
	gint ret;
	MonoThreadInfo *info = mono_thread_info_current ();
	
	if(byteswritten!=NULL) {
		*byteswritten=0;
	}
	
	if(!(filehandle->fileaccess & (GENERIC_WRITE | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_WRITE access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);

		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: writing up to %" G_GUINT32_FORMAT " bytes to pipe %d", __func__, numbytes, ((MonoFDHandle*) filehandle)->fd);

	do {
		MONO_ENTER_GC_SAFE;
		ret = write (((MonoFDHandle*) filehandle)->fd, buffer, numbytes);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EINTR &&
		 !mono_thread_info_is_interrupt_state (info));

	if (ret == -1) {
		if (errno == EINTR) {
			ret = 0;
		} else {
			_wapi_set_last_error_from_errno ();
			
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: write of fd %d error: %s", __func__,((MonoFDHandle*) filehandle)->fd, g_strerror(errno));

			return(FALSE);
		}
	}
	if(byteswritten!=NULL) {
		*byteswritten=ret;
	}
	
	return(TRUE);
}

static gint convert_flags(guint32 fileaccess, guint32 createmode)
{
	gint flags=0;
	
	switch(fileaccess) {
	case GENERIC_READ:
		flags=O_RDONLY;
		break;
	case GENERIC_WRITE:
		flags=O_WRONLY;
		break;
	case GENERIC_READ|GENERIC_WRITE:
		flags=O_RDWR;
		break;
	default:
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Unknown access type 0x%" PRIx32, __func__,
			  fileaccess);
		break;
	}

	switch(createmode) {
	case CREATE_NEW:
		flags|=O_CREAT|O_EXCL;
		break;
	case CREATE_ALWAYS:
		flags|=O_CREAT|O_TRUNC;
		break;
	case OPEN_EXISTING:
		break;
	case OPEN_ALWAYS:
		flags|=O_CREAT;
		break;
	case TRUNCATE_EXISTING:
		flags|=O_TRUNC;
		break;
	default:
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Unknown create mode 0x%" PRIx32, __func__,
			  createmode);
		break;
	}
	
	return(flags);
}

#if 0 /* unused */
static mode_t convert_perms(guint32 sharemode)
{
	mode_t perms=0600;
	
	if(sharemode&FILE_SHARE_READ) {
		perms|=044;
	}
	if(sharemode&FILE_SHARE_WRITE) {
		perms|=022;
	}

	return(perms);
}
#endif

static gboolean share_allows_open (struct stat *statbuf, guint32 sharemode,
				   guint32 fileaccess,
				   FileShare **share_info)
{
	gboolean file_already_shared;
	guint32 file_existing_share, file_existing_access;

	file_already_shared = file_share_get (statbuf->st_dev, statbuf->st_ino, sharemode, fileaccess, &file_existing_share, &file_existing_access, share_info);
	
	if (file_already_shared) {
		/* The reference to this share info was incremented
		 * when we looked it up, so be careful to put it back
		 * if we conclude we can't use this file.
		 */
		if (file_existing_share == 0) {
			/* Quick and easy, no possibility to share */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Share mode prevents open: requested access: 0x%" PRIx32 ", file has sharing = NONE", __func__, fileaccess);

			file_share_release (*share_info);
			*share_info = NULL;
			
			return(FALSE);
		}

		if (((file_existing_share == FILE_SHARE_READ) &&
		     (fileaccess != GENERIC_READ)) ||
		    ((file_existing_share == FILE_SHARE_WRITE) &&
		     (fileaccess != GENERIC_WRITE))) {
			/* New access mode doesn't match up */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Share mode prevents open: requested access: 0x%" PRIx32 ", file has sharing: 0x%" PRIx32, __func__, fileaccess, file_existing_share);

			file_share_release (*share_info);
			*share_info = NULL;
		
			return(FALSE);
		}

		if (((file_existing_access & GENERIC_READ) &&
		     !(sharemode & FILE_SHARE_READ)) ||
		    ((file_existing_access & GENERIC_WRITE) &&
		     !(sharemode & FILE_SHARE_WRITE))) {
			/* New share mode doesn't match up */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Access mode prevents open: requested share: 0x%" PRIx32 ", file has access: 0x%" PRIx32, __func__, sharemode, file_existing_access);

			file_share_release (*share_info);
			*share_info = NULL;
		
			return(FALSE);
		}
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: New file!", __func__);
	}

	return(TRUE);
}


static gboolean
share_allows_delete (struct stat *statbuf, FileShare **share_info)
{
	gboolean file_already_shared;
	guint32 file_existing_share, file_existing_access;

	file_already_shared = file_share_get (statbuf->st_dev, statbuf->st_ino, FILE_SHARE_DELETE, GENERIC_READ, &file_existing_share, &file_existing_access, share_info);

	if (file_already_shared) {
		/* The reference to this share info was incremented
		 * when we looked it up, so be careful to put it back
		 * if we conclude we can't use this file.
		 */
		if (file_existing_share == 0) {
			/* Quick and easy, no possibility to share */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Share mode prevents open: requested access: 0x%" PRIx32 ", file has sharing = NONE", __func__, (*share_info)->access);

			file_share_release (*share_info);
			*share_info = NULL;

			return(FALSE);
		}

		if (!(file_existing_share & FILE_SHARE_DELETE)) {
			/* New access mode doesn't match up */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Share mode prevents open: requested access: 0x%" PRIx32 ", file has sharing: 0x%" PRIx32, __func__, (*share_info)->access, file_existing_share);

			file_share_release (*share_info);
			*share_info = NULL;

			return(FALSE);
		}
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: New file!", __func__);
	}

	return(TRUE);
}

gpointer
mono_w32file_create(const gunichar2 *name, guint32 fileaccess, guint32 sharemode, guint32 createmode, guint32 attrs)
{
	FileHandle *filehandle;
	MonoFDType type;
	gint flags=convert_flags(fileaccess, createmode);
	/*mode_t perms=convert_perms(sharemode);*/
	/* we don't use sharemode, because that relates to sharing of
	 * the file when the file is open and is already handled by
	 * other code, perms instead are the on-disk permissions and
	 * this is a sane default.
	 */
	mode_t perms=0666;
	gchar *filename;
	gint fd, ret;
	struct stat statbuf;

	if (attrs & FILE_ATTRIBUTE_TEMPORARY)
		perms = 0600;
	
	if (attrs & FILE_ATTRIBUTE_ENCRYPTED){
		mono_w32error_set_last (ERROR_ENCRYPTION_FAILED);
		return INVALID_HANDLE_VALUE;
	}
	
	if (name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: name is NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(INVALID_HANDLE_VALUE);
	}

	filename = mono_unicode_to_external (name);
	if (filename == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(INVALID_HANDLE_VALUE);
	}
	
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Opening %s with share 0x%" PRIx32 " and access 0x%" PRIx32, __func__,
		   filename, sharemode, fileaccess);
	
	fd = _wapi_open (filename, flags, perms);
    
	/* If we were trying to open a directory with write permissions
	 * (e.g. O_WRONLY or O_RDWR), this call will fail with
	 * EISDIR. However, this is a bit bogus because calls to
	 * manipulate the directory (e.g. mono_w32file_set_times) will still work on
	 * the directory because they use other API calls
	 * (e.g. utime()). Hence, if we failed with the EISDIR error, try
	 * to open the directory again without write permission.
	 */
	if (fd == -1 && errno == EISDIR)
	{
		/* Try again but don't try to make it writable */
		fd = _wapi_open (filename, flags & ~(O_RDWR|O_WRONLY), perms);
	}
	
	if (fd == -1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Error opening file %s: %s", __func__, filename, g_strerror(errno));
		_wapi_set_last_path_error_from_errno (NULL, filename);
		g_free (filename);

		return(INVALID_HANDLE_VALUE);
	}

	MONO_ENTER_GC_SAFE;
	ret = fstat (fd, &statbuf);
	MONO_EXIT_GC_SAFE;
	if (ret == -1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fstat error of file %s: %s", __func__, filename, g_strerror (errno));
		_wapi_set_last_error_from_errno ();
		MONO_ENTER_GC_SAFE;
		close (fd);
		MONO_EXIT_GC_SAFE;

		return(INVALID_HANDLE_VALUE);
	}

#ifndef S_ISFIFO
#define S_ISFIFO(m) ((m & S_IFIFO) != 0)
#endif
	if (S_ISFIFO (statbuf.st_mode)) {
		type = MONO_FDTYPE_PIPE;
		/* maintain invariant that pipes have no filename */
		g_free (filename);
		filename = NULL;
	} else if (S_ISCHR (statbuf.st_mode)) {
		type = MONO_FDTYPE_CONSOLE;
	} else {
		type = MONO_FDTYPE_FILE;
	}

	filehandle = file_data_create (type, fd);
	filehandle->filename = filename;
	filehandle->fileaccess = fileaccess;
	filehandle->sharemode = sharemode;
	filehandle->attrs = attrs;

	if (!share_allows_open (&statbuf, filehandle->sharemode, filehandle->fileaccess, &filehandle->share_info)) {
		mono_w32error_set_last (ERROR_SHARING_VIOLATION);
		MONO_ENTER_GC_SAFE;
		close (((MonoFDHandle*) filehandle)->fd);
		MONO_EXIT_GC_SAFE;
		
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return (INVALID_HANDLE_VALUE);
	}
	if (!filehandle->share_info) {
		/* No space, so no more files can be opened */
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: No space in the share table", __func__);

		mono_w32error_set_last (ERROR_TOO_MANY_OPEN_FILES);
		MONO_ENTER_GC_SAFE;
		close (((MonoFDHandle*) filehandle)->fd);
		MONO_EXIT_GC_SAFE;
		
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return(INVALID_HANDLE_VALUE);
	}

#ifdef HAVE_POSIX_FADVISE
	if (attrs & FILE_FLAG_SEQUENTIAL_SCAN) {
		MONO_ENTER_GC_SAFE;
		posix_fadvise (((MonoFDHandle*) filehandle)->fd, 0, 0, POSIX_FADV_SEQUENTIAL);
		MONO_EXIT_GC_SAFE;
	}
	if (attrs & FILE_FLAG_RANDOM_ACCESS) {
		MONO_ENTER_GC_SAFE;
		posix_fadvise (((MonoFDHandle*) filehandle)->fd, 0, 0, POSIX_FADV_RANDOM);
		MONO_EXIT_GC_SAFE;
	}
#endif

#ifdef F_RDAHEAD
	if (attrs & FILE_FLAG_SEQUENTIAL_SCAN) {
		MONO_ENTER_GC_SAFE;
		fcntl(((MonoFDHandle*) filehandle)->fd, F_RDAHEAD, 1);
		MONO_EXIT_GC_SAFE;
	}
#endif

	mono_fdhandle_insert ((MonoFDHandle*) filehandle);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: returning handle %p", __func__, GINT_TO_POINTER(((MonoFDHandle*) filehandle)->fd));

	return GINT_TO_POINTER(((MonoFDHandle*) filehandle)->fd);
}

gboolean
mono_w32file_close (gpointer handle)
{
	if (!mono_fdhandle_close (GPOINTER_TO_INT (handle))) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	return TRUE;
}

gboolean mono_w32file_delete(const gunichar2 *name)
{
	gchar *filename;
	gint retval;
	gboolean ret = FALSE;
#if 0
	struct stat statbuf;
	FileShare *shareinfo;
#endif
	
	if(name==NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: name is NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(FALSE);
	}

	filename=mono_unicode_to_external(name);
	if(filename==NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(FALSE);
	}

#if 0
	/* Check to make sure sharing allows us to open the file for
	 * writing.  See bug 323389.
	 *
	 * Do the checks that don't need an open file descriptor, for
	 * simplicity's sake.  If we really have to do the full checks
	 * then we can implement that later.
	 */
	if (_wapi_stat (filename, &statbuf) < 0) {
		_wapi_set_last_path_error_from_errno (NULL, filename);
		g_free (filename);
		return(FALSE);
	}
	
	if (share_allows_open (&statbuf, 0, GENERIC_WRITE,
			       &shareinfo) == FALSE) {
		mono_w32error_set_last (ERROR_SHARING_VIOLATION);
		g_free (filename);
		return FALSE;
	}
	if (shareinfo)
		file_share_release (shareinfo);
#endif

	retval = _wapi_unlink (filename);
	
	if (retval == -1) {
		/* On linux, calling unlink on an non-existing file in a read-only mount will fail with EROFS.
		 * The expected behavior is for this function to return FALSE and not trigger an exception.
		 * To work around this behavior, we stat the file on failure.
		 *
		 * This was supposedly fixed on kernel 3.0 [1] but we could reproduce it with Ubuntu 16.04 which has kernel 4.4.
		 * We can't remove this workaround until the early 2020's when most Android deviced will have a fix.
		 * [1] https://github.com/torvalds/linux/commit/50338b889dc504c69e0cb316ac92d1b9e51f3c8a
		 */
		if (errno == EROFS) {
			MonoIOStat stat;
			if (mono_w32file_get_attributes_ex (name, &stat)) //The file exists, so must be due the RO file system
				errno = EROFS;
		}
		_wapi_set_last_path_error_from_errno (NULL, filename);
	} else {
		ret = TRUE;
	}

	g_free(filename);

	return(ret);
}

static gboolean
MoveFile (const gunichar2 *name, const gunichar2 *dest_name)
{
	gchar *utf8_name, *utf8_dest_name;
	gint result, errno_copy;
	struct stat stat_src, stat_dest;
	gboolean ret = FALSE;
	FileShare *shareinfo;
	
	if(name==NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: name is NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(FALSE);
	}

	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);
		
		mono_w32error_set_last (ERROR_INVALID_NAME);
		return FALSE;
	}
	
	if(dest_name==NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: name is NULL", __func__);

		g_free (utf8_name);
		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(FALSE);
	}

	utf8_dest_name = mono_unicode_to_external (dest_name);
	if (utf8_dest_name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);

		g_free (utf8_name);
		mono_w32error_set_last (ERROR_INVALID_NAME);
		return FALSE;
	}

	/*
	 * In C# land we check for the existence of src, but not for dest.
	 * We check it here and return the failure if dest exists and is not
	 * the same file as src.
	 */
	if (_wapi_stat (utf8_name, &stat_src) < 0) {
		if (errno != ENOENT || _wapi_lstat (utf8_name, &stat_src) < 0) {
			_wapi_set_last_path_error_from_errno (NULL, utf8_name);
			g_free (utf8_name);
			g_free (utf8_dest_name);
			return FALSE;
		}
	}
	
	if (!_wapi_stat (utf8_dest_name, &stat_dest)) {
		if (stat_dest.st_dev != stat_src.st_dev ||
		    stat_dest.st_ino != stat_src.st_ino) {
			g_free (utf8_name);
			g_free (utf8_dest_name);
			mono_w32error_set_last (ERROR_ALREADY_EXISTS);
			return FALSE;
		}
	}

	/* Check to make that we have delete sharing permission.
	 * See https://bugzilla.xamarin.com/show_bug.cgi?id=17009
	 *
	 * Do the checks that don't need an open file descriptor, for
	 * simplicity's sake.  If we really have to do the full checks
	 * then we can implement that later.
	 */
	if (share_allows_delete (&stat_src, &shareinfo) == FALSE) {
		mono_w32error_set_last (ERROR_SHARING_VIOLATION);
		return FALSE;
	}
	if (shareinfo) {
		file_share_release (shareinfo);
		shareinfo = NULL;
	}

	result = _wapi_rename (utf8_name, utf8_dest_name);
	errno_copy = errno;
	
	if (result == -1) {
		switch(errno_copy) {
		case EEXIST:
			mono_w32error_set_last (ERROR_ALREADY_EXISTS);
			break;

		case EXDEV:
			/* Ignore here, it is dealt with below */
			break;

		case ENOENT:
			/* We already know src exists. Must be dest that doesn't exist. */
			_wapi_set_last_path_error_from_errno (NULL, utf8_dest_name);
			break;

		default:
			_wapi_set_last_error_from_errno ();
		}
	}
	
	g_free (utf8_name);
	g_free (utf8_dest_name);

	if (result != 0 && errno_copy == EXDEV) {
		gint32 copy_error;

		if (S_ISDIR (stat_src.st_mode)) {
			mono_w32error_set_last (ERROR_NOT_SAME_DEVICE);
			return FALSE;
		}
		/* Try a copy to the new location, and delete the source */
		if (!mono_w32file_copy (name, dest_name, FALSE, &copy_error)) {
			/* mono_w32file_copy will set the error */
			return(FALSE);
		}
		
		return(mono_w32file_delete (name));
	}

	if (result == 0) {
		ret = TRUE;
	}

	return(ret);
}

static gboolean
write_file (gint src_fd, gint dest_fd, struct stat *st_src, gboolean report_errors)
{
	gint remain, n;
	gchar *buf, *wbuf;
	gint buf_size = st_src->st_blksize;
	MonoThreadInfo *info = mono_thread_info_current ();

	buf_size = buf_size < 8192 ? 8192 : (buf_size > 65536 ? 65536 : buf_size);
	buf = (gchar *) g_malloc (buf_size);

	for (;;) {
		MONO_ENTER_GC_SAFE;
		remain = read (src_fd, buf, buf_size);
		MONO_EXIT_GC_SAFE;
		if (remain < 0) {
			if (errno == EINTR && !mono_thread_info_is_interrupt_state (info))
				continue;

			if (report_errors)
				_wapi_set_last_error_from_errno ();

			g_free (buf);
			return FALSE;
		}
		if (remain == 0) {
			break;
		}

		wbuf = buf;
		while (remain > 0) {
			MONO_ENTER_GC_SAFE;
			n = write (dest_fd, wbuf, remain);
			MONO_EXIT_GC_SAFE;
			if (n < 0) {
				if (errno == EINTR && !mono_thread_info_is_interrupt_state (info))
					continue;

				if (report_errors)
					_wapi_set_last_error_from_errno ();
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: write failed.", __func__);
				g_free (buf);
				return FALSE;
			}

			remain -= n;
			wbuf += n;
		}
	}

	g_free (buf);
	return TRUE ;
}

static gboolean
CopyFile (const gunichar2 *name, const gunichar2 *dest_name, gboolean fail_if_exists)
{
	gchar *utf8_src, *utf8_dest;
	gint src_fd, dest_fd;
	struct stat st, dest_st;
	struct utimbuf dest_time;
	gboolean ret = TRUE;
	gint ret_utime;
	gint syscall_res;
	
	if(name==NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: name is NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(FALSE);
	}
	
	utf8_src = mono_unicode_to_external (name);
	if (utf8_src == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion of source returned NULL",
			   __func__);

		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		return(FALSE);
	}
	
	if(dest_name==NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: dest is NULL", __func__);

		g_free (utf8_src);
		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(FALSE);
	}
	
	utf8_dest = mono_unicode_to_external (dest_name);
	if (utf8_dest == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion of dest returned NULL",
			   __func__);

		mono_w32error_set_last (ERROR_INVALID_PARAMETER);

		g_free (utf8_src);
		
		return(FALSE);
	}
	
	src_fd = _wapi_open (utf8_src, O_RDONLY, 0);
	if (src_fd < 0) {
		_wapi_set_last_path_error_from_errno (NULL, utf8_src);
		
		g_free (utf8_src);
		g_free (utf8_dest);
		
		return(FALSE);
	}

	MONO_ENTER_GC_SAFE;
	syscall_res = fstat (src_fd, &st);
	MONO_EXIT_GC_SAFE;
	if (syscall_res < 0) {
		_wapi_set_last_error_from_errno ();

		g_free (utf8_src);
		g_free (utf8_dest);
		MONO_ENTER_GC_SAFE;
		close (src_fd);
		MONO_EXIT_GC_SAFE;
		
		return(FALSE);
	}

	/* Before trying to open/create the dest, we need to report a 'file busy'
	 * error if src and dest are actually the same file. We do the check here to take
	 * advantage of the IOMAP capability */
	if (!_wapi_stat (utf8_dest, &dest_st) && st.st_dev == dest_st.st_dev && 
			st.st_ino == dest_st.st_ino) {

		g_free (utf8_src);
		g_free (utf8_dest);
		MONO_ENTER_GC_SAFE;
		close (src_fd);
		MONO_EXIT_GC_SAFE;

		mono_w32error_set_last (ERROR_SHARING_VIOLATION);
		return (FALSE);
	}
	
	if (fail_if_exists) {
		dest_fd = _wapi_open (utf8_dest, O_WRONLY | O_CREAT | O_EXCL, st.st_mode);
	} else {
		/* FIXME: it kinda sucks that this code path potentially scans
		 * the directory twice due to the weird mono_w32error_set_last()
		 * behavior. */
		dest_fd = _wapi_open (utf8_dest, O_WRONLY | O_TRUNC, st.st_mode);
		if (dest_fd < 0) {
			/* The file does not exist, try creating it */
			dest_fd = _wapi_open (utf8_dest, O_WRONLY | O_CREAT | O_TRUNC, st.st_mode);
		} else {
			/* Apparently this error is set if we
			 * overwrite the dest file
			 */
			mono_w32error_set_last (ERROR_ALREADY_EXISTS);
		}
	}
	if (dest_fd < 0) {
		_wapi_set_last_error_from_errno ();

		g_free (utf8_src);
		g_free (utf8_dest);
		MONO_ENTER_GC_SAFE;
		close (src_fd);
		MONO_EXIT_GC_SAFE;

		return(FALSE);
	}

	if (!write_file (src_fd, dest_fd, &st, TRUE))
		ret = FALSE;

	close (src_fd);
	close (dest_fd);
	
	dest_time.modtime = st.st_mtime;
	dest_time.actime = st.st_atime;
	MONO_ENTER_GC_SAFE;
	ret_utime = utime (utf8_dest, &dest_time);
	MONO_EXIT_GC_SAFE;
	if (ret_utime == -1)
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: file [%s] utime failed: %s", __func__, utf8_dest, g_strerror(errno));
	
	g_free (utf8_src);
	g_free (utf8_dest);

	return ret;
}

static gchar*
convert_arg_to_utf8 (const gunichar2 *arg, const gchar *arg_name)
{
	gchar *utf8_ret;

	if (arg == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: %s is NULL", __func__, arg_name);
		mono_w32error_set_last (ERROR_INVALID_NAME);
		return NULL;
	}

	utf8_ret = mono_unicode_to_external (arg);
	if (utf8_ret == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion of %s returned NULL",
			   __func__, arg_name);
		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		return NULL;
	}

	return utf8_ret;
}

static gboolean
ReplaceFile (const gunichar2 *replacedFileName, const gunichar2 *replacementFileName, const gunichar2 *backupFileName, guint32 replaceFlags, gpointer exclude, gpointer reserved)
{
	gint result, backup_fd = -1,replaced_fd = -1;
	gchar *utf8_replacedFileName, *utf8_replacementFileName = NULL, *utf8_backupFileName = NULL;
	struct stat stBackup;
	gboolean ret = FALSE;

	if (!(utf8_replacedFileName = convert_arg_to_utf8 (replacedFileName, "replacedFileName")))
		return FALSE;
	if (!(utf8_replacementFileName = convert_arg_to_utf8 (replacementFileName, "replacementFileName")))
		goto replace_cleanup;
	if (backupFileName != NULL) {
		if (!(utf8_backupFileName = convert_arg_to_utf8 (backupFileName, "backupFileName")))
			goto replace_cleanup;
	}

	if (utf8_backupFileName) {
		// Open the backup file for read so we can restore the file if an error occurs.
		backup_fd = _wapi_open (utf8_backupFileName, O_RDONLY, 0);
		result = _wapi_rename (utf8_replacedFileName, utf8_backupFileName);
		if (result == -1)
			goto replace_cleanup;
	}

	result = _wapi_rename (utf8_replacementFileName, utf8_replacedFileName);
	if (result == -1) {
		_wapi_set_last_path_error_from_errno (NULL, utf8_replacementFileName);
		_wapi_rename (utf8_backupFileName, utf8_replacedFileName);
		if (backup_fd != -1 && !fstat (backup_fd, &stBackup)) {
			replaced_fd = _wapi_open (utf8_backupFileName, O_WRONLY | O_CREAT | O_TRUNC,
						  stBackup.st_mode);
			
			if (replaced_fd == -1)
				goto replace_cleanup;

			write_file (backup_fd, replaced_fd, &stBackup, FALSE);
		}

		goto replace_cleanup;
	}

	ret = TRUE;

replace_cleanup:
	g_free (utf8_replacedFileName);
	g_free (utf8_replacementFileName);
	g_free (utf8_backupFileName);
	if (backup_fd != -1) {
		MONO_ENTER_GC_SAFE;
		close (backup_fd);
		MONO_EXIT_GC_SAFE;
	}
	if (replaced_fd != -1) {
		MONO_ENTER_GC_SAFE;
		close (replaced_fd);
		MONO_EXIT_GC_SAFE;
	}
	return ret;
}

static gpointer
_wapi_stdhandle_create (gint fd, const gchar *name)
{
	gint flags;
	FileHandle *filehandle;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: creating standard handle type %s, fd %d", __func__, name, fd);

	/* Check if fd is valid */
	do {
		flags = fcntl(fd, F_GETFL);
	} while (flags == -1 && errno == EINTR);

	if (flags == -1) {
		/* Invalid fd.  Not really much point checking for EBADF
		 * specifically
		 */
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fcntl error on fd %d: %s", __func__, fd, g_strerror(errno));

		mono_w32error_set_last (mono_w32error_unix_to_win32 (errno));
		return INVALID_HANDLE_VALUE;
	}

	filehandle = file_data_create (MONO_FDTYPE_CONSOLE, fd);
	filehandle->filename = g_strdup(name);

	switch (flags & (O_RDONLY|O_WRONLY|O_RDWR)) {
	case O_RDONLY:
		filehandle->fileaccess = GENERIC_READ;
		break;
	case O_WRONLY:
		filehandle->fileaccess = GENERIC_WRITE;
		break;
	case O_RDWR:
		filehandle->fileaccess = GENERIC_READ | GENERIC_WRITE;
		break;
	default:
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Can't figure out flags 0x%x", __func__, flags);
		filehandle->fileaccess = 0;
		break;
	}

	/* some default security attributes might be needed */
	filehandle->security_attributes = 0;

	/* Apparently input handles can't be written to.  (I don't
	 * know if output or error handles can't be read from.)
	 */
	if (fd == 0)
		filehandle->fileaccess &= ~GENERIC_WRITE;

	filehandle->sharemode = 0;
	filehandle->attrs = 0;

	if (!mono_fdhandle_try_insert ((MonoFDHandle*) filehandle)) {
		/* we raced between 2 invocations of _wapi_stdhandle_create */
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return GINT_TO_POINTER(fd);
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: returning handle %p", __func__, GINT_TO_POINTER(((MonoFDHandle*) filehandle)->fd));

	return GINT_TO_POINTER(((MonoFDHandle*) filehandle)->fd);
}

enum {
	STD_INPUT_HANDLE  = -10,
	STD_OUTPUT_HANDLE = -11,
	STD_ERROR_HANDLE  = -12,
};

static gpointer
mono_w32file_get_std_handle (gint stdhandle)
{
	FileHandle **filehandle;
	gint fd;
	const gchar *name;
	
	switch(stdhandle) {
	case STD_INPUT_HANDLE:
		fd = 0;
		name = "<stdin>";
		break;

	case STD_OUTPUT_HANDLE:
		fd = 1;
		name = "<stdout>";
		break;

	case STD_ERROR_HANDLE:
		fd = 2;
		name = "<stderr>";
		break;

	default:
		g_assert_not_reached ();
	}

	if (!mono_fdhandle_lookup_and_ref(fd, (MonoFDHandle**) &filehandle)) {
		gpointer handle;

		handle = _wapi_stdhandle_create (fd, name);
		if (handle == INVALID_HANDLE_VALUE) {
			mono_w32error_set_last (ERROR_NO_MORE_FILES);
			return INVALID_HANDLE_VALUE;
		}
	}

	return GINT_TO_POINTER (fd);
}

gboolean
mono_w32file_read (gpointer handle, gpointer buffer, guint32 numbytes, guint32 *bytesread)
{
	FileHandle *filehandle;
	gboolean ret;

	if (!mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	switch (((MonoFDHandle*) filehandle)->type) {
	case MONO_FDTYPE_FILE:
		ret = file_read(filehandle, buffer, numbytes, bytesread);
		break;
	case MONO_FDTYPE_CONSOLE:
		ret = console_read(filehandle, buffer, numbytes, bytesread);
		break;
	case MONO_FDTYPE_PIPE:
		ret = pipe_read(filehandle, buffer, numbytes, bytesread);
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FALSE;
	}

	mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	return ret;
}

gboolean
mono_w32file_write (gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten)
{
	FileHandle *filehandle;
	gboolean ret;

	if (!mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	switch (((MonoFDHandle*) filehandle)->type) {
	case MONO_FDTYPE_FILE:
		ret = file_write(filehandle, buffer, numbytes, byteswritten);
		break;
	case MONO_FDTYPE_CONSOLE:
		ret = console_write(filehandle, buffer, numbytes, byteswritten);
		break;
	case MONO_FDTYPE_PIPE:
		ret = pipe_write(filehandle, buffer, numbytes, byteswritten);
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FALSE;
	}

	mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	return ret;
}

gboolean
mono_w32file_flush (gpointer handle)
{
	FileHandle *filehandle;
	gboolean ret;

	if (!mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	switch (((MonoFDHandle*) filehandle)->type) {
	case MONO_FDTYPE_FILE:
		ret = file_flush(filehandle);
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FALSE;
	}

	mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	return ret;
}

gboolean
mono_w32file_truncate (gpointer handle)
{
	FileHandle *filehandle;
	gboolean ret;

	if (!mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	switch (((MonoFDHandle*) filehandle)->type) {
	case MONO_FDTYPE_FILE:
		ret = file_setendoffile(filehandle);
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FALSE;
	}

	mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	return ret;
}

guint32
mono_w32file_seek (gpointer handle, gint32 movedistance, gint32 *highmovedistance, guint32 method)
{
	FileHandle *filehandle;
	guint32 ret;

	if (!mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return INVALID_SET_FILE_POINTER;
	}

	switch (((MonoFDHandle*) filehandle)->type) {
	case MONO_FDTYPE_FILE:
		ret = file_seek(filehandle, movedistance, highmovedistance, method);
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return INVALID_SET_FILE_POINTER;
	}

	mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	return ret;
}

gint
mono_w32file_get_type(gpointer handle)
{
	FileHandle *filehandle;
	gint ret;

	if (!mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FILE_TYPE_UNKNOWN;
	}

	switch (((MonoFDHandle*) filehandle)->type) {
	case MONO_FDTYPE_FILE:
		ret = FILE_TYPE_DISK;
		break;
	case MONO_FDTYPE_CONSOLE:
		ret = FILE_TYPE_CHAR;
		break;
	case MONO_FDTYPE_PIPE:
		ret = FILE_TYPE_PIPE;
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FILE_TYPE_UNKNOWN;
	}

	mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	return ret;
}

static guint32
GetFileSize(gpointer handle, guint32 *highsize)
{
	FileHandle *filehandle;
	guint32 ret;

	if (!mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return INVALID_FILE_SIZE;
	}

	switch (((MonoFDHandle*) filehandle)->type) {
	case MONO_FDTYPE_FILE:
		ret = file_getfilesize(filehandle, highsize);
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return INVALID_FILE_SIZE;
	}

	mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	return ret;
}

gboolean
mono_w32file_get_times(gpointer handle, FILETIME *create_time, FILETIME *access_time, FILETIME *write_time)
{
	FileHandle *filehandle;
	gboolean ret;

	if (!mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	switch (((MonoFDHandle*) filehandle)->type) {
	case MONO_FDTYPE_FILE:
		ret = file_getfiletime(filehandle, create_time, access_time, write_time);
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FALSE;
	}

	mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	return ret;
}

gboolean
mono_w32file_set_times(gpointer handle, const FILETIME *create_time, const FILETIME *access_time, const FILETIME *write_time)
{
	FileHandle *filehandle;
	gboolean ret;

	if (!mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	switch (((MonoFDHandle*) filehandle)->type) {
	case MONO_FDTYPE_FILE:
		ret = file_setfiletime(filehandle, create_time, access_time, write_time);
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FALSE;
	}

	mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	return ret;
}

/* A tick is a 100-nanosecond interval.  File time epoch is Midnight,
 * January 1 1601 GMT
 */

#define TICKS_PER_MILLISECOND 10000L
#define TICKS_PER_SECOND 10000000L
#define TICKS_PER_MINUTE 600000000L
#define TICKS_PER_HOUR 36000000000LL
#define TICKS_PER_DAY 864000000000LL

#define isleap(y) ((y) % 4 == 0 && ((y) % 100 != 0 || (y) % 400 == 0))

static const guint16 mon_yday[2][13]={
	{0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365},
	{0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366},
};

gboolean
mono_w32file_filetime_to_systemtime(const FILETIME *file_time, SYSTEMTIME *system_time)
{
	gint64 file_ticks, totaldays, rem, y;
	const guint16 *ip;
	
	if(system_time==NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: system_time NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		return(FALSE);
	}
	
	file_ticks=((gint64)file_time->dwHighDateTime << 32) +
		file_time->dwLowDateTime;
	
	/* Really compares if file_ticks>=0x8000000000000000
	 * (LLONG_MAX+1) but we're working with a signed value for the
	 * year and day calculation to work later
	 */
	if(file_ticks<0) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: file_time too big", __func__);

		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		return(FALSE);
	}

	totaldays=(file_ticks / TICKS_PER_DAY);
	rem = file_ticks % TICKS_PER_DAY;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: totaldays: %" G_GINT64_FORMAT " rem: %" G_GINT64_FORMAT, __func__,
		  totaldays, rem);

	system_time->wHour=rem/TICKS_PER_HOUR;
	rem %= TICKS_PER_HOUR;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Hour: %d rem: %" G_GINT64_FORMAT,  __func__,
		  system_time->wHour, rem);
	
	system_time->wMinute = rem / TICKS_PER_MINUTE;
	rem %= TICKS_PER_MINUTE;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Minute: %d rem: %" G_GINT64_FORMAT, __func__,
		  system_time->wMinute, rem);
	
	system_time->wSecond = rem / TICKS_PER_SECOND;
	rem %= TICKS_PER_SECOND;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Second: %d rem: %" G_GINT64_FORMAT, __func__,
		  system_time->wSecond, rem);
	
	system_time->wMilliseconds = rem / TICKS_PER_MILLISECOND;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Milliseconds: %d", __func__,
		  system_time->wMilliseconds);

	/* January 1, 1601 was a Monday, according to Emacs calendar */
	system_time->wDayOfWeek = ((1 + totaldays) % 7) + 1;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Day of week: %d", __func__, system_time->wDayOfWeek);
	
	/* This algorithm to find year and month given days from epoch
	 * from glibc
	 */
	y=1601;
	
#define DIV(a, b) ((a) / (b) - ((a) % (b) < 0))
#define LEAPS_THRU_END_OF(y) (DIV(y, 4) - DIV (y, 100) + DIV (y, 400))

	while(totaldays < 0 || totaldays >= (isleap(y)?366:365)) {
		/* Guess a corrected year, assuming 365 days per year */
		gint64 yg = y + totaldays / 365 - (totaldays % 365 < 0);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: totaldays: %" G_GINT64_FORMAT " yg: %" G_GINT64_FORMAT " y: %" G_GINT64_FORMAT, __func__,
			  totaldays, yg, y);
		g_message("%s: LEAPS(yg): %li LEAPS(y): %li", __func__,
			  LEAPS_THRU_END_OF(yg-1), LEAPS_THRU_END_OF(y-1));
		
		/* Adjust days and y to match the guessed year. */
		totaldays -= ((yg - y) * 365
			      + LEAPS_THRU_END_OF (yg - 1)
			      - LEAPS_THRU_END_OF (y - 1));
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: totaldays: %" G_GINT64_FORMAT,
			  __func__, totaldays);
		y = yg;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: y: %" G_GINT64_FORMAT, __func__, y);
	}
	
	system_time->wYear = y;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Year: %d", __func__, system_time->wYear);

	ip = mon_yday[isleap(y)];
	
	for(y=11; totaldays < ip[y]; --y) {
		continue;
	}
	totaldays-=ip[y];
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: totaldays: %" G_GINT64_FORMAT, __func__, totaldays);
	
	system_time->wMonth = y + 1;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Month: %d", __func__, system_time->wMonth);

	system_time->wDay = totaldays + 1;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Day: %d", __func__, system_time->wDay);
	
	return(TRUE);
}

static void
findhandle_destroy (gpointer data)
{
	FindHandle *findhandle;

	findhandle = (FindHandle*) data;
	g_assert (findhandle);

	mono_coop_mutex_destroy (&findhandle->mutex);

	if (findhandle->namelist)
		g_strfreev (findhandle->namelist);
	if (findhandle->dir_part)
		g_free (findhandle->dir_part);

	g_free (findhandle);
}

static FindHandle*
findhandle_create (void)
{
	FindHandle* findhandle;

	findhandle = g_new0 (FindHandle, 1);
	mono_refcount_init (findhandle, findhandle_destroy);

	mono_coop_mutex_init (&findhandle->mutex);

	return findhandle;
}

static void
findhandle_insert (FindHandle *findhandle)
{
	mono_coop_mutex_lock (&finds_mutex);

	if (g_hash_table_lookup_extended (finds, (gpointer) findhandle, NULL, NULL))
		g_error("%s: duplicate Find handle %p", __func__, (gpointer) findhandle);

	g_hash_table_insert (finds, (gpointer) findhandle, findhandle);

	mono_coop_mutex_unlock (&finds_mutex);
}

static gboolean
findhandle_lookup_and_ref (gpointer handle, FindHandle **findhandle)
{
	mono_coop_mutex_lock (&finds_mutex);

	if (!g_hash_table_lookup_extended (finds, handle, NULL, (gpointer*) findhandle)) {
		mono_coop_mutex_unlock (&finds_mutex);
		return FALSE;
	}

	mono_refcount_inc (*findhandle);

	mono_coop_mutex_unlock (&finds_mutex);

	return TRUE;
}

static void
findhandle_unref (FindHandle *findhandle)
{
	mono_refcount_dec (findhandle);
}

static gboolean
findhandle_close (gpointer handle)
{
	FindHandle *findhandle;
	gboolean removed;

	mono_coop_mutex_lock (&finds_mutex);

	if (!g_hash_table_lookup_extended (finds, handle, NULL, (gpointer*) &findhandle)) {
		mono_coop_mutex_unlock (&finds_mutex);

		return FALSE;
	}

	removed = g_hash_table_remove (finds, (gpointer) findhandle);
	g_assert (removed);

	mono_coop_mutex_unlock (&finds_mutex);

	return TRUE;
}

gpointer
mono_w32file_find_first (const gunichar2 *pattern, WIN32_FIND_DATA *find_data)
{
	FindHandle *findhandle;
	gchar *utf8_pattern = NULL, *dir_part, *entry_part, **namelist;
	gint result;
	
	if (pattern == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: pattern is NULL", __func__);

		mono_w32error_set_last (ERROR_PATH_NOT_FOUND);
		return(INVALID_HANDLE_VALUE);
	}

	utf8_pattern = mono_unicode_to_external (pattern);
	if (utf8_pattern == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);
		
		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(INVALID_HANDLE_VALUE);
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: looking for [%s]", __func__, utf8_pattern);
	
	/* Figure out which bit of the pattern is the directory */
	dir_part = _wapi_dirname (utf8_pattern);
	entry_part = _wapi_basename (utf8_pattern);

#if 0
	/* Don't do this check for now, it breaks if directories
	 * really do have metachars in their names (see bug 58116).
	 * FIXME: Figure out a better solution to keep some checks...
	 */
	if (strchr (dir_part, '*') || strchr (dir_part, '?')) {
		mono_w32error_set_last (ERROR_INVALID_NAME);
		g_free (dir_part);
		g_free (entry_part);
		g_free (utf8_pattern);
		return(INVALID_HANDLE_VALUE);
	}
#endif

	/* The pattern can specify a directory or a set of files.
	 *
	 * The pattern can have wildcard characters ? and *, but only
	 * in the section after the last directory delimiter.  (Return
	 * ERROR_INVALID_NAME if there are wildcards in earlier path
	 * sections.)  "*" has the usual 0-or-more chars meaning.  "?" 
	 * means "match one character", "??" seems to mean "match one
	 * or two characters", "???" seems to mean "match one, two or
	 * three characters", etc.  Windows will also try and match
	 * the mangled "short name" of files, so 8 character patterns
	 * with wildcards will show some surprising results.
	 *
	 * All the written documentation I can find says that '?' 
	 * should only match one character, and doesn't mention '??',
	 * '???' etc.  I'm going to assume that the strict behaviour
	 * (ie '???' means three and only three characters) is the
	 * correct one, because that lets me use fnmatch(3) rather
	 * than mess around with regexes.
	 */

	namelist = NULL;
	result = _wapi_io_scandir (dir_part, entry_part,
				   &namelist);
	
	if (result == 0) {
		/* No files, which windows seems to call
		 * FILE_NOT_FOUND
		 */
		mono_w32error_set_last (ERROR_FILE_NOT_FOUND);
		g_free (utf8_pattern);
		g_free (entry_part);
		g_free (dir_part);
		g_strfreev (namelist);
		return (INVALID_HANDLE_VALUE);
	}
	
	if (result < 0) {
		_wapi_set_last_path_error_from_errno (dir_part, NULL);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: scandir error: %s", __func__, g_strerror (errno));
		g_free (utf8_pattern);
		g_free (entry_part);
		g_free (dir_part);
		g_strfreev (namelist);
		return (INVALID_HANDLE_VALUE);
	}

	g_free (utf8_pattern);
	g_free (entry_part);
	
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Got %d matches", __func__, result);

	findhandle = findhandle_create ();
	findhandle->namelist = namelist;
	findhandle->dir_part = dir_part;
	findhandle->num = result;
	findhandle->count = 0;

	findhandle_insert (findhandle);

	if (!mono_w32file_find_next ((gpointer) findhandle, find_data)) {
		mono_w32file_find_close ((gpointer) findhandle);
		mono_w32error_set_last (ERROR_NO_MORE_FILES);
		return INVALID_HANDLE_VALUE;
	}

	return (gpointer) findhandle;
}

gboolean
mono_w32file_find_next (gpointer handle, WIN32_FIND_DATA *find_data)
{
	FindHandle *findhandle;
	struct stat buf, linkbuf;
	gint result;
	gchar *filename;
	gchar *utf8_filename, *utf8_basename;
	gunichar2 *utf16_basename;
	time_t create_time;
	glong bytes;
	gboolean ret = FALSE;

	if (!findhandle_lookup_and_ref (handle, &findhandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	mono_coop_mutex_lock (&findhandle->mutex);

retry:
	if (findhandle->count >= findhandle->num) {
		mono_w32error_set_last (ERROR_NO_MORE_FILES);
		goto cleanup;
	}

	/* stat next match */

	filename = g_build_filename (findhandle->dir_part, findhandle->namelist[findhandle->count ++], NULL);

	result = _wapi_stat (filename, &buf);
	if (result == -1 && errno == ENOENT) {
		/* Might be a dangling symlink */
		result = _wapi_lstat (filename, &buf);
	}
	
	if (result != 0) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: stat failed: %s", __func__, filename);

		g_free (filename);
		goto retry;
	}

	result = _wapi_lstat (filename, &linkbuf);
	if (result != 0) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: lstat failed: %s", __func__, filename);

		g_free (filename);
		goto retry;
	}

	utf8_filename = mono_utf8_from_external (filename);
	if (utf8_filename == NULL) {
		/* We couldn't turn this filename into utf8 (eg the
		 * encoding of the name wasn't convertible), so just
		 * ignore it.
		 */
		g_warning ("%s: Bad encoding for '%s'\nConsider using MONO_EXTERNAL_ENCODINGS\n", __func__, filename);
		
		g_free (filename);
		goto retry;
	}
	g_free (filename);
	
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Found [%s]", __func__, utf8_filename);
	
	/* fill data block */

	if (buf.st_mtime < buf.st_ctime)
		create_time = buf.st_mtime;
	else
		create_time = buf.st_ctime;
	
	find_data->dwFileAttributes = _wapi_stat_to_file_attributes (utf8_filename, &buf, &linkbuf);

	time_t_to_filetime (create_time, &find_data->ftCreationTime);
	time_t_to_filetime (buf.st_atime, &find_data->ftLastAccessTime);
	time_t_to_filetime (buf.st_mtime, &find_data->ftLastWriteTime);

	if (find_data->dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
		find_data->nFileSizeHigh = 0;
		find_data->nFileSizeLow = 0;
	} else {
		find_data->nFileSizeHigh = buf.st_size >> 32;
		find_data->nFileSizeLow = buf.st_size & 0xFFFFFFFF;
	}

	find_data->dwReserved0 = 0;
	find_data->dwReserved1 = 0;

	utf8_basename = _wapi_basename (utf8_filename);
	utf16_basename = g_utf8_to_utf16 (utf8_basename, -1, NULL, &bytes,
					  NULL);
	if(utf16_basename==NULL) {
		g_free (utf8_basename);
		g_free (utf8_filename);
		goto retry;
	}
	ret = TRUE;
	
	/* utf16 is 2 * utf8 */
	bytes *= 2;

	memset (find_data->cFileName, '\0', (MAX_PATH*2));

	/* Truncating a utf16 string like this might leave the last
	 * gchar incomplete
	 */
	memcpy (find_data->cFileName, utf16_basename,
		bytes<(MAX_PATH*2)-2?bytes:(MAX_PATH*2)-2);

	find_data->cAlternateFileName [0] = 0;	/* not used */

	g_free (utf8_basename);
	g_free (utf8_filename);
	g_free (utf16_basename);

cleanup:
	mono_coop_mutex_unlock (&findhandle->mutex);

	findhandle_unref (findhandle);
	
	return(ret);
}

gboolean
mono_w32file_find_close (gpointer handle)
{
	if (!findhandle_close (handle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	return TRUE;
}

gboolean
mono_w32file_create_directory (const gunichar2 *name)
{
	gchar *utf8_name;
	gint result;
	
	if (name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: name is NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(FALSE);
	}
	
	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);
	
		mono_w32error_set_last (ERROR_INVALID_NAME);
		return FALSE;
	}

	result = _wapi_mkdir (utf8_name, 0777);

	if (result == 0) {
		g_free (utf8_name);
		return TRUE;
	}

	_wapi_set_last_path_error_from_errno (NULL, utf8_name);
	g_free (utf8_name);
	return FALSE;
}

gboolean
mono_w32file_remove_directory (const gunichar2 *name)
{
	gchar *utf8_name;
	gint result;
	
	if (name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: name is NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(FALSE);
	}

	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);
		
		mono_w32error_set_last (ERROR_INVALID_NAME);
		return FALSE;
	}

	result = _wapi_rmdir (utf8_name);
	if (result == -1) {
		_wapi_set_last_path_error_from_errno (NULL, utf8_name);
		g_free (utf8_name);
		
		return(FALSE);
	}
	g_free (utf8_name);

	return(TRUE);
}

guint32
mono_w32file_get_attributes (const gunichar2 *name)
{
	gchar *utf8_name;
	struct stat buf, linkbuf;
	gint result;
	guint32 ret;
	
	if (name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: name is NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(FALSE);
	}
	
	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		return (INVALID_FILE_ATTRIBUTES);
	}

	result = _wapi_stat (utf8_name, &buf);
	if (result == -1 && (errno == ENOENT || errno == ELOOP)) {
		/* Might be a dangling symlink... */
		result = _wapi_lstat (utf8_name, &buf);
	}

	if (result != 0) {
		_wapi_set_last_path_error_from_errno (NULL, utf8_name);
		g_free (utf8_name);
		return (INVALID_FILE_ATTRIBUTES);
	}

	result = _wapi_lstat (utf8_name, &linkbuf);
	if (result != 0) {
		_wapi_set_last_path_error_from_errno (NULL, utf8_name);
		g_free (utf8_name);
		return (INVALID_FILE_ATTRIBUTES);
	}
	
	ret = _wapi_stat_to_file_attributes (utf8_name, &buf, &linkbuf);
	
	g_free (utf8_name);

	return(ret);
}

gboolean
mono_w32file_get_attributes_ex (const gunichar2 *name, MonoIOStat *stat)
{
	gchar *utf8_name;

	struct stat buf, linkbuf;
	gint result;
	
	if (name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: name is NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(FALSE);
	}

	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		return FALSE;
	}

	result = _wapi_stat (utf8_name, &buf);
	if (result == -1 && errno == ENOENT) {
		/* Might be a dangling symlink... */
		result = _wapi_lstat (utf8_name, &buf);
	}
	
	if (result != 0) {
		_wapi_set_last_path_error_from_errno (NULL, utf8_name);
		g_free (utf8_name);
		return FALSE;
	}

	result = _wapi_lstat (utf8_name, &linkbuf);
	if (result != 0) {
		_wapi_set_last_path_error_from_errno (NULL, utf8_name);
		g_free (utf8_name);
		return(FALSE);
	}

	/* fill stat block */

	stat->attributes = _wapi_stat_to_file_attributes (utf8_name, &buf, &linkbuf);
	stat->creation_time = (((guint64) (buf.st_mtime < buf.st_ctime ? buf.st_mtime : buf.st_ctime)) * 10 * 1000 * 1000) + 116444736000000000ULL;
	stat->last_access_time = (((guint64) (buf.st_atime)) * 10 * 1000 * 1000) + 116444736000000000ULL;
	stat->last_write_time = (((guint64) (buf.st_mtime)) * 10 * 1000 * 1000) + 116444736000000000ULL;
	stat->length = (stat->attributes & FILE_ATTRIBUTE_DIRECTORY) ? 0 : buf.st_size;

	g_free (utf8_name);
	return TRUE;
}

gboolean
mono_w32file_set_attributes (const gunichar2 *name, guint32 attrs)
{
	/* FIXME: think of something clever to do on unix */
	gchar *utf8_name;
	struct stat buf;
	gint result;

	/*
	 * Currently we only handle one *internal* case, with a value that is
	 * not standard: 0x80000000, which means `set executable bit'
	 */
	
	if (name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: name is NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return(FALSE);
	}

	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);

		mono_w32error_set_last (ERROR_INVALID_NAME);
		return FALSE;
	}

	result = _wapi_stat (utf8_name, &buf);
	if (result == -1 && errno == ENOENT) {
		/* Might be a dangling symlink... */
		result = _wapi_lstat (utf8_name, &buf);
	}

	if (result != 0) {
		_wapi_set_last_path_error_from_errno (NULL, utf8_name);
		g_free (utf8_name);
		return FALSE;
	}

	/* Contrary to the documentation, ms allows NORMAL to be
	 * specified along with other attributes, so dont bother to
	 * catch that case here.
	 */
	if (attrs & FILE_ATTRIBUTE_READONLY) {
		result = _wapi_chmod (utf8_name, buf.st_mode & ~(S_IWUSR | S_IWOTH | S_IWGRP));
	} else {
		result = _wapi_chmod (utf8_name, buf.st_mode | S_IWUSR);
	}

	/* Ignore the other attributes for now */

	if (attrs & 0x80000000){
		mode_t exec_mask = 0;

		if ((buf.st_mode & S_IRUSR) != 0)
			exec_mask |= S_IXUSR;

		if ((buf.st_mode & S_IRGRP) != 0)
			exec_mask |= S_IXGRP;

		if ((buf.st_mode & S_IROTH) != 0)
			exec_mask |= S_IXOTH;

		MONO_ENTER_GC_SAFE;
		result = chmod (utf8_name, buf.st_mode | exec_mask);
		MONO_EXIT_GC_SAFE;
	}
	/* Don't bother to reset executable (might need to change this
	 * policy)
	 */
	
	g_free (utf8_name);

	return(TRUE);
}

guint32
mono_w32file_get_cwd (guint32 length, gunichar2 *buffer)
{
	gunichar2 *utf16_path;
	glong count;
	gsize bytes;

	if (getcwd ((gchar*)buffer, length) == NULL) {
		if (errno == ERANGE) { /*buffer length is not big enough */ 
			gchar *path = g_get_current_dir (); /*FIXME g_get_current_dir doesn't work with broken paths and calling it just to know the path length is silly*/
			if (path == NULL)
				return 0;
			utf16_path = mono_unicode_from_external (path, &bytes);
			g_free (utf16_path);
			g_free (path);
			return (bytes/2)+1;
		}
		_wapi_set_last_error_from_errno ();
		return 0;
	}

	utf16_path = mono_unicode_from_external ((gchar*)buffer, &bytes);
	count = (bytes/2)+1;
	g_assert (count <= length); /*getcwd must have failed before with ERANGE*/

	/* Add the terminator */
	memset (buffer, '\0', bytes+2);
	memcpy (buffer, utf16_path, bytes);
	
	g_free (utf16_path);

	return count;
}

gboolean
mono_w32file_set_cwd (const gunichar2 *path)
{
	gchar *utf8_path;
	gboolean result;

	if (path == NULL) {
		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		return(FALSE);
	}
	
	utf8_path = mono_unicode_to_external (path);
	if (_wapi_chdir (utf8_path) != 0) {
		_wapi_set_last_error_from_errno ();
		result = FALSE;
	}
	else
		result = TRUE;

	g_free (utf8_path);
	return result;
}

gboolean
mono_w32file_create_pipe (gpointer *readpipe, gpointer *writepipe, guint32 size)
{
	FileHandle *read_filehandle, *write_filehandle;
	gint filedes[2];
	gint ret;
	
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Creating pipe", __func__);

	MONO_ENTER_GC_SAFE;
	ret=pipe (filedes);
	MONO_EXIT_GC_SAFE;
	if (ret==-1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Error creating pipe: (%d) %s",
			__func__, errno, g_strerror (errno));

		_wapi_set_last_error_from_errno ();
		return FALSE;
	}

	/* filedes[0] is open for reading, filedes[1] for writing */

	read_filehandle = file_data_create (MONO_FDTYPE_PIPE, filedes[0]);
	read_filehandle->fileaccess = GENERIC_READ;

	write_filehandle = file_data_create (MONO_FDTYPE_PIPE, filedes[1]);
	write_filehandle->fileaccess = GENERIC_WRITE;

	mono_fdhandle_insert ((MonoFDHandle*) read_filehandle);
	mono_fdhandle_insert ((MonoFDHandle*) write_filehandle);

	*readpipe = GINT_TO_POINTER(((MonoFDHandle*) read_filehandle)->fd);
	*writepipe = GINT_TO_POINTER(((MonoFDHandle*) write_filehandle)->fd);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Returning pipe: read handle %p, write handle %p",
		__func__, GINT_TO_POINTER(((MonoFDHandle*) read_filehandle)->fd), GINT_TO_POINTER(((MonoFDHandle*) write_filehandle)->fd));

	return(TRUE);
}

#ifdef HAVE_GETFSSTAT
/* Darwin has getfsstat */
gint32
mono_w32file_get_logical_drive (guint32 len, gunichar2 *buf)
{
	struct statfs *stats;
	gint size, n, i;
	gunichar2 *dir;
	glong length, total = 0;
	gint syscall_res;

	MONO_ENTER_GC_SAFE;
	n = getfsstat (NULL, 0, MNT_NOWAIT);
	MONO_EXIT_GC_SAFE;
	if (n == -1)
		return 0;
	size = n * sizeof (struct statfs);
	stats = (struct statfs *) g_malloc (size);
	if (stats == NULL)
		return 0;
	MONO_ENTER_GC_SAFE;
	syscall_res = getfsstat (stats, size, MNT_NOWAIT);
	MONO_EXIT_GC_SAFE;
	if (syscall_res == -1){
		g_free (stats);
		return 0;
	}
	for (i = 0; i < n; i++){
		dir = g_utf8_to_utf16 (stats [i].f_mntonname, -1, NULL, &length, NULL);
		if (total + length < len){
			memcpy (buf + total, dir, sizeof (gunichar2) * length);
			buf [total+length] = 0;
		} 
		g_free (dir);
		total += length + 1;
	}
	if (total < len)
		buf [total] = 0;
	total++;
	g_free (stats);
	return total;
}
#else
/* In-place octal sequence replacement */
static void
unescape_octal (gchar *str)
{
	gchar *rptr;
	gchar *wptr;

	if (str == NULL)
		return;

	rptr = wptr = str;
	while (*rptr != '\0') {
		if (*rptr == '\\') {
			gchar c;
			rptr++;
			c = (*(rptr++) - '0') << 6;
			c += (*(rptr++) - '0') << 3;
			c += *(rptr++) - '0';
			*wptr++ = c;
		} else if (wptr != rptr) {
			*wptr++ = *rptr++;
		} else {
			rptr++; wptr++;
		}
	}
	*wptr = '\0';
}
static gint32 GetLogicalDriveStrings_Mtab (guint32 len, gunichar2 *buf);

#if __linux__
#define GET_LOGICAL_DRIVE_STRINGS_BUFFER 512
#define GET_LOGICAL_DRIVE_STRINGS_MOUNTPOINT_BUFFER 512
#define GET_LOGICAL_DRIVE_STRINGS_FSNAME_BUFFER 64

typedef struct 
{
	glong total;
	guint32 buffer_index;
	guint32 mountpoint_index;
	guint32 field_number;
	guint32 allocated_size;
	guint32 fsname_index;
	guint32 fstype_index;
	gchar mountpoint [GET_LOGICAL_DRIVE_STRINGS_MOUNTPOINT_BUFFER + 1];
	gchar *mountpoint_allocated;
	gchar buffer [GET_LOGICAL_DRIVE_STRINGS_BUFFER];
	gchar fsname [GET_LOGICAL_DRIVE_STRINGS_FSNAME_BUFFER + 1];
	gchar fstype [GET_LOGICAL_DRIVE_STRINGS_FSNAME_BUFFER + 1];
	ssize_t nbytes;
	gchar delimiter;
	gboolean check_mount_source;
} LinuxMountInfoParseState;

static gboolean GetLogicalDriveStrings_Mounts (guint32 len, gunichar2 *buf, LinuxMountInfoParseState *state);
static gboolean GetLogicalDriveStrings_MountInfo (guint32 len, gunichar2 *buf, LinuxMountInfoParseState *state);
static void append_to_mountpoint (LinuxMountInfoParseState *state);
static gboolean add_drive_string (guint32 len, gunichar2 *buf, LinuxMountInfoParseState *state);

gint32
mono_w32file_get_logical_drive (guint32 len, gunichar2 *buf)
{
	gint fd;
	gint32 ret = 0;
	LinuxMountInfoParseState state;
	gboolean (*parser)(guint32, gunichar2*, LinuxMountInfoParseState*) = NULL;

	memset (buf, 0, len * sizeof (gunichar2));
	MONO_ENTER_GC_SAFE;
	fd = open ("/proc/self/mountinfo", O_RDONLY);
	MONO_EXIT_GC_SAFE;
	if (fd != -1)
		parser = GetLogicalDriveStrings_MountInfo;
	else {
		MONO_ENTER_GC_SAFE;
		fd = open ("/proc/mounts", O_RDONLY);
		MONO_EXIT_GC_SAFE;
		if (fd != -1)
			parser = GetLogicalDriveStrings_Mounts;
	}

	if (!parser) {
		ret = GetLogicalDriveStrings_Mtab (len, buf);
		goto done_and_out;
	}

	memset (&state, 0, sizeof (LinuxMountInfoParseState));
	state.field_number = 1;
	state.delimiter = ' ';

	while (1) {
		MONO_ENTER_GC_SAFE;
		state.nbytes = read (fd, state.buffer, GET_LOGICAL_DRIVE_STRINGS_BUFFER);
		MONO_EXIT_GC_SAFE;
		if (!(state.nbytes > 0))
			break;
		state.buffer_index = 0;

		while ((*parser)(len, buf, &state)) {
			if (state.buffer [state.buffer_index] == '\n') {
				gboolean quit = add_drive_string (len, buf, &state);
				state.field_number = 1;
				state.buffer_index++;
				if (state.mountpoint_allocated) {
					g_free (state.mountpoint_allocated);
					state.mountpoint_allocated = NULL;
				}
				if (quit) {
					ret = state.total;
					goto done_and_out;
				}
			}
		}
	};
	ret = state.total;

  done_and_out:
	if (fd != -1) {
		MONO_ENTER_GC_SAFE;
		close (fd);
		MONO_EXIT_GC_SAFE;
	}
	return ret;
}

static gboolean GetLogicalDriveStrings_Mounts (guint32 len, gunichar2 *buf, LinuxMountInfoParseState *state)
{
	gchar *ptr;

	if (state->field_number == 1)
		state->check_mount_source = TRUE;

	while (state->buffer_index < (guint32)state->nbytes) {
		if (state->buffer [state->buffer_index] == state->delimiter) {
			state->field_number++;
			switch (state->field_number) {
				case 2:
					state->mountpoint_index = 0;
					break;

				case 3:
					if (state->mountpoint_allocated)
						state->mountpoint_allocated [state->mountpoint_index] = 0;
					else
						state->mountpoint [state->mountpoint_index] = 0;
					break;

				default:
					ptr = (gchar*)memchr (state->buffer + state->buffer_index, '\n', GET_LOGICAL_DRIVE_STRINGS_BUFFER - state->buffer_index);
					if (ptr)
						state->buffer_index = (ptr - (gchar*)state->buffer) - 1;
					else
						state->buffer_index = state->nbytes;
					return TRUE;
			}
			state->buffer_index++;
			continue;
		} else if (state->buffer [state->buffer_index] == '\n')
			return TRUE;

		switch (state->field_number) {
			case 1:
				if (state->check_mount_source) {
					if (state->fsname_index == 0 && state->buffer [state->buffer_index] == '/') {
						/* We can ignore the rest, it's a device
						 * path */
						state->check_mount_source = FALSE;
						state->fsname [state->fsname_index++] = '/';
						break;
					}
					if (state->fsname_index < GET_LOGICAL_DRIVE_STRINGS_FSNAME_BUFFER)
						state->fsname [state->fsname_index++] = state->buffer [state->buffer_index];
				}
				break;

			case 2:
				append_to_mountpoint (state);
				break;

			case 3:
				if (state->fstype_index < GET_LOGICAL_DRIVE_STRINGS_FSNAME_BUFFER)
					state->fstype [state->fstype_index++] = state->buffer [state->buffer_index];
				break;
		}

		state->buffer_index++;
	}

	return FALSE;
}

static gboolean GetLogicalDriveStrings_MountInfo (guint32 len, gunichar2 *buf, LinuxMountInfoParseState *state)
{
	while (state->buffer_index < (guint32)state->nbytes) {
		if (state->buffer [state->buffer_index] == state->delimiter) {
			state->field_number++;
			switch (state->field_number) {
				case 5:
					state->mountpoint_index = 0;
					break;

				case 6:
					if (state->mountpoint_allocated)
						state->mountpoint_allocated [state->mountpoint_index] = 0;
					else
						state->mountpoint [state->mountpoint_index] = 0;
					break;

				case 7:
					state->delimiter = '-';
					break;

				case 8:
					state->delimiter = ' ';
					break;

				case 10:
					state->check_mount_source = TRUE;
					break;
			}
			state->buffer_index++;
			continue;
		} else if (state->buffer [state->buffer_index] == '\n')
			return TRUE;

		switch (state->field_number) {
			case 5:
				append_to_mountpoint (state);
				break;

			case 9:
				if (state->fstype_index < GET_LOGICAL_DRIVE_STRINGS_FSNAME_BUFFER)
					state->fstype [state->fstype_index++] = state->buffer [state->buffer_index];
				break;

			case 10:
				if (state->check_mount_source) {
					if (state->fsname_index == 0 && state->buffer [state->buffer_index] == '/') {
						/* We can ignore the rest, it's a device
						 * path */
						state->check_mount_source = FALSE;
						state->fsname [state->fsname_index++] = '/';
						break;
					}
					if (state->fsname_index < GET_LOGICAL_DRIVE_STRINGS_FSNAME_BUFFER)
						state->fsname [state->fsname_index++] = state->buffer [state->buffer_index];
				}
				break;
		}

		state->buffer_index++;
	}

	return FALSE;
}

static void
append_to_mountpoint (LinuxMountInfoParseState *state)
{
	gchar ch = state->buffer [state->buffer_index];
	if (state->mountpoint_allocated) {
		if (state->mountpoint_index >= state->allocated_size) {
			guint32 newsize = (state->allocated_size << 1) + 1;
			gchar *newbuf = (gchar *)g_malloc0 (newsize * sizeof (gchar));

			memcpy (newbuf, state->mountpoint_allocated, state->mountpoint_index);
			g_free (state->mountpoint_allocated);
			state->mountpoint_allocated = newbuf;
			state->allocated_size = newsize;
		}
		state->mountpoint_allocated [state->mountpoint_index++] = ch;
	} else {
		if (state->mountpoint_index >= GET_LOGICAL_DRIVE_STRINGS_MOUNTPOINT_BUFFER) {
			state->allocated_size = (state->mountpoint_index << 1) + 1;
			state->mountpoint_allocated = (gchar *)g_malloc0 (state->allocated_size * sizeof (gchar));
			memcpy (state->mountpoint_allocated, state->mountpoint, state->mountpoint_index);
			state->mountpoint_allocated [state->mountpoint_index++] = ch;
		} else
			state->mountpoint [state->mountpoint_index++] = ch;
	}
}

static gboolean
add_drive_string (guint32 len, gunichar2 *buf, LinuxMountInfoParseState *state)
{
	gboolean quit = FALSE;
	gboolean ignore_entry;

	if (state->fsname_index == 1 && state->fsname [0] == '/')
		ignore_entry = FALSE;
	else if (memcmp ("overlay", state->fsname, state->fsname_index) == 0 ||
		memcmp ("aufs", state->fstype, state->fstype_index) == 0) {
		/* Don't ignore overlayfs and aufs - these might be used on Docker
		 * (https://bugzilla.xamarin.com/show_bug.cgi?id=31021) */
		ignore_entry = FALSE;
	} else if (state->fsname_index == 0 || memcmp ("none", state->fsname, state->fsname_index) == 0) {
		ignore_entry = TRUE;
	} else if (state->fstype_index >= 5 && memcmp ("fuse.", state->fstype, 5) == 0) {
		/* Ignore GNOME's gvfs */
		if (state->fstype_index == 21 && memcmp ("fuse.gvfs-fuse-daemon", state->fstype, state->fstype_index) == 0)
			ignore_entry = TRUE;
		else
			ignore_entry = FALSE;
	} else if (state->fstype_index == 3 && memcmp ("nfs", state->fstype, state->fstype_index) == 0)
		ignore_entry = FALSE;
	else
		ignore_entry = TRUE;

	if (!ignore_entry) {
		gunichar2 *dir;
		glong length;
		gchar *mountpoint = state->mountpoint_allocated ? state->mountpoint_allocated : state->mountpoint;

		unescape_octal (mountpoint);
		dir = g_utf8_to_utf16 (mountpoint, -1, NULL, &length, NULL);
		if (state->total + length + 1 > len) {
			quit = TRUE;
			state->total = len * 2;
		} else {
			length++;
			memcpy (buf + state->total, dir, sizeof (gunichar2) * length);
			state->total += length;
		}
		g_free (dir);
	}
	state->fsname_index = 0;
	state->fstype_index = 0;

	return quit;
}
#else
gint32
mono_w32file_get_logical_drive (guint32 len, gunichar2 *buf)
{
	return GetLogicalDriveStrings_Mtab (len, buf);
}
#endif
static gint32
GetLogicalDriveStrings_Mtab (guint32 len, gunichar2 *buf)
{
	FILE *fp;
	gunichar2 *ptr, *dir;
	glong length, total = 0;
	gchar buffer [512];
	gchar **splitted;

	memset (buf, 0, sizeof (gunichar2) * (len + 1)); 
	buf [0] = '/';
	buf [1] = 0;
	buf [2] = 0;

	/* Sigh, mntent and friends don't work well.
	 * It stops on the first line that doesn't begin with a '/'.
	 * (linux 2.6.5, libc 2.3.2.ds1-12) - Gonz */
	MONO_ENTER_GC_SAFE;
	fp = fopen ("/etc/mtab", "rt");
	MONO_EXIT_GC_SAFE;
	if (fp == NULL) {
		MONO_ENTER_GC_SAFE;
		fp = fopen ("/etc/mnttab", "rt");
		MONO_EXIT_GC_SAFE;
		if (fp == NULL)
			return 1;
	}

	ptr = buf;
	while (1) {
		gchar *fgets_res;
		MONO_ENTER_GC_SAFE;
		fgets_res = fgets (buffer, 512, fp);
		MONO_EXIT_GC_SAFE;
		if (!fgets_res)
			break;
		if (*buffer != '/')
			continue;

		splitted = g_strsplit (buffer, " ", 0);
		if (!*splitted || !*(splitted + 1)) {
			g_strfreev (splitted);
			continue;
		}

		unescape_octal (*(splitted + 1));
		dir = g_utf8_to_utf16 (*(splitted + 1), -1, NULL, &length, NULL);
		g_strfreev (splitted);
		if (total + length + 1 > len) {
			MONO_ENTER_GC_SAFE;
			fclose (fp);
			MONO_EXIT_GC_SAFE;
			g_free (dir);
			return len * 2; /* guess */
		}

		memcpy (ptr + total, dir, sizeof (gunichar2) * length);
		g_free (dir);
		total += length + 1;
	}

	MONO_ENTER_GC_SAFE;
	fclose (fp);
	MONO_EXIT_GC_SAFE;
	return total;
/* Commented out, does not work with my mtab!!! - Gonz */
#ifdef NOTENABLED /* HAVE_MNTENT_H */
{
	FILE *fp;
	struct mntent *mnt;
	gunichar2 *ptr, *dir;
	glong len, total = 0;
	

	MONO_ENTER_GC_SAFE;
	fp = setmntent ("/etc/mtab", "rt");
	MONO_EXIT_GC_SAFE;
	if (fp == NULL) {
		MONO_ENTER_GC_SAFE;
		fp = setmntent ("/etc/mnttab", "rt");
		MONO_EXIT_GC_SAFE;
		if (fp == NULL)
			return;
	}

	ptr = buf;
	while (1) {
		MONO_ENTER_GC_SAFE;
		mnt = getmntent (fp);
		MONO_EXIT_GC_SAFE;
		if (mnt == NULL)
			break;
		g_print ("GOT %s\n", mnt->mnt_dir);
		dir = g_utf8_to_utf16 (mnt->mnt_dir, &len, NULL, NULL, NULL);
		if (total + len + 1 > len) {
			MONO_ENTER_GC_SAFE;
			endmntent (fp);
			MONO_EXIT_GC_SAFE;
			return len * 2; /* guess */
		}

		memcpy (ptr + total, dir, sizeof (gunichar2) * len);
		g_free (dir);
		total += len + 1;
	}

	MONO_ENTER_GC_SAFE;
	endmntent (fp);
	MONO_EXIT_GC_SAFE;
	return total;
}
#endif
}
#endif

#if defined(HAVE_STATVFS) || defined(HAVE_STATFS)
gboolean
mono_w32file_get_disk_free_space (const gunichar2 *path_name, guint64 *free_bytes_avail, guint64 *total_number_of_bytes, guint64 *total_number_of_free_bytes)
{
#ifdef HAVE_STATVFS
	struct statvfs fsstat;
#elif defined(HAVE_STATFS)
	struct statfs fsstat;
#endif
	gboolean isreadonly;
	gchar *utf8_path_name;
	gint ret;
	unsigned long block_size;

	if (path_name == NULL) {
		utf8_path_name = g_strdup (g_get_current_dir());
		if (utf8_path_name == NULL) {
			mono_w32error_set_last (ERROR_DIRECTORY);
			return(FALSE);
		}
	}
	else {
		utf8_path_name = mono_unicode_to_external (path_name);
		if (utf8_path_name == NULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);

			mono_w32error_set_last (ERROR_INVALID_NAME);
			return(FALSE);
		}
	}

	do {
#ifdef HAVE_STATVFS
		MONO_ENTER_GC_SAFE;
		ret = statvfs (utf8_path_name, &fsstat);
		MONO_EXIT_GC_SAFE;
		isreadonly = ((fsstat.f_flag & ST_RDONLY) == ST_RDONLY);
		block_size = fsstat.f_frsize;
#elif defined(HAVE_STATFS)
		MONO_ENTER_GC_SAFE;
		ret = statfs (utf8_path_name, &fsstat);
		MONO_EXIT_GC_SAFE;
#if defined (MNT_RDONLY)
		isreadonly = ((fsstat.f_flags & MNT_RDONLY) == MNT_RDONLY);
#elif defined (MS_RDONLY)
		isreadonly = ((fsstat.f_flags & MS_RDONLY) == MS_RDONLY);
#endif
		block_size = fsstat.f_bsize;
#endif
	} while(ret == -1 && errno == EINTR);

	g_free(utf8_path_name);

	if (ret == -1) {
		_wapi_set_last_error_from_errno ();
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: statvfs failed: %s", __func__, g_strerror (errno));
		return(FALSE);
	}

	/* total number of free bytes for non-root */
	if (free_bytes_avail != NULL) {
		if (isreadonly) {
			*free_bytes_avail = 0;
		}
		else {
			*free_bytes_avail = block_size * (guint64)fsstat.f_bavail;
		}
	}

	/* total number of bytes available for non-root */
	if (total_number_of_bytes != NULL) {
		*total_number_of_bytes = block_size * (guint64)fsstat.f_blocks;
	}

	/* total number of bytes available for root */
	if (total_number_of_free_bytes != NULL) {
		if (isreadonly) {
			*total_number_of_free_bytes = 0;
		}
		else {
			*total_number_of_free_bytes = block_size * (guint64)fsstat.f_bfree;
		}
	}
	
	return(TRUE);
}
#else
gboolean
mono_w32file_get_disk_free_space (const gunichar2 *path_name, guint64 *free_bytes_avail, guint64 *total_number_of_bytes, guint64 *total_number_of_free_bytes)
{
	if (free_bytes_avail != NULL) {
		*free_bytes_avail = (guint64) -1;
	}

	if (total_number_of_bytes != NULL) {
		*total_number_of_bytes = (guint64) -1;
	}

	if (total_number_of_free_bytes != NULL) {
		*total_number_of_free_bytes = (guint64) -1;
	}

	return(TRUE);
}
#endif

/*
 * General Unix support
 */
typedef struct {
	guint32 drive_type;
#if __linux__
	const long fstypeid;
#endif
	const gchar* fstype;
} _wapi_drive_type;

static _wapi_drive_type _wapi_drive_types[] = {
#if HOST_DARWIN
	{ DRIVE_REMOTE, "afp" },
	{ DRIVE_REMOTE, "autofs" },
	{ DRIVE_CDROM, "cddafs" },
	{ DRIVE_CDROM, "cd9660" },
	{ DRIVE_RAMDISK, "devfs" },
	{ DRIVE_FIXED, "exfat" },
	{ DRIVE_RAMDISK, "fdesc" },
	{ DRIVE_REMOTE, "ftp" },
	{ DRIVE_FIXED, "hfs" },
	{ DRIVE_FIXED, "apfs" },
	{ DRIVE_REMOTE, "kbfuse" },
	{ DRIVE_FIXED, "msdos" },
	{ DRIVE_REMOTE, "nfs" },
	{ DRIVE_FIXED, "ntfs" },
	{ DRIVE_REMOTE, "smbfs" },
	{ DRIVE_FIXED, "udf" },
	{ DRIVE_REMOTE, "webdav" },
	{ DRIVE_UNKNOWN, NULL }
#elif __linux__
	{ DRIVE_FIXED, ADFS_SUPER_MAGIC, "adfs"},
	{ DRIVE_FIXED, AFFS_SUPER_MAGIC, "affs"},
	{ DRIVE_REMOTE, AFS_SUPER_MAGIC, "afs"},
	{ DRIVE_RAMDISK, AUTOFS_SUPER_MAGIC, "autofs"},
	{ DRIVE_RAMDISK, AUTOFS_SBI_MAGIC, "autofs4"},
	{ DRIVE_REMOTE, CODA_SUPER_MAGIC, "coda" },
	{ DRIVE_RAMDISK, CRAMFS_MAGIC, "cramfs"},
	{ DRIVE_RAMDISK, CRAMFS_MAGIC_WEND, "cramfs"},
	{ DRIVE_REMOTE, CIFS_MAGIC_NUMBER, "cifs"},
	{ DRIVE_RAMDISK, DEBUGFS_MAGIC, "debugfs"},
	{ DRIVE_RAMDISK, SYSFS_MAGIC, "sysfs"},
	{ DRIVE_RAMDISK, SECURITYFS_MAGIC, "securityfs"},
	{ DRIVE_RAMDISK, SELINUX_MAGIC, "selinuxfs"},
	{ DRIVE_RAMDISK, RAMFS_MAGIC, "ramfs"},
	{ DRIVE_FIXED, SQUASHFS_MAGIC, "squashfs"},
	{ DRIVE_FIXED, EFS_SUPER_MAGIC, "efs"},
	{ DRIVE_FIXED, EXT2_SUPER_MAGIC, "ext"},
	{ DRIVE_FIXED, EXT3_SUPER_MAGIC, "ext"},
	{ DRIVE_FIXED, EXT4_SUPER_MAGIC, "ext"},
	{ DRIVE_REMOTE, XENFS_SUPER_MAGIC, "xenfs"},
	{ DRIVE_FIXED, BTRFS_SUPER_MAGIC, "btrfs"},
	{ DRIVE_FIXED, HFS_SUPER_MAGIC, "hfs"},
	{ DRIVE_FIXED, HFSPLUS_SUPER_MAGIC, "hfsplus"},
	{ DRIVE_FIXED, HPFS_SUPER_MAGIC, "hpfs"},
	{ DRIVE_RAMDISK, HUGETLBFS_MAGIC, "hugetlbfs"},
	{ DRIVE_CDROM, ISOFS_SUPER_MAGIC, "iso"},
	{ DRIVE_FIXED, JFFS2_SUPER_MAGIC, "jffs2"},
	{ DRIVE_RAMDISK, ANON_INODE_FS_MAGIC, "anon_inode"},
	{ DRIVE_FIXED, JFS_SUPER_MAGIC, "jfs"},
	{ DRIVE_FIXED, MINIX_SUPER_MAGIC, "minix"},
	{ DRIVE_FIXED, MINIX_SUPER_MAGIC2, "minix v2"},
	{ DRIVE_FIXED, MINIX2_SUPER_MAGIC, "minix2"},
	{ DRIVE_FIXED, MINIX2_SUPER_MAGIC2, "minix2 v2"},
	{ DRIVE_FIXED, MINIX3_SUPER_MAGIC, "minix3"},
	{ DRIVE_FIXED, MSDOS_SUPER_MAGIC, "msdos"},
	{ DRIVE_REMOTE, NCP_SUPER_MAGIC, "ncp"},
	{ DRIVE_REMOTE, NFS_SUPER_MAGIC, "nfs"},
	{ DRIVE_FIXED, NTFS_SB_MAGIC, "ntfs"},
	{ DRIVE_RAMDISK, OPENPROM_SUPER_MAGIC, "openpromfs"},
	{ DRIVE_RAMDISK, PROC_SUPER_MAGIC, "proc"},
	{ DRIVE_FIXED, QNX4_SUPER_MAGIC, "qnx4"},
	{ DRIVE_FIXED, REISERFS_SUPER_MAGIC, "reiserfs"},
	{ DRIVE_RAMDISK, ROMFS_MAGIC, "romfs"},
	{ DRIVE_REMOTE, SMB_SUPER_MAGIC, "samba"},
	{ DRIVE_RAMDISK, CGROUP_SUPER_MAGIC, "cgroupfs"},
	{ DRIVE_RAMDISK, FUTEXFS_SUPER_MAGIC, "futexfs"},
	{ DRIVE_FIXED, SYSV2_SUPER_MAGIC, "sysv2"},
	{ DRIVE_FIXED, SYSV4_SUPER_MAGIC, "sysv4"},
	{ DRIVE_RAMDISK, TMPFS_MAGIC, "tmpfs"},
	{ DRIVE_RAMDISK, DEVPTS_SUPER_MAGIC, "devpts"},
	{ DRIVE_CDROM, UDF_SUPER_MAGIC, "udf"},
	{ DRIVE_FIXED, UFS_MAGIC, "ufs"},
	{ DRIVE_FIXED, UFS_MAGIC_BW, "ufs"},
	{ DRIVE_FIXED, UFS2_MAGIC, "ufs2"},
	{ DRIVE_FIXED, UFS_CIGAM, "ufs"},
	{ DRIVE_RAMDISK, USBDEVICE_SUPER_MAGIC, "usbdev"},
	{ DRIVE_FIXED, XENIX_SUPER_MAGIC, "xenix"},
	{ DRIVE_FIXED, XFS_SB_MAGIC, "xfs"},
	{ DRIVE_RAMDISK, FUSE_SUPER_MAGIC, "fuse"},
	{ DRIVE_FIXED, V9FS_MAGIC, "9p"},
	{ DRIVE_REMOTE, CEPH_SUPER_MAGIC, "ceph"},
	{ DRIVE_RAMDISK, CONFIGFS_MAGIC, "configfs"},
	{ DRIVE_RAMDISK, ECRYPTFS_SUPER_MAGIC, "eCryptfs"},
	{ DRIVE_FIXED, EXOFS_SUPER_MAGIC, "exofs"},
	{ DRIVE_FIXED, VXFS_SUPER_MAGIC, "vxfs"},
	{ DRIVE_FIXED, VXFS_OLT_MAGIC, "vxfs_olt"},
	{ DRIVE_REMOTE, GFS2_MAGIC, "gfs2"},
	{ DRIVE_FIXED, LOGFS_MAGIC_U32, "logfs"},
	{ DRIVE_FIXED, OCFS2_SUPER_MAGIC, "ocfs2"},
	{ DRIVE_FIXED, OMFS_MAGIC, "omfs"},
	{ DRIVE_FIXED, UBIFS_SUPER_MAGIC, "ubifs"},
	{ DRIVE_UNKNOWN, 0, NULL}
#else
	{ DRIVE_RAMDISK, "ramfs"      },
	{ DRIVE_RAMDISK, "tmpfs"      },
	{ DRIVE_RAMDISK, "proc"       },
	{ DRIVE_RAMDISK, "sysfs"      },
	{ DRIVE_RAMDISK, "debugfs"    },
	{ DRIVE_RAMDISK, "devpts"     },
	{ DRIVE_RAMDISK, "securityfs" },
	{ DRIVE_CDROM,   "iso9660"    },
	{ DRIVE_FIXED,   "ext2"       },
	{ DRIVE_FIXED,   "ext3"       },
	{ DRIVE_FIXED,   "ext4"       },
	{ DRIVE_FIXED,   "sysv"       },
	{ DRIVE_FIXED,   "reiserfs"   },
	{ DRIVE_FIXED,   "ufs"        },
	{ DRIVE_FIXED,   "vfat"       },
	{ DRIVE_FIXED,   "msdos"      },
	{ DRIVE_FIXED,   "udf"        },
	{ DRIVE_FIXED,   "hfs"        },
	{ DRIVE_FIXED,   "hpfs"       },
	{ DRIVE_FIXED,   "qnx4"       },
	{ DRIVE_FIXED,   "ntfs"       },
	{ DRIVE_FIXED,   "ntfs-3g"    },
	{ DRIVE_REMOTE,  "smbfs"      },
	{ DRIVE_REMOTE,  "fuse"       },
	{ DRIVE_REMOTE,  "nfs"        },
	{ DRIVE_REMOTE,  "nfs4"       },
	{ DRIVE_REMOTE,  "cifs"       },
	{ DRIVE_REMOTE,  "ncpfs"      },
	{ DRIVE_REMOTE,  "coda"       },
	{ DRIVE_REMOTE,  "afs"        },
	{ DRIVE_UNKNOWN, NULL         }
#endif
};

#if __linux__
static guint32 _wapi_get_drive_type(long f_type)
{
	_wapi_drive_type *current;

	current = &_wapi_drive_types[0];
	while (current->drive_type != DRIVE_UNKNOWN) {
		if (current->fstypeid == f_type)
			return current->drive_type;
		current++;
	}

	return DRIVE_UNKNOWN;
}
#else
static guint32 _wapi_get_drive_type(const gchar* fstype)
{
	_wapi_drive_type *current;

	current = &_wapi_drive_types[0];
	while (current->drive_type != DRIVE_UNKNOWN) {
		if (strcmp (current->fstype, fstype) == 0)
			break;

		current++;
	}
	
	return current->drive_type;
}
#endif

#if defined (HOST_DARWIN) || defined (__linux__)
static guint32
GetDriveTypeFromPath (const gchar *utf8_root_path_name)
{
	struct statfs buf;
	gint res;

	MONO_ENTER_GC_SAFE;
	res = statfs (utf8_root_path_name, &buf);
	MONO_EXIT_GC_SAFE;
	if (res == -1)
		return DRIVE_UNKNOWN;
#if HOST_DARWIN
	return _wapi_get_drive_type (buf.f_fstypename);
#else
	return _wapi_get_drive_type (buf.f_type);
#endif
}
#else
static guint32
GetDriveTypeFromPath (const gchar *utf8_root_path_name)
{
	guint32 drive_type;
	FILE *fp;
	gchar buffer [512];
	gchar **splitted;

	MONO_ENTER_GC_SAFE;
	fp = fopen ("/etc/mtab", "rt");
	MONO_EXIT_GC_SAFE;
	if (fp == NULL) {
		MONO_ENTER_GC_SAFE;
		fp = fopen ("/etc/mnttab", "rt");
		MONO_EXIT_GC_SAFE;
		if (fp == NULL) 
			return(DRIVE_UNKNOWN);
	}

	drive_type = DRIVE_NO_ROOT_DIR;
	while (1) {
		gchar *fgets_res;
		MONO_ENTER_GC_SAFE;
		fgets_res = fgets (buffer, 512, fp);
		MONO_EXIT_GC_SAFE;
		if (fgets_res == NULL)
			break;
		splitted = g_strsplit (buffer, " ", 0);
		if (!*splitted || !*(splitted + 1) || !*(splitted + 2)) {
			g_strfreev (splitted);
			continue;
		}

		/* compare given root_path_name with the one from mtab, 
		  if length of utf8_root_path_name is zero it must be the root dir */
		if (strcmp (*(splitted + 1), utf8_root_path_name) == 0 ||
		    (strcmp (*(splitted + 1), "/") == 0 && strlen (utf8_root_path_name) == 0)) {
			drive_type = _wapi_get_drive_type (*(splitted + 2));
			/* it is possible this path might be mounted again with
			   a known type...keep looking */
			if (drive_type != DRIVE_UNKNOWN) {
				g_strfreev (splitted);
				break;
			}
		}

		g_strfreev (splitted);
	}

	MONO_ENTER_GC_SAFE;
	fclose (fp);
	MONO_EXIT_GC_SAFE;
	return drive_type;
}
#endif

guint32
mono_w32file_get_drive_type(const gunichar2 *root_path_name)
{
	gchar *utf8_root_path_name;
	guint32 drive_type;

	if (root_path_name == NULL) {
		utf8_root_path_name = g_strdup (g_get_current_dir());
		if (utf8_root_path_name == NULL) {
			return(DRIVE_NO_ROOT_DIR);
		}
	}
	else {
		utf8_root_path_name = mono_unicode_to_external (root_path_name);
		if (utf8_root_path_name == NULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL", __func__);
			return(DRIVE_NO_ROOT_DIR);
		}
		
		/* strip trailing slash for compare below */
		if (g_str_has_suffix(utf8_root_path_name, "/") && utf8_root_path_name [1] != 0) {
			utf8_root_path_name[strlen(utf8_root_path_name) - 1] = 0;
		}
	}
	drive_type = GetDriveTypeFromPath (utf8_root_path_name);
	g_free (utf8_root_path_name);

	return (drive_type);
}

#if defined (HOST_DARWIN) || defined (__linux__) || defined(HOST_BSD) || defined(__FreeBSD_kernel__) || defined(__HAIKU__)
static gchar*
get_fstypename (gchar *utfpath)
{
#if defined (HOST_DARWIN) || defined (__linux__)
	struct statfs stat;
#if __linux__
	_wapi_drive_type *current;
#endif
	gint statfs_res;
	MONO_ENTER_GC_SAFE;
	statfs_res = statfs (utfpath, &stat);
	MONO_EXIT_GC_SAFE;
	if (statfs_res == -1)
		return NULL;
#if HOST_DARWIN
	return g_strdup (stat.f_fstypename);
#else
	current = &_wapi_drive_types[0];
	while (current->drive_type != DRIVE_UNKNOWN) {
		if (stat.f_type == current->fstypeid)
			return g_strdup (current->fstype);
		current++;
	}
	return NULL;
#endif
#else
	return NULL;
#endif
}

/* Linux has struct statfs which has a different layout */
gboolean
mono_w32file_get_volume_information (const gunichar2 *path, gunichar2 *volumename, gint volumesize, gint *outserial, gint *maxcomp, gint *fsflags, gunichar2 *fsbuffer, gint fsbuffersize)
{
	gchar *utfpath;
	gchar *fstypename;
	gboolean status = FALSE;
	glong len;
	
	// We only support getting the file system type
	if (fsbuffer == NULL)
		return 0;
	
	utfpath = mono_unicode_to_external (path);
	if ((fstypename = get_fstypename (utfpath)) != NULL){
		gunichar2 *ret = g_utf8_to_utf16 (fstypename, -1, NULL, &len, NULL);
		if (ret != NULL && len < fsbuffersize){
			memcpy (fsbuffer, ret, len * sizeof (gunichar2));
			fsbuffer [len] = 0;
			status = TRUE;
		}
		if (ret != NULL)
			g_free (ret);
		g_free (fstypename);
	}
	g_free (utfpath);
	return status;
}
#endif

static gboolean
LockFile (gpointer handle, guint32 offset_low, guint32 offset_high, guint32 length_low, guint32 length_high)
{
	FileHandle *filehandle;
	gboolean ret;
	off_t offset, length;

	if (!mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (((MonoFDHandle*) filehandle)->type != MONO_FDTYPE_FILE) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FALSE;
	}

	if (!(filehandle->fileaccess & (GENERIC_READ | GENERIC_WRITE | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_READ or GENERIC_WRITE access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);
		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FALSE;
	}

#ifdef HAVE_LARGE_FILE_SUPPORT
	offset = ((gint64)offset_high << 32) | offset_low;
	length = ((gint64)length_high << 32) | length_low;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Locking fd %d, offset %" G_GINT64_FORMAT ", length %" G_GINT64_FORMAT, __func__, ((MonoFDHandle*) filehandle)->fd, (gint64) offset, (gint64) length);
#else
	if (offset_high > 0 || length_high > 0) {
		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FALSE;
	}

	offset = offset_low;
	length = length_low;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Locking fd %d, offset %" G_GINT64_FORMAT ", length %" G_GINT64_FORMAT, __func__, ((MonoFDHandle*) filehandle)->fd, (gint64) offset, (gint64) length);
#endif

	ret = _wapi_lock_file_region (((MonoFDHandle*) filehandle)->fd, offset, length);

	mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	return ret;
}

static gboolean
UnlockFile (gpointer handle, guint32 offset_low, guint32 offset_high, guint32 length_low, guint32 length_high)
{
	FileHandle *filehandle;
	gboolean ret;
	off_t offset, length;

	if (!mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle)) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (((MonoFDHandle*) filehandle)->type != MONO_FDTYPE_FILE) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FALSE;
	}

	if (!(filehandle->fileaccess & (GENERIC_READ | GENERIC_WRITE | GENERIC_ALL))) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: fd %d doesn't have GENERIC_READ or GENERIC_WRITE access: %u", __func__, ((MonoFDHandle*) filehandle)->fd, filehandle->fileaccess);
		mono_w32error_set_last (ERROR_ACCESS_DENIED);
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
		return FALSE;
	}

#ifdef HAVE_LARGE_FILE_SUPPORT
	offset = ((gint64)offset_high << 32) | offset_low;
	length = ((gint64)length_high << 32) | length_low;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Unlocking fd %d, offset %" G_GINT64_FORMAT ", length %" G_GINT64_FORMAT, __func__, ((MonoFDHandle*) filehandle)->fd, (gint64) offset, (gint64) length);
#else
	offset = offset_low;
	length = length_low;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: Unlocking fd %p, offset %" G_GINT64_FORMAT ", length %" G_GINT64_FORMAT, __func__, ((MonoFDHandle*) filehandle)->fd, (gint64) offset, (gint64) length);
#endif

	ret = _wapi_unlock_file_region (((MonoFDHandle*) filehandle)->fd, offset, length);

	mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	return ret;
}

void
mono_w32file_init (void)
{
	MonoFDHandleCallback file_data_callbacks = {
		.close = file_data_close,
		.destroy = file_data_destroy
	};

	mono_fdhandle_register (MONO_FDTYPE_FILE, &file_data_callbacks);
	mono_fdhandle_register (MONO_FDTYPE_CONSOLE, &file_data_callbacks);
	mono_fdhandle_register (MONO_FDTYPE_PIPE, &file_data_callbacks);

	mono_coop_mutex_init (&file_share_mutex);

	finds = g_hash_table_new (g_direct_hash, g_direct_equal);
	mono_coop_mutex_init (&finds_mutex);

	if (g_hasenv ("MONO_STRICT_IO_EMULATION"))
		lock_while_writing = TRUE;
}

void
mono_w32file_cleanup (void)
{
	mono_coop_mutex_destroy (&file_share_mutex);

	if (file_share_table)
		g_hash_table_destroy (file_share_table);

	g_hash_table_destroy (finds);
	mono_coop_mutex_destroy (&finds_mutex);
}

gboolean
mono_w32file_move (const gunichar2 *path, const gunichar2 *dest, gint32 *error)
{
	gboolean result;

	result = MoveFile (path, dest);
	if (!result)
		*error = mono_w32error_get_last ();
	return result;
}

gboolean
mono_w32file_copy (const gunichar2 *path, const gunichar2 *dest, gboolean overwrite, gint32 *error)
{
	gboolean result;

	result = CopyFile (path, dest, !overwrite);
	if (!result)
		*error = mono_w32error_get_last ();

	return result;
}

gboolean
mono_w32file_replace (const gunichar2 *destination_file_name, const gunichar2 *source_file_name, const gunichar2 *destination_backup_file_name, guint32 flags, gint32 *error)
{
	gboolean result;

	result = ReplaceFile (destination_file_name, source_file_name, destination_backup_file_name, flags, NULL, NULL);
	if (!result)
		*error = mono_w32error_get_last ();
	return result;
}

gint64
mono_w32file_get_file_size (gpointer handle, gint32 *error)
{
	gint64 length;
	guint32 length_hi;

	length = GetFileSize (handle, &length_hi);
	if(length==INVALID_FILE_SIZE) {
		*error=mono_w32error_get_last ();
	}

	return length | ((gint64)length_hi << 32);
}

gboolean
mono_w32file_lock (gpointer handle, gint64 position, gint64 length, gint32 *error)
{
	gboolean result;

	result = LockFile (handle, position & 0xFFFFFFFF, position >> 32, length & 0xFFFFFFFF, length >> 32);
	if (!result)
		*error = mono_w32error_get_last ();
	return result;
}

gboolean
mono_w32file_unlock (gpointer handle, gint64 position, gint64 length, gint32 *error)
{
	gboolean result;

	result = UnlockFile (handle, position & 0xFFFFFFFF, position >> 32, length & 0xFFFFFFFF, length >> 32);
	if (!result)
		*error = mono_w32error_get_last ();
	return result;
}

gpointer
mono_w32file_get_console_input (void)
{
	return mono_w32file_get_std_handle (STD_INPUT_HANDLE);
}

gpointer
mono_w32file_get_console_output (void)
{
	return mono_w32file_get_std_handle (STD_OUTPUT_HANDLE);
}

gpointer
mono_w32file_get_console_error (void)
{
	return mono_w32file_get_std_handle (STD_ERROR_HANDLE);
}
