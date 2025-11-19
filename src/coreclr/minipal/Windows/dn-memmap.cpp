// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <dn-memmap.h>
#include <minipal/utils.h>

MemoryMappedFile::MemoryMappedFile(const WCHAR* path)
: m_size(0)
, m_address(nullptr)
, m_hFile(NULL)
, m_hFileMapping(NULL)
{
    HANDLE hFile = NULL;
    HANDLE hFileMapping = NULL;
    void* address = nullptr;

    hFile = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (hFile == NULL)
        goto Fail;

    LARGE_INTEGER size;
    if (!GetFileSizeEx(hFile, &size))
        goto Fail;

    if (size.QuadPart > SIZE_MAX)
        goto Fail;

    hFileMapping = CreateFileMappingW(hFile, NULL, PAGE_READONLY, 0, 0, NULL);
    if (hFileMapping == NULL)
        goto Fail;

    address = MapViewOfFile(hFileMapping, FILE_MAP_READ, 0, 0, 0);
    if (address == nullptr)
        goto Fail;

    m_hFile = hFile;
    m_hFileMapping = hFileMapping;
    m_address = address;
    m_size = (size_t)size.QuadPart;
    return;

Fail:
    int error = GetLastError();
    if (address != nullptr)
        UnmapViewOfFile(address);
    if (hFileMapping != NULL)
        CloseHandle(hFileMapping);
    if (hFile != NULL)
        CloseHandle(hFile);
}

MemoryMappedFile::~MemoryMappedFile()
{
    if (m_address != nullptr)
        UnmapViewOfFile(m_address);
    if (m_hFileMapping != NULL)
        CloseHandle(m_hFileMapping);
    if (m_hFile != NULL)
        CloseHandle(m_hFile);
}
