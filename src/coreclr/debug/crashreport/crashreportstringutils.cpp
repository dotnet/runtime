// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "crashreportstringutils.h"

#include <string.h>

void CrashReportStringUtils::CopyString(char* buffer, size_t bufferSize, const char* value)
{
    if (buffer == nullptr || bufferSize == 0)
    {
        return;
    }

    if (value == nullptr)
    {
        buffer[0] = '\0';
        return;
    }

    size_t toCopy = strnlen(value, bufferSize - 1);
    if (toCopy != 0)
    {
        memcpy(buffer, value, toCopy);
    }

    buffer[toCopy] = '\0';
}

bool CrashReportStringUtils::AppendString(char* buffer, size_t bufferSize, size_t* pos, const char* value)
{
    if (buffer == nullptr || pos == nullptr || value == nullptr || bufferSize == 0)
    {
        return false;
    }

    size_t p = *pos;
    while (*value != '\0' && p + 1 < bufferSize)
    {
        buffer[p++] = *value++;
    }
    buffer[p] = '\0';
    *pos = p;
    return *value == '\0';
}
