// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "policy_load.h"
#include "corehost.h"

bool policy_load_t::library_exists_in_dir(const pal::string_t& lib_dir, const pal::string_t& lib_name, pal::string_t* p_host_path)
{
    pal::string_t host_path = lib_dir;
    append_path(&host_path, lib_name.c_str());

    if (!pal::file_exists(host_path))
    {
        return false;
    }
    if (p_host_path)
    {
        *p_host_path = host_path;
    }
    return true;
}

int policy_load_t::load_host_library(
    const pal::string_t& lib_dir,
    pal::dll_t* h_host,
    corehost_load_fn* load_fn,
    corehost_main_fn* main_fn,
    corehost_unload_fn* unload_fn)
{
    pal::string_t host_path;
    if (!library_exists_in_dir(lib_dir, LIBHOST_NAME, &host_path))
    {
        return StatusCode::CoreHostLibMissingFailure;
    }

    // Load library
    if (!pal::load_library(host_path.c_str(), h_host))
    {
        trace::info(_X("Load library of %s failed"), host_path.c_str());
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Obtain entrypoint symbols
    *load_fn = (corehost_load_fn)pal::get_symbol(*h_host, "corehost_load");
    *main_fn = (corehost_main_fn)pal::get_symbol(*h_host, "corehost_main");
    *unload_fn = (corehost_unload_fn)pal::get_symbol(*h_host, "corehost_unload");

    return (*main_fn) && (*load_fn) && (*unload_fn)
        ? StatusCode::Success
        : StatusCode::CoreHostEntryPointFailure;
}

int policy_load_t::execute_app(
    const pal::string_t& impl_dll_dir,
    const corehost_init_t* init,
    const int argc,
    const pal::char_t* argv[])
{
    pal::dll_t corehost;
    corehost_main_fn host_main = nullptr;
    corehost_load_fn host_load = nullptr;
    corehost_unload_fn host_unload = nullptr;

    int code = load_host_library(impl_dll_dir, &corehost, &host_load, &host_main, &host_unload);

    if (code != StatusCode::Success)
    {
        trace::error(_X("Could not load host policy library [%s]"), impl_dll_dir.c_str());
        return code;
    }

    if ((code = host_load(init)) == 0)
    {
        code = host_main(argc, argv);
        (void) host_unload();
    }
    return code;
}

