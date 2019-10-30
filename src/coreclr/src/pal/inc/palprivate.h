// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __PAL_PRIVATE_H__
#define __PAL_PRIVATE_H__

#ifdef  __cplusplus
extern "C" {
#endif

PALIMPORT
int
PALAPI
MessageBoxA(
        IN LPVOID hWnd,  // NOTE: diff from winuser.h
        IN LPCSTR lpText,
        IN LPCSTR lpCaption,
        IN UINT uType);


PALIMPORT
HANDLE
PALAPI
CreateFileA(
        IN LPCSTR lpFileName,
        IN DWORD dwDesiredAccess,
        IN DWORD dwShareMode,
        IN LPSECURITY_ATTRIBUTES lpSecurityAttributes,
        IN DWORD dwCreationDisposition,
        IN DWORD dwFlagsAndAttributes,
        IN HANDLE hTemplateFile);

PALIMPORT
DWORD
PALAPI
SearchPathA(
    IN LPCSTR lpPath,
    IN LPCSTR lpFileName,
    IN LPCSTR lpExtension,
    IN DWORD nBufferLength,
    OUT LPSTR lpBuffer,
    OUT LPSTR *lpFilePart
    );

PALIMPORT
BOOL
PALAPI
CopyFileA(
      IN LPCSTR lpExistingFileName,
      IN LPCSTR lpNewFileName,
      IN BOOL bFailIfExists);


PALIMPORT
BOOL
PALAPI
DeleteFileA(
        IN LPCSTR lpFileName);

PALIMPORT
BOOL
PALAPI
MoveFileExA(
        IN LPCSTR lpExistingFileName,
        IN LPCSTR lpNewFileName,
        IN DWORD dwFlags);

PALIMPORT
BOOL
PALAPI
CreateDirectoryA(
         IN LPCSTR lpPathName,
         IN LPSECURITY_ATTRIBUTES lpSecurityAttributes);

PALIMPORT
HANDLE
PALAPI
FindFirstFileA(
           IN LPCSTR lpFileName,
           OUT LPWIN32_FIND_DATAA lpFindFileData);

PALIMPORT
BOOL
PALAPI
FindNextFileA(
          IN HANDLE hFindFile,
          OUT LPWIN32_FIND_DATAA lpFindFileData);

PALIMPORT
DWORD
PALAPI
GetFileAttributesA(
           IN LPCSTR lpFileName);

PALIMPORT
BOOL
PALAPI
SetFileAttributesA(
           IN LPCSTR lpFileName,
           IN DWORD dwFileAttributes);

PALIMPORT
DWORD
PALAPI
GetFullPathNameA(
         IN LPCSTR lpFileName,
         IN DWORD nBufferLength,
         OUT LPSTR lpBuffer,
         OUT LPSTR *lpFilePart);

PALIMPORT
UINT
PALAPI
GetTempFileNameA(
         IN LPCSTR lpPathName,
         IN LPCSTR lpPrefixString,
         IN UINT uUnique,
         OUT LPSTR lpTempFileName);

PALIMPORT
DWORD
PALAPI
GetTempPathA(
         IN DWORD nBufferLength,
         OUT LPSTR lpBuffer);

PALIMPORT
DWORD
PALAPI
GetCurrentDirectoryA(
             IN DWORD nBufferLength,
             OUT LPSTR lpBuffer);

PALIMPORT
BOOL
PALAPI
SetCurrentDirectoryA(
            IN LPCSTR lpPathName);

PALIMPORT
HANDLE
PALAPI
CreateSemaphoreA(
         IN LPSECURITY_ATTRIBUTES lpSemaphoreAttributes,
         IN LONG lInitialCount,
         IN LONG lMaximumCount,
         IN LPCSTR lpName);

PALIMPORT
HANDLE
PALAPI
CreateSemaphoreExA(
         IN LPSECURITY_ATTRIBUTES lpSemaphoreAttributes,
         IN LONG lInitialCount,
         IN LONG lMaximumCount,
         IN LPCSTR lpName,
         IN /*_Reserved_*/  DWORD dwFlags,
         IN DWORD dwDesiredAccess);

PALIMPORT
HANDLE
PALAPI
CreateEventA(
         IN LPSECURITY_ATTRIBUTES lpEventAttributes,
         IN BOOL bManualReset,
         IN BOOL bInitialState,
         IN LPCSTR lpName);

PALIMPORT
HANDLE
PALAPI
CreateMutexA(
    IN LPSECURITY_ATTRIBUTES lpMutexAttributes,
    IN BOOL bInitialOwner,
    IN LPCSTR lpName);

PALIMPORT
HANDLE
PALAPI
OpenMutexA(
       IN DWORD dwDesiredAccess,
       IN BOOL bInheritHandle,
       IN LPCSTR lpName);

PALIMPORT
BOOL
PALAPI
CreateProcessA(
           IN LPCSTR lpApplicationName,
           IN LPSTR lpCommandLine,
           IN LPSECURITY_ATTRIBUTES lpProcessAttributes,
           IN LPSECURITY_ATTRIBUTES lpThreadAttributes,
           IN BOOL bInheritHandles,
           IN DWORD dwCreationFlags,
           IN LPVOID lpEnvironment,
           IN LPCSTR lpCurrentDirectory,
           IN LPSTARTUPINFOA lpStartupInfo,
           OUT LPPROCESS_INFORMATION lpProcessInformation);

PALIMPORT
HANDLE
PALAPI
CreateFileMappingA(
           IN HANDLE hFile,
           IN LPSECURITY_ATTRIBUTES lpFileMappingAttributes,
           IN DWORD flProtect,
           IN DWORD dwMaximumSizeHigh,
           IN DWORD dwMaximumSizeLow,
           IN LPCSTR lpName);

PALIMPORT
HANDLE
PALAPI
OpenFileMappingA(
         IN DWORD dwDesiredAccess,
         IN BOOL bInheritHandle,
         IN LPCSTR lpName);

PALIMPORT
HMODULE
PALAPI
LoadLibraryA(
        IN LPCSTR lpLibFileName);

PALIMPORT
HMODULE
PALAPI
LoadLibraryExA(
        IN LPCSTR lpLibFileName,
        IN /*Reserved*/ HANDLE hFile,
        IN DWORD dwFlags);

PALIMPORT
DWORD
PALAPI
GetModuleFileNameA(
    IN HMODULE hModule,
    OUT LPSTR lpFileName,
    IN DWORD nSize);


PALIMPORT
LPSTR
PALAPI
GetEnvironmentStringsA(
               VOID);

PALIMPORT
BOOL
PALAPI
SetEnvironmentVariableA(
            IN LPCSTR lpName,
            IN LPCSTR lpValue);

PALIMPORT
DWORD
PALAPI
GetEnvironmentVariableA(
            IN LPCSTR lpName,
            OUT LPSTR lpBuffer,
            IN DWORD nSize);

PALIMPORT
BOOL
PALAPI
FreeEnvironmentStringsA(
            IN LPSTR);

PALIMPORT
BOOL
PALAPI
RemoveDirectoryA(
                 IN LPCSTR lpPathName);

PALIMPORT
BOOL
PALAPI
PAL_GetPALDirectoryA(
    OUT LPSTR lpDirectoryName,
    IN UINT* cchDirectoryName);

#ifdef  __cplusplus
}
#endif

#endif// __PAL_PRIVATE_H__
