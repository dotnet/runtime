/*
 * io.c:  File, console and find handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright (c) 2002-2006 Novell, Inc.
 */

#include <config.h>
#include <glib.h>
#include <fcntl.h>
#include <unistd.h>
#include <errno.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <dirent.h>
#include <fnmatch.h>
#include <stdio.h>
#include <utime.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/io-private.h>
#include <mono/io-layer/timefuncs-private.h>
#include <mono/io-layer/thread-private.h>
#include <mono/io-layer/io-portability.h>
#include <mono/utils/strenc.h>

#undef DEBUG

static gboolean _wapi_lock_file_region (int fd, off_t offset, off_t length);
static gboolean _wapi_unlock_file_region (int fd, off_t offset, off_t length);

static void file_close (gpointer handle, gpointer data);
static WapiFileType file_getfiletype(void);
static gboolean file_read(gpointer handle, gpointer buffer,
			  guint32 numbytes, guint32 *bytesread,
			  WapiOverlapped *overlapped);
static gboolean file_write(gpointer handle, gconstpointer buffer,
			   guint32 numbytes, guint32 *byteswritten,
			   WapiOverlapped *overlapped);
static gboolean file_flush(gpointer handle);
static guint32 file_seek(gpointer handle, gint32 movedistance,
			 gint32 *highmovedistance, WapiSeekMethod method);
static gboolean file_setendoffile(gpointer handle);
static guint32 file_getfilesize(gpointer handle, guint32 *highsize);
static gboolean file_getfiletime(gpointer handle, WapiFileTime *create_time,
				 WapiFileTime *last_access,
				 WapiFileTime *last_write);
static gboolean file_setfiletime(gpointer handle,
				 const WapiFileTime *create_time,
				 const WapiFileTime *last_access,
				 const WapiFileTime *last_write);

/* File handle is only signalled for overlapped IO */
struct _WapiHandleOps _wapi_file_ops = {
	file_close,		/* close */
	NULL,			/* signal */
	NULL,			/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL			/* prewait */
};

void _wapi_file_details (gpointer handle_info)
{
	struct _WapiHandle_file *file = (struct _WapiHandle_file *)handle_info;
	
	g_print ("[%20s] acc: %c%c%c, shr: %c%c%c, attrs: %5u",
		 file->filename,
		 file->fileaccess&GENERIC_READ?'R':'.',
		 file->fileaccess&GENERIC_WRITE?'W':'.',
		 file->fileaccess&GENERIC_EXECUTE?'X':'.',
		 file->sharemode&FILE_SHARE_READ?'R':'.',
		 file->sharemode&FILE_SHARE_WRITE?'W':'.',
		 file->sharemode&FILE_SHARE_DELETE?'D':'.',
		 file->attrs);
}

static void console_close (gpointer handle, gpointer data);
static WapiFileType console_getfiletype(void);
static gboolean console_read(gpointer handle, gpointer buffer,
			     guint32 numbytes, guint32 *bytesread,
			     WapiOverlapped *overlapped);
static gboolean console_write(gpointer handle, gconstpointer buffer,
			      guint32 numbytes, guint32 *byteswritten,
			      WapiOverlapped *overlapped);

/* Console is mostly the same as file, except it can block waiting for
 * input or output
 */
struct _WapiHandleOps _wapi_console_ops = {
	console_close,		/* close */
	NULL,			/* signal */
	NULL,			/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL			/* prewait */
};

void _wapi_console_details (gpointer handle_info)
{
	_wapi_file_details (handle_info);
}

/* Find handle has no ops.
 */
struct _WapiHandleOps _wapi_find_ops = {
	NULL,			/* close */
	NULL,			/* signal */
	NULL,			/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL			/* prewait */
};

static void pipe_close (gpointer handle, gpointer data);
static WapiFileType pipe_getfiletype (void);
static gboolean pipe_read (gpointer handle, gpointer buffer, guint32 numbytes,
			   guint32 *bytesread, WapiOverlapped *overlapped);
static gboolean pipe_write (gpointer handle, gconstpointer buffer,
			    guint32 numbytes, guint32 *byteswritten,
			    WapiOverlapped *overlapped);

/* Pipe handles
 */
struct _WapiHandleOps _wapi_pipe_ops = {
	pipe_close,		/* close */
	NULL,			/* signal */
	NULL,			/* own */
	NULL,			/* is_owned */
	NULL,			/* special_wait */
	NULL			/* prewait */
};

void _wapi_pipe_details (gpointer handle_info)
{
	_wapi_file_details (handle_info);
}

static const struct {
	/* File, console and pipe handles */
	WapiFileType (*getfiletype)(void);
	
	/* File, console and pipe handles */
	gboolean (*readfile)(gpointer handle, gpointer buffer,
			     guint32 numbytes, guint32 *bytesread,
			     WapiOverlapped *overlapped);
	gboolean (*writefile)(gpointer handle, gconstpointer buffer,
			      guint32 numbytes, guint32 *byteswritten,
			      WapiOverlapped *overlapped);
	gboolean (*flushfile)(gpointer handle);
	
	/* File handles */
	guint32 (*seek)(gpointer handle, gint32 movedistance,
			gint32 *highmovedistance, WapiSeekMethod method);
	gboolean (*setendoffile)(gpointer handle);
	guint32 (*getfilesize)(gpointer handle, guint32 *highsize);
	gboolean (*getfiletime)(gpointer handle, WapiFileTime *create_time,
				WapiFileTime *last_access,
				WapiFileTime *last_write);
	gboolean (*setfiletime)(gpointer handle,
				const WapiFileTime *create_time,
				const WapiFileTime *last_access,
				const WapiFileTime *last_write);
} io_ops[WAPI_HANDLE_COUNT]={
	{NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL},
	/* file */
	{file_getfiletype,
	 file_read, file_write,
	 file_flush, file_seek,
	 file_setendoffile,
	 file_getfilesize,
	 file_getfiletime,
	 file_setfiletime},
	/* console */
	{console_getfiletype,
	 console_read,
	 console_write,
	 NULL, NULL, NULL, NULL, NULL, NULL},
	/* thread */
	{NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL},
	/* sem */
	{NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL},
	/* mutex */
	{NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL},
	/* event */
	{NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL},
	/* socket (will need at least read and write) */
	{NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL},
	/* find */
	{NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL},
	/* process */
	{NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL},
	/* pipe */
	{pipe_getfiletype,
	 pipe_read,
	 pipe_write,
	 NULL, NULL, NULL, NULL, NULL, NULL},
};

static mono_once_t io_ops_once=MONO_ONCE_INIT;
static gboolean lock_while_writing = FALSE;

static void io_ops_init (void)
{
/* 	_wapi_handle_register_capabilities (WAPI_HANDLE_FILE, */
/* 					    WAPI_HANDLE_CAP_WAIT); */
/* 	_wapi_handle_register_capabilities (WAPI_HANDLE_CONSOLE, */
/* 					    WAPI_HANDLE_CAP_WAIT); */

	if (g_getenv ("MONO_STRICT_IO_EMULATION") != NULL) {
		lock_while_writing = TRUE;
	}
}

/* Some utility functions.
 */

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
		if (!(buf->st_mode & S_IWUSR)) {
			attrs |= FILE_ATTRIBUTE_READONLY;
		}
		if (filename[0] == '.') {
			attrs |= FILE_ATTRIBUTE_HIDDEN;
		}
	} else {
		if (!(buf->st_mode & S_IWUSR)) {
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
	SetLastError (_wapi_get_win32_file_error (errno));
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
			SetLastError (ERROR_FILE_NOT_FOUND);
		} else {
			SetLastError (ERROR_PATH_NOT_FOUND);
		}

		g_free (dirname);
	} else {
		_wapi_set_last_error_from_errno ();
	}
}

/* Handle ops.
 */
static void file_close (gpointer handle, gpointer data)
{
	struct _WapiHandle_file *file_handle = (struct _WapiHandle_file *)data;
	
#ifdef DEBUG
	g_message("%s: closing file handle %p [%s]", __func__, handle,
		  file_handle->filename);
#endif

	if (file_handle->attrs & FILE_FLAG_DELETE_ON_CLOSE)
		_wapi_unlink (file_handle->filename);
	
	g_free (file_handle->filename);
	
	_wapi_handle_share_release (file_handle->share_info);
	
	close (GPOINTER_TO_UINT(handle));
}

static WapiFileType file_getfiletype(void)
{
	return(FILE_TYPE_DISK);
}

static gboolean file_read(gpointer handle, gpointer buffer,
			  guint32 numbytes, guint32 *bytesread,
			  WapiOverlapped *overlapped)
{
	struct _WapiHandle_file *file_handle;
	gboolean ok;
	int fd = GPOINTER_TO_UINT(handle);
	int ret;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_FILE,
				(gpointer *)&file_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up file handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}

	if(bytesread!=NULL) {
		*bytesread=0;
	}
	
	if(!(file_handle->fileaccess & GENERIC_READ) &&
	   !(file_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message("%s: handle %p doesn't have GENERIC_READ access: %u",
			  __func__, handle, file_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}

	do {
		ret = read (fd, buffer, numbytes);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending());
			
	if(ret==-1) {
		gint err = errno;

#ifdef DEBUG
		g_message("%s: read of handle %p error: %s", __func__,
			  handle, strerror(err));
#endif
		SetLastError (_wapi_get_win32_file_error (err));
		return(FALSE);
	}
		
	if (bytesread != NULL) {
		*bytesread = ret;
	}
		
	return(TRUE);
}

static gboolean file_write(gpointer handle, gconstpointer buffer,
			   guint32 numbytes, guint32 *byteswritten,
			   WapiOverlapped *overlapped G_GNUC_UNUSED)
{
	struct _WapiHandle_file *file_handle;
	gboolean ok;
	int ret;
	off_t current_pos = 0;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_FILE,
				(gpointer *)&file_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up file handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	if(byteswritten!=NULL) {
		*byteswritten=0;
	}
	
	if(!(file_handle->fileaccess & GENERIC_WRITE) &&
	   !(file_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message("%s: handle %p doesn't have GENERIC_WRITE access: %u", __func__, handle, file_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
	if (lock_while_writing) {
		/* Need to lock the region we're about to write to,
		 * because we only do advisory locking on POSIX
		 * systems
		 */
		current_pos = lseek (fd, (off_t)0, SEEK_CUR);
		if (current_pos == -1) {
#ifdef DEBUG
			g_message ("%s: handle %p lseek failed: %s", __func__,
				   handle, strerror (errno));
#endif
			_wapi_set_last_error_from_errno ();
			return(FALSE);
		}
		
		if (_wapi_lock_file_region (fd, current_pos,
					    numbytes) == FALSE) {
			/* The error has already been set */
			return(FALSE);
		}
	}
		
	do {
		ret = write (fd, buffer, numbytes);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending());
	
	if (lock_while_writing) {
		_wapi_unlock_file_region (fd, current_pos, numbytes);
	}

	if (ret == -1) {
		if (errno == EINTR) {
			ret = 0;
		} else {
			_wapi_set_last_error_from_errno ();
				
#ifdef DEBUG
			g_message("%s: write of handle %p error: %s",
				  __func__, handle, strerror(errno));
#endif

			return(FALSE);
		}
	}
	if (byteswritten != NULL) {
		*byteswritten = ret;
	}
	return(TRUE);
}

static gboolean file_flush(gpointer handle)
{
	struct _WapiHandle_file *file_handle;
	gboolean ok;
	int ret;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_FILE,
				(gpointer *)&file_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up file handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}

	if(!(file_handle->fileaccess & GENERIC_WRITE) &&
	   !(file_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message("%s: handle %p doesn't have GENERIC_WRITE access: %u", __func__, handle, file_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}

	ret=fsync(fd);
	if (ret==-1) {
#ifdef DEBUG
		g_message("%s: fsync of handle %p error: %s", __func__, handle,
			  strerror(errno));
#endif

		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}
	
	return(TRUE);
}

static guint32 file_seek(gpointer handle, gint32 movedistance,
			 gint32 *highmovedistance, WapiSeekMethod method)
{
	struct _WapiHandle_file *file_handle;
	gboolean ok;
	off_t offset, newpos;
	int whence;
	guint32 ret;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_FILE,
				(gpointer *)&file_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up file handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(INVALID_SET_FILE_POINTER);
	}
	
	if(!(file_handle->fileaccess & GENERIC_READ) &&
	   !(file_handle->fileaccess & GENERIC_WRITE) &&
	   !(file_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message ("%s: handle %p doesn't have GENERIC_READ or GENERIC_WRITE access: %u", __func__, handle, file_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
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
		g_message("%s: invalid seek type %d", __func__, method);
#endif

		SetLastError (ERROR_INVALID_PARAMETER);
		return(INVALID_SET_FILE_POINTER);
	}

#ifdef HAVE_LARGE_FILE_SUPPORT
	if(highmovedistance==NULL) {
		offset=movedistance;
#ifdef DEBUG
		g_message("%s: setting offset to %lld (low %d)", __func__,
			  offset, movedistance);
#endif
	} else {
		offset=((gint64) *highmovedistance << 32) | (guint32)movedistance;
		
#ifdef DEBUG
		g_message("%s: setting offset to %lld 0x%llx (high %d 0x%x, low %d 0x%x)", __func__, offset, offset, *highmovedistance, *highmovedistance, movedistance, movedistance);
#endif
	}
#else
	offset=movedistance;
#endif

#ifdef DEBUG
#ifdef HAVE_LARGE_FILE_SUPPORT
	g_message("%s: moving handle %p by %lld bytes from %d", __func__,
		  handle, offset, whence);
#else
	g_message("%s: moving handle %p fd %d by %ld bytes from %d", __func__,
		  handle, offset, whence);
#endif
#endif

	newpos=lseek(fd, offset, whence);
	if(newpos==-1) {
#ifdef DEBUG
		g_message("%s: lseek on handle %p returned error %s",
			  __func__, handle, strerror(errno));
#endif

		_wapi_set_last_error_from_errno ();
		return(INVALID_SET_FILE_POINTER);
	}

#ifdef DEBUG
#ifdef HAVE_LARGE_FILE_SUPPORT
	g_message("%s: lseek returns %lld", __func__, newpos);
#else
	g_message ("%s: lseek returns %ld", __func__, newpos);
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
	g_message ("%s: move of handle %p returning %d/%d", __func__,
		   handle, ret, highmovedistance==NULL?0:*highmovedistance);
#endif

	return(ret);
}

static gboolean file_setendoffile(gpointer handle)
{
	struct _WapiHandle_file *file_handle;
	gboolean ok;
	struct stat statbuf;
	off_t size, pos;
	int ret;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_FILE,
				(gpointer *)&file_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up file handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	if(!(file_handle->fileaccess & GENERIC_WRITE) &&
	   !(file_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message("%s: handle %p doesn't have GENERIC_WRITE access: %u", __func__, handle, file_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}

	/* Find the current file position, and the file length.  If
	 * the file position is greater than the length, write to
	 * extend the file with a hole.  If the file position is less
	 * than the length, truncate the file.
	 */
	
	ret=fstat(fd, &statbuf);
	if(ret==-1) {
#ifdef DEBUG
		g_message ("%s: handle %p fstat failed: %s", __func__,
			   handle, strerror(errno));
#endif

		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}
	size=statbuf.st_size;

	pos=lseek(fd, (off_t)0, SEEK_CUR);
	if(pos==-1) {
#ifdef DEBUG
		g_message("%s: handle %p lseek failed: %s", __func__,
			  handle, strerror(errno));
#endif

		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}
	
#ifdef FTRUNCATE_DOESNT_EXTEND
	/* I haven't bothered to write the configure.in stuff for this
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
			ret = write (fd, "", 1);
		} while (ret == -1 && errno == EINTR &&
			 !_wapi_thread_cur_apc_pending());

		if(ret==-1) {
#ifdef DEBUG
			g_message("%s: handle %p extend write failed: %s", __func__, handle, strerror(errno));
#endif

			_wapi_set_last_error_from_errno ();
			return(FALSE);
		}

		/* And put the file position back after the write */
		ret = lseek (fd, pos, SEEK_SET);
		if (ret == -1) {
#ifdef DEBUG
			g_message ("%s: handle %p second lseek failed: %s",
				   __func__, handle, strerror(errno));
#endif

			_wapi_set_last_error_from_errno ();
			return(FALSE);
		}
	}
#endif

	/* always truncate, because the extend write() adds an extra
	 * byte to the end of the file
	 */
	do {
		ret=ftruncate(fd, pos);
	}
	while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending()); 
	if(ret==-1) {
#ifdef DEBUG
		g_message("%s: handle %p ftruncate failed: %s", __func__,
			  handle, strerror(errno));
#endif
		
		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}
		
	return(TRUE);
}

static guint32 file_getfilesize(gpointer handle, guint32 *highsize)
{
	struct _WapiHandle_file *file_handle;
	gboolean ok;
	struct stat statbuf;
	guint32 size;
	int ret;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_FILE,
				(gpointer *)&file_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up file handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(INVALID_FILE_SIZE);
	}
	
	if(!(file_handle->fileaccess & GENERIC_READ) &&
	   !(file_handle->fileaccess & GENERIC_WRITE) &&
	   !(file_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message("%s: handle %p doesn't have GENERIC_READ or GENERIC_WRITE access: %u", __func__, handle, file_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
		return(INVALID_FILE_SIZE);
	}

	/* If the file has a size with the low bits 0xFFFFFFFF the
	 * caller can't tell if this is an error, so clear the error
	 * value
	 */
	SetLastError (ERROR_SUCCESS);
	
	ret = fstat(fd, &statbuf);
	if (ret == -1) {
#ifdef DEBUG
		g_message ("%s: handle %p fstat failed: %s", __func__,
			   handle, strerror(errno));
#endif

		_wapi_set_last_error_from_errno ();
		return(INVALID_FILE_SIZE);
	}
	
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

#ifdef DEBUG
	g_message ("%s: Returning size %d/%d", __func__, size, *highsize);
#endif
	
	return(size);
}

static gboolean file_getfiletime(gpointer handle, WapiFileTime *create_time,
				 WapiFileTime *last_access,
				 WapiFileTime *last_write)
{
	struct _WapiHandle_file *file_handle;
	gboolean ok;
	struct stat statbuf;
	guint64 create_ticks, access_ticks, write_ticks;
	int ret;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_FILE,
				(gpointer *)&file_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up file handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}

	if(!(file_handle->fileaccess & GENERIC_READ) &&
	   !(file_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message("%s: handle %p doesn't have GENERIC_READ access: %u",
			  __func__, handle, file_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
	ret=fstat(fd, &statbuf);
	if(ret==-1) {
#ifdef DEBUG
		g_message("%s: handle %p fstat failed: %s", __func__, handle,
			  strerror(errno));
#endif

		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}

#ifdef DEBUG
	g_message("%s: atime: %ld ctime: %ld mtime: %ld", __func__,
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
			+ 116444736000000000ULL;
	} else {
		create_ticks=((guint64)statbuf.st_ctime*10000000)
			+ 116444736000000000ULL;
	}
	
	access_ticks=((guint64)statbuf.st_atime*10000000)+116444736000000000ULL;
	write_ticks=((guint64)statbuf.st_mtime*10000000)+116444736000000000ULL;
	
#ifdef DEBUG
	g_message("%s: aticks: %llu cticks: %llu wticks: %llu", __func__,
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

static gboolean file_setfiletime(gpointer handle,
				 const WapiFileTime *create_time G_GNUC_UNUSED,
				 const WapiFileTime *last_access,
				 const WapiFileTime *last_write)
{
	struct _WapiHandle_file *file_handle;
	gboolean ok;
	struct utimbuf utbuf;
	struct stat statbuf;
	guint64 access_ticks, write_ticks;
	int ret;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_FILE,
				(gpointer *)&file_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up file handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	if(!(file_handle->fileaccess & GENERIC_WRITE) &&
	   !(file_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message("%s: handle %p doesn't have GENERIC_WRITE access: %u", __func__, handle, file_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}

	if(file_handle->filename == NULL) {
#ifdef DEBUG
		g_message("%s: handle %p unknown filename", __func__, handle);
#endif

		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	/* Get the current times, so we can put the same times back in
	 * the event that one of the FileTime structs is NULL
	 */
	ret=fstat (fd, &statbuf);
	if(ret==-1) {
#ifdef DEBUG
		g_message("%s: handle %p fstat failed: %s", __func__, handle,
			  strerror(errno));
#endif

		SetLastError (ERROR_INVALID_PARAMETER);
		return(FALSE);
	}

	if(last_access!=NULL) {
		access_ticks=((guint64)last_access->dwHighDateTime << 32) +
			last_access->dwLowDateTime;
		/* This is (time_t)0.  We can actually go to INT_MIN,
		 * but this will do for now.
		 */
		if (access_ticks < 116444736000000000ULL) {
#ifdef DEBUG
			g_message ("%s: attempt to set access time too early",
				   __func__);
#endif
			SetLastError (ERROR_INVALID_PARAMETER);
			return(FALSE);
		}
		
		utbuf.actime=(access_ticks - 116444736000000000ULL) / 10000000;
	} else {
		utbuf.actime=statbuf.st_atime;
	}

	if(last_write!=NULL) {
		write_ticks=((guint64)last_write->dwHighDateTime << 32) +
			last_write->dwLowDateTime;
		/* This is (time_t)0.  We can actually go to INT_MIN,
		 * but this will do for now.
		 */
		if (write_ticks < 116444736000000000ULL) {
#ifdef DEBUG
			g_message ("%s: attempt to set write time too early",
				   __func__);
#endif
			SetLastError (ERROR_INVALID_PARAMETER);
			return(FALSE);
		}
		
		utbuf.modtime=(write_ticks - 116444736000000000ULL) / 10000000;
	} else {
		utbuf.modtime=statbuf.st_mtime;
	}

#ifdef DEBUG
	g_message ("%s: setting handle %p access %ld write %ld", __func__,
		   handle, utbuf.actime, utbuf.modtime);
#endif

	ret = _wapi_utime (file_handle->filename, &utbuf);
	if (ret == -1) {
#ifdef DEBUG
		g_message ("%s: handle %p [%s] utime failed: %s", __func__,
			   handle, file_handle->filename, strerror(errno));

#endif
		SetLastError (ERROR_INVALID_PARAMETER);
		return(FALSE);
	}
	
	return(TRUE);
}

static void console_close (gpointer handle, gpointer data)
{
	struct _WapiHandle_file *console_handle = (struct _WapiHandle_file *)data;
	
#ifdef DEBUG
	g_message("%s: closing console handle %p", __func__, handle);
#endif

	g_free (console_handle->filename);
	
	close (GPOINTER_TO_UINT(handle));
}

static WapiFileType console_getfiletype(void)
{
	return(FILE_TYPE_CHAR);
}

static gboolean console_read(gpointer handle, gpointer buffer,
			     guint32 numbytes, guint32 *bytesread,
			     WapiOverlapped *overlapped G_GNUC_UNUSED)
{
	struct _WapiHandle_file *console_handle;
	gboolean ok;
	int ret;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_CONSOLE,
				(gpointer *)&console_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up console handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	if(bytesread!=NULL) {
		*bytesread=0;
	}
	
	if(!(console_handle->fileaccess & GENERIC_READ) &&
	   !(console_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message ("%s: handle %p doesn't have GENERIC_READ access: %u",
			   __func__, handle, console_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
	do {
		ret=read(fd, buffer, numbytes);
	} while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());

	if(ret==-1) {
#ifdef DEBUG
		g_message("%s: read of handle %p error: %s", __func__, handle,
			  strerror(errno));
#endif

		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}
	
	if(bytesread!=NULL) {
		*bytesread=ret;
	}
	
	return(TRUE);
}

static gboolean console_write(gpointer handle, gconstpointer buffer,
			      guint32 numbytes, guint32 *byteswritten,
			      WapiOverlapped *overlapped G_GNUC_UNUSED)
{
	struct _WapiHandle_file *console_handle;
	gboolean ok;
	int ret;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_CONSOLE,
				(gpointer *)&console_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up console handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	if(byteswritten!=NULL) {
		*byteswritten=0;
	}
	
	if(!(console_handle->fileaccess & GENERIC_WRITE) &&
	   !(console_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message("%s: handle %p doesn't have GENERIC_WRITE access: %u", __func__, handle, console_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
	do {
		ret = write(fd, buffer, numbytes);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending());

	if (ret == -1) {
		if (errno == EINTR) {
			ret = 0;
		} else {
			_wapi_set_last_error_from_errno ();
			
#ifdef DEBUG
			g_message ("%s: write of handle %p error: %s",
				   __func__, handle, strerror(errno));
#endif

			return(FALSE);
		}
	}
	if(byteswritten!=NULL) {
		*byteswritten=ret;
	}
	
	return(TRUE);
}

static void pipe_close (gpointer handle, gpointer data G_GNUC_UNUSED)
{
#ifdef DEBUG
	g_message("%s: closing pipe handle %p", __func__, handle);
#endif

	/* No filename with pipe handles */

	close(GPOINTER_TO_UINT(handle));
}

static WapiFileType pipe_getfiletype(void)
{
	return(FILE_TYPE_PIPE);
}

static gboolean pipe_read (gpointer handle, gpointer buffer,
			   guint32 numbytes, guint32 *bytesread,
			   WapiOverlapped *overlapped G_GNUC_UNUSED)
{
	struct _WapiHandle_file *pipe_handle;
	gboolean ok;
	int ret;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_PIPE,
				(gpointer *)&pipe_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up pipe handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}

	if(bytesread!=NULL) {
		*bytesread=0;
	}
	
	if(!(pipe_handle->fileaccess & GENERIC_READ) &&
	   !(pipe_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message("%s: handle %p doesn't have GENERIC_READ access: %u",
			  __func__, handle, pipe_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
#ifdef DEBUG
	g_message ("%s: reading up to %d bytes from pipe %p", __func__,
		   numbytes, handle);
#endif

	do {
		ret=read(fd, buffer, numbytes);
	} while (ret==-1 && errno==EINTR && !_wapi_thread_cur_apc_pending());
		
	if (ret == -1) {
		if (errno == EINTR) {
			ret = 0;
		} else {
			_wapi_set_last_error_from_errno ();
			
#ifdef DEBUG
			g_message("%s: read of handle %p error: %s", __func__,
				  handle, strerror(errno));
#endif

			return(FALSE);
		}
	}
	
#ifdef DEBUG
	g_message ("%s: read %d bytes from pipe", __func__, ret);
#endif

	if(bytesread!=NULL) {
		*bytesread=ret;
	}
	
	return(TRUE);
}

static gboolean pipe_write(gpointer handle, gconstpointer buffer,
			   guint32 numbytes, guint32 *byteswritten,
			   WapiOverlapped *overlapped G_GNUC_UNUSED)
{
	struct _WapiHandle_file *pipe_handle;
	gboolean ok;
	int ret;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_PIPE,
				(gpointer *)&pipe_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up pipe handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	if(byteswritten!=NULL) {
		*byteswritten=0;
	}
	
	if(!(pipe_handle->fileaccess & GENERIC_WRITE) &&
	   !(pipe_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message("%s: handle %p doesn't have GENERIC_WRITE access: %u", __func__, handle, pipe_handle->fileaccess);
#endif

		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}
	
#ifdef DEBUG
	g_message ("%s: writing up to %d bytes to pipe %p", __func__, numbytes,
		   handle);
#endif

	do {
		ret = write (fd, buffer, numbytes);
	} while (ret == -1 && errno == EINTR &&
		 !_wapi_thread_cur_apc_pending());

	if (ret == -1) {
		if (errno == EINTR) {
			ret = 0;
		} else {
			_wapi_set_last_error_from_errno ();
			
#ifdef DEBUG
			g_message("%s: write of handle %p error: %s", __func__,
				  handle, strerror(errno));
#endif

			return(FALSE);
		}
	}
	if(byteswritten!=NULL) {
		*byteswritten=ret;
	}
	
	return(TRUE);
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
		g_message("%s: Unknown access type 0x%x", __func__,
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
		g_message("%s: Unknown create mode 0x%x", __func__,
			  createmode);
#endif
		break;
	}
	
	return(flags);
}

static guint32 convert_from_flags(int flags)
{
	guint32 fileaccess=0;
	
#ifndef O_ACCMODE
#define O_ACCMODE (O_RDONLY|O_WRONLY|O_RDWR)
#endif

	if((flags & O_ACCMODE) == O_RDONLY) {
		fileaccess=GENERIC_READ;
	} else if ((flags & O_ACCMODE) == O_WRONLY) {
		fileaccess=GENERIC_WRITE;
	} else if ((flags & O_ACCMODE) == O_RDWR) {
		fileaccess=GENERIC_READ|GENERIC_WRITE;
	} else {
#ifdef DEBUG
		g_message("%s: Can't figure out flags 0x%x", __func__, flags);
#endif
	}

	/* Maybe sort out create mode too */

	return(fileaccess);
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
				   struct _WapiFileShare **share_info)
{
	gboolean file_already_shared;
	guint32 file_existing_share, file_existing_access;

	file_already_shared = _wapi_handle_get_or_set_share (statbuf->st_dev, statbuf->st_ino, sharemode, fileaccess, &file_existing_share, &file_existing_access, share_info);
	
	if (file_already_shared) {
		/* The reference to this share info was incremented
		 * when we looked it up, so be careful to put it back
		 * if we conclude we can't use this file.
		 */
		if (file_existing_share == 0) {
			/* Quick and easy, no possibility to share */
#ifdef DEBUG
			g_message ("%s: Share mode prevents open: requested access: 0x%x, file has sharing = NONE", __func__, fileaccess);
#endif

			_wapi_handle_share_release (*share_info);
			
			return(FALSE);
		}

		if (((file_existing_share == FILE_SHARE_READ) &&
		     (fileaccess != GENERIC_READ)) ||
		    ((file_existing_share == FILE_SHARE_WRITE) &&
		     (fileaccess != GENERIC_WRITE))) {
			/* New access mode doesn't match up */
#ifdef DEBUG
			g_message ("%s: Share mode prevents open: requested access: 0x%x, file has sharing: 0x%x", __func__, fileaccess, file_existing_share);
#endif

			_wapi_handle_share_release (*share_info);
		
			return(FALSE);
		}

		if (((file_existing_access & GENERIC_READ) &&
		     !(sharemode & FILE_SHARE_READ)) ||
		    ((file_existing_access & GENERIC_WRITE) &&
		     !(sharemode & FILE_SHARE_WRITE))) {
			/* New share mode doesn't match up */
#ifdef DEBUG
			g_message ("%s: Access mode prevents open: requested share: 0x%x, file has access: 0x%x", __func__, sharemode, file_existing_access);
#endif

			_wapi_handle_share_release (*share_info);
		
			return(FALSE);
		}
	} else {
#ifdef DEBUG
		g_message ("%s: New file!", __func__);
#endif
	}

	return(TRUE);
}

static gboolean share_check (struct stat *statbuf, guint32 sharemode,
			     guint32 fileaccess,
			     struct _WapiFileShare **share_info, int fd)
{
	if (share_allows_open (statbuf, sharemode, fileaccess,
			       share_info) == TRUE) {
		return (TRUE);
	}
	
	/* Got a share violation.  Double check that the file is still
	 * open by someone, in case a process crashed while still
	 * holding a file handle.  This will also cope with someone
	 * using Mono.Posix to close the file.  This is cheaper and
	 * less intrusive to other processes than initiating a handle
	 * collection.
	 */

	_wapi_handle_check_share (*share_info, fd);
	if (share_allows_open (statbuf, sharemode, fileaccess,
			       share_info) == TRUE) {
		return (TRUE);
	}
	
	/* Still violating.  It's possible that a process crashed
	 * while still holding a file handle, and that a non-mono
	 * process has the file open.  (For example, C-c mcs while
	 * editing a source file.)  As a last resort, run a handle
	 * collection, which will remove stale share entries.
	 */
	_wapi_handle_collect ();

	return(share_allows_open (statbuf, sharemode, fileaccess, share_info));
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
gpointer CreateFile(const gunichar2 *name, guint32 fileaccess,
		    guint32 sharemode, WapiSecurityAttributes *security,
		    guint32 createmode, guint32 attrs,
		    gpointer template G_GNUC_UNUSED)
{
	struct _WapiHandle_file file_handle = {0};
	gpointer handle;
	int flags=convert_flags(fileaccess, createmode);
	/*mode_t perms=convert_perms(sharemode);*/
	/* we don't use sharemode, because that relates to sharing of
	 * the file when the file is open and is already handled by
	 * other code, perms instead are the on-disk permissions and
	 * this is a sane default.
	 */
	mode_t perms=0666;
	gchar *filename;
	int fd, ret;
	int handle_type;
	struct stat statbuf;
	
	mono_once (&io_ops_once, io_ops_init);

	if (attrs & FILE_ATTRIBUTE_TEMPORARY)
		perms = 0600;
	
	if (attrs & FILE_ATTRIBUTE_ENCRYPTED){
		SetLastError (ERROR_ENCRYPTION_FAILED);
		return INVALID_HANDLE_VALUE;
	}
	
	if (name == NULL) {
#ifdef DEBUG
		g_message ("%s: name is NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(INVALID_HANDLE_VALUE);
	}

	filename = mono_unicode_to_external (name);
	if (filename == NULL) {
#ifdef DEBUG
		g_message("%s: unicode conversion returned NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(INVALID_HANDLE_VALUE);
	}
	
#ifdef DEBUG
	g_message ("%s: Opening %s with share 0x%x and access 0x%x", __func__,
		   filename, sharemode, fileaccess);
#endif
	
	fd = _wapi_open (filename, flags, perms);
    
	/* If we were trying to open a directory with write permissions
	 * (e.g. O_WRONLY or O_RDWR), this call will fail with
	 * EISDIR. However, this is a bit bogus because calls to
	 * manipulate the directory (e.g. SetFileTime) will still work on
	 * the directory because they use other API calls
	 * (e.g. utime()). Hence, if we failed with the EISDIR error, try
	 * to open the directory again without write permission.
	 */
	if (fd == -1 && errno == EISDIR)
	{
		/* Try again but don't try to make it writable */
		fd = open(filename, flags  & ~(O_RDWR|O_WRONLY), perms);
	}
	
	if (fd == -1) {
#ifdef DEBUG
		g_message("%s: Error opening file %s: %s", __func__, filename,
			  strerror(errno));
#endif
		_wapi_set_last_path_error_from_errno (NULL, filename);
		g_free (filename);

		return(INVALID_HANDLE_VALUE);
	}

	if (fd >= _wapi_fd_reserve) {
#ifdef DEBUG
		g_message ("%s: File descriptor is too big", __func__);
#endif

		SetLastError (ERROR_TOO_MANY_OPEN_FILES);
		
		close (fd);
		g_free (filename);
		
		return(INVALID_HANDLE_VALUE);
	}

	ret = fstat (fd, &statbuf);
	if (ret == -1) {
#ifdef DEBUG
		g_message ("%s: fstat error of file %s: %s", __func__,
			   filename, strerror (errno));
#endif
		_wapi_set_last_error_from_errno ();
		g_free (filename);
		close (fd);
		
		return(INVALID_HANDLE_VALUE);
	}

	if (share_check (&statbuf, sharemode, fileaccess,
			 &file_handle.share_info, fd) == FALSE) {
		SetLastError (ERROR_SHARING_VIOLATION);
		g_free (filename);
		close (fd);
		
		return (INVALID_HANDLE_VALUE);
	}
	if (file_handle.share_info == NULL) {
		/* No space, so no more files can be opened */
#ifdef DEBUG
		g_message ("%s: No space in the share table", __func__);
#endif

		SetLastError (ERROR_TOO_MANY_OPEN_FILES);
		close (fd);
		g_free (filename);
		
		return(INVALID_HANDLE_VALUE);
	}
	
	file_handle.filename = filename;

	if(security!=NULL) {
		//file_handle->security_attributes=_wapi_handle_scratch_store (
		//security, sizeof(WapiSecurityAttributes));
	}
	
	file_handle.fileaccess=fileaccess;
	file_handle.sharemode=sharemode;
	file_handle.attrs=attrs;

#ifdef HAVE_POSIX_FADVISE
	if (attrs & FILE_FLAG_SEQUENTIAL_SCAN)
		posix_fadvise (fd, 0, 0, POSIX_FADV_SEQUENTIAL);
	if (attrs & FILE_FLAG_RANDOM_ACCESS)
		posix_fadvise (fd, 0, 0, POSIX_FADV_RANDOM);
#endif
	
#ifndef S_ISFIFO
#define S_ISFIFO(m) ((m & S_IFIFO) != 0)
#endif
	if (S_ISFIFO (statbuf.st_mode)) {
		handle_type = WAPI_HANDLE_PIPE;
	} else if (S_ISCHR (statbuf.st_mode)) {
		handle_type = WAPI_HANDLE_CONSOLE;
	} else {
		handle_type = WAPI_HANDLE_FILE;
	}

	handle = _wapi_handle_new_fd (handle_type, fd, &file_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating file handle", __func__);
		g_free (filename);
		close (fd);
		
		SetLastError (ERROR_GEN_FAILURE);
		return(INVALID_HANDLE_VALUE);
	}
	
#ifdef DEBUG
	g_message("%s: returning handle %p", __func__, handle);
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
	int retval;
	gboolean ret = FALSE;
	guint32 attrs;
#if 0
	struct stat statbuf;
	struct _WapiFileShare *shareinfo;
#endif
	
	if(name==NULL) {
#ifdef DEBUG
		g_message("%s: name is NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(FALSE);
	}

	filename=mono_unicode_to_external(name);
	if(filename==NULL) {
#ifdef DEBUG
		g_message("%s: unicode conversion returned NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(FALSE);
	}

	attrs = GetFileAttributes (name);
	if (attrs == INVALID_FILE_ATTRIBUTES) {
#ifdef DEBUG
		g_message ("%s: file attributes error", __func__);
#endif
		/* Error set by GetFileAttributes() */
		g_free (filename);
		return(FALSE);
	}

	if (attrs & FILE_ATTRIBUTE_READONLY) {
#ifdef DEBUG
		g_message ("%s: file %s is readonly", __func__, filename);
#endif
		SetLastError (ERROR_ACCESS_DENIED);
		g_free (filename);
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
		SetLastError (ERROR_SHARING_VIOLATION);
		g_free (filename);
		return FALSE;
	}
	_wapi_handle_share_release (shareinfo);
#endif

	retval = _wapi_unlink (filename);
	
	if (retval == -1) {
		_wapi_set_last_path_error_from_errno (NULL, filename);
	} else {
		ret = TRUE;
	}

	g_free(filename);

	return(ret);
}

/**
 * MoveFile:
 * @name: a pointer to a NULL-terminated unicode string, that names
 * the file to be moved.
 * @dest_name: a pointer to a NULL-terminated unicode string, that is the
 * new name for the file.
 *
 * Renames file @name to @dest_name.
 * MoveFile sets ERROR_ALREADY_EXISTS if the destination exists, except
 * when it is the same file as the source.  In that case it silently succeeds.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean MoveFile (const gunichar2 *name, const gunichar2 *dest_name)
{
	gchar *utf8_name, *utf8_dest_name;
	int result, errno_copy;
	struct stat stat_src, stat_dest;
	gboolean ret = FALSE;
	struct _WapiFileShare *shareinfo;
	
	if(name==NULL) {
#ifdef DEBUG
		g_message("%s: name is NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(FALSE);
	}

	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion returned NULL", __func__);
#endif
		
		SetLastError (ERROR_INVALID_NAME);
		return FALSE;
	}
	
	if(dest_name==NULL) {
#ifdef DEBUG
		g_message("%s: name is NULL", __func__);
#endif

		g_free (utf8_name);
		SetLastError (ERROR_INVALID_NAME);
		return(FALSE);
	}

	utf8_dest_name = mono_unicode_to_external (dest_name);
	if (utf8_dest_name == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion returned NULL", __func__);
#endif

		g_free (utf8_name);
		SetLastError (ERROR_INVALID_NAME);
		return FALSE;
	}

	/*
	 * In C# land we check for the existence of src, but not for dest.
	 * We check it here and return the failure if dest exists and is not
	 * the same file as src.
	 */
	if (_wapi_stat (utf8_name, &stat_src) < 0) {
		_wapi_set_last_path_error_from_errno (NULL, utf8_name);
		g_free (utf8_name);
		g_free (utf8_dest_name);
		return FALSE;
	}
	
	if (!_wapi_stat (utf8_dest_name, &stat_dest)) {
		if (stat_dest.st_dev != stat_src.st_dev ||
		    stat_dest.st_ino != stat_src.st_ino) {
			g_free (utf8_name);
			g_free (utf8_dest_name);
			SetLastError (ERROR_ALREADY_EXISTS);
			return FALSE;
		}
	}

	/* Check to make sure sharing allows us to open the file for
	 * writing.  See bug 377049.
	 *
	 * Do the checks that don't need an open file descriptor, for
	 * simplicity's sake.  If we really have to do the full checks
	 * then we can implement that later.
	 */
	if (share_allows_open (&stat_src, 0, GENERIC_WRITE,
			       &shareinfo) == FALSE) {
		SetLastError (ERROR_SHARING_VIOLATION);
		return FALSE;
	}
	_wapi_handle_share_release (shareinfo);

	result = _wapi_rename (utf8_name, utf8_dest_name);
	errno_copy = errno;
	
	if (result == -1) {
		switch(errno_copy) {
		case EEXIST:
			SetLastError (ERROR_ALREADY_EXISTS);
			break;

		case EXDEV:
			/* Ignore here, it is dealt with below */
			break;
			
		default:
			_wapi_set_last_path_error_from_errno (NULL, utf8_name);
		}
	}
	
	g_free (utf8_name);
	g_free (utf8_dest_name);

	if (result != 0 && errno_copy == EXDEV) {
		/* Try a copy to the new location, and delete the source */
		if (CopyFile (name, dest_name, TRUE)==FALSE) {
			/* CopyFile will set the error */
			return(FALSE);
		}
		
		return(DeleteFile (name));
	}

	if (result == 0) {
		ret = TRUE;
	}

	return(ret);
}

static gboolean
write_file (int src_fd, int dest_fd, struct stat *st_src, gboolean report_errors)
{
	int remain, n;
	char *buf, *wbuf;
	int buf_size = st_src->st_blksize;

	buf_size = buf_size < 8192 ? 8192 : (buf_size > 65536 ? 65536 : buf_size);
	buf = (char *) malloc (buf_size);

	for (;;) {
		remain = read (src_fd, buf, buf_size);
		if (remain < 0) {
			if (errno == EINTR && !_wapi_thread_cur_apc_pending ())
				continue;

			if (report_errors)
				_wapi_set_last_error_from_errno ();

			free (buf);
			return FALSE;
		}
		if (remain == 0) {
			break;
		}

		wbuf = buf;
		while (remain > 0) {
			if ((n = write (dest_fd, wbuf, remain)) < 0) {
				if (errno == EINTR && !_wapi_thread_cur_apc_pending ())
					continue;

				if (report_errors)
					_wapi_set_last_error_from_errno ();
#ifdef DEBUG
				g_message ("%s: write failed.", __func__);
#endif
				free (buf);
				return FALSE;
			}

			remain -= n;
			wbuf += n;
		}
	}

	free (buf);
	return TRUE ;
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
gboolean CopyFile (const gunichar2 *name, const gunichar2 *dest_name,
		   gboolean fail_if_exists)
{
	gchar *utf8_src, *utf8_dest;
	int src_fd, dest_fd;
	struct stat st;
	gboolean ret = TRUE;
	
	if(name==NULL) {
#ifdef DEBUG
		g_message("%s: name is NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(FALSE);
	}
	
	utf8_src = mono_unicode_to_external (name);
	if (utf8_src == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion of source returned NULL",
			   __func__);
#endif

		SetLastError (ERROR_INVALID_PARAMETER);
		return(FALSE);
	}
	
	if(dest_name==NULL) {
#ifdef DEBUG
		g_message("%s: name is NULL", __func__);
#endif

		g_free (utf8_src);
		SetLastError (ERROR_INVALID_NAME);
		return(FALSE);
	}
	
	utf8_dest = mono_unicode_to_external (dest_name);
	if (utf8_dest == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion of dest returned NULL",
			   __func__);
#endif

		SetLastError (ERROR_INVALID_PARAMETER);

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

	if (fstat (src_fd, &st) < 0) {
		_wapi_set_last_error_from_errno ();

		g_free (utf8_src);
		g_free (utf8_dest);
		close (src_fd);
		
		return(FALSE);
	}
	
	if (fail_if_exists) {
		dest_fd = open (utf8_dest, O_WRONLY | O_CREAT | O_EXCL,
				st.st_mode);
	} else {
		dest_fd = open (utf8_dest, O_WRONLY | O_TRUNC, st.st_mode);
		if (dest_fd < 0) {
			/* O_TRUNC might cause a fail if the file
			 * doesn't exist
			 */
			dest_fd = open (utf8_dest, O_WRONLY | O_CREAT,
					st.st_mode);
		} else {
			/* Apparently this error is set if we
			 * overwrite the dest file
			 */
			SetLastError (ERROR_ALREADY_EXISTS);
		}
	}
	if (dest_fd < 0) {
		_wapi_set_last_error_from_errno ();

		g_free (utf8_src);
		g_free (utf8_dest);
		close (src_fd);

		return(FALSE);
	}

	if (!write_file (src_fd, dest_fd, &st, TRUE))
		ret = FALSE;

	g_free (utf8_src);
	g_free (utf8_dest);
	close (src_fd);
	close (dest_fd);

	return ret;
}

static gchar*
convert_arg_to_utf8 (const gunichar2 *arg, const gchar *arg_name)
{
	gchar *utf8_ret;

	if (arg == NULL) {
#ifdef DEBUG
		g_message ("%s: %s is NULL", __func__, arg_name);
#endif
		SetLastError (ERROR_INVALID_NAME);
		return NULL;
	}

	utf8_ret = mono_unicode_to_external (arg);
	if (utf8_ret == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion of %s returned NULL",
			   __func__, arg_name);
#endif
		SetLastError (ERROR_INVALID_PARAMETER);
		return NULL;
	}

	return utf8_ret;
}

gboolean
ReplaceFile (const gunichar2 *replacedFileName, const gunichar2 *replacementFileName,
		      const gunichar2 *backupFileName, guint32 replaceFlags, 
		      gpointer exclude, gpointer reserved)
{
	int result, errno_copy, backup_fd = -1,replaced_fd = -1;
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
		errno_copy = errno;
		if (result == -1)
			goto replace_cleanup;
	}

	result = _wapi_rename (utf8_replacementFileName, utf8_replacedFileName);
	errno_copy = errno;
	if (result == -1) {
		_wapi_set_last_path_error_from_errno (NULL, utf8_replacementFileName);
		_wapi_rename (utf8_backupFileName, utf8_replacedFileName);
		if (backup_fd != -1 && !fstat (backup_fd, &stBackup)) {
			replaced_fd = open (utf8_backupFileName, O_WRONLY | O_CREAT | O_TRUNC,
					     stBackup.st_mode);
			if (replaced_fd == -1) {
				replaced_fd = open (utf8_backupFileName, O_WRONLY | O_CREAT,
					     stBackup.st_mode);
			}
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
	if (backup_fd != -1)
		close (backup_fd);
	if (replaced_fd != -1)
		close (replaced_fd);
	return ret;
}

static gpointer stdhandle_create (int fd, const gchar *name)
{
	struct _WapiHandle_file file_handle = {0};
	gpointer handle;
	int flags;
	
#ifdef DEBUG
	g_message("%s: creating standard handle type %s, fd %d", __func__,
		  name, fd);
#endif
	
	/* Check if fd is valid */
	do {
		flags=fcntl(fd, F_GETFL);
	} while (flags == -1 && errno == EINTR);

	if(flags==-1) {
		/* Invalid fd.  Not really much point checking for EBADF
		 * specifically
		 */
#ifdef DEBUG
		g_message("%s: fcntl error on fd %d: %s", __func__, fd,
			  strerror(errno));
#endif

		_wapi_set_last_error_from_errno ();
		return(INVALID_HANDLE_VALUE);
	}

	file_handle.filename = g_strdup(name);
	/* some default security attributes might be needed */
	file_handle.security_attributes=0;
	file_handle.fileaccess=convert_from_flags(flags);

	/* Apparently input handles can't be written to.  (I don't
	 * know if output or error handles can't be read from.)
	 */
	if (fd == 0) {
		file_handle.fileaccess &= ~GENERIC_WRITE;
	}
	
	file_handle.sharemode=0;
	file_handle.attrs=0;

	handle = _wapi_handle_new_fd (WAPI_HANDLE_CONSOLE, fd, &file_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating file handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		return(INVALID_HANDLE_VALUE);
	}
	
#ifdef DEBUG
	g_message("%s: returning handle %p", __func__, handle);
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

static mono_mutex_t stdhandle_mutex = MONO_MUTEX_INITIALIZER;

gpointer GetStdHandle(WapiStdHandle stdhandle)
{
	struct _WapiHandle_file *file_handle;
	gpointer handle;
	int thr_ret, fd;
	const gchar *name;
	gboolean ok;
	
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
#ifdef DEBUG
		g_message("%s: unknown standard handle type", __func__);
#endif

		SetLastError (ERROR_INVALID_PARAMETER);
		return(INVALID_HANDLE_VALUE);
	}

	handle = GINT_TO_POINTER (fd);

	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&stdhandle_mutex);
	thr_ret = mono_mutex_lock (&stdhandle_mutex);
	g_assert (thr_ret == 0);

	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_CONSOLE,
				  (gpointer *)&file_handle);
	if (ok == FALSE) {
		/* Need to create this console handle */
		handle = stdhandle_create (fd, name);
		
		if (handle == INVALID_HANDLE_VALUE) {
			SetLastError (ERROR_NO_MORE_FILES);
			goto done;
		}
	} else {
		/* Add a reference to this handle */
		_wapi_handle_ref (handle);
	}
	
  done:
	thr_ret = mono_mutex_unlock (&stdhandle_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
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
gboolean ReadFile(gpointer handle, gpointer buffer, guint32 numbytes,
		  guint32 *bytesread, WapiOverlapped *overlapped)
{
	WapiHandleType type;

	type = _wapi_handle_type (handle);
	
	if(io_ops[type].readfile==NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	return(io_ops[type].readfile (handle, buffer, numbytes, bytesread,
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
gboolean WriteFile(gpointer handle, gconstpointer buffer, guint32 numbytes,
		   guint32 *byteswritten, WapiOverlapped *overlapped)
{
	WapiHandleType type;

	type = _wapi_handle_type (handle);
	
	if(io_ops[type].writefile==NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	return(io_ops[type].writefile (handle, buffer, numbytes, byteswritten,
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
gboolean FlushFileBuffers(gpointer handle)
{
	WapiHandleType type;

	type = _wapi_handle_type (handle);
	
	if(io_ops[type].flushfile==NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	return(io_ops[type].flushfile (handle));
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
gboolean SetEndOfFile(gpointer handle)
{
	WapiHandleType type;

	type = _wapi_handle_type (handle);
	
	if (io_ops[type].setendoffile == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	return(io_ops[type].setendoffile (handle));
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
guint32 SetFilePointer(gpointer handle, gint32 movedistance,
		       gint32 *highmovedistance, WapiSeekMethod method)
{
	WapiHandleType type;

	type = _wapi_handle_type (handle);
	
	if (io_ops[type].seek == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(INVALID_SET_FILE_POINTER);
	}
	
	return(io_ops[type].seek (handle, movedistance, highmovedistance,
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
WapiFileType GetFileType(gpointer handle)
{
	WapiHandleType type;

	if (!_WAPI_PRIVATE_HAVE_SLOT (handle)) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FILE_TYPE_UNKNOWN);
	}

	type = _wapi_handle_type (handle);
	
	if (io_ops[type].getfiletype == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FILE_TYPE_UNKNOWN);
	}
	
	return(io_ops[type].getfiletype ());
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
guint32 GetFileSize(gpointer handle, guint32 *highsize)
{
	WapiHandleType type;

	type = _wapi_handle_type (handle);
	
	if (io_ops[type].getfilesize == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(INVALID_FILE_SIZE);
	}
	
	return(io_ops[type].getfilesize (handle, highsize));
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
gboolean GetFileTime(gpointer handle, WapiFileTime *create_time,
		     WapiFileTime *last_access, WapiFileTime *last_write)
{
	WapiHandleType type;

	type = _wapi_handle_type (handle);
	
	if (io_ops[type].getfiletime == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	return(io_ops[type].getfiletime (handle, create_time, last_access,
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
gboolean SetFileTime(gpointer handle, const WapiFileTime *create_time,
		     const WapiFileTime *last_access,
		     const WapiFileTime *last_write)
{
	WapiHandleType type;

	type = _wapi_handle_type (handle);
	
	if (io_ops[type].setfiletime == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	return(io_ops[type].setfiletime (handle, create_time, last_access,
					 last_write));
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
		g_message("%s: system_time NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_PARAMETER);
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
		g_message("%s: file_time too big", __func__);
#endif

		SetLastError (ERROR_INVALID_PARAMETER);
		return(FALSE);
	}

	totaldays=(file_ticks / TICKS_PER_DAY);
	rem = file_ticks % TICKS_PER_DAY;
#ifdef DEBUG
	g_message("%s: totaldays: %lld rem: %lld", __func__, totaldays, rem);
#endif

	system_time->wHour=rem/TICKS_PER_HOUR;
	rem %= TICKS_PER_HOUR;
#ifdef DEBUG
	g_message("%s: Hour: %d rem: %lld", __func__, system_time->wHour, rem);
#endif
	
	system_time->wMinute = rem / TICKS_PER_MINUTE;
	rem %= TICKS_PER_MINUTE;
#ifdef DEBUG
	g_message("%s: Minute: %d rem: %lld", __func__, system_time->wMinute,
		  rem);
#endif
	
	system_time->wSecond = rem / TICKS_PER_SECOND;
	rem %= TICKS_PER_SECOND;
#ifdef DEBUG
	g_message("%s: Second: %d rem: %lld", __func__, system_time->wSecond,
		  rem);
#endif
	
	system_time->wMilliseconds = rem / TICKS_PER_MILLISECOND;
#ifdef DEBUG
	g_message("%s: Milliseconds: %d", __func__,
		  system_time->wMilliseconds);
#endif

	/* January 1, 1601 was a Monday, according to Emacs calendar */
	system_time->wDayOfWeek = ((1 + totaldays) % 7) + 1;
#ifdef DEBUG
	g_message("%s: Day of week: %d", __func__, system_time->wDayOfWeek);
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
		g_message("%s: totaldays: %lld yg: %lld y: %lld", __func__,
			  totaldays, yg,
			  y);
		g_message("%s: LEAPS(yg): %lld LEAPS(y): %lld", __func__,
			  LEAPS_THRU_END_OF(yg-1), LEAPS_THRU_END_OF(y-1));
#endif
		
		/* Adjust days and y to match the guessed year. */
		totaldays -= ((yg - y) * 365
			      + LEAPS_THRU_END_OF (yg - 1)
			      - LEAPS_THRU_END_OF (y - 1));
#ifdef DEBUG
		g_message("%s: totaldays: %lld", __func__, totaldays);
#endif
		y = yg;
#ifdef DEBUG
		g_message("%s: y: %lld", __func__, y);
#endif
	}
	
	system_time->wYear = y;
#ifdef DEBUG
	g_message("%s: Year: %d", __func__, system_time->wYear);
#endif

	ip = mon_yday[isleap(y)];
	
	for(y=11; totaldays < ip[y]; --y) {
		continue;
	}
	totaldays-=ip[y];
#ifdef DEBUG
	g_message("%s: totaldays: %lld", __func__, totaldays);
#endif
	
	system_time->wMonth = y + 1;
#ifdef DEBUG
	g_message("%s: Month: %d", __func__, system_time->wMonth);
#endif

	system_time->wDay = totaldays + 1;
#ifdef DEBUG
	g_message("%s: Day: %d", __func__, system_time->wDay);
#endif
	
	return(TRUE);
}

gpointer FindFirstFile (const gunichar2 *pattern, WapiFindData *find_data)
{
	struct _WapiHandle_find find_handle = {0};
	gpointer handle;
	gchar *utf8_pattern = NULL, *dir_part, *entry_part;
	int result;
	
	if (pattern == NULL) {
#ifdef DEBUG
		g_message ("%s: pattern is NULL", __func__);
#endif

		SetLastError (ERROR_PATH_NOT_FOUND);
		return(INVALID_HANDLE_VALUE);
	}

	utf8_pattern = mono_unicode_to_external (pattern);
	if (utf8_pattern == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion returned NULL", __func__);
#endif
		
		SetLastError (ERROR_INVALID_NAME);
		return(INVALID_HANDLE_VALUE);
	}

#ifdef DEBUG
	g_message ("%s: looking for [%s]", __func__, utf8_pattern);
#endif
	
	/* Figure out which bit of the pattern is the directory */
	dir_part = _wapi_dirname (utf8_pattern);
	entry_part = _wapi_basename (utf8_pattern);

#if 0
	/* Don't do this check for now, it breaks if directories
	 * really do have metachars in their names (see bug 58116).
	 * FIXME: Figure out a better solution to keep some checks...
	 */
	if (strchr (dir_part, '*') || strchr (dir_part, '?')) {
		SetLastError (ERROR_INVALID_NAME);
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

	find_handle.namelist = NULL;
	result = _wapi_io_scandir (dir_part, entry_part,
				   &find_handle.namelist);
	
	if (result == 0) {
		/* No files, which windows seems to call
		 * FILE_NOT_FOUND
		 */
		SetLastError (ERROR_FILE_NOT_FOUND);
		g_free (utf8_pattern);
		g_free (entry_part);
		g_free (dir_part);
		return (INVALID_HANDLE_VALUE);
	}
	
	if (result < 0) {
#ifdef DEBUG
		gint errnum = errno;
#endif
		_wapi_set_last_path_error_from_errno (dir_part, NULL);
#ifdef DEBUG
		g_message ("%s: scandir error: %s", __func__,
			   g_strerror (errnum));
#endif
		g_free (utf8_pattern);
		g_free (entry_part);
		g_free (dir_part);
		return (INVALID_HANDLE_VALUE);
	}

	g_free (utf8_pattern);
	g_free (entry_part);
	
#ifdef DEBUG
	g_message ("%s: Got %d matches", __func__, result);
#endif

	find_handle.dir_part = dir_part;
	find_handle.num = result;
	find_handle.count = 0;
	
	handle = _wapi_handle_new (WAPI_HANDLE_FIND, &find_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating find handle", __func__);
		g_free (dir_part);
		g_free (entry_part);
		g_free (utf8_pattern);
		SetLastError (ERROR_GEN_FAILURE);
		
		return(INVALID_HANDLE_VALUE);
	}

	if (handle != INVALID_HANDLE_VALUE &&
	    !FindNextFile (handle, find_data)) {
		FindClose (handle);
		SetLastError (ERROR_NO_MORE_FILES);
		handle = INVALID_HANDLE_VALUE;
	}

	return (handle);
}

gboolean FindNextFile (gpointer handle, WapiFindData *find_data)
{
	struct _WapiHandle_find *find_handle;
	gboolean ok;
	struct stat buf, linkbuf;
	int result;
	gchar *filename;
	gchar *utf8_filename, *utf8_basename;
	gunichar2 *utf16_basename;
	time_t create_time;
	glong bytes;
	int thr_ret;
	gboolean ret = FALSE;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_FIND,
				(gpointer *)&find_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up find handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
retry:
	if (find_handle->count >= find_handle->num) {
		SetLastError (ERROR_NO_MORE_FILES);
		goto cleanup;
	}

	/* stat next match */

	filename = g_build_filename (find_handle->dir_part, find_handle->namelist[find_handle->count ++], NULL);

	result = _wapi_stat (filename, &buf);
	if (result == -1 && errno == ENOENT) {
		/* Might be a dangling symlink */
		result = _wapi_lstat (filename, &buf);
	}
	
	if (result != 0) {
#ifdef DEBUG
		g_message ("%s: stat failed: %s", __func__, filename);
#endif

		g_free (filename);
		goto retry;
	}

	result = _wapi_lstat (filename, &linkbuf);
	if (result != 0) {
#ifdef DEBUG
		g_message ("%s: lstat failed: %s", __func__, filename);
#endif

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
	
#ifdef DEBUG
	g_message ("%s: Found [%s]", __func__, utf8_filename);
#endif
	
	/* fill data block */

	if (buf.st_mtime < buf.st_ctime)
		create_time = buf.st_mtime;
	else
		create_time = buf.st_ctime;
	
	find_data->dwFileAttributes = _wapi_stat_to_file_attributes (utf8_filename, &buf, &linkbuf);

	_wapi_time_t_to_filetime (create_time, &find_data->ftCreationTime);
	_wapi_time_t_to_filetime (buf.st_atime, &find_data->ftLastAccessTime);
	_wapi_time_t_to_filetime (buf.st_mtime, &find_data->ftLastWriteTime);

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
	 * char incomplete
	 */
	memcpy (find_data->cFileName, utf16_basename,
		bytes<(MAX_PATH*2)-2?bytes:(MAX_PATH*2)-2);

	find_data->cAlternateFileName [0] = 0;	/* not used */

	g_free (utf8_basename);
	g_free (utf8_filename);
	g_free (utf16_basename);

cleanup:
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	return(ret);
}

/**
 * FindClose:
 * @wapi_handle: the find handle to close.
 *
 * Closes find handle @wapi_handle
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean FindClose (gpointer handle)
{
	struct _WapiHandle_find *find_handle;
	gboolean ok;
	int thr_ret;

	if (handle == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_FIND,
				(gpointer *)&find_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up find handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	g_strfreev (find_handle->namelist);
	g_free (find_handle->dir_part);

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	_wapi_handle_unref (handle);
	
	return(TRUE);
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
gboolean CreateDirectory (const gunichar2 *name,
			  WapiSecurityAttributes *security)
{
	gchar *utf8_name;
	int result;
	
	if (name == NULL) {
#ifdef DEBUG
		g_message("%s: name is NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(FALSE);
	}
	
	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion returned NULL", __func__);
#endif
	
		SetLastError (ERROR_INVALID_NAME);
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
	
	if (name == NULL) {
#ifdef DEBUG
		g_message("%s: name is NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(FALSE);
	}

	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion returned NULL", __func__);
#endif
		
		SetLastError (ERROR_INVALID_NAME);
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

/**
 * GetFileAttributes:
 * @name: a pointer to a NULL-terminated unicode filename.
 *
 * Gets the attributes for @name;
 *
 * Return value: %INVALID_FILE_ATTRIBUTES on failure
 */
guint32 GetFileAttributes (const gunichar2 *name)
{
	gchar *utf8_name;
	struct stat buf, linkbuf;
	int result;
	guint32 ret;
	
	if (name == NULL) {
#ifdef DEBUG
		g_message("%s: name is NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(FALSE);
	}
	
	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion returned NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_PARAMETER);
		return (INVALID_FILE_ATTRIBUTES);
	}

	result = _wapi_stat (utf8_name, &buf);
	if (result == -1 && errno == ENOENT) {
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

	struct stat buf, linkbuf;
	time_t create_time;
	int result;
	
	if (level != GetFileExInfoStandard) {
#ifdef DEBUG
		g_message ("%s: info level %d not supported.", __func__,
			   level);
#endif

		SetLastError (ERROR_INVALID_PARAMETER);
		return FALSE;
	}
	
	if (name == NULL) {
#ifdef DEBUG
		g_message("%s: name is NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(FALSE);
	}

	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion returned NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_PARAMETER);
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

	/* fill data block */

	data = (WapiFileAttributesData *)info;

	if (buf.st_mtime < buf.st_ctime)
		create_time = buf.st_mtime;
	else
		create_time = buf.st_ctime;
	
	data->dwFileAttributes = _wapi_stat_to_file_attributes (utf8_name,
								&buf,
								&linkbuf);

	g_free (utf8_name);

	_wapi_time_t_to_filetime (create_time, &data->ftCreationTime);
	_wapi_time_t_to_filetime (buf.st_atime, &data->ftLastAccessTime);
	_wapi_time_t_to_filetime (buf.st_mtime, &data->ftLastWriteTime);

	if (data->dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
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
	gchar *utf8_name;
	struct stat buf;
	int result;

	/*
	 * Currently we only handle one *internal* case, with a value that is
	 * not standard: 0x80000000, which means `set executable bit'
	 */
	
	if (name == NULL) {
#ifdef DEBUG
		g_message("%s: name is NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return(FALSE);
	}

	utf8_name = mono_unicode_to_external (name);
	if (utf8_name == NULL) {
#ifdef DEBUG
		g_message ("%s: unicode conversion returned NULL", __func__);
#endif

		SetLastError (ERROR_INVALID_NAME);
		return FALSE;
	}

	result = _wapi_stat (utf8_name, &buf);
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
		result = _wapi_chmod (utf8_name, buf.st_mode & ~(S_IWRITE | S_IWOTH | S_IWGRP));
	} else {
		result = _wapi_chmod (utf8_name, buf.st_mode | S_IWRITE);
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

		result = chmod (utf8_name, buf.st_mode | exec_mask);
	}
	/* Don't bother to reset executable (might need to change this
	 * policy)
	 */
	
	g_free (utf8_name);

	return(TRUE);
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
	gunichar2 *utf16_path;
	glong count;
	gsize bytes;
	
	path = g_get_current_dir ();
	if (path == NULL)
		return 0;

	utf16_path=mono_unicode_from_external (path, &bytes);
	
	/* if buffer too small, return number of characters required.
	 * this is plain dumb.
	 */
	
	count = (bytes/2)+1;
	if (count > length) {
		g_free(path);
		g_free (utf16_path);
		
		return (count);
	}

	/* Add the terminator */
	memset (buffer, '\0', bytes+2);
	memcpy (buffer, utf16_path, bytes);
	
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

	if (path == NULL) {
		SetLastError (ERROR_INVALID_PARAMETER);
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

gboolean CreatePipe (gpointer *readpipe, gpointer *writepipe,
		     WapiSecurityAttributes *security G_GNUC_UNUSED, guint32 size)
{
	struct _WapiHandle_file pipe_read_handle = {0};
	struct _WapiHandle_file pipe_write_handle = {0};
	gpointer read_handle;
	gpointer write_handle;
	int filedes[2];
	int ret;
	
	mono_once (&io_ops_once, io_ops_init);
	
#ifdef DEBUG
	g_message ("%s: Creating pipe", __func__);
#endif

	ret=pipe (filedes);
	if(ret==-1) {
#ifdef DEBUG
		g_message ("%s: Error creating pipe: %s", __func__,
			   strerror (errno));
#endif
		
		_wapi_set_last_error_from_errno ();
		return(FALSE);
	}

	if (filedes[0] >= _wapi_fd_reserve ||
	    filedes[1] >= _wapi_fd_reserve) {
#ifdef DEBUG
		g_message ("%s: File descriptor is too big", __func__);
#endif

		SetLastError (ERROR_TOO_MANY_OPEN_FILES);
		
		close (filedes[0]);
		close (filedes[1]);
		
		return(FALSE);
	}
	
	/* filedes[0] is open for reading, filedes[1] for writing */

	pipe_read_handle.fileaccess = GENERIC_READ;
	read_handle = _wapi_handle_new_fd (WAPI_HANDLE_PIPE, filedes[0],
					   &pipe_read_handle);
	if (read_handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating pipe read handle", __func__);
		close (filedes[0]);
		close (filedes[1]);
		SetLastError (ERROR_GEN_FAILURE);
		
		return(FALSE);
	}
	
	pipe_write_handle.fileaccess = GENERIC_WRITE;
	write_handle = _wapi_handle_new_fd (WAPI_HANDLE_PIPE, filedes[1],
					    &pipe_write_handle);
	if (write_handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating pipe write handle", __func__);
		_wapi_handle_unref (read_handle);
		
		close (filedes[0]);
		close (filedes[1]);
		SetLastError (ERROR_GEN_FAILURE);
		
		return(FALSE);
	}
	
	*readpipe = read_handle;
	*writepipe = write_handle;

#ifdef DEBUG
	g_message ("%s: Returning pipe: read handle %p, write handle %p",
		   __func__, read_handle, write_handle);
#endif

	return(TRUE);
}

guint32 GetTempPath (guint32 len, gunichar2 *buf)
{
	gchar *tmpdir=g_strdup (g_get_tmp_dir ());
	gunichar2 *tmpdir16=NULL;
	glong dirlen;
	gsize bytes;
	guint32 ret;
	
	if(tmpdir[strlen (tmpdir)]!='/') {
		g_free (tmpdir);
		tmpdir=g_strdup_printf ("%s/", g_get_tmp_dir ());
	}
	
	tmpdir16=mono_unicode_from_external (tmpdir, &bytes);
	if(tmpdir16==NULL) {
		g_free (tmpdir);
		return(0);
	} else {
		dirlen=(bytes/2);
		
		if(dirlen+1>len) {
#ifdef DEBUG
			g_message ("%s: Size %d smaller than needed (%ld)",
				   __func__, len, dirlen+1);
#endif
		
			ret=dirlen+1;
		} else {
			/* Add the terminator */
			memset (buf, '\0', bytes+2);
			memcpy (buf, tmpdir16, bytes);
		
			ret=dirlen;
		}
	}

	if(tmpdir16!=NULL) {
		g_free (tmpdir16);
	}
	g_free (tmpdir);
	
	return(ret);
}

gint32
GetLogicalDriveStrings (guint32 len, gunichar2 *buf)
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
	fp = fopen ("/etc/mtab", "rt");
	if (fp == NULL) {
		fp = fopen ("/etc/mnttab", "rt");
		if (fp == NULL)
			return 1;
	}

	ptr = buf;
	while (fgets (buffer, 512, fp) != NULL) {
		if (*buffer != '/')
			continue;

		splitted = g_strsplit (buffer, " ", 0);
		if (!*splitted || !*(splitted + 1)) {
			g_strfreev (splitted);
			continue;
		}

		dir = g_utf8_to_utf16 (*(splitted + 1), -1, &length, NULL, NULL);
		g_strfreev (splitted);
		if (total + length + 1 > len) {
			fclose (fp);
			return len * 2; /* guess */
		}

		memcpy (ptr + total, dir, sizeof (gunichar2) * length);
		g_free (dir);
		total += length + 1;
	}

	fclose (fp);
	return total;
/* Commented out, does not work with my mtab!!! - Gonz */
#ifdef NOTENABLED /* HAVE_MNTENT_H */
{
	FILE *fp;
	struct mntent *mnt;
	gunichar2 *ptr, *dir;
	glong len, total = 0;
	

	fp = setmntent ("/etc/mtab", "rt");
	if (fp == NULL) {
		fp = setmntent ("/etc/mnttab", "rt");
		if (fp == NULL)
			return;
	}

	ptr = buf;
	while ((mnt = getmntent (fp)) != NULL) {
		g_print ("GOT %s\n", mnt->mnt_dir);
		dir = g_utf8_to_utf16 (mnt->mnt_dir, &len, NULL, NULL, NULL);
		if (total + len + 1 > len) {
			return len * 2; /* guess */
		}

		memcpy (ptr + total, dir, sizeof (gunichar2) * len);
		g_free (dir);
		total += len + 1;
	}

	endmntent (fp);
	return total;
}
#endif
}

static gboolean _wapi_lock_file_region (int fd, off_t offset, off_t length)
{
	struct flock lock_data;
	int ret;

	lock_data.l_type = F_WRLCK;
	lock_data.l_whence = SEEK_SET;
	lock_data.l_start = offset;
	lock_data.l_len = length;
	
	do {
		ret = fcntl (fd, F_SETLK, &lock_data);
	} while(ret == -1 && errno == EINTR);
	
#ifdef DEBUG
	g_message ("%s: fcntl returns %d", __func__, ret);
#endif

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
			return (TRUE);
		}
		
		SetLastError (ERROR_LOCK_VIOLATION);
		return(FALSE);
	}

	return(TRUE);
}

static gboolean _wapi_unlock_file_region (int fd, off_t offset, off_t length)
{
	struct flock lock_data;
	int ret;

	lock_data.l_type = F_UNLCK;
	lock_data.l_whence = SEEK_SET;
	lock_data.l_start = offset;
	lock_data.l_len = length;
	
	do {
		ret = fcntl (fd, F_SETLK, &lock_data);
	} while(ret == -1 && errno == EINTR);
	
#ifdef DEBUG
	g_message ("%s: fcntl returns %d", __func__, ret);
#endif
	
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
			return (TRUE);
		}
		
		SetLastError (ERROR_LOCK_VIOLATION);
		return(FALSE);
	}

	return(TRUE);
}

gboolean LockFile (gpointer handle, guint32 offset_low, guint32 offset_high,
		   guint32 length_low, guint32 length_high)
{
	struct _WapiHandle_file *file_handle;
	gboolean ok;
	off_t offset, length;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_FILE,
				  (gpointer *)&file_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up file handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}

	if (!(file_handle->fileaccess & GENERIC_READ) &&
	    !(file_handle->fileaccess & GENERIC_WRITE) &&
	    !(file_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message ("%s: handle %p doesn't have GENERIC_READ or GENERIC_WRITE access: %u", __func__, handle, file_handle->fileaccess);
#endif
		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}

#ifdef HAVE_LARGE_FILE_SUPPORT
	offset = ((gint64)offset_high << 32) | offset_low;
	length = ((gint64)length_high << 32) | length_low;

#ifdef DEBUG
	g_message ("%s: Locking handle %p, offset %lld, length %lld", __func__,
		   handle, offset, length);
#endif
#else
	offset = offset_low;
	length = length_low;

#ifdef DEBUG
	g_message ("%s: Locking handle %p, offset %ld, length %ld", __func__,
		   handle, offset, length);
#endif
#endif

	return(_wapi_lock_file_region (fd, offset, length));
}

gboolean UnlockFile (gpointer handle, guint32 offset_low,
		     guint32 offset_high, guint32 length_low,
		     guint32 length_high)
{
	struct _WapiHandle_file *file_handle;
	gboolean ok;
	off_t offset, length;
	int fd = GPOINTER_TO_UINT(handle);
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_FILE,
				  (gpointer *)&file_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up file handle %p", __func__,
			   handle);
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	if (!(file_handle->fileaccess & GENERIC_READ) &&
	    !(file_handle->fileaccess & GENERIC_WRITE) &&
	    !(file_handle->fileaccess & GENERIC_ALL)) {
#ifdef DEBUG
		g_message ("%s: handle %p doesn't have GENERIC_READ or GENERIC_WRITE access: %u", __func__, handle, file_handle->fileaccess);
#endif
		SetLastError (ERROR_ACCESS_DENIED);
		return(FALSE);
	}

#ifdef HAVE_LARGE_FILE_SUPPORT
	offset = ((gint64)offset_high << 32) | offset_low;
	length = ((gint64)length_high << 32) | length_low;

#ifdef DEBUG
	g_message ("%s: Unlocking handle %p, offset %lld, length %lld",
		   __func__, handle, offset, length);
#endif
#else
	offset = offset_low;
	length = length_low;

#ifdef DEBUG
	g_message ("%s: Unlocking handle %p, offset %ld, length %ld", __func__,
		   handle, offset, length);
#endif
#endif

	return(_wapi_unlock_file_region (fd, offset, length));
}
