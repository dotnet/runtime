// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "palrt.h"
#include "pal.h"
#include "stdlib.h"
#include "pal_mstypes.h"
#include "pal_error.h"
#include <new>
#include <memory.h>

#define wcslen PAL_wcslen

bool ResizeBuffer(char *&buffer, size_t& size, size_t currLen, size_t newSize, bool &fixedBuffer)
{
    newSize = (size_t)(newSize * 1.5);
    _ASSERTE(newSize > size); // check for overflow

    if (newSize < 32)
        newSize = 32;

    // We can't use coreclr includes here so we use std::nothrow
    // rather than the coreclr version
    char *newBuffer = new (std::nothrow) char[newSize];

    if (newBuffer == NULL)
        return false;

    memcpy(newBuffer, buffer, currLen);

    if (!fixedBuffer)
        delete[] buffer;

    buffer = newBuffer;
    size = newSize;
    fixedBuffer = false;

    return true;
}

bool WriteToBuffer(const BYTE *src, size_t len, char *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
{
    if(!src) return true;
    if (offset + len > size)
    {
        if (!ResizeBuffer(buffer, size, offset, size + len, fixedBuffer))
            return false;
    }

    memcpy(buffer + offset, src, len);
    offset += len;
    return true;
}

bool WriteToBuffer(PCWSTR str, char *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
{
    if(!str) return true;
    size_t byteCount = (wcslen(str) + 1) * sizeof(*str);

    if (offset + byteCount > size)
    {
        if (!ResizeBuffer(buffer, size, offset, size + byteCount, fixedBuffer))
            return false;
    }

    memcpy(buffer + offset, str, byteCount);
    offset += byteCount;
    return true;
}

bool WriteToBuffer(const char *str, char *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
{
    if(!str) return true;
    size_t len = strlen(str) + 1;
    if (offset + len > size)
    {
        if (!ResizeBuffer(buffer, size, offset, size + len, fixedBuffer))
            return false;
    }

    memcpy(buffer + offset, str, len);
    offset += len;
    return true;
}
