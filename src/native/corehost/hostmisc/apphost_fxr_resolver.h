// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef APPHOST_FXR_RESOLVER_H
#define APPHOST_FXR_RESOLVER_H

#include <stdbool.h>
#include <stdint.h>
#include <stddef.h>

// Keep in sync with DotNetRootOptions.SearchLocation in HostWriter.cs
// and fxr_resolver.h
typedef enum
{
    search_location_default = 0,
    search_location_app_local = 1 << 0,             // Next to the app
    search_location_app_relative = 1 << 1,          // Path relative to the app read from the app binary
    search_location_environment_variable = 1 << 2,  // DOTNET_ROOT[_<arch>] environment variables
    search_location_global = 1 << 3,                // Registered and default global locations
} fxr_search_location;

// Try to find the path to hostfxr.
// root_path: directory of the app
// search: search location flags
// app_relative_dotnet_root: optional app-relative .NET root path (may be NULL or empty)
// out_dotnet_root: receives the dotnet root directory
// out_fxr_path: receives the full path to the hostfxr library
// All output buffers must be at least APPHOST_PATH_MAX bytes.
bool fxr_resolver_try_get_path(
    const char* root_path,
    fxr_search_location search,
    const char* app_relative_dotnet_root,
    char* out_dotnet_root,
    size_t out_dotnet_root_len,
    char* out_fxr_path,
    size_t out_fxr_path_len);

#endif // APPHOST_FXR_RESOLVER_H
