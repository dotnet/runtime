#include <config.h>
#include <limits.h>

#include "wrapper.h"

gint64
mono_wrapper_seek (int fd, gint64 offset, gint32 whence)
{
	if (offset > INT_MAX || offset < INT_MIN)
		return -1;

	return lseek (fd, offset, whence);
}

gint32
mono_wrapper_read (int fd, void* buf, gint32 count)
{
	return read (fd, buf, count);
}

gint32
mono_wrapper_write (int fd, void* buf, gint32 count)
{
	return write (fd, buf, count);
}

gint32
mono_wrapper_fstat (int fd, MonoWrapperStat* buf)
{
	struct stat fs;

	if (fstat (fd, &fs) != 0)
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
mono_wrapper_ftruncate (int fd, gint64 length) 
{
	if (length > INT_MAX || length < INT_MIN)
		return -1;

	return ftruncate (fd, length);
}

int
mono_wrapper_open (const char * path, gint32 flags, gint32 mode)
{
	return open (path, flags, mode);
}

gint32
mono_wrapper_close (int fd)
{
	return close (fd);
}

