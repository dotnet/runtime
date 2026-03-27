// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "apphost_pal.h"
#include "apphost_trace.h"

#include <ctype.h>
#include <dlfcn.h>
#include <dirent.h>
#include <fcntl.h>
#include <fnmatch.h>
#include <sys/stat.h>
#include <minipal/getexepath.h>
#include "config.h"

#if !HAVE_DIRENT_D_TYPE
#define DT_UNKNOWN 0
#define DT_DIR 4
#define DT_REG 8
#define DT_LNK 10
#endif

bool pal_get_own_executable_path(char* recv, size_t recv_len)
{
    char* path = minipal_getexepath();
    if (!path)
        return false;

    size_t len = strlen(path);
    if (len >= recv_len)
    {
        free(path);
        return false;
    }

    memcpy(recv, path, len + 1);
    free(path);
    return true;
}

bool pal_fullpath(char* path, size_t path_len)
{
    char* resolved = realpath(path, NULL);
    if (resolved == NULL)
    {
        if (errno == ENOENT)
            return false;

        trace_error("realpath(%s) failed: %s", path, strerror(errno));
        return false;
    }

    size_t len = strlen(resolved);
    if (len >= path_len)
    {
        free(resolved);
        return false;
    }

    memcpy(path, resolved, len + 1);
    free(resolved);
    return true;
}

bool pal_file_exists(const char* path)
{
    return (access(path, F_OK) == 0);
}

bool pal_directory_exists(const char* path)
{
    return pal_file_exists(path);
}

bool pal_is_path_fully_qualified(const char* path)
{
    return path != NULL && path[0] == '/';
}

bool pal_getenv(const char* name, char* recv, size_t recv_len)
{
    if (recv_len > 0)
        recv[0] = '\0';

    const char* result = getenv(name);
    if (result != NULL && result[0] != '\0')
    {
        size_t len = strlen(result);
        if (len >= recv_len)
            return false;

        memcpy(recv, result, len + 1);
        return true;
    }

    return false;
}

int pal_xtoi(const char* input)
{
    return atoi(input);
}

bool pal_load_library(const char* path, void** dll)
{
    *dll = dlopen(path, RTLD_LAZY);
    if (*dll == NULL)
    {
        trace_error("Failed to load %s, error: %s", path, dlerror());
        return false;
    }
    return true;
}

void pal_unload_library(void* library)
{
    if (dlclose(library) != 0)
    {
        trace_warning("Failed to unload library, error: %s", dlerror());
    }
}

void* pal_get_symbol(void* library, const char* name)
{
    void* result = dlsym(library, name);
    if (result == NULL)
    {
        trace_info("Probed for and did not find library symbol %s, error: %s", name, dlerror());
    }
    return result;
}

void pal_err_print_line(const char* message)
{
    fputs(message, stderr);
    fputc('\n', stderr);
}

void pal_readdir_onlydirectories(const char* path, pal_readdir_callback_fn callback, void* context)
{
    DIR* dir = opendir(path);
    if (dir == NULL)
        return;

    struct dirent* entry = NULL;
    while ((entry = readdir(dir)) != NULL)
    {
        if (strcmp(entry->d_name, ".") == 0 || strcmp(entry->d_name, "..") == 0)
            continue;

#if HAVE_DIRENT_D_TYPE
        int dirEntryType = entry->d_type;
#else
        int dirEntryType = DT_UNKNOWN;
#endif

        switch (dirEntryType)
        {
        case DT_DIR:
            break;

        case DT_LNK:
        case DT_UNKNOWN:
        {
            struct stat sb;
            if (fstatat(dirfd(dir), entry->d_name, &sb, 0) == -1)
                continue;

            if (!S_ISDIR(sb.st_mode))
                continue;
            break;
        }

        default:
            continue;
        }

        if (!callback(entry->d_name, context))
            break;
    }

    closedir(dir);
}

#define TEST_ONLY_MARKER "d38cc827-e34f-4453-9df4-1e796e9f1d07"

// Retrieves environment variable which is only used for testing.
// This will return the value of the variable only if the product binary is stamped
// with test-only marker.
static bool test_only_getenv(const char* name, char* recv, size_t recv_len)
{
    enum { EMBED_SIZE = sizeof(TEST_ONLY_MARKER) / sizeof(TEST_ONLY_MARKER[0]) };
    volatile static char embed[EMBED_SIZE] = TEST_ONLY_MARKER;

    if (embed[0] != 'e')
        return false;

    return pal_getenv(name, recv, recv_len);
}

static bool get_install_location_from_file(const char* file_path, bool* file_found, char* install_location, size_t install_location_len)
{
    *file_found = true;
    FILE* f = fopen(file_path, "r");
    if (f != NULL)
    {
        if (fgets(install_location, (int)install_location_len, f) != NULL)
        {
            // Remove trailing newline
            size_t len = strlen(install_location);
            if (len > 0 && install_location[len - 1] == '\n')
                install_location[len - 1] = '\0';

            fclose(f);
            return install_location[0] != '\0';
        }

        trace_warning("Did not find any install location in '%s'.", file_path);
        fclose(f);
    }
    else
    {
        if (errno == ENOENT)
        {
            trace_verbose("The install_location file ['%s'] does not exist - skipping.", file_path);
            *file_found = false;
        }
        else
        {
            trace_error("The install_location file ['%s'] failed to open: %s.", file_path, strerror(errno));
        }
    }

    return false;
}

const char* pal_get_dotnet_self_registered_config_location(char* buf, size_t buf_len)
{
    const char* config_location = "/etc/dotnet";
    const char* arch_name;

    // ***Used only for testing***
    char environment_install_location_override[APPHOST_PATH_MAX];
    if (test_only_getenv("_DOTNET_TEST_INSTALL_LOCATION_PATH", environment_install_location_override, sizeof(environment_install_location_override)))
    {
        config_location = environment_install_location_override;
    }

#if defined(TARGET_AMD64)
    arch_name = "x64";
#elif defined(TARGET_X86)
    arch_name = "x86";
#elif defined(TARGET_ARMV6)
    arch_name = "armv6";
#elif defined(TARGET_ARM)
    arch_name = "arm";
#elif defined(TARGET_ARM64)
    arch_name = "arm64";
#elif defined(TARGET_LOONGARCH64)
    arch_name = "loongarch64";
#elif defined(TARGET_RISCV64)
    arch_name = "riscv64";
#elif defined(TARGET_S390X)
    arch_name = "s390x";
#elif defined(TARGET_POWERPC64)
    arch_name = "ppc64le";
#else
    arch_name = _STRINGIFY(CURRENT_ARCH_NAME);
#endif

    // Need to use a lowercase version of the arch name
    char arch_lower[32];
    size_t arch_len = strlen(arch_name);
    if (arch_len >= sizeof(arch_lower))
        arch_len = sizeof(arch_lower) - 1;
    for (size_t i = 0; i < arch_len; i++)
        arch_lower[i] = (char)tolower((unsigned char)arch_name[i]);
    arch_lower[arch_len] = '\0';

    snprintf(buf, buf_len, "%s/install_location_%s", config_location, arch_lower);
    return buf;
}

bool pal_get_dotnet_self_registered_dir(char* recv, size_t recv_len)
{
    recv[0] = '\0';

    //  ***Used only for testing***
    char environment_override[APPHOST_PATH_MAX];
    if (test_only_getenv("_DOTNET_TEST_GLOBALLY_REGISTERED_PATH", environment_override, sizeof(environment_override)))
    {
        size_t len = strlen(environment_override);
        if (len < recv_len)
            memcpy(recv, environment_override, len + 1);
        return true;
    }
    //  ***************************

    char arch_specific_path[APPHOST_PATH_MAX];
    pal_get_dotnet_self_registered_config_location(arch_specific_path, sizeof(arch_specific_path));
    trace_verbose("Looking for architecture-specific install_location file in '%s'.", arch_specific_path);

    char install_location[APPHOST_PATH_MAX];
    bool file_found = false;
    if (!get_install_location_from_file(arch_specific_path, &file_found, install_location, sizeof(install_location)))
    {
        if (file_found)
            return false;

        // Also look for the non-architecture-specific file
        // Get directory of arch_specific_path
        char dir_buf[APPHOST_PATH_MAX];
        size_t len = strlen(arch_specific_path);
        memcpy(dir_buf, arch_specific_path, len + 1);

        // Find last '/' to get directory
        char* last_sep = strrchr(dir_buf, '/');
        if (last_sep != NULL)
            *last_sep = '\0';

        char legacy_path[APPHOST_PATH_MAX];
        snprintf(legacy_path, sizeof(legacy_path), "%s/install_location", dir_buf);
        trace_verbose("Looking for install_location file in '%s'.", legacy_path);

        if (!get_install_location_from_file(legacy_path, &file_found, install_location, sizeof(install_location)))
            return false;
    }

    size_t install_len = strlen(install_location);
    if (install_len >= recv_len)
        return false;

    memcpy(recv, install_location, install_len + 1);
    trace_verbose("Found registered install location '%s'.", recv);
    return file_found;
}

bool pal_get_default_installation_dir(char* recv, size_t recv_len)
{
    //  ***Used only for testing***
    char environment_override[APPHOST_PATH_MAX];
    if (test_only_getenv("_DOTNET_TEST_DEFAULT_INSTALL_PATH", environment_override, sizeof(environment_override)))
    {
        size_t len = strlen(environment_override);
        if (len < recv_len)
            memcpy(recv, environment_override, len + 1);
        return true;
    }
    //  ***************************

#if defined(TARGET_OSX)
    const char* default_dir = "/usr/local/share/dotnet";
#elif defined(TARGET_FREEBSD)
    const char* default_dir = "/usr/local/share/dotnet";
#else
    const char* default_dir = "/usr/share/dotnet";
#endif

    size_t len = strlen(default_dir);
    if (len >= recv_len)
        return false;

    memcpy(recv, default_dir, len + 1);
    return true;
}
