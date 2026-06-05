// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementations of the utils_* APIs needed by trace.c and fxr_resolver.c.

#include "utils.h"

#include <assert.h>
#include <string.h>

void utils_get_filename(const pal_char_t* path, pal_char_t* out_name, size_t out_name_len)
{
    if (path == NULL)
    {
        if (out_name_len > 0)
            out_name[0] = _X('\0');

        return;
    }

    const pal_char_t* last_sep = pal_strrchr(path, DIR_SEPARATOR);
    const pal_char_t* name = (last_sep != NULL) ? last_sep + 1 : path;
    size_t len = pal_strlen(name);
    if (len >= out_name_len)
    {
        assert(false && "utils_get_filename: out_name buffer too small");
        if (out_name_len > 0)
            out_name[0] = _X('\0');

        return;
    }

    memcpy(out_name, name, (len + 1) * sizeof(pal_char_t));
}

void utils_append_path(pal_char_t* path_buffer, size_t path_buffer_len, const pal_char_t* component)
{
    if (component == NULL || component[0] == _X('\0'))
        return;

    size_t current_len = pal_strlen(path_buffer);
    size_t comp_len = pal_strlen(component);

    // Insert a separator only when the buffer is non-empty, doesn't already end in one,
    // and the component doesn't already start with one.
    bool need_sep = current_len > 0
        && path_buffer[current_len - 1] != DIR_SEPARATOR
        && component[0] != DIR_SEPARATOR;

    if (current_len + (need_sep ? 1u : 0u) + comp_len >= path_buffer_len)
    {
        assert(false && "utils_append_path: path_buffer too small");
        return;
    }

    if (need_sep)
        path_buffer[current_len++] = DIR_SEPARATOR;

    memcpy(path_buffer + current_len, component, (comp_len + 1) * sizeof(pal_char_t));
}

pal_char_t* utils_append_path_alloc(const pal_char_t* path, const pal_char_t* component)
{
    size_t cap = pal_strlen(path) + pal_strlen(component) + 2; // +1 separator, +1 null terminator
    pal_char_t* out = (pal_char_t*)malloc(cap * sizeof(pal_char_t));
    if (out == NULL)
        return NULL;

    out[0] = _X('\0');
    utils_append_path(out, cap, path);
    utils_append_path(out, cap, component);
    return out;
}

pal_char_t* utils_find_file_in_dir(const pal_char_t* dir, const pal_char_t* file_name)
{
    if (dir == NULL || file_name == NULL)
        return NULL;

    pal_char_t* file_path = utils_append_path_alloc(dir, file_name);
    if (file_path == NULL)
        return NULL;

    if (!pal_file_exists(file_path))
    {
        free(file_path);
        return NULL;
    }

    return file_path;
}

pal_char_t* utils_get_directory(const pal_char_t* path)
{
    if (path == NULL)
        return NULL;

    size_t path_len = pal_strlen(path);
    pal_char_t* result = (pal_char_t*)malloc((path_len + 2) * sizeof(pal_char_t)); // +2 for trailing separator and null terminator
    if (result == NULL)
        return NULL;

    memcpy(result, path, (path_len + 1) * sizeof(pal_char_t));

    // Drop trailing separators.
    size_t len = path_len;
    while (len > 0 && result[len - 1] == DIR_SEPARATOR)
        result[--len] = _X('\0');

    // Find the last separator
    pal_char_t* last_sep = pal_strrchr(result, DIR_SEPARATOR);
    if (last_sep != NULL)
    {
        // Strip any trailing separators before the last separator.
        len = (size_t)(last_sep - result);
        while (len > 0 && result[len - 1] == DIR_SEPARATOR)
            len--;
    }

    result[len] = DIR_SEPARATOR;
    result[len + 1] = _X('\0');
    return result;
}

pal_char_t* utils_get_file_path_from_env(const pal_char_t* env_key)
{
    pal_char_t* env_value = pal_getenv(env_key);
    if (env_value == NULL)
        return NULL;

    pal_char_t* file_path = pal_fullpath(env_value, /* skip_error_logging */ false);
    if (file_path == NULL)
    {
        trace_verbose(_X("Did not find [%s] directory [%s]"), env_key, env_value);
        free(env_value);
        return NULL;
    }

    free(env_value);
    return file_path;
}

bool utils_get_dotnet_root_from_env(const pal_char_t** out_env_var_name, pal_char_t** out_dotnet_root)
{
    if (out_env_var_name == NULL || out_dotnet_root == NULL)
        return false;

    *out_env_var_name = NULL;
    *out_dotnet_root = NULL;

    const pal_char_t* arch_env_var_name = DOTNET_ROOT_ARCH_ENV_VAR;
    pal_char_t* dotnet_root = utils_get_file_path_from_env(arch_env_var_name);
    if (dotnet_root != NULL)
    {
        *out_env_var_name = arch_env_var_name;
        *out_dotnet_root = dotnet_root;
        return true;
    }

#if defined(_WIN32)
    if (pal_is_running_in_wow64())
    {
        dotnet_root = utils_get_file_path_from_env(_X("DOTNET_ROOT(x86)"));
        if (dotnet_root != NULL)
        {
            *out_env_var_name = _X("DOTNET_ROOT(x86)");
            *out_dotnet_root = dotnet_root;
            return true;
        }
    }
#endif

    dotnet_root = utils_get_file_path_from_env(DOTNET_ROOT_ENV_VAR);
    if (dotnet_root != NULL)
    {
        *out_env_var_name = DOTNET_ROOT_ENV_VAR;
        *out_dotnet_root = dotnet_root;
        return true;
    }

    return false;
}
