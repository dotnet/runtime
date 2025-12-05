// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <dn-memmap.h>
#include <minipal/utils.h>

MemoryMappedFile* MemoryMappedFile::Open(const WCHAR* path)
{
    MemoryMappedFile* result = new MemoryMappedFile();

    HANDLE hFile = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFile == INVALID_HANDLE_VALUE)
        goto Fail;

    LARGE_INTEGER size;
    if (!GetFileSizeEx(hFile, &size))
        goto Fail;

    if (size.QuadPart > SIZE_MAX)
        goto Fail;

    result->m_size = (size_t)size.QuadPart;

    result->m_hFileMapping = CreateFileMappingW(hFile, NULL, PAGE_READONLY, 0, 0, NULL);
    if (result->m_hFileMapping == NULL)
        goto Fail;

    result->m_address = MapViewOfFile(result->m_hFileMapping, FILE_MAP_READ, 0, 0, 0);
    if (result->m_address == nullptr)
        goto Fail;

    return result;

Fail:
    if (hFile != INVALID_HANDLE_VALUE)
        CloseHandle(hFile);
    delete result;
    return nullptr;
}

MemoryMappedFile::~MemoryMappedFile()
{
    if (m_address != nullptr)
        UnmapViewOfFile(m_address);
    if (m_hFileMapping != NULL)
        CloseHandle(m_hFileMapping);
}
