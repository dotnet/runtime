// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <dn-stdio.h>

int u16_fopen_s(FILE** stream, const WCHAR* path, const WCHAR* mode)
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

int64_t ftell_64(FILE* stream)
{
    return _ftelli64(stream);
}

int fsetpos_64(FILE* stream, int64_t pos)
{
    fpos_t fpos = pos;
    return fsetpos(stream, &fpos);
}

HRESULT HRESULTFromErr(int err)
{
    // maps the common I/O errors
    // based on FILEGetLastErrorFromErrno

    // stdio functions aren't guaranteed to preserve GetLastError.
    // errno/ferror should be used as source of truth.
    
    DWORD win32Err;

    switch(err)
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
