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

gint32 
ves_icall_System_IO_MonoIO_GetLastError ()
{
	return GetLastError ();
}

gboolean 
ves_icall_System_IO_MonoIO_CreateDirectory (MonoString *path)
{
	gunichar2 *utf16_path;
	gboolean result;

	utf16_path = mono_string_to_utf16 (path);
	result = CreateDirectory (utf16_path, NULL);
	g_free (utf16_path);

	return result;
}

gboolean 
ves_icall_System_IO_MonoIO_RemoveDirectory (MonoString *path)
{
	gunichar2 *utf16_path;
	gboolean result;

	utf16_path = mono_string_to_utf16 (path);
	result = RemoveDirectory (utf16_path);
	g_free (utf16_path);

	return result;
}

HANDLE 
ves_icall_System_IO_MonoIO_FindFirstFile (MonoString *path, MonoIOStat *stat)
{
	gunichar2 *utf16_path;
	WIN32_FIND_DATA data;
	HANDLE result;

	utf16_path = mono_string_to_utf16 (path);
	result = FindFirstFile (utf16_path, &data);
	g_free (utf16_path);

	/* note: WIN32_FIND_DATA is an extension of WIN32_FILE_ATTRIBUTE_DATA */

	if (result != INVALID_HANDLE_VALUE)
		convert_win32_file_attribute_data ((const WIN32_FILE_ATTRIBUTE_DATA *)&data,
						   &data.cFileName [0], stat);

	return result;
}

gboolean 
ves_icall_System_IO_MonoIO_FindNextFile (HANDLE find, MonoIOStat *stat)
{
	WIN32_FIND_DATA data;
	gboolean result;

	result = FindNextFile (find, &data);
	if (result)
		convert_win32_file_attribute_data ((const WIN32_FILE_ATTRIBUTE_DATA *)&data,
						   &data.cFileName [0], stat);

	return result;
}

gboolean 
ves_icall_System_IO_MonoIO_FindClose (HANDLE find)
{
	return FindClose (find);
}

MonoString *
ves_icall_System_IO_MonoIO_GetCurrentDirectory ()
{
	MonoString *result;
	gunichar2 *buf;
	int len;

	len = MAX_PATH + 1;
	buf = g_new (gunichar2, len);
	
	result = NULL;
	if (GetCurrentDirectory (len, buf) > 0) {
		len = 0;
		while (buf [len])
			++ len;

		result = mono_string_new_utf16 (mono_domain_get (), buf, len);
	}

	g_free (buf);
	return result;
}

gboolean 
ves_icall_System_IO_MonoIO_SetCurrentDirectory (MonoString *path)
{
	gunichar2 *utf16_path;
	gboolean result;
	
	utf16_path = mono_string_to_utf16 (path);
	result = SetCurrentDirectory (utf16_path);
	g_free (utf16_path);

	return result;
}

gboolean 
ves_icall_System_IO_MonoIO_MoveFile (MonoString *path, MonoString *dest)
{
	gunichar2 *utf16_path, *utf16_dest;
	gboolean result;

	utf16_path = mono_string_to_utf16 (path);
	utf16_dest = mono_string_to_utf16 (dest);
	result = MoveFile (utf16_path, utf16_dest);
	g_free (utf16_path);
	g_free (utf16_dest);

	return result;
}

gboolean 
ves_icall_System_IO_MonoIO_CopyFile (MonoString *path, MonoString *dest, gboolean overwrite)
{
	gunichar2 *utf16_path, *utf16_dest;
	gboolean result;

	utf16_path = mono_string_to_utf16 (path);
	utf16_dest = mono_string_to_utf16 (dest);
	result = CopyFile (utf16_path, utf16_dest, !overwrite);
	g_free (utf16_path);
	g_free (utf16_dest);

	return result;
}

gboolean 
ves_icall_System_IO_MonoIO_DeleteFile (MonoString *path)
{
	gunichar2 *utf16_path;
	gboolean result;

	utf16_path = mono_string_to_utf16 (path);
	result = DeleteFile (utf16_path);
	g_free (utf16_path);

	return result;
}

gint32 
ves_icall_System_IO_MonoIO_GetFileAttributes (MonoString *path)
{
	gunichar2 *utf16_path;
	gint32 result;

	utf16_path = mono_string_to_utf16 (path);
	result = GetFileAttributes (utf16_path);
	g_free (utf16_path);

	return result;
}

gboolean
ves_icall_System_IO_MonoIO_SetFileAttributes (MonoString *path, gint32 attrs)
{
	gunichar2 *utf16_path;
	gboolean result;

	utf16_path = mono_string_to_utf16 (path);
	result = SetFileAttributes (utf16_path, attrs);
	g_free (utf16_path);

	return result;
}

gboolean 
ves_icall_System_IO_MonoIO_GetFileStat (MonoString *path, MonoIOStat *stat)
{
	gunichar2 *utf16_path;
	gboolean result;
	WIN32_FILE_ATTRIBUTE_DATA data;

	utf16_path = mono_string_to_utf16 (path);
	result = GetFileAttributesEx (utf16_path, GetFileExInfoStandard, &data);
	g_free (utf16_path);

	if (result)
		convert_win32_file_attribute_data (&data, utf16_path, stat);

	return result;
}

HANDLE 
ves_icall_System_IO_MonoIO_Open (MonoString *filename, gint32 mode, gint32 access_mode, gint32 share)
{
	gunichar2 *utf16_filename;
	HANDLE result;

	utf16_filename = mono_string_to_utf16 (filename);
	result = CreateFile (utf16_filename, convert_access (access_mode), convert_share (share),
			     NULL, convert_mode (mode), FILE_ATTRIBUTE_NORMAL, NULL);
	g_free (utf16_filename);

	return result;
}

gboolean 
ves_icall_System_IO_MonoIO_Close (HANDLE handle)
{
	return CloseHandle (handle);
}

gint32 
ves_icall_System_IO_MonoIO_Read (HANDLE handle, MonoArray *dest, gint32 dest_offset, gint32 count)
{
	guchar *buffer;
	gboolean result;
	guint32 n;

	if (dest_offset + count > mono_array_length (dest))
		return 0;

	buffer = mono_array_addr (dest, guchar, dest_offset);
	result = ReadFile (handle, buffer, count, &n, NULL);

	if (!result)
		return -1;

	return (gint32)n;
}

gint32 
ves_icall_System_IO_MonoIO_Write (HANDLE handle, MonoArray *src, gint32 src_offset, gint32 count)
{
	guchar *buffer;
	gboolean result;
	guint32 n;

	if (src_offset + count > mono_array_length (src))
		return 0;
	
	buffer = mono_array_addr (src, guchar, src_offset);
	result = WriteFile (handle, buffer, count, &n, NULL);

	if (!result)
		return -1;

	return (gint32)n;
}

gint64 
ves_icall_System_IO_MonoIO_Seek (HANDLE handle, gint64 offset, gint32 origin)
{
	guint32 offset_hi;

	offset_hi = offset >> 32;
	offset = SetFilePointer (handle, offset & 0xFFFFFFFF, &offset_hi,
				 convert_seekorigin (origin));

	return offset | ((gint64)offset_hi << 32);
}

gboolean 
ves_icall_System_IO_MonoIO_Flush (HANDLE handle)
{
	return FlushFileBuffers (handle);
}

gint64 
ves_icall_System_IO_MonoIO_GetLength (HANDLE handle)
{
	gint64 length;
	guint32 length_hi;

	length = GetFileSize (handle, &length_hi);
	return length | ((gint64)length_hi << 32);
}

gboolean 
ves_icall_System_IO_MonoIO_SetLength (HANDLE handle, gint64 length)
{
	gint64 offset;
	gint32 offset_hi;
	gint32 length_hi;
	gboolean result;

	/* save file pointer */

	offset_hi = 0;
	offset = SetFilePointer (handle, 0, &offset_hi, FILE_CURRENT);

	/* extend or truncate */

	length_hi = length >> 32;
	SetFilePointer (handle, length & 0xFFFFFFFF, &length_hi, FILE_BEGIN);
	result = SetEndOfFile (handle);

	/* restore file pointer */

	SetFilePointer (handle, offset & 0xFFFFFFFF, &offset_hi, FILE_BEGIN);

	return result;
}

gboolean
ves_icall_System_IO_MonoIO_SetFileTime (HANDLE handle, gint64 creation_time, gint64 last_access_time, gint64 last_write_time)
{
	const FILETIME *creation_filetime;
	const FILETIME *last_access_filetime;
	const FILETIME *last_write_filetime;

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

	return SetFileTime (handle, creation_filetime, last_access_filetime, last_write_filetime);
}

HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleOutput ()
{
	return GetStdHandle (STD_OUTPUT_HANDLE);
}

HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleInput ()
{
	return GetStdHandle (STD_INPUT_HANDLE);
}

HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleError ()
{
	return GetStdHandle (STD_ERROR_HANDLE);
}

gunichar2 
ves_icall_System_IO_MonoIO_get_VolumeSeparatorChar ()
{
#if defined (PLATFORM_WIN32)
	return (gunichar2) 0x003a;	/* colon */
#else
	return (gunichar2) 0x002f;	/* forward slash */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar ()
{
#if defined (PLATFORM_WIN32)
	return (gunichar2) 0x005c;	/* backslash */
#else
	return (gunichar2) 0x002f;	/* forward slash */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar ()
{
#if defined (PLATFORM_WIN32)
	return (gunichar2) 0x002f;	/* forward slash */
#else
	return (gunichar2) 0x005c;	/* backslash */
#endif
}

gunichar2 
ves_icall_System_IO_MonoIO_get_PathSeparator ()
{
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

	domain = mono_domain_get ();
	chars = mono_array_new (domain, mono_defaults.char_class, 5);

	n = sizeof (invalid_path_chars) / sizeof (gunichar2);

	for (i = 0; i < n; ++ i)
		mono_array_set (chars, gunichar2, i, invalid_path_chars [i]);
	
	return chars;
}
