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
#ifdef HAVE_UTIME_H
#include <utime.h>
#endif
#ifdef __linux__
#include <sys/ioctl.h>
#include <linux/fs.h>
#include <mono/utils/linux_magic.h>
#endif
#ifdef _AIX
#include <sys/mntctl.h>
#include <sys/vmount.h>
#endif
#include <sys/time.h>
#ifdef HAVE_DIRENT_H
# include <dirent.h>
#endif
#if HOST_DARWIN
#include <dlfcn.h>
#endif

#include "w32file.h"
#include "w32error.h"
#include "fdhandle.h"
#include "utils/mono-error-internals.h"
#include "utils/mono-io-portability.h"
#include "utils/mono-logger-internals.h"
#include "utils/mono-os-mutex.h"
#include "utils/mono-threads.h"
#include "utils/mono-threads-api.h"
#include "utils/strenc-internals.h"
#include "utils/strenc.h"
#include "utils/refcount.h"
#include "icall-decl.h"
#include "utils/mono-errno.h"

#define INVALID_HANDLE_VALUE ((gpointer)-1)

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

/*
 * If SHM is disabled, this will point to a hash of FileShare structures, otherwise
 * it will be NULL. We use this instead of _wapi_fileshare_layout to avoid allocating a
 * 4MB array.
 */
static GHashTable *file_share_table;
static MonoCoopMutex file_share_mutex;

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
				mono_set_errno (saved_errno);
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
			mono_set_errno (saved_errno);
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
			mono_set_errno (saved_errno);
			return -1;
		}

		MONO_ENTER_GC_SAFE;
		ret = unlink (located_filename);
		MONO_EXIT_GC_SAFE;
		g_free (located_filename);
	}

	return ret;
}

static gchar*
_wapi_dirname (const gchar *filename)
{
	gchar *new_filename = g_strdup (filename), *ret;

	if (IS_PORTABILITY_SET)
		g_strdelimit (new_filename, '\\', '/');

	if (IS_PORTABILITY_DRIVE && g_ascii_isalpha (new_filename[0]) && (new_filename[1] == ':')) {
		gint len = strlen (new_filename);

		g_memmove (new_filename, new_filename + 2, len - 2);
		new_filename[len - 2] = '\0';
	}

	ret = g_path_get_dirname (new_filename);
	g_free (new_filename);

	return ret;
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
file_write (FileHandle *filehandle, gpointer buffer, guint32 numbytes, guint32 *byteswritten)
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

static gboolean
console_write (FileHandle *filehandle, gpointer buffer, guint32 numbytes, guint32 *byteswritten)
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
pipe_write (FileHandle *filehandle, gpointer buffer, guint32 numbytes, guint32 *byteswritten)
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
		if ((file_existing_share == FILE_SHARE_NONE) || (sharemode == FILE_SHARE_NONE)) {
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
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: New file!", __func__);
	}

	return(TRUE);
}

void
mono_w32file_init (void)
{
	MonoFDHandleCallback file_data_callbacks;
	memset (&file_data_callbacks, 0, sizeof (file_data_callbacks));
	file_data_callbacks.close = file_data_close;
	file_data_callbacks.destroy = file_data_destroy;

	mono_fdhandle_register (MONO_FDTYPE_FILE, &file_data_callbacks);
	mono_fdhandle_register (MONO_FDTYPE_CONSOLE, &file_data_callbacks);
	mono_fdhandle_register (MONO_FDTYPE_PIPE, &file_data_callbacks);

	mono_coop_mutex_init (&file_share_mutex);

	if (g_hasenv ("MONO_STRICT_IO_EMULATION"))
		lock_while_writing = TRUE;
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
	ERROR_DECL (error);

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

	filename = mono_unicode_to_external_checked (name, error);
	if (filename == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_FILE, "%s: unicode conversion returned NULL; %s", __func__, mono_error_get_message (error));

		mono_error_cleanup (error);
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

static gboolean
mono_w32file_read_or_write (gpointer handle, gpointer buffer, guint32 numbytes, guint32 *bytesread, gint32 *win32error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	FileHandle *filehandle;
	gboolean ret = FALSE;

	gboolean const ref = mono_fdhandle_lookup_and_ref(GPOINTER_TO_INT(handle), (MonoFDHandle**) &filehandle);
	if (!ref) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		goto exit;
	}

	switch (((MonoFDHandle*) filehandle)->type) {
	case MONO_FDTYPE_FILE:
		ret = (file_write) (filehandle, buffer, numbytes, bytesread);
		break;
	case MONO_FDTYPE_CONSOLE:
		ret = (console_write) (filehandle, buffer, numbytes, bytesread);
		break;
	case MONO_FDTYPE_PIPE:
		ret = (pipe_write) (filehandle, buffer, numbytes, bytesread);
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		break;
	}

exit:
	if (ref)
		mono_fdhandle_unref ((MonoFDHandle*) filehandle);
	if (!ret)
		*win32error = mono_w32error_get_last ();
	return ret;
}

gboolean
mono_w32file_write (gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten, gint32 *win32error)
{
	return mono_w32file_read_or_write (handle, (gpointer)buffer, numbytes, byteswritten, win32error);
}
