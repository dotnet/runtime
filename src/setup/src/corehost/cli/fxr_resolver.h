// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _COREHOST_CLI_FXR_RESOLVER_H_
#define _COREHOST_CLI_FXR_RESOLVER_H_

#include <pal.h>
#include "hostfxr.h"
#include "trace.h"
#include "utils.h"
#include "error_codes.h"

namespace fxr_resolver
{
    bool try_get_path(const pal::string_t& root_path, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path);
    bool try_get_existing_fxr(pal::dll_t *out_fxr, pal::string_t *out_fxr_path);
}

template<typename THostPathToConfigCallback, typename TDelegate>
int load_fxr_and_get_delegate(hostfxr_delegate_type type, THostPathToConfigCallback host_path_to_config_path, TDelegate* delegate)
{
    pal::dll_t fxr;

    pal::string_t host_path;
    if (!pal::get_own_module_path(&host_path) || !pal::realpath(&host_path))
    {
        trace::error(_X("Failed to resolve full path of the current host module [%s]"), host_path.c_str());
        return StatusCode::CoreHostCurHostFindFailure;
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

        // Load library
        if (!pal::load_library(&fxr_path, &fxr))
        {
            trace::error(_X("The library %s was found, but loading it from %s failed"), LIBFXR_NAME, fxr_path.c_str());
            trace::error(_X("  - Installing .NET Core prerequisites might help resolve this problem."));
            trace::error(_X("     %s"), DOTNET_CORE_INSTALL_PREREQUISITES_URL);
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
    {
        return status;
    }

    hostfxr_initialize_parameters parameters {
        sizeof(hostfxr_initialize_parameters),
        host_path.c_str(),
        dotnet_root.c_str()
    };

    hostfxr_handle context;
    int rc = hostfxr_initialize_for_runtime_config(config_path.c_str(), &parameters, &context);
    if (!STATUS_CODE_SUCCEEDED(rc))
        return rc;

    rc = hostfxr_get_runtime_delegate(context, type, reinterpret_cast<void**>(delegate));

    int rcClose = hostfxr_close(context);
    if (rcClose != StatusCode::Success)
    {
        assert(false && "Failed to close host context");
        trace::verbose(_X("Failed to close host context: 0x%x"), rcClose);
    }

    return rc;
}

#endif //_COREHOST_CLI_FXR_RESOLVER_H_
