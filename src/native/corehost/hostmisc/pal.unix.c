// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementations of the pal_* APIs needed by trace.c on non-Windows.

#include "pal.h"
#include "trace.h"
#include "utils.h"

#include <dirent.h>
#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <unistd.h>

#if defined(TARGET_OSX) || defined(TARGET_FREEBSD)
#include <sys/sysctl.h>
#endif

#include "config.h"
#include <minipal/getexepath.h>
#include <minipal/utils.h>

pal_char_t* pal_get_own_executable_path(void)
{
    return minipal_getexepath();
}

bool pal_directory_exists(const pal_char_t* path)
{
    struct stat sb;
    if (stat(path, &sb) != 0)
        return false;

    return S_ISDIR(sb.st_mode);
}

pal_char_t* pal_strdup(const pal_char_t* str)
{
    return strdup(str);
}

pal_char_t* pal_getenv(const pal_char_t* name)
{
    const char* result = getenv(name);
    if (result == NULL || result[0] == '\0')
        return NULL;

    return strdup(result);
}

pal_char_t* pal_fullpath(const pal_char_t* path, bool skip_error_logging)
{
    if (path == NULL)
        return NULL;

    char* resolved = realpath(path, NULL);
    if (resolved == NULL)
    {
        if (errno != ENOENT && !skip_error_logging)
            trace_error(_X("realpath(%s) failed: %s"), path, strerror(errno));
        return NULL;
    }

    return resolved;
}

bool pal_file_exists(const pal_char_t* path)
{
    return access(path, F_OK) == 0;
}

bool pal_readdir_onlydirectories(const pal_char_t* path, pal_readdir_callback_t callback, void* ctx)
{
    if (path == NULL || callback == NULL)
        return false;

    DIR* dir = opendir(path);
    if (dir == NULL)
        return false;

    struct dirent* entry;
    while ((entry = readdir(dir)) != NULL)
    {
#if HAVE_DIRENT_D_TYPE
        int entry_type = entry->d_type;
#else
        int entry_type = DT_UNKNOWN;
#endif

        bool is_dir;
        switch (entry_type)
        {
        case DT_DIR:
            is_dir = true;
            break;

        case DT_LNK:
        case DT_UNKNOWN:
        {
            // Resolve via stat for symlinks and file systems that do not
            // provide d_type.
            struct stat sb;
            if (fstatat(dirfd(dir), entry->d_name, &sb, 0) == -1)
                continue;

            is_dir = S_ISDIR(sb.st_mode);
            break;
        }

        default:
            continue;
        }

        if (!is_dir)
            continue;

        if (strcmp(entry->d_name, ".") == 0 || strcmp(entry->d_name, "..") == 0)
            continue;

        if (!callback(entry->d_name, ctx))
            break;
    }

    closedir(dir);
    return true;
}

bool pal_is_running_in_wow64(void)
{
    return false;
}

bool pal_is_emulating_x64(void)
{
    int is_translated_process = 0;
#if defined(TARGET_OSX)
    size_t size = sizeof(is_translated_process);
    if (sysctlbyname("sysctl.proc_translated", &is_translated_process, &size, NULL, 0) == -1)
    {
        trace_info(_X("Could not determine whether the current process is running under Rosetta."));
        if (errno != ENOENT)
        {
            trace_info(_X("Call to sysctlbyname failed: %s"), strerror(errno));
        }

        return false;
    }
#endif

    return is_translated_process == 1;
}

// ASCII-only lowercase of `src` into a fresh allocation. Used to lower-case
// architecture name segments embedded into file paths (matches the C++ side's
// defensive to_lower() over arch names).
static pal_char_t* ascii_lower_dup(const pal_char_t* src)
{
    if (src == NULL)
        return NULL;

    size_t len = strlen(src);
    pal_char_t* dup = (pal_char_t*)malloc(len + 1);
    if (dup == NULL)
        return NULL;

    for (size_t i = 0; i < len; ++i)
    {
        pal_char_t c = src[i];
        dup[i] = (c >= 'A' && c <= 'Z') ? (pal_char_t)(c + ('a' - 'A')) : c;
    }
    dup[len] = '\0';
    return dup;
}

// Allocates the concatenation of `dir + '/' + leaf`. Returns NULL on
// allocation failure.
static pal_char_t* join_path_alloc(const pal_char_t* dir, const pal_char_t* leaf)
{
    size_t dir_len = strlen(dir);
    size_t leaf_len = strlen(leaf);
    bool need_sep = dir_len > 0 && dir[dir_len - 1] != '/';
    size_t total = dir_len + (need_sep ? 1 : 0) + leaf_len + 1;

    pal_char_t* out = (pal_char_t*)malloc(total);
    if (out == NULL)
        return NULL;

    memcpy(out, dir, dir_len);
    if (need_sep)
        out[dir_len] = '/';
    memcpy(out + dir_len + (need_sep ? 1 : 0), leaf, leaf_len);
    out[total - 1] = '\0';
    return out;
}

// Reads up to the first newline from `file`, returning the line (with the
// trailing '\n' stripped) as a fresh allocation in *out_line.
// Returns true only if a non-empty line was read. Mirrors the C++
// `get_line_from_file` semantics: blank lines are NOT skipped, and an empty
// line (or pure-EOF) returns false.
static bool get_line_from_file(FILE* file, pal_char_t** out_line)
{
    *out_line = NULL;

    char buffer[256];
    pal_char_t* line = NULL;
    size_t line_len = 0;

    while (fgets(buffer, sizeof(buffer), file))
    {
        size_t chunk_len = strlen(buffer);
        pal_char_t* grown = (pal_char_t*)realloc(line, line_len + chunk_len + 1);
        if (grown == NULL)
        {
            free(line);
            return false;
        }
        line = grown;
        memcpy(line + line_len, buffer, chunk_len);
        line_len += chunk_len;
        line[line_len] = '\0';

        if (line_len > 0 && line[line_len - 1] == '\n')
        {
            line[--line_len] = '\0';
            break;
        }
    }

    if (line == NULL || line_len == 0)
    {
        free(line);
        return false;
    }

    *out_line = line;
    return true;
}

// Reads the first non-blank-stripped line of `file_path` (the recorded install
// location). On success returns true and sets *out_location to a fresh
// allocation. *out_file_found is set to indicate whether the file existed
// (false → caller may attempt a fallback file; true → no fallback).
static bool get_install_location_from_file(const pal_char_t* file_path, bool* out_file_found, pal_char_t** out_location)
{
    *out_file_found = true;
    *out_location = NULL;

    FILE* file = pal_file_open(file_path, _X("r"));
    if (file == NULL)
    {
        if (errno == ENOENT)
        {
            trace_verbose(_X("The install_location file ['%s'] does not exist - skipping."), file_path);
            *out_file_found = false;
        }
        else
        {
            trace_error(_X("The install_location file ['%s'] failed to open: %s."), file_path, strerror(errno));
        }

        return false;
    }

    bool got_line = get_line_from_file(file, out_location);
    fclose(file);

    if (!got_line)
    {
        trace_warning(_X("Did not find any install location in '%s'."), file_path);
        return false;
    }

    return true;
}

// Computes "<base>/install_location_<lower-arch>". `base` defaults to
// "/etc/dotnet" but is overridable by _DOTNET_TEST_INSTALL_LOCATION_PATH.
static pal_char_t* compose_install_location_file_path(void)
{
    pal_char_t* base = utils_test_only_getenv(_X("_DOTNET_TEST_INSTALL_LOCATION_PATH"));
    if (base == NULL)
    {
        base = pal_strdup(_X("/etc/dotnet"));
        if (base == NULL)
            return NULL;
    }

    pal_char_t* arch_lower = ascii_lower_dup(_STRINGIFY(CURRENT_ARCH_NAME));
    if (arch_lower == NULL)
    {
        free(base);
        return NULL;
    }

    size_t base_len = strlen(base);
    bool need_sep = base_len > 0 && base[base_len - 1] != '/';
    const char prefix[] = "install_location_";
    size_t prefix_len = ARRAY_SIZE(prefix) - 1;
    size_t arch_len = strlen(arch_lower);
    size_t total = base_len + (need_sep ? 1 : 0) + prefix_len + arch_len + 1;

    pal_char_t* out = (pal_char_t*)malloc(total);
    if (out == NULL)
    {
        free(base);
        free(arch_lower);
        return NULL;
    }

    memcpy(out, base, base_len);
    size_t pos = base_len;
    if (need_sep)
        out[pos++] = '/';
    memcpy(out + pos, prefix, prefix_len);
    pos += prefix_len;
    memcpy(out + pos, arch_lower, arch_len);
    out[total - 1] = '\0';

    free(base);
    free(arch_lower);
    return out;
}

pal_char_t* pal_get_dotnet_self_registered_config_location(void)
{
    return compose_install_location_file_path();
}

pal_char_t* pal_get_dotnet_self_registered_dir(void)
{
    pal_char_t* override = utils_test_only_getenv(_X("_DOTNET_TEST_GLOBALLY_REGISTERED_PATH"));
    if (override != NULL)
        return override;

    pal_char_t* arch_path = compose_install_location_file_path();
    if (arch_path == NULL)
        return NULL;

    trace_verbose(_X("Looking for architecture-specific install_location file in '%s'."), arch_path);

    pal_char_t* location = NULL;
    bool file_found = false;
    bool ok = get_install_location_from_file(arch_path, &file_found, &location);
    if (!ok && !file_found)
    {
        // For current architecture, fall back to the non-arch-specific file
        // in the same base directory.
        char* sep = strrchr(arch_path, '/');
        pal_char_t* legacy = NULL;
        if (sep != NULL)
        {
            size_t base_len = (size_t)(sep - arch_path);
            pal_char_t* base = (pal_char_t*)malloc(base_len + 1);
            if (base != NULL)
            {
                memcpy(base, arch_path, base_len);
                base[base_len] = '\0';
                legacy = join_path_alloc(base, _X("install_location"));
                free(base);
            }
        }
        else
        {
            legacy = pal_strdup(_X("install_location"));
        }

        free(arch_path);
        arch_path = NULL;

        if (legacy == NULL)
            return NULL;

        trace_verbose(_X("Looking for install_location file in '%s'."), legacy);
        ok = get_install_location_from_file(legacy, &file_found, &location);
        free(legacy);
        if (!ok)
            return NULL;
    }
    else
    {
        free(arch_path);
        if (!ok)
            return NULL;
    }

    trace_verbose(_X("Found registered install location '%s'."), location);
    return location;
}

pal_char_t* pal_get_default_installation_dir(void)
{
    pal_char_t* override = utils_test_only_getenv(_X("_DOTNET_TEST_DEFAULT_INSTALL_PATH"));
    if (override != NULL)
        return override;

#if defined(TARGET_OSX)
    const pal_char_t* base = _X("/usr/local/share/dotnet");
    if (pal_is_emulating_x64())
    {
        return join_path_alloc(base, _STRINGIFY(CURRENT_ARCH_NAME));
    }

    return pal_strdup(base);
#elif defined(TARGET_FREEBSD)
    int mib[2];
    char buf[PATH_MAX];
    size_t len = PATH_MAX;

    mib[0] = CTL_USER;
    mib[1] = USER_LOCALBASE;
    if (sysctl(mib, 2, buf, &len, NULL, 0) == 0)
    {
        return join_path_alloc(buf, _X("share/dotnet"));
    }

    return pal_strdup(_X("/usr/local/share/dotnet"));
#else
    return pal_strdup(_X("/usr/share/dotnet"));
#endif
}
