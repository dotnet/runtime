/*
 * file-io.c: File IO internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * (C) 2001,2002,2003 Ximian, Inc.
 * Copyright (c) 2004,2005,2006 Novell, Inc. (http://www.novell.com)
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <errno.h>
#include <signal.h>
#include <unistd.h>
#ifdef HAVE_SYS_STAT_H
#include <sys/stat.h>
#endif
#ifdef HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif

#include <mono/metadata/object.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/file-io.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/utils/strenc.h>

#undef DEBUG

/* conversion functions */

static guint32 convert_mode(MonoFileMode mono_mode)
{
	guint32 mode;

	switch(mono_mode) {
	case FileMode_CreateNew:
		mode=CREATE_NEW;
		break;
	case FileMode_Create:
		mode=CREATE_ALWAYS;
		break;
	case FileMode_Open:
		mode=OPEN_EXISTING;
		break;
	case FileMode_OpenOrCreate:
		mode=OPEN_ALWAYS;
		break;
	case FileMode_Truncate:
		mode=TRUNCATE_EXISTING;
		break;
	case FileMode_Append:
		mode=OPEN_ALWAYS;
		break;
	default:
		g_warning("System.IO.FileMode has unknown value 0x%x",
			  mono_mode);
		/* Safe fallback */
		mode=OPEN_EXISTING;
	}
	
	return(mode);
}

static guint32 convert_access(MonoFileAccess mono_access)
{
	guint32 access;
	
	switch(mono_access) {
	case FileAccess_Read:
		access=GENERIC_READ;
		break;
	case FileAccess_Write:
		access=GENERIC_WRITE;
		break;
	case FileAccess_ReadWrite:
		access=GENERIC_READ|GENERIC_WRITE;
		break;
	default:
		g_warning("System.IO.FileAccess has unknown value 0x%x",
			  mono_access);
		/* Safe fallback */
		access=GENERIC_READ;
	}
	
	return(access);
}

static guint32 convert_share(MonoFileShare mono_share)
{
	guint32 share;
	
	switch(mono_share) {
	case FileShare_None:
		share=0;
		break;
	case FileShare_Read:
		share=FILE_SHARE_READ;
		break;
	case FileShare_Write:
		share=FILE_SHARE_WRITE;
		break;
	case FileShare_ReadWrite:
		share=FILE_SHARE_READ|FILE_SHARE_WRITE;
		break;
	default:
		g_warning("System.IO.FileShare has unknown value 0x%x",
			  mono_share);
		/* Safe fallback */
		share=0;
	}
	
	return(share);
}

#if 0
static guint32 convert_stdhandle(guint32 fd)
{
	guint32 stdhandle;
	
	switch(fd) {
	case 0:
		stdhandle=STD_INPUT_HANDLE;
		break;
	case 1:
		stdhandle=STD_OUTPUT_HANDLE;
		break;
	case 2:
		stdhandle=STD_ERROR_HANDLE;
		break;
	default:
		g_warning("unknown standard file descriptor %d", fd);
		stdhandle=STD_INPUT_HANDLE;
	}
	
	return(stdhandle);
}
#endif

static guint32 convert_seekorigin(MonoSeekOrigin origin)
{
	guint32 w32origin;
	
	switch(origin) {
	case SeekOrigin_Begin:
		w32origin=FILE_BEGIN;
		break;
	case SeekOrigin_Current:
		w32origin=FILE_CURRENT;
		break;
	case SeekOrigin_End:
		w32origin=FILE_END;
		break;
	default:
		g_warning("System.IO.SeekOrigin has unknown value 0x%x",
			  origin);
		/* Safe fallback */
		w32origin=FILE_CURRENT;
	}
	
	return(w32origin);
}

static gint64 convert_filetime (const FILETIME *filetime)
{
	guint64 ticks = filetime->dwHighDateTime;
	ticks <<= 32;
	ticks += filetime->dwLowDateTime;
	return (gint64)ticks;
}

static void convert_win32_file_attribute_data (const WIN32_FILE_ATTRIBUTE_DATA *data, const gunichar2 *name, MonoIOStat *stat)
{
	int len;
	
	stat->attributes = data->dwFileAttributes;
	stat->creation_time = convert_filetime (&data->ftCreationTime);
	stat->last_access_time = convert_filetime (&data->ftLastAccessTime);
	stat->last_write_time = convert_filetime (&data->ftLastWriteTime);
	stat->length = ((gint64)data->nFileSizeHigh << 32) | data->nFileSizeLow;

	len = 0;
	while (name [len])
		++ len;

	stat->name = mono_string_new_utf16 (mono_domain_get (), name, len);
}

/* Managed file attributes have nearly but not quite the same values
 * as the w32 equivalents.
 */
static guint32 convert_attrs(MonoFileAttributes attrs)
{
	if(attrs & FileAttributes_Encrypted) {
		attrs |= FILE_ATTRIBUTE_ENCRYPTED;
	}
	
	return(attrs);
}

/* System.IO.MonoIO internal calls */

MonoBoolean
ves_icall_System_IO_MonoIO_CreateDirectory (MonoString *path, gint32 *error)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=CreateDirectory (mono_string_chars (path), NULL);
	if(ret==FALSE) {
		*error=GetLastError ();
	}
	
	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_RemoveDirectory (MonoString *path, gint32 *error)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=RemoveDirectory (mono_string_chars (path));
	if(ret==FALSE) {
		*error=GetLastError ();
	}
	
	return(ret);
}

static gint
get_error_from_g_file_error (gint error)
{
	switch (error) {
	case G_FILE_ERROR_ACCES:
		error = ERROR_ACCESS_DENIED;
		break;
	case G_FILE_ERROR_NAMETOOLONG:
		error = ERROR_FILENAME_EXCED_RANGE;
		break;
	case G_FILE_ERROR_NOENT:
		error = ERROR_FILE_NOT_FOUND;
		break;
	case G_FILE_ERROR_NOTDIR:
		error = ERROR_FILE_NOT_FOUND;
		break;
	case G_FILE_ERROR_ROFS:
		error = ERROR_ACCESS_DENIED;
		break;
	case G_FILE_ERROR_TXTBSY:
		error = ERROR_SHARING_VIOLATION;
		break;
	case G_FILE_ERROR_NOSPC:
		error = ERROR_HANDLE_DISK_FULL;
		break;
	case G_FILE_ERROR_NFILE:
	case G_FILE_ERROR_MFILE:
		error = ERROR_TOO_MANY_OPEN_FILES;
		break;
	case G_FILE_ERROR_BADF:
		error = ERROR_INVALID_HANDLE;
		break;
	case G_FILE_ERROR_INVAL:
		error = ERROR_INVALID_PARAMETER;
		break;
	case G_FILE_ERROR_AGAIN:
		error = ERROR_SHARING_VIOLATION;
		break;
	case G_FILE_ERROR_INTR:
		error = ERROR_IO_PENDING;
		break;
	case G_FILE_ERROR_PERM:
		error = ERROR_ACCESS_DENIED;
		break;
	case G_FILE_ERROR_FAILED:
		error = ERROR_INVALID_PARAMETER;
		break;
	case G_FILE_ERROR_NXIO:
	case G_FILE_ERROR_NOMEM:
	case G_FILE_ERROR_NODEV:
	case G_FILE_ERROR_FAULT:
	case G_FILE_ERROR_LOOP:
	case G_FILE_ERROR_PIPE:
	case G_FILE_ERROR_IO:
	default:
		error = ERROR_GEN_FAILURE;
		break;

	}

	return error;
}

static gint
file_compare (gconstpointer a, gconstpointer b)
{
	gchar *astr = *(gchar **) a;
	gchar *bstr = *(gchar **) b;

	return strcmp (astr, bstr);
}

static gint
get_file_attributes (const char *filename)
{
#ifdef PLATFORM_WIN32
	gunichar2 *full16;
	gint result;

	full16 = g_utf8_to_utf16 (filename, -1, NULL, NULL, NULL);
	if (full16 == NULL) {
		g_message ("Bad encoding for '%s'\n", filename);
		return FALSE;
	}

	result = GetFileAttributes (full16);
	g_free (full16);
	return result;
#else
	struct stat buf;
	struct stat linkbuf;
	int result;
	int file_attrs;

	result = lstat (filename, &buf);
	if (result == -1)
		return FALSE;

	if (S_ISLNK (buf.st_mode)) {
		result = stat (filename, &linkbuf);
		if (result != -1) {
			buf = linkbuf;
		} else {
			buf.st_mode |= ~S_IFDIR; /* force it to be returned as regular file */
		}
	}

	/* Sockets (0140000) != Directory (040000) + Regular file (0100000) */
	if (S_ISSOCK (buf.st_mode))
		buf.st_mode &= ~S_IFSOCK; /* don't consider socket protection */

	file_attrs = 0;
	if (S_ISDIR (buf.st_mode))
		file_attrs |= FILE_ATTRIBUTE_DIRECTORY;
	else
		file_attrs |= FILE_ATTRIBUTE_ARCHIVE;

	if ((buf.st_mode & S_IWUSR) == 0)
		file_attrs |= FILE_ATTRIBUTE_READONLY;

	if (*filename == '.')
		file_attrs |= FILE_ATTRIBUTE_HIDDEN;

	return file_attrs;
#endif
}

static gboolean
test_file (const char *filename, int attrs, int mask)
{
	int file_attr;

	file_attr = get_file_attributes (filename);
	if (file_attr == FALSE)
		return FALSE;

	return ((file_attr & mask) == attrs);
}

/* scandir using glib */
static gint
mono_io_scandir (const gchar *dirname, const gchar *pattern, int attrs,
		int mask, gchar ***namelist)
{
	GError *error = NULL;
	GDir *dir;
	GPtrArray *names;
	const gchar *name;
	gint result;
	GPatternSpec *patspec;
	gchar *full_name;

	mask = convert_attrs (mask);
	*namelist = NULL;
	dir = g_dir_open (dirname, 0, &error);
	if (dir == NULL) {
		/* g_dir_open returns ENOENT on directories on which we don't
		 * have read/x permission */
		gint errnum = get_error_from_g_file_error (error->code);
		g_error_free (error);
		if (errnum == ERROR_FILE_NOT_FOUND && g_file_test (dirname, G_FILE_TEST_IS_DIR))
			errnum = ERROR_ACCESS_DENIED;

		SetLastError (errnum);
		return -1;
	}

	patspec = g_pattern_spec_new (pattern);
	names = g_ptr_array_new ();
	while ((name = g_dir_read_name (dir)) != NULL) {
		if (!g_pattern_match_string (patspec, name))
			continue;

		full_name = g_build_filename (dirname, name, NULL);
		if (FALSE == test_file (full_name, attrs, mask)) {
			g_free (full_name);
			continue;
		}

		g_ptr_array_add (names, full_name);
	}
	
	g_pattern_spec_free (patspec);
	g_dir_close (dir);
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

MonoArray *
ves_icall_System_IO_MonoIO_GetFileSystemEntries (MonoString *_path, MonoString *_pattern,
					gint attrs, gint mask, gint32 *error)
{
	MonoDomain *domain;
	MonoArray *result;
	gchar **namelist;
	gchar *path;
	gchar *pattern;
	int i, nnames;
	int removed;
	MonoString *str_name;
#ifndef PLATFORM_WIN32
	gunichar2 *utf16;
	gsize nbytes;
#endif

	MONO_ARCH_SAVE_REGS;

	*error = ERROR_SUCCESS;

	path = mono_string_to_utf8 (_path);
	pattern = mono_string_to_utf8 (_pattern);
	nnames = mono_io_scandir (path, pattern, attrs, mask, &namelist);
	if (nnames < 0) {
		*error = GetLastError ();
		g_free (pattern);
		g_free (path);
		return NULL;
	}

	domain = mono_domain_get ();
	result = mono_array_new (domain, mono_defaults.string_class, nnames);
	removed = 0;
	for (i = 0; i < nnames; i++) {
#if PLATFORM_WIN32
		str_name = mono_string_new (domain, namelist [i]);
#else
		utf16 = mono_unicode_from_external (namelist [i], &nbytes);
		if (utf16 == NULL) {
			g_message ("Bad encoding for '%s'\nConsider using MONO_EXTERNAL_ENCODINGS\n",
				namelist [i]);
			removed++;
			continue;
		}
		str_name = mono_string_from_utf16 (utf16);
		g_free (utf16);
#endif
		mono_array_setref (result, i - removed, str_name);
	}

	if (removed > 0) {
		MonoArray *shrinked;
		shrinked = mono_array_new (domain, mono_defaults.string_class, nnames - removed);
		for (i = 0; i < (nnames - removed); i++) {
			MonoString *str;
			str = mono_array_get (result, MonoString *, i);
			mono_array_setref (shrinked, i, str);
		}
		result = shrinked;
	}

	g_strfreev (namelist);
	g_free (pattern);
	g_free (path);
	return result;
}

MonoString *
ves_icall_System_IO_MonoIO_GetCurrentDirectory (gint32 *error)
{
	MonoString *result;
	gunichar2 *buf;
	int len;

	MONO_ARCH_SAVE_REGS;

	len = MAX_PATH + 1;
	buf = g_new (gunichar2, len);
	
	*error=ERROR_SUCCESS;
	result = NULL;

	if (GetCurrentDirectory (len, buf) > 0) {
		len = 0;
		while (buf [len])
			++ len;

		result = mono_string_new_utf16 (mono_domain_get (), buf, len);
	} else {
		*error=GetLastError ();
	}

	g_free (buf);
	return result;
}

MonoBoolean
ves_icall_System_IO_MonoIO_SetCurrentDirectory (MonoString *path,
						gint32 *error)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=SetCurrentDirectory (mono_string_chars (path));
	if(ret==FALSE) {
		*error=GetLastError ();
	}
	
	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_MoveFile (MonoString *path, MonoString *dest,
				     gint32 *error)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=MoveFile (mono_string_chars (path), mono_string_chars (dest));
	if(ret==FALSE) {
		*error=GetLastError ();
	}
	
	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_CopyFile (MonoString *path, MonoString *dest,
				     MonoBoolean overwrite, gint32 *error)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=CopyFile (mono_string_chars (path), mono_string_chars (dest), !overwrite);
	if(ret==FALSE) {
		*error=GetLastError ();
	}
	
	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_DeleteFile (MonoString *path, gint32 *error)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=DeleteFile (mono_string_chars (path));
	if(ret==FALSE) {
		*error=GetLastError ();
	}
	
	return(ret);
}

gint32 
ves_icall_System_IO_MonoIO_GetFileAttributes (MonoString *path, gint32 *error)
{
	gint32 ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=GetFileAttributes (mono_string_chars (path));

	/* 
	 * The definition of INVALID_FILE_ATTRIBUTES in the cygwin win32
	 * headers is wrong, hence this temporary workaround.
	 * See
	 * http://cygwin.com/ml/cygwin/2003-09/msg01771.html
	 */
	if (ret==-1) {
	  /* if(ret==INVALID_FILE_ATTRIBUTES) { */
		*error=GetLastError ();
	}
	
	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_SetFileAttributes (MonoString *path, gint32 attrs,
					      gint32 *error)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=SetFileAttributes (mono_string_chars (path),
			       convert_attrs (attrs));
	if(ret==FALSE) {
		*error=GetLastError ();
	}
	
	return(ret);
}

gint32
ves_icall_System_IO_MonoIO_GetFileType (HANDLE handle, gint32 *error)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=GetFileType (handle);
	if(ret==FILE_TYPE_UNKNOWN) {
		/* Not necessarily an error, but the caller will have
		 * to decide based on the error value.
		 */
		*error=GetLastError ();
	}
	
	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_GetFileStat (MonoString *path, MonoIOStat *stat,
					gint32 *error)
{
	gboolean result;
	WIN32_FILE_ATTRIBUTE_DATA data;

	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	result = GetFileAttributesEx (mono_string_chars (path), GetFileExInfoStandard, &data);

	if (result) {
		convert_win32_file_attribute_data (&data,
						   mono_string_chars (path),
						   stat);
	} else {
		*error=GetLastError ();
	}

	return result;
}

HANDLE 
ves_icall_System_IO_MonoIO_Open (MonoString *filename, gint32 mode,
				 gint32 access_mode, gint32 share, gint32 options,
				 gint32 *error)
{
	HANDLE ret;
	int attributes;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;

	if (options != 0){
		if (options & FileOptions_Encrypted)
			attributes = FILE_ATTRIBUTE_ENCRYPTED;
		else
			attributes = FILE_ATTRIBUTE_NORMAL;
		if (options & FileOptions_DeleteOnClose)
			attributes |= FILE_FLAG_DELETE_ON_CLOSE;
		if (options & FileOptions_SequentialScan)
			attributes |= FILE_FLAG_SEQUENTIAL_SCAN;
		if (options & FileOptions_RandomAccess)
			attributes |= FILE_FLAG_RANDOM_ACCESS;

		/* Not sure if we should set FILE_FLAG_OVERLAPPED, how does this mix with the "Async" bool here? */
		if (options & FileOptions_Asynchronous)
			attributes |= FILE_FLAG_OVERLAPPED;
		
		if (options & FileOptions_WriteThrough)
			attributes |= FILE_FLAG_WRITE_THROUGH;
	} else
		attributes = FILE_ATTRIBUTE_NORMAL;


	ret=CreateFile (mono_string_chars (filename),
			convert_access (access_mode), convert_share (share),
			NULL, convert_mode (mode),
			attributes,
			NULL);
	if(ret==INVALID_HANDLE_VALUE) {
		*error=GetLastError ();
	} 
	
	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_Close (HANDLE handle, gint32 *error)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=CloseHandle (handle);
	if(ret==FALSE) {
		*error=GetLastError ();
	}
	
	return(ret);
}

gint32 
ves_icall_System_IO_MonoIO_Read (HANDLE handle, MonoArray *dest,
				 gint32 dest_offset, gint32 count,
				 gint32 *error)
{
	guchar *buffer;
	gboolean result;
	guint32 n;

	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	if (dest_offset + count > mono_array_length (dest))
		return 0;

	buffer = mono_array_addr (dest, guchar, dest_offset);
	result = ReadFile (handle, buffer, count, &n, NULL);

	if (!result) {
		*error=GetLastError ();
		return -1;
	}

	return (gint32)n;
}

gint32 
ves_icall_System_IO_MonoIO_Write (HANDLE handle, MonoArray *src,
				  gint32 src_offset, gint32 count,
				  gint32 *error)
{
	guchar *buffer;
	gboolean result;
	guint32 n;

	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	if (src_offset + count > mono_array_length (src))
		return 0;
	
	buffer = mono_array_addr (src, guchar, src_offset);
	result = WriteFile (handle, buffer, count, &n, NULL);

	if (!result) {
		*error=GetLastError ();
		return -1;
	}

	return (gint32)n;
}

gint64 
ves_icall_System_IO_MonoIO_Seek (HANDLE handle, gint64 offset, gint32 origin,
				 gint32 *error)
{
	gint32 offset_hi;

	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	offset_hi = offset >> 32;
	offset = SetFilePointer (handle, offset & 0xFFFFFFFF, &offset_hi,
				 convert_seekorigin (origin));

	if(offset==INVALID_SET_FILE_POINTER) {
		*error=GetLastError ();
	}
	
	return offset | ((gint64)offset_hi << 32);
}

MonoBoolean
ves_icall_System_IO_MonoIO_Flush (HANDLE handle, gint32 *error)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=FlushFileBuffers (handle);
	if(ret==FALSE) {
		*error=GetLastError ();
	}
	
	return(ret);
}

gint64 
ves_icall_System_IO_MonoIO_GetLength (HANDLE handle, gint32 *error)
{
	gint64 length;
	guint32 length_hi;

	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	length = GetFileSize (handle, &length_hi);
	if(length==INVALID_FILE_SIZE) {
		*error=GetLastError ();
	}
	
	return length | ((gint64)length_hi << 32);
}

MonoBoolean
ves_icall_System_IO_MonoIO_SetLength (HANDLE handle, gint64 length,
				      gint32 *error)
{
	gint64 offset, offset_set;
	gint32 offset_hi;
	gint32 length_hi;
	gboolean result;

	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	/* save file pointer */

	offset_hi = 0;
	offset = SetFilePointer (handle, 0, &offset_hi, FILE_CURRENT);
	if(offset==INVALID_SET_FILE_POINTER) {
		*error=GetLastError ();
		return(FALSE);
	}

	/* extend or truncate */

	length_hi = length >> 32;
	offset_set=SetFilePointer (handle, length & 0xFFFFFFFF, &length_hi,
				   FILE_BEGIN);
	if(offset_set==INVALID_SET_FILE_POINTER) {
		*error=GetLastError ();
		return(FALSE);
	}

	result = SetEndOfFile (handle);
	if(result==FALSE) {
		*error=GetLastError ();
		return(FALSE);
	}

	/* restore file pointer */

	offset_set=SetFilePointer (handle, offset & 0xFFFFFFFF, &offset_hi,
				   FILE_BEGIN);
	if(offset_set==INVALID_SET_FILE_POINTER) {
		*error=GetLastError ();
		return(FALSE);
	}

	return result;
}

MonoBoolean
ves_icall_System_IO_MonoIO_SetFileTime (HANDLE handle, gint64 creation_time,
					gint64 last_access_time,
					gint64 last_write_time, gint32 *error)
{
	gboolean ret;
	const FILETIME *creation_filetime;
	const FILETIME *last_access_filetime;
	const FILETIME *last_write_filetime;

	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	if (creation_time < 0)
		creation_filetime = NULL;
	else
		creation_filetime = (FILETIME *)&creation_time;

	if (last_access_time < 0)
		last_access_filetime = NULL;
	else
		last_access_filetime = (FILETIME *)&last_access_time;

	if (last_write_time < 0)
		last_write_filetime = NULL;
	else
		last_write_filetime = (FILETIME *)&last_write_time;

	ret=SetFileTime (handle, creation_filetime, last_access_filetime, last_write_filetime);
	if(ret==FALSE) {
		*error=GetLastError ();
	}
	
	return(ret);
}

HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleOutput ()
{
	MONO_ARCH_SAVE_REGS;

	return GetStdHandle (STD_OUTPUT_HANDLE);
}

HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleInput ()
{
	MONO_ARCH_SAVE_REGS;

	return GetStdHandle (STD_INPUT_HANDLE);
}

HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleError ()
{
	MONO_ARCH_SAVE_REGS;

	return GetStdHandle (STD_ERROR_HANDLE);
}

MonoBoolean
ves_icall_System_IO_MonoIO_CreatePipe (HANDLE *read_handle,
				       HANDLE *write_handle)
{
	SECURITY_ATTRIBUTES attr;
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	attr.nLength=sizeof(SECURITY_ATTRIBUTES);
	attr.bInheritHandle=TRUE;
	attr.lpSecurityDescriptor=NULL;
	
	ret=CreatePipe (read_handle, write_handle, &attr, 0);
	if(ret==FALSE) {
		/* FIXME: throw an exception? */
		return(FALSE);
	}
	
	return(TRUE);
}

gunichar2 
ves_icall_System_IO_MonoIO_get_VolumeSeparatorChar ()
{
#if defined (PLATFORM_WIN32)
	return (gunichar2) ':';	/* colon */
#else
	return (gunichar2) '/';	/* forward slash */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar ()
{
#if defined (PLATFORM_WIN32)
	return (gunichar2) '\\';	/* backslash */
#else
	return (gunichar2) '/';	/* forward slash */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar ()
{
#if defined (PLATFORM_WIN32)
	return (gunichar2) '/';	/* forward slash */
#else
	return (gunichar2) '/';	/* slash, same as DirectorySeparatorChar */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_PathSeparator ()
{
#if defined (PLATFORM_WIN32)
	return (gunichar2) ';';	/* semicolon */
#else
	return (gunichar2) ':';	/* colon */
#endif
}

static const gunichar2
invalid_path_chars [] = {
#if defined (PLATFORM_WIN32)
	0x0022,				/* double quote, which seems allowed in MS.NET but should be rejected */
	0x003c,				/* less than */
	0x003e,				/* greater than */
	0x007c,				/* pipe */
	0x0008,
	0x0010,
	0x0011,
	0x0012,
	0x0014,
	0x0015,
	0x0016,
	0x0017,
	0x0018,
	0x0019,
#endif
	0x0000				/* null */
};

MonoArray *
ves_icall_System_IO_MonoIO_get_InvalidPathChars ()
{
	MonoArray *chars;
	MonoDomain *domain;
	int i, n;

	MONO_ARCH_SAVE_REGS;

	domain = mono_domain_get ();
	n = sizeof (invalid_path_chars) / sizeof (gunichar2);
	chars = mono_array_new (domain, mono_defaults.char_class, n);

	for (i = 0; i < n; ++ i)
		mono_array_set (chars, gunichar2, i, invalid_path_chars [i]);
	
	return chars;
}

gint32
ves_icall_System_IO_MonoIO_GetTempPath (MonoString **mono_name)
{
	gunichar2 *name;
	int ret;

	name=g_new0 (gunichar2, 256);
	
	ret=GetTempPath (256, name);
	if(ret>255) {
		/* Buffer was too short. Try again... */
		g_free (name);
		name=g_new0 (gunichar2, ret+2);	/* include the terminator */
		ret=GetTempPath (ret, name);
	}
	
	if(ret>0) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": Temp path is [%s] (len %d)", name, ret);
#endif

		*mono_name=mono_string_new_utf16 (mono_domain_get (), name,
						  ret);
	}

	g_free (name);
	
	return(ret);
}

void ves_icall_System_IO_MonoIO_Lock (HANDLE handle, gint64 position,
				      gint64 length, gint32 *error)
{
	gboolean ret;
	
	*error=ERROR_SUCCESS;
	
	ret=LockFile (handle, position & 0xFFFFFFFF, position >> 32,
		      length & 0xFFFFFFFF, length >> 32);
	if (ret == FALSE) {
		*error = GetLastError ();
	}
}

void ves_icall_System_IO_MonoIO_Unlock (HANDLE handle, gint64 position,
					gint64 length, gint32 *error)
{
	gboolean ret;
	
	*error=ERROR_SUCCESS;
	
	ret=UnlockFile (handle, position & 0xFFFFFFFF, position >> 32,
			length & 0xFFFFFFFF, length >> 32);
	if (ret == FALSE) {
		*error = GetLastError ();
	}
}

