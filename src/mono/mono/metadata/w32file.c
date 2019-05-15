/**
 * \file
 * File IO internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#include <glib.h>
#include <string.h>
#include <errno.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_SYS_STAT_H
#include <sys/stat.h>
#endif
#ifdef HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif

#include <mono/metadata/object.h>
#include <mono/metadata/w32file.h>
#include <mono/metadata/w32error.h>
#include <mono/metadata/w32file-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/metadata/w32handle.h>
#include <mono/utils/w32api.h>
#include "icall-decl.h"

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
	guint32 share = 0;
	
	if (mono_share & FileShare_Read) {
		share |= FILE_SHARE_READ;
	}
	if (mono_share & FileShare_Write) {
		share |= FILE_SHARE_WRITE;
	}
	if (mono_share & FileShare_Delete) {
		share |= FILE_SHARE_DELETE;
	}
	
	if (mono_share & ~(FileShare_Read|FileShare_Write|FileShare_Delete)) {
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

/* Managed file attributes have nearly but not quite the same values
 * as the w32 equivalents.
 */
static guint32 convert_attrs(MonoFileAttributes attrs)
{
	if(attrs & FileAttributes_Encrypted) {
		attrs = (MonoFileAttributes)(attrs | FILE_ATTRIBUTE_ENCRYPTED);
	}
	
	return(attrs);
}

/* System.IO.MonoIO internal calls */

#if !ENABLE_NETCORE

MonoBoolean
ves_icall_System_IO_MonoIO_CreateDirectory (const gunichar2 *path, gint32 *error)
{
	gboolean ret;
	
	*error=ERROR_SUCCESS;
	
	ret=mono_w32file_create_directory (path);
	if(ret==FALSE) {
		*error=mono_w32error_get_last ();
	}

	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_RemoveDirectory (const gunichar2 *path, gint32 *error)
{
	gboolean ret;
	
	*error=ERROR_SUCCESS;
	
	ret=mono_w32file_remove_directory (path);
	if(ret==FALSE) {
		*error=mono_w32error_get_last ();
	}

	return(ret);
}

HANDLE
ves_icall_System_IO_MonoIO_FindFirstFile (const gunichar2 *path_with_pattern, MonoStringHandleOut file_name, gint32 *file_attr, gint32 *ioerror, MonoError *error)
{
	HANDLE hnd;
	WIN32_FIND_DATA data;

	hnd = mono_w32file_find_first (path_with_pattern, &data);

	if (hnd == INVALID_HANDLE_VALUE) {
		MONO_HANDLE_ASSIGN (file_name, NULL_HANDLE_STRING);
		*file_attr = 0;
		*ioerror = mono_w32error_get_last ();
		return hnd;
	}

	int len = 0;
	while (data.cFileName [len]) len++;
	MONO_HANDLE_ASSIGN (file_name, mono_string_new_utf16_handle (mono_domain_get (), data.cFileName, len, error));
	return_val_if_nok (error, INVALID_HANDLE_VALUE);

	*file_attr = data.dwFileAttributes;
	*ioerror = ERROR_SUCCESS;

	return hnd;
}

MonoBoolean
ves_icall_System_IO_MonoIO_FindNextFile (HANDLE hnd, MonoStringHandleOut file_name, gint32 *file_attr, gint32 *ioerror, MonoError *error)
{
	MonoBoolean res;
	WIN32_FIND_DATA data;

	res = mono_w32file_find_next (hnd, &data);

	if (res == FALSE) {
		MONO_HANDLE_ASSIGN (file_name, NULL_HANDLE_STRING);
		*file_attr = 0;
		*ioerror = mono_w32error_get_last ();
		return res;
	}

	int len = 0;
	while (data.cFileName [len]) len++;
	MONO_HANDLE_ASSIGN (file_name, mono_string_new_utf16_handle (mono_domain_get (), data.cFileName, len, error));
	return_val_if_nok (error, FALSE);

	*file_attr = data.dwFileAttributes;
	*ioerror = ERROR_SUCCESS;

	return res;
}

MonoBoolean
ves_icall_System_IO_MonoIO_FindCloseFile (HANDLE hnd)
{
	return mono_w32file_find_close (hnd);
}

MonoStringHandle
ves_icall_System_IO_MonoIO_GetCurrentDirectory (gint32 *io_error, MonoError *error)
{
	MonoStringHandle result;
	gunichar2 *buf;
	int len, res_len;

	len = MAX_PATH + 1; /*FIXME this is too smal under most unix systems.*/
	buf = g_new (gunichar2, len);
	
	*io_error = ERROR_SUCCESS;
	result = MONO_HANDLE_NEW (MonoString, NULL);

	res_len = mono_w32file_get_cwd (len, buf);
	if (res_len > len) { /*buf is too small.*/
		int old_res_len = res_len;
		g_free (buf);
		buf = g_new (gunichar2, res_len);
		res_len = mono_w32file_get_cwd (res_len, buf) == old_res_len;
	}
	
	if (res_len) {
		len = 0;
		while (buf [len])
			++ len;

		MONO_HANDLE_ASSIGN (result, mono_string_new_utf16_handle (mono_domain_get (), buf, len, error));
	} else {
		*io_error=mono_w32error_get_last ();
	}

	g_free (buf);
	return_val_if_nok (error, NULL_HANDLE_STRING);
	return result;
}

MonoBoolean
ves_icall_System_IO_MonoIO_SetCurrentDirectory (const gunichar2 *path,
						gint32 *error)
{
	gboolean ret;
	
	*error=ERROR_SUCCESS;
	
	ret=mono_w32file_set_cwd (path);
	if(ret==FALSE) {
		*error=mono_w32error_get_last ();
	}
	
	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_MoveFile (const gunichar2 *path, const gunichar2 *dest, gint32 *error)
{
	*error=ERROR_SUCCESS;
	return mono_w32file_move (path, dest, error);
}

MonoBoolean
ves_icall_System_IO_MonoIO_ReplaceFile (const gunichar2 *source_file_name, const gunichar2 *destination_file_name,
					const gunichar2 *destination_backup_file_name, MonoBoolean ignore_metadata_errors,
					gint32 *error)
{
	guint32 replace_flags = REPLACEFILE_WRITE_THROUGH;

	*error = ERROR_SUCCESS;
	if (ignore_metadata_errors)
		replace_flags |= REPLACEFILE_IGNORE_MERGE_ERRORS;

	/* FIXME: source and destination file names must not be NULL, but apparently they might be! */
	return mono_w32file_replace (destination_file_name, source_file_name,
					  destination_backup_file_name, replace_flags, error);
}

MonoBoolean
ves_icall_System_IO_MonoIO_CopyFile (const gunichar2 *path, const gunichar2 *dest,
				     MonoBoolean overwrite, gint32 *error)
{
	*error=ERROR_SUCCESS;
	return mono_w32file_copy (path, dest, overwrite, error);
}

MonoBoolean
ves_icall_System_IO_MonoIO_DeleteFile (const gunichar2 *path, gint32 *error)
{
	gboolean ret;
	
	*error=ERROR_SUCCESS;
	
	ret=mono_w32file_delete (path);
	if(ret==FALSE) {
		*error=mono_w32error_get_last ();
	}
	
	return(ret);
}

gint32 
ves_icall_System_IO_MonoIO_GetFileAttributes (const gunichar2 *path, gint32 *error)
{
	gint32 ret;
	*error=ERROR_SUCCESS;
	
	ret = mono_w32file_get_attributes (path);

	/* 
	 * The definition of INVALID_FILE_ATTRIBUTES in the cygwin win32
	 * headers is wrong, hence this temporary workaround.
	 * See
	 * http://cygwin.com/ml/cygwin/2003-09/msg01771.html
	 */
	if (ret==-1) {
	  /* if(ret==INVALID_FILE_ATTRIBUTES) { */
		*error=mono_w32error_get_last ();
	}
	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_SetFileAttributes (const gunichar2 *path, gint32 attrs,
					      gint32 *error)
{
	gboolean ret;
	*error=ERROR_SUCCESS;
	
	ret=mono_w32file_set_attributes (path,
		convert_attrs ((MonoFileAttributes)attrs));
	if(ret==FALSE) {
		*error=mono_w32error_get_last ();
	}
	return(ret);
}

gint32
ves_icall_System_IO_MonoIO_GetFileType (HANDLE handle, gint32 *error)
{
	gboolean ret;

	*error=ERROR_SUCCESS;
	
	ret=mono_w32file_get_type (handle);
	if(ret==FILE_TYPE_UNKNOWN) {
		/* Not necessarily an error, but the caller will have
		 * to decide based on the error value.
		 */
		*error=mono_w32error_get_last ();
	}
	
	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_GetFileStat (const gunichar2 *path, MonoIOStat *stat, gint32 *error)
{
	gboolean result;

	*error=ERROR_SUCCESS;
	
	result = mono_w32file_get_attributes_ex (path, stat);

	if (!result) {
		*error=mono_w32error_get_last ();
		memset (stat, 0, sizeof (MonoIOStat));
	}

	return result;
}

HANDLE 
ves_icall_System_IO_MonoIO_Open (const gunichar2 *filename, gint32 mode,
				 gint32 access_mode, gint32 share, gint32 options,
				 gint32 *error)
{
	HANDLE ret;
	int attributes, attrs;

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

		if (options & FileOptions_Temporary)
			attributes |= FILE_ATTRIBUTE_TEMPORARY;
		
		if (options & FileOptions_WriteThrough)
			attributes |= FILE_FLAG_WRITE_THROUGH;
	} else
		attributes = FILE_ATTRIBUTE_NORMAL;

	/* If we're opening a directory we need to set the extra flag
	 */
	attrs = mono_w32file_get_attributes (filename);
	if (attrs != INVALID_FILE_ATTRIBUTES) {
		if (attrs & FILE_ATTRIBUTE_DIRECTORY) {
			attributes |= FILE_FLAG_BACKUP_SEMANTICS;
		}
	}
	
	ret=mono_w32file_create (filename, convert_access ((MonoFileAccess)access_mode), convert_share ((MonoFileShare)share), convert_mode ((MonoFileMode)mode), attributes);
	if(ret==INVALID_HANDLE_VALUE) {
		*error=mono_w32error_get_last ();
	} 
	
	return(ret);
}

MonoBoolean
ves_icall_System_IO_MonoIO_Close (HANDLE handle, gint32 *error)
{
	gboolean ret;
	*error=ERROR_SUCCESS;
	
	ret=mono_w32file_close (handle);
	if(ret==FALSE) {
		*error=mono_w32error_get_last ();
	}
	return(ret);
}

gint32 
ves_icall_System_IO_MonoIO_Read (HANDLE handle, MonoArrayHandle dest,
				 gint32 dest_offset, gint32 count,
				 gint32 *io_error,
				 MonoError *error)
{
	void *buffer;
	gboolean result;
	guint32 n;

	*io_error=ERROR_SUCCESS;

	MONO_CHECK_ARG_NULL (MONO_HANDLE_RAW (dest), 0);

	if (dest_offset > mono_array_handle_length (dest) - count) {
		mono_error_set_argument (error, "array", "array too small. numBytes/offset wrong.");
		return 0;
	}

	guint32 buffer_handle = 0;
	buffer = MONO_ARRAY_HANDLE_PIN (dest, guchar, dest_offset, &buffer_handle);
	result = mono_w32file_read (handle, buffer, count, &n, io_error);
	mono_gchandle_free_internal (buffer_handle);

	if (!result)
		return -1;

	return (gint32)n;
}

gint32 
ves_icall_System_IO_MonoIO_Write (HANDLE handle, MonoArrayHandle src,
				  gint32 src_offset, gint32 count,
				  gint32 *io_error,
				  MonoError *error)
{
	void *buffer;
	gboolean result;
	guint32 n;

	*io_error=ERROR_SUCCESS;

	MONO_CHECK_ARG_NULL (MONO_HANDLE_RAW (src), 0);
	
	if (src_offset > mono_array_handle_length (src) - count) {
		mono_error_set_argument (error, "array", "array too small. numBytes/offset wrong.");
		return 0;
	}
	
	guint32 src_handle = 0;
	buffer = MONO_ARRAY_HANDLE_PIN (src, guchar, src_offset, &src_handle);
	result = mono_w32file_write (handle, buffer, count, &n, io_error);
	mono_gchandle_free_internal (src_handle);

	if (!result)
		return -1;

	return (gint32)n;
}

gint64 
ves_icall_System_IO_MonoIO_Seek (HANDLE handle, gint64 offset, gint32 origin,
				 gint32 *error)
{
	gint32 offset_hi;

	*error=ERROR_SUCCESS;
	
	offset_hi = offset >> 32;
	offset = mono_w32file_seek (handle, (gint32) (offset & 0xFFFFFFFF), &offset_hi,
				 convert_seekorigin ((MonoSeekOrigin)origin));

	if(offset==INVALID_SET_FILE_POINTER) {
		*error=mono_w32error_get_last ();
	}

	return offset | ((gint64)offset_hi << 32);
}

MonoBoolean
ves_icall_System_IO_MonoIO_Flush (HANDLE handle, gint32 *error)
{
	gboolean ret;

	*error=ERROR_SUCCESS;
	
	ret=mono_w32file_flush (handle);
	if(ret==FALSE) {
		*error=mono_w32error_get_last ();
	}
	
	return(ret);
}

gint64
ves_icall_System_IO_MonoIO_GetLength (HANDLE handle, gint32 *error)
{
	*error=ERROR_SUCCESS;
	return mono_w32file_get_file_size (handle, error);
}

/* FIXME make gc suspendable */
MonoBoolean
ves_icall_System_IO_MonoIO_SetLength (HANDLE handle, gint64 length,
				      gint32 *error)
{
	gint64 offset, offset_set;
	gint32 offset_hi;
	gint32 length_hi;
	gboolean result;

	*error=ERROR_SUCCESS;
	
	/* save file pointer */

	offset_hi = 0;
	offset = mono_w32file_seek (handle, 0, &offset_hi, FILE_CURRENT);
	if(offset==INVALID_SET_FILE_POINTER) {
		*error=mono_w32error_get_last ();
		return(FALSE);
	}

	/* extend or truncate */

	length_hi = length >> 32;
	offset_set=mono_w32file_seek (handle, length & 0xFFFFFFFF, &length_hi,
				   FILE_BEGIN);
	if(offset_set==INVALID_SET_FILE_POINTER) {
		*error=mono_w32error_get_last ();
		return(FALSE);
	}

	result = mono_w32file_truncate (handle);
	if(result==FALSE) {
		*error=mono_w32error_get_last ();
		return(FALSE);
	}

	/* restore file pointer */

	offset_set=mono_w32file_seek (handle, offset & 0xFFFFFFFF, &offset_hi,
				   FILE_BEGIN);
	if(offset_set==INVALID_SET_FILE_POINTER) {
		*error=mono_w32error_get_last ();
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
	const FILETIME *access_filetime;
	const FILETIME *write_filetime;

	*error=ERROR_SUCCESS;
	
	if (creation_time < 0)
		creation_filetime = NULL;
	else
		creation_filetime = (FILETIME *)&creation_time;

	if (last_access_time < 0)
		access_filetime = NULL;
	else
		access_filetime = (FILETIME *)&last_access_time;

	if (last_write_time < 0)
		write_filetime = NULL;
	else
		write_filetime = (FILETIME *)&last_write_time;

	ret=mono_w32file_set_times (handle, creation_filetime, access_filetime, write_filetime);
	if(ret==FALSE) {
		*error=mono_w32error_get_last ();
	}

	return(ret);
}

HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleOutput (void)
{
	return mono_w32file_get_console_output ();
}

HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleInput (void)
{
	return mono_w32file_get_console_input ();
}

HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleError (void)
{
	return mono_w32file_get_console_error ();
}

MonoBoolean
ves_icall_System_IO_MonoIO_CreatePipe (HANDLE *read_handle, HANDLE *write_handle, gint32 *error)
{
	*error = ERROR_SUCCESS;

	gboolean ret;

	ret=mono_w32file_create_pipe (read_handle, write_handle, 0);

	if(ret==FALSE) {
		*error = mono_w32error_get_last ();
		/* FIXME: throw an exception? */
		return(FALSE);
	}
	
	return(TRUE);
}

MonoBoolean
ves_icall_System_IO_MonoIO_DuplicateHandle (HANDLE source_process_handle, HANDLE source_handle,
		HANDLE target_process_handle, HANDLE *target_handle, gint32 access, gint32 inherit, gint32 options, gint32 *error)
{
#ifndef HOST_WIN32
	MonoW32Handle *source_handle_data;

	if (!mono_w32handle_lookup_and_ref (source_handle, &source_handle_data)) {
		*error = ERROR_INVALID_HANDLE;
		return FALSE;
	}

	*target_handle = mono_w32handle_duplicate (source_handle_data);

	mono_w32handle_unref ((MonoW32Handle*)source_handle);
#else
	gboolean ret;

	MONO_ENTER_GC_SAFE;
	ret=DuplicateHandle (source_process_handle, source_handle, target_process_handle, target_handle, access, inherit, options);
	MONO_EXIT_GC_SAFE;

	if (!ret) {
		*error = mono_w32error_get_last ();
		/* FIXME: throw an exception? */
		return(FALSE);
	}
#endif

	return(TRUE);
}

#ifndef HOST_WIN32
gunichar2 
ves_icall_System_IO_MonoIO_get_VolumeSeparatorChar (void)
{
	return (gunichar2) '/';	/* forward slash */
}

gunichar2 
ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar (void)
{
	return (gunichar2) '/';	/* forward slash */
}

gunichar2 
ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar (void)
{
	if (IS_PORTABILITY_SET)
		return (gunichar2) '\\';	/* backslash */
	else
		return (gunichar2) '/';	/* forward slash */
}

gunichar2 
ves_icall_System_IO_MonoIO_get_PathSeparator (void)
{
	return (gunichar2) ':';	/* colon */
}
#endif /* !HOST_WIN32 */

static const gunichar2
invalid_path_chars [] = {
#if defined (TARGET_WIN32)
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

MonoArrayHandle
ves_icall_System_IO_MonoIO_get_InvalidPathChars (MonoError *error)
{
	MonoArrayHandle chars = MONO_HANDLE_NEW (MonoArray, NULL);
	MonoDomain *domain;
	int i, n;

	domain = mono_domain_get ();
	n = sizeof (invalid_path_chars) / sizeof (gunichar2);
	MONO_HANDLE_ASSIGN (chars, mono_array_new_handle (domain, mono_defaults.char_class, n, error));
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoArray, mono_new_null ()));

	for (i = 0; i < n; ++ i)
		MONO_HANDLE_ARRAY_SETVAL (chars, gunichar2, i, invalid_path_chars [i]);
	
	return chars;
}

void ves_icall_System_IO_MonoIO_Lock (HANDLE handle, gint64 position,
				      gint64 length, gint32 *error)
{
	*error=ERROR_SUCCESS;
	mono_w32file_lock (handle, position, length, error);
}

void ves_icall_System_IO_MonoIO_Unlock (HANDLE handle, gint64 position,
					gint64 length, gint32 *error)
{
	*error=ERROR_SUCCESS;
	mono_w32file_unlock (handle, position, length, error);
}


#ifndef HOST_WIN32
void mono_w32handle_dump (void);

void ves_icall_System_IO_MonoIO_DumpHandles (void)
{
	mono_w32handle_dump ();
}
#endif /* !HOST_WIN32 */

#endif /* !ENABLE_NETCORE */

//Support for io-layer free mmap'd files.

#if defined (TARGET_IOS) || defined (TARGET_ANDROID)

gint64
mono_filesize_from_path (MonoString *string)
{
	ERROR_DECL (error);
	struct stat buf;
	gint64 res;
	char *path = mono_string_to_utf8_checked_internal (string, error);
	mono_error_raise_exception_deprecated (error); /* OK to throw, external only without a good alternative */

	gint stat_res;
	MONO_ENTER_GC_SAFE;
	stat_res = stat (path, &buf);
	MONO_EXIT_GC_SAFE;
	if (stat_res == -1)
		res = -1;
	else
		res = (gint64)buf.st_size;

	g_free (path);

	return res;
}

gint64
mono_filesize_from_fd (int fd)
{
	struct stat buf;
	int res;

	MONO_ENTER_GC_SAFE;
	res = fstat (fd, &buf);
	MONO_EXIT_GC_SAFE;
	
	if (res == -1)
		return (gint64)-1;

	return (gint64)buf.st_size;
}

#endif
