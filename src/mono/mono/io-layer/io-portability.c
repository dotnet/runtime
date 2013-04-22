/*
 * io-portability.c:  Optional filename mangling to try to cope with
 *			badly-written non-portable windows apps
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * Copyright (c) 2006 Novell, Inc.
 */

#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <errno.h>
#include <string.h>
#include <unistd.h>
#include <stdlib.h>
#include <sys/types.h>
#include <sys/time.h>
#ifdef HAVE_DIRENT_H
# include <dirent.h>
#endif
#include <utime.h>
#include <sys/stat.h>

#include <mono/io-layer/error.h>
#include <mono/io-layer/wapi_glob.h>
#include <mono/io-layer/io-portability.h>
#include <mono/utils/mono-io-portability.h>

#include <mono/utils/mono-mutex.h>

#undef DEBUG

int _wapi_open (const char *pathname, int flags, mode_t mode)
{
	int fd;
	gchar *located_filename;
	
	if (flags & O_CREAT) {
		located_filename = mono_portability_find_file (pathname, FALSE);
		if (located_filename == NULL) {
			fd = open (pathname, flags, mode);
		} else {
			fd = open (located_filename, flags, mode);
			g_free (located_filename);
		}
	} else {
		fd = open (pathname, flags, mode);
		if (fd == -1 &&
		    (errno == ENOENT || errno == ENOTDIR) &&
		    IS_PORTABILITY_SET) {
			int saved_errno = errno;
			located_filename = mono_portability_find_file (pathname, TRUE);
			
			if (located_filename == NULL) {
				errno = saved_errno;
				return (-1);
			}
			
			fd = open (located_filename, flags, mode);
			g_free (located_filename);
		}
	}
	
	
	return(fd);
}

int _wapi_access (const char *pathname, int mode)
{
	int ret;
	
	ret = access (pathname, mode);
	if (ret == -1 &&
	    (errno == ENOENT || errno == ENOTDIR) &&
	    IS_PORTABILITY_SET) {
		int saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (pathname, TRUE);
		
		if (located_filename == NULL) {
			errno = saved_errno;
			return(-1);
		}
		
		ret = access (located_filename, mode);
		g_free (located_filename);
	}
	
	return(ret);
}

int _wapi_chmod (const char *pathname, mode_t mode)
{
	int ret;
	
	ret = chmod (pathname, mode);
	if (ret == -1 &&
	    (errno == ENOENT || errno == ENOTDIR) &&
	    IS_PORTABILITY_SET) {
		int saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (pathname, TRUE);
		
		if (located_filename == NULL) {
			errno = saved_errno;
			return(-1);
		}
		
		ret = chmod (located_filename, mode);
		g_free (located_filename);
	}
	
	return(ret);
}

int _wapi_utime (const char *filename, const struct utimbuf *buf)
{
	int ret;
	
	ret = utime (filename, buf);
	if (ret == -1 &&
	    errno == ENOENT &&
	    IS_PORTABILITY_SET) {
		int saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (filename, TRUE);
		
		if (located_filename == NULL) {
			errno = saved_errno;
			return(-1);
		}
		
		ret = utime (located_filename, buf);
		g_free (located_filename);
	}
	
	return(ret);
}

int _wapi_unlink (const char *pathname)
{
	int ret;
	
	ret = unlink (pathname);
	if (ret == -1 &&
	    (errno == ENOENT || errno == ENOTDIR || errno == EISDIR) &&
	    IS_PORTABILITY_SET) {
		int saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (pathname, TRUE);
		
		if (located_filename == NULL) {
			errno = saved_errno;
			return(-1);
		}
		
		ret = unlink (located_filename);
		g_free (located_filename);
	}
	
	return(ret);
}

int _wapi_rename (const char *oldpath, const char *newpath)
{
	int ret;
	gchar *located_newpath = mono_portability_find_file (newpath, FALSE);
	
	if (located_newpath == NULL) {
		ret = rename (oldpath, newpath);
	} else {
		ret = rename (oldpath, located_newpath);
	
		if (ret == -1 &&
		    (errno == EISDIR || errno == ENAMETOOLONG ||
		     errno == ENOENT || errno == ENOTDIR || errno == EXDEV) &&
		    IS_PORTABILITY_SET) {
			int saved_errno = errno;
			gchar *located_oldpath = mono_portability_find_file (oldpath, TRUE);
			
			if (located_oldpath == NULL) {
				g_free (located_oldpath);
				g_free (located_newpath);
			
				errno = saved_errno;
				return(-1);
			}
			
			ret = rename (located_oldpath, located_newpath);
			g_free (located_oldpath);
		}
		g_free (located_newpath);
	}
	
	return(ret);
}

int _wapi_stat (const char *path, struct stat *buf)
{
	int ret;
	
	ret = stat (path, buf);
	if (ret == -1 &&
	    (errno == ENOENT || errno == ENOTDIR) &&
	    IS_PORTABILITY_SET) {
		int saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (path, TRUE);
		
		if (located_filename == NULL) {
			errno = saved_errno;
			return(-1);
		}
		
		ret = stat (located_filename, buf);
		g_free (located_filename);
	}
	
	return(ret);
}

int _wapi_lstat (const char *path, struct stat *buf)
{
	int ret;
	
	ret = lstat (path, buf);
	if (ret == -1 &&
	    (errno == ENOENT || errno == ENOTDIR) &&
	    IS_PORTABILITY_SET) {
		int saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (path, TRUE);
		
		if (located_filename == NULL) {
			errno = saved_errno;
			return(-1);
		}
		
		ret = lstat (located_filename, buf);
		g_free (located_filename);
	}
	
	return(ret);
}

int _wapi_mkdir (const char *pathname, mode_t mode)
{
	int ret;
	gchar *located_filename = mono_portability_find_file (pathname, FALSE);
	
	if (located_filename == NULL) {
		ret = mkdir (pathname, mode);
	} else {
		ret = mkdir (located_filename, mode);
		g_free (located_filename);
	}
	
	return(ret);
}

int _wapi_rmdir (const char *pathname)
{
	int ret;
	
	ret = rmdir (pathname);
	if (ret == -1 &&
	    (errno == ENOENT || errno == ENOTDIR || errno == ENAMETOOLONG) &&
	    IS_PORTABILITY_SET) {
		int saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (pathname, TRUE);
		
		if (located_filename == NULL) {
			errno = saved_errno;
			return(-1);
		}
		
		ret = rmdir (located_filename);
		g_free (located_filename);
	}
	
	return(ret);
}

int _wapi_chdir (const char *path)
{
	int ret;
	
	ret = chdir (path);
	if (ret == -1 &&
	    (errno == ENOENT || errno == ENOTDIR || errno == ENAMETOOLONG) &&
	    IS_PORTABILITY_SET) {
		int saved_errno = errno;
		gchar *located_filename = mono_portability_find_file (path, TRUE);
		
		if (located_filename == NULL) {
			errno = saved_errno;
			return(-1);
		}
		
		ret = chdir (located_filename);
		g_free (located_filename);
	}
	
	return(ret);
}

gchar *_wapi_basename (const gchar *filename)
{
	gchar *new_filename = g_strdup (filename), *ret;

	if (IS_PORTABILITY_SET) {
		g_strdelimit (new_filename, "\\", '/');
	}

	if (IS_PORTABILITY_DRIVE &&
	    g_ascii_isalpha (new_filename[0]) &&
	    (new_filename[1] == ':')) {
		int len = strlen (new_filename);
		
		g_memmove (new_filename, new_filename + 2, len - 2);
		new_filename[len - 2] = '\0';
	}
	
	ret = g_path_get_basename (new_filename);
	g_free (new_filename);
	
	return(ret);
}

gchar *_wapi_dirname (const gchar *filename)
{
	gchar *new_filename = g_strdup (filename), *ret;

	if (IS_PORTABILITY_SET) {
		g_strdelimit (new_filename, "\\", '/');
	}

	if (IS_PORTABILITY_DRIVE &&
	    g_ascii_isalpha (new_filename[0]) &&
	    (new_filename[1] == ':')) {
		int len = strlen (new_filename);
		
		g_memmove (new_filename, new_filename + 2, len - 2);
		new_filename[len - 2] = '\0';
	}
	
	ret = g_path_get_dirname (new_filename);
	g_free (new_filename);
	
	return(ret);
}

GDir *_wapi_g_dir_open (const gchar *path, guint flags, GError **error)
{
	GDir *ret;
	
	ret = g_dir_open (path, flags, error);
	if (ret == NULL &&
	    ((*error)->code == G_FILE_ERROR_NOENT ||
	     (*error)->code == G_FILE_ERROR_NOTDIR ||
	     (*error)->code == G_FILE_ERROR_NAMETOOLONG) &&
	    IS_PORTABILITY_SET) {
		gchar *located_filename = mono_portability_find_file (path, TRUE);
		GError *tmp_error = NULL;
		
		if (located_filename == NULL) {
			return(NULL);
		}
		
		ret = g_dir_open (located_filename, flags, &tmp_error);
		g_free (located_filename);
		if (tmp_error == NULL) {
			g_clear_error (error);
		}
	}
	
	return(ret);
}


static gint
file_compare (gconstpointer a, gconstpointer b)
{
	gchar *astr = *(gchar **) a;
	gchar *bstr = *(gchar **) b;

	return strcmp (astr, bstr);
}

static gint
get_errno_from_g_file_error (gint error)
{
	switch (error) {
#ifdef EACCESS
	case G_FILE_ERROR_ACCES:
		error = EACCES;
		break;
#endif
#ifdef ENAMETOOLONG
	case G_FILE_ERROR_NAMETOOLONG:
		error = ENAMETOOLONG;
		break;
#endif
#ifdef ENOENT
	case G_FILE_ERROR_NOENT:
		error = ENOENT;
		break;
#endif
#ifdef ENOTDIR
	case G_FILE_ERROR_NOTDIR:
		error = ENOTDIR;
		break;
#endif
#ifdef ENXIO
	case G_FILE_ERROR_NXIO:
		error = ENXIO;
		break;
#endif
#ifdef ENODEV
	case G_FILE_ERROR_NODEV:
		error = ENODEV;
		break;
#endif
#ifdef EROFS
	case G_FILE_ERROR_ROFS:
		error = EROFS;
		break;
#endif
#ifdef ETXTBSY
	case G_FILE_ERROR_TXTBSY:
		error = ETXTBSY;
		break;
#endif
#ifdef EFAULT
	case G_FILE_ERROR_FAULT:
		error = EFAULT;
		break;
#endif
#ifdef ELOOP
	case G_FILE_ERROR_LOOP:
		error = ELOOP;
		break;
#endif
#ifdef ENOSPC
	case G_FILE_ERROR_NOSPC:
		error = ENOSPC;
		break;
#endif
#ifdef ENOMEM
	case G_FILE_ERROR_NOMEM:
		error = ENOMEM;
		break;
#endif
#ifdef EMFILE
	case G_FILE_ERROR_MFILE:
		error = EMFILE;
		break;
#endif
#ifdef ENFILE
	case G_FILE_ERROR_NFILE:
		error = ENFILE;
		break;
#endif
#ifdef EBADF
	case G_FILE_ERROR_BADF:
		error = EBADF;
		break;
#endif
#ifdef EINVAL
	case G_FILE_ERROR_INVAL:
		error = EINVAL;
		break;
#endif
#ifdef EPIPE
	case G_FILE_ERROR_PIPE:
		error = EPIPE;
		break;
#endif
#ifdef EAGAIN
	case G_FILE_ERROR_AGAIN:
		error = EAGAIN;
		break;
#endif
#ifdef EINTR
	case G_FILE_ERROR_INTR:
		error = EINTR;
		break;
#endif
#ifdef EWIO
	case G_FILE_ERROR_IO:
		error = EIO;
		break;
#endif
#ifdef EPERM
	case G_FILE_ERROR_PERM:
		error = EPERM;
		break;
#endif
	case G_FILE_ERROR_FAILED:
		error = ERROR_INVALID_PARAMETER;
		break;
	}

	return error;
}

/* scandir using glib */
gint _wapi_io_scandir (const gchar *dirname, const gchar *pattern,
		       gchar ***namelist)
{
	GError *error = NULL;
	GDir *dir;
	GPtrArray *names;
	gint result;
	wapi_glob_t glob_buf;
	int flags = 0, i;
	
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
		flags = WAPI_GLOB_IGNORECASE;
	}
	
	result = _wapi_glob (dir, pattern, flags, &glob_buf);
	if (g_str_has_suffix (pattern, ".*")) {
		/* Special-case the patterns ending in '.*', as
		 * windows also matches entries with no extension with
		 * this pattern.
		 * 
		 * TODO: should this be a MONO_IOMAP option?
		 */
		gchar *pattern2 = g_strndup (pattern, strlen (pattern) - 2);
		gint result2;
		
		g_dir_rewind (dir);
		result2 = _wapi_glob (dir, pattern2, flags | WAPI_GLOB_APPEND | WAPI_GLOB_UNIQUE, &glob_buf);

		g_free (pattern2);

		if (result != 0) {
			result = result2;
		}
	}
	
	g_dir_close (dir);
	if (glob_buf.gl_pathc == 0) {
		return(0);
	} else if (result != 0) {
		return(-1);
	}
	
	names = g_ptr_array_new ();
	for (i = 0; i < glob_buf.gl_pathc; i++) {
		g_ptr_array_add (names, g_strdup (glob_buf.gl_pathv[i]));
	}

	_wapi_globfree (&glob_buf);

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
