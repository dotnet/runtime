// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "ProcessEnvironment.h"

#ifdef TARGET_WINDOWS

#include <windows.h>

uint32_t ProcessEnvironment_GetVariable(const CHAR16_T* name, CHAR16_T* buffer, uint32_t bufferSize)
{
    return (uint32_t)GetEnvironmentVariableW((LPCWSTR)name, (LPWSTR)buffer, bufferSize);
}

uint32_t ProcessEnvironment_SetVariable(const CHAR16_T* name, const CHAR16_T* value)
{
    return SetEnvironmentVariableW((LPCWSTR)name, (LPCWSTR)value) ? 0 : (uint32_t)GetLastError();
}

#else // TARGET_WINDOWS

#include <minipal/environ.h>
#include <minipal/strings.h>
#include <minipal/utf8.h>

#include <pthread.h>
#include <stdlib.h>
#include <string.h>

static pthread_mutex_t s_envLock = PTHREAD_MUTEX_INITIALIZER;
// Null until a variable is first set. It is never reset after that.
static char** s_env;
static size_t s_envCount;
static size_t s_envCapacity;

// Until a variable is set, reads go to the process environment. The copy is made on the first set,
// and changes made through libc setenv after that are not visible.
static bool EnsureEnvironmentLocked()
{
    if (s_env != nullptr)
    {
        return true;
    }

    char** source = minipal_environ_get();
    size_t count = 0;
    while (source != nullptr && source[count] != nullptr)
    {
        count++;
    }

    size_t capacity = count < 16 ? 16 : count + 1;
    char** env = (char**)malloc(capacity * sizeof(char*));
    if (env == nullptr)
    {
        return false;
    }

    for (size_t i = 0; i < count; i++)
    {
        env[i] = strdup(source[i]);
        if (env[i] == nullptr)
        {
            while (i > 0)
            {
                free(env[--i]);
            }
            free(env);
            return false;
        }
    }

    env[count] = nullptr;
    s_envCount = count;
    s_envCapacity = capacity;
    __atomic_store_n(&s_env, env, __ATOMIC_RELEASE);
    return true;
}

static bool IsEnvironmentModified()
{
    return __atomic_load_n(&s_env, __ATOMIC_ACQUIRE) != nullptr;
}

// Returns the index of name in s_env, or s_envCount if it is not present.
static size_t FindVariableLocked(const char* name)
{
    size_t nameLength = strlen(name);
    for (size_t i = 0; i < s_envCount; i++)
    {
        if (strncmp(s_env[i], name, nameLength) == 0 && s_env[i][nameLength] == '=')
        {
            return i;
        }
    }

    return s_envCount;
}

// Returns the value, or null if the variable is not set. Until the environment is modified, the value
// points into the process environment. After that, s_envLock is acquired and *locked is set to true;
// the value is only valid until the caller releases the lock.
static const char* FindValueAndLock(const char* name, bool* locked)
{
    *locked = false;
    if (name[0] == '\0' || strchr(name, '=') != nullptr)
    {
        return nullptr;
    }

    if (!IsEnvironmentModified())
    {
        return getenv(name);
    }

    pthread_mutex_lock(&s_envLock);
    *locked = true;
    size_t index = FindVariableLocked(name);
    return index < s_envCount ? s_env[index] + strlen(name) + 1 : nullptr;
}

static uint32_t SetVariableUtf8(const char* name, const char* value)
{
    if (name[0] == '\0' || strchr(name, '=') != nullptr)
    {
        return PROCESSENVIRONMENT_ERROR_INVALID_PARAMETER;
    }

    // Unsetting a variable that is not set does not change the environment, so it does not need a copy.
    if (value == nullptr && !IsEnvironmentModified() && getenv(name) == nullptr)
    {
        return PROCESSENVIRONMENT_ERROR_ENVVAR_NOT_FOUND;
    }

    char* entry = nullptr;
    if (value != nullptr)
    {
        size_t nameLength = strlen(name);
        size_t valueLength = strlen(value);
        entry = (char*)malloc(nameLength + valueLength + 2);
        if (entry == nullptr)
        {
            return PROCESSENVIRONMENT_ERROR_NOT_ENOUGH_MEMORY;
        }

        memcpy(entry, name, nameLength);
        entry[nameLength] = '=';
        memcpy(entry + nameLength + 1, value, valueLength + 1);
    }

    uint32_t result = PROCESSENVIRONMENT_ERROR_NOT_ENOUGH_MEMORY;
    pthread_mutex_lock(&s_envLock);
    if (EnsureEnvironmentLocked())
    {
        size_t index = FindVariableLocked(name);
        if (entry == nullptr)
        {
            if (index < s_envCount)
            {
                free(s_env[index]);
                memmove(&s_env[index], &s_env[index + 1], (s_envCount - index) * sizeof(char*));
                s_envCount--;
                result = 0;
            }
            else
            {
                result = PROCESSENVIRONMENT_ERROR_ENVVAR_NOT_FOUND;
            }
        }
        else if (index < s_envCount)
        {
            free(s_env[index]);
            s_env[index] = entry;
            entry = nullptr;
            result = 0;
        }
        else
        {
            if (s_envCount + 1 >= s_envCapacity)
            {
                size_t newCapacity = s_envCapacity * 2;
                char** newEnv = (char**)realloc(s_env, newCapacity * sizeof(char*));
                if (newEnv != nullptr)
                {
                    __atomic_store_n(&s_env, newEnv, __ATOMIC_RELEASE);
                    s_envCapacity = newCapacity;
                }
            }

            if (s_envCount + 1 < s_envCapacity)
            {
                s_env[s_envCount++] = entry;
                s_env[s_envCount] = nullptr;
                entry = nullptr;
                result = 0;
            }
        }
    }
    pthread_mutex_unlock(&s_envLock);

    free(entry);
    return result;
}

static char* Utf16ToUtf8(const CHAR16_T* source)
{
    size_t sourceLength = minipal_u16_strlen(source);
    size_t length = minipal_get_length_utf16_to_utf8(source, sourceLength, 0);
    char* result = (char*)malloc(length + 1);
    if (result == nullptr)
    {
        return nullptr;
    }

    size_t written = minipal_convert_utf16_to_utf8(source, sourceLength, result, length + 1, 0);
    result[written] = '\0';
    return result;
}

uint32_t ProcessEnvironment_GetVariable(const CHAR16_T* name, CHAR16_T* buffer, uint32_t bufferSize)
{
    if (name == nullptr || name[0] == 0)
    {
        return 0;
    }

    char* nameUtf8 = Utf16ToUtf8(name);
    if (nameUtf8 == nullptr)
    {
        return 0;
    }

    bool locked;
    const char* value = FindValueAndLock(nameUtf8, &locked);
    free(nameUtf8);

    uint32_t result = 0;
    if (value != nullptr)
    {
        size_t valueLength = strlen(value);
        size_t needed = minipal_get_length_utf8_to_utf16(value, valueLength, 0);
        if (needed <= UINT32_MAX - 1)
        {
            if (buffer != nullptr && needed < bufferSize)
            {
                size_t written = minipal_convert_utf8_to_utf16(value, valueLength, buffer, bufferSize, 0);
                buffer[written] = 0;
                result = (uint32_t)written;
            }
            else
            {
                result = (uint32_t)(needed + 1);
            }
        }
    }

    if (locked)
    {
        pthread_mutex_unlock(&s_envLock);
    }
    return result;
}

uint32_t ProcessEnvironment_SetVariable(const CHAR16_T* name, const CHAR16_T* value)
{
    if (name == nullptr || name[0] == 0)
    {
        return PROCESSENVIRONMENT_ERROR_INVALID_PARAMETER;
    }

    char* nameUtf8 = Utf16ToUtf8(name);
    if (nameUtf8 == nullptr)
    {
        return PROCESSENVIRONMENT_ERROR_NOT_ENOUGH_MEMORY;
    }

    char* valueUtf8 = nullptr;
    if (value != nullptr)
    {
        valueUtf8 = Utf16ToUtf8(value);
        if (valueUtf8 == nullptr)
        {
            free(nameUtf8);
            return PROCESSENVIRONMENT_ERROR_NOT_ENOUGH_MEMORY;
        }
    }

    uint32_t result = SetVariableUtf8(nameUtf8, valueUtf8);
    free(nameUtf8);
    free(valueUtf8);
    return result;
}

uint32_t ProcessEnvironment_GetVariableUtf8(const char* name, char* buffer, uint32_t bufferSize)
{
    bool locked;
    const char* value = FindValueAndLock(name, &locked);

    uint32_t result = 0;
    if (value != nullptr)
    {
        size_t valueLength = strlen(value);
        if (valueLength <= UINT32_MAX - 1)
        {
            if (buffer != nullptr && valueLength < bufferSize)
            {
                memcpy(buffer, value, valueLength + 1);
                result = (uint32_t)valueLength;
            }
            else
            {
                result = (uint32_t)(valueLength + 1);
            }
        }
    }

    if (locked)
    {
        pthread_mutex_unlock(&s_envLock);
    }
    return result;
}

void ProcessEnvironment_EnumerateVariablesUtf8(ProcessEnvironmentEntryCallback callback, void* context)
{
    if (!IsEnvironmentModified())
    {
        for (char** entry = minipal_environ_get(); entry != nullptr && *entry != nullptr; entry++)
        {
            callback(*entry, context);
        }
        return;
    }

    pthread_mutex_lock(&s_envLock);
    for (size_t i = 0; i < s_envCount; i++)
    {
        callback(s_env[i], context);
    }
    pthread_mutex_unlock(&s_envLock);
}

//
// QCalls for System.Environment. Names and values are UTF-8.
//

// Returns the value, or null if the variable is not set. If *needsFree is set, the value is a copy; free it
// with free(). Null with *needsFree set means out of memory.
extern "C" const char* QCALLTYPE Environment_GetEnvironmentVariable(const char* name, int32_t* needsFree)
{
    *needsFree = 0;

    bool locked;
    const char* value = FindValueAndLock(name, &locked);
    if (!locked)
    {
        return value;
    }

    // The value is only valid while the lock is held.
    char* copy = nullptr;
    if (value != nullptr)
    {
        copy = strdup(value);
        *needsFree = 1;
    }
    pthread_mutex_unlock(&s_envLock);
    return copy;
}

// Returns 0 on success, or a PROCESSENVIRONMENT_ERROR_* code.
extern "C" int32_t QCALLTYPE Environment_SetEnvironmentVariable(const char* name, const char* value)
{
    return (int32_t)SetVariableUtf8(name, value);
}

// Returns the null-terminated array of "name=value" strings. If *needsFree is set, the array is a copy,
// with the strings stored in the same allocation; free it with free(). Null with *needsFree set means out of memory.
extern "C" char** QCALLTYPE Environment_GetEnviron(int32_t* needsFree)
{
    if (!IsEnvironmentModified())
    {
        *needsFree = 0;
        return minipal_environ_get();
    }

    *needsFree = 1;
    pthread_mutex_lock(&s_envLock);
    size_t stringsLength = 0;
    for (size_t i = 0; i < s_envCount; i++)
    {
        stringsLength += strlen(s_env[i]) + 1;
    }

    char** block = (char**)malloc((s_envCount + 1) * sizeof(char*) + stringsLength);
    if (block != nullptr)
    {
        char* dest = (char*)&block[s_envCount + 1];
        for (size_t i = 0; i < s_envCount; i++)
        {
            size_t length = strlen(s_env[i]) + 1;
            memcpy(dest, s_env[i], length);
            block[i] = dest;
            dest += length;
        }

        block[s_envCount] = nullptr;
    }
    pthread_mutex_unlock(&s_envLock);
    return block;
}

#endif // TARGET_WINDOWS
