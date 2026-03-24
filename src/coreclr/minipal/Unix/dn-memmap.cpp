// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

typedef char16_t WCHAR;

#include <dn-u16.h>
#include <dn-memmap.h>
#include <minipal/utf8.h>
#include <fcntl.h>
#include <unistd.h>
#include <errno.h>
#include <sys/mman.h>
#include <sys/stat.h>

MemoryMappedFile* MemoryMappedFile::OpenImpl(const WCHAR* path, bool readWrite, uint32_t desiredSize, void* desiredAddress)
{
    size_t pathLen = u16_strlen(path);
    size_t pathU8Len = minipal_get_length_utf16_to_utf8((CHAR16_T*)path, pathLen, 0);
    char* pathU8 = new char[pathU8Len + 1];
    size_t ret = minipal_convert_utf16_to_utf8((CHAR16_T*)path, pathLen, pathU8, pathU8Len, 0);
    pathU8[ret] = '\0';

    void* address = nullptr;
    MemoryMappedFile* result = nullptr;

    int fd = open(pathU8, readWrite ? (O_RDWR | O_CREAT) : O_RDONLY);
    delete[] pathU8;

    if (fd == -1)
        goto Fail;

    struct stat st;
    if (fstat(fd, &st) != 0)
        goto Fail;
    
    // All the use cases are not accepting files >4GB
    if (st.st_size > UINT32_MAX)
        goto Fail;

    if (st.st_size < desiredSize)
    {
        if (ftruncate(fd, desiredSize) != 0)
            goto Fail;
        if (fstat(fd, &st) != 0 || st.st_size != desiredSize)
            goto Fail;
    }

    address = mmap(desiredAddress, (size_t)st.st_size, readWrite ? (PROT_WRITE | PROT_READ) : PROT_READ , MAP_SHARED, fd, 0);
    if (address == MAP_FAILED)
        goto Fail;
    
    close(fd);
    result = new MemoryMappedFile();
    result->m_address = address;
    result->m_size = (size_t)st.st_size;
    return result;

Fail:
    if (fd != -1)
        close(fd);
    return nullptr;
}

MemoryMappedFile::~MemoryMappedFile()
{
    if (m_address != nullptr)
        munmap(m_address, m_size);
}
