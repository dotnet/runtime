/**
 * \file
 * Windows File IO internal calls.
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
	gpointer res;
	MONO_ENTER_GC_SAFE;
	res = CreateFile (name, fileaccess, sharemode, NULL, createmode, attrs, NULL);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_close (gpointer handle)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = CloseHandle (handle);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_delete (const gunichar2 *name)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = DeleteFile (name);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_read(gpointer handle, gpointer buffer, guint32 numbytes, guint32 *bytesread)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = ReadFile (handle, buffer, numbytes, bytesread, NULL);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_write (gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = WriteFile (handle, buffer, numbytes, byteswritten, NULL);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_flush (gpointer handle)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = FlushFileBuffers (handle);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_truncate (gpointer handle)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = SetEndOfFile (handle);
	MONO_EXIT_GC_SAFE;
	return res;
}

guint32
mono_w32file_seek (gpointer handle, gint32 movedistance, gint32 *highmovedistance, guint32 method)
{
	guint32 res;
	MONO_ENTER_GC_SAFE;
	res = SetFilePointer (handle, movedistance, highmovedistance, method);
	MONO_EXIT_GC_SAFE;
	return res;
}

gint
mono_w32file_get_type (gpointer handle)
{
	gint res;
	MONO_ENTER_GC_SAFE;
	res = GetFileType (handle);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_get_times (gpointer handle, FILETIME *create_time, FILETIME *access_time, FILETIME *write_time)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = GetFileTime (handle, create_time, access_time, write_time);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_set_times (gpointer handle, const FILETIME *create_time, const FILETIME *access_time, const FILETIME *write_time)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = SetFileTime (handle, create_time, access_time, write_time);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_filetime_to_systemtime (const FILETIME *file_time, SYSTEMTIME *system_time)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = FileTimeToSystemTime (file_time, system_time);
	MONO_EXIT_GC_SAFE;
	return res;
}

gpointer
mono_w32file_find_first (const gunichar2 *pattern, WIN32_FIND_DATA *find_data)
{
	gpointer res;
	MONO_ENTER_GC_SAFE;
	res = FindFirstFile (pattern, find_data);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_find_next (gpointer handle, WIN32_FIND_DATA *find_data)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = FindNextFile (handle, find_data);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_find_close (gpointer handle)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = FindClose (handle);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_create_directory (const gunichar2 *name)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = CreateDirectory (name, NULL);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_remove_directory (const gunichar2 *name)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = RemoveDirectory (name);
	MONO_EXIT_GC_SAFE;
	return res;
}

/*
 * GetFileAttributes|Ex () seems to try opening the file, which might lead to sharing violation errors, whereas
 * FindFirstFile always succeeds.
 */
guint32
mono_w32file_get_attributes (const gunichar2 *name)
{
	guint32 res;
	guint32 error;
	HANDLE find_handle;
	WIN32_FIND_DATA find_data;

	MONO_ENTER_GC_SAFE;
	res = GetFileAttributes (name);
	MONO_EXIT_GC_SAFE;

	if (res != INVALID_FILE_ATTRIBUTES)
		return res;

	error = GetLastError ();
	if (error != ERROR_SHARING_VIOLATION)
		return INVALID_FILE_ATTRIBUTES;

	MONO_ENTER_GC_SAFE;
	find_handle = FindFirstFile (name, &find_data);
	MONO_EXIT_GC_SAFE;

	if (find_handle == INVALID_HANDLE_VALUE)
		return INVALID_FILE_ATTRIBUTES;

	MONO_ENTER_GC_SAFE;
	FindClose (find_handle);
	MONO_EXIT_GC_SAFE;

	return find_data.dwFileAttributes;
}

static gint64
convert_filetime (const FILETIME *filetime)
{
	return (gint64) ((((guint64) filetime->dwHighDateTime) << 32) + filetime->dwLowDateTime);
}

gboolean
mono_w32file_get_attributes_ex (const gunichar2 *name, MonoIOStat *stat)
{
	gboolean res;
	guint32 error;
	HANDLE find_handle;
	WIN32_FIND_DATA find_data;
	WIN32_FILE_ATTRIBUTE_DATA file_attribute_data;

	MONO_ENTER_GC_SAFE;
	res = GetFileAttributesEx (name, GetFileExInfoStandard, &file_attribute_data);
	MONO_EXIT_GC_SAFE;
	if (res) {
		stat->attributes = file_attribute_data.dwFileAttributes;
		stat->creation_time = convert_filetime (&file_attribute_data.ftCreationTime);
		stat->last_access_time = convert_filetime (&file_attribute_data.ftLastAccessTime);
		stat->last_write_time = convert_filetime (&file_attribute_data.ftLastWriteTime);
		stat->length = ((gint64)file_attribute_data.nFileSizeHigh << 32) | file_attribute_data.nFileSizeLow;
		return TRUE;
	}

	error = GetLastError ();
	if (error != ERROR_SHARING_VIOLATION)
		return FALSE;

	MONO_ENTER_GC_SAFE;
	find_handle = FindFirstFile (name, &find_data);
	MONO_EXIT_GC_SAFE;

	if (find_handle == INVALID_HANDLE_VALUE)
		return FALSE;

	MONO_ENTER_GC_SAFE;
	FindClose (find_handle);
	MONO_EXIT_GC_SAFE;

	stat->attributes = find_data.dwFileAttributes;
	stat->creation_time = convert_filetime (&find_data.ftCreationTime);
	stat->last_access_time = convert_filetime (&find_data.ftLastAccessTime);
	stat->last_write_time = convert_filetime (&find_data.ftLastWriteTime);
	stat->length = ((gint64)find_data.nFileSizeHigh << 32) | find_data.nFileSizeLow;
	return TRUE;
}

gboolean
mono_w32file_set_attributes (const gunichar2 *name, guint32 attrs)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = SetFileAttributes (name, attrs);
	MONO_EXIT_GC_SAFE;
	return res;
}

guint32
mono_w32file_get_cwd (guint32 length, gunichar2 *buffer)
{
	guint32 res;
	MONO_ENTER_GC_SAFE;
	res = GetCurrentDirectory (length, buffer);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_set_cwd (const gunichar2 *path)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = SetCurrentDirectory (path);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_create_pipe (gpointer *readpipe, gpointer *writepipe, guint32 size)
{
	gboolean res;
	SECURITY_ATTRIBUTES attr;
	attr.nLength = sizeof(SECURITY_ATTRIBUTES);
	attr.bInheritHandle = TRUE;
	attr.lpSecurityDescriptor = NULL;
	MONO_ENTER_GC_SAFE;
	res = CreatePipe (readpipe, writepipe, &attr, size);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_get_disk_free_space (const gunichar2 *path_name, guint64 *free_bytes_avail, guint64 *total_number_of_bytes, guint64 *total_number_of_free_bytes)
{
	gboolean result;
	ULARGE_INTEGER wapi_free_bytes_avail;
	ULARGE_INTEGER wapi_total_number_of_bytes;
	ULARGE_INTEGER wapi_total_number_of_free_bytes;

	MONO_ENTER_GC_SAFE;
	result = GetDiskFreeSpaceEx (path_name, &wapi_free_bytes_avail, &wapi_total_number_of_bytes, &wapi_total_number_of_free_bytes);
	MONO_EXIT_GC_SAFE;
	if (result) {
		if (free_bytes_avail)
			*free_bytes_avail = wapi_free_bytes_avail.QuadPart;
		if (total_number_of_bytes)
			*total_number_of_bytes = wapi_total_number_of_bytes.QuadPart;
		if (total_number_of_free_bytes)
			*total_number_of_free_bytes = wapi_total_number_of_free_bytes.QuadPart;
	}

	return result;
}

gboolean
mono_w32file_get_volume_information (const gunichar2 *path, gunichar2 *volumename, gint volumesize, gint *outserial, gint *maxcomp, gint *fsflags, gunichar2 *fsbuffer, gint fsbuffersize)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = GetVolumeInformation (path, volumename, volumesize, outserial, maxcomp, fsflags, fsbuffer, fsbuffersize);
	MONO_EXIT_GC_SAFE;
	return res;
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)

gboolean
mono_w32file_move (const gunichar2 *path, const gunichar2 *dest, gint32 *error)
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
mono_w32file_replace (const gunichar2 *destinationFileName, const gunichar2 *sourceFileName, const gunichar2 *destinationBackupFileName, guint32 flags, gint32 *error)
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
mono_w32file_copy (const gunichar2 *path, const gunichar2 *dest, gboolean overwrite, gint32 *error)
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
	HANDLE res;
	MONO_ENTER_GC_SAFE;
	res = GetStdHandle (STD_INPUT_HANDLE);
	MONO_EXIT_GC_SAFE;
	return res;
}

HANDLE
mono_w32file_get_console_output (void)
{
	HANDLE res;
	MONO_ENTER_GC_SAFE;
	res = GetStdHandle (STD_OUTPUT_HANDLE);
	MONO_EXIT_GC_SAFE;
	return res;
}

HANDLE
mono_w32file_get_console_error (void)
{
	HANDLE res;
	MONO_ENTER_GC_SAFE;
	res = GetStdHandle (STD_ERROR_HANDLE);
	MONO_EXIT_GC_SAFE;
	return res;
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
	guint32 res;
	MONO_ENTER_GC_SAFE;
	res = GetDriveType (root_path_name);
	MONO_EXIT_GC_SAFE;
	return res;
}

gint32
mono_w32file_get_logical_drive (guint32 len, gunichar2 *buf)
{
	gint32 res;
	MONO_ENTER_GC_SAFE;
	res = GetLogicalDriveStrings (len, buf);
	MONO_EXIT_GC_SAFE;
	return res;
}

#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */
