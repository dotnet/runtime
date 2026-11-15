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

#ifndef HOST_WINDOWS
static char* utf16_to_utf8_alloc(const CHAR16_T* source)
{
    size_t source_len = minipal_u16_strlen(source);
    size_t dest_len = minipal_get_length_utf16_to_utf8(source, source_len, 0);
    char* dest = (char*)malloc(dest_len + 1);
    if (dest == NULL)
    {
        return NULL;
    }

    size_t written = minipal_convert_utf16_to_utf8(source, source_len, dest, dest_len + 1, 0);
    dest[written] = '\0';
    return dest;
}
#endif

uint32_t minipal_environ_get_variable_u16(const CHAR16_T* name, CHAR16_T* buffer, uint32_t buffer_size)
{
    assert(name != NULL && name[0] != 0);

#ifdef HOST_WINDOWS
    return (uint32_t)GetEnvironmentVariableW((LPCWSTR)name, (LPWSTR)buffer, buffer_size);
#else
    char* name_utf8 = utf16_to_utf8_alloc(name);
    if (name_utf8 == NULL)
    {
        return 0;
    }

    assert(strchr(name_utf8, '=') == NULL);
    const char* value = getenv(name_utf8);
    free(name_utf8);
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
