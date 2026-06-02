// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementations of the utils_* APIs needed by trace.c.

#include "utils.h"

#include <string.h>

bool utils_get_filename(const pal_char_t* path, pal_char_t* out_name, size_t out_name_len)
{
    if (path == NULL)
    {
        if (out_name_len > 0)
            out_name[0] = _X('\0');

        return true;
    }

    const pal_char_t* last_sep = pal_strrchr(path, DIR_SEPARATOR);
    const pal_char_t* name = (last_sep != NULL) ? last_sep + 1 : path;
    size_t len = pal_strlen(name);
    if (len >= out_name_len)
    {
        if (out_name_len > 0)
            out_name[0] = _X('\0');

        return false;
    }

    memcpy(out_name, name, (len + 1) * sizeof(pal_char_t));
    return true;
}
