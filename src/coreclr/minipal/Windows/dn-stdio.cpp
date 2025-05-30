// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <dn-stdio.h>

int fopen_u16(FILE** stream, const WCHAR* path, const WCHAR* mode)
{
    return _wfopen_s(stream, path, mode);
}

int64_t fgetsize(FILE* stream)
{
    fpos_t current;
    fgetpos(stream, &current);
    fseek(stream, 0, SEEK_END);
    int64_t length = _ftelli64(stream);
    fsetpos(stream, &current);
    return length;
}
