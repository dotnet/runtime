// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef DN_MEMMAP_H
#define DN_MEMMAP_H

#include <stdint.h>

class MemoryMappedFile final
{
    size_t m_size;
    void* m_address;
    
    MemoryMappedFile() = default;

public:
    static MemoryMappedFile* Open(const WCHAR* path);
    ~MemoryMappedFile();
    
    MemoryMappedFile(const MemoryMappedFile&) = delete;
    MemoryMappedFile& operator=(const MemoryMappedFile&) = delete;

    void* Address() { return m_address; }
    size_t Size() { return m_size; }
};

#endif // DN_MEMMAP_H
