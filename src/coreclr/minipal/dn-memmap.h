// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef DN_MEMMAP_H
#define DN_MEMMAP_H

#include <stdint.h>

class MemoryMappedFile
{
    size_t m_size;
    void* m_address;
    
#ifdef TARGET_WINDOWS
    HANDLE m_hFile;
    HANDLE m_hFileMapping;
#endif

public:
    MemoryMappedFile()
        : m_size(0)
        , m_address(nullptr)
    #ifdef TARGET_WINDOWS
        , m_hFile(NULL)
        , m_hFileMapping(NULL)
    #endif
    {
    }

    MemoryMappedFile(const WCHAR* path);
    ~MemoryMappedFile();
    
    MemoryMappedFile(const MemoryMappedFile&) = delete;
    MemoryMappedFile& operator=(const MemoryMappedFile&) = delete;

    MemoryMappedFile(MemoryMappedFile&&) noexcept = default;
    MemoryMappedFile& operator=(MemoryMappedFile&&) noexcept = default;

    void* Address() { return m_address; }
    size_t Size() { return m_size; }
    bool Valid() { return m_address != nullptr; }
};

#endif // DN_MEMMAP_H
