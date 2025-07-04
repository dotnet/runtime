// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "minipalconfig.h"
#include <errno.h>
#include <string.h>
#include "minipal/env.h"

#if HAVE_CRT_EXTERNS_H
#include <crt_externs.h>
#endif

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
#else
    extern char **environ;
    sys_env = environ;
#endif

    return sys_env;
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
#if HAVE_GETENV_S
    return (int)getenv_s(required_len, buffer, buffer_len, name);
#else
    if (name == NULL || required_len == NULL || (buffer == NULL && buffer_len > 0))
    {
        return EINVAL;
    }

#if HAVE_GETENV
    char *value = getenv(name);
#else
    char *value = NULL;
#endif

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
#endif
}

// System environ provider 
EnvironmentProvider g_minipal_env_system_environ_provider =
{
    ENV_SYSTEM_ENVIRON_PROVIDER_TYPE,
    NULL,
    NULL,
    get_system_env_s,
    get_system_environ,
    NULL,
};
