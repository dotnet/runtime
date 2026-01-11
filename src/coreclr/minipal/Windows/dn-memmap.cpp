// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <dn-memmap.h>
#include <minipal/utils.h>

MemoryMappedFile* MemoryMappedFile::Open(const WCHAR* path)
{
    HANDLE hFile = INVALID_HANDLE_VALUE;
    HANDLE hFileMapping = NULL;

    hFile = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFile == INVALID_HANDLE_VALUE)
        goto Fail;

    LARGE_INTEGER li;
    if (!GetFileSizeEx(hFile, &li))
        goto Fail;

    if (li.QuadPart > SIZE_MAX)
        goto Fail;

    size_t size = (size_t)li.QuadPart;

    hFileMapping = CreateFileMappingW(hFile, NULL, PAGE_READONLY, 0, 0, NULL);
    if (hFileMapping == NULL)
        goto Fail;

    void* address = MapViewOfFile(hFileMapping, FILE_MAP_READ, 0, 0, 0);
    if (address == nullptr)
        goto Fail;
    
    CloseHandle(hFile);
    CloseHandle(hFileMapping);

    MemoryMappedFile* result = new MemoryMappedFile();
    result->m_address = address;
    result->m_size = size;
    return result;

Fail:
    if (hFile != INVALID_HANDLE_VALUE)
        CloseHandle(hFile);
    if (hFileMapping != NULL)
        CloseHandle(hFileMapping);
    return nullptr;
}

MemoryMappedFile::~MemoryMappedFile()
{
    if (m_address != nullptr)
        UnmapViewOfFile(m_address);
}
