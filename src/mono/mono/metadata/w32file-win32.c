/*
 * w32file-win32.c: Windows File IO internal calls.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#include <winsock2.h>
#include <windows.h>
#include "mono/metadata/w32file-win32-internals.h"

void
mono_w32file_init (void)
{
}

void
mono_w32file_cleanup (void)
{
}

gunichar2
ves_icall_System_IO_MonoIO_get_VolumeSeparatorChar ()
{
	return (gunichar2) ':';	/* colon */
}

gunichar2
ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar ()
{
	return (gunichar2) '\\';	/* backslash */
}

gunichar2
ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar ()
{
	return (gunichar2) '/';	/* forward slash */
}

gunichar2
ves_icall_System_IO_MonoIO_get_PathSeparator ()
{
	return (gunichar2) ';';	/* semicolon */
}

void ves_icall_System_IO_MonoIO_DumpHandles (void)
{
	return;
}

gpointer
mono_w32file_create(const gunichar2 *name, guint32 fileaccess, guint32 sharemode, guint32 createmode, guint32 attrs)
{
	return CreateFile (name, fileaccess, sharemode, NULL, createmode, attrs, NULL);
}

gboolean
mono_w32file_delete (const gunichar2 *name)
{
	return DeleteFile (name);
}

gboolean
mono_w32file_read(gpointer handle, gpointer buffer, guint32 numbytes, guint32 *bytesread)
{
	return ReadFile (handle, buffer, numbytes, bytesread, NULL);
}

gboolean
mono_w32file_write (gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten)
{
	return WriteFile (handle, buffer, numbytes, byteswritten, NULL);
}

gboolean
mono_w32file_flush (gpointer handle)
{
	return FlushFileBuffers (handle);
}

gboolean
mono_w32file_truncate (gpointer handle)
{
	return SetEndOfFile (handle);
}

guint32
mono_w32file_seek (gpointer handle, gint32 movedistance, gint32 *highmovedistance, guint32 method)
{
	return SetFilePointer (handle, movedistance, highmovedistance, method);
}

gint
mono_w32file_get_type (gpointer handle)
{
	return GetFileType (handle);
}

gboolean
mono_w32file_get_times (gpointer handle, FILETIME *create_time, FILETIME *access_time, FILETIME *write_time)
{
	return GetFileTime (handle, create_time, access_time, write_time);
}

gboolean
mono_w32file_set_times (gpointer handle, const FILETIME *create_time, const FILETIME *access_time, const FILETIME *write_time)
{
	return SetFileTime (handle, create_time, access_time, write_time);
}

gboolean
mono_w32file_filetime_to_systemtime (const FILETIME *file_time, SYSTEMTIME *system_time)
{
	return FileTimeToSystemTime (file_time, system_time);
}

gpointer
mono_w32file_find_first (const gunichar2 *pattern, WIN32_FIND_DATA *find_data)
{
	return FindFirstFile (pattern, find_data);
}

gboolean
mono_w32file_find_next (gpointer handle, WIN32_FIND_DATA *find_data)
{
	return FindNextFile (handle, find_data);
}

gboolean
mono_w32file_find_close (gpointer handle)
{
	return FindClose (handle);
}

gboolean
mono_w32file_create_directory (const gunichar2 *name)
{
	return CreateDirectory (name, NULL);
}

gboolean
mono_w32file_remove_directory (const gunichar2 *name)
{
	return RemoveDirectory (name);
}

guint32
mono_w32file_get_attributes (const gunichar2 *name)
{
	return GetFileAttributes (name);
}

gboolean
mono_w32file_get_attributes_ex (const gunichar2 *name, MonoIOStat *stat)
{
	gboolean result;
	WIN32_FILE_ATTRIBUTE_DATA data;

	result = GetFileAttributesEx (name, GetFileExInfoStandard, &data);
	if (result) {
		stat->attributes = data.dwFileAttributes;
		stat->creation_time = (gint64) ((((guint64) data.ftCreationTime.dwHighDateTime) << 32) + data.ftCreationTime.dwLowDateTime);
		stat->last_access_time = (gint64) ((((guint64) data.ftLastAccessTime.dwHighDateTime) << 32) + data.ftLastAccessTime.dwLowDateTime);
		stat->last_write_time = (gint64) ((((guint64) data.ftLastWriteTime.dwHighDateTime) << 32) + data.ftLastWriteTime.dwLowDateTime);
		stat->length = ((gint64)data.nFileSizeHigh << 32) | data.nFileSizeLow;
	}

	return result;
}

gboolean
mono_w32file_set_attributes (const gunichar2 *name, guint32 attrs)
{
	return SetFileAttributes (name, attrs);
}

guint32
mono_w32file_get_cwd (guint32 length, gunichar2 *buffer)
{
	return GetCurrentDirectory (length, buffer);
}

gboolean
mono_w32file_set_cwd (const gunichar2 *path)
{
	return SetCurrentDirectory (path);
}

gboolean
mono_w32file_create_pipe (gpointer *readpipe, gpointer *writepipe, guint32 size)
{
	SECURITY_ATTRIBUTES attr;
	attr.nLength = sizeof(SECURITY_ATTRIBUTES);
	attr.bInheritHandle = TRUE;
	attr.lpSecurityDescriptor = NULL;
	return CreatePipe (readpipe, writepipe, &attr, size);
}

gboolean
mono_w32file_get_disk_free_space (const gunichar2 *path_name, guint64 *free_bytes_avail, guint64 *total_number_of_bytes, guint64 *total_number_of_free_bytes)
{
	gboolean result;
	ULARGE_INTEGER *wapi_free_bytes_avail;
	ULARGE_INTEGER *wapi_total_number_of_bytes;
	ULARGE_INTEGER *wapi_total_number_of_free_bytes;

	result = GetDiskFreeSpaceEx (path_name, wapi_free_bytes_avail, wapi_total_number_of_bytes, wapi_total_number_of_free_bytes);
	if (result) {
		if (free_bytes_avail)
			*free_bytes_avail = wapi_free_bytes_avail->QuadPart;
		if (total_number_of_bytes)
			*total_number_of_bytes = wapi_total_number_of_bytes->QuadPart;
		if (total_number_of_free_bytes)
			*total_number_of_free_bytes = wapi_total_number_of_free_bytes->QuadPart;
	}

	return result;
}

gboolean
mono_w32file_get_volume_information (const gunichar2 *path, gunichar2 *volumename, gint volumesize, gint *outserial, gint *maxcomp, gint *fsflags, gunichar2 *fsbuffer, gint fsbuffersize)
{
	return GetVolumeInformation (path, volumename, volumesize, outserial, maxcomp, fsflags, fsbuffer, fsbuffersize);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)

gboolean
mono_w32file_move (gunichar2 *path, gunichar2 *dest, gint32 *error)
{
	gboolean result;

	MONO_ENTER_GC_SAFE;

	result = MoveFile (path, dest);
	if (!result)
		*error = GetLastError ();

	MONO_EXIT_GC_SAFE;

	return result;
}

gboolean
mono_w32file_replace (gunichar2 *destinationFileName, gunichar2 *sourceFileName, gunichar2 *destinationBackupFileName, guint32 flags, gint32 *error)
{
	gboolean result;

	MONO_ENTER_GC_SAFE;

	result = ReplaceFile (destinationFileName, sourceFileName, destinationBackupFileName, flags, NULL, NULL);
	if (!result)
		*error = GetLastError ();

	MONO_EXIT_GC_SAFE;

	return result;
}

gboolean
mono_w32file_copy (gunichar2 *path, gunichar2 *dest, gboolean overwrite, gint32 *error)
{
	gboolean result;

	MONO_ENTER_GC_SAFE;

	result = CopyFile (path, dest, !overwrite);
	if (!result)
		*error = GetLastError ();

	MONO_EXIT_GC_SAFE;

	return result;
}

gboolean
mono_w32file_lock (gpointer handle, gint64 position, gint64 length, gint32 *error)
{
	gboolean result;

	MONO_ENTER_GC_SAFE;

	result = LockFile (handle, position & 0xFFFFFFFF, position >> 32, length & 0xFFFFFFFF, length >> 32);
	if (!result)
		*error = GetLastError ();

	MONO_EXIT_GC_SAFE;

	return result;
}

gboolean
mono_w32file_unlock (gpointer handle, gint64 position, gint64 length, gint32 *error)
{
	gboolean result;

	MONO_ENTER_GC_SAFE;

	result = UnlockFile (handle, position & 0xFFFFFFFF, position >> 32, length & 0xFFFFFFFF, length >> 32);
	if (!result)
		*error = GetLastError ();

	MONO_EXIT_GC_SAFE;

	return result;
}

HANDLE
mono_w32file_get_console_input (void)
{
	return GetStdHandle (STD_INPUT_HANDLE);
}

HANDLE
mono_w32file_get_console_output (void)
{
	return GetStdHandle (STD_OUTPUT_HANDLE);
}

HANDLE
mono_w32file_get_console_error (void)
{
	return GetStdHandle (STD_ERROR_HANDLE);
}

gint64
mono_w32file_get_file_size (gpointer handle, gint32 *error)
{
	gint64 length;
	guint32 length_hi;

	MONO_ENTER_GC_SAFE;

	length = GetFileSize (handle, &length_hi);
	if(length==INVALID_FILE_SIZE) {
		*error=GetLastError ();
	}

	MONO_EXIT_GC_SAFE;

	return length | ((gint64)length_hi << 32);
}

guint32
mono_w32file_get_drive_type (const gunichar2 *root_path_name)
{
	return GetDriveType (root_path_name);
}

gint32
mono_w32file_get_logical_drive (guint32 len, gunichar2 *buf)
{
	return GetLogicalDriveStrings (len, buf);
}

#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */
