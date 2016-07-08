/*
 * file-io.h: File IO internal calls
 *
 * Authors:
 *	Dick Porter (dick@ximian.com)
 *	Dan Lewis (dihlewis@yahoo.co.uk)
 *
 * (C) 2001 Ximian, Inc.
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _MONO_METADATA_FILEIO_H_
#define _MONO_METADATA_FILEIO_H_

#include <config.h>
#include <glib.h>

#include <mono/metadata/object-internals.h>
#include <mono/io-layer/io-layer.h>
#include <mono/utils/mono-compiler.h>

G_BEGIN_DECLS

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

extern MonoBoolean
ves_icall_System_IO_MonoIO_CreateDirectory (MonoString *path, gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_RemoveDirectory (MonoString *path, gint32 *error);

MonoArray *
ves_icall_System_IO_MonoIO_GetFileSystemEntries (MonoString *path,
						 MonoString *path_with_pattern,
						 gint mask, gint attrs,
						 gint32 *error);

extern MonoString *
ves_icall_System_IO_MonoIO_FindFirst (MonoString *path,
				      MonoString *path_with_pattern,
				      gint32 *result_mask,
				      gint32 *error,
				      gpointer *handle);
extern MonoString *
ves_icall_System_IO_MonoIO_FindNext (gpointer handle, gint32 *result_mask, gint32 *error);

extern int
ves_icall_System_IO_MonoIO_FindClose (gpointer handle);

extern MonoString *
ves_icall_System_IO_MonoIO_GetCurrentDirectory (gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_SetCurrentDirectory (MonoString *path,
						gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_MoveFile (MonoString *path, MonoString *dest,
				     gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_CopyFile (MonoString *path, MonoString *dest,
				     MonoBoolean overwrite, gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_DeleteFile (MonoString *path, gint32 *error);

extern gint32 
ves_icall_System_IO_MonoIO_GetFileAttributes (MonoString *path, gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_SetFileAttributes (MonoString *path, gint32 attrs,
					      gint32 *error);

extern gint32
ves_icall_System_IO_MonoIO_GetFileType (HANDLE handle, gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_GetFileStat (MonoString *path, MonoIOStat *stat,
					gint32 *error);

extern HANDLE 
ves_icall_System_IO_MonoIO_Open (MonoString *filename, gint32 mode,
				 gint32 access_mode, gint32 share, gint32 options,
				 gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_Close (HANDLE handle, gint32 *error);

extern gint32 
ves_icall_System_IO_MonoIO_Read (HANDLE handle, MonoArray *dest,
				 gint32 dest_offset, gint32 count,
				 gint32 *error);

extern gint32 
ves_icall_System_IO_MonoIO_Write (HANDLE handle, MonoArray *src,
				  gint32 src_offset, gint32 count,
				  gint32 *error);

extern gint64 
ves_icall_System_IO_MonoIO_Seek (HANDLE handle, gint64 offset, gint32 origin,
				 gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_Flush (HANDLE handle, gint32 *error);

extern gint64 
ves_icall_System_IO_MonoIO_GetLength (HANDLE handle, gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_SetLength (HANDLE handle, gint64 length,
				      gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_SetFileTime (HANDLE handle, gint64 creation_time,
					gint64 last_access_time,
					gint64 last_write_time, gint32 *error);

extern HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleOutput (void);

extern HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleInput (void);

extern HANDLE 
ves_icall_System_IO_MonoIO_get_ConsoleError (void);

extern MonoBoolean
ves_icall_System_IO_MonoIO_CreatePipe (HANDLE *read_handle, HANDLE *write_handle, gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_DuplicateHandle (HANDLE source_process_handle, HANDLE source_handle,
		HANDLE target_process_handle, HANDLE *target_handle, gint32 access, gint32 inherit, gint32 options, gint32 *error);

extern gunichar2 
ves_icall_System_IO_MonoIO_get_VolumeSeparatorChar (void);

extern gunichar2 
ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar (void);

extern gunichar2 
ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar (void);

extern gunichar2 
ves_icall_System_IO_MonoIO_get_PathSeparator (void);

extern MonoArray *
ves_icall_System_IO_MonoIO_get_InvalidPathChars (void);

extern void ves_icall_System_IO_MonoIO_Lock (HANDLE handle, gint64 position,
					     gint64 length, gint32 *error);
extern void ves_icall_System_IO_MonoIO_Unlock (HANDLE handle, gint64 position,
					       gint64 length, gint32 *error);

extern MonoBoolean
ves_icall_System_IO_MonoIO_ReplaceFile (MonoString *sourceFileName, MonoString *destinationFileName,
					MonoString *destinationBackupFileName, MonoBoolean ignoreMetadataErrors,
					gint32 *error);

MONO_RT_EXTERNAL_ONLY
extern gint64
mono_filesize_from_path (MonoString *path);

extern gint64
mono_filesize_from_fd (int fd);

void
ves_icall_System_IO_MonoIO_DumpHandles (void);

G_END_DECLS

#endif /* _MONO_METADATA_FILEIO_H_ */
