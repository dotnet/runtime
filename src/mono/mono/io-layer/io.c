#include <config.h>
#include <glib.h>
#include <fcntl.h>
#include <unistd.h>
#include <errno.h>
#include <string.h>
#include <sys/poll.h>
#include <sys/stat.h>

#include "mono/io-layer/wapi.h"
#include "unicode.h"
#include "wapi-private.h"

#define DEBUG

/* Currently used for both FILE and CONSOLE handle types.  This may
 * have to change in future.
 */
struct _WapiHandle_file
{
	WapiHandle handle;
	int fd;
	WapiSecurityAttributes *security_attributes;
	guint32 fileaccess;
	guint32 sharemode;
	guint32 attrs;
};

static void file_close(WapiHandle *handle);
static WapiFileType file_getfiletype(void);
static gboolean file_read(WapiHandle *handle, gpointer buffer,
			  guint32 numbytes, guint32 *bytesread,
			  WapiOverlapped *overlapped);
static gboolean file_write(WapiHandle *handle, gconstpointer buffer,
			   guint32 numbytes, guint32 *byteswritten,
			   WapiOverlapped *overlapped);
static guint32 file_seek(WapiHandle *handle, gint32 movedistance,
			 gint32 *highmovedistance, WapiSeekMethod method);
static gboolean file_setendoffile(WapiHandle *handle);
static guint32 file_getfilesize(WapiHandle *handle, guint32 *highsize);

/* File handle is only signalled for overlapped IO */
static struct _WapiHandleOps file_ops = {
	file_close,		/* close */
	file_getfiletype,	/* getfiletype */
	file_read,		/* readfile */
	file_write,		/* writefile */
	file_seek,		/* seek */
	file_setendoffile,	/* setendoffile */
	file_getfilesize,	/* getfilesize */
	NULL,			/* wait */
	NULL,			/* wait_multiple */
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
	NULL,			/* seek */
	NULL,			/* setendoffile */
	NULL,			/* getfilesize */
	NULL,			/* FIXME: wait */
	NULL,			/* FIXME: wait_multiple */
};

static void file_close(WapiHandle *handle)
{
	struct _WapiHandle_file *file_handle=(struct _WapiHandle_file *)handle;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": closing file handle %p with fd %d",
		  file_handle, file_handle->fd);
#endif
	
	close(file_handle->fd);
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
	} else {
		offset=*highmovedistance;
		offset<<=32;
		offset+=movedistance;
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
WapiHandle *CreateFile(const guchar *name, guint32 fileaccess,
		       guint32 sharemode, WapiSecurityAttributes *security,
		       guint32 createmode, guint32 attrs,
		       WapiHandle *template G_GNUC_UNUSED)
{
	struct _WapiHandle_file *file_handle;
	WapiHandle *handle;
	int flags=convert_flags(fileaccess, createmode);
	mode_t perms=convert_perms(sharemode);
	guchar *filename;
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
	
	g_free(filename);
	
	if(ret==-1) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Error opening file: %s",
			  strerror(errno));
#endif
		return(INVALID_HANDLE_VALUE);
	}

	file_handle=g_new0(struct _WapiHandle_file, 1);
	handle=(WapiHandle *)file_handle;
	
	_WAPI_HANDLE_INIT(handle, WAPI_HANDLE_FILE, file_ops);

	file_handle->fd=ret;
	file_handle->security_attributes=security;
	file_handle->fileaccess=fileaccess;
	file_handle->sharemode=sharemode;
	file_handle->attrs=attrs;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": returning handle %p with fd %d",
		  handle, file_handle->fd);
#endif

	return(handle);
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
