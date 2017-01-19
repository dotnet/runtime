/*
 * wapi-remap.h: io-layer symbol remapping support
 *
 * (C) 2014 Xamarin, Inc.
 */

#ifndef __WAPI_REMAP_H__
#define __WAPI_REMAP_H__

/*
 * The windows function names used by the io-layer can collide with symbols in system and 3rd party libs, esp. on osx/ios. So remap them to
 * wapi_<funcname>.
 */

#define GetLastError wapi_GetLastError
#define SetLastError wapi_SetLastError
#define CloseHandle wapi_CloseHandle 
#define CreateFile wapi_CreateFile
#define DeleteFile wapi_DeleteFile
#define GetStdHandle wapi_GetStdHandle
#define ReadFile wapi_ReadFile
#define WriteFile wapi_WriteFile
#define FlushFileBuffers wapi_FlushFileBuffers
#define SetEndOfFile wapi_SetEndOfFile
#define SetFilePointer wapi_SetFilePointer
#define GetFileType wapi_GetFileType
#define GetFileSize wapi_GetFileSize
#define GetFileTime wapi_GetFileTime
#define SetFileTime wapi_SetFileTime
#define FileTimeToSystemTime wapi_FileTimeToSystemTime
#define FindFirstFile wapi_FindFirstFile 
#define FindNextFile wapi_FindNextFile 
#define FindClose wapi_FindClose 
#define CreateDirectory wapi_CreateDirectory 
#define RemoveDirectory wapi_RemoveDirectory 
#define MoveFile wapi_MoveFile 
#define CopyFile wapi_CopyFile 
#define ReplaceFile wapi_ReplaceFile 
#define GetFileAttributes wapi_GetFileAttributes 
#define GetFileAttributesEx wapi_GetFileAttributesEx 
#define SetFileAttributes wapi_SetFileAttributes 
#define GetCurrentDirectory wapi_GetCurrentDirectory 
#define SetCurrentDirectory wapi_SetCurrentDirectory 
#define CreatePipe wapi_CreatePipe 
#define GetLogicalDriveStrings wapi_GetLogicalDriveStrings 
#define GetDiskFreeSpaceEx wapi_GetDiskFreeSpaceEx
#define GetDriveType wapi_GetDriveType
#define LockFile wapi_LockFile 
#define UnlockFile wapi_UnlockFile 
#define GetVolumeInformation wapi_GetVolumeInformation 
#define ImpersonateLoggedOnUser wapi_ImpersonateLoggedOnUser 
#define RevertToSelf wapi_RevertToSelf 
#define GetSystemInfo wapi_GetSystemInfo

#endif /* __WAPI_REMAP_H__ */
