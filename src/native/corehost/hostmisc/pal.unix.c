// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementations of the pal_* APIs needed by trace.c on non-Windows.

#include "pal.h"

#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/types.h>

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

pal_char_t* pal_getenv(const pal_char_t* name)
{
    const char* result = getenv(name);
    if (result == NULL || result[0] == '\0')
        return NULL;

    return strdup(result);
}
