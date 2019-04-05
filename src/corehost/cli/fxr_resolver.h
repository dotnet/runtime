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
}

template<typename THostNameToAppNameCallback, typename TDelegate>
int load_fxr_and_get_delegate(hostfxr_delegate_type type, THostNameToAppNameCallback host_path_to_app_path, TDelegate* delegate, pal::string_t* out_app_path = nullptr)
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
    if (!fxr_resolver::try_get_path(get_directory(host_path), &dotnet_root, &fxr_path))
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

    // Leak fxr

    auto get_delegate_from_hostfxr = (hostfxr_get_delegate_fn)pal::get_symbol(fxr, "hostfxr_get_runtime_delegate");
    if (get_delegate_from_hostfxr == nullptr)
        return StatusCode::CoreHostEntryPointFailure;

    pal::string_t app_path;

    pal::string_t* app_path_to_use = out_app_path != nullptr ? out_app_path : &app_path;

    pal::hresult_t status = host_path_to_app_path(host_path, app_path_to_use);

    if (status != StatusCode::Success)
    {
        return status;
    }
    
    return get_delegate_from_hostfxr(host_path.c_str(), dotnet_root.c_str(), app_path_to_use->c_str(), type, (void**)delegate);
}

#endif //_COREHOST_CLI_FXR_RESOLVER_H_
