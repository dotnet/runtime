// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementations of the pal_* APIs needed by trace.c on non-Windows.

#include "pal.h"
#include "trace.h"

#include <dirent.h>
#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <unistd.h>

#include "config.h"
#include <minipal/getexepath.h>

pal_char_t* pal_get_own_executable_path(void)
{
    return minipal_getexepath();
}

bool pal_directory_exists(const pal_char_t* path)
{
    struct stat sb;
    if (stat(path, &sb) != 0)
        return false;

    return S_ISDIR(sb.st_mode);
}

pal_char_t* pal_strdup(const pal_char_t* str)
{
    return strdup(str);
}

pal_char_t* pal_getenv(const pal_char_t* name)
{
    const char* result = getenv(name);
    if (result == NULL || result[0] == '\0')
        return NULL;

    return strdup(result);
}

pal_char_t* pal_fullpath(const pal_char_t* path, bool skip_error_logging)
{
    if (path == NULL)
        return NULL;

    char* resolved = realpath(path, NULL);
    if (resolved == NULL)
    {
        if (errno != ENOENT && !skip_error_logging)
            trace_error(_X("realpath(%s) failed: %s"), path, strerror(errno));
        return NULL;
    }

    return resolved;
}

bool pal_file_exists(const pal_char_t* path)
{
    return access(path, F_OK) == 0;
}

bool pal_readdir_onlydirectories(const pal_char_t* path, pal_readdir_callback_t callback, void* ctx)
{
    if (path == NULL || callback == NULL)
        return false;

    DIR* dir = opendir(path);
    if (dir == NULL)
        return false;

    struct dirent* entry;
    while ((entry = readdir(dir)) != NULL)
    {
#if HAVE_DIRENT_D_TYPE
        int entry_type = entry->d_type;
#else
        int entry_type = DT_UNKNOWN;
#endif

        bool is_dir;
        switch (entry_type)
        {
        case DT_DIR:
            is_dir = true;
            break;

        case DT_LNK:
        case DT_UNKNOWN:
        {
            // Resolve via stat for symlinks and file systems that do not
            // provide d_type.
            struct stat sb;
            if (fstatat(dirfd(dir), entry->d_name, &sb, 0) == -1)
                continue;

            is_dir = S_ISDIR(sb.st_mode);
            break;
        }

        default:
            continue;
        }

        if (!is_dir)
            continue;

        if (strcmp(entry->d_name, ".") == 0 || strcmp(entry->d_name, "..") == 0)
            continue;

        if (!callback(entry->d_name, ctx))
            break;
    }

    closedir(dir);
    return true;
}

bool pal_is_running_in_wow64(void)
{
    return false;
}
