// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <dn-stdio.h>

FILE* fopen_u16(const WCHAR* path, const WCHAR* mode)
{
    FILE* stream;
    errno_t err = _wfopen_s(&stream, path, mode);
    if (err == 0)
        return stream;
    else
        return NULL;
}
