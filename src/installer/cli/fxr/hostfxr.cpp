// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include "trace.h"
#include "pal.h"
#include "utils.h"
#include "fx_ver.h"
#include "fx_muxer.h"
#include "error_codes.h"
#include "libhost.h"
#include "runtime_config.h"

typedef int(*corehost_load_fn) (const host_interface_t* init);
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
    corehost_init_t* init,
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
        trace::error(_X("Could not load host policy library from [%s]"), impl_dll_dir.c_str());
        if (init->fx_dir() == impl_dll_dir)
        {
            pal::string_t name = init->fx_name();
            pal::string_t version = init->fx_version();
            trace::error(_X("This may be because the targeted framework [\"%s\": \"%s\"] was not found."),
                name.c_str(), version.c_str());
        }
        return code;
    }

    const host_interface_t& intf = init->get_host_init_data();
    if ((code = host_load(&intf)) == 0)
    {
        code = host_main(argc, argv);
        (void)host_unload();
    }

    pal::unload_library(corehost);

    return code;
}

bool hostpolicy_exists_in_svc(pal::string_t* resolved_dir)
{
    pal::string_t svc_dir;
    pal::get_default_extensions_directory(&svc_dir);
    append_path(&svc_dir, _X("pkgs"));

    pal::string_t version = _STRINGIFY(HOST_POLICY_PKG_VER);
    pal::string_t rel_dir = _STRINGIFY(HOST_POLICY_PKG_REL_DIR);
    if (DIR_SEPARATOR != '/')
    {
        replace_char(&rel_dir, '/', DIR_SEPARATOR);
    }

    pal::string_t path = svc_dir;
    append_path(&path, _STRINGIFY(HOST_POLICY_PKG_NAME));
    append_path(&path, version.c_str());
    append_path(&path, rel_dir.c_str());

    if (library_exists_in_dir(path, LIBHOSTPOLICY_NAME, nullptr))
    {
        resolved_dir->assign(path);
        trace::verbose(_X("[%s] exists in servicing [%s]"), LIBHOSTPOLICY_NAME, path.c_str());
        return true;
    }
    trace::verbose(_X("[%s] doesn't exist in servicing [%s]"), LIBHOSTPOLICY_NAME, path.c_str());
    return false;
}

SHARED_API int hostfxr_main(const int argc, const pal::char_t* argv[])
{
    trace::setup();

    fx_muxer_t muxer;
    return muxer.execute(argc, argv);
}

