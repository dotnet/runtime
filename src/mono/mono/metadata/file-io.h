/*
 * file-io.h: File IO internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_METADATA_FILEIO_H_
#define _MONO_METADATA_FILEIO_H_

#include <config.h>
#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/io-layer/io-layer.h>

/* This is a copy of System.IO.FileAccess */
typedef enum {
	FileAccess_Read=0x01,
	FileAccess_Write=0x02,
	FileAccess_ReadWrite=FileAccess_Read|FileAccess_Write,
} MonoFileAccess;

/* This is a copy of System.IO.FileMode */
typedef enum {
	FileMode_CreateNew=1,
	FileMode_Create=2,
	FileMode_Open=3,
	FileMode_OpenOrCreate=4,
	FileMode_Truncate=5,
	FileMode_Append=6,
} MonoFileMode;

/* This is a copy of System.IO.FileShare */
typedef enum {
	FileShare_None=0x0,
	FileShare_Read=0x01,
	FileShare_Write=0x02,
	FileShare_ReadWrite=FileShare_Read|FileShare_Write,
} MonoFileShare;

/* This is a copy of System.IO.SeekOrigin */
typedef enum {
	SeekOrigin_Begin=0,
	SeekOrigin_Current=1,
	SeekOrigin_End=2,
} MonoSeekOrigin;


extern HANDLE ves_icall_System_PAL_OpSys_GetStdHandle(MonoObject *this, gint32 fs);
extern gint32 ves_icall_System_PAL_OpSys_ReadFile(MonoObject *this, HANDLE handle, MonoArray *buffer, gint32 offset, gint32 count);
extern gint32 ves_icall_System_PAL_OpSys_WriteFile(MonoObject *this, HANDLE handle,  MonoArray *buffer, gint32 offset, gint32 count);
extern gint32 ves_icall_System_PAL_OpSys_SetLengthFile(MonoObject *this, HANDLE handle, gint64 length);
extern HANDLE ves_icall_System_PAL_OpSys_OpenFile(MonoObject *this, MonoString *path, gint32 mode, gint32 access, gint32 share);
extern void ves_icall_System_PAL_OpSys_CloseFile(MonoObject *this, HANDLE handle);
extern gint64 ves_icall_System_PAL_OpSys_SeekFile(MonoObject *this, HANDLE handle, gint64 offset, gint32 origin);
extern void ves_icall_System_PAL_OpSys_DeleteFile(MonoObject *this, MonoString *path);
extern gboolean ves_icall_System_PAL_OpSys_ExistsFile(MonoObject *this, MonoString *path);
extern gboolean ves_icall_System_PAL_OpSys_GetFileTime(HANDLE handle, gint64 *createtime, gint64 *lastaccess, gint64 *lastwrite);
extern gboolean ves_icall_System_PAL_OpSys_SetFileTime(HANDLE handle, gint64 createtime, gint64 lastaccess, gint64 lastwrite);

#endif /* _MONO_METADATA_FILEIO_H_ */
