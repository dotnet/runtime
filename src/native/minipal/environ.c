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
#else
#include <pthread.h>
#ifdef HOST_APPLE
#include <crt_externs.h>
#endif
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

#ifndef HOST_WINDOWS
// setenv/unsetenv are not safe while other threads read the environment, so on Unix
// the process environment is cloned on first use and all access goes through this
// lock-protected copy. Changes are not visible to libc getenv in this process; child
// processes see them only when launched with minipal_environ_get() as their environment.
static pthread_mutex_t s_env_lock = PTHREAD_MUTEX_INITIALIZER;
static char** s_env;
static size_t s_env_count;
static size_t s_env_capacity;

static char** os_environ(void)
{
#ifdef HOST_APPLE
    return *(_NSGetEnviron());
#else
    extern char** environ;
    return environ;
#endif
}

static bool ensure_env_locked(void)
{
    if (s_env != NULL)
    {
        return true;
    }

    char** source = os_environ();
    size_t count = 0;
    while (source != NULL && source[count] != NULL)
    {
        count++;
    }

    size_t capacity = count < 16 ? 16 : count + 1;
    char** env = (char**)malloc(capacity * sizeof(char*));
    if (env == NULL)
    {
        return false;
    }

    for (size_t i = 0; i < count; i++)
    {
        env[i] = strdup(source[i]);
        if (env[i] == NULL)
        {
            while (i > 0)
            {
                free(env[--i]);
            }
            free(env);
            return false;
        }
    }

    env[count] = NULL;
    s_env = env;
    s_env_count = count;
    s_env_capacity = capacity;
    return true;
}

// Returns the index of name in s_env, or s_env_count if not present.
static size_t find_env_locked(const char* name)
{
    size_t name_len = strlen(name);
    for (size_t i = 0; i < s_env_count; i++)
    {
        if (strncmp(s_env[i], name, name_len) == 0 && s_env[i][name_len] == '=')
        {
            return i;
        }
    }

    return s_env_count;
}

static const char* get_value_locked(const char* name)
{
    if (!ensure_env_locked())
    {
        return NULL;
    }

    size_t index = find_env_locked(name);
    return index < s_env_count ? s_env[index] + strlen(name) + 1 : NULL;
}
#endif // !HOST_WINDOWS

uint32_t minipal_environ_get_variable(const char* name, char* buffer, uint32_t buffer_size)
{
    if (name == NULL || name[0] == '\0' || strchr(name, '=') != NULL)
    {
        return 0;
    }

#ifdef HOST_WINDOWS
    return (uint32_t)GetEnvironmentVariableA(name, buffer, buffer_size);
#else
    uint32_t result = 0;
    pthread_mutex_lock(&s_env_lock);
    const char* value = get_value_locked(name);
    if (value != NULL)
    {
        result = copy_to_buffer(value, buffer, buffer_size);
    }
    pthread_mutex_unlock(&s_env_lock);
    return result;
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
    char* entry = NULL;
    if (value != NULL)
    {
        size_t name_len = strlen(name);
        size_t value_len = strlen(value);
        entry = (char*)malloc(name_len + value_len + 2);
        if (entry == NULL)
        {
            return false;
        }

        memcpy(entry, name, name_len);
        entry[name_len] = '=';
        memcpy(entry + name_len + 1, value, value_len + 1);
    }

    bool result = false;
    pthread_mutex_lock(&s_env_lock);
    if (ensure_env_locked())
    {
        size_t index = find_env_locked(name);
        if (entry == NULL)
        {
            if (index < s_env_count)
            {
                free(s_env[index]);
                memmove(&s_env[index], &s_env[index + 1], (s_env_count - index) * sizeof(char*));
                s_env_count--;
                result = true;
            }
        }
        else if (index < s_env_count)
        {
            free(s_env[index]);
            s_env[index] = entry;
            entry = NULL;
            result = true;
        }
        else
        {
            if (s_env_count + 1 >= s_env_capacity)
            {
                size_t new_capacity = s_env_capacity * 2;
                char** new_env = (char**)realloc(s_env, new_capacity * sizeof(char*));
                if (new_env != NULL)
                {
                    s_env = new_env;
                    s_env_capacity = new_capacity;
                }
            }

            if (s_env_count + 1 < s_env_capacity)
            {
                s_env[s_env_count++] = entry;
                s_env[s_env_count] = NULL;
                entry = NULL;
                result = true;
            }
        }
    }
    pthread_mutex_unlock(&s_env_lock);

    free(entry);
    return result;
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
    if (strchr(name, '=') != NULL)
    {
        return NULL;
    }

    char* result = NULL;
    pthread_mutex_lock(&s_env_lock);
    const char* value = get_value_locked(name);
    if (value != NULL)
    {
        result = strdup(value);
    }
    pthread_mutex_unlock(&s_env_lock);
    return result;
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
    CHAR16_T* block = NULL;
    pthread_mutex_lock(&s_env_lock);
    if (ensure_env_locked())
    {
        size_t total_chars = 1; // final extra null
        for (size_t i = 0; i < s_env_count; i++)
        {
            size_t len = strlen(s_env[i]);
            total_chars += minipal_get_length_utf8_to_utf16(s_env[i], len, 0) + 1;
        }

        block = (CHAR16_T*)malloc(total_chars * sizeof(CHAR16_T));
        if (block != NULL)
        {
            CHAR16_T* dest = block;
            size_t remaining = total_chars;
            for (size_t i = 0; i < s_env_count; i++)
            {
                size_t len = strlen(s_env[i]);
                size_t written = minipal_convert_utf8_to_utf16(s_env[i], len, dest, remaining, 0);
                dest[written] = 0;
                dest += written + 1;
                remaining -= written + 1;
            }

            *dest = 0;
        }
    }
    pthread_mutex_unlock(&s_env_lock);
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
#else
    // No locking or allocation: this is called between fork and exec.
    return s_env != NULL ? s_env : os_environ();
#endif
}
