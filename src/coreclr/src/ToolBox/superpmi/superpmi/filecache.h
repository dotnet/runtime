//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// TODO-Cleanup: this class is unused

//----------------------------------------------------------
// FileCache.h - very simple read ahead/ReadFile abstraction
//----------------------------------------------------------
#ifndef _FileCache
#define _FileCache

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#define CACHE_THIS_FILE 0xFF

class FileCache
{
public:
    static HANDLE CreateFileA(_In_ LPCSTR lpFileName,
                              _In_ DWORD dwDesiredAccess,
                              _In_ DWORD dwShareMode,
                              _In_opt_ LPSECURITY_ATTRIBUTES lpSecurityAttributes,
                              _In_ DWORD dwCreationDisposition,
                              _In_ DWORD dwFlagsAndAttributes,
                              _In_opt_ HANDLE hTemplateFile);

    static HANDLE CreateFileW(_In_ LPCWSTR lpFileName,
                              _In_ DWORD dwDesiredAccess,
                              _In_ DWORD dwShareMode,
                              _In_opt_ LPSECURITY_ATTRIBUTES lpSecurityAttributes,
                              _In_ DWORD dwCreationDisposition,
                              _In_ DWORD dwFlagsAndAttributes,
                              _In_opt_ HANDLE hTemplateFile);

    static BOOL ReadFile(_In_ HANDLE hFile,
                         _Out_writes_bytes_to_opt_(nNumberOfBytesToRead, *lpNumberOfBytesRead) __out_data_source(FILE)
                             LPVOID lpBuffer,
                         _In_ DWORD nNumberOfBytesToRead,
                         _Out_opt_ LPDWORD lpNumberOfBytesRead,
                         _Inout_opt_ LPOVERLAPPED lpOverlapped);

    static BOOL WriteFile(_In_ HANDLE                                         hFile,
                          _In_reads_bytes_opt_(nNumberOfBytesToWrite) LPCVOID lpBuffer,
                          _In_ DWORD nNumberOfBytesToWrite,
                          _Out_opt_ LPDWORD lpNumberOfBytesWritten,
                          _Inout_opt_ LPOVERLAPPED lpOverlapped);

    static BOOL CloseHandle(_In_ HANDLE hObject);

    static __int64 GetFilePos(HANDLE hFile);

private:
    static HANDLE       cachedHandle;
    static bool         openAsCache;
    static BYTE*        rawBuff;
    static unsigned int offset;
    static unsigned int length;
    static __int64      fileoffset;
};
#endif
