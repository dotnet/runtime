// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef DN_MEMMAP_H
#define DN_MEMMAP_H

#include <stdint.h>

class MemoryMappedFile final
{
    uint32_t m_size;
    void* m_address;
    
    MemoryMappedFile() = default;
    static MemoryMappedFile* OpenImpl(const WCHAR* path, bool readWrite, uint32_t desiredSize, void* desiredAddress);

public:
    static MemoryMappedFile* Open(const WCHAR* path) { return OpenImpl(path, false, 0, nullptr); }
    static MemoryMappedFile* OpenRW(const WCHAR* path, uint32_t desiredSize, void* desiredAddress)
    {
        return OpenImpl(path, true, desiredSize, desiredAddress);
    }
    ~MemoryMappedFile();
    
    MemoryMappedFile(const MemoryMappedFile&) = delete;
    MemoryMappedFile& operator=(const MemoryMappedFile&) = delete;

    void* Address() { return m_address; }
    uint32_t Size() { return m_size; }
};

#endif // DN_MEMMAP_H
