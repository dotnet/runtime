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

#if defined (TARGET_IOS) || defined (TARGET_ANDROID)

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

#endif /* !defined(HOST_WIN32) */

void
mono_w32file_init (void);

MONO_COMPONENT_API
gpointer
mono_w32file_create(const gunichar2 *name, guint32 fileaccess, guint32 sharemode, guint32 createmode, guint32 attrs);

MONO_COMPONENT_API
gboolean
mono_w32file_close (gpointer handle);

MONO_COMPONENT_API
gboolean
mono_w32file_write (gpointer handle, gconstpointer buffer, guint32 numbytes, guint32 *byteswritten, gint32 *win32error);

#endif /* _MONO_METADATA_W32FILE_H_ */
