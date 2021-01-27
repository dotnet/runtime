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
#include <mono/utils/w32subset.h>
#include "icall-decl.h"

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
	res = CreateFileW (name, fileaccess, sharemode, NULL, createmode, attrs, NULL);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_cancel (gpointer handle)
{
	return CancelIoEx (handle, NULL);
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
	res = DeleteFileW (name);
	MONO_EXIT_GC_SAFE;
	return res;
}

// See win32_wait_interrupt_handler for details.
static void
win32_io_interrupt_handler (gpointer ignored)
{
}

gboolean
mono_w32file_read(gpointer handle, gpointer buffer, guint32 numbytes, guint32 *bytesread, gint32 *win32error)
{
	gboolean res;
	MonoThreadInfo *info = mono_thread_info_current ();
	gboolean alerted = FALSE;

	if (info) {
		mono_thread_info_install_interrupt (win32_io_interrupt_handler, NULL, &alerted);
		if (alerted) {
			SetLastError (ERROR_OPERATION_ABORTED);
			*win32error = ERROR_OPERATION_ABORTED;
			return FALSE;
		}
		mono_win32_enter_blocking_io_call (info, handle);
	}

	MONO_ENTER_GC_SAFE;
	if (info && mono_thread_info_is_interrupt_state (info)) {
		res = FALSE;
		SetLastError (ERROR_OPERATION_ABORTED);
	} else {
		res = ReadFile (handle, buffer, numbytes, (PDWORD)bytesread, NULL);
	}
	if (!res)
		*win32error = GetLastError ();
	MONO_EXIT_GC_SAFE;

	if (info) {
		mono_win32_leave_blocking_io_call (info, handle);
		mono_thread_info_uninstall_interrupt (&alerted);
	}

	return res;
}

gboolean
mono_w32file_write (gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten, gint32 *win32error)
{
	gboolean res;
	MonoThreadInfo *info = mono_thread_info_current ();
	gboolean alerted = FALSE;

	if (info) {
		mono_thread_info_install_interrupt (win32_io_interrupt_handler, NULL, &alerted);
		if (alerted) {
			SetLastError (ERROR_OPERATION_ABORTED);
			*win32error = ERROR_OPERATION_ABORTED;
			return FALSE;
		}
		mono_win32_enter_blocking_io_call (info, handle);
	}

	MONO_ENTER_GC_SAFE;
	if (info && mono_thread_info_is_interrupt_state (info)) {
		res = FALSE;
		SetLastError (ERROR_OPERATION_ABORTED);
	} else {
		res = WriteFile (handle, buffer, numbytes, (PDWORD)byteswritten, NULL);
	}
	if (!res)
		*win32error = GetLastError ();
	MONO_EXIT_GC_SAFE;

	if (info) {
		mono_win32_leave_blocking_io_call (info, handle);
		mono_thread_info_uninstall_interrupt (&alerted);
	}

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
	res = SetFilePointer (handle, movedistance, (PLONG)highmovedistance, method);
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
mono_w32file_find_first (const gunichar2 *pattern, WIN32_FIND_DATAW *find_data)
{
	gpointer res;
	MONO_ENTER_GC_SAFE;
	res = FindFirstFileW (pattern, find_data);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_find_next (gpointer handle, WIN32_FIND_DATAW *find_data)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = FindNextFileW (handle, find_data);
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
	res = CreateDirectoryW (name, NULL);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_remove_directory (const gunichar2 *name)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = RemoveDirectoryW (name);
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
	HANDLE find_handle;
	WIN32_FIND_DATAW find_data;

	MONO_ENTER_GC_SAFE;

	res = GetFileAttributesW (name);
	if (res == INVALID_FILE_ATTRIBUTES && GetLastError () == ERROR_SHARING_VIOLATION) {
		find_handle = FindFirstFileW (name, &find_data);
		if (find_handle != INVALID_HANDLE_VALUE) {
			FindClose (find_handle);
			res = find_data.dwFileAttributes;
		} else {
			res = INVALID_FILE_ATTRIBUTES;
		}
	}

	MONO_EXIT_GC_SAFE;

	return res;
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
	HANDLE find_handle;
	WIN32_FIND_DATAW find_data;
	WIN32_FILE_ATTRIBUTE_DATA file_attribute_data;

	MONO_ENTER_GC_SAFE;

	res = GetFileAttributesExW (name, GetFileExInfoStandard, &file_attribute_data);
	if (res) {
		stat->attributes = file_attribute_data.dwFileAttributes;
		stat->creation_time = convert_filetime (&file_attribute_data.ftCreationTime);
		stat->last_access_time = convert_filetime (&file_attribute_data.ftLastAccessTime);
		stat->last_write_time = convert_filetime (&file_attribute_data.ftLastWriteTime);
		stat->length = ((gint64)file_attribute_data.nFileSizeHigh << 32) | file_attribute_data.nFileSizeLow;
	} else if (!res && GetLastError () == ERROR_SHARING_VIOLATION) {
		find_handle = FindFirstFileW (name, &find_data);
		if (find_handle != INVALID_HANDLE_VALUE) {
			FindClose (find_handle);
			stat->attributes = find_data.dwFileAttributes;
			stat->creation_time = convert_filetime (&find_data.ftCreationTime);
			stat->last_access_time = convert_filetime (&find_data.ftLastAccessTime);
			stat->last_write_time = convert_filetime (&find_data.ftLastWriteTime);
			stat->length = ((gint64)find_data.nFileSizeHigh << 32) | find_data.nFileSizeLow;
			res = TRUE;
		}
	}

	MONO_EXIT_GC_SAFE;

	return res;
}

gboolean
mono_w32file_set_attributes (const gunichar2 *name, guint32 attrs)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = SetFileAttributesW (name, attrs);
	MONO_EXIT_GC_SAFE;
	return res;
}

guint32
mono_w32file_get_cwd (guint32 length, gunichar2 *buffer)
{
	guint32 res;
	MONO_ENTER_GC_SAFE;
	res = GetCurrentDirectoryW (length, buffer);
	MONO_EXIT_GC_SAFE;
	return res;
}

gboolean
mono_w32file_set_cwd (const gunichar2 *path)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = SetCurrentDirectoryW (path);
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

#ifndef PLATFORM_NO_DRIVEINFO
gboolean
mono_w32file_get_disk_free_space (const gunichar2 *path_name, guint64 *free_bytes_avail, guint64 *total_number_of_bytes, guint64 *total_number_of_free_bytes)
{
	gboolean result;
	ULARGE_INTEGER wapi_free_bytes_avail = { 0 };
	ULARGE_INTEGER wapi_total_number_of_bytes = { 0 };
	ULARGE_INTEGER wapi_total_number_of_free_bytes = { 0 };

	g_assert (free_bytes_avail);
	g_assert (total_number_of_bytes);
	g_assert (total_number_of_free_bytes);

	MONO_ENTER_GC_SAFE;
	result = GetDiskFreeSpaceExW (path_name, &wapi_free_bytes_avail, &wapi_total_number_of_bytes, &wapi_total_number_of_free_bytes);
	MONO_EXIT_GC_SAFE;

	*free_bytes_avail = wapi_free_bytes_avail.QuadPart;
	*total_number_of_bytes = wapi_total_number_of_bytes.QuadPart;
	*total_number_of_free_bytes = wapi_total_number_of_free_bytes.QuadPart;

	return result;
}
#endif // PLATFORM_NO_DRIVEINFO

gboolean
mono_w32file_get_file_system_type (const gunichar2 *path, gunichar2 *fsbuffer, gint fsbuffersize)
{
	gboolean res;
	MONO_ENTER_GC_SAFE;
	res = GetVolumeInformationW (path, NULL, 0, NULL, NULL, NULL, fsbuffer, fsbuffersize);
	MONO_EXIT_GC_SAFE;
	return res;
}

#if HAVE_API_SUPPORT_WIN32_MOVE_FILE
gboolean
mono_w32file_move (const gunichar2 *path, const gunichar2 *dest, gint32 *error)
{
	gboolean result = FALSE;
	MONO_ENTER_GC_SAFE;

	result = MoveFileW (path, dest);
	if (!result)
		*error = GetLastError ();

	MONO_EXIT_GC_SAFE;
	return result;
}
#elif HAVE_API_SUPPORT_WIN32_MOVE_FILE_EX
gboolean
mono_w32file_move (const gunichar2 *path, const gunichar2 *dest, gint32 *error)
{
	gboolean result = FALSE;
	MONO_ENTER_GC_SAFE;

	result = MoveFileExW (path, dest, MOVEFILE_COPY_ALLOWED);
	if (!result) {
		*error = GetLastError ();
	}

	MONO_EXIT_GC_SAFE;
	return result;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_MOVE_FILE && !HAVE_EXTERN_DEFINED_WIN32_MOVE_FILE_EX
gboolean
mono_w32file_move (const gunichar2 *path, const gunichar2 *dest, gint32 *error)
{
	g_unsupported_api ("MoveFile, MoveFileEx");
	*error = ERROR_NOT_SUPPORTED;
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif

#if HAVE_API_SUPPORT_WIN32_REPLACE_FILE
gboolean
mono_w32file_replace (const gunichar2 *destination_file_name, const gunichar2 *source_file_name, const gunichar2 *destination_backup_file_name, guint32 flags, gint32 *error)
{
	gboolean result = FALSE;
	MONO_ENTER_GC_SAFE;

	result = ReplaceFileW (destination_file_name, source_file_name, destination_backup_file_name, flags, NULL, NULL);
	if (!result)
		*error = GetLastError ();

	MONO_EXIT_GC_SAFE;
	return result;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_REPLACE_FILE
gboolean
mono_w32file_replace (const gunichar2 *destination_file_name, const gunichar2 *source_file_name, const gunichar2 *destination_backup_file_name, guint32 flags, gint32 *error)
{
	g_unsupported_api ("ReplaceFile");
	*error = ERROR_NOT_SUPPORTED;
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif

#if HAVE_API_SUPPORT_WIN32_COPY_FILE

gboolean
mono_w32file_copy (const gunichar2 *path, const gunichar2 *dest, gboolean overwrite, gint32 *error)
{
	gboolean result = FALSE;
	MONO_ENTER_GC_SAFE;

	result = CopyFileW (path, dest, !overwrite);
	if (!result)
		*error = GetLastError ();

	MONO_EXIT_GC_SAFE;
	return result;
}
#elif HAVE_API_SUPPORT_WIN32_COPY_FILE2
gboolean
mono_w32file_copy (const gunichar2 *path, const gunichar2 *dest, gboolean overwrite, gint32 *error)
{
	gboolean result = FALSE;
	COPYFILE2_EXTENDED_PARAMETERS copy_param = {0};

	copy_param.dwSize = sizeof (COPYFILE2_EXTENDED_PARAMETERS);
	copy_param.dwCopyFlags = (!overwrite) ? COPY_FILE_FAIL_IF_EXISTS : 0;

	MONO_ENTER_GC_SAFE;

	result = SUCCEEDED (CopyFile2 (path, dest, &copy_param));
	if (result == FALSE) {
		*error=GetLastError ();
	}

	MONO_EXIT_GC_SAFE;
	return result;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_COPY_FILE && !HAVE_EXTERN_DEFINED_WIN32_COPY_FILE2
gboolean
mono_w32file_copy (const gunichar2 *path, const gunichar2 *dest, gboolean overwrite, gint32 *error)
{
	g_unsupported_api ("CopyFile, CopyFile2");
	*error = ERROR_NOT_SUPPORTED;
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif

#if HAVE_API_SUPPORT_WIN32_LOCK_FILE
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
#elif !HAVE_EXTERN_DEFINED_WIN32_LOCK_FILE
gboolean
mono_w32file_lock (gpointer handle, gint64 position, gint64 length, gint32 *error)
{
	g_unsupported_api ("LockFile");
	*error = ERROR_NOT_SUPPORTED;
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif

#if HAVE_API_SUPPORT_WIN32_UNLOCK_FILE
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
#elif !HAVE_EXTERN_DEFINED_WIN32_UNLOCK_FILE
gboolean
mono_w32file_unlock (gpointer handle, gint64 position, gint64 length, gint32 *error)
{
	g_unsupported_api ("UnlockFile");
	*error = ERROR_NOT_SUPPORTED;
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif

#if HAVE_API_SUPPORT_WIN32_GET_STD_HANDLE
gpointer
mono_w32file_get_console_input (void)
{
	HANDLE res;
	MONO_ENTER_GC_SAFE;
	res = GetStdHandle (STD_INPUT_HANDLE);
	MONO_EXIT_GC_SAFE;
	return res;
}

gpointer
mono_w32file_get_console_output (void)
{
	HANDLE res;
	MONO_ENTER_GC_SAFE;
	res = GetStdHandle (STD_OUTPUT_HANDLE);
	MONO_EXIT_GC_SAFE;
	return res;
}

gpointer
mono_w32file_get_console_error (void)
{
	HANDLE res;
	MONO_ENTER_GC_SAFE;
	res = GetStdHandle (STD_ERROR_HANDLE);
	MONO_EXIT_GC_SAFE;
	return res;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_STD_HANDLE
gpointer
mono_w32file_get_console_input (void)
{
	g_unsupported_api ("GetStdHandle (STD_INPUT_HANDLE)");
	SetLastError (ERROR_NOT_SUPPORTED);
	return INVALID_HANDLE_VALUE;
}

gpointer
mono_w32file_get_console_output (void)
{
	g_unsupported_api ("GetStdHandle (STD_OUTPUT_HANDLE)");
	SetLastError (ERROR_NOT_SUPPORTED);
	return INVALID_HANDLE_VALUE;
}

gpointer
mono_w32file_get_console_error (void)
{
	g_unsupported_api ("GetStdHandle (STD_ERROR_HANDLE)");
	SetLastError (ERROR_NOT_SUPPORTED);
	return INVALID_HANDLE_VALUE;
}
#endif

#if HAVE_API_SUPPORT_WIN32_GET_FILE_SIZE_EX
gint64
mono_w32file_get_file_size (HANDLE handle, gint32 *error)
{
	LARGE_INTEGER length;

	MONO_ENTER_GC_SAFE;

	if (!GetFileSizeEx (handle, &length)) {
		*error=GetLastError ();
		length.QuadPart = INVALID_FILE_SIZE;
	}

	MONO_EXIT_GC_SAFE;
	return length.QuadPart;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_FILE_SIZE_EX
gint64
mono_w32file_get_file_size (HANDLE handle, gint32 *error)
{
	g_unsupported_api ("GetFileSizeEx");
	*error = ERROR_NOT_SUPPORTED;
	SetLastError (ERROR_NOT_SUPPORTED);
	return 0;
}
#endif

#if HAVE_API_SUPPORT_WIN32_GET_DRIVE_TYPE
guint32
mono_w32file_get_drive_type (const gunichar2 *root_path_name, gint32 root_path_name_length, MonoError *error)
{
	guint32 res;
	MONO_ENTER_GC_SAFE;
	res = GetDriveTypeW (root_path_name);
	MONO_EXIT_GC_SAFE;
	return res;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_DRIVE_TYPE
guint32
mono_w32file_get_drive_type (const gunichar2 *root_path_name, gint32 root_path_name_length, MonoError *error)
{
	g_unsupported_api ("GetDriveType");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "GetDriveType");
	SetLastError (ERROR_NOT_SUPPORTED);
	return DRIVE_UNKNOWN;
}
#endif

#if HAVE_API_SUPPORT_WIN32_GET_LOGICAL_DRIVE_STRINGS
gint32
mono_w32file_get_logical_drive (guint32 len, gunichar2 *buf, MonoError *error)
{
	gint32 res;
	MONO_ENTER_GC_SAFE;
	res = GetLogicalDriveStringsW (len, buf);
	MONO_EXIT_GC_SAFE;
	return res;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_GET_LOGICAL_DRIVE_STRINGS
gint32
mono_w32file_get_logical_drive (guint32 len, gunichar2 *buf, MonoError *error)
{
	g_unsupported_api ("GetLogicalDriveStrings");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "GetLogicalDriveStrings");
	SetLastError (ERROR_NOT_SUPPORTED);
	return 0;
}
#endif
