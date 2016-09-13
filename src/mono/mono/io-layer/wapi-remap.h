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
#define TransmitFile wapi_TransmitFile
#define GetThreadContext wapi_GetThreadContext
#define CloseHandle wapi_CloseHandle 
#define DuplicateHandle wapi_DuplicateHandle 
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
#define FormatMessage wapi_FormatMessage 
#define ShellExecuteEx wapi_ShellExecuteEx 
#define CreateProcess wapi_CreateProcess 
#define CreateProcessWithLogonW wapi_CreateProcessWithLogonW 
#define GetCurrentProcess wapi_GetCurrentProcess 
#define GetProcessId wapi_GetProcessId 
#define CloseProcess wapi_CloseProcess 
#define OpenProcess wapi_OpenProcess 
#define GetExitCodeProcess wapi_GetExitCodeProcess 
#define GetProcessTimes wapi_GetProcessTimes 
#define EnumProcessModules wapi_EnumProcessModules 
#define GetModuleBaseName wapi_GetModuleBaseName 
#define GetModuleFileNameEx wapi_GetModuleFileNameEx 
#define GetModuleInformation wapi_GetModuleInformation 
#define GetProcessWorkingSetSize wapi_GetProcessWorkingSetSize 
#define SetProcessWorkingSetSize wapi_SetProcessWorkingSetSize 
#define TerminateProcess wapi_TerminateProcess 
#define GetPriorityClass wapi_GetPriorityClass 
#define SetPriorityClass wapi_SetPriorityClass 
#define ImpersonateLoggedOnUser wapi_ImpersonateLoggedOnUser 
#define RevertToSelf wapi_RevertToSelf 
#define WSASetLastError wapi_WSASetLastError
#define WSAGetLastError wapi_WSAGetLastError
#define WSAIoctl wapi_WSAIoctl 
#define WSARecv wapi_WSARecv 
#define WSASend wapi_WSASend 
#define GetSystemInfo wapi_GetSystemInfo
#define GetFileVersionInfoSize wapi_GetFileVersionInfoSize 
#define GetFileVersionInfo wapi_GetFileVersionInfo 
#define VerQueryValue wapi_VerQueryValue 
#define VerLanguageName wapi_VerLanguageName 
#define WaitForSingleObjectEx wapi_WaitForSingleObjectEx
#define SignalObjectAndWait wapi_SignalObjectAndWait
#define WaitForMultipleObjectsEx wapi_WaitForMultipleObjectsEx

#endif /* __WAPI_REMAP_H__ */
