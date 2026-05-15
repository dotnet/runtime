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

pal_char_t* pal_getenv(const pal_char_t* name)
{
    DWORD needed = GetEnvironmentVariableW(name, NULL, 0);
    if (needed == 0)
        return NULL; // unset, empty, or other failure

    pal_char_t* result = (pal_char_t*)malloc(needed * sizeof(pal_char_t));
    if (result == NULL)
        return NULL;

    DWORD written = GetEnvironmentVariableW(name, result, needed);
    if (written == 0 || written >= needed)
    {
        // The variable disappeared between the probe and the read, or some
        // other failure occurred. Don't return a partial buffer.
        free(result);
        return NULL;
    }

    return result;
}
