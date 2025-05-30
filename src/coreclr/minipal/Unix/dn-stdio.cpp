// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

typedef char16_t WCHAR;

#include <stdio.h>
#include <errno.h>
#include <dn-stdio.h>
#include <dn-u16.h>
#include <minipal/utf8.h>

int fopen_u16(FILE** stream, const WCHAR* path, const WCHAR* mode)
{
    size_t pathLen = u16_strlen(path);
    size_t pathU8Len = minipal_get_length_utf16_to_utf8((CHAR16_T*)path, pathLen, 0);
    char* pathU8 = new char[pathU8Len + 1];
    minipal_convert_utf16_to_utf8((CHAR16_T*)path, pathLen, pathU8, pathU8Len, 0);
    
    size_t modeLen = u16_strlen(mode);
    size_t modeU8Len = minipal_get_length_utf16_to_utf8((CHAR16_T*)mode, modeLen, 0);
    char* modeU8 = new char[modeU8Len + 1];
    minipal_convert_utf16_to_utf8((CHAR16_T*)mode, modeLen, modeU8, modeU8Len, 0);

    FILE* result = fopen(pathU8, modeU8);

    delete[] pathU8;
    delete[] modeU8;

    if (result)
    {
        *stream = result;
        return 0;
    }
    else
    {
        *stream = NULL;
        return errno;
    }
}

int64_t fgetsize(FILE* stream)
{
    fpos_t current;
    fgetpos(stream, &current);
    fseek(stream, 0, SEEK_END);
    int64_t length = ftell(stream);
    fsetpos(stream, &current);
    return length;
}
