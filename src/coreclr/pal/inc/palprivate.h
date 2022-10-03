// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __PAL_PRIVATE_H__
#define __PAL_PRIVATE_H__

#ifdef  __cplusplus
extern "C" {
#endif

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
BOOL
PALAPI
CreateDirectoryW(
         IN LPCWSTR lpPathName,
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
BOOL
PALAPI
SetFileAttributesW(
           IN LPCWSTR lpFileName,
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
SetCurrentDirectoryW(
            IN LPCWSTR lpPathName);

PALIMPORT
BOOL
PALAPI
SetCurrentDirectoryA(
            IN LPCSTR lpPathName);

PALIMPORT
HANDLE
PALAPI
OpenMutexA(
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
RemoveDirectoryW(
                 IN LPCWSTR lpPathName);

PALIMPORT
LONG
PALAPI
CompareFileTime(
        IN CONST FILETIME *lpFileTime1,
        IN CONST FILETIME *lpFileTime2);

/*  These are from the <FCNTL.H> file in windows.
    They are needed for _open_osfhandle.*/
#define _O_RDONLY   0x0000
#define _O_APPEND   0x0008
#define _O_TEXT     0x4000
#define _O_BINARY   0x8000

PALIMPORT int __cdecl _open_osfhandle(INT_PTR, int);

#ifdef  __cplusplus
}
#endif

#endif// __PAL_PRIVATE_H__
