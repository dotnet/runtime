// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C++ wrappers around the C fxr_resolver_* APIs in fxr_resolver.c. The C++
// surface stays the same; each member just marshals between pal::string_t and
// pal_char_t*. try_get_existing_fxr remains a C++ function because
// pal::get_loaded_library is a templated helper that only has a C++ caller.

#include "fxr_resolver.h"

#include <pal.h>
#include <trace.h>
#include <utils.h>
#include <stdlib.h>

bool fxr_resolver::try_get_path(const pal::string_t& root_path, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path)
{
    return try_get_path(root_path, search_location_default, nullptr, out_dotnet_root, out_fxr_path);
}

bool fxr_resolver::try_get_path(
    const pal::string_t& root_path,
    search_location search,
    /*opt*/ pal::string_t* app_relative_dotnet_root,
    /*out*/ pal::string_t* out_dotnet_root,
    /*out*/ pal::string_t* out_fxr_path)
{
    pal_char_t* dotnet_root_c = nullptr;
    pal_char_t* fxr_path_c = nullptr;
    bool ok = ::fxr_resolver_try_get_path(
        root_path.c_str(),
        static_cast<fxr_search_location>(search),
        app_relative_dotnet_root != nullptr ? app_relative_dotnet_root->c_str() : nullptr,
        &dotnet_root_c,
        &fxr_path_c);

    if (ok)
    {
        out_dotnet_root->assign(dotnet_root_c);
        out_fxr_path->assign(fxr_path_c);
    }

    free(dotnet_root_c);
    free(fxr_path_c);
    return ok;
}

bool fxr_resolver::try_get_path_from_dotnet_root(const pal::string_t& dotnet_root, pal::string_t* out_fxr_path)
{
    pal_char_t* fxr_path_c = nullptr;
    bool ok = ::fxr_resolver_try_get_path_from_dotnet_root(dotnet_root.c_str(), &fxr_path_c);
    if (ok)
        out_fxr_path->assign(fxr_path_c);

    free(fxr_path_c);
    return ok;
}

bool fxr_resolver::try_get_existing_fxr(pal::dll_t* out_fxr, pal::string_t* out_fxr_path)
{
    if (!pal::get_loaded_library(LIBFXR_NAME, "hostfxr_main", out_fxr, out_fxr_path))
        return false;

    trace::verbose(_X("Found previously loaded library %s [%s]."), LIBFXR_NAME, out_fxr_path->c_str());
    return true;
}
