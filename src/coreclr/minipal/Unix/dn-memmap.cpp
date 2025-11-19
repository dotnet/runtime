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

MemoryMappedFile::MemoryMappedFile(const WCHAR* path)
: m_size(0)
, m_address(nullptr)
{
    size_t pathLen = u16_strlen(path);
    size_t pathU8Len = minipal_get_length_utf16_to_utf8((CHAR16_T*)path, pathLen, 0);
    char* pathU8 = new char[pathU8Len + 1];
    size_t ret = minipal_convert_utf16_to_utf8((CHAR16_T*)path, pathLen, pathU8, pathU8Len, 0);
    pathU8[ret] = '\0';

    int fd = -1;
    void* address = nullptr;

    int fd = open(pathU8, O_RDONLY);
    delete[] pathU8;

    if (fd == -1)
        goto Fail;

    struct stat st;
    if (fstat(fd, &st) != 0)
        goto Fail;

    if (st.st_size > SIZE_MAX)
        goto Fail;

    address = mmap(nullptr, (size_t)st.st_size, PROT_READ, 0, fd, 0);
    if (address == MAP_FAILED)
        goto Fail;
    
    m_address = address;
    m_size = (size_t)st.st_size;
    close(fd);
    return;

Fail:
    if (fd != -1)
        close(fd);
}

MemoryMappedFile::~MemoryMappedFile()
{
    if (m_address != nullptr)
        munmap(m_address, m_size);
}
