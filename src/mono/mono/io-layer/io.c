#include <config.h>
#include <glib.h>
#include <fcntl.h>
#include <unistd.h>
#include <errno.h>
#include <string.h>
#include <sys/poll.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <glob.h>
#include <stdio.h>
#include <utime.h>

#include "mono/io-layer/wapi.h"
#include "unicode.h"
#include "wapi-private.h"

#undef DEBUG
#define ACTUALLY_DO_UNICODE

/* Currently used for both FILE and CONSOLE handle types.  This may
 * have to change in future.
 */
struct _WapiHandle_file
{
	WapiHandle handle;
	int fd;
	gchar *filename;
	WapiSecurityAttributes *security_attributes;
	guint32 fileaccess;
	guint32 sharemode;
	guint32 attrs;
};

struct _WapiHandle_find
{
	WapiHandle handle;
	glob_t glob;
	size_t count;
};

static void file_close(WapiHandle *handle);
static WapiFileType file_getfiletype(void);
static gboolean file_read(WapiHandle *handle, gpointer buffer,
			  guint32 numbytes, guint32 *bytesread,
			  WapiOverlapped *overlapped);
static gboolean file_write(WapiHandle *handle, gconstpointer buffer,
			   guint32 numbytes, guint32 *byteswritten,
			   WapiOverlapped *overlapped);
static gboolean file_flush(WapiHandle *handle);
static guint32 file_seek(WapiHandle *handle, gint32 movedistance,
			 gint32 *highmovedistance, WapiSeekMethod method);
static gboolean file_setendoffile(WapiHandle *handle);
static guint32 file_getfilesize(WapiHandle *handle, guint32 *highsize);
static gboolean file_getfiletime(WapiHandle *handle, WapiFileTime *create_time,
				 WapiFileTime *last_access,
				 WapiFileTime *last_write);
static gboolean file_setfiletime(WapiHandle *handle,
				 const WapiFileTime *create_time,
				 const WapiFileTime *last_access,
				 const WapiFileTime *last_write);

/* File handle is only signalled for overlapped IO */
static struct _WapiHandleOps file_ops = {
	file_close,		/* close */
	file_getfiletype,	/* getfiletype */
	file_read,		/* readfile */
	file_write,		/* writefile */
	file_flush,		/* flushfile */
	file_seek,		/* seek */
	file_setendoffile,	/* setendoffile */
	file_getfilesize,	/* getfilesize */
	file_getfiletime,	/* getfiletime */
	file_setfiletime,	/* setfiletime */
	NULL,			/* wait */
	NULL,			/* wait_multiple */
	NULL,			/* signal */
};

static WapiFileType console_getfiletype(void);

/* Console is mostly the same as file, except it can block waiting for
 * input or output
 */
static struct _WapiHandleOps console_ops = {
	file_close,		/* close */
	console_getfiletype,	/* getfiletype */
	file_read,		/* readfile */
	file_write,		/* writefile */
	file_flush,		/* flushfile */
	NULL,			/* seek */
	NULL,			/* setendoffile */
	NULL,			/* getfilesize */
	NULL,			/* getfiletime */
	NULL,			/* setfiletime */
	NULL,			/* FIXME: wait */
	NULL,			/* FIXME: wait_multiple */
	NULL,			/* signal */
};

/* Find handle has no ops.
 */
static struct _WapiHandleOps find_ops = {
	NULL,			/* close */
	NULL,			/* getfiletype */
	NULL,			/* readfile */
	NULL,			/* writefile */
	NULL,			/* flushfile */
	NULL,			/* seek */
	NULL,			/* setendoffile */
	NULL,			/* getfilesize */
	NULL,			/* getfiletime */
	NULL,			/* setfiletime */
	NULL,			/* FIXME: wait */
	NULL,			/* FIXME: wait_multiple */
	NULL,			/* signal */
};

/* Some utility functions.
 */
static void _wapi_time_t_to_filetime (time_t timeval, WapiFileTime *filetime)
{
	guint64 ticks;
	
	ticks = ((guint64)timeval * 10000000) + 116444736000000000UL;
	filetime->dwLowDateTime = ticks & 0xFFFFFFFF;
	filetime->dwHighDateTime = ticks >> 32;
}

static guint32 _wapi_stat_to_file_attributes (struct stat *buf)
{
	guint32 attrs = 0;

	/* FIXME: this could definitely be better */

	if (S_ISDIR (buf->st_mode))
		attrs |= FILE_ATTRIBUTE_DIRECTORY;
	else
		attrs |= FILE_ATTRIBUTE_ARCHIVE;
	
	if (!(buf->st_mode & S_IWUSR))
		attrs |= FILE_ATTRIBUTE_READONLY;
	
	return attrs;
}

static void _wapi_set_last_error_from_errno (void)
{
	/* mapping ideas borrowed from wine. they may need some work */

	switch (errno) {
	case EACCES: case EPERM: case EROFS:
		SetLastError (ERROR_ACCESS_DENIED);
		break;
	
	case EAGAIN:
		SetLastError (ERROR_SHARING_VIOLATION);
		break;
	
	case EBUSY:
		SetLastError (ERROR_LOCK_VIOLATION);
		break;
	
	case EEXIST:
		SetLastError (ERROR_FILE_EXISTS);
		break;
	
	case EINVAL: case ESPIPE:
		SetLastError (ERROR_SEEK);
		break;
	
	case EISDIR:
		SetLastError (ERROR_CANNOT_MAKE);
		break;
	
	case ENFILE: case EMFILE:
		SetLastError (ERROR_NO_MORE_FILES);
		break;

	case ENOENT: case ENOTDIR:
		SetLastError (ERROR_FILE_NOT_FOUND);
		break;
	
	case ENOSPC:
		SetLastError (ERROR_HANDLE_DISK_FULL);
		break;
	
	case ENOTEMPTY:
		SetLastError (ERROR_DIR_NOT_EMPTY);
		break;

	case ENOEXEC:
		SetLastError (ERROR_BAD_FORMAT);
		break;

	case ENAMETOOLONG:
		SetLastError (ERROR_FILENAME_EXCED_RANGE);
		break;
	
	default:
		g_message ("Unknown errno: %s\n", strerror (errno));
		SetLastError (ERROR_GEN_FAILURE);
		break;
	}
}

/* Handle ops.
 */
static void file_close(WapiHandle *handle)
{
	struct _WapiHandle_file *file_handle=(struct _WapiHandle_file *)handle;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": closing file handle %p with fd %d",
		  file_handle, file_handle->fd);
#endif
	
	close(file_handle->fd);
	if(file_handle->filename!=NULL) {
		g_free(file_handle->filename);
		file_handle->filename=NULL;
	}
}

static WapiFileType file_getfiletype(void)
{
	return(FILE_TYPE_DISK);
}

static gboolean file_read(WapiHandle *handle, gpointer buffer,
			  guint32 numbytes, guint32 *bytesread,
			  WapiOverlapped *overlapped G_GNUC_UNUSED)
{
	struct _WapiHandle_file *file_handle=(struct _WapiHandle_file *)handle;
	int ret;
	
	if(bytesread!=NULL) {
		*bytesread=0;
	}
	
	if(!(file_handle->fileaccess&GENERIC_READ) &&
	   !(file_handle->fileaccess&GENERIC_ALL)) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": handle %p fd %d doesn't have GENERIC_READ access: %u", handle, file_handle->fd, file_handle->fileaccess);
#endif

		return(FALSE);
	}
	
	ret=read(file_handle->fd, buffer, numbytes);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": read of handle %p fd %d error: %s", handle,
			  file_handle->fd, strerror(errno));
#endif

		return(FALSE);
	}
	
	if(bytesread!=NULL) {
		*bytesread=ret;
	}
	
	return(TRUE);
}

static gboolean file_write(WapiHandle *handle, gconstpointer buffer,
			   guint32 numbytes, guint32 *byteswritten,
			   WapiOverlapped *overlapped G_GNUC_UNUSED)
{
	struct _WapiHandle_file *file_handle=(struct _WapiHandle_file *)handle;
	int ret;
	
	if(byteswritten!=NULL) {
		*byteswritten=0;
	}
	
	if(!(file_handle->fileaccess&GENERIC_WRITE) &&
	   !(file_handle->fileaccess&GENERIC_ALL)) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": handle %p fd %d doesn't have GENERIC_WRITE access: %u", handle, file_handle->fd, file_handle->fileaccess);
#endif

		return(FALSE);
	}
	
	ret=write(file_handle->fd, buffer, numbytes);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": write of handle %p fd %d error: %s", handle,
			  file_handle->fd, strerror(errno));
#endif

		return(FALSE);
	}
	if(byteswritten!=NULL) {
		*byteswritten=ret;
	}
	
	return(TRUE);
}

static gboolean file_flush(WapiHandle *handle)
{
	struct _WapiHandle_file *file_handle=(struct _WapiHandle_file *)handle;
	int ret;

	if(!(file_handle->fileaccess&GENERIC_WRITE) &&
	   !(file_handle->fileaccess&GENERIC_ALL)) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": handle %p fd %d doesn't have GENERIC_WRITE access: %u", handle, file_handle->fd, file_handle->fileaccess);
#endif

		return(FALSE);
	}

	ret=fsync(file_handle->fd);
	if (ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": write of handle %p fd %d error: %s", handle,
			  file_handle->fd, strerror(errno));
#endif

		return(FALSE);
	}
	
	return(TRUE);
}

static guint32 file_seek(WapiHandle *handle, gint32 movedistance,
			 gint32 *highmovedistance, WapiSeekMethod method)
{
	struct _WapiHandle_file *file_handle=(struct _WapiHandle_file *)handle;
	off_t offset, newpos;
	int whence;
	guint32 ret;
	
	if(!(file_handle->fileaccess&GENERIC_READ) &&
	   !(file_handle->fileaccess&GENERIC_WRITE) &&
	   !(file_handle->fileaccess&GENERIC_ALL)) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": handle %p fd %d doesn't have GENERIC_READ or GENERIC_WRITE access: %u", handle, file_handle->fd, file_handle->fileaccess);
#endif

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
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": invalid seek type %d",
			  method);
#endif

		return(INVALID_SET_FILE_POINTER);
	}

#ifdef HAVE_LARGE_FILE_SUPPORT
	if(highmovedistance==NULL) {
		offset=movedistance;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": setting offset to %lld (low %d)", offset,
			  movedistance);
#endif
	} else {
		offset=((gint64) *highmovedistance << 32) | movedistance;
		
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": setting offset to %lld 0x%llx (high %d 0x%x, low %d 0x%x)", offset, offset, *highmovedistance, *highmovedistance, movedistance, movedistance);
#endif
	}
#else
	offset=movedistance;
#endif

#ifdef DEBUG
#ifdef HAVE_LARGE_FILE_SUPPORT
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": moving handle %p fd %d by %lld bytes from %d", handle,
		  file_handle->fd, offset, whence);
#else
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": moving handle %p fd %d by %ld bytes from %d", handle,
		  file_handle->fd, offset, whence);
#endif
#endif

	newpos=lseek(file_handle->fd, offset, whence);
	if(newpos==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": lseek on handle %p fd %d returned error %s",
			  handle, file_handle->fd, strerror(errno));
#endif

		return(INVALID_SET_FILE_POINTER);
	}

#ifdef DEBUG
#ifdef HAVE_LARGE_FILE_SUPPORT
	g_message(G_GNUC_PRETTY_FUNCTION ": lseek returns %lld", newpos);
#else
	g_message(G_GNUC_PRETTY_FUNCTION ": lseek returns %ld", newpos);
#endif
#endif

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

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": move of handle %p fd %d returning %d/%d", handle,
		  file_handle->fd, ret,
		  highmovedistance==NULL?0:*highmovedistance);
#endif

	return(ret);
}

static gboolean file_setendoffile(WapiHandle *handle)
{
	struct _WapiHandle_file *file_handle=(struct _WapiHandle_file *)handle;
	struct stat statbuf;
	off_t size, pos;
	int ret;
	
	if(!(file_handle->fileaccess&GENERIC_WRITE) &&
	   !(file_handle->fileaccess&GENERIC_ALL)) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": handle %p fd %d doesn't have GENERIC_WRITE access: %u", handle, file_handle->fd, file_handle->fileaccess);
#endif

		return(FALSE);
	}

	/* Find the current file position, and the file length.  If
	 * the file position is greater than the length, write to
	 * extend the file with a hole.  If the file position is less
	 * than the length, truncate the file.
	 */
	
	ret=fstat(file_handle->fd, &statbuf);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": handle %p fd %d fstat failed: %s", handle,
			  file_handle->fd, strerror(errno));
#endif

		return(FALSE);
	}
	size=statbuf.st_size;

	pos=lseek(file_handle->fd, (off_t)0, SEEK_CUR);
	if(pos==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": handle %p fd %d lseek failed: %s", handle,
			  file_handle->fd, strerror(errno));
#endif

		return(FALSE);
	}
	
	if(pos>size) {
		/* extend */
		ret=write(file_handle->fd, "", 1);
		if(ret==-1) {
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION
				  ": handle %p fd %d extend write failed: %s",
				  handle, file_handle->fd, strerror(errno));
#endif

			return(FALSE);
		}
	}

	/* always truncate, because the extend write() adds an extra
	 * byte to the end of the file
	 */
	ret=ftruncate(file_handle->fd, pos);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": handle %p fd %d ftruncate failed: %s", handle,
			  file_handle->fd, strerror(errno));
#endif
		
		return(FALSE);
	}
		
	return(TRUE);
}

static guint32 file_getfilesize(WapiHandle *handle, guint32 *highsize)
{
	struct _WapiHandle_file *file_handle=(struct _WapiHandle_file *)handle;
	struct stat statbuf;
	guint32 size;
	int ret;
	
	if(!(file_handle->fileaccess&GENERIC_READ) &&
	   !(file_handle->fileaccess&GENERIC_WRITE) &&
	   !(file_handle->fileaccess&GENERIC_ALL)) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": handle %p fd %d doesn't have GENERIC_READ or GENERIC_WRITE access: %u", handle, file_handle->fd, file_handle->fileaccess);
#endif

		return(INVALID_FILE_SIZE);
	}

	ret=fstat(file_handle->fd, &statbuf);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": handle %p fd %d fstat failed: %s", handle,
			  file_handle->fd, strerror(errno));
#endif

		return(INVALID_FILE_SIZE);
	}
	
#ifdef HAVE_LARGE_FILE_SUPPORT
	size=statbuf.st_size & 0xFFFFFFFF;
	if(highsize!=NULL) {
		*highsize=statbuf.st_size>>32;
	}
#else
	if(highsize!=NULL) {
		/* Accurate, but potentially dodgy :-) */
		*highsize=0;
	}
	size=statbuf.st_size;
#endif

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Returning size %d/%d", size,
		  *highsize);
#endif
	
	return(size);
}

static gboolean file_getfiletime(WapiHandle *handle, WapiFileTime *create_time,
				 WapiFileTime *last_access,
				 WapiFileTime *last_write)
{
	struct _WapiHandle_file *file_handle=(struct _WapiHandle_file *)handle;
	struct stat statbuf;
	guint64 create_ticks, access_ticks, write_ticks;
	int ret;
	
	if(!(file_handle->fileaccess&GENERIC_READ) &&
	   !(file_handle->fileaccess&GENERIC_ALL)) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": handle %p fd %d doesn't have GENERIC_READ access: %u", handle, file_handle->fd, file_handle->fileaccess);
#endif

		return(FALSE);
	}
	
	ret=fstat(file_handle->fd, &statbuf);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": handle %p fd %d fstat failed: %s", handle,
			  file_handle->fd, strerror(errno));
#endif

		return(FALSE);
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": atime: %ld ctime: %ld mtime: %ld",
		  statbuf.st_atime, statbuf.st_ctime,
		  statbuf.st_mtime);
#endif

	/* Try and guess a meaningful create time by using the older
	 * of atime or ctime
	 */
	/* The magic constant comes from msdn documentation
	 * "Converting a time_t Value to a File Time"
	 */
	if(statbuf.st_atime < statbuf.st_ctime) {
		create_ticks=((guint64)statbuf.st_atime*10000000)
			+ 116444736000000000UL;
	} else {
		create_ticks=((guint64)statbuf.st_ctime*10000000)
			+ 116444736000000000UL;
	}
	
	access_ticks=((guint64)statbuf.st_atime*10000000)+116444736000000000UL;
	write_ticks=((guint64)statbuf.st_mtime*10000000)+116444736000000000UL;
	
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": aticks: %llu cticks: %llu wticks: %llu",
			  access_ticks, create_ticks, write_ticks);
#endif

	if(create_time!=NULL) {
		create_time->dwLowDateTime = create_ticks & 0xFFFFFFFF;
		create_time->dwHighDateTime = create_ticks >> 32;
	}
	
	if(last_access!=NULL) {
		last_access->dwLowDateTime = access_ticks & 0xFFFFFFFF;
		last_access->dwHighDateTime = access_ticks >> 32;
	}
	
	if(last_write!=NULL) {
		last_write->dwLowDateTime = write_ticks & 0xFFFFFFFF;
		last_write->dwHighDateTime = write_ticks >> 32;
	}

	return(TRUE);
}

static gboolean file_setfiletime(WapiHandle *handle,
				 const WapiFileTime *create_time G_GNUC_UNUSED,
				 const WapiFileTime *last_access,
				 const WapiFileTime *last_write)
{
	struct _WapiHandle_file *file_handle=(struct _WapiHandle_file *)handle;
	struct utimbuf utbuf;
	struct stat statbuf;
	guint64 access_ticks, write_ticks;
	int ret;
	
	if(!(file_handle->fileaccess&GENERIC_WRITE) &&
	   !(file_handle->fileaccess&GENERIC_ALL)) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": handle %p fd %d doesn't have GENERIC_WRITE access: %u", handle, file_handle->fd, file_handle->fileaccess);
#endif

		return(FALSE);
	}

	if(file_handle->filename==NULL) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": handle %p fd %d unknown filename", handle,
			  file_handle->fd);
#endif

		return(FALSE);
	}
	
	/* Get the current times, so we can put the same times back in
	 * the event that one of the FileTime structs is NULL
	 */
	ret=fstat(file_handle->fd, &statbuf);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": handle %p fd %d fstat failed: %s", handle,
			  file_handle->fd, strerror(errno));
#endif

		return(FALSE);
	}

	if(last_access!=NULL) {
		access_ticks=((guint64)last_access->dwHighDateTime << 32) +
			last_access->dwLowDateTime;
		utbuf.actime=(access_ticks - 116444736000000000) / 10000000;
	} else {
		utbuf.actime=statbuf.st_atime;
	}

	if(last_write!=NULL) {
		write_ticks=((guint64)last_write->dwHighDateTime << 32) +
			last_write->dwLowDateTime;
		utbuf.modtime=(write_ticks - 116444736000000000) / 10000000;
	} else {
		utbuf.modtime=statbuf.st_mtime;
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": setting handle %p access %ld write %ld", handle,
		  utbuf.actime, utbuf.modtime);
#endif

	ret=utime(file_handle->filename, &utbuf);
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": handle %p [%s] fd %d utime failed: %s", handle,
			  file_handle->filename, file_handle->fd,
			  strerror(errno));
#endif

		return(FALSE);
	}

	return(TRUE);
}

static WapiFileType console_getfiletype(void)
{
	return(FILE_TYPE_CHAR);
}

static int convert_flags(guint32 fileaccess, guint32 createmode)
{
	int flags=0;
	
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
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Unknown access type 0x%x",
			  fileaccess);
#endif
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
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Unknown create mode 0x%x",
			  createmode);
#endif
		break;
	}
	
	return(flags);
}

static guint32 convert_from_flags(int flags)
{
	guint32 fileaccess=0;
	
	if(flags&O_RDONLY) {
		fileaccess=GENERIC_READ;
	} else if (flags&O_WRONLY) {
		fileaccess=GENERIC_WRITE;
	} else if (flags&O_RDWR) {
		fileaccess=GENERIC_READ|GENERIC_WRITE;
	} else {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Can't figure out flags 0x%x", flags);
#endif
	}

	/* Maybe sort out create mode too */

	return(fileaccess);
}

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


/**
 * CreateFile:
 * @name: a pointer to a NULL-terminated unicode string, that names
 * the file or other object to create.
 * @fileaccess: specifies the file access mode
 * @sharemode: whether the file should be shared.  This parameter is
 * currently ignored.
 * @security: Ignored for now.
 * @createmode: specifies whether to create a new file, whether to
 * overwrite an existing file, whether to truncate the file, etc.
 * @attrs: specifies file attributes and flags.  On win32 attributes
 * are characteristics of the file, not the handle, and are ignored
 * when an existing file is opened.  Flags give the library hints on
 * how to process a file to optimise performance.
 * @template: the handle of an open %GENERIC_READ file that specifies
 * attributes to apply to a newly created file, ignoring @attrs.
 * Normally this parameter is NULL.  This parameter is ignored when an
 * existing file is opened.
 *
 * Creates a new file handle.  This only applies to normal files:
 * pipes are handled by CreatePipe(), and console handles are created
 * with GetStdHandle().
 *
 * Return value: the new handle, or %INVALID_HANDLE_VALUE on error.
 */
WapiHandle *CreateFile(const gunichar2 *name, guint32 fileaccess,
		       guint32 sharemode, WapiSecurityAttributes *security,
		       guint32 createmode, guint32 attrs,
		       WapiHandle *template G_GNUC_UNUSED)
{
	struct _WapiHandle_file *file_handle;
	WapiHandle *handle;
	int flags=convert_flags(fileaccess, createmode);
	mode_t perms=convert_perms(sharemode);
	gchar *filename;
	int ret;
	
	if(name==NULL) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": name is NULL");
#endif

		return(INVALID_HANDLE_VALUE);
	}
	filename=_wapi_unicode_to_utf8(name);

#ifdef ACTUALLY_DO_UNICODE
	if(filename==NULL) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": unicode conversion returned NULL");
#endif

		return(INVALID_HANDLE_VALUE);
	}
#endif
	
#ifdef ACTUALLY_DO_UNICODE
	ret=open(filename, flags, perms);
#else
	ret=open(name, flags, perms);
#endif
	
	if(ret==-1) {
#ifdef DEBUG
#ifdef ACTUALLY_DO_UNICODE
		g_message(G_GNUC_PRETTY_FUNCTION ": Error opening file %s: %s",
			  filename, strerror(errno));
#else
		g_message(G_GNUC_PRETTY_FUNCTION ": Error opening file %s: %s",
			  filename, strerror(errno));
#endif
#endif
		return(INVALID_HANDLE_VALUE);
	}

	file_handle=g_new0(struct _WapiHandle_file, 1);
	handle=(WapiHandle *)file_handle;
	
	_WAPI_HANDLE_INIT(handle, WAPI_HANDLE_FILE, file_ops);

	file_handle->fd=ret;
#ifdef ACTUALLY_DO_UNICODE
	file_handle->filename=filename;
#else
	file_handle->filename=g_strdup(name);
#endif
	file_handle->security_attributes=security;
	file_handle->fileaccess=fileaccess;
	file_handle->sharemode=sharemode;
	file_handle->attrs=attrs;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": returning handle %p [%s] with fd %d",
		  handle, file_handle->filename, file_handle->fd);
#endif

	return(handle);
}

/**
 * DeleteFile:
 * @name: a pointer to a NULL-terminated unicode string, that names
 * the file to be deleted.
 *
 * Deletes file @name.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean DeleteFile(const gunichar2 *name)
{
	gchar *filename;
	int ret;
	
	if(name==NULL) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": name is NULL");
#endif

		return(FALSE);
	}

	filename=_wapi_unicode_to_utf8(name);
#ifdef ACTUALLY_DO_UNICODE
	if(filename==NULL) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": unicode conversion returned NULL");
#endif

		return(FALSE);
	}
#endif
	
#ifdef ACTUALLY_DO_UNICODE
	ret=unlink(filename);
#else
	ret=unlink(name);
#endif
	
	g_free(filename);

	if(ret==0) {
		return(TRUE);
	} else {
		return(FALSE);
	}
}

/**
 * MoveFile:
 * @name: a pointer to a NULL-terminated unicode string, that names
 * the file to be moved.
 * @dest_name: a pointer to a NULL-terminated unicode string, that is the
 * new name for the file.
 *
 * Renames file @name to @dest_name
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean MoveFile (const gunichar2 *name, const gunichar2 *dest_name)
{
	gchar *utf8_name, *utf8_dest_name;
	int result;

	utf8_name = _wapi_unicode_to_utf8 (name);
	if (utf8_name == NULL) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": unicode conversion returned NULL");
#endif
		
		return FALSE;
	}

	utf8_dest_name = _wapi_unicode_to_utf8 (dest_name);
	if (utf8_dest_name == NULL) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": unicode conversion returned NULL");
#endif

		g_free (utf8_name);
		return FALSE;
	}

	result = rename (utf8_name, utf8_dest_name);
	g_free (utf8_name);
	g_free (utf8_dest_name);

	if (result == 0)
		return TRUE;
	
	switch (errno) {
	case EEXIST:
		SetLastError (ERROR_ALREADY_EXISTS);
		break;
	
	default:
		_wapi_set_last_error_from_errno ();
		break;
	}

	return FALSE;
}

/**
 * CopyFile:
 * @name: a pointer to a NULL-terminated unicode string, that names
 * the file to be copied.
 * @dest_name: a pointer to a NULL-terminated unicode string, that is the
 * new name for the file.
 * @fail_if_exists: if TRUE and dest_name exists, the copy will fail.
 *
 * Copies file @name to @dest_name
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean CopyFile (const gunichar2 *name, const gunichar2 *dest_name, gboolean fail_if_exists)
{
	WapiHandle *src, *dest;
	guint32 attrs;
	char *buffer;
	int remain, n;

	attrs = GetFileAttributes (name);
	if (attrs == -1) {
		SetLastError (ERROR_FILE_NOT_FOUND);
		return  FALSE;
	}
	
	src = CreateFile (name, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE,
			  NULL, OPEN_EXISTING, 0, NULL);
	if (src == INVALID_HANDLE_VALUE) {
		_wapi_set_last_error_from_errno ();
		return FALSE;
	}
	
	dest = CreateFile (dest_name, GENERIC_WRITE, 0, NULL,
			   fail_if_exists ? CREATE_NEW : CREATE_ALWAYS, attrs, NULL);
	if (dest == INVALID_HANDLE_VALUE) {
		_wapi_set_last_error_from_errno ();
		CloseHandle (src);
		return FALSE;
	}

	buffer = g_new (gchar, 2048);

	for (;;) {
		if (ReadFile (src, buffer,sizeof (buffer), &remain, NULL) == 0) {
			_wapi_set_last_error_from_errno ();
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": read failed.");
#endif
			
			CloseHandle (dest);
			CloseHandle (src);
			return FALSE;
		}

		if (remain == 0)
			break;

		while (remain > 0) {
			if (WriteFile (dest, buffer, remain, &n, NULL) == 0) {
				_wapi_set_last_error_from_errno ();
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION ": write failed.");
#endif
				
				CloseHandle (dest);
				CloseHandle (src);
				return FALSE;
			}

			remain -= n;
		}
	}

	g_free (buffer);

	CloseHandle (dest);
	CloseHandle (src);
	return TRUE;
}

/**
 * GetStdHandle:
 * @stdhandle: specifies the file descriptor
 *
 * Returns a handle for stdin, stdout, or stderr.  Always returns the
 * same handle for the same @stdhandle.
 *
 * Return value: the handle, or %INVALID_HANDLE_VALUE on error
 */

WapiHandle *GetStdHandle(WapiStdHandle stdhandle)
{
	struct _WapiHandle_file *file_handle;
	WapiHandle *handle;
	int flags, fd;
	
	switch(stdhandle) {
	case STD_INPUT_HANDLE:
		fd=0;
		break;

	case STD_OUTPUT_HANDLE:
		fd=1;
		break;

	case STD_ERROR_HANDLE:
		fd=2;
		break;

	default:
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": unknown standard handle type");
#endif

		return(INVALID_HANDLE_VALUE);
	}
	
	/* Check if fd is valid */
	flags=fcntl(fd, F_GETFL);
	if(flags==-1) {
		/* Invalid fd.  Not really much point checking for EBADF
		 * specifically
		 */
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": fcntl error on fd %d: %s",
			  fd, strerror(errno));
#endif

		return(INVALID_HANDLE_VALUE);
	}
	
	file_handle=g_new0(struct _WapiHandle_file, 1);
	handle=(WapiHandle *)file_handle;
	
	_WAPI_HANDLE_INIT(handle, WAPI_HANDLE_CONSOLE, console_ops);

	file_handle->fd=fd;
	/* We might want to set file_handle->filename to something
	 * like "<stdin>" if we ever want to display handle internal
	 * details somehow
	 */
	file_handle->security_attributes=/*some default*/NULL;
	file_handle->fileaccess=convert_from_flags(flags);
	file_handle->sharemode=0;
	file_handle->attrs=0;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": returning handle %p with fd %d",
		  handle, file_handle->fd);
#endif

	return(handle);
}

/**
 * ReadFile:
 * @handle: The file handle to read from.  The handle must have
 * %GENERIC_READ access.
 * @buffer: The buffer to store read data in
 * @numbytes: The maximum number of bytes to read
 * @bytesread: The actual number of bytes read is stored here.  This
 * value can be zero if the handle is positioned at the end of the
 * file.
 * @overlapped: points to a required %WapiOverlapped structure if
 * @handle has the %FILE_FLAG_OVERLAPPED option set, should be NULL
 * otherwise.
 *
 * If @handle does not have the %FILE_FLAG_OVERLAPPED option set, this
 * function reads up to @numbytes bytes from the file from the current
 * file position, and stores them in @buffer.  If there are not enough
 * bytes left in the file, just the amount available will be read.
 * The actual number of bytes read is stored in @bytesread.

 * If @handle has the %FILE_FLAG_OVERLAPPED option set, the current
 * file position is ignored and the read position is taken from data
 * in the @overlapped structure.
 *
 * Return value: %TRUE if the read succeeds (even if no bytes were
 * read due to an attempt to read past the end of the file), %FALSE on
 * error.
 */
gboolean ReadFile(WapiHandle *handle, gpointer buffer, guint32 numbytes,
		  guint32 *bytesread, WapiOverlapped *overlapped)
{
	if(handle->ops->readfile==NULL) {
		return(FALSE);
	}
	
	return(handle->ops->readfile(handle, buffer, numbytes, bytesread,
				     overlapped));
}

/**
 * WriteFile:
 * @handle: The file handle to write to.  The handle must have
 * %GENERIC_WRITE access.
 * @buffer: The buffer to read data from.
 * @numbytes: The maximum number of bytes to write.
 * @byteswritten: The actual number of bytes written is stored here.
 * If the handle is positioned at the file end, the length of the file
 * is extended.  This parameter may be %NULL.
 * @overlapped: points to a required %WapiOverlapped structure if
 * @handle has the %FILE_FLAG_OVERLAPPED option set, should be NULL
 * otherwise.
 *
 * If @handle does not have the %FILE_FLAG_OVERLAPPED option set, this
 * function writes up to @numbytes bytes from @buffer to the file at
 * the current file position.  If @handle is positioned at the end of
 * the file, the file is extended.  The actual number of bytes written
 * is stored in @byteswritten.
 *
 * If @handle has the %FILE_FLAG_OVERLAPPED option set, the current
 * file position is ignored and the write position is taken from data
 * in the @overlapped structure.
 *
 * Return value: %TRUE if the write succeeds, %FALSE on error.
 */
gboolean WriteFile(WapiHandle *handle, gconstpointer buffer, guint32 numbytes,
		   guint32 *byteswritten, WapiOverlapped *overlapped)
{
	if(handle->ops->writefile==NULL) {
		return(FALSE);
	}
	
	return(handle->ops->writefile(handle, buffer, numbytes, byteswritten,
				      overlapped));
}

/**
 * FlushFileBuffers:
 * @handle: Handle to open file.  The handle must have
 * %GENERIC_WRITE access.
 *
 * Flushes buffers of the file and causes all unwritten data to
 * be written.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean FlushFileBuffers(WapiHandle *handle)
{
	if(handle->ops->flushfile==NULL) {
		return(FALSE);
	}
	
	return(handle->ops->flushfile(handle));
}

/**
 * SetEndOfFile:
 * @handle: The file handle to set.  The handle must have
 * %GENERIC_WRITE access.
 *
 * Moves the end-of-file position to the current position of the file
 * pointer.  This function is used to truncate or extend a file.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean SetEndOfFile(WapiHandle *handle)
{
	if(handle->ops->setendoffile==NULL) {
		return(FALSE);
	}
	
	return(handle->ops->setendoffile(handle));
}

/**
 * SetFilePointer:
 * @handle: The file handle to set.  The handle must have
 * %GENERIC_READ or %GENERIC_WRITE access.
 * @movedistance: Low 32 bits of a signed value that specifies the
 * number of bytes to move the file pointer.
 * @highmovedistance: Pointer to the high 32 bits of a signed value
 * that specifies the number of bytes to move the file pointer, or
 * %NULL.
 * @method: The starting point for the file pointer move.
 *
 * Sets the file pointer of an open file.
 *
 * The distance to move the file pointer is calculated from
 * @movedistance and @highmovedistance: If @highmovedistance is %NULL,
 * @movedistance is the 32-bit signed value; otherwise, @movedistance
 * is the low 32 bits and @highmovedistance a pointer to the high 32
 * bits of a 64 bit signed value.  A positive distance moves the file
 * pointer forward from the position specified by @method; a negative
 * distance moves the file pointer backward.
 *
 * If the library is compiled without large file support,
 * @highmovedistance is ignored and its value is set to zero on a
 * successful return.
 *
 * Return value: On success, the low 32 bits of the new file pointer.
 * If @highmovedistance is not %NULL, the high 32 bits of the new file
 * pointer are stored there.  On failure, %INVALID_SET_FILE_POINTER.
 */
guint32 SetFilePointer(WapiHandle *handle, gint32 movedistance,
		       gint32 *highmovedistance, WapiSeekMethod method)
{
	if(handle->ops->seek==NULL) {
		return(INVALID_SET_FILE_POINTER);
	}
	
	return(handle->ops->seek(handle, movedistance, highmovedistance,
				 method));
}

/**
 * GetFileType:
 * @handle: The file handle to test.
 *
 * Finds the type of file @handle.
 *
 * Return value: %FILE_TYPE_UNKNOWN - the type of the file @handle is
 * unknown.  %FILE_TYPE_DISK - @handle is a disk file.
 * %FILE_TYPE_CHAR - @handle is a character device, such as a console.
 * %FILE_TYPE_PIPE - @handle is a named or anonymous pipe.
 */
WapiFileType GetFileType(WapiHandle *handle)
{
	if(handle->ops->getfiletype==NULL) {
		return(FILE_TYPE_UNKNOWN);
	}
	
	return(handle->ops->getfiletype());
}

/**
 * GetFileSize:
 * @handle: The file handle to query.  The handle must have
 * %GENERIC_READ or %GENERIC_WRITE access.
 * @highsize: If non-%NULL, the high 32 bits of the file size are
 * stored here.
 *
 * Retrieves the size of the file @handle.
 *
 * If the library is compiled without large file support, @highsize
 * has its value set to zero on a successful return.
 *
 * Return value: On success, the low 32 bits of the file size.  If
 * @highsize is non-%NULL then the high 32 bits of the file size are
 * stored here.  On failure %INVALID_FILE_SIZE is returned.
 */
guint32 GetFileSize(WapiHandle *handle, guint32 *highsize)
{
	if(handle->ops->getfilesize==NULL) {
		return(INVALID_FILE_SIZE);
	}
	
	return(handle->ops->getfilesize(handle, highsize));
}

/**
 * GetFileTime:
 * @handle: The file handle to query.  The handle must have
 * %GENERIC_READ access.
 * @create_time: Points to a %WapiFileTime structure to receive the
 * number of ticks since the epoch that file was created.  May be
 * %NULL.
 * @last_access: Points to a %WapiFileTime structure to receive the
 * number of ticks since the epoch when file was last accessed.  May be
 * %NULL.
 * @last_write: Points to a %WapiFileTime structure to receive the
 * number of ticks since the epoch when file was last written to.  May
 * be %NULL.
 *
 * Finds the number of ticks since the epoch that the file referenced
 * by @handle was created, last accessed and last modified.  A tick is
 * a 100 nanosecond interval.  The epoch is Midnight, January 1 1601
 * GMT.
 *
 * Create time isn't recorded on POSIX file systems or reported by
 * stat(2), so that time is guessed by returning the oldest of the
 * other times.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean GetFileTime(WapiHandle *handle, WapiFileTime *create_time,
		     WapiFileTime *last_access, WapiFileTime *last_write)
{
	if(handle->ops->getfiletime==NULL) {
		return(FALSE);
	}
	
	return(handle->ops->getfiletime(handle, create_time, last_access,
					last_write));
}

/**
 * SetFileTime:
 * @handle: The file handle to set.  The handle must have
 * %GENERIC_WRITE access.
 * @create_time: Points to a %WapiFileTime structure that contains the
 * number of ticks since the epoch that the file was created.  May be
 * %NULL.
 * @last_access: Points to a %WapiFileTime structure that contains the
 * number of ticks since the epoch when the file was last accessed.
 * May be %NULL.
 * @last_write: Points to a %WapiFileTime structure that contains the
 * number of ticks since the epoch when the file was last written to.
 * May be %NULL.
 *
 * Sets the number of ticks since the epoch that the file referenced
 * by @handle was created, last accessed or last modified.  A tick is
 * a 100 nanosecond interval.  The epoch is Midnight, January 1 1601
 * GMT.
 *
 * Create time isn't recorded on POSIX file systems, and is ignored.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean SetFileTime(WapiHandle *handle, const WapiFileTime *create_time,
		     const WapiFileTime *last_access,
		     const WapiFileTime *last_write)
{
	if(handle->ops->setfiletime==NULL) {
		return(FALSE);
	}
	
	return(handle->ops->setfiletime(handle, create_time, last_access,
					last_write));
}

/* A tick is a 100-nanosecond interval.  File time epoch is Midnight,
 * January 1 1601 GMT
 */

#define TICKS_PER_MILLISECOND 10000L
#define TICKS_PER_SECOND 10000000L
#define TICKS_PER_MINUTE 600000000L
#define TICKS_PER_HOUR 36000000000L
#define TICKS_PER_DAY 864000000000L

#define isleap(y) ((y) % 4 == 0 && ((y) % 100 != 0 || (y) % 400 == 0))

static const guint16 mon_yday[2][13]={
	{0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365},
	{0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366},
};

/**
 * FileTimeToSystemTime:
 * @file_time: Points to a %WapiFileTime structure that contains the
 * number of ticks to convert.
 * @system_time: Points to a %WapiSystemTime structure to receive the
 * broken-out time.
 *
 * Converts a tick count into broken-out time values.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean FileTimeToSystemTime(const WapiFileTime *file_time,
			      WapiSystemTime *system_time)
{
	gint64 file_ticks, totaldays, rem, y;
	const guint16 *ip;
	
	if(system_time==NULL) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": system_time NULL");
#endif

		return(FALSE);
	}
	
	file_ticks=((gint64)file_time->dwHighDateTime << 32) +
		file_time->dwLowDateTime;
	
	/* Really compares if file_ticks>=0x8000000000000000
	 * (LLONG_MAX+1) but we're working with a signed value for the
	 * year and day calculation to work later
	 */
	if(file_ticks<0) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": file_time too big");
#endif

		return(FALSE);
	}

	totaldays=(file_ticks / TICKS_PER_DAY);
	rem = file_ticks % TICKS_PER_DAY;
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": totaldays: %lld rem: %lld",
		  totaldays, rem);
#endif

	system_time->wHour=rem/TICKS_PER_HOUR;
	rem %= TICKS_PER_HOUR;
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Hour: %d rem: %lld",
		  system_time->wHour, rem);
#endif
	
	system_time->wMinute = rem / TICKS_PER_MINUTE;
	rem %= TICKS_PER_MINUTE;
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Minute: %d rem: %lld",
		  system_time->wMinute, rem);
#endif
	
	system_time->wSecond = rem / TICKS_PER_SECOND;
	rem %= TICKS_PER_SECOND;
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Second: %d rem: %lld",
		  system_time->wSecond, rem);
#endif
	
	system_time->wMilliseconds = rem / TICKS_PER_MILLISECOND;
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Milliseconds: %d",
		  system_time->wMilliseconds);
#endif

	/* January 1, 1601 was a Monday, according to Emacs calendar */
	system_time->wDayOfWeek = ((1 + totaldays) % 7) + 1;
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Day of week: %d",
		  system_time->wDayOfWeek);
#endif
	
	/* This algorithm to find year and month given days from epoch
	 * from glibc
	 */
	y=1601;
	
#define DIV(a, b) ((a) / (b) - ((a) % (b) < 0))
#define LEAPS_THRU_END_OF(y) (DIV(y, 4) - DIV (y, 100) + DIV (y, 400))

	while(totaldays < 0 || totaldays >= (isleap(y)?366:365)) {
		/* Guess a corrected year, assuming 365 days per year */
		gint64 yg = y + totaldays / 365 - (totaldays % 365 < 0);
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": totaldays: %lld yg: %lld y: %lld", totaldays, yg,
			  y);
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": LEAPS(yg): %lld LEAPS(y): %lld",
			  LEAPS_THRU_END_OF(yg-1), LEAPS_THRU_END_OF(y-1));
#endif
		
		/* Adjust days and y to match the guessed year. */
		totaldays -= ((yg - y) * 365
			      + LEAPS_THRU_END_OF (yg - 1)
			      - LEAPS_THRU_END_OF (y - 1));
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": totaldays: %lld",
			  totaldays);
#endif
		y = yg;
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": y: %lld", y);
#endif
	}
	
	system_time->wYear = y;
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Year: %d", system_time->wYear);
#endif

	ip = mon_yday[isleap(y)];
	
	for(y=11; totaldays < ip[y]; --y) {
		continue;
	}
	totaldays-=ip[y];
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": totaldays: %lld", totaldays);
#endif
	
	system_time->wMonth = y + 1;
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Month: %d", system_time->wMonth);
#endif

	system_time->wDay = totaldays + 1;
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Day: %d", system_time->wDay);
#endif
	
	return(TRUE);
}

WapiHandle *FindFirstFile (const gunichar2 *pattern, WapiFindData *find_data)
{
	struct _WapiHandle_find *handle;
	gchar *utf8_pattern = NULL;
	int result;
	
	if (pattern == NULL) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": pattern is NULL");
#endif

		return INVALID_HANDLE_VALUE;
	}

	utf8_pattern = _wapi_unicode_to_utf8 (pattern);
	if (utf8_pattern == NULL) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": unicode conversion returned NULL");
#endif
		
		return INVALID_HANDLE_VALUE;
	}
	
	handle = g_new0 (struct _WapiHandle_find, 1);
	_WAPI_HANDLE_INIT ((&handle->handle), WAPI_HANDLE_FIND, find_ops);

	result = glob (utf8_pattern, 0, NULL, &handle->glob);
	g_free (utf8_pattern);

	if (result != 0) {
		globfree (&handle->glob);
		g_free (handle);

		switch (result) {
#ifdef GLOB_NOMATCH
		case GLOB_NOMATCH:
			SetLastError (ERROR_NO_MORE_FILES);
			break;
#endif

		default:
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": glob failed with code %d.", result);
#endif

			break;
		}

		return INVALID_HANDLE_VALUE;
	}

	handle->count = 0;
	if (!FindNextFile ((WapiHandle *)handle, find_data)) {
		FindClose ((WapiHandle *)handle);
		SetLastError (ERROR_NO_MORE_FILES);
		return INVALID_HANDLE_VALUE;
	}

	return (WapiHandle *)handle;
}

gboolean FindNextFile (WapiHandle *wapi_handle, WapiFindData *find_data)
{
	struct _WapiHandle_find *handle;
	struct stat buf;
	const gchar *filename;
	
	gchar *base_filename;
	gunichar2 *utf16_basename;
	time_t create_time;
	int i;

	if (wapi_handle == INVALID_HANDLE_VALUE || wapi_handle->type != WAPI_HANDLE_FIND) {
		SetLastError (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	handle = (struct _WapiHandle_find *)wapi_handle;
	if (handle->count >= handle->glob.gl_pathc) {
		SetLastError (ERROR_NO_MORE_FILES);
		return FALSE;
	}

	/* stat next glob match */

	filename = handle->glob.gl_pathv [handle->count ++];
	if (stat (filename, &buf) != 0) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": stat failed: %s", filename);
#endif

		SetLastError (ERROR_NO_MORE_FILES);
		return FALSE;
	}

	/* fill data block */

	if (buf.st_mtime < buf.st_ctime)
		create_time = buf.st_mtime;
	else
		create_time = buf.st_ctime;
	
	find_data->dwFileAttributes = _wapi_stat_to_file_attributes (&buf);

	_wapi_time_t_to_filetime (create_time, &find_data->ftCreationTime);
	_wapi_time_t_to_filetime (buf.st_atime, &find_data->ftLastAccessTime);
	_wapi_time_t_to_filetime (buf.st_mtime, &find_data->ftLastWriteTime);

	if (find_data->dwFileAttributes && FILE_ATTRIBUTE_DIRECTORY) {
		find_data->nFileSizeHigh = 0;
		find_data->nFileSizeLow = 0;
	}
	else {
		find_data->nFileSizeHigh = buf.st_size >> 32;
		find_data->nFileSizeLow = buf.st_size & 0xFFFFFFFF;
	}

	find_data->dwReserved0 = 0;
	find_data->dwReserved1 = 0;

	base_filename = g_path_get_basename (filename);
	utf16_basename = g_utf8_to_utf16 (base_filename, MAX_PATH, NULL, NULL, NULL);

	i = 0;
	while (utf16_basename [i] != 0) {	/* copy basename */
		find_data->cFileName [i] = utf16_basename [i];
		++ i;
	}

	find_data->cFileName[i] = 0;		/* null terminate */
	find_data->cAlternateFileName [0] = 0;	/* not used */

	g_free (base_filename);
	g_free (utf16_basename);
	return TRUE;
}

/**
 * FindClose:
 * @wapi_handle: the find handle to close.
 *
 * Closes find handle @wapi_handle
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean FindClose (WapiHandle *wapi_handle)
{
	struct _WapiHandle_find *handle;

	if (wapi_handle == INVALID_HANDLE_VALUE || wapi_handle->type != WAPI_HANDLE_FIND) {
		SetLastError (ERROR_INVALID_HANDLE);
		return FALSE;
	}
	
	handle = (struct _WapiHandle_find *)wapi_handle;
	globfree (&handle->glob);
	g_free (handle);

	return TRUE;
}

/**
 * CreateDirectory:
 * @name: a pointer to a NULL-terminated unicode string, that names
 * the directory to be created.
 * @security: ignored for now
 *
 * Creates directory @name
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean CreateDirectory (const gunichar2 *name, WapiSecurityAttributes *security)
{
	gchar *utf8_name;
	int result;
	
	utf8_name = _wapi_unicode_to_utf8 (name);
	if (utf8_name == NULL) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": unicode conversion returned NULL");
#endif
	
		return FALSE;
	}

	result = mkdir (utf8_name, 0777);
	g_free (utf8_name);

	if (result == 0)
		return TRUE;

	switch (errno) {
	case EEXIST:
		return TRUE;
	default:
		_wapi_set_last_error_from_errno ();
		break;
	}
	
	return FALSE;
}

/**
 * RemoveDirectory:
 * @name: a pointer to a NULL-terminated unicode string, that names
 * the directory to be removed.
 *
 * Removes directory @name
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean RemoveDirectory (const gunichar2 *name)
{
	gchar *utf8_name;
	int result;

	utf8_name = _wapi_unicode_to_utf8 (name);
	if (utf8_name == NULL) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": unicode conversion returned NULL");
#endif
		
		return FALSE;
	}

	result = rmdir (utf8_name);
	g_free (utf8_name);

	if (result == 0)
		return TRUE;
	
	_wapi_set_last_error_from_errno ();
	return FALSE;
}

/**
 * GetFileAttributes:
 * @name: a pointer to a NULL-terminated unicode filename.
 *
 * Gets the attributes for @name;
 *
 * Return value: -1 on failure
 */
guint32 GetFileAttributes (const gunichar2 *name)
{
	gchar *utf8_name;
	struct stat buf;
	int result;
	
	utf8_name = _wapi_unicode_to_utf8 (name);
	if (utf8_name == NULL) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": unicode conversion returned NULL");
#endif

		SetLastError (ERROR_INVALID_PARAMETER);
		return -1;
	}

	result = stat (utf8_name, &buf);
	g_free (utf8_name);

	if (result != 0) {
		SetLastError (ERROR_FILE_NOT_FOUND);
		return -1;
	}
	
	return _wapi_stat_to_file_attributes (&buf);
}

/**
 * GetFileAttributesEx:
 * @name: a pointer to a NULL-terminated unicode filename.
 * @level: must be GetFileExInfoStandard
 * @info: pointer to a WapiFileAttributesData structure
 *
 * Gets attributes, size and filetimes for @name;
 *
 * Return value: %TRUE on success, %FALSE on failure
 */
gboolean GetFileAttributesEx (const gunichar2 *name, WapiGetFileExInfoLevels level, gpointer info)
{
	gchar *utf8_name;
	WapiFileAttributesData *data;

	struct stat buf;
	time_t create_time;
	int result;
	
	if (level != GetFileExInfoStandard) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": info level %d not supported.", level);
#endif

		return FALSE;
	}

	utf8_name = _wapi_unicode_to_utf8 (name);
	if (utf8_name == NULL) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": unicode conversion returned NULL");
#endif

		SetLastError (ERROR_INVALID_PARAMETER);
		return FALSE;
	}

	result = stat (utf8_name, &buf);
	g_free (utf8_name);

	if (result != 0) {
		SetLastError (ERROR_FILE_NOT_FOUND);
		return FALSE;
	}

	/* fill data block */

	data = (WapiFileAttributesData *)info;

	if (buf.st_mtime < buf.st_ctime)
		create_time = buf.st_mtime;
	else
		create_time = buf.st_ctime;
	
	data->dwFileAttributes = _wapi_stat_to_file_attributes (&buf);

	_wapi_time_t_to_filetime (create_time, &data->ftCreationTime);
	_wapi_time_t_to_filetime (buf.st_atime, &data->ftLastAccessTime);
	_wapi_time_t_to_filetime (buf.st_mtime, &data->ftLastWriteTime);

	if (data->dwFileAttributes && FILE_ATTRIBUTE_DIRECTORY) {
		data->nFileSizeHigh = 0;
		data->nFileSizeLow = 0;
	}
	else {
		data->nFileSizeHigh = buf.st_size >> 32;
		data->nFileSizeLow = buf.st_size & 0xFFFFFFFF;
	}

	return TRUE;
}

/**
 * SetFileAttributes
 * @name: name of file
 * @attrs: attributes to set
 *
 * Changes the attributes on a named file.
 *
 * Return value: %TRUE on success, %FALSE on failure.
 */
extern gboolean SetFileAttributes (const gunichar2 *name, guint32 attrs)
{
	/* FIXME: think of something clever to do on unix */
	
	SetLastError (ERROR_INVALID_FUNCTION);
	return FALSE;
}

/**
 * GetCurrentDirectory
 * @length: size of the buffer
 * @buffer: pointer to buffer that recieves path
 *
 * Retrieves the current directory for the current process.
 *
 * Return value: number of characters in buffer on success, zero on failure
 */
extern guint32 GetCurrentDirectory (guint32 length, gunichar2 *buffer)
{
	gchar *path;
	gunichar2 *utf16_path, *ptr;
	glong count = 0;
	
	path = g_get_current_dir ();
	if (path == NULL)
		return 0;
	
	/* if buffer too small, return number of characters required.
	 * this is plain dumb.
	 */
	
	count = strlen (path) + 1;
	if (count > length)
		return count;
	
	utf16_path = g_utf8_to_utf16 (path, -1, NULL, NULL, NULL);
	if (utf16_path == NULL)
		return 0;

	ptr = utf16_path;
	while (*ptr)
		*buffer ++ = *ptr ++;
	
	*buffer = 0;
	
	g_free (utf16_path);
	g_free (path);

	return count;
}

/**
 * SetCurrentDirectory
 * @path: path to new directory
 *
 * Changes the directory path for the current process.
 *
 * Return value: %TRUE on success, %FALSE on failure.
 */
extern gboolean SetCurrentDirectory (const gunichar2 *path)
{
	gchar *utf8_path;
	gboolean result;

	utf8_path = _wapi_unicode_to_utf8 (path);
	if (chdir (utf8_path) != 0) {
		_wapi_set_last_error_from_errno ();
		result = FALSE;
	}
	else
		result = TRUE;

	g_free (utf8_path);
	return result;
}
