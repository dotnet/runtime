// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

typedef char16_t WCHAR;
typedef uint32_t HRESULT;

#include <stdio.h>
#include <errno.h>
#include <dn-stdio.h>
#include <dn-u16.h>
#include <minipal/utf8.h>

int u16_fopen_s(FILE** stream, const WCHAR* path, const WCHAR* mode)
{
    size_t pathLen = u16_strlen(path);
    size_t pathU8Len = minipal_get_length_utf16_to_utf8((CHAR16_T*)path, pathLen, 0);
    char* pathU8 = new char[pathU8Len + 1];
    size_t ret = minipal_convert_utf16_to_utf8((CHAR16_T*)path, pathLen, pathU8, pathU8Len, 0);
    pathU8[ret] = '\0';
    
    size_t modeLen = u16_strlen(mode);
    size_t modeU8Len = minipal_get_length_utf16_to_utf8((CHAR16_T*)mode, modeLen, 0);
    char* modeU8 = new char[modeU8Len + 1];
    ret = minipal_convert_utf16_to_utf8((CHAR16_T*)mode, modeLen, modeU8, modeU8Len, 0);
    modeU8[ret] = '\0';

    FILE* result = fopen(pathU8, modeU8);
    int err = errno;

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
        return err;
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

int64_t ftell_64(FILE* stream)
{
    return ftell(stream);
}

int fsetpos_64(FILE* stream, int64_t pos)
{
    return fseek(stream, pos, SEEK_SET);
}

#define FACILITY_WIN32                   7
#define HRESULT_FROM_WIN32(x) ((HRESULT)(x) <= 0 ? ((HRESULT)(x)) : ((HRESULT) (((x) & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000)))

#define ERROR_SUCCESS 0L
#define ERROR_FILE_NOT_FOUND 2L
#define ERROR_PATH_NOT_FOUND 3L
#define ERROR_TOO_MANY_OPEN_FILES 4L
#define ERROR_ACCESS_DENIED 5L
#define ERROR_INVALID_HANDLE 6L
#define ERROR_NOT_ENOUGH_MEMORY 8L
#define ERROR_WRITE_FAULT 29L
#define ERROR_GEN_FAILURE 31L
#define ERROR_DISK_FULL 112L
#define ERROR_DIR_NOT_EMPTY 145L
#define ERROR_BAD_PATHNAME 161L
#define ERROR_BUSY 170L
#define ERROR_ALREADY_EXISTS 183L
#define ERROR_FILENAME_EXCED_RANGE 206L

HRESULT HRESULT_FROM_LAST_STDIO()
{
    // maps the common I/O errors
    // based on FILEGetLastErrorFromErrno
    
    uint32_t win32Err;

    switch(errno)
    {
    case 0:
        win32Err = ERROR_SUCCESS;
        break;
    case ENAMETOOLONG:
        win32Err = ERROR_FILENAME_EXCED_RANGE;
        break;
    case ENOTDIR:
        win32Err = ERROR_PATH_NOT_FOUND;
        break;
    case ENOENT:
        win32Err = ERROR_FILE_NOT_FOUND;
        break;
    case EACCES:
    case EPERM:
    case EROFS:
    case EISDIR:
        win32Err = ERROR_ACCESS_DENIED;
        break;
    case EEXIST:
        win32Err = ERROR_ALREADY_EXISTS;
        break;
    case ENOTEMPTY:
        win32Err = ERROR_DIR_NOT_EMPTY;
        break;
    case EBADF:
        win32Err = ERROR_INVALID_HANDLE;
        break;
    case ENOMEM:
        win32Err = ERROR_NOT_ENOUGH_MEMORY;
        break;
    case EBUSY:
        win32Err = ERROR_BUSY;
        break;
    case ENOSPC:
    case EDQUOT:
        win32Err = ERROR_DISK_FULL;
        break;
    case ELOOP:
        win32Err = ERROR_BAD_PATHNAME;
        break;
    case EIO:
        win32Err = ERROR_WRITE_FAULT;
        break;
    case EMFILE:
        win32Err = ERROR_TOO_MANY_OPEN_FILES;
        break;
    case ERANGE:
        win32Err = ERROR_BAD_PATHNAME;
        break;
    default:
        win32Err = ERROR_GEN_FAILURE;
    }

    return HRESULT_FROM_WIN32(win32Err);
}
