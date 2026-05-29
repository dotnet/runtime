// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementations of the utils_* APIs needed by trace.c and fxr_resolver.c.

#include "utils.h"

#include <string.h>

bool utils_get_filename(const pal_char_t* path, pal_char_t* out_name, size_t out_name_len)
{
    if (path == NULL)
    {
        if (out_name_len > 0)
            out_name[0] = _X('\0');

        return true;
    }

    const pal_char_t* last_sep = pal_strrchr(path, DIR_SEPARATOR);
    const pal_char_t* name = (last_sep != NULL) ? last_sep + 1 : path;
    size_t len = pal_strlen(name);
    if (len >= out_name_len)
    {
        if (out_name_len > 0)
            out_name[0] = _X('\0');

        return false;
    }

    memcpy(out_name, name, (len + 1) * sizeof(pal_char_t));
    return true;
}

void utils_append_path(pal_char_t* path, size_t path_len, const pal_char_t* component)
{
    if (component == NULL || component[0] == _X('\0'))
        return;

    size_t current_len = pal_strlen(path);
    size_t comp_len = pal_strlen(component);

    if (current_len == 0)
    {
        if (comp_len < path_len)
            memcpy(path, component, (comp_len + 1) * sizeof(pal_char_t));
        return;
    }

    bool need_sep = (path[current_len - 1] != DIR_SEPARATOR) && (component[0] != DIR_SEPARATOR);
    if (current_len + (need_sep ? 1u : 0u) + comp_len >= path_len)
        return;

    if (need_sep)
        path[current_len++] = DIR_SEPARATOR;

    memcpy(path + current_len, component, (comp_len + 1) * sizeof(pal_char_t));
}

pal_char_t* utils_file_exists_in_dir_alloc(const pal_char_t* dir, const pal_char_t* file_name)
{
    if (dir == NULL || file_name == NULL)
        return NULL;

    size_t cap = pal_strlen(dir) + pal_strlen(file_name) + 2; // +1 separator, +1 NUL
    pal_char_t* file_path = (pal_char_t*)malloc(cap * sizeof(pal_char_t));
    file_path[0] = _X('\0');
    utils_append_path(file_path, cap, dir);
    utils_append_path(file_path, cap, file_name);

    if (!pal_file_exists(file_path))
    {
        free(file_path);
        return NULL;
    }

    return file_path;
}

pal_char_t* utils_get_directory_alloc(const pal_char_t* path)
{
    if (path == NULL)
        return NULL;

    // Drop trailing separators (matches C++ get_directory which pops them off the working copy).
    size_t len = pal_strlen(path);
    while (len > 0 && path[len - 1] == DIR_SEPARATOR)
        len--;

    // Find the last separator within [0, len).
    intptr_t sep_pos = -1;
    for (intptr_t i = (intptr_t)len - 1; i >= 0; i--)
    {
        if (path[i] == DIR_SEPARATOR)
        {
            sep_pos = i;
            break;
        }
    }

    size_t prefix_len;
    if (sep_pos < 0)
    {
        // No separator: result is path[0..len) + DIR_SEPARATOR.
        prefix_len = len;
    }
    else
    {
        // Skip any run of separators that precedes sep_pos (matches the C++ pos-- loop).
        intptr_t pos = sep_pos;
        while (pos >= 0 && path[pos] == DIR_SEPARATOR)
            pos--;
        prefix_len = (size_t)(pos + 1);
    }

    pal_char_t* result = (pal_char_t*)malloc((prefix_len + 2) * sizeof(pal_char_t));
    if (result == NULL)
        return NULL;

    if (prefix_len > 0)
        memcpy(result, path, prefix_len * sizeof(pal_char_t));
    result[prefix_len] = DIR_SEPARATOR;
    result[prefix_len + 1] = _X('\0');
    return result;
}

pal_char_t* utils_get_file_path_from_env(const pal_char_t* env_key)
{
    pal_char_t* file_path = pal_getenv(env_key);
    if (file_path == NULL)
        return NULL;

    pal_char_t* canonical = pal_fullpath(file_path, /* skip_error_logging */ false);
    if (canonical == NULL)
    {
        trace_verbose(_X("Did not find [%s] directory [%s]"), env_key, file_path);
        free(file_path);
        return NULL;
    }

    free(file_path);
    return canonical;
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
