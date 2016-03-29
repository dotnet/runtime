// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include "trace.h"
#include "pal.h"
#include "utils.h"
#include "libhost.h"
#include "fx_muxer.h"
#include "error_codes.h"

typedef int(*corehost_load_fn) (const corehost_init_t* init);
typedef int(*corehost_main_fn) (const int argc, const pal::char_t* argv[]);
typedef int(*corehost_unload_fn) ();

int load_host_library(
    const pal::string_t& lib_dir,
    pal::dll_t* h_host,
    corehost_load_fn* load_fn,
    corehost_main_fn* main_fn,
    corehost_unload_fn* unload_fn)
{
    pal::string_t host_path;
    if (!library_exists_in_dir(lib_dir, LIBHOSTPOLICY_NAME, &host_path))
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

int execute_app(
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
        (void)host_unload();
    }

    pal::unload_library(corehost);

    return code;
}

bool hostpolicy_exists_in_svc(pal::string_t* resolved_dir)
{
#ifdef COREHOST_PACKAGE_SERVICING
    pal::string_t svc_dir;
    if (!pal::getenv(_X("DOTNET_SERVICING"), &svc_dir))
    {
        return false;
    }

    pal::string_t path = svc_dir;
    append_path(&path, COREHOST_PACKAGE_NAME);
    append_path(&path, COREHOST_PACKAGE_VERSION);
    append_path(&path, COREHOST_PACKAGE_COREHOST_RELATIVE_DIR);
    if (library_exists_in_dir(path, LIBHOSTPOLICY_NAME))
    {
        resolved_dir->assign(path);
    }
    return true;
#else
    return false;
#endif
}

SHARED_API int hostfxr_main(const int argc, const pal::char_t* argv[])
{
    trace::setup();

    pal::string_t own_dir;
    auto mode = detect_operating_mode(argc, argv, &own_dir);

    switch (mode)
    {
    case muxer:
        {
            trace::info(_X("Host operating in Muxer mode"));
            fx_muxer_t muxer;
            return muxer.execute(argc, argv);
        }

    case split_fx:
        {
            trace::info(_X("Host operating in split mode; own dir=[%s]"), own_dir.c_str());
            corehost_init_t init(_X(""), _X(""), own_dir, host_mode_t::split_fx, nullptr);
            return execute_app(own_dir, &init, argc, argv);
        }

    case standalone:
        {
            trace::info(_X("Host operating from standalone app dir %s"), own_dir.c_str());

            pal::string_t svc_dir;
            corehost_init_t init(_X(""), _X(""), _X(""), host_mode_t::standalone, nullptr);
            return execute_app(
                hostpolicy_exists_in_svc(&svc_dir) ? svc_dir : own_dir, &init, argc, argv);
        }

    default:
        trace::error(_X("Unknown mode detected or could not resolve the mode."));
        return StatusCode::CoreHostResolveModeFailure;
    }
}

