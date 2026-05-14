// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementations of the pal_* APIs needed by trace.c on Windows.

#include "pal.h"

bool pal_get_own_executable_path(pal_char_t* recv, size_t recv_len)
{
    if (recv_len == 0)
        return false;

    DWORD result = GetModuleFileNameW(NULL, recv, (DWORD)recv_len);
    // result == 0 means failure; result == recv_len means the buffer was too small
    // and the path was truncated (the function returns the buffer size in that case
    // and ERROR_INSUFFICIENT_BUFFER is set). The caller-supplied buffer is sized
    // for APPHOST_PATH_MAX which is plenty for any reasonable executable path.
    return result > 0 && result < (DWORD)recv_len;
}

bool pal_directory_exists(const pal_char_t* path)
{
    // Use GetFileAttributesW directly for a true "is a directory" check, matching
    // Unix stat() + S_ISDIR semantics. The C++ pal::directory_exists is an alias
    // for pal::file_exists which would also return true for regular files.
    DWORD attributes = GetFileAttributesW(path);
    return attributes != INVALID_FILE_ATTRIBUTES && (attributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
}
