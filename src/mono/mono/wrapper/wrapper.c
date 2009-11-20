#include <config.h>
#include <limits.h>
#include <dirent.h>
#include <stdlib.h>
#include <errno.h>
#include <unistd.h>
#include <stdio.h>
#include <sys/types.h>
#ifdef HAVE_UTIME_H
#include <utime.h>
#endif
#include "wrapper.h"

extern char **environ;

gint64
mono_wrapper_seek (gpointer fd, gint64 offset, gint32 whence)
{
	off_t code;

	if (offset > INT_MAX || offset < INT_MIN)
		return -EINVAL;

	code = lseek ((int)fd, offset, whence);
	if (code == -1)
		return -errno;
	else
		return code;
}

gint32
mono_wrapper_read (gpointer fd, void* buf, gint32 count)
{
	int n = read ((int)fd, buf, count);

	if (n == -1)
		return -errno;
	return n;
}

gint32
mono_wrapper_write (gpointer fd, void* buf, gint32 count)
{
	int n = write ((int)fd, buf, count);

	if (n == -1)
		return -errno;
	return n;
}

gint32
mono_wrapper_fstat (gpointer fd, MonoWrapperStat* buf)
{
	struct stat fs;

	if (fstat ((int)fd, &fs) == -1)
		return -errno;

	buf->mst_dev = fs.st_dev;
	buf->mst_mode = fs.st_mode;
	buf->mst_nlink = fs.st_nlink;
	buf->mst_uid = fs.st_uid;
	buf->mst_gid = fs.st_gid;
	buf->mst_size = fs.st_size;
	buf->mst_atime = fs.st_atime;
	buf->mst_mtime = fs.st_mtime;
	buf->mst_ctime = fs.st_ctime;

	return 0;
}

gint32
mono_wrapper_ftruncate (gpointer fd, gint64 length) 
{
	int code;

	if (length > INT_MAX || length < INT_MIN)
		return -1;

	code = ftruncate ((int)fd, length);
	if (code == -1)
		return -errno;
	return code;
}

gpointer
mono_wrapper_open (const char * path, gint32 flags, gint32 mode)
{
	return (gpointer) open (path, flags, mode);
}

gint32
mono_wrapper_close (gpointer fd)
{
	return close ((int)fd);
}

gint32
mono_wrapper_stat (const char * path, MonoWrapperStat* buf)
{
	struct stat fs;

	if (stat (path, &fs) != 0)
		return errno;

	buf->mst_dev = fs.st_dev;
	buf->mst_mode = fs.st_mode;
	buf->mst_nlink = fs.st_nlink;
	buf->mst_uid = fs.st_uid;
	buf->mst_gid = fs.st_gid;
	buf->mst_size = fs.st_size;
	buf->mst_atime = fs.st_atime;
	buf->mst_mtime = fs.st_mtime;
	buf->mst_ctime = fs.st_ctime;

	return 0;
}

gint32
mono_wrapper_unlink (const char * path)
{
	if (unlink(path) == -1)
		return -errno;
	return 0;
}

gpointer
mono_wrapper_opendir (const char * path)
{
	return (gpointer)opendir(path);
}

const char *
mono_wrapper_readdir (gpointer dir)
{
	struct dirent* p;

	p = readdir((DIR*)dir);

	return p != NULL ? p->d_name : NULL;
}

gint32
mono_wrapper_closedir (gpointer dir)
{
	return closedir((DIR*)dir);
}

gpointer
mono_wrapper_getenv (const char * variable)
{
	return (gpointer)getenv(variable);
}

gpointer
mono_wrapper_environ ()
{
	return (gpointer)environ;
}

int
mono_wrapper_mkdir (const char *path, int mode)
{
#ifndef HOST_WIN32
	if (mkdir (path, mode) == -1)
		return -errno;
#endif
	return 0;
}

int
mono_wrapper_rmdir (const char *path)
{
	if (rmdir (path) == -1)
		return -errno;
	return 0;
}

int
mono_wrapper_rename (const char *src, const char *dst)
{
	if (rename (src, dst) == -1)
		return -errno;
	return 0;
}

int
mono_wrapper_utime (const char *path, int atime, int mtime)
{
#ifndef HOST_WIN32
	struct utimbuf buf;

	buf.actime = atime;
	buf.modtime = mtime;

	if (utime (path, &buf) == -1)
		return -errno;
#endif
	return 0;
}

