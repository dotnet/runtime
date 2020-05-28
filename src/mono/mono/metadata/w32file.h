/**
 * \file
 * File IO internal calls
 *
 * Authors:
 *	Dick Porter (dick@ximian.com)
 *	Dan Lewis (dihlewis@yahoo.co.uk)
 *
 * (C) 2001 Ximian, Inc.
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _MONO_METADATA_W32FILE_H_
#define _MONO_METADATA_W32FILE_H_

#include <config.h>
#include <glib.h>

#include <mono/metadata/object-internals.h>
#include <mono/utils/mono-compiler.h>
#include <mono/metadata/icalls.h>

/* This is a copy of System.IO.FileAccess */
typedef enum {
	FileAccess_Read=0x01,
	FileAccess_Write=0x02,
	FileAccess_ReadWrite=FileAccess_Read|FileAccess_Write
} MonoFileAccess;

/* This is a copy of System.IO.FileMode */
typedef enum {
	FileMode_CreateNew=1,
	FileMode_Create=2,
	FileMode_Open=3,
	FileMode_OpenOrCreate=4,
	FileMode_Truncate=5,
	FileMode_Append=6
} MonoFileMode;

/* This is a copy of System.IO.FileShare */
typedef enum {
	FileShare_None=0x0,
	FileShare_Read=0x01,
	FileShare_Write=0x02,
	FileShare_ReadWrite=FileShare_Read|FileShare_Write,
	FileShare_Delete=0x04
} MonoFileShare;

/* This is a copy of System.IO.FileOptions */
typedef enum {
	FileOptions_None = 0,
	FileOptions_Temporary = 1,		// Internal.   See note in System.IO.FileOptions
	FileOptions_Encrypted = 0x4000,
	FileOptions_DeleteOnClose = 0x4000000,
	FileOptions_SequentialScan = 0x8000000,
	FileOptions_RandomAccess = 0x10000000,
	FileOptions_Asynchronous = 0x40000000,
	FileOptions_WriteThrough = 0x80000000
} MonoFileOptions;

/* This is a copy of System.IO.SeekOrigin */
typedef enum {
	SeekOrigin_Begin=0,
	SeekOrigin_Current=1,
	SeekOrigin_End=2
} MonoSeekOrigin;

/* This is a copy of System.IO.MonoIOStat */
typedef struct _MonoIOStat {
	gint32 attributes;
	gint64 length;
	gint64 creation_time;
	gint64 last_access_time;
	gint64 last_write_time;
} MonoIOStat;

/* This is a copy of System.IO.FileAttributes */
typedef enum {
	FileAttributes_ReadOnly=0x00001,
	FileAttributes_Hidden=0x00002,
	FileAttributes_System=0x00004,
	FileAttributes_Directory=0x00010,
	FileAttributes_Archive=0x00020,
	FileAttributes_Device=0x00040,
	FileAttributes_Normal=0x00080,
	FileAttributes_Temporary=0x00100,
	FileAttributes_SparseFile=0x00200,
	FileAttributes_ReparsePoint=0x00400,
	FileAttributes_Compressed=0x00800,
	FileAttributes_Offline=0x01000,
	FileAttributes_NotContentIndexed=0x02000,
	FileAttributes_Encrypted=0x04000,
	FileAttributes_MonoExecutable= (int) 0x80000000
} MonoFileAttributes;
/* This is not used anymore
typedef struct _MonoFSAsyncResult {
	MonoObject obj;
	MonoObject *state;
	MonoBoolean completed;
	MonoBoolean done;
	MonoException *exc;
	MonoWaitHandle *wait_handle;
	MonoDelegate *async_callback;
	MonoBoolean completed_synch;
	MonoArray *buffer;
	gint offset;
	gint count;
	gint original_count;
	gint bytes_read;
	MonoDelegate *real_cb;
} MonoFSAsyncResult;
*/
/* System.IO.MonoIO */

#if !ENABLE_NETCORE

ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_CreateDirectory (const gunichar2 *path, gint32 *error);

ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_RemoveDirectory (const gunichar2 *path, gint32 *error);

ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_FindCloseFile (gpointer hnd);

ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_SetCurrentDirectory (const gunichar2 *path,
						gint32 *error);
ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_MoveFile (const gunichar2 *path, const gunichar2 *dest,
				     gint32 *error);
ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_CopyFile (const gunichar2 *path, const gunichar2 *dest,
				     MonoBoolean overwrite, gint32 *error);
ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_DeleteFile (const gunichar2 *path, gint32 *error);

ICALL_EXPORT
gint32
ves_icall_System_IO_MonoIO_GetFileAttributes (const gunichar2 *path, gint32 *error);

ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_SetFileAttributes (const gunichar2 *path, gint32 attrs,
					      gint32 *error);
ICALL_EXPORT
gint32
ves_icall_System_IO_MonoIO_GetFileType (gpointer handle, gint32 *error);

ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_GetFileStat (const gunichar2 *path, MonoIOStat *stat,
					gint32 *error);
ICALL_EXPORT
gpointer
ves_icall_System_IO_MonoIO_Open (const gunichar2 *filename, gint32 mode,
				 gint32 access_mode, gint32 share, gint32 options,
				 gint32 *error);

ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_Cancel (gpointer handle, gint32 *error);

ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_Close (gpointer handle, gint32 *error);

ICALL_EXPORT
gint64
ves_icall_System_IO_MonoIO_Seek (gpointer handle, gint64 offset, gint32 origin,
				 gint32 *error);
ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_Flush (gpointer handle, gint32 *error);

ICALL_EXPORT
gint64
ves_icall_System_IO_MonoIO_GetLength (gpointer handle, gint32 *error);

ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_SetLength (gpointer handle, gint64 length,
				      gint32 *error);
ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_SetFileTime (gpointer handle, gint64 creation_time,
					gint64 last_access_time,
					gint64 last_write_time, gint32 *error);
ICALL_EXPORT
gpointer
ves_icall_System_IO_MonoIO_get_ConsoleOutput (void);

ICALL_EXPORT
gpointer
ves_icall_System_IO_MonoIO_get_ConsoleInput (void);

ICALL_EXPORT
gpointer
ves_icall_System_IO_MonoIO_get_ConsoleError (void);

ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_CreatePipe (gpointer *read_handle, gpointer *write_handle, gint32 *error);

ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_DuplicateHandle (gpointer source_process_handle, gpointer source_handle,
		gpointer target_process_handle, gpointer *target_handle, gint32 access, gint32 inherit, gint32 options, gint32 *error);

ICALL_EXPORT
gunichar2
ves_icall_System_IO_MonoIO_get_VolumeSeparatorChar (void);

ICALL_EXPORT
gunichar2
ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar (void);

ICALL_EXPORT
gunichar2
ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar (void);

ICALL_EXPORT
gunichar2
ves_icall_System_IO_MonoIO_get_PathSeparator (void);

ICALL_EXPORT
void ves_icall_System_IO_MonoIO_Lock (gpointer handle, gint64 position,
					     gint64 length, gint32 *error);

ICALL_EXPORT
void ves_icall_System_IO_MonoIO_Unlock (gpointer handle, gint64 position,
					       gint64 length, gint32 *error);
ICALL_EXPORT
MonoBoolean
ves_icall_System_IO_MonoIO_ReplaceFile (const gunichar2 *source_file_name, const gunichar2 *destination_file_name,
					const gunichar2 *destination_backup_file_name, MonoBoolean ignore_metadata_errors,
					gint32 *error);

ICALL_EXPORT
void
ves_icall_System_IO_MonoIO_DumpHandles (void);

#endif /* !ENABLE_NETCORE */

#if defined (TARGET_IOS) || defined (TARGET_ANDROID)

MONO_API MONO_RT_EXTERNAL_ONLY gint64
mono_filesize_from_path (MonoString *path);

extern gint64
mono_filesize_from_fd (int fd);

#endif

#if !defined(HOST_WIN32)

#define GENERIC_READ    0x80000000
#define GENERIC_WRITE   0x40000000
#define GENERIC_EXECUTE 0x20000000
#define GENERIC_ALL     0x10000000

#define FILE_SHARE_NONE   0x00000000
#define FILE_SHARE_READ   0x00000001
#define FILE_SHARE_WRITE  0x00000002
#define FILE_SHARE_DELETE 0x00000004

#define CREATE_NEW        1
#define CREATE_ALWAYS     2
#define OPEN_EXISTING     3
#define OPEN_ALWAYS       4
#define TRUNCATE_EXISTING 5

#define FILE_ATTRIBUTE_READONLY            0x00000001
#define FILE_ATTRIBUTE_HIDDEN              0x00000002
#define FILE_ATTRIBUTE_SYSTEM              0x00000004
#define FILE_ATTRIBUTE_DIRECTORY           0x00000010
#define FILE_ATTRIBUTE_ARCHIVE             0x00000020
#define FILE_ATTRIBUTE_ENCRYPTED           0x00000040
#define FILE_ATTRIBUTE_NORMAL              0x00000080
#define FILE_ATTRIBUTE_TEMPORARY           0x00000100
#define FILE_ATTRIBUTE_SPARSE_FILE         0x00000200
#define FILE_ATTRIBUTE_REPARSE_POINT       0x00000400
#define FILE_ATTRIBUTE_COMPRESSED          0x00000800
#define FILE_ATTRIBUTE_OFFLINE             0x00001000
#define FILE_ATTRIBUTE_NOT_CONTENT_INDEXED 0x00002000
#define FILE_FLAG_OPEN_NO_RECALL           0x00100000
#define FILE_FLAG_OPEN_REPARSE_POINT       0x00200000
#define FILE_FLAG_POSIX_SEMANTICS          0x01000000
#define FILE_FLAG_BACKUP_SEMANTICS         0x02000000
#define FILE_FLAG_DELETE_ON_CLOSE          0x04000000
#define FILE_FLAG_SEQUENTIAL_SCAN          0x08000000
#define FILE_FLAG_RANDOM_ACCESS            0x10000000
#define FILE_FLAG_NO_BUFFERING             0x20000000
#define FILE_FLAG_OVERLAPPED               0x40000000
#define FILE_FLAG_WRITE_THROUGH            0x80000000

#define REPLACEFILE_WRITE_THROUGH       0x00000001
#define REPLACEFILE_IGNORE_MERGE_ERRORS 0x00000002

#define MAX_PATH 260

#define INVALID_SET_FILE_POINTER ((guint32) 0xFFFFFFFF)
#define INVALID_FILE_SIZE        ((guint32) 0xFFFFFFFF)
#define INVALID_FILE_ATTRIBUTES  ((guint32) 0xFFFFFFFF)

#define FILE_TYPE_UNKNOWN 0x0000
#define FILE_TYPE_DISK    0x0001
#define FILE_TYPE_CHAR    0x0002
#define FILE_TYPE_PIPE    0x0003
#define FILE_TYPE_REMOTE  0x8000

#define FILE_BEGIN   0
#define FILE_CURRENT 1
#define FILE_END     2

#define DRIVE_UNKNOWN     0
#define DRIVE_NO_ROOT_DIR 1
#define DRIVE_REMOVABLE   2
#define DRIVE_FIXED       3
#define DRIVE_REMOTE      4
#define DRIVE_CDROM       5
#define DRIVE_RAMDISK     6

typedef struct {
	guint16 wYear;
	guint16 wMonth;
	guint16 wDayOfWeek;
	guint16 wDay;
	guint16 wHour;
	guint16 wMinute;
	guint16 wSecond;
	guint16 wMilliseconds;
} SYSTEMTIME;

typedef struct {
#if G_BYTE_ORDER == G_BIG_ENDIAN
	guint32 dwHighDateTime;
	guint32 dwLowDateTime;
#else
	guint32 dwLowDateTime;
	guint32 dwHighDateTime;
#endif
} FILETIME;

typedef struct {
	guint32 dwFileAttributes;
	FILETIME ftCreationTime;
	FILETIME ftLastAccessTime;
	FILETIME ftLastWriteTime;
	guint32 nFileSizeHigh;
	guint32 nFileSizeLow;
	guint32 dwReserved0;
	guint32 dwReserved1;
	gunichar2 cFileName [MAX_PATH];
	gunichar2 cAlternateFileName [14];
} WIN32_FIND_DATA;

#endif /* !defined(HOST_WIN32) */

void
mono_w32file_init (void);

void
mono_w32file_cleanup (void);

gpointer
mono_w32file_create(const gunichar2 *name, guint32 fileaccess, guint32 sharemode, guint32 createmode, guint32 attrs);

gboolean
mono_w32file_cancel (gpointer handle);

gboolean
mono_w32file_close (gpointer handle);

gboolean
mono_w32file_delete (const gunichar2 *name);

gboolean
mono_w32file_read (gpointer handle, gpointer buffer, guint32 numbytes, guint32 *bytesread, gint32 *win32error);

gboolean
mono_w32file_write (gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten, gint32 *win32error);

gboolean
mono_w32file_flush (gpointer handle);

gboolean
mono_w32file_truncate (gpointer handle);

guint32
mono_w32file_seek (gpointer handle, gint32 movedistance, gint32 *highmovedistance, guint32 method);

gboolean
mono_w32file_move (const gunichar2 *path, const gunichar2 *dest, gint32 *error);

gboolean
mono_w32file_copy (const gunichar2 *path, const gunichar2 *dest, gboolean overwrite, gint32 *error);

gboolean
mono_w32file_lock (gpointer handle, gint64 position, gint64 length, gint32 *error);

gboolean
mono_w32file_replace (const gunichar2 *destination_file_name, const gunichar2 *source_file_name, const gunichar2 *destination_backup_file_name, guint32 flags, gint32 *error);

gboolean
mono_w32file_unlock (gpointer handle, gint64 position, gint64 length, gint32 *error);

gpointer
mono_w32file_get_console_output (void);

gpointer
mono_w32file_get_console_error (void);

gpointer
mono_w32file_get_console_input (void);

gint64
mono_w32file_get_file_size (gpointer handle, gint32 *error);

gint
mono_w32file_get_type (gpointer handle);

gboolean
mono_w32file_set_times (gpointer handle, const FILETIME *create_time, const FILETIME *access_time, const FILETIME *write_time);

gboolean
mono_w32file_filetime_to_systemtime (const FILETIME *file_time, SYSTEMTIME *system_time);

gpointer
mono_w32file_find_first (const gunichar2 *pattern, WIN32_FIND_DATA *find_data);

gboolean
mono_w32file_find_next (gpointer handle, WIN32_FIND_DATA *find_data);

gboolean
mono_w32file_find_close (gpointer handle);

gboolean
mono_w32file_create_directory (const gunichar2 *name);

gboolean
mono_w32file_remove_directory (const gunichar2 *name);

guint32
mono_w32file_get_attributes (const gunichar2 *name);

gboolean
mono_w32file_get_attributes_ex (const gunichar2 *name, MonoIOStat *stat);

gboolean
mono_w32file_set_attributes (const gunichar2 *name, guint32 attrs);

guint32
mono_w32file_get_cwd (guint32 length, gunichar2 *buffer);

gboolean
mono_w32file_set_cwd (const gunichar2 *path);

gboolean
mono_w32file_create_pipe (gpointer *readpipe, gpointer *writepipe, guint32 size);

gint32
mono_w32file_get_logical_drive (guint32 len, gunichar2 *buf);

#ifndef PLATFORM_NO_DRIVEINFO
gboolean
mono_w32file_get_disk_free_space (const gunichar2 *path_name, guint64 *free_bytes_avail, guint64 *total_number_of_bytes, guint64 *total_number_of_free_bytes);
#endif // PLATFORM_NO_DRIVEINFO

gboolean
mono_w32file_get_file_system_type (const gunichar2 *path, gunichar2 *fsbuffer, gint fsbuffersize);

#endif /* _MONO_METADATA_W32FILE_H_ */
