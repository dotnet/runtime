// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _COREHOST_CLI_FXR_RESOLVER_H_
#define _COREHOST_CLI_FXR_RESOLVER_H_

#include <pal.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

// Keep in sync with fxr_resolver::search_location below and with
// DotNetRootOptions.SearchLocation in HostWriter.cs.
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

#ifdef __cplusplus

#include "hostfxr.h"
#include "trace.h"
#include "utils.h"
#include "error_codes.h"

namespace fxr_resolver
{
    // Keep in sync with DotNetRootOptions.SearchLocation in HostWriter.cs
    enum search_location : uint8_t
    {
        search_location_default = fxr_search_location_default,
        search_location_app_local = fxr_search_location_app_local,
        search_location_app_relative = fxr_search_location_app_relative,
        search_location_environment_variable = fxr_search_location_environment_variable,
        search_location_global = fxr_search_location_global,
    };

    bool try_get_path(const pal::string_t& root_path, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path);
    bool try_get_path(const pal::string_t& root_path, search_location search, /*opt*/ pal::string_t* embedded_dotnet_root, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path);
    bool try_get_path_from_dotnet_root(const pal::string_t& dotnet_root, pal::string_t* out_fxr_path);
    bool try_get_existing_fxr(pal::dll_t *out_fxr, pal::string_t *out_fxr_path);
}

template<typename THostPathToConfigCallback, typename TBeforeRunCallback>
int load_fxr_and_get_delegate(hostfxr_delegate_type type, THostPathToConfigCallback host_path_to_config_path, TBeforeRunCallback on_before_run, void** delegate, bool try_ignore_missing_config)
{
    pal::dll_t fxr;

    pal::string_t host_path;
    if (!pal::get_own_module_path(&host_path) || !pal::fullpath(&host_path))
    {
        trace::error(_X("Failed to resolve full path of the current host module [%s]"), host_path.c_str());
        return StatusCode::CurrentHostFindFailure;
    }

    pal::string_t dotnet_root;
    pal::string_t fxr_path;
    if (fxr_resolver::try_get_existing_fxr(&fxr, &fxr_path))
    {
        dotnet_root = get_dotnet_root_from_fxr_path(fxr_path);
        trace::verbose(_X("The library %s was already loaded. Reusing the previously loaded library [%s]."), LIBFXR_NAME, fxr_path.c_str());
    }
    else
    {
        // Do not specify the root path. Getting a delegate does not support self-contained (app-local fxr)
        if (!fxr_resolver::try_get_path(pal::string_t{}, &dotnet_root, &fxr_path))
        {
            return StatusCode::CoreHostLibMissingFailure;
        }

        // We should always be loading hostfxr from an absolute path
        if (!pal::is_path_fully_qualified(fxr_path))
            return StatusCode::CoreHostLibMissingFailure;

        // Load library
        if (!pal::load_library(&fxr_path, &fxr))
        {
            trace::error(_X("The library %s was found, but loading it from %s failed"), LIBFXR_NAME, fxr_path.c_str());
            return StatusCode::CoreHostLibLoadFailure;
        }
    }

    // Leak fxr

    auto hostfxr_initialize_for_runtime_config = reinterpret_cast<hostfxr_initialize_for_runtime_config_fn>(pal::get_symbol(fxr, "hostfxr_initialize_for_runtime_config"));
    auto hostfxr_get_runtime_delegate = reinterpret_cast<hostfxr_get_runtime_delegate_fn>(pal::get_symbol(fxr, "hostfxr_get_runtime_delegate"));
    auto hostfxr_close = reinterpret_cast<hostfxr_close_fn>(pal::get_symbol(fxr, "hostfxr_close"));
    if (hostfxr_initialize_for_runtime_config == nullptr || hostfxr_get_runtime_delegate == nullptr || hostfxr_close == nullptr)
        return StatusCode::CoreHostEntryPointFailure;

    pal::string_t config_path;
    pal::hresult_t status = host_path_to_config_path(host_path, &config_path);
    if (status != StatusCode::Success)
        return status;

    hostfxr_set_error_writer_fn set_error_writer_fn = reinterpret_cast<hostfxr_set_error_writer_fn>(pal::get_symbol(fxr, "hostfxr_set_error_writer"));
    {
        propagate_error_writer_t propagate_error_writer_to_hostfxr(set_error_writer_fn);
        if (!try_ignore_missing_config || pal::file_exists(config_path))
        {
            hostfxr_initialize_parameters parameters {
                sizeof(hostfxr_initialize_parameters),
                host_path.c_str(),
                dotnet_root.c_str()
            };

            hostfxr_handle context;
            int rc = hostfxr_initialize_for_runtime_config(config_path.c_str(), &parameters, &context);
            if (!STATUS_CODE_SUCCEEDED(rc))
                return rc;

            on_before_run(fxr, context);

            rc = hostfxr_get_runtime_delegate(context, type, delegate);

            int rcClose = hostfxr_close(context);
            if (rcClose != StatusCode::Success)
            {
                assert(false && "Failed to close host context");
                trace::verbose(_X("Failed to close host context: 0x%x"), rcClose);
            }

            return rc;
        }
        else
        {
            // null context means use the current one, if none exists it will fail
            int rc = hostfxr_get_runtime_delegate(nullptr, type, delegate);
            if (rc == StatusCode::HostInvalidState)
            {
                trace::error(_X("Expected active runtime context because runtimeconfig.json [%s] does not exist."), config_path.c_str());
            }
            return rc;
        }
    }
}

#endif // __cplusplus

#endif //_COREHOST_CLI_FXR_RESOLVER_H_
