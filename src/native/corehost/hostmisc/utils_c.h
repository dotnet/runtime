// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef UTILS_C_H
#define UTILS_C_H

#include <stdbool.h>
#include <stddef.h>
#include <runtime_version.h>
#include <minipal/utils.h>

#include "pal_c.h" // for pal_char_t, _X, _STRINGIFY

#ifdef __cplusplus
extern "C" {
#endif

#ifndef DOTNET_CORE_DOWNLOAD_URL
#define DOTNET_CORE_DOWNLOAD_URL _X("https://aka.ms/dotnet/download")
#endif
#ifndef DOTNET_CORE_APPLAUNCH_URL
#define DOTNET_CORE_APPLAUNCH_URL _X("https://aka.ms/dotnet-core-applaunch")
#endif

#ifndef DOTNET_INFO_URL
#define DOTNET_INFO_URL _X("https://aka.ms/dotnet/info")
#endif
#ifndef DOTNET_APP_LAUNCH_FAILED_URL
#define DOTNET_APP_LAUNCH_FAILED_URL _X("https://aka.ms/dotnet/app-launch-failed")
#endif
#ifndef DOTNET_SDK_NOT_FOUND_URL
#define DOTNET_SDK_NOT_FOUND_URL _X("https://aka.ms/dotnet/sdk-not-found")
#endif

#ifndef INSTALL_OR_UPDATE_NET_ERROR_MESSAGE
#define INSTALL_OR_UPDATE_NET_ERROR_MESSAGE _X("You must install or update .NET to run this application.")
#endif
#ifndef INSTALL_NET_ERROR_MESSAGE
#define INSTALL_NET_ERROR_MESSAGE _X("You must install .NET to run this application.")
#endif
#ifndef INSTALL_NET_DESKTOP_ERROR_MESSAGE
#define INSTALL_NET_DESKTOP_ERROR_MESSAGE _X("You must install .NET Desktop Runtime to run this application.")
#endif

#ifndef DOC_LINK_INTRO
#define DOC_LINK_INTRO _X("Learn more:")
#endif

#ifndef MISSING_RUNTIME_ERROR_FORMAT
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
#endif

#ifndef DOTNET_ROOT_ENV_VAR
#define DOTNET_ROOT_ENV_VAR _X("DOTNET_ROOT")
#endif

#ifndef _TEXT
#define _TEXT(x) #x
#endif
#ifndef _QUOTE
#define _QUOTE(x) _TEXT(x)
#endif

#ifndef HOST_VERSION
#define HOST_VERSION _QUOTE(RuntimeProductVersion)
#endif

// Get the directory portion of a path. Result written to out_dir (must be at least APPHOST_PATH_MAX).
void utils_get_directory(const pal_char_t* path, pal_char_t* out_dir, size_t out_dir_len);

// Get the filename portion of a path.
void utils_get_filename(const pal_char_t* path, pal_char_t* out_name, size_t out_name_len);

// Append a path component with directory separator.
void utils_append_path(pal_char_t* path, size_t path_len, const pal_char_t* component);

// Replace all occurrences of match with repl in path (in-place).
void utils_replace_char(pal_char_t* path, pal_char_t match, pal_char_t repl);

// Check if a file exists in a directory. If found, writes full path to out_file_path.
bool utils_file_exists_in_dir(const pal_char_t* dir, const pal_char_t* file_name, pal_char_t* out_file_path, size_t out_path_len);

// Get architecture name string.
const pal_char_t* utils_get_current_arch_name(void);

// Get a download URL for the runtime.
void utils_get_download_url(pal_char_t* out_url, size_t out_url_len);

// Get host version description.
void utils_get_host_version_description(pal_char_t* out_desc, size_t out_desc_len);

// Check if value starts with prefix.
bool utils_starts_with(const pal_char_t* value, const pal_char_t* prefix);

// Check if value ends with suffix.
bool utils_ends_with(const pal_char_t* value, const pal_char_t* suffix);

// Get dotnet root from environment variables (DOTNET_ROOT_<ARCH> or DOTNET_ROOT).
// Returns true if found. out_env_var_name gets the name of the env var used.
bool utils_get_dotnet_root_from_env(pal_char_t* out_env_var_name, size_t env_var_name_len, pal_char_t* recv, size_t recv_len);

// Convert string to uppercase in-place.
void utils_to_upper(pal_char_t* str);

// Get runtime ID string.
void utils_get_runtime_id(pal_char_t* out_rid, size_t out_rid_len);

// Get the dotnet root env var name for the current architecture.
void utils_get_dotnet_root_env_var_for_arch(pal_char_t* out_name, size_t out_name_len);

#ifdef __cplusplus
}
#endif

#endif // UTILS_C_H
