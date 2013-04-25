/*
 * io.h: File, console and find handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_IO_H_
#define _WAPI_IO_H_

#include <stdlib.h>

#include "mono/io-layer/wapi.h"
#include "mono/io-layer/timefuncs.h"

G_BEGIN_DECLS

typedef struct _WapiSecurityAttributes WapiSecurityAttributes;

struct _WapiSecurityAttributes 
{
	guint32 nLength;
	gpointer lpSecurityDescriptor;
	gboolean bInheritHandle;
};

typedef struct _WapiOverlapped WapiOverlapped;

struct _WapiOverlapped
{
	guint32 Internal;
	guint32 InternalHigh;
	guint32 Offset;
	guint32 OffsetHigh;
	gpointer hEvent;
	gpointer handle1;
	gpointer handle2;
};

typedef void (*WapiOverlappedCB) (guint32 error, guint32 numbytes,
				  WapiOverlapped *overlapped);

#define GENERIC_READ	0x80000000
#define GENERIC_WRITE	0x40000000
#define GENERIC_EXECUTE	0x20000000
#define GENERIC_ALL	0x10000000

#define FILE_SHARE_READ		0x00000001
#define FILE_SHARE_WRITE	0x00000002
#define FILE_SHARE_DELETE	0x00000004

#define CREATE_NEW		1
#define CREATE_ALWAYS		2
#define OPEN_EXISTING		3
#define OPEN_ALWAYS		4
#define TRUNCATE_EXISTING	5


#define FILE_ATTRIBUTE_READONLY			0x00000001
#define FILE_ATTRIBUTE_HIDDEN			0x00000002
#define FILE_ATTRIBUTE_SYSTEM			0x00000004
#define FILE_ATTRIBUTE_DIRECTORY		0x00000010
#define FILE_ATTRIBUTE_ARCHIVE			0x00000020
#define FILE_ATTRIBUTE_ENCRYPTED		0x00000040
#define FILE_ATTRIBUTE_NORMAL			0x00000080
#define FILE_ATTRIBUTE_TEMPORARY		0x00000100
#define FILE_ATTRIBUTE_SPARSE_FILE		0x00000200
#define FILE_ATTRIBUTE_REPARSE_POINT		0x00000400
#define FILE_ATTRIBUTE_COMPRESSED		0x00000800
#define FILE_ATTRIBUTE_OFFLINE			0x00001000
#define FILE_ATTRIBUTE_NOT_CONTENT_INDEXED	0x00002000
#define FILE_FLAG_OPEN_NO_RECALL		0x00100000
#define FILE_FLAG_OPEN_REPARSE_POINT		0x00200000
#define FILE_FLAG_POSIX_SEMANTICS		0x01000000
#define FILE_FLAG_BACKUP_SEMANTICS		0x02000000
#define FILE_FLAG_DELETE_ON_CLOSE		0x04000000
#define FILE_FLAG_SEQUENTIAL_SCAN		0x08000000
#define FILE_FLAG_RANDOM_ACCESS			0x10000000
#define FILE_FLAG_NO_BUFFERING			0x20000000
#define FILE_FLAG_OVERLAPPED			0x40000000
#define FILE_FLAG_WRITE_THROUGH			0x80000000

#define REPLACEFILE_WRITE_THROUGH       0x00000001
#define REPLACEFILE_IGNORE_MERGE_ERRORS 0x00000002

#define MAX_PATH	260

typedef enum {
	STD_INPUT_HANDLE=-10,
	STD_OUTPUT_HANDLE=-11,
	STD_ERROR_HANDLE=-12
} WapiStdHandle;

typedef enum {
	FILE_BEGIN=0,
	FILE_CURRENT=1,
	FILE_END=2
} WapiSeekMethod;

typedef enum {
	FILE_TYPE_UNKNOWN=0x0000,
	FILE_TYPE_DISK=0x0001,
	FILE_TYPE_CHAR=0x0002,
	FILE_TYPE_PIPE=0x0003,
	FILE_TYPE_REMOTE=0x8000
} WapiFileType;

typedef enum {
	DRIVE_UNKNOWN=0,
	DRIVE_NO_ROOT_DIR=1,
	DRIVE_REMOVABLE=2,
	DRIVE_FIXED=3,
	DRIVE_REMOTE=4,
	DRIVE_CDROM=5,
	DRIVE_RAMDISK=6
} WapiDriveType;

typedef enum {
	GetFileExInfoStandard=0x0000,
	GetFileExMaxInfoLevel=0x0001
} WapiGetFileExInfoLevels;

typedef struct 
{
	guint16 wYear;
	guint16 wMonth;
	guint16 wDayOfWeek;
	guint16 wDay;
	guint16 wHour;
	guint16 wMinute;
	guint16 wSecond;
	guint16 wMilliseconds;
} WapiSystemTime;

typedef struct
{
	guint32 dwFileAttributes;
	WapiFileTime ftCreationTime;
	WapiFileTime ftLastAccessTime;
	WapiFileTime ftLastWriteTime;
	guint32 nFileSizeHigh;
	guint32 nFileSizeLow;
	guint32 dwReserved0;
	guint32 dwReserved1;
	gunichar2 cFileName [MAX_PATH];
	gunichar2 cAlternateFileName [14];
} WapiFindData;

typedef struct
{
	guint32 dwFileAttributes;
	WapiFileTime ftCreationTime;
	WapiFileTime ftLastAccessTime;
	WapiFileTime ftLastWriteTime;
	guint32 nFileSizeHigh;
	guint32 nFileSizeLow;
} WapiFileAttributesData;

#define INVALID_SET_FILE_POINTER ((guint32)-1)
#define INVALID_FILE_SIZE ((guint32)0xFFFFFFFF)
#define INVALID_FILE_ATTRIBUTES ((guint32)-1)

extern gpointer CreateFile(const gunichar2 *name, guint32 fileaccess,
			   guint32 sharemode,
			   WapiSecurityAttributes *security,
			   guint32 createmode,
			   guint32 attrs, gpointer tmplate);
extern gboolean DeleteFile(const gunichar2 *name);
extern gpointer GetStdHandle(WapiStdHandle stdhandle);
extern gboolean ReadFile(gpointer handle, gpointer buffer, guint32 numbytes,
			 guint32 *bytesread, WapiOverlapped *overlapped);
extern gboolean WriteFile(gpointer handle, gconstpointer buffer,
			  guint32 numbytes, guint32 *byteswritten,
			  WapiOverlapped *overlapped);
extern gboolean FlushFileBuffers(gpointer handle);
extern gboolean SetEndOfFile(gpointer handle);
extern guint32 SetFilePointer(gpointer handle, gint32 movedistance,
			      gint32 *highmovedistance, WapiSeekMethod method);
extern WapiFileType GetFileType(gpointer handle);
extern guint32 GetFileSize(gpointer handle, guint32 *highsize);
extern gboolean GetFileTime(gpointer handle, WapiFileTime *create_time,
			    WapiFileTime *last_access,
			    WapiFileTime *last_write);
extern gboolean SetFileTime(gpointer handle, const WapiFileTime *create_time,
			    const WapiFileTime *last_access,
			    const WapiFileTime *last_write);
extern gboolean FileTimeToSystemTime(const WapiFileTime *file_time,
				     WapiSystemTime *system_time);
extern gpointer FindFirstFile (const gunichar2 *pattern,
			       WapiFindData *find_data);
extern gboolean FindNextFile (gpointer handle, WapiFindData *find_data);
extern gboolean FindClose (gpointer handle);
extern gboolean CreateDirectory (const gunichar2 *name,
				 WapiSecurityAttributes *security);
extern gboolean RemoveDirectory (const gunichar2 *name);
extern gboolean MoveFile (const gunichar2 *name, const gunichar2 *dest_name);
extern gboolean CopyFile (const gunichar2 *name, const gunichar2 *dest_name,
			  gboolean fail_if_exists);
extern gboolean ReplaceFile (const gunichar2 *replacedFileName, const gunichar2 *replacementFileName,
			     const gunichar2 *backupFileName, guint32 replaceFlags, 
			     gpointer exclude, gpointer reserved);
extern guint32 GetFileAttributes (const gunichar2 *name);
extern gboolean GetFileAttributesEx (const gunichar2 *name,
				     WapiGetFileExInfoLevels level,
				     gpointer info);
extern gboolean SetFileAttributes (const gunichar2 *name, guint32 attrs);
extern guint32 GetCurrentDirectory (guint32 length, gunichar2 *buffer);
extern gboolean SetCurrentDirectory (const gunichar2 *path);
extern gboolean CreatePipe (gpointer *readpipe, gpointer *writepipe,
			    WapiSecurityAttributes *security, guint32 size);
extern guint32 GetTempPath (guint32 len, gunichar2 *buf);
extern gint32 GetLogicalDriveStrings (guint32 len, gunichar2 *buf);
extern gboolean GetDiskFreeSpaceEx(const gunichar2 *path_name, WapiULargeInteger *free_bytes_avail,
				   WapiULargeInteger *total_number_of_bytes,
				   WapiULargeInteger *total_number_of_free_bytes);
extern guint32 GetDriveType(const gunichar2 *root_path_name);
extern gboolean LockFile (gpointer handle, guint32 offset_low,
			  guint32 offset_high, guint32 length_low,
			  guint32 length_high);
extern gboolean UnlockFile (gpointer handle, guint32 offset_low,
			    guint32 offset_high, guint32 length_low,
			    guint32 length_high);
extern gboolean GetVolumeInformation (const gunichar2 *path, gunichar2 *volumename, int volumesize, int *outserial, int *maxcomp, int *fsflags, gunichar2 *fsbuffer, int fsbuffersize);


extern void _wapi_io_init (void);

G_END_DECLS

#endif /* _WAPI_IO_H_ */
