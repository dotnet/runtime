#include <config.h>
#include <limits.h>
#include <dirent.h>
#include <stdlib.h>

#include "wrapper.h"

extern char **environ;

gint64
mono_wrapper_seek (gpointer fd, gint64 offset, gint32 whence)
{
	if (offset > INT_MAX || offset < INT_MIN)
		return -1;

	return lseek ((int)fd, offset, whence);
}

gint32
mono_wrapper_read (gpointer fd, void* buf, gint32 count)
{
	return read ((int)fd, buf, count);
}

gint32
mono_wrapper_write (gpointer fd, void* buf, gint32 count)
{
	return write ((int)fd, buf, count);
}

gint32
mono_wrapper_fstat (gpointer fd, MonoWrapperStat* buf)
{
	struct stat fs;

	if (fstat ((int)fd, &fs) != 0)
		return -1;

	buf->st_dev = fs.st_dev;
	buf->st_mode = fs.st_mode;
	buf->st_nlink = fs.st_nlink;
	buf->st_uid = fs.st_uid;
	buf->st_gid = fs.st_gid;
	buf->st_size = fs.st_size;
	buf->st_atime = fs.st_atime;
	buf->st_mtime = fs.st_ctime;
	buf->st_ctime = fs.st_ctime;

	return 0;
}

gint32
mono_wrapper_ftruncate (gpointer fd, gint64 length) 
{
	if (length > INT_MAX || length < INT_MIN)
		return -1;

	return ftruncate ((int)fd, length);
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

	buf->st_dev = fs.st_dev;
	buf->st_mode = fs.st_mode;
	buf->st_nlink = fs.st_nlink;
	buf->st_uid = fs.st_uid;
	buf->st_gid = fs.st_gid;
	buf->st_size = fs.st_size;
	buf->st_atime = fs.st_atime;
	buf->st_mtime = fs.st_ctime;
	buf->st_ctime = fs.st_ctime;

	return 0;
}

gint32
mono_wrapper_unlink (const char * path)
{
	return unlink(path);
}

gpointer
mono_wrapper_opendir (const char * path)
{
	return (gpointer)opendir(path);
}

const char *
mono_wrapper_readdir (gpointer dir)
{
	struct dirent* p = readdir((DIR*)dir);
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
	return mkdir (path, mode);
}

int
mono_wrapper_rmdir (const char *path)
{
	return rmdir (path);
}

int
mono_wrapper_rename (const char *src, const char *dst)
{
	return rename (src, dst);
}


