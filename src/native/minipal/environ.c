// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "environ.h"
#include "strings.h"
#include "utf8.h"

#include <limits.h>
#include <stdlib.h>
#include <string.h>

#ifdef HOST_WINDOWS
#include <windows.h>
#elif defined(HOST_APPLE)
#include <crt_externs.h>
#endif

static uint32_t copy_to_buffer(const char* value, char* buffer, uint32_t buffer_size)
{
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
}

uint32_t minipal_environ_get_variable(const char* name, char* buffer, uint32_t buffer_size)
{
    if (name == NULL || name[0] == '\0' || strchr(name, '=') != NULL)
    {
        return 0;
    }

#ifdef HOST_WINDOWS
    return (uint32_t)GetEnvironmentVariableA(name, buffer, buffer_size);
#else
    const char* value = getenv(name);
    if (value == NULL)
    {
        return 0;
    }

    return copy_to_buffer(value, buffer, buffer_size);
#endif
}

static char* utf16_to_utf8_alloc(const CHAR16_T* source)
{
    if (source == NULL)
    {
        return NULL;
    }

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

uint32_t minipal_environ_get_variable_u16(const CHAR16_T* name, CHAR16_T* buffer, uint32_t buffer_size)
{
    if (name == NULL || name[0] == 0)
    {
        return 0;
    }

#ifdef HOST_WINDOWS
    return (uint32_t)GetEnvironmentVariableW((LPCWSTR)name, (LPWSTR)buffer, buffer_size);
#else
    char* name_utf8 = utf16_to_utf8_alloc(name);
    if (name_utf8 == NULL)
    {
        return 0;
    }

    char* value_utf8 = minipal_environ_getenv(name_utf8);
    free(name_utf8);
    if (value_utf8 == NULL)
    {
        return 0;
    }

    size_t value_len = strlen(value_utf8);
    size_t needed = minipal_get_length_utf8_to_utf16(value_utf8, value_len, 0);
    if (needed > UINT32_MAX - 1)
    {
        free(value_utf8);
        return 0;
    }

    if (buffer != NULL && needed < buffer_size)
    {
        size_t written = minipal_convert_utf8_to_utf16(value_utf8, value_len, buffer, buffer_size, 0);
        buffer[written] = 0;
        free(value_utf8);
        return (uint32_t)written;
    }

    free(value_utf8);
    return (uint32_t)(needed + 1);
#endif
}

bool minipal_environ_set_variable(const char* name, const char* value)
{
    if (name == NULL || name[0] == '\0' || strchr(name, '=') != NULL)
    {
        return false;
    }

#ifdef HOST_WINDOWS
    if (value == NULL)
    {
        if (GetEnvironmentVariableA(name, NULL, 0) == 0 && GetLastError() == ERROR_ENVVAR_NOT_FOUND)
        {
            return false;
        }
    }

    return SetEnvironmentVariableA(name, value) != 0;
#else
    if (value == NULL)
    {
        if (getenv(name) == NULL)
        {
            return false;
        }

        return unsetenv(name) == 0;
    }

    return setenv(name, value, 1) == 0;
#endif
}

bool minipal_environ_set_variable_u16(const CHAR16_T* name, const CHAR16_T* value)
{
    if (name == NULL || name[0] == 0)
    {
        return false;
    }

#ifdef HOST_WINDOWS
    if (value == NULL)
    {
        if (GetEnvironmentVariableW((LPCWSTR)name, NULL, 0) == 0 && GetLastError() == ERROR_ENVVAR_NOT_FOUND)
        {
            return false;
        }
    }

    return SetEnvironmentVariableW((LPCWSTR)name, (LPCWSTR)value) != 0;
#else
    char* name_utf8 = utf16_to_utf8_alloc(name);
    if (name_utf8 == NULL)
    {
        return false;
    }

    char* value_utf8 = NULL;
    if (value != NULL)
    {
        value_utf8 = utf16_to_utf8_alloc(value);
        if (value_utf8 == NULL)
        {
            free(name_utf8);
            return false;
        }
    }

    bool result = minipal_environ_set_variable(name_utf8, value_utf8);
    free(name_utf8);
    free(value_utf8);
    return result;
#endif
}

char* minipal_environ_getenv(const char* name)
{
    if (name == NULL || name[0] == '\0')
    {
        return NULL;
    }

#ifdef HOST_WINDOWS
    // Probe returns 0 if missing, 1+ (including null) if present — even when empty.
    DWORD needed = GetEnvironmentVariableA(name, NULL, 0);
    if (needed == 0)
    {
        return NULL;
    }

    char* value = (char*)malloc(needed);
    if (value == NULL)
    {
        return NULL;
    }

    DWORD written = GetEnvironmentVariableA(name, value, needed);
    // Empty values return 0 on success; treat truncation/growth as failure.
    if (written >= needed)
    {
        free(value);
        return NULL;
    }

    return value;
#else
    const char* value = getenv(name);
    if (value == NULL)
    {
        return NULL;
    }

    return strdup(value);
#endif
}

CHAR16_T* minipal_environ_get_strings_u16(void)
{
#ifdef HOST_WINDOWS
    LPWCH strings = GetEnvironmentStringsW();
    if (strings == NULL)
    {
        return NULL;
    }

    LPWCH end = strings;
    while (*end != 0)
    {
        while (*end != 0)
        {
            end++;
        }
        end++;
    }
    end++; // include the final extra null
    size_t count = (size_t)(end - strings);

    CHAR16_T* copy = (CHAR16_T*)malloc(count * sizeof(CHAR16_T));
    if (copy == NULL)
    {
        FreeEnvironmentStringsW(strings);
        return NULL;
    }

    memcpy(copy, strings, count * sizeof(CHAR16_T));
    FreeEnvironmentStringsW(strings);
    return copy;
#else
    char** env = minipal_environ_get();
    if (env == NULL)
    {
        return NULL;
    }

    size_t total_chars = 1; // final extra null
    for (int i = 0; env[i] != NULL; i++)
    {
        size_t len = strlen(env[i]);
        total_chars += minipal_get_length_utf8_to_utf16(env[i], len, 0) + 1;
    }

    CHAR16_T* block = (CHAR16_T*)malloc(total_chars * sizeof(CHAR16_T));
    if (block == NULL)
    {
        return NULL;
    }

    CHAR16_T* dest = block;
    size_t remaining = total_chars;
    for (int i = 0; env[i] != NULL; i++)
    {
        size_t len = strlen(env[i]);
        size_t written = minipal_convert_utf8_to_utf16(env[i], len, dest, remaining, 0);
        dest[written] = 0;
        dest += written + 1;
        remaining -= written + 1;
    }

    *dest = 0;
    return block;
#endif
}

void minipal_environ_free_strings_u16(void* strings)
{
    free(strings);
}

char** minipal_environ_get(void)
{
#ifdef HOST_WINDOWS
    // Windows callers should use minipal_environ_get_strings_u16.
    return NULL;
#elif defined(HOST_APPLE)
    return *(_NSGetEnviron());
#else
    extern char** environ;
    return environ;
#endif
}
