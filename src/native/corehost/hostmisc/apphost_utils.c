// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "apphost_utils.h"
#include "apphost_pal.h"
#include "apphost_trace.h"

#include <string.h>
#include <stdio.h>
#include <ctype.h>

#if defined(TARGET_WINDOWS)
#include <_version.h>
#else
#include <_version.c>
#endif

bool utils_file_exists_in_dir(const char* dir, const char* file_name, char* out_file_path, size_t out_path_len)
{
    char file_path[APPHOST_PATH_MAX];
    size_t dir_len = strlen(dir);
    size_t name_len = strlen(file_name);

    if (dir_len + 1 + name_len >= sizeof(file_path))
        return false;

    memcpy(file_path, dir, dir_len);
    if (dir_len > 0 && dir[dir_len - 1] != '/')
    {
        file_path[dir_len] = '/';
        memcpy(file_path + dir_len + 1, file_name, name_len + 1);
    }
    else
    {
        memcpy(file_path + dir_len, file_name, name_len + 1);
    }

    if (!pal_file_exists(file_path))
        return false;

    if (out_file_path != NULL)
    {
        size_t path_len = strlen(file_path);
        if (path_len >= out_path_len)
            return false;
        memcpy(out_file_path, file_path, path_len + 1);
    }

    return true;
}

void utils_get_directory(const char* path, char* out_dir, size_t out_dir_len)
{
    if (path == NULL || path[0] == '\0')
    {
        if (out_dir_len > 0)
            out_dir[0] = '\0';
        return;
    }

    size_t len = strlen(path);
    // Copy path to work buffer
    char buf[APPHOST_PATH_MAX];
    if (len >= sizeof(buf))
    {
        if (out_dir_len > 0)
            out_dir[0] = '\0';
        return;
    }
    memcpy(buf, path, len + 1);

    // Remove trailing separators
    while (len > 0 && buf[len - 1] == '/')
        buf[--len] = '\0';

    // Find last separator
    char* last_sep = strrchr(buf, '/');
    if (last_sep == NULL)
    {
        // No separator found - return path + "/"
        snprintf(out_dir, out_dir_len, "%s/", buf);
        return;
    }

    // Remove trailing separators before the found position
    char* pos = last_sep;
    while (pos > buf && *(pos - 1) == '/')
        pos--;

    size_t dir_len = (size_t)(pos - buf) + 1; // +1 for the trailing separator
    if (dir_len >= out_dir_len)
    {
        if (out_dir_len > 0)
            out_dir[0] = '\0';
        return;
    }

    memcpy(out_dir, buf, dir_len);
    out_dir[dir_len] = '/';
    out_dir[dir_len + 1] = '\0';
}

void utils_get_filename(const char* path, char* out_name, size_t out_name_len)
{
    if (path == NULL || path[0] == '\0')
    {
        if (out_name_len > 0)
            out_name[0] = '\0';
        return;
    }

    const char* last_sep = strrchr(path, '/');
    const char* name = (last_sep != NULL) ? last_sep + 1 : path;
    size_t len = strlen(name);
    if (len >= out_name_len)
    {
        if (out_name_len > 0)
            out_name[0] = '\0';
        return;
    }

    memcpy(out_name, name, len + 1);
}

void utils_append_path(char* path, size_t path_len, const char* component)
{
    if (component == NULL || component[0] == '\0')
        return;

    size_t current_len = strlen(path);
    if (current_len == 0)
    {
        size_t comp_len = strlen(component);
        if (comp_len < path_len)
            memcpy(path, component, comp_len + 1);
        return;
    }

    size_t comp_len = strlen(component);
    bool need_sep = (path[current_len - 1] != '/' && component[0] != '/');
    size_t total = current_len + (need_sep ? 1 : 0) + comp_len;

    if (total >= path_len)
        return;

    if (need_sep)
    {
        path[current_len] = '/';
        current_len++;
    }

    memcpy(path + current_len, component, comp_len + 1);
}

void utils_replace_char(char* path, char match, char repl)
{
    for (char* p = path; *p != '\0'; p++)
    {
        if (*p == match)
            *p = repl;
    }
}

const char* utils_get_current_arch_name(void)
{
    return _STRINGIFY(CURRENT_ARCH_NAME);
}

void utils_get_runtime_id(char* out_rid, size_t out_rid_len)
{
    char rid_env[256];
    if (pal_getenv("DOTNET_RUNTIME_ID", rid_env, sizeof(rid_env)))
    {
        size_t len = strlen(rid_env);
        if (len < out_rid_len)
        {
            memcpy(out_rid, rid_env, len + 1);
            return;
        }
    }

    snprintf(out_rid, out_rid_len, "%s-%s",
        _STRINGIFY(HOST_RID_PLATFORM), _STRINGIFY(CURRENT_ARCH_NAME));
}

void utils_get_download_url(char* out_url, size_t out_url_len)
{
    char rid[256];
    utils_get_runtime_id(rid, sizeof(rid));

    snprintf(out_url, out_url_len,
        DOTNET_CORE_APPLAUNCH_URL "?missing_runtime=true&arch=%s&rid=%s&os=%s",
        utils_get_current_arch_name(),
        rid,
        _STRINGIFY(HOST_RID_PLATFORM));
}

void utils_get_host_version_description(char* out_desc, size_t out_desc_len)
{
    // sccsid is @(#)Version <file_version> [@Commit: <commit_hash>]
    // Get the commit portion if available
    char* commit_maybe = strchr(&sccsid[STRING_LENGTH("@(#)Version ")], '@');
    if (commit_maybe != NULL)
    {
        snprintf(out_desc, out_desc_len, "%s %s", _STRINGIFY(HOST_VERSION), commit_maybe);
    }
    else
    {
        snprintf(out_desc, out_desc_len, "%s", _STRINGIFY(HOST_VERSION));
    }
}

bool utils_starts_with(const char* value, const char* prefix)
{
    size_t prefix_len = strlen(prefix);
    if (prefix_len == 0)
        return false;

    return strncmp(value, prefix, prefix_len) == 0;
}

bool utils_ends_with(const char* value, const char* suffix)
{
    size_t value_len = strlen(value);
    size_t suffix_len = strlen(suffix);
    if (value_len < suffix_len)
        return false;

    return strcmp(value + value_len - suffix_len, suffix) == 0;
}

void utils_to_upper(char* str)
{
    for (char* p = str; *p != '\0'; p++)
    {
        *p = (char)toupper((unsigned char)*p);
    }
}

void utils_get_dotnet_root_env_var_for_arch(char* out_name, size_t out_name_len)
{
    char arch_upper[32];
    const char* arch = utils_get_current_arch_name();
    size_t arch_len = strlen(arch);
    if (arch_len >= sizeof(arch_upper))
        arch_len = sizeof(arch_upper) - 1;
    memcpy(arch_upper, arch, arch_len);
    arch_upper[arch_len] = '\0';
    utils_to_upper(arch_upper);

    snprintf(out_name, out_name_len, "%s_%s", DOTNET_ROOT_ENV_VAR, arch_upper);
}

static bool get_file_path_from_env(const char* env_key, char* recv, size_t recv_len)
{
    recv[0] = '\0';
    char file_path[APPHOST_PATH_MAX];
    if (pal_getenv(env_key, file_path, sizeof(file_path)))
    {
        if (pal_fullpath(file_path, sizeof(file_path)))
        {
            size_t len = strlen(file_path);
            if (len < recv_len)
            {
                memcpy(recv, file_path, len + 1);
                return true;
            }
        }
        trace_verbose("Did not find [%s] directory [%s]", env_key, file_path);
    }

    return false;
}

bool utils_get_dotnet_root_from_env(char* out_env_var_name, size_t env_var_name_len, char* recv, size_t recv_len)
{
    char env_var_name[256];
    utils_get_dotnet_root_env_var_for_arch(env_var_name, sizeof(env_var_name));
    if (get_file_path_from_env(env_var_name, recv, recv_len))
    {
        size_t len = strlen(env_var_name);
        if (len < env_var_name_len)
            memcpy(out_env_var_name, env_var_name, len + 1);
        return true;
    }

    // Fallback to the default DOTNET_ROOT
    if (get_file_path_from_env(DOTNET_ROOT_ENV_VAR, recv, recv_len))
    {
        size_t len = strlen(DOTNET_ROOT_ENV_VAR);
        if (len < env_var_name_len)
            memcpy(out_env_var_name, DOTNET_ROOT_ENV_VAR, len + 1);
        return true;
    }

    return false;
}
