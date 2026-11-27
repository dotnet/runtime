// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _COREHOST_CLI_FXR_RESOLVER_H_
#define _COREHOST_CLI_FXR_RESOLVER_H_

#include <pal.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

// Keep in sync with HostWriter.DotNetSearchOptions.SearchLocation in HostWriter.cs.
typedef enum
{
    fxr_search_location_default = 0,
    fxr_search_location_app_local = 1 << 0,             // Next to the app
    fxr_search_location_app_relative = 1 << 1,          // Path relative to the app read from the app binary
    fxr_search_location_environment_variable = 1 << 2,  // DOTNET_ROOT[_<arch>] environment variables
    fxr_search_location_global = 1 << 3,                // Registered and default global locations
} fxr_search_location;

// Try to locate hostfxr using the specified search options.
// Caller should free() out_dotnet_root and out_fxr_path.
bool fxr_resolver_try_get_path(
    const pal_char_t* root_path,
    fxr_search_location search,
    /*opt*/ const pal_char_t* app_relative_dotnet_root,
    /*out*/ pal_char_t** out_dotnet_root,
    /*out*/ pal_char_t** out_fxr_path);

// Try to locate hostfxr at <dotnet_root>/host/fxr/<latest>/hostfxr.<ext>.
// Caller should free() out_fxr_path.
bool fxr_resolver_try_get_path_from_dotnet_root(
    const pal_char_t* dotnet_root,
    /*out*/ pal_char_t** out_fxr_path);

// Try to find an already-loaded hostfxr in the current process. On success
// returns true, sets *out_fxr to the library handle and *out_fxr_path to a
// heap-allocated path. Caller should free() out_fxr_path.
bool fxr_resolver_try_get_existing_fxr(
    /*out*/ pal_dll_t* out_fxr,
    /*out*/ pal_char_t** out_fxr_path);

#ifdef __cplusplus
}
#endif

#endif //_COREHOST_CLI_FXR_RESOLVER_H_
