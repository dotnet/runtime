// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementations of the pal_* APIs needed by trace.c on Windows.

#include "pal.h"
#include "trace.h"

#include <stdlib.h>
#include <string.h>

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

pal_char_t* pal_strdup(const pal_char_t* str)
{
    return _wcsdup(str);
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

// Returns true if the path is already prefixed with one of the long-path
// extended-syntax prefixes:
//   \\?\        (extended path prefix - includes \\?\UNC\)
//   \\.\        (device path prefix)
// Mirrors LongFile::IsNormalized.
static bool is_path_normalized(const pal_char_t* path)
{
    if (path[0] == L'\0')
        return true;

    return path[0] == L'\\'
        && path[1] == L'\\'
        && (path[2] == L'?' || path[2] == L'.')
        && path[3] == L'\\';
}

pal_char_t* pal_fullpath(const pal_char_t* path, bool skip_error_logging)
{
    if (path == NULL || path[0] == L'\0')
        return NULL;

    // Already-normalized (long-path-prefixed) paths are passed through as-is;
    // GetFullPathNameW does not handle the \\?\ prefix correctly.
    if (is_path_normalized(path))
    {
        WIN32_FILE_ATTRIBUTE_DATA data;
        if (GetFileAttributesExW(path, GetFileExInfoStandard, &data) != 0)
            return pal_strdup(path);
        // Fall through and let GetFullPathNameW have a chance, matching C++ pal::fullpath.
    }

    // Start with a MAX_PATH-sized buffer; the typical case fits.
    pal_char_t* buf = (pal_char_t*)malloc(MAX_PATH * sizeof(pal_char_t));
    if (buf == NULL)
        return NULL;

    DWORD size = GetFullPathNameW(path, MAX_PATH, buf, NULL);
    if (size == 0)
    {
        if (!skip_error_logging)
            trace_error(_X("Error resolving full path [%s]"), path);
        free(buf);
        return NULL;
    }

    if (size >= MAX_PATH)
    {
        // Need a larger buffer. Allocate enough room for the canonicalized
        // path plus the longest long-path prefix ("\\?\UNC\" = 8 chars).
        const DWORD prefix_headroom = 8;
        pal_char_t* new_buf = (pal_char_t*)realloc(buf, (size + prefix_headroom) * sizeof(pal_char_t));
        if (new_buf == NULL)
        {
            free(buf);
            return NULL;
        }
        buf = new_buf;

        DWORD new_size = GetFullPathNameW(path, size, buf, NULL);
        if (new_size == 0 || new_size >= size)
        {
            if (!skip_error_logging)
                trace_error(_X("Error resolving full path [%s]"), path);
            free(buf);
            return NULL;
        }

        // Long paths require the \\?\ (or \\?\UNC\) prefix to be usable.
        // For UNC paths (\\server\share\...), strip the leading "\\" and
        // prepend "\\?\UNC\"; otherwise just prepend "\\?\".
        bool is_unc = (buf[0] == L'\\' && buf[1] == L'\\');
        const pal_char_t* prefix = is_unc ? L"\\\\?\\UNC\\" : L"\\\\?\\";
        DWORD prefix_len = is_unc ? 8 : 4;
        DWORD skip = is_unc ? 2 : 0;

        // Make room for the prefix by shifting the path right (including the NUL).
        memmove(buf + prefix_len, buf + skip, (new_size - skip + 1) * sizeof(pal_char_t));
        memcpy(buf, prefix, prefix_len * sizeof(pal_char_t));
    }

    WIN32_FILE_ATTRIBUTE_DATA data;
    if (GetFileAttributesExW(buf, GetFileExInfoStandard, &data) == 0)
    {
        free(buf);
        return NULL;
    }

    return buf;
}

bool pal_file_exists(const pal_char_t* path)
{
    // Matches the C++ pal::file_exists semantics: canonicalize and verify
    // existence (with logging suppressed). This handles long paths via the
    // \\?\ prefix machinery in pal_fullpath.
    pal_char_t* resolved = pal_fullpath(path, true);
    bool exists = resolved != NULL;
    free(resolved);
    return exists;
}

bool pal_readdir_onlydirectories(const pal_char_t* path, pal_readdir_callback_t callback, void* ctx)
{
    if (path == NULL || callback == NULL)
        return false;

    // Build the search string: path + "\\*". One extra char beyond path
    // length is needed for the separator if path doesn't already end with one.
    size_t path_len = wcslen(path);
    size_t search_len = path_len + 3; // worst case: '\\', '*', NUL
    pal_char_t* search = (pal_char_t*)malloc(search_len * sizeof(pal_char_t));
    if (search == NULL)
        return false;

    memcpy(search, path, path_len * sizeof(pal_char_t));
    size_t pos = path_len;
    if (pos == 0 || (search[pos - 1] != L'\\' && search[pos - 1] != L'/'))
    {
        search[pos++] = L'\\';
    }
    search[pos++] = L'*';
    search[pos] = L'\0';

    WIN32_FIND_DATAW data = { 0 };
    HANDLE handle = FindFirstFileExW(search, FindExInfoStandard, &data, FindExSearchNameMatch, NULL, 0);
    free(search);
    if (handle == INVALID_HANDLE_VALUE)
        return false;

    do
    {
        if ((data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0)
            continue;

        const pal_char_t* name = data.cFileName;
        if (name[0] == L'.' && (name[1] == L'\0' || (name[1] == L'.' && name[2] == L'\0')))
            continue;

        if (!callback(name, ctx))
            break;
    } while (FindNextFileW(handle, &data));

    FindClose(handle);
    return true;
}

bool pal_is_running_in_wow64(void)
{
    BOOL is_wow64 = FALSE;
    if (!IsWow64Process(GetCurrentProcess(), &is_wow64))
        return false;
    return is_wow64 != FALSE;
}
