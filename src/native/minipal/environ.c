// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "environ.h"
#include "strings.h"
#include "utf8.h"

#include <assert.h>
#include <limits.h>
#include <stdlib.h>
#include <string.h>

#ifdef HOST_WINDOWS
#include <windows.h>
#elif defined(HOST_APPLE)
#include <crt_externs.h>
#endif

uint32_t minipal_environ_get_variable(const char* name, char* buffer, uint32_t buffer_size)
{
    assert(name != NULL && name[0] != '\0' && strchr(name, '=') == NULL);

#ifdef HOST_WINDOWS
    return (uint32_t)GetEnvironmentVariableA(name, buffer, buffer_size);
#else
    const char* value = getenv(name);
    if (value == NULL)
    {
        return 0;
    }

    size_t value_len = strlen(value);
    if (value_len > UINT32_MAX - 1)
    {
        return 0;
    }

    if (buffer != NULL && value_len < buffer_size)
    {
        memcpy(buffer, value, value_len + 1);
        return (uint32_t)value_len;
    }

    return (uint32_t)(value_len + 1);
#endif
}

uint32_t minipal_environ_get_variable_u16(const CHAR16_T* name, CHAR16_T* buffer, uint32_t buffer_size)
{
    assert(name != NULL && name[0] != 0);

#ifdef HOST_WINDOWS
    return (uint32_t)GetEnvironmentVariableW((LPCWSTR)name, (LPWSTR)buffer, buffer_size);
#else
    char name_buffer[256];
    size_t name_len = minipal_u16_strlen(name);
    char* name_utf8 = name_buffer;
    size_t name_utf8_len = sizeof(name_buffer) - 1;
    if (name_len >= (sizeof(name_buffer) - 1) / 3)
    {
        name_utf8_len = minipal_get_length_utf16_to_utf8(name, name_len, 0);
        if (name_utf8_len >= sizeof(name_buffer))
        {
            name_utf8 = (char*)malloc(name_utf8_len + 1);
            if (name_utf8 == NULL)
            {
                return 0;
            }
        }
    }

    size_t name_written = minipal_convert_utf16_to_utf8(name, name_len, name_utf8, name_utf8_len + 1, 0);
    name_utf8[name_written] = '\0';

    assert(strchr(name_utf8, '=') == NULL);
    const char* value = getenv(name_utf8);
    if (name_utf8 != name_buffer)
    {
        free(name_utf8);
    }
    if (value == NULL)
    {
        return 0;
    }

    size_t value_len = strlen(value);
    size_t needed = minipal_get_length_utf8_to_utf16(value, value_len, 0);
    if (needed > UINT32_MAX - 1)
    {
        return 0;
    }

    if (buffer != NULL && needed < buffer_size)
    {
        size_t written = minipal_convert_utf8_to_utf16(value, value_len, buffer, buffer_size, 0);
        buffer[written] = 0;
        return (uint32_t)written;
    }

    return (uint32_t)(needed + 1);
#endif
}

#ifndef HOST_WINDOWS
char** minipal_environ_get(void)
{
#ifdef HOST_APPLE
    return *(_NSGetEnviron());
#else
    extern char** environ;
    return environ;
#endif
}
#endif // !HOST_WINDOWS
