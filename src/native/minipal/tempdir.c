// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include <string.h>

#include "tempdir.h"

#ifdef _WIN32
#define MINIPAL_TEMP_SEPARATOR '\\'
#define MINIPAL_TEMP_FALLBACK  "C:\\Temp\\"
#else
#define MINIPAL_TEMP_SEPARATOR '/'
#define MINIPAL_TEMP_FALLBACK  "/tmp/"
#endif

static bool minipal_write_tempdir(char* buffer, size_t buffer_size, const char* value)
{
    size_t valueLen = strlen(value);
    if (valueLen == 0)
    {
        return false;
    }

    bool needsSeparator = value[valueLen - 1] != MINIPAL_TEMP_SEPARATOR;
    size_t required = valueLen + (needsSeparator ? 1u : 0u) + 1u; // +1 for NUL
    if (required > buffer_size)
    {
        return false;
    }

    memcpy(buffer, value, valueLen);
    if (needsSeparator)
    {
        buffer[valueLen] = MINIPAL_TEMP_SEPARATOR;
        buffer[valueLen + 1] = '\0';
    }
    else
    {
        buffer[valueLen] = '\0';
    }

    return true;
}

bool minipal_get_tempdir(char* buffer, size_t buffer_size)
{
    if (buffer == NULL || buffer_size == 0)
    {
        return false;
    }

    buffer[0] = '\0';

    static const char* const envVars[] =
    {
#ifdef _WIN32
        "TMP",
        "TEMP",
#else
        "TMPDIR",
#endif
    };

    for (size_t i = 0; i < sizeof(envVars) / sizeof(envVars[0]); ++i)
    {
        const char* value = getenv(envVars[i]);
        if (value != NULL && value[0] != '\0')
        {
            if (minipal_write_tempdir(buffer, buffer_size, value))
            {
                return true;
            }
        }
    }

    return minipal_write_tempdir(buffer, buffer_size, MINIPAL_TEMP_FALLBACK);
}
