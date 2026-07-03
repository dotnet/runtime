// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C implementations of the pal_* APIs needed by trace.c on Windows.

#include "pal.h"
#include "trace.h"
#include "utils.h"

#include <stdlib.h>
#include <string.h>
#include <wchar.h>

#include <minipal/utils.h>

// Long-path extended-syntax prefixes for Windows paths.
#define EXTENDED_PATH_PREFIX     _X("\\\\?\\")       // "\\?\" (also covers "\\?\UNC\")
#define UNC_EXTENDED_PATH_PREFIX _X("\\\\?\\UNC\\")  // "\\?\UNC\"
#define DEVICE_PATH_PREFIX       _X("\\\\.\\")       // "\\.\"

// Windows path separators.
#define ALT_DIR_SEPARATOR L'/'
#define VOLUME_SEPARATOR  L':'

// Returns the full path of `module` (or the current executable when `module`
// is NULL) as a heap-allocated string, or NULL on failure. Caller must free().
static pal_char_t* get_module_file_name(HMODULE module)
{
    // GetModuleFileNameW returns 0 on failure, the number of characters
    // written (not including the terminating null) on success, and the buffer
    // size to signal that the buffer was too small. Start with MAX_PATH and
    // double until the result fits.
    DWORD size = MAX_PATH / 2;
    pal_char_t* buf = NULL;
    DWORD size_written;
    do
    {
        size *= 2;
        pal_char_t* new_buf = (pal_char_t*)realloc(buf, size * sizeof(pal_char_t));
        if (new_buf == NULL)
        {
            free(buf);
            return NULL;
        }
        buf = new_buf;

        size_written = GetModuleFileNameW(module, buf, size);
    } while (size_written == size);

    if (size_written == 0)
    {
        free(buf);
        return NULL;
    }

    return buf;
}

pal_char_t* pal_get_own_executable_path(void)
{
    return get_module_file_name(NULL);
}

bool pal_directory_exists(const pal_char_t* path)
{
    DWORD attributes = GetFileAttributesW(path);
    return attributes != INVALID_FILE_ATTRIBUTES && (attributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
}

pal_char_t* pal_strdup(const pal_char_t* str)
{
    return _wcsdup(str);
}

pal_char_t* pal_getenv(const pal_char_t* name)
{
    DWORD needed = GetEnvironmentVariableW(name, NULL, 0);
    if (needed == 0)
    {
        DWORD err = GetLastError();
        if (err != ERROR_ENVVAR_NOT_FOUND && err != ERROR_SUCCESS)
        {
            trace_warning(_X("Failed to read environment variable [%s], HRESULT: 0x%X"), name, HRESULT_FROM_WIN32(err));
        }
        return NULL;
    }

    pal_char_t* result = (pal_char_t*)malloc(needed * sizeof(pal_char_t));
    if (result == NULL)
        return NULL;

    DWORD written = GetEnvironmentVariableW(name, result, needed);
    if (written == 0 || written >= needed)
    {
        DWORD err = GetLastError();
        if (err != ERROR_ENVVAR_NOT_FOUND && err != ERROR_SUCCESS)
        {
            trace_warning(_X("Failed to read environment variable [%s], HRESULT: 0x%X"), name, HRESULT_FROM_WIN32(err));
        }
        free(result);
        return NULL;
    }

    return result;
}

// Returns true if the path is already prefixed with one of the long-path
// extended-syntax prefixes:
//   \\?\        (extended path prefix - includes \\?\UNC\)
//   \\.\        (device path prefix)
static bool is_path_normalized(const pal_char_t* path)
{
    if (path[0] == L'\0')
        return true;

    return wcsncmp(path, EXTENDED_PATH_PREFIX, STRING_LENGTH(EXTENDED_PATH_PREFIX)) == 0
        || wcsncmp(path, DEVICE_PATH_PREFIX, STRING_LENGTH(DEVICE_PATH_PREFIX)) == 0;
}

pal_char_t* pal_fullpath(const pal_char_t* path, bool skip_error_logging)
{
    if (path == NULL || path[0] == L'\0')
        return NULL;

    // Already-normalized (long-path-prefixed) paths are passed through as-is;
    // GetFullPathNameW does not handle the \\?\ prefix correctly.
    if (is_path_normalized(path))
    {
        WIN32_FILE_ATTRIBUTE_DATA data;
        if (GetFileAttributesExW(path, GetFileExInfoStandard, &data) != 0)
            return pal_strdup(path);
    }

    // Start with a MAX_PATH-sized buffer; the typical case fits.
    pal_char_t* buf = (pal_char_t*)malloc(MAX_PATH * sizeof(pal_char_t));
    if (buf == NULL)
        return NULL;

    DWORD size = GetFullPathNameW(path, MAX_PATH, buf, NULL);
    if (size == 0)
    {
        if (!skip_error_logging)
            trace_error(_X("Error resolving full path [%s]"), path);
        free(buf);
        return NULL;
    }

    if (size >= MAX_PATH)
    {
        // Need a larger buffer. Allocate enough room for the canonicalized
        // path plus the longest long-path prefix ("\\?\UNC\").
        const DWORD prefix_headroom = STRING_LENGTH(UNC_EXTENDED_PATH_PREFIX);
        pal_char_t* new_buf = (pal_char_t*)realloc(buf, (size + prefix_headroom) * sizeof(pal_char_t));
        if (new_buf == NULL)
        {
            free(buf);
            return NULL;
        }
        buf = new_buf;

        DWORD new_size = GetFullPathNameW(path, size, buf, NULL);
        if (new_size == 0 || new_size >= size)
        {
            if (!skip_error_logging)
                trace_error(_X("Error resolving full path [%s]"), path);
            free(buf);
            return NULL;
        }

        // Long paths require the \\?\ (or \\?\UNC\) prefix to be usable.
        // For UNC paths (\\server\share\...), strip the leading "\\" and
        // prepend "\\?\UNC\"; otherwise just prepend "\\?\".
        bool is_unc = (buf[0] == L'\\' && buf[1] == L'\\');
        const pal_char_t* prefix = is_unc ? UNC_EXTENDED_PATH_PREFIX : EXTENDED_PATH_PREFIX;
        DWORD prefix_len = is_unc ? STRING_LENGTH(UNC_EXTENDED_PATH_PREFIX) : STRING_LENGTH(EXTENDED_PATH_PREFIX);
        DWORD skip = is_unc ? STRING_LENGTH(_X("\\\\")) : 0; // drop the UNC's leading "\\"

        // Make room for the prefix by shifting the path right (including the NUL).
        memmove(buf + prefix_len, buf + skip, (new_size - skip + 1) * sizeof(pal_char_t));
        memcpy(buf, prefix, prefix_len * sizeof(pal_char_t));
    }

    WIN32_FILE_ATTRIBUTE_DATA data;
    if (GetFileAttributesExW(buf, GetFileExInfoStandard, &data) == 0)
    {
        free(buf);
        return NULL;
    }

    return buf;
}

bool pal_file_exists(const pal_char_t* path)
{
    // pal_fullpath canonicalizes (adding the long-path \\?\ prefix when needed)
    // and verifies existence, so a non-NULL result means the path exists.
    pal_char_t* resolved = pal_fullpath(path, true);
    bool exists = resolved != NULL;
    free(resolved);
    return exists;
}

static bool is_dir_separator(pal_char_t c)
{
    return c == DIR_SEPARATOR || c == ALT_DIR_SEPARATOR;
}

// Returns true if the path is relative to the current drive or working
// directory (i.e. not rooted at a specific drive or UNC share), and therefore
// must be canonicalized before it can be reliably used.
static bool is_path_not_fully_qualified(const pal_char_t* path)
{
    size_t len = pal_strlen(path);

    // Too short to encode a drive ("X:") or UNC ("\\") root.
    if (len < 2)
        return true;

    // Starts with a separator: fully qualified only if it's a UNC path,
    // i.e. the second character is also a separator ("\\server\share").
    if (is_dir_separator(path[0]))
        return !is_dir_separator(path[1]);

    // Otherwise it must be a drive-rooted path of the form "X:\": at least
    // three characters, a volume separator at index 1, and a directory
    // separator at index 2.
    return len < 3
        || path[1] != VOLUME_SEPARATOR
        || !is_dir_separator(path[2]);
}

// Returns true if the path needs normalization (canonicalization, and the \\?\
// prefix for long paths): it isn't already normalized and is either not fully
// qualified or at least MAX_PATH characters long.
static bool should_normalize_path(const pal_char_t* path)
{
    if (is_path_normalized(path))
        return false;

    if (!is_path_not_fully_qualified(path) && pal_strlen(path) < MAX_PATH)
        return false;

    return true;
}

bool pal_readdir_onlydirectories(const pal_char_t* path, pal_readdir_callback_t callback, void* ctx)
{
    if (path == NULL || callback == NULL)
        return false;

    // Long or not-fully-qualified paths must be canonicalized (and prefixed with
    // \\?\ when long) before being passed to FindFirstFileExW.
    pal_char_t* normalized = NULL;
    if (should_normalize_path(path))
    {
        normalized = pal_fullpath(path, /*skip_error_logging*/ false);
        if (normalized == NULL)
            return false;

        path = normalized;
    }

    // Build the search string: path + "\\*". One extra char beyond path
    // length is needed for the separator if path doesn't already end with one.
    size_t path_len = pal_strlen(path);
    size_t search_len = path_len + 3; // worst case: '\\', '*', NUL
    pal_char_t* search = (pal_char_t*)malloc(search_len * sizeof(pal_char_t));
    if (search == NULL)
    {
        free(normalized);
        return false;
    }

    memcpy(search, path, path_len * sizeof(pal_char_t));
    free(normalized); // path has been copied into search; no longer needed
    size_t pos = path_len;
    if (pos == 0 || !is_dir_separator(search[pos - 1]))
    {
        search[pos++] = DIR_SEPARATOR;
    }
    search[pos++] = L'*';
    search[pos] = L'\0';

    WIN32_FIND_DATAW data = { 0 };
    HANDLE handle = FindFirstFileExW(search, FindExInfoStandard, &data, FindExSearchNameMatch, NULL, 0);
    free(search);
    if (handle == INVALID_HANDLE_VALUE)
        return false;

    do
    {
        if ((data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0)
            continue;

        const pal_char_t* name = data.cFileName;
        if (name[0] == L'.' && (name[1] == L'\0' || (name[1] == L'.' && name[2] == L'\0')))
            continue;

        if (!callback(name, ctx))
            break;
    } while (FindNextFileW(handle, &data));

    FindClose(handle);
    return true;
}

typedef BOOL(WINAPI* is_wow64_process2_fn)(HANDLE, USHORT*, USHORT*);

pal_process_emulation_t pal_get_process_emulation(void)
{
#if defined(TARGET_AMD64)
    // x64 build: detect x64-on-arm64 emulation via IsWow64Process2.
    HMODULE kernel32 = LoadLibraryExW(L"kernel32.dll", NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (kernel32 == NULL)
    {
        trace_info(_X("Could not load 'kernel32.dll': %u"), GetLastError());
    }
    else
    {
        is_wow64_process2_fn is_wow64_process2_func = (is_wow64_process2_fn)(void*)GetProcAddress(kernel32, "IsWow64Process2");
        if (is_wow64_process2_func != NULL)
        {
            USHORT process_machine;
            USHORT native_machine;
            if (is_wow64_process2_func(GetCurrentProcess(), &process_machine, &native_machine))
            {
                if (native_machine != IMAGE_FILE_MACHINE_AMD64)
                    return pal_process_emulation_x64;
            }
            else
            {
                trace_info(_X("Call to IsWow64Process2 failed: %u"), GetLastError());
            }
        }
    }
#endif

    // 32-bit (x86) process running on a 64-bit Windows OS.
    BOOL is_wow64 = FALSE;
    if (IsWow64Process(GetCurrentProcess(), &is_wow64) && is_wow64 != FALSE)
        return pal_process_emulation_wow64;

    return pal_process_emulation_none;
}

// Get the registry hive and sub-key for the globally-registered .NET install:
//   HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\<arch>.
// If set, the _DOTNET_TEST_REGISTRY_PATH environment variable can switch the
// hive to HKCU and change the SOFTWARE\dotnet part of the sub-key.
static bool get_dotnet_install_location_registry_path(HKEY* out_hive, pal_char_t** out_sub_key)
{
    *out_hive = HKEY_LOCAL_MACHINE;

    const pal_char_t* base = _X("SOFTWARE\\dotnet");
    pal_char_t* override = utils_test_only_getenv(_X("_DOTNET_TEST_REGISTRY_PATH"));
    if (override != NULL)
    {
        const pal_char_t hkcu_prefix[] = _X("HKEY_CURRENT_USER\\");
        if (wcsncmp(override, hkcu_prefix, ARRAY_SIZE(hkcu_prefix) - 1) == 0)
        {
            *out_hive = HKEY_CURRENT_USER;
            base = override + (ARRAY_SIZE(hkcu_prefix) - 1);
        }
        else
        {
            base = override;
        }
    }

    size_t total = pal_strlen(base) + STRING_LENGTH("\\Setup\\InstalledVersions\\" CURRENT_ARCH_NAME) + 1;

    pal_char_t* combined = (pal_char_t*)malloc(total * sizeof(pal_char_t));
    if (combined != NULL)
        pal_str_printf(combined, total, _X("%s\\Setup\\InstalledVersions\\") _STRINGIFY(CURRENT_ARCH_NAME), base);

    free(override);
    *out_sub_key = combined;
    return combined != NULL;
}

// Allocates "HKLM\<sub_key>\InstallLocation" or "HKCU\..." for display/tracing.
static pal_char_t* format_registry_path(HKEY hive, const pal_char_t* sub_key)
{
    const pal_char_t* prefix = (hive == HKEY_CURRENT_USER) ? _X("HKCU\\") : _X("HKLM\\");
    size_t total = pal_strlen(prefix) + pal_strlen(sub_key) + STRING_LENGTH("\\InstallLocation") + 1;

    pal_char_t* result = (pal_char_t*)malloc(total * sizeof(pal_char_t));
    if (result == NULL)
        return NULL;

    pal_str_printf(result, total, _X("%s%s\\InstallLocation"), prefix, sub_key);
    return result;
}

pal_char_t* pal_get_dotnet_self_registered_config_location(void)
{
    HKEY hive;
    pal_char_t* sub_key;
    if (!get_dotnet_install_location_registry_path(&hive, &sub_key))
        return NULL;

    pal_char_t* result = format_registry_path(hive, sub_key);
    free(sub_key);
    return result;
}

pal_char_t* pal_get_dotnet_self_registered_dir(void)
{
    pal_char_t* override = utils_test_only_getenv(_X("_DOTNET_TEST_GLOBALLY_REGISTERED_PATH"));
    if (override != NULL)
        return override;

    HKEY hive;
    pal_char_t* sub_key;
    if (!get_dotnet_install_location_registry_path(&hive, &sub_key))
        return NULL;

    if (trace_is_enabled())
    {
        pal_char_t* display = format_registry_path(hive, sub_key);
        if (display != NULL)
        {
            trace_verbose(_X("Looking for architecture-specific registry value in '%s'."), display);
            free(display);
        }
    }

    // Always look in the 32-bit registry view via KEY_WOW64_32KEY.
    HKEY hkey = NULL;
    LSTATUS status = RegOpenKeyExW(hive, sub_key, 0, KEY_READ | KEY_WOW64_32KEY, &hkey);
    if (status != ERROR_SUCCESS)
    {
        if (status == ERROR_FILE_NOT_FOUND)
            trace_verbose(_X("The registry key ['%s'] does not exist."), sub_key);
        else
            trace_verbose(_X("Failed to open the registry key. Error code: 0x%X"), (unsigned int)status);

        free(sub_key);
        return NULL;
    }

    free(sub_key);

    const pal_char_t* value = _X("InstallLocation");

    // RegGetValueW reports size in BYTES (including null terminator on REG_SZ).
    DWORD size_bytes = 0;
    status = RegGetValueW(hkey, NULL, value, RRF_RT_REG_SZ, NULL, NULL, &size_bytes);
    if (status != ERROR_SUCCESS || size_bytes == 0)
    {
        trace_verbose(_X("Failed to get the size of the install location registry value or it's empty. Error code: 0x%X"), (unsigned int)status);
        RegCloseKey(hkey);
        return NULL;
    }

    pal_char_t* buffer = (pal_char_t*)malloc(size_bytes);
    if (buffer == NULL)
    {
        RegCloseKey(hkey);
        return NULL;
    }

    status = RegGetValueW(hkey, NULL, value, RRF_RT_REG_SZ, NULL, buffer, &size_bytes);
    RegCloseKey(hkey);
    if (status != ERROR_SUCCESS)
    {
        trace_verbose(_X("Failed to get the value of the install location registry value. Error code: 0x%X"), (unsigned int)status);
        free(buffer);
        return NULL;
    }

    trace_verbose(_X("Found registered install location '%s'."), buffer);
    return buffer;
}

pal_char_t* pal_get_default_installation_dir(void)
{
    pal_char_t* override = utils_test_only_getenv(_X("_DOTNET_TEST_DEFAULT_INSTALL_PATH"));
    if (override != NULL)
        return override;

    pal_char_t* program_files = pal_getenv(_X("ProgramFiles"));
    if (program_files == NULL)
        return NULL;

    pal_char_t* canonical = pal_fullpath(program_files, /*skip_error_logging*/ false);
    if (canonical == NULL)
    {
        trace_verbose(_X("Did not find [%s] directory [%s]"), _X("ProgramFiles"), program_files);
        free(program_files);
        return NULL;
    }

    free(program_files);

    // Append "\dotnet" (and "\x64" if emulating x64).
    const pal_char_t dotnet_seg[] = _X("\\dotnet");
    const pal_char_t arch_seg[] = _X("\\") _STRINGIFY(CURRENT_ARCH_NAME);

    size_t canonical_len = pal_strlen(canonical);
    size_t dotnet_len = ARRAY_SIZE(dotnet_seg) - 1;
    bool emulating_x64 = pal_get_process_emulation() == pal_process_emulation_x64;
    size_t arch_len = emulating_x64 ? (ARRAY_SIZE(arch_seg) - 1) : 0;
    size_t total = canonical_len + dotnet_len + arch_len + 1;

    pal_char_t* result = (pal_char_t*)realloc(canonical, total * sizeof(pal_char_t));
    if (result == NULL)
    {
        free(canonical);
        return NULL;
    }

    memcpy(result + canonical_len, dotnet_seg, dotnet_len * sizeof(pal_char_t));
    if (emulating_x64)
        memcpy(result + canonical_len + dotnet_len, arch_seg, arch_len * sizeof(pal_char_t));
    result[total - 1] = L'\0';

    return result;
}

bool pal_get_loaded_library(
    const pal_char_t* library_name,
    const char* symbol_name,
    pal_dll_t* dll,
    pal_char_t** out_path)
{
    (void)symbol_name;
    *dll = NULL;
    *out_path = NULL;

    HMODULE dll_maybe = GetModuleHandleW(library_name);
    if (dll_maybe == NULL)
        return false;

    pal_char_t* path = get_module_file_name(dll_maybe);
    if (path == NULL)
        return false;

    *dll = dll_maybe;
    *out_path = path;
    return true;
}
