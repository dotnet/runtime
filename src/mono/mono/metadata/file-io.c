/*
 * file-io.c: File IO internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/file-io.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>

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
	gint64 *ticks;

	ticks = (gint64 *)filetime;
	return *ticks;
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

HANDLE 
ves_icall_System_IO_MonoIO_FindFirstFile (MonoString *path, MonoIOStat *stat,
					  gint32 *error)
{
	WIN32_FIND_DATA data;
	HANDLE result;

	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	result = FindFirstFile (mono_string_chars (path), &data);

	/* note: WIN32_FIND_DATA is an extension of WIN32_FILE_ATTRIBUTE_DATA */

	if (result != INVALID_HANDLE_VALUE) {
		convert_win32_file_attribute_data ((const WIN32_FILE_ATTRIBUTE_DATA *)&data,
						   &data.cFileName [0], stat);
	} else {
		*error=GetLastError ();
	}

	return result;
}

MonoBoolean
ves_icall_System_IO_MonoIO_FindNextFile (HANDLE find, MonoIOStat *stat,
					 gint32 *error)
{
	WIN32_FIND_DATA data;
	gboolean result;

	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	result = FindNextFile (find, &data);
	if (result) {
		convert_win32_file_attribute_data ((const WIN32_FILE_ATTRIBUTE_DATA *)&data,
						   &data.cFileName [0], stat);
	} else {
		*error=GetLastError ();
	}

	return result;
}

MonoBoolean
ves_icall_System_IO_MonoIO_FindClose (HANDLE find, gint32 *error)
{
	gboolean ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=FindClose (find);
	if(ret==FALSE) {
		*error=GetLastError ();
	}
	
	return(ret);
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
	if(ret==INVALID_FILE_ATTRIBUTES) {
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
	
	ret=SetFileAttributes (mono_string_chars (path), attrs);
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
				 gint32 access_mode, gint32 share,
				 gint32 *error)
{
	HANDLE ret;
	
	MONO_ARCH_SAVE_REGS;

	*error=ERROR_SUCCESS;
	
	ret=CreateFile (mono_string_chars (filename),
			convert_access (access_mode), convert_share (share),
			NULL, convert_mode (mode), FILE_ATTRIBUTE_NORMAL,
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
	guint32 offset_hi;

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
	MONO_ARCH_SAVE_REGS;

#if defined (PLATFORM_WIN32)
	return (gunichar2) 0x003a;	/* colon */
#else
	return (gunichar2) 0x002f;	/* forward slash */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar ()
{
	MONO_ARCH_SAVE_REGS;

#if defined (PLATFORM_WIN32)
	return (gunichar2) 0x005c;	/* backslash */
#else
	return (gunichar2) 0x002f;	/* forward slash */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar ()
{
	MONO_ARCH_SAVE_REGS;

#if defined (PLATFORM_WIN32)
	return (gunichar2) 0x002f;	/* forward slash */
#else
	return (gunichar2) 0x005c;	/* backslash */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_PathSeparator ()
{
	MONO_ARCH_SAVE_REGS;

#if defined (PLATFORM_WIN32)
	return (gunichar2) 0x003b;	/* semicolon */
#else
	return (gunichar2) 0x003a;	/* colon */
#endif
}

static gunichar2 invalid_path_chars [] = {
#if defined (PLATFORM_WIN32)
	0x0022,				/* double quote */
	0x003c,				/* less than */
	0x003e,				/* greater than */
	0x007c,				/* pipe */
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
	chars = mono_array_new (domain, mono_defaults.char_class, 5);

	n = sizeof (invalid_path_chars) / sizeof (gunichar2);

	for (i = 0; i < n; ++ i)
		mono_array_set (chars, gunichar2, i, invalid_path_chars [i]);
	
	return chars;
}
