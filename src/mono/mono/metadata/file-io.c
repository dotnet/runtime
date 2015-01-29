/*
 * file-io.c: File IO internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
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
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/file-io.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/utils/strenc.h>
#include <utils/mono-io-portability.h>

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

static gint64 convert_filetime (const FILETIME *filetime)
{
	guint64 ticks = filetime->dwHighDateTime;
	ticks <<= 32;
	ticks += filetime->dwLowDateTime;
	return (gint64)ticks;
}

static void convert_win32_file_attribute_data (const WIN32_FILE_ATTRIBUTE_DATA *data, MonoIOStat *stat)
{
	stat->attributes = data->dwFileAttributes;
	stat->creation_time = convert_filetime (&data->ftCreationTime);
	stat->last_access_time = convert_filetime (&data->ftLastAccessTime);
	stat->last_write_time = convert_filetime (&data->ftLastWriteTime);
	stat->length = ((gint64)data->nFileSizeHigh << 32) | data->nFileSizeLow;
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

/*
 * On Win32, GetFileAttributes|Ex () seems to try opening the file,
 * which might lead to sharing violation errors, whereas FindFirstFile
 * always succeeds. These 2 wrappers resort to FindFirstFile if
 * GetFileAttributes|Ex () has failed.
 */
static guint32
get_file_attributes (const gunichar2 *path)
{
	guint32 res;
	WIN32_FIND_DATA find_data;
	HANDLE find_handle;
	gint32 error;

	res = GetFileAttributes (path);
	if (res != -1)
		return res;

	error = GetLastError ();

	if (error != ERROR_SHARING_VIOLATION)
		return res;

	find_handle = FindFirstFile (path, &find_data);

	if (find_handle == INVALID_HANDLE_VALUE)
		return res;

	FindClose (find_handle);

	return find_data.dwFileAttributes;
}

static gboolean
get_file_attributes_ex (const gunichar2 *path, WIN32_FILE_ATTRIBUTE_DATA *data)
{
	gboolean res;
	WIN32_FIND_DATA find_data;
	HANDLE find_handle;
	gint32 error;

	res = GetFileAttributesEx (path, GetFileExInfoStandard, data);
	if (res)
		return TRUE;

	error = GetLastError ();

	if (error != ERROR_SHARING_VIOLATION)
		return FALSE;

	find_handle = FindFirstFile (path, &find_data);

	if (find_handle == INVALID_HANDLE_VALUE)
		return FALSE;

	FindClose (find_handle);

	data->dwFileAttributes = find_data.dwFileAttributes;
	data->ftCreationTime = find_data.ftCreationTime;
	data->ftLastAccessTime = find_data.ftLastAccessTime;
	data->ftLastWriteTime = find_data.ftLastWriteTime;
	data->nFileSizeHigh = find_data.nFileSizeHigh;
	data->nFileSizeLow = find_data.nFileSizeLow;
	
	return TRUE;
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

static gchar *
get_search_dir (MonoString *pattern)
{
	gchar *p;
	gchar *result;

	p = mono_string_to_utf8 (pattern);
	result = g_path_get_dirname (p);
	g_free (p);
	return result;
}

MonoArray *
ves_icall_System_IO_MonoIO_GetFileSystemEntries (MonoString *path,
						 MonoString *path_with_pattern,
						 gint attrs, gint mask,
						 gint32 *error)
{
	MonoDomain *domain;
	MonoArray *result;
	int i;
	WIN32_FIND_DATA data;
	HANDLE find_handle;
	GPtrArray *names;
	gchar *utf8_path, *utf8_result, *full_name;
	gint32 attributes;
	
	MONO_ARCH_SAVE_REGS;

	result = NULL;
	*error = ERROR_SUCCESS;

	domain = mono_domain_get ();
	mask = convert_attrs (mask);
	attributes = get_file_attributes (mono_string_chars (path));
	if (attributes != -1) {
		if ((attributes & FILE_ATTRIBUTE_DIRECTORY) == 0) {
			*error = ERROR_INVALID_NAME;
			goto leave;
		}
	} else {
		*error = GetLastError ();
		goto leave;
	}
	
	find_handle = FindFirstFile (mono_string_chars (path_with_pattern), &data);
	if (find_handle == INVALID_HANDLE_VALUE) {
		gint32 find_error = GetLastError ();
		
		if (find_error == ERROR_FILE_NOT_FOUND || find_error == ERROR_NO_MORE_FILES) {
			/* No files, so just return an empty array */
			goto leave;
		}
		
		*error = find_error;
		goto leave;
	}

	utf8_path = get_search_dir (path_with_pattern);
	names = g_ptr_array_new ();

	do {
		if ((data.cFileName[0] == '.' && data.cFileName[1] == 0) ||
		    (data.cFileName[0] == '.' && data.cFileName[1] == '.' && data.cFileName[2] == 0)) {
			continue;
		}
		
		if ((data.dwFileAttributes & mask) == attrs) {
			utf8_result = g_utf16_to_utf8 (data.cFileName, -1, NULL, NULL, NULL);
			if (utf8_result == NULL) {
				continue;
			}
			
			full_name = g_build_filename (utf8_path, utf8_result, NULL);
			g_ptr_array_add (names, full_name);

			g_free (utf8_result);
		}
	} while(FindNextFile (find_handle, &data));

	if (FindClose (find_handle) == FALSE) {
		*error = GetLastError ();
		result = NULL;
	} else {
		result = mono_array_new (domain, mono_defaults.string_class, names->len);
		for (i = 0; i < names->len; i++) {
			mono_array_setref (result, i, mono_string_new (domain, g_ptr_array_index (names, i)));
		}
	}

	for (i = 0; i < names->len; i++) {
		g_free (g_ptr_array_index (names, i));
	}
	g_ptr_array_free (names, TRUE);
	g_free (utf8_path);

leave:
	// If there's no array and no error, then return an empty array.
	if (result == NULL && *error == ERROR_SUCCESS)
		result = mono_array_new (domain, mono_defaults.string_class, 0);

	return result;
}

typedef struct {
	MonoDomain *domain;
	gchar *utf8_path;
	HANDLE find_handle;
} IncrementalFind;
	
static gboolean
incremental_find_check_match (IncrementalFind *handle, WIN32_FIND_DATA *data, MonoString **result)
{
	gchar *utf8_result;
	gchar *full_name;
	
	if ((data->cFileName[0] == '.' && data->cFileName[1] == 0) || (data->cFileName[0] == '.' && data->cFileName[1] == '.' && data->cFileName[2] == 0))
		return FALSE;

	utf8_result = g_utf16_to_utf8 (data->cFileName, -1, NULL, NULL, NULL);
	if (utf8_result == NULL) 
		return FALSE;
	
	full_name = g_build_filename (handle->utf8_path, utf8_result, NULL);
	g_free (utf8_result);
	*result = mono_string_new (mono_domain_get (), full_name);
	g_free (full_name);
	
	return TRUE;
}

MonoString *
ves_icall_System_IO_MonoIO_FindFirst (MonoString *path,
				      MonoString *path_with_pattern,
				      gint32 *result_attr, gint32 *error,
				      gpointer *handle)
{
	WIN32_FIND_DATA data;
	HANDLE find_handle;
	IncrementalFind *ifh;
	MonoString *result;
	
	*error = ERROR_SUCCESS;
	
	find_handle = FindFirstFile (mono_string_chars (path_with_pattern), &data);
	
	if (find_handle == INVALID_HANDLE_VALUE) {
		gint32 find_error = GetLastError ();
		*handle = NULL;
		
		if (find_error == ERROR_FILE_NOT_FOUND) 
			return NULL;
		
		*error = find_error;
		return NULL;
	}

	ifh = g_new (IncrementalFind, 1);
	ifh->find_handle = find_handle;
	ifh->utf8_path = mono_string_to_utf8 (path);
	ifh->domain = mono_domain_get ();
	*handle = ifh;

	while (incremental_find_check_match (ifh, &data, &result) == 0){
		if (FindNextFile (find_handle, &data) == FALSE){
			int e = GetLastError ();
			if (e != ERROR_NO_MORE_FILES)
				*error = e;
			return NULL;
		}
	}
	*result_attr = data.dwFileAttributes;
	
	return result;
}

MonoString *
ves_icall_System_IO_MonoIO_FindNext (gpointer handle, gint32 *result_attr, gint32 *error)
{
	IncrementalFind *ifh = handle;
	WIN32_FIND_DATA data;
	MonoString *result;

	*error = ERROR_SUCCESS;
	do {
		if (FindNextFile (ifh->find_handle, &data) == FALSE){
			int e = GetLastError ();
			if (e != ERROR_NO_MORE_FILES)
				*error = e;
			return NULL;
		}
	} while (incremental_find_check_match (ifh, &data, &result) == 0);

	*result_attr = data.dwFileAttributes;
	return result;
}

int
ves_icall_System_IO_MonoIO_FindClose (gpointer handle)
{
	IncrementalFind *ifh = handle;
	gint32 error;
	
	if (FindClose (ifh->find_handle) == FALSE){
		error = GetLastError ();
	} else
		error = ERROR_SUCCESS;
	g_free (ifh->utf8_path);
	g_free (ifh);

	return error;
}

MonoString *
ves_icall_System_IO_MonoIO_GetCurrentDirectory (gint32 *error)
{
	MonoString *result;
	gunichar2 *buf;
	int len, res_len;

	MONO_ARCH_SAVE_REGS;

	len = MAX_PATH + 1; /*FIXME this is too smal under most unix systems.*/
	buf = g_new (gunichar2, len);
	
	*error=ERROR_SUCCESS;
	result = NULL;

	res_len = GetCurrentDirectory (len, buf);
	if (res_len > len) { /*buf is too small.*/
		int old_res_len = res_len;
		g_free (buf);
		buf = g_new (gunichar2, res_len);
		res_len = GetCurrentDirectory (res_len, buf) == old_res_len;
	}
	
	if (res_len) {
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
ves_icall_System_IO_MonoIO_ReplaceFile (MonoString *sourceFileName, MonoString *destinationFileName,
					MonoString *destinationBackupFileName, MonoBoolean ignoreMetadataErrors,
					gint32 *error)
{
	gboolean ret;
	gunichar2 *utf16_sourceFileName = NULL, *utf16_destinationFileName = NULL, *utf16_destinationBackupFileName = NULL;
	guint32 replaceFlags = REPLACEFILE_WRITE_THROUGH;

	MONO_ARCH_SAVE_REGS;

	if (sourceFileName)
		utf16_sourceFileName = mono_string_chars (sourceFileName);
	if (destinationFileName)
		utf16_destinationFileName = mono_string_chars (destinationFileName);
	if (destinationBackupFileName)
		utf16_destinationBackupFileName = mono_string_chars (destinationBackupFileName);

	*error = ERROR_SUCCESS;
	if (ignoreMetadataErrors)
		replaceFlags |= REPLACEFILE_IGNORE_MERGE_ERRORS;

	/* FIXME: source and destination file names must not be NULL, but apparently they might be! */
	ret = ReplaceFile (utf16_destinationFileName, utf16_sourceFileName, utf16_destinationBackupFileName,
			 replaceFlags, NULL, NULL);
	if (ret == FALSE)
		*error = GetLastError ();

	return ret;
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
	
	ret=get_file_attributes (mono_string_chars (path));

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
	
	result = get_file_attributes_ex (mono_string_chars (path), &data);

	if (result) {
		convert_win32_file_attribute_data (&data, stat);
	} else {
		*error=GetLastError ();
		memset (stat, 0, sizeof (MonoIOStat));
	}

	return result;
}

HANDLE 
ves_icall_System_IO_MonoIO_Open (MonoString *filename, gint32 mode,
				 gint32 access_mode, gint32 share, gint32 options,
				 gint32 *error)
{
	HANDLE ret;
	int attributes, attrs;
	gunichar2 *chars = mono_string_chars (filename);
	
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

		if (options & FileOptions_Temporary)
			attributes |= FILE_ATTRIBUTE_TEMPORARY;
		
		/* Not sure if we should set FILE_FLAG_OVERLAPPED, how does this mix with the "Async" bool here? */
		if (options & FileOptions_Asynchronous)
			attributes |= FILE_FLAG_OVERLAPPED;
		
		if (options & FileOptions_WriteThrough)
			attributes |= FILE_FLAG_WRITE_THROUGH;
	} else
		attributes = FILE_ATTRIBUTE_NORMAL;

	/* If we're opening a directory we need to set the extra flag
	 */
	attrs = get_file_attributes (chars);
	if (attrs != INVALID_FILE_ATTRIBUTES) {
		if (attrs & FILE_ATTRIBUTE_DIRECTORY) {
			attributes |= FILE_FLAG_BACKUP_SEMANTICS;
		}
	}
	
	ret=CreateFile (chars, convert_access (access_mode),
			convert_share (share), NULL, convert_mode (mode),
			attributes, NULL);
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

	MONO_CHECK_ARG_NULL (dest);

	if (dest_offset > mono_array_length (dest) - count)
		mono_raise_exception (mono_get_exception_argument ("array", "array too small. numBytes/offset wrong."));

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

	MONO_CHECK_ARG_NULL (src);
	
	if (src_offset > mono_array_length (src) - count)
		mono_raise_exception (mono_get_exception_argument ("array", "array too small. numBytes/offset wrong."));
	
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
	offset = SetFilePointer (handle, (gint32) (offset & 0xFFFFFFFF), &offset_hi,
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

MonoBoolean ves_icall_System_IO_MonoIO_DuplicateHandle (HANDLE source_process_handle, 
						HANDLE source_handle, HANDLE target_process_handle, HANDLE *target_handle, 
						gint32 access, gint32 inherit, gint32 options)
{
	/* This is only used on Windows */
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;
	
	ret=DuplicateHandle (source_process_handle, source_handle, target_process_handle, target_handle, access, inherit, options);
	if(ret==FALSE) {
		/* FIXME: throw an exception? */
		return(FALSE);
	}
	
	return(TRUE);
}

gunichar2 
ves_icall_System_IO_MonoIO_get_VolumeSeparatorChar ()
{
#if defined (TARGET_WIN32)
	return (gunichar2) ':';	/* colon */
#else
	return (gunichar2) '/';	/* forward slash */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar ()
{
#if defined (TARGET_WIN32)
	return (gunichar2) '\\';	/* backslash */
#else
	return (gunichar2) '/';	/* forward slash */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar ()
{
#if defined (TARGET_WIN32)
	return (gunichar2) '/';	/* forward slash */
#else
	if (IS_PORTABILITY_SET)
		return (gunichar2) '\\';	/* backslash */
	else
		return (gunichar2) '/';	/* forward slash */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_PathSeparator ()
{
#if defined (TARGET_WIN32)
	return (gunichar2) ';';	/* semicolon */
#else
	return (gunichar2) ':';	/* colon */
#endif
}

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
		g_message ("%s: Temp path is [%s] (len %d)", __func__, name, ret);
#endif

		mono_gc_wbarrier_generic_store ((gpointer) mono_name,
				(MonoObject*) mono_string_new_utf16 (mono_domain_get (), name, ret));
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

//Support for io-layer free mmap'd files.

#if defined (TARGET_IOS) || defined (TARGET_ANDROID)

gint64
mono_filesize_from_path (MonoString *string)
{
	struct stat buf;
	gint64 res;
	char *path = mono_string_to_utf8 (string);
	
	if (stat (path, &buf) == -1)
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

	if (fstat (fd, &buf) == -1)
		return (gint64)-1;

	return (gint64)buf.st_size;
}

#endif
