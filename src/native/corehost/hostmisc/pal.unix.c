// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementations of the pal_* APIs needed by trace.c on non-Windows.

#if defined(TARGET_FREEBSD)
#define _WITH_GETLINE
#endif

#include "pal.h"
#include "trace.h"
#include "utils.h"

#include <assert.h>
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

pal_process_emulation_t pal_get_process_emulation(void)
{
#if defined(TARGET_OSX)
    int is_translated_process = 0;
    size_t size = sizeof(is_translated_process);
    if (sysctlbyname("sysctl.proc_translated", &is_translated_process, &size, NULL, 0) == -1)
    {
        trace_info(_X("Could not determine whether the current process is running under Rosetta."));
        if (errno != ENOENT)
        {
            trace_info(_X("Call to sysctlbyname failed: %s"), strerror(errno));
        }

        return pal_process_emulation_none;
    }

    if (is_translated_process == 1)
        return pal_process_emulation_x64;
#endif

    return pal_process_emulation_none;
}

// Reads up to the first newline from `file`, returning the line (with the
// trailing '\n' stripped) as a fresh allocation in *out_line. Blank lines are
// not skipped; an empty line (or pure EOF) returns false, so the function
// returns true only when a non-empty line was read.
static bool get_line_from_file(FILE* file, pal_char_t** out_line)
{
    *out_line = NULL;

    pal_char_t* line = NULL;
    size_t capacity = 0;
    ssize_t len = getline(&line, &capacity, file);

    // getline keeps the trailing newline; strip it.
    if (len > 0 && line[len - 1] == '\n')
        line[--len] = '\0';

    // Reject EOF/error (len < 0) and blank lines (len == 0). getline may
    // allocate the buffer even on failure, so free it either way.
    if (len <= 0)
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

pal_char_t* pal_get_dotnet_self_registered_config_location(void)
{
    pal_char_t* override = utils_test_only_getenv(_X("_DOTNET_TEST_INSTALL_LOCATION_PATH"));
    const pal_char_t* base = override != NULL ? override : _X("/etc/dotnet");
    pal_char_t* result = utils_append_path_alloc(base, _X("install_location_") _STRINGIFY(CURRENT_ARCH_NAME));
    free(override);
    return result;
}

pal_char_t* pal_get_dotnet_self_registered_dir(void)
{
    pal_char_t* override = utils_test_only_getenv(_X("_DOTNET_TEST_GLOBALLY_REGISTERED_PATH"));
    if (override != NULL)
        return override;

    pal_char_t* path = pal_get_dotnet_self_registered_config_location();
    if (path == NULL)
        return NULL;

    trace_verbose(_X("Looking for architecture-specific install_location file in '%s'."), path);

    pal_char_t* location = NULL;
    bool file_found = false;
    bool success = get_install_location_from_file(path, &file_found, &location);
    if (!success && !file_found)
    {
        // Fall back to the non-arch-specific file in the same directory:
        // install_location instead of install_location_<arch>.
        path[pal_strlen(path) - (sizeof(_X("_") _STRINGIFY(CURRENT_ARCH_NAME)) - 1)] = '\0';

        trace_verbose(_X("Looking for install_location file in '%s'."), path);
        success = get_install_location_from_file(path, &file_found, &location);
    }

    free(path);

    if (!success)
        return NULL;

    assert(file_found);
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
    if (pal_get_process_emulation() == pal_process_emulation_x64)
    {
        return utils_append_path_alloc(base, _STRINGIFY(CURRENT_ARCH_NAME));
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
        return utils_append_path_alloc(buf, _X("share/dotnet"));
    }

    return pal_strdup(_X("/usr/local/share/dotnet"));
#else
    return pal_strdup(_X("/usr/share/dotnet"));
#endif
}
