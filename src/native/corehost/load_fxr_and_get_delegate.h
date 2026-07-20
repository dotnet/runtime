// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _COREHOST_LOAD_FXR_AND_GET_DELEGATE_H_
#define _COREHOST_LOAD_FXR_AND_GET_DELEGATE_H_

#include <assert.h>

#include "fxr_resolver.h"

#include "hostfxr.h"
#include "trace.h"
#include "utils.h"
#include "error_codes.h"

template<typename THostPathToConfigCallback, typename TBeforeRunCallback>
int load_fxr_and_get_delegate(hostfxr_delegate_type type, THostPathToConfigCallback host_path_to_config_path, TBeforeRunCallback on_before_run, void** delegate, bool try_ignore_missing_config)
{
    pal_dll_t fxr;

    pal::string_t host_path;
    if (!pal::get_own_module_path(&host_path) || !pal::fullpath(&host_path))
    {
        trace::error(_X("Failed to resolve full path of the current host module [%s]"), host_path.c_str());
        return StatusCode::CurrentHostFindFailure;
    }

    pal::string_t dotnet_root;
    pal::string_t fxr_path;
    pal_char_t* existing_fxr_path = nullptr;
    if (fxr_resolver_try_get_existing_fxr(&fxr, &existing_fxr_path))
    {
        fxr_path.assign(existing_fxr_path);
        free(existing_fxr_path);
        dotnet_root = get_dotnet_root_from_fxr_path(fxr_path);
        trace::verbose(_X("The library %s was already loaded. Reusing the previously loaded library [%s]."), LIBFXR_NAME, fxr_path.c_str());
    }
    else
    {
        bool resolved;
        {
            // Do not specify the root path. Getting a delegate does not support self-contained (app-local fxr)
            pal_char_t* dotnet_root_c = nullptr;
            pal_char_t* fxr_path_c = nullptr;
            resolved = fxr_resolver_try_get_path(nullptr, fxr_search_location_default, nullptr, &dotnet_root_c, &fxr_path_c);
            if (resolved)
            {
                dotnet_root.assign(dotnet_root_c);
                fxr_path.assign(fxr_path_c);
            }
    
            free(dotnet_root_c);
            free(fxr_path_c);
        }

        if (!resolved)
            return StatusCode::CoreHostLibMissingFailure;

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

#endif //_COREHOST_LOAD_FXR_AND_GET_DELEGATE_H_
