// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "utils.h"
#include "trace.h"

#include <ctype.h>

#if defined(TARGET_WINDOWS)
#include <_version.h>
#else
#include <_version.c>
#endif

// === Cross-platform functions (no OS dependencies) ===

pal_char_t* utils_get_directory_alloc(const pal_char_t* path)
{
    if (path == NULL || path[0] == _X('\0'))
        return NULL;

    size_t len = pal_strlen(path);
    pal_char_t* buf = (pal_char_t*)malloc((len + 2) * sizeof(pal_char_t)); // +2 for trailing separator and NUL
    if (buf == NULL)
        return NULL;
    memcpy(buf, path, (len + 1) * sizeof(pal_char_t));

    // Remove trailing separators
    while (len > 0 && buf[len - 1] == DIR_SEPARATOR)
        buf[--len] = _X('\0');

    // Find last separator
    pal_char_t* last_sep = pal_strrchr(buf, DIR_SEPARATOR);
    if (last_sep == NULL)
    {
        // No separator found - return path + separator
        buf[len] = DIR_SEPARATOR;
        buf[len + 1] = _X('\0');
        return buf;
    }

    // Remove trailing separators before the found position
    pal_char_t* pos = last_sep;
    while (pos > buf && *(pos - 1) == DIR_SEPARATOR)
        pos--;

    size_t dir_len = (size_t)(pos - buf) + 1; // +1 to include the trailing separator
    buf[dir_len] = _X('\0');
    return buf;
}

void utils_get_directory(const pal_char_t* path, pal_char_t* out_dir, size_t out_dir_len)
{
    if (path == NULL || path[0] == _X('\0'))
    {
        if (out_dir_len > 0)
            out_dir[0] = _X('\0');
        return;
    }

    size_t len = pal_strlen(path);
    // Copy path to work buffer
    pal_char_t buf[APPHOST_PATH_MAX];
    if (len >= ARRAY_SIZE(buf))
    {
        if (out_dir_len > 0)
            out_dir[0] = _X('\0');
        return;
    }
    memcpy(buf, path, (len + 1) * sizeof(pal_char_t));

    // Remove trailing separators
    while (len > 0 && buf[len - 1] == DIR_SEPARATOR)
        buf[--len] = _X('\0');

    // Find last separator
    pal_char_t* last_sep = pal_strrchr(buf, DIR_SEPARATOR);
    if (last_sep == NULL)
    {
        // No separator found - return path + separator
        pal_str_printf(out_dir, out_dir_len, _X("%s") _X("%c"), buf, DIR_SEPARATOR);
        return;
    }

    // Remove trailing separators before the found position
    pal_char_t* pos = last_sep;
    while (pos > buf && *(pos - 1) == DIR_SEPARATOR)
        pos--;

    size_t dir_len = (size_t)(pos - buf) + 1; // +1 to include the trailing separator
    if (dir_len + 1 > out_dir_len) // need dir_len chars + NUL
    {
        if (out_dir_len > 0)
            out_dir[0] = _X('\0');
        return;
    }

    memcpy(out_dir, buf, dir_len * sizeof(pal_char_t));
    out_dir[dir_len] = _X('\0');
}

void utils_get_filename(const pal_char_t* path, pal_char_t* out_name, size_t out_name_len)
{
    if (path == NULL || path[0] == _X('\0'))
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
        if (out_name_len > 0)
            out_name[0] = _X('\0');
        return;
    }

    memcpy(out_name, name, (len + 1) * sizeof(pal_char_t));
}

void utils_append_path(pal_char_t* path, size_t path_len, const pal_char_t* component)
{
    if (component == NULL || component[0] == _X('\0'))
        return;

    size_t current_len = pal_strlen(path);
    if (current_len == 0)
    {
        size_t comp_len = pal_strlen(component);
        if (comp_len < path_len)
            memcpy(path, component, (comp_len + 1) * sizeof(pal_char_t));
        return;
    }

    size_t comp_len = pal_strlen(component);
    bool need_sep = (path[current_len - 1] != DIR_SEPARATOR && component[0] != DIR_SEPARATOR);
    size_t total = current_len + (need_sep ? 1 : 0) + comp_len;

    if (total >= path_len)
        return;

    if (need_sep)
    {
        path[current_len] = DIR_SEPARATOR;
        current_len++;
    }

    memcpy(path + current_len, component, (comp_len + 1) * sizeof(pal_char_t));
}

void utils_replace_char(pal_char_t* path, pal_char_t match, pal_char_t repl)
{
    for (pal_char_t* p = path; *p != _X('\0'); p++)
    {
        if (*p == match)
            *p = repl;
    }
}

const pal_char_t* utils_get_current_arch_name(void)
{
    return _STRINGIFY(CURRENT_ARCH_NAME);
}

void utils_get_runtime_id(pal_char_t* out_rid, size_t out_rid_len)
{
    pal_char_t rid_env[256];
    if (pal_getenv(_X("DOTNET_RUNTIME_ID"), rid_env, ARRAY_SIZE(rid_env)))
    {
        size_t len = pal_strlen(rid_env);
        if (len < out_rid_len)
        {
            memcpy(out_rid, rid_env, (len + 1) * sizeof(pal_char_t));
            return;
        }
    }

    pal_str_printf(out_rid, out_rid_len, _X("%s-%s"),
        _STRINGIFY(HOST_RID_PLATFORM), _STRINGIFY(CURRENT_ARCH_NAME));
}

void utils_get_download_url(pal_char_t* out_url, size_t out_url_len)
{
    pal_char_t rid[256];
    utils_get_runtime_id(rid, ARRAY_SIZE(rid));

    pal_str_printf(out_url, out_url_len,
        DOTNET_CORE_APPLAUNCH_URL _X("?missing_runtime=true&arch=%s&rid=%s&os=%s"),
        utils_get_current_arch_name(),
        rid,
        _STRINGIFY(HOST_RID_PLATFORM));
}

void utils_get_host_version_description(pal_char_t* out_desc, size_t out_desc_len)
{
#if defined(TARGET_WINDOWS)
    pal_str_printf(out_desc, out_desc_len, _X("%s"), _STRINGIFY(VER_PRODUCTVERSION_STR));
#else
    // sccsid is @(#)Version <file_version> [@Commit: <commit_hash>]
    // Get the commit portion if available
    char* commit_maybe = strchr(&sccsid[STRING_LENGTH("@(#)Version ")], '@');
    if (commit_maybe != NULL)
    {
        pal_str_printf(out_desc, out_desc_len, _X("%s %s"), _STRINGIFY(HOST_VERSION), commit_maybe);
    }
    else
    {
        pal_str_printf(out_desc, out_desc_len, _X("%s"), _STRINGIFY(HOST_VERSION));
    }
#endif
}

bool utils_starts_with(const pal_char_t* value, const pal_char_t* prefix)
{
    size_t prefix_len = pal_strlen(prefix);
    if (prefix_len == 0)
        return false;

    return pal_strncmp(value, prefix, prefix_len) == 0;
}

bool utils_ends_with(const pal_char_t* value, const pal_char_t* suffix)
{
    size_t value_len = pal_strlen(value);
    size_t suffix_len = pal_strlen(suffix);
    if (value_len < suffix_len)
        return false;

    return pal_strcmp(value + value_len - suffix_len, suffix) == 0;
}

void utils_to_upper(pal_char_t* str)
{
    for (pal_char_t* p = str; *p != _X('\0'); p++)
    {
        *p = (pal_char_t)toupper((unsigned char)*p);
    }
}

void utils_get_dotnet_root_env_var_for_arch(pal_char_t* out_name, size_t out_name_len)
{
    pal_char_t arch_upper[32];
    const pal_char_t* arch = utils_get_current_arch_name();
    size_t arch_len = pal_strlen(arch);
    if (arch_len >= ARRAY_SIZE(arch_upper))
        arch_len = ARRAY_SIZE(arch_upper) - 1;
    memcpy(arch_upper, arch, arch_len * sizeof(pal_char_t));
    arch_upper[arch_len] = _X('\0');
    utils_to_upper(arch_upper);

    pal_str_printf(out_name, out_name_len, _X("%s_%s"), DOTNET_ROOT_ENV_VAR, arch_upper);
}

// === Functions requiring OS-specific pal functions (pal_file_exists, pal_fullpath) ===
// These functions are available on all platforms since Windows PAL is now implemented.

bool utils_file_exists_in_dir(const pal_char_t* dir, const pal_char_t* file_name, pal_char_t* out_file_path, size_t out_path_len)
{
    pal_char_t file_path[APPHOST_PATH_MAX];
    size_t dir_len = pal_strlen(dir);
    size_t name_len = pal_strlen(file_name);

    if (dir_len + 1 + name_len >= ARRAY_SIZE(file_path))
        return false;

    memcpy(file_path, dir, dir_len * sizeof(pal_char_t));
    if (dir_len > 0 && dir[dir_len - 1] != DIR_SEPARATOR)
    {
        file_path[dir_len] = DIR_SEPARATOR;
        memcpy(file_path + dir_len + 1, file_name, (name_len + 1) * sizeof(pal_char_t));
    }
    else
    {
        memcpy(file_path + dir_len, file_name, (name_len + 1) * sizeof(pal_char_t));
    }

    if (!pal_file_exists(file_path))
        return false;

    if (out_file_path != NULL)
    {
        size_t path_len = pal_strlen(file_path);
        if (path_len >= out_path_len)
            return false;
        memcpy(out_file_path, file_path, (path_len + 1) * sizeof(pal_char_t));
    }

    return true;
}

bool utils_file_exists_in_dir_alloc(const pal_char_t* dir, const pal_char_t* file_name, pal_char_t** out_file_path)
{
    size_t dir_len = pal_strlen(dir);
    size_t name_len = pal_strlen(file_name);
    size_t total = dir_len + 1 + name_len + 1; // dir + sep + name + NUL

    pal_char_t* file_path = (pal_char_t*)malloc(total * sizeof(pal_char_t));
    if (file_path == NULL)
        return false;

    memcpy(file_path, dir, dir_len * sizeof(pal_char_t));
    if (dir_len > 0 && dir[dir_len - 1] != DIR_SEPARATOR)
    {
        file_path[dir_len] = DIR_SEPARATOR;
        memcpy(file_path + dir_len + 1, file_name, (name_len + 1) * sizeof(pal_char_t));
    }
    else
    {
        memcpy(file_path + dir_len, file_name, (name_len + 1) * sizeof(pal_char_t));
    }

    if (!pal_file_exists(file_path))
    {
        free(file_path);
        return false;
    }

    *out_file_path = file_path;
    return true;
}

static bool get_file_path_from_env(const pal_char_t* env_key, pal_char_t* recv, size_t recv_len)
{
    recv[0] = _X('\0');
    pal_char_t file_path[APPHOST_PATH_MAX];
    if (pal_getenv(env_key, file_path, ARRAY_SIZE(file_path)))
    {
        if (pal_fullpath(file_path, ARRAY_SIZE(file_path)))
        {
            size_t len = pal_strlen(file_path);
            if (len < recv_len)
            {
                memcpy(recv, file_path, (len + 1) * sizeof(pal_char_t));
                return true;
            }
        }
        trace_verbose(_X("Did not find [%s] directory [%s]"), env_key, file_path);
    }

    return false;
}

#define TEST_ONLY_MARKER "d38cc827-e34f-4453-9df4-1e796e9f1d07"

// Single source of truth for the test-only marker.
// The marker is a GUID embedded in the product binary so it can be located by tests
// (via a simple byte-pattern search) and switched between disabled ('d') and enabled ('e').
// Keep this in exactly one place so `BinaryUtils.SearchAndReplace` only ever needs to
// flip a single byte; otherwise some `test_only_getenv` callers would observe a stale
// state and the test infrastructure's backup/restore lifecycle would get out of sync.
bool is_test_only_enabled(void)
{
    enum { EMBED_SIZE = sizeof(TEST_ONLY_MARKER) / sizeof(TEST_ONLY_MARKER[0]) };
    volatile static char embed[EMBED_SIZE] = TEST_ONLY_MARKER;
    return embed[0] == 'e';
}

bool utils_get_dotnet_root_from_env(pal_char_t* out_env_var_name, size_t env_var_name_len, pal_char_t* recv, size_t recv_len)
{
    pal_char_t env_var_name[256];
    utils_get_dotnet_root_env_var_for_arch(env_var_name, ARRAY_SIZE(env_var_name));
    if (get_file_path_from_env(env_var_name, recv, recv_len))
    {
        size_t len = pal_strlen(env_var_name);
        if (len < env_var_name_len)
            memcpy(out_env_var_name, env_var_name, (len + 1) * sizeof(pal_char_t));
        return true;
    }

#if defined(_WIN32)
    // WOW64: 32-bit process on 64-bit Windows. Check the legacy DOTNET_ROOT(x86) variable.
    {
        BOOL is_wow64 = FALSE;
        if (IsWow64Process(GetCurrentProcess(), &is_wow64) && is_wow64)
        {
            if (get_file_path_from_env(_X("DOTNET_ROOT(x86)"), recv, recv_len))
            {
                const pal_char_t* name = _X("DOTNET_ROOT(x86)");
                size_t len = pal_strlen(name);
                if (len < env_var_name_len)
                    memcpy(out_env_var_name, name, (len + 1) * sizeof(pal_char_t));
                return true;
            }
        }
    }
#endif

    // Fallback to the default DOTNET_ROOT
    if (get_file_path_from_env(DOTNET_ROOT_ENV_VAR, recv, recv_len))
    {
        size_t len = pal_strlen(DOTNET_ROOT_ENV_VAR);
        if (len < env_var_name_len)
            memcpy(out_env_var_name, DOTNET_ROOT_ENV_VAR, (len + 1) * sizeof(pal_char_t));
        return true;
    }

    return false;
}
