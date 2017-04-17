//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// TODO-Cleanup: this class is unused

#include "standardpch.h"
#include "filecache.h"

#define FileCacheSize 0xFFFFFF // needs to be bigger than the biggest read request.

HANDLE       FileCache::cachedHandle;
bool         FileCache::openAsCache;
BYTE*        FileCache::rawBuff;
unsigned int FileCache::offset;
unsigned int FileCache::length;
__int64      FileCache::fileoffset;

HANDLE
FileCache::CreateFileA(_In_ LPCSTR lpFileName,
                       _In_ DWORD dwDesiredAccess,
                       _In_ DWORD dwShareMode,
                       _In_opt_ LPSECURITY_ATTRIBUTES lpSecurityAttributes,
                       _In_ DWORD dwCreationDisposition,
                       _In_ DWORD dwFlagsAndAttributes,
                       _In_opt_ HANDLE hTemplateFile)
{
    openAsCache = false;
    rawBuff     = nullptr;
    offset      = 0;
    length      = 0;

    if ((dwShareMode & CACHE_THIS_FILE) == CACHE_THIS_FILE)
    {
        dwShareMode ^= CACHE_THIS_FILE;
        openAsCache = true;
    }
    HANDLE temp = ::CreateFileA(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition,
                                dwFlagsAndAttributes, hTemplateFile);
    if (openAsCache)
        cachedHandle = temp; // yes yes.. this is unsafe.. but one accessor now is okay. bswhack
    return temp;
}

HANDLE
FileCache::CreateFileW(_In_ LPCWSTR lpFileName,
                       _In_ DWORD dwDesiredAccess,
                       _In_ DWORD dwShareMode,
                       _In_opt_ LPSECURITY_ATTRIBUTES lpSecurityAttributes,
                       _In_ DWORD dwCreationDisposition,
                       _In_ DWORD dwFlagsAndAttributes,
                       _In_opt_ HANDLE hTemplateFile)
{
    openAsCache = false;
    rawBuff     = nullptr;
    offset      = 0;
    length      = 0;
    return ::CreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition,
                         dwFlagsAndAttributes, hTemplateFile);
}

// Somewhat sloppy quick copy... we don't treat lpNumberOfBytesRead etc correctly
BOOL FileCache::ReadFile(_In_ HANDLE hFile,
                         _Out_writes_bytes_to_opt_(nNumberOfBytesToRead, *lpNumberOfBytesRead) __out_data_source(FILE)
                             LPVOID lpBuffer,
                         _In_ DWORD nNumberOfBytesToRead,
                         _Out_opt_ LPDWORD lpNumberOfBytesRead,
                         _Inout_opt_ LPOVERLAPPED lpOverlapped)
{
    if (!openAsCache)
        return ::ReadFile(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped);
    else
    {
        if (rawBuff == nullptr)
        {
            rawBuff = new BYTE[FileCacheSize];
            length  = FileCacheSize;
            offset  = FileCacheSize;
        }
        if (nNumberOfBytesToRead > FileCacheSize)
        {
            printf("ERROR: nNumberOfBytesToRead exceeds FileCacheSize %u > %u\n", nNumberOfBytesToRead, FileCacheSize);
            __debugbreak();
        }
        if ((offset + nNumberOfBytesToRead) > length)
        {
            memmove(rawBuff, &rawBuff[offset],
                    length - offset); // Use memmove since we have overlapping regions more than half the time
            ::ReadFile(hFile, &rawBuff[length - offset], offset, lpNumberOfBytesRead, lpOverlapped);
            if (*lpNumberOfBytesRead == 0)
                __debugbreak();
            length -= offset - (*lpNumberOfBytesRead);
            LARGE_INTEGER DataTemp;
            LARGE_INTEGER zero;
            zero.QuadPart = 0;
            ::SetFilePointerEx(hFile, zero, &DataTemp, FILE_CURRENT);
            fileoffset = DataTemp.QuadPart;

            offset = 0;
        }
        memcpy(lpBuffer, &rawBuff[offset], nNumberOfBytesToRead);
        offset += nNumberOfBytesToRead;
        if (offset > FileCacheSize)
            __debugbreak();
        return true;
    }
}

BOOL FileCache::WriteFile(_In_ HANDLE                                         hFile,
                          _In_reads_bytes_opt_(nNumberOfBytesToWrite) LPCVOID lpBuffer,
                          _In_ DWORD nNumberOfBytesToWrite,
                          _Out_opt_ LPDWORD lpNumberOfBytesWritten,
                          _Inout_opt_ LPOVERLAPPED lpOverlapped)
{
    if (!openAsCache)
        return ::WriteFile(hFile, lpBuffer, nNumberOfBytesToWrite, lpNumberOfBytesWritten, lpOverlapped);
    else
    {
        printf("ERROR: We only support one file open via the cache.\n");
        __debugbreak();
        return false;
    }
}

BOOL FileCache::CloseHandle(_In_ HANDLE hObject)
{
    if (!openAsCache)
        return ::CloseHandle(hObject);
    else
    {
        if (rawBuff != nullptr)
            delete[] rawBuff;
        return ::CloseHandle(hObject);
    }
}
__int64 FileCache::GetFilePos(HANDLE hFile)
{
    return (fileoffset - (__int64)length) + (__int64)offset;
}
