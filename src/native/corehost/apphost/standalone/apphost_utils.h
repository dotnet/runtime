// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef APPHOST_UTILS_H
#define APPHOST_UTILS_H

#include <stdbool.h>
#include <stddef.h>
#include <runtime_version.h>
#include <minipal/utils.h>

#define _X(s) s
#define _STRINGIFY(s) _X(s)

#define DOTNET_CORE_DOWNLOAD_URL _X("https://aka.ms/dotnet/download")
#define DOTNET_CORE_APPLAUNCH_URL _X("https://aka.ms/dotnet-core-applaunch")

#define DOTNET_INFO_URL _X("https://aka.ms/dotnet/info")
#define DOTNET_APP_LAUNCH_FAILED_URL _X("https://aka.ms/dotnet/app-launch-failed")
#define DOTNET_SDK_NOT_FOUND_URL _X("https://aka.ms/dotnet/sdk-not-found")

#define INSTALL_OR_UPDATE_NET_ERROR_MESSAGE _X("You must install or update .NET to run this application.")
#define INSTALL_NET_ERROR_MESSAGE _X("You must install .NET to run this application.")
#define INSTALL_NET_DESKTOP_ERROR_MESSAGE _X("You must install .NET Desktop Runtime to run this application.")

#define DOC_LINK_INTRO _X("Learn more:")

#define MISSING_RUNTIME_ERROR_FORMAT \
    _X("%s\n\n")                                \
    _X("App: %s\n")                             \
    _X("Architecture: %s\n")                    \
    _X("App host version: %s\n")                \
    _X(".NET location: %s\n")                   \
    _X("\n")                                    \
    DOC_LINK_INTRO _X("\n")                     \
    DOTNET_APP_LAUNCH_FAILED_URL                \
    _X("\n\n")                                  \
    _X("Download the .NET runtime:\n")          \
    _X("%s&apphost_version=%s")

#define DOTNET_ROOT_ENV_VAR _X("DOTNET_ROOT")

#define _TEXT(x) #x
#define _QUOTE(x) _TEXT(x)

#define HOST_VERSION _QUOTE(RuntimeProductVersion)

// Path buffer size
#define APPHOST_PATH_MAX 4096

// Get the directory portion of a path. Result written to out_dir (must be at least APPHOST_PATH_MAX).
void utils_get_directory(const char* path, char* out_dir, size_t out_dir_len);

// Get the filename portion of a path.
void utils_get_filename(const char* path, char* out_name, size_t out_name_len);

// Append a path component with directory separator.
void utils_append_path(char* path, size_t path_len, const char* component);

// Replace all occurrences of match with repl in path (in-place).
void utils_replace_char(char* path, char match, char repl);

// Check if a file exists in a directory. If found, writes full path to out_file_path.
bool utils_file_exists_in_dir(const char* dir, const char* file_name, char* out_file_path, size_t out_path_len);

// Get architecture name string.
const char* utils_get_current_arch_name(void);

// Get a download URL for the runtime.
void utils_get_download_url(char* out_url, size_t out_url_len);

// Get host version description.
void utils_get_host_version_description(char* out_desc, size_t out_desc_len);

// Check if value starts with prefix.
bool utils_starts_with(const char* value, const char* prefix);

// Check if value ends with suffix.
bool utils_ends_with(const char* value, const char* suffix);

// Get dotnet root from environment variables (DOTNET_ROOT_<ARCH> or DOTNET_ROOT).
// Returns true if found. out_env_var_name gets the name of the env var used.
bool utils_get_dotnet_root_from_env(char* out_env_var_name, size_t env_var_name_len, char* recv, size_t recv_len);

// Convert string to uppercase in-place.
void utils_to_upper(char* str);

// Get runtime ID string.
void utils_get_runtime_id(char* out_rid, size_t out_rid_len);

// Get the dotnet root env var name for the current architecture.
void utils_get_dotnet_root_env_var_for_arch(char* out_name, size_t out_name_len);

#endif // APPHOST_UTILS_H
