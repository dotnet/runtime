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
#include "hostfxr.h"
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
        trace::error(_X("An error occurred while loading required library %s from [%s]"), LIBHOSTPOLICY_NAME, impl_dll_dir.c_str());
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

static char sccsid[] = "@(#)"            \
                       HOST_PKG_VER      \
                       "; Commit Hash: " \
                       REPO_COMMIT_HASH  \
                       "; Built on: "    \
                       __DATE__          \
                       " "               \
                       __TIME__          \
                       ;


hostfxr_init_t g_init;

/**
 *  This export was added to hostfxr.dll since 1.1.*, dotnet.exe from RTM 1.0.0 will not call this export.
 */
SHARED_API int hostfxr_load(const hostfxr_interface_t* input)
{
    trace::setup();

    // Check if there are any breaking changes.
    if (input->version_hi != HOSTFXR_INTERFACE_LAYOUT_VERSION_HI)
    {
        trace::error(_X("The version of the data layout used to initialize %s is [0x%04x]; expected version [0x%04x]"), LIBFXR_NAME, input->version_hi, HOSTFXR_INTERFACE_LAYOUT_VERSION_HI);
        return StatusCode::LibHostInitFailure;
    }
    // Check if the size is at least what we expect to contain.
    if (input->version_lo < HOSTFXR_INTERFACE_LAYOUT_VERSION_LO)
    {
        trace::error(_X("The size of the data layout used to initialize %s is %d; expected at least %d"), LIBFXR_NAME, input->version_lo, HOSTFXR_INTERFACE_LAYOUT_VERSION_LO);
        return StatusCode::LibHostInitFailure;
    }
    trace::verbose(_X("Reading from exe interface version: [0x%04x:%d] to initialize fxr version: [0x%04x:%d]"), input->version_hi, input->version_lo, HOSTFXR_INTERFACE_LAYOUT_VERSION_HI, HOSTFXR_INTERFACE_LAYOUT_VERSION_LO);

    g_init.exe_type = input->exe_type;
    g_init.exe_commit = input->exe_commit;
    g_init.exe_version = input->exe_version;

    return 0;
}

SHARED_API int hostfxr_main(const int argc, const pal::char_t* argv[])
{
    trace::setup();
    
    trace::info(_X("--- Invoked hostfxr [commit hash: %s] main"), _STRINGIFY(REPO_COMMIT_HASH));

    fx_muxer_t muxer;
    return muxer.execute(g_init.exe_type, argc, argv);
}

SHARED_API int hostfxr_unload()
{
    return 0;
}
