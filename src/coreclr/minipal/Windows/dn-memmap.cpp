// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <dn-memmap.h>
#include <minipal/utils.h>

MemoryMappedFile* MemoryMappedFile::OpenImpl(const WCHAR* path, bool readWrite, uint32_t desiredSize, void* desiredAddress)
{
    HANDLE hFile = INVALID_HANDLE_VALUE;
    HANDLE hFileMapping = NULL;

    hFile = CreateFileW(path,
        readWrite ? (GENERIC_READ | GENERIC_WRITE) : GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        readWrite ? CREATE_ALWAYS : OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
        NULL);
    if (hFile == INVALID_HANDLE_VALUE)
        goto Fail;

    LARGE_INTEGER li;
    // All the use cases are not accepting files >4GB
    if (!GetFileSizeEx(hFile, &li) || li.HighPart != 0)
        goto Fail;

    uint32_t size = li.LowPart;

    hFileMapping = CreateFileMappingW(hFile,
        NULL,
        readWrite ? PAGE_READWRITE : PAGE_READONLY,
        0,
        desiredSize,
        NULL);
    if (hFileMapping == NULL)
        goto Fail;

    void* address = MapViewOfFileEx(hFileMapping,
        readWrite ? FILE_MAP_ALL_ACCESS : FILE_MAP_READ,
        0,
        0,
        0,
        desiredAddress);
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
