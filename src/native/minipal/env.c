// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_NO_WARNINGS
#endif

#include "minipalconfig.h"
#include <errno.h>
#include <assert.h>
#include <stddef.h>
#include <ctype.h>
#include <string.h>
#include <stdio.h>
#include "minipal/env.h"
#include "minipal/volatile.h"
#include "minipal/atomic.h"
#include "minipal/utils.h"
#include "minipal/strings.h"
#include "minipal/mutex.h"

#if HAVE_CRT_EXTERNS_H
#include <crt_externs.h>
#endif

/**
 * Structure representing a dynamic array of environment variable strings.
 * Each entry is a string in the form "NAME=VALUE".
 */
typedef struct _EnvironmentVariables
{
    char ** data;
    int size;
    int capacity;
} EnvironmentVariables;

// Global lock protecting g_env.
static minipal_mutex* g_env_lock;

// Global flag tracking if environment is loaded and cached.
static bool g_env_loaded;

// Global environment, only accessed when holding lock,
// except when accessed from minipal_env_get_environ.
static EnvironmentVariables g_env;

// Empty environ for platforms without env support.
char* g_empty_environ[] = { NULL };

//Forward declarations.
static char* get_system_env(const char* name);
static char* get_system_env_copy(const char* name);
static void free_system_env(char* value);
static int get_system_env_s(size_t *required_len, char *buffer, size_t buffer_len, const char *name);
static char** get_system_environ(void);
static bool put_env(EnvironmentVariables* env, char* env_s);

// System environ provider.
EnvironmentProvider g_env_system_environ_provider =
{
    ENV_SYSTEM_ENVIRON_PROVIDER_TYPE,
    get_system_env,
    get_system_env_copy,
    free_system_env,
    get_system_env_s,
    get_system_environ,
    NULL,
    NULL,
};

// Environment providers array.
#ifdef HOST_ANDROID
extern EnvironmentProvider g_minipal_env_android_system_properties_provider;
static EnvironmentProvider* g_env_providers[] =
{
    &g_env_system_environ_provider,
    &g_minipal_env_android_system_properties_provider
};
#else
extern EnvironmentProvider g_minipal_env_system_environ_provider;
static EnvironmentProvider* g_env_providers[] =
{
    &g_env_system_environ_provider
};
#endif

// Number of environment providers.
#define g_env_providers_count ARRAY_SIZE(g_env_providers)

/**
 * @brief Retrieves the system environment variable.
 *
 * @return Pointer to the system environment variable, if found, NULL otherwise.
 *
 * @remarks
 * - Returned system environment variable is not thread-safe.
 */
static char* get_system_env(const char* name)
{
#if HAVE_GETENV
    return getenv(name);
#else
    return NULL;
#endif
}

/**
 * @brief Get a copy of the value of an environment variable.
 *
 * @param name The name of the environment variable to get.
 *
 * @return Newly allocated string containing the value. Returns NULL if not found or on error.
 *
 * @remarks
 * - Caller is responsible freeing the returned string.
 */
static char* get_system_env_copy(const char* name)
{
    const char* result = get_system_env(name);
    if (result == NULL)
    {
        return NULL;
    }

    return minipal_strdup(result);
}

/**
 * @brief Frees the memory allocated for a system environment variable value.
 *
 * @param value Pointer to the value to free. If NULL, no action is taken.
 *
 * @remarks
 * - This function should be used to free values obtained from get_system_env.
 */
static void free_system_env(char* value)
{
    if (value != NULL)
    {
        free(value);
    }
}

/**
 * @brief Retrieves the value of an environment variable into a caller-provided buffer.
 *
 * This function attempts to obtain the value of the environment variable specified by name.
 * If the variable exists, its value (including the null terminator) is copied into buffer,
 * provided the buffer is large enough. The required length (including the null terminator) is
 * returned in required_len. If the buffer is too small, the value is truncated, and
 * required_len is set to the full required size.
 *
 * On platforms supporting `getenv_s`, that function is used. Otherwise, a portable fallback
 * using `getenv` is provided.
 *
 * @param required_len  Pointer to a size_t that receives the required length of the value,
 *                      including the null terminator. Set to 0 if the variable is not found.
 * @param buffer        Buffer to receive the value. May be NULL if only the required length is needed.
 * @param buffer_len    Size of the buffer in bytes.
 * @param name          Name of the environment variable to retrieve.
 *
 * @return 0 on success, or an error code (EINVAL for invalid arguments, ERANGE if buffer is too small).
 *
 * @remarks
 * - If buffer is NULL or buffer_len is 0, only the required length is returned in required_len.
 * - If the environment variable does not exist, required_len is set to 0 and buffer (if provided) is set to an empty string.
 * - If the buffer is too small, the value is truncated, @p required_len is set to the full required size, and ERANGE is returned.
 */
static int get_system_env_s(size_t *required_len, char *buffer, size_t buffer_len, const char *name)
{
    if (name == NULL || required_len == NULL || (buffer == NULL && buffer_len > 0))
    {
        return EINVAL;
    }

    char *value = get_system_env(name);

    size_t value_len = value ? strlen(value) : 0;

    *required_len = value ? (value_len + 1) : 0;

    if (buffer && buffer_len > 0)
    {
        if (!value)
        {
            buffer[0] = '\0';
            return 0;
        }

        if (buffer_len < value_len + 1)
        {
            buffer[0] = '\0';
            return ERANGE;
        }

        memcpy(buffer, value, value_len + 1);
    }

    return 0;
}

/**
 * @brief Retrieves the system environment variables array.
 *
 * @return Pointer to the system environment variables array.
 *
 * @remarks
 * - Returned system environment variables array is not thread-safe.
 */
static char** get_system_environ(void)
{
    char** sys_env;

#if HAVE__NSGETENVIRON
    sys_env = *(_NSGetEnviron());
#elif HAVE__ENVIRON
    sys_env = _environ;
#elif HAVE_ENVIRON
    extern char **environ;
    sys_env = environ;
#endif

    if (sys_env == NULL)
    {
        // If no system environment is available, return an empty array.
        sys_env = g_empty_environ;
    }

    return sys_env;
}

/**
 * @brief Checks if cached env is in use or needed.
 *
 * This function determines if the cached environment is currently in use or needed.
 * Mainly used as an internal optimization to have a low overhead path when only
 * the system environment provider is used and no cache is currently loaded.
 *
 * @return True if cache is in use, false otherwise.
 */
static bool use_cached_env(void)
{
    // If we only have the system environment provider and nothing is loaded in cache,
    // no need for cache unless explicitly requested.
    bool only_system_provider = (g_env_providers_count == 1 && g_env_providers[0]->type == ENV_SYSTEM_ENVIRON_PROVIDER_TYPE);
    if (!only_system_provider)
    {
        return true;
    }

    char** cached_env_data = (char**)minipal_volatile_load_ptr((void**)&g_env.data);
    return cached_env_data != NULL;
}

/**
 * @brief Atomically loads the pointer to the global environment mutex lock.
 *
 * @return Pointer to the global environment mutex lock.
 */
static minipal_mutex* atomic_load_env_lock_ptr(void)
{
    return (minipal_mutex *)minipal_volatile_load_ptr((void **)&g_env_lock);
}

/**
 * @brief Atomically sets the global environment mutex lock pointer, if NULL.
 *
 * @param lock Pointer to the allocated and initialized mutex.
 *
 * @return true if pointer was set, false if raced with another thread and lost.
 *
 * @remarks
 * - Takes ownership of lock on success. On failure, caller still owns lock.
 */
static bool atomic_cas_env_lock_ptr(minipal_mutex* lock)
{
    assert(lock != NULL);
    return (minipal_atomic_compare_exchange_ptr((volatile void **)&g_env_lock, (void *)lock, NULL) == NULL);
}

/**
 * @brief Acquires the global environment mutex lock, lazy initializing it if needed.
 *
 * @return true if the mutex lock was successfully acquired, false on failure.
 */
static bool lock_env(void)
{
    minipal_mutex* lock = atomic_load_env_lock_ptr();
    if (lock == NULL)
    {
        lock = (minipal_mutex *)malloc(sizeof(minipal_mutex));
        if (lock == NULL)
        {
            errno = ENOMEM;
            return false;
        }

        minipal_mutex_init(lock);

        if (!atomic_cas_env_lock_ptr(lock))
        {
            minipal_mutex_destroy(lock);
            free(lock);

            lock = atomic_load_env_lock_ptr();
        }
    }

    assert(lock != NULL && lock == atomic_load_env_lock_ptr());
    minipal_mutex_enter(lock);

    return true;
}

/**
 * @brief Releases the global environment mutex lock.
 *
 * @remarks
 * - Caller must own mutex lock through previous successfull lock_env call.
 */
static void unlock_env(void)
{
    assert(g_env_lock != NULL);
    minipal_mutex_leave(g_env_lock);
}

/**
 * @brief Resizes the environment variable array to the specified size.
 *
 * @param env       Pointer to the EnvironmentVariables structure.
 * @param new_size  The new capacity for the environment array (number of slots).
 *
 * @return true on success, false on failure.
 *
 * @remarks
 * - Function assumes exclusive access to env.
 */
static bool resize_env(EnvironmentVariables* env, int new_size)
{
    assert(env != NULL);

    bool result = false;

    if (new_size >= env->size)
    {
        char** new_env = (char**)malloc(new_size * sizeof(char *));
        if (new_env != NULL)
        {
            memcpy(new_env, env->data, env->size * sizeof(char *));
            memset(new_env + env->size, 0, (new_size - env->size) * sizeof(char *));

            if (env->data != NULL)
            {
                char**old_env = env->data;
                minipal_volatile_store_ptr((void**)&env->data, NULL);
                free(old_env);
            }

            env->capacity = new_size;
            minipal_volatile_store_ptr((void**)&env->data, (void*)new_env);

            result = true;
        }
    }

    return result;
}

/**
 * @brief Destroy and free previously allocted environment variables.
 *
 * @param env   Pointer to the EnvironmentVariables structure.
 *
 */
static void destroy_env(EnvironmentVariables* env)
{
    assert(env != NULL);

    char** data = env->data;

    minipal_volatile_store_ptr((void**)&env->data, NULL);
    env->capacity = 0;
    env->size = 0;

    if (data != NULL)
    {
        for (int i = 0; data[i] != NULL; ++i)
        {
            free(data[i]);
        }

        free(data);
    }
}

/**
 * @brief Initialize an empty environment variable array.
 *
 * @param env       Pointer to the EnvironmentVariables structure.
 *
 * @return true on success, false on failure.
 *
 * @remarks
 * - Function assumes exclusive access to env.
 */
static bool init_empty_env(EnvironmentVariables* env)
{
    assert(env != NULL && env->data == NULL);

    if (!resize_env(env, 32))
    {
        return false;
    }

    if (env->data != NULL)
    {
        env->data[0] = NULL;
    }

    return true;
}

/**
 * @brief Initialize the environment variable array from the system environment
 * and system properties (on platform supporting system properties).
 *
 * Providers are used to retrieve environment variables from different sources.
 * If multiple providers include the same environment variable, the last one will
 * be used.
 *
 * @return An initialized EnvironmentVariables structure.
 */
static EnvironmentVariables init_env(void)
{
    EnvironmentVariables env = { 0 };
    int variable_count = 0;

    char** env_provider_data[g_env_providers_count] = { 0 };
    bool env_provider_data_copy[g_env_providers_count] = { false };

    for (int i = 0; i < g_env_providers_count; ++i)
    {
        if (g_env_providers[i]->get_environ_func != NULL)
        {
            env_provider_data[i] = g_env_providers[i]->get_environ_func();
        }
        else if (g_env_providers[i]->get_environ_copy_func != NULL)
        {
            env_provider_data[i] = g_env_providers[i]->get_environ_copy_func();
            env_provider_data_copy[i] = true;
        }
        if (env_provider_data[i] != NULL)
        {
            int data_index = 0;
            while (env_provider_data[i][data_index] != NULL)
            {
                data_index++;
                variable_count++;
            }
        }
    }

    int initial_size = (variable_count == 0) ? 1 : variable_count * 2;

    if (resize_env(&env, initial_size))
    {
        assert(env.data != NULL);

        for (int i = 0; i < g_env_providers_count; ++i)
        {
            if (env_provider_data[i] != NULL)
            {
                int data_index = 0;
                while (env_provider_data[i][data_index] != NULL)
                {
                    char* env_s = NULL;
                    if (env_provider_data_copy[i])
                    {
                        // Transfer ownership
                        env_s = env_provider_data[i][data_index];
                        env_provider_data[i][data_index] = NULL;
                    }
                    else
                    {
                        env_s = minipal_strdup(env_provider_data[i][data_index]);
                    }

                    if (i == 0)
                    {
                        env.data[env.size] = env_s;
                        data_index++;
                        env.size++;
                    }
                    else
                    {
                        put_env(&env, env_s);
                        data_index++;
                    }

                }
            }

            env.data[env.size] = NULL;
        }
    }

    for (int i = 0; i < g_env_providers_count; ++i)
    {
        if (env_provider_data[i] != NULL && env_provider_data_copy[i])
        {
            assert(g_env_providers[i]->free_environ_func != NULL);
            g_env_providers[i]->free_environ_func(env_provider_data[i]);
        }
    }

    return env;
}

/**
 * @brief Makes a deep copy of an EnvironmentVariables structure.
 *
 * Allocates and copies all environment variable strings from src to dest.
 * The destination must be destroyed with destroy_env when no longer needed.
 *
 * @param src   Pointer to the source EnvironmentVariables structure.
 * @param dest  Pointer to the destination EnvironmentVariables structure.
 *
 * @return true on success, false on failure.
 */
static bool copy_env(const EnvironmentVariables* src, EnvironmentVariables* dest)
{
    assert(dest != NULL && src != NULL);
    assert(dest != &g_env);

    dest->size = src->size;
    dest->capacity = src->capacity;
    dest->data = (char**)malloc(sizeof(char*) * (dest->capacity));

    if (dest->data == NULL)
    {
        dest->size = 0;
        dest->capacity = 0;
        return false;
    }

    memset(dest->data, 0, sizeof(char*) * (dest->capacity));

    for (int i = 0; i < src->size; ++i)
    {
        if (src->data[i] != NULL)
        {
            dest->data[i] = minipal_strdup(src->data[i]);
            if (dest->data[i] == NULL)
            {
                destroy_env(dest);
                return false;
            }
        }
        else
        {
            dest->data[i] = NULL;
        }
    }

    if (src->data[src->size] == NULL)
    {
        dest->data[src->size] = NULL;
    }

    return true;
}

/**
 * @brief Assigns an EnvironmentVariables structure from source. Ownersip transfered to dest.
 *
 * @param src   Pointer to the source EnvironmentVariables structure.
 * @param dest  Pointer to the destination EnvironmentVariables structure.
 *
 */
static void assign_env(const EnvironmentVariables* src, EnvironmentVariables* dest)
{
    assert(src != NULL && dest != NULL);

    dest->capacity = src->capacity;
    dest->size = src->size;
    minipal_volatile_store_ptr((void**)&dest->data, (void*)src->data);
}

/**
 * @brief Merges environment variables from a source EnvironmentVariables structure into a destination.
 *
 * For each environment variable in src, adds or updates it in dest using the put_env function.
 * If a variable is successfully merged, the corresponding entry in src is set to NULL to indicate
 * ownership has been transferred to dest.
 *
 * @param src                   Pointer to the source EnvironmentVariables structure whose variables will be merged.
 * @param dest                  Pointer to the destination EnvironmentVariables structure to receive variables.
 * @param transfer_ownership    Flag indicating whether to transfer ownership of merged variables.
 *
 * @return true on success, false on failure.
 *
 * @remarks
 * - Both dest and src must be non-NULL and initialized.
 * - After merging, variables in src that were transferred will be set to NULL.
 * - This function assumes exclusive access to both dest and src.
 */
static bool merge_envs(EnvironmentVariables* src, EnvironmentVariables* dest, bool transfer_ownership)
{
    assert(dest != NULL && src != NULL);
    assert(dest != &g_env);

    if (dest->data == NULL || src->data == NULL)
    {
        return true;
    }

    for (int i = 0; src->data[i] != NULL; ++i)
    {
        if (transfer_ownership)
        {
            if (put_env(dest, src->data[i]))
            {
                src->data[i] = NULL;
            }
        }
        else
        {
            put_env(dest, minipal_strdup(src->data[i]));
        }
    }

    return true;
}

/**
 * @brief Retrieves and caches the value of an environment variable in env.
 *
 * Looks up the value of the specified environment variable. Providers are checked in reversed order.
 * If found, the variable is formatted as "NAME=VALUE" and added to env if not already present.
 *
 * @param env   Pointer to the EnvironmentVariables structure where the variable should be cached.
 * @param name  Name of the environment variable to retrieve and cache.
 *
 * @return Pointer to the cached "NAME=VALUE" string in the environment array, or NULL if not found or on error.
 *
 * @remarks
 * - The returned pointer is owned by the environment array and must not be freed by the caller.
 * - Function assumes exclusive access to env.
 */
static const char* cache_env_string(EnvironmentVariables* env, const char* name)
{
    assert(env != NULL && name != NULL);

    char* env_s = NULL;

    size_t name_len = strlen(name);

    for (int i = g_env_providers_count; i > 0; --i)
    {
        EnvironmentProvider* provider = g_env_providers[i - 1];
        if (provider->get_env_s_func != NULL)
        {
            size_t value_len;
            if (provider->get_env_s_func(&value_len, NULL, 0, name) == 0 && value_len != 0)
            {
                size_t env_s_len = name_len + value_len + 1; // +1 for '=', getenv_s_func returns value_len including null terminator.
                env_s = (char*)malloc(env_s_len);
                if (env_s != NULL)
                {
                    snprintf(env_s, env_s_len, "%s=", name);
                    provider->get_env_s_func(&value_len, env_s + name_len + 1, value_len, name);
                    break;
                }
            }
        }
        else if (provider->get_env_copy_func != NULL)
        {
            char* value = provider->get_env_copy_func(name);
            if (value != NULL)
            {
                size_t env_s_len = name_len + strlen(value) + 2; // +2 for '=' and null terminator.
                env_s = (char *) malloc(env_s_len);
                if (env_s != NULL)
                {
                    snprintf(env_s, env_s_len, "%s=%s", name, value);
                }

                if (provider->free_env_func != NULL)
                {
                    provider->free_env_func(value);
                }

                if (env_s != NULL)
                {
                    break;
                }
            }
        }

    }

    if (env_s != NULL)
    {
        if (!put_env(env, env_s))
        {
            free(env_s);
            env_s = NULL;
        }
    }

    return env_s;
}

/**
 * @brief Retrieves the value of an environment variable from cache.
 *
 * @param env   Pointer to the EnvironmentVariables structure.
 * @param name  Name of the environment variable.
 *
 * @return Pointer to the value, or NULL if not found.
 *
 * @remarks
 * - Returned value is a direct pointer into environment variables data block and is only
 *   safe to use if environment variable data block won't be changed.
 * - Pointer is still owned by environment data block and should never be freed by caller.
 * - Function assumes exclusive access to env.
 */
static const char* find_env(EnvironmentVariables* env, const char* name)
{
    assert(env != NULL && name != NULL);

    if (*name == '\0')
    {
        return NULL;
    }

    if (env->data != NULL)
    {
        for (int i = 0; env->data[i] != NULL; ++i)
        {
            const char* pch = name;
            char* p = env->data[i];

            do
            {
                if (*pch == '\0')
                {
                    if (*p == '=')
                        return p + 1;

                    if (*p == '\0')
                        return p;

                    break;
                }
            }
#ifdef HOST_WINDOWS
            while (tolower(*pch++) == tolower(*p++));
#else
            while (*pch++ == *p++);
#endif
        }
    }

    return NULL;
}

/**
 * @brief Retrieves the value of an environment variable.
 *
 * If env has not been loaded prior this call, this function will not load and cache env.
 *
 * @param env   Pointer to the EnvironmentVariables structure.
 * @param name  Name of the environment variable.
 *
 * @return Pointer to the value, or NULL if not found.
 *
 * @remarks
 * - Returned value is a direct pointer into environment variables data block and is only
 *   safe to use if environment variable data block won't be changed.
 * - Pointer is still owned by environment data block and should never be freed by caller.
 * - Function assumes exclusive access to env.
 */
static const char* get_env(EnvironmentVariables* env, const char* name)
{
    assert(env != NULL && name != NULL);

    const char* env_value = find_env(env, name);
    if (env_value != NULL)
    {
        return env_value;
    }

    if (g_env_loaded)
    {
        return NULL;
    }

    const char* env_s = cache_env_string(env, name);
    if (env_s != NULL)
    {
        env_s = strchr(env_s, '=');
        if (env_s != NULL)
        {
            env_s++;
        }
    }

    return env_s;
}

/**
 * @brief Get the value of an environment variable into a caller provided buffer bypassing cache.
 *
 * @param len       If not NULL, receives the length of the value in bytes.
 * @param value     Buffer receiving the value.
 * @param valuesz   Size of the value buffer in bytes.
 * @param name      Name of the environment variable value to get.
 *
 * @return true if the variable was found, false otherwise.
 *
 * @remarks
 * - If the buffer is too small, value is truncated and len is set to the required size in bytes
 *   (including null terminator) and function returns success.
 * - If complete value fit into buffer, len includes the number copied bytes
 *   (including the null terminator).
 */
static bool get_env_s_nocache(size_t* len, char* value, size_t valuesz, const char* name)
{
    assert(!use_cached_env());
    assert(g_env_providers[0]->get_env_s_func != NULL);

    size_t required_len = 0;
    g_env_providers[0]->get_env_s_func(&required_len, value, valuesz, name);
    if (len != NULL)
    {
        *len = required_len;
    }

    return required_len > 0 ? true : false;
}

/**
 * @brief Get the value of an environment variable into a caller provided buffer.
 *
 * @param env       Pointer to the EnvironmentVariables structure.
 * @param len       If not NULL, receives the length of the value in bytes.
 * @param value     Buffer receiving the value.
 * @param valuesz   Size of the value buffer in bytes.
 * @param name      Name of the environment variable value to get.
 *
 * @return true if the variable was found, false otherwise.
 *
 * @remarks
 * - If the buffer is too small, len is set to the required size in bytes
 *   (including null terminator) and function returns success.
 * - If complete value fit into buffer, len includes the number copied bytes
 *   (including the null terminator).
 * - Function assumes exclusive access to env.
 */
static bool get_env_s(EnvironmentVariables* env, size_t* len, char* value, size_t valuesz, const char* name)
{
    assert(env != NULL && name != NULL);

    if (value == NULL && valuesz != 0)
    {
        if (len != NULL)
        {
            *len = 0;
        }

        return false;
    }

    bool result = false;
    const char* env_value = get_env(env, name);
    if (env_value != NULL)
    {
        size_t env_value_len = strlen(env_value);
        if (env_value_len < valuesz)
        {
            if (value != NULL)
            {
                minipal_strncpy_s(value, valuesz, env_value, env_value_len);
                value[env_value_len] = '\0';
            }

            if (len != NULL)
            {
                *len = env_value_len + 1;
            }
        }
        else
        {
            if (value != NULL)
            {
                value[0] = '\0';
            }

            if (len != NULL)
            {
                *len = env_value_len + 1;
            }
        }

        result = true;
    }
    else
    {
        if (value != NULL)
        {
            value[0] = '\0';
        }

        if (len != NULL)
        {
            *len = 0;
        }
    }

    return result;
}

/**
 * @brief Adds or updates an environment variable using "NAME=VALUE" format.
 *
 * @param env       Pointer to the EnvironmentVariables structure.
 * @param env_s     The environment variable string to add or update.
 *
 * @return true on success, false on failure.
 *
 * @remarks
 * - Function takes ownership of allocated env_var on success.
 * - Function assumes exclusive access to env.
 */
static bool put_env(EnvironmentVariables* env, char* env_s)
{
    assert(env != NULL);

    if (env_s == NULL)
    {
        return true;
    }

    bool result = false;

    const char *equals_sign_position = strchr(env_s, '=');
    if (equals_sign_position == env_s || equals_sign_position == NULL)
    {
        errno = EINVAL;
        return false;
    }

    if (env->data == NULL)
    {
        if (!init_empty_env(env))
        {
            errno = ENOMEM;
            return false;
        }
    }

    ptrdiff_t name_len = equals_sign_position - env_s;

    int i;
    for (i = 0; env->data[i] != NULL; i++)
    {
        const char *existing_equals = strchr(env->data[i], '=');
        if (existing_equals == NULL)
        {
            existing_equals = env->data[i] + strlen(env->data[i]);
        }

        if (existing_equals - env->data[i] == name_len)
        {
            if (memcmp(env_s, env->data[i], name_len) == 0)
            {
                free(env->data[i]);
                env->data[i] = env_s;

                result = true;
                break;
            }
        }
    }

    if (env->data[i] == NULL)
    {
        assert(i < env->capacity);
        if (i == (env->capacity - 1))
        {
            if (!resize_env(env, env->capacity * 2))
            {
                errno = ENOMEM;
                return false;
            }
        }

        env->data[i] = env_s;
        env->data[i + 1] = NULL;
        env->size++;

        result = true;
    }

    return result;
}

/**
 * @brief Sets or updates an environment variable.
 *
 * @param env           Pointer to the EnvironmentVariables structure.
 * @param name          Name of the environment variable.
 * @param value         Value to set.
 * @param overwrite     If false and the variable exists, does not update it, but returns success.
 *
 * @return true on success, false on failure.
 *
 * @remarks
 * - Function assumes exclusive access to env.
 */
static bool set_env(EnvironmentVariables* env, const char* name, const char* value, bool overwrite)
{
    assert(env != NULL && name != NULL);

    if (name[0] == '\0' || strchr(name, '=') != NULL)
    {
        errno = EINVAL;
        return false;
    }

    if (env->data == NULL)
    {
        if (!init_empty_env(env))
        {
            errno = ENOMEM;
            return false;
        }
    }

    if (get_env(env, name) != NULL && !overwrite)
    {
        return true;
    }

    if (value == NULL)
    {
        value = "";
    }

    size_t len = strlen(name) + strlen(value) + 2;
    char* env_var = (char *) malloc(len);
    if (env_var == NULL)
    {
        errno = ENOMEM;
        return false;
    }

    snprintf(env_var, len, "%s=%s", name, value);
    if (!put_env(env, env_var))
    {
        free(env_var);
        errno = ENOMEM;
        return false;
    }

    return true;
}

/**
 * @brief Removes an environment variable.
 *
 * @param env   Pointer to the EnvironmentVariables structure.
 * @param name  Name of the environment variable to remove.
 *
 * @return true if environment variable is successfully removed or not found, false on failure.
 *
 * @remarks
 * - Function assumes exclusive access to env.
 */
static bool unset_env(EnvironmentVariables* env, const char* name)
{
    assert(env != NULL && name != NULL);

    if (name[0] == '\0' || strchr(name, '=') != NULL)
    {
        errno = EINVAL;
        return false;
    }

    if (env->data == NULL)
    {
        return true;
    }

    size_t name_len = strlen(name);

    for (int i = 0; env->data[i] != NULL; ++i)
    {
        const char* equals_sign_position = strchr(env->data[i], '=');
        if (equals_sign_position == NULL)
        {
            equals_sign_position = env->data[i] + strlen(env->data[i]);
        }

        if (equals_sign_position - env->data[i] == (ptrdiff_t)name_len)
        {
            if (memcmp(name, env->data[i], name_len) == 0)
            {
                free(env->data[i]);
                env->data[i] = NULL;

                env->data[i] = env->data[env->size - 1];
                env->data[env->size - 1] = NULL;

                env->size--;
            }
        }
    }

    return true;
}

/**
 * @brief Loads and cache the environment variable subsystem using process data.
 *
 * @return true on success, false on failure.
 */
static bool load_environ(void)
{
    if (!g_env_loaded)
    {
        EnvironmentVariables env = init_env();
        if (g_env.data != NULL)
        {
            merge_envs(&g_env, &env, true);
            destroy_env(&g_env);
        }

        assign_env(&env, &g_env);
        g_env_loaded = g_env.data != NULL;

        if (!g_env_loaded)
        {
            errno = ENOMEM;
        }
    }

    return g_env_loaded;
}

/**
 * @brief Unload and free cached environment subsystem.
 *
 */
static void unload_environ(void)
{
    destroy_env(&g_env);
    g_env_loaded = false;
}

/**
* @see env.h
*/
bool minipal_env_load_environ(void)
{
    bool result = false;

    errno = 0;

    if (lock_env())
    {
        result = load_environ();
        unlock_env();
    }

    return result;
}

/**
* @see env.h
*/
void minipal_env_unload_environ(void)
{
    if (lock_env())
    {
        unload_environ();
        unlock_env();
    }
}

/**
* @see env.h
*/
char** minipal_env_get_environ(void)
{
    char** result = NULL;

    errno = 0;

    if (!use_cached_env())
    {
        assert(g_env_providers[0]->get_environ_func != NULL);
        result = g_env_providers[0]->get_environ_func();
    }
    else
    {
        result = (char**)minipal_volatile_load_ptr((void **)&g_env.data);
    }

    return result;
}

/**
* @see env.h
*/
char** minipal_env_get_environ_copy(void)
{
    char** result = NULL;

    errno = 0;

    if (!use_cached_env())
    {
        EnvironmentVariables env = init_env();
        if (env.data == NULL)
        {
            errno = ENOMEM;
        }

        return env.data;
    }

    if (lock_env())
    {
        if (g_env_loaded)
        {
            EnvironmentVariables env;
            if (copy_env(&g_env, &env))
            {
                result = env.data;
            }
        }
        else
        {
            EnvironmentVariables env = init_env();
            if (env.data != NULL)
            {
                merge_envs(&g_env, &env, false);
                result = env.data;
            }
        }

        unlock_env();
    }

    if (result == NULL && errno == 0)
    {
        errno = ENOMEM;
    }

    return result;
}

/**
* @see env.h
*/
void minipal_env_free_environ(char** data)
{
    EnvironmentVariables env = { 0 };
    env.data = data;
    destroy_env(&env);
}

/**
* @see env.h
*/
bool minipal_env_exists(const char* name)
{
    if (name == NULL)
    {
        errno = EINVAL;
        return false;
    }

    bool result = false;

    errno = 0;

    if (!use_cached_env())
    {
        assert(g_env_providers[0]->get_env_func != NULL);
        return g_env_providers[0]->get_env_func(name) != NULL;
    }

    if (lock_env())
    {
        result = get_env(&g_env, name) != NULL;
        unlock_env();
    }

    return result;
}

/**
* @see env.h
*/
char* minipal_env_get_copy(const char* name)
{
    if (name == NULL)
    {
        errno = EINVAL;
        return NULL;
    }

    char* result = NULL;

    errno = 0;

    if (!use_cached_env())
    {
        assert(g_env_providers[0]->get_env_copy_func != NULL);
        return g_env_providers[0]->get_env_copy_func(name);
    }

    if (lock_env())
    {
        const char* value = get_env(&g_env, name);
        result = value ? minipal_strdup(value) : NULL;
        unlock_env();
    }

    return result;
}

/**
* @see env.h
*/
char* minipal_env_get(const char* name)
{
    if (name == NULL)
    {
        errno = EINVAL;
        return NULL;
    }

    const char* result = NULL;

    errno = 0;

    if (!use_cached_env())
    {
        assert(g_env_providers[0]->get_env_func != NULL);
        return g_env_providers[0]->get_env_func(name);
    }

    if (lock_env())
    {
        result = get_env(&g_env, name);
        unlock_env();
    }

    return (char*)result;
}

/**
* @see env.h
*/
bool minipal_env_get_s(size_t* len, char* value, size_t valuesz, const char* name)
{
    if (name == NULL)
    {
        errno = EINVAL;
        return false;
    }

    bool result = false;

    errno = 0;

    if (!use_cached_env())
    {
        return get_env_s_nocache(len, value, valuesz, name);
    }

    if (lock_env())
    {
        result = get_env_s(&g_env, len, value, valuesz, name);
        unlock_env();
    }
    else
    {
        if (len != NULL)
        {
            *len = 0;
        }
    }

    return result;
}

/**
* @see env.h
*/
bool minipal_env_set(const char* name, const char* value, bool overwrite)
{
    if (name == NULL)
    {
        errno = EINVAL;
        return false;
    }

    bool result = false;

    errno = 0;

    if (lock_env())
    {
        result = set_env(&g_env, name, value, overwrite);
        unlock_env();
    }

    return result;
}

/**
* @see env.h
*/
bool minipal_env_put(const char* env_s)
{
    if (env_s == NULL)
    {
        errno = EINVAL;
        return false;
    }

    char* copy = minipal_strdup(env_s);
    if (copy == NULL)
    {
        errno = ENOMEM;
        return false;
    }

    bool result = false;

    errno = 0;

    if (lock_env())
    {
        result = put_env(&g_env, copy);
        unlock_env();
    }

    if (!result && copy != NULL)
    {
        free(copy);
    }

    return result;
}

/**
* @see env.h
*/
bool minipal_env_unset(const char* name)
{
    if (name == NULL)
    {
        errno = EINVAL;
        return false;
    }

    bool result = false;

    errno = 0;

    if (lock_env())
    {
        if (load_environ())
        {
            result = unset_env(&g_env, name);
        }

        unlock_env();
    }

    return result;
}
