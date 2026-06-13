// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementations of the pal_* APIs needed by trace.c on Windows.

#include "pal.h"
#include "trace.h"

#include <stdlib.h>

pal_char_t* pal_get_own_executable_path(void)
{
    // GetModuleFileNameW returns 0 on failure, the number of characters
    // written (not including the terminating null) on success, and the buffer
    // size to signal that the buffer was too small. Start with MAX_PATH and
    // double until the result fits.
    DWORD size = MAX_PATH / 2;
    pal_char_t* buf = NULL;
    DWORD size_written;
    do
    {
        size *= 2;
        pal_char_t* new_buf = (pal_char_t*)realloc(buf, size * sizeof(pal_char_t));
        if (new_buf == NULL)
        {
            free(buf);
            return NULL;
        }
        buf = new_buf;

        size_written = GetModuleFileNameW(NULL, buf, size);
    } while (size_written == size);

    if (size_written == 0)
    {
        free(buf);
        return NULL;
    }

    return buf;
}

bool pal_directory_exists(const pal_char_t* path)
{
    DWORD attributes = GetFileAttributesW(path);
    return attributes != INVALID_FILE_ATTRIBUTES && (attributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
}

pal_char_t* pal_getenv(const pal_char_t* name)
{
    DWORD needed = GetEnvironmentVariableW(name, NULL, 0);
    if (needed == 0)
    {
        DWORD err = GetLastError();
        if (err != ERROR_ENVVAR_NOT_FOUND && err != ERROR_SUCCESS)
        {
            trace_warning(_X("Failed to read environment variable [%s], HRESULT: 0x%X"), name, HRESULT_FROM_WIN32(err));
        }
        return NULL;
    }

    pal_char_t* result = (pal_char_t*)malloc(needed * sizeof(pal_char_t));
    if (result == NULL)
        return NULL;

    DWORD written = GetEnvironmentVariableW(name, result, needed);
    if (written == 0 || written >= needed)
    {
        DWORD err = GetLastError();
        if (err != ERROR_ENVVAR_NOT_FOUND && err != ERROR_SUCCESS)
        {
            trace_warning(_X("Failed to read environment variable [%s], HRESULT: 0x%X"), name, HRESULT_FROM_WIN32(err));
        }
        free(result);
        return NULL;
    }

    return result;
}
