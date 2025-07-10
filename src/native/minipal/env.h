// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_ENV_H
#define HAVE_MINIPAL_ENV_H

#include <minipal/types.h>
#include <stdbool.h>
#include <stdarg.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/**
 * @brief Loads and cache the environment variable subsystem using process data.
 *
 * Loads and cache environment variables from the system and prepares internal data structures.
 * If environment variables have already been used through minipal_env_get, minipal_env_get_copy, minipal_env_get_s,
 * minipal_env_set or minipal_env_put existing cached values will be merged into loaded environment.
 *
 * Function exists to explicitly load and cache environment.
 *
 * @return true on success, false on failure.
 */
bool minipal_env_load_environ(void);

/**
 * @brief Unload and free cached environment subsystem.
 *
 * Unloads and free cache environment variables. Any changes done to the environment will be lost.
 * This function should be called when the environment is no longer needed or if the environment needs
 * to be reloaded using minipal_env_load_environ.
 */
void minipal_env_unload_environ(void);

/**
 * @brief Get a pointer to an internal environment variable array.
 *
 * @warning The returned pointer is not thread-safe and should not be modified.
 * This function should only be called from code that guarantees environment won't
 * change while using returned pointer.
 *
 * @return Pointer to the environment variable array.
 *
 * @remarks
 * - Access internal environment variable array wihout doing allocs or taking locks.
 */
char** minipal_env_get_environ(void);

/**
 * @brief Get a pointer to the environment variable array.
 *
 * @return Pointer to the environment variable array, or NULL on failure.
 *
 * @remarks
 * - Returned environment is a copy and owned by caller, freed using minipal_env_free_environ.
 * - If environment has NOT been explicitly loaded, a copy of the underlying system environment
 *   will created and returned on each call. To avoid recreating environment on each call,
 *   explicit load environment using minipal_env_load_environ before calling this function.
 */
char** minipal_env_get_environ_copy(void);

/**
 * @brief Frees an environment variable array previously returned by minipal_env_get_environ_copy.
 *
 * This function releases all memory associated with the environment array returned by
 * minipal_env_get_environ_copy. The pointer passed must not be used after this call.
 *
 * @param data Pointer to the environment variable array to free. May be NULL.
 *
 * @remarks
 * - Only use this function to free arrays returned by minipal_env_get_environ_copy.
 * - Passing NULL is safe and has no effect.
 */
void minipal_env_free_environ(char** data);

/**
 * @brief Checks if specified environment variable exists.
 *
 * @param name      Name of the environment variable to check.
 *
 * @return true if the variable was found, false otherwise.
 */
bool minipal_env_exists(const char* name);

/**
 * @brief Get the value of an environment variable.
 *
 * @param name The name of the environment variable to get.
 *
 * @warning The returned pointer is not thread-safe and should not be modified.
 * This function should only be called from code that guarantees environment won't
 * change while using returned pointer.
 *
 * @return String containing the value, ownership is still held by the environment and should NOT
 * be freed by caller. Returns NULL if not found or on error.
 */
char* minipal_env_get(const char* name);

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
char* minipal_env_get_copy(const char* name);

/**
 * @brief Get the value of an environment variable into a user allocated buffer.
 *
 * This functions gives callers ability to use a pre-allocated buffer when
 * requesting an environment variable. Passing in a pointer for len, but NULL
 * for value and 0 for valuesz returns needed size in bytes to hold full environment
 * variable value in len.
 *
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
 */
bool minipal_env_get_s(size_t* len, char* value, size_t valuesz, const char* name);

/**
 * @brief Set or update an environment variable.
 *
 * @param name          Name of the environment variable.
 * @param value         Value to set. If NULL, sets the variable to an empty string.
 * @param overwrite     If false and the variable exists, does not update it.
 *
 * @return true on success, false on failure.
 */
bool minipal_env_set(const char* name, const char* value, bool overwrite);

/**
 * @brief Add or update an environment variable from a "NAME=VALUE" string.
 *
 * @param env_s Environment variable to add/update.
 *
 * @return true on success, false on failure.
 *
 * @remarks
 * - Function makes a copy so caller continues to owns env_s.
 */
bool minipal_env_put(const char* env_s);

/**
 * @brief Remove an environment variable.
 *
 * @param name Name of the environment variable to remove.
 *
 * @return true if environment variable is successfully removed or not found, false on failure.
 *
 * @remarks
 * - If environment has NOT been explicitly loaded, function will automatically load
 *   enviroment maintaining a unified view of all changes.
 */
bool minipal_env_unset(const char* name);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* HAVE_MINIPAL_ENV_H */
